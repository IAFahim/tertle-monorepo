// <copyright file="ConditionCompositeBuilder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Builders
{
    using System;
    using BovineLabs.Core.Collections;
    using BovineLabs.Reaction.Data.Conditions;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>
    /// Builder for creating composite logic blob assets with boolean expressions using hierarchical group structure.
    /// </summary>
    public struct ConditionCompositeBuilder : IDisposable
    {
        private readonly Allocator allocator;
        private NativeList<GroupContext> groupStack;
        private int totalBeginGroupCalls;
        private int totalEndGroupCalls;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConditionCompositeBuilder"/> struct.
        /// </summary>
        /// <param name="allocator">The allocator to use for temporary storage.</param>
        public ConditionCompositeBuilder(Allocator allocator)
        {
            this.groupStack = new NativeList<GroupContext>(4, allocator);
            this.allocator = allocator;
            this.totalBeginGroupCalls = 0;
            this.totalEndGroupCalls = 0;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!this.groupStack.IsCreated)
            {
                return;
            }

            foreach (var group in this.groupStack)
            {
                this.DisposeGroupContext(group);
            }

            this.groupStack.Dispose();
        }

        /// <summary>
        /// Begins a new logical group with the specified combination type.
        /// </summary>
        /// <param name="combination">How to combine elements within this group (AND/OR).</param>
        /// <returns>This builder for method chaining.</returns>
        public ConditionCompositeBuilder BeginGroup(LogicOperation combination)
        {
            var context = new GroupContext
            {
                Combination = combination,
                Groups = new NativeList<LogicGroup>(4, this.allocator),
                NestedLogics = new NativeList<NestedLogicBuilder>(4, this.allocator),
                CurrentConditions = default,
                CurrentLogicType = LogicOperation.And,
                HasCurrentGroup = false,
            };

            this.groupStack.Add(context);
            this.totalBeginGroupCalls++;
            return this;
        }

        /// <summary>
        /// Ends the current logical group and returns to the parent level.
        /// </summary>
        /// <returns>This builder for method chaining.</returns>
        public ConditionCompositeBuilder EndGroup()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (this.groupStack.Length == 0)
            {
                throw new InvalidOperationException("No group to end - BeginGroup must be called first.");
            }
#endif

            ref var currentContext = ref this.groupStack.ElementAt(this.groupStack.Length - 1);

            // If there are pending conditions, add them as a group
            this.FlushCurrentConditions(ref currentContext);

            this.totalEndGroupCalls++;

            // If this is the root group, don't remove it from the stack
            if (this.groupStack.Length == 1)
            {
                // Root group - leave it on the stack for CreateBlobAsset
                return this;
            }

            // Remove the current context from the stack
            var completedContext = currentContext;
            this.groupStack.RemoveAt(this.groupStack.Length - 1);

            // Add this group as nested logic to the parent
            if (this.groupStack.Length > 0)
            {
                ref var parentContext = ref this.groupStack.ElementAt(this.groupStack.Length - 1);

                var nestedLogic = new NestedLogicBuilder
                {
                    Groups = completedContext.Groups,
                    NestedLogics = completedContext.NestedLogics,
                    GroupCombination = completedContext.Combination,
                };

                var nestedIndex = (short)parentContext.NestedLogics.Length;
                parentContext.NestedLogics.Add(nestedLogic);

                // Add a reference to this nested logic in the parent's groups
                parentContext.Groups.Add(new LogicGroup
                {
                    Mask = default,
                    Logic = LogicOperation.And, // Default logic for nested groups
                    NestedLogicIndex = nestedIndex,
                });
            }

            return this;
        }

        /// <summary>
        /// Adds a condition to the current group.
        /// </summary>
        /// <param name="condition">The condition index to add.</param>
        /// <returns>This builder for method chaining.</returns>
        public ConditionCompositeBuilder Add(byte condition)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (this.groupStack.Length == 0)
            {
                throw new InvalidOperationException("No active group - BeginGroup must be called first.");
            }

            if (condition >= ConditionActive.MaxConditions)
            {
                throw new ArgumentOutOfRangeException(nameof(condition), "Condition index exceeds maximum allowed conditions.");
            }
