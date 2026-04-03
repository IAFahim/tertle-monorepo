// <copyright file="TargetsCustom.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Data.Core
{
    using Unity.Entities;

    /// <summary>
    /// Component storing additional custom target entity references (Target0, Target1) for extended reaction targeting.
    /// </summary>
    public struct TargetsCustom : IComponentData
    {
        // Changing these outside of initialization is breaking
        public Entity Target0;
        public Entity Target1;
    }
}
