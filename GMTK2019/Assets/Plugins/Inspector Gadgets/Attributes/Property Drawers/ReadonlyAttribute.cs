// Inspector Gadgets // Copyright 2019 Kybernetik //

#if UNITY_EDITOR
using InspectorGadgets.Attributes;
using System.Reflection;
using UnityEditor;
#endif
using UnityEngine;

namespace InspectorGadgets.Attributes
{
    /// <summary>[Pro-Only]
    /// Causes the attributed field to be greyed out and un-editable in the inspector.
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public sealed class ReadonlyAttribute : PropertyAttribute
    {
        /// <summary>
        /// Indicates when the field should be greyed out.
        /// </summary>
        public readonly EditorState When;

        /// <summary>
        /// Constructs a new <see cref="ReadonlyAttribute"/> to apply its effects in the specified <see cref="EditorState"/>.
        /// </summary>
        public ReadonlyAttribute(EditorState when = EditorState.Always)
        {
            When = when;
        }
    }
}

#if UNITY_EDITOR
namespace InspectorGadgets.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(ReadonlyAttribute))]
    internal sealed class ReadonlyDrawer : ObjectDrawer
    {
        /************************************************************************************************************************/

        public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
        {
            var attribute = this.attribute as ReadonlyAttribute;

            var enabled = GUI.enabled;
            GUI.enabled = !attribute.When.IsNow();
            base.OnGUI(area, property, label);
            GUI.enabled = enabled;
        }

        /************************************************************************************************************************/

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        /************************************************************************************************************************/
    }
}
#endif
