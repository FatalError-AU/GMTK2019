// Inspector Gadgets // Copyright 2019 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(Object), true)]
    internal class ObjectDrawer : PropertyDrawer
    {
        /************************************************************************************************************************/

        public static readonly AutoPrefs.EditorInt
            ObjectEditorNestLimit = new AutoPrefs.EditorInt(Strings.PrefsKeyPrefix + "ObjectEditorNestLimit", 3);
        public static readonly AutoPrefs.EditorBool
            ItaliciseSelfReferences = new AutoPrefs.EditorBool(Strings.PrefsKeyPrefix + "ItaliciseSelfReferences", true);

        private static readonly GUIContent
            FindInHierarchy = new GUIContent("H", "Find in Hierarchy"),
            FindInScene = new GUIContent("S", "Find in Scene"),
            FindInAssets = new GUIContent("A", "Find in Assets");

        private static readonly HashSet<Object>
            CurrentReferences = new HashSet<Object>();

        private static int _NestLevel;
        private static GUIStyle _NestAreaStyle;

        private bool _IsInitialised;
        private bool _AllowNestedEditors;
        private UnityEditor.Editor _TargetEditor;

        /************************************************************************************************************************/

        public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(area, property, label, property.isExpanded);
                return;
            }

            Object reference;
            bool isSelf;
            GetReference(property, out reference, out isSelf);

            // Find Buttons.
            DoFindButtonGUI(ref area, property, reference);

            // Property Field.

            var originalStyle = EditorStyles.objectField.fontStyle;
            if (isSelf)
                EditorStyles.objectField.fontStyle = FontStyle.Italic;

            EditorGUI.PropertyField(area, property, label, property.isExpanded);

            if (isSelf)
            {
                EditorStyles.objectField.fontStyle = originalStyle;
                return;
            }

            // Nested Inspector.
            DoNestedInspectorGUI(area, property, reference);
        }

        /************************************************************************************************************************/

        private void GetReference(SerializedProperty property, out Object reference, out bool isSelf)
        {
            isSelf = false;

            if (!property.hasMultipleDifferentValues && property.propertyType == SerializedPropertyType.ObjectReference)
            {
                reference = property.objectReferenceValue;

                if (!ItaliciseSelfReferences)
                    return;

                var targetComponent = property.serializedObject.targetObject as Component;
                if (targetComponent != null)
                {
                    var component = reference as Component;
                    if (component != null)
                    {
                        if (component.gameObject == targetComponent.gameObject)
                        {
                            isSelf = true;
                        }
                    }
                    else
                    {
                        var gameObject = reference as Component;
                        if (gameObject != null)
                        {
                            if (gameObject == targetComponent.gameObject)
                            {
                                isSelf = true;
                            }
                        }
                    }
                }
                else if (reference == property.serializedObject.targetObject)
                {
                    isSelf = true;
                }
            }
            else
            {
                reference = null;
            }
        }

        /************************************************************************************************************************/

        private static void DoFindButtonGUI(ref Rect area, SerializedProperty property, Object reference)
        {
            if (reference != null)
                return;

            var buttonArea = IGEditorUtils.StealFromRight(ref area, area.height);
            if (GUI.Button(buttonArea, FindInAssets, InternalGUI.SmallButtonStyle))
                FindObjectInAssets(property);

            buttonArea = IGEditorUtils.StealFromRight(ref area, area.height);
            if (GUI.Button(buttonArea, FindInScene, InternalGUI.SmallButtonStyle))
                FindObjectInScene(property);

            var accessor = SerializedPropertyAccessor.GetAccessor(property);
            if (accessor != null && typeof(Component).IsAssignableFrom(accessor.FieldType))
            {
                buttonArea = IGEditorUtils.StealFromRight(ref area, area.height);
                if (GUI.Button(buttonArea, FindInHierarchy, InternalGUI.SmallButtonStyle))
                    FindComponentInHierarchy(property);
            }
        }

        /************************************************************************************************************************/

        private static void FindComponentInHierarchy(SerializedProperty property)
        {
            var accessor = SerializedPropertyAccessor.GetAccessor(property);
            IGEditorUtils.ForEachTarget(property, (prop) =>
            {
                var gameObject = (prop.serializedObject.targetObject as Component).gameObject;
                prop.objectReferenceValue = IGUtils.GetComponentInHierarchy(gameObject, accessor.FieldType, prop.displayName);
            });
        }

        private static void FindObjectInScene(SerializedProperty property)
        {
            var accessor = SerializedPropertyAccessor.GetAccessor(property);
            IGEditorUtils.ForEachTarget(property, (prop) =>
            {
                prop.objectReferenceValue = IGUtils.GetBestComponent(Object.FindObjectsOfType(accessor.FieldType), prop.displayName);
            });
        }

        private static void FindObjectInAssets(SerializedProperty property)
        {
            var accessor = SerializedPropertyAccessor.GetAccessor(property);
            IGEditorUtils.ForEachTarget(property, (prop) =>
            {
                prop.objectReferenceValue = IGEditorUtils.FindAssetOfType(accessor.FieldType, prop.displayName);
            });
        }

        /************************************************************************************************************************/

        private void DoNestedInspectorGUI(Rect area, SerializedProperty property, Object reference)
        {

            if (reference == null ||
                !AllowNesting(reference.GetType()) ||
                _NestLevel >= ObjectEditorNestLimit ||
                property.hasMultipleDifferentValues ||
                !AllowNesting(property) ||
                CurrentReferences.Contains(reference))
            {
                return;
            }

            CurrentReferences.Add(reference);

            // Disable the GUI if HideFlags.NotEditable is set.
            var enabled = GUI.enabled;
            GUI.enabled = (reference.hideFlags & HideFlags.NotEditable) != HideFlags.NotEditable;

            _NestLevel++;

            property.isExpanded = EditorGUI.Foldout(area, property.isExpanded, GUIContent.none, true);
            if (property.isExpanded)
            {
                const float NegativePadding = 4;
                EditorGUIUtility.labelWidth -= NegativePadding;

                if (_NestAreaStyle == null)
                {
                    _NestAreaStyle = new GUIStyle(GUI.skin.box);
                    var rect = _NestAreaStyle.margin;
                    rect.bottom = rect.top = 0;
                    _NestAreaStyle.margin = rect;
                }

                EditorGUI.indentLevel++;
                GUILayout.BeginVertical(_NestAreaStyle);

                try
                {
                    // If the target has changed, destroy the old editor and make a new one.
                    if (_TargetEditor == null || _TargetEditor.target != reference)
                    {
                        Editors.Destroy(_TargetEditor);
                        _TargetEditor = Editors.Create(reference);
                    }

                    // Draw the target editor.
                    _TargetEditor.OnInspectorGUI();
                }
                catch (ExitGUIException)
                {
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                GUILayout.EndVertical();
                EditorGUI.indentLevel--;

                EditorGUIUtility.labelWidth += NegativePadding;
            }

            _NestLevel--;

            GUI.enabled = enabled;

            CurrentReferences.Remove(reference);
        }

        /************************************************************************************************************************/

        private bool AllowNesting(SerializedProperty property)
        {
            if (!_IsInitialised)
            {
                _IsInitialised = true;
                _AllowNestedEditors = AllowNesting(SerializedPropertyAccessor.GetAccessor(property));
            }

            return _AllowNestedEditors;
        }

        /************************************************************************************************************************/

        private static readonly Dictionary<SerializedPropertyAccessor, bool>
            AllowNestingAccessorCache = new Dictionary<SerializedPropertyAccessor, bool>();

        private static bool AllowNesting(SerializedPropertyAccessor accessor)
        {
            if (accessor == null)
                return true;

            bool allow;
            if (!AllowNestingAccessorCache.TryGetValue(accessor, out allow))
            {
                if (!AllowNesting(accessor.FieldType))
                {
                    allow = false;
                }
                else
                {
                    allow = AllowNesting(accessor.Parent);
                }

                AllowNestingAccessorCache.Add(accessor, allow);
            }

            return allow;
        }

        /************************************************************************************************************************/

        private static readonly Dictionary<Type, bool>
            AllowNestingTypeCache = new Dictionary<Type, bool>();

        private static bool AllowNesting(Type type)
        {
            if (type == null)
                return true;

            bool allow;
            if (!AllowNestingTypeCache.TryGetValue(type, out allow))
            {
                var field = type.GetField("NestedObjectDrawers", IGEditorUtils.StaticBindings);
                if (field != null && field.IsLiteral && field.FieldType == typeof(bool))
                {
                    allow = (bool)field.GetValue(null);
                }
                else
                {
                    allow = AllowNesting(type.BaseType);
                }

                AllowNestingTypeCache.Add(type, allow);
            }

            return allow;
        }

        static ObjectDrawer()
        {
            AllowNestingTypeCache.Add(typeof(GameObject), false);
            AllowNestingTypeCache.Add(typeof(Material), false);
            AllowNestingTypeCache.Add(typeof(AudioClip), false);
            AllowNestingTypeCache.Add(typeof(DefaultAsset), false);
        }

        /************************************************************************************************************************/

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, property.isExpanded);
        }

        /************************************************************************************************************************/
    }
}

#endif
