// <copyright file="CinemachineThirdPersonFollowDots.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && UNITY_PHYSICS
namespace BovineLabs.Bridge.Data.Cinemachine
{
    using System;
    using Unity.Cinemachine;
    using Unity.Entities;
    using Unity.Physics;
#if UNITY_PHYSICS_CUSTOM
    using Unity.Physics.Authoring;
#endif
    using UnityEngine;

    /// <summary>
    /// Third-person follower, with complex pivoting: horizontal about the origin,
    /// vertical about the shoulder.
    /// </summary>
    [AddComponentMenu("Cinemachine/Procedural/Position Control/Cinemachine Third Person Follow Dots")]
    [SaveDuringPlay]
    [CameraPipeline(CinemachineCore.Stage.Body)]
    public class CinemachineThirdPersonFollowDots : CinemachineComponentBase, CinemachineFreeLookModifier.IModifierValueSource,
        CinemachineFreeLookModifier.IModifiablePositionDamping, CinemachineFreeLookModifier.IModifiableDistance
    {
        /// <summary>
        /// How responsively the camera tracks the target.  Each axis (camera-local)
        /// can have its own setting.  Value is the approximate time it takes the camera
        /// to catch up to the target's new position.  Smaller values give a more rigid
        /// effect, larger values give a squishier one.
        /// </summary>
        [Tooltip("How responsively the camera tracks the target.  Each axis (camera-local) " +
            "can have its own setting.  Value is the approximate time it takes the camera " +
            "to catch up to the target's new position.  Smaller values give a more " +
            "rigid effect, larger values give a squishier one")]
        public Vector3 Damping;

        /// <summary>
        /// Position of the shoulder pivot relative to the Follow target origin.
        /// This offset is in target-local space.
        /// </summary>
        [Header("Rig")]
        [Tooltip("Position of the shoulder pivot relative to the Follow target origin.  " + "This offset is in target-local space")]
        public Vector3 ShoulderOffset;

        /// <summary>
        /// Vertical offset of the hand in relation to the shoulder.
        /// Arm length will affect the follow target's screen position
        /// when the camera rotates vertically.
        /// </summary>
        [Tooltip("Vertical offset of the hand in relation to the shoulder.  " +
            "Arm length will affect the follow target's screen position when " +
            "the camera rotates vertically")]
        public float VerticalArmLength;

        /// <summary> Specifies which shoulder (left, right, or in-between) the camera is on. </summary>
        [Tooltip("Specifies which shoulder (left, right, or in-between) the camera is on")]
        [Range(0, 1)]
        public float CameraSide;

        /// <summary> How far behind the hand the camera will be placed. </summary>
        [Tooltip("How far behind the hand the camera will be placed")]
        public float CameraDistance;

        /// <summary> If enabled, camera will be pulled in front of occluding obstacles. </summary>
        [FoldoutWithEnabledButton]
        public ObstacleSettings AvoidObstacles = ObstacleSettings.Default;

        private float camPosCollisionCorrection;
        private Vector3 dampingCorrection; // this is in local rig space

        // State info
        private Vector3 previousFollowTargetPosition;

        internal World World { get; set; }

        internal Entity PhysicsWorldEntity { get; set; }

        /// <summary> True if component is enabled and has a Follow target defined </summary>
        public override bool IsValid => this.enabled && this.FollowTarget != null;

        /// <summary>
        /// Get the Cinemachine Pipeline stage that this component implements.
        /// Always returns the Aim stage
        /// </summary>
        public override CinemachineCore.Stage Stage => CinemachineCore.Stage.Body;

        float CinemachineFreeLookModifier.IModifierValueSource.NormalizedModifierValue
        {
            get
            {
                var up = this.VirtualCamera.State.ReferenceUp;
                var rot = this.FollowTargetRotation;
                var a = Vector3.SignedAngle(rot * Vector3.up, up, rot * Vector3.right);
                return Mathf.Clamp(a, -90, 90) / -90;
            }
        }

        Vector3 CinemachineFreeLookModifier.IModifiablePositionDamping.PositionDamping
        {
            get => this.Damping;
            set => this.Damping = value;
        }

        float CinemachineFreeLookModifier.IModifiableDistance.Distance
        {
            get => this.CameraDistance;
            set => this.CameraDistance = value;
        }

        private void Reset()
        {
            this.ShoulderOffset = new Vector3(0.5f, -0.4f, 0.0f);
            this.VerticalArmLength = 0.4f;
            this.CameraSide = 1.0f;
            this.CameraDistance = 2.0f;
            this.Damping = new Vector3(0.1f, 0.5f, 0.3f);
            this.AvoidObstacles = ObstacleSettings.Default;
        }

        private void OnValidate()
        {
            this.CameraSide = Mathf.Clamp(this.CameraSide, -1.0f, 1.0f);
            this.Damping.x = Mathf.Max(0, this.Damping.x);
            this.Damping.y = Mathf.Max(0, this.Damping.y);
            this.Damping.z = Mathf.Max(0, this.Damping.z);
            this.AvoidObstacles.CameraRadius = Mathf.Max(0.001f, this.AvoidObstacles.CameraRadius);
            this.AvoidObstacles.DampingIntoCollision = Mathf.Max(0, this.AvoidObstacles.DampingIntoCollision);
            this.AvoidObstacles.DampingFromCollision = Mathf.Max(0, this.AvoidObstacles.DampingFromCollision);
        }

        /// <summary>
        /// Report maximum damping time needed for this component.
        /// </summary>
        /// <returns> Highest damping setting in this component </returns>
        public override float GetMaxDampTime()
        {
            return Mathf.Max(this.AvoidObstacles.Enabled ? Mathf.Max(this.AvoidObstacles.DampingIntoCollision, this.AvoidObstacles.DampingFromCollision) : 0,
                Mathf.Max(this.Damping.x, Mathf.Max(this.Damping.y, this.Damping.z)));
        }

        /// <summary> Orients the camera to match the Follow target's orientation </summary>
        /// <param name="curState"> The current camera state </param>
        /// <param name="deltaTime">
        /// Elapsed time since last frame, for damping calculations.
        /// If negative, previous state is reset.
        /// </param>
        public override void MutateCameraState(ref CameraState curState, float deltaTime)
        {
            if (this.IsValid)
            {
                if (!this.VirtualCamera.PreviousStateIsValid)
                {
                    deltaTime = -1;
                }

                this.PositionCamera(ref curState, deltaTime);
            }
        }

        /// <summary>
        /// This is called to notify the user that a target got warped,
        /// so that we can update its internal state to make the camera
        /// also warp seamlessly.
        /// </summary>
        /// <param name="target"> The object that was warped </param>
        /// <param name="positionDelta"> The amount the target's position changed </param>
        public override void OnTargetObjectWarped(Transform target, Vector3 positionDelta)
        {
            base.OnTargetObjectWarped(target, positionDelta);
            if (target == this.FollowTarget)
            {
                this.previousFollowTargetPosition += positionDelta;
            }
        }

        private void PositionCamera(ref CameraState curState, float deltaTime)
        {
            var up = curState.ReferenceUp;
            var targetPos = this.FollowTargetPosition;
            var targetRot = this.FollowTargetRotation;
            var targetForward = targetRot * Vector3.forward;
            var heading = GetHeading(targetRot, up);

            if (deltaTime < 0)
            {
                // No damping - reset damping state info
                this.dampingCorrection = Vector3.zero;
                this.camPosCollisionCorrection = 0;
            }
            else
            {
                // Damping correction is applied to the shoulder offset - stretching the rig
                this.dampingCorrection += Quaternion.Inverse(heading) * (this.previousFollowTargetPosition - targetPos);
                this.dampingCorrection -= this.VirtualCamera.DetachedFollowTargetDamp(this.dampingCorrection, this.Damping, deltaTime);
            }

            this.previousFollowTargetPosition = targetPos;
            var root = targetPos;
            this.GetRawRigPositions(root, targetRot, heading, out _, out var hand);

            // Place the camera at the correct distance from the hand
            var camPos = hand - (targetForward * (this.CameraDistance - this.dampingCorrection.z));

            if (this.AvoidObstacles.Enabled)
            {
                // Check if hand is colliding with something, if yes, then move the hand
                // closer to the player. The radius is slightly enlarged, to avoid problems
                // next to walls
                float dummy = 0;
                var collidedHand = this.ResolveCollisions(root, hand, -1, this.AvoidObstacles.CameraRadius * 1.05f, ref dummy);
                camPos = this.ResolveCollisions(collidedHand, camPos, deltaTime, this.AvoidObstacles.CameraRadius, ref this.camPosCollisionCorrection);
            }

            // Set state
            curState.RawPosition = camPos;
            curState.RawOrientation = targetRot; // not necessary, but left in to avoid breaking scenes that depend on this
        }

        /// <summary>
        /// Internal use only.  Public for the inspector gizmo
        /// </summary>
        /// <param name="root"> Root of the rig. </param>
        /// <param name="shoulder"> Shoulder of the rig. </param>
        /// <param name="hand"> Hand of the rig. </param>
        public void GetRigPositions(out Vector3 root, out Vector3 shoulder, out Vector3 hand)
        {
            var up = this.VirtualCamera.State.ReferenceUp;
            var targetRot = this.FollowTargetRotation;
            var heading = GetHeading(targetRot, up);
            root = this.previousFollowTargetPosition;
            this.GetRawRigPositions(root, targetRot, heading, out shoulder, out hand);
            if (this.AvoidObstacles.Enabled)
            {
                float dummy = 0;
                hand = this.ResolveCollisions(root, hand, -1, this.AvoidObstacles.CameraRadius * 1.05f, ref dummy);
            }
        }

        internal static Quaternion GetHeading(Quaternion targetRot, Vector3 up)
        {
            var targetForward = targetRot * Vector3.forward;
            var planeForward = Vector3.Cross(up, Vector3.Cross(targetForward.ProjectOntoPlane(up), up));
            if (planeForward.AlmostZero())
            {
                planeForward = Vector3.Cross(targetRot * Vector3.right, up);
            }

            return Quaternion.LookRotation(planeForward, up);
        }

        private void GetRawRigPositions(Vector3 root, Quaternion targetRot, Quaternion heading, out Vector3 shoulder, out Vector3 hand)
        {
            var shoulderOffset = this.ShoulderOffset;
            shoulderOffset.x = Mathf.Lerp(-shoulderOffset.x, shoulderOffset.x, this.CameraSide);
            shoulderOffset.x += this.dampingCorrection.x;
            shoulderOffset.y += this.dampingCorrection.y;
            shoulder = root + (heading * shoulderOffset);
            hand = shoulder + (targetRot * new Vector3(0, this.VerticalArmLength, 0));
        }

        private Vector3 ResolveCollisions(Vector3 root, Vector3 tip, float deltaTime, float cameraRadius, ref float collisionCorrection)
        {
            // Subscene and hybrid component shenanigans can cause issues here so let's just avoid failures
            if (this.World is not { IsCreated: true })
            {
                return tip;
            }

#if UNITY_PHYSICS_CUSTOM
            if (this.AvoidObstacles.BelongsTo.Value == 0 || this.AvoidObstacles.CollidesWith.Value == 0)
            {
                return tip;
            }
#endif

            var dir = tip - root;
            var len = dir.magnitude;
            dir /= len;

            var result = tip;
            float desiredCorrection = 0;

            var physicsWorld = this.World.EntityManager.GetComponentData<PhysicsWorldSingleton>(this.PhysicsWorldEntity).PhysicsWorld;

            var collisionFilter = new CollisionFilter
            {
#if UNITY_PHYSICS_CUSTOM
                BelongsTo = this.AvoidObstacles.BelongsTo.Value,
                CollidesWith = this.AvoidObstacles.CollidesWith.Value,
#else
                BelongsTo = (uint)this.AvoidObstacles.BelongsTo.value,
                CollidesWith = (uint)this.AvoidObstacles.CollidesWith.value,
#endif
            };

            if (physicsWorld.SphereCast(root, cameraRadius, dir, len, out var hitInfo, collisionFilter))
            {
                Vector3 desiredResult = hitInfo.Position + (hitInfo.SurfaceNormal * cameraRadius);
                desiredCorrection = (desiredResult - tip).magnitude;
            }

            collisionCorrection += deltaTime < 0
                ? desiredCorrection - collisionCorrection
                : Damper.Damp(desiredCorrection - collisionCorrection,
                    desiredCorrection > collisionCorrection ? this.AvoidObstacles.DampingIntoCollision : this.AvoidObstacles.DampingFromCollision, deltaTime);

            // Apply the correction
            if (collisionCorrection > Epsilon)
            {
                result -= dir * collisionCorrection;
            }

            return result;
        }

        /// <summary>
        /// Holds settings for collision resolution.
        /// </summary>
        [Serializable]
        public struct ObstacleSettings
        {
            /// <summary>
            /// Enable or disable obstacle handling.
            /// If enabled, camera will be pulled in front of occluding obstacles.
            /// </summary>
            [Tooltip("If enabled, camera will be pulled in front of occluding obstacles")]
            public bool Enabled;

#if UNITY_PHYSICS_CUSTOM
            /// <summary> Camera will avoid obstacles on these layers. </summary>
            [Tooltip("Camera will avoid obstacles on these layers")]
            public PhysicsCategoryTags BelongsTo;

            public PhysicsCategoryTags CollidesWith;
#else
            public LayerMask BelongsTo;
            public LayerMask CollidesWith;
#endif

            /// <summary>
            /// Specifies how close the camera can get to obstacles
            /// </summary>
            [Tooltip("Specifies how close the camera can get to obstacles")]
            [Range(0, 1)]
            public float CameraRadius;

            /// <summary>
            /// How gradually the camera moves to correct for occlusions.
            /// Higher numbers will move the camera more gradually.
            /// </summary>
            [Range(0, 10)]
            [Tooltip("How gradually the camera moves to correct for occlusions.  " + "Higher numbers will move the camera more gradually.")]
            public float DampingIntoCollision;

            /// <summary>
            /// How gradually the camera returns to its normal position after having been corrected by the built-in
            /// collision resolution system. Higher numbers will move the camera more gradually back to normal.
            /// </summary>
            [Range(0, 10)]
            [Tooltip("How gradually the camera returns to its normal position after having been corrected by the built-in " +
                "collision resolution system.  Higher numbers will move the camera more gradually back to normal.")]
            public float DampingFromCollision;

            internal static ObstacleSettings Default => new()
            {
                Enabled = false,
#if UNITY_PHYSICS_CUSTOM
                BelongsTo = PhysicsCategoryTags.Everything,
                CollidesWith = PhysicsCategoryTags.Everything,
#else
                BelongsTo = new LayerMask {value = int.MinValue},
                CollidesWith = new LayerMask {value = int.MinValue},
#endif
                CameraRadius = 0.2f,
                DampingIntoCollision = 0,
                DampingFromCollision = 0.5f,
            };
        }
    }
}
#endif