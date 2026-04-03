// <copyright file="PlayableDirectorBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Authoring
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Timeline.Data;
    using BovineLabs.Timeline.Data.Schedular;
    using Unity.Entities;
    using Unity.IntegerTime;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    /// <summary>
    /// Baker for converting Unity Timeline PlayableDirector components to ECS entities with DOTS Timeline components.
    /// This baker handles the conversion of timeline assets, tracks, and clips into their ECS equivalents.
    /// </summary>
    public class PlayableDirectorBaker : Baker<PlayableDirector>
    {
        /// <inheritdoc />
        public override void Bake(PlayableDirector director)
        {
            if (director.playableAsset is not TimelineAsset)
            {
                return;
            }

            var entity = this.GetEntity(TransformUsageFlags.None);
            this.AddComponent(entity, new Timer
            {
                Time = new DiscreteTime(director.initialTime),
                TimeScale = 1,
            });

            this.AddComponent<TimerPaused>(entity);
            this.SetComponentEnabled<TimerPaused>(entity, false);

            this.AddComponent<ClockData>(entity);

            var clockSettings = new ClockSettings
            {
                UpdateMode = ClockUpdateMode.GameTime,
                DeltaTime = DiscreteTime.Zero,
                TimeScale = 1,
                Reverse = false,
            };

            switch (director.timeUpdateMode)
            {
                case DirectorUpdateMode.DSPClock:
                    clockSettings.UpdateMode = ClockUpdateMode.UnscaledGameTime;
                    Debug.LogWarning("DSP Clock mode not yet supported in DOTS. Using realtime clock instead");
                    break;

                case DirectorUpdateMode.GameTime:
                    clockSettings.UpdateMode = ClockUpdateMode.GameTime;
                    break;

                case DirectorUpdateMode.UnscaledGameTime:
                    clockSettings.UpdateMode = ClockUpdateMode.UnscaledGameTime;
                    break;

                case DirectorUpdateMode.Manual:
                    clockSettings.UpdateMode = ClockUpdateMode.Constant;
                    break;
            }

            this.AddComponent(entity, clockSettings);


            var duration = new DiscreteTime(director.playableAsset.duration);
            switch (director.extrapolationMode)
            {
                case DirectorWrapMode.Hold:
                    // Auto Pause
                    this.AddComponent(entity, new TimerRange
                    {
                        Behaviour = RangeBehaviour.AutoPause,
                        Range = new DiscreteTimeInterval(DiscreteTime.Zero, duration),
                    });

                    break;
                case DirectorWrapMode.Loop:
                    this.AddComponent(entity, new TimerRange
                    {
                        Behaviour = RangeBehaviour.Loop,
                        Range = new DiscreteTimeInterval(DiscreteTime.Zero, duration),
                        LoopCount = 0,
                    });

                    break;
                case DirectorWrapMode.None:
                default:
                    // SampleLastFrame is enabled to ensure the final frame is evaluated before stopping
                    this.AddComponent(entity, new TimerRange
                    {
                        Behaviour = RangeBehaviour.AutoStop,
                        Range = new DiscreteTimeInterval(DiscreteTime.Zero, duration),
                        SampleLastFrame = true,
                    });

                    break;
            }

            var context = new BakingContext(this, entity, this.GetEntity(TransformUsageFlags.None), director);

            context.AddActive(entity);

            ConvertPlayableDirector(context, ActiveRange.CompleteRange);

            var binders = this.AddBuffer<DirectorBinding>(entity);
            foreach (var binding in context.SharedContextValues.BindingToClip)
            {
                binders.Add(new DirectorBinding
                {
                    TrackIdentifier = TimelineBakingUtility.TrackToIdentifier(binding.Binding.Track),
                    TrackEntity = binding.Binder,
                });
            }
        }

        /// <summary>
        /// Converts a PlayableDirector and its associated timeline asset into DOTS entities.
        /// </summary>
        /// <param name="context">The baking context containing the director to convert.</param>
        /// <param name="range">The active time range to convert.</param>
        /// <exception cref="ArgumentException">Thrown when context.Director is null.</exception>
        public static void ConvertPlayableDirector(BakingContext context, ActiveRange range)
        {
            if (context.Director == null)
            {
                throw new ArgumentException("context.Director cannot be null");
            }

            var timeline = context.Director.playableAsset as TimelineAsset;
            if (timeline == null)
            {
                return;
            }

            ConvertTimeline(context, timeline, range);
        }

        /// <summary>
        /// Converts a single track and its clips into DOTS entities.
        /// </summary>
        /// <param name="context">The baking context containing the track to convert.</param>
        /// <param name="range">The active time range to convert.</param>
        /// <exception cref="ArgumentException">Thrown when context.Track is not a valid DOTS track.</exception>
        public static void ConvertTrack(BakingContext context, ActiveRange range)
        {
            var track = context.Track as DOTSTrack;
            if (track == null)
            {
                throw new ArgumentException("context.Track must be a valid DOTS track");
            }

            context.Baker.DependsOn(track);
            foreach (var clip in track.GetClips())
            {
                context.Baker.DependsOn(clip.asset);
            }

            track.BakeTrack(context, range);
        }

        private static void ConvertTimeline(BakingContext context, TimelineAsset timeline, ActiveRange range)
        {
            context.Baker.DependsOn(timeline);

            var entity = context.Timer;

            var cachedTimeDataEntities = context.SharedContextValues.TimeDataEntities.ToArray();
            context.SharedContextValues.TimeDataEntities.Clear();

            ConvertTracks(context, timeline.GetDOTSTracks(context.Baker), range);

            var links = context.Baker.AddBuffer<TimerDataLink>(entity);
            foreach (var e in context.SharedContextValues.TimeDataEntities)
            {
                links.Add(new TimerDataLink { Value = e });
            }

            var timerLinks = context.Baker.AddBuffer<CompositeTimerLink>(context.Timer);
            foreach (var link in context.SharedContextValues.CompositeLinkEntities)
            {
                timerLinks.Add(new CompositeTimerLink { Value = link });
            }

            context.SharedContextValues.TimeDataEntities.Clear();
            context.SharedContextValues.TimeDataEntities.AddRange(cachedTimeDataEntities);
        }

        private static void ConvertTracks(BakingContext context, IEnumerable<DOTSTrack> dotsTracks, ActiveRange range)
        {
            foreach (var track in dotsTracks)
            {
                var trackContext = context;
                trackContext.Track = track;
                trackContext.Binding = default;

                var trackBinding = trackContext.Director!.GetGenericBinding(track); // as TrackBaseBinding;

                trackContext.Binding = context.GetBinding(track, trackBinding);

                ConvertTrack(trackContext, range);
            }
        }
    }
}
