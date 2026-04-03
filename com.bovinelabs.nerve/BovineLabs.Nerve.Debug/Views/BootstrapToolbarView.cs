// <copyright file="BootstrapToolbarView.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Debug.Views
{
    using BovineLabs.Anchor;
    using BovineLabs.Anchor.Toolbar;
    using BovineLabs.Core;
    using BovineLabs.Nerve.Debug.ViewModels;
    using Toggle = Unity.AppUI.UI.Toggle;

    [AutoToolbar("Bootstrap")]
    public class BootstrapToolbarView : View<BootstrapToolbarViewModel>
    {
        public const string UssClassName = "bl-net-bootstrap-tab";

        public BootstrapToolbarView()
            : base(new BootstrapToolbarViewModel())
        {
            if (BovineLabsBootstrap.Instance == null)
            {
                this.schedule.Execute(() => this.parent?.RemoveFromHierarchy());
                return;
            }

            this.AddToClassList(UssClassName);

#if !UNITY_CLIENT && !UNITY_CLIENT
            var privateGame = new Toggle { label = "Private", dataSource = this.ViewModel };
            privateGame.SetBindingTwoWay(nameof(Toggle.value), nameof(BootstrapToolbarViewModel.Private));
            privateGame.SetBindingToUI(nameof(this.enabledSelf), nameof(BootstrapToolbarViewModel.PrivateGameEnabled));
            this.Add(privateGame);

            var host = new Toggle { label = "Host", dataSource = this.ViewModel };
            host.SetBindingTwoWay(nameof(Toggle.value), nameof(BootstrapToolbarViewModel.Host));
            host.SetBindingToUI(nameof(this.enabledSelf), nameof(BootstrapToolbarViewModel.HostEnabled));
            this.Add(host);
#endif

#if !UNITY_SERVER
            var join = new Toggle { label = "Join", dataSource = this.ViewModel };
            join.SetBindingTwoWay(nameof(Toggle.value), nameof(BootstrapToolbarViewModel.Join));
            join.SetBindingToUI(nameof(this.enabledSelf), nameof(BootstrapToolbarViewModel.JoinEnabled));
            this.Add(join);
#endif

#if !UNITY_CLIENT
            var dedicated = new Toggle { label = "Dedicated", dataSource = this.ViewModel, };
            dedicated.SetBindingTwoWay(nameof(Toggle.value), nameof(BootstrapToolbarViewModel.Dedicated));
            dedicated.SetBindingToUI(nameof(this.enabledSelf), nameof(BootstrapToolbarViewModel.DedicatedEnabled));
            this.Add(dedicated);
#endif

            this.schedule.Execute(this.ViewModel.Update).Every(1);
        }
    }
}
