// <copyright file="SplineSyncSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_SPLINES
namespace BovineLabs.Bridge.Tests.Systems
{
    using System.Collections.Generic;
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Spline;
    using BovineLabs.Core.Collections;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Splines;

    public class SplineSyncSystemTests : ECSTestsFixture
    {
        private SystemHandle system;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<SplineSyncSystem>();
        }

        [Test]
        public void Update_WithoutAddSplineBridgeTag_DoesNotThrow()
        {
            var go = new GameObject("SplineSyncSystemTests");

            try
            {
                var entity = this.Manager.CreateEntity(typeof(BridgeObject), typeof(Splines));
                this.Manager.SetComponentData(entity, new BridgeObject { Value = go });
                this.Manager.SetComponentData(entity, default(Splines));

                Assert.DoesNotThrow(() =>
                {
                    this.system.Update(this.WorldUnmanaged);
                    this.Manager.CompleteAllTrackedJobs();
                });
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Update_WithAddSplineBridge_UpdatesSplineContainer()
        {
            var go = new GameObject("SplineSyncSystemTests_Positive", typeof(SplineContainer));
            var blob = default(BlobAssetReference<BlobArray<BlobSpline>>);

            try
            {
                var spline = new Spline
                {
                    new BezierKnot(new float3(0f, 0f, 0f)),
                    new BezierKnot(new float3(0f, 0f, 5f)),
                };

                blob = BlobSpline.Create(new List<Spline> { spline }, float4x4.identity, Allocator.Persistent);

                var entity = this.Manager.CreateEntity(typeof(BridgeObject), typeof(Splines), typeof(AddSplineBridge));
                this.Manager.SetComponentData(entity, new BridgeObject { Value = go });
                this.Manager.SetComponentData(entity, new Splines { Value = blob });

                this.system.Update(this.WorldUnmanaged);
                this.Manager.CompleteAllTrackedJobs();

                var container = go.GetComponent<SplineContainer>();
                Assert.AreEqual(1, container.Splines.Count);
                Assert.AreEqual(2, container.Splines[0].Count);
            }
            finally
            {
                if (blob.IsCreated)
                {
                    blob.Dispose();
                }

                Object.DestroyImmediate(go);
            }
        }
    }
}
#endif
