// <copyright file="EditorAudioSourcePoolConfigSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Editor
{
    using BovineLabs.Bridge.Audio;
    using BovineLabs.Bridge.Authoring;
    using BovineLabs.Bridge.Data.Audio;
    using BovineLabs.Core.Editor.Settings;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Groups;
    using Unity.Entities;

    [WorldSystemFilter(WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(AfterTransformSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(AudioSourceAmbiancePoolSystem))]
    public partial class EditorAudioSourcePoolConfigSystem : SystemBase
    {
        private Entity entity;

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            var cameraQuery = SystemAPI.QueryBuilder().WithAllRW<AudioSourcePoolConfig>().Build();

            var configs = cameraQuery.CalculateEntityCount();
            if (configs == 0)
            {
                this.entity = this.EntityManager.CreateEntity<AudioSourcePoolConfig>("Audio Source Pool Config");
            }
            else if (configs > 1)
            {
                this.EntityManager.DestroyEntity(this.entity);
                this.entity = Entity.Null;
            }

            cameraQuery.CompleteDependency();

            var settings = EditorSettingsUtility.GetSettings<BridgeSettings>();
            cameraQuery.GetSingletonRW<AudioSourcePoolConfig>().ValueRW = settings.AudioSourcePoolConfig;
        }
    }
}
#endif
