// <copyright file="VolumeProfileURPSyncSystemTests.cs" company="BovineLabs">
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
    using UnityEngine.Rendering.Universal;

    public class VolumeProfileURPSyncSystemTests : ECSTestsFixture
    {
        private SystemHandle system;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<VolumeProfileURPSyncSystem>();
        }

        [Test]
        public void Update_WithoutVolumeComponent_DoesNotThrow()
        {
            var go = new GameObject("VolumeProfileURPSyncSystemTests");

            try
            {
                var entity = this.Manager.CreateEntity(typeof(BridgeObject), typeof(VolumeBloom));
                this.Manager.SetComponentData(entity, new BridgeObject { Value = go });
                this.Manager.SetComponentData(entity, new VolumeBloom { Active = true, Intensity = 0.5f, IntensityOverride = true });

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
        public void Update_WithVolumeComponentWithoutProfile_DoesNotThrow()
        {
            var go = new GameObject("VolumeProfileURPSyncSystemTests_NoProfile", typeof(Volume));

            try
            {
                var entity = this.Manager.CreateEntity(typeof(BridgeObject), typeof(VolumeBloom));
                this.Manager.SetComponentData(entity, new BridgeObject { Value = go });
                this.Manager.SetComponentData(entity, new VolumeBloom
                {
                    Active = true,
                    ThresholdOverride = true,
                    Threshold = 1.1f,
                });

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
        public void Update_WithBloomComponent_MapsRepresentativeValues()
        {
            var (go, profile) = CreateVolumeProfileContext("VolumeProfileURPSyncSystemTests_Bloom");
            profile.Add<Bloom>();

            try
            {
                var entity = this.Manager.CreateEntity(typeof(BridgeObject), typeof(VolumeBloom));

                this.Manager.SetComponentData(entity, new BridgeObject { Value = go });
                this.Manager.SetComponentData(entity, new VolumeBloom
                {
                    Active = true,
                    ThresholdOverride = true,
                    Threshold = 1.25f,
                    IntensityOverride = true,
                    Intensity = 0.6f,
                    ScatterOverride = true,
                    Scatter = 0.7f,
                    ClampOverride = true,
                    Clamp = 65000f,
                    TintOverride = true,
                    Tint = Color.cyan,
                    DownscaleOverride = true,
                    Downscale = (BloomDownscaleMode)1,
                    DirtIntensityOverride = true,
                    DirtIntensity = 0.35f,
                });

                this.UpdateSystem();

                Assert.IsTrue(profile.TryGet(out Bloom bloom));
                Assert.IsTrue(bloom.active);
                Assert.IsTrue(bloom.threshold.overrideState);
                Assert.That(bloom.threshold.value, Is.EqualTo(1.25f).Within(0.0001f));
                Assert.IsTrue(bloom.intensity.overrideState);
                Assert.That(bloom.intensity.value, Is.EqualTo(0.6f).Within(0.0001f));
                Assert.IsTrue(bloom.scatter.overrideState);
                Assert.That(bloom.scatter.value, Is.EqualTo(0.7f).Within(0.0001f));
                Assert.IsTrue(bloom.clamp.overrideState);
                Assert.That(bloom.clamp.value, Is.EqualTo(65000f).Within(0.0001f));
                Assert.IsTrue(bloom.downscale.overrideState);
                Assert.AreEqual((BloomDownscaleMode)1, bloom.downscale.value);
                Assert.IsTrue(bloom.dirtIntensity.overrideState);
                Assert.That(bloom.dirtIntensity.value, Is.EqualTo(0.35f).Within(0.0001f));
                Assert.AreEqual(Color.cyan, bloom.tint.value);
            }
            finally
            {
                DestroyProfileContext(profile, go);
            }
        }

        [Test]
        public void Update_WithColorAdjustmentsComponent_MapsRepresentativeValues()
        {
            var (go, profile) = CreateVolumeProfileContext("VolumeProfileURPSyncSystemTests_Color");
            profile.Add<ColorAdjustments>();

            try
            {
                var entity = this.Manager.CreateEntity(typeof(BridgeObject), typeof(VolumeColorAdjustments));

                this.Manager.SetComponentData(entity, new BridgeObject { Value = go });
                this.Manager.SetComponentData(entity, new VolumeColorAdjustments
                {
                    Active = true,
                    ColorFilterOverride = true,
                    ColorFilter = Color.magenta,
                    SaturationOverride = true,
                    Saturation = -35f,
                });

                this.UpdateSystem();

                Assert.IsTrue(profile.TryGet(out ColorAdjustments colorAdjustments));
                Assert.IsTrue(colorAdjustments.active);
                Assert.IsTrue(colorAdjustments.colorFilter.overrideState);
                Assert.AreEqual(Color.magenta, colorAdjustments.colorFilter.value);
                Assert.That(colorAdjustments.saturation.value, Is.EqualTo(-35f).Within(0.0001f));
            }
            finally
            {
                DestroyProfileContext(profile, go);
            }
        }

        [Test]
        public void Update_WithLensDistortionComponent_MapsRepresentativeValues()
        {
            var (go, profile) = CreateVolumeProfileContext("VolumeProfileURPSyncSystemTests_Lens");
            profile.Add<LensDistortion>();

            try
            {
                var entity = this.Manager.CreateEntity(typeof(BridgeObject), typeof(VolumeLensDistortion));
                this.Manager.SetComponentData(entity, new BridgeObject { Value = go });
                this.Manager.SetComponentData(entity, new VolumeLensDistortion
                {
                    Active = true,
                    IntensityOverride = true,
                    Intensity = -0.25f,
                    CenterOverride = true,
                    Center = new Vector2(0.3f, 0.7f),
                });

                this.UpdateSystem();

                Assert.IsTrue(profile.TryGet(out LensDistortion lensDistortion));
                Assert.IsTrue(lensDistortion.active);
                Assert.IsTrue(lensDistortion.intensity.overrideState);
                Assert.That(lensDistortion.intensity.value, Is.EqualTo(-0.25f).Within(0.0001f));
                Assert.AreEqual(new Vector2(0.3f, 0.7f), lensDistortion.center.value);
            }
            finally
            {
                DestroyProfileContext(profile, go);
            }
        }

        [Test]
        public void Update_WithLiftGammaGainComponent_MapsRepresentativeValues()
        {
            var (go, profile) = CreateVolumeProfileContext("VolumeProfileURPSyncSystemTests_LiftGammaGain");
            profile.Add<LiftGammaGain>();

            try
            {
                var entity = this.Manager.CreateEntity(typeof(BridgeObject), typeof(VolumeLiftGammaGain));
                this.Manager.SetComponentData(entity, new BridgeObject { Value = go });
                this.Manager.SetComponentData(entity, new VolumeLiftGammaGain
                {
                    Active = true,
                    LiftOverride = true,
                    Lift = new Vector4(0.1f, 0.2f, 0.3f, 0.4f),
                    GammaOverride = true,
                    Gamma = new Vector4(0.9f, 0.8f, 0.7f, 0.6f),
                    GainOverride = true,
                    Gain = new Vector4(1.1f, 1.2f, 1.3f, 1.4f),
                });

                this.UpdateSystem();

                Assert.IsTrue(profile.TryGet(out LiftGammaGain liftGammaGain));
                Assert.IsTrue(liftGammaGain.active);
                Assert.AreEqual(new Vector4(0.1f, 0.2f, 0.3f, 0.4f), liftGammaGain.lift.value);
                Assert.AreEqual(new Vector4(0.9f, 0.8f, 0.7f, 0.6f), liftGammaGain.gamma.value);
                Assert.AreEqual(new Vector4(1.1f, 1.2f, 1.3f, 1.4f), liftGammaGain.gain.value);
            }
            finally
            {
                DestroyProfileContext(profile, go);
            }
        }

        [Test]
        public void Update_WithMotionBlurComponent_MapsRepresentativeValues()
        {
            var (go, profile) = CreateVolumeProfileContext("VolumeProfileURPSyncSystemTests_MotionBlur");
            profile.Add<MotionBlur>();

            try
            {
                var entity = this.Manager.CreateEntity(typeof(BridgeObject), typeof(VolumeMotionBlur));
                this.Manager.SetComponentData(entity, new BridgeObject { Value = go });
                this.Manager.SetComponentData(entity, new VolumeMotionBlur
                {
                    Active = true,
                    ModeOverride = true,
                    Mode = (MotionBlurMode)1,
                    QualityOverride = true,
                    Quality = (MotionBlurQuality)2,
                    IntensityOverride = true,
                    Intensity = 0.35f,
                    ClampOverride = true,
                    Clamp = 0.1f,
                });

                this.UpdateSystem();

                Assert.IsTrue(profile.TryGet(out MotionBlur motionBlur));
                Assert.IsTrue(motionBlur.active);
                Assert.AreEqual((MotionBlurMode)1, motionBlur.mode.value);
                Assert.AreEqual((MotionBlurQuality)2, motionBlur.quality.value);
                Assert.That(motionBlur.intensity.value, Is.EqualTo(0.35f).Within(0.0001f));
                Assert.That(motionBlur.clamp.value, Is.EqualTo(0.1f).Within(0.0001f));
            }
            finally
            {
                DestroyProfileContext(profile, go);
            }
        }

        [Test]
        public void Update_WithPaniniProjectionComponent_MapsRepresentativeValues()
        {
            var (go, profile) = CreateVolumeProfileContext("VolumeProfileURPSyncSystemTests_Panini");
            profile.Add<PaniniProjection>();

            try
            {
                var entity = this.Manager.CreateEntity(typeof(BridgeObject), typeof(VolumePaniniProjection));
                this.Manager.SetComponentData(entity, new BridgeObject { Value = go });
                this.Manager.SetComponentData(entity, new VolumePaniniProjection
                {
                    Active = true,
                    DistanceOverride = true,
                    Distance = 0.3f,
                    CropToFitOverride = true,
                    CropToFit = 0.4f,
                });

                this.UpdateSystem();

                Assert.IsTrue(profile.TryGet(out PaniniProjection paniniProjection));
                Assert.IsTrue(paniniProjection.active);
                Assert.That(paniniProjection.distance.value, Is.EqualTo(0.3f).Within(0.0001f));
                Assert.That(paniniProjection.cropToFit.value, Is.EqualTo(0.4f).Within(0.0001f));
            }
            finally
            {
                DestroyProfileContext(profile, go);
            }
        }

        [Test]
        public void Update_WithScreenSpaceLensFlareComponent_MapsRepresentativeValues()
        {
            var (go, profile) = CreateVolumeProfileContext("VolumeProfileURPSyncSystemTests_ScreenSpaceLensFlare");
            profile.Add<ScreenSpaceLensFlare>();

            try
            {
                var entity = this.Manager.CreateEntity(typeof(BridgeObject), typeof(VolumeScreenSpaceLensFlare));
                this.Manager.SetComponentData(entity, new BridgeObject { Value = go });
                this.Manager.SetComponentData(entity, new VolumeScreenSpaceLensFlare
                {
                    Active = true,
                    IntensityOverride = true,
                    Intensity = 0.9f,
                    TintColorOverride = true,
                    TintColor = Color.green,
                    BloomMipOverride = true,
                    BloomMip = 2,
                    SamplesOverride = true,
                    Samples = 3,
                    VignetteEffectOverride = true,
                    VignetteEffect = 0.4f,
                    ScaleOverride = true,
                    Scale = 1.4f,
                    StreaksIntensityOverride = true,
                    StreaksIntensity = 0.8f,
                    ResolutionOverride = true,
                    Resolution = (ScreenSpaceLensFlareResolution)1,
                    ChromaticAbberationIntensityOverride = true,
                    ChromaticAbberationIntensity = 0.2f,
                });

                this.UpdateSystem();

                Assert.IsTrue(profile.TryGet(out ScreenSpaceLensFlare screenSpaceLensFlare));
                Assert.IsTrue(screenSpaceLensFlare.active);
                Assert.That(screenSpaceLensFlare.intensity.value, Is.EqualTo(0.9f).Within(0.0001f));
                Assert.AreEqual(Color.green, screenSpaceLensFlare.tintColor.value);
                Assert.IsTrue(screenSpaceLensFlare.bloomMip.overrideState);
                Assert.AreEqual(2, screenSpaceLensFlare.bloomMip.value);
                Assert.AreEqual(3, screenSpaceLensFlare.samples.value);
                Assert.IsTrue(screenSpaceLensFlare.vignetteEffect.overrideState);
                Assert.That(screenSpaceLensFlare.vignetteEffect.value, Is.EqualTo(0.4f).Within(0.0001f));
                Assert.That(screenSpaceLensFlare.scale.value, Is.EqualTo(1.4f).Within(0.0001f));
                Assert.IsTrue(screenSpaceLensFlare.streaksIntensity.overrideState);
                Assert.That(screenSpaceLensFlare.streaksIntensity.value, Is.EqualTo(0.8f).Within(0.0001f));
                Assert.AreEqual((ScreenSpaceLensFlareResolution)1, screenSpaceLensFlare.resolution.value);
                Assert.That(screenSpaceLensFlare.chromaticAbberationIntensity.value, Is.EqualTo(0.2f).Within(0.0001f));
            }
            finally
            {
                DestroyProfileContext(profile, go);
            }
        }

        [Test]
        public void Update_WithSplitToningComponent_MapsRepresentativeValues()
        {
            var (go, profile) = CreateVolumeProfileContext("VolumeProfileURPSyncSystemTests_SplitToning");
            profile.Add<SplitToning>();

            try
            {
                var entity = this.Manager.CreateEntity(typeof(BridgeObject), typeof(VolumeSplitToning));
                this.Manager.SetComponentData(entity, new BridgeObject { Value = go });
                this.Manager.SetComponentData(entity, new VolumeSplitToning
                {
                    Active = true,
                    ShadowsOverride = true,
                    Shadows = Color.red,
                    HighlightsOverride = true,
                    Highlights = Color.blue,
                    BalanceOverride = true,
                    Balance = -25f,
                });

                this.UpdateSystem();

                Assert.IsTrue(profile.TryGet(out SplitToning splitToning));
                Assert.IsTrue(splitToning.active);
                Assert.AreEqual(Color.red, splitToning.shadows.value);
                Assert.AreEqual(Color.blue, splitToning.highlights.value);
                Assert.That(splitToning.balance.value, Is.EqualTo(-25f).Within(0.0001f));
            }
            finally
            {
                DestroyProfileContext(profile, go);
            }
        }

        [Test]
        public void Update_WithTonemappingComponent_MapsRepresentativeValues()
        {
            var (go, profile) = CreateVolumeProfileContext("VolumeProfileURPSyncSystemTests_Tonemapping");
            profile.Add<Tonemapping>();

            try
            {
                var entity = this.Manager.CreateEntity(typeof(BridgeObject), typeof(VolumeTonemapping));
                this.Manager.SetComponentData(entity, new BridgeObject { Value = go });
                this.Manager.SetComponentData(entity, new VolumeTonemapping
                {
                    Active = true,
                    ModeOverride = true,
                    Mode = (TonemappingMode)2,
                    NeutralHDRRangeReductionModeOverride = true,
                    NeutralHDRRangeReductionMode = (NeutralRangeReductionMode)1,
                    AcesPresetOverride = true,
                    AcesPreset = (HDRACESPreset)1,
                    HueShiftAmountOverride = true,
                    HueShiftAmount = 0.5f,
                    DetectPaperWhiteOverride = true,
                    DetectPaperWhite = true,
                    PaperWhiteOverride = true,
                    PaperWhite = 170f,
                    DetectBrightnessLimitsOverride = true,
                    DetectBrightnessLimits = true,
                    MinNitsOverride = true,
                    MinNits = 1.5f,
                    MaxNitsOverride = true,
                    MaxNits = 900f,
                });

                this.UpdateSystem();

                Assert.IsTrue(profile.TryGet(out Tonemapping tonemapping));
                Assert.IsTrue(tonemapping.active);
                Assert.AreEqual((TonemappingMode)2, tonemapping.mode.value);
                Assert.AreEqual((NeutralRangeReductionMode)1, tonemapping.neutralHDRRangeReductionMode.value);
                Assert.AreEqual((HDRACESPreset)1, tonemapping.acesPreset.value);
                Assert.IsTrue(tonemapping.hueShiftAmount.overrideState);
                Assert.That(tonemapping.hueShiftAmount.value, Is.EqualTo(0.5f).Within(0.0001f));
                Assert.IsTrue(tonemapping.detectPaperWhite.value);
                Assert.IsTrue(tonemapping.paperWhite.overrideState);
                Assert.That(tonemapping.paperWhite.value, Is.EqualTo(170f).Within(0.0001f));
                Assert.IsTrue(tonemapping.detectBrightnessLimits.overrideState);
                Assert.IsTrue(tonemapping.detectBrightnessLimits.value);
                Assert.IsTrue(tonemapping.minNits.overrideState);
                Assert.That(tonemapping.minNits.value, Is.EqualTo(1.5f).Within(0.0001f));
                Assert.That(tonemapping.maxNits.value, Is.EqualTo(900f).Within(0.0001f));
            }
            finally
            {
                DestroyProfileContext(profile, go);
            }
        }

        [Test]
        public void Update_WithWhiteBalanceComponent_MapsRepresentativeValues()
        {
            var (go, profile) = CreateVolumeProfileContext("VolumeProfileURPSyncSystemTests_WhiteBalance");
            profile.Add<WhiteBalance>();

            try
            {
                var entity = this.Manager.CreateEntity(typeof(BridgeObject), typeof(VolumeWhiteBalance));
                this.Manager.SetComponentData(entity, new BridgeObject { Value = go });
                this.Manager.SetComponentData(entity, new VolumeWhiteBalance
                {
                    Active = true,
                    TemperatureOverride = true,
                    Temperature = 10f,
                    TintOverride = true,
                    Tint = -5f,
                });

                this.UpdateSystem();

                Assert.IsTrue(profile.TryGet(out WhiteBalance whiteBalance));
                Assert.IsTrue(whiteBalance.active);
                Assert.That(whiteBalance.temperature.value, Is.EqualTo(10f).Within(0.0001f));
                Assert.That(whiteBalance.tint.value, Is.EqualTo(-5f).Within(0.0001f));
            }
            finally
            {
                DestroyProfileContext(profile, go);
            }
        }

        [Test]
        public void Update_WithVignetteComponent_MapsRepresentativeValues()
        {
            var (go, profile) = CreateVolumeProfileContext("VolumeProfileURPSyncSystemTests_Vignette");
            profile.Add<Vignette>();

            try
            {
                var entity = this.Manager.CreateEntity(typeof(BridgeObject), typeof(VolumeVignette));
                this.Manager.SetComponentData(entity, new BridgeObject { Value = go });
                this.Manager.SetComponentData(entity, new VolumeVignette
                {
                    Active = true,
                    ColorOverride = true,
                    Color = Color.yellow,
                    CenterOverride = true,
                    Center = new Vector2(0.45f, 0.55f),
                    IntensityOverride = true,
                    Intensity = 0.5f,
                    SmoothnessOverride = true,
                    Smoothness = 0.65f,
                    RoundedOverride = true,
                    Rounded = true,
                });

                this.UpdateSystem();

                Assert.IsTrue(profile.TryGet(out Vignette vignette));
                Assert.IsTrue(vignette.active);
                Assert.AreEqual(Color.yellow, vignette.color.value);
                Assert.AreEqual(new Vector2(0.45f, 0.55f), vignette.center.value);
                Assert.That(vignette.intensity.value, Is.EqualTo(0.5f).Within(0.0001f));
                Assert.That(vignette.smoothness.value, Is.EqualTo(0.65f).Within(0.0001f));
                Assert.IsTrue(vignette.rounded.value);
            }
            finally
            {
                DestroyProfileContext(profile, go);
            }
        }

        private static (GameObject Go, VolumeProfile Profile) CreateVolumeProfileContext(string name)
        {
            var go = new GameObject(name, typeof(Volume));
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            go.GetComponent<Volume>().profile = profile;
            return (go, profile);
        }

        private static void DestroyProfileContext(VolumeProfile profile, GameObject go)
        {
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(go);
        }

        private void UpdateSystem()
        {
            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();
        }
    }
}
#endif
