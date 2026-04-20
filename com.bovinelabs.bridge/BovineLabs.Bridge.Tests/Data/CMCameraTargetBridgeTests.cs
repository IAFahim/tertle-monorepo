// <copyright file="CMCameraTargetBridgeTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Tests.Data
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using NUnit.Framework;
    using Unity.Entities;

    public class CMCameraTargetBridgeObjectsTests
    {
        [Test]
        public void Fields_RoundTripAssignedBridgeEntities()
        {
            var trackingTargetBridge = new Entity { Index = 1, Version = 2 };
            var lookAtTargetBridge = new Entity { Index = 3, Version = 4 };
            var bridgeObjects = new CMCameraTargetBridgeObjects
            {
                TrackingTargetBridge = trackingTargetBridge,
                LookAtTargetBridge = lookAtTargetBridge,
            };

            Assert.AreEqual(trackingTargetBridge, bridgeObjects.TrackingTargetBridge);
            Assert.AreEqual(lookAtTargetBridge, bridgeObjects.LookAtTargetBridge);
        }
    }
}
#endif
