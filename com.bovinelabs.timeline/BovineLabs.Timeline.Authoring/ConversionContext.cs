// <copyright file="ConversionContext.cs" company="BovineLabs">
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
    using Object = UnityEngine.Object;

    /// <summary> Relevant information about the current timeline conversion. </summary>
    public struct BakingContext
    {
        /// <summary> The Conversion System </summary>
        public readonly IBaker Baker;

        /// <summary> The current timer entity for this conversion </summary>
        public Entity Timer;

        /// <summary> The target object being converted. This is the top most gameObject with a PlayableDirector component. </summary>
        public Entity Target;

        /// <summary> The entity representing the current track being converted. </summary>
        public Entity TrackEntity;

        /// <summary> The current playable director being converted. For the top most director, this is the PlayableDirector. </summary>
        public PlayableDirector? Director;

        /// <summary> The current track being converted </summary>
        public TrackAsset? Track;

        /// <summary> The current clip being converted </summary>
        public TimelineClip? Clip;

        /// <summary> The current identifier for the track binding. </summary>
        public Binding? Binding;

        /// <summary> Shared values that should be maintained across context copies during conversion. </summary>
        public SharedContextValues SharedContextValues;

        public BakingContext(IBaker baker, Entity timer, Entity target, PlayableDirector director)
        {
            this.Baker = baker;
            this.Timer = timer;
            this.Target = target;
            this.Director = director;
            this.TrackEntity = default;
            this.Binding = null;
            this.Clip = null;
            this.Track = null;

            this.SharedContextValues = new SharedContextValues();
        }
    }

    /// <summary>
    /// Represents a binding between a DOTS track and its target entity.
    /// </summary>
    public class Binding
    {
        /// <summary> The DOTS track being bound. </summary>
        public readonly DOTSTrack Track;

        /// <summary> The target entity this track is bound to. </summary>
        public readonly Entity Target;

        /// <summary>
        /// Initializes a new instance of the <see cref="Binding"/> class.
        /// </summary>
        /// <param name="track">The DOTS track being bound.</param>
        /// <param name="target">The target entity for the binding.</param>
        public Binding(DOTSTrack track, Entity target)
        {
            this.Track = track;
            this.Target = target;
        }
    }

    /// <summary> Managed object to track values that should be maintained across conversion context copies. </summary>
    public class SharedContextValues
    {
        /// <summary> The current track priorities. </summary>
        public int TrackPriority;

        /// <summary> The current list of clip entities to compile. </summary>
        public List<(Entity ClipEntity, TimelineClip Clip)> ClipEntities = new();

        /// <summary> Dictionary mapping timer entities to their composite timer components. </summary>
        public readonly Dictionary<Entity, CompositeTimer> CompositeTimers = new();

        /// <summary> List of entities that have timer data components. </summary>
        public readonly List<Entity> TimeDataEntities = new();

        /// <summary> List of entities that link to composite timers. </summary>
        public readonly List<Entity> CompositeLinkEntities = new();

        /// <summary> List of bindings and their associated binder entities. </summary>
        public readonly List<(Binding Binding, Entity Binder)> BindingToClip = new();
    }

    /// <summary>
    /// Extension methods for BakingContext
    /// </summary>
    public static class ConversionContextExtensions
    {
        /// <summary>
        /// Creates an entity during timeline baking, binding it to the target conversion object.
        /// </summary>
        /// <param name="context">The baking context.</param>
        /// <param name="name">Optional name for the entity.</param>
        /// <returns>The created entity.</returns>
        public static Entity CreateEntity(this BakingContext context, string? name = null)
        {
            return context.Baker.CreateAdditionalEntity(TransformUsageFlags.None, false, name);
        }

        /// <summary>
        /// Creates a composite timer entity using the context TimelineClip values.
        /// This requires the context to contain an existing Timer (which can be composite).
        /// </summary>
        /// <param name="context">The baking context.</param>
        /// <returns>A new baking context with the composite timer as its timer entity.</returns>
        /// <exception cref="ArgumentException">Thrown if no TimelineClip is supplied.</exception>
        public static BakingContext CreateCompositeTimer(this BakingContext context)
        {
            if (context.Clip == null)
            {
                throw new ArgumentException("CreateCompositeTimer requires a TimelineClip to create a CompositeTimer");
            }

            return CreateCompositeTimer(context, new ActiveRange
                {
                    Start = new DiscreteTime(context.Clip.extrapolatedStart),
                    End = new DiscreteTime(context.Clip.extrapolatedStart) + new DiscreteTime(context.Clip.extrapolatedDuration),
                }, new DiscreteTime(context.Clip.clipIn) + (new DiscreteTime(-context.Clip.start) * context.Clip.timeScale), context.Clip.timeScale,
                context.Clip.displayName + " (Composite Timer)");
        }

        /// <summary>
        /// Creates a composite timer using preset values.
        /// </summary>
        /// <param name="context">The current baking context.</param>
        /// <param name="range">The range, relative to the parent timer, that this timer is active.</param>
        /// <param name="offset">The time offset of this timer relative to the parent timer.</param>
        /// <param name="scale">The scale offset of this timer relative to the parent timer.</param>
        /// <param name="name">The name of the entity.</param>
        /// <returns>A new baking context with the composite timer as its timer entity.</returns>
        /// <exception cref="ArgumentException">Thrown if the context does not contain the required values for creating a composite timer.</exception>
        public static BakingContext CreateCompositeTimer(this BakingContext context, ActiveRange range, DiscreteTime offset, double scale, string name)
        {
            if (context.Timer == default)
            {
                throw new ArgumentException("ConversionContext is invalid for creating a CompositeTimer");
            }

            var entity = context.CreateEntity(name);

            var parentScale = 1.0;
            var parentOffset = DiscreteTime.Zero;
            var masterTimer = context.Timer;

            if (context.SharedContextValues.CompositeTimers.TryGetValue(context.Timer, out var parent))
            {
                parentScale = parent.Scale;
                parentOffset = parent.Offset;
                masterTimer = parent.SourceTimer;
            }

            context.AddActive(entity);

            context.Baker.AddComponent(entity, new Timer { TimeScale = 1 });

            var composite = new CompositeTimer
            {
                SourceTimer = masterTimer,
                Offset = offset + (parentOffset * scale),
                Scale = scale * parentScale,
                ActiveRange = new ActiveRange
                {
                    Start = (range.Start / parentScale) - parentOffset,
                    End = (range.End / parentScale) - parentOffset,
                },
            };

            context.Baker.AddComponent(entity, composite);

            var newContext = context;
            newContext.Timer = entity;

            context.SharedContextValues.CompositeTimers.Add(context.Timer, composite);

            return newContext;
        }

        /// <summary>
        /// Gets or creates a binding between a track and its target entity.
        /// </summary>
        /// <param name="context">The baking context.</param>
        /// <param name="track">The DOTS track to bind.</param>
        /// <param name="trackBinding">The Unity object bound to the track (GameObject or Component).</param>
        /// <returns>A binding object associating the track with its target entity.</returns>
        public static Binding GetBinding(this BakingContext context, DOTSTrack track, Object? trackBinding)
        {
            var entity = Entity.Null;

            if (trackBinding != null)
            {
                entity = trackBinding switch
                {
                    GameObject go => context.Baker.GetEntity(go, TransformUsageFlags.None),
                    Component component => context.Baker.GetEntity(component, TransformUsageFlags.None),
                    _ => Entity.Null,
                };
            }

            return new Binding(track, entity);
        }

        /// <summary>
        /// Adds timeline active tracking components to an entity.
        /// </summary>
        /// <param name="context">The baking context.</param>
        /// <param name="entity">The entity to add components to.</param>
        public static void AddActive(this BakingContext context, Entity entity)
        {
            context.Baker.AddComponent<TimelineActive>(entity);
            context.Baker.SetComponentEnabled<TimelineActive>(entity, false);

            context.Baker.AddComponent<TimelineActivePrevious>(entity);
            context.Baker.SetComponentEnabled<TimelineActivePrevious>(entity, false);
        }

        /// <summary>
        /// Creates an entity representing a timeline track.
        /// </summary>
        /// <param name="context">The baking context.</param>
        /// <returns>The created track entity.</returns>
        /// <exception cref="ArgumentException">Thrown when required context values are missing.</exception>
        internal static Entity CreateTrackEntity(this BakingContext context)
        {
            if (context.Track == null || context.Timer == Entity.Null || context.Binding == null)
            {
                throw new ArgumentException("Track Entities require a track, a timer and a binding");
            }

            var linked = CreateEntity(context, context.Track.name);
            context.AddActive(linked);
            context.Baker.AddComponent(linked, new TrackBinding { Value = context.Binding.Target });
            context.SharedContextValues.BindingToClip.Add((context.Binding, linked));

            context.Baker.AddComponent<TimerData>(linked);
            context.SharedContextValues.TimeDataEntities.Add(linked);
            return linked;
        }

        /// <summary> Create an entity representing a timeline clip. </summary>
        internal static Entity CreateClipEntity(this BakingContext context)
        {
            if (context.Clip == null)
            {
                throw new ArgumentException("context.Clip cannot be null");
            }

            var entity = CreateEntity(context, "context.Clip.displayName");
            ClipBaker.AddClipBaseComponents(context, entity, context.Clip);
            ClipBaker.AddExtrapolationComponents(context, entity, context.Clip);
            ClipBaker.AddMixCurvesComponents(context, entity, context.Clip);

            if (context.Binding != null)
            {
                context.Baker.AddComponent(entity, new TrackBinding { Value = context.Binding.Target });
                context.SharedContextValues.BindingToClip.Add((context.Binding, entity));
            }

            return entity;
        }
    }
}
