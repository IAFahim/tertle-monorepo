// <copyright file="ConditionCompositeAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Authoring.Conditions
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.EntityCommands;
    using BovineLabs.Reaction.Data.Builders;
    using BovineLabs.Reaction.Data.Conditions;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    /// <summary>
    /// Authoring component for composite boolean condition logic.
    /// Use this instead of ConditionAuthoring when you need complex boolean expressions.
    ///
    /// Supports string-based expression parsing with operators: & (AND), | (OR), ^ (XOR), ! (NOT), and parentheses for grouping.
    /// Example expressions: "0 & 1", "(0 | 1) & !2", "(0 & 1) | (!2 & 3)", "0 ^ 1 ^ 2".
    /// Numbers represent condition indices (0-31) that correspond to ConditionActive bit positions.
    ///
    /// The parser creates an abstract syntax tree using ExpressionNode structures, which are then
    /// converted to CompositeLogic blob assets using ConditionCompositeBuilder for runtime evaluation.
    /// </summary>
    [Serializable]
    public class ConditionCompositeAuthoring
    {
        [SerializeField]
        [Tooltip("Parse expression from string notation like '(0 & 1) | (2 & !3)' or '0 ^ 1 ^ 2'. Leave empty to use default all conditions required.")]
        private string expression = string.Empty;

        /// <summary>
        /// Bakes the composite condition logic into the entity.
        /// </summary>
        /// <param name="builder">The builder instance.</param>
        /// <typeparam name="T">The builder type.</typeparam>
        public void Bake<T>(ref T builder)
            where T : struct, IEntityCommands
        {
            BlobAssetReference<CompositeLogic> blobAsset;

            // Use expression string if provided, otherwise fall back to manual groups
            if (string.IsNullOrWhiteSpace(this.expression))
            {
                return;
            }

            try
            {
                blobAsset = ParseExpressionString(this.expression);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse expression '{this.expression}': {ex.Message}");
                return;
            }

            builder.AddBlobAsset(ref blobAsset, out _);
            builder.AddComponent(new ConditionComposite { Logic = blobAsset });
        }

        /// <summary>
        /// Parses an expression string into a CompositeLogic blob asset.
        /// Supports operators: & (AND), | (OR), ^ (XOR), ! (NOT), parentheses for grouping.
        /// Numbers represent condition indices (0-31).
        /// Example: "(0 & 1) | (!2 & 3)" or "0 ^ 1 ^ 2".
        /// </summary>
        /// <param name="expression">The expression string to parse.</param>
        /// <returns>The parsed CompositeLogic blob asset.</returns>
        private static BlobAssetReference<CompositeLogic> ParseExpressionString(string expression)
        {
            var parser = new ExpressionParser(expression);
            return parser.Parse();
        }

        /// <summary>
        /// Recursive descent parser for boolean expressions using temporary managed structures.
        /// Grammar:
        /// Expression  := OrTerm
        /// OrTerm      := XorTerm ('|' XorTerm)*
        /// XorTerm     := AndTerm ('^' AndTerm)*
        /// AndTerm     := NotTerm ('&' NotTerm)*
        /// NotTerm     := '!' Primary | Primary
        /// Primary     := Number | '(' Expression ')'.
        /// </summary>
        private class ExpressionParser
        {
            private readonly string expression;
            private int position;

            public ExpressionParser(string expression)
            {
                this.expression = expression.Replace(" ", string.Empty); // Remove whitespace
                this.position = 0;
            }

            public BlobAssetReference<CompositeLogic> Parse()
            {
                var result = this.ParseOrExpression();

                if (this.position < this.expression.Length)
                {
                    throw new ArgumentException($"Unexpected character '{this.expression[this.position]}' at position {this.position}");
                }

                return result.ToBlobAsset();
            }

            private ExpressionNode ParseOrExpression()
            {
                var left = this.ParseXorExpression();

                if (this.position < this.expression.Length && this.expression[this.position] == '|')
                {
                    var terms = new List<ExpressionNode> { left };

                    while (this.position < this.expression.Length && this.expression[this.position] == '|')
                    {
                        this.position++; // Skip '|'
                        terms.Add(this.ParseXorExpression());
                    }

                    return new ExpressionNode
                    {
                        Type = ExpressionNode.NodeType.Or,
                        Children = terms,
                    };
                }

                return left;
            }

            private ExpressionNode ParseXorExpression()
            {
                var left = this.ParseAndExpression();

                if (this.position < this.expression.Length && this.expression[this.position] == '^')
                {
                    var terms = new List<ExpressionNode> { left };

                    while (this.position < this.expression.Length && this.expression[this.position] == '^')
                    {
                        this.position++; // Skip '^'
                        terms.Add(this.ParseAndExpression());
                    }

                    return new ExpressionNode
                    {
                        Type = ExpressionNode.NodeType.Xor,
                        Children = terms,
                    };
                }

                return left;
            }

            private ExpressionNode ParseAndExpression()
            {
                var left = this.ParseNotExpression();

                if (this.position < this.expression.Length && this.expression[this.position] == '&')
                {
                    var terms = new List<ExpressionNode> { left };

                    while (this.position < this.expression.Length && this.expression[this.position] == '&')
                    {
                        this.position++; // Skip '&'
                        terms.Add(this.ParseNotExpression());
                    }

                    return new ExpressionNode
                    {
                        Type = ExpressionNode.NodeType.And,
                        Children = terms,
                    };
                }

                return left;
            }

            private ExpressionNode ParseNotExpression()
            {
                if (this.position < this.expression.Length && this.expression[this.position] == '!')
                {
                    this.position++; // Skip '!'
                    var operand = this.ParsePrimary();
                    return new ExpressionNode
                    {
                        Type = ExpressionNode.NodeType.Not,
                        Children = new List<ExpressionNode> { operand },
                    };
                }

                return this.ParsePrimary();
            }

            private ExpressionNode ParsePrimary()
            {
                if (this.position >= this.expression.Length)
                {
                    throw new ArgumentException("Unexpected end of expression");
                }

                if (this.expression[this.position] == '(')
                {
                    this.position++; // Skip '('
                    var result = this.ParseOrExpression();

                    if (this.position >= this.expression.Length || this.expression[this.position] != ')')
                    {
                        throw new ArgumentException($"Expected ')' at position {this.position}");
                    }

                    this.position++; // Skip ')'
                    return result;
                }

                // Parse number
                if (char.IsDigit(this.expression[this.position]))
                {
                    var startPos = this.position;
                    while (this.position < this.expression.Length && char.IsDigit(this.expression[this.position]))
                    {
                        this.position++;
                    }

                    var numberStr = this.expression.Substring(startPos, this.position - startPos);
                    if (int.TryParse(numberStr, out var conditionIndex))
                    {
                        if (conditionIndex is < 0 or >= ConditionActive.MaxConditions)
                        {
                            throw new ArgumentException($"Condition index {conditionIndex} is out of range (0-{ConditionActive.MaxConditions - 1})");
                        }

                        return new ExpressionNode
                        {
                            Type = ExpressionNode.NodeType.Condition,
                            ConditionIndex = conditionIndex,
                        };
                    }

                    throw new ArgumentException($"Invalid number '{numberStr}' at position {startPos}");
                }

                throw new ArgumentException($"Unexpected character '{this.expression[this.position]}' at position {this.position}");
            }

            /// <summary>
            /// Represents a node in the abstract syntax tree for boolean expressions during parsing.
            /// This intermediate representation allows building complex nested logic before converting
            /// to the final CompositeLogic blob asset using ConditionCompositeBuilder.
            /// </summary>
            private struct ExpressionNode
            {
                /// <summary>
                /// The type of this expression node (Condition, And, Or, Xor, Not).
                /// </summary>
                public NodeType Type;

                /// <summary>
                /// The condition index (0-31) for Condition nodes.
                /// Only valid when Type is NodeType.Condition.
                /// </summary>
                public int ConditionIndex;

                /// <summary>
                /// Child nodes for compound expressions (And, Or, Xor, Not).
                /// Null for Condition nodes, contains operands for other node types.
                /// </summary>
                public List<ExpressionNode>? Children;

                /// <summary>
                /// Defines the type of expression node in the abstract syntax tree.
                /// Used to determine how to process the node when building the CompositeLogic.
                /// </summary>
                public enum NodeType
                {
                    /// <summary>
                    /// Leaf node representing a single condition index (0-31).
                    /// </summary>
                    Condition,

                    /// <summary>
                    /// Logical AND operation - all children must be true.
                    /// </summary>
                    And,

                    /// <summary>
                    /// Logical OR operation - at least one child must be true.
                    /// </summary>
                    Or,

                    /// <summary>
                    /// Logical XOR operation - exactly one child must be true.
                    /// </summary>
                    Xor,

                    /// <summary>
                    /// Logical NOT operation - inverts the result of its single child.
                    /// </summary>
                    Not,
                }

                /// <summary>
                /// Converts this expression tree to a CompositeLogic blob asset.
                /// </summary>
                /// <returns>A blob asset reference containing the compiled logic structure.</returns>
                public BlobAssetReference<CompositeLogic> ToBlobAsset()
                {
                    var builder = new ConditionCompositeBuilder(Allocator.Temp);
                    builder.BeginGroup(LogicOperation.And);
                    AddNodeToBuilder(ref builder, this);
                    builder.EndGroup();
                    return builder.CreateBlobAsset();
                }

                /// <summary>
                /// Recursively adds an expression node to the ConditionCompositeBuilder.
                /// </summary>
                /// <param name="builder">The builder to add the node to.</param>
                /// <param name="node">The expression node to add.</param>
                private static void AddNodeToBuilder(ref ConditionCompositeBuilder builder, ExpressionNode node)
                {
                    switch (node.Type)
                    {
                        case NodeType.Condition:
                            builder.Add((byte)node.ConditionIndex);
                            break;

                        case NodeType.And:
                            if (node.Children?.Count > 0)
                            {
                                builder.BeginGroup(LogicOperation.And);
                                foreach (var child in node.Children)
                                {
                                    AddNodeToBuilder(ref builder, child);
                                }

                                builder.EndGroup();
                            }

                            break;

                        case NodeType.Or:
                            if (node.Children?.Count > 0)
                            {
                                builder.BeginGroup(LogicOperation.Or);
                                foreach (var child in node.Children)
                                {
                                    AddNodeToBuilder(ref builder, child);
                                }

                                builder.EndGroup();
                            }

                            break;

                        case NodeType.Xor:
                            if (node.Children?.Count > 0)
                            {
                                // Check if all children are simple conditions
                                bool allConditions = true;
                                foreach (var child in node.Children)
                                {
                                    if (child.Type != NodeType.Condition)
                                    {
                                        allConditions = false;
                                        break;
                                    }
                                }

                                if (allConditions)
                                {
                                    // Simple XOR: add all conditions with XOR logic
                                    foreach (var child in node.Children)
                                    {
                                        builder.AddXor((byte)child.ConditionIndex);
                                    }
                                }
                                else
                                {
                                    // Complex XOR: use LogicOperation.Xor to XOR the results of child groups
                                    builder.BeginGroup(LogicOperation.Xor);
                                    foreach (var child in node.Children)
                                    {
                                        AddNodeToBuilder(ref builder, child);
                                    }

                                    builder.EndGroup();
                                }
                            }

                            break;

                        case NodeType.Not:
                            if (node.Children?.Count == 1)
                            {
                                AddNotToBuilder(ref builder, node.Children[0]);
                            }

                            break;
                    }
                }

                /// <summary>
                /// Adds the negation of a node to the builder.
                /// </summary>
                /// <param name="builder">The builder to add to.</param>
                /// <param name="node">The node to negate.</param>
                private static void AddNotToBuilder(ref ConditionCompositeBuilder builder, ExpressionNode node)
                {
                    if (node.Type == NodeType.Condition)
                    {
                        // Simple NOT of a condition
                        builder.AddNot((byte)node.ConditionIndex);
                    }
                    else
                    {
                        // Complex NOT - apply De Morgan's laws
                        switch (node.Type)
                        {
                            case NodeType.And:
                                // !(A & B) = !A | !B
                                if (node.Children?.Count > 0)
                                {
                                    builder.BeginGroup(LogicOperation.Or);
                                    foreach (var child in node.Children)
                                    {
                                        AddNotToBuilder(ref builder, child);
                                    }

                                    builder.EndGroup();
                                }

                                break;

                            case NodeType.Or:
                                // !(A | B) = !A & !B
                                if (node.Children?.Count > 0)
                                {
                                    builder.BeginGroup(LogicOperation.And);
                                    foreach (var child in node.Children)
                                    {
                                        AddNotToBuilder(ref builder, child);
                                    }

                                    builder.EndGroup();
                                }

                                break;

                            case NodeType.Not:
                                // Double negative cancels out
                                if (node.Children?.Count == 1)
                                {
                                    AddNodeToBuilder(ref builder, node.Children[0]);
                                }

                                break;

                            case NodeType.Xor:
                                // !(A ^ B) is complex, fall back to nested structure
                                builder.BeginGroup(LogicOperation.And);
                                builder.BeginGroup(LogicOperation.And);
                                AddNodeToBuilder(ref builder, node);
                                builder.EndGroup();
                                builder.EndGroup();
                                break;
                        }
                    }
                }
            }
        }
    }
}