// <copyright file="IntrinsicSchemaObject.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Authoring
{
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Core.PropertyDrawers;
    using BovineLabs.Essence.Data;
    using BovineLabs.Reaction.Authoring.Conditions;
    using BovineLabs.Reaction.Data.Conditions;
    using UnityEngine;

    /// <summary>
    /// ScriptableObject that defines an intrinsic schema with a unique identifier, default value, and valid range.
    /// This schema object is used to define intrinsic types in the game and integrates with the condition system.
    /// Automatically managed by the StatSettings and provides implicit conversion to IntrinsicKey.
    /// </summary>
    [AutoRef("EssenceSettings", "intrinsicSchemas", nameof(IntrinsicSchemaObject), "Schemas/Intrinsics/")]
    public sealed class IntrinsicSchemaObject : ConditionSchemaObject, IUID
    {
        [InspectorReadOnly]
        [SerializeField]
        private IntrinsicKey key;

        [SerializeField]
        private int defaultValue;

        [MinMax(int.MinValue, int.MaxValue)]
        [SerializeField]
        private Vector2Int range;

        [SerializeField]
        private StatSchemaObject? minStat;

        [SerializeField]
        private StatSchemaObject? maxStat;

        /// <inheritdoc/>
        int IUID.ID
        {
            get => this.key;
            set => this.key = value;
        }

        /// <inheritdoc/>
        public override ushort Key => this.key;

        /// <inheritdoc/>
        public override string ConditionType => ConditionTypes.IntrinsicType;

        public Vector2Int Range => this.range;

        public int DefaultValue => this.defaultValue;

        public StatSchemaObject? MinStat => this.minStat;

        public StatSchemaObject? MaxStat => this.maxStat;

        public static implicit operator IntrinsicKey(IntrinsicSchemaObject? intrinsic)
        {
            return intrinsic == null ? 0 : intrinsic.key;
        }
    }
}
