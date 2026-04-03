// <copyright file="ClientNetworkToolbarView.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Debug.Views
{
    using System;
    using BovineLabs.Anchor;
    using BovineLabs.Anchor.Elements;
    using BovineLabs.Nerve.Debug.ViewModels;
    using Unity.Properties;
    using UnityEngine.UIElements;

    public class ClientNetworkToolbarView : View<ClientNetworkToolbarViewModel>
    {
        public const string UssClassName = "bl-client-network-tab";

        public ClientNetworkToolbarView()
            : base(new ClientNetworkToolbarViewModel())
        {
            this.AddToClassList(UssClassName);

            this.Add(KeyValueGroup.Create(this.ViewModel,
                new (string, string, Action<DataBinding>)[]
                {
                    ("Ping", nameof(ClientNetworkToolbarViewModel.Ping),
                        db => db.sourceToUiConverters.AddConverter((TypeConverter<PingData, string>)PingConvert)),
                }));
        }

        private static string PingConvert(ref PingData p)
        {
            return p.EstimatedRTT < 1000 ? $"{(int)p.EstimatedRTT}±{(int)p.DeviationRTT}ms" : $"~{(int)p.EstimatedRTT + ((int)p.DeviationRTT / 2):0}ms";
        }
    }
}
