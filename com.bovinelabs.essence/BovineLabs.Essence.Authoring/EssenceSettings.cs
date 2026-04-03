// <copyright file="EssenceSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Authoring
{
    using System.Collections.Generic;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Settings;
    using BovineLabs.Essence.Data;
    using BovineLabs.Reaction.Authoring.Conditions;
    using BovineLabs.Reaction.Data.Conditions;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Settings ScriptableObject for configuring stat and intrinsic schemas in the game.
    /// This settings object manages the collection of stat and intrinsic schema definitions and handles their baking into runtime data.
    /// Configured for the "Reaction" settings group and server world.
    /// </summary>
    [SettingsGroup("Reaction")]
    [SettingsWorld("Server")]
    public class EssenceSettings : SettingsBase
    {
        [SerializeField]
        private List<StatSchemaObject> statSchemas = new();

        [SerializeField]
        private List<IntrinsicSchemaObject> intrinsicSchemas = new();

        public IReadOnlyList<StatSchemaObject> StatSchemas => this.statSchemas;

        public IReadOnlyList<IntrinsicSchemaObject> IntrinsicSchemas => this.intrinsicSchemas;

        /// <inheritdoc/>
        public override void Bake(Baker<SettingsAuthoring> baker)
        {
            var entity = baker.GetEntity(TransformUsageFlags.None);

            this.SetupIntrinsicConditions(baker, entity);
        }

        private void SetupIntrinsicConditions(Baker<SettingsAuthoring> baker, Entity entity)
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<EssenceConfig.Data>();

            // this.SetupStatData(baker, builder, ref root);
            this.SetupIntrinsicData(baker, builder, ref root);

            var blobAssetReference = builder.CreateBlobAssetReference<EssenceConfig.Data>(Allocator.Persistent);
            baker.AddBlobAsset(ref blobAssetReference, out _);

            baker.AddComponent(entity, new EssenceConfig { Value = blobAssetReference });
        }

        // private void SetupStatData(Baker<SettingsAuthoring> baker, BlobBuilder builder, ref EssenceConfig.Data root)
        // {
        //     var statBuilder = builder.AllocateHashMap(ref root.StatsDatas, this.statSchemas.Count, 2);
        //
        //     foreach (var stat in this.statSchemas)
        //     {
        //         if (stat == null)
        //         {
        //             continue;
        //         }
        //
        //         baker.DependsOn(stat);
        //
        //
        //         statBuilder.Add(stat, new EssenceConfig.StatData
        //         {
        //             AddOnly = stat.AddOnly,
        //         });
        //     }
        // }

        private void SetupIntrinsicData(Baker<SettingsAuthoring> baker, BlobBuilder builder, ref EssenceConfig.Data root)
        {
            var intrinsicBuilder = builder.AllocateHashMap(ref root.IntrinsicDatas, this.intrinsicSchemas.Count, 2);

            var statMap = new NativeMultiHashMap<StatKey, EssenceConfig.StatLimit>(this.intrinsicSchemas.Count, Allocator.Temp);

            foreach (var intrinsic in this.intrinsicSchemas)
            {
                if (intrinsic == null)
                {
                    continue;
                }

                baker.DependsOn(intrinsic);

                var path = AssetDatabase.GetAssetPath(intrinsic);
                var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);

                ConditionKey eventKey = default;

                foreach (var asset in assets)
                {
                    if (asset is not ConditionEventObject condition)
                    {
                        continue;
                    }

                    eventKey = condition.Key;
                    break;
                }

                intrinsicBuilder.Add(intrinsic, new EssenceConfig.IntrinsicData
                {
                    DefaultValue = intrinsic.DefaultValue,
                    Event = eventKey,
                    Min = intrinsic.Range.x,
                    Max = intrinsic.Range.y,
                    MinStatKey = intrinsic.MinStat,
                    MaxStatKey = intrinsic.MaxStat,
                });

                if (intrinsic.MinStat)
                {
                    statMap.Add(intrinsic.MinStat, new EssenceConfig.StatLimit
                    {
                        Intrinsic = intrinsic,
                        IsMin = true,
                    });
                }

                if (intrinsic.MaxStat)
                {
                    statMap.Add(intrinsic.MaxStat, new EssenceConfig.StatLimit
                    {
                        Intrinsic = intrinsic,
                        IsMin = false,
                    });
                }
            }

            var statBuilder = builder.AllocateMultiHashMap(ref root.StatsLimitIntrinsics, statMap.Count, 2);
            foreach (var s in statMap)
            {
                statBuilder.Add(s.Key, s.Value);
            }
        }
    }
}
