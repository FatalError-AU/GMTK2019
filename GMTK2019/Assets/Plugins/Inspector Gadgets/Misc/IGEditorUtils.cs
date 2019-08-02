// Inspector Gadgets // Copyright 2019 Kybernetik //

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only]
    /// Various utility methods used by <see cref="InspectorGadgets"/>.
    /// </summary>
    public static partial class IGEditorUtils
    {
        /************************************************************************************************************************/

        /// <summary>Commonly used <see cref="BindingFlags"/>.</summary>
        public const BindingFlags
            AnyAccessBindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
            InstanceBindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            StaticBindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        /************************************************************************************************************************/
        #region Menu Items
        /************************************************************************************************************************/

        /// <summary>
        /// Replaces any forward slashes with backslashes: <c>/</c> -> <c>\</c>.
        /// </summary>
        public static string AllBackslashes(this string str)
        {
            return str.Replace('/', '\\');
        }

        /************************************************************************************************************************/

        [MenuItem(Strings.OpenDocumentation)]
        internal static void OpenDocumentation()
        {
            EditorUtility.OpenWithDefaultApp(Strings.DocumentationURL);
        }

        /************************************************************************************************************************/

        [MenuItem(Strings.CollapseAllComponents)]
        private static void CollapseAllComponents(MenuCommand command)
        {
            var components = ((Transform)command.context).GetComponents<Component>();

            var expand = true;

            for (int i = 0; i < components.Length; i++)
            {
                if (UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(components[i]))
                {
                    expand = false;
                    break;
                }
            }

            for (int i = 0; i < components.Length; i++)
            {
                UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(components[i], expand);
            }

            var selection = Selection.objects;

            Selection.activeObject = null;

            EditorApplication.delayCall += () =>
                EditorApplication.delayCall += () =>
                    Selection.objects = selection;
        }

        /************************************************************************************************************************/
        // Open Locked Editor Windows.

        [MenuItem(Strings.NewLockedInspector)]
        internal static void NewLockedInspector()
        {
            Type type;
            var window = CreateEditorWindow("UnityEditor.InspectorWindow", out type);

            var isLocked = type.GetProperty("isLocked", InstanceBindings);
            isLocked.GetSetMethod().Invoke(window, new object[] { true });
        }

        /************************************************************************************************************************/

        internal static void NewLockedInspector(Object target)
        {
            var selection = Selection.objects;
            Selection.activeObject = target;
            NewLockedInspector();
            Selection.objects = selection;
        }

        /************************************************************************************************************************/

        [MenuItem(Strings.ObjectNewLockedInspector)]
        private static void NewLockedInspector(MenuCommand command)
        {
            NewLockedInspector(command.context);
        }

        /************************************************************************************************************************/

        // The window throws exceptions because it fails to initialise properly.
        //[MenuItem("Assets/New Locked Project Browser")]
        //internal static void NewLockedProjectBrowser()
        //{
        //    var window = CreateEditorWindow("UnityEditor.ProjectBrowser", out type);

        //    var isLocked = type.GetField("m_IsLocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        //    isLocked.SetValue(window, true);
        //}

        /************************************************************************************************************************/

        private static EditorWindow CreateEditorWindow(string typeName, out Type type)
        {
            type = typeof(EditorWindow).Assembly.GetType(typeName);
            if (type == null)
            {
                throw new Exception("Unable to find " + typeName + " class in " + typeof(EditorWindow).Assembly.Location);
            }

            var window = ScriptableObject.CreateInstance(type) as EditorWindow;
            window.Show();
            return window;
        }

        /************************************************************************************************************************/

        [MenuItem(Strings.GameObjectResetSelectedTransforms)]
        private static void ResetSelectedTransforms()
        {
            var gameObjects = Selection.gameObjects;
            if (gameObjects.Length == 0)
                return;

            Undo.RecordObjects(gameObjects, "Reset Transforms");
            for (int i = 0; i < gameObjects.Length; i++)
            {
                var transform = gameObjects[i].transform;
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
            }
        }

        /************************************************************************************************************************/

        [MenuItem(Strings.PingScriptAsset)]
        private static void PingScriptAsset(MenuCommand menuCommand)
        {
            PingScriptAsset(menuCommand.context);
        }

        internal static void PingScriptAsset(Object obj)
        {
            MonoScript script;

            var behaviour = obj as MonoBehaviour;
            if (behaviour != null)
            {
                script = MonoScript.FromMonoBehaviour(behaviour);
            }
            else
            {
                var scriptable = obj as ScriptableObject;
                if (scriptable != null)
                    script = MonoScript.FromScriptableObject(scriptable);
                else
                    return;
            }

            if (script != null)
                EditorGUIUtility.PingObject(script);
        }

        /************************************************************************************************************************/

        [MenuItem(Strings.ShowOrHideScriptProperty)]
        private static void HideScriptProperty()
        {
            ComponentEditor.HideScriptProperty.Invert();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        /************************************************************************************************************************/

        [MenuItem(Strings.CopyTransformPath)]
        private static void CopyTransformPath(MenuCommand menuCommand)
        {
            EditorGUIUtility.systemCopyBuffer = IGUtils.GetTransformPath(menuCommand.context as Transform);
        }

        /************************************************************************************************************************/

        private static List<Object> _GroupedContext;

        /// <summary>
        /// When a context menu function is executed with multiple objects selected, it calls the method once for each
        /// object. Passing each 'command' into this method will group them all into a list and invoke the specified
        /// 'method' once they have all been gathered.
        /// </summary>
        public static void GroupedInvoke(MenuCommand command, Action<List<Object>> method)
        {
            if (_GroupedContext == null)
                _GroupedContext = new List<Object>();

            if (_GroupedContext.Count == 0)
            {
                EditorApplication.delayCall += () =>
                {
                    method(_GroupedContext);
                    _GroupedContext.Clear();
                };
            }

            _GroupedContext.Add(command.context);
        }

        /************************************************************************************************************************/
        #region Create Editor Script

        [MenuItem(Strings.CreateEditorScript)]
        private static void CreateEditorScript(MenuCommand command)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                CreateEditorScript(command.context);
        }

        internal static void CreateEditorScript(Object target)
        {
            var path = AskForEditorScriptSavePath(target);
            if (path == null)
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var editorName = Path.GetFileNameWithoutExtension(path);

            File.WriteAllText(path, BuildEditorScript(target, editorName));

            Debug.Log(editorName + " script created at " + path);

            AssetDatabase.ImportAsset(path);
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(path));
        }

        /************************************************************************************************************************/

        private static string FindComponentDirectory(Object component)
        {
            var behaviour = component as MonoBehaviour;
            if (behaviour != null)
            {
                var script = MonoScript.FromMonoBehaviour(behaviour);
                if (script != null)
                {
                    return Path.GetDirectoryName(AssetDatabase.GetAssetPath(script));
                }
            }

            return "Assets";
        }

        /************************************************************************************************************************/

        private static string AskForEditorScriptSavePath(Object target)
        {
            var path = FindComponentDirectory(target);
            var name = target.GetType().Name;
            var fileName = name + "Editor.cs";

            var dialogResult = EditorUtility.DisplayDialogComplex(
                "Create Editor Script",
                string.Concat("Create Editor Script for ", name, " at ", path, "/", fileName),
                "Create", "Browse", "Cancel");

            switch (dialogResult)
            {
                case 0:// Create.
                    return path + "/" + fileName;

                case 1:// Browse.
                    return EditorUtility.SaveFilePanelInProject("Create Editor Script", fileName, "cs",
                        "Where do you want to save the Editor Script for " + name + "?", path);

                default:// Cancel.
                    return null;
            }
        }

        /************************************************************************************************************************/

        private static string BuildEditorScript(Object target, string editorName)
        {
            const string Indent = "    ";

            var type = target.GetType();

            var text = new StringBuilder();
            text.AppendLine("#if UNITY_EDITOR");
            text.AppendLine();
            text.AppendLine("using UnityEditor;");
            text.AppendLine("using UnityEngine;");
            text.AppendLine();

            var indent = false;
            if (type.Namespace != null)
            {
                text.Append("namespace ").AppendLine(type.Namespace);
                text.AppendLine("{");
                indent = true;

                text.Append(Indent);
            }

            text.Append("[CustomEditor(typeof(");
            text.Append(type.Name);
            text.AppendLine("), true)]");

            if (indent) text.Append(Indent);
            text.Append("sealed class ");
            text.Append(editorName);
            text.Append(" : InspectorGadgets.Editor<");
            text.Append(type.Name);
            text.AppendLine(">");

            if (indent) text.Append(Indent);
            text.AppendLine("{");

            if (indent) text.Append(Indent);
            text.AppendLine("}");

            if (indent) text.AppendLine("}");

            text.AppendLine();
            text.Append("#endif");

            return text.ToString();
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Snapping
        /************************************************************************************************************************/

        /// <summary>[Editor-Only] The Unity editor's "Move X" snap setting (as specified in Edit/Snap Settings).</summary>
        public static float MoveSnapX
        {
            get { return EditorPrefs.GetFloat("MoveSnapX", 1); }
            set { EditorPrefs.SetFloat("MoveSnapX", value); }
        }

        /// <summary>[Editor-Only] The Unity editor's "Move Y" snap setting (as specified in Edit/Snap Settings).</summary>
        public static float MoveSnapY
        {
            get { return EditorPrefs.GetFloat("MoveSnapY", 1); }
            set { EditorPrefs.SetFloat("MoveSnapY", value); }
        }

        /// <summary>[Editor-Only] The Unity editor's "Move Z" snap setting (as specified in Edit/Snap Settings).</summary>
        public static float MoveSnapZ
        {
            get { return EditorPrefs.GetFloat("MoveSnapZ", 1); }
            set { EditorPrefs.SetFloat("MoveSnapZ", value); }
        }

        /// <summary>[Editor-Only] (<see cref="MoveSnapX"/>, <see cref="MoveSnapY"/>, <see cref="MoveSnapZ"/>).</summary>
        public static Vector3 MoveSnapVector
        {
            get
            {
                return new Vector3(MoveSnapX, MoveSnapY, MoveSnapZ);
            }
            set
            {
                MoveSnapX = value.x;
                MoveSnapY = value.y;
                MoveSnapZ = value.z;
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] The Unity editor's "Rotation" snap setting (as specified in Edit/Snap Settings).</summary>
        public static float RotationSnap
        {
            get { return EditorPrefs.GetFloat("RotationSnap", 15); }
            set { EditorPrefs.SetFloat("RotationSnap", value); }
        }

        /// <summary>[Editor-Only] (<see cref="RotationSnap"/>, <see cref="RotationSnap"/>, <see cref="RotationSnap"/>).</summary>
        public static Vector3 RotationSnapVector
        {
            get
            {
                var snap = RotationSnap;
                return new Vector3(snap, snap, snap);
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] The Unity editor's "Scale" snap setting (as specified in Edit/Snap Settings).</summary>
        public static float ScaleSnap
        {
            get { return EditorPrefs.GetFloat("ScaleSnap", 0.1f); }
            set { EditorPrefs.SetFloat("ScaleSnap", value); }
        }

        /// <summary>[Editor-Only] (<see cref="ScaleSnap"/>, <see cref="ScaleSnap"/>, <see cref="ScaleSnap"/>).</summary>
        public static Vector3 ScaleSnapVector
        {
            get
            {
                var snap = ScaleSnap;
                return new Vector3(snap, snap, snap);
            }
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Snaps the given 'value' to a grid with the specified 'snap' size.</summary>
        public static float Snap(float value, float snap)
        {
            return Mathf.Round(value / snap) * snap;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Snaps the given 'position' to the grid (as specified in Edit/Snap Settings).</summary>
        public static Vector3 SnapPosition(Vector3 position)
        {
            var snap = MoveSnapX;
            position.x = Snap(position.x, snap);

            snap = MoveSnapY;
            position.y = Snap(position.y, snap);

            snap = MoveSnapZ;
            position.z = Snap(position.z, snap);

            return position;
        }

        /// <summary>[Editor-Only] Snaps the given 'position' to the grid on the specified axis (as specified in Edit/Snap Settings).</summary>
        public static Vector3 SnapPosition(Vector3 position, int axisIndex)
        {
            position[axisIndex] = Snap(position[axisIndex], MoveSnapVector[axisIndex]);
            return position;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Snaps the given 'rotationEuler' to the nearest snap increment on all axes (as specified in Edit/Snap Settings).</summary>
        public static Vector3 SnapRotation(Vector3 rotationEuler)
        {
            var snap = RotationSnap;
            rotationEuler.x = Snap(rotationEuler.x, snap);
            rotationEuler.y = Snap(rotationEuler.y, snap);
            rotationEuler.z = Snap(rotationEuler.z, snap);
            return rotationEuler;
        }

        /// <summary>[Editor-Only] Snaps the given 'rotationEuler' to the nearest snap increment on the specified axis (as specified in Edit/Snap Settings).</summary>
        public static Vector3 SnapRotation(Vector3 rotationEuler, int axisIndex)
        {
            rotationEuler[axisIndex] = Snap(rotationEuler[axisIndex], RotationSnap);
            return rotationEuler;
        }

        /// <summary>[Editor-Only] Snaps the given 'rotation' to the nearest snap increment on all axes (as specified in Edit/Snap Settings).</summary>
        public static Quaternion SnapRotation(Quaternion rotation)
        {
            return Quaternion.Euler(SnapRotation(rotation.eulerAngles));
        }

        /// <summary>[Editor-Only] Snaps the given 'rotation' to the nearest snap increment on the specified axis (as specified in Edit/Snap Settings).</summary>
        public static Quaternion SnapRotation(Quaternion rotation, int axisIndex)
        {
            return Quaternion.Euler(SnapRotation(rotation.eulerAngles, axisIndex));
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Snaps the given 'scale' to the nearest snap increment on all axes (as specified in Edit/Snap Settings).</summary>
        public static Vector3 SnapScale(Vector3 scale)
        {
            var snap = ScaleSnap;
            scale.x = Snap(scale.x, snap);
            scale.y = Snap(scale.y, snap);
            scale.z = Snap(scale.z, snap);
            return scale;
        }

        /// <summary>[Editor-Only] Snaps the given 'scale' to the nearest snap increment on the specified axis (as specified in Edit/Snap Settings).</summary>
        public static Vector3 SnapScale(Vector3 scale, int axisIndex)
        {
            scale[axisIndex] = Snap(scale[axisIndex], ScaleSnap);
            return scale;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only] Returns true if 'value' is approximately equal to a multiple of 'snap'.</summary>
        public static bool IsSnapped(float value, float snap)
        {
            return Mathf.Approximately(value, Mathf.Round(value / snap) * snap);
        }

        /************************************************************************************************************************/

        [MenuItem(Strings.SnapToGrid)]
        private static void SnapSelectionToGrid()
        {
            var transforms = Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.OnlyUserModifiable);

            Undo.RecordObjects(transforms, "Snap to Grid");
            for (int i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                transform.localPosition = SnapPosition(transform.localPosition);
                transform.localRotation = SnapRotation(transform.localRotation);
                transform.localScale = SnapScale(transform.localScale);
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/

        /// <summary><see cref="EditorGUIUtility.standardVerticalSpacing"/>. This value is 2 in Unity 2018.</summary>
        public static float Spacing { get { return EditorGUIUtility.standardVerticalSpacing; } }

        /************************************************************************************************************************/

        /// <summary>[Pro-Only] [Editor-Only]
        /// Draws the GUI for all <see cref="Attributes.BaseInspectableAttribute"/>s of the 'targets'.
        /// </summary>
        public static void DoInspectableGUI(Object[] targets)
        {
            var target = targets[0];
            if (target == null)
                return;

            var inspectables = Attributes.BaseInspectableAttribute.Gather(target.GetType());
            for (int i = 0; i < inspectables.Count; i++)
            {
                var inspectable = inspectables[i];
                if (inspectable.When.IsNow())
                    inspectable.OnGUI(targets);
            }

            if (targets.Length == 1)
                DynamicInspector.DrawExtras(target);
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Draw the target and name of the specified <see cref="Delegate"/>.
        /// </summary>
        public static void DrawDelegate(Rect area, Delegate del)
        {
            var width = area.width;

            area.xMax = EditorGUIUtility.labelWidth + IndentSize;

            var obj = del.Target as Object;
            if (obj != null)
            {
                // If the target is a Unity Object, draw it in an Object Field so the user can click to ping the object.

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.ObjectField(area, obj, typeof(Object), true);
                }
            }
            else if (del.Method.DeclaringType.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), true))
            {
                // Anonymous Methods draw only their method name.

                area.width = width;

                GUI.Label(area, del.Method.GetNameCS());

                return;
            }
            else if (del.Target == null)
            {
                GUI.Label(area, del.Method.DeclaringType.GetNameCS());
            }
            else
            {
                GUI.Label(area, del.Target.ToString());
            }

            area.x += area.width;
            area.width = width - area.width;

            GUI.Label(area, del.Method.GetNameCS(false));
        }

        /************************************************************************************************************************/

        /// <summary>Used by <see cref="TempContent"/>.</summary>
        private static GUIContent _TempContent;

        /// <summary>
        /// Creates and returns a <see cref="GUIContent"/> with the specified parameters on the first call and then
        /// simply returns the same one with new parameters on each subsequent call.
        /// </summary>
        public static GUIContent TempContent(string text = null, string tooltip = null)
        {
            if (_TempContent == null)
                _TempContent = new GUIContent();

            _TempContent.text = text;
            _TempContent.tooltip = tooltip;
            return _TempContent;
        }

        /************************************************************************************************************************/

        private static Dictionary<Func<GUIStyle>, GUIStyle> _GUIStyles;

        /// <summary>
        /// Creates a <see cref="GUIStyle"/> using the provided delegate and caches it so the same style can be
        /// returned when this method is called again for the same delegate.
        /// </summary>
        /// <remarks>
        /// This method allows you to create custom styles without needing to make a new field to store them in.
        /// </remarks>
        public static GUIStyle GetCachedStyle(Func<GUIStyle> createStyle)
        {
            if (_GUIStyles == null)
                _GUIStyles = new Dictionary<Func<GUIStyle>, GUIStyle>();

            GUIStyle style;
            if (!_GUIStyles.TryGetValue(createStyle, out style))
            {
                style = createStyle();
                _GUIStyles.Add(createStyle, style);

                var currentEvent = Event.current;
                if (currentEvent != null && currentEvent.type == EventType.Repaint)
                    Debug.LogWarning("GetCachedStyle created " + createStyle + " during a Repaint event." +
                        " This likely means that a new delegate is being passed into every call" +
                        " so it can't actually return the same cached object.");
            }

            return style;
        }

        /************************************************************************************************************************/

        private static GUILayoutOption[] _DontExpandWidth;

        /// <summary>
        /// A single <see cref="GUILayoutOption"/> created by passing <c>false</c> into <see cref="GUILayout.ExpandWidth"/>.
        /// </summary>
        public static GUILayoutOption[] DontExpandWidth
        {
            get
            {
                if (_DontExpandWidth == null)
                    _DontExpandWidth = new GUILayoutOption[] { GUILayout.ExpandWidth(false) };
                return _DontExpandWidth;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Calls <see cref="GUIStyle.CalcMinMaxWidth"/> and returns the max width.
        /// </summary>
        public static float CalculateWidth(this GUIStyle style, GUIContent content)
        {
            float _, width;
            style.CalcMinMaxWidth(content, out _, out width);
            return width;
        }

        /// <summary>
        /// Calls <see cref="GUIStyle.CalcMinMaxWidth"/> and returns the max width.
        /// <para></para>
        /// This method uses the <see cref="TempContent"/>.
        /// </summary>
        public static float CalculateWidth(this GUIStyle style, string content)
        {
            return style.CalculateWidth(TempContent(content));
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Subtracts the 'width' from the left side of the 'area' and returns a new <see cref="Rect"/> occupying the
        /// removed section.
        /// </summary>
        public static Rect StealFromLeft(ref Rect area, float width)
        {
            var newRect = new Rect(area.x, area.y, width, area.height);
            area.x += width;
            area.width -= width;
            return newRect;
        }

        /// <summary>
        /// Subtracts the 'width' from the left side of the 'area' and returns a new <see cref="Rect"/> occupying the
        /// removed section.
        /// </summary>
        public static Rect StealFromLeft(ref Rect area, float width, RectOffset padding)
        {
            width += padding.left + padding.right;
            var newArea = StealFromLeft(ref area, width);
            newArea.x += padding.left;
            newArea.width -= padding.left + padding.right;
            newArea.y += padding.top;
            return newArea;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Subtracts the 'width' from the right side of the 'area' and returns a new <see cref="Rect"/> occupying the
        /// removed section.
        /// </summary>
        public static Rect StealFromRight(ref Rect area, float width)
        {
            area.width -= width;
            return new Rect(area.xMax, area.y, width, area.height);
        }

        /// <summary>
        /// Subtracts the 'width' from the right side of the 'area' and returns a new <see cref="Rect"/> occupying the
        /// removed section.
        /// </summary>
        public static Rect StealFromRight(ref Rect area, float width, RectOffset padding)
        {
            width += padding.left + padding.right;
            var newArea = StealFromRight(ref area, width);
            newArea.x += padding.left;
            newArea.width -= padding.left + padding.right;
            newArea.y += padding.top;
            return newArea;
        }

        /************************************************************************************************************************/

        private static float _IndentSize = -1;

        /// <summary>
        /// The number of pixels of indentation for each <see cref="EditorGUI.indentLevel"/> increment.
        /// </summary>
        public static float IndentSize
        {
            get
            {
                if (_IndentSize < 0)
                {
                    var indentLevel = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 1;
                    _IndentSize = EditorGUI.IndentedRect(new Rect()).x;
                    EditorGUI.indentLevel = indentLevel;
                }

                return _IndentSize;
            }
        }

        /************************************************************************************************************************/

        private static StringBuilder _ColorBuilder;

        /// <summary>Returns a string containing the hexadecimal representation of 'color'.</summary>
        public static string ColorToHex(Color32 color)
        {
            if (_ColorBuilder == null)
                _ColorBuilder = new StringBuilder();
            else
                _ColorBuilder.Length = 0;

            AppendColorToHex(_ColorBuilder, color);
            return _ColorBuilder.ToString();
        }

        /// <summary>Appends the hexadecimal representation of 'color'.</summary>
        public static void AppendColorToHex(StringBuilder text, Color32 color)
        {
            text.Append(color.r.ToString("X2"));
            text.Append(color.g.ToString("X2"));
            text.Append(color.b.ToString("X2"));
            text.Append(color.a.ToString("X2"));
        }

        /// <summary>Appends the a rich text color tag around the 'message'.</summary>
        public static void AppendColorTag(StringBuilder text, Color32 color, string message)
        {
            text.Append("<color=#");
            AppendColorToHex(text, color);
            text.Append('>');
            text.Append(message);
            text.Append("</color>");
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Calls the specified 'method' for each <see cref="SerializedProperty"/> in the 'serializedObject' then
        /// applies any modified properties.
        /// </summary>
        public static void ForEachProperty(SerializedObject serializedObject, bool enterChildren, Action<SerializedProperty> method)
        {
            var property = serializedObject.GetIterator();
            if (!property.Next(true))
                return;

            do
            {
                method(property);
            }
            while (property.Next(enterChildren));

            serializedObject.ApplyModifiedProperties();
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Creates a new <see cref="SerializedProperty"/> targeting the same field in each of the target objects of
        /// the specified 'property' and calls the 'function' for each of them, then calls
        /// <see cref="SerializedObject.ApplyModifiedProperties"/>.
        /// </summary>
        public static void ForEachTarget(SerializedProperty property, Action<SerializedProperty> function, string undoName = "Inspector")
        {
            var targets = property.serializedObject.targetObjects;

            if (undoName != null)
                Undo.RecordObjects(targets, undoName);

            if (targets.Length == 1)
            {
                function(property);
                property.serializedObject.ApplyModifiedProperties();
            }
            else
            {
                var path = property.propertyPath;
                for (int i = 0; i < targets.Length; i++)
                {
                    using (var serializedObject = new SerializedObject(targets[i]))
                    {
                        property = serializedObject.FindProperty(path);
                        function(property);
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Indicates whether both properties refer to the same underlying field.
        /// </summary>
        public static bool AreSameProperty(SerializedProperty a, SerializedProperty b)
        {
            if (a == b)
                return true;

            if (a == null)
                return b == null;

            if (b == null)
                return false;

            if (a.propertyPath != b.propertyPath)
                return false;

            var aTargets = a.serializedObject.targetObjects;
            var bTargets = b.serializedObject.targetObjects;
            if (aTargets.Length != bTargets.Length)
                return false;

            for (int i = 0; i < aTargets.Length; i++)
            {
                if (aTargets[i] != bTargets[i])
                    return false;
            }

            return true;
        }

        /************************************************************************************************************************/

        /// <summary>[Editor-Only]
        /// Searches through all assets of the specified 'type' and returns the one with a name closest to the
        /// 'nameHint'.
        /// </summary>
        public static Object FindAssetOfType(Type type, string nameHint)
        {
            var filter = typeof(Component).IsAssignableFrom(type) ? "t:GameObject" : "t:" + type.Name;
            var guids = AssetDatabase.FindAssets(filter);
            if (guids.Length == 0)
                return null;

            var assets = new Object[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath(path, type);
                if (asset != null)
                    assets[i] = asset;
            }
            return IGUtils.GetBestComponent(assets, nameHint);
        }

        /************************************************************************************************************************/

        private static List<string> _LayerNames;
        private static List<int> _LayerNumbers;

        /// <summary>[Editor-Only]
        /// Make a field for layer masks.
        /// </summary>
        public static int LayerMaskField(Rect area, GUIContent label, int layerMask)
        {
            if (_LayerNames == null)
            {
                _LayerNames = new List<string>();
                _LayerNumbers = new List<int>();
            }
            else
            {
                _LayerNames.Clear();
                _LayerNumbers.Clear();
            }

            for (int i = 0; i < 32; i++)
            {
                var layerName = LayerMask.LayerToName(i);
                if (layerName != "")
                {
                    _LayerNames.Add(layerName);
                    _LayerNumbers.Add(i);
                }
            }

            var maskWithoutEmpty = 0;
            for (int i = 0; i < _LayerNumbers.Count; i++)
            {
                if (((1 << _LayerNumbers[i]) & layerMask) > 0)
                    maskWithoutEmpty |= (1 << i);
            }

            maskWithoutEmpty = EditorGUI.MaskField(area, label, maskWithoutEmpty, _LayerNames.ToArray());
            var mask = 0;
            for (int i = 0; i < _LayerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) > 0)
                    mask |= (1 << _LayerNumbers[i]);
            }

            return mask;
        }

        /************************************************************************************************************************/

    }
}

#endif
