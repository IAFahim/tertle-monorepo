// <copyright file="BridgeTypeSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Tests.Systems
{
    using BovineLabs.Bridge.Data;
#if UNITY_CINEMACHINE
    using BovineLabs.Bridge.Data.Cinemachine;
#endif
    using BovineLabs.Bridge.Data.Lighting;
#if UNITY_URP
    using BovineLabs.Bridge.Data.Volume;
#endif
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Entities;

    public class BridgeTypeSystemTests : ECSTestsFixture
    {
        private SystemHandle system;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<BridgeTypeSystem>();
        }

        [Test]
        public void Update_WithLightData_AddsLightChunkFlag()
        {
            var entity = this.Manager.CreateEntity(typeof(LightData));

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.IsTrue(this.Manager.HasChunkComponent<BridgeType>(entity));

            var type = this.Manager.GetChunkComponentData<BridgeType>(entity);
            Assert.AreEqual(UnityComponentType.Light, type.Types);
        }

#if UNITY_URP
        [Test]
        public void Update_WithLightAndVolume_SetsCombinedFlags()
        {
            var entity = this.Manager.CreateEntity(typeof(LightData), typeof(VolumeSettings));

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var type = this.Manager.GetChunkComponentData<BridgeType>(entity);
            var expected = UnityComponentType.Light | UnityComponentType.Volume;
            Assert.AreEqual(expected, type.Types);
        }
#endif

#if UNITY_CINEMACHINE
        [Test]
        public void Update_WithCinemachineCamera_AddsCinemachineChunkFlagAndShape()
        {
            var entity = this.Manager.CreateEntity(typeof(CMCamera), typeof(CMFollow));

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.IsTrue(this.Manager.HasChunkComponent<BridgeType>(entity));

            var type = this.Manager.GetChunkComponentData<BridgeType>(entity);
            Assert.AreEqual(UnityComponentType.Cinemachine, type.Types);
            Assert.AreEqual(CMCameraRuntimeType.Camera | CMCameraRuntimeType.Follow, type.Cinemachine);
        }

        [Test]
        public void Update_WithCinemachineTargetBridgeObject_AddsCinemachineChunkFlagWithoutShape()
        {
            var entity = this.Manager.CreateEntity(typeof(CMCameraTargetBridgeObject));

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.IsTrue(this.Manager.HasChunkComponent<BridgeType>(entity));

            var type = this.Manager.GetChunkComponentData<BridgeType>(entity);
            Assert.AreEqual(UnityComponentType.None, type.Types);
            Assert.AreEqual(CMCameraRuntimeType.None, type.Cinemachine);
        }
#endif

        [Test]
        public void Update_WithoutBridgeComponents_DoesNotAddChunkType()
        {
            var entity = this.Manager.CreateEntity(typeof(BridgeTypeSystemTestTag));

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.IsFalse(this.Manager.HasChunkComponent<BridgeType>(entity));
        }

        private struct BridgeTypeSystemTestTag : IComponentData
        {
        }
    }
}
