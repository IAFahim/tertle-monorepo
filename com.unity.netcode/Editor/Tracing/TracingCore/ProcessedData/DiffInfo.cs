using System;
using Unity.Collections;
using Unity.NetCode.LowLevel.StateSave;

namespace Unity.NetCode.Editor.Tracing
{
    internal enum TraceType
    {
        Default,
        NetcodeGhostUpdateVsSendComparison, // should check for the other side's version of the associated system for diff
    }

    // when was this trace recorded relative to the target system it's tracing.
    internal  enum TracePosition
    {
        undefined,
        before, // before and after should be mirrored. There should be an after trace for every before trace
        after,
        netcode, // special case for netcode specific traces
    }


    /// <summary>
    /// Stores the result of the diff processing between client and server.
    /// An instance of diffInfo is stored per frame, tick and system.
    /// </summary>
    internal struct DiffInfo : IDisposable
    {
        [Flags]
        public enum DiffReasons : ushort
        {
            Undefined,
            Tick = 1,
            System = 2,
            Data = 4,
            DT = 8,
            ObjectNotFound = 16,
            ObjectEntriesCount = 32,
            Missing = 64,
        }

        public bool HasDiff;
        public DiffReasons DiffReasonFlags;
        NativeList<SavedEntityID> m_ServerMissingObjects;
        NativeList<SavedEntityID> m_ClientMissingObjects;

        public void Add(DiffInfo other)
        {
            HasDiff |= other.HasDiff;
            DiffReasonFlags |= other.DiffReasonFlags;
            if (other.m_ServerMissingObjects.IsCreated)
                MissingObjectsFromServer.AddRange(other.m_ServerMissingObjects.AsArray());
            if (other.m_ClientMissingObjects.IsCreated)
                MissingObjectsFromClient.AddRange(other.m_ClientMissingObjects.AsArray());
        }

        // client objects missing from the server
        public NativeList<SavedEntityID> MissingObjectsFromServer {
            get
            {
                if (!m_ServerMissingObjects.IsCreated)
                    m_ServerMissingObjects = new NativeList<SavedEntityID>(2, Allocator.Persistent); // Only use memory if needed, else it remains uninitialized
                return m_ServerMissingObjects;
            }
        }
        // server objects missing from the client
        public NativeList<SavedEntityID> MissingObjectsFromClient {
            get
            {
                if (!m_ClientMissingObjects.IsCreated)
                    m_ClientMissingObjects = new NativeList<SavedEntityID>(2, Allocator.Persistent); // Only use memory if needed, else it remains uninitialized
                return m_ClientMissingObjects;
            }
        }

        public void Dispose()
        {
            if (m_ClientMissingObjects.IsCreated)
                m_ClientMissingObjects.Dispose();
            if (m_ServerMissingObjects.IsCreated)
                m_ServerMissingObjects.Dispose();
        }
    }
}
