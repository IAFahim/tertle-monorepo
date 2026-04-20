// <copyright file="AudioSourceOneShotResetSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Tests.Systems
{
    using BovineLabs.Bridge.Audio;
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Entities;

    public class AudioSourceOneShotResetSystemTests : ECSTestsFixture
    {
        private SystemHandle system;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<AudioSourceOneShotResetSystem>();
        }

        [Test]
        public void Update_OneShotEntity_ResetsAndDisablesIndex()
        {
            var oneShot = this.Manager.CreateEntity(typeof(AudioSourceOneShot), typeof(AudioSourceIndex));
            this.Manager.SetComponentData(oneShot, new AudioSourceIndex { PoolIndex = 12 });
            this.Manager.SetComponentEnabled<AudioSourceIndex>(oneShot, true);

            var looped = this.Manager.CreateEntity(typeof(AudioSourceIndex));
            this.Manager.SetComponentData(looped, new AudioSourceIndex { PoolIndex = 9 });
            this.Manager.SetComponentEnabled<AudioSourceIndex>(looped, true);

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var oneShotIndex = this.Manager.GetComponentData<AudioSourceIndex>(oneShot);
            Assert.AreEqual(-1, oneShotIndex.PoolIndex);
            Assert.IsFalse(this.Manager.IsComponentEnabled<AudioSourceIndex>(oneShot));

            var loopedIndex = this.Manager.GetComponentData<AudioSourceIndex>(looped);
            Assert.AreEqual(9, loopedIndex.PoolIndex);
            Assert.IsTrue(this.Manager.IsComponentEnabled<AudioSourceIndex>(looped));
        }
    }
}
#endif
