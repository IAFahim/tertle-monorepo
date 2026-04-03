// <copyright file="DetourMathTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast.Tests
{
    using NUnit.Framework;
    using Unity.Mathematics;

    public unsafe class DetourMathTests
    {
        [TestCase(0f, 1f, 0f, 1f)]
        [TestCase(0.5f, 1f, 0.5f, 0.5f)]
        [TestCase(1f, 1f, 1f, 0f)]
        public void RandomPointInConvexPoly_SelectsExpectedPoint(float s, float t, float expectedX, float expectedZ)
        {
            var pts = stackalloc float3[3];
            pts[0] = new float3(0, 0, 0);
            pts[1] = new float3(0, 0, 1);
            pts[2] = new float3(1, 0, 0);

            var areas = stackalloc float[6];

            var result = Detour.RandomPointInConvexPoly(pts, 3, areas, s, t);

            Assert.AreEqual(expectedX, result.x, 1e-6f);
            Assert.AreEqual(0f, result.y, 1e-6f);
            Assert.AreEqual(expectedZ, result.z, 1e-6f);
        }
    }
}
