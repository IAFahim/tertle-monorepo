// <copyright file="VolumeProfileURPSyncSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge
{
    using System;
    using BovineLabs.Bridge.Data;
    using BovineLabs.Bridge.Data.Volume;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [UpdateAfter(typeof(VolumeSyncSystem))]
    [UpdateInGroup(typeof(BridgeSyncSystemGroup))]
    public partial struct VolumeProfileURPSyncSystem : ISystem
    {
        private const int TextureCurveSampleCount = 128;
        private const float TextureCurveSampleStep = 1f / TextureCurveSampleCount;

        static unsafe VolumeProfileURPSyncSystem()
        {
            Burst.Bloom.Data = new BurstTrampoline(&BloomChangedPacked);
            Burst.ChannelMixer.Data = new BurstTrampoline(&ChannelMixerChangedPacked);
            Burst.ChromaticAberration.Data = new BurstTrampoline(&ChromaticAberrationChangedPacked);
            Burst.ColorAdjustments.Data = new BurstTrampoline(&ColorAdjustmentsChangedPacked);
            Burst.ColorCurves.Data = new BurstTrampoline(&ColorCurvesChangedPacked);
            Burst.ColorLookup.Data = new BurstTrampoline(&ColorLookupChangedPacked);
            Burst.DepthOfField.Data = new BurstTrampoline(&DepthOfFieldChangedPacked);
            Burst.FilmGrain.Data = new BurstTrampoline(&FilmGrainChangedPacked);
            Burst.LensDistortion.Data = new BurstTrampoline(&LensDistortionChangedPacked);
            Burst.LiftGammaGain.Data = new BurstTrampoline(&LiftGammaGainChangedPacked);
            Burst.MotionBlur.Data = new BurstTrampoline(&MotionBlurChangedPacked);
            Burst.PaniniProjection.Data = new BurstTrampoline(&PaniniProjectionChangedPacked);
            Burst.ScreenSpaceLensFlare.Data = new BurstTrampoline(&ScreenSpaceLensFlareChangedPacked);
            Burst.ShadowsMidtonesHighlights.Data = new BurstTrampoline(&ShadowsMidtonesHighlightsChangedPacked);
            Burst.SplitToning.Data = new BurstTrampoline(&SplitToningChangedPacked);
            Burst.Tonemapping.Data = new BurstTrampoline(&TonemappingChangedPacked);
            Burst.WhiteBalance.Data = new BurstTrampoline(&WhiteBalanceChangedPacked);
            Burst.Vignette.Data = new BurstTrampoline(&VignetteChangedPacked);
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (bloomData, bridge) in SystemAPI.Query<RefRO<VolumeBloom>, RefRO<BridgeObject>>()
                .WithChangeFilter<VolumeBloom>())
            {
                Burst.Bloom.Data.Invoke(bridge.ValueRO, bloomData.ValueRO);
            }

            foreach (var (channelMixerData, bridge) in SystemAPI.Query<RefRO<VolumeChannelMixer>, RefRO<BridgeObject>>()
                .WithChangeFilter<VolumeChannelMixer>())
            {
                Burst.ChannelMixer.Data.Invoke(bridge.ValueRO, channelMixerData.ValueRO);
            }

            foreach (var (chromaticAberrationData, bridge) in SystemAPI.Query<RefRO<VolumeChromaticAberration>, RefRO<BridgeObject>>()
                .WithChangeFilter<VolumeChromaticAberration>())
            {
                Burst.ChromaticAberration.Data.Invoke(bridge.ValueRO, chromaticAberrationData.ValueRO);
            }

            foreach (var (colorAdjustmentsData, bridge) in SystemAPI.Query<RefRO<VolumeColorAdjustments>, RefRO<BridgeObject>>()
                .WithChangeFilter<VolumeColorAdjustments>())
            {
                Burst.ColorAdjustments.Data.Invoke(bridge.ValueRO, colorAdjustmentsData.ValueRO);
            }

            foreach (var (colorCurvesData, bridge) in SystemAPI.Query<RefRO<VolumeColorCurves>, RefRO<BridgeObject>>()
                .WithChangeFilter<VolumeColorCurves>())
            {
                Burst.ColorCurves.Data.Invoke(bridge.ValueRO, colorCurvesData.ValueRO);
            }

            foreach (var (colorLookupData, bridge) in SystemAPI.Query<RefRO<VolumeColorLookup>, RefRO<BridgeObject>>()
                .WithChangeFilter<VolumeColorLookup>())
            {
                Burst.ColorLookup.Data.Invoke(bridge.ValueRO, colorLookupData.ValueRO);
            }

            foreach (var (depthOfFieldData, bridge) in SystemAPI.Query<RefRO<VolumeDepthOfField>, RefRO<BridgeObject>>()
                .WithChangeFilter<VolumeDepthOfField>())
            {
                Burst.DepthOfField.Data.Invoke(bridge.ValueRO, depthOfFieldData.ValueRO);
            }

            foreach (var (filmGrainData, bridge) in SystemAPI.Query<RefRO<VolumeFilmGrain>, RefRO<BridgeObject>>()
                .WithChangeFilter<VolumeFilmGrain>())
            {
                Burst.FilmGrain.Data.Invoke(bridge.ValueRO, filmGrainData.ValueRO);
            }

            foreach (var (lensDistortionData, bridge) in SystemAPI.Query<RefRO<VolumeLensDistortion>, RefRO<BridgeObject>>()
                .WithChangeFilter<VolumeLensDistortion>())
            {
                Burst.LensDistortion.Data.Invoke(bridge.ValueRO, lensDistortionData.ValueRO);
            }

            foreach (var (liftGammaGainData, bridge) in SystemAPI.Query<RefRO<VolumeLiftGammaGain>, RefRO<BridgeObject>>()
                .WithChangeFilter<VolumeLiftGammaGain>())
            {
                Burst.LiftGammaGain.Data.Invoke(bridge.ValueRO, liftGammaGainData.ValueRO);
            }

            foreach (var (motionBlurData, bridge) in SystemAPI.Query<RefRO<VolumeMotionBlur>, RefRO<BridgeObject>>()
                .WithChangeFilter<VolumeMotionBlur>())
            {
                Burst.MotionBlur.Data.Invoke(bridge.ValueRO, motionBlurData.ValueRO);
            }

            foreach (var (paniniProjectionData, bridge) in SystemAPI.Query<RefRO<VolumePaniniProjection>, RefRO<BridgeObject>>()
                .WithChangeFilter<VolumePaniniProjection>())
            {
                Burst.PaniniProjection.Data.Invoke(bridge.ValueRO, paniniProjectionData.ValueRO);
            }

            foreach (var (screenSpaceLensFlareData, bridge) in SystemAPI.Query<RefRO<VolumeScreenSpaceLensFlare>, RefRO<BridgeObject>>()
                .WithChangeFilter<VolumeScreenSpaceLensFlare>())
            {
                Burst.ScreenSpaceLensFlare.Data.Invoke(bridge.ValueRO, screenSpaceLensFlareData.ValueRO);
            }

            foreach (var (shadowsMidtonesHighlightsData, bridge) in SystemAPI.Query<RefRO<VolumeShadowsMidtonesHighlights>, RefRO<BridgeObject>>()
                .WithChangeFilter<VolumeShadowsMidtonesHighlights>())
            {
                Burst.ShadowsMidtonesHighlights.Data.Invoke(bridge.ValueRO, shadowsMidtonesHighlightsData.ValueRO);
            }

            foreach (var (splitToningData, bridge) in SystemAPI.Query<RefRO<VolumeSplitToning>, RefRO<BridgeObject>>()
                .WithChangeFilter<VolumeSplitToning>())
            {
                Burst.SplitToning.Data.Invoke(bridge.ValueRO, splitToningData.ValueRO);
            }

            foreach (var (tonemappingData, bridge) in SystemAPI.Query<RefRO<VolumeTonemapping>, RefRO<BridgeObject>>()
                .WithChangeFilter<VolumeTonemapping>())
            {
                Burst.Tonemapping.Data.Invoke(bridge.ValueRO, tonemappingData.ValueRO);
            }

            foreach (var (whiteBalanceData, bridge) in SystemAPI.Query<RefRO<VolumeWhiteBalance>, RefRO<BridgeObject>>()
                .WithChangeFilter<VolumeWhiteBalance>())
            {
                Burst.WhiteBalance.Data.Invoke(bridge.ValueRO, whiteBalanceData.ValueRO);
            }

            foreach (var (vignetteData, bridge) in SystemAPI.Query<RefRO<VolumeVignette>, RefRO<BridgeObject>>()
                .WithChangeFilter<VolumeVignette>())
            {
                Burst.Vignette.Data.Invoke(bridge.ValueRO, vignetteData.ValueRO);
            }
        }

        private static bool TryGetProfile(Volume volume, out VolumeProfile profile)
        {
            if (volume == null)
            {
                profile = null;
                return false;
            }

            profile = volume.profile;
            return profile != null;
        }

                private static unsafe void BloomChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, VolumeBloom>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var volume = bridge.Q<Volume>();
            if (!TryGetProfile(volume, out var profile))
            {
                return;
            }

            if (profile.TryGet(out Bloom bloom))
            {
                bloom.active = component.Active;
                bloom.threshold.overrideState = component.ThresholdOverride;
                bloom.threshold.value = component.Threshold;
                bloom.intensity.overrideState = component.IntensityOverride;
                bloom.intensity.value = component.Intensity;
                bloom.scatter.overrideState = component.ScatterOverride;
                bloom.scatter.value = component.Scatter;
                bloom.clamp.overrideState = component.ClampOverride;
                bloom.clamp.value = component.Clamp;
                bloom.tint.overrideState = component.TintOverride;
                bloom.tint.value = component.Tint;
                bloom.highQualityFiltering.overrideState = component.HighQualityFilteringOverride;
                bloom.highQualityFiltering.value = component.HighQualityFiltering;
                bloom.filter.overrideState = component.FilterOverride;
                bloom.filter.value = component.Filter;
                bloom.downscale.overrideState = component.DownscaleOverride;
                bloom.downscale.value = component.Downscale;
                bloom.maxIterations.overrideState = component.MaxIterationsOverride;
                bloom.maxIterations.value = component.MaxIterations;
                bloom.dirtTexture.overrideState = component.DirtTextureOverride;
                bloom.dirtTexture.value = component.DirtTexture.Value;
                bloom.dirtIntensity.overrideState = component.DirtIntensityOverride;
                bloom.dirtIntensity.value = component.DirtIntensity;
            }
        }

                private static unsafe void ChannelMixerChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, VolumeChannelMixer>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var volume = bridge.Q<Volume>();
            if (!TryGetProfile(volume, out var profile))
            {
                return;
            }

            if (profile.TryGet(out ChannelMixer channelMixer))
            {
                channelMixer.active = component.Active;
                channelMixer.redOutRedIn.overrideState = component.RedOutRedInOverride;
                channelMixer.redOutRedIn.value = component.RedOutRedIn;
                channelMixer.redOutGreenIn.overrideState = component.RedOutGreenInOverride;
                channelMixer.redOutGreenIn.value = component.RedOutGreenIn;
                channelMixer.redOutBlueIn.overrideState = component.RedOutBlueInOverride;
                channelMixer.redOutBlueIn.value = component.RedOutBlueIn;
                channelMixer.greenOutRedIn.overrideState = component.GreenOutRedInOverride;
                channelMixer.greenOutRedIn.value = component.GreenOutRedIn;
                channelMixer.greenOutGreenIn.overrideState = component.GreenOutGreenInOverride;
                channelMixer.greenOutGreenIn.value = component.GreenOutGreenIn;
                channelMixer.greenOutBlueIn.overrideState = component.GreenOutBlueInOverride;
                channelMixer.greenOutBlueIn.value = component.GreenOutBlueIn;
                channelMixer.blueOutRedIn.overrideState = component.BlueOutRedInOverride;
                channelMixer.blueOutRedIn.value = component.BlueOutRedIn;
                channelMixer.blueOutGreenIn.overrideState = component.BlueOutGreenInOverride;
                channelMixer.blueOutGreenIn.value = component.BlueOutGreenIn;
                channelMixer.blueOutBlueIn.overrideState = component.BlueOutBlueInOverride;
                channelMixer.blueOutBlueIn.value = component.BlueOutBlueIn;
            }
        }

                private static unsafe void ChromaticAberrationChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, VolumeChromaticAberration>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var volume = bridge.Q<Volume>();
            if (!TryGetProfile(volume, out var profile))
            {
                return;
            }

            if (profile.TryGet(out ChromaticAberration chromaticAberration))
            {
                chromaticAberration.active = component.Active;
                chromaticAberration.intensity.overrideState = component.IntensityOverride;
                chromaticAberration.intensity.value = component.Intensity;
            }
        }

                private static unsafe void ColorAdjustmentsChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, VolumeColorAdjustments>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var volume = bridge.Q<Volume>();
            if (!TryGetProfile(volume, out var profile))
            {
                return;
            }

            if (profile.TryGet(out ColorAdjustments colorAdjustments))
            {
                colorAdjustments.active = component.Active;
                colorAdjustments.postExposure.overrideState = component.PostExposureOverride;
                colorAdjustments.postExposure.value = component.PostExposure;
                colorAdjustments.contrast.overrideState = component.ContrastOverride;
                colorAdjustments.contrast.value = component.Contrast;
                colorAdjustments.colorFilter.overrideState = component.ColorFilterOverride;
                colorAdjustments.colorFilter.value = component.ColorFilter;
                colorAdjustments.hueShift.overrideState = component.HueShiftOverride;
                colorAdjustments.hueShift.value = component.HueShift;
                colorAdjustments.saturation.overrideState = component.SaturationOverride;
                colorAdjustments.saturation.value = component.Saturation;
            }
        }

                private static unsafe void ColorCurvesChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, VolumeColorCurves>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            if (!component.Curves.IsCreated)
            {
                return;
            }

            var volume = bridge.Q<Volume>();
            if (!TryGetProfile(volume, out var profile))
            {
                return;
            }

            if (profile.TryGet(out ColorCurves colorCurves))
            {
                ref var curves = ref component.Curves.Value;

                colorCurves.active = component.Active;
                colorCurves.master.overrideState = component.MasterOverride;
                ApplyCurve(colorCurves.master, ref curves.Master);
                colorCurves.red.overrideState = component.RedOverride;
                ApplyCurve(colorCurves.red, ref curves.Red);
                colorCurves.green.overrideState = component.GreenOverride;
                ApplyCurve(colorCurves.green, ref curves.Green);
                colorCurves.blue.overrideState = component.BlueOverride;
                ApplyCurve(colorCurves.blue, ref curves.Blue);
                colorCurves.hueVsHue.overrideState = component.HueVsHueOverride;
                ApplyCurve(colorCurves.hueVsHue, ref curves.HueVsHue);
                colorCurves.hueVsSat.overrideState = component.HueVsSatOverride;
                ApplyCurve(colorCurves.hueVsSat, ref curves.HueVsSat);
                colorCurves.satVsSat.overrideState = component.SatVsSatOverride;
                ApplyCurve(colorCurves.satVsSat, ref curves.SatVsSat);
                colorCurves.lumVsSat.overrideState = component.LumVsSatOverride;
                ApplyCurve(colorCurves.lumVsSat, ref curves.LumVsSat);
            }
        }

        private static void ApplyCurve(TextureCurveParameter target, ref VolumeColorCurveBlob source)
        {
            var curve = target.value;
            if (curve == null)
            {
                curve = new TextureCurve(Array.Empty<Keyframe>(), source.ZeroValue, source.Loop, new Vector2(source.Bounds.x, source.Bounds.y));
                target.value = curve;
            }
            else
            {
                curve.Evaluate(0f);
                for (var i = curve.length - 1; i >= 0; i--)
                {
                    curve.RemoveKey(i);
                }
            }

            for (var i = 0; i < TextureCurveSampleCount; i++)
            {
                var time = i * TextureCurveSampleStep;
                var value = source.Curve.IsCreated ? source.Curve.Evaluate(time) : source.ZeroValue;
                curve.AddKey(time, value);
            }
        }

                private static unsafe void ColorLookupChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, VolumeColorLookup>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var volume = bridge.Q<Volume>();
            if (!TryGetProfile(volume, out var profile))
            {
                return;
            }

            if (profile.TryGet(out ColorLookup colorLookup))
            {
                colorLookup.active = component.Active;
                colorLookup.texture.overrideState = component.TextureOverride;
                colorLookup.texture.value = component.Texture.Value;
                colorLookup.contribution.overrideState = component.ContributionOverride;
                colorLookup.contribution.value = component.Contribution;
            }
        }

                private static unsafe void DepthOfFieldChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, VolumeDepthOfField>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var volume = bridge.Q<Volume>();
            if (!TryGetProfile(volume, out var profile))
            {
                return;
            }

            if (profile.TryGet(out DepthOfField depthOfField))
            {
                depthOfField.active = component.Active;
                depthOfField.mode.overrideState = component.ModeOverride;
                depthOfField.mode.value = component.Mode;
                depthOfField.gaussianStart.overrideState = component.GaussianStartOverride;
                depthOfField.gaussianStart.value = component.GaussianStart;
                depthOfField.gaussianEnd.overrideState = component.GaussianEndOverride;
                depthOfField.gaussianEnd.value = component.GaussianEnd;
                depthOfField.gaussianMaxRadius.overrideState = component.GaussianMaxRadiusOverride;
                depthOfField.gaussianMaxRadius.value = component.GaussianMaxRadius;
                depthOfField.highQualitySampling.overrideState = component.HighQualitySamplingOverride;
                depthOfField.highQualitySampling.value = component.HighQualitySampling;
                depthOfField.focusDistance.overrideState = component.FocusDistanceOverride;
                depthOfField.focusDistance.value = component.FocusDistance;
                depthOfField.aperture.overrideState = component.ApertureOverride;
                depthOfField.aperture.value = component.Aperture;
                depthOfField.focalLength.overrideState = component.FocalLengthOverride;
                depthOfField.focalLength.value = component.FocalLength;
                depthOfField.bladeCount.overrideState = component.BladeCountOverride;
                depthOfField.bladeCount.value = component.BladeCount;
                depthOfField.bladeCurvature.overrideState = component.BladeCurvatureOverride;
                depthOfField.bladeCurvature.value = component.BladeCurvature;
                depthOfField.bladeRotation.overrideState = component.BladeRotationOverride;
                depthOfField.bladeRotation.value = component.BladeRotation;
            }
        }

                private static unsafe void FilmGrainChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, VolumeFilmGrain>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var volume = bridge.Q<Volume>();
            if (!TryGetProfile(volume, out var profile))
            {
                return;
            }

            if (profile.TryGet(out FilmGrain filmGrain))
            {
                filmGrain.active = component.Active;
                filmGrain.type.overrideState = component.TypeOverride;
                filmGrain.type.value = component.Type;
                filmGrain.intensity.overrideState = component.IntensityOverride;
                filmGrain.intensity.value = component.Intensity;
                filmGrain.response.overrideState = component.ResponseOverride;
                filmGrain.response.value = component.Response;
                filmGrain.texture.overrideState = component.TextureOverride;
                filmGrain.texture.value = component.Texture.Value;
            }
        }

                private static unsafe void LensDistortionChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, VolumeLensDistortion>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var volume = bridge.Q<Volume>();
            if (!TryGetProfile(volume, out var profile))
            {
                return;
            }

            if (profile.TryGet(out LensDistortion lensDistortion))
            {
                lensDistortion.active = component.Active;
                lensDistortion.intensity.overrideState = component.IntensityOverride;
                lensDistortion.intensity.value = component.Intensity;
                lensDistortion.xMultiplier.overrideState = component.XMultiplierOverride;
                lensDistortion.xMultiplier.value = component.XMultiplier;
                lensDistortion.yMultiplier.overrideState = component.YMultiplierOverride;
                lensDistortion.yMultiplier.value = component.YMultiplier;
                lensDistortion.center.overrideState = component.CenterOverride;
                lensDistortion.center.value = component.Center;
                lensDistortion.scale.overrideState = component.ScaleOverride;
                lensDistortion.scale.value = component.Scale;
            }
        }

                private static unsafe void LiftGammaGainChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, VolumeLiftGammaGain>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var volume = bridge.Q<Volume>();
            if (!TryGetProfile(volume, out var profile))
            {
                return;
            }

            if (profile.TryGet(out LiftGammaGain liftGammaGain))
            {
                liftGammaGain.active = component.Active;
                liftGammaGain.lift.overrideState = component.LiftOverride;
                liftGammaGain.lift.value = component.Lift;
                liftGammaGain.gamma.overrideState = component.GammaOverride;
                liftGammaGain.gamma.value = component.Gamma;
                liftGammaGain.gain.overrideState = component.GainOverride;
                liftGammaGain.gain.value = component.Gain;
            }
        }

                private static unsafe void MotionBlurChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, VolumeMotionBlur>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var volume = bridge.Q<Volume>();
            if (!TryGetProfile(volume, out var profile))
            {
                return;
            }

            if (profile.TryGet(out MotionBlur motionBlur))
            {
                motionBlur.active = component.Active;
                motionBlur.mode.overrideState = component.ModeOverride;
                motionBlur.mode.value = component.Mode;
                motionBlur.quality.overrideState = component.QualityOverride;
                motionBlur.quality.value = component.Quality;
                motionBlur.intensity.overrideState = component.IntensityOverride;
                motionBlur.intensity.value = component.Intensity;
                motionBlur.clamp.overrideState = component.ClampOverride;
                motionBlur.clamp.value = component.Clamp;
            }
        }

                private static unsafe void PaniniProjectionChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, VolumePaniniProjection>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var volume = bridge.Q<Volume>();
            if (!TryGetProfile(volume, out var profile))
            {
                return;
            }

            if (profile.TryGet(out PaniniProjection paniniProjection))
            {
                paniniProjection.active = component.Active;
                paniniProjection.distance.overrideState = component.DistanceOverride;
                paniniProjection.distance.value = component.Distance;
                paniniProjection.cropToFit.overrideState = component.CropToFitOverride;
                paniniProjection.cropToFit.value = component.CropToFit;
            }
        }

                private static unsafe void ScreenSpaceLensFlareChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, VolumeScreenSpaceLensFlare>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var volume = bridge.Q<Volume>();
            if (!TryGetProfile(volume, out var profile))
            {
                return;
            }

            if (profile.TryGet(out ScreenSpaceLensFlare screenSpaceLensFlare))
            {
                screenSpaceLensFlare.active = component.Active;
                screenSpaceLensFlare.intensity.overrideState = component.IntensityOverride;
                screenSpaceLensFlare.intensity.value = component.Intensity;
                screenSpaceLensFlare.tintColor.overrideState = component.TintColorOverride;
                screenSpaceLensFlare.tintColor.value = component.TintColor;
                screenSpaceLensFlare.bloomMip.overrideState = component.BloomMipOverride;
                screenSpaceLensFlare.bloomMip.value = component.BloomMip;
                screenSpaceLensFlare.firstFlareIntensity.overrideState = component.FirstFlareIntensityOverride;
                screenSpaceLensFlare.firstFlareIntensity.value = component.FirstFlareIntensity;
                screenSpaceLensFlare.secondaryFlareIntensity.overrideState = component.SecondaryFlareIntensityOverride;
                screenSpaceLensFlare.secondaryFlareIntensity.value = component.SecondaryFlareIntensity;
                screenSpaceLensFlare.warpedFlareIntensity.overrideState = component.WarpedFlareIntensityOverride;
                screenSpaceLensFlare.warpedFlareIntensity.value = component.WarpedFlareIntensity;
                screenSpaceLensFlare.warpedFlareScale.overrideState = component.WarpedFlareScaleOverride;
                screenSpaceLensFlare.warpedFlareScale.value = component.WarpedFlareScale;
                screenSpaceLensFlare.samples.overrideState = component.SamplesOverride;
                screenSpaceLensFlare.samples.value = component.Samples;
                screenSpaceLensFlare.sampleDimmer.overrideState = component.SampleDimmerOverride;
                screenSpaceLensFlare.sampleDimmer.value = component.SampleDimmer;
                screenSpaceLensFlare.vignetteEffect.overrideState = component.VignetteEffectOverride;
                screenSpaceLensFlare.vignetteEffect.value = component.VignetteEffect;
                screenSpaceLensFlare.startingPosition.overrideState = component.StartingPositionOverride;
                screenSpaceLensFlare.startingPosition.value = component.StartingPosition;
                screenSpaceLensFlare.scale.overrideState = component.ScaleOverride;
                screenSpaceLensFlare.scale.value = component.Scale;
                screenSpaceLensFlare.streaksIntensity.overrideState = component.StreaksIntensityOverride;
                screenSpaceLensFlare.streaksIntensity.value = component.StreaksIntensity;
                screenSpaceLensFlare.streaksLength.overrideState = component.StreaksLengthOverride;
                screenSpaceLensFlare.streaksLength.value = component.StreaksLength;
                screenSpaceLensFlare.streaksOrientation.overrideState = component.StreaksOrientationOverride;
                screenSpaceLensFlare.streaksOrientation.value = component.StreaksOrientation;
                screenSpaceLensFlare.streaksThreshold.overrideState = component.StreaksThresholdOverride;
                screenSpaceLensFlare.streaksThreshold.value = component.StreaksThreshold;
                screenSpaceLensFlare.resolution.overrideState = component.ResolutionOverride;
                screenSpaceLensFlare.resolution.value = component.Resolution;
                screenSpaceLensFlare.chromaticAbberationIntensity.overrideState = component.ChromaticAbberationIntensityOverride;
                screenSpaceLensFlare.chromaticAbberationIntensity.value = component.ChromaticAbberationIntensity;
            }
        }

                private static unsafe void ShadowsMidtonesHighlightsChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, VolumeShadowsMidtonesHighlights>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var volume = bridge.Q<Volume>();
            if (!TryGetProfile(volume, out var profile))
            {
                return;
            }

            if (profile.TryGet(out ShadowsMidtonesHighlights shadowsMidtonesHighlights))
            {
                shadowsMidtonesHighlights.active = component.Active;
                shadowsMidtonesHighlights.shadows.overrideState = component.ShadowsOverride;
                shadowsMidtonesHighlights.shadows.value = component.Shadows;
                shadowsMidtonesHighlights.midtones.overrideState = component.MidtonesOverride;
                shadowsMidtonesHighlights.midtones.value = component.Midtones;
                shadowsMidtonesHighlights.highlights.overrideState = component.HighlightsOverride;
                shadowsMidtonesHighlights.highlights.value = component.Highlights;
                shadowsMidtonesHighlights.shadowsStart.overrideState = component.ShadowsStartOverride;
                shadowsMidtonesHighlights.shadowsStart.value = component.ShadowsStart;
                shadowsMidtonesHighlights.shadowsEnd.overrideState = component.ShadowsEndOverride;
                shadowsMidtonesHighlights.shadowsEnd.value = component.ShadowsEnd;
                shadowsMidtonesHighlights.highlightsStart.overrideState = component.HighlightsStartOverride;
                shadowsMidtonesHighlights.highlightsStart.value = component.HighlightsStart;
                shadowsMidtonesHighlights.highlightsEnd.overrideState = component.HighlightsEndOverride;
                shadowsMidtonesHighlights.highlightsEnd.value = component.HighlightsEnd;
            }
        }

                private static unsafe void SplitToningChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, VolumeSplitToning>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var volume = bridge.Q<Volume>();
            if (!TryGetProfile(volume, out var profile))
            {
                return;
            }

            if (profile.TryGet(out SplitToning splitToning))
            {
                splitToning.active = component.Active;
                splitToning.shadows.overrideState = component.ShadowsOverride;
                splitToning.shadows.value = component.Shadows;
                splitToning.highlights.overrideState = component.HighlightsOverride;
                splitToning.highlights.value = component.Highlights;
                splitToning.balance.overrideState = component.BalanceOverride;
                splitToning.balance.value = component.Balance;
            }
        }

                private static unsafe void TonemappingChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, VolumeTonemapping>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var volume = bridge.Q<Volume>();
            if (!TryGetProfile(volume, out var profile))
            {
                return;
            }

            if (profile.TryGet(out Tonemapping tonemapping))
            {
                tonemapping.active = component.Active;
                tonemapping.mode.overrideState = component.ModeOverride;
                tonemapping.mode.value = component.Mode;
                tonemapping.neutralHDRRangeReductionMode.overrideState = component.NeutralHDRRangeReductionModeOverride;
                tonemapping.neutralHDRRangeReductionMode.value = component.NeutralHDRRangeReductionMode;
                tonemapping.acesPreset.overrideState = component.AcesPresetOverride;
                tonemapping.acesPreset.value = component.AcesPreset;
                tonemapping.hueShiftAmount.overrideState = component.HueShiftAmountOverride;
                tonemapping.hueShiftAmount.value = component.HueShiftAmount;
                tonemapping.detectPaperWhite.overrideState = component.DetectPaperWhiteOverride;
                tonemapping.detectPaperWhite.value = component.DetectPaperWhite;
                tonemapping.paperWhite.overrideState = component.PaperWhiteOverride;
                tonemapping.paperWhite.value = component.PaperWhite;
                tonemapping.detectBrightnessLimits.overrideState = component.DetectBrightnessLimitsOverride;
                tonemapping.detectBrightnessLimits.value = component.DetectBrightnessLimits;
                tonemapping.minNits.overrideState = component.MinNitsOverride;
                tonemapping.minNits.value = component.MinNits;
                tonemapping.maxNits.overrideState = component.MaxNitsOverride;
                tonemapping.maxNits.value = component.MaxNits;
            }
        }

                private static unsafe void WhiteBalanceChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, VolumeWhiteBalance>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var volume = bridge.Q<Volume>();
            if (!TryGetProfile(volume, out var profile))
            {
                return;
            }

            if (profile.TryGet(out WhiteBalance whiteBalance))
            {
                whiteBalance.active = component.Active;
                whiteBalance.temperature.overrideState = component.TemperatureOverride;
                whiteBalance.temperature.value = component.Temperature;
                whiteBalance.tint.overrideState = component.TintOverride;
                whiteBalance.tint.value = component.Tint;
            }
        }

                private static unsafe void VignetteChangedPacked(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedPair<BridgeObject, VolumeVignette>>(argumentsPtr, argumentsSize);
            ref var bridge = ref arguments.First;
            ref var component = ref arguments.Second;
            var volume = bridge.Q<Volume>();
            if (!TryGetProfile(volume, out var profile))
            {
                return;
            }

            if (profile.TryGet(out Vignette vignette))
            {
                vignette.active = component.Active;
                vignette.color.overrideState = component.ColorOverride;
                vignette.color.value = component.Color;
                vignette.center.overrideState = component.CenterOverride;
                vignette.center.value = component.Center;
                vignette.intensity.overrideState = component.IntensityOverride;
                vignette.intensity.value = component.Intensity;
                vignette.smoothness.overrideState = component.SmoothnessOverride;
                vignette.smoothness.value = component.Smoothness;
                vignette.rounded.overrideState = component.RoundedOverride;
                vignette.rounded.value = component.Rounded;
            }
        }

        private static class Burst
        {
            public static readonly SharedStatic<BurstTrampoline> Bloom =
                SharedStatic<BurstTrampoline>.GetOrCreate<VolumeProfileURPSyncSystem, VolumeBloom>();

            public static readonly SharedStatic<BurstTrampoline> ChannelMixer =
                SharedStatic<BurstTrampoline>.GetOrCreate<VolumeProfileURPSyncSystem, VolumeChannelMixer>();

            public static readonly SharedStatic<BurstTrampoline> ChromaticAberration =
                SharedStatic<BurstTrampoline>.GetOrCreate<VolumeProfileURPSyncSystem, VolumeChromaticAberration>();

            public static readonly SharedStatic<BurstTrampoline> ColorAdjustments =
                SharedStatic<BurstTrampoline>.GetOrCreate<VolumeProfileURPSyncSystem, VolumeColorAdjustments>();

            public static readonly SharedStatic<BurstTrampoline> ColorCurves =
                SharedStatic<BurstTrampoline>.GetOrCreate<VolumeProfileURPSyncSystem, VolumeColorCurves>();

            public static readonly SharedStatic<BurstTrampoline> ColorLookup =
                SharedStatic<BurstTrampoline>.GetOrCreate<VolumeProfileURPSyncSystem, VolumeColorLookup>();

            public static readonly SharedStatic<BurstTrampoline> DepthOfField =
                SharedStatic<BurstTrampoline>.GetOrCreate<VolumeProfileURPSyncSystem, VolumeDepthOfField>();

            public static readonly SharedStatic<BurstTrampoline> FilmGrain =
                SharedStatic<BurstTrampoline>.GetOrCreate<VolumeProfileURPSyncSystem, VolumeFilmGrain>();

            public static readonly SharedStatic<BurstTrampoline> LensDistortion =
                SharedStatic<BurstTrampoline>.GetOrCreate<VolumeProfileURPSyncSystem, VolumeLensDistortion>();

            public static readonly SharedStatic<BurstTrampoline> LiftGammaGain =
                SharedStatic<BurstTrampoline>.GetOrCreate<VolumeProfileURPSyncSystem, VolumeLiftGammaGain>();

            public static readonly SharedStatic<BurstTrampoline> MotionBlur =
                SharedStatic<BurstTrampoline>.GetOrCreate<VolumeProfileURPSyncSystem, VolumeMotionBlur>();

            public static readonly SharedStatic<BurstTrampoline> PaniniProjection =
                SharedStatic<BurstTrampoline>.GetOrCreate<VolumeProfileURPSyncSystem, VolumePaniniProjection>();

            public static readonly SharedStatic<BurstTrampoline> ScreenSpaceLensFlare =
                SharedStatic<BurstTrampoline>.GetOrCreate<VolumeProfileURPSyncSystem, VolumeScreenSpaceLensFlare>();

            public static readonly SharedStatic<BurstTrampoline> ShadowsMidtonesHighlights =
                SharedStatic<BurstTrampoline>.GetOrCreate<VolumeProfileURPSyncSystem, VolumeShadowsMidtonesHighlights>();

            public static readonly SharedStatic<BurstTrampoline> SplitToning =
                SharedStatic<BurstTrampoline>.GetOrCreate<VolumeProfileURPSyncSystem, VolumeSplitToning>();

            public static readonly SharedStatic<BurstTrampoline> Tonemapping =
                SharedStatic<BurstTrampoline>.GetOrCreate<VolumeProfileURPSyncSystem, VolumeTonemapping>();

            public static readonly SharedStatic<BurstTrampoline> WhiteBalance =
                SharedStatic<BurstTrampoline>.GetOrCreate<VolumeProfileURPSyncSystem, VolumeWhiteBalance>();

            public static readonly SharedStatic<BurstTrampoline> Vignette =
                SharedStatic<BurstTrampoline>.GetOrCreate<VolumeProfileURPSyncSystem, VolumeVignette>();
        }
    }
}
#endif

