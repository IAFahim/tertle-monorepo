// // <copyright file="IntrinsicStates.cs" company="BovineLabs">
// //     Copyright (c) BovineLabs. All rights reserved.
// // </copyright>
//
// namespace BovineLabs.Stats.Data
// {
//     using BovineLabs.Core.Iterators;
//     using BovineLabs.Reaction.Data.Conditions;
//     using Unity.Entities;
//
//     /// <summary> A map of state condition keys to intrinsic keys. </summary>
//     [InternalBufferCapacity(0)]
//     public struct IntrinsicStates : IDynamicHashMap<ConditionKey, IntrinsicKey>
//     {
//         byte IDynamicHashMap<ConditionKey, IntrinsicKey>.Value { get; }
//     }
//
//     public static class InternalBufferCapacityExtensions
//     {
//         public static DynamicBuffer<IntrinsicStates> Initialize(this DynamicBuffer<IntrinsicStates> intrinsicConditions)
//         {
//             return intrinsicConditions.InitializeHashMap<IntrinsicStates, ConditionKey, IntrinsicKey>();
//         }
//
//         public static DynamicHashMap<ConditionKey, IntrinsicKey> AsMap(this DynamicBuffer<IntrinsicStates> buffer)
//         {
//             return buffer.AsHashMap<IntrinsicStates, ConditionKey, IntrinsicKey>();
//         }
//     }
// }
