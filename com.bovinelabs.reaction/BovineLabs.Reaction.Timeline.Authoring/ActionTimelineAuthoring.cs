// <copyright file="ActionTimelineAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Timeline.Authoring
{
    using System;
    using BovineLabs.Reaction.Authoring;
    using BovineLabs.Reaction.Authoring.Core;
    using BovineLabs.Reaction.Data.Core;
    using BovineLabs.Reaction.Timeline.Data;
    using BovineLabs.Timeline.Authoring;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Playables;

    [ReactionAuthoring]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ReactionAuthoring))]
    public class ActionTimelineAuthoring : MonoBehaviour
    {
        public Data[] Timelines = Array.Empty<Data>();

        [Serializable]
        public class Data
        {
            public PlayableDirector Director;

            public float InitialTime;

            public bool DisableTimelineOnDeactivate = true;

            [Tooltip("if this is true, if the reaction triggers and it's already active then it will reset the timeline and start again. " +
                "If it's false, it'll prevent any new activation until the current one is done. " +
                "Note that if deactivate is true then it should not be possible to hit the reset case and this does nothing.")]
            public bool ResetWhenActive;

            public Binding[] Bindings = Array.Empty<Binding>();

            [Serializable]
            public class Binding
            {
                public DOTSTrack Track;
                public Target Target;
            }
        }

        /// <inheritdoc />
        private class Baker : Baker<ActionTimelineAuthoring>
        {
            /// <inheritdoc />
            public override void Bake(ActionTimelineAuthoring authoring)
            {
                if (authoring.Timelines.Length == 0)
                {
                    return;
                }

                var entity = this.GetEntity(TransformUsageFlags.None);

                var timelines = this.AddBuffer<ActionTimeline>(entity);
                var bindings = this.AddBuffer<ActionTimelineBinding>(entity);

                for (byte index = 0; index < authoring.Timelines.Length; index++)
                {
                    var timeline = authoring.Timelines[index];

                    timelines.Add(new ActionTimeline
                    {
                        Director = this.GetEntity(timeline.Director, TransformUsageFlags.None),
                        InitialTime = timeline.InitialTime,
                        DisableTimelineOnDeactivate = timeline.DisableTimelineOnDeactivate,
                        ResetWhenActive = timeline.ResetWhenActive,
                    });

                    foreach (var binding in timeline.Bindings)
                    {
                        if (binding.Track == null)
                        {
                            continue;
                        }

                        this.DependsOn(binding.Track);

                        // No point binding if target is none
                        if (binding.Target == Target.None)
                        {
                            continue;
                        }

                        bindings.Add(new ActionTimelineBinding
                        {
                            Index = index,
                            TrackIdentifier = TimelineBakingUtility.TrackToIdentifier(binding.Track),
                            Target = binding.Target,
                        });
                    }
                }
            }
        }
    }
}