// <copyright file="LightHdBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_HDRP

namespace BovineLabs.Bridge.Authoring.Light
{
    using BovineLabs.Bridge.Data.Light;
    using Unity.Entities;
    using UnityEngine.Rendering.HighDefinition;

    public class LightHdBaker : Baker<HDAdditionalLightData>
    {
        /// <inheritdoc />
        public override void Bake(HDAdditionalLightData authoring)
        {
            var entity = this.GetEntity(TransformUsageFlags.None);

            this.AddComponent(entity, new LightHdData
            {
                LightDimmer = authoring.lightDimmer,
                VolumetricDimmer = authoring.volumetricDimmer,
                AffectDiffuse = authoring.affectDiffuse,
                AffectSpecular = authoring.affectSpecular,
                FadeDistance = authoring.fadeDistance,
                ShadowDimmer = authoring.shadowDimmer,
                ShadowFadeDistance = authoring.shadowFadeDistance,
                AffectsVolumetric = authoring.affectsVolumetric,
            });
        }
    }
}

#endif
