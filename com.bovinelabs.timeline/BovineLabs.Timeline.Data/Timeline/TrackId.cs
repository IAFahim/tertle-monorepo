// <copyright file="TrackId.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Data
{
    using System;
    using Unity.Entities;

    public struct TrackId : IEquatable<TrackId>
    {
        public ulong SceneObjectIdentifier0;
        public Hash128 AssetGUID; // this is required as we support nested directors

        public bool Equals(TrackId other)
        {
            return this.SceneObjectIdentifier0 == other.SceneObjectIdentifier0 && this.AssetGUID.Equals(other.AssetGUID);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (this.SceneObjectIdentifier0.GetHashCode() * 397) ^ this.AssetGUID.GetHashCode();
            }
        }
    }
}
