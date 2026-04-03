// <copyright file="RcHeightfieldLayerSet.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Recast
{
    using System;
    using System.Runtime.InteropServices;
    using Unity.Collections;

    /// <summary>
    /// Represents a set of heightfield layers.
    /// </summary>
    /// <remarks>Memory layout must match C++ rcHeightfieldLayerSet exactly.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RcHeightfieldLayerSet : IDisposable
    {
        /// <summary>
        /// The layers in the set [Size: <see cref="NLayers"/>].
        /// </summary>
        public RcHeightfieldLayer* Layers;

        /// <summary>
        /// The number of layers in the set.
        /// </summary>
        public int NLayers;

        public RcHeightfieldLayerSet(Allocator allocator)
        {
            this = default;
            this.Allocator = allocator;
        }

        public Allocator Allocator { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.Layers != null)
            {
                for (var i = 0; i < this.NLayers; i++)
                {
                    this.Layers[i].Dispose();
                }

                AllocatorManager.Free(this.Allocator, this.Layers);
                this.Layers = null;
            }

            this = default;
        }
    }
}
