// Inspector Gadgets // Copyright 2019 Kybernetik //

#if UNITY_EDITOR

#define DISABLE_USELESS_BUTTONS

using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor.PropertyDrawers
{
    internal abstract class TransformPropertyDrawer
    {
        /************************************************************************************************************************/

        public static readonly AutoPrefs.EditorBool
            ShowCopyButton = new AutoPrefs.EditorBool(Strings.PrefsKeyPrefix + "ShowCopyButton", true),
            ShowPasteButton = new AutoPrefs.EditorBool(Strings.PrefsKeyPrefix + "ShowPasteButton", true),
            ShowSnapButton = new AutoPrefs.EditorBool(Strings.PrefsKeyPrefix + "ShowSnapButton", true),
            ShowResetButton = new AutoPrefs.EditorBool(Strings.PrefsKeyPrefix + "ShowResetButton", false),
            DisableUselessButtons = new AutoPrefs.EditorBool(Strings.PrefsKeyPrefix + "DisableUselessButtons", true),
            UseFieldColors = new AutoPrefs.EditorBool(Strings.PrefsKeyPrefix + "UseFieldColors", true),
            EmphasizeNonDefaultFields = new AutoPrefs.EditorBool(Strings.PrefsKeyPrefix + "EmphasizeNonDefaultFields", true),
            ItaliciseNonSnappedFields = new AutoPrefs.EditorBool(Strings.PrefsKeyPrefix + "ItaliciseNonSnappedFields", true);

        /************************************************************************************************************************/

        protected readonly TransformEditor
            ParentEditor;
        private readonly GUIContent
            LocalLabel,
            WorldLabel;

        /************************************************************************************************************************/
        #region Field Colors
        /************************************************************************************************************************/

        public static readonly AutoPrefs.EditorFloat
            FieldPrimaryColor = new AutoPrefs.EditorFloat(Strings.PrefsKeyPrefix + "FieldPrimaryColor",
                1, onValueChanged: (value) => GenerateFieldColors()),
            FieldSecondaryColor = new AutoPrefs.EditorFloat(Strings.PrefsKeyPrefix + "FieldSecondaryColor",
                0.65f, onValueChanged: (value) => GenerateFieldColors());

        public static Color FieldColorX { get; private set; }
        public static Color FieldColorY { get; private set; }
        public static Color FieldColorZ { get; private set; }

        private static void GenerateFieldColors()
        {
            FieldColorX = new Color(FieldPrimaryColor, FieldSecondaryColor, FieldSecondaryColor);
            FieldColorY = new Color(FieldSecondaryColor, FieldPrimaryColor, FieldSecondaryColor);
            FieldColorZ = new Color(FieldSecondaryColor, FieldSecondaryColor, FieldPrimaryColor);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        public static TransformPropertyDrawer CurrentlyDrawing { get; private set; }

        /************************************************************************************************************************/

        public Transform[] Targets
        {
            get { return ParentEditor.Targets; }
        }

        /************************************************************************************************************************/

        protected GUIContent CurrentLabel
        {
            get { return ParentEditor.CurrentIsLocalMode ? LocalLabel : WorldLabel; }
        }

        /************************************************************************************************************************/

        static TransformPropertyDrawer()
        {
            GenerateFieldColors();
            CurrentVectorAxis = -1;
        }

        /************************************************************************************************************************/

        protected TransformPropertyDrawer(TransformEditor parentEditor, string label, string localTooltip, string worldTooltip)
        {
            ParentEditor = parentEditor;
            LocalLabel = new GUIContent(label, localTooltip);
            WorldLabel = new GUIContent(label, worldTooltip);
        }

        /************************************************************************************************************************/

        public float Height
        {
            get { return EditorGUIUtility.singleLineHeight; }
        }

        /************************************************************************************************************************/

        public void DoInspectorGUI(Rect area)
        {
            CurrentlyDrawing = this;

            var startID = GUIUtility.GetControlID(FocusType.Passive);

            UpdateDisplayValues();

            DoMiniButtonsGUI(ref area);

            EditorGUI.BeginProperty(area, CurrentLabel, _MainSerializedProperty);
            DoVectorField(area, area.x + InternalGUI.NameLabelWidth);
            EditorGUI.EndProperty();

            CheckInspectorClipboardHotkeys(startID);

            CurrentlyDrawing = null;
        }

        /************************************************************************************************************************/
        #region Abstractions
        /************************************************************************************************************************/

        protected SerializedProperty
            _MainSerializedProperty,
            _XSerializedProperty,
            _YSerializedProperty,
            _ZSerializedProperty;

        /************************************************************************************************************************/

        public virtual void OnEnable(SerializedObject transform)
        {
            UpdatePasteTooltip();
            SnapContent.tooltip = "Left Click = Snap " + GetSnapTooltip() + "\nRight Click = Open Snap Settings";
        }

        /************************************************************************************************************************/

        public virtual void OnDisable() { }

        /************************************************************************************************************************/

        public abstract Vector3 GetLocalValue(Transform target);
        public abstract Vector3 GetWorldValue(Transform target);

        public Vector3 GetCurrentValue(Transform target)
        {
            if (ParentEditor.CurrentIsLocalMode)
                return GetLocalValue(target);
            else
                return GetWorldValue(target);
        }

        public Vector3 GetCurrentValue(int targetIndex)
        {
            return GetCurrentValue(Targets[targetIndex]);
        }

        /************************************************************************************************************************/

        public abstract void SetLocalValue(Transform target, Vector3 value);
        public abstract void SetWorldValue(Transform target, Vector3 value);

        public void SetCurrentValue(Transform target, Vector3 value)
        {
            if (ParentEditor.CurrentIsLocalMode)
                SetLocalValue(target, value);
            else
                SetWorldValue(target, value);
        }

        public void SetCurrentValue(int targetIndex, Vector3 value)
        {
            SetCurrentValue(Targets[targetIndex], value);
        }

        public void SetCurrentValue(Transform target, NullableVector4 values)
        {
            var value = GetCurrentValue(target);
            value = values.ToVector3(value);
            SetCurrentValue(target, value);
        }

        /************************************************************************************************************************/

        public abstract GUIContent PasteContent { get; }
        public abstract GUIContent SnapContent { get; }

        public abstract string UndoName { get; }

        public abstract Vector3 DefaultValue { get; }
        public abstract Vector3 SnapValues { get; }
        public abstract Vector3 SnapValue(Vector3 value);

        /************************************************************************************************************************/

        public abstract NullableVector4 Clipboard { get; }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Serialized Property Context Menu
        /************************************************************************************************************************/

        public abstract void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property);

        protected abstract string GetCurrentModePropertyPrefix();

        /************************************************************************************************************************/

        protected void AddPropertyNameItem(GenericMenu menu, SerializedProperty property)
        {
            string name;

            switch (property.propertyType)
            {
                case SerializedPropertyType.Float:
                    name = "float      Transform.";
                    break;
                case SerializedPropertyType.Vector3:
                    name = "Vector3      Transform.";
                    break;
                case SerializedPropertyType.Quaternion:
                    name = "Quaternion      Transform.";
                    break;
                default:
                    return;
            }

            name += GetCurrentModePropertyPrefix();

            if (CurrentVectorAxis >= 0)
            {
                name += ".";
                switch (CurrentVectorAxis)
                {
                    case 0: name += "x"; break;
                    case 1: name += "y"; break;
                    case 2: name += "z"; break;
                    default: throw new Exception("Unexpected Case");
                }
            }

            menu.AddDisabledItem(new GUIContent(name));
        }

        /************************************************************************************************************************/

        protected void AddVectorClipboardFunctions(GenericMenu menu)
        {
            menu.AddSeparator("");

            UpdateDisplayValues();

            if (!DisplayValues.AnyNull(3))
            {
                menu.AddItem(new GUIContent(
                    "Copy " + LocalLabel.text + " (Vector3)"), false,
                    () => { UpdateDisplayValues(); CopyCurrentValueToClipboard(); });
            }

            menu.AddItem(new GUIContent(
                "Paste (public): " + SerializedPropertyContextMenu.Vector3MenuHandler.GetClipboardString(_MainSerializedProperty)), false,
                () => PasteValue(SerializedPropertyContextMenu.Vector3MenuHandler.Clipboard));

            menu.AddItem(new GUIContent("Paste (private): " + Clipboard.ToString(3)), false, () => PasteValue(Clipboard));

#if UNITY_2017_3_OR_NEWER
            PersistentValues.AddMenuItem(menu, _MainSerializedProperty);
#endif
        }

        /************************************************************************************************************************/

        protected void AddFloatClipboardFunctions(GenericMenu menu, int axis)
        {
            menu.AddSeparator("");

            UpdateDisplayValues();

            switch (axis)
            {
                case 0: AddCopyFloatFunction(menu, DisplayValues.x); break;
                case 1: AddCopyFloatFunction(menu, DisplayValues.y); break;
                case 2: AddCopyFloatFunction(menu, DisplayValues.z); break;
                default: throw new Exception("Unexpected Case");
            }

            menu.AddItem(new GUIContent(
                "Paste: " + SerializedPropertyContextMenu.FloatMenuHandler.GetClipboardString(_MainSerializedProperty)), false, () =>
            {
                var undoName = "Paste " + LocalLabel.text + ".";

                switch (axis)
                {
                    case 0: undoName += 'x'; break;
                    case 1: undoName += 'y'; break;
                    case 2: undoName += 'z'; break;
                    default: throw new Exception("Unexpected Case");
                }

                RecordTargetsForUndo(undoName);

                var value = SerializedPropertyContextMenu.FloatMenuHandler.Clipboard.x;
                if (value != null)
                {
                    for (int i = 0; i < Targets.Length; i++)
                    {
                        var target = Targets[i];
                        if (target == null)
                            continue;

                        var vector = GetCurrentValue(target);
                        vector[axis] = value.Value;
                        SetCurrentValue(target, vector);
                    }
                }
            });
        }

        /************************************************************************************************************************/

        protected void AddCopyFloatFunction(GenericMenu menu, float? value)
        {
            if (value == null)
                return;

            menu.AddItem(new GUIContent("Copy float"), false, () =>
            {
                SerializedPropertyContextMenu.FloatMenuHandler.SetClipboard(value.Value);
            });
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Vector Fields
        /************************************************************************************************************************/

        protected virtual void DoVectorField(Rect area, float labelRight)
        {
            EditorGUI.BeginChangeCheck();

            DoVectorLabel(ref area, labelRight);

            MultiVector3Field(area, DisplayValues);

            if (EditorGUI.EndChangeCheck())
            {
                OnVectorFieldChanged(DisplayValues);

                RecordTargetsForUndo(UndoName);

                for (int i = 0; i < Targets.Length; i++)
                {
                    SetCurrentValue(Targets[i], DisplayValues);
                }
            }
        }

        protected virtual void OnVectorFieldChanged(NullableVector4 values) { }

        /************************************************************************************************************************/

        private static readonly int
            DragControlHint = "LabelDragControl".GetHashCode();

        private void DoVectorLabel(ref Rect area, float right)
        {
            var labelArea = IGEditorUtils.StealFromLeft(ref area, right - area.x);

            InternalGUI.FieldLabelStyle.fontStyle = _MainSerializedProperty.prefabOverride ? FontStyle.Bold : FontStyle.Normal;

            GUI.Label(labelArea, CurrentLabel, InternalGUI.FieldLabelStyle);

            // Allow the vector label to be dragged.
            var controlID = GUIUtility.GetControlID(DragControlHint, FocusType.Passive, labelArea);
            HandleDragVectorLabel(labelArea, controlID);

            if (UseMiddleClickInRect(labelArea))
                DisplayValues.CopyFrom(DefaultValue);
        }

        /************************************************************************************************************************/

        private static Vector3[] _DragStartValues;
        private static Vector3[] _DragDirections;
        private static float[] _DragDistances;

        private void HandleDragVectorLabel(Rect area, int id)
        {
            var current = Event.current;
            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:// Begin.
                    if (current.button == 0 && area.Contains(current.mousePosition))
                    {
                        EditorGUIUtility.editingTextField = false;
                        GUIUtility.hotControl = id;
                        GUIUtility.keyboardControl = id;
                        Undo.IncrementCurrentGroup();
                        current.Use();

                        var targetCount = Targets.Length;
                        if (_DragStartValues == null || _DragStartValues.Length != targetCount)
                        {
                            _DragStartValues = new Vector3[targetCount];
                            _DragDirections = new Vector3[targetCount];
                            _DragDistances = new float[targetCount];
                        }

                        for (int i = 0; i < targetCount; i++)
                        {
                            var target = Targets[i];
                            if (target == null)
                                continue;

                            var value = GetCurrentValue(target);
                            _DragStartValues[i] = value;

                            _DragDirections[i] = GetDragDirection(target);

                            _DragDistances[i] = 0;
                        }

                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;

                case EventType.MouseUp:// End.
                    if (GUIUtility.hotControl == id)// && EditorGUI.s_DragCandidateState != 0)
                    {
                        GUIUtility.hotControl = 0;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;

                case EventType.MouseDrag:// Move.
                    if (GUIUtility.hotControl == id)
                    {
                        RecordTargetsForUndo(UndoName);

                        // This value seems to be what Unity uses for regular dragging on a float field.
                        var delta = HandleUtility.niceMouseDelta * 0.03f;

                        var targetCount = Targets.Length;
                        for (int i = 0; i < targetCount; i++)
                        {
                            var distance = _DragDistances[i] + delta;
                            _DragDistances[i] = distance;

                            var value = _DragStartValues[i] + _DragDirections[i] * distance;

                            if (current.control)
                                value = SnapValue(value);

                            SetCurrentValue(i, value);
                        }

                        UpdateDisplayValues();

                        GUI.changed = true;
                        current.Use();
                    }
                    break;

                case EventType.KeyDown:// Cancel.
                    if (GUIUtility.hotControl == id && current.keyCode == KeyCode.Escape)
                    {
                        RecordTargetsForUndo(UndoName);

                        for (int i = 0; i < Targets.Length; i++)
                        {
                            SetCurrentValue(i, _DragStartValues[i]);
                        }

                        UpdateDisplayValues();

                        GUI.changed = true;
                        GUIUtility.hotControl = 0;
                        current.Use();
                    }
                    break;

                case EventType.Repaint:// Repaint.
                    EditorGUIUtility.AddCursorRect(area, MouseCursor.SlideArrow);
                    break;

                default:
                    break;
            }
        }

        protected abstract Vector3 GetDragDirection(Transform target);

        /************************************************************************************************************************/

        protected static bool UseMiddleClickInRect(Rect area)
        {
            var currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseUp &&
                currentEvent.button == 2 &&
                area.Contains(currentEvent.mousePosition))
            {
                GUI.changed = true;
                currentEvent.Use();
                GUIUtility.keyboardControl = 0;
                return true;
            }
            else return false;
        }

        /************************************************************************************************************************/

        public static int CurrentVectorAxis { get; private set; }

        protected void MultiVector3Field(Rect area, NullableVector4 values)
        {
            var xMin = area.xMin;
            var xMax = area.xMax;

            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 12;

            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var snapValue = SnapValues;
            var defaultValue = DefaultValue;

            CurrentVectorAxis = 0;
            area.xMax = Mathf.Lerp(xMin, xMax, 1 / 3f);
            values.x = MultiFloatField(area, _XSerializedProperty, FieldColorX, Strings.GUI.X, values.x, snapValue.x, defaultValue.x);

            CurrentVectorAxis = 1;
            area.xMin = area.xMax;
            area.xMax = Mathf.Lerp(xMin, xMax, 2 / 3f);
            values.y = MultiFloatField(area, _YSerializedProperty, FieldColorY, Strings.GUI.Y, values.y, snapValue.y, defaultValue.y);

            CurrentVectorAxis = 2;
            area.xMin = area.xMax;
            area.xMax = xMax;
            values.z = MultiFloatField(area, _ZSerializedProperty, FieldColorZ, Strings.GUI.Z, values.z, snapValue.z, defaultValue.z);

            CurrentVectorAxis = -1;

            EditorGUI.indentLevel = indentLevel;
            EditorGUIUtility.labelWidth = labelWidth;
        }

        /************************************************************************************************************************/

        protected static readonly NullableVector4 DisplayValues = new NullableVector4();

        protected virtual void UpdateDisplayValues()
        {
            var firstValue = GetCurrentValue(0);
            DisplayValues.CopyFrom(firstValue);

            for (int i = 1; i < Targets.Length; i++)
            {
                var otherValue = GetCurrentValue(i);
                if (otherValue.x != firstValue.x) DisplayValues.x = null;
                if (otherValue.y != firstValue.y) DisplayValues.y = null;
                if (otherValue.z != firstValue.z) DisplayValues.z = null;
            }
        }

        /************************************************************************************************************************/

        private static readonly int BoxHash = "Box".GetHashCode();

        protected float? MultiFloatField(Rect area, SerializedProperty property, Color color, GUIContent label,
            float? value, float snapValue, float defaultValue)
        {
            var originalColor = GUI.color;

            //var area = EditorGUILayout.GetControlRect(InternalGUI.VectorFieldOptions);
            //area.width += 2;

            EditorGUI.BeginChangeCheck();
            label = EditorGUI.BeginProperty(area, label, property);

            // Emphasize non-default fields.
            if (EmphasizeNonDefaultFields && (value == null || value.Value != defaultValue))
            {
                const float Border = 1;
                var box = new Rect(area);
                box.xMin += EditorGUIUtility.labelWidth - Border;
                box.y -= Border;
                box.width += Border;
                box.height += Border * 2;

                GUI.Box(box, GUIContent.none);
            }
            else
            {
                // If we didn't draw the box, get a control ID to make sure that the field's ID is consistent between showing and not showing the box.
                GUIUtility.GetControlID(BoxHash, FocusType.Passive);
            }

            // Field Colors.
            if (UseFieldColors)
                GUI.color *= color;

            float fieldValue;
            if (value != null)
            {
                fieldValue = value.Value;
                EditorGUI.showMixedValue = false;
            }
            else
            {
                fieldValue = 0;
                EditorGUI.showMixedValue = true;
            }

            // Italic for properties which aren't multiples of their snap threshold.
            if (ItaliciseNonSnappedFields &&
                !EditorGUI.showMixedValue &&
                !IGEditorUtils.IsSnapped(fieldValue, snapValue))
            {
                InternalGUI.FloatFieldStyle.fontStyle = FontStyle.Italic;
            }

            // Draw the number field.
            fieldValue = EditorGUI.FloatField(area, label, fieldValue, InternalGUI.FloatFieldStyle);

            // Revert any style changes.
            InternalGUI.FloatFieldStyle.fontStyle = FontStyle.Normal;
            EditorGUI.showMixedValue = false;
            GUI.color = originalColor;

            EditorGUI.EndProperty();
            if (EditorGUI.EndChangeCheck())
                return fieldValue;

            // Middle click a field to revert to the default value.
            if (UseMiddleClickInRect(area))
                value = defaultValue;

            return value;
        }

        /************************************************************************************************************************/

        private void CheckInspectorClipboardHotkeys(int startID)
        {
            var endID = GUIUtility.GetControlID(FocusType.Passive);

            if (GUIUtility.keyboardControl > startID && GUIUtility.keyboardControl < endID)
            {
                var currentEvent = Event.current;
                if (currentEvent.type == EventType.KeyUp &&
                    currentEvent.control &&
                    !EditorGUIUtility.editingTextField)
                {
                    switch (currentEvent.keyCode)
                    {
                        case KeyCode.C:
                            CopyCurrentValueToClipboard();
                            Event.current.Use();
                            break;

                        case KeyCode.V:
                            PasteValueFromClipboard();
                            Event.current.Use();
                            break;
                    }
                }
            }
        }

        /************************************************************************************************************************/

        private static Transform[] _UndoTargets;

        protected void RecordTargetsForUndo(string name)
        {
            if (ParentEditor.CurrentFreezeChildTransforms)
            {
                var count = Targets.Length;
                for (int i = 0; i < Targets.Length; i++)
                {
                    count += Targets[i].childCount;
                }

                Array.Resize(ref _UndoTargets, count);

                count = 0;
                for (int i = 0; i < Targets.Length; i++)
                {
                    var target = Targets[i];

                    _UndoTargets[count++] = target;

                    for (int j = 0; j < target.childCount; j++)
                    {
                        _UndoTargets[count++] = target.GetChild(j);
                    }
                }

                Undo.RecordObjects(_UndoTargets, name);
            }
            else
            {
                Undo.RecordObjects(Targets, name);
            }
        }

        protected void RecordTransformForUndo(Transform target, string name)
        {
            if (ParentEditor.CurrentFreezeChildTransforms)
            {
                if (TransformEditor.DrawAllGizmos)
                {
                    Array.Resize(ref _UndoTargets, target.childCount + 1);

                    var i = 0;
                    for (; i < target.childCount; i++)
                    {
                        _UndoTargets[i] = target.GetChild(i);
                    }
                    _UndoTargets[i] = target;
                }
                else
                {
                    var count = ParentEditor.Targets.Length;
                    for (int i = 0; i < ParentEditor.Targets.Length; i++)
                    {
                        count += ParentEditor.Targets[i].childCount;
                    }

                    Array.Resize(ref _UndoTargets, count);

                    var index = 0;
                    for (int i = 0; i < ParentEditor.Targets.Length; i++)
                    {
                        target = ParentEditor.Targets[i];
                        for (int j = 0; j < target.childCount; j++)
                        {
                            _UndoTargets[index++] = target.GetChild(j);
                        }
                        _UndoTargets[index++] = target;
                    }
                }

                Undo.RecordObjects(_UndoTargets, name);
            }
            else if (TransformEditor.DrawAllGizmos)
            {
                Undo.RecordObject(target, name);
            }
            else
            {
                Undo.RecordObjects(ParentEditor.Targets, name);
            }
        }

        /************************************************************************************************************************/

        public abstract void DrawTool(Transform target, Vector3 handlePosition);

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Mini Buttons
        /************************************************************************************************************************/

        private void DoMiniButtonsGUI(ref Rect area)
        {
            var width = area.width;

            DoResetButton(ref area);
            DoSnapButton(ref area);
            DoPasteButton(ref area);
            DoCopyButton(ref area);

            if (area.width != width)
                area.width -= IGEditorUtils.Spacing;
        }

        /************************************************************************************************************************/
        #region Copy
        /************************************************************************************************************************/

        private void DoCopyButton(ref Rect area)
        {
            if (!ShowCopyButton)
                return;

            var enabled = GUI.enabled;
            DisableGuiIfCopyIsUseless();

            var buttonArea = IGEditorUtils.StealFromRight(ref area, InternalGUI.SmallButtonStyle.fixedWidth);

            if (GUI.Button(buttonArea, Strings.GUI.Copy, InternalGUI.SmallButtonStyle))
            {
                if (Event.current.button == 1)
                {
                    LogCurrentValue();
                }
                else
                {
                    CopyCurrentValueToClipboard();
                }
            }

            GUI.enabled = enabled;
        }

        /************************************************************************************************************************/

        private void DisableGuiIfCopyIsUseless()
        {
            if (!GUI.enabled)
                return;

            if (!DisableUselessButtons)
                return;

            if (Event.current.type != EventType.Repaint)
                return;

            if (DisplayValues != Clipboard ||
                !DisplayValues.Equals(SerializedPropertyContextMenu.Vector3MenuHandler.Clipboard, 3) ||
                DisplayValues.AllNull(3))
                return;

            GUI.enabled = false;
        }

        /************************************************************************************************************************/

        private void LogCurrentValue()
        {
            var message = new StringBuilder();
            var target = Targets[0];
            if (Targets.Length == 1)
            {
                message.Append(target.name)
                    .Append('.')
                    .Append(ParentEditor.CurrentIsLocalMode ? "Local" : "World")
                    .Append(LocalLabel.text)
                    .Append(" = ")
                    .Append(GetCurrentValue(target));
            }
            else
            {
                message.Append("Selection.")
                    .Append(ParentEditor.CurrentIsLocalMode ? "Local" : "World")
                    .Append(LocalLabel.text)
                    .Append("s = ")
                    .Append(Targets.Length)
                    .AppendLine(" values:");

                for (int i = 0; i < Targets.Length; i++)
                    message.Append('[').Append(i).Append("] ").Append(GetCurrentValue(i)).AppendLine();
            }
            Debug.Log(message, target);
        }

        /************************************************************************************************************************/

        private void CopyCurrentValueToClipboard()
        {
            Clipboard.CopyFrom(DisplayValues);
            UpdatePasteTooltip();
            SerializedPropertyContextMenu.Vector3MenuHandler.Clipboard = new NullableVector4(DisplayValues);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Paste
        /************************************************************************************************************************/

        private string _PreviousCopyBuffer;

        private void DoPasteButton(ref Rect area)
        {
            if (!ShowPasteButton)
                return;

            var enabled = GUI.enabled;
            if (enabled)
                DisableGuiIfPasteIsUseless();

            if (_PreviousCopyBuffer != EditorGUIUtility.systemCopyBuffer)
            {
                _PreviousCopyBuffer = EditorGUIUtility.systemCopyBuffer;
                UpdatePasteTooltip();
            }

            var buttonArea = IGEditorUtils.StealFromRight(ref area, InternalGUI.SmallButtonStyle.fixedWidth);

            if (GUI.Button(buttonArea, PasteContent, InternalGUI.SmallButtonStyle))
            {
                GUIUtility.keyboardControl = 0;

                if (Event.current.button == 1)
                {
                    PasteValue(Clipboard);
                }
                else
                {
                    PasteValueFromClipboard();
                }
            }

            GUI.enabled = enabled;
        }

        /************************************************************************************************************************/

        private void DisableGuiIfPasteIsUseless()
        {
            if (!DisableUselessButtons)
                return;

            if (Event.current.type != EventType.Repaint)
                return;

            if (DisplayValues != Clipboard ||
                !DisplayValues.Equals(SerializedPropertyContextMenu.Vector3MenuHandler.Clipboard, 3) ||
                Clipboard.AllNull(3))
                return;

            GUI.enabled = false;
        }

        /************************************************************************************************************************/

        protected virtual void PasteValue(NullableVector4 clipboard)
        {
            RecordTargetsForUndo("Paste " + LocalLabel.text);

            for (int i = 0; i < Targets.Length; i++)
            {
                var target = Targets[i];
                if (target == null)
                    continue;

                SetCurrentValue(target, clipboard);
            }
        }

        private void PasteValueFromClipboard()
        {
            PasteValue(SerializedPropertyContextMenu.Vector3MenuHandler.Clipboard);
        }

        /************************************************************************************************************************/

        private void UpdatePasteTooltip()
        {
            PasteContent.tooltip =
                "Left Click = Paste (public): " + SerializedPropertyContextMenu.Vector3MenuHandler.Clipboard.ToString(3) +
                "\nRight Click = Paste (private): " + Clipboard.ToString(3);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Snap
        /************************************************************************************************************************/

        private void DoSnapButton(ref Rect area)
        {
            if (!ShowSnapButton)
                return;

            var enabled = GUI.enabled;
            if (enabled) DisableGuiIfSnapIsUseless();

            var buttonArea = IGEditorUtils.StealFromRight(ref area, InternalGUI.SmallButtonStyle.fixedWidth);

            if (GUI.Button(buttonArea, SnapContent, InternalGUI.SmallButtonStyle))
            {
                GUIUtility.keyboardControl = 0;

                if (Event.current.button == 1)
                {
                    EditorApplication.ExecuteMenuItem("Edit/Snap Settings...");
                }
                else
                {
                    RecordTargetsForUndo("Snap " + LocalLabel.text);

                    for (int i = 0; i < Targets.Length; i++)
                    {
                        var target = Targets[i];
                        SetCurrentValue(target, SnapValue(GetCurrentValue(target)));
                    }
                }
            }

            GUI.enabled = enabled;
        }

        /************************************************************************************************************************/

        private void DisableGuiIfSnapIsUseless()
        {
            if (!DisableUselessButtons)
                return;

            if (Event.current.type != EventType.Repaint)
                return;

            var snap = SnapValues;
            for (int i = 0; i < Targets.Length; i++)
            {
                var value = GetCurrentValue(i);
                if (!IGEditorUtils.IsSnapped(value.x, snap.x) ||
                    !IGEditorUtils.IsSnapped(value.y, snap.y) ||
                    !IGEditorUtils.IsSnapped(value.z, snap.z))
                {
                    return;
                }
            }

            GUI.enabled = false;
        }

        /************************************************************************************************************************/

        protected abstract string GetSnapTooltip();

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Reset
        /************************************************************************************************************************/

        private void DoResetButton(ref Rect area)
        {
            if (!ShowResetButton)
                return;

            var enabled = GUI.enabled;
            if (enabled) DisableGuiIfResetIsUseless();

            var buttonArea = IGEditorUtils.StealFromRight(ref area, InternalGUI.SmallButtonStyle.fixedWidth);

            if (GUI.Button(buttonArea, Strings.GUI.Reset, InternalGUI.SmallButtonStyle))
            {
                GUIUtility.keyboardControl = 0;

                ResetToDefaultValue();
            }

            GUI.enabled = enabled;
        }

        /************************************************************************************************************************/

        private void DisableGuiIfResetIsUseless()
        {
            if (!DisableUselessButtons)
                return;

            if (Event.current.type != EventType.Repaint)
                return;

            for (int i = 0; i < Targets.Length; i++)
            {
                if (GetCurrentValue(i) != DefaultValue)
                {
                    return;
                }
            }

            GUI.enabled = false;
        }

        /************************************************************************************************************************/

        protected virtual void ResetToDefaultValue()
        {
            RecordTargetsForUndo("Reset " + LocalLabel.text);

            for (int i = 0; i < Targets.Length; i++)
            {
                SetCurrentValue(i, DefaultValue);
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif
