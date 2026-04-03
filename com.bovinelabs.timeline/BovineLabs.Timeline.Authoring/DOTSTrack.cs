// <copyright file="DOTSTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Authoring
{
    using BovineLabs.Timeline.Data;
    using BovineLabs.Timeline.Data.Schedular;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    /// <summary>
    /// Base class for all DOTS-compatible timeline tracks.
    /// Extends Unity's TrackAsset with ECS baking functionality.
    /// </summary>
    public abstract class DOTSTrack : TrackAsset
    {
        [SerializeField]
        [Tooltip("If the track supports it, enables resetting state of target after the track is finished. Note this is forced on in the editor world.")]
        private bool resetOnDeactivate = true;

        /// <summary>
        /// Bakes this track and its clips into DOTS entities.
        /// </summary>
        /// <param name="context">The baking context.</param>
        /// <param name="range">The active time range to bake.</param>
        public void BakeTrack(BakingContext context, ActiveRange range)
        {
            context.TrackEntity = context.CreateTrackEntity();

            if (this.resetOnDeactivate)
            {
                context.Baker.AddComponent<TrackResetOnDeactivate>(context.TrackEntity);
            }

            foreach (var clip in this.GetActiveClipsFromAllLayers())
            {
                if (!clip.InRangeInclLoops(range))
                {
                    continue;
                }

                var dotsClip = clip.asset as DOTSClip;
                if (dotsClip == null)
                {
                    continue;
                }

                var clipContext = context;
                clipContext.Clip = clip;

                var clipEntity = dotsClip.CreateClipEntity(clipContext);
                if (clipEntity != Entity.Null)
                {
                    clipContext.Baker.AddComponent(clipEntity, new DirectorRoot { Director = context.Target });

                    context.SharedContextValues.ClipEntities.Add((clipEntity, clip));

                    dotsClip.Bake(clipEntity, clipContext);
                }
            }

            this.Bake(context);
            context.SharedContextValues.ClipEntities.Clear();
            context.SharedContextValues.CompositeTimers.Clear();
        }

        /// <inheritdoc />
        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            base.GatherProperties(director, driver);
            if (director.GetGenericBinding(this) is IPropertyPreview preview)
            {
                preview.GatherProperties(director, driver);
            }
        }

        /// <summary>
        /// Override this method to add custom baking logic for derived track types.
        /// Called after all clips have been baked.
        /// </summary>
        /// <param name="context">The baking context.</param>
        protected virtual void Bake(BakingContext context)
        {
        }
    }
}
