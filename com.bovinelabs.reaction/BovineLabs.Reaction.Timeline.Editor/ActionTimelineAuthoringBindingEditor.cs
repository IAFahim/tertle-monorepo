// <copyright file="ActionTimelineAuthoringBindingEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Timeline.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Reaction.Timeline.Authoring;
    using BovineLabs.Timeline.Authoring;
    using UnityEditor;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(ActionTimelineAuthoring.Data.Binding))]
    public class ActionTimelineAuthoringBindingEditor : ElementProperty
    {
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            switch (property.name)
            {
                case nameof(ActionTimelineAuthoring.Data.Binding.Track):

                    var trackField = CreatePropertyField(property, property.serializedObject);
                    trackField.SetEnabled(false);

                    // Multi selection we just show default field
                    if (property.serializedObject.targetObjects.Length != 1)
                    {
                        return trackField;
                    }

                    var ve = new VisualElement();

                    var options = GetTracksForProperty(property);
                    var names = options.Select(s => s.name).ToList();
                    var current = property.objectReferenceValue as DOTSTrack;
                    var defaultIndex = options.IndexOf(current);

                    var trackDropDown = new DropdownField(property.name, names, defaultIndex); // TODO default
                    trackDropDown.AddToClassList(DropdownField.alignedFieldUssClassName);

                    trackDropDown.RegisterValueChangedCallback(evt =>
                    {
                        var index = names.IndexOf(evt.newValue);
                        property.objectReferenceValue = options[index];
                        property.serializedObject.ApplyModifiedProperties();
                    });

                    ve.Add(trackDropDown);
                    ve.Add(trackField);

                    return ve;
            }

            return base.CreateElement(property);
        }

        /// <inheritdoc/>
        protected override string GetDisplayName(SerializedProperty property)
        {
            var track = property.FindPropertyRelative(nameof(ActionTimelineAuthoring.Data.Binding.Track)).objectReferenceValue;
            return track == null ? "Null" : track.name;
        }

        private static List<DOTSTrack> GetTracksForProperty(SerializedProperty property)
        {
            var timelineProperty = GetTimelineProperty(property);
            var tracks = new List<DOTSTrack>();

            if (timelineProperty != null)
            {
                var directorProperty = timelineProperty.FindPropertyRelative(nameof(ActionTimelineAuthoring.Data.Director));
                var director = directorProperty?.objectReferenceValue as PlayableDirector;
                GetTracksFromDirector(director, tracks);
            }

            return tracks;
        }

        private static SerializedProperty GetTimelineProperty(SerializedProperty property)
        {
            var path = property.propertyPath;
            var timelineSegment = $"{nameof(ActionTimelineAuthoring.Timelines)}.Array.data[";
            var index = path.IndexOf(timelineSegment, StringComparison.Ordinal);

            if (index < 0)
            {
                return null;
            }

            index += timelineSegment.Length;
            var end = path.IndexOf(']', index);

            if (end < 0)
            {
                return null;
            }

            var indexString = path.Substring(index, end - index);

            if (!int.TryParse(indexString, out var timelineIndex))
            {
                return null;
            }

            var timelinePath = $"{nameof(ActionTimelineAuthoring.Timelines)}.Array.data[{timelineIndex}]";
            return property.serializedObject.FindProperty(timelinePath);
        }

        private static void GetTracksFromDirector(PlayableDirector director, List<DOTSTrack> tracks)
        {
            if (director == null)
            {
                return;
            }

            var timeline = director.playableAsset as TimelineAsset;
            GetTracksFromTimeline(director, timeline, tracks);
        }

        private static void GetTracksFromTimeline(PlayableDirector director, TimelineAsset timeline, List<DOTSTrack> tracks)
        {
            if (timeline == null)
            {
                return;
            }

            var dotsTracks = timeline.GetDOTSTracks();

            foreach (var track in dotsTracks)
            {
                tracks.Add(track);

                if (track is SubDirectorTrack)
                {
                    foreach (var clip in track.GetClips())
                    {
                        if (clip.asset is SubDirectorClip subDirectorClip)
                        {
                            var subDirector = subDirectorClip.SubDirector.Resolve(director);
                            GetTracksFromDirector(subDirector, tracks);
                        }
                        else if (clip.asset is SubTimelineClip subTimelineClip)
                        {
                            GetTracksFromTimeline(director, subTimelineClip.Timeline, tracks);
                        }
                    }
                }
            }
        }
    }
}
