// <copyright file="IntrinsicDefault.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Essence.Authoring
{
    using System;

    /// <summary>
    /// Configuration class for defining a default intrinsic value.
    /// Used to specify the initial value for a particular intrinsic type when configuring entities.
    /// </summary>
    [Serializable]
    public class IntrinsicDefault
    {
        public IntrinsicSchemaObject? Intrinsic;
        public int Value;
    }
}
