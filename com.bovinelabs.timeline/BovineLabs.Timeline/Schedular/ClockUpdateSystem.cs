// <copyright file="ClockUpdateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Schedular
{
    using BovineLabs.Timeline.Data;
    using BovineLabs.Timeline.Data.Schedular;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.IntegerTime;
    using UnityEngine;

    /// <summary>
    /// System that captures time update data from different clocks
    /// Copies from ClockTypeXXX Component types to ClockData
    /// ClockData is used by the timer system to update timers
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.Presentation)]
    [UpdateBefore(typeof(TimerUpdateSystem))]
    [UpdateInGroup(typeof(ScheduleSystemGroup))]
    public partial struct ClockUpdateSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ClockGameTimeUpdateJob
            {
                GameTimeScale = Time.timeScale,
                GameTimeDeltaTime = new DiscreteTime(SystemAPI.Time.DeltaTime),
            }.ScheduleParallel();

            new ClockUnscaledGameTimeUpdateJob
            {
                UnscaledGameTimeDeltaTime = new DiscreteTime(Time.unscaledDeltaTime),
            }.ScheduleParallel();

            new ClockConstantTimeUpdateJob().ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(ClockTypeGameTime))]
        [WithAll(typeof(TimelineActive))]
        private partial struct ClockGameTimeUpdateJob : IJobEntity
        {
            public DiscreteTime GameTimeDeltaTime;
            public double GameTimeScale;

            private void Execute(ref ClockData clockData)
            {
                clockData.DeltaTime = this.GameTimeDeltaTime;
                clockData.Scale = this.GameTimeScale;
            }
        }

        [BurstCompile]
        [WithAll(typeof(ClockTypeUnscaledGameTime))]
        [WithAll(typeof(TimelineActive))]
        private partial struct ClockUnscaledGameTimeUpdateJob : IJobEntity
        {
            public DiscreteTime UnscaledGameTimeDeltaTime;

            private void Execute(ref ClockData clockData)
            {
                clockData.DeltaTime = this.UnscaledGameTimeDeltaTime;
                clockData.Scale = 1;
            }
        }

        [BurstCompile]
        [WithAll(typeof(TimelineActive))]
        private partial struct ClockConstantTimeUpdateJob : IJobEntity
        {
            private static void Execute(ref ClockData clockData, in ClockTypeConstant constant)
            {
                clockData.DeltaTime = constant.DeltaTime;
                clockData.Scale = constant.TimeScale;
            }
        }
    }
}
