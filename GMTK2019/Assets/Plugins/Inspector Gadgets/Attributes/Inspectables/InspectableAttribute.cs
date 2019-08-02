// Inspector Gadgets // Copyright 2019 Kybernetik //

using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using InspectorGadgets.Editor;
using UnityEditor;
#endif

namespace InspectorGadgets.Attributes
{
    /// <summary>[Pro-Only]
    /// Adds the attributed field or property to the inspector as if it were serialized.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class InspectableAttribute : BaseInspectableAttribute
    {
        /************************************************************************************************************************/

        /// <summary>If true, the displayed field will be greyed out so the user can't modify it.</summary>
        public bool Readonly { get; set; }

        /// <summary>If true, the inspector will be constantly repainted while this label is shown to keep it updated.</summary>
        public bool ConstantlyRepaint { get; set; }

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        private FieldInfo _Field;
        private PropertyInfo _Property;
        private Type _MemberType;
        private SerializedPropertyType _PropertyType;
        private GUIContent _ExceptionText;

        /************************************************************************************************************************/

        /// <summary>Initialise this inspectable with a member.</summary>
        protected override string Initialise()
        {
            // Validate the specified member.
            var field = Member as FieldInfo;
            if (field != null)
            {
                _Field = field;
                _MemberType = field.FieldType;

                if (field.IsLiteral || field.IsInitOnly)
                    Readonly = true;
            }
            else
            {
                var property = Member as PropertyInfo;
                if (property != null)
                {
                    if (property.GetGetMethod(true) == null)
                    {
                        return "it must have both a getter and setter";
                    }

                    if (property.GetSetMethod(true) == null)
                        Readonly = true;

                    _Property = property;
                    _MemberType = property.PropertyType;
                }
                else
                {
                    return "it isn't a field or property";
                }
            }

            if (Label == null)
                Label = IGUtils.ConvertFieldNameToFriendly(Member.Name, true);

            if (Tooltip == null)
                Tooltip = "[Inspectable] " + Member.GetNameCS();

            _PropertyType = SerializedPropertyAccessor.GetPropertyType(_MemberType);

            return null;
        }

        /************************************************************************************************************************/

        private object _Value;

        /// <summary>Draw this inspectable using <see cref="GUILayout"/>.</summary>
        public override void OnGUI(Object[] targets)
        {
            if (Event.current.type == EventType.Layout)
            {
                try
                {
                    _Value = GetValue(targets);
                    _ExceptionText = null;
                }
                catch (Exception ex)
                {
                    if (_ExceptionText == null)
                        _ExceptionText = new GUIContent();

                    _ExceptionText.text = ex.GetBaseException().GetType().Name;
                }
            }

            var area = EditorGUILayout.BeginHorizontal();

            PrefixLabel(LabelContent);

            if (_ExceptionText != null)
            {
                GUILayout.Label(_ExceptionText);
            }
            else
            {
                EditorGUI.BeginChangeCheck();

                var guiEnabled = GUI.enabled;
                if (Readonly)
                    GUI.enabled = false;

                switch (_PropertyType)
                {
                    // Primitives.

                    case SerializedPropertyType.Boolean: _Value = EditorGUILayout.Toggle((bool)_Value); break;
                    case SerializedPropertyType.Integer: _Value = EditorGUILayout.IntField((int)_Value); break;
                    case SerializedPropertyType.Float: _Value = EditorGUILayout.FloatField((float)_Value); break;
                    case SerializedPropertyType.String: _Value = EditorGUILayout.TextField((string)_Value); break;

                    // Vectors.

                    case SerializedPropertyType.Vector2: _Value = EditorGUILayout.Vector2Field(GUIContent.none, (Vector2)_Value); break;
                    case SerializedPropertyType.Vector3: _Value = EditorGUILayout.Vector3Field(GUIContent.none, (Vector3)_Value); break;
                    case SerializedPropertyType.Vector4: _Value = EditorGUILayout.Vector4Field(GUIContent.none, (Vector4)_Value); break;

                    case SerializedPropertyType.Quaternion:
                        _Value = Quaternion.Euler(EditorGUILayout.Vector3Field(GUIContent.none, ((Quaternion)_Value).eulerAngles));
                        break;

                    // Other.

                    case SerializedPropertyType.Color: _Value = EditorGUILayout.ColorField((Color)_Value); break;

                    case SerializedPropertyType.Gradient:
                        break;

                    case SerializedPropertyType.Rect: _Value = EditorGUILayout.RectField((Rect)_Value); break;
                    case SerializedPropertyType.Bounds: _Value = EditorGUILayout.BoundsField((Bounds)_Value); break;

#if UNITY_2017_3_OR_NEWER
                    case SerializedPropertyType.Vector2Int: _Value = EditorGUILayout.Vector2IntField(GUIContent.none, (Vector2Int)_Value); break;
                    case SerializedPropertyType.Vector3Int: _Value = EditorGUILayout.Vector3IntField(GUIContent.none, (Vector3Int)_Value); break;
                    case SerializedPropertyType.RectInt: _Value = EditorGUILayout.RectIntField((RectInt)_Value); break;
                    case SerializedPropertyType.BoundsInt: _Value = EditorGUILayout.BoundsIntField((BoundsInt)_Value); break;
#endif

                    case SerializedPropertyType.AnimationCurve: _Value = EditorGUILayout.CurveField((AnimationCurve)_Value); break;

                    // Special.

                    case SerializedPropertyType.ObjectReference: _Value = EditorGUILayout.ObjectField((Object)_Value, _MemberType, true); break;
                    case SerializedPropertyType.Enum: _Value = EditorGUILayout.EnumPopup((Enum)_Value); break;

                    default: GUILayout.Label("Unsupported Type: " + _MemberType); break;
                }

                GUI.enabled = guiEnabled;

                if (EditorGUI.EndChangeCheck() && !Readonly)
                    SetValue(targets, _Value);

                EditorGUI.showMixedValue = false;
            }

            EditorGUILayout.EndHorizontal();

            CheckContextMenu(area, targets);

            if (ConstantlyRepaint)
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        /************************************************************************************************************************/

        private object GetValue(Object[] targets)
        {
            if (targets == null)
                return GetSingleValue(null);

            var firstValue = GetSingleValue(targets[0]);

            var i = 1;
            for (; i < targets.Length; i++)
            {
                var value = GetSingleValue(targets[i]);
                if (!Equals(firstValue, value))
                {
                    EditorGUI.showMixedValue = true;
                    break;
                }
            }

            return firstValue;
        }

        private object GetSingleValue(Object target)
        {
            if (_Field != null)
                return _Field.GetValue(target);
            else
                return _Property.GetValue(target, null);
        }

        /************************************************************************************************************************/

        private void SetValue(Object[] targets, object value)
        {
            if (targets == null)
                SetSingleValue(null, value);

            for (int i = 0; i < targets.Length; i++)
            {
                SetSingleValue(targets[i], value);
            }
        }

        private void SetSingleValue(Object target, object value)
        {
            if (_Field != null)
                _Field.SetValue(target, value);
            else
                _Property.SetValue(target, value, null);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Adds various items to the 'menu' relating to the 'targets'.
        /// </summary>
        protected override void PopulateContextMenu(GenericMenu menu, Object[] targets)
        {
            menu.AddItem(new GUIContent("Copy to Clipboard"), false, () =>
            {
                var value = GetValue(targets);
                EditorGUIUtility.systemCopyBuffer = value != null ? value.ToString() : "null";
            });

            menu.AddItem(new GUIContent("Log Value"), false, () =>
            {
                MemberInfo member = _Field;
                if (member == null)
                    member = _Property;

                var value = GetValue(targets);

                Debug.Log(member.GetNameCS() + ": " + value, targets[0]);
            });
        }

        /************************************************************************************************************************/
#endif
    }
}

