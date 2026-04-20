// <copyright file="AudioSourcePrioritySortJobTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Tests.Audio
{
    using BovineLabs.Bridge.Audio;
    using BovineLabs.Bridge.Data.Audio;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;

    public class AudioSourcePrioritySortJobTests
    {
        [Test]
        public void Execute_EmptyClosest_DoesNothing()
        {
            var entities = new NativeArray<Entity>(1, Allocator.Temp);
            var worlds = new NativeArray<LocalToWorld>(1, Allocator.Temp);
            var ranges = new NativeArray<AudioSourceAudibleRange>(1, Allocator.Temp);
            var data = new NativeArray<AudioSourceDataExtended>(1, Allocator.Temp);
            var closest = new NativeArray<Entity>(0, Allocator.Temp);
            var listenerPosition = new NativeReference<float3>(Allocator.Temp);

            var job = new AudioSourcePrioritySortJob
            {
                Entities = entities,
                LocalToWorlds = worlds,
                AudibleRanges = ranges,
                AudioSourceDataExtended = data,
                Closests = closest,
                ListenerPosition = listenerPosition,
            };

            Assert.DoesNotThrow(() => job.Execute());
        }

        [Test]
        public void Execute_ExcludesOutOfRangeSources()
        {
            using var world = new World("AudioSourcePrioritySortJobTests");
            var manager = world.EntityManager;

            var entities = new NativeArray<Entity>(1, Allocator.Temp);
            var localToWorlds = new NativeArray<LocalToWorld>(1, Allocator.Temp);
            var ranges = new NativeArray<AudioSourceAudibleRange>(1, Allocator.Temp);
            var data = new NativeArray<AudioSourceDataExtended>(1, Allocator.Temp);
            var closest = new NativeArray<Entity>(1, Allocator.Temp);
            var listenerPosition = new NativeReference<float3>(Allocator.Temp);

            entities[0] = manager.CreateEntity();
            localToWorlds[0] = CreateLocalToWorld(new float3(10f, 0f, 0f));
            ranges[0] = new AudioSourceAudibleRange { MaxDistance = 1f };
            data[0] = new AudioSourceDataExtended { Priority = 100 };

            var job = new AudioSourcePrioritySortJob
            {
                Entities = entities,
                LocalToWorlds = localToWorlds,
                AudibleRanges = ranges,
                AudioSourceDataExtended = data,
                Closests = closest,
                ListenerPosition = listenerPosition,
            };

            job.Execute();

            Assert.AreEqual(Entity.Null, closest[0]);
        }

        [Test]
        public void Execute_SortsByPriorityThenDistance()
        {
            using var world = new World("AudioSourcePrioritySortJobTests");
            var manager = world.EntityManager;

            var e0 = manager.CreateEntity();
            var e1 = manager.CreateEntity();
            var e2 = manager.CreateEntity();

            var entities = new NativeArray<Entity>(3, Allocator.Temp);
            var localToWorlds = new NativeArray<LocalToWorld>(3, Allocator.Temp);
            var ranges = new NativeArray<AudioSourceAudibleRange>(3, Allocator.Temp);
            var data = new NativeArray<AudioSourceDataExtended>(3, Allocator.Temp);
            var closest = new NativeArray<Entity>(2, Allocator.Temp);
            var listenerPosition = new NativeReference<float3>(Allocator.Temp);

            entities[0] = e0;
            entities[1] = e1;
            entities[2] = e2;
            localToWorlds[0] = CreateLocalToWorld(new float3(1f, 0f, 0f));
            localToWorlds[1] = CreateLocalToWorld(new float3(0.1f, 0f, 0f));
            localToWorlds[2] = CreateLocalToWorld(new float3(0.2f, 0f, 0f));
            ranges[0] = new AudioSourceAudibleRange { MaxDistance = 10f };
            ranges[1] = new AudioSourceAudibleRange { MaxDistance = 10f };
            ranges[2] = new AudioSourceAudibleRange { MaxDistance = 10f };
            data[0] = new AudioSourceDataExtended { Priority = 5 };
            data[1] = new AudioSourceDataExtended { Priority = 5 };
            data[2] = new AudioSourceDataExtended { Priority = 10 };

            var job = new AudioSourcePrioritySortJob
            {
                Entities = entities,
                LocalToWorlds = localToWorlds,
                AudibleRanges = ranges,
                AudioSourceDataExtended = data,
                Closests = closest,
                ListenerPosition = listenerPosition,
            };

            job.Execute();

            Assert.AreEqual(e2, closest[0], "Higher priority source should be selected first");
            Assert.AreEqual(e1, closest[1], "At equal priority, nearest source should be selected");
        }

        private static LocalToWorld CreateLocalToWorld(float3 position)
        {
            return new LocalToWorld { Value = float4x4.Translate(position) };
        }
    }
}
#endif
