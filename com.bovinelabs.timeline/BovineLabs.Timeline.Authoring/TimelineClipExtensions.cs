// <copyright file="TimelineClipExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Authoring
{
    using System.Collections.Generic;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Timeline.Data.Schedular;
    using Unity.IntegerTime;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Timeline;

    /// <summary>
    /// Extension methods for Unity's TimelineClip to support DOTS timeline operations.
    /// </summary>
    public static class TimelineClipExtensions
    {
        /// <summary>
        /// Gets the active time range for a timeline clip, including extrapolation.
        /// </summary>
        /// <param name="clip">The timeline clip to get the range from.</param>
        /// <returns>An ActiveRange representing the clip's start and end times.</returns>
        public static ActiveRange GetActiveRange(this TimelineClip clip)
        {
            var activeRange = new ActiveRange
            {
                Start = new DiscreteTime(clip.extrapolatedStart),
                End = new DiscreteTime(clip.extrapolatedStart) + new DiscreteTime(clip.extrapolatedDuration),
            };

            return activeRange;
        }

        /// <summary>
        /// Gets the time transform that converts parent timeline time to local clip time.
        /// </summary>
        /// <param name="clip">The timeline clip to get the transform from.</param>
        /// <returns>A TimeTransform containing start, end, scale, and clip-in offset.</returns>
        public static TimeTransform GetTimeTransform(this TimelineClip clip)
        {
            return new TimeTransform
            {
                Start = new DiscreteTime(clip.start),
                End = new DiscreteTime(clip.end),
                Scale = clip.timeScale,
                ClipIn = new DiscreteTime(clip.clipIn),
            };
        }

        /// <summary>
        /// Creates a single animation curve that represents the blending curve of the entire clip.
        /// Combines mix-in and mix-out curves into a unified weight curve in local time.
        /// </summary>
        /// <param name="clip">The timeline clip to create the weight curve from.</param>
        /// <returns>An AnimationCurve representing the clip's weight over time, or null if the clip has no blend weights.</returns>
        public static AnimationCurve? CreateClipWeightCurve(this TimelineClip? clip)
        {
            if (clip == null)
            {
                return null;
            }

            if (clip.mixInDuration < float.Epsilon && clip.mixOutDuration < float.Epsilon)
            {
                return null;
            }

            // Add the mix in and mix out keys together. the curves should end and start at 1
            var keys = new List<Keyframe>(10);
            if (clip.mixInDuration >= float.Epsilon)
            {
                var range = new float2((float)clip.clipIn, (float)clip.ToLocalTime(clip.mixInDuration + clip.start));
                var curve = clip.mixInCurve;

                // remap keys to local time
                var mixInKeys = curve.keys;
                for (var i = 0; i < mixInKeys.Length; i++)
                {
                    mixInKeys[i].time = (mixInKeys[i].time * (range.y - range.x)) + range.x;
                }

                keys.AddRange(mixInKeys);
            }

            if (clip.mixOutDuration >= float.Epsilon)
            {
                var range = new float2((float)clip.ToLocalTime(clip.end - clip.mixOutDuration), (float)clip.ToLocalTime(clip.end));
                var curve = clip.mixOutCurve;

                // remap keys to local time
                var mixOutKeys = curve.keys;
                for (var i = 0; i < mixOutKeys.Length; i++)
                {
                    mixOutKeys[i].time = (mixOutKeys[i].time * (range.y - range.x)) + range.x;
                }

                keys.AddRange(mixOutKeys);
            }

            if (keys.Count > 0)
            {
                return new AnimationCurve(keys.ToArray());
            }

            return null;
        }

        /// <summary>
        /// Checks if a timeline clip is within the specified range, accounting for timeline loops.
        /// </summary>
        /// <param name="clip">The timeline clip to check.</param>
        /// <param name="activeRange">The active range to check against. If larger than the timeline's range, it accounts for loops.</param>
        /// <returns>True if the clip is within the active range (including loops); otherwise, false.</returns>
        public static bool InRangeInclLoops(this TimelineClip clip, ActiveRange activeRange)
        {
            var clipRange = clip.GetActiveRange();
            var timelineRange = default(ActiveRange);
            var parentTrack = clip.GetParentTrack();

            if (parentTrack != null && parentTrack.timelineAsset != null)
            {
                timelineRange = parentTrack.timelineAsset.GetRange();
            }

            return InRangeInclLoops(clipRange, activeRange, timelineRange);
        }

        /// <summary>
        /// Checks if a clip range is within the specified active range, accounting for timeline loops.
        /// This is the static version that works with pre-calculated ranges.
        /// </summary>
        /// <param name="clipRange">The time range of the clip.</param>
        /// <param name="activeRange">The active range to check against. If larger than the timeline's range, it accounts for loops.</param>
        /// <param name="timelineRange">The full range of the parent timeline asset.</param>
        /// <returns>True if the clip range is within the active range (including loops); otherwise, false.</returns>
        public static bool InRangeInclLoops(ActiveRange clipRange, ActiveRange activeRange, ActiveRange timelineRange)
        {
            if (!activeRange.IsValid())
            {
                return false;
            }

            // the range overlaps the clip
            if (clipRange.Overlaps(activeRange))
            {
                return true;
            }

            if (!timelineRange.IsValid())
            {
                return false;
            }

            // if the active range is larger, this clip needs to be included
            if (timelineRange.Length() < activeRange.Length())
            {
                return true;
            }

            // if the other range is completely contained in the timeline range
            if (timelineRange.Contains(activeRange))
            {
                return false;
            }

            // compute the range at the start and end of the clip that the loops represent
            var endRange = timelineRange;
            endRange.Start = activeRange.Start % timelineRange.Length();

            var startRange = timelineRange;
            startRange.End = activeRange.End % timelineRange.Length();

            return endRange.Overlaps(clipRange) || startRange.Overlaps(clipRange);
        }

        /// <summary>
        /// Gets the active time range of a nested sub-timeline referenced by this clip.
        /// Converts the clip's extrapolated range to local timeline time.
        /// </summary>
        /// <param name="clip">The timeline clip containing a sub-timeline reference.</param>
        /// <returns>An ActiveRange representing the portion of the sub-timeline to play.</returns>
        public static ActiveRange GetSubTimelineRange(this TimelineClip clip)
        {
            return new ActiveRange
            {
                Start = new DiscreteTime(clip.ToLocalTimeUnbound(clip.extrapolatedStart)),
                End = new DiscreteTime(clip.ToLocalTimeUnbound(clip.extrapolatedStart + clip.extrapolatedDuration)),
            };
        }
    }
}
