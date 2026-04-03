// <copyright file="EssenceConfig.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Data
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Reaction.Data.Conditions;
    using Unity.Entities;
    using Unity.NetCode;

    /// <summary>
    /// Contains blob asset reference to intrinsic type definitions and configuration data.
    /// </summary>
    [GhostComponent(PrefabType=GhostPrefabType.Server)]
    public struct EssenceConfig : IComponentData
    {
        public BlobAssetReference<Data> Value;

        public struct Data
        {
            // public BlobHashMap<StatKey, StatData> StatsDatas;
            public BlobHashMap<IntrinsicKey, IntrinsicData> IntrinsicDatas;

            /// <summary>
            /// Buffer that matches stats to all the intrinsics they limit.
            /// </summary>
            public BlobMultiHashMap<StatKey, StatLimit> StatsLimitIntrinsics;
        }

        // /// <summary>
        // /// Configuration data for a single stat type.
        // /// </summary>
        // public struct StatData
        // {
        //     public bool AddOnly;
        // }

        /// <summary>
        /// Configuration data for a single intrinsic type.
        /// </summary>
        public struct IntrinsicData
        {
            public int DefaultValue;
            public ConditionKey Event;
            public int Min;
            public int Max;
            public StatKey MinStatKey;
            public StatKey MaxStatKey;
        }

        public struct StatLimit
        {
            public IntrinsicKey Intrinsic;
            public bool IsMin;
        }
    }
}
