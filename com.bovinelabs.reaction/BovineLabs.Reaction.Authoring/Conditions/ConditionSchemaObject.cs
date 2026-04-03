// <copyright file="ConditionSchemaObject.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Authoring.Conditions
{
    using UnityEngine;

    /// <summary>
    /// The base scriptable object for defining custom conditions.
    /// </summary>
    public abstract class ConditionSchemaObject : ScriptableObject
    {
        [SerializeField]
        [Tooltip("If ticked it is expected only 1 entity in the project will exist that can invoke this condition" +
                 "which needs to be explicitly registered using GlobalConditionAuthoring.")]
        private bool isGlobal;

        /// <summary>
        /// Gets a value indicating whether is this condition is an event and if so, its value is automatically reset every frame.
        /// If false, it's treated as a persistent state.
        /// </summary>
        public virtual bool IsEvent => false;

        /// <summary>
        /// Gets the unique key within the <see cref="ConditionType"/>.
        /// </summary>
        public abstract ushort Key { get; }

        /// <summary>
        /// Gets the type of condition that needs to make to ConditionTypes settings.
        /// </summary>
        public abstract string ConditionType { get; }

        /// <summary>
        /// Gets a value indicating whether this is a global condition where only one will ever exist.
        /// </summary>
        public bool IsGlobal => this.isGlobal;
    }
}
