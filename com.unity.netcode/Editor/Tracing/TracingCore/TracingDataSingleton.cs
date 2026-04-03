using System;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode.LowLevel.StateSave;
using Unity.Profiling;
using Unity.Assertions;

namespace Unity.NetCode.Editor.Tracing
{
    // Tracing backend internal class,<see cref="TracingDataAccess"/> for public API.
    // These world singletons are added to the client and server to enable tracing. They hold the raw and processed trace data.
    internal struct TracingDataSingleton : IComponentData, IDisposable
    {
        public WorldData ProcessedWorldData;
        public UnprocessedTraces UnprocessedTraces;
        public NativeHashMap<SavedEntityID, FixedString64Bytes> GhostNames;
        public bool IsCreated;
        readonly WorldID.WorldType m_WorldType;
        internal readonly WorldID m_WorldID;

        public TracingDataSingleton(WorldUnmanaged world, Allocator allocator)
        {
            ProcessedWorldData = new WorldData(allocator);
            UnprocessedTraces = new UnprocessedTraces(allocator);
            m_WorldType = world.IsClient() ? WorldID.WorldType.Client : WorldID.WorldType.Server;
            m_WorldID = new WorldID(world);
            GhostNames = new NativeHashMap<SavedEntityID, FixedString64Bytes>(0, allocator);
            IsCreated = true;
        }

        /// <see cref="StateSave.DisposeForWorld"/>
        public void DisposeForWorld()
        {
            if (!IsCreated)
                return;
            UnprocessedTraces.DisposeForWorld();
        }

        public void Dispose()
        {
            if(!IsCreated)
                return;
            IsCreated = false;
            UnprocessedTraces.EndOfFrameComplete();
            UnprocessedTraces.Dispose();
            DisposeProcessedData();
            if(GhostNames.IsCreated)
                GhostNames.Dispose();
        }

        static readonly ProfilerMarker s_ProcessRawTraceMarker = new ProfilerMarker("ProcessRawTraceData");


        private void DisposeProcessedData()
        {
            if (ProcessedWorldData.IsCreated)
            {
                ProcessedWorldData.Dispose();
            }
        }

        /// <summary>
        /// Create the process traces, store the Frame, Tick, System IDS and metadata with their references to the RawTracingStateSaves stored in RawTraces.
        /// </summary>
        public void ProcessRawTraceData()
        {
            DisposeProcessedData();
            ProcessedWorldData = new WorldData(10, GhostNames, Allocator.Persistent);
            using var marker = s_ProcessRawTraceMarker.Auto();
            ulong counter = 0;
            using var it = UnprocessedTraces.GetEnumerator();
            RawTracingStateSave previousSystemRawData = UnprocessedTraces.FirstElement();
            if (!previousSystemRawData.Initialized)
                return;
            while(it.MoveNext())
            {
                var rawData = it.Current;
                Assert.IsTrue(rawData.Initialized); // sanity check
                var traceType = TraceType.Default;
                if (rawData.system.value == TypeManager.GetSystemTypeIndex<GhostSendSystem>())
                    traceType = TraceType.NetcodeGhostUpdateVsSendComparison;

                if (!ProcessedWorldData.PerFrameData.TryGetValue(rawData.frame, out var frameData))
                {
                    frameData = new FrameData(rawData.frameDeltaTime);
                    ProcessedWorldData.PerFrameData.Add(rawData.frame, frameData);
                    if(!ProcessedWorldData.FrameIDs.Contains(rawData.frame))
                        ProcessedWorldData.FrameIDs.Add(rawData.frame);
                }

                TickData tickData = default;
                // servers have only one instance of a tick, so indexing by tick instead of by frame.
                if ((m_WorldType == WorldID.WorldType.Client) && !frameData.PerTickData.TryGetValue(rawData.tick, out tickData))
                {
                    tickData = new TickData(rawData.tickDeltaTime, rawData.networkTime, traceType);
                    frameData.PerTickData.Add(rawData.tick, tickData);
                    if(!frameData.TickIDs.Contains(rawData.tick))
                        frameData.TickIDs.Add(rawData.tick);
                }
                else if (m_WorldType == WorldID.WorldType.Server && !ProcessedWorldData.PerTickData.TryGetValue(rawData.tick, out tickData))
                {
                    // for servers, there should only be one iteration of a tick, so we're allowing to access them directly from the world in addition to grouping them by frame
                    tickData = new TickData(rawData.tickDeltaTime, rawData.networkTime, traceType);
                    ProcessedWorldData.PerTickData.Add(rawData.tick, tickData);
                    if(!ProcessedWorldData.TickIDs.Contains(rawData.tick))
                        ProcessedWorldData.TickIDs.Add(rawData.tick);
                }

                if (!tickData.PerSystemData.TryGetValue(rawData.system, out var systemData))
                {
                    SystemID systemPair = default; // The system that should be used to compare.
                    if (rawData.tick.Equals(previousSystemRawData.tick) && rawData.system.tracePosition == TracePosition.after && previousSystemRawData.system.tracePosition == TracePosition.before && previousSystemRawData.system.Equivalent(rawData.system))
                    {
                        // for client/server only systems, we try to see if that system did any simulation changes that could influence determinism
                        systemPair = previousSystemRawData.system;
                    }

                    var worldType = m_WorldType;
                    if (rawData.SystemPairOverride.value != default)
                    {
                        systemPair = rawData.SystemPairOverride;
                        worldType = rawData.SystemPairOverrideWorld;
                    }
                    systemData = new SystemData(rawData.system.tracePosition, systemPair, worldType, traceType);
                    var systemId = rawData.system;
                    systemId.executionOrder = ++counter;

                    tickData.PerSystemData.Add(systemId, systemData);
                    if(!tickData.SystemIds.Contains(systemId))
                        tickData.SystemIds.Add(systemId);
                }

                ProcessedTracingStateData tracingStateData = new ProcessedTracingStateData(rawData);
                systemData.MainGameTracingState = tracingStateData;
                tickData.PerSystemData[rawData.system] = systemData;

                previousSystemRawData = rawData;
            }
        }
    }
}
