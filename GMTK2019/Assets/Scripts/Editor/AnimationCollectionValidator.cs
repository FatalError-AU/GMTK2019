using Animation;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomPropertyDrawer(typeof(AnimationCollection.ValidateAttribute))]
    public class AnimationCollectionValidator : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label);
            
            AnimationCollection.ValidateAttribute attrib = (AnimationCollection.ValidateAttribute)attribute;

            if (property.objectReferenceValue == null)
                return;

            if (!((AnimationCollection) property.objectReferenceValue).IsType(attrib.type))
            {
                Debug.LogWarning($"Wrong animation collection given, expected type: {attrib.type.FullName}");
                property.objectReferenceValue = null;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }
    }
}