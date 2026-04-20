// <copyright file="DefaultInputSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Bridge.Input.Tests.Systems
{
    using System.Text.RegularExpressions;
    using BovineLabs.Bridge.Data.Camera;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Transforms;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.TestTools;

    public class DefaultInputSystemTests : ECSTestsFixture
    {
        private DefaultInputSystem system;
        private InputCommonSettingsTestScope settingsScope;

        public override void Setup()
        {
            base.Setup();

            this.settingsScope = new InputCommonSettingsTestScope();
            this.settingsScope.Set(null);
            this.system = this.World.CreateSystemManaged<DefaultInputSystem>();
        }

        public override void TearDown()
        {
            this.settingsScope.Dispose();
            base.TearDown();
        }

        [Test]
        public void OnCreate_CreatesInputCommonSingleton_WithOffscreenCursor()
        {
            Assert.AreEqual(1, this.Manager.CreateEntityQuery(typeof(InputCommon)).CalculateEntityCount());

            InputActionAsset inputAsset = null;
            InputActionReference cursorActionReference = null;
            Camera camera = null;

            try
            {
                inputAsset = ScriptableObject.CreateInstance<InputActionAsset>();
                var map = inputAsset.AddActionMap("Tests");
                var cursorAction = map.AddAction("CursorPosition", InputActionType.Value);
                cursorActionReference = InputActionReference.Create(cursorAction);
                this.settingsScope.Set(asset: null, cursorPosition: cursorActionReference);

                camera = new GameObject("DefaultInputSystemTests_MainCamera_Offscreen").AddComponent<Camera>();
                var cameraEntity = this.Manager.CreateEntity(typeof(CameraMain), typeof(CameraBridge), typeof(LocalTransform));
                this.Manager.SetComponentData(cameraEntity, new CameraBridge { Value = camera });
                this.Manager.SetComponentData(cameraEntity, LocalTransform.Identity);

                this.system.Update();

                var query = this.Manager.CreateEntityQuery(typeof(InputCommon));
                var inputCommon = query.GetSingleton<InputCommon>();
                Assert.AreEqual(-1f, inputCommon.CursorScreenPoint.x);
                Assert.AreEqual(-1f, inputCommon.CursorScreenPoint.y);
            }
            finally
            {
                if (cursorActionReference != null)
                {
                    Object.DestroyImmediate(cursorActionReference);
                }

                if (inputAsset != null)
                {
                    Object.DestroyImmediate(inputAsset);
                }

                if (camera != null)
                {
                    Object.DestroyImmediate(camera.gameObject);
                }
            }
        }

        [Test]
        public void Update_WithNullCursorAction_LogsErrorAndWritesSingleton()
        {
            this.settingsScope.Set(null);
            var query = this.Manager.CreateEntityQuery(typeof(InputCommon));
            Camera camera = null;

            try
            {
                camera = new GameObject("DefaultInputSystemTests_MainCamera").AddComponent<Camera>();

                var cameraEntity = this.Manager.CreateEntity(typeof(CameraMain), typeof(CameraBridge), typeof(LocalTransform));
                this.Manager.SetComponentData(cameraEntity, new CameraBridge { Value = camera });
                this.Manager.SetComponentData(cameraEntity, LocalTransform.Identity);

                LogAssert.Expect(LogType.Error, new Regex("Input CursorPosition not setup"));
                this.system.Update();

                var inputCommon = query.GetSingleton<InputCommon>();
                Assert.GreaterOrEqual(inputCommon.ScreenSize.x, 0);
                Assert.GreaterOrEqual(inputCommon.ScreenSize.y, 0);
                Assert.IsFalse(inputCommon.AnyButtonPress);
            }
            finally
            {
                if (camera != null)
                {
                    Object.DestroyImmediate(camera.gameObject);
                }
            }
        }
    }
}
