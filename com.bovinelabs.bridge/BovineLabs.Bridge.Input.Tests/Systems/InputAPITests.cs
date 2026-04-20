// <copyright file="InputAPITests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Input.Tests.Systems
{
    using BovineLabs.Core;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;

    public partial class InputAPITests : ECSTestsFixture
    {
        private const byte CommandEnable = 1;
        private const byte CommandDisable = 2;

        private SystemHandle system;
        private Entity commandEntity;
        private Entity inputBufferEntity;

        public override void Setup()
        {
            base.Setup();

            this.system = this.World.CreateSystem<InputApiProxySystem>();
            this.inputBufferEntity = this.Manager.CreateEntity(typeof(InputActionMapEnable));
            this.commandEntity = this.Manager.CreateEntity(typeof(InputApiCommand));
        }

        [Test]
        public void InputEnable_AppendsEnabledBufferEntry()
        {
            this.ExecuteCommand("Gameplay", CommandEnable);

            var buffer = this.Manager.GetBuffer<InputActionMapEnable>(this.inputBufferEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual("Gameplay", buffer[0].Input.ToString());
            Assert.IsTrue(buffer[0].Enable);
        }

        [Test]
        public void InputDisable_AppendsDisabledBufferEntry()
        {
            this.ExecuteCommand("Menu", CommandDisable);

            var buffer = this.Manager.GetBuffer<InputActionMapEnable>(this.inputBufferEntity);
            Assert.AreEqual(1, buffer.Length);
            Assert.AreEqual("Menu", buffer[0].Input.ToString());
            Assert.IsFalse(buffer[0].Enable);
        }

        [Test]
        public void MultipleCommands_AppendsInCallOrder()
        {
            this.ExecuteCommand("Gameplay", CommandEnable);
            this.ExecuteCommand("Menu", CommandDisable);

            var buffer = this.Manager.GetBuffer<InputActionMapEnable>(this.inputBufferEntity);
            Assert.AreEqual(2, buffer.Length);

            Assert.AreEqual("Gameplay", buffer[0].Input.ToString());
            Assert.IsTrue(buffer[0].Enable);

            Assert.AreEqual("Menu", buffer[1].Input.ToString());
            Assert.IsFalse(buffer[1].Enable);
        }

        private void ExecuteCommand(FixedString32Bytes input, byte command)
        {
            this.Manager.SetComponentData(this.commandEntity, new InputApiCommand
            {
                Input = input,
                Command = command,
            });

            this.system.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();
        }

        private struct InputApiCommand : IComponentData
        {
            public FixedString32Bytes Input;
            public byte Command;
        }

        private partial struct InputApiProxySystem : ISystem
        {
            public void OnUpdate(ref SystemState state)
            {
                var command = SystemAPI.GetSingleton<InputApiCommand>();

                if (command.Command == CommandEnable)
                {
                    InputAPI.InputEnable(ref state, command.Input);
                }
                else if (command.Command == CommandDisable)
                {
                    InputAPI.InputDisable(ref state, command.Input);
                }

                SystemAPI.SetSingleton(new InputApiCommand
                {
                    Input = command.Input,
                    Command = 0,
                });
            }
        }
    }
}
