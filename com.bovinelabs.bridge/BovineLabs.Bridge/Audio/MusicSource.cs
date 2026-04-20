// <copyright file="MusicSource.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Audio
{
    using UnityEngine;

    [RequireComponent(typeof(AudioSource))]
public sealed class MusicSource : MonoBehaviour
    {
    }
}
#endif
