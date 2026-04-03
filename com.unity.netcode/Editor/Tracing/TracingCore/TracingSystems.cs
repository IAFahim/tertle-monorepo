using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.NetCode.EntitiesInternalAccess;
using Unity.NetCode.LowLevel.StateSave;

namespace Unity.NetCode.Editor.Tracing
{

    /// <summary>
    /// This system is responsible for enabling tracing by creating the TracingDataSingleton.
    /// All other tracing systems RequireForUpdate the presence of the TracingDataSingleton.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    internal partial struct EnableTracingSystem : ISystem
    {
        private bool m_PrintRawTracesRequested;
        private bool m_PrintProcessedTracesRequested;
        private EntityQuery m_TracingDataQuery;
        private EntityQuery m_NetworkStreamInGameQuery;

        public void OnCreate(ref SystemState state)
        {
            if (state.WorldUnmanaged.IsHost())
            {
                state.Enabled = false;
                return;
            }
            m_TracingDataQuery = state.EntityManager.CreateEntityQuery(typeof(TracingDataSingleton));
            m_NetworkStreamInGameQuery = state.EntityManager.CreateEntityQuery(typeof(NetworkStreamInGame));
        }

        public void OnUpdate(ref SystemState state)
        {
            var tracingSingletonPresent = m_TracingDataQuery.TryGetSingleton<TracingDataSingleton>(out var tracingDataSingleton);
            // If we don't want tracing, but the singleton is present remove it.
            if (!TracingDataAccess.Config.Data.EnableTracing)
            {
                if (tracingSingletonPresent)
                {
                    tracingDataSingleton.Dispose();
                    state.EntityManager.DestroyEntity(m_TracingDataQuery);
                }
                return;
            }
            // Otherwise if we want tracing, have a valid connection and the singleton isn't present we create it.
            if (!tracingSingletonPresent && !m_NetworkStreamInGameQuery.IsEmpty)
            {
                tracingDataSingleton = new TracingDataSingleton(state.WorldUnmanaged, Allocator.Persistent);
                if (state.WorldUnmanaged.IsClient())
                    TracingDataAccess.SetClientTracingDataSingleton(tracingDataSingleton);
                else
                    TracingDataAccess.SetServerTracingDataSingleton(tracingDataSingleton);
                var singletonEntity = state.EntityManager.CreateEntity(typeof(TracingDataSingleton));
                state.EntityManager.SetComponentData(singletonEntity, tracingDataSingleton);
            }
        }

        public void OnDestroy(ref SystemState state)
        {
            // Set a static reference so that the singleton and it's data can be read outside playmode.
            // TracingDataAccess is responsible for disposing the singleton.
            if (state.Enabled && m_TracingDataQuery.TryGetSingletonRW<TracingDataSingleton>(out var tracingDataSingleton))
            {
                // Call an early dispose of state saves entity query that flags them as already disposed.
                // That way when we want to later dispose those state saves fully we now not to dispose entity queries because they were already disposed here.
                tracingDataSingleton.ValueRW.DisposeForWorld();
                if (state.WorldUnmanaged.IsClient())
                    TracingDataAccess.SetClientTracingDataSingleton(tracingDataSingleton.ValueRO);
                else
                    TracingDataAccess.SetServerTracingDataSingleton(tracingDataSingleton.ValueRO);
            }
        }
    }

