// <copyright file="TrackAssetExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Authoring
{
    using System.Collections.Generic;
    using UnityEngine.Timeline;

    /// <summary>
    /// Extension methods for Unity's TrackAsset to support multi-layer track operations.
    /// </summary>
    public static class TrackAssetExtensions
    {
        /// <summary>
        /// Gets all clips from all layers of a track, excluding clips on muted tracks.
        /// This includes clips from the main track and all child tracks.
        /// </summary>
        /// <param name="asset">The track asset to get clips from.</param>
        /// <returns>An enumerable of timeline clips from all active layers.</returns>
        public static IEnumerable<TimelineClip> GetActiveClipsFromAllLayers(this TrackAsset asset)
        {
            if (asset.muted)
            {
                yield break;
            }

            foreach (var c in asset.GetClips())
            {
                yield return c;
            }

            foreach (var t in asset.GetChildTracks())
            {
                if (t.muted)
                {
                    continue;
                }

                foreach (var c in t.GetClips())
                {
                    yield return c;
                }
            }
        }
    }
}
