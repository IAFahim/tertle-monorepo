// <copyright file="ServerPrefabs.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Data.App
{
    using BovineLabs.Core.ObjectManagement;
    using Unity.Entities;

    public struct ServerPrefabs : IComponentData
    {
        // public ObjectId PauseGhost;

        public ObjectId PlayerController;
        public ObjectId PlayerCharacter;
    }
}
