// <copyright file="CMVolumeSettingsFocusBridgeTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Tests.Data
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using NUnit.Framework;
    using Unity.Entities;

    public class CMVolumeSettingsFocusBridgeTests
    {
        [Test]
        public void Field_RoundTripsAssignedBridgeEntity()
        {
            var value = new Entity { Index = 2, Version = 9 };
            var bridge = new CMVolumeSettingsFocusBridge { Value = value };

            Assert.AreEqual(value, bridge.Value);
        }
    }
}
#endif
