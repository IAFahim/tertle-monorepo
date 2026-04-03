// <copyright file="CompositeLogic.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Conditions
{
    using BovineLabs.Core.Collections;
    using Unity.Entities;

    /// <summary>
    /// Unified logic operation enum for both condition evaluation and group combination.
    /// </summary>
    public enum LogicOperation : byte
    {
        /// <summary>
        /// All operands must be true (default).
        /// </summary>
        And = 0,

        /// <summary>
        /// Any operand can be true.
        /// </summary>
        Or = 1,

        /// <summary>
        /// Exactly one operand must be true (odd number of true operands).
        /// </summary>
        Xor = 2,

        /// <summary>
        /// All operands must be false (negation).
        /// </summary>
        Not = 3,
    }

    /// <summary>
    /// Blob asset structure defining composite boolean logic for condition evaluation.
    /// Supports nested expressions like: ((A AND B) OR C) AND (D OR (E AND F)).
    /// </summary>
    public struct CompositeLogic
    {
        /// <summary>
        /// Array of logic groups that are combined with the GroupCombination operation.
        /// </summary>
        public BlobArray<LogicGroup> Groups;

        /// <summary>
        /// Array of nested logic structures that can be referenced by groups.
        /// </summary>
        public BlobArray<CompositeLogic> NestedLogics;

        /// <summary>
        /// How to combine the results of all groups.
        /// </summary>
        public LogicOperation GroupCombination;
    }

    /// <summary>
    /// A single logic group containing conditions with AND, OR, and NOT operations.
    /// Can reference either condition bits (Mask) or nested logic (NestedLogicIndex).
    /// </summary>
    public struct LogicGroup
    {
        /// <summary>
        /// Bitmask of conditions to evaluate. Used when NestedLogicIndex is -1.
        /// </summary>
        public BitArray32 Mask;

        /// <summary>
        /// Type of logic operation to perform.
        /// </summary>
        public LogicOperation Logic;

        /// <summary>
        /// Index into the NestedLogics array, or -1 to use Mask instead.
        /// When >= 0, this group evaluates the nested logic at this index.
        /// </summary>
        public short NestedLogicIndex;
    }
}