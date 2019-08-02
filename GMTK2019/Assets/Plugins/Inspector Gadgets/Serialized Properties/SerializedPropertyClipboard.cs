// Inspector Gadgets // Copyright 2019 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor
{
    internal static partial class AssemblyLinker
    {
        /************************************************************************************************************************/

        private static Func<SerializedProperty, IDisposable> GetSerializedPropertyContextFunction()
        {
            return IGEditorUtils.GetSerializedPropertyContext;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Invokes InspectorGadgets.AssemblyLinker.GetSerializedPropertyContextFunction using reflection so that the
        /// caller doesn't need to reference InspectorGadgets.dll directly.
        /// <para></para>
        /// Requires Inspector Gadgets Pro v6.0+.
        /// </summary>
        private static Func<SerializedProperty, IDisposable> GetExternalSerializedPropertyContextFunction()
        {
            var assembly = Assembly.Load("InspectorGadgets");
            if (assembly == null)
                return null;

            var linker = assembly.GetType("InspectorGadgets.AssemblyLinker");
            if (linker == null)
                return null;

            var getter = linker.GetMethod("GetSerializedPropertyContextFunction",
                IGEditorUtils.StaticBindings, null, Type.EmptyTypes, null);
            if (getter == null)
                return null;

            return getter.Invoke(null, null) as Func<SerializedProperty, IDisposable>;
        }

        /************************************************************************************************************************/
    }

    public static partial class IGEditorUtils
    {
        /************************************************************************************************************************/
        #region Copy and Paste GUI
        /************************************************************************************************************************/

        /// <summary>
        /// Returns a disposable context that will allow copy and paste commands to be executed on the 'property'.
        /// </summary>
        public static IDisposable GetSerializedPropertyContext(SerializedProperty property)
        {
            return SerializedPropertyContext.Get(property);
        }

        /************************************************************************************************************************/

        private sealed class SerializedPropertyContext : DisposableStaticLazyStack<SerializedPropertyContext>
        {
            private SerializedProperty _Property;
            private EventType _EventType;
            private int _StartID;
            private bool _WasEditingTextField;

            /************************************************************************************************************************/

            public static SerializedPropertyContext Get(SerializedProperty property)
            {
                var context = Increment();

                context._Property = property;
                context._EventType = Event.current.type;
                context._StartID = GUIUtility.GetControlID(FocusType.Passive);
                context._WasEditingTextField = EditorGUIUtility.editingTextField;
                EditorGUILayout.BeginFadeGroup(1);

                return context;
            }

            /************************************************************************************************************************/

            public override void Dispose()
            {
                base.Dispose();

                EditorGUILayout.EndFadeGroup();

                var endID = GUIUtility.GetControlID(FocusType.Passive);

                var currentEvent = Event.current;
                switch (currentEvent.type)
                {
                    case EventType.ValidateCommand:
                    case EventType.ExecuteCommand:
                        if (GUIUtility.keyboardControl >= _StartID &&
                            GUIUtility.keyboardControl <= endID)
                            HandleCommand(currentEvent);
                        break;

                    case EventType.Used:
                        if (_EventType == EventType.ExecuteCommand)
                        {
                            currentEvent.type = _EventType;

                            HandleCommand(currentEvent);

                            if (currentEvent.type == EventType.Used)
                                EditorGUIUtility.editingTextField = false;
                            else
                                currentEvent.type = EventType.Used;
                        }
                        break;
                }
            }

            /************************************************************************************************************************/

            private bool IsReceivingEvent(Event e)
            {
                if (_EventType != e.type)
                    return true;

                var position = GUILayoutUtility.GetLastRect();
                position.x = 0;
                return position.Contains(e.mousePosition);
            }

            /************************************************************************************************************************/

            private static Dictionary<Type, object> _TypeToClipboardValue;

            private bool HandleCommand(Event currentEvent)
            {
                if (_WasEditingTextField)
                    return false;

                switch (currentEvent.commandName)
                {
                    case "Copy":
                        {
                            if (_TypeToClipboardValue == null)
                                _TypeToClipboardValue = new Dictionary<Type, object>();

                            var accessor = SerializedPropertyAccessor.GetAccessor(_Property);
                            if (accessor == null)
                                return false;

                            if (currentEvent.type == EventType.ExecuteCommand)
                            {
                                _TypeToClipboardValue[accessor.FieldType] = accessor.GetValue(_Property.serializedObject.targetObject);
                            }

                            currentEvent.Use();
                            MarkStackAsUsed();
                        }
                        return true;

                    case "Paste":
                        {
                            if (_TypeToClipboardValue == null)
                                return false;

                            var accessor = SerializedPropertyAccessor.GetAccessor(_Property);
                            if (accessor == null)
                                return false;

                            object value;
                            if (!_TypeToClipboardValue.TryGetValue(accessor.FieldType, out value))
                                return false;

                            if (currentEvent.type == EventType.ExecuteCommand)
                            {
                                EditorGUIUtility.editingTextField = false;
                                SerializedPropertyAccessor.SetValue(_Property, value);
                            }

                            currentEvent.Use();
                            MarkStackAsUsed();
                        }
                        return true;
                }

                return false;
            }

            /************************************************************************************************************************/

            private static void MarkStackAsUsed()
            {
                for (int i = 0; i < Stack.Count; i++)
                    Stack[i]._EventType = EventType.Used;
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif
