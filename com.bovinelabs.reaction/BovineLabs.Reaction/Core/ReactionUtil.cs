// <copyright file="ReactionUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Core
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Collections;
    using BovineLabs.Reaction.Data.Actions;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Data.Core;
    using Unity.Burst.CompilerServices;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static class ReactionUtil
    {
        public static FixedList64Bytes<(Target Target, byte Count)> GetUniqueTargets<T>(in DynamicBuffer<T> buffer)
            where T : unmanaged, IBufferElementData, IActionWithTarget
        {
            return GetUniqueTargets(buffer.AsNativeArray());
        }

        public static FixedList64Bytes<(Target Target, byte Count)> GetUniqueTargets<T>(in NativeArray<T> buffer)
            where T : unmanaged, IBufferElementData, IActionWithTarget
        {
            var uniqueTargets = new FixedList64Bytes<(Target Target, byte Count)>();

            foreach (var b in buffer)
            {
                var contains = false;

                for (var index = 0; index < uniqueTargets.Length; index++)
                {
                    ref var t = ref uniqueTargets.ElementAt(index);
                    if (t.Target == b.Target)
                    {
                        t.Count++;
                        contains = true;
                        break;
                    }
                }

                if (!contains)
                {
                    uniqueTargets.Add((b.Target, 1));
                }
            }

            return uniqueTargets;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualityCheck(in EventSubscriber subscriber, BufferLookup<ConditionComparisonValue> values, int value)
        {
            return subscriber.Operation switch
            {
                Equality.Equal => value == GetValue(subscriber, values),
                Equality.NotEqual => value != GetValue(subscriber, values),
                Equality.GreaterThan => value > GetValue(subscriber, values),
                Equality.GreaterThanEqual => value >= GetValue(subscriber, values),
                Equality.LessThan => value < GetValue(subscriber, values),
                Equality.LessThanEqual => value <= GetValue(subscriber, values),
                Equality.Between => Between(subscriber, values, value),
                Equality.Any => true,
                _ => true,
            };

            static int GetValue(in EventSubscriber subscriber, BufferLookup<ConditionComparisonValue> values)
            {
                return values[subscriber.Subscriber][subscriber.ValueIndex.Value].Value;
            }

            static bool Between(in EventSubscriber subscriber, BufferLookup<ConditionComparisonValue> values, int value)
            {
                return value >= values[subscriber.Subscriber][subscriber.ValueIndex.Value].Value &&
                    value <= values[subscriber.Subscriber][subscriber.ValueIndex.Value].Value;
            }
        }

        public static void WriteState(EventSubscriber subscriber, int value, BufferLookup<ConditionComparisonValue> values,
            ComponentLookup<ConditionActive> conditionActives, BufferLookup<ConditionValues> conditionValues)
        {
            Check.Assume(subscriber.Index < ConditionActive.MaxConditions);
            Check.Assume(!subscriber.Feature.IsAccumulate(), "State event with accumulate is not allowed");

            if (Hint.Unlikely(subscriber.Feature.HasValue()))
            {
                var conditionValue = conditionValues[subscriber.Subscriber];
                conditionValue[subscriber.Index] = new ConditionValues { Value = value };
            }

            if (Hint.Unlikely(!subscriber.Feature.HasCondition()))
            {
                return;
            }

            var match = EqualityCheck(subscriber, values, value);

            ref readonly var conditionRO = ref conditionActives.GetRefRO(subscriber.Subscriber).ValueRO.Value;
            if (conditionRO[subscriber.Index] == match)
            {
                return;
            }

            ref var conditions = ref conditionActives.GetRefRW(subscriber.Subscriber).ValueRW.Value;
            ref var bitField = ref UnsafeUtility.As<BitArray32, uint>(ref conditions);

#if UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS
            if (match)
            {
                Common.InterlockedOr(ref bitField, 1u << subscriber.Index);
            }
            else
            {
                Common.InterlockedAnd(ref bitField, ~(1u << subscriber.Index));
            }
#else
            throw new System.Exception("UNITY_BURST_EXPERIMENTAL_ATOMIC_INTRINSICS not set");
#endif
        }
    }
}
