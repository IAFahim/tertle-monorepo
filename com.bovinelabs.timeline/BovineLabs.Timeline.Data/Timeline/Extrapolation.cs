// <copyright file="Extrapolation.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data
{
    using Unity.Entities;

    /// <summary>Indicates that a clip holds its time values outside the clip range.</summary>
    public struct ExtrapolationHold : IComponentData
    {
        /// <summary>The extrapolation sides that apply for this clip.</summary>
        public ExtrapolationPosition ExtrapolateOptions;
    }

    /// <summary>Indicates a clip ping-pongs its time values outside its range.</summary>
    public struct ExtrapolationPingPong : IComponentData
    {
        /// <summary>The extrapolation sides that apply for this clip.</summary>
        public ExtrapolationPosition ExtrapolateOptions;
    }

    /// <summary>Indicates a clip loops its time values outside its range.</summary>
    public struct ExtrapolationLoop : IComponentData
    {
        /// <summary>The extrapolation sides that apply for this clip.</summary>
        public ExtrapolationPosition ExtrapolateOptions;
    }
}
