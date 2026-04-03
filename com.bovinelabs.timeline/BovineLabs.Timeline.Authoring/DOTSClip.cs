// <copyright file="DOTSClip.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Timeline.Authoring
{
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    /// <summary>
    /// Base class for all DOTS-compatible timeline clips.
    /// Extends Unity's PlayableAsset with ECS baking functionality.
    /// </summary>
    public abstract class DOTSClip : PlayableAsset
    {
        /// <inheritdoc />
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return Playable.Create(graph);
        }

        /// <summary>
        /// Creates an entity from this clip using the provided baking context.
        /// Override this method to change the default clip entity creation behavior,
        /// or return Entity.Null when baking is not required for this clip.
        /// </summary>
        /// <param name="context">The current baking context.</param>
        /// <returns>The clip entity or Entity.Null when baking is not required.</returns>
        public Entity CreateClipEntity(BakingContext context)
        {
            return context.CreateClipEntity();
        }

        /// <summary>
        /// Override this method to add additional components to the clip entity.
        /// Called after the base clip components have been added.
        /// </summary>
        /// <param name="clipEntity">The entity representing this clip.</param>
        /// <param name="context">The baking context.</param>
        public virtual void Bake(Entity clipEntity, BakingContext context)
        {
        }
    }
}
