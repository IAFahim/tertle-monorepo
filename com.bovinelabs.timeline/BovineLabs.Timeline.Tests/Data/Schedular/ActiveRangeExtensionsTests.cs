// <copyright file="ActiveRangeExtensionsTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Tests.Data.Schedular
{
    using BovineLabs.Timeline.Data.Schedular;
    using NUnit.Framework;
    using Unity.IntegerTime;

    public class ActiveRangeExtensionsTests
    {
        [Test]
        public void IsValid_OnlyWhenStartIsStrictlyLessThanEnd()
        {
            Assert.IsTrue(new ActiveRange { Start = Ticks(1), End = Ticks(2) }.IsValid());
            Assert.IsFalse(new ActiveRange { Start = Ticks(1), End = Ticks(1) }.IsValid());
            Assert.IsFalse(new ActiveRange { Start = Ticks(2), End = Ticks(1) }.IsValid());
        }

        [Test]
        public void Overlaps_ReturnsFalseIfEitherRangeInvalid()
        {
            var valid = new ActiveRange { Start = Ticks(1), End = Ticks(3) };
            var invalid = new ActiveRange { Start = Ticks(3), End = Ticks(3) };

            Assert.IsFalse(valid.Overlaps(invalid));
            Assert.IsFalse(invalid.Overlaps(valid));
        }

        [Test]
        public void Overlaps_HandlesTypicalRangeCombinations()
        {
            var a = new ActiveRange { Start = Ticks(10), End = Ticks(20) };

            Assert.IsTrue(a.Overlaps(new ActiveRange { Start = Ticks(15), End = Ticks(25) }));
            Assert.IsFalse(a.Overlaps(new ActiveRange { Start = Ticks(20), End = Ticks(30) }));
        }

        [Test]
        public void ContainsTime_IsStartInclusiveAndEndExclusive()
        {
            var range = new ActiveRange { Start = Ticks(10), End = Ticks(20) };

            Assert.IsTrue(range.Contains(Ticks(10)));
            Assert.IsTrue(range.Contains(Ticks(19)));
            Assert.IsFalse(range.Contains(Ticks(20)));
        }

        [Test]
        public void ContainsRange_RequiresFullContainment()
        {
            var range = new ActiveRange { Start = Ticks(10), End = Ticks(20) };

            Assert.IsTrue(range.Contains(new ActiveRange { Start = Ticks(10), End = Ticks(20) }));
            Assert.IsTrue(range.Contains(new ActiveRange { Start = Ticks(12), End = Ticks(18) }));
            Assert.IsFalse(range.Contains(new ActiveRange { Start = Ticks(9), End = Ticks(18) }));
            Assert.IsFalse(range.Contains(new ActiveRange { Start = Ticks(12), End = Ticks(21) }));
        }

        [Test]
        public void Length_EqualsEndMinusStart()
        {
            var range = new ActiveRange { Start = Ticks(5), End = Ticks(18) };

            Assert.AreEqual(Ticks(13), range.Length());
        }

        [Test]
        public void CompleteRange_UsesDiscreteTimeMinAndMax()
        {
            Assert.AreEqual(DiscreteTime.MinValue, ActiveRange.CompleteRange.Start);
            Assert.AreEqual(DiscreteTime.MaxValue, ActiveRange.CompleteRange.End);
        }

        private static DiscreteTime Ticks(long ticks)
        {
            return DiscreteTime.FromTicks(ticks);
        }
    }
}
