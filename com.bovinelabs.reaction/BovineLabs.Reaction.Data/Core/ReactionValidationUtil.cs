// <copyright file="ReactionValidationUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Core
{
    using Unity.Entities;

    /// <summary>
    /// Static utility class providing validation methods for reaction components, particularly for tag components and stable type hashes.
    /// </summary>
    public static class ReactionValidationUtil
    {
        // This check is similar to StableTypeHashAttributeDrawer but not identical
        /// <summary>
        /// Validates that the specified stable type hash represents a valid tag component type for use in reactions.
        /// </summary>
        /// <param name="stableHash">The stable type hash to validate.</param>
        /// <returns>True if the hash represents a valid tag component; otherwise, false.</returns>
        public static bool ValidateStableHashForTag(ulong stableHash)
        {
            var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(stableHash);

            if (typeIndex == -1)
            {
                return false;
            }

            var typeInfo = TypeManager.GetTypeInfo(typeIndex);
            return ValidateTypeInfoForTag(typeInfo);
        }

        /// <summary>
        /// Validates that the specified type info represents a valid tag component type for use in reactions.
        /// </summary>
        /// <param name="typeInfo">The type information to validate.</param>
        /// <returns>True if the type is a valid tag component; otherwise, false.</returns>
        public static bool ValidateTypeInfoForTag(TypeManager.TypeInfo typeInfo)
        {
            if (!typeInfo.IsZeroSized)
            {
                return false;
            }

            if (typeInfo.Category != TypeManager.TypeCategory.ComponentData)
            {
                return false;
            }

            var type = typeInfo.Type;

            if (type == null)
            {
                return false;
            }

            if (type.IsClass || type.ContainsGenericParameters)
            {
                return false;
            }

            if (type.Namespace != null && type.Namespace.StartsWith("Unity"))
            {
                return false;
            }

            return true;
        }
    }
}
