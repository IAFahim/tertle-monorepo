// <copyright file="TargetsAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Authoring.Core
{
    using System;
    using BovineLabs.Core.Authoring.ObjectManagement;
    using BovineLabs.Core.PropertyDrawers;
    using BovineLabs.Reaction.Data.Core;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Authoring component for configuring target entities used in reactions.
    /// Defines Owner, Source, and Target entities that can be referenced during reaction execution.
    /// </summary>
    [Serializable]
    [DisallowMultipleComponent]
    public class TargetsAuthoring : MonoBehaviour, ILookupAuthoring<InitializeTarget, InitializeTarget.Data>
    {
        public GameObject? Owner;
        public GameObject? Source;
        public GameObject? Target;

        public TargetsCustomAuthoring Custom = new();

        [PrefabElement]
        [Tooltip("This allows the Target to be changed on Instantiation, for example a buff might target the Owner not the Target.")]
        public InitializeData Initialize = new();

        public static void AddComponents(
            IBaker baker, Entity entity, Entity owner, Entity source, Entity target, bool addCustom, Entity custom0, Entity custom1)
        {
            baker.AddComponent(entity, new Targets
            {
                Owner = owner,
                Source = source,
                Target = target,
            });

            if (addCustom)
            {
                baker.AddComponent(entity, new TargetsCustom
                {
                    Target0 = custom0,
                    Target1 = custom1,
                });
            }
        }

        public bool TryGetInitialization(out InitializeTarget.Data value)
        {
            value = new InitializeTarget.Data { Target = this.Initialize.Target };
            return true;
        }

        /// <summary>
        /// Configuration data for initializing target assignments during instantiation.
        /// </summary>
        [Serializable]
        public class InitializeData
        {
            [Tooltip("What should Target be set to on Instantiation.")]
            public Target Target = Target.Target;
        }

        /// <summary>
        /// Configuration for additional custom target entities (Target0 and Target1).
        /// </summary>
        [Serializable]
        public class TargetsCustomAuthoring
        {
            public bool Enable;
            public GameObject? Target0;
            public GameObject? Target1;
        }

        private class Baker : Baker<TargetsAuthoring>
        {
            public override void Bake(TargetsAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);

                var owner = this.GetEntityOrDefaultRoot(authoring, authoring.Owner);
                var source = this.GetEntityOrDefaultRoot(authoring, authoring.Source);
                var target = this.GetEntity(authoring.Target, TransformUsageFlags.None);

                var target0 = Entity.Null;
                var target1 = Entity.Null;

                if (authoring.Custom.Enable)
                {
                    target0 = this.GetEntity(authoring.Custom.Target0, TransformUsageFlags.None);
                    target1 = this.GetEntity(authoring.Custom.Target1, TransformUsageFlags.None);
                }

                AddComponents(this, entity, owner, source, target, authoring.Custom.Enable, target0, target1);
            }

            private Entity GetEntityOrDefaultRoot(TargetsAuthoring authoring, GameObject? field)
            {
                var entity = this.GetEntity(field, TransformUsageFlags.None);
                if (entity == Entity.Null)
                {
                    entity = this.GetEntity(authoring.transform.root, TransformUsageFlags.None);
                }

                return entity;
            }
        }
    }
}
