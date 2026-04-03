// <copyright file="CinemachineBrainBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Authoring.Cinemachine
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using Unity.Cinemachine;
    using Unity.Entities;

    public class CinemachineBrainBaker : Baker<CinemachineBrain>
    {
        /// <inheritdoc />
        public override void Bake(CinemachineBrain authoring)
        {
            this.AddComponent(this.GetEntity(TransformUsageFlags.None), new CMBrain(authoring));
        }
    }
}
#endif