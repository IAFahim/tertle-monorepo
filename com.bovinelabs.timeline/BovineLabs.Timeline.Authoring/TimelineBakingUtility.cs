// <copyright file="TimelineBakingUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Authoring
{
    /// <summary>
    /// Utility methods for timeline baking operations.
    /// </summary>
    public static class TimelineBakingUtility
    {
        /// <summary>
        /// Converts a DOTS track into a unique string identifier used for track binding lookups.
        /// </summary>
        /// <param name="dotsTrack">The track to get an identifier for.</param>
        /// <returns>A unique string identifier for the track.</returns>
        public static string TrackToIdentifier(DOTSTrack dotsTrack)
        {
            return dotsTrack.name;
        }
    }
}
