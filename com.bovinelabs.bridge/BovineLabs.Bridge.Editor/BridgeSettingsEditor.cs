// <copyright file="BridgeSettingsEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BOVINELABS_BRIDGE_DISABLE_AUDIO
namespace BovineLabs.Bridge.Editor
{
    using BovineLabs.Bridge.Authoring;
    using BovineLabs.Bridge.Authoring.Audio;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.Editor.ObjectManagement;
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(BridgeSettings))]
    public class BridgeSettingsEditor : ElementEditor
    {
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            if (property.name == "musicTracks")
            {
                return new AssetCreator<MusicTrackDefinition>(this.serializedObject, property).Element;
            }

            return base.CreateElement(property);
        }
    }
}
#endif
