using System;
using System.Collections.Generic;
using Unity.Collections;

namespace Unity.NetCode.Editor.Tracing
{
    internal struct TickID : IEquatable<TickID>, IComparer<TickID>
    {
        public NetworkTick value;

        public bool Equals(TickID other)
        {
            return value.Equals(other.value);
        }

        public int Compare(TickID x, TickID y)
        {
            return x.value.TicksSince(y.value);
        }

        public override bool Equals(object obj)
        {
            return obj is TickID other && Equals(other);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return $"tick[{value.TickIndexForValidTick}]";
        }
    }

    internal struct TickData : IDisposable
    {
        public NativeList<SystemID> SystemIds;
        public NativeHashMap<SystemID, SystemData> PerSystemData;
        public float CurrentDeltaTimeSeconds;
        public NetworkTime NetworkTime;
        public TraceType TraceType;
        public DiffInfo DiffInfo;
        public bool IsCreated => PerSystemData.IsCreated;

        public TickData(float dt, NetworkTime networkTime, TraceType traceType)
        {
            PerSystemData = new(0, Allocator.Persistent);
            SystemIds = new(0, Allocator.Persistent);
            CurrentDeltaTimeSeconds = dt;
            DiffInfo = default;
            NetworkTime = networkTime;
            TraceType = traceType;
        }

        public void Dispose()
        {
            foreach (var systemKvp in PerSystemData)
            {
                systemKvp.Value.Dispose();
            }
            PerSystemData.Dispose();
            SystemIds.Dispose();
            DiffInfo.Dispose();
        }

        public bool ProcessDiff(TickData serverTickData)
        {
            var clientTickData = this;
            if (clientTickData.TraceType == TraceType.Default && // ghost update system runs while the DT and network time is not set yet, so partial tick information is invalid as that point
                clientTickData.NetworkTime.IsPartialTick && TracingConfig.instance.IgnorePartialTicks)
                return false;

            if (clientTickData.TraceType == TraceType.Default && !TracingConfig.instance.IgnoreDTDiffs && CurrentDeltaTimeSeconds != serverTickData.CurrentDeltaTimeSeconds && !NetworkTime.IsPartialTick)
            {
                DiffInfo.HasDiff = true;
                DiffInfo.DiffReasonFlags |= DiffInfo.DiffReasons.DT;
            }

            using var serverSystemsCovered = new NativeHashSet<SystemID>(20, Allocator.Temp);
            using var serverSystemIDs = serverTickData.PerSystemData.GetKeyArray(Allocator.Temp);
            // is the system client only (then should diff with previous system)
            // is the system server only (then should diff with previous system)
            // is it a special netcode system where we override the diff (ex: ghost send and ghost update systems)
            // does the shared system have a diff with its server version
            foreach (var systemIDToTest in serverSystemIDs)
            {
                var serverSystemData = serverTickData.PerSystemData[systemIDToTest];
                serverSystemsCovered.Add(systemIDToTest);
                SystemData clientSystemDataToTest;
                SystemID clientSystemIDToTest;

                if (clientTickData.PerSystemData.ContainsKey(systemIDToTest))
                {
                    clientSystemDataToTest = clientTickData.PerSystemData[systemIDToTest];
                    clientSystemIDToTest = systemIDToTest;
                }
                else
                {
                    if (serverSystemData.Family == TraceType.NetcodeGhostUpdateVsSendComparison)
                    {
                        if (!clientTickData.PerSystemData.ContainsKey(serverSystemData.AssociatedSystem))
                            // client tick runs multiple times, but only once with GhostUpdate, so it's expected to only have one tick that would have a diff with ghost send
                            continue;

                        clientSystemDataToTest = clientTickData.PerSystemData[serverSystemData.AssociatedSystem];
                        clientSystemIDToTest = serverSystemData.AssociatedSystem;
                        serverSystemsCovered.Add(serverSystemData.AssociatedSystem);
                    }
                    else
                    {
                        // couldn't find system in client, checking if that system did any relevant changes vs the previous system
                        serverSystemData.DiffInfo.DiffReasonFlags |= DiffInfo.DiffReasons.Missing;
                        serverSystemData.DiffInfo.HasDiff = true;
                        if (serverSystemData.TracePosition != TracePosition.netcode)
                        {
                            DiffInfo.HasDiff = true;
                            DiffInfo.DiffReasonFlags |= DiffInfo.DiffReasons.System;
                        }

                        if (!serverSystemData.AssociatedSystem.Equals(default))
                        {
                            SystemData previousSystemRun = serverTickData.PerSystemData[serverSystemData.AssociatedSystem];
                            if (serverSystemData.ProcessDiff(ref previousSystemRun))
                            {
                                serverSystemData.DiffInfo.HasDiff = true;
                                serverTickData.PerSystemData[serverSystemData.AssociatedSystem] = previousSystemRun;
                            }

                        }

                        serverTickData.PerSystemData[systemIDToTest] = serverSystemData;

                        continue;
                    }
                }

                if (clientSystemDataToTest.ProcessDiff(ref serverSystemData))
                {
                    clientTickData.PerSystemData[clientSystemIDToTest] = clientSystemDataToTest;
                    DiffInfo.HasDiff = true;
                    DiffInfo.DiffReasonFlags |= DiffInfo.DiffReasons.System;
                    serverTickData.PerSystemData[systemIDToTest] = serverSystemData;
                }
            }

            using var currentSystemsIDs = clientTickData.PerSystemData.GetKeyArray(Allocator.Temp);
            foreach (var systemID in currentSystemsIDs)
            {
                if (serverSystemsCovered.Contains(systemID)) continue;

                var currentSystem = clientTickData.PerSystemData[systemID];
                currentSystem.DiffInfo.DiffReasonFlags |= DiffInfo.DiffReasons.Missing;
                currentSystem.DiffInfo.HasDiff = true;
                if (currentSystem.TracePosition != TracePosition.netcode)
                {
                    DiffInfo.HasDiff = true;
                    DiffInfo.DiffReasonFlags |= DiffInfo.DiffReasons.System;
                }
                if (!currentSystem.AssociatedSystem.Equals(default) && !NetworkTime.IsPartialTick) // no previous system, so no diff
                {
                    if (!currentSystemsIDs.Contains(currentSystem.AssociatedSystem))
                    {
                        DiffInfo.HasDiff = true;
                        DiffInfo.DiffReasonFlags |= DiffInfo.DiffReasons.Missing;
                        DiffInfo.DiffReasonFlags |= DiffInfo.DiffReasons.System;
                        clientTickData.DiffInfo.DiffReasonFlags |= DiffInfo.DiffReasons.System;
                        continue;
                    }

                    SystemData previousSystem = clientTickData.PerSystemData[currentSystem.AssociatedSystem];

                    if (currentSystem.ProcessDiff(ref previousSystem))
                    {
                        currentSystem.DiffInfo.HasDiff = true;
                        clientTickData.PerSystemData[currentSystem.AssociatedSystem] = previousSystem;
                    }
                }

                clientTickData.PerSystemData[systemID] = currentSystem;
            }
            return DiffInfo.HasDiff;
        }
    }
}
