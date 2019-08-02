// Inspector Gadgets // Copyright 2019 Kybernetik //

using InspectorGadgets.Attributes;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Conditional] [Experimental]
    /// Contains various utilities for dynamically modifying the inspector.
    /// </summary>
    public static class DynamicInspector
    {
        /************************************************************************************************************************/
        #region Public API
        /************************************************************************************************************************/

        /// <summary>
        /// Adds a label to the bottom of the 'context' object's inspector.
        /// If an entry with the specified 'name' already exists, it will be updated to display the new 'value' instead.
        /// <para></para>
        /// This method does nothing outside of the Unity Editor.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void AddInspectorLabel(this Object context, string name, object value, float duration = float.PositiveInfinity)
        {
#if UNITY_EDITOR
            var inspector = Inspectors.GetOrAdd(context);

            inspector.AddLabel(name, value, duration);

            RepaintIfSelected(context);
#endif
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Adds a button to the bottom of the 'context' object's inspector which triggers the specified 'callback' when clicked.
        /// If an entry with the specified 'label' already exists, it will be updated to display the new 'value' instead.
        /// <para></para>
        /// This method does nothing outside of the Unity Editor.
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void AddInspectorButton(this Object context, string label, Action callback, float duration = float.PositiveInfinity)
        {
#if UNITY_EDITOR
            var inspector = Inspectors.GetOrAdd(context);

            inspector.AddButton(label, callback, duration);

            RepaintIfSelected(context);
#endif
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Removes an inspector extra with the specified 'name' such as a label or button.
        /// </summary>
        public static void RemoveInspectorExtra(this Object context, string name)
        {
#if UNITY_EDITOR
            InspectorModifiers inspector;
            if (Inspectors.TryGetValue(context, out inspector) &&
                inspector.RemoveExtra(name))
            {
                RepaintIfSelected(context);
            }
#endif
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/
        #region Drawing
        /************************************************************************************************************************/

        private static readonly Dictionary<Object, InspectorModifiers>
            Inspectors = new Dictionary<Object, InspectorModifiers>();

        /************************************************************************************************************************/

        /// <summary>
        /// Gets the modifiers applied to the 'context' object.
        /// </summary>
        public static InspectorModifiers GetModifiers(Object context)
        {
            InspectorModifiers modifiers;
            Inspectors.TryGetValue(context, out modifiers);
            return modifiers;
        }

        /************************************************************************************************************************/

        private static void RepaintIfSelected(Object context)
        {
            var component = context as Component;
            if (component != null)
                context = component.gameObject;

            if (Selection.activeObject == context)
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Draws any extra GUI elements added to the 'context' object.
        /// </summary>
        public static void DrawExtras(Object context)
        {
            InspectorModifiers inspector;
            if (Inspectors.TryGetValue(context, out inspector))
                inspector.DrawExtras();
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region InspectorModifiers
        /************************************************************************************************************************/

        /// <summary>
        /// A collection of modifications and/or additions to the inspector for a particular object.
        /// </summary>
        public sealed class InspectorModifiers
        {
            /************************************************************************************************************************/

            private readonly List<InspectorExtra>
                Extras = new List<InspectorExtra>();
            private readonly Dictionary<string, InspectorExtra>
                NameToExtra = new Dictionary<string, InspectorExtra>();

            /************************************************************************************************************************/

            /// <summary>
            /// Adds a label to the bottom of the inspector for the specified duration.
            /// </summary>
            public void AddLabel(string name, object value, float duration)
            {
                ExtraLabel label = null;

                InspectorExtra extra;
                if (!NameToExtra.TryGetValue(name, out extra) ||
                    !(extra is ExtraLabel))
                {
                    label = new ExtraLabel(name);

                    Extras.Add(label);
                    NameToExtra.Add(name, label);
                }
                else
                {
                    label = extra as ExtraLabel;
                }

                label.Value = value;
                label.SetDuration(duration);
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Adds a button to the bottom of the inspector for the specified duration.
            /// </summary>
            public void AddButton(string name, Action callback, float duration)
            {
                ExtraButton button = null;

                InspectorExtra extra;
                if (!NameToExtra.TryGetValue(name, out extra) ||
                    !(extra is ExtraButton))
                {
                    button = new ExtraButton(name);

                    Extras.Add(button);
                    NameToExtra.Add(name, button);
                }
                else
                {
                    button = extra as ExtraButton;
                }

                button.Callback = callback;
                button.SetDuration(duration);
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Removes an extra GUI element such as one added by <see cref="AddLabel"/> or <see cref="AddButton"/>.
            /// </summary>
            public bool RemoveExtra(string name)
            {
                if (NameToExtra.Remove(name))
                {
                    for (int i = 0; i < Extras.Count; i++)
                    {
                        if (Extras[i].Name.text == name)
                        {
                            Extras.RemoveAt(i);
                            break;
                        }
                    }

                    return true;
                }
                else return false;
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Draws all extra GUI elements.
            /// </summary>
            public void DrawExtras()
            {
                if (Event.current.type == EventType.Layout)
                {
                    for (int i = Extras.Count - 1; i >= 0; i--)
                    {
                        var extra = Extras[i];
                        if (extra.HasExpired)
                        {
                            Extras.RemoveAt(i);
                            NameToExtra.Remove(extra.Name.text);
                        }
                    }
                }

                for (int i = 0; i < Extras.Count; i++)
                {
                    Extras[i].Draw();
                }
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Property Modifiers
        /************************************************************************************************************************/

        //private sealed class PropertyModifier
        //{
        //    /************************************************************************************************************************/

        //    public bool isHidden;
        //    public bool isReadonly;
        //    public Color color;

        //    /************************************************************************************************************************/
        //}

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Inspector Extras
        /************************************************************************************************************************/

        private abstract class InspectorExtra
        {
            /************************************************************************************************************************/

            public readonly GUIContent Name;

            public float duration;

            public double lastUpdated;

            public bool HasExpired
            {
                get { return lastUpdated + duration < EditorApplication.timeSinceStartup; }
            }

            protected InspectorExtra(string name)
            {
                Name = new GUIContent(name);
            }

            public void SetDuration(float duration)
            {
                this.duration = duration;
                lastUpdated = EditorApplication.timeSinceStartup;
            }

            /************************************************************************************************************************/

            public abstract void Draw();

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Extra Label
        /************************************************************************************************************************/

        private sealed class ExtraLabel : InspectorExtra
        {
            /************************************************************************************************************************/

            private readonly GUIContent Text = new GUIContent();

            private object _Value;
            public object Value
            {
                get { return _Value; }
                set
                {
                    _Value = value;
                    Text.text = null;
                }
            }

            /************************************************************************************************************************/

            public ExtraLabel(string name)
                : base(name)
            {
                Name.tooltip = "Custom Inspector Entry";
            }

            /************************************************************************************************************************/

            public override void Draw()
            {
                GUILayout.BeginHorizontal();

                BaseInspectableAttribute.PrefixLabel(Name);

                if (Text.text == null)
                    Text.text = Value != null ? Value.ToString() : "null";

                EditorGUILayout.LabelField(Text);

                GUILayout.EndHorizontal();
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Extra Button
        /************************************************************************************************************************/

        private sealed class ExtraButton : InspectorExtra
        {
            /************************************************************************************************************************/

            private Action _Callback;
            public Action Callback
            {
                get { return _Callback; }
                set
                {
                    _Callback = value;
                    Name.tooltip = value.Method.GetNameCS();
                }
            }

            /************************************************************************************************************************/

            public ExtraButton(string name)
                : base(name)
            { }

            /************************************************************************************************************************/

            public override void Draw()
            {
                if (GUILayout.Button(Name, EditorStyles.miniButton))
                    Callback();
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
#endif
    }
}

