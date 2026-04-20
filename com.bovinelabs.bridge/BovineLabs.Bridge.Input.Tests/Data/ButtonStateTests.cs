// <copyright file="ButtonStateTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Input.Tests.Data
{
    using BovineLabs.Bridge.Input;
    using NUnit.Framework;

    public class ButtonStateTests
    {
        [Test]
        public void Started_SetsDownAndPressed()
        {
            var state = default(ButtonState);

            state.Started();

            Assert.IsTrue(state.Down);
            Assert.IsTrue(state.Pressed);
            Assert.IsFalse(state.Up);
        }

        [Test]
        public void Cancelled_SetsPressedFalseAndUpTrue()
        {
            var state = default(ButtonState);
            state.Started();

            state.Cancelled();

            Assert.IsFalse(state.Pressed);
            Assert.IsTrue(state.Up);
        }

        [Test]
        public void Reset_ClearsDownAndUp_WithoutChangingPressed()
        {
            var state = default(ButtonState);
            state.Started();
            state.Cancelled();

            state.Reset();

            Assert.IsFalse(state.Down);
            Assert.IsFalse(state.Up);
            Assert.IsFalse(state.Pressed);
        }
    }
}
