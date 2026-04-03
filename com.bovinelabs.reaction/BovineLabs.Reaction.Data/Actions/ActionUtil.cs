// <copyright file="ActionUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Actions
{
    using Unity.Entities;

    /// <summary>
    /// Static utility class providing validation methods for action components in the reaction system.
    /// </summary>
    public static class ActionUtil
    {
        /// <summary>
        /// Validates that the specified type info represents a valid enableable component type for use in reaction actions.
        /// </summary>
        /// <param name="typeInfo">The type information to validate.</param>
        /// <returns>True if the type is a valid enableable component; otherwise, false.</returns>
        public static bool ValidateTypeInfoForEnableable(TypeManager.TypeInfo typeInfo)
        {
            if (!typeInfo.TypeIndex.IsEnableable)
            {
                return false;
            }

            if (typeInfo.Category != TypeManager.TypeCategory.ComponentData &&
                typeInfo.Category != TypeManager.TypeCategory.BufferData)
            {
                return false;
            }

            if (typeInfo.TypeIndex.IsManagedComponent)
            {
                return false;
            }

            return typeInfo.Type != null;
        }
    }
}
