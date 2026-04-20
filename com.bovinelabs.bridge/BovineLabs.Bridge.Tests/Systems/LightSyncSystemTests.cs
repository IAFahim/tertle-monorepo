// <copyright file="LightSyncSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Tests.Systems
{
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Lighting;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
#if UNITY_URP
    using UnityEngine.Rendering.Universal;
#endif
#if UNITY_HDRP
    using UnityEngine.Rendering.HighDefinition;
#endif
    using LightData = BovineLabs.Bridge.Data.Lighting.LightData;

    public class LightSyncSystemTests : ECSTestsFixture
    {
        private SystemHandle system;
        private GameObject lightObject;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<LightSyncSystem>();
        }

        public override void TearDown()
        {
            if (this.lightObject != null)
            {
                Object.DestroyImmediate(this.lightObject);
            }

            base.TearDown();
        }

        [Test]
        public void Update_AppliesCoreLightDataToManagedLight()
        {
            this.lightObject = new GameObject("LightSyncSystemTests_Light", typeof(Light));

            var entity = this.Manager.CreateEntity(typeof(BridgeObject), typeof(LightEnabled), typeof(LightData), typeof(LightDataExtended));
            this.Manager.SetComponentData(entity, new BridgeObject { Value = this.lightObject });
            this.Manager.SetComponentEnabled<LightEnabled>(entity, false);
            this.Manager.SetComponentData(entity, new LightData
            {
                Color = Color.red,
                Intensity = 2.5f,
                ColorTemperature = 4500f,
            });
            this.Manager.SetComponentData(entity, new LightDataExtended
            {
                Type = LightType.Spot,
                Range = 30f,
                SpotAngle = 45f,
                InnerSpotAngle = 22f,
                CookieSize = new float2(3f, 4f),
                BounceIntensity = 0.8f,
                Shadows = LightShadows.Soft,
                ShadowStrength = 0.6f,
                ShadowBias = 0.1f,
                ShadowNormalBias = 0.2f,
                ShadowNearPlane = 0.3f,
                RenderMode = LightRenderMode.ForcePixel,
                CullingMask = 123,
                RenderingLayerMask = 7,
            });

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var light = this.lightObject.GetComponent<Light>();
            Assert.IsFalse(light.enabled);
            Assert.AreEqual(Color.red, light.color);
            Assert.That(light.intensity, Is.EqualTo(2.5f).Within(0.001f));
            Assert.That(light.range, Is.EqualTo(30f).Within(0.001f));
            Assert.That(light.spotAngle, Is.EqualTo(45f).Within(0.001f));
            Assert.AreEqual(LightType.Spot, light.type);
        }

#if UNITY_URP
        [Test]
        public void Update_AppliesUrpLightData()
        {
            this.lightObject = new GameObject("LightSyncSystemTests_URP", typeof(Light), typeof(UniversalAdditionalLightData));

            var entity = this.Manager.CreateEntity(typeof(BridgeObject), typeof(LightUniversalData));
            this.Manager.SetComponentData(entity, new BridgeObject { Value = this.lightObject });
            this.Manager.SetComponentData(entity, new LightUniversalData
            {
                UsePipelineSettings = false,
                SoftShadowQuality = SoftShadowQuality.High,
                RenderingLayers = (RenderingLayerMask)15u,
                CookieSize = new float2(5f, 6f),
                CookieOffset = new float2(1f, 2f),
            });

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var additional = this.lightObject.GetComponent<UniversalAdditionalLightData>();
            Assert.IsFalse(additional.usePipelineSettings);
            Assert.AreEqual(SoftShadowQuality.High, additional.softShadowQuality);
            Assert.AreEqual((uint)15, (uint)additional.renderingLayers);
        }
#endif

#if UNITY_HDRP
        [Test]
        public void Update_AppliesHdrpLightData()
        {
            this.lightObject = new GameObject("LightSyncSystemTests_HDRP", typeof(Light), typeof(HDAdditionalLightData));

            var entity = this.Manager.CreateEntity(typeof(BridgeObject), typeof(LightHdData));
            this.Manager.SetComponentData(entity, new BridgeObject { Value = this.lightObject });
            this.Manager.SetComponentData(entity, new LightHdData
            {
                LightDimmer = 0.4f,
                VolumetricDimmer = 0.5f,
                AffectDiffuse = false,
                AffectSpecular = true,
                FadeDistance = 6f,
                ShadowDimmer = 0.7f,
                ShadowFadeDistance = 8f,
                AffectsVolumetric = true,
            });

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var additional = this.lightObject.GetComponent<HDAdditionalLightData>();
            Assert.That(additional.lightDimmer, Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(additional.volumetricDimmer, Is.EqualTo(0.5f).Within(0.001f));
            Assert.IsFalse(additional.affectDiffuse);
            Assert.IsTrue(additional.affectSpecular);
        }
#endif
    }
}
