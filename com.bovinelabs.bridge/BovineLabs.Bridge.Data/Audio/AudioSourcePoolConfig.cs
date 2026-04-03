// <copyright file="AudioSourcePoolConfig.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Data.Audio
{
    using Unity.Entities;

    public struct AudioSourcePoolConfig : IComponentData
    {
        /// <summary>
        /// Maximum number of AudioSource objects to keep in the pool.
        /// Only the closest (or highest priority) N audio sources will play at once.
        /// </summary>
        public int LoopedAudioPoolSize;

        public float MaxListenDistanceSq;
    }
}
