// <copyright file="CMBlendDefinitionTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE
namespace BovineLabs.Bridge.Tests.Data
{
    using BovineLabs.Bridge.Data.Cinemachine;
    using NUnit.Framework;
    using Unity.Cinemachine;

    public class CMBlendDefinitionTests
    {
        [Test]
        public void ImplicitConversions_RoundTripStyleAndTime()
        {
            var source = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1.75f);

            CMBlendDefinition bridge = source;
            CinemachineBlendDefinition roundTrip = bridge;

            Assert.AreEqual(source.Style, roundTrip.Style);
            Assert.That(roundTrip.Time, Is.EqualTo(source.Time).Within(0.0001f));
        }
    }
}
#endif
