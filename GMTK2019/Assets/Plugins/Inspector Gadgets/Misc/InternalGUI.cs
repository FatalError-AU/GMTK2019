// Inspector Gadgets // Copyright 2019 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor
{
    internal static class InternalGUI
    {
        /************************************************************************************************************************/

        public static readonly float
            NameLabelWidth;

        public static readonly GUIStyle
            FieldLabelStyle,
            FloatFieldStyle,
            SmallButtonStyle,
            UniformScaleButtonStyle,
            ModeButtonStyle,
            ModeLabelStyle;

        /************************************************************************************************************************/

        static InternalGUI()
        {
            FieldLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 2, 2),
            };

            NameLabelWidth = FieldLabelStyle.CalcSize(new GUIContent("Rotation")).x;

            FloatFieldStyle = EditorStyles.numberField;

            SmallButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                margin = new RectOffset(0, 0, 2, 0),
                padding = new RectOffset(2, 3, 2, 2),
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = EditorGUIUtility.singleLineHeight,
                fixedWidth = EditorGUIUtility.singleLineHeight - 1,
            };

            UniformScaleButtonStyle = new GUIStyle(SmallButtonStyle)
            {
                margin = new RectOffset(2, 0, 2, 0),
                padding = new RectOffset(1, 3, -2, 0),
                fontSize = (int)EditorGUIUtility.singleLineHeight - 1
            };

            ModeLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter
            };

            ModeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset()
            };
        }

        /************************************************************************************************************************/

        public static readonly AutoPrefs.EditorVector4 SceneLabelBackgroundColor = new AutoPrefs.EditorVector4(
            Strings.PrefsKeyPrefix + "SceneLabelBackgroundColor", new Vector4(0.15f, 0.15f, 0.15f, 0.5f),
            onValueChanged: (value) => _SceneLabelBackgroundColorChanged = true);

        private static Texture2D _SceneLabelBackground;
        private static bool _SceneLabelBackgroundColorChanged;

        public static Texture2D SceneLabelBackground
        {
            get
            {
                if (SceneLabelBackgroundColor.Value.w <= 0)
                    return null;

                if (_SceneLabelBackground == null)
                {
                    _SceneLabelBackground = new Texture2D(1, 1);
                    _SceneLabelBackgroundColorChanged = true;

                    AssemblyReloadEvents.beforeAssemblyReload +=
                        () => Object.DestroyImmediate(_SceneLabelBackground);
                }

                if (_SceneLabelBackgroundColorChanged)
                {
                    _SceneLabelBackgroundColorChanged = false;
                    _SceneLabelBackground.SetPixel(0, 0, SceneLabelBackgroundColor);
                    _SceneLabelBackground.Apply();
                }

                return _SceneLabelBackground;
            }
        }

        /************************************************************************************************************************/
    }
}

#endif
