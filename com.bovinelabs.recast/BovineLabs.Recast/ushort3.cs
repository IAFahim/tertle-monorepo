// <copyright file="ushort3.cs" company="BovineLabs">
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
    public struct ushort3
    {
        public ushort x;
        public ushort y;
        public ushort z;

        public ushort3(ushort x, ushort y, ushort z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public unsafe ushort this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if ((uint)index >= 3)
                {
                    throw new ArgumentException("index must be between[0...2]");
                }
#endif
                fixed (ushort3* array = &this)
                {
                    return ((ushort*)array)[index];
                }
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if ((uint)index >= 3)
                {
                    throw new ArgumentException("index must be between[0...2]");
                }
#endif
                fixed (ushort3* array = &this)
                {
                    ((ushort*)array)[index] = value;
                }
            }
        }
    }
}
