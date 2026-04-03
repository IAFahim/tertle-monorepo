// <copyright file="InitializeTransform.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Core
{
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.ObjectManagement;
    using Unity.Entities;

    /// <summary>
    /// Dynamic hash map buffer for configuring transform initialization of created entities based on target positions.
    /// </summary>
    [InternalBufferCapacity(0)]
    public struct InitializeTransform : IDynamicHashMap<ObjectId, InitializeTransform.Data>
    {
        /// <inheritdoc />
        byte IDynamicHashMap<ObjectId, Data>.Value { get; }

        /// <summary>
        /// Configuration data for initializing entity transforms with position, rotation, and scale settings.
        /// </summary>
        public struct Data
        {
            public Target From;
            public Target To;
            public bool ApplyInitialTransform;

            public PositionInit Position;
            public RotationInit Rotation;
            public ScaleInit Scale;

            public enum PositionInit : byte
            {
                /// <summary> Don't set the position. </summary>
                None,

                /// <summary> Set the position to the From target. </summary>
                From,

                /// <summary> Set the position to the To target. </summary>
                To,
            }

            public enum RotationInit : byte
            {
                /// <summary> Don't set the rotation. </summary>
                None,

                /// <summary> Set the rotation to the From target. </summary>
                From,

                /// <summary> Set the rotation to the To target. </summary>
                To,

                /// <summary> Set the forward to To - From direction using the From up. </summary>
                Direction,

                /// <summary> Set the forward to From - To direction using the To up. </summary>
                DirectionInverse,
            }

            public enum ScaleInit : byte
            {
                /// <summary> Don't set the scale. </summary>
                None,

                /// <summary> Set the scale to the From target. </summary>
                From,

                /// <summary> Set the scale to the To target. </summary>
                To,

                /// <summary> Set the scale to the distance between From and To. </summary>
                Distance,
            }
        }
    }
}
