using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode.LowLevel.StateSave;

namespace Unity.NetCode.Editor.Tracing
{
    internal struct WorldID : IEquatable<WorldID>, IComparer<WorldID>
    {
        public enum WorldType
        {
            Undefined,
            Client,
            Server
        }

        public ulong value;

        public WorldID(WorldUnmanaged world)
        {
            value = world.SequenceNumber;
        }

        public bool Equals(WorldID other)
        {
            return value == other.value;
        }

        public int Compare(WorldID x, WorldID y)
        {
            return x.value.CompareTo(y.value);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            string toRet = $"world[{value}]";
            toRet = ToWorld().Name;
            return toRet;
        }

        public World ToWorld()
        {
            foreach (var world in World.All)
            {
                if (world.SequenceNumber == this.value)
                {
                    return world;
                }
            }

            return null;
        }
    }

    internal struct WorldData : IDisposable
    {
        public NativeList<FrameID> FrameIDs;
        public NativeHashMap<FrameID, FrameData> PerFrameData;
        public NativeList<TickID> TickIDs;
        public NativeHashMap<TickID, TickData> PerTickData;
        public NativeHashMap<SavedEntityID, FixedString64Bytes> GhostNames;
        public DiffInfo DiffInfo;
        public bool IsCreated;

        public void Dispose()
        {
            if (!IsCreated)
                return;
            if (PerFrameData.IsCreated)
            {
                FrameIDs.Dispose();
                foreach (var pair in PerFrameData)
                {
                    pair.Value.Dispose();
                }

                PerFrameData.Clear();
                PerFrameData.Dispose();
            }

            if (PerTickData.IsCreated)
            {
                TickIDs.Dispose();
                foreach (var pair in PerTickData)
                {
                    pair.Value.Dispose();
                }

                PerTickData.Clear();
                PerTickData.Dispose();
            }

            DiffInfo.Dispose();
            IsCreated = false;
        }

        public WorldData(Allocator allocator)
        {
            FrameIDs = new(0, allocator);
            TickIDs = new(0, allocator);
            PerFrameData = new(0, allocator);
            PerTickData = new(0, allocator);

            GhostNames = default;
            DiffInfo = default;
            IsCreated = true;
        }

        public WorldData(int initialCapacity, NativeHashMap<SavedEntityID, FixedString64Bytes> ghostNames,
            Allocator allocator)
        {
            FrameIDs = new(initialCapacity, allocator);
            TickIDs = new(initialCapacity, allocator);
            PerFrameData = new(initialCapacity, allocator);
            PerTickData = new(initialCapacity, allocator);
            GhostNames = ghostNames;

            DiffInfo = default;
            IsCreated = true;
        }

        public bool ProcessDiff(WorldData authoritativeWorldData)
        {
            bool foundTick = false;

            foreach (var authoritativeTickKvp in authoritativeWorldData.PerTickData)
            {
                using var framesKeys = PerFrameData.GetKeyArray(Allocator.Temp);
                foreach (var frameKey in framesKeys)
                {
                    var frameToTest = PerFrameData[frameKey];
                    if (!frameToTest.PerTickData.ContainsKey(authoritativeTickKvp.Key))
                    {
                        continue;
                    }

                    foundTick = true;
                    var tickToTest = frameToTest.PerTickData[authoritativeTickKvp.Key];
                    if (tickToTest.ProcessDiff(authoritativeTickKvp.Value))
                    {
                        frameToTest.PerTickData[authoritativeTickKvp.Key] = tickToTest;
                        frameToTest.DiffInfo.HasDiff = true;
                        frameToTest.DiffInfo.DiffReasonFlags |= DiffInfo.DiffReasons.Tick;

                        PerFrameData[frameKey] = frameToTest;
                        DiffInfo.HasDiff = true;

                        DiffInfo.DiffReasonFlags |= DiffInfo.DiffReasons.Tick;
                    }
                }
            }

            DiffInfo.HasDiff |= !foundTick;
            return DiffInfo.HasDiff;
        }
    }
}
