// <copyright file="ReactionAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Authoring.Core
{
    using BovineLabs.Core.Authoring.LifeCycle;
    using BovineLabs.Reaction.Authoring.Active;
    using BovineLabs.Reaction.Authoring.Conditions;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Core authoring component for the Reaction system that combines active states and conditions.
    /// This component requires LifeCycleAuthoring and TargetsAuthoring to function properly.
    /// </summary>
    [RequireComponent(typeof(LifeCycleAuthoring))]
    [RequireComponent(typeof(TargetsAuthoring))]
    [DisallowMultipleComponent]
    public class ReactionAuthoring : MonoBehaviour
    {
        public ActiveAuthoring Active = new();
        public ConditionAuthoring Conditions = new();

        private void OnValidate()
        {
            this.Conditions.OnValidate(this.Active);
        }

        private class Baker : Baker<ReactionAuthoring>
        {
            public override void Bake(ReactionAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);

                authoring.Active.Bake(this, entity);
                authoring.Conditions.Bake(this, entity);
            }
        }
    }
}
