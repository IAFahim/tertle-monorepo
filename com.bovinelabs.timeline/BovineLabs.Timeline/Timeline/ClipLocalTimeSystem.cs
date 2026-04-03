// <copyright file="ClipLocalTimeSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline
{
    using BovineLabs.Timeline.Data;
    using BovineLabs.Timeline.Data.Schedular;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.IntegerTime;

    /// <summary>
    /// System that calculates the local time for each clip based on timer data and time transforms.
    /// Handles clip extrapolation (loop, ping-pong, hold) and determines clip active state.
    /// Updates the ClipActive enableable component based on whether the clip's local time is within bounds.
    /// </summary>
    [UpdateInGroup(typeof(TimelineUpdateSystemGroup))]
    public partial struct ClipLocalTimeSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new LocalTimeJob
            {
                ExtrapolationLoopType = SystemAPI.GetComponentTypeHandle<ExtrapolationLoop>(true),
                ExtrapolationPingPongType = SystemAPI.GetComponentTypeHandle<ExtrapolationPingPong>(true),
                ExtrapolationHoldType = SystemAPI.GetComponentTypeHandle<ExtrapolationHold>(true),
            }.ScheduleParallel(state.Dependency);

            state.Dependency = new ResetOnTimelineDeactivatedJob().ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(TimelineActive))]
        [WithPresent(typeof(ClipActive))]
        [WithChangeFilter(typeof(TimerData))]
        private unsafe partial struct LocalTimeJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            [ReadOnly]
            public ComponentTypeHandle<ExtrapolationLoop> ExtrapolationLoopType;

            [ReadOnly]
            public ComponentTypeHandle<ExtrapolationPingPong> ExtrapolationPingPongType;

            [ReadOnly]
            public ComponentTypeHandle<ExtrapolationHold> ExtrapolationHoldType;

            [NativeDisableUnsafePtrRestriction]
            private ExtrapolationLoop* loops;

            [NativeDisableUnsafePtrRestriction]
            private ExtrapolationPingPong* pingPongs;

            [NativeDisableUnsafePtrRestriction]
            private ExtrapolationHold* holds;

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                this.loops = chunk.GetComponentDataPtrRO(ref this.ExtrapolationLoopType);
                this.pingPongs = chunk.GetComponentDataPtrRO(ref this.ExtrapolationPingPongType);
                this.holds = chunk.GetComponentDataPtrRO(ref this.ExtrapolationHoldType);

                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
            {
            }

            private void Execute([EntityIndexInChunk] int entityIndexInChunk, ref LocalTime localTime, in TimerData timerData, in TimeTransform timeTransform,
                EnabledRefRW<ClipActive> clipActive)
            {
                localTime.Value = timeTransform.ToLocalTimeUnbound(timerData.Time);

                if (this.loops != null)
                {
                    UpdateLoop(ref localTime, timerData, timeTransform, this.loops[entityIndexInChunk]);
                }

                if (this.pingPongs != null)
                {
                    UpdatePingPong(ref localTime, timerData, timeTransform, this.pingPongs[entityIndexInChunk]);
                }

                if (this.holds != null)
                {
                    UpdateHold(ref localTime, timerData, timeTransform, this.holds[entityIndexInChunk]);
                }

                clipActive.ValueRW = timeTransform.IsLocalTimeBounded(localTime.Value);
            }

            private static void UpdateLoop(
                ref LocalTime localTime, in TimerData timerData, in TimeTransform timeTransform, in ExtrapolationLoop extrapolationLoop)
            {
                var duration = timeTransform.End - timeTransform.Start;
                if (duration <= DiscreteTime.Zero)
                {
                    localTime.Value = DiscreteTime.Zero;
                }
                else if ((extrapolationLoop.ExtrapolateOptions & ExtrapolationPosition.Pre) != 0 && timerData.Time < timeTransform.Start)
                {
                    var time = timerData.Time - timeTransform.Start;
                    time = duration - (-time % duration);
                    localTime.Value = (time * timeTransform.Scale) + timeTransform.ClipIn;
                }
                else if ((extrapolationLoop.ExtrapolateOptions & ExtrapolationPosition.Post) != 0 && timerData.Time >= timeTransform.End)
                {
                    var time = (timerData.Time - timeTransform.Start) % duration;
                    localTime.Value = (time * timeTransform.Scale) + timeTransform.ClipIn;
                }
            }

            private static void UpdatePingPong(
                ref LocalTime localTime, in TimerData timerData, in TimeTransform timeTransform, in ExtrapolationPingPong extrapolationPingPong)
            {
                var duration = timeTransform.End - timeTransform.Start;
                if (duration <= DiscreteTime.Zero)
                {
                    localTime.Value = DiscreteTime.Zero;
                }
                else if ((extrapolationPingPong.ExtrapolateOptions & ExtrapolationPosition.Pre) != 0 && timerData.Time < timeTransform.Start)
                {
                    var time = timerData.Time - timeTransform.Start;
                    time = (duration * 2) - (-time % (duration * 2));
                    time = duration - (time - duration).Abs();
                    localTime.Value = (time * timeTransform.Scale) + timeTransform.ClipIn;
                }
                else if ((extrapolationPingPong.ExtrapolateOptions & ExtrapolationPosition.Post) != 0 && timerData.Time >= timeTransform.End)
                {
                    var time = timerData.Time - timeTransform.Start;
                    time %= duration * 2;
                    time = duration - (time - duration).Abs();
                    localTime.Value = (time * timeTransform.Scale) + timeTransform.ClipIn;
                }
            }

            private static void UpdateHold(
                ref LocalTime localTime, in TimerData timerData, in TimeTransform timeTransform, in ExtrapolationHold extrapolationHold)
            {
                if ((extrapolationHold.ExtrapolateOptions & ExtrapolationPosition.Pre) != 0 && timerData.Time < timeTransform.Start)
                {
                    localTime.Value = timeTransform.ClipIn;
                }
                else if ((extrapolationHold.ExtrapolateOptions & ExtrapolationPosition.Post) != 0 && timerData.Time >= timeTransform.End)
                {
                    localTime.Value = ((timeTransform.End - timeTransform.Start) * timeTransform.Scale) + timeTransform.ClipIn;
                }
            }
        }

        // If we deactivate the timeline then LocalTimeJob won't update so we just need to reset ClipActive as well
        [WithDisabled(typeof(TimelineActive))]
        private partial struct ResetOnTimelineDeactivatedJob : IJobEntity
        {
            private void Execute(EnabledRefRW<ClipActive> clipActive)
            {
                clipActive.ValueRW = false;
            }
        }
    }
}