#endif

            ref var currentContext = ref this.groupStack.ElementAt(this.groupStack.Length - 1);

            // Determine the logic type based on the group's combination
            var logicType = currentContext.Combination == LogicOperation.Or ? LogicOperation.Or : LogicOperation.And;

            // If we're switching logic types, flush current conditions
            if (currentContext.HasCurrentGroup && currentContext.CurrentLogicType != logicType)
            {
                this.FlushCurrentConditions(ref currentContext);
            }

            // Add to current batch
            currentContext.CurrentConditions[condition] = true;
            currentContext.CurrentLogicType = logicType;
            currentContext.HasCurrentGroup = true;

            return this;
        }

        /// <summary>
        /// Adds a negated condition to the current group.
        /// </summary>
        /// <param name="condition">The condition index to negate and add.</param>
        /// <returns>This builder for method chaining.</returns>
        public ConditionCompositeBuilder AddNot(byte condition)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (this.groupStack.Length == 0)
            {
                throw new InvalidOperationException("No active group - BeginGroup must be called first.");
            }

            if (condition >= ConditionActive.MaxConditions)
            {
                throw new ArgumentOutOfRangeException(nameof(condition), "Condition index exceeds maximum allowed conditions.");
            }
#endif

            ref var currentContext = ref this.groupStack.ElementAt(this.groupStack.Length - 1);

            // If we're switching logic types, flush current conditions
            if (currentContext.HasCurrentGroup && currentContext.CurrentLogicType != LogicOperation.Not)
            {
                this.FlushCurrentConditions(ref currentContext);
            }

            // Add to current batch
            currentContext.CurrentConditions[condition] = true;
            currentContext.CurrentLogicType = LogicOperation.Not;
            currentContext.HasCurrentGroup = true;

            return this;
        }

        /// <summary>
        /// Adds a condition to the current group using XOR logic.
        /// </summary>
        /// <param name="condition">The condition index to add.</param>
        /// <returns>This builder for method chaining.</returns>
        public ConditionCompositeBuilder AddXor(byte condition)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (this.groupStack.Length == 0)
            {
                throw new InvalidOperationException("No active group - BeginGroup must be called first.");
            }

            if (condition >= ConditionActive.MaxConditions)
            {
                throw new ArgumentOutOfRangeException(nameof(condition), "Condition index exceeds maximum allowed conditions.");
            }
#endif

            ref var currentContext = ref this.groupStack.ElementAt(this.groupStack.Length - 1);

            // If we're switching logic types, flush current conditions
            if (currentContext.HasCurrentGroup && currentContext.CurrentLogicType != LogicOperation.Xor)
            {
                this.FlushCurrentConditions(ref currentContext);
            }

            // Add to current batch
            currentContext.CurrentConditions[condition] = true;
            currentContext.CurrentLogicType = LogicOperation.Xor;
            currentContext.HasCurrentGroup = true;

            return this;
        }

        /// <summary>
        /// Creates the composite logic blob asset.
        /// </summary>
        /// <param name="blobAllocator">The blobs allocator, defaults to persistent.</param>
        /// <returns>The blob asset reference containing the composite logic.</returns>
        public BlobAssetReference<CompositeLogic> CreateBlobAsset(Allocator blobAllocator = Allocator.Persistent)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<CompositeLogic>();
            this.ApplyTo(ref builder, ref root);
            return builder.CreateBlobAssetReference<CompositeLogic>(blobAllocator);
        }

        /// <summary>
        /// Applies the built composite logic structure to an existing CompositeLogic reference within a blob builder.
        /// This method allows for more flexible blob asset construction where the CompositeLogic is part of a larger
        /// blob structure rather than being the root element.
        /// </summary>
        /// <param name="builder">The blob builder to use for allocation.</param>
        /// <param name="root">Reference to the CompositeLogic structure to populate.</param>
        /// <exception cref="InvalidOperationException">Thrown when no groups are defined or when there are unclosed groups.</exception>
        public void ApplyTo(ref BlobBuilder builder, ref CompositeLogic root)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            // Validate that all groups have been properly closed
            if (this.groupStack.Length == 0)
            {
                throw new InvalidOperationException("No groups defined. Use BeginGroup() to create at least one group.");
            }

            if (this.totalBeginGroupCalls != this.totalEndGroupCalls)
            {
                throw new InvalidOperationException($"Unclosed groups detected. {this.totalBeginGroupCalls - this.totalEndGroupCalls} groups need EndGroup() called.");
            }
