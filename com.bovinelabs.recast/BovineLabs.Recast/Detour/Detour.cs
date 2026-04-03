// <copyright file="Detour.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>Status flags for Detour operations.</summary>
    [Flags]
    public enum DtStatus : uint
    {
        /// <summary>Indicates the operation failed.</summary>
        Failure = 1u << 31,

        /// <summary>Indicates the operation succeeded.</summary>
        Success = 1u << 30,

        /// <summary>Indicates the operation is still in progress.</summary>
        InProgress = 1u << 29,

        /// <summary>Mask used to extract the detail information.</summary>
        StatusDetailMask = 0x0ffffff,

        /// <summary>The input data is not recognized.</summary>
        WrongMagic = 1 << 0,

        /// <summary>The input data uses an unsupported version.</summary>
        WrongVersion = 1 << 1,

        /// <summary>The operation ran out of memory.</summary>
        OutOfMemory = 1 << 2,

        /// <summary>An input parameter was invalid.</summary>
        InvalidParam = 1 << 3,

        /// <summary>The result buffer for the query was too small.</summary>
        BufferTooSmall = 1 << 4,

        /// <summary>The query ran out of nodes during the search.</summary>
        OutOfNodes = 1 << 5,

        /// <summary>The query did not reach the end location.</summary>
        PartialResult = 1 << 6,

        /// <summary>A tile has already been assigned to the given coordinate.</summary>
        AlreadyOccupied = 1 << 7,
    }

    /// <summary>Options for dtNavMeshQuery::initSlicedFindPath and updateSlicedFindPath.</summary>
    [Flags]
    public enum DtFindPathOptions : uint
    {
        /// <summary>No find path options are enabled.</summary>
        None = 0x00,

        /// <summary>Uses raycasts during pathfinding to shortcut while still considering costs.</summary>
        FindpathAnyAngle = 0x02,
    }

    /// <summary>Options for raycast operations.</summary>
    [Flags]
    public enum DtRaycastOptions : uint
    {
        /// <summary>Calculates movement cost along the ray and stores it on the hit result.</summary>
        RaycastUseCosts = 0x01,
    }

    [Flags]
    public enum DtDetailTriEdgeFlags : byte
    {
        /// <summary>
        /// Detail triangle edge is part of the poly boundary
        /// </summary>
        DetailEdgeBoundary = 0x01, // DT_DETAIL_EDGE_BOUNDARY
    }

    /// <summary>Flags for straight path vertices.</summary>
    [Flags]
    public enum DtStraightPathFlags : byte
    {
        /// <summary>The vertex is the start position in the path.</summary>
        StraightpathStart = 0x01,

        /// <summary>The vertex is the end position in the path.</summary>
        StraightpathEnd = 0x02,

        /// <summary>The vertex is the start of an off-mesh connection.</summary>
        StraightpathOffmeshConnection = 0x04,
    }

    /// <summary>Options for straight path generation.</summary>
    [Flags]
    public enum DtStraightPathOptions
    {
        /// <summary>Adds a vertex at every polygon edge crossing where the area changes.</summary>
        StraightPathAreaCrossings = 0x01,

        /// <summary>Adds a vertex at every polygon edge crossing.</summary>
        StraightPathAllCrossings = 0x02,
    }

    public static partial class Detour
    {
        /// <summary>Determines whether the status indicates success.</summary>
        /// <param name="status">The status flag to evaluate.</param>
        /// <returns><c>true</c> if the status includes <see cref="DtStatus.Success"/>; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StatusSucceed(DtStatus status)
        {
            return (status & DtStatus.Success) != 0;
        }

        /// <summary>Determines whether the status indicates failure.</summary>
        /// <param name="status">The status flag to evaluate.</param>
        /// <returns><c>true</c> if the status includes <see cref="DtStatus.Failure"/>; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StatusFailed(DtStatus status)
        {
            return (status & DtStatus.Failure) != 0;
        }

        /// <summary>Determines whether the status indicates that the operation is in progress.</summary>
        /// <param name="status">The status flag to evaluate.</param>
        /// <returns><c>true</c> if the status includes <see cref="DtStatus.InProgress"/>; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StatusInProgress(DtStatus status)
        {
            return (status & DtStatus.InProgress) != 0;
        }

        /// <summary>Determines whether a specific detail flag is present on the status.</summary>
        /// <param name="status">The status flag to evaluate.</param>
        /// <param name="detail">The detail flag to check for.</param>
        /// <returns><c>true</c> if the status includes the specified detail flag; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StatusDetail(DtStatus status, DtStatus detail)
        {
            return (status & detail) != 0;
        }
    }
}
