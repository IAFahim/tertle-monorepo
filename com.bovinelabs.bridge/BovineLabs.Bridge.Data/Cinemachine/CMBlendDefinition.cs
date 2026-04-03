// <copyright file="CMBlendDefinition.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Cinemachine;

    public struct CMBlendDefinition
    {
        public CinemachineBlendDefinition.Styles Style;
        public float Time;

        public static implicit operator CMBlendDefinition(CinemachineBlendDefinition blend)
        {
            return new CMBlendDefinition
            {
                Style = blend.Style,
                Time = blend.Time,
            };
        }

        public static implicit operator CinemachineBlendDefinition(CMBlendDefinition blend)
        {
            return new CinemachineBlendDefinition
            {
                Style = blend.Style,
                Time = blend.Time,
            };
        }
    }
}
#endif