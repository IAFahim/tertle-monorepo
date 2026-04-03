// <copyright file="ConditionGlobalAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Authoring.Conditions
{
    using System;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Reaction.Data.Conditions;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Authoring component for global conditions.
    /// The entity this is assigned to will be stored and linked to all conditions assigned to <see cref="ConditionGlobalAuthoring.conditions"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EventWriterAuthoring))]
    public class ConditionGlobalAuthoring : MonoBehaviour
    {
        [Tooltip("All the global conditions registered to this entity. Conditions in this list should only be registered in 1 place to 1 entity.")]
        [SerializeField]
        private ConditionSchemaObject[] conditions = Array.Empty<ConditionSchemaObject>();

        /// <summary>
        /// Baker for <see cref="ConditionGlobalAuthoring"/>.
        /// </summary>
        private class Baker : Baker<ConditionGlobalAuthoring>
        {
            /// <inheritdoc />
            public override void Bake(ConditionGlobalAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);

                var buffer = this.AddBuffer<ConditionGlobal>(entity);

                var conditionTypes = this.DependsOn(AuthoringSettingsUtility.GetSettings<ConditionTypes>());

                foreach (var condition in authoring.conditions)
                {
                    if (!condition)
                    {
                        continue;
                    }

                    this.DependsOn(condition);

                    if (!condition.IsGlobal)
                    {
                        Debug.LogError($"{authoring} is trying to register condition as global that has not been marked as IsGlobal.");
                        continue;
                    }

                    buffer.Add(new ConditionGlobal(condition.Key, conditionTypes[condition.ConditionType]));
                }
            }
        }
    }
}
