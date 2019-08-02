// Inspector Gadgets // Copyright 2019 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace InspectorGadgets.Editor.PropertyDrawers
{
    /// <summary>[Pro-Only]
    /// Allows [TexDefine(KEYWORD)] to be used on texture properties in shaders.
    /// </summary>
    internal sealed class TexDefineDrawer : MaterialPropertyDrawer
    {
        /************************************************************************************************************************/

        private readonly string Keyword;
        private readonly bool Log;

        public TexDefineDrawer(string keyword)
        {
            Keyword = keyword;
        }

        /// <summary>
        /// It seems shaders can't give a bool parameter, so the presence of the second parameter will enable logging.
        /// </summary>
        public TexDefineDrawer(string keyword, string log)
        {
            Keyword = keyword;
            Log = bool.Parse(log);
        }

        /************************************************************************************************************************/

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            return MaterialEditor.GetDefaultPropertyHeight(prop);
        }

        /************************************************************************************************************************/

        public override void OnGUI(Rect area, MaterialProperty prop, string label, MaterialEditor editor)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;

            var texture = editor.TextureProperty(area, prop, label);

            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                prop.textureValue = texture;

                var material = editor.target as Material;
                if (texture != null)
                    material.EnableKeyword(Keyword);
                else
                    material.DisableKeyword(Keyword);

                if (Log)
                    Debug.Log(Keyword + " = " + (texture != null));
            }
        }

        /************************************************************************************************************************/
    }
}

#endif
