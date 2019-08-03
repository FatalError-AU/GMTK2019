using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Animation;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(AnimationCollection))]
    public class AnimationCollectionEditor : UnityEditor.Editor
    {
        private static Type[] availableTypes = FetchTypes();
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty type = serializedObject.FindProperty("serializedType");
            SerializedProperty clips = serializedObject.FindProperty("animations");

            EditorGUILayout.BeginVertical();
            
            Type typeVal = DrawTypeDropdown(type);
            DrawAnimationsList(typeVal, clips);
            
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        public Type DrawTypeDropdown(SerializedProperty type)
        {
            EditorGUILayout.BeginHorizontal();
            Type typeVal = AnimationCollection.ParseType(type.stringValue);

            string[] stringTypes = availableTypes
                    .Select(x => new {obj = x, attr = x.GetCustomAttribute(typeof(AnimationCollection.EnumAttribute)) as AnimationCollection.EnumAttribute})
                    .Select(x => string.IsNullOrWhiteSpace(x.attr.name) ? x.obj.FullName : x.attr.name)
                    .Prepend("None").ToArray();

            int index = typeVal != null ? Array.IndexOf(availableTypes, typeVal) : -1;
            
            int selection = EditorGUILayout.Popup("Type", index + 1, stringTypes);

            if (selection != index + 1)
                type.stringValue = selection == 0 ? null : availableTypes[selection - 1].AssemblyQualifiedName;

            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh"), GUILayout.Width(EditorGUIUtility.singleLineHeight * 2F), GUILayout.Height(EditorGUIUtility.singleLineHeight - 1F)))
                availableTypes = FetchTypes();
            
            EditorGUILayout.EndHorizontal();
            
            return typeVal;
        }

        public void DrawAnimationsList(Type typeVal, SerializedProperty clips)
        {
            if (typeVal == null)
            {
                EditorGUILayout.HelpBox("You must select a type before specifying animator clips", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();

            string[] vals = Enum.GetNames(typeVal);

            for (int i = 0; i < clips.arraySize && i < vals.Length; i++)
            {
                MemberInfo info = typeVal.GetMember(vals[i]).FirstOrDefault();
                if (info != null)
                {
                    IEnumerable<PropertyAttribute> attribs = info.GetCustomAttributes<PropertyAttribute>();
                    
                    foreach(PropertyAttribute attrib in attribs)
                    {
                        switch (attrib)
                        {
                            case SpaceAttribute _:
                                EditorGUILayout.Space();
                                break;
                            case HeaderAttribute header:
                                EditorGUILayout.Space();
                                EditorGUILayout.LabelField(header.header, EditorStyles.centeredGreyMiniLabel);
                                break;
                        }
                    }
                }

                EditorGUILayout.PropertyField(clips.GetArrayElementAtIndex(i), new GUIContent(ObjectNames.NicifyVariableName(vals[i])));
            }
        }

        private static Type[] FetchTypes() => 
                (from a in AppDomain.CurrentDomain.GetAssemblies()
                from t in a.GetTypes()
                where t.IsEnum && t.GetCustomAttribute(typeof(AnimationCollection.EnumAttribute)) != null
                select t).ToArray();
    }
}
