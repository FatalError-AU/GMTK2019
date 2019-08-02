// Inspector Gadgets // Copyright 2019 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only]
    /// A serializable reference to a <see cref="SerializedProperty"/>.
    /// </summary>
    [Serializable]
    public sealed class SerializedPropertyReference
    {
        /************************************************************************************************************************/

        [SerializeField] private SerializedObjectReference[] _TargetObjects;
        [SerializeField] private SerializedObjectReference _Context;
        [SerializeField] private string _PropertyPath;

        [NonSerialized] private bool _IsInitialised;
        [NonSerialized] private SerializedProperty _TargetProperty;

        /************************************************************************************************************************/

        /// <summary>The <see cref="SerializedObject.targetObject"/>.</summary>
        public SerializedObjectReference TargetObject { get { return _TargetObjects[0]; } }

        /// <summary>The <see cref="SerializedObject.targetObjects"/>.</summary>
        public SerializedObjectReference[] TargetObjects { get { return _TargetObjects; } }

        /// <summary>The <see cref="SerializedProperty.propertyPath"/>.</summary>
        public string PropertyPath { get { return _PropertyPath; } }

        /// <summary>The referenced <see cref="SerializedProperty"/>.</summary>
        public SerializedProperty Property
        {
            get
            {
                Initialise();
                return _TargetProperty;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Constructs a new <see cref="SerializedPropertyReference"/> which wraps the specified 'property'.
        /// </summary>
        public SerializedPropertyReference(SerializedProperty property)
        {
            _TargetObjects = SerializedObjectReference.Convert(property.serializedObject.targetObjects);

            _Context = property.serializedObject.context;
            _PropertyPath = property.propertyPath;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Constructs a new <see cref="SerializedPropertyReference"/> which wraps the specified 'property'.
        /// </summary>
        public static implicit operator SerializedPropertyReference(SerializedProperty property)
        {
            return new SerializedPropertyReference(property);
        }

        /// <summary>
        /// Returns the target <see cref="Property"/>.
        /// </summary>
        public static implicit operator SerializedProperty(SerializedPropertyReference reference)
        {
            return reference.Property;
        }

        /************************************************************************************************************************/

        private void Initialise()
        {
            if (_IsInitialised)
                return;

            _IsInitialised = true;

            if (!AllTargetsExist)
                return;

            if (string.IsNullOrEmpty(_PropertyPath))
                return;

            var targetObjects = SerializedObjectReference.Convert(_TargetObjects);
            var serializedObject = new SerializedObject(targetObjects, _Context);
            _TargetProperty = serializedObject.FindProperty(_PropertyPath);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if the specified property and objects match the targets of this reference.
        /// </summary>
        public bool IsTarget(SerializedProperty property, Object[] targetObjects)
        {
            if (_TargetProperty == null ||
                _TargetProperty.propertyPath != property.propertyPath ||
                _TargetObjects.Length != targetObjects.Length)
                return false;

            for (int i = 0; i < _TargetObjects.Length; i++)
            {
                if (_TargetObjects[i] != targetObjects[i])
                    return false;
            }

            return true;
        }

        /************************************************************************************************************************/

        private bool AllTargetsExist
        {
            get
            {
                for (int i = 0; i < _TargetObjects.Length; i++)
                {
                    if (_TargetObjects[i] == null)
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Returns true if the target property and all its target objects still exist.
        /// </summary>
        public bool TargetExists
        {
            get
            {
                Initialise();

                return
                    _TargetProperty != null &&
                    AllTargetsExist;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Gets the height needed to draw the target property.
        /// </summary>
        public float GetPropertyHeight()
        {
            if (_TargetProperty == null)
                return 0;

            return EditorGUI.GetPropertyHeight(_TargetProperty, _TargetProperty.isExpanded);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws the target object within the specified 'area'.
        /// </summary>
        public void DoTargetGUI(Rect area)
        {
            area.height = EditorGUIUtility.singleLineHeight;

            Initialise();

            if (_TargetProperty == null)
            {
                GUI.Label(area, "Missing " + this);
                return;
            }

            var targets = _TargetProperty.serializedObject.targetObjects;

            var enabled = GUI.enabled;
            GUI.enabled = false;

            var showMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = targets.Length > 1;

            EditorGUI.ObjectField(area, targets[0], typeof(Object), true);

            EditorGUI.showMixedValue = showMixedValue;
            GUI.enabled = enabled;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws the target property within the specified 'area'.
        /// </summary>
        public void DoPropertyGUI(Rect area)
        {
            Initialise();

            if (_TargetProperty == null)
                return;

            _TargetProperty.serializedObject.Update();

            GUI.BeginGroup(area);
            area.x = area.y = 0;

            EditorGUI.PropertyField(area, _TargetProperty, _TargetProperty.isExpanded);

            GUI.EndGroup();

            _TargetProperty.serializedObject.ApplyModifiedProperties();
        }

        /************************************************************************************************************************/

        /// <summary>Returns a string describing this object.</summary>
        public override string ToString()
        {
            return string.Concat(
                "SerializedPropertyReference ",
                _PropertyPath,
                ": ",
                IGUtils.DeepToString(_TargetObjects));
        }

        /************************************************************************************************************************/
    }
}

#endif
