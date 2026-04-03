// <copyright file="BootstrapToolbarViewModel.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Debug.ViewModels
{
    using BovineLabs.Core;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.States;
    using BovineLabs.Nerve.Data.States;
    using Unity.AppUI.MVVM;
    using Unity.Properties;

    public class BootstrapToolbarViewModel : ObservableObject
    {
        private bool privateGame;
        private bool host;
        private bool join;
        private bool dedicated;

        [CreateProperty]
        public bool Private
        {
            get => this.privateGame;
            set
            {
                if (this.SetProperty(ref this.privateGame, value))
                {
                    this.SetState(ServiceStates.Game, value);
                }
            }
        }

        [CreateProperty(ReadOnly = true)]
        public bool PrivateGameEnabled => this.Private || (!this.Host && !this.Join && !this.Dedicated);

        [CreateProperty]
        public bool Host
        {
            get => this.host;
            set
            {
                if (this.SetProperty(ref this.host, value))
                {
                    this.SetState(ServiceStates.HostGame, value);
                }
            }
        }

        [CreateProperty(ReadOnly = true)]
        public bool HostEnabled => this.Host || (!this.Private && !this.Join && !this.Dedicated);

        [CreateProperty]
        public bool Join
        {
            get => this.join;
            set
            {
                if (this.SetProperty(ref this.join, value))
                {
                    this.SetState(ServiceStates.JoinGame, value);
                }
            }
        }

        [CreateProperty(ReadOnly = true)]
        public bool JoinEnabled => this.Join || (!this.Private && !this.Host && !this.Dedicated);

        [CreateProperty]
        public bool Dedicated
        {
            get => this.dedicated;
            set
            {
                if (this.SetProperty(ref this.dedicated, value))
                {
                    this.SetState(ServiceStates.Dedicated, value);
                }
            }
        }

        [CreateProperty(ReadOnly = true)]
        public bool DedicatedEnabled => this.Dedicated || (!this.Private && !this.Host && !this.Join);

        public void Update()
        {
            this.Private = this.GetState(ServiceStates.Game);
            this.Host = this.GetState(ServiceStates.HostGame);
            this.Join = this.GetState(ServiceStates.JoinGame);
            this.Dedicated = this.GetState(ServiceStates.Dedicated);
        }

        private bool GetState(string state)
        {
            if (BovineLabsBootstrap.ServiceWorld == null)
            {
                return false;
            }

            return AppAPI.StateIsEnabled<ServiceState, BitArray256, ServiceStates>(BovineLabsBootstrap.ServiceWorld.EntityManager, state);
        }

        private void SetState(string state, bool value)
        {
            this.OnPropertyChanged(nameof(this.PrivateGameEnabled));
            this.OnPropertyChanged(nameof(this.HostEnabled));
            this.OnPropertyChanged(nameof(this.JoinEnabled));
            this.OnPropertyChanged(nameof(this.DedicatedEnabled));

            if (BovineLabsBootstrap.ServiceWorld == null)
            {
                return;
            }

            if (this.GetState(state) != value)
            {
                AppAPI.StateSet<ServiceState, BitArray256, ServiceStates>(BovineLabsBootstrap.ServiceWorld.EntityManager, value ? state : ServiceStates.Init);
            }
        }
    }
}
