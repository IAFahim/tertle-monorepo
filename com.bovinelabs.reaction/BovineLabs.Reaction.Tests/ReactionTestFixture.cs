// <copyright file="ReactionTestFixture.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Tests
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Reaction.Data.Active;
    using BovineLabs.Reaction.Data.Builders;
    using BovineLabs.Reaction.Data.Conditions;
    using BovineLabs.Reaction.Data.Core;
    using BovineLabs.Testing;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Base test fixture for reaction system tests, providing common setup and helper methods.
    /// </summary>
    public abstract class ReactionTestFixture : ECSTestsFixture
    {
        /// <summary>
        /// Delegate for building composite condition logic using the fluent ConditionCompositeBuilder API.
        /// </summary>
        /// <param name="builder">The builder instance to configure (passed by reference for fluent API).</param>
        protected delegate void BuilderAction(ref ConditionCompositeBuilder builder);

        /// <summary>
        /// Creates a test entity with basic condition components.
        /// </summary>
        /// <param name="conditions">The condition states to set.</param>
        /// <returns>Entity with condition components configured.</returns>
        protected Entity CreateConditionEntity(BitArray32 conditions)
        {
            var archetype = this.Manager.CreateArchetype(
                typeof(ConditionActive),
                typeof(ConditionAllActive));
            var entity = this.Manager.CreateEntity(archetype);

            this.Manager.SetComponentData(entity, new ConditionActive { Value = conditions });
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, false);

            return entity;
        }

        /// <summary>
        /// Creates a test entity with condition components including chance-based logic.
        /// </summary>
        /// <param name="conditions">The condition states to set.</param>
        /// <param name="chance">The chance value (0-10000, representing 0%-100%).</param>
        /// <returns>Entity with condition and chance components configured.</returns>
        protected Entity CreateConditionChanceEntity(BitArray32 conditions, ushort chance)
        {
            var archetype = this.Manager.CreateArchetype(
                typeof(ConditionActive),
                typeof(ConditionChance),
                typeof(ConditionAllActive));
            var entity = this.Manager.CreateEntity(archetype);

            this.Manager.SetComponentData(entity, new ConditionActive { Value = conditions });
            this.Manager.SetComponentData(entity, new ConditionChance { Value = chance });
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, false);

            return entity;
        }

        /// <summary>
        /// Creates a test entity with composite condition logic.
        /// </summary>
        /// <param name="conditions">The condition states to set.</param>
        /// <param name="compositeLogic">The composite logic blob asset.</param>
        /// <returns>Entity with composite condition components configured.</returns>
        protected Entity CreateConditionCompositeEntity(BitArray32 conditions, BlobAssetReference<CompositeLogic> compositeLogic)
        {
            var archetype = this.Manager.CreateArchetype(
                typeof(ConditionActive),
                typeof(ConditionComposite),
                typeof(ConditionAllActive));
            var entity = this.Manager.CreateEntity(archetype);

            this.Manager.SetComponentData(entity, new ConditionActive { Value = conditions });
            this.Manager.SetComponentData(entity, new ConditionComposite { Logic = compositeLogic });
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, false);

            return entity;
        }

        /// <summary>
        /// Creates a test entity with active and cancel components for testing cancel logic.
        /// </summary>
        /// <param name="cancelConditions">The conditions required to prevent cancellation.</param>
        /// <param name="activeConditions">The currently active conditions.</param>
        /// <returns>Entity configured for cancel logic testing.</returns>
        protected Entity CreateActiveCancelEntity(BitArray32 cancelConditions, BitArray32 activeConditions)
        {
            var archetype = this.Manager.CreateArchetype(
                typeof(ConditionCancelActive),
                typeof(ConditionActive),
                typeof(ActiveOnDuration),
                typeof(Active),
                typeof(ActiveCancel),
                typeof(ConditionAllActive));
            var entity = this.Manager.CreateEntity(archetype);

            this.Manager.SetComponentData(entity, new ConditionCancelActive { Value = cancelConditions });
            this.Manager.SetComponentData(entity, new ConditionActive { Value = activeConditions });
            this.Manager.SetComponentEnabled<ActiveCancel>(entity, false);
            this.Manager.SetComponentEnabled<ConditionAllActive>(entity, false);

            return entity;
        }

        /// <summary>
        /// Creates a test entity with basic condition components, correcting unused conditions for simple tests.
        /// Assumes first 8 conditions (0-7) are used, sets remaining bits 8-31 to true.
        /// </summary>
        /// <param name="conditions">The condition states to set (only bits 0-7 should be set).</param>
        /// <returns>Entity with condition components configured.</returns>
        protected Entity CreateSimpleConditionEntity(BitArray32 conditions)
        {
            ValidateSimpleConditions(conditions);
            var correctedConditions = CorrectSimpleConditions(conditions);
            return this.CreateConditionEntity(correctedConditions);
        }

        /// <summary>
        /// Creates a test entity with condition components including chance-based logic, correcting unused conditions.
        /// Assumes first 8 conditions (0-7) are used, sets remaining bits 8-31 to true.
        /// </summary>
        /// <param name="conditions">The condition states to set (only bits 0-7 should be set).</param>
        /// <param name="chance">The chance value (0-10000, representing 0%-100%).</param>
        /// <returns>Entity with condition and chance components configured.</returns>
        protected Entity CreateSimpleConditionChanceEntity(BitArray32 conditions, ushort chance)
        {
            ValidateSimpleConditions(conditions);
            var correctedConditions = CorrectSimpleConditions(conditions);
            return this.CreateConditionChanceEntity(correctedConditions, chance);
        }

        /// <summary>
        /// Sets up corrected condition data by setting unused condition bits to true.
        /// This is required by some systems that expect unused conditions to be in the true state.
        /// </summary>
        /// <param name="conditions">The input conditions.</param>
        /// <param name="usedConditionsMask">Mask indicating which conditions are actually used.</param>
        /// <returns>Corrected conditions with unused bits set to true.</returns>
        protected static BitArray32 CorrectUnusedConditions(BitArray32 conditions, uint usedConditionsMask)
        {
            uint correctedData = conditions.Data;
            uint unusedConditionsMask = ~usedConditionsMask;
            correctedData |= unusedConditionsMask;
            return new BitArray32(correctedData);
        }

        /// <summary>
        /// Creates a composite logic builder with common setup.
        /// </summary>
        /// <returns>A new ConditionCompositeBuilder with Temp allocator.</returns>
        protected static ConditionCompositeBuilder CreateCompositeBuilder()
        {
            return new ConditionCompositeBuilder(Allocator.Temp);
        }

        /// <summary>
        /// Creates a test entity with composite condition logic, automatically correcting unused conditions.
        /// </summary>
        /// <param name="conditions">The condition states to set.</param>
        /// <param name="buildLogic">Delegate to build the composite logic using fluent API.</param>
        /// <returns>Entity with composite condition components configured.</returns>
        protected Entity CreateCompositeConditionEntity(BitArray32 conditions, BuilderAction buildLogic)
        {
            var builder = CreateCompositeBuilder();
            buildLogic(ref builder);
            var compositeLogic = builder.CreateBlobAsset();
            this.BlobAssetStore.TryAdd(ref compositeLogic);
            builder.Dispose();

            // Extract which conditions are referenced in the composite logic
            ref var logic = ref compositeLogic.Value;
            uint usedConditionsMask = ExtractUsedConditionsMask(ref logic);

            // Set unused conditions to true as per system requirement
            var correctedConditions = CorrectUnusedConditions(conditions, usedConditionsMask);

            return this.CreateConditionCompositeEntity(correctedConditions, compositeLogic);
        }

        /// <summary>
        /// Extracts which conditions are referenced in composite logic (including nested logic).
        /// </summary>
        /// <param name="logic">The composite logic to analyze.</param>
        /// <returns>Mask indicating which conditions are used.</returns>
        protected static uint ExtractUsedConditionsMask(ref CompositeLogic logic)
        {
            uint usedConditionsMask = 0;
            ExtractUsedConditionsRecursive(ref logic, ref usedConditionsMask);
            return usedConditionsMask;
        }

        /// <summary>
        /// Sets up corrected condition data for simple tests (assumes first 8 conditions are used).
        /// </summary>
        /// <param name="conditions">The input conditions.</param>
        /// <returns>Corrected conditions with bits 8-31 set to true.</returns>
        private static BitArray32 CorrectSimpleConditions(BitArray32 conditions)
        {
            const uint unusedMask = 0xFFFFFF00; // Bits 8-31 set to 1
            return CorrectUnusedConditions(conditions, ~unusedMask);
        }

        /// <summary>
        /// Validates that only simple conditions (bits 0-7) are set.
        /// </summary>
        /// <param name="conditions">The conditions to validate.</param>
        private static void ValidateSimpleConditions(BitArray32 conditions)
        {
            const uint simpleMask = 0x000000FF; // Only bits 0-7 should be set
            if ((conditions.Data & ~simpleMask) != 0)
            {
                throw new System.ArgumentException($"Simple conditions should only use bits 0-7, but found bits set beyond that: 0x{conditions.Data:X8}");
            }
        }

        // Helper method to recursively extract used conditions from composite logic
        private static void ExtractUsedConditionsRecursive(ref CompositeLogic logic, ref uint usedConditionsMask)
        {
            for (int i = 0; i < logic.Groups.Length; i++)
            {
                ref var group = ref logic.Groups[i];
                if (group.NestedLogicIndex >= 0 && group.NestedLogicIndex < logic.NestedLogics.Length)
                {
                    // Recursively extract from nested logic
                    ref var nestedLogic = ref logic.NestedLogics[group.NestedLogicIndex];
                    ExtractUsedConditionsRecursive(ref nestedLogic, ref usedConditionsMask);
                }
                else
                {
                    // Extract from condition mask
                    usedConditionsMask |= group.Mask.Data;
                }
            }
        }

        /// <summary>
        /// Creates a basic reaction entity with the minimal components needed for reaction system testing.
        /// The entity is set up as "newly activated" (Active enabled, ActivePrevious disabled).
        /// </summary>
        /// <param name="target">The target entity.</param>
        /// <returns>Entity configured with Active, ActivePrevious, and Targets components.</returns>
        protected Entity CreateReactionEntity(Entity target = default)
        {
            var archetype = this.Manager.CreateArchetype(
                typeof(Active),
                typeof(ActivePrevious),
                typeof(Targets));
            var entity = this.Manager.CreateEntity(archetype);

            // Set up as "newly activated" - Active enabled, ActivePrevious disabled
            this.Manager.SetComponentEnabled<Active>(entity, true);
            this.Manager.SetComponentEnabled<ActivePrevious>(entity, false);

            // Set up basic Targets with self-references
            this.Manager.SetComponentData(entity, new Targets
            {
                Owner = entity,
                Source = entity,
                Target = target,
            });

            return entity;
        }

        /// <summary>
        /// Creates a basic reaction entity with custom target entities.
        /// </summary>
        /// <param name="owner">The owner entity.</param>
        /// <param name="source">The source entity.</param>
        /// <param name="target">The target entity.</param>
        /// <returns>Entity configured with specified targets.</returns>
        protected Entity CreateReactionEntity(Entity owner, Entity source, Entity target)
        {
            var entity = this.CreateReactionEntity();

            // Update targets with specified entities
            this.Manager.SetComponentData(entity, new Targets
            {
                Owner = owner,
                Source = source,
                Target = target,
            });

            return entity;
        }

        /// <summary>
        /// Creates an entity with InitializeEntity, ObjectId, and Targets components for initialization system testing.
        /// </summary>
        /// <param name="objectId">The ObjectId to assign to the entity.</param>
        /// <param name="owner">The owner entity (defaults to self if not specified).</param>
        /// <param name="source">The source entity (defaults to self if not specified).</param>
        /// <param name="target">The target entity.</param>
        /// <returns>Entity configured for initialization system testing.</returns>
        protected Entity CreateInitializationEntity(ObjectId objectId, Entity owner = default, Entity source = default, Entity target = default)
        {
            var entity = this.Manager.CreateEntity(typeof(InitializeEntity), typeof(ObjectId), typeof(Targets));

            this.Manager.SetComponentData(entity, objectId);
            this.Manager.SetComponentData(entity, new Targets
            {
                Owner = owner == default ? entity : owner,
                Source = source == default ? entity : source,
                Target = target,
            });

            return entity;
        }

        /// <summary>
        /// Runs a system group and completes all tracked jobs for testing.
        /// </summary>
        /// <param name="systemGroup">The system group to update.</param>
        protected void RunSystemGroup(ComponentSystemGroup systemGroup)
        {
            systemGroup.Update();
            this.Manager.CompleteAllTrackedJobs();
        }

        /// <summary>
        /// Sets up the ObjectDefinitionRegistry singleton with the provided prefab mappings.
        /// Creates the singleton entity, initializes a NativeHashMap with the mappings, and registers it.
        /// </summary>
        /// <param name="mappings">Array of (ObjectId, Entity) tuples representing prefab mappings.</param>
        /// <returns>The NativeHashMap created for the registry. Caller is responsible for disposing it.</returns>
        protected NativeHashMap<ObjectId, Entity> SetupObjectRegistry(params (ObjectId id, Entity prefab)[] mappings)
        {
            // Create the registry singleton entity
            var registryEntity = this.Manager.CreateSingleton<ObjectDefinitionRegistry>();

            // Create and populate the NativeHashMap
            var objectMap = new NativeHashMap<ObjectId, Entity>(mappings.Length, Allocator.Persistent);
            foreach (var (id, prefab) in mappings)
            {
                objectMap[id] = prefab;
            }

            // Set the registry component data
            var registry = new ObjectDefinitionRegistry(objectMap);
            this.Manager.SetComponentData(registryEntity, registry);

            return objectMap;
        }

        /// <summary>
        /// Adds additional mappings to an existing ObjectDefinitionRegistry.
        /// </summary>
        /// <param name="objectMap">The existing object map to update.</param>
        /// <param name="mappings">Array of (ObjectId, Entity) tuples to add.</param>
        protected void AddToObjectRegistry(NativeHashMap<ObjectId, Entity> objectMap, params (ObjectId id, Entity prefab)[] mappings)
        {
            // Add new mappings to the existing map
            foreach (var (id, prefab) in mappings)
            {
                objectMap[id] = prefab;
            }

            // Update the registry component data
            var registryEntity = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ObjectDefinitionRegistry>()
                .Build(this.Manager).GetSingletonEntity();

            var registry = new ObjectDefinitionRegistry(objectMap);
            this.Manager.SetComponentData(registryEntity, registry);
        }

        /// <summary>
        /// Gets the stable type hash for a type, commonly used for action components.
        /// </summary>
        /// <typeparam name="T">The type to get the hash for.</typeparam>
        /// <returns>The stable type hash for the type.</returns>
        protected static ulong GetTestComponentTypeHash<T>()
        {
            return TypeManager.GetTypeInfo<T>().StableTypeHash;
        }

        /// <summary>
        /// Runs multiple systems in sequence and completes all tracked jobs.
        /// This is a common pattern for running systems with optional command buffer systems.
        /// </summary>
        /// <param name="systems">The systems to run in order.</param>
        protected void RunSystems(params SystemHandle[] systems)
        {
            foreach (var system in systems)
            {
                system.Update(this.WorldUnmanaged);
            }

            this.Manager.CompleteAllTrackedJobs();
        }

        /// <summary>
        /// Sets the active state of a reaction entity by configuring Active and ActivePrevious components.
        /// This follows the common pattern for reaction activation/deactivation.
        /// </summary>
        /// <param name="entity">The reaction entity to configure.</param>
        /// <param name="isActive">True to activate the reaction, false to deactivate it.</param>
        protected void SetReactionActiveState(Entity entity, bool isActive)
        {
            this.Manager.SetComponentEnabled<Active>(entity, isActive);
            this.Manager.SetComponentEnabled<ActivePrevious>(entity, !isActive);
        }
    }
}
