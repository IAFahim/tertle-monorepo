// <copyright file="DtTileRef.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
#if DT_POLYREF32
    public readonly struct DtTileRef
    {
        internal readonly uint Value;

        private DtTileRef(uint value)
        {
            this.Value = value;
        }

        public static implicit operator uint(DtTileRef dtTileRef)
        {
            return dtTileRef.Value;
        }

        public static implicit operator DtTileRef(uint dtTileRef)
        {
            return new DtTileRef(dtTileRef);
        }

        public static implicit operator DtTileRef(DtPolyRef dtPolyRef)
        {
            return new DtTileRef(dtPolyRef.Value);
        }
    }
#else
    public readonly struct DtTileRef
    {
        internal readonly ulong Value;

        private DtTileRef(ulong value)
        {
            this.Value = value;
        }

        public static implicit operator ulong(DtTileRef dtTileRef)
        {
            return dtTileRef.Value;
        }

        public static implicit operator DtTileRef(ulong dtTileRef)
        {
            return new DtTileRef(dtTileRef);
        }

        public static implicit operator DtTileRef(DtPolyRef dtPolyRef)
        {
            return new DtTileRef(dtPolyRef.Value);
        }
    }
#endif
}
