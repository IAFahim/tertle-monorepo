// <copyright file="VolumeSyncSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge.Tests.Systems
{
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Volume;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Rendering;

    public class VolumeSyncSystemTests : ECSTestsFixture
    {
        private SystemHandle system;
        private GameObject volumeObject;
        private VolumeProfile profile;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<VolumeSyncSystem>();
        }

        public override void TearDown()
        {
            if (this.volumeObject != null)
            {
                Object.DestroyImmediate(this.volumeObject);
            }

            if (this.profile != null)
            {
                Object.DestroyImmediate(this.profile);
            }

            base.TearDown();
        }

        [Test]
        public void Update_AppliesVolumeSettingsToManagedVolume()
        {
            this.volumeObject = new GameObject("VolumeSyncSystemTests_Volume", typeof(Volume));
            this.profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var entity = this.Manager.CreateEntity(typeof(BridgeObject), typeof(VolumeSettings));
            this.Manager.SetComponentData(entity, new BridgeObject { Value = this.volumeObject });
            this.Manager.SetComponentData(entity, new VolumeSettings
            {
                Weight = 0.4f,
                Priority = 10f,
                BlendDistance = 3f,
                IsGlobal = true,
                Profile = this.profile,
            });

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var volume = this.volumeObject.GetComponent<Volume>();
            Assert.That(volume.weight, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(volume.priority, Is.EqualTo(10f).Within(0.001f));
            Assert.That(volume.blendDistance, Is.EqualTo(3f).Within(0.001f));
            Assert.IsTrue(volume.isGlobal);
            Assert.AreEqual(this.profile, volume.sharedProfile);
        }
    }
}
#endif
