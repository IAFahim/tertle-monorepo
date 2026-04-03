using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Properties;
using Unity.Entities.UI;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine.UIElements;
using UnityEngine.Pool;

namespace Unity.Entities.Editor
{
    class ComponentsTab : ITabContent
    {
        readonly EntityInspectorContext m_Context;
        bool m_IsVisible;
        ComponentsTabInspector m_Inspector;

        public string TabName { get; } = L10n.Tr("Components");
        public void OnTabVisibilityChanged(bool isVisible)
        {
            if (isVisible)
                Analytics.SendEditorEvent(Analytics.Window.Inspector, Analytics.EventType.InspectorTabFocus, Analytics.ComponentsTabName);
            m_IsVisible = isVisible;
        }

        public ComponentsTab(EntityInspectorContext entityInspectorContext)
        {
            m_Context = entityInspectorContext;
        }
        
        internal void ClearSearch() => m_Inspector?.ClearSearch();
        internal void ApplySearch(string searchText) => m_Inspector?.ApplySearch(searchText);

        class ComponentsSearchView : SearchViewModel
        {
            readonly ComponentsTabInspector m_Inspector;

            public ComponentsSearchView(ComponentsTabInspector inspector)
                : base(new SearchViewState(SearchService.CreateContext(Array.Empty<SearchProvider>(), "")).LoadDefaults())
            {
                m_Inspector = inspector;
                context.searchView = this;
            }

            public override void SetSearchText(string searchText, TextCursorPlacement moveCursor = TextCursorPlacement.Default)
            {
                SetSearchText(searchText, moveCursor, 0);
            }

            public override void SetSearchText(string searchText, TextCursorPlacement moveCursor, int cursorInsertPosition)
            {
                if (string.Equals(context.searchText.Trim(), searchText.Trim(), StringComparison.Ordinal))
                    return;

                context.searchText = searchText;

                if (string.IsNullOrEmpty(searchText))
                    m_Inspector.ClearSearch();
                else
                    m_Inspector.ApplySearch(searchText);
            }
        }

        [UsedImplicitly]
        internal class ComponentsTabInspector : PropertyInspector<ComponentsTab>
        {
            EntityInspectorComponentStructure m_CurrentComponentStructure;
            EntityInspectorComponentStructure m_LastComponentStructure;
            EntityInspectorBuilderVisitor m_InspectorBuilderVisitor;
            VisualElement m_Root;
            TagComponentContainer m_TagsRoot;
            VisualElement m_ComponentsRoot;
            ComponentsSearchView m_SearchView;
            SearchFieldElement m_SearchField;

            public override VisualElement Build()
            {
                Target.m_Inspector = this;

                m_Root = Resources.Templates.Inspector.ComponentsTab.Clone();

                Resources.AddCommonVariables(m_Root);
                UnityEditor.Search.SearchElement.AppendStyleSheets(m_Root);

                m_SearchView = new ComponentsSearchView(this);
                m_SearchField = new SearchFieldElement("ComponentsSearch", m_SearchView, SearchQueryBuilderViewFlags.Default);
                m_SearchField.AddToClassList(UssClasses.Inspector.ComponentsTab.SearchField);

                var searchContainer = m_Root.Q(className: "search-field-container");
                searchContainer.Add(m_SearchField);

                m_TagsRoot = new TagComponentContainer(Target.m_Context);
                m_ComponentsRoot = new VisualElement();
                m_Root.Add(m_TagsRoot);
                m_Root.Add(m_ComponentsRoot);

                m_Root.RegisterCallback<GeometryChangedEvent, VisualElement>((_, elem) =>
                {
                    StylingUtility.AlignInspectorLabelWidth(elem);
                }, m_Root);

                m_InspectorBuilderVisitor = new EntityInspectorBuilderVisitor(Target.m_Context);
                m_CurrentComponentStructure = new EntityInspectorComponentStructure();
                BuildOrUpdateUI();

                return m_Root;
            }

            public void ClearSearch()
            {
                using var _ = ListPool<ComponentElementBase>.Get(out var list);
                m_Root.Query<ComponentElementBase>().ToList(list);

                for (var i = 0; i < list.Count; i++)
                {
                    list[i].Show();
                }
            }

            public void ApplySearch(string searchText)
            {
                using var _ = ListPool<ComponentElementBase>.Get(out var list);
                m_Root.Query<ComponentElementBase>().ToList(list);

                for (var i = 0; i < list.Count; i++)
                {
                    var element = list[i];
                    var isMatch = element.DisplayName != null &&
                                  element.DisplayName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                    element.SetVisibility(isMatch);
                }
            }

            public override void Update()
            {
                if (!Target.m_IsVisible || !Target.m_Context.TargetExists())
                    return;

                BuildOrUpdateUI();
            }

            void BuildOrUpdateUI()
            {
                m_CurrentComponentStructure.Reset();

                var container = Target.m_Context.EntityContainer;
                var propertyBag = PropertyBag.GetPropertyBag<EntityContainer>();
                var properties = propertyBag.GetProperties(ref container);

                foreach (var property in properties)
                {
                    if (property is IComponentProperty componentProperty)
                    {
                        if (componentProperty.Type == ComponentPropertyType.Tag)
                            m_CurrentComponentStructure.Tags.Add(componentProperty);
                        else
                            m_CurrentComponentStructure.Components.Add(componentProperty);
                    }
                }

                m_CurrentComponentStructure.Sort();

                if (m_LastComponentStructure == null)
                {
                    m_LastComponentStructure = new EntityInspectorComponentStructure();
                    foreach (var p in m_CurrentComponentStructure.Tags)
                    {
                        PropertyContainer.Accept(m_InspectorBuilderVisitor, ref container, new PropertyPath(p.Name));
                        m_TagsRoot.Add(m_InspectorBuilderVisitor.Result);
                    }

                    foreach (var p in m_CurrentComponentStructure.Components)
                    {
                        PropertyContainer.Accept(m_InspectorBuilderVisitor, ref container, new PropertyPath(p.Name));
                        m_ComponentsRoot.Add(m_InspectorBuilderVisitor.Result);
                    }
                }
                else
                {
                    UpdateUI(!m_CurrentComponentStructure.Tags.SequenceEqual(m_LastComponentStructure.Tags),
                             !m_CurrentComponentStructure.Components.SequenceEqual(m_LastComponentStructure.Components));
                }

                m_LastComponentStructure.CopyFrom(m_CurrentComponentStructure);
            }

            void UpdateUI(bool updateTags, bool updateComponents)
            {
                if (!updateTags && !updateComponents)
                    return;

                var container = Target.m_Context.EntityContainer;

                // update tags
                if (updateTags)
                {
                    InspectorUtility.Synchronize(m_LastComponentStructure.Tags,
                        m_CurrentComponentStructure.Tags,
                                EntityInspectorComponentsComparer.Instance,
                                m_TagsRoot,
                                Factory);
                }

                // update regular components
                if (updateComponents)
                {
                    InspectorUtility.Synchronize(m_LastComponentStructure.Components,
                        m_CurrentComponentStructure.Components,
                                EntityInspectorComponentsComparer.Instance,
                                m_ComponentsRoot,
                                Factory);
                }

                VisualElement Factory(IComponentProperty property)
                {
                    PropertyContainer.Accept(m_InspectorBuilderVisitor, ref container, new PropertyPath(property.Name));
                    return m_InspectorBuilderVisitor.Result;
                }
            }

        }
    }
}
