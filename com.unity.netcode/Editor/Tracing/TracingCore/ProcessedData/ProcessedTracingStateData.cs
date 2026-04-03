using System;
using Unity.Collections;
using Unity.NetCode.LowLevel.StateSave;
using UnityEngine.Assertions;

namespace Unity.NetCode.Editor.Tracing
{
    internal struct StateID : IEquatable<StateID>
    {
        public int ID;
        public const int NetcodeID = -1;
        public static readonly StateID DefaultNetcode = new StateID() { ID = NetcodeID };

        // useful for adding traces inside a system. By default, there is one state per system position. If you want to add more than one for
        // debugging purposes, assign your own custom ID when adding traces
        public StateID(int customID)
        {
            ID = customID;
        }

        public bool Equals(StateID other)
        {
            return ID == other.ID;
        }

        public override bool Equals(object obj)
        {
            return obj is StateID other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ID;
        }

        public override string ToString()
        {
            return $"{ID}{(ID == NetcodeID ? "(DefaultNetcode)" : "")}";
        }
    }

    internal struct ProcessedTracingStateData : IDisposable
    {
        public RawTracingStateSave m_RawStateSave;
        public DiffInfo DiffInfo;
        public bool Initialized;

        public ProcessedTracingStateData(RawTracingStateSave rawStateSave)
        {
            DiffInfo = default;
            Initialized = true;
            m_RawStateSave = rawStateSave;
        }

        public void Dispose()
        {
            Assert.IsTrue(Initialized, "Initialized");
            DiffInfo.Dispose();
            Initialized = false;
        }

        public unsafe bool ProcessDiff(ref ProcessedTracingStateData authoritativeValue)
        {
            WorldStateSave authoritativeStateSave = authoritativeValue.m_RawStateSave.stateSave;
            WorldStateSave currentStateSave = this.m_RawStateSave.stateSave;
            foreach (var objectID in authoritativeStateSave.GetAllEntities(Allocator.Temp))
            {
                if (!currentStateSave.Exists(objectID))
                {
                    DiffInfo.HasDiff = true;
                    DiffInfo.DiffReasonFlags |= DiffInfo.DiffReasons.ObjectNotFound;
                    DiffInfo.MissingObjectsFromClient.Add(objectID);
                    authoritativeValue.DiffInfo.HasDiff = true;
                    authoritativeValue.DiffInfo.DiffReasonFlags |= DiffInfo.DiffReasons.ObjectNotFound;
                    authoritativeValue.DiffInfo.MissingObjectsFromClient.Add(objectID);
                    continue;
                }

                using var currentTypes = currentStateSave.GetComponentTypes(objectID);
                using var authoritativeTypes = authoritativeStateSave.GetComponentTypes(objectID);

                for (int i = 0; i < authoritativeTypes.Length; i++)
                {
                    if (i >= currentTypes.Length)
                    {
                        DiffInfo.HasDiff = authoritativeValue.DiffInfo.HasDiff = true;
                        DiffInfo.DiffReasonFlags |= DiffInfo.DiffReasons.ObjectEntriesCount;
                        authoritativeValue.DiffInfo.DiffReasonFlags = DiffInfo.DiffReasons.ObjectEntriesCount;
                        break;
                    }
                    var traceEntryType = currentTypes[i];
                    currentStateSave.TryGetComponentData(objectID, authoritativeTypes[i], out var leftBytes);
                    authoritativeStateSave.TryGetComponentData(objectID, authoritativeTypes[i], out var rightBytes);
                    AllTypeDiffers.Instance.TryAddTypeDiffer(traceEntryType); // necessary since state save itself can add types to the save (like ghost instance when using the indexing save strategy).
                    if (AllTypeDiffers.Instance.AllDiffers[traceEntryType].ProcessDiff(TracingConfig.instance.FuzzyFactor, leftBytes, rightBytes))
                    {
                        DiffInfo.HasDiff = authoritativeValue.DiffInfo.HasDiff = true;
                        DiffInfo.DiffReasonFlags |= DiffInfo.DiffReasons.Data;
                        authoritativeValue.DiffInfo.DiffReasonFlags |= DiffInfo.DiffReasons.Data;
                    }
                }
            }

            foreach (var clientObjectID in currentStateSave.GetAllEntities(Allocator.Temp))
            {
                if (!authoritativeStateSave.Exists(clientObjectID))
                {
                    DiffInfo.HasDiff = true;
                    DiffInfo.DiffReasonFlags |= DiffInfo.DiffReasons.ObjectNotFound;
                    DiffInfo.MissingObjectsFromServer.Add(clientObjectID);
                    authoritativeValue.DiffInfo.HasDiff = true;
                    authoritativeValue.DiffInfo.DiffReasonFlags |= DiffInfo.DiffReasons.ObjectNotFound;
                    authoritativeValue.DiffInfo.MissingObjectsFromServer.Add(clientObjectID);
                }
            }

            return DiffInfo.HasDiff;
        }
    }
}
