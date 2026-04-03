// <copyright file="ConditionEventObject.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Authoring.Conditions
{
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Core.PropertyDrawers;
    using BovineLabs.Reaction.Data.Conditions;
    using UnityEngine;

    /// <summary>
    /// The default event based condition triggered using EventWriter that will automatically reset next frame.
    /// Use this for things like, OnHit, OnBlock, GainedLife etc.
    /// </summary>
    [AutoRef("ReactionSettings", "conditionEvents", nameof(ConditionEventObject), "Schemas/Events/")]
    public sealed class ConditionEventObject : ConditionSchemaObject, IUID
    {
        [SerializeField]
        [InspectorReadOnly]
        private ConditionKey key;

        /// <inheritdoc/>
        public override bool IsEvent => true;

        /// <inheritdoc/>
        public override ushort Key => this.key.Value;

        /// <inheritdoc/>
        public override string ConditionType => ConditionTypes.EventType;

        /// <inheritdoc/>
        int IUID.ID
        {
            get => this.key;
            set => this.key = value;
        }

        public static implicit operator ConditionKey(ConditionEventObject? obj)
        {
            return obj ? obj.key : ConditionKey.Null;
        }
    }
}