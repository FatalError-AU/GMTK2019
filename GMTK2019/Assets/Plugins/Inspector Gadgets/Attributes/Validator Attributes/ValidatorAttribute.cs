// Inspector Gadgets // Copyright 2019 Kybernetik //

using InspectorGadgets.Attributes;
using System.Reflection;
using UnityEngine;

namespace InspectorGadgets.Attributes
{
    /// <summary>[Pro-Only]
    /// Base class for attributes that apply some sort of validation to a field.
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public abstract class ValidatorAttribute : PropertyAttribute
    {
        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>
        /// Called before the property is drawn so you can cache its value if needed.
        /// </summary>
        public virtual void OnBeforeProperty(UnityEditor.SerializedProperty property) { }

        /// <summary>
        /// Validate the value of the specified 'property'.
        /// </summary>
        public abstract void Validate(UnityEditor.SerializedProperty property);

        /************************************************************************************************************************/
#endif
    }
}

#if UNITY_EDITOR
namespace InspectorGadgets.Editor.PropertyDrawers
{
    [UnityEditor.CustomPropertyDrawer(typeof(ValidatorAttribute), true)]
    internal sealed class ValidatorDrawer : ObjectDrawer
    {
        /************************************************************************************************************************/

        public override void OnGUI(Rect area, UnityEditor.SerializedProperty property, GUIContent label)
        {
            UnityEditor.EditorGUI.BeginChangeCheck();
            base.OnGUI(area, property, label);
            if (UnityEditor.EditorGUI.EndChangeCheck())
                (attribute as ValidatorAttribute).Validate(property);
        }

        /************************************************************************************************************************/

        public override float GetPropertyHeight(UnityEditor.SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label);
        }

        /************************************************************************************************************************/
    }
}
#endif
