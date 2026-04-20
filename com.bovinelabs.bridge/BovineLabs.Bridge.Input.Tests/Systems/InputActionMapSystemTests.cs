// <copyright file="InputActionMapSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Input.Tests.Systems
{
    using System.Text.RegularExpressions;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.TestTools;

    public class InputActionMapSystemTests : ECSTestsFixture
    {
        private InputActionMapSystem system;
        private InputActionAsset inputAsset;
        private InputCommonSettingsTestScope settingsScope;

        public override void Setup()
        {
            base.Setup();

            this.settingsScope = new InputCommonSettingsTestScope();

            this.inputAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            this.inputAsset.AddActionMap("Gameplay").AddAction("Move");
            this.inputAsset.AddActionMap("Menu").AddAction("Open");
            this.settingsScope.Set(this.inputAsset);

            this.system = this.World.CreateSystemManaged<InputActionMapSystem>();
        }

        public override void TearDown()
        {
            this.settingsScope.Dispose();
            Object.DestroyImmediate(this.inputAsset);
            base.TearDown();
        }

        [Test]
        public void OnCreate_CreatesSingletonBufferEntity()
        {
            Assert.AreEqual(1, this.Manager.CreateEntityQuery(typeof(InputActionMapEnable)).CalculateEntityCount());
        }

        [Test]
        public void Update_EnableEntry_EnablesTargetActionMap()
        {
            this.system.Update();

            var query = this.Manager.CreateEntityQuery(typeof(InputActionMapEnable));
            var entity = query.GetSingletonEntity();
            var buffer = this.Manager.GetBuffer<InputActionMapEnable>(entity);
            buffer.Add(new InputActionMapEnable { Input = new FixedString32Bytes("Gameplay"), Enable = true });

            this.system.Update();

            Assert.AreEqual(0, buffer.Length);
            Assert.IsTrue(this.inputAsset.FindActionMap("Gameplay").enabled);
        }

        [Test]
        public void Update_DisableEntry_DisablesTargetActionMap()
        {
            this.system.Update();
            this.inputAsset.FindActionMap("Gameplay").Enable();

            var query = this.Manager.CreateEntityQuery(typeof(InputActionMapEnable));
            var entity = query.GetSingletonEntity();
            var buffer = this.Manager.GetBuffer<InputActionMapEnable>(entity);
            buffer.Add(new InputActionMapEnable { Input = new FixedString32Bytes("Gameplay"), Enable = false });

            this.system.Update();

            Assert.AreEqual(0, buffer.Length);
            Assert.IsFalse(this.inputAsset.FindActionMap("Gameplay").enabled);
        }

        [Test]
        public void Update_WithMissingMapName_LogsWarningAndClearsBuffer()
        {
            this.system.Update();

            var query = this.Manager.CreateEntityQuery(typeof(InputActionMapEnable));
            var entity = query.GetSingletonEntity();
            var buffer = this.Manager.GetBuffer<InputActionMapEnable>(entity);
            buffer.Add(new InputActionMapEnable { Input = new FixedString32Bytes("Missing"), Enable = true });

            LogAssert.Expect(LogType.Warning, new Regex("Unable to find action map of name Missing"));
            this.system.Update();

            Assert.AreEqual(0, buffer.Length);
        }

        [Test]
        public void OnStartRunning_EnablesOnlyDefaultMaps()
        {
            this.settingsScope.Set(this.inputAsset, defaultEnabled: new[] { "Menu" });

            this.inputAsset.FindActionMap("Gameplay").Enable();
            this.inputAsset.FindActionMap("Menu").Disable();

            this.system.Update();

            Assert.IsFalse(this.inputAsset.FindActionMap("Gameplay").enabled);
            Assert.IsTrue(this.inputAsset.FindActionMap("Menu").enabled);
        }
    }
}
