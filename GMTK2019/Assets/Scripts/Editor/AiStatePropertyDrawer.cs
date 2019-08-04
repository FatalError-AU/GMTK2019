using Enemy;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AiStateMachine))]
public class AiStatePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty state = property.FindPropertyRelative("state");

        if (string.IsNullOrWhiteSpace(state.stringValue))
            state.stringValue = "None";

        GUI.enabled = false;
        EditorGUI.PropertyField(position, state, label);
        GUI.enabled = true;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}