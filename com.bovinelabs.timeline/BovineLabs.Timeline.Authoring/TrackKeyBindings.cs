// <copyright file="TrackKeyBindings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Authoring
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine.Timeline;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Serializable container for track-to-target bindings in sub-timelines.
    /// Used to store and manage which Unity objects are bound to which tracks.
    /// </summary>
    [Serializable]
    public struct TrackKeyBindings
    {
        /// <summary> The list of track-to-target bindings. </summary>
        public List<TrackKeyPair> Bindings;

        /// <summary>
        /// Synchronizes the binding list with the given timeline asset's tracks.
        /// Adds new tracks and removes tracks that no longer exist in the timeline.
        /// </summary>
        /// <param name="timeline">The timeline asset to synchronize with.</param>
        public void SyncToTimeline(TimelineAsset timeline)
        {
            this.Bindings ??= new List<TrackKeyPair>();

            if (timeline == null)
            {
                return;
            }

            var list = this.Bindings;

            var outputs = timeline.outputs.ToList();
            foreach (var output in outputs)
            {
                var track = output.sourceObject as TrackAsset;
                if (output.outputTargetType == null || track == null)
                {
                    continue;
                }

                if (list.FindIndex(x => x.Track == track) == -1)
                {
                    list.Add(new TrackKeyPair { Track = track });
                }
            }

            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Track == null || outputs.FindIndex(o => o.sourceObject == list[i].Track) == -1)
                {
                    list.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Finds the target object bound to the specified track.
        /// </summary>
        /// <param name="asset">The track asset to find the binding for.</param>
        /// <returns>The bound object, or null if no binding exists for the track.</returns>
        public Object? FindObject(TrackAsset? asset)
        {
            if (asset == null || this.Bindings == null)
            {
                return null;
            }

            var index = this.Bindings.FindIndex(x => x.Track == asset);
            return index >= 0 ? this.Bindings[index].Target : null;
        }

        /// <summary>
        /// Represents a binding between a timeline track and its target object.
        /// </summary>
        [Serializable]
        public struct TrackKeyPair
        {
            /// <summary> The timeline track asset. </summary>
            public TrackAsset Track;

            /// <summary> The Unity object bound to this track (typically a GameObject or Component). </summary>
            public Object Target;
        }
    }
}
