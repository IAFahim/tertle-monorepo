// <copyright file="ClipBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Authoring
{
    using BovineLabs.Core.Authoring;
    using BovineLabs.Core.Collections;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Timeline.Data.Schedular;
    using Unity.Entities;
    using Unity.IntegerTime;
    using UnityEngine.Timeline;

    /// <summary>
    /// Utility class for baking Unity Timeline clips into DOTS entities with the appropriate components.
    /// </summary>
    public static class ClipBaker
    {
        /// <summary>
        /// Adds the base components required for all timeline clips to function in DOTS.
        /// This includes timing, transform, active state, and optional blending components.
        /// </summary>
        /// <param name="context">The baking context.</param>
        /// <param name="clipEntity">The entity representing the clip.</param>
        /// <param name="clip">The source timeline clip.</param>
        public static void AddClipBaseComponents(BakingContext context, Entity clipEntity, TimelineClip clip)
        {
            context.SharedContextValues.TimeDataEntities.Add(clipEntity);

            context.AddActive(clipEntity);

            context.Baker.AddComponent(clipEntity, new Clip { Track = context.TrackEntity });
            context.Baker.AddComponent<TimerData>(clipEntity);
            context.Baker.AddComponent(clipEntity, clip.GetTimeTransform());
            context.Baker.AddComponent(clipEntity, clip.GetActiveRange());
            context.Baker.AddComponent(clipEntity, new LocalTime { Value = DiscreteTime.Zero });
            context.Baker.AddEnabledComponent<ClipActive>(clipEntity, false);
            context.Baker.AddEnabledComponent<ClipActivePrevious>(clipEntity, false);

            if ((clip.clipCaps & ClipCaps.Blending) != 0)
            {
                context.Baker.AddComponent(clipEntity, new ClipWeight { Value = 1 });
            }
        }

        /// <summary>
        /// Adds extrapolation components for clips that have pre or post extrapolation enabled.
        /// Extrapolation determines how a clip behaves before its start time or after its end time.
        /// </summary>
        /// <param name="context">The baking context.</param>
        /// <param name="clipEntity">The entity representing the clip.</param>
        /// <param name="clip">The source timeline clip.</param>
        public static void AddExtrapolationComponents(BakingContext context, Entity clipEntity, TimelineClip clip)
        {
            if (clip.hasPreExtrapolation || clip.hasPostExtrapolation)
            {
                // 'Continue' is default behaviour, so it has no component
                // It is valid for one clip to have multiple extrapolation types, one on pre, one on post
                var options = GetExtrapolationOptions(clip, TimelineClip.ClipExtrapolation.Hold);
                if (options != ExtrapolationPosition.None)
                {
                    context.Baker.AddComponent(clipEntity, new ExtrapolationHold
                    {
                        ExtrapolateOptions = options,
                    });
                }

                options = GetExtrapolationOptions(clip, TimelineClip.ClipExtrapolation.Loop);
                if (options != ExtrapolationPosition.None)
                {
                    context.Baker.AddComponent(clipEntity, new ExtrapolationLoop
                    {
                        ExtrapolateOptions = options,
                    });
                }

                options = GetExtrapolationOptions(clip, TimelineClip.ClipExtrapolation.PingPong);
                if (options != ExtrapolationPosition.None)
                {
                    context.Baker.AddComponent(clipEntity, new ExtrapolationPingPong
                    {
                        ExtrapolateOptions = options,
                    });
                }
            }
        }

        /// <summary>
        /// Adds animated weight components for clips that have blend curves (mix in/out).
        /// This handles the smooth blending of clips when they overlap.
        /// </summary>
        /// <param name="context">The baking context.</param>
        /// <param name="clipEntity">The entity representing the clip.</param>
        /// <param name="clip">The source timeline clip.</param>
        public static void AddMixCurvesComponents(BakingContext context, Entity clipEntity, TimelineClip clip)
        {
            var dotsClip = clip.asset as DOTSClip;
            if (dotsClip == null)
            {
                return;
            }

            var curve = clip.CreateClipWeightCurve();

            if (curve != null)
            {
                var blobCurve = BlobCurve.Create(curve);
                context.Baker.AddBlobAsset(ref blobCurve, out _);

                context.Baker.AddComponent(clipEntity, new AnimatedClipWeight
                {
                    Value = new BlobCurveSampler(blobCurve),
                });
            }
        }

        /// <summary>
        /// Gets the extrapolation positions that match a specific extrapolation mode.
        /// </summary>
        /// <param name="clip">The timeline clip to inspect.</param>
        /// <param name="mode">The extrapolation mode to match.</param>
        /// <returns>The extrapolation positions that use the specified mode.</returns>
        private static ExtrapolationPosition GetExtrapolationOptions(TimelineClip clip, TimelineClip.ClipExtrapolation mode)
        {
            var options = ExtrapolationPosition.None;
            if (clip.hasPreExtrapolation && clip.preExtrapolationMode == mode)
            {
                options |= ExtrapolationPosition.Pre;
            }

            if (clip.hasPostExtrapolation && clip.postExtrapolationMode == mode)
            {
                options |= ExtrapolationPosition.Post;
            }

            return options;
        }
    }
}
