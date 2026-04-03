// <copyright file="TimerRangeImpl.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Schedular
{
    using BovineLabs.Timeline.Data;
    using BovineLabs.Timeline.Data.Schedular;
    using Unity.Entities;
    using Unity.IntegerTime;

    /// <summary>
    /// Helper methods for applying timer range behavior.
    /// </summary>
    public static class TimerRangeImpl
    {
        /// <summary>
        /// Applies the configured range behavior to the timer.
        /// </summary>
        /// <param name="timer">The timer to update.</param>
        /// <param name="range">The range settings to apply.</param>
        /// <param name="previousTime">The previous timer time, used for sampling behavior.</param>
        /// <param name="timerPauseds">The timer paused enableable reference.</param>
        /// <param name="actives">The timeline active enableable reference.</param>
        /// <param name="reverse">True if the timer is running in reverse.</param>
        public static void ApplyTimerRange(
            ref Timer timer, ref TimerRange range, DiscreteTime previousTime, EnabledRefRW<TimerPaused> timerPauseds,
            EnabledRefRW<TimelineActive> actives, bool reverse)
        {
            switch (range.Behaviour)
            {
                case RangeBehaviour.AutoStop:
                    if (reverse)
                    {
                        ApplyAutoStopReverse(ref timer, range, previousTime, actives);
                    }
                    else
                    {
                        ApplyAutoStop(ref timer, range, previousTime, actives);
                    }

                    break;
                case RangeBehaviour.AutoPause:
                    if (reverse)
                    {
                        ApplyAutoPauseReverse(ref timer, range, timerPauseds);
                    }
                    else
                    {
                        ApplyAutoPause(ref timer, range, timerPauseds);
                    }

                    break;
                case RangeBehaviour.Loop:
                    if (reverse)
                    {
                        ApplyLoopReverse(ref timer, ref range);
                    }
                    else
                    {
                        ApplyLoop(ref timer, ref range);
                    }

                    break;
            }
        }

        private static void ApplyAutoStop(ref Timer timer, in TimerRange range, DiscreteTime previousTime, EnabledRefRW<TimelineActive> actives)
        {
            timer.Time = timer.Time.Max(range.Range.Start);
            if (timer.Time >= range.Range.End)
            {
                if (range.SampleLastFrame && previousTime < range.Range.End)
                {
                    timer.Time = range.Range.End;
                }
                else
                {
                    timer.Time = range.Range.Start;
                    actives.ValueRW = false;
                }
            }
        }

        private static void ApplyAutoStopReverse(ref Timer timer, in TimerRange range, DiscreteTime previousTime, EnabledRefRW<TimelineActive> actives)
        {
            timer.Time = timer.Time.Min(range.Range.End);
            if (timer.Time <= range.Range.Start)
            {
                if (range.SampleLastFrame && previousTime > range.Range.Start)
                {
                    timer.Time = range.Range.Start;
                }
                else
                {
                    timer.Time = range.Range.End;
                    actives.ValueRW = false;
                }
            }
        }

        private static void ApplyAutoPause(ref Timer timer, in TimerRange clamp, EnabledRefRW<TimerPaused> timerPauseds)
        {
            timer.Time = clamp.Range.Clamp(timer.Time);
            if (timer.Time == clamp.Range.End)
            {
                timerPauseds.ValueRW = true;
            }
        }

        private static void ApplyAutoPauseReverse(ref Timer timer, in TimerRange clamp, EnabledRefRW<TimerPaused> timerPauseds)
        {
            timer.Time = clamp.Range.Clamp(timer.Time);
            if (timer.Time == clamp.Range.Start)
            {
                timerPauseds.ValueRW = true;
            }
        }

        private static void ApplyLoop(ref Timer timer, ref TimerRange range)
        {
            if (timer.Time < range.Range.Start)
            {
                timer.Time = range.Range.Start;
            }
            else if (timer.Time >= range.Range.End)
            {
                if (range.Range.Start == range.Range.End)
                {
                    timer.Time = range.Range.Start;
                }
                else
                {
                    var deltaTicks = range.Range.Duration.Value;
                    var timeTicks = timer.Time.Value - range.Range.Start.Value;
                    range.LoopCount += (uint)(timeTicks / deltaTicks);
                    timer.Time = DiscreteTime.FromTicks(range.Range.Start.Value + (timeTicks % deltaTicks));
                }
            }
        }

        private static void ApplyLoopReverse(ref Timer timer, ref TimerRange range)
        {
            if (timer.Time > range.Range.End)
            {
                timer.Time = range.Range.End;
            }
            else if (timer.Time < range.Range.Start)
            {
                if (range.Range.Start == range.Range.End)
                {
                    timer.Time = range.Range.Start;
                }
                else
                {
                    var deltaTicks = range.Range.Duration.Value;
                    var timeTicks = range.Range.Start.Value - timer.Time.Value;
                    range.LoopCount += (uint)(timeTicks / deltaTicks);
                    var remainder = timeTicks % deltaTicks;
                    timer.Time = remainder == 0
                        ? range.Range.Start
                        : DiscreteTime.FromTicks(range.Range.End.Value - remainder);
                }
            }
        }
    }
}
