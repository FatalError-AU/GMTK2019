#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member.

// Inspector Gadgets // Copyright 2019 Kybernetik //

using UnityEngine;

namespace InspectorGadgets
{
    /// <summary>String constants used throughout <see cref="InspectorGadgets"/>.</summary>
    public static class Strings
    {
        /************************************************************************************************************************/

        /// <summary>The URL of the website where the Inspector Gadgets documentation is hosted.</summary>
        public const string DocumentationURL = "https://kybernetikgames.github.io/inspector-gadgets";

        /// <summary>The URL of the website where the Inspector Gadgets API documentation is hosted.</summary>
        public const string APIDocumentationURL = DocumentationURL + "/api/InspectorGadgets";

        /// <summary>This is Inspector Gadgets v6.0 (Preview).</summary>
        public const string InspectorGadgetsVersion = "6.0 (Preview)";

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        public const string PrefsKeyPrefix = "InspectorGadgets.";

        public const string Context = "CONTEXT/";
        public const string Alt = "&";
        public const string Ctrl = "%";
        public const string Shift = "#";

        /// <summary>
        /// Menu items where the last word begins with an underscore or certain other characters are interpreted as
        /// having a keyboard shortcut. So we use the '\b' (backspace) character to prevent it from doing that.
        /// </summary>
        public const string NegateShortcut = "\b";

        public const string CommentAssetIncludeInBuild = Context + "CommentAsset/Include in Build";
        public const string CommentComponentIncludeInBuild = Context + "CommentComponent/Include in Build";

        public const string RectTransformToggleAutoHideUI = Context + "RectTransform/Toggle Auto Hide UI";
        public const string UIBehaviourToggleAutoHideUI = Context + "UIBehaviour/Toggle Auto Hide UI";
        public const string CanvasToggleAutoHideUI = Context + "Canvas/Toggle Auto Hide UI";

        public const string PingScriptAsset = Context + "MonoBehaviour/Ping Script Asset";
        public const string ShowOrHideScriptProperty = Context + "MonoBehaviour/Show or Hide Script Property";

        public const string CreateEditorScript = Context + "Component/Create Editor Script";

        public const string CopyTransformPath = Context + "Transform/Copy Transform Path";
        public const string OpenDocumentation = Context + "Transform/Inspector Gadgets Documentation";
        public const string CollapseAllComponents = Context + "Transform/Collapse All Components";

        public const string NewLockedInspector = "Edit/Selection/New Locked Inspector " + Ctrl + Alt + "I";
        public const string ObjectNewLockedInspector = Context + "Object/New Locked Inspector " + Ctrl + Alt + "I";

        public const string GameObjectResetSelectedTransforms = "GameObject/Reset Selected Transforms " + Ctrl + Shift + "Z";
        public const string SnapToGrid = "GameObject/Snap to Grid " + Ctrl + Shift + "X";

        /************************************************************************************************************************/

        public const string PersistAfterPlayMode = "Persist After Play Mode";
        public const string PersistAfterPlayModeComponent = Context + "Component/" + PersistAfterPlayMode;

        /************************************************************************************************************************/

        public static class GUI
        {
            /************************************************************************************************************************/

            public const string
                LocalWorldTooltip = "[L] Local position/rotation/scale\n[W] World position/rotation/scale",
                ScaleModeTooltip = "[=] Uniform scale\n[≠] Non-uniform scale",
                ScaleSkewWarning = "An arbitrarily rotated object cannot have its World Scale properly represented by a Vector3" +
                    " because it may be skewed, so the value may not be accurate.",
                PrecisionWarning = "Due to floating-point precision limitations, it is recommended to bring the world coordinates" +
                    " of the GameObject within a smaller range.";

            /************************************************************************************************************************/

            public static readonly GUIContent
                X = new GUIContent("X"),
                Y = new GUIContent("Y"),
                Z = new GUIContent("Z"),
                LocalMode = new GUIContent("L", LocalWorldTooltip),
                WorldMode = new GUIContent("W", LocalWorldTooltip),
                FreezeChildTransforms = new GUIContent("F", "Freeze child transforms?"),
                DrawAllGizmos = new GUIContent("G", "Draw gizmos for all selected objects?"),
                Copy = new GUIContent("C", "Left Click = Copy to clipboard\nRight Click = Log current value"),
                Reset = new GUIContent("R", "Reset to Default"),
                UniformScale = new GUIContent("≠", ScaleModeTooltip),
                NonUniformScale = new GUIContent("=", ScaleModeTooltip);

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
#endif
        /************************************************************************************************************************/
    }
}

