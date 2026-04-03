// <copyright file="SubDirectorTrack.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Authoring
{
    using System;
    using System.ComponentModel;
    using UnityEngine.Timeline;

    /// <summary>
    /// A DOTS-compatible track that supports nested timelines through SubDirectorClip and SubTimelineClip.
    /// Allows for hierarchical timeline composition where timelines can contain other timelines.
    /// </summary>
    [Serializable]
    [TrackColor(0.5f, 0.1f, 0.5f)]
    [TrackClipType(typeof(SubDirectorClip))]
    [TrackClipType(typeof(SubTimelineClip))]
    [DisplayName("DOTS/Sub Director Track")]
    public class SubDirectorTrack : DOTSTrack
    {
    }
}
