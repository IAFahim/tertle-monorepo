# Authoring Tracks and Clips

This document covers custom clip/track authoring and how to drive runtime systems.

## Create a Clip
Derive from DOTSClip and add clip data during Bake.

```csharp
using System;
using BovineLabs.Timeline.Authoring;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public class MyCustomClip : DOTSClip
{
    public float value;
    public float3 offset;

    public override void Bake(Entity clipEntity, BakingContext context)
    {
        context.Baker.AddComponent(clipEntity, new MyClipData
        {
            Value = this.value,
            Offset = this.offset,
        });
    }
}
```

## Create a Track
Derive from DOTSTrack and add track or binding data during Bake.

```csharp
using System;
using BovineLabs.Timeline.Authoring;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Timeline;

[Serializable]
[TrackClipType(typeof(MyCustomClip))]
[TrackBindingType(typeof(Transform))]
public class MyCustomTrack : DOTSTrack
{
    protected override void Bake(BakingContext context)
    {
        if (context.Binding == null || context.Binding.Target == Entity.Null)
        {
            return;
        }

        // Track-level data goes on the track entity.
        context.Baker.AddComponent(context.TrackEntity, new MyTrackTag());

        // Target-level data goes on the bound entity.
        context.Baker.AddComponent(context.Binding.Target, new MyAnimatedComponent());
    }
}
```

Notes:
- context.TrackEntity is created for you by DOTSTrack.BakeTrack.
- context.Binding.Target is the ECS entity bound to the Unity track binding.
- If you need extra entities for helper data, use context.CreateEntity and add the components you need.

## Track Systems and Blending
Schedule your systems in TimelineComponentAnimationGroup and use TrackBlendImpl to accumulate clip values.

```csharp
using BovineLabs.Core.Jobs;
using BovineLabs.Timeline;
using BovineLabs.Timeline.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(TimelineComponentAnimationGroup))]
public partial struct MyCustomTrackSystem : ISystem
{
    private TrackBlendImpl<float3, MyAnimatedComponent> blend;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        this.blend.OnCreate(ref state);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        this.blend.OnDestroy(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var blendData = this.blend.Update(ref state);
        state.Dependency = new WriteJob
        {
            BlendData = blendData,
            LocalTransforms = SystemAPI.GetComponentLookup<LocalTransform>(),
        }.ScheduleParallel(blendData, 64, state.Dependency);
    }

    [BurstCompile]
    private struct WriteJob : IJobParallelHashMapDefer
    {
        [ReadOnly]
        public NativeParallelHashMap<Entity, MixData<float3>>.ReadOnly BlendData;

        public ComponentLookup<LocalTransform> LocalTransforms;

        public void ExecuteNext(int entryIndex, int jobIndex)
        {
            this.Read(this.BlendData, entryIndex, out var entity, out var mix);

            var localTransform = this.LocalTransforms.GetRefRWOptional(entity);
            if (!localTransform.IsValid)
            {
                return;
            }

            localTransform.ValueRW.Position = JobHelpers.Blend<float3, Float3Mixer>(ref mix, localTransform.ValueRO.Position);
        }
    }
}
```

## Animated Components and Mixers
TrackBlendImpl expects components that implement IAnimatedComponent<T> so it can read the current value.
Use the built-in mixers for common types: FloatMixer, Float2Mixer, Float3Mixer, Float4Mixer, QuaternionMixer.

```csharp
using BovineLabs.Timeline.Data;
using Unity.Entities;
using Unity.Mathematics;

public struct MyAnimatedComponent : IComponentData, IAnimatedComponent<float3>
{
    public float3 Value { get; set; }
}
```

## Reset on Deactivate
DOTSTrack exposes a Reset On Deactivate toggle (enabled by default and forced on in the editor world). When enabled, TrackResetOnDeactivate is added to the track entity. Use TimelineActive/TimelineActivePrevious or ClipActive/ClipActivePrevious to capture defaults and restore state when a track stops.
