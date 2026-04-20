// <copyright file="BridgeCompanionSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Tests.Systems
{
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Lighting;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Entities;

    public class BridgeCompanionSystemTests : ECSTestsFixture
    {
        private BridgeCompanionSystem system;
        private SystemHandle bridgeTypeSystem;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystemManaged<BridgeCompanionSystem>();
            this.bridgeTypeSystem = this.World.CreateSystem<BridgeTypeSystem>();
        }

        [Test]
        public void Update_AddsAndRemovesBridgeObject_WithChunkTypeTransitions()
        {
            var entity = this.Manager.CreateEntity(typeof(LightData));
            this.Manager.SetComponentData(entity, new LightData { Intensity = 1f });

            this.bridgeTypeSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();
            this.system.Update();

            Assert.IsTrue(this.Manager.HasComponent<BridgeObject>(entity));
            var bridgeObject = this.Manager.GetComponentData<BridgeObject>(entity).Value.Value;
            Assert.IsNotNull(bridgeObject);
            Assert.IsNotNull(bridgeObject.GetComponent<UnityEngine.Light>());

            var query = this.Manager.CreateEntityQuery(typeof(LightData));
            this.Manager.RemoveChunkComponentData<BridgeType>(query);

            this.system.Update();

            Assert.IsFalse(this.Manager.HasComponent<BridgeObject>(entity));
        }
    }
}
