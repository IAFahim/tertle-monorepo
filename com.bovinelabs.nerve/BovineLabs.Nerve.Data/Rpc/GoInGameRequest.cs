// <copyright file="GoInGameRequest.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Data.Rpc
{
    using Unity.NetCode;

    /// <summary> RPC request from client to server for game to go "in game" and send snapshots / inputs. </summary>
    public struct GoInGameRequest : IRpcCommand
    {
    }
}
