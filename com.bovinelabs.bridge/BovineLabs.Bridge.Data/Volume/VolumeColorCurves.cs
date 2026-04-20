// <copyright file="VolumeColorCurves.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Bridge.Data.Volume
{
    using BovineLabs.Core.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    public struct VolumeColorCurves : IComponentData
    {
        public BlobAssetReference<VolumeColorCurvesBlob> Curves;

        public bool Active;
        public bool MasterOverride;
        public bool RedOverride;
        public bool GreenOverride;
        public bool BlueOverride;
        public bool HueVsHueOverride;
        public bool HueVsSatOverride;
        public bool SatVsSatOverride;
        public bool LumVsSatOverride;
    }

    public struct VolumeColorCurvesBlob
    {
        public VolumeColorCurveBlob Master;
        public VolumeColorCurveBlob Red;
        public VolumeColorCurveBlob Green;
        public VolumeColorCurveBlob Blue;
        public VolumeColorCurveBlob HueVsHue;
        public VolumeColorCurveBlob HueVsSat;
        public VolumeColorCurveBlob SatVsSat;
        public VolumeColorCurveBlob LumVsSat;
    }

    public struct VolumeColorCurveBlob
    {
        public BlobCurve Curve;
        public float ZeroValue;
        public float2 Bounds;
        public bool Loop;
    }
}
#endif
