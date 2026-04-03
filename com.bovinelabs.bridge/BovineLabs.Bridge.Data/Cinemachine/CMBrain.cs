// <copyright file="CMBrain.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using Unity.Cinemachine;
    using Unity.Entities;

    public struct CMBrain : IComponentData
    {
        public bool IgnoreTimeScale;
        public CinemachineBrain.UpdateMethods UpdateMethod;
        public CinemachineBrain.BrainUpdateMethods BlendUpdateMethod;
        public CMBlendDefinition DefaultBlend;

        public CMBrain(CinemachineBrain brain)
        {
            this.IgnoreTimeScale = brain.IgnoreTimeScale;
            this.UpdateMethod = brain.UpdateMethod;
            this.BlendUpdateMethod = brain.BlendUpdateMethod;
            this.DefaultBlend = brain.DefaultBlend;
        }
    }
}
#endif