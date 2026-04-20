// <copyright file="AudioSourcePrioritySortJob.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Audio
{
    using System;
    using BovineLabs.Bridge.Data.Audio;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Unity.Transforms;

    [BurstCompile]
    internal unsafe struct AudioSourcePrioritySortJob : IJob
    {
        [ReadOnly]
        public NativeArray<Entity> Entities;

        [ReadOnly]
        public NativeArray<LocalToWorld> LocalToWorlds;

        [ReadOnly]
        public NativeArray<AudioSourceAudibleRange> AudibleRanges;

        [ReadOnly]
        public NativeArray<AudioSourceDataExtended> AudioSourceDataExtended;

        [ReadOnly]
        public NativeReference<float3> ListenerPosition;

        public NativeArray<Entity> Closests;

        /// <inheritdoc/>
        public void Execute()
        {
            if (this.Closests.Length == 0)
            {
                return;
            }

            for (var i = 0; i < this.Closests.Length; i++)
            {
                this.Closests[i] = Entity.Null;
            }

            Span<float> distances = stackalloc float[this.Closests.Length];
            distances.Fill(float.PositiveInfinity);

            Span<int> priorities = stackalloc int[this.Closests.Length];
            priorities.Fill(int.MinValue);

            for (var index = 0; index < this.Entities.Length; index++)
            {
                var distanceSq = math.distancesq(this.ListenerPosition.Value, this.LocalToWorlds[index].Position);
                var maxDistance = this.AudibleRanges[index].MaxDistance;
                var maxDistanceSq = maxDistance * maxDistance;
                if (distanceSq >= maxDistanceSq)
                {
                    continue;
                }

                var priority = this.AudioSourceDataExtended[index].Priority;

                for (var k = 0; k < this.Closests.Length; k++)
                {
                    if (priority < priorities[k])
                    {
                        continue;
                    }

                    if (priority == priorities[k] && distanceSq >= distances[k])
                    {
                        continue;
                    }

                    var itemsToMove = this.Closests.Length - 1 - k;
                    if (itemsToMove > 0)
                    {
                        var neighbourPtr = (Entity*)this.Closests.GetUnsafePtr();
                        UnsafeUtility.MemMove(neighbourPtr + k + 1, neighbourPtr + k, itemsToMove * sizeof(Entity));

                        var distancePtr = (float*)UnsafeUtility.AddressOf(ref distances.GetPinnableReference());
                        UnsafeUtility.MemMove(distancePtr + k + 1, distancePtr + k, itemsToMove * sizeof(float));

                        var priorityPtr = (int*)UnsafeUtility.AddressOf(ref priorities.GetPinnableReference());
                        UnsafeUtility.MemMove(priorityPtr + k + 1, priorityPtr + k, itemsToMove * sizeof(int));
                    }

                    this.Closests[k] = this.Entities[index];
                    distances[k] = distanceSq;
                    priorities[k] = priority;

                    break;
                }
            }
        }
    }
}
#endif
