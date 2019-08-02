// Inspector Gadgets // Copyright 2019 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor
{
    internal static class OnOpenAsset
    {
        /************************************************************************************************************************/

        [OnOpenAsset]
        private static bool HandleOpenEvent(int instanceID, int line)
        {
            if (Event.current == null)
                return false;

            var asset = EditorUtility.InstanceIDToObject(instanceID);

            var prefab = asset as GameObject;
            if (prefab != null)
                return OnOpenPrefab(prefab);

            return OnUnhandledAsset(asset);
        }

        /************************************************************************************************************************/

        private static bool OnOpenPrefab(GameObject prefab)
        {
            var current = Event.current;

            if (!current.shift && current.alt)// Alt to Instantiate.
            {
                var instance = PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(instance, "Alt Instantiate");
                (instance as GameObject).transform.SetAsLastSibling();
                Selection.activeObject = instance;

                if (current.control)// + Ctrl to frame the instance in the scene view.
                {
                    if (SceneView.lastActiveSceneView != null)
                    {
                        SceneView.lastActiveSceneView.FrameSelected();
                    }
                }

                return true;
            }

            return false;
        }

        /************************************************************************************************************************/

        private static bool OnUnhandledAsset(Object asset)
        {
            var current = Event.current;

            if (current.control && current.shift && current.alt)// Ctrl + Shift + Alt to open in explorer.
            {
                var assetPath = AssetDatabase.GetAssetPath(asset);
                EditorUtility.RevealInFinder(assetPath);
                return true;
            }

            return false;
        }

        /************************************************************************************************************************/
    }
}

#endif