    // This system schedules a trace job on the client at right after the prediction loop.
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [BurstCompile]
    internal partial struct TraceEndPredictionSystem : ISystem
    {
        private EntityQuery m_TracingDataQuery;
        ComponentTypeHandle<GhostInstance> m_GhostInstanceHandle;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TracingDataSingleton>();
            m_TracingDataQuery = state.EntityManager.CreateEntityQuery(typeof(TracingDataSingleton));
            m_GhostInstanceHandle = state.GetComponentTypeHandle<GhostInstance>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_GhostInstanceHandle.Update(ref state);
            m_TracingDataQuery.GetSingletonRW<TracingDataSingleton>().ValueRW.UnprocessedTraces.ScheduleTraceJob(
                ref state,
                new SystemID(TypeManager.GetSystemTypeIndex<TraceEndPredictionSystem>(), tracePosition: TracePosition.netcode),
                state.Dependency,
                m_GhostInstanceHandle
            );
        }
    }

    // This system schedules a trace job on the client at the end of the frame
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(TracingPerFrameFinalizeSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [BurstCompile]
    internal partial struct TraceEndFrameSystem : ISystem
    {
        EntityQuery m_TracingDataQuery;
        ComponentTypeHandle<GhostInstance> m_GhostInstanceHandle;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<TracingDataSingleton>();
            m_TracingDataQuery = state.EntityManager.CreateEntityQuery(typeof(TracingDataSingleton));
            m_GhostInstanceHandle = state.GetComponentTypeHandle<GhostInstance>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tracingDataSingleton = m_TracingDataQuery.GetSingletonRW<TracingDataSingleton>();
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!networkTime.ServerTick.IsValid) return;

            var rawState = new RawTracingStateSave(new WorldID(state.WorldUnmanaged))
            {
                system = new SystemID(TypeManager.GetSystemTypeIndex<TraceEndFrameSystem>(), tracePosition: TracePosition.netcode),
                tick = new TickID() {value = networkTime.ServerTick},
                networkTime = networkTime,
                tickDeltaTime = state.WorldUnmanaged.Time.DeltaTime,
            };
            rawState.SetSystemPairOverride(new SystemID(TypeManager.GetSystemTypeIndex<TraceEndPredictionSystem>(), tracePosition: TracePosition.netcode), WorldID.WorldType.Client);
            m_GhostInstanceHandle.Update(ref state);
            tracingDataSingleton.ValueRW.UnprocessedTraces.ScheduleTraceJob(
                state: ref state,
                currentState: ref rawState,
                saveStrategy: new IndexedByGhostSaveStrategy(m_GhostInstanceHandle),
                dependency: state.Dependency
            );
        }
    }


    /// <summary>
    /// This system calls complete on all the tracing job handle and their dependency at the end of the frame.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    internal partial struct TracingPerFrameFinalizeSystem : ISystem
    {
        private EntityQuery m_TracingDataQuery;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TracingDataSingleton>();
            m_TracingDataQuery = state.EntityManager.CreateEntityQuery(typeof(TracingDataSingleton));
        }

        public void OnUpdate(ref SystemState state)
        {
            m_TracingDataQuery.GetSingletonRW<TracingDataSingleton>().ValueRW.UnprocessedTraces.EndOfFrameComplete();
        }
    }

    internal struct TracingNameCollected : IComponentData {}

    /// <summary>
    /// This system collects the name of all ghosts (or entity) if dots debug names are enabled to later display in UI.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [BurstCompile]
    internal partial struct TracingCollectGhostNamesSystem : ISystem
    {
        private EntityQuery m_TracingDataQuery;
        private EntityQuery m_GhostDataQuery;
        public void OnCreate(ref SystemState state)
        {
            #if DOTS_DISABLE_DEBUG_NAMES
            state.Enabled = false;
            #endif
            m_TracingDataQuery = state.EntityManager.CreateEntityQuery(typeof(TracingDataSingleton));
            m_GhostDataQuery = state.EntityManager.CreateEntityQuery(new EntityQueryBuilder(Allocator.Temp).WithAll<GhostInstance>().WithNone<TracingNameCollected>());
            state.RequireForUpdate(m_TracingDataQuery);
            state.RequireForUpdate(m_GhostDataQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var tracingDataSingleton = m_TracingDataQuery.GetSingletonRW<TracingDataSingleton>().ValueRW;
            using var ghostsToCollect = m_GhostDataQuery.ToEntityArray(Allocator.Temp);

            foreach (var ghost in ghostsToCollect)
            {
                var ghostInstance = state.EntityManager.GetComponentData<GhostInstance>(ghost);
                if(!ghostInstance.spawnTick.IsValid)
                    continue;
                var key = new SavedEntityID(ghostInstance);
                state.EntityManager.GetName(ghost, out var ghostName);
                tracingDataSingleton.GhostNames[key] = ghostName;
            }
            state.EntityManager.AddComponent<TracingNameCollected>(ghostsToCollect);
        }
    }

    /// <summary>
    /// This systems schedule traces jobs before after every system (unless systems are filtered out) using internal entities access.
    /// It also adds the traced component as dependency to the scheduled trace job.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [BurstCompile]
    internal partial class DeepTracingRegistrationSystem : SystemBase
    {
        static readonly SharedStatic<ComponentTypeHandle<GhostInstance>> s_GhostInstanceHandle = SharedStatic<ComponentTypeHandle<GhostInstance>>.GetOrCreate<ComponentTypeHandle<GhostInstance>>();

        protected override void OnCreate()
        {
            RequireForUpdate<TracingDataSingleton>();
            s_GhostInstanceHandle.Data = CheckedStateRef.GetComponentTypeHandle<GhostInstance>(isReadOnly: true);
        }

        protected override void OnUpdate()
        {
            var predictedSimulationSystemGroup = this.World.GetExistingSystemManaged<PredictedSimulationSystemGroup>();
            SetTracingRecursive(predictedSimulationSystemGroup);
            Enabled = false;
        }

        private static void SetTracingRecursive(ComponentSystemGroup parentGroup)
        {
            EntitiesStaticInternalAccessBursted.SetOnUpdateBefore(parentGroup, UpdateBeforeFunctionPointer);
            EntitiesStaticInternalAccessBursted.SetOnUpdateAfter(parentGroup, UpdateAfterFunctionPointer);
            foreach (var sys in parentGroup.ManagedSystems)
            {
                if (TypeManager.IsSystemAGroup(sys.GetType()))
                {
                    var group = (sys as ComponentSystemGroup);
                    SetTracingRecursive(group);
                }
            }
        }

        [BurstCompile]
        private static void UpdateBeforeFunctionPointer(SystemTypeIndex targetSystem, ref SystemState state)
        {
            Trace(targetSystem, ref state, TracePosition.before);
        }

        [BurstCompile]
        private static void UpdateAfterFunctionPointer(SystemTypeIndex targetSystem, ref SystemState state)
        {
            Trace(targetSystem, ref state, TracePosition.after);
        }

        [BurstCompile]
        private static void Trace(SystemTypeIndex targetSystem, ref SystemState state, TracePosition position)
        {
            //todo-next@NetcodeWorld cache this query
            var tracingDataSingleton = new EntityQueryBuilder(Allocator.Temp).WithAllRW<TracingDataSingleton>().Build(ref state).GetSingletonRW<TracingDataSingleton>();
            // If we have filters for which system to trace, check that the targeted system is in the list.
            if(!TracingDataAccess.Config.Data.SystemTypesToTrace.IsEmpty && !TracingDataAccess.Config.Data.SystemTypesToTrace.Contains(targetSystem))
                return;

            var traceTypesToRead = tracingDataSingleton.ValueRW.UnprocessedTraces.GetComponentTypesDependency(Allocator.Temp);
            JobHandle toCompleteForTrace = default;
            if (traceTypesToRead.Length > 0)
            {
                EntitiesStaticInternalAccessBursted.GetDependency(ref state, ref traceTypesToRead, ref toCompleteForTrace);
            }

            s_GhostInstanceHandle.Data.Update(ref state);
            var jobHandle = tracingDataSingleton.ValueRW.UnprocessedTraces.ScheduleTraceJob(ref state, new SystemID(targetSystem, position), toCompleteForTrace, s_GhostInstanceHandle.Data, false);

            if (traceTypesToRead.Length > 0)
            {
                EntitiesStaticInternalAccessBursted.AddDependency(ref state, ref traceTypesToRead, ref jobHandle);
            }
        }
    }
}
