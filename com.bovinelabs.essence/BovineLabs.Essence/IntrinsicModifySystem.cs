// // <copyright file="DegenSystem.cs" company="BovineLabs">
// //     Copyright (c) BovineLabs. All rights reserved.
// // </copyright>
//
// namespace BovineLabs.Essence
// {
//     using BovineLabs.Core.Iterators;
//     using BovineLabs.Essence.Data;
//     using Unity.Burst;
//     using Unity.Burst.Intrinsics;
//     using Unity.Collections;
//     using Unity.Entities;
//     using UnityEngine;
//
//     public partial struct IntrinsicModifySystem : ISystem
//     {
//         private EntityQuery query;
//         private IntrinsicWriter.TypeHandle intrinsicWriterHandle;
//
//         public void OnCreate(ref SystemState state)
//         {
//             this.query = IntrinsicWriter.CreateQueryBuilder().WithAll<IntrinsicModifier>.Build(ref state);
//             this.intrinsicWriterHandle.Create(ref state);
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             this.intrinsicWriterHandle.Update(ref state);
//
//             state.Dependency = new ModifyJob
//             {
//                 Config = SystemAPI.GetSingletonBuffer<IntrinsicModifyConfig>(true).AsMap(),
//                 IntrinsicModifierHandle = SystemAPI.GetBufferTypeHandle<IntrinsicModifier>(),
//                 IntrinsicWriterHandle = this.intrinsicWriterHandle,
//             }.ScheduleParallel(this.query, state.Dependency);
//         }
//     }
//
//     [BurstCompile]
//     public struct ModifyJob : IJobChunk
//     {
//         [ReadOnly]
//         public DynamicHashMap<IntrinsicKey, IntrinsicModifyConfig.Data> Config;
//
//         public BufferTypeHandle<IntrinsicModifier> IntrinsicModifierHandle;
//
//         public IntrinsicWriter.TypeHandle IntrinsicWriterHandle;
//
//         public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
//         {
//             var intrinsicWriters = this.IntrinsicWriterHandle.Resolve(chunk);
//             var intrinsicModifierAccessor = chunk.GetBufferAccessor(ref this.IntrinsicModifierHandle);
//
//             for (var i = 0; i < chunk.Count; i++)
//             {
//                 var intrinsicModifier = intrinsicModifierAccessor[i];
//                 if (intrinsicModifier.Length == 0)
//                 {
//                     continue;
//                 }
//
//                 var intrinsicWriter = intrinsicWriters[i];
//
//                 for (var index = intrinsicModifier.Length - 1; index >= 0; index--)
//                 {
//                     var modifier = intrinsicModifier[index];
//
//                     if (!this.Config.TryGetValue(modifier.Intrinsic, out var config))
//                     {
//                         Debug.LogError($"Intrinsic {modifier.Intrinsic} modifying with associated config");
//                         continue;
//                     }
//
//                     var result = intrinsicWriter.Add(modifier.Intrinsic, config.Rate);
//                     if (config.RemoveDegenWhenZero && result <= 0)
//                     {
//                         intrinsicModifier.RemoveAtSwapBack(index);
//                     }
//                 }
//             }
//         }
//     }
//
//     [InternalBufferCapacity(0)]
//     public struct IntrinsicModifier : IBufferElementData
//     {
//         public IntrinsicKey Intrinsic;
//     }
//
//     public struct IntrinsicModifyConfig : IDynamicHashMap<IntrinsicKey, IntrinsicModifyConfig.Data>
//     {
//         byte IDynamicHashMap<IntrinsicKey, Data>.Value { get; }
//
//         public struct Data
//         {
//             public bool RemoveDegenWhenZero;
//             public int Rate;
//         }
//     }
// }
