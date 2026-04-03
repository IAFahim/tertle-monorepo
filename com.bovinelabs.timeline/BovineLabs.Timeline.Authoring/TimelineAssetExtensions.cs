// <copyright file="TimelineAssetExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Authoring
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Timeline.Data.Schedular;
    using Unity.Entities;
    using Unity.IntegerTime;
    using UnityEngine.Timeline;

    /// <summary>
    /// Extension methods for Unity's TimelineAsset to support DOTS baking operations.
    /// </summary>
    public static class TimelineAssetExtensions
    {
        /// <summary>
        /// Gets all DOTS-compatible tracks from a timeline asset, excluding muted tracks.
        /// Registers dependencies with the baker for each track.
        /// </summary>
        /// <param name="asset">The timeline asset to get tracks from.</param>
        /// <param name="baker">The baker to register dependencies with.</param>
        /// <returns>An enumerable of DOTS tracks.</returns>
        public static IEnumerable<DOTSTrack> GetDOTSTracks(this TimelineAsset asset, IBaker baker)
        {
            if (asset == null)
            {
                yield break;
            }

            foreach (var track in asset.GetOutputTracks().OfType<DOTSTrack>())
            {
                baker.DependsOn(track);
                if (!track.mutedInHierarchy)
                {
                    yield return track;
                }
            }
        }

        /// <summary>
        /// Gets all DOTS-compatible tracks from a timeline asset, excluding muted tracks.
        /// </summary>
        /// <param name="asset">The timeline asset to get tracks from.</param>
        /// <returns>An enumerable of DOTS tracks.</returns>
        public static IEnumerable<DOTSTrack> GetDOTSTracks(this TimelineAsset asset)
        {
            if (asset == null)
            {
                return Enumerable.Empty<DOTSTrack>();
            }

            return asset.GetOutputTracks().OfType<DOTSTrack>();
        }

        /// <summary>
        /// Gets the active time range of the timeline asset from start to duration.
        /// </summary>
        /// <param name="asset">The timeline asset.</param>
        /// <returns>An active range representing the full timeline duration.</returns>
        public static ActiveRange GetRange(this TimelineAsset asset)
        {
            return new ActiveRange
            {
                Start = DiscreteTime.Zero,
                End = new DiscreteTime(asset.duration),
            };
        }
    }
}
