// <copyright file="ClockUpdateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Schedular
{
    using BovineLabs.Core;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Timeline.Data.Schedular;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.IntegerTime;
    using UnityEngine;

    /// <summary>
    /// System that captures time update data from different clocks.
    /// Copies from <see cref="ClockSettings"/> to <see cref="ClockData"/>.
    /// ClockData is used by the timer system to update timers.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.Presentation | Worlds.Menu)]
    [UpdateBefore(typeof(TimerUpdateSystem))]
    [UpdateInGroup(typeof(ScheduleSystemGroup))]
    public partial struct ClockUpdateSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ClockUpdateJob
            {
                GameTimeScale = Time.timeScale,
                GameTimeDeltaTime = new DiscreteTime(SystemAPI.Time.DeltaTime),
                UnscaledGameTimeDeltaTime = new DiscreteTime(Time.unscaledDeltaTime),
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(TimelineActive))]
        private partial struct ClockUpdateJob : IJobEntity
        {
            public DiscreteTime GameTimeDeltaTime;
            public DiscreteTime UnscaledGameTimeDeltaTime;
            public double GameTimeScale;

            private void Execute(ref ClockData clockData, in ClockSettings clockSettings)
            {
                var deltaTime = DiscreteTime.Zero;
                var timeScale = 1.0;

                switch (clockSettings.UpdateMode)
                {
                    case ClockUpdateMode.GameTime:
                        deltaTime = this.GameTimeDeltaTime;
                        timeScale = this.GameTimeScale;
                        break;
                    case ClockUpdateMode.UnscaledGameTime:
                        deltaTime = this.UnscaledGameTimeDeltaTime;
                        timeScale = 1;
                        break;
                    case ClockUpdateMode.Constant:
                        deltaTime = clockSettings.DeltaTime;
                        timeScale = clockSettings.TimeScale;
                        break;
                }

                if (clockSettings.Reverse)
                {
                    deltaTime = -deltaTime;
                }

                clockData.DeltaTime = deltaTime;
                clockData.Scale = timeScale;
            }
        }
    }
}
