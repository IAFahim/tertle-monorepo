// <copyright file="StatAuthoringType.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Authoring
{
    /// <summary>
    /// For design we ensure only positive values and more clear options.
    /// These are converted into StatModifyType and negatives added during conversion.
    /// </summary>
    public enum StatAuthoringType : byte
    {
        Added = 0,
        Subtracted = 1,
        Increased = 2,
        Reduced = 3,
        More = 4,
        Less = 5,
    }
}
