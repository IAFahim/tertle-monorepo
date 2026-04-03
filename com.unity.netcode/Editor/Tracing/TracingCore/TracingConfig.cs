using System;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Unity.NetCode.Editor.Tracing
{
    /// <summary>
    /// The shared static that stores the config to enable tracing and filter what components and systems to trace.
    /// </summary>
    internal struct UnmanagedConfig : IDisposable
    {
        public NativeHashSet<ComponentType> RequiredTypesToTrace;
        public NativeHashSet<ComponentType> OptionalTypesToTrace;
        public NativeHashSet<SystemTypeIndex> SystemTypesToTrace;

        private bool m_EnabledTracing;
        private bool m_initialized;
        public bool EnableTracing
        {
            get => m_EnabledTracing && m_initialized;
            set
            {
                if (value)
                    TracingDataAccess.IsProcessed = false;
                m_EnabledTracing = value;
            }
        }

        public void Init()
        {
            EnableTracing = false;

            InitializeComponentTypes();
        }

        void InitializeComponentTypes()
        {
            RequiredTypesToTrace = new(0, Allocator.Persistent);
            OptionalTypesToTrace = new(0, Allocator.Persistent);
            SystemTypesToTrace = new(0, Allocator.Persistent);
            m_initialized = true;
        }

        public void AddRequiredTypeToTrace(ComponentType type)
        {
            if(!m_initialized)
                InitializeComponentTypes();
            if(RequiredTypesToTrace.Contains(type))
            {
                return;
            }
            if (OptionalTypesToTrace.Contains(type))
            {
                Debug.LogWarning($"Type {type} already registered as optional type to trace, it can't also be registered as required.");
                return;
            }
            RequiredTypesToTrace.Add(type);
            AllTypeDiffers.Instance.TryAddTypeDiffer(type);
        }

        public void RemoveRequiredTypeToTrace(ComponentType type)
        {
            if(!m_initialized)
                InitializeComponentTypes();
            RequiredTypesToTrace.Remove(type);
        }

        public void AddOptionalTypeToTrace(ComponentType type)
        {
            if(!m_initialized)
                InitializeComponentTypes();
            if(OptionalTypesToTrace.Contains(type))
            {
                return;
            }
            if (RequiredTypesToTrace.Contains(type))
            {
                Debug.LogWarning($"Type {type} already registered as required type to trace, it can't also be registered as optional.");
                return;
            }
            OptionalTypesToTrace.Add(type);
            AllTypeDiffers.Instance.TryAddTypeDiffer(type);
        }

        public void RemoveOptionalTypeToTrace(ComponentType type)
        {
            if(!m_initialized)
                InitializeComponentTypes();
            OptionalTypesToTrace.Remove(type);
        }

        /// <summary>
        /// Add systems for which a trace will be saved.
        /// If no systems are registered all systems will be traced.
        /// </summary>
        /// <param name="type"></param>
        public void AddSystemTypeToTrace(SystemTypeIndex type)
        {
            if(!m_initialized)
                InitializeComponentTypes();
            if (SystemTypesToTrace.Contains(type))
            {
                Debug.LogWarning($"System type {type} already registered as system to trace.");
                return;
            }
            SystemTypesToTrace.Add(type);
        }

        public void RemoveSystemTypeToTrace(SystemTypeIndex type)
        {
            if(!m_initialized)
                InitializeComponentTypes();
            SystemTypesToTrace.Remove(type);
        }

        public void Dispose()
        {
            if(!m_initialized)
                return;
            if(RequiredTypesToTrace.IsCreated)
                RequiredTypesToTrace.Dispose();
            if(OptionalTypesToTrace.IsCreated)
                OptionalTypesToTrace.Dispose();
            if(SystemTypesToTrace.IsCreated)
                SystemTypesToTrace.Dispose();
            EnableTracing = false;
            m_initialized = false;
        }
    }

    // Scriptable object that stores the user prefrences for the tracing tool. Commented until release to give us flexibility as to where exactly we put it.
    //[FilePath("UserSettings/NetcodeTracingConfig.asset", FilePathAttribute.Location.PreferencesFolder)] We will make this an actual user setting when tracing becomes public
    internal class TracingConfig : ScriptableSingleton<TracingConfig>
    {
        [SerializeField]
        public int TracingMemoryLimitMb;
        [SerializeField]
        public float FuzzyFactor;
        [SerializeField]
        public bool IgnoreDTDiffs;
        [SerializeField]
        public bool IgnorePartialTicks;

        public TracingConfig()
        {
            TracingMemoryLimitMb = 200; // Need to improve raw trace -> trace processing impact on editor perf before we can really increase this limit
            FuzzyFactor = 1e-4f;
            IgnoreDTDiffs = false;
            IgnorePartialTicks = false;
        }
    }
}
