// Inspector Gadgets // Copyright 2019 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor
{
    internal sealed class WatcherWindow : EditorWindow
    {
        /************************************************************************************************************************/

        [SerializeField] private Vector2 _ScrollPosition;
        [SerializeField] private List<PropertyWatcher> _PropertyWatchers;
        [SerializeField] private List<ObjectWatcher> _ObjectWatchers;

        [NonSerialized] private ReorderableList _PropertyWatcherDisplay;

        /************************************************************************************************************************/

        private void OnEnable()
        {
            var title = EditorGUIUtility.IconContent("UnityEditor.LookDevView");
            title.text = "Watcher";
            titleContent = title;
            minSize = new Vector2(275, 50);
            autoRepaintOnSceneChange = true;

            if (_ObjectWatchers != null && _ObjectWatchers.Count > 0)
                InitialiseObjectWatchers();

            if (_PropertyWatchers != null && _PropertyWatchers.Count > 0)
                InitialisePropertyWatchers();
        }

        /************************************************************************************************************************/

        private void OnGUI()
        {
            EditorGUIUtility.wideMode = true;

            DoHeaderGUI();

            EditorGUIUtility.labelWidth = Mathf.Max(position.width * 0.45f - 40, 120);

            _ScrollPosition = GUILayout.BeginScrollView(_ScrollPosition);
            DoPropertyWatcherGUI();
            DoObjectWatcherGUI();
            GUILayout.EndScrollView();

            var isEmpty = true;

            if (_PropertyWatchers != null && _PropertyWatchers.Count > 0)
                isEmpty = false;
            else if (_ObjectWatchers != null && _ObjectWatchers.Count > 0)
                isEmpty = false;

            if (isEmpty)
            {
                if (AutoClose)
                {
                    Close();
                }
                else
                {
                    GUILayout.Label("Nothing is currently being watched." +
                        "\n\nRight Click on something in the Inspector and use the 'Watch' command to add it to this window.",
                        EditorStyles.wordWrappedLabel);
                    GUILayout.FlexibleSpace();
                }
            }
        }

        /************************************************************************************************************************/

        private interface IWatcher
        {
            bool TargetExists { get; }
            Object Target { get; }
            int TargetObjectCount { get; }
        }

        /************************************************************************************************************************/

        private static readonly AutoPrefs.EditorBool
            AutoClose = new AutoPrefs.EditorBool(Strings.PrefsKeyPrefix + "WatcherWindow.AutoClose", true);
        private static readonly GUIContent
            AutoCloseLabel = new GUIContent("Auto Close",
                "If enabled, this window will be closed when nothing is being watched.");

        private static float _AutoCloseWidth = -1;

        private void DoHeaderGUI()
        {
            EditorGUILayout.BeginVertical(IGEditorUtils.GetCachedStyle(
                () => new GUIStyle(GUI.skin.box) { margin = new RectOffset(), }));

            if (_AutoCloseWidth < 0)
                _AutoCloseWidth = IGEditorUtils.CalculateWidth(GUI.skin.toggle, AutoCloseLabel);

            var area = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            area.width -= _AutoCloseWidth;

            if (GUI.Button(area, "Clear"))
            {
                if (_PropertyWatchers != null)
                {
#if UNITY_2017_3_OR_NEWER
                    for (int i = 0; i < _PropertyWatchers.Count; i++)
                    {
                        PersistentValues.PersistProperty(PersistentValues.Operation.Remove, _PropertyWatchers[i].Property);
                    }
#endif
                    _PropertyWatchers.Clear();
                }

                if (_ObjectWatchers != null)
                {
#if UNITY_2017_3_OR_NEWER
                    for (int i = 0; i < _ObjectWatchers.Count; i++)
                    {
                        var targets = SerializedObjectReference.Convert(_ObjectWatchers[i].Objects);
                        PersistentValues.PersistObjects(PersistentValues.Operation.Remove, targets);
                    }
#endif
                    _ObjectWatchers.Clear();
                }

                if (AutoClose)
                {
                    Close();
                    GUIUtility.ExitGUI();
                }
            }

            area.x += area.width + IGEditorUtils.Spacing;
            area.width = _AutoCloseWidth;
            AutoClose.Value = GUI.Toggle(area, AutoClose, AutoCloseLabel);

            EditorGUILayout.EndVertical();
        }

        /************************************************************************************************************************/

        private static bool DoRemoveButtonGUI(ref Rect area, RectOffset padding)
        {
            var buttonArea = IGEditorUtils.StealFromRight(ref area, InternalGUI.SmallButtonStyle.fixedWidth, padding);
            return GUI.Button(buttonArea, "X", InternalGUI.SmallButtonStyle);
        }

        /************************************************************************************************************************/

        private static float _PersistToggleWidth = -1;

        private static bool DoPersistToggleGUI(ref Rect area, RectOffset padding, bool willPersist)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                return false;

            var label = IGEditorUtils.TempContent("Persist");

            if (_PersistToggleWidth < 0)
                _PersistToggleWidth = IGEditorUtils.CalculateWidth(GUI.skin.toggle, label);

            var persistArea = IGEditorUtils.StealFromRight(ref area, _PersistToggleWidth, padding);

            var shouldPersist = GUI.Toggle(persistArea, willPersist, label);
            return willPersist != shouldPersist;
        }

        /************************************************************************************************************************/
        #region Property Watchers
        /************************************************************************************************************************/

        public static void Watch(SerializedProperty property)
        {
            var window = GetWindow<WatcherWindow>();

            if (window._PropertyWatchers != null)
            {

                for (int i = 0; i < window._PropertyWatchers.Count; i++)
                {
                    if (IGEditorUtils.AreSameProperty(property, window._PropertyWatchers[i].Property.Property))
                        return;
                }
            }

            window.InitialisePropertyWatchers();
            window._PropertyWatchers.Add(new PropertyWatcher(property));
        }

        /************************************************************************************************************************/

        private void InitialisePropertyWatchers()
        {
            if (_PropertyWatchers == null)
                _PropertyWatchers = new List<PropertyWatcher>();

            if (_PropertyWatcherDisplay == null)
            {
                _PropertyWatcherDisplay = new ReorderableList(_PropertyWatchers, typeof(PropertyWatcher))
                {
                    headerHeight = 0,
                    elementHeightCallback = CalculatePropertyHeight,
                    drawElementCallback = DoPropertyGUI,
                    displayAdd = false,
                    displayRemove = false,
                    footerHeight = 0,
                };
            }
        }

        /************************************************************************************************************************/

        private void DoPropertyWatcherGUI()
        {
            if (_PropertyWatchers == null || _PropertyWatchers.Count == 0 || _PropertyWatcherDisplay == null)
                return;

            for (int i = _PropertyWatchers.Count - 1; i >= 0; i--)
            {
                var watcher = _PropertyWatchers[i];
                if (!watcher.TargetExists)
                    _PropertyWatchers.RemoveAt(i);
            }

            _PropertyWatcherDisplay.DoLayoutList();
        }

        /************************************************************************************************************************/

        private float CalculatePropertyHeight(int index)
        {
            return EditorGUIUtility.singleLineHeight + IGEditorUtils.Spacing +
                _PropertyWatchers[index].Property.GetPropertyHeight() + IGEditorUtils.Spacing;
        }

        /************************************************************************************************************************/

        private void DoPropertyGUI(Rect area, int index, bool isActive, bool isFocused)
        {
            var property = _PropertyWatchers[index].Property;
            var height = area.height;

            area.height = EditorGUIUtility.singleLineHeight + IGEditorUtils.Spacing;

            // Remove.
            var targetArea = area;
            if (DoRemoveButtonGUI(ref targetArea, new RectOffset()))
            {
#if UNITY_2017_3_OR_NEWER
                PersistentValues.PersistProperty(PersistentValues.Operation.Remove, property);
#endif
                _PropertyWatchers.RemoveAt(index);
                GUIUtility.ExitGUI();
            }

#if UNITY_2017_3_OR_NEWER
            // Persist.
            var willPersist = PersistentValues.WillPersist(property);
            if (DoPersistToggleGUI(ref targetArea, new RectOffset(), willPersist))
            {
                PersistentValues.PersistProperty(PersistentValues.Operation.Toggle, property);
            }
#endif

            // Target.
            property.DoTargetGUI(targetArea);

            // Property Main.
            area.y += area.height;
            area.height = height - area.height - IGEditorUtils.Spacing;
            property.DoPropertyGUI(area);
        }

        /************************************************************************************************************************/

        [Serializable]
        private sealed class PropertyWatcher : IWatcher
        {
            /************************************************************************************************************************/

            [SerializeField]
            private SerializedPropertyReference _Property;
            public SerializedPropertyReference Property { get { return _Property; } }

            public bool TargetExists { get { return _Property.TargetExists; } }

            public Object Target { get { return _Property.TargetObject; } }

            public int TargetObjectCount { get { return _Property.TargetObjects.Length; } }

            /************************************************************************************************************************/

            public PropertyWatcher(SerializedProperty property)
            {
                _Property = property;
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Object Watchers
        /************************************************************************************************************************/

        [MenuItem("CONTEXT/Object/Watch")]
        private static void Watch(MenuCommand command)
        {
            IGEditorUtils.GroupedInvoke(command, (context) =>
            {
                Watch(context.ToArray());
            });
        }

        /************************************************************************************************************************/

        public static void Watch(params Object[] targets)
        {
            var window = GetWindow<WatcherWindow>();

            if (window._ObjectWatchers != null)
            {
                for (int i = 0; i < window._ObjectWatchers.Count; i++)
                {
                    if (SerializedObjectReference.AreSameObjects(window._ObjectWatchers[i].Objects, targets))
                        return;
                }
            }

            window.InitialiseObjectWatchers();
            window._ObjectWatchers.Add(new ObjectWatcher(targets));
        }

        /************************************************************************************************************************/

        private void InitialiseObjectWatchers()
        {
            if (_ObjectWatchers == null)
                _ObjectWatchers = new List<ObjectWatcher>();
        }

        /************************************************************************************************************************/

        private void DoObjectWatcherGUI()
        {
            if (_ObjectWatchers == null)
                return;

            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth -= IGEditorUtils.IndentSize * 2;

            if (_PropertyWatchers == null || _PropertyWatchers.Count == 0)
                GUILayout.Space(-IGEditorUtils.Spacing);

            for (int i = 0; i < _ObjectWatchers.Count; i++)
            {
                var watcher = _ObjectWatchers[i];

                bool remove;
                watcher.DoGUI(out remove);

                if (remove)
                {
                    _ObjectWatchers.RemoveAt(i);
#if UNITY_2017_3_OR_NEWER
                    var targets = SerializedObjectReference.Convert(watcher.Objects);
                    PersistentValues.PersistObjects(PersistentValues.Operation.Remove, targets);
#endif
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUIUtility.labelWidth = labelWidth;
        }

        /************************************************************************************************************************/

        [Serializable]
        private sealed class ObjectWatcher : IWatcher
        {
            /************************************************************************************************************************/

            [SerializeField]
            private bool _IsExpanded = true;

            [SerializeField]
            private SerializedObjectReference[] _Objects;
            public SerializedObjectReference[] Objects { get { return _Objects; } }

            /************************************************************************************************************************/

            public bool TargetExists
            {
                get
                {
                    for (int i = 0; i < _Objects.Length; i++)
                    {
                        if (_Objects[i].Object == null)
                            return false;
                    }

                    return true;
                }
            }

            public Object Target { get { return _Objects[0].Object; } }

            public int TargetObjectCount { get { return _Objects.Length; } }

            /************************************************************************************************************************/

            private UnityEditor.Editor _Editor;
            public UnityEditor.Editor Editor
            {
                get
                {
                    if (!TargetExists)
                    {
                        Editors.Destroy(_Editor);
                        _Editor = null;
                    }
                    else if (_Editor == null)
                    {
                        _Editor = Editors.Create(SerializedObjectReference.Convert(_Objects));
                    }

                    return _Editor;
                }
            }

            /************************************************************************************************************************/

            public ObjectWatcher(Object[] targets)
            {
                _Objects = SerializedObjectReference.Convert(targets);
            }

            /************************************************************************************************************************/

            public void DoGUI(out bool remove)
            {
                remove = false;

                var editor = Editor;
                if (editor == null)
                    return;

                const float
                    Spacing = 1,
                    Padding = 2;

                // Get Rect.
                var indent = IGEditorUtils.IndentSize - 1;
                var area = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + Spacing + Padding);
                area.y += Spacing;
                area.height -= Spacing + Padding;
                area.width += 4;

                var boxArea = new Rect(-1, area.y - Spacing + 1, area.xMax + 2, 1);

                // Target.
                GUI.enabled = false;
                var showMixedValue = EditorGUI.showMixedValue;
                EditorGUI.showMixedValue = editor.targets.Length > 1;
                var targetWidth = EditorGUIUtility.labelWidth + indent - IGEditorUtils.Spacing * 2;
                var targetArea = IGEditorUtils.StealFromLeft(ref area, targetWidth, new RectOffset(0, 0, 2, 0));
                EditorGUI.ObjectField(targetArea, editor.target, typeof(Object), true);
                EditorGUI.showMixedValue = showMixedValue;

                // Remove.
                GUI.enabled = true;
                if (DoRemoveButtonGUI(ref area, new RectOffset((int)IGEditorUtils.Spacing, (int)(IGEditorUtils.Spacing * 2), 2, 0)))
                {
                    remove = true;
                    return;
                }

#if UNITY_2017_3_OR_NEWER
                // Persist.
                if (DoPersistToggleGUI(ref area, new RectOffset(0, -(int)IGEditorUtils.Spacing, 2, 0), PersistentValues.WillPersist(Target)))
                {
                    PersistentValues.PersistObjects(PersistentValues.Operation.Toggle, editor.targets);
                }
#endif

                // Titlebar.
                _IsExpanded = EditorGUI.InspectorTitlebar(area, _IsExpanded, editor.targets, false);

                EditorGUIUtility.hierarchyMode = false;
                area.xMin += 2;
                area.yMin += 2;
                EditorGUI.Foldout(area, _IsExpanded, GUIContent.none);

                GUI.Box(boxArea, "");

                if (_IsExpanded)
                {
                    GUI.enabled = true;

                    var targets = editor.targets;
                    for (int i = 0; i < targets.Length; i++)
                    {
                        if ((targets[i].hideFlags & HideFlags.NotEditable) == HideFlags.NotEditable)
                        {
                            GUI.enabled = false;
                            break;
                        }
                    }

                    // Main Inspector GUI.
                    EditorGUIUtility.hierarchyMode = true;
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(indent);
                    GUILayout.BeginVertical();
                    editor.OnInspectorGUI();
                    GUILayout.Space(IGEditorUtils.Spacing);
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #endregion
    }
}

#endif
