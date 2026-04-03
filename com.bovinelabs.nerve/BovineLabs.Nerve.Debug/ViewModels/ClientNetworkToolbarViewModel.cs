// <copyright file="ClientNetworkToolbarViewModel.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Debug.ViewModels
{
    using BovineLabs.Anchor;
    using BovineLabs.Anchor.Binding;
    using Unity.Properties;

    public class ClientNetworkToolbarViewModel : SystemObservableObject<ClientNetworkToolbarViewModel.Data>
    {
        [CreateProperty]
        public PingData Ping => this.Value.Ping;

        public struct Data
        {
            private PingData ping;

            public PingData Ping
            {
                readonly get => this.ping;
                set => this.SetProperty(ref this.ping, value);
            }
        }
    }
}
