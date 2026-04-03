// <copyright file="ActionInitializeStatRangeSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Actions
{
    using BovineLabs.Core;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Core.Utility;
    using BovineLabs.Essence.Data;
    using BovineLabs.Essence.Data.Actions;
    using Unity.Burst;
    using Unity.Entities;

    /// <summary>
    /// The InitializeActionStatRangeSystem manages the initialization of <see cref="ActionStat"/> when using the <see cref="StatValueType.Range"/>
    /// by randomly picking a value between the Min and Max specified values.
    /// </summary>
    [WorldSystemFilter(Worlds.ServerLocal)]
    [UpdateInGroup(typeof(InitializeSystemGroup))]
    public partial struct InitializeActionStatRangeSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new InitializeStatRangeJob().ScheduleParallel();
        }

        [BurstCompile]
        [WithAny(typeof(InitializeEntity), typeof(InitializeSubSceneEntity))]
        private partial struct InitializeStatRangeJob : IJobEntity
        {
            private void Execute(DynamicBuffer<ActionStat> actionStats)
            {
                var stats = actionStats.AsNativeArray();
                for (var i = 0; i < stats.Length; i++)
                {
                    ref var effectStat = ref stats.ElementAt(i);

                    if (effectStat.ValueType != StatValueType.Range)
                    {
                        continue;
                    }

                    if (effectStat.ModifyType == StatModifyType.Added)
                    {
                        effectStat.Range.Value.Int = GlobalRandom.NextInt(effectStat.Range.Min.Int, effectStat.Range.Max.Int);
                    }
                    else
                    {
                        effectStat.Range.Value.Float = GlobalRandom.NextFloat(effectStat.Range.Min.Float, effectStat.Range.Max.Float);
                    }
                }
            }
        }
    }
}
