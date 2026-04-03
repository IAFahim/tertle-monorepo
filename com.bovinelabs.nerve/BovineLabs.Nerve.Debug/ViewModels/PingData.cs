// <copyright file="PingData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Debug.ViewModels
{
    using System;
    using UnityEngine;

    public readonly struct PingData : IEquatable<PingData>
    {
        public readonly float EstimatedRTT;
        public readonly float DeviationRTT;

        public PingData(float estimatedRTT, float deviationRTT)
        {
            this.DeviationRTT = deviationRTT;
            this.EstimatedRTT = estimatedRTT;
        }

        public bool Equals(PingData other)
        {
            return Mathf.Approximately(this.EstimatedRTT, other.EstimatedRTT) && Mathf.Approximately(this.DeviationRTT, other.DeviationRTT);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (this.EstimatedRTT.GetHashCode() * 397) ^ this.DeviationRTT.GetHashCode();
            }
        }
    }
}
