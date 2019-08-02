// Inspector Gadgets // Copyright 2019 Kybernetik //

// This script is based on a concept created by Astral Byte Ltd: http://www.astralbyte.co.nz/code/AutoHideUILayer.cs.
// It has been heavily modified and improved for use in Inspector Gadgets, but the core concept is the same.

#pragma warning disable CS0618 // Type or member is obsolete: EditorApplication.playmodeStateChanged

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only]
    /// Automatically hides the UI layer inside the Editor so it doesn't get in the way of 3D objects in scene view.
    ///<para></para>
    /// When any object is selected that is on the UI layer, the layer will be shown and the camera changed to 2D orthographic and zoomed to the current selection.
    ///<para></para>
    /// When any object on another layer is selected, the UI layer will be hidden and the camera changed back to the previous state.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoHideUI
    {
        /************************************************************************************************************************/

        /// <summary>The inbuilt layer named "UI".</summary>
        public const int
            UiLayer = 5;

        /// <summary>Is this system currently operating?</summary>
        public static readonly AutoPrefs.EditorBool
            IsEnabled = new AutoPrefs.EditorBool(Strings.PrefsKeyPrefix + "AutoHideUI", false, OnEnabledChanged);

        /// <summary>Is the UI layer currently visible?</summary>
        public static readonly AutoPrefs.EditorBool
            IsShowingUI = Strings.PrefsKeyPrefix + "IsShowingUI";

        /// <summary>
        /// If true, selecting a UI object will focus the scene camera on that object. Otherwise it will focus on the
        /// root <see cref="Canvas"/> of that object.
        /// </summary>
        public static readonly AutoPrefs.EditorBool
            FocusOnSelection = Strings.PrefsKeyPrefix + "FocusOnSelection";

        /// <summary>Was the scene camera in 2D mode before the UI was focussed?</summary>
        public static readonly AutoPrefs.EditorBool
            Previous2dMode = Strings.PrefsKeyPrefix + "Previous2dMode";

        /// <summary>Was the scene camera in Orthographic mode before the UI was focussed?</summary>
        public static readonly AutoPrefs.EditorBool
            PreviousOrthographicMode = Strings.PrefsKeyPrefix + "PreviousOrthographicMode";

        /// <summary>The bit mask of layers that are considered to contain UI objects.</summary>
        public static readonly AutoPrefs.EditorInt
            UILayerMask = new AutoPrefs.EditorInt(Strings.PrefsKeyPrefix + "UILayerMask", 1 << UiLayer);

        /// <summary>The pivot point of the scene camera from before the UI was focussed.</summary>
        public static readonly AutoPrefs.EditorVector3
            PreviousPivot = Strings.PrefsKeyPrefix + "PreviousPivot";

        /// <summary>The rotation of the scene camera from before the UI was focussed.</summary>
        public static readonly AutoPrefs.EditorQuaternion
            PreviousRotation = Strings.PrefsKeyPrefix + "PreviousRotation";

        /// <summary>The orthographic size of the scene camera from before the UI was focussed.</summary>
        public static readonly AutoPrefs.EditorFloat
            PreviousSize = new AutoPrefs.EditorFloat(Strings.PrefsKeyPrefix + "PreviousSize", 1);

        /************************************************************************************************************************/

        static AutoHideUI()
        {
            EditorApplication.delayCall += () =>
            {
                if (!IsEnabled.IsSaved())
                {
                    Selection.selectionChanged += OnFirstRun;
                }
                else if (IsEnabled)
                {
                    Selection.selectionChanged += OnSelectionChanged;
                    EditorApplication.playmodeStateChanged += OnSelectionChanged;

                    if (!EditorApplication.isPlayingOrWillChangePlaymode && !ShouldShow())
                    {
                        Tools.visibleLayers &= ~UILayerMask;
                    }
                }

#if UNITY_2018_1_OR_NEWER
                EditorApplication.quitting += OnEditorQuit;
#endif
            };
        }

        /************************************************************************************************************************/

        private static void OnFirstRun()
        {
            if (!ShouldShow())
                return;

            Selection.selectionChanged -= OnFirstRun;

            IsEnabled.Value = false;

            const string FirstRunMessage =
@"Would you like Inspector Gadgets to automatically show and hide the UI layer?

On UI selected: show UI layer, enter 2D orthographic mode, and focus the camera on the selected object.

On UI deselected: hide UI layer and return camera to the previous state.

You can turn this feature on/off via the Inspector Gadgets tab in the Edit/Preferences menu.";

            if (EditorUtility.DisplayDialog("Auto Hide UI", FirstRunMessage, "Enable", "Do Nothing"))
            {
                IsEnabled.Value = true;
                EditorApplication.delayCall += Enable;
            }
        }

        /************************************************************************************************************************/

        private static void Enable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.playmodeStateChanged += OnSelectionChanged;
            OnSelectionChanged();
        }

        /************************************************************************************************************************/

        private static bool ShouldShow()
        {
            var activeGameObject = Selection.activeGameObject;
            if (activeGameObject == null ||
                !activeGameObject.activeInHierarchy ||
                EditorUtility.IsPersistent(activeGameObject))
                return false;

            if (activeGameObject.layer == UiLayer)
                return true;

            var canvas = activeGameObject.GetComponentInParent<Canvas>();
            if (canvas != null &&
                canvas.renderMode != RenderMode.WorldSpace)
                return true;

            return false;
        }

        /************************************************************************************************************************/

        private static void OnSelectionChanged()
        {
            if (ShouldShow())
                ShowUI();
            else
                HideUI();
        }

        /************************************************************************************************************************/

        private static void ShowUI()
        {
            if (IsShowingUI)
                return;

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
                return;

            // Store the current scene view state.
            Previous2dMode.Value = sceneView.in2DMode;
            PreviousOrthographicMode.Value = sceneView.orthographic;
            PreviousPivot.Value = sceneView.pivot;
            PreviousRotation.Value = sceneView.rotation;
            PreviousSize.Value = sceneView.size;

            // Apply UI mode and show the UI layer.
            sceneView.in2DMode = true;
            sceneView.orthographic = true;

            Tools.visibleLayers |= UILayerMask;
            IsShowingUI.Value = true;

            if (!FocusOnSelection)
            {
                var rootRect = Selection.activeTransform.root.GetComponentInChildren<RectTransform>();

                if (rootRect != null)
                {
                    var corners = new Vector3[4];
                    rootRect.GetWorldCorners(corners);

                    var bounds = new Bounds(corners[0], Vector3.zero);
                    bounds.Encapsulate(corners[1]);
                    bounds.Encapsulate(corners[2]);
                    bounds.Encapsulate(corners[3]);

                    var size = Mathf.Max(bounds.size.x / sceneView.camera.aspect, bounds.size.y) * 1.1f;

                    if (size > 0)
                    {
                        // This gives us a much better fit than sceneView.Frame(bounds, false);
                        sceneView.LookAt(bounds.center, rootRect.rotation, size, true, false);
                        return;
                    }
                }
            }

            sceneView.FrameSelected();
        }

        /************************************************************************************************************************/

        private static void HideUI()
        {
            if (!IsShowingUI)
                return;

            var previousPivot = PreviousPivot.Value;
            var previousRotation = PreviousRotation.Value;
            var previousSize = PreviousSize.Value;

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
                return;

            // Return to the stored scene view state and hide the UI layer.
            sceneView.in2DMode = Previous2dMode;

            // Only revert the camera state if all the values are actually valid.
            if (IsValidNumber(previousPivot.x) &&
                IsValidNumber(previousPivot.y) &&
                IsValidNumber(previousPivot.z) &&
                IsValidNumber(previousRotation.x) &&
                IsValidNumber(previousRotation.y) &&
                IsValidNumber(previousRotation.z) &&
                IsValidNumber(previousRotation.w) &&
                IsValidNumber(previousSize))
            {
                sceneView.LookAt(previousPivot, previousRotation, previousSize, PreviousOrthographicMode);
            }

            Tools.visibleLayers &= ~UILayerMask;
            IsShowingUI.Value = false;
        }

        /************************************************************************************************************************/

        private static bool IsValidNumber(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        /************************************************************************************************************************/

        private static void OnEditorQuit()
        {
            // If we are currently enabled when the editor closes, show the UI layer in case the next project the user
            // opens doesn't have this script.

            if (IsEnabled)
            {
                Tools.visibleLayers |= UILayerMask;
            }
        }

        /************************************************************************************************************************/
        #region Preferences
        /************************************************************************************************************************/

        private static GUIContent
            _EnabledToggleContent,
            _UILayerMaskContent;
        private static GUIContent[] _FocusModes;

        /************************************************************************************************************************/

        internal static void DoPrefsGUI()
        {
            // Enabled.
            if (_EnabledToggleContent == null)
            {
                _EnabledToggleContent = new GUIContent("Auto Hide UI",
                   "If enabled, the UI layer will be automatically hidden until a UI object is selected.");
                _UILayerMaskContent = new GUIContent("UI Layer Mask",
                   "Objects on these layers will trigger the Auto Hide feature.");
            }

            IsEnabled.OnGUI(_EnabledToggleContent);

            var guiEnabled = GUI.enabled;

            // UI Layer Mask.
            UILayerMask.OnGUI(_UILayerMaskContent, (position, content, style) =>
            {
                return IGEditorUtils.LayerMaskField(position, content, UILayerMask);
            });

            // Auto Focus Mode.
            if (!IsEnabled)
                GUI.enabled = false;

            if (_FocusModes == null)
            {
                _FocusModes = new GUIContent[]
                {
                    new GUIContent("Root", "When a UI object is first selected, the scene camera will frame the root RectTransform of the selected object."),
                    new GUIContent("Selection", "When a UI object is first selected, the scene camera will frame the selected object."),
                };
            }

            FocusOnSelection.OnGUI(new GUIContent("UI Auto Focus Mode"), (position, label, style) =>
            {
                EditorGUI.PrefixLabel(position, label);

                position.xMin += EditorGUIUtility.labelWidth;

                var previousMode = FocusOnSelection ? 1 : 0;
                return GUI.Toolbar(position, previousMode, _FocusModes) != 0;
            });

            GUI.enabled = guiEnabled;
        }

        /************************************************************************************************************************/

        private static void OnEnabledChanged(bool value)
        {
            if (value)
            {
                Enable();
            }
            else
            {
                Selection.selectionChanged -= OnSelectionChanged;
                EditorApplication.playmodeStateChanged -= OnSelectionChanged;
                HideUI();
                Tools.visibleLayers |= UILayerMask;
            }
        }

        /************************************************************************************************************************/

        [MenuItem(Strings.RectTransformToggleAutoHideUI)]
        [MenuItem(Strings.UIBehaviourToggleAutoHideUI)]
        [MenuItem(Strings.CanvasToggleAutoHideUI)]
        private static void ToggleEnabled()
        {
            IsEnabled.Invert();
            Debug.Log("Inspector Gadgets Auto Hide UI is now " +
                (IsEnabled.Value ? "Enabled" : "Disabled"));
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif
