// <copyright file="TrackIdTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Tests.Data.Timeline
{
    using BovineLabs.Timeline.Data;
    using NUnit.Framework;
    using Unity.Entities;

    public class TrackIdTests
    {
        [Test]
        public void Equals_ReturnsTrueForMatchingValues()
        {
            var a = new TrackId
            {
                SceneObjectIdentifier0 = 123,
                AssetGUID = new Hash128(1, 2, 3, 4),
            };

            var b = a;

            Assert.IsTrue(a.Equals(b));
        }

        [Test]
        public void Equals_ReturnsFalseWhenSceneObjectIdentifierDiffers()
        {
            var a = new TrackId
            {
                SceneObjectIdentifier0 = 123,
                AssetGUID = new Hash128(1, 2, 3, 4),
            };

            var b = new TrackId
            {
                SceneObjectIdentifier0 = 999,
                AssetGUID = a.AssetGUID,
            };

            Assert.IsFalse(a.Equals(b));
        }

        [Test]
        public void Equals_ReturnsFalseWhenAssetGuidDiffers()
        {
            var a = new TrackId
            {
                SceneObjectIdentifier0 = 123,
                AssetGUID = new Hash128(1, 2, 3, 4),
            };

            var b = new TrackId
            {
                SceneObjectIdentifier0 = a.SceneObjectIdentifier0,
                AssetGUID = new Hash128(5, 6, 7, 8),
            };

            Assert.IsFalse(a.Equals(b));
        }

        [Test]
        public void HashCode_MatchesForEqualValues()
        {
            var a = new TrackId
            {
                SceneObjectIdentifier0 = 123,
                AssetGUID = new Hash128(1, 2, 3, 4),
            };

            var b = a;

            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }
    }
}
