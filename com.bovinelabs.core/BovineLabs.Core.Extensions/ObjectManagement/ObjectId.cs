// <copyright file="ObjectId.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using System;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Properties;

    /// <summary>
    /// Wrapper for the ID of an object. This can be used to store weak references to entities
    /// that can be instantiated at runtime via <see cref="ObjectDefinitionRegistry" />.
    /// </summary>
    [Serializable]
    public readonly struct ObjectId : IComponentData, IEquatable<ObjectId>, IComparable<ObjectId>
    {
        public const int MaxModsIds = 1 << ModBytes;

        private const int ModBytes = 10;
        private const int ModShift = 32 - ModBytes;
        private const int IDMask = (1 << ModShift) - 1;

        public static readonly ObjectId Null = default;

        [CreateProperty(ReadOnly = true)]
        private readonly int rawValue;

        public ObjectId(int id, ushort mod = 0)
        {
#if UNITY_EDITOR
            if (id > IDMask)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "Id too large");
            }

            if (mod >= MaxModsIds)
            {
                throw new ArgumentOutOfRangeException(nameof(mod), "Mod id too large");
            }
#endif

            this.rawValue = mod << ModShift | id;
        }

        public int RawValue => this.rawValue;

        [CreateProperty]
        public ushort Mod => (ushort)(this.rawValue >> ModShift);

        [CreateProperty]
        public int ID => this.rawValue & IDMask;

        public static bool operator ==(ObjectId left, ObjectId right)
        {
            return left.ID == right.ID;
        }

        public static bool operator !=(ObjectId left, ObjectId right)
        {
            return left.ID != right.ID;
        }

        /// <inheritdoc/>
        public int CompareTo(ObjectId other)
        {
            return this.rawValue.CompareTo(other.rawValue);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"ID:{this.ID}";
        }

        public FixedString32Bytes ToFixedString()
        {
            return $"ID:{this.ID}";
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is ObjectId other && this.Equals(other);
        }

        /// <inheritdoc />
        public bool Equals(ObjectId other)
        {
            return this.rawValue == other.rawValue;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return this.rawValue;
        }
    }
}
#endif
