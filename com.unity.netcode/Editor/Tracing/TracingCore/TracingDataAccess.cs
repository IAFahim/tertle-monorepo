using Unity.Burst;
using Unity.Entities;
using Unity.NetCode.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.NetCode.Editor.Tracing
{
    // Tracing backend public class
    internal struct TracingDataAccess
    {
        // Public shared static for the config that enables tracing and what to trace.
        public static readonly SharedStatic<UnmanagedConfig> Config = SharedStatic<UnmanagedConfig>.GetOrCreate<TracingDataAccess>();

        // Before bootstrapping reset the config to make sure this session has to enable tracing.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void ResetConfig()
        {
            Config.Data.Dispose();
        }

        internal struct WorldsSaveSize
        {
            public int ClientTracesSize;
            public bool ServerShouldReset;
            public bool ClientShouldReset;
            public int ServerTracesSize;
            public int MaxTracesSizeBytes;
        }

        // Private shared static to keep track of the total size used by both the client and server traces and reset timing.
        internal static readonly SharedStatic<WorldsSaveSize> s_WorldsSaveSize = SharedStatic<WorldsSaveSize>.GetOrCreate<WorldsSaveSize>();
        private static bool m_IsCreated;
        private static TracingDataSingleton s_Client;
        private static TracingDataSingleton s_Server;
        /// <summary>
        /// True if the TracingDataSingletons have been processed and the ProcessedWorldData is up to date with the unprocessed traces, false otherwise.
        /// </summary>
        internal static bool IsProcessed;

        /// <summary>
        /// TracingDataAccess is responsible for disposing of client and server singleton/traces since it's used to access those outside of playmode.
        /// </summary>
        private static void Init()
        {
            if(m_IsCreated)
                return;
            EditorApplication.pauseStateChanged += OnPauseStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += DisposeAllWorlds;
            EditorApplication.quitting += DisposeAllWorlds;

            EditorApplication.playModeStateChanged += OnEditorApplicationOnPlayModeStateChanged;
            m_IsCreated = true;
            IsProcessed = false;
            s_Client = default;
            s_Server = default;
            ResetStateSaveSizes();
        }

        private static void ResetStateSaveSizes()
        {
            s_WorldsSaveSize.Data.ClientTracesSize = 0;
            s_WorldsSaveSize.Data.ServerTracesSize = 0;
            s_WorldsSaveSize.Data.ClientShouldReset = false;
            s_WorldsSaveSize.Data.ServerShouldReset = false;
            s_WorldsSaveSize.Data.MaxTracesSizeBytes = (int)(TracingConfig.instance.TracingMemoryLimitMb*1024*1024);
        }

        private static void OnEditorApplicationOnPlayModeStateChanged(PlayModeStateChange state)
        {
            if(state == PlayModeStateChange.ExitingEditMode)
                DisposeAllWorlds();
        }


        /// <summary>
        /// Adds the size of the new server trace to total size used by tracing, and checks if we need to reset the tracing window to not go over memory limits.
        /// </summary>
        /// <param name="sizeBytes">Size in bytes of the StateSave that was written to the server tracingDataSingleton unprocess trace array.</param>
        /// <returns></returns>
        internal static bool AddServerTraceSize(int sizeBytes)
        {
            s_WorldsSaveSize.Data.ServerTracesSize += sizeBytes;
            if ( s_WorldsSaveSize.Data.ServerShouldReset)
            {
                s_WorldsSaveSize.Data.ServerShouldReset = false;
                return true;
            }
            if ( s_WorldsSaveSize.Data.ClientTracesSize +  s_WorldsSaveSize.Data.ServerTracesSize >  s_WorldsSaveSize.Data.MaxTracesSizeBytes)
            {
                s_WorldsSaveSize.Data.ClientTracesSize = 0;
                s_WorldsSaveSize.Data.ServerTracesSize = sizeBytes;
                s_WorldsSaveSize.Data.ClientShouldReset = true;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds the size of the new client trace to total size used by tracing, and checks if we need to reset the tracing window to not go over memory limits.
        /// </summary>
        /// <param name="sizeBytes">Size in bytes of the StateSave that was written to the client tracingDataSingleton unprocess trace array.</param>
        /// <returns></returns>
        internal static bool AddClientTraceSize(int sizeBytes)
        {
            s_WorldsSaveSize.Data.ClientTracesSize += sizeBytes;
            if ( s_WorldsSaveSize.Data.ClientShouldReset)
            {
                s_WorldsSaveSize.Data.ClientShouldReset = false;
                return true;
            }
            if ( s_WorldsSaveSize.Data.ClientTracesSize +  s_WorldsSaveSize.Data.ServerTracesSize >  s_WorldsSaveSize.Data.MaxTracesSizeBytes)
            {
                s_WorldsSaveSize.Data.ClientTracesSize = sizeBytes;
                s_WorldsSaveSize.Data.ServerTracesSize = 0;
                s_WorldsSaveSize.Data.ServerShouldReset = true;
                return true;
            }
            return false;
        }

        private static void OnPauseStateChanged(PauseState state)
        {
            if (state == PauseState.Unpaused)
            {
                IsProcessed = false; // Tracing could be resuming
            }
        }

        // TracingDataAccess is responsible for disposing of client and server singleton/traces since it's used to access those outside of playmode.
        private static void DisposeAllWorlds()
        {
            if (!m_IsCreated)
                return;
            AllTypeDiffers.Instance.Dispose();
            Config.Data.Dispose();
            if(s_Client.IsCreated)
                s_Client.Dispose();
            if(s_Server.IsCreated)
                s_Server.Dispose();
            EditorApplication.pauseStateChanged -= OnPauseStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= DisposeAllWorlds;
            EditorApplication.quitting -= DisposeAllWorlds;
            EditorApplication.playModeStateChanged -= OnEditorApplicationOnPlayModeStateChanged;
            m_IsCreated = false;
        }

        /// <summary>
        /// Get the most up to date TracingDataSingletons for client and server, process their raw trace data and then process the client server diff.
        /// </summary>
        /// <returns>Returns true if the diff was processed successfully.</returns>
        private static bool ProcessDiff()
        {
            // First get updated world singletons if available.
            RefRW<TracingDataSingleton> clientSingleton = default;
            RefRW<TracingDataSingleton> serverSingleton = default;
            foreach (var world in World.All)
            {
                if (world.IsServer())
                {
                    if (serverSingleton.IsValid)
                    {
                        Debug.LogWarning("Tracing only supports one server world.");
                        return false;
                    }

                    if (world.EntityManager.CreateEntityQuery(typeof(TracingDataSingleton))
                        .TryGetSingletonRW<TracingDataSingleton>(out var serverData))
                    {
                        serverSingleton = serverData;
                    }
                }
                else if (world.IsClient())
                {
                    if (clientSingleton.IsValid)
                    {
                        Debug.LogWarning("Tracing only supports one client world.");
                        return false;
                    }

                    if (world.EntityManager.CreateEntityQuery(typeof(TracingDataSingleton))
                        .TryGetSingletonRW<TracingDataSingleton>(out var clientData))
                    {
                        clientSingleton = clientData;
                    }
                }
            }

            // If we are during playmode with tracing running, we use the updated singletons.
            if (clientSingleton.IsValid && clientSingleton.ValueRO.IsCreated && serverSingleton.IsValid && serverSingleton.ValueRO.IsCreated)
            {
                clientSingleton.ValueRW.ProcessRawTraceData();
                serverSingleton.ValueRW.ProcessRawTraceData();
                clientSingleton.ValueRW.ProcessedWorldData.ProcessDiff(serverSingleton.ValueRW.ProcessedWorldData);
                s_Client = clientSingleton.ValueRO;
                s_Server = serverSingleton.ValueRO;
                IsProcessed = EditorApplication.isPaused || !Config.Data.EnableTracing; // We can cache the result if the editor is paused, or we paused tracing
            }
            else
            {
                // Outside playmode we can use the singleton references that wre set by EnableTracingSystem's OnDestroy.
                if (!s_Client.IsCreated || !s_Server.IsCreated)
                    return false;
                s_Client.ProcessRawTraceData();
                s_Server.ProcessRawTraceData();
                s_Client.ProcessedWorldData.ProcessDiff(s_Server.ProcessedWorldData);
                IsProcessed = true;
            }
            return true;
        }


        /// <summary>
        /// Getter for the processed tracing data for both client and server.
        /// Will trigger processing of the raw trace data if it hasn't been processed yet, or if the singletons have been updated with new traces since last processing.
        /// </summary>
        internal static (WorldData ClientWorldData, WorldData ServerWorldData) GetProcessedWorldsData()
        {
            if(!m_IsCreated)
                Init();
            if (!IsProcessed)
            {
                if (!ProcessDiff())
                    return (default, default);
            }
            return (s_Client.ProcessedWorldData, s_Server.ProcessedWorldData);
        }

        /// <summary>
        /// Setter for the client tracing data singletons.
        /// Should be called by the system that handles the client TracingDataSingleton
        /// before exiting playmode so we have an updated reference to the unprocessed traces outside playmode/ecs worlds.
        /// </summary>
        /// <param name="value">The client tracing data singleton</param>
        internal static void SetClientTracingDataSingleton(TracingDataSingleton value)
        {
            if(!m_IsCreated)
                Init();
            if (s_Client.IsCreated && s_Client.m_WorldID.value != value.m_WorldID.value)
            {
                s_Client.Dispose();
                ResetStateSaveSizes();
            }
            s_Client = value;
            IsProcessed = false;
        }


        /// <summary>
        /// Setter for the server tracing data singletons.
        /// Should be called by the system that handles the server TracingDataSingleton
        /// before exiting playmode so we have an updated reference to the unprocessed traces outside playmode/ecs worlds.
        /// </summary>//
        /// <param name="value">The server tracing data singleton</param>
        internal static void SetServerTracingDataSingleton(TracingDataSingleton value)
        {
            if(!m_IsCreated)
                Init();
            if (s_Server.IsCreated && s_Server.m_WorldID.value != value.m_WorldID.value)
            {
                s_Server.Dispose();
                ResetStateSaveSizes();
            }
            s_Server = value;
            IsProcessed = false;
        }
    }
}
