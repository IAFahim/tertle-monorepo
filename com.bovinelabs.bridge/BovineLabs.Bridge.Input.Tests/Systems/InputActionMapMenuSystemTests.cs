// <copyright file="InputActionMapMenuSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Input.Tests.Systems
{
    using System.Text.RegularExpressions;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.TestTools;

    public class InputActionMapMenuSystemTests : ECSTestsFixture
    {
        private InputActionMapMenuSystem system;
        private InputCommonSettingsTestScope settingsScope;

        public override void Setup()
        {
            base.Setup();
            this.settingsScope = new InputCommonSettingsTestScope();
            this.system = this.World.CreateSystemManaged<InputActionMapMenuSystem>();
        }

        public override void TearDown()
        {
            this.settingsScope.Dispose();
            base.TearDown();
        }

        [Test]
        public void OnStartRunning_WithMissingAsset_DisablesSystem()
        {
            this.settingsScope.Set(null);

            LogAssert.Expect(LogType.Error, new Regex("Input asset not setup"));
            this.system.Update();
            Assert.IsFalse(this.system.Enabled);
        }

        [Test]
        public void OnStartRunning_WithMenuDefaults_EnablesMappedAction()
        {
            InputActionAsset inputAsset = null;
            try
            {
                inputAsset = ScriptableObject.CreateInstance<InputActionAsset>();
                inputAsset.AddActionMap("Menu").AddAction("Open");
                this.settingsScope.Set(inputAsset, menuEnabled: new[] { "Menu" });

                this.system.Update();
                Assert.IsTrue(this.system.Enabled);
                Assert.IsTrue(inputAsset.FindActionMap("Menu").enabled);
            }
            finally
            {
                if (inputAsset != null)
                {
                    Object.DestroyImmediate(inputAsset);
                }
            }
        }
    }
}
