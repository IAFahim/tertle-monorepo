// <copyright file="AudioSourceStateSystemTests.cs" company="BovineLabs">
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

    public class AudioSourceStateSystemTests : ECSTestsFixture
    {
        private SystemHandle system;

        public override void Setup()
        {
            base.Setup();
            this.system = this.World.CreateSystem<AudioSourceStateSystem>();
        }

        [Test]
        public void Update_CopiesEnabledStateToPrevious_WhenEnabled()
        {
            var entity = this.Manager.CreateEntity(typeof(AudioSourceEnabled), typeof(AudioSourceEnabledPrevious));
            this.Manager.SetComponentEnabled<AudioSourceEnabled>(entity, true);
            this.Manager.SetComponentEnabled<AudioSourceEnabledPrevious>(entity, false);

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.IsTrue(this.Manager.IsComponentEnabled<AudioSourceEnabledPrevious>(entity));
        }

        [Test]
        public void Update_CopiesEnabledStateToPrevious_WhenDisabled()
        {
            var entity = this.Manager.CreateEntity(typeof(AudioSourceEnabled), typeof(AudioSourceEnabledPrevious));
            this.Manager.SetComponentEnabled<AudioSourceEnabled>(entity, false);
            this.Manager.SetComponentEnabled<AudioSourceEnabledPrevious>(entity, true);

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.IsFalse(this.Manager.IsComponentEnabled<AudioSourceEnabledPrevious>(entity));
        }
    }
}
#endif
