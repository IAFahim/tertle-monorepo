// <copyright file="EditorTimelineSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Editor
{
    using BovineLabs.Timeline.Data;
    using BovineLabs.Timeline.Data.Schedular;
    using BovineLabs.Timeline.Schedular;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.IntegerTime;
    using UnityEditor.Timeline;
    using UnityEngine;

    [WorldSystemFilter(WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(TimelineSystemGroup), OrderFirst = true)]
    public partial class EditorTimelineSystem : SystemBase
    {
        /// <inheritdoc />
        protected override void OnUpdate()
        {
            // force reset for editor world
            var missingReset = SystemAPI.QueryBuilder().WithNone<TrackResetOnDeactivate>().WithAll<Track>().Build();
            this.EntityManager.AddComponent<TrackResetOnDeactivate>(missingReset);

            var isActiveQuery = SystemAPI.QueryBuilder().WithAll<Timer, TimelineActive>().Build();

            this.ResetActive(isActiveQuery);
            this.EnableSelected(isActiveQuery);
        }

        private static TimelineWindow[] GetAllOpenEditorWindows()
        {
            return Resources.FindObjectsOfTypeAll<TimelineWindow>();
        }

        private void Disable(Entity entity)
        {
            this.EntityManager.SetComponentEnabled<TimelineActive>(entity, false);
            this.EntityManager.SetComponentData(entity, new Timer
            {
                Time = new DiscreteTime(0),
                TimeScale = 1,
            });
        }

        private void ResetActive(EntityQuery isActiveQuery)
        {
            // Reset everything in case of deselection so that it'll return to state at 0
            // If something is selected the time here will just be overridden next
            foreach (var e in isActiveQuery.ToEntityArray(this.WorldUpdateAllocator))
            {
                this.Disable(e);
            }
        }

        private void EnableSelected(EntityQuery isActiveQuery)
        {
            var entities = new NativeList<Entity>(this.WorldUpdateAllocator);
            var mask = isActiveQuery.GetEntityQueryMask();

            foreach (var w in GetAllOpenEditorWindows())
            {
                if (w.state?.masterSequence?.director == null)
                {
                    continue;
                }

                var director = w.state.masterSequence.director;

                if (director.time >= director.duration || director.time <= 0)
                {
                    // If outside window, allow it to remain disabled
                    continue;
                }

                this.EntityManager.Debug.GetEntitiesForAuthoringObject(director, entities);

                foreach (var e in entities)
                {
                    if (!mask.MatchesIgnoreFilter(e))
                    {
                        continue;
                    }

                    this.EntityManager.SetComponentEnabled<TimelineActive>(e, true);
                    this.EntityManager.SetComponentData(e, new Timer
                    {
                        Time = new DiscreteTime(director.time),
                        TimeScale = 1,
                    });

                    break;
                }
            }
        }
    }
}
