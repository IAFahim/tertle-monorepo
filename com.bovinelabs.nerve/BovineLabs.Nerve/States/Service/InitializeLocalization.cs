// <copyright file="InitializeLocalization.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_LOCALIZATION
namespace BovineLabs.Nerve.States.Service
{
    using System.Threading;
    using System.Threading.Tasks;
    using BovineLabs.Nerve.Data;
    using Unity.Entities;
    using UnityEngine.Localization.Settings;
    using UnityEngine.Scripting;

    [Preserve]
    public class InitializeLocalization : IInitTask
    {
        public async Task<bool> Initialize(World world, CancellationToken cancellation)
        {
            await LocalizationSettings.InitializationOperation.Task;
            return true;
        }
    }
}
#endif
