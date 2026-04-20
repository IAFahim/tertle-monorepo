// <copyright file="TerrainAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS && UNITY_TERRAIN
namespace BovineLabs.Bridge.Authoring.Terrain
{
#if UNITY_PHYSICS_CUSTOM
    using Unity.Physics.Authoring;
#endif
    using BovineLabs.Core.Utility;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Physics;
    using UnityEngine;
    using TerrainCollider = Unity.Physics.TerrainCollider;

    [RequireComponent(typeof(UnityEngine.TerrainCollider))]
    public class TerrainAuthoring : MonoBehaviour
    {
#if UNITY_PHYSICS_CUSTOM
        public PhysicsCategoryTags BelongsTo;
        public PhysicsCategoryTags CollidesWith;
#endif
        public TerrainCollider.CollisionMethod CollisionMethod = TerrainCollider.CollisionMethod.VertexSamples;
        public float SmallestOffset = 0.01f; // 1 cm offset, works with 2048 resolution terrain

        private class Baker : Baker<TerrainAuthoring>
        {
            public override void Bake(TerrainAuthoring authoring)
            {
                var terrain = this.GetComponent<UnityEngine.TerrainCollider>();
                this.DependsOn(terrain);

#if UNITY_PHYSICS_CUSTOM
                var collisionFilter = new CollisionFilter
                {

                    BelongsTo = authoring.BelongsTo.Value,
                    CollidesWith = authoring.CollidesWith.Value,
                };
#else
                var collisionFilter = BovineLabs.Core.Utility.PhysicsLayerUtil.ProduceCollisionFilter(terrain, authoring.gameObject);
#endif

                var collider = CreateTerrainCollider(terrain.terrainData, collisionFilter, authoring);
                this.AddBlobAsset(ref collider.Value, out _);

                var entity = this.GetEntity(TransformUsageFlags.Renderable);
                this.AddComponent(entity, collider);
                this.AddSharedComponent(entity, default(PhysicsWorldIndex));
                this.AddBuffer<PhysicsColliderKeyEntityPair>(entity);
            }

            // Reference: https://gist.github.com/bernatgy/be96366a3df4ab349190c292b3c588ed
            private static PhysicsCollider CreateTerrainCollider(TerrainData terrainData, CollisionFilter filter, TerrainAuthoring authoring)
            {
                var scale = terrainData.heightmapScale;

                var colliderHeights = new NativeArray<float>(terrainData.heightmapResolution * terrainData.heightmapResolution, Allocator.Temp);
                var terrainHeights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

                // NOTE: Solves an issue with perfectly flat terrain failing to collide with objects.
                var heightmapScale = terrainData.size.z;
                var heightmapValuePerMeterInWorldSpace = 0.5f / heightmapScale;
                var inHeightMapUnits = authoring.SmallestOffset * heightmapValuePerMeterInWorldSpace;

                for (var j = 0; j < terrainData.heightmapResolution; j++)
                {
                    for (var i = 0; i < terrainData.heightmapResolution; i++)
                    {
                        var checkerboard = (i + j) % 2;

                        // Note: assumes terrain neighbours are never 1 cm difference from eachother
                        colliderHeights[j + (i * terrainData.heightmapResolution)] = terrainHeights[i, j] + (inHeightMapUnits * checkerboard);
                    }
                }

                // Note: Heightmap is between 0 and 0.5f (https://forum.unity.com/threads/terraindata-heightmaptexture-float-value-range.672421/)
                var physicsCollider = new PhysicsCollider
                {
                    Value = TerrainCollider.Create(colliderHeights, new int2(terrainData.heightmapResolution, terrainData.heightmapResolution), scale,
                        authoring.CollisionMethod, filter),
                };

                return physicsCollider;
            }
        }
    }
}
#endif