// <copyright file="PauseRequest.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Data.Pause
{
    using Unity.NetCode;

    public struct PauseRequest : IRpcCommand
    {
        public bool Value;
    }
}
