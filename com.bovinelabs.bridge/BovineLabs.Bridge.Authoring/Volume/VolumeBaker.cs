// <copyright file="VolumeBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge.Authoring.Volume
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Bridge.Data.Volume;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [ForceBakingOnDisabledComponents]
    public class VolumeBaker : Baker<Volume>
    {
        /// <inheritdoc />
        public override void Bake(Volume authoring)
        {
            var entity = this.GetEntity(TransformUsageFlags.None);

            var profile = authoring.sharedProfile;
            if (profile == null && authoring.HasInstantiatedProfile())
            {
                profile = authoring.profile;
            }

            this.AddComponent(entity, new VolumeSettings
            {
                Weight = authoring.weight,
                Priority = authoring.priority,
                BlendDistance = authoring.blendDistance,
                IsGlobal = authoring.isGlobal,
                Profile = profile,
            });

            if (profile == null)
            {
                return;
            }

            if (profile.TryGet(out Bloom bloom))
            {
                this.AddComponent(entity, new VolumeBloom
                {
                    Threshold = bloom.threshold.value,
                    Intensity = bloom.intensity.value,
                    Scatter = bloom.scatter.value,
                    Clamp = bloom.clamp.value,
                    Tint = bloom.tint.value,
                    HighQualityFiltering = bloom.highQualityFiltering.value,
                    Filter = bloom.filter.value,
                    Downscale = bloom.downscale.value,
                    MaxIterations = bloom.maxIterations.value,
                    DirtTexture = bloom.dirtTexture.value,
                    DirtIntensity = bloom.dirtIntensity.value,
                    Active = bloom.active,
                    ThresholdOverride = bloom.threshold.overrideState,
                    IntensityOverride = bloom.intensity.overrideState,
                    ScatterOverride = bloom.scatter.overrideState,
                    ClampOverride = bloom.clamp.overrideState,
                    TintOverride = bloom.tint.overrideState,
                    HighQualityFilteringOverride = bloom.highQualityFiltering.overrideState,
                    FilterOverride = bloom.filter.overrideState,
                    DownscaleOverride = bloom.downscale.overrideState,
                    MaxIterationsOverride = bloom.maxIterations.overrideState,
                    DirtTextureOverride = bloom.dirtTexture.overrideState,
                    DirtIntensityOverride = bloom.dirtIntensity.overrideState,
                });
            }

            if (profile.TryGet(out ChannelMixer channelMixer))
            {
                this.AddComponent(entity, new VolumeChannelMixer
                {
                    RedOutRedIn = channelMixer.redOutRedIn.value,
                    RedOutGreenIn = channelMixer.redOutGreenIn.value,
                    RedOutBlueIn = channelMixer.redOutBlueIn.value,
                    GreenOutRedIn = channelMixer.greenOutRedIn.value,
                    GreenOutGreenIn = channelMixer.greenOutGreenIn.value,
                    GreenOutBlueIn = channelMixer.greenOutBlueIn.value,
                    BlueOutRedIn = channelMixer.blueOutRedIn.value,
                    BlueOutGreenIn = channelMixer.blueOutGreenIn.value,
                    BlueOutBlueIn = channelMixer.blueOutBlueIn.value,
                    Active = channelMixer.active,
                    RedOutRedInOverride = channelMixer.redOutRedIn.overrideState,
                    RedOutGreenInOverride = channelMixer.redOutGreenIn.overrideState,
                    RedOutBlueInOverride = channelMixer.redOutBlueIn.overrideState,
                    GreenOutRedInOverride = channelMixer.greenOutRedIn.overrideState,
                    GreenOutGreenInOverride = channelMixer.greenOutGreenIn.overrideState,
                    GreenOutBlueInOverride = channelMixer.greenOutBlueIn.overrideState,
                    BlueOutRedInOverride = channelMixer.blueOutRedIn.overrideState,
                    BlueOutGreenInOverride = channelMixer.blueOutGreenIn.overrideState,
                    BlueOutBlueInOverride = channelMixer.blueOutBlueIn.overrideState,
                });
            }

            if (profile.TryGet(out ChromaticAberration chromaticAberration))
            {
                this.AddComponent(entity, new VolumeChromaticAberration
                {
                    Intensity = chromaticAberration.intensity.value,
                    Active = chromaticAberration.active,
                    IntensityOverride = chromaticAberration.intensity.overrideState,
                });
            }

            if (profile.TryGet(out ColorAdjustments colorAdjustments))
            {
                this.AddComponent(entity, new VolumeColorAdjustments
                {
                    PostExposure = colorAdjustments.postExposure.value,
                    Contrast = colorAdjustments.contrast.value,
                    ColorFilter = colorAdjustments.colorFilter.value,
                    HueShift = colorAdjustments.hueShift.value,
                    Saturation = colorAdjustments.saturation.value,
                    Active = colorAdjustments.active,
                    PostExposureOverride = colorAdjustments.postExposure.overrideState,
                    ContrastOverride = colorAdjustments.contrast.overrideState,
                    ColorFilterOverride = colorAdjustments.colorFilter.overrideState,
                    HueShiftOverride = colorAdjustments.hueShift.overrideState,
                    SaturationOverride = colorAdjustments.saturation.overrideState,
                });
            }

            if (profile.TryGet(out ColorCurves colorCurves))
            {
                var builder = new BlobBuilder(Allocator.Temp);
                ref var blob = ref builder.ConstructRoot<VolumeColorCurvesBlob>();
                BakeCurves(ref builder, ref blob, colorCurves);

                var blobRef = builder.CreateBlobAssetReference<VolumeColorCurvesBlob>(Allocator.Persistent);
                this.AddBlobAsset(ref blobRef, out _);

                this.AddComponent(entity, new VolumeColorCurves
                {
                    Curves = blobRef,
                    Active = colorCurves.active,
                    MasterOverride = colorCurves.master.overrideState,
                    RedOverride = colorCurves.red.overrideState,
                    GreenOverride = colorCurves.green.overrideState,
                    BlueOverride = colorCurves.blue.overrideState,
                    HueVsHueOverride = colorCurves.hueVsHue.overrideState,
                    HueVsSatOverride = colorCurves.hueVsSat.overrideState,
                    SatVsSatOverride = colorCurves.satVsSat.overrideState,
                    LumVsSatOverride = colorCurves.lumVsSat.overrideState,
                });
            }

            if (profile.TryGet(out ColorLookup colorLookup))
            {
                this.AddComponent(entity, new VolumeColorLookup
                {
                    Texture = colorLookup.texture.value,
                    Contribution = colorLookup.contribution.value,
                    Active = colorLookup.active,
                    TextureOverride = colorLookup.texture.overrideState,
                    ContributionOverride = colorLookup.contribution.overrideState,
                });
            }

            if (profile.TryGet(out DepthOfField depthOfField))
            {
                this.AddComponent(entity, new VolumeDepthOfField
                {
                    Mode = depthOfField.mode.value,
                    GaussianStart = depthOfField.gaussianStart.value,
                    GaussianEnd = depthOfField.gaussianEnd.value,
                    GaussianMaxRadius = depthOfField.gaussianMaxRadius.value,
                    HighQualitySampling = depthOfField.highQualitySampling.value,
                    FocusDistance = depthOfField.focusDistance.value,
                    Aperture = depthOfField.aperture.value,
                    FocalLength = depthOfField.focalLength.value,
                    BladeCount = depthOfField.bladeCount.value,
                    BladeCurvature = depthOfField.bladeCurvature.value,
                    BladeRotation = depthOfField.bladeRotation.value,
                    Active = depthOfField.active,
                    ModeOverride = depthOfField.mode.overrideState,
                    GaussianStartOverride = depthOfField.gaussianStart.overrideState,
                    GaussianEndOverride = depthOfField.gaussianEnd.overrideState,
                    GaussianMaxRadiusOverride = depthOfField.gaussianMaxRadius.overrideState,
                    HighQualitySamplingOverride = depthOfField.highQualitySampling.overrideState,
                    FocusDistanceOverride = depthOfField.focusDistance.overrideState,
                    ApertureOverride = depthOfField.aperture.overrideState,
                    FocalLengthOverride = depthOfField.focalLength.overrideState,
                    BladeCountOverride = depthOfField.bladeCount.overrideState,
                    BladeCurvatureOverride = depthOfField.bladeCurvature.overrideState,
                    BladeRotationOverride = depthOfField.bladeRotation.overrideState,
                });
            }

            if (profile.TryGet(out FilmGrain filmGrain))
            {
                this.AddComponent(entity, new VolumeFilmGrain
                {
                    Type = filmGrain.type.value,
                    Intensity = filmGrain.intensity.value,
                    Response = filmGrain.response.value,
                    Texture = filmGrain.texture.value,
                    Active = filmGrain.active,
                    TypeOverride = filmGrain.type.overrideState,
                    IntensityOverride = filmGrain.intensity.overrideState,
                    ResponseOverride = filmGrain.response.overrideState,
                    TextureOverride = filmGrain.texture.overrideState,
                });
            }

            if (profile.TryGet(out LensDistortion lensDistortion))
            {
                this.AddComponent(entity, new VolumeLensDistortion
                {
                    Intensity = lensDistortion.intensity.value,
                    XMultiplier = lensDistortion.xMultiplier.value,
                    YMultiplier = lensDistortion.yMultiplier.value,
                    Center = lensDistortion.center.value,
                    Scale = lensDistortion.scale.value,
                    Active = lensDistortion.active,
                    IntensityOverride = lensDistortion.intensity.overrideState,
                    XMultiplierOverride = lensDistortion.xMultiplier.overrideState,
                    YMultiplierOverride = lensDistortion.yMultiplier.overrideState,
                    CenterOverride = lensDistortion.center.overrideState,
                    ScaleOverride = lensDistortion.scale.overrideState,
                });
            }

            if (profile.TryGet(out LiftGammaGain liftGammaGain))
            {
                this.AddComponent(entity, new VolumeLiftGammaGain
                {
                    Lift = liftGammaGain.lift.value,
                    Gamma = liftGammaGain.gamma.value,
                    Gain = liftGammaGain.gain.value,
                    Active = liftGammaGain.active,
                    LiftOverride = liftGammaGain.lift.overrideState,
                    GammaOverride = liftGammaGain.gamma.overrideState,
                    GainOverride = liftGammaGain.gain.overrideState,
                });
            }

            if (profile.TryGet(out MotionBlur motionBlur))
            {
                this.AddComponent(entity, new VolumeMotionBlur
                {
                    Mode = motionBlur.mode.value,
                    Quality = motionBlur.quality.value,
                    Intensity = motionBlur.intensity.value,
                    Clamp = motionBlur.clamp.value,
                    Active = motionBlur.active,
                    ModeOverride = motionBlur.mode.overrideState,
                    QualityOverride = motionBlur.quality.overrideState,
                    IntensityOverride = motionBlur.intensity.overrideState,
                    ClampOverride = motionBlur.clamp.overrideState,
                });
            }

            if (profile.TryGet(out PaniniProjection paniniProjection))
            {
                this.AddComponent(entity, new VolumePaniniProjection
                {
                    Distance = paniniProjection.distance.value,
                    CropToFit = paniniProjection.cropToFit.value,
                    Active = paniniProjection.active,
                    DistanceOverride = paniniProjection.distance.overrideState,
                    CropToFitOverride = paniniProjection.cropToFit.overrideState,
                });
            }

            if (profile.TryGet(out ScreenSpaceLensFlare screenSpaceLensFlare))
            {
                this.AddComponent(entity, new VolumeScreenSpaceLensFlare
                {
                    Intensity = screenSpaceLensFlare.intensity.value,
                    TintColor = screenSpaceLensFlare.tintColor.value,
                    BloomMip = screenSpaceLensFlare.bloomMip.value,
                    FirstFlareIntensity = screenSpaceLensFlare.firstFlareIntensity.value,
                    SecondaryFlareIntensity = screenSpaceLensFlare.secondaryFlareIntensity.value,
                    WarpedFlareIntensity = screenSpaceLensFlare.warpedFlareIntensity.value,
                    WarpedFlareScale = screenSpaceLensFlare.warpedFlareScale.value,
                    Samples = screenSpaceLensFlare.samples.value,
                    SampleDimmer = screenSpaceLensFlare.sampleDimmer.value,
                    VignetteEffect = screenSpaceLensFlare.vignetteEffect.value,
                    StartingPosition = screenSpaceLensFlare.startingPosition.value,
                    Scale = screenSpaceLensFlare.scale.value,
                    StreaksIntensity = screenSpaceLensFlare.streaksIntensity.value,
                    StreaksLength = screenSpaceLensFlare.streaksLength.value,
                    StreaksOrientation = screenSpaceLensFlare.streaksOrientation.value,
                    StreaksThreshold = screenSpaceLensFlare.streaksThreshold.value,
                    Resolution = screenSpaceLensFlare.resolution.value,
                    ChromaticAbberationIntensity = screenSpaceLensFlare.chromaticAbberationIntensity.value,
                    Active = screenSpaceLensFlare.active,
                    IntensityOverride = screenSpaceLensFlare.intensity.overrideState,
                    TintColorOverride = screenSpaceLensFlare.tintColor.overrideState,
                    BloomMipOverride = screenSpaceLensFlare.bloomMip.overrideState,
                    FirstFlareIntensityOverride = screenSpaceLensFlare.firstFlareIntensity.overrideState,
                    SecondaryFlareIntensityOverride = screenSpaceLensFlare.secondaryFlareIntensity.overrideState,
                    WarpedFlareIntensityOverride = screenSpaceLensFlare.warpedFlareIntensity.overrideState,
                    WarpedFlareScaleOverride = screenSpaceLensFlare.warpedFlareScale.overrideState,
                    SamplesOverride = screenSpaceLensFlare.samples.overrideState,
                    SampleDimmerOverride = screenSpaceLensFlare.sampleDimmer.overrideState,
                    VignetteEffectOverride = screenSpaceLensFlare.vignetteEffect.overrideState,
                    StartingPositionOverride = screenSpaceLensFlare.startingPosition.overrideState,
                    ScaleOverride = screenSpaceLensFlare.scale.overrideState,
                    StreaksIntensityOverride = screenSpaceLensFlare.streaksIntensity.overrideState,
                    StreaksLengthOverride = screenSpaceLensFlare.streaksLength.overrideState,
                    StreaksOrientationOverride = screenSpaceLensFlare.streaksOrientation.overrideState,
                    StreaksThresholdOverride = screenSpaceLensFlare.streaksThreshold.overrideState,
                    ResolutionOverride = screenSpaceLensFlare.resolution.overrideState,
                    ChromaticAbberationIntensityOverride = screenSpaceLensFlare.chromaticAbberationIntensity.overrideState,
                });
            }

            if (profile.TryGet(out ShadowsMidtonesHighlights shadowsMidtonesHighlights))
            {
                this.AddComponent(entity, new VolumeShadowsMidtonesHighlights
                {
                    Shadows = shadowsMidtonesHighlights.shadows.value,
                    Midtones = shadowsMidtonesHighlights.midtones.value,
                    Highlights = shadowsMidtonesHighlights.highlights.value,
                    ShadowsStart = shadowsMidtonesHighlights.shadowsStart.value,
                    ShadowsEnd = shadowsMidtonesHighlights.shadowsEnd.value,
                    HighlightsStart = shadowsMidtonesHighlights.highlightsStart.value,
                    HighlightsEnd = shadowsMidtonesHighlights.highlightsEnd.value,
                    Active = shadowsMidtonesHighlights.active,
                    ShadowsOverride = shadowsMidtonesHighlights.shadows.overrideState,
                    MidtonesOverride = shadowsMidtonesHighlights.midtones.overrideState,
                    HighlightsOverride = shadowsMidtonesHighlights.highlights.overrideState,
                    ShadowsStartOverride = shadowsMidtonesHighlights.shadowsStart.overrideState,
                    ShadowsEndOverride = shadowsMidtonesHighlights.shadowsEnd.overrideState,
                    HighlightsStartOverride = shadowsMidtonesHighlights.highlightsStart.overrideState,
                    HighlightsEndOverride = shadowsMidtonesHighlights.highlightsEnd.overrideState,
                });
            }

            if (profile.TryGet(out SplitToning splitToning))
            {
                this.AddComponent(entity, new VolumeSplitToning
                {
                    Shadows = splitToning.shadows.value,
                    Highlights = splitToning.highlights.value,
                    Balance = splitToning.balance.value,
                    Active = splitToning.active,
                    ShadowsOverride = splitToning.shadows.overrideState,
                    HighlightsOverride = splitToning.highlights.overrideState,
                    BalanceOverride = splitToning.balance.overrideState,
                });
            }

            if (profile.TryGet(out Tonemapping tonemapping))
            {
                this.AddComponent(entity, new VolumeTonemapping
                {
                    Mode = tonemapping.mode.value,
                    NeutralHDRRangeReductionMode = tonemapping.neutralHDRRangeReductionMode.value,
                    AcesPreset = tonemapping.acesPreset.value,
                    HueShiftAmount = tonemapping.hueShiftAmount.value,
                    DetectPaperWhite = tonemapping.detectPaperWhite.value,
                    PaperWhite = tonemapping.paperWhite.value,
                    DetectBrightnessLimits = tonemapping.detectBrightnessLimits.value,
                    MinNits = tonemapping.minNits.value,
                    MaxNits = tonemapping.maxNits.value,
                    Active = tonemapping.active,
                    ModeOverride = tonemapping.mode.overrideState,
                    NeutralHDRRangeReductionModeOverride = tonemapping.neutralHDRRangeReductionMode.overrideState,
                    AcesPresetOverride = tonemapping.acesPreset.overrideState,
                    HueShiftAmountOverride = tonemapping.hueShiftAmount.overrideState,
                    DetectPaperWhiteOverride = tonemapping.detectPaperWhite.overrideState,
                    PaperWhiteOverride = tonemapping.paperWhite.overrideState,
                    DetectBrightnessLimitsOverride = tonemapping.detectBrightnessLimits.overrideState,
                    MinNitsOverride = tonemapping.minNits.overrideState,
                    MaxNitsOverride = tonemapping.maxNits.overrideState,
                });
            }

            if (profile.TryGet(out WhiteBalance whiteBalance))
            {
                this.AddComponent(entity, new VolumeWhiteBalance
                {
                    Temperature = whiteBalance.temperature.value,
                    Tint = whiteBalance.tint.value,
                    Active = whiteBalance.active,
                    TemperatureOverride = whiteBalance.temperature.overrideState,
                    TintOverride = whiteBalance.tint.overrideState,
                });
            }

            if (profile.TryGet(out Vignette vignette))
            {
                this.AddComponent(entity, new VolumeVignette
                {
                    Color = vignette.color.value,
                    Center = vignette.center.value,
                    Intensity = vignette.intensity.value,
                    Smoothness = vignette.smoothness.value,
                    Active = vignette.active,
                    Rounded = vignette.rounded.value,
                    ColorOverride = vignette.color.overrideState,
                    CenterOverride = vignette.center.overrideState,
                    IntensityOverride = vignette.intensity.overrideState,
                    SmoothnessOverride = vignette.smoothness.overrideState,
                    RoundedOverride = vignette.rounded.overrideState,
                });
            }
        }

        private static void BakeCurves(ref BlobBuilder builder, ref VolumeColorCurvesBlob blob, ColorCurves colorCurves)
        {
            BakeCurve(ref builder, ref blob.Master, colorCurves.master.value, 0f, false);
            BakeCurve(ref builder, ref blob.Red, colorCurves.red.value, 0f, false);
            BakeCurve(ref builder, ref blob.Green, colorCurves.green.value, 0f, false);
            BakeCurve(ref builder, ref blob.Blue, colorCurves.blue.value, 0f, false);
            BakeCurve(ref builder, ref blob.HueVsHue, colorCurves.hueVsHue.value, 0.5f, true);
            BakeCurve(ref builder, ref blob.HueVsSat, colorCurves.hueVsSat.value, 0.5f, true);
            BakeCurve(ref builder, ref blob.SatVsSat, colorCurves.satVsSat.value, 0.5f, false);
            BakeCurve(ref builder, ref blob.LumVsSat, colorCurves.lumVsSat.value, 0.5f, false);
        }

        private static void BakeCurve(ref BlobBuilder builder, ref VolumeColorCurveBlob target, TextureCurve source, float zeroValue, bool loop)
        {
            target.ZeroValue = zeroValue;
            target.Bounds = new float2(0f, 1f);
            target.Loop = loop;

            var curve = ExtractCurve(source, zeroValue, loop);
            BlobCurve.Construct(ref builder, ref target.Curve, curve);
        }

        private static AnimationCurve ExtractCurve(TextureCurve source, float zeroValue, bool loop)
        {
            if (source == null)
            {
                return CreateFallbackCurve(zeroValue, loop);
            }

            source.Evaluate(0f);
            var length = source.length;
            if (length == 0)
            {
                return CreateFallbackCurve(zeroValue, loop);
            }

            var keys = new Keyframe[length];
            for (var i = 0; i < length; i++)
            {
                keys[i] = source[i];
            }

            var curve = new AnimationCurve(keys);
            SetWrapMode(curve, loop);
            return curve;
        }

        private static AnimationCurve CreateFallbackCurve(float zeroValue, bool loop)
        {
            var curve = new AnimationCurve(new Keyframe(0f, zeroValue));
            SetWrapMode(curve, loop);
            return curve;
        }

        private static void SetWrapMode(AnimationCurve curve, bool loop)
        {
            var wrapMode = loop ? WrapMode.Loop : WrapMode.Clamp;
            curve.preWrapMode = wrapMode;
            curve.postWrapMode = wrapMode;
        }
    }
}
#endif
