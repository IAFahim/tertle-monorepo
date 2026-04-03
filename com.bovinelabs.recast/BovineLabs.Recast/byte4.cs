// <copyright file="byte4.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Unity Mathematics")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "Unity Mathematics")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "Unity Mathematics")]
    [StructLayout(LayoutKind.Sequential)]
    public struct byte4
    {
        public byte x;
        public byte y;
        public byte z;
        public byte w;

        public byte4(byte x, byte y, byte z, byte w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public unsafe byte this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if ((uint)index >= 4)
                {
                    throw new ArgumentException("index must be between[0...3]");
                }
#endif
                fixed (byte4* array = &this)
                {
                    return ((byte*)array)[index];
                }
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if ((uint)index >= 4)
                {
                    throw new ArgumentException("index must be between[0...3]");
                }
#endif
                fixed (byte4* array = &this)
                {
                    ((byte*)array)[index] = value;
                }
            }
        }
    }
}
