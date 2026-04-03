// <copyright file="StatSchemaObject.cs" company="BovineLabs">
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
    /// ScriptableObject that defines a stat schema with a unique identifier.
    /// This schema object is used to define stat types in the game and integrates with the condition system.
    /// Automatically managed by the StatSettings and provides implicit conversion to StatKey.
    /// </summary>
    [AutoRef("EssenceSettings", "statSchemas", nameof(StatSchemaObject), "Schemas/Stats")]
    public sealed class StatSchemaObject : ConditionSchemaObject, IUID
    {
        [SerializeField]
        [InspectorReadOnly]
        private ushort key;

        // [SerializeField]
        // [Tooltip("If enabled, only StatModifyType.Added will be allowed. Using Additive or Multiplicative will result in an error.")]
        // private bool addOnly;

        // /// <summary> An optional default value that will be used anywhere this stat is used and hasn't been given a default value. </summary>
        // [SerializeField]
        // private short defaultValue;

        /// <inheritdoc/>
        int IUID.ID
        {
            get => this.key;
            set
            {
                if (value is < 0 or > ushort.MaxValue)
                {
                    Debug.LogError("Ran out of keys");
                    return;
                }

                this.key = (ushort)value;
            }
        }

        // public bool AddOnly => this.addOnly;

        /// <inheritdoc/>
        public override ushort Key => this.key;

        /// <inheritdoc/>
        public override string ConditionType => ConditionTypes.StatType;

        // public short DefaultValue => this.defaultValue;

        public static implicit operator StatKey(StatSchemaObject? stat)
        {
            return stat == null ? 0 : stat.key;
        }
    }
}
