// <copyright file="EventWriterAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Authoring.Conditions
{
    using BovineLabs.Core.Authoring.LifeCycle;
    using BovineLabs.Reaction.Data.Conditions;
    using Unity.Entities;
    using Unity.Entities.Hybrid.Baking;
    using UnityEngine;

    /// <summary>
    /// Authoring component that enables an entity to be able to send condition events.
    /// </summary>
    [DisallowMultipleComponent]
    [ReactionAuthoring]
    [RequireComponent(typeof(LinkedEntityGroupAuthoring))]
    [RequireComponent(typeof(LifeCycleAuthoring))]
    public class EventWriterAuthoring : MonoBehaviour
    {
        [Tooltip("Will only add the EventSubscriber buffer so things can subscribe but won't send events. Use when this entity is only using custom events.")]
        [SerializeField]
        private bool subscriptionOnly;

        private class Baker : Baker<EventWriterAuthoring>
        {
            /// <inheritdoc />
            public override void Bake(EventWriterAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);

                this.AddBuffer<EventSubscriber>(entity);

                if (authoring.subscriptionOnly)
                {
                    return;
                }

                this.AddBuffer<ConditionEvent>(entity).Initialize();
                this.AddComponent<EventsDirty>(entity);
                this.SetComponentEnabled<EventsDirty>(entity, false);
            }
        }
    }
}
