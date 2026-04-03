// <copyright file="SubDirectorClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Authoring
{
    using System;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    /// <summary>
    /// A clip that references and embeds another PlayableDirector's timeline into the current timeline.
    /// This enables nested timeline hierarchies where one timeline can trigger and control another.
    /// </summary>
    [Serializable]
    public class SubDirectorClip : DOTSClip, ITimelineClipAsset, IPropertyPreview
    {
        /// <summary> The sub timeline as a playable director. </summary>
        public ExposedReference<PlayableDirector> SubDirector;

        /// <summary> The default duration of the clip based on the referenced timeline's duration. This is set in the editor. </summary>
        [HideInInspector]
        public double DefaultClipDuration = TimelineClip.kDefaultClipDurationInSeconds;

        /// <inheritdoc />
        public ClipCaps clipCaps => ClipCaps.ClipIn | ClipCaps.SpeedMultiplier;

        /// <summary> Gets the default duration of the clip used by the UI, based on the referenced timeline. </summary>
        public override double duration => this.DefaultClipDuration;

        /// <inheritdoc />
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            // instead of returning a single clip entity, compiles the nested timeline.
            var player = this.SubDirector.Resolve(context.Director);
            if (player != null)
            {
                var composites = context.SharedContextValues.CompositeLinkEntities.ToArray();
                context.SharedContextValues.CompositeLinkEntities.Clear();

                context = context.CreateCompositeTimer();
                context.Director = player;

                PlayableDirectorBaker.ConvertPlayableDirector(context, context.Clip!.GetSubTimelineRange());

                context.SharedContextValues.CompositeLinkEntities.Clear();
                context.SharedContextValues.CompositeLinkEntities.AddRange(composites);
                context.SharedContextValues.CompositeLinkEntities.Add(context.Timer);
            }
        }

        /// <summary>
        /// Propagates property gathering calls to the nested sub-timeline for preview purposes.
        /// </summary>
        /// <param name="director">The director playing this clip.</param>
        /// <param name="driver">The property collector to gather properties into.</param>
        public void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            if (director == null)
            {
                return;
            }

            var subDir = this.SubDirector.Resolve(director);
            if (subDir != null)
            {
                var tlAsset = subDir.playableAsset as TimelineAsset;
                if (tlAsset != null)
                {
                    tlAsset.GatherProperties(subDir, driver);
                }
            }
        }

        /// <summary>
        /// Creates a playable that syncs time with the nested PlayableDirector.
        /// </summary>
        /// <param name="graph">The playable graph.</param>
        /// <param name="go">The GameObject with the PlayableDirector component.</param>
        /// <returns>A playable that manages time synchronization with the sub-director.</returns>
        /// <remarks>Needed to ensure the sub director displays the correct time in the Timeline window.</remarks>
        public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
        {
            var director = go.GetComponent<PlayableDirector>();
            if (director != null)
            {
                director = director.GetReferenceValue(this.SubDirector.exposedName, out _) as PlayableDirector;
            }

            var timeSync = ScriptPlayable<TimeSyncBehaviour>.Create(graph);
            timeSync.GetBehaviour().Director = director;

            return timeSync;
        }

        /// <summary>
        /// PlayableBehaviour that synchronizes the time between the parent timeline and nested director.
        /// This ensures the nested timeline displays the correct time during playback.
        /// </summary>
        private class TimeSyncBehaviour : PlayableBehaviour
        {
            /// <summary> The nested PlayableDirector to synchronize time with. </summary>
            public PlayableDirector? Director;

            /// <inheritdoc />
            public override void PrepareFrame(Playable playable, FrameData info)
            {
                if (this.Director != null)
                {
                    this.Director.time = playable.GetTime();
                }
            }
        }
    }
}
