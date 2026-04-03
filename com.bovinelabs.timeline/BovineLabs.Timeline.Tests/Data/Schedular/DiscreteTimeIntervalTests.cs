// <copyright file="DiscreteTimeIntervalTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Tests.Data.Schedular
{
    using BovineLabs.Timeline.Data.Schedular;
    using NUnit.Framework;
    using Unity.IntegerTime;

    public class DiscreteTimeIntervalTests
    {
        [Test]
        public void Constructor_NormalizesReversedEndpoints()
        {
            var interval = new DiscreteTimeInterval(Ticks(20), Ticks(10));

            Assert.AreEqual(Ticks(10), interval.Start);
            Assert.AreEqual(Ticks(20), interval.End);
        }

        [Test]
        public void Duration_ComputesEndMinusStart()
        {
            var interval = new DiscreteTimeInterval(Ticks(10), Ticks(35));

            Assert.AreEqual(Ticks(25), interval.Duration);
        }

        [Test]
        public void Duration_Overflow_ReturnsMaxValue()
        {
            Assert.AreEqual(DiscreteTime.MaxValue, DiscreteTimeInterval.MaxRange.Duration);
        }

        [Test]
        public void Contains_IsInclusiveAtBothBounds()
        {
            var interval = new DiscreteTimeInterval(Ticks(10), Ticks(20));

            Assert.IsTrue(interval.Contains(Ticks(10)));
            Assert.IsTrue(interval.Contains(Ticks(20)));
            Assert.IsFalse(interval.Contains(Ticks(9)));
            Assert.IsFalse(interval.Contains(Ticks(21)));
        }

        [Test]
        public void Overlaps_TrueForIntersectingOrEdgeTouching_FalseForDisjoint()
        {
            var range = new DiscreteTimeInterval(Ticks(10), Ticks(20));

            Assert.IsTrue(range.Overlaps(new DiscreteTimeInterval(Ticks(15), Ticks(25))));
            Assert.IsTrue(range.Overlaps(new DiscreteTimeInterval(Ticks(20), Ticks(30))));
            Assert.IsFalse(range.Overlaps(new DiscreteTimeInterval(Ticks(21), Ticks(30))));
        }

        [Test]
        public void Clamp_ReturnsBoundsWhenOutside_IdentityWhenInside()
        {
            var range = new DiscreteTimeInterval(Ticks(10), Ticks(20));

            Assert.AreEqual(Ticks(10), range.Clamp(Ticks(1)));
            Assert.AreEqual(Ticks(14), range.Clamp(Ticks(14)));
            Assert.AreEqual(Ticks(20), range.Clamp(Ticks(30)));
        }

        [Test]
        public void EqualsAndHashCode_AreConsistentForEqualRanges()
        {
            var a = new DiscreteTimeInterval(Ticks(10), Ticks(20));
            var b = new DiscreteTimeInterval(Ticks(10), Ticks(20));
            var c = new DiscreteTimeInterval(Ticks(11), Ticks(20));

            Assert.IsTrue(a.Equals(b));
            Assert.IsFalse(a.Equals(c));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        private static DiscreteTime Ticks(long ticks)
        {
            return DiscreteTime.FromTicks(ticks);
        }
    }
}