#endif

            // Get the root group and flush any pending conditions
            ref var rootContext = ref this.groupStack.ElementAt(0);
            this.FlushCurrentConditions(ref rootContext);

            root.GroupCombination = rootContext.Combination;

            // Build groups array
            var groupArray = builder.Allocate(ref root.Groups, rootContext.Groups.Length);
            for (var i = 0; i < rootContext.Groups.Length; i++)
            {
                groupArray[i] = rootContext.Groups[i];
            }

            // Build nested logics recursively
            this.BuildNestedLogics(ref builder, ref root.NestedLogics, ref rootContext.NestedLogics);
        }

        private void FlushCurrentConditions(ref GroupContext context)
        {
            if (context.HasCurrentGroup && context.CurrentConditions.Data != 0)
            {
                context.Groups.Add(new LogicGroup
                {
                    Mask = context.CurrentConditions,
                    Logic = context.CurrentLogicType,
                    NestedLogicIndex = -1,
                });

                context.CurrentConditions = default;
                context.HasCurrentGroup = false;
            }
        }

        private void BuildNestedLogics(ref BlobBuilder builder, ref BlobArray<CompositeLogic> nestedArray, ref NativeList<NestedLogicBuilder> tempNestedLogics)
        {
            var array = builder.Allocate(ref nestedArray, tempNestedLogics.Length);
            for (int i = 0; i < tempNestedLogics.Length; i++)
            {
                ref var nested = ref array[i];
                ref var temp = ref tempNestedLogics.ElementAt(i);

                nested.GroupCombination = temp.GroupCombination;

                var groupArray = builder.Allocate(ref nested.Groups, temp.Groups.Length);
                for (int j = 0; j < temp.Groups.Length; j++)
                {
                    groupArray[j] = temp.Groups[j];
                }

                // Recursively build nested logics
                this.BuildNestedLogics(ref builder, ref nested.NestedLogics, ref temp.NestedLogics);
            }
        }

        private void DisposeGroupContext(GroupContext context)
        {
            foreach (var nestedLogic in context.NestedLogics)
            {
                this.DisposeNestedTemp(nestedLogic);
            }

            context.NestedLogics.Dispose();
        }

        private void DisposeNestedTemp(NestedLogicBuilder nested)
        {
            foreach (var nestedLogic in nested.NestedLogics)
            {
                this.DisposeNestedTemp(nestedLogic);
            }

            nested.NestedLogics.Dispose();
        }

        // Context for tracking a single group level during construction
        private struct GroupContext
        {
            public LogicOperation Combination;
            public NativeList<LogicGroup> Groups;
            public NativeList<NestedLogicBuilder> NestedLogics;
            public BitArray32 CurrentConditions;
            public LogicOperation CurrentLogicType;
            public bool HasCurrentGroup;
        }

        // Helper struct for temporarily storing nested logic data during construction
        private struct NestedLogicBuilder
        {
            public NativeList<LogicGroup> Groups;
            public NativeList<NestedLogicBuilder> NestedLogics;
            public LogicOperation GroupCombination;
        }
    }
}
