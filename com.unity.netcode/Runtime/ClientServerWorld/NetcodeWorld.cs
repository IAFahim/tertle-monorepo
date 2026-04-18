using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Assertions;
using Unity.Collections;
using Unity.Entities;
using Unity.Networking.Transport;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Unity.NetCode
{
    /// <summary>
    /// Provides various utility APIs for the current world. Most of these should have an ECS counter part, this class simply provides direct access
    /// to those various components.
    /// Provides shortcuts to its current <see cref="P:Unity.NetCode.Connection" /> if online, its various singletons, etc.
    /// </summary>

    // Design note: The goal is to store as little state as possible here. That state should be stored ECS side in most cases.
    [DebuggerDisplay("{GetDebugName(this)}")]
#if NETCODE_GAMEOBJECT_BRIDGE_EXPERIMENTAL
    public
#endif
    class NetcodeWorld : World
    {
        bool m_Initialized;
        #region query caching
        // TODO-next@potentialOptim singleton pointer caching here too, instead of doing a query all the time
        EntityQuery m_NetworkTimeSingletonQuery;

        #endregion

        internal NetcodeWorld(string name, WorldFlags flags = WorldFlags.Simulation)
            : base(name, flags)
        {
            Initialize();
        }

        internal NetcodeWorld(string name, WorldFlags flags, AllocatorManager.AllocatorHandle backingAllocatorHandle)
            : base(name, flags, backingAllocatorHandle)
        {
            Initialize();
        }

        void Initialize()
        {
            if (!Netcode.Instance.m_ActiveWorld.ExistsAndIsCreated())
                Netcode.Instance.m_ActiveWorld = this;
            m_NetworkTimeSingletonQuery = this.EntityManager.CreateEntityQuery(typeof(NetworkTime));
            m_Initialized = true;
        }

        public NetworkTime NetworkTime => m_Initialized ? m_NetworkTimeSingletonQuery.GetSingleton<NetworkTime>() : default;

        [Conditional("UNITY_ASSERTIONS")]
        private void AssertIsServer()
        {
            Assert.IsTrue(this.IsServer(), "Should only be called from servers or hosts");
        }

        [Conditional("UNITY_ASSERTIONS")]
        private void AssertIsServerOnly()
        {
            Assert.IsTrue(!this.IsClient(), "Should only be called from dedicated servers");
        }

        [Conditional("UNITY_ASSERTIONS")]
        private void AssertIsClientOnly()
        {
            Assert.IsTrue(this.IsClient() && !this.IsHost(), "Should only be called from standalone clients");
        }

        [Conditional("UNITY_ASSERTIONS")]
        private void AssertIsClient()
        {
            Assert.IsTrue(this.IsClient(), "Should only be called from clients or hosts");
        }

        [Conditional("UNITY_ASSERTIONS")]
        private void AssertIsHost()
        {
            Assert.IsTrue(this.IsHost(), "Should only be called from hosts");
        }

        public static string GetDebugName(NetcodeWorld self)
        {
            if (self.ExistsAndIsCreated())
                return $"{(self.IsThinClient() ? "Thin" : "")} {(self.IsClient() ? self.IsServer() ? "Host" : "Client" : "Server")} NetcodeWorld [NetworkTime {self.NetworkTime.ToString()}] {self.Name}";
            else
                return $"Not created";
        }
    }

#if NETCODE_GAMEOBJECT_BRIDGE_EXPERIMENTAL
    public
#endif
    static class NetcodeWorldExtensions
    {
        public static bool ExistsAndIsCreated(this NetcodeWorld world)
        {
            return world != null && world.IsCreated;
        }
    }
}
