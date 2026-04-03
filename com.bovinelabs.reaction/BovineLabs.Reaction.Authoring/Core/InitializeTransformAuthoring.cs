// <copyright file="InitializeTransformAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Reaction.Authoring.Core
{
    using BovineLabs.Core.Authoring.LifeCycle;
    using BovineLabs.Core.Authoring.ObjectManagement;
    using BovineLabs.Reaction.Data.Core;
    using UnityEngine;
    using UnityEngine.Serialization;

    /// <summary>
    /// Authoring component for configuring initial transform properties when entities are instantiated.
    /// Allows setting position, rotation, and scale based on source and target entities.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ObjectDefinitionAuthoring), typeof(LifeCycleAuthoring), typeof(TargetsAuthoring))]
    public class InitializeTransformAuthoring : LookupAuthoring<InitializeTransform, InitializeTransform.Data>
    {
        [Header("Targets")]
        [FormerlySerializedAs("Source")] // TODO remove
        [Tooltip("The Target to be considered From when setting the transform values.")]
        public Target From = Target.Owner;

        [FormerlySerializedAs("Destination")] // TODO remove
        [Tooltip("The Target to be considered To when setting the transform values.")]
        public Target To = Target.Target;

        [Header("Transform")]
        [Tooltip("None - Don't set the position.\n" +
                 "From - Set the position to the From target.\n" +
                 "To - Set the position to the To target.")]
        public InitializeTransform.Data.PositionInit Position = InitializeTransform.Data.PositionInit.From;

        [Tooltip("None - Don't set the rotation.\n" +
                 "From - Set the rotation to the From target.\n" +
                 "To - Set the rotation to the To target.\n" +
                 "Direction - Set the forward to To - From direction using the From up.\n" +
                 "DirectionInverse - Set the forward to From - To direction using the To up.")]
        public InitializeTransform.Data.RotationInit Rotation = InitializeTransform.Data.RotationInit.None;

        [Tooltip("None - Don't set the scale.\n" +
                 "From - Set the scale to the From target.\n" +
                 "To - Set the scale to the To target.\n" +
                 "Distance - Set the scale to the distance between From and To.")]
        public InitializeTransform.Data.ScaleInit Scale = InitializeTransform.Data.ScaleInit.None;

        [Tooltip("If true, the entities LocalTransform will be applied as if it was in local space. If false, any LocalTransform will be discarded. " +
                 "This can either be set from baking or from the system that is instantiating the entity and is useful for setting an offset.")]
        public bool ApplyInitialTransform = true;

        /// <inheritdoc/>
        public override bool TryGetInitialization(out InitializeTransform.Data value)
        {
            value = new InitializeTransform.Data
            {
                From = this.From,
                To = this.To,
                ApplyInitialTransform = this.ApplyInitialTransform,
                Position = this.Position,
                Rotation = this.Rotation,
                Scale = this.Scale,
            };

            return true;
        }
    }
}
