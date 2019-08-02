// Inspector Gadgets // Copyright 2019 Kybernetik //

#if UNITY_EDITOR

using InspectorGadgets.Editor.PropertyDrawers;
using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor
{
    internal static class Preferences
    {
        /************************************************************************************************************************/

#pragma warning disable CS0414// Private field is assigned but its value is never used.
        private static readonly GUIContent
            // Transform Inspector.
            ShowCopyButton = new GUIContent("Show Copy Button",
                "If enabled, the Transform Inspector will show the [C] button to copy a transform property to an internal clipboard."),
            ShowPasteButton = new GUIContent("Show Paste Button",
                "If enabled, the Transform Inspector will show the [P] button to paste a transform property from the internal clipboard."),
            ShowSnapButton = new GUIContent("Show Snap Button",
                "If enabled, the Transform Inspector will show the [S] button to snap a transform property to the nearest snap increment specified in Edit/Snap Setings."),
            ShowResetButton = new GUIContent("Show Reset Button",
                "If enabled, the Transform Inspector will show the [R] button to reset a transform property to its default value."),
            DisableUselessButtons = new GUIContent("Disable Useless Buttons",
                "If enabled, the [C/P/R/S] buttons will be greyed out when they would do nothing."),
            UseFieldColors = new GUIContent("Use Field Colors",
                "If enabled, the X/Y/Z fields will be colored Red/Green/Blue respectively."),
            FieldPrimaryColor = new GUIContent("Field Primary Color",
                "The strength of the main color for each axis."),
            FieldSecondaryColor = new GUIContent("Field Secondary Color",
                "The strength of the other colors."),
            EmphasizeNonDefaultFields = new GUIContent("Emphasize Non-Default Fields",
                "If enabled, Transform fields which aren't at their default value will be given a thicker border."),
            ItaliciseNonSnappedFields = new GUIContent("Italicise Non-Snapped Fields",
                "If enabled, Transform fields which aren't a multiple of the snap increment specified in Edit/Snap Setings will use italic text."),
            DefaultToUniformScale = new GUIContent("Default to Uniform Scale",
                "If enabled, Transform scale will be shown as a single float field by default when the selected object has the same scale on all axes."),
            SnapToGroundDistance = new GUIContent("Snap to Ground Distance",
                "The distance within which to check for the ground when using the Snap to Ground function in the Transform Position context menu."),
            SnapToGroundLayers = new GUIContent("Snap to Ground Layers",
                "This layer mask determines which physics layers are treated as ground for the Transform Position context menu."),

            // Scene Tools.
            OverrideTransformGizmos = new GUIContent("Override Transform Gizmos",
                "If enabled, the default scene gizmos will be overwritten in order to implement various features like \"Freeze child transforms\" and \"Draw gizmos for all selected objects\"."),
            ShowMovementGuides = new GUIContent("Show Movement Guides",
                "If enabled, the scene view movement tool will show some extra lines while you are moving an object to indicate where you are moving it from."),
            ShowMovementDistance = new GUIContent("Show Movement Distance",
                "If enabled, moving an object will display the distance from the old position."),
            ShowMovementDistancePerAxis = new GUIContent("Show Movement Distance Per Axis",
                "If enabled, the distance moved on each individual axis will also be displayed."),
            SceneLabelBackgroundColor = new GUIContent("Scene Label Background Color",
                "The color to use behind scene view labels to make them easier to read."),
            ShowPositionLabels = new GUIContent("Show Position Labels",
                "If enabled, the scene view will show the selected object's position around the Move tool."),

            // Script Inspector.
            HideScriptProperty = new GUIContent("Hide Script Property",
                "If enabled, the \"Script\" property at the top of each inspector will be hidden to save space."),
            AutoGatherRequiredComponents = new GUIContent("Auto Gather Required Components",
                "If enabled, selecting an object in the editor will automatically gather references for any of its component fields with a [RequireAssignment] attribute." +
                "\n\nGathering is conducted using InspectorGadgetsUtils.GetComponentInHierarchy which finds the most appropriately named component in the selected object's children or parents."),
            AutoGatherSerializedComponents = new GUIContent("Auto Gather Serialized Components",
                "If enabled, selecting an object in the editor will automatically gather references for any of its component fields which are public or have [SerializeField] attribute." +
                "\n\nGathering is conducted using InspectorGadgetsUtils.GetComponentInHierarchy which finds the most appropriately named component in the selected object's children or parents."),
            ItaliciseSelfReferences = new GUIContent("Italicise Self References",
                "If enabled, Object reference fields will be drawn in italics when referencing another component on the same GameObject."),
            ObjectEditorNestLimit = new GUIContent("Object Editor Nest Limit",
                "If higher than 0, Object fields will be drawn with a foldout arrow to draw the target object's inspector nested inside the current one.");
#pragma warning restore CS0414// Private field is assigned but its value is never used.

        /************************************************************************************************************************/

#pragma warning disable CS0618 // Type or member is obsolete (Unity 2018.3+).
        [PreferenceItem("Inspector\nGadgets")]
#pragma warning restore CS0618 // Type or member is obsolete (Unity 2018.3+).
        private static void DrawPreferences()
        {
            // Version.
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Inspector Gadgets Pro v" + Strings.InspectorGadgetsVersion,
                    GUILayout.Width(EditorGUIUtility.labelWidth - 4));

                if (GUILayout.Button("Open Manual"))
                    IGEditorUtils.OpenDocumentation();
            }
            GUILayout.EndHorizontal();

            var enabled = GUI.enabled;

            EditorGUILayout.Space();
            GUILayout.Label("Transform Inspector", EditorStyles.boldLabel);

            TransformPropertyDrawer.ShowCopyButton.OnGUI(ShowCopyButton);
            TransformPropertyDrawer.ShowPasteButton.OnGUI(ShowPasteButton);
            TransformPropertyDrawer.ShowSnapButton.OnGUI(ShowSnapButton);
            TransformPropertyDrawer.ShowResetButton.OnGUI(ShowResetButton);
            TransformPropertyDrawer.DisableUselessButtons.OnGUI(DisableUselessButtons);

            TransformPropertyDrawer.UseFieldColors.OnGUI(UseFieldColors);
            if (!TransformPropertyDrawer.UseFieldColors)
                GUI.enabled = false;

            TransformPropertyDrawer.FieldPrimaryColor.OnGUI(FieldPrimaryColor, GUI.skin.horizontalSlider,
            (area, content, style) =>
            {
                return EditorGUI.Slider(area, content, TransformPropertyDrawer.FieldPrimaryColor, 0, 1);
            });

            TransformPropertyDrawer.FieldSecondaryColor.OnGUI(FieldSecondaryColor, GUI.skin.horizontalSlider,
            (area, content, style) =>
            {
                return EditorGUI.Slider(area, content, TransformPropertyDrawer.FieldSecondaryColor, 0, 1);
            });

            GUI.enabled = enabled;

            TransformPropertyDrawer.EmphasizeNonDefaultFields.OnGUI(EmphasizeNonDefaultFields);
            TransformPropertyDrawer.ItaliciseNonSnappedFields.OnGUI(ItaliciseNonSnappedFields);
            TransformEditor.DefaultToUniformScale.OnGUI(DefaultToUniformScale);

            if (PositionDrawer.SnapToGroundDistance.OnGUI(SnapToGroundDistance))
                PositionDrawer.SnapToGroundDistance.Value = Mathf.Max(PositionDrawer.SnapToGroundDistance, 0);
            PositionDrawer.SnapToGroundLayers.OnGUI(SnapToGroundLayers, (position, content, style) =>
            {
                return IGEditorUtils.LayerMaskField(position, content, PositionDrawer.SnapToGroundLayers);
            });

            EditorGUILayout.Space();
            GUILayout.Label("Scene Tools", EditorStyles.boldLabel);
            TransformEditor.OverrideTransformGizmos.OnGUI(OverrideTransformGizmos);
            if (!TransformEditor.OverrideTransformGizmos)
            {
                EditorGUILayout.HelpBox("With this disabled, features like \"Freeze child transforms\" and \"Draw gizmos for all selected objects\" won't work.", MessageType.Warning);
                Tools.hidden = false;
                GUI.enabled = false;
            }
            PositionDrawer.ShowMovementGuides.OnGUI(ShowMovementGuides);
            PositionDrawer.ShowMovementDistance.OnGUI(ShowMovementDistance);

            if (!PositionDrawer.ShowMovementDistance)
                GUI.enabled = false;
            PositionDrawer.ShowMovementDistancePerAxis.OnGUI(ShowMovementDistancePerAxis);
            GUI.enabled = enabled;

            InternalGUI.SceneLabelBackgroundColor.DoColorGUIField(SceneLabelBackgroundColor);

            PositionDrawer.ShowPositionLabels.OnGUI(ShowPositionLabels);

            GUI.enabled = true;
            AutoHideUI.DoPrefsGUI();

            EditorGUILayout.Space();
            GUILayout.Label("Script Inspector", EditorStyles.boldLabel);

            ComponentEditor.HideScriptProperty.OnGUI(HideScriptProperty);
            ObjectDrawer.ItaliciseSelfReferences.OnGUI(ItaliciseSelfReferences);

            ObjectDrawer.ObjectEditorNestLimit.OnGUI(ObjectEditorNestLimit,
                (position, content, style) => EditorGUI.IntSlider(position, content, ObjectDrawer.ObjectEditorNestLimit.Value, 0, 10));

            if (GUILayout.Button("Find and Fix Missing Scripts"))
                EditorWindow.GetWindow<MissingScriptWindow>();

        }

        /************************************************************************************************************************/
    }
}

#endif
