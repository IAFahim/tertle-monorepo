// <copyright file="SubTimelineClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Authoring
{
    using System;
    using BovineLabs.Timeline.Data.Schedular;
    using Unity.Entities;
    using UnityEngine.Timeline;

    /// <summary>
    /// A clip that embeds a TimelineAsset directly (without requiring a PlayableDirector).
    /// Unlike SubDirectorClip, this allows you to directly reference timeline assets and configure track bindings.
    /// Useful for reusable timeline compositions with custom bindings.
    /// </summary>
    [Serializable]
    public class SubTimelineClip : DOTSClip, ITimelineClipAsset
    {
        /// <summary> The timeline asset to embed in this clip. </summary>
        public TimelineAsset Timeline;

        /// <summary> Track bindings that map tracks in the timeline to target objects. </summary>
        public TrackKeyBindings TrackBindings;

        /// <inheritdoc />
        public ClipCaps clipCaps => ClipCaps.ClipIn | ClipCaps.SpeedMultiplier;

        /// <inheritdoc />
        public override double duration => this.Timeline != null ? this.Timeline.duration : base.duration;

        /// <inheritdoc />
        /// <remarks>
        /// Bakes the embedded timeline asset into DOTS entities, creating a composite timer
        /// and converting all DOTS-compatible tracks with their configured bindings.
        /// </remarks>
        public override void Bake(Entity clipEntity, BakingContext context)
        {
            if (this.Timeline != null)
            {
                var composites = context.SharedContextValues.CompositeLinkEntities.ToArray();
                var cachedTimeDataEntities = context.SharedContextValues.TimeDataEntities.ToArray();
                context.SharedContextValues.CompositeLinkEntities.Clear();
                context.SharedContextValues.TimeDataEntities.Clear();

                context.Baker.DependsOn(this.Timeline);

                var range = context.Clip!.GetSubTimelineRange();
                var newContext = context.CreateCompositeTimer();
                newContext.Director = null;

                foreach (var track in this.Timeline.GetDOTSTracks(context.Baker))
                {
                    newContext.Track = track;
                    newContext.Clip = null;
                    newContext.Binding = context.GetBinding(track, this.TrackBindings.FindObject(track));

                    PlayableDirectorBaker.ConvertTrack(newContext, range);
                }

                var links = context.Baker.AddBuffer<TimerDataLink>(newContext.Timer);
                foreach (var e in context.SharedContextValues.TimeDataEntities)
                {
                    links.Add(new TimerDataLink { Value = e });
                }

                var timerLinks = context.Baker.AddBuffer<CompositeTimerLink>(newContext.Timer);
                foreach (var link in context.SharedContextValues.CompositeLinkEntities)
                {
                    timerLinks.Add(new CompositeTimerLink { Value = link });
                }

                context.SharedContextValues.TimeDataEntities.Clear();
                context.SharedContextValues.TimeDataEntities.AddRange(cachedTimeDataEntities);

                context.SharedContextValues.CompositeLinkEntities.Clear();
                context.SharedContextValues.CompositeLinkEntities.AddRange(composites);
                context.SharedContextValues.CompositeLinkEntities.Add(newContext.Timer);
            }
        }

        private void OnValidate()
        {
            if (this.Timeline != null)
            {
                this.TrackBindings.SyncToTimeline(this.Timeline);
            }
        }
    }
}
