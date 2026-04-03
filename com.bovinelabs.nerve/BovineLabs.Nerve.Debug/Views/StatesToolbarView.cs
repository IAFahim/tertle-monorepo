// <copyright file="StatesToolbarView.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Debug.Views
{
    using BovineLabs.Anchor;
    using BovineLabs.Nerve.Debug.ViewModels;
    using Unity.AppUI.UI;
    using Unity.Properties;
    using UnityEngine.UIElements;

    [Transient]
    public class StatesToolbarView : View<StatesToolbarViewModel>
    {
        public const string UssClassName = "bl-states-tab";

        public StatesToolbarView()
            : base(new StatesToolbarViewModel())

        {
            this.AddToClassList(UssClassName);

            var dropdown = new Dropdown
            {
                dataSource = this.ViewModel,
                selectionType = PickerSelectionType.Multiple,
                closeOnSelection = false,
                defaultMessage = "States",
                bindItem = this.ViewModel.BindItem,
                bindTitle = this.ViewModel.BindTitle,
            };

            dropdown.SetBinding(nameof(Dropdown.sourceItems), new DataBinding
            {
                bindingMode = BindingMode.ToTarget,
                dataSourcePath = new PropertyPath(nameof(StatesToolbarViewModel.StateItems)),
            });

            dropdown.SetBinding(nameof(Dropdown.value), new DataBinding { dataSourcePath = new PropertyPath(nameof(StatesToolbarViewModel.StateValues)) });

            this.Add(dropdown);
        }
    }
}
