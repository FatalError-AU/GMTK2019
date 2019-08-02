// Inspector Gadgets // Copyright 2019 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Attributes;
using InspectorGadgets.Editor.PropertyDrawers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor
{
    internal static class SerializedPropertyContextMenu
    {
        /************************************************************************************************************************/

        public static readonly FloatHandler FloatMenuHandler = new FloatHandler();
        public static readonly Vector2Handler Vector2MenuHandler = new Vector2Handler();
        public static readonly Vector3Handler Vector3MenuHandler = new Vector3Handler();
        public static readonly Vector4Handler Vector4MenuHandler = new Vector4Handler();
        public static readonly QuaternionHandler QuaternionMenuHandler = new QuaternionHandler();

        private static readonly Dictionary<SerializedPropertyType, ContextMenuHandler>
            ContextMenuHandlers = new Dictionary<SerializedPropertyType, ContextMenuHandler>
            {
                { SerializedPropertyType.Boolean, new BoolHandler() },
                { SerializedPropertyType.ArraySize, new BaseIntHandler() },
                { SerializedPropertyType.Integer, new IntHandler() },
                { SerializedPropertyType.String, new StringHandler() },
                { SerializedPropertyType.Float, FloatMenuHandler },
                { SerializedPropertyType.Vector2, Vector2MenuHandler },
                { SerializedPropertyType.Vector3, Vector3MenuHandler },
                { SerializedPropertyType.Vector4, Vector4MenuHandler },
                { SerializedPropertyType.Quaternion, QuaternionMenuHandler },
                { SerializedPropertyType.Rect, new RectHandler() },
                { SerializedPropertyType.Bounds, new BoundsHandler() },
                { SerializedPropertyType.Color, new ColorHandler() },
                { SerializedPropertyType.Enum, new EnumHandler() },
                { SerializedPropertyType.AnimationCurve, new AnimationCurveHandler() },
                { SerializedPropertyType.ObjectReference, new ObjectReferenceHandler() },
                { SerializedPropertyType.Generic, new GenericHandler() },
            };

        /************************************************************************************************************************/

        [InitializeOnLoadMethod]
        private static void OnPropertyContextMenu()
        {
            EditorApplication.contextualPropertyMenu += (menu, property) =>
            {

                // Cache the serialized object and property path since the 'property' itself will be reused and no
                // longer point to the right place by the time any of the menu functions are called.
                property = property.Copy();

                if (TransformPropertyDrawer.CurrentlyDrawing != null)
                {
                    TransformPropertyDrawer.CurrentlyDrawing.OnPropertyContextMenu(menu, property);
                }
                else
                {
                    if (property.serializedObject.targetObject is RectTransform)
                    {
                        if (AddRectTransformItems(menu, property))
                        {
                            AddWatchItem(menu, property);
                            return;
                        }
                    }

                    ContextMenuHandler handler;
                    if (ContextMenuHandlers.TryGetValue(property.propertyType, out handler))
                    {
                        handler.AddItems(menu, property);
                    }
                }

                AddWatchItem(menu, property);
            };
        }

        /************************************************************************************************************************/

        private static void AddWatchItem(GenericMenu menu, SerializedProperty property)
        {
            menu.AddItem(new GUIContent("Watch"), false, () => WatcherWindow.Watch(property));
        }

        /************************************************************************************************************************/

        // Unsupported: LayerMask, Character, Gradient, ExposedReference, FixedBufferSize.

        // Character uses (char)property.intValue

        // case SerializedPropertyType.LayerMask:
        //      SerializedProperty.layerMaskStringValue
        //      int[] GetLayerMaskSelectedIndex();
        //      string[] GetLayerMaskNames();
        //     break;

        // case SerializedPropertyType.Gradient:
        //      SerializedProperty.gradientValue
        //     break;

        /************************************************************************************************************************/
        #region Context Menu Handlers
        /************************************************************************************************************************/
        #region Rect Transform
        /************************************************************************************************************************/

        private static bool AddRectTransformItems(GenericMenu menu, SerializedProperty property)
        {
            switch (property.propertyPath)
            {
                case "m_AnchoredPosition.x": AddRectPositionItems(menu, property, 0, "RectTransform.anchoredPosition.x"); break;
                case "m_AnchoredPosition.y": AddRectPositionItems(menu, property, 1, "RectTransform.anchoredPosition.y"); break;

                case "m_SizeDelta.x": AddRectPositionItems(menu, property, 0, "RectTransform.sizeDelta.x"); break;
                case "m_SizeDelta.y": AddRectPositionItems(menu, property, 1, "RectTransform.sizeDelta.y"); break;

                case "m_LocalPosition.z": AddRectPositionItems(menu, property, 2, "Transform.localPosition.z"); break;

                case "m_AnchorMin": AddRectVector2Items(menu, property, "RectTransform.anchorMin"); break;
                case "m_AnchorMin.x": AddRectFloatItems(menu, property, "RectTransform.anchorMin.x"); break;
                case "m_AnchorMin.y": AddRectFloatItems(menu, property, "RectTransform.anchorMin.y"); break;

                case "m_AnchorMax": AddRectVector2Items(menu, property, "RectTransform.anchorMax"); break;
                case "m_AnchorMax.x": AddRectFloatItems(menu, property, "RectTransform.anchorMax.x"); break;
                case "m_AnchorMax.y": AddRectFloatItems(menu, property, "RectTransform.anchorMax.y"); break;

                case "m_Pivot": AddRectVector2Items(menu, property, "RectTransform.pivot"); break;
                case "m_Pivot.x": AddRectFloatItems(menu, property, "RectTransform.pivot.x"); break;
                case "m_Pivot.y": AddRectFloatItems(menu, property, "RectTransform.pivot.y"); break;

                case "m_LocalRotation": AddRectRotationItems(menu, property); break;

                case "m_LocalScale": AddRectScaleVectorItems(menu, property); break;
                case "m_LocalScale.x": AddRectScaleFloatItems(menu, property); break;
                case "m_LocalScale.y": AddRectScaleFloatItems(menu, property); break;
                case "m_LocalScale.z": AddRectScaleFloatItems(menu, property); break;

                default: return false;
            }

            return true;
        }

        /************************************************************************************************************************/

        private static void AddRectPositionItems(GenericMenu menu, SerializedProperty property, int axis, string name)
        {
            menu.AddDisabledItem(new GUIContent("float      " + name));

            FloatMenuHandler.AddClipboardFunctions(menu, property);

            menu.AddSeparator("");
            FloatHandler.AddSetFunction(menu, property);
            FloatHandler.AddNegateFunction(menu, property);
            FloatHandler.AddRoundFunction(menu, property);

            PositionDrawer.AddSnapFloatToGridItem(menu, property, axis);

            AddRectSnapToSiblingsItems(menu, property, axis);

            FloatMenuHandler.AddLogFunction(menu, property);
        }

        /************************************************************************************************************************/

        private static void AddRectVector2Items(GenericMenu menu, SerializedProperty property, string name)
        {
            menu.AddDisabledItem(new GUIContent("Vector2      " + name));
            Vector2MenuHandler.AddClipboardFunctions(menu, property);
            AddRectVectorAlignmentItems(menu, property);
            Vector2MenuHandler.AddLogFunction(menu, property);
        }

        /************************************************************************************************************************/

        private static void AddRectVectorAlignmentItems(GenericMenu menu, SerializedProperty property)
        {
            menu.AddSeparator("");

            AddPropertyModifierFunction(menu, property, "Bottom Left (0, 0)", () => property.vector2Value = new Vector2(0, 0));
            AddPropertyModifierFunction(menu, property, "Bottom Center (0.5, 0)", () => property.vector2Value = new Vector2(0.5f, 0));
            AddPropertyModifierFunction(menu, property, "Bottom Right (1, 0)", () => property.vector2Value = new Vector2(1, 0));

            AddPropertyModifierFunction(menu, property, "Middle Left (0, 0.5)", () => property.vector2Value = new Vector2(0, 0.5f));
            AddPropertyModifierFunction(menu, property, "Middle Center (0.5, 0.5)", () => property.vector2Value = new Vector2(0.5f, 0.5f));
            AddPropertyModifierFunction(menu, property, "Middle Right (1, 0.5)", () => property.vector2Value = new Vector2(1, 0.5f));

            AddPropertyModifierFunction(menu, property, "Top Left (0, 1)", () => property.vector2Value = new Vector2(0, 1));
            AddPropertyModifierFunction(menu, property, "Top Center (0.5, 1)", () => property.vector2Value = new Vector2(0.5f, 1));
            AddPropertyModifierFunction(menu, property, "Top Right (1, 1)", () => property.vector2Value = new Vector2(1, 1));

        }

        /************************************************************************************************************************/

        private static void AddRectFloatItems(GenericMenu menu, SerializedProperty property, string name)
        {
            menu.AddDisabledItem(new GUIContent("float      " + name));
            FloatMenuHandler.AddClipboardFunctions(menu, property);
            AddRectFloatAlignmentItems(menu, property);
            FloatMenuHandler.AddLogFunction(menu, property);
        }

        /************************************************************************************************************************/

        private static void AddRectFloatAlignmentItems(GenericMenu menu, SerializedProperty property)
        {
            menu.AddSeparator("");
            FloatHandler.AddSetFunction(menu, property);
            FloatHandler.AddSetFunction(menu, property, "Half (0.5)", 0.5f);
            FloatHandler.AddSetFunction(menu, property, "One (1)", 1);
        }

        /************************************************************************************************************************/

        private static void AddRectRotationItems(GenericMenu menu, SerializedProperty property)
        {
            menu.AddDisabledItem(new GUIContent("Quaternion      RectTransform.localRotation"));

            QuaternionMenuHandler.AddClipboardFunctions(menu, property);

            QuaternionMenuHandler.AddCustomItems(menu, property);
            AddPropertyModifierFunction(menu, property, "Randomize Z", (targetProperty) =>
            {
                var euler = targetProperty.quaternionValue.eulerAngles;
                euler.z = UnityEngine.Random.Range(0, 360f);
                targetProperty.quaternionValue = Quaternion.Euler(euler);
            });
            RotationDrawer.AddSnapQuaternionToGridFunction(menu, property);

            QuaternionMenuHandler.AddLogFunction(menu, property);
        }

        /************************************************************************************************************************/

        private static void AddRectScaleVectorItems(GenericMenu menu, SerializedProperty property)
        {
            menu.AddDisabledItem(new GUIContent("Vector3      RectTransform.localScale"));

            Vector3MenuHandler.AddClipboardFunctions(menu, property);

            menu.AddSeparator("");
            AddPropertyModifierFunction(menu, property, "Zero (0, 0, 0)", () => property.vector3Value = Vector3.zero);
            AddPropertyModifierFunction(menu, property, "One (1, 1, 1)", () => property.vector3Value = Vector3.one);
            AddPropertyModifierFunction(menu, property, "Negate", (targetProperty) => targetProperty.vector3Value *= -1);

            AddPropertyModifierFunction(menu, property, "Average", (targetProperty) =>
            {
                var value = targetProperty.vector3Value;
                value.x = value.y = value.z = (value.x + value.y + value.z) / 3;
                targetProperty.vector3Value = value;
            });

            AddPropertyModifierFunction(menu, property, "Randomize 0.5-1.5 (Uniform)", (targetProperty) =>
            {
                var value = UnityEngine.Random.value;
                targetProperty.vector3Value = new Vector3(value, value, value);
            });
            AddPropertyModifierFunction(menu, property, "Randomize 0.5-1.5 (Non-Uniform)", (targetProperty) =>
            {
                targetProperty.vector3Value = new Vector3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
            });

            ScaleDrawer.AddSnapVectorToGridItem(menu, property);

            Vector3MenuHandler.AddLogFunction(menu, property);
        }

        /************************************************************************************************************************/

        private static void AddRectScaleFloatItems(GenericMenu menu, SerializedProperty property)
        {
            menu.AddDisabledItem(new GUIContent("float      RectTransform.localScale." + property.name));

            FloatMenuHandler.AddClipboardFunctions(menu, property);

            menu.AddSeparator("");
            FloatHandler.AddSetFunction(menu, property);
            FloatHandler.AddSetFunction(menu, property, "One (1)", 1);
            AddPropertyModifierFunction(menu, property, "Negate", (targetProperty) => targetProperty.floatValue *= -1);

            AddPropertyModifierFunction(menu, property, "Randomize 0.5-1.5", (targetProperty) =>
            {
                targetProperty.floatValue = UnityEngine.Random.value;
            });

            ScaleDrawer.AddSnapFloatToGridItem(menu, property);

            FloatMenuHandler.AddLogFunction(menu, property);
        }

        /************************************************************************************************************************/
        #region Snap to Siblings
        /************************************************************************************************************************/

        private static void AddRectSnapToSiblingsItems(GenericMenu menu, SerializedProperty property, int axis)
        {
            menu.AddSeparator("");

            AddSquarifyItem(menu, property, "Set Width = Height", 0, (rect) => rect.height - rect.width);
            AddSquarifyItem(menu, property, "Set Height = Width", 1, (rect) => rect.width - rect.height);

            menu.AddItem(new GUIContent("Snap to Siblings/Right"), false, () => SnapToSiblings(property, 0));
            menu.AddItem(new GUIContent("Snap to Siblings/Up"), false, () => SnapToSiblings(property, 1));
            menu.AddItem(new GUIContent("Snap to Siblings/Left"), false, () => SnapToSiblings(property, 2));
            menu.AddItem(new GUIContent("Snap to Siblings/Down"), false, () => SnapToSiblings(property, 3));
        }

        /************************************************************************************************************************/

        private static void AddSquarifyItem(GenericMenu menu, SerializedProperty property, string label, int setAxis, Func<Rect, float> calculateResize)
        {
            AddPropertyModifierFunction(menu, property, label, (targetProperty) =>
            {
                var transform = targetProperty.serializedObject.targetObject as RectTransform;
                var rect = transform.rect;
                if (rect.width != rect.height)
                {
                    var sizeDelta = transform.sizeDelta;
                    sizeDelta[setAxis] += calculateResize(rect);
                    transform.sizeDelta = sizeDelta;
                }
            });
        }

        /************************************************************************************************************************/

        private static Vector3[] _FourCorners;

        private static float GetEdge(RectTransform transform, int direction)
        {
            // Right, Up, Left, Down.

            const int Corners = 4;

            if (_FourCorners == null)
                _FourCorners = new Vector3[Corners];

            transform.GetLocalCorners(_FourCorners);

            int i;

            // Transform the corners into the parent's local space.
            for (i = 0; i < Corners; i++)
            {
                var corner = _FourCorners[i];
                corner = transform.TransformPoint(corner);
                if (transform.parent != null)
                    corner = transform.parent.InverseTransformPoint(corner);
                _FourCorners[i] = corner;
            }

            var axis = direction % 2;
            var isPositiveDirection = IsPositiveDirection(direction);

            // Find the edge furthest in the target direction.
            var edge = _FourCorners[0][axis];

            i = 1;
            for (; i < Corners; i++)
            {
                var corner = _FourCorners[i][axis];

                if (isPositiveDirection)
                {
                    if (edge < corner)
                        edge = corner;
                }
                else
                {
                    if (edge > corner)
                        edge = corner;
                }
            }

            return edge;
        }

        private static bool IsPositiveDirection(int direction)
        {
            return (direction % 4) < 2;
        }

        private static int CompareEdges(RectTransform a, RectTransform b, int direction)
        {
            var result = GetEdge(a, direction).CompareTo(GetEdge(b, direction));
            return IsPositiveDirection(direction) ? -result : result;
        }

        /************************************************************************************************************************/

        private static void SnapToSiblings(SerializedProperty property, int direction)
        {
            var selection = GetSortedTargetRects(property, (a, b) => CompareEdges(a, b, direction));

            var hasRecordedUndo = false;

            for (int i = 0; i < selection.Count; i++)
            {
                var transform = selection[i];
                var edge = GetEdge(transform, direction);
                var nextEdge = GetNextEdge(edge, transform, direction);

                if (nextEdge != edge)
                {
                    if (!hasRecordedUndo)
                    {
                        hasRecordedUndo = true;
                        Undo.RecordObjects(selection.ToArray(), "Snap to Siblings");
                    }

                    var anchoredPosition = transform.anchoredPosition;
                    anchoredPosition[direction % 2] += nextEdge - edge;
                    transform.anchoredPosition = anchoredPosition;
                }
            }
        }

        /************************************************************************************************************************/

        private static List<RectTransform> GetSortedTargetRects(SerializedProperty property, Comparison<RectTransform> comparison)
        {
            var targetObjects = property.serializedObject.targetObjects;

            var rects = new List<RectTransform>();
            for (int i = 0; i < targetObjects.Length; i++)
            {
                var rect = targetObjects[i] as RectTransform;
                if (rect != null && rect.parent != null)
                    rects.Add(rect);
            }

            rects.Sort(comparison);

            return rects;
        }

        /************************************************************************************************************************/

        private static float GetNextEdge(float current, RectTransform transform, int direction)
        {
            var positiveDirection = IsPositiveDirection(direction);

            float nextEdge;
            bool foundEdge;

            var parentRect = transform.parent as RectTransform;
            if (parentRect != null)
            {
                nextEdge = GetEdge(parentRect, direction) - parentRect.localPosition[direction % 2];
                foundEdge = true;
            }
            else
            {
                nextEdge = positiveDirection ? float.PositiveInfinity : float.NegativeInfinity;
                foundEdge = false;
            }

            var minSideDirection = direction + 1;
            if (IsPositiveDirection(minSideDirection))
                minSideDirection += 2;

            var maxSideDirection = minSideDirection + 2;

            var minSide = GetEdge(transform, minSideDirection);
            var maxSide = GetEdge(transform, maxSideDirection);

            direction += 2;
            foreach (RectTransform child in transform.parent)
            {
                // Ignore objects that aren't in line with the target.
                if (child == transform ||
                    GetEdge(child, minSideDirection) > maxSide ||
                    GetEdge(child, maxSideDirection) < minSide)
                    continue;

                var edge = GetEdge(child, direction);

                const float Leeway = 0.1f;

                if (positiveDirection)
                {
                    if (nextEdge > edge && edge > current - Leeway)
                    {
                        nextEdge = edge;
                        foundEdge = true;
                    }
                }
                else
                {
                    if (nextEdge < edge && edge < current + Leeway)
                    {
                        nextEdge = edge;
                        foundEdge = true;
                    }
                }
            }

            return foundEdge ? nextEdge : current;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        public abstract class ContextMenuHandler
        {
            private MethodCache.OnPropertyContextMenuMethod _CustomEvent;

            public void AddItems(GenericMenu menu, SerializedProperty property)
            {
                var typeName = GetTypeName(property);

                menu.AddDisabledItem(new GUIContent(typeName + "      " + Strings.NegateShortcut + property.propertyPath));

                AddClipboardFunctions(menu, property, typeName);
                AddCustomItems(menu, property);
                AddLogFunction(menu, property);

                if (_CustomEvent == null)
                    _CustomEvent = MethodCache.OnPropertyContextMenu.GetDelegate(property.serializedObject.targetObject);

                if (_CustomEvent != null)
                    _CustomEvent(menu, property);
            }

            public abstract string GetTypeName(SerializedProperty property);

            public abstract void AddCustomItems(GenericMenu menu, SerializedProperty property);

            public virtual void AddLogFunction(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");
                AddLogValueFunction(menu, property, GetValueString);
            }

            public abstract string GetValueString(SerializedProperty property);

            public virtual void AddClipboardFunctions(GenericMenu menu, SerializedProperty property, string typeName)
            {
#if UNITY_2017_3_OR_NEWER
                PersistentValues.AddMenuItem(menu, property);
#endif
            }

            public void AddClipboardFunctions(GenericMenu menu, SerializedProperty property)
            {
                AddClipboardFunctions(menu, property, GetTypeName(property));
            }
        }

        /************************************************************************************************************************/

        public abstract class ContextMenuHandler<T> : ContextMenuHandler
        {
            public override void AddClipboardFunctions(GenericMenu menu, SerializedProperty property, string typeName)
            {
                menu.AddSeparator("");

                var content = new GUIContent("Copy " + typeName);
                if (ShouldEnableCopy(property))
                    menu.AddItem(content, false, () => CopyValue(property));
                else
                    menu.AddDisabledItem(content);

                content = new GUIContent("Paste: " + GetClipboardString(property));
                if (ShouldEnablePaste(property))
                    AddPropertyModifierFunction(menu, property, content.text, () => PasteValue(property));
                else
                    menu.AddDisabledItem(content);

                base.AddClipboardFunctions(menu, property, typeName);
            }

            public virtual bool ShouldEnableCopy(SerializedProperty property)
            {
                return !property.hasMultipleDifferentValues;
            }

            public virtual bool ShouldEnablePaste(SerializedProperty property)
            {
                return true;
            }

            public abstract void CopyValue(SerializedProperty property);
            public abstract void PasteValue(SerializedProperty property);
            public abstract bool TryParse(string value, out T result);
            public abstract string GetClipboardString(SerializedProperty property);
        }

        /************************************************************************************************************************/

        public abstract class ContextMenuHandlerWithClipboard<T> : ContextMenuHandler<T>
        {
            protected static T _Clipboard;

            public T Clipboard
            {
                get
                {
                    T value;
                    if (TryParse(EditorGUIUtility.systemCopyBuffer, out value))
                        _Clipboard = value;

                    return _Clipboard;
                }
                set
                {
                    _Clipboard = value;
                    var clipboardString = GetClipboardString(_Clipboard);
                    if (clipboardString != null)
                        EditorGUIUtility.systemCopyBuffer = clipboardString;
                }
            }

            public virtual string GetClipboardString(T value)
            {
                return value != null ? value.ToString().AllBackslashes() : "null";
            }

            public override string GetClipboardString(SerializedProperty property)
            {
                return GetClipboardString(Clipboard);
            }
        }

        /************************************************************************************************************************/

        public sealed class BoolHandler : ContextMenuHandlerWithClipboard<bool>
        {
            public override string GetTypeName(SerializedProperty property)
            {
                return "bool";
            }

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");
                AddPropertyModifierFunction(menu, property, "Randomize",
                    (targetProperty) => targetProperty.boolValue = UnityEngine.Random.Range(0, 2) != 0);
            }

            public override string GetValueString(SerializedProperty property)
            {
                return property.boolValue.ToString();
            }

            /************************************************************************************************************************/
            #region Clipboard
            /************************************************************************************************************************/

            public override void CopyValue(SerializedProperty property)
            {
                Clipboard = property.boolValue;
            }

            public override void PasteValue(SerializedProperty property)
            {
                property.boolValue = Clipboard;
            }

            public override bool TryParse(string value, out bool result)
            {
                return bool.TryParse(value, out result);
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public class BaseIntHandler : ContextMenuHandlerWithClipboard<int>
        {
            public override string GetTypeName(SerializedProperty property)
            {
                return "int";
            }

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
            }

            public override string GetValueString(SerializedProperty property)
            {
                return property.intValue.ToString();
            }

            /************************************************************************************************************************/
            #region Clipboard
            /************************************************************************************************************************/

            public override void CopyValue(SerializedProperty property)
            {
                Clipboard = property.intValue;
            }

            public override void PasteValue(SerializedProperty property)
            {
                property.intValue = Clipboard;
            }

            public override bool TryParse(string value, out int result)
            {
                return int.TryParse(value, out result);
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class IntHandler : BaseIntHandler
        {
            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");

                var accessor = SerializedPropertyAccessor.GetAccessor(property);
                if (accessor != null)
                {
                    var field = accessor.Field;
                    if (field != null)
                    {
                        var intRange = field.GetCustomAttribute<RangeAttribute>(true);
                        if (intRange != null)
                        {
                            AddPropertyModifierFunction(menu, property, "Randomize",
                                (targetProperty) => targetProperty.intValue = UnityEngine.Random.Range((int)intRange.min, (int)intRange.max));
                            return;
                        }
                    }
                }

                AddPropertyModifierFunction(menu, property, "Negate",
                    (targetProperty) => targetProperty.intValue *= -1);
                AddPropertyModifierFunction(menu, property, "Randomize 0-1",
                    (targetProperty) => targetProperty.intValue = UnityEngine.Random.Range(0, 2));
                AddPropertyModifierFunction(menu, property, "Randomize 0-99",
                    (targetProperty) => targetProperty.intValue = UnityEngine.Random.Range(0, 100));
            }
        }

        /************************************************************************************************************************/

        public sealed class StringHandler : ContextMenuHandlerWithClipboard<string>
        {
            public override string GetTypeName(SerializedProperty property)
            {
                return "string";
            }

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");
                AddPropertyModifierFunction(menu, property, "To Lower", (targetProperty) => targetProperty.stringValue = targetProperty.stringValue.ToLower());
                AddPropertyModifierFunction(menu, property, "To Upper", (targetProperty) => targetProperty.stringValue = targetProperty.stringValue.ToUpper());
            }

            public override string GetValueString(SerializedProperty property)
            {
                return property.stringValue;
            }

            /************************************************************************************************************************/
            #region Clipboard
            /************************************************************************************************************************/

            public override void CopyValue(SerializedProperty property)
            {
                Clipboard = property.stringValue;
            }

            public override void PasteValue(SerializedProperty property)
            {
                property.stringValue = Clipboard;
            }

            public override bool TryParse(string value, out string result)
            {
                result = value;
                return !string.IsNullOrEmpty(value);
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class FloatHandler : ContextMenuHandlerWithClipboard<NullableVector4>
        {
            public override string GetTypeName(SerializedProperty property)
            {
                return "float";
            }

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");

                var accessor = SerializedPropertyAccessor.GetAccessor(property);
                if (accessor != null)
                {
                    var field = accessor.Field;
                    if (field != null)
                    {
                        var floatRange = field.GetCustomAttribute<RangeAttribute>(true);
                        if (floatRange != null)
                        {
                            AddPropertyModifierFunction(menu, property, "Randomize",
                                (targetProperty) => targetProperty.floatValue = UnityEngine.Random.Range(floatRange.min, floatRange.max));

                            if ((int)floatRange.min != (int)floatRange.max)
                            {
                                AddPropertyModifierFunction(menu, property, "Round to Integer",
                                    (targetProperty) => targetProperty.floatValue = Mathf.Clamp(Mathf.Round(targetProperty.floatValue), floatRange.min, floatRange.max));
                            }

                            return;
                        }
                    }
                }

                AddSetFunction(menu, property);
                AddNegateFunction(menu, property);
                AddRoundFunction(menu, property);
                AddPropertyModifierFunction(menu, property, "Randomize 0-1", (targetProperty) => targetProperty.floatValue = UnityEngine.Random.value);
                AddPropertyModifierFunction(menu, property, "Randomize 0-100", (targetProperty) => targetProperty.floatValue = UnityEngine.Random.Range(0f, 100f));
                AddPropertyModifierFunction(menu, property, "Randomize 0-360", (targetProperty) => targetProperty.floatValue = UnityEngine.Random.Range(0f, 360f));
                AddPropertyModifierFunction(menu, property, "Degrees to Radians", (targetProperty) => targetProperty.floatValue *= Mathf.Deg2Rad);
                AddPropertyModifierFunction(menu, property, "Radians to Degrees", (targetProperty) => targetProperty.floatValue *= Mathf.Rad2Deg);
                AddPropertyModifierFunction(menu, property, "Round to Integer", (targetProperty) => targetProperty.floatValue = Mathf.Round(targetProperty.floatValue));

                menu.AddSeparator("");
                PropertyVisualiserWindow.AddVisualiseItem<FloatVisualiserWindow>(menu, property);
            }

            public override string GetValueString(SerializedProperty property)
            {
                return property.floatValue.ToString(CultureInfo.InvariantCulture);
            }

            public static void AddSetFunction(GenericMenu menu, SerializedProperty property, string label = "Zero (0)", float value = 0)
            {
                AddPropertyModifierFunction(menu, property, label, (targetProperty) => targetProperty.floatValue = value);
            }

            public static void AddNegateFunction(GenericMenu menu, SerializedProperty property)
            {
                AddPropertyModifierFunction(menu, property, "Negate", (targetProperty) => targetProperty.floatValue *= -1);
            }

            public static void AddRoundFunction(GenericMenu menu, SerializedProperty property)
            {
                AddPropertyModifierFunction(menu, property, "Round to Int", (targetProperty) => targetProperty.floatValue = Mathf.Round(targetProperty.floatValue));
            }

            /************************************************************************************************************************/
            #region Clipboard
            /************************************************************************************************************************/

            public void SetClipboard(float value)
            {
                Clipboard = new NullableVector4(value, 0, 0, 0);
            }

            public override bool ShouldEnablePaste(SerializedProperty property)
            {
                return !Clipboard.AllNull(1);
            }

            public override void CopyValue(SerializedProperty property)
            {
                SetClipboard(property.floatValue);
            }

            public override void PasteValue(SerializedProperty property)
            {
                var value = Clipboard[0];
                if (value != null)
                    property.floatValue = value.Value;
            }

            public override bool TryParse(string value, out NullableVector4 result)
            {
                return Vector4MenuHandler.TryParse(value, out result);
            }

            public override string GetClipboardString(NullableVector4 value)
            {
                Vector4Handler.EnsureNotNull(ref value);
                return value[0].ToDisplayString();
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class Vector2Handler : ContextMenuHandlerWithClipboard<NullableVector4>
        {
            public override string GetTypeName(SerializedProperty property)
            {
                return "Vector2";
            }

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");

                AddPropertyModifierFunction(menu, property, "Zero (0, 0)", () => property.vector2Value = Vector2.zero);
                AddPropertyModifierFunction(menu, property, "Right (1, 0)", () => property.vector2Value = Vector2.right);
                AddPropertyModifierFunction(menu, property, "Up (0, 1)", () => property.vector2Value = Vector2.up);
                AddPropertyModifierFunction(menu, property, "One (1, 1)", () => property.vector2Value = Vector2.one);

                AddPropertyModifierFunction(menu, property, "Negate", (targetProperty) => targetProperty.vector2Value *= -1);

                AddPropertyModifierFunction(menu, property, "Normalize", (targetProperty) => targetProperty.vector2Value = targetProperty.vector2Value.normalized);

                AddPropertyModifierFunction(menu, property, "Randomize Inside Unit Circle", (targetProperty) => targetProperty.vector2Value = UnityEngine.Random.insideUnitCircle);

                menu.AddSeparator("");
                PropertyVisualiserWindow.AddVisualiseItem<Vector2VisualiserWindow>(menu, property);
            }

            public override string GetValueString(SerializedProperty property)
            {
                return GetClipboardString(Vector4Handler.GetClipboardArray(property.vector2Value));
            }

            /************************************************************************************************************************/
            #region Clipboard
            /************************************************************************************************************************/

            public override bool ShouldEnableCopy(SerializedProperty property)
            {
                return HasAnySharedValues(property);
            }

            public override void CopyValue(SerializedProperty property)
            {
                var value = new NullableVector4(property.vector2Value);

                if (property.FindPropertyRelative("x").hasMultipleDifferentValues)
                    value.x = null;

                if (property.FindPropertyRelative("y").hasMultipleDifferentValues)
                    value.y = null;

                Clipboard = value;
            }

            public override bool ShouldEnablePaste(SerializedProperty property)
            {
                return !Clipboard.AllNull(2);
            }

            public override void PasteValue(SerializedProperty property)
            {
                property.vector2Value = Vector4MenuHandler.GetClipboardVector(property.vector2Value);
            }

            public override bool TryParse(string value, out NullableVector4 result)
            {
                return Vector4MenuHandler.TryParse(value, out result);
            }

            public override string GetClipboardString(NullableVector4 value)
            {
                Vector4Handler.EnsureNotNull(ref value);
                return value.ToString(2);
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class Vector3Handler : ContextMenuHandlerWithClipboard<NullableVector4>
        {
            public override string GetTypeName(SerializedProperty property)
            {
                return "Vector3";
            }

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");

                var accessor = SerializedPropertyAccessor.GetAccessor(property);
                if (accessor != null)
                {
                    var field = accessor.Field;
                    if (field != null && !field.IsDefined(typeof(EulerAttribute), true))
                    {
                        AddPropertyModifierFunction(menu, property, "Zero (0, 0, 0)", () => property.vector3Value = Vector3.zero);
                        AddPropertyModifierFunction(menu, property, "Right (1, 0, 0)", () => property.vector3Value = Vector3.right);
                        AddPropertyModifierFunction(menu, property, "Up (0, 1, 0)", () => property.vector3Value = Vector3.up);
                        AddPropertyModifierFunction(menu, property, "Forward (0, 0, 1)", () => property.vector3Value = Vector3.forward);
                        AddPropertyModifierFunction(menu, property, "One (1, 1, 1)", () => property.vector3Value = Vector3.one);

                        AddPropertyModifierFunction(menu, property, "Normalize", (targetProperty) => targetProperty.vector3Value = targetProperty.vector3Value.normalized);
                    }
                }

                AddPropertyModifierFunction(menu, property, "Negate", (targetProperty) => targetProperty.vector3Value *= -1);

                AddPropertyModifierFunction(menu, property, "Randomize Inside Unit Sphere", (targetProperty) => targetProperty.vector3Value = UnityEngine.Random.insideUnitSphere);
                AddPropertyModifierFunction(menu, property, "Randomize On Unit Sphere", (targetProperty) => targetProperty.vector3Value = UnityEngine.Random.onUnitSphere);
                AddPropertyModifierFunction(menu, property, "Randomize Euler Angles", (targetProperty) => targetProperty.vector3Value = UnityEngine.Random.rotationUniform.eulerAngles);

                menu.AddSeparator("");
                PropertyVisualiserWindow.AddVisualiseItem<Vector3VisualiserWindow>(menu, property);
            }

            public override string GetValueString(SerializedProperty property)
            {
                return GetClipboardString(Vector4Handler.GetClipboardArray(property.vector3Value));
            }

            /************************************************************************************************************************/
            #region Clipboard
            /************************************************************************************************************************/

            public void SetClipboard(Vector3 value)
            {
                Vector4MenuHandler.SetClipboard(value);
            }

            public override bool ShouldEnableCopy(SerializedProperty property)
            {
                return HasAnySharedValues(property);
            }

            public override void CopyValue(SerializedProperty property)
            {
                var value = new NullableVector4(property.vector3Value);

                if (property.FindPropertyRelative("x").hasMultipleDifferentValues)
                    value.x = null;

                if (property.FindPropertyRelative("y").hasMultipleDifferentValues)
                    value.y = null;

                if (property.FindPropertyRelative("z").hasMultipleDifferentValues)
                    value.z = null;

                Clipboard = value;
            }

            public override bool ShouldEnablePaste(SerializedProperty property)
            {
                return !Clipboard.AllNull(3);
            }

            public override void PasteValue(SerializedProperty property)
            {
                property.vector3Value = Vector4MenuHandler.GetClipboardVector(property.vector3Value);
            }

            public override bool TryParse(string value, out NullableVector4 result)
            {
                return Vector4MenuHandler.TryParse(value, out result);
            }

            public override string GetClipboardString(NullableVector4 value)
            {
                Vector4Handler.EnsureNotNull(ref value);
                return string.Concat("(",
                    value[0].ToDisplayString(), ", ",
                    value[1].ToDisplayString(), ", ",
                    value[2].ToDisplayString(), ")");
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class Vector4Handler : ContextMenuHandlerWithClipboard<NullableVector4>
        {
            public override string GetTypeName(SerializedProperty property)
            {
                return "Vector4";
            }

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");

                AddPropertyModifierFunction(menu, property, "Zero (0, 0, 0, 0)", () => property.vector4Value = Vector4.zero);
                AddPropertyModifierFunction(menu, property, "One (1, 1, 1, 1)", () => property.vector4Value = Vector4.one);

                AddPropertyModifierFunction(menu, property, "Negate", (targetProperty) => targetProperty.vector4Value *= -1);

                AddPropertyModifierFunction(menu, property, "Normalize", (targetProperty) => targetProperty.vector4Value = targetProperty.vector4Value.normalized);
            }

            public override string GetValueString(SerializedProperty property)
            {
                return GetClipboardString(GetClipboardArray(property.vector4Value));
            }

            /************************************************************************************************************************/
            #region Clipboard
            /************************************************************************************************************************/

            public Vector4 GetClipboardVector(Vector4 fallback)
            {
                Clipboard[0].Set(ref fallback.x);
                Clipboard[1].Set(ref fallback.y);
                Clipboard[2].Set(ref fallback.z);
                Clipboard[3].Set(ref fallback.w);
                return fallback;
            }

            public void SetClipboard(Vector4 value)
            {
                Clipboard = GetClipboardArray(value);
            }

            public static NullableVector4 GetClipboardArray(Vector4 value)
            {
                return new NullableVector4(value);
            }

            public override bool ShouldEnableCopy(SerializedProperty property)
            {
                return HasAnySharedValues(property);
            }

            public override bool ShouldEnablePaste(SerializedProperty property)
            {
                return !Clipboard.AllNull();
            }

            public override void CopyValue(SerializedProperty property)
            {
                var value = new NullableVector4(property.vector4Value);

                if (property.FindPropertyRelative("x").hasMultipleDifferentValues)
                    value.x = null;

                if (property.FindPropertyRelative("y").hasMultipleDifferentValues)
                    value.y = null;

                if (property.FindPropertyRelative("z").hasMultipleDifferentValues)
                    value.z = null;

                if (property.FindPropertyRelative("w").hasMultipleDifferentValues)
                    value.w = null;

                Clipboard = value;
            }

            public override void PasteValue(SerializedProperty property)
            {
                property.vector4Value = GetClipboardVector(property.vector4Value);
            }

            public override bool TryParse(string value, out NullableVector4 result)
            {
                if (NullableVector4.TryParse(value, 4, out result) >= 0)
                {
                    return true;
                }
                else if (_Clipboard == null)
                {
                    result = new NullableVector4(0, 0, 0, 0);
                    return true;
                }
                else return false;
            }

            public static void EnsureNotNull(ref NullableVector4 value)
            {
                if (value == null)
                    value = new NullableVector4(0, 0, 0, 0);
            }

            public override string GetClipboardString(NullableVector4 value)
            {
                EnsureNotNull(ref value);
                return value.ToString();
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class QuaternionHandler : ContextMenuHandlerWithClipboard<NullableVector4>
        {
            public override string GetTypeName(SerializedProperty property)
            {
                return "Quaternion";
            }

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");
                AddPropertyModifierFunction(menu, property, "Identity (Euler 0, 0, 0)", () => property.quaternionValue = Quaternion.identity);

                AddPropertyModifierFunction(menu, property, "Negate", (targetProperty) =>
                {
                    var euler = targetProperty.quaternionValue.eulerAngles;
                    euler *= -1;
                    targetProperty.quaternionValue = Quaternion.Euler(euler);
                });

                AddPropertyModifierFunction(menu, property, "Randomize Rotation", (targetProperty) => targetProperty.quaternionValue = UnityEngine.Random.rotationUniform);
            }

            public override void AddLogFunction(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");
                AddLogValueFunction(menu, property, GetValueString, "Log Euler Angles");
                AddLogValueFunction(menu, property, (targetProperty) => GetFullString(targetProperty.quaternionValue), "Log Quaternion");
            }

            public override string GetValueString(SerializedProperty property)
            {
                return GetClipboardString(Vector4Handler.GetClipboardArray(property.quaternionValue.eulerAngles));
            }

            public static string GetFullString(Quaternion value)
            {
                return string.Concat("(",
                    value.x.ToString(CultureInfo.InvariantCulture), ", ",
                    value.y.ToString(CultureInfo.InvariantCulture), ", ",
                    value.z.ToString(CultureInfo.InvariantCulture), ", ",
                    value.w.ToString(CultureInfo.InvariantCulture), ")");
            }

            /************************************************************************************************************************/
            #region Clipboard
            /************************************************************************************************************************/

            public override bool ShouldEnablePaste(SerializedProperty property)
            {
                return Vector3MenuHandler.ShouldEnablePaste(property);
            }

            public override void CopyValue(SerializedProperty property)
            {
                Vector3MenuHandler.SetClipboard(property.quaternionValue.eulerAngles);
            }

            public override void PasteValue(SerializedProperty property)
            {
                property.quaternionValue = Quaternion.Euler(Vector4MenuHandler.GetClipboardVector(property.quaternionValue.eulerAngles));
            }

            public override bool TryParse(string value, out NullableVector4 result)
            {
                return Vector3MenuHandler.TryParse(value, out result);
            }

            public override string GetClipboardString(NullableVector4 value)
            {
                return Vector3MenuHandler.GetClipboardString(value);
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class RectHandler : ContextMenuHandlerWithClipboard<NullableVector4>
        {
            public override string GetTypeName(SerializedProperty property)
            {
                return "Rect";
            }

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");
                AddPropertyModifierFunction(menu, property, "Zero (0, 0, 0, 0)", () => property.rectValue = new Rect());
                AddPropertyModifierFunction(menu, property, "Make Square", (targetProperty) =>
                {
                    var rect = targetProperty.rectValue;
                    var size = Mathf.Floor((rect.width + rect.height) * 0.5f);
                    targetProperty.rectValue = new Rect(rect.x, rect.y, size, size);
                });
            }

            public override string GetValueString(SerializedProperty property)
            {
                return property.rectValue.ToString();
            }

            /************************************************************************************************************************/
            #region Clipboard
            /************************************************************************************************************************/

            public override bool ShouldEnableCopy(SerializedProperty property)
            {
                return HasAnySharedValues(property);
            }

            public override bool ShouldEnablePaste(SerializedProperty property)
            {
                return !Clipboard.AllNull();
            }

            public override void CopyValue(SerializedProperty property)
            {
                var rect = property.rectValue;
                var value = new NullableVector4(rect.x, rect.y, rect.width, rect.height);

                if (property.FindPropertyRelative("x").hasMultipleDifferentValues)
                    value.x = null;

                if (property.FindPropertyRelative("y").hasMultipleDifferentValues)
                    value.y = null;

                if (property.FindPropertyRelative("width").hasMultipleDifferentValues)
                    value.z = null;

                if (property.FindPropertyRelative("height").hasMultipleDifferentValues)
                    value.w = null;

                Clipboard = value;
            }

            public override void PasteValue(SerializedProperty property)
            {
                var rect = property.rectValue;
                var clipboard = Clipboard;
                rect.x = clipboard.x.Set(rect.x);
                rect.y = clipboard.y.Set(rect.y);
                rect.width = clipboard.z.Set(rect.width);
                rect.height = clipboard.w.Set(rect.height);
                property.rectValue = rect;
            }

            public override bool TryParse(string value, out NullableVector4 result)
            {
                result = new NullableVector4();

                var success = false;

                int start, end;
                string substring;

                // X.
                start = value.IndexOf("x:", StringComparison.CurrentCultureIgnoreCase);
                if (start >= 0)
                {
                    start += 2;
                    end = value.IndexOf(',', start + 1);
                    if (end < 0) end = value.Length;
                    substring = value.Substring(start, end - start);

                    float floatValue;
                    if (float.TryParse(substring, NumberStyles.Float, CultureInfo.InvariantCulture, out floatValue))
                    {
                        result.x = floatValue;
                        success = true;
                    }
                }

                // Y.
                start = value.IndexOf("y:", StringComparison.CurrentCultureIgnoreCase);
                if (start >= 0)
                {
                    start += 2;
                    end = value.IndexOf(',', start + 1);
                    if (end < 0) end = value.Length;
                    substring = value.Substring(start, end - start);

                    float floatValue;
                    if (float.TryParse(substring, NumberStyles.Float, CultureInfo.InvariantCulture, out floatValue))
                    {
                        result.y = floatValue;
                        success = true;
                    }
                }

                // Width.
                start = value.IndexOf("width:", StringComparison.CurrentCultureIgnoreCase);
                if (start >= 0)
                {
                    start += 6;
                    end = value.IndexOf(',', start + 1);
                    if (end < 0) end = value.Length;
                    substring = value.Substring(start, end - start);

                    float floatValue;
                    if (float.TryParse(substring, NumberStyles.Float, CultureInfo.InvariantCulture, out floatValue))
                    {
                        result.z = floatValue;
                        success = true;
                    }
                }

                // Height.
                start = value.IndexOf("height:", StringComparison.CurrentCultureIgnoreCase);
                if (start >= 0)
                {
                    start += 7;
                    end = value.IndexOf(')', start + 1);
                    if (end < 0)
                    {
                        end = value.IndexOf(',', start + 1);
                        if (end < 0) end = value.Length;
                    }
                    substring = value.Substring(start, end - start);

                    float floatValue;
                    if (float.TryParse(substring, NumberStyles.Float, CultureInfo.InvariantCulture, out floatValue))
                    {
                        result.w = floatValue;
                        success = true;
                    }
                }

                if (success)
                    return true;
                else
                    return Vector4MenuHandler.TryParse(value, out result);
            }

            public override string GetClipboardString(NullableVector4 value)
            {
                Vector4Handler.EnsureNotNull(ref value);
                return string.Concat(
                    "(x:", value.x.ToDisplayString(),
                    ", y:", value.y.ToDisplayString(),
                    ", width:", value.z.ToDisplayString(),
                    ", height:", value.w.ToDisplayString(), ")");
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class BoundsHandler : ContextMenuHandlerWithClipboard<Bounds>
        {
            public override string GetTypeName(SerializedProperty property)
            {
                return "Bounds";
            }

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
            }

            public override string GetValueString(SerializedProperty property)
            {
                return property.boundsValue.ToString();
            }

            /************************************************************************************************************************/
            #region Clipboard
            /************************************************************************************************************************/

            public override string GetClipboardString(Bounds value)
            {
                var text = new StringBuilder();

                text.Append("Center: (");
                text.Append(value.center.x.ToString(CultureInfo.InvariantCulture));
                text.Append(", ");
                text.Append(value.center.y.ToString(CultureInfo.InvariantCulture));
                text.Append(", ");
                text.Append(value.center.z.ToString(CultureInfo.InvariantCulture));
                text.Append("), Extents: (");
                text.Append(value.extents.x.ToString(CultureInfo.InvariantCulture));
                text.Append(", ");
                text.Append(value.extents.y.ToString(CultureInfo.InvariantCulture));
                text.Append(", ");
                text.Append(value.extents.z.ToString(CultureInfo.InvariantCulture));
                text.Append(")");

                return text.ToString();
            }

            public override void CopyValue(SerializedProperty property)
            {
                Clipboard = property.boundsValue;
            }

            public override void PasteValue(SerializedProperty property)
            {
                property.boundsValue = Clipboard;
            }

            public override bool TryParse(string value, out Bounds result)
            {
                result = new Bounds();

                var success = false;

                int start, end;
                string substring;

                // Center.
                start = value.IndexOf("Center: (", StringComparison.CurrentCultureIgnoreCase);
                if (start >= 0)
                {
                    start += 9;
                    end = value.IndexOf(')', start + 1);
                    if (end < 0) end = value.Length;
                    substring = value.Substring(start, end - start);

                    NullableVector4 parser;
                    if (NullableVector4.TryParse(substring, 3, out parser) >= 0)
                    {
                        result.center = parser.ToVector3();
                        success = true;
                    }
                }

                // Extents.
                start = value.IndexOf("Extents: (", StringComparison.CurrentCultureIgnoreCase);
                if (start >= 0)
                {
                    start += 10;
                    end = value.IndexOf(')', start + 1);
                    if (end < 0) end = value.Length;
                    substring = value.Substring(start, end - start);

                    NullableVector4 parser;
                    if (NullableVector4.TryParse(substring, 3, out parser) >= 0)
                    {
                        result.extents = parser.ToVector3();
                        success = true;
                    }
                }

                return success;
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class ColorHandler : ContextMenuHandlerWithClipboard<NullableVector4>
        {
            public override string GetTypeName(SerializedProperty property)
            {
                return "Color";
            }

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");

                AddPropertyModifierFunction(menu, property, "Invert", (targetProperty) =>
                {
                    var color = targetProperty.colorValue;
                    color.r = 1 - color.r;
                    color.g = 1 - color.g;
                    color.b = 1 - color.b;
                    color.a = 1 - color.a;
                    targetProperty.colorValue = color;
                });

                AddPropertyModifierFunction(menu, property, "Randomize Hue", (targetProperty) =>
                {
                    var color = targetProperty.colorValue;
                    Color.RGBToHSV(color, out color.r, out color.g, out color.b);
                    color.r = UnityEngine.Random.Range(0f, 1f);
                    targetProperty.colorValue = Color.HSVToRGB(color.r, color.g, color.b);
                });
            }

            public override string GetValueString(SerializedProperty property)
            {
                return property.colorValue.ToString();
            }

            /************************************************************************************************************************/
            #region Clipboard
            /************************************************************************************************************************/

            public override string GetClipboardString(NullableVector4 value)
            {
                return "RGBA" + value;
            }

            public override void CopyValue(SerializedProperty property)
            {
                var color = property.colorValue;
                Clipboard = new NullableVector4(color.r, color.g, color.b, color.a);
            }

            public override void PasteValue(SerializedProperty property)
            {
                var clipboard = Clipboard;
                var color = property.colorValue;

                clipboard.x.Set(ref color.r);
                clipboard.y.Set(ref color.g);
                clipboard.z.Set(ref color.b);
                clipboard.w.Set(ref color.a);

                // Try to determine if the clipboard values are bytes or floats.
                // Unfortunately this will treat byte color (1, 1, 1, 1) which is near black as the float color white.
                // We could require float values to always show the decimal point and make a custom parser method.
                // But that should be rare enough to not be a significant issue.

                if (color.r > 1 || color.g > 1 || color.b > 1 || color.a > 1)// Byte values (0-255).
                {
                    const float Rescale = 1f / byte.MaxValue;
                    color.r *= Rescale;
                    color.g *= Rescale;
                    color.b *= Rescale;
                    color.a *= Rescale;
                }

                color.r = Mathf.Clamp01(color.r);
                color.g = Mathf.Clamp01(color.g);
                color.b = Mathf.Clamp01(color.b);
                color.a = Mathf.Clamp01(color.a);

                property.colorValue = color;
            }

            public override bool TryParse(string value, out NullableVector4 result)
            {
                return Vector4MenuHandler.TryParse(value, out result);
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class EnumHandler : ContextMenuHandler
        {
            public override string GetTypeName(SerializedProperty property)
            {
                return property.type;
            }

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");
                AddPropertyModifierFunction(menu, property, "Randomize", (targetProperty) => targetProperty.enumValueIndex = UnityEngine.Random.Range(0, targetProperty.enumNames.Length));
            }

            public override string GetValueString(SerializedProperty property)
            {
                return property.enumNames[property.enumValueIndex];
            }

            /************************************************************************************************************************/
            #region Clipboard
            /************************************************************************************************************************/

            public override void AddClipboardFunctions(GenericMenu menu, SerializedProperty property, string typeName)
            {
                var accessor = SerializedPropertyAccessor.GetAccessor(property);
                if (accessor == null)
                    return;

                var fieldType = accessor.FieldType;
                if (fieldType == null)
                    return;

                menu.AddSeparator("");

                if (!property.hasMultipleDifferentValues)
                {
                    menu.AddItem(new GUIContent("Copy " + typeName), false, () => Clipboard.CacheValue(property, fieldType));
                }

                var enumNames = property.enumNames;
                var enumValueIndex = GetClipboardValueIndex(enumNames, fieldType);
                AddPropertyModifierFunction(menu, property, "Paste: " + enumNames[enumValueIndex], () =>
                {
                    property.enumValueIndex = enumValueIndex;
                });

                base.AddClipboardFunctions(menu, property, typeName);
            }

            public int GetClipboardValueIndex(string[] enumNames, Type fieldType)
            {
                var index = Array.IndexOf(enumNames, EditorGUIUtility.systemCopyBuffer);
                if (index >= 0)
                    return index;

                object value;
                if (Clipboard.TryGetCachedValue(fieldType, out value))
                    return Array.IndexOf(enumNames, value.ToString());

                return 0;
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class AnimationCurveHandler : ContextMenuHandlerWithClipboard<AnimationCurve>
        {
            /************************************************************************************************************************/

            public override string GetTypeName(SerializedProperty property)
            {
                return "AnimationCurve";
            }

            /************************************************************************************************************************/

            public override void AddClipboardFunctions(GenericMenu menu, SerializedProperty property, string typeName)
            {
                var curve = property.animationCurveValue;

                float start, end;
                curve.GetStartEndTime(out start, out end);
                menu.AddDisabledItem(new GUIContent("Time: " + start + " -> " + end));

                curve.GetStartEndValue(out start, out end);
                menu.AddDisabledItem(new GUIContent("Value: " + start + " -> " + end));

                base.AddClipboardFunctions(menu, property, typeName);
            }

            /************************************************************************************************************************/

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");

                AddCurveModifierFunction(menu, property, "Normalize", (curve) => curve.Normalize());

                AddCurveModifierFunction(menu, property, "Smooth Tangents", (curve) => curve.SmoothTangents());

                AddCurveModifierFunction(menu, property, "Flip/Horizontal", (curve) => curve.FlipHorizontal());
                AddCurveModifierFunction(menu, property, "Flip/Vertical", (curve) => curve.FlipVertical());
                AddCurveModifierFunction(menu, property, "Flip/Both Axes", (curve) => curve.FlipHorizontal().FlipVertical());
                AddCurveModifierFunction(menu, property, "Flip/Extend Mirrorred", (curve) => curve.ExtendMirrorred());
                AddCurveModifierFunction(menu, property, "Flip/Extend Mirrorred (Normalize)", (curve) => curve.ExtendMirrorred().Normalize());
                AddCurveModifierFunction(menu, property, "Flip/Enforce Horizontal Symmetry", (curve) => curve.EnforceHorizontalSymmetry());
            }

            /************************************************************************************************************************/

            private static void AddCurveModifierFunction(GenericMenu menu, SerializedProperty property,
                string label, Action<AnimationCurve> function)
            {
                AddPropertyModifierFunction(menu, property, label, (targetProperty) =>
                {
                    var curve = targetProperty.animationCurveValue;
                    function(curve);
                    targetProperty.animationCurveValue = curve;
                });
            }

            /************************************************************************************************************************/

            public override string GetValueString(SerializedProperty property)
            {
                return property.animationCurveValue.ToString();
            }

            /************************************************************************************************************************/
            #region Clipboard
            /************************************************************************************************************************/

            public override string GetClipboardString(AnimationCurve value)
            {
                return IGUtils.GetDescription(value);
            }

            public override void CopyValue(SerializedProperty property)
            {
                Clipboard = IGUtils.CopyCurve(property.animationCurveValue);
            }

            public override void PasteValue(SerializedProperty property)
            {
                property.animationCurveValue = IGUtils.CopyCurve(Clipboard);
            }

            public override bool TryParse(string value, out AnimationCurve result)
            {
                result = null;
                return false;
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class ObjectReferenceHandler : ContextMenuHandler
        {
            public static string GetPropertyTypeName(SerializedProperty property)
            {
                var name = property.type;

                if (name.Length > 6 && name.StartsWith("PPtr<"))
                {
                    var start = 5;

                    if (name[start] == '$')
                        start++;

                    return name.Substring(start, name.Length - 1 - start);
                }

                return name;
            }

            public override string GetTypeName(SerializedProperty property)
            {
                return GetPropertyTypeName(property);
            }

            private static readonly Dictionary<string, Type>
                UnderlyingTypes = new Dictionary<string, Type>();

            public static Type GetUnderlyingType(SerializedProperty property)
            {
                var accessor = SerializedPropertyAccessor.GetAccessor(property);
                if (accessor != null)
                {
                    return accessor.FieldType;
                }
                else
                {
                    Type type;
                    if (!UnderlyingTypes.TryGetValue(property.type, out type))
                    {
                        var typeName = "UnityEngine." + GetPropertyTypeName(property);
                        type = typeof(Application).Assembly.GetType(typeName);
                        UnderlyingTypes.Add(property.type, type);
                    }

                    return type;
                }
            }

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                menu.AddSeparator("");

                AddPropertyModifierFunction(menu, property, "Null", () => property.objectReferenceValue = null);

                AddPropertyModifierFunction(menu, property, "Destroy Immediate", () =>
                {
                    Undo.DestroyObjectImmediate(property.objectReferenceValue);
                    property.objectReferenceValue = null;
                });

                var value = property.objectReferenceValue;
                if (value != null && !property.hasMultipleDifferentValues)
                {
                    menu.AddItem(new GUIContent("Open Inspector"), false, () =>
                    {
                        var component = value as Component;
                        if (component != null)
                            value = component.gameObject;

                        IGEditorUtils.NewLockedInspector(value);
                    });
                }

                var fieldType = GetUnderlyingType(property);

                if (fieldType == null)
                {
                    menu.AddDisabledItem(new GUIContent("Unable to determine underlying property type"));
                }
                else
                {
                    if (fieldType.IsArray)
                        fieldType = fieldType.GetElementType();

                    AddPropertyModifierFunction(menu, property, "Find Object of Type",
                        () => property.objectReferenceValue = IGUtils.GetBestComponent(Object.FindObjectsOfType(fieldType), property.displayName));

                    AddPropertyModifierFunction(menu, property, "Find Asset of Type",
                        () => property.objectReferenceValue = IGEditorUtils.FindAssetOfType(fieldType, property.displayName));

                    if (typeof(Component).IsAssignableFrom(fieldType))
                    {
                        AddComponentFunctions(menu, property, fieldType);
                    }
                    else if (typeof(ScriptableObject).IsAssignableFrom(fieldType))
                    {
                        AddScriptableObjectFunctions(menu, property, fieldType);
                    }
                }

                AddSaveAsAssetFunction(menu, property);
            }

            /************************************************************************************************************************/

            private static void AddComponentFunctions(GenericMenu menu, SerializedProperty property, Type fieldType)
            {
                AddSetObjectReferenceFunction<Component>(menu, property, "Find Component",
                    (target) => GetComponentInHierarchy(property, target.gameObject, fieldType));

                var derivedTypes = fieldType.GetDerivedTypes(true);
                if (derivedTypes.Count == 0)
                {
                    return;
                }
                else if (derivedTypes.Count == 1 && derivedTypes[0] == fieldType)
                {
                    AddSetObjectReferenceFunction<Component>(menu, property, "Add Component", (target) => Undo.AddComponent(target.gameObject, fieldType));
                }
                else
                {
                    for (int i = 0; i < derivedTypes.Count; i++)
                    {
                        var type = derivedTypes[i];
                        var label = "Add Component ->/" + type.GetNameCS();
                        AddSetObjectReferenceFunction<Component>(menu, property, label, (target) => Undo.AddComponent(target.gameObject, type));
                    }
                }
            }

            /************************************************************************************************************************/

            private static Component GetComponentInHierarchy(SerializedProperty property, GameObject gameObject, Type componentType)
            {
                return IGUtils.GetComponentInHierarchy(gameObject, componentType, property.displayName);
            }

            /************************************************************************************************************************/

            private static void AddScriptableObjectFunctions(GenericMenu menu, SerializedProperty property, Type fieldType)
            {
                var derivedTypes = fieldType.GetDerivedTypes(true);

                for (int i = derivedTypes.Count - 1; i >= 0; i--)
                {
                    if (derivedTypes[i].IsGenericTypeDefinition)
                        derivedTypes.RemoveAt(i);
                }

                if (derivedTypes.Count == 0)
                {
                    return;
                }
                else if (derivedTypes.Count == 1 && derivedTypes[0] == fieldType)
                {
                    AddPropertyModifierFunction(menu, property, "Create new Instance", () => CreateScriptableObject(property, fieldType));
                }
                else
                {
                    for (int i = 0; i < derivedTypes.Count; i++)
                    {
                        var type = derivedTypes[i];
                        var label = "Create new Instance ->/" + type.GetNameCS();
                        AddPropertyModifierFunction(menu, property, label, () => CreateScriptableObject(property, type));
                    }
                }
            }

            private static void CreateScriptableObject(SerializedProperty property, Type type)
            {
                var obj = ScriptableObject.CreateInstance(type);
                obj.name = type.Name;

                // If the target object is an asset, we need to save the new object inside it.
                if (EditorUtility.IsPersistent(property.serializedObject.targetObject))
                {
                    AssetDatabase.AddObjectToAsset(obj, property.serializedObject.targetObject);
                }

                property.objectReferenceValue = obj;
            }

            /************************************************************************************************************************/

            public override string GetValueString(SerializedProperty property)
            {
                var obj = property.objectReferenceValue;
                return obj != null ? obj.ToString() : "null";
            }

            /************************************************************************************************************************/
            #region Clipboard
            /************************************************************************************************************************/

            public override void AddClipboardFunctions(GenericMenu menu, SerializedProperty property, string typeName)
            {
                var fieldType = GetUnderlyingType(property);

                if (fieldType == null)
                    return;

                menu.AddSeparator("");

                if (!property.hasMultipleDifferentValues)
                    menu.AddItem(new GUIContent("Copy " + typeName), false,
                        () => Clipboard.CacheValue(fieldType, property.objectReferenceValue));

                object value;
                Clipboard.TryGetCachedValue(fieldType, out value);
                var label = value != null ? "Paste: " + value : "Paste: null";
                AddPropertyModifierFunction(menu, property, label, () =>
                {
                    property.objectReferenceValue = value as Object;
                });

                base.AddClipboardFunctions(menu, property, typeName);
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        public sealed class GenericHandler : ContextMenuHandler
        {
            public override string GetTypeName(SerializedProperty property)
            {
                var accessor = SerializedPropertyAccessor.GetAccessor(property);
                if (accessor != null)
                {
                    var propertyType = accessor.FieldType;
                    if (propertyType != null)
                        return propertyType.GetNameCS();
                }

                return "Unknown Type";
            }

            public override void AddCustomItems(GenericMenu menu, SerializedProperty property)
            {
                if (property.isArray)
                {
                    var accessor = SerializedPropertyAccessor.GetAccessor(property);
                    if (accessor == null)
                        return;

                    var elementType = accessor.FieldType;
                    if (typeof(Object).IsAssignableFrom(elementType))
                    {
                        menu.AddSeparator("");

                        AddPropertyModifierFunction(menu, property, "Clear Array", (targetProperty) => targetProperty.ClearArray());

                        AddSetObjectReferenceArrayFunction<Object>(menu, property, "Find Objects of Type (in the Scene)", (target) => Object.FindObjectsOfType(elementType));
                        AddSetObjectReferenceArrayFunction<Object>(menu, property, "Find Objects of Type (including Assets)", (target) =>
                        {
                            var guids = AssetDatabase.FindAssets("t:" + elementType.Name);

                            var objects = new Object[guids.Length];
                            for (int i = 0; i < guids.Length; i++)
                            {
                                objects[i] = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]), elementType);
                            }

                            return objects;
                        });

                        if (typeof(Component).IsAssignableFrom(elementType))
                        {
                            AddSetObjectReferenceArrayFunction<Component>(menu, property, "Get Components", (target) => target.GetComponents(elementType));
                            AddSetObjectReferenceArrayFunction<Component>(menu, property, "Get Components in Children", (target) => target.GetComponentsInChildren(elementType));
                            AddSetObjectReferenceArrayFunction<Component>(menu, property, "Get Components in Parent", (target) => target.GetComponentsInParent(elementType));
                        }
                    }
                }
            }

            public override void AddLogFunction(GenericMenu menu, SerializedProperty property)
            {
                if (property.isArray)
                    AddLogValueFunction(menu, property, GetValueString, "Log Collection Values");
            }

            public override string GetValueString(SerializedProperty property)
            {
                var value = SerializedPropertyAccessor.GetValue<Array>(property);
                return IGUtils.DeepToString(value);
            }

            /************************************************************************************************************************/
            #region Clipboard
            /************************************************************************************************************************/

            //public object GetClipboardValue(SerializedProperty property)
            //{
            //    var accessor = SerializedPropertyAccessor.GetAccessor(property);
            //    if (accessor == null)
            //        return null;

            //    object value;
            //    Clipboard.TryGetCachedValue(accessor.FieldType, out value);
            //    return value;
            //}

            //public override string GetClipboardString(SerializedProperty property)
            //{
            //    var value = GetClipboardValue(property);
            //    return value != null ? value.ToString() : null;
            //}

            /************************************************************************************************************************/

            //public override void CopyValue(SerializedProperty property)
            //{
            //    var value = SerializedPropertyAccessor.GetValue(property);
            //    var type = value != null ? value.GetType() : null;
            //    Clipboard.CacheValue(type, value);
            //}

            //public override void PasteValue(SerializedProperty property)
            //{
            //    var accessor = SerializedPropertyAccessor.GetAccessor(property);
            //    if (accessor == null)
            //        return;

            //    var value = GetClipboardValue(property);

            //    IGEditorUtils.ForEachTarget(property, (prop) =>
            //    {
            //        accessor.SetValue(prop.serializedObject.targetObject, value);
            //    });
            //}

            //public override bool TryParse(string value, out object result)
            //{
            //    result = null;
            //    return false;
            //}

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Utils
        /************************************************************************************************************************/

        public static void AddPropertyModifierFunction(GenericMenu menu, SerializedProperty property, string label, Action function)
        {
            menu.AddItem(new GUIContent(label), false, () =>
            {
                function();
                property.serializedObject.ApplyModifiedProperties();
            });
        }

        public static void AddPropertyModifierFunction(GenericMenu menu, SerializedProperty property, string label, Action<SerializedProperty> function)
        {
            menu.AddItem(new GUIContent(label), false, () =>
            {
                IGEditorUtils.ForEachTarget(property, function);
            });
        }

        /************************************************************************************************************************/

        private static void AddSetObjectReferenceFunction<T>(GenericMenu menu, SerializedProperty property, string label,
            Func<T, Object> getValue) where T : Object
        {
            AddPropertyModifierFunction(menu, property, label, () =>
            {
                IGEditorUtils.ForEachTarget(property, (targetProperty) =>
                {
                    targetProperty.objectReferenceValue = getValue(targetProperty.serializedObject.targetObject as T);
                });
            });
        }

        /************************************************************************************************************************/

        private static void AddSetObjectReferenceArrayFunction<T>(GenericMenu menu, SerializedProperty property, string label, Func<T, Object[]> getValues) where T : Object
        {
            menu.AddItem(new GUIContent(label), false, () =>
            {
                IGEditorUtils.ForEachTarget(property, (targetProperty) =>
                {
                    var values = getValues(targetProperty.serializedObject.targetObject as T);

                    targetProperty.Next(true);
                    targetProperty.arraySize = values.Length;
                    targetProperty.Next(true);

                    for (int j = 0; j < values.Length; j++)
                    {
                        targetProperty.Next(false);
                        targetProperty.objectReferenceValue = values[j];
                    }
                });
            });
        }

        /************************************************************************************************************************/

        private static void AddSaveAsAssetFunction(GenericMenu menu, SerializedProperty property)
        {
            var saveThis = property.objectReferenceValue;

            if (saveThis == null ||
                property.hasMultipleDifferentValues ||
                saveThis is Component ||
                saveThis is UnityEditor.Editor ||
                saveThis is EditorWindow)
                return;

            menu.AddItem(new GUIContent("Save as Asset"), false, () =>
            {
                var path = EditorUtility.SaveFilePanelInProject("Save as Asset", saveThis.name, "asset", "Save as Asset");
                if (string.IsNullOrEmpty(path))
                    return;

                var existingAsset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (existingAsset == null || existingAsset.GetType() == saveThis.GetType())
                {
                    AssetDatabase.CreateAsset(saveThis, path);
                }
                else
                {
                    AssetDatabase.AddObjectToAsset(saveThis, path);
                }
            });
        }

        /************************************************************************************************************************/

        public static void AddLogValueFunction(GenericMenu menu, SerializedProperty property, Func<SerializedProperty, object> getValue, string label = "Log Value")
        {
            menu.AddItem(new GUIContent(label), false, () =>
            {
                var path = " -> " + property.propertyPath + " = ";

                var targets = property.serializedObject.targetObjects;

                if (!property.hasMultipleDifferentValues)
                {
                    var target = targets[0];
                    Debug.Log(target + path + getValue(property), property.serializedObject.targetObject);
                    return;
                }
                else
                {
                    IGEditorUtils.ForEachTarget(property, (targetProperty) => Debug.Log(targetProperty.serializedObject.targetObject + path + getValue(targetProperty), property.serializedObject.targetObject));
                }
            });
        }

        /************************************************************************************************************************/

        public static bool HasAnySharedValues(SerializedProperty property)
        {
            if (!property.hasMultipleDifferentValues)
                return true;

            property = property.Copy();
            var nextElement = property.Copy();
            var hasNextElement = nextElement.NextVisible(false);
            if (!hasNextElement)
            {
                nextElement = null;
            }

            property.NextVisible(true);
            while (true)
            {
                if (SerializedProperty.EqualContents(property, nextElement))
                    break;

                if (!property.hasMultipleDifferentValues)
                    return true;

                var hasNext = property.NextVisible(false);
                if (!hasNext)
                    break;
            }

            return false;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Clipboard
        /************************************************************************************************************************/

        public static class Clipboard
        {
            /************************************************************************************************************************/

            private static readonly Dictionary<Type, object>
                CachedValues = new Dictionary<Type, object>();

            /************************************************************************************************************************/

            public static object CacheValue(SerializedProperty property, Type propertyType)
            {
                var value = SerializedPropertyAccessor.GetValue(property);
                CacheValue(propertyType, value);
                EditorGUIUtility.systemCopyBuffer = value != null ? value.ToString() : "null";
                return value;
            }

            /************************************************************************************************************************/

            public static void CacheValue(Type propertyType, object value)
            {
                do
                {
                    CachedValues[propertyType] = value;

                    propertyType = propertyType.DeclaringType;
                }
                while (propertyType != null);
            }

            /************************************************************************************************************************/

            public static bool TryGetCachedValue(Type propertyType, out object value)
            {
                if (CachedValues.TryGetValue(propertyType, out value))
                    return true;

                foreach (var item in CachedValues)
                {
                    if (item.Key == propertyType)
                        continue;

                    if (propertyType.IsAssignableFrom(item.Key))
                    {
                        value = item.Value;
                        return true;
                    }
                }

                return false;
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif
