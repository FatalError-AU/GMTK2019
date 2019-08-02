// Inspector Gadgets // Copyright 2019 Kybernetik //

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor.PropertyDrawers
{
    internal sealed class RotationDrawer : TransformPropertyDrawer
    {
        /************************************************************************************************************************/

        private Vector3 _EulerAngles, _OldEulerAngles;

        /************************************************************************************************************************/

        public RotationDrawer(TransformEditor parentEditor)
            : base(parentEditor,
                  "Rotation",
                  "The local rotation of this Game Object relative to the parent.",
                  "The world rotation of this Game Object.")
        { }

        /************************************************************************************************************************/

        public override void OnEnable(SerializedObject transform)
        {
            base.OnEnable(transform);
            _MainSerializedProperty = transform.FindProperty("m_LocalRotation");
            _XSerializedProperty = _YSerializedProperty = _ZSerializedProperty = _MainSerializedProperty;
            CacheEulerAngles();
        }

        /************************************************************************************************************************/

        public override Vector3 GetLocalValue(Transform target) { return target.localEulerAngles; }
        public override Vector3 GetWorldValue(Transform target) { return target.eulerAngles; }

        public override void SetLocalValue(Transform target, Vector3 localEulerAngles)
        {
            if (ParentEditor.CurrentFreezeChildTransforms)
            {
                PositionDrawer.CacheChildPositions(target);
                CacheChildRotations(target);
            }

            target.localEulerAngles = localEulerAngles;

            if (ParentEditor.CurrentFreezeChildTransforms)
            {
                PositionDrawer.RevertChildPositions(target);
                RevertChildRotations(target);
            }
        }

        public override void SetWorldValue(Transform target, Vector3 eulerAngles)
        {
            if (ParentEditor.CurrentFreezeChildTransforms)
            {
                PositionDrawer.CacheChildPositions(target);
                CacheChildRotations(target);
            }

            target.eulerAngles = eulerAngles;

            if (ParentEditor.CurrentFreezeChildTransforms)
            {
                PositionDrawer.RevertChildPositions(target);
                RevertChildRotations(target);
            }
        }

        public override string UndoName { get { return "Rotate"; } }
        public override Vector3 DefaultValue { get { return Vector3.zero; } }

        public override Vector3 SnapValues { get { return IGEditorUtils.RotationSnapVector; } }
        public override Vector3 SnapValue(Vector3 value)
        {
            return IGEditorUtils.SnapRotation(value);
        }

        /************************************************************************************************************************/

        protected override Vector3 GetDragDirection(Transform target)
        {
            // Just rotate around Z.
            return Vector3.forward;
        }

        /************************************************************************************************************************/

        private static Quaternion[] _CachedChildRotations;

        public static void CacheChildRotations(Transform parent)
        {
            Array.Resize(ref _CachedChildRotations, parent.childCount);
            for (int i = 0; i < _CachedChildRotations.Length; i++)
            {
                _CachedChildRotations[i] = parent.GetChild(i).rotation;
            }
        }

        public static void RevertChildRotations(Transform parent)
        {
            for (int i = 0; i < _CachedChildRotations.Length; i++)
            {
                parent.GetChild(i).rotation = _CachedChildRotations[i];
            }
        }

        /************************************************************************************************************************/

        public void CacheEulerAngles()
        {
            _EulerAngles = _OldEulerAngles = GetCurrentValue(0);
        }

        /************************************************************************************************************************/

        protected override void UpdateDisplayValues()
        {
            var firstValue = GetCurrentValue(0);
            if (_OldEulerAngles != firstValue)
                _EulerAngles = firstValue;

            for (int i = 1; i < Targets.Length; i++)
            {
                var otherValue = GetCurrentValue(i);

                if (otherValue != firstValue)
                {
                    DisplayValues.SetAllNull();
                    return;
                }
            }

            DisplayValues.CopyFrom(_EulerAngles);
        }

        /************************************************************************************************************************/

        protected override void OnVectorFieldChanged(NullableVector4 values)
        {
            values.ZeroAllNulls();

            _EulerAngles.x = values.x.Value % 360;
            _EulerAngles.y = values.y.Value % 360;
            _EulerAngles.z = values.z.Value % 360;
        }

        protected override void DoVectorField(Rect area, float labelRight)
        {
            base.DoVectorField(area, labelRight);
            _OldEulerAngles = GetCurrentValue(0);
        }

        protected override void PasteValue(NullableVector4 clipboard)
        {
            base.PasteValue(clipboard);
            CacheEulerAngles();
        }

        protected override void ResetToDefaultValue()
        {
            base.ResetToDefaultValue();
            CacheEulerAngles();
        }

        /************************************************************************************************************************/

        private static readonly GUIContent PasteContentValue = new GUIContent("P");
        public override GUIContent PasteContent { get { return PasteContentValue; } }

        private static readonly GUIContent SnapContentValue = new GUIContent("S");
        public override GUIContent SnapContent { get { return SnapContentValue; } }

        protected override string GetSnapTooltip()
        {
            return "(" + IGEditorUtils.RotationSnap + ")";
        }

        /************************************************************************************************************************/

        private static readonly NullableVector4 ClipboardValue = new NullableVector4(Vector3.zero);
        public override NullableVector4 Clipboard { get { return ClipboardValue; } }

        /************************************************************************************************************************/

        public bool IsArbitrarilyRotated { get; private set; }

        public void CheckIfArbitrarilyRotated()
        {
            IsArbitrarilyRotated = false;
            for (int i = 0; i < Targets.Length; i++)
            {
                var target = Targets[i];
                if (target == null)
                    continue;

                if (target.rotation != Quaternion.identity)
                {
                    IsArbitrarilyRotated = true;
                    break;
                }
            }
        }

        /************************************************************************************************************************/

        public override void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {
            AddPropertyNameItem(menu, property);

            var axis = CurrentVectorAxis;
            if (axis < 0)// Vector Label.
            {
                AddVectorClipboardFunctions(menu);

                SerializedPropertyContextMenu.QuaternionMenuHandler.AddCustomItems(menu, property);
                AddSnapQuaternionToGridFunction(menu, property);
                AddLookAtFunction(menu);
                SerializedPropertyContextMenu.QuaternionMenuHandler.AddLogFunction(menu, property);
            }
            else// X, Y, Z.
            {
                AddFloatClipboardFunctions(menu, axis);

                menu.AddSeparator("");

                SerializedPropertyContextMenu.AddPropertyModifierFunction(menu, property, "Negate", (targetProperty) =>
                {
                    var euler = targetProperty.quaternionValue.eulerAngles;
                    euler[axis] *= -1;
                    targetProperty.quaternionValue = Quaternion.Euler(euler);
                });

                SerializedPropertyContextMenu.AddPropertyModifierFunction(menu, property, "Snap to Grid (" + IGEditorUtils.RotationSnap + ")", () => property.quaternionValue = IGEditorUtils.SnapRotation(property.quaternionValue, axis));

                SerializedPropertyContextMenu.AddPropertyModifierFunction(menu, property, "Randomize 0-360", (targetProperty) =>
                {
                    var euler = targetProperty.quaternionValue.eulerAngles;
                    euler[axis] = UnityEngine.Random.Range(0f, 360f);
                    targetProperty.quaternionValue = Quaternion.Euler(euler);
                });

                menu.AddSeparator("");

                SerializedPropertyContextMenu.AddLogValueFunction(menu, property, (targetProperty) => targetProperty.quaternionValue.eulerAngles[axis]);
            }
        }

        /************************************************************************************************************************/

        public static void AddSnapQuaternionToGridFunction(GenericMenu menu, SerializedProperty property)
        {
            SerializedPropertyContextMenu.AddPropertyModifierFunction(menu, property, "Snap to Grid (" + IGEditorUtils.RotationSnap + ")",
                () => property.quaternionValue = IGEditorUtils.SnapRotation(property.quaternionValue));
        }

        /************************************************************************************************************************/

        private void AddLookAtFunction(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Look At Next Selected Object"), false, () =>
            {
                var currentSelection = Selection.objects;

                Action onSelectionChanged = null;
                onSelectionChanged = () =>
                {
                    Selection.selectionChanged -= onSelectionChanged;

                    var lookAt = Selection.activeTransform;
                    if (lookAt != null)
                    {
                        var lookAtPoint = lookAt.position;

                        RecordTargetsForUndo("Look At");

                        for (int i = 0; i < Targets.Length; i++)
                        {
                            var target = Targets[i];
                            if (target == null)
                                continue;

                            SetWorldValue(target, Quaternion.LookRotation(lookAtPoint - target.position).eulerAngles);
                        }

                        EditorApplication.delayCall += () => Selection.objects = currentSelection;
                    }
                };

                Selection.selectionChanged += onSelectionChanged;
            });
        }

        /************************************************************************************************************************/

        protected override string GetCurrentModePropertyPrefix()
        {
            return ParentEditor.CurrentIsLocalMode ? "localEulerAngles" : "eulerAngles";
        }

        /************************************************************************************************************************/
        #region Custom Handles
        /************************************************************************************************************************/

        public override void DrawTool(Transform target, Vector3 handlePosition)
        {
            var rotation = Tools.pivotRotation == PivotRotation.Local ? target.rotation : Tools.handleRotation;
            EditorGUI.BeginChangeCheck();
            var newRotation = Handles.RotationHandle(rotation, handlePosition);
            if (EditorGUI.EndChangeCheck() && GUI.enabled)
            {
                float angle;
                Vector3 axis;
                (Quaternion.Inverse(rotation) * newRotation).ToAngleAxis(out angle, out axis);
                axis = rotation * axis;

                var isIndividualSpace = Tools.pivotRotation == PivotRotation.Local && Tools.pivotMode == PivotMode.Pivot;
                if (isIndividualSpace)
                {
                    axis = target.rotation * Quaternion.Inverse(rotation) * axis;
                }

                RecordTransformForUndo(target, UndoName);

                if (TransformEditor.DrawAllGizmos)
                {
                    ApplyRotation(target, handlePosition, axis, angle);
                }
                else
                {
                    for (int i = 0; i < Targets.Length; i++)
                    {
                        ApplyRotation(Targets[i], handlePosition, axis, angle);
                    }
                }

                Tools.handleRotation = newRotation;
            }
        }

        /************************************************************************************************************************/

        private void ApplyRotation(Transform target, Vector3 pivot, Vector3 axis, float angle)
        {
            if (ParentEditor.CurrentFreezeChildTransforms)
            {
                PositionDrawer.CacheChildPositions(target);
                CacheChildRotations(target);
            }

            target.RotateAround(pivot, axis, angle);

            if (ParentEditor.CurrentFreezeChildTransforms)
            {
                PositionDrawer.RevertChildPositions(target);
                RevertChildRotations(target);
            }

            //if (transform.parent != null)
            //{
            //    transform.SendTransformChangedScale();// Might need to call private.
            //}
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif
