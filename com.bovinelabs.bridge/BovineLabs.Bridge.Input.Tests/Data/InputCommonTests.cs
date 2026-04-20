// <copyright file="InputCommonTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Input.Tests.Data
{
    using BovineLabs.Bridge.Input;
    using NUnit.Framework;

    public class InputCommonTests
    {
        [TestCase(true, true, true)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(false, false, false)]
        public void InViewWithFocus_ReturnsCursorInViewportAndApplicationFocus(
            bool cursorInViewport,
            bool applicationFocus,
            bool expected)
        {
            var input = new InputCommon
            {
                CursorInViewPort = cursorInViewport,
                ApplicationFocus = applicationFocus,
            };

            Assert.AreEqual(expected, input.InViewWithFocus);
        }
    }
}
