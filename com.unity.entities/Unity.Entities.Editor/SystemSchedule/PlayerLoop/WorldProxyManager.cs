using System;
using System.Collections.Generic;

namespace Unity.Entities.Editor
{
    internal class WorldProxyManager : IDisposable
    {
        readonly Dictionary<World, WorldProxy> m_WorldProxyForLocalWorldsDict = new ();
        List<WorldProxy> m_WorldProxies = new ();
        List<WorldProxyUpdater> m_ProxyUpdaters = new ();
        
        public bool IsFullPlayerLoop { get; set; }
        
        WorldProxy m_SelectedWorldProxy;

        public WorldProxy GetWorldProxyForGivenWorld(World world)
        {
            if (world == null || !world.IsCreated)
                throw new ArgumentNullException(nameof(world));

            if (m_WorldProxyForLocalWorldsDict.TryGetValue(world, out var worldProxy))
                return worldProxy;

            throw new ArgumentException($"WorldProxy for given world {world.Name} does not exist or is null");
        }

        public bool TryGetWorldProxy(World world, out WorldProxy proxy)
        {
            proxy = null;
            if (world == null || !world.IsCreated)
                return false;
            return m_WorldProxyForLocalWorldsDict.TryGetValue(world, out proxy);
        }

        public IReadOnlyList<WorldProxyUpdater> GetAllWorldProxyUpdaters() => m_ProxyUpdaters.AsReadOnly();

        public void SetSelectedWorldProxy(WorldProxy proxy)
        {
            if (m_SelectedWorldProxy != null && m_SelectedWorldProxy.Equals(proxy))
                return;

            m_SelectedWorldProxy = proxy;
            SetActiveUpdater();
        }
        
        void SetActiveUpdater()
        {
            if (m_SelectedWorldProxy == null)
                return;

            if (IsFullPlayerLoop)
            {
                foreach (var updater in m_ProxyUpdaters)
                    updater.EnableUpdater();
                return;
            }

            for (var i = 0; i < m_WorldProxies.Count; ++i)
            {
                var worldProxy = m_WorldProxies[i];
                var updater = m_ProxyUpdaters[i];

                if (worldProxy.Equals(m_SelectedWorldProxy))
                    updater.EnableUpdater();
                else
                    updater.DisableUpdater();
            }
        }        

        public void CreateWorldProxiesForAllWorlds()
        {
            foreach (var world in World.All)
            {
                if (m_WorldProxyForLocalWorldsDict.ContainsKey(world))
                    continue;

                CreateWorldProxy(world);
            }

            CleanUpWorldProxyDictionary();
        }

        int GetProxyIndexFromWorldSequence(ulong sequenceNumber) => m_WorldProxies.FindIndex(x => x.SequenceNumber == sequenceNumber);

        public void RebuildWorldProxyForGivenWorld(World world)
        {
            CreateWorldProxy(world);
            
            var proxyIndex = GetProxyIndexFromWorldSequence(world.SequenceNumber);
            if (proxyIndex != -1)
                m_ProxyUpdaters[proxyIndex].ResetWorldProxy();
        }

        void CreateWorldProxy(World world)
        {
            if (world == null || !world.IsCreated)
                throw new ArgumentNullException(nameof(world));
            // Skip streaming worlds, as they shouldn't be displayed in the Systems window.
            if (world.Flags.HasFlag(WorldFlags.Streaming))
                return;

            CleanUpWorldProxyDictionary();

            if (m_WorldProxyForLocalWorldsDict.TryGetValue(world, out var worldProxy))
                return;

            worldProxy = new WorldProxy(world.SequenceNumber);
            var updater = new WorldProxyUpdater(world, worldProxy);
            updater.PopulateWorldProxy();
            if (IsFullPlayerLoop)
                updater.EnableUpdater();

            m_WorldProxyForLocalWorldsDict.Add(world, worldProxy);
            m_WorldProxies.Add(worldProxy);
            m_ProxyUpdaters.Add(updater);
        }

        void CleanUpWorldProxyDictionary()
        {
            var worldsToRemove = new List<World>();
            foreach (var (world, proxy) in m_WorldProxyForLocalWorldsDict)
            {
                if (world.IsCreated)
                    continue;
                var proxyIndex = GetProxyIndexFromWorldSequence(proxy.SequenceNumber);
                
                m_ProxyUpdaters[proxyIndex].DisableUpdater();
                
                m_ProxyUpdaters.RemoveAt(proxyIndex);
                m_WorldProxies.RemoveAt(proxyIndex);
                
                worldsToRemove.Add(world);
            }
            
            foreach (var world in worldsToRemove)
                m_WorldProxyForLocalWorldsDict.Remove(world);
        }

        public void Dispose()
        {
            foreach (var updater in m_ProxyUpdaters)
                updater.DisableUpdater();
        }
    }
}
