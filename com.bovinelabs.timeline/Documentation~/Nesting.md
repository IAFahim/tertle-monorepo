# Nested Timelines

Nested timelines are supported via SubDirectorTrack, SubDirectorClip, and SubTimelineClip. Nested playback is implemented using composite timers.

## SubDirectorTrack and SubDirectorClip
- SubDirectorClip references a PlayableDirector via an exposed reference.
- The referenced director's TimelineAsset is baked into a composite timer.
- The clip uses a TimeSyncBehaviour to keep the Timeline window time in sync during preview.

## SubTimelineClip
SubTimelineClip embeds a TimelineAsset directly without a PlayableDirector.
It includes TrackKeyBindings to map tracks in the nested asset to target objects.

TrackKeyBindings.SyncToTimeline is called during OnValidate to keep the binding list aligned with the asset.

## Setup Steps
1) Add a SubDirectorTrack to your main timeline.
2) Add SubDirectorClip or SubTimelineClip and assign the asset or director.
3) For SubTimelineClip, assign track bindings for each nested track target.
