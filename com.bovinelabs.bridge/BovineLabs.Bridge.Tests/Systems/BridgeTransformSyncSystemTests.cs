// <copyright file="BridgeTransformSyncSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Tests.Systems
{
    using BovineLabs.Bridge.Data;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;
    using UnityEngine;

    public class BridgeTransformSyncSystemTests : ECSTestsFixture
    {
        private SystemHandle system;
        private GameObject bridgeObject;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<BridgeTransformSyncSystem>();
        }

        public override void TearDown()
        {
            if (this.bridgeObject != null)
            {
                Object.DestroyImmediate(this.bridgeObject);
            }

            base.TearDown();
        }

        [Test]
        public void Update_SyncsLocalToWorldToManagedTransform()
        {
            this.bridgeObject = new GameObject("BridgeTransformSyncSystemTests_Object");

            var entity = this.Manager.CreateEntity(typeof(LocalToWorld), typeof(BridgeObject));
            this.Manager.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(new float3(4f, 5f, 6f)) });
            this.Manager.SetComponentData(entity, new BridgeObject
            {
                Value = this.bridgeObject,
                Transform = this.bridgeObject.transform.transformHandle,
            });

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.That(this.bridgeObject.transform.position.x, Is.EqualTo(4f).Within(0.001f));
            Assert.That(this.bridgeObject.transform.position.y, Is.EqualTo(5f).Within(0.001f));
            Assert.That(this.bridgeObject.transform.position.z, Is.EqualTo(6f).Within(0.001f));
        }
    }
}
