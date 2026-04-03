// <copyright file="StatesToolbarViewModel.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Nerve.Debug.ViewModels
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Anchor;
    using BovineLabs.Anchor.Binding;
    using BovineLabs.Core.Extensions;
    using Unity.AppUI.UI;
    using Unity.Collections;
    using Unity.Properties;

    [Transient]
    public class StatesToolbarViewModel : SystemObservableObject<StatesToolbarViewModel.Data>
    {
        private readonly List<string> stateItems = new();
        private readonly List<int> stateValues = new();

        [CreateProperty]
        public List<string> StateItems
        {
            get
            {
                if (this.Value.StateItems.IsCreated)
                {
                    this.stateItems.Clear();
                    foreach (var c in this.Value.StateItems)
                    {
                        this.stateItems.Add(c.ToString());
                    }
                }

                return this.stateItems;
            }
        }

        [CreateProperty]
        public IEnumerable<int> StateValues
        {
            get
            {
                if (this.Value.StateValues.IsCreated)
                {
                    this.stateValues.Clear();
                    this.stateValues.AddRangeNative(this.Value.StateValues.AsArray());
                }

                return this.stateValues;
            }

            set
            {
                if (this.Value.StateValues.IsCreated)
                {
                    this.Value.StateValues.Clear();
                    foreach (var v in value)
                    {
                        this.Value.StateValues.Add(v);
                    }
                }
            }
        }

        public void BindItem(DropdownItem item, int index)
        {
            item.label = this.stateItems[index];
        }

        public void BindTitle(DropdownItem item, IEnumerable<int> selected)
        {
            var text = string.Join(',', selected.Select(s => this.StateItems[s]));
            if (string.IsNullOrWhiteSpace(text))
            {
                text = "States";
            }

            item.labelElement.text = text;
        }

        public struct Data
        {
            private NativeArray<FixedString64Bytes> stateItems;
            private NativeList<int> stateValues;

            public NativeArray<FixedString64Bytes> StateItems
            {
                readonly get => this.stateItems;
                set => this.SetValueNotify(ref this.stateItems, value);
            }

            public NativeList<int> StateValues
            {
                readonly get => this.stateValues;
                set => this.SetValueNotify(ref this.stateValues, value);
            }
        }
    }
}
