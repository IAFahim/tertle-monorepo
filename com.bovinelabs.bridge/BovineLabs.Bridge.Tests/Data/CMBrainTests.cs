// <copyright file="CMBrainTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Tests.Data
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using NUnit.Framework;
    using Unity.Cinemachine;
    using UnityEngine;

    public class CMBrainTests
    {
        [Test]
        public void Constructor_ReadsRepresentativeValuesFromBrain()
        {
            var go = new GameObject("CMBrainTests", typeof(CinemachineBrain));

            try
            {
                var brain = go.GetComponent<CinemachineBrain>();
                brain.IgnoreTimeScale = true;
                brain.UpdateMethod = (CinemachineBrain.UpdateMethods)1;
                brain.BlendUpdateMethod = (CinemachineBrain.BrainUpdateMethods)1;
                brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.HardIn, 1.5f);

                var data = new CMBrain(brain);

                Assert.IsTrue(data.IgnoreTimeScale);
                Assert.AreEqual((CinemachineBrain.UpdateMethods)1, data.UpdateMethod);
                Assert.AreEqual((CinemachineBrain.BrainUpdateMethods)1, data.BlendUpdateMethod);
                Assert.AreEqual(CinemachineBlendDefinition.Styles.HardIn, data.DefaultBlend.Style);
                Assert.That(data.DefaultBlend.Time, Is.EqualTo(1.5f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
#endif
