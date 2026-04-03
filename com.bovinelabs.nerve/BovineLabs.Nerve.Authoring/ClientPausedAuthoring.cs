// <copyright file="ClientPausedAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Authoring
{
    using BovineLabs.Nerve.Data.Pause;
    using Unity.Entities;
    using UnityEngine;

    public class ClientPausedAuthoring : MonoBehaviour
    {
        private class ClientPausedBaker : Baker<ClientPausedAuthoring>
        {
            public override void Bake(ClientPausedAuthoring authoring)
            {
                this.AddComponent<ClientPaused>(this.GetEntity(TransformUsageFlags.ManualOverride));
            }
        }
    }
}
