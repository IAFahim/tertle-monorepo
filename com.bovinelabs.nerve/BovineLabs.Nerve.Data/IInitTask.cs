// <copyright file="IInitTask.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Data
{
    using System.Threading;
    using System.Threading.Tasks;
    using Unity.Entities;

    public interface IInitTask
    {
        public Task<bool> Initialize(World world, CancellationToken cancellation);
    }
}
