// <copyright file="IBakerWrapper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Input
{
    using Unity.Entities;

    public interface IBakerWrapper
    {
        void AddComponent<T>(T component)
            where T : unmanaged, IComponentData;
    }
}
