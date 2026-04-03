// <copyright file="TimeTransformTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Tests.Data.Timeline
{
    using BovineLabs.Timeline.Data;
    using NUnit.Framework;
    using Unity.IntegerTime;

    public class TimeTransformTests
    {
        [Test]
        public void ToLocalTimeUnbound_UsesStartScaleAndClipIn()
        {
            var transform = new TimeTransform
            {
                Start = Ticks(10),
                End = Ticks(30),
                ClipIn = Ticks(3),
                Scale = 2.0,
            };

            var localTime = transform.ToLocalTimeUnbound(Ticks(14));

            Assert.AreEqual(Ticks(11), localTime);
        }

        [Test]
        public void IsLocalTimeBounded_IsTrueInsideClosedRange()
        {
            var transform = new TimeTransform
            {
                Start = Ticks(10),
                End = Ticks(20),
                Scale = 1.0,
            };

            Assert.IsFalse(transform.IsLocalTimeBounded(Ticks(-1)));
            Assert.IsTrue(transform.IsLocalTimeBounded(Ticks(0)));
            Assert.IsTrue(transform.IsLocalTimeBounded(Ticks(10)));
            Assert.IsFalse(transform.IsLocalTimeBounded(Ticks(11)));
        }

        [Test]
        public void EqualityOperators_ReflectFieldEquality()
        {
            var a = new TimeTransform
            {
                Start = Ticks(1),
                End = Ticks(2),
                ClipIn = Ticks(3),
                Scale = 4.0,
            };

            var b = a;
            var c = a;
            c.Scale = 5.0;

            Assert.IsTrue(a == b);
            Assert.IsTrue(a != c);
        }

        [Test]
        public void EqualsObjectAndTypedEquals_Match()
        {
            var a = new TimeTransform
            {
                Start = Ticks(1),
                End = Ticks(2),
                ClipIn = Ticks(3),
                Scale = 4.0,
            };

            object boxed = a;
            var different = a;
            different.End = Ticks(8);

            Assert.IsTrue(a.Equals(boxed));
            Assert.IsTrue(a.Equals(a));
            Assert.IsFalse(a.Equals(different));
        }

        [Test]
        public void GetHashCode_IsStableForEqualValues()
        {
            var a = new TimeTransform
            {
                Start = Ticks(10),
                End = Ticks(20),
                ClipIn = Ticks(5),
                Scale = 2.0,
            };

            var b = a;

            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        private static DiscreteTime Ticks(long ticks)
        {
            return DiscreteTime.FromTicks(ticks);
        }
    }
}
