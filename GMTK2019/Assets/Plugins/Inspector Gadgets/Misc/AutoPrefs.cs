// Inspector Gadgets // Copyright 2019 Kybernetik //

// The missing methods these warnings complain about are implemented by the child types, so they aren't actually missing.
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)

using System;
using UnityEngine;

namespace InspectorGadgets
{
    /// <summary>
    /// A collection of wrappers for PlayerPrefs and EditorPrefs which simplify the way you can store and retrieve values.
    /// </summary>
    public static class AutoPrefs
    {
        /************************************************************************************************************************/

        /// <summary>Encapsulates a pref value stored with a specific key.</summary>
        public interface IAutoPref
        {
            /// <summary>The key used to identify this pref.</summary>
            string Key { get; }

            /// <summary>The current value of this pref.</summary>
            object Value { get; }
        }

        /************************************************************************************************************************/

        /// <summary>Encapsulates a pref value stored with a specific key.</summary>
        public abstract class AutoPref<T> : IAutoPref
        {
            /************************************************************************************************************************/
            #region Fields and Properties
            /************************************************************************************************************************/

            /// <summary>The key used to identify this pref.</summary>
            public readonly string Key;

            /// <summary>The default value to use if this pref has no existing value.</summary>
            public readonly T DefaultValue;

            /// <summary>Called when the <see cref="Value"/> is changed.</summary>
            public readonly Action<T> OnValueChanged;

            /************************************************************************************************************************/

            private bool _IsLoaded;
            private T _Value;

            /// <summary>The current value of this pref.</summary>
            public T Value
            {
                get
                {
                    if (!_IsLoaded)
                        Reload();

                    return _Value;
                }
                set
                {
                    if (!_IsLoaded)
                    {
                        if (!IsSaved())
                        {
                            // If there is no saved value, set the value and make sure it is saved.
                            _Value = value;
                            _IsLoaded = true;
                            Save();

                            if (OnValueChanged != null)
                                OnValueChanged(value);

#if UNITY_EDITOR
                            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
#endif
                            return;
                        }
                        else Reload();
                    }

                    // Otherwise store and save the new value if it is different.
                    if (!Equals(_Value, value))
                    {
                        _Value = value;
                        Save();

                        if (OnValueChanged != null)
                            OnValueChanged(value);

#if UNITY_EDITOR
                        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
#endif
                    }
                }
            }

            /************************************************************************************************************************/

            string IAutoPref.Key { get { return Key; } }
            object IAutoPref.Value { get { return Value; } }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Methods
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="AutoPref{T}"/> with the specified 'key' and 'defaultValue'.</summary>
            protected AutoPref(string key, T defaultValue, Action<T> onValueChanged)
            {
                Key = key;
                DefaultValue = defaultValue;
                OnValueChanged = onValueChanged;
            }

            /// <summary>Loads the value of this pref from the system.</summary>
            protected abstract T Load();

            /// <summary>Saves the value of this pref to the system.</summary>
            protected abstract void Save();

            /************************************************************************************************************************/

            /// <summary>Returns the current value of this pref.</summary>
            public static implicit operator T(AutoPref<T> pref)
            {
                return pref.Value;
            }

            /************************************************************************************************************************/

            /// <summary>Checks if the value of this pref is equal to the specified 'value'.</summary>
            public static bool operator ==(AutoPref<T> pref, T value)
            {
                return Equals(pref.Value, value);
            }

            /// <summary>Checks if the value of this pref is not equal to the specified 'value'.</summary>
            public static bool operator !=(AutoPref<T> pref, T value)
            {
                return !(pref == value);
            }

            /************************************************************************************************************************/

            /// <summary>Reloads the value of this pref from the system.</summary>
            public void Reload()
            {
                _Value = Load();
                _IsLoaded = true;
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Utils
            /************************************************************************************************************************/

            /// <summary>Returns a hash code for the current pref value.</summary>
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if the preferences currently contains a saved value for this pref.</summary>
            public virtual bool IsSaved()
            {
                return PlayerPrefs.HasKey(Key);
            }

            /************************************************************************************************************************/

            /// <summary>Deletes the value of this pref from the preferences and reverts to the default value.</summary>
            public virtual void DeletePref()
            {
                PlayerPrefs.DeleteKey(Key);
                RevertToDefaultValue();
            }

            /// <summary>
            /// Sets <see cref="Value"/> = <see cref="DefaultValue"/>.
            /// </summary>
            protected void RevertToDefaultValue()
            {
                _Value = DefaultValue;
            }

            /************************************************************************************************************************/

            /// <summary>Returns Value != null ? ToString().</summary>
            public override string ToString()
            {
                var value = Value;
                return value != null ? value.ToString() : null;
            }

            /************************************************************************************************************************/
#if UNITY_EDITOR
            /************************************************************************************************************************/

            /// <summary>[Editor-Only]
            /// A delegate used to draw a GUI field and return its value.
            /// </summary>
            public delegate T GUIFieldMethod(Rect area, GUIContent content, GUIStyle style);

            /************************************************************************************************************************/

            /// <summary>[Editor-Only]
            /// Draws GUI controls for this pref and returns true if the value was changed.
            /// </summary>
            /// <param name="area">The rect in which to draw.</param>
            /// <param name="content">The content to draw.</param>
            /// <param name="style">The style to draw with.</param>
            /// <param name="doGUIField">T delegate(<see cref="Rect"/>, <see cref="GUIContent"/>, <see cref="GUIStyle"/>)</param>
            public virtual bool OnGUI(Rect area, GUIContent content, GUIStyle style, GUIFieldMethod doGUIField)
            {
                var isDefault = Equals(Value, DefaultValue);
                if (!isDefault)
                    area.width -= Editor.InternalGUI.SmallButtonStyle.fixedWidth;

                UnityEditor.EditorGUI.BeginChangeCheck();
                var value = doGUIField(area, content, style);
                if (UnityEditor.EditorGUI.EndChangeCheck())
                {
                    Value = value;
                    return true;
                }

                if (!isDefault)
                {
                    var resetStyle = Editor.InternalGUI.SmallButtonStyle;
                    var resetPosition = new Rect(area.xMax, area.y, resetStyle.fixedWidth, resetStyle.fixedHeight);

                    if (GUI.Button(resetPosition, Strings.GUI.Reset, resetStyle))
                    {
                        Value = DefaultValue;
                        return true;
                    }
                }

                return false;
            }

            /// <summary>[Editor-Only]
            /// Draws GUI controls for this pref and returns true if the value was changed.
            /// </summary>
            public virtual bool OnGUI(Rect area, GUIContent content, GUIStyle style)
            {
                return OnGUI(area, content, style, DoGUIField);
            }

            /// <summary>[Editor-Only]
            /// Draws the default GUI style used by this pref if none is specified when calling
            /// <see cref="OnGUI(Rect, GUIContent, GUIStyle)"/>.
            /// </summary>
            public virtual GUIStyle DefaultStyle { get { return UnityEditor.EditorStyles.label; } }

            /// <summary>[Editor-Only]
            /// Draws a GUI field for this pref and returns the value it is set to.
            /// </summary>
            public virtual T DoGUIField(Rect area, GUIContent content, GUIStyle style)
            {
                UnityEditor.EditorGUI.LabelField(area, content, new GUIContent(Value.ToString()), style);
                return Value;
            }

            /************************************************************************************************************************/

            /// <summary>
            /// Uses <see cref="UnityEditor.EditorGUILayout.GetControlRect(bool, float, GUIStyle, GUILayoutOption[])"/> to get a rect for a control.
            /// </summary>
            public static Rect GetControlRect(GUIStyle style, params GUILayoutOption[] options)
            {
                return UnityEditor.EditorGUILayout.GetControlRect(true, UnityEditor.EditorGUIUtility.singleLineHeight, style, options);
            }

            /// <summary>
            /// Uses <see cref="UnityEditor.EditorGUILayout.GetControlRect(bool, float, GUIStyle, GUILayoutOption[])"/> to get a rect for a control.
            /// </summary>
            public Rect GetControlRect(params GUILayoutOption[] options)
            {
                return GetControlRect(DefaultStyle, options);
            }

            /************************************************************************************************************************/

            /// <summary>[Editor-Only]
            /// Draws GUI controls for this pref and returns true if the value was changed.
            /// </summary>
            public bool OnGUI(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
            {
                var position = GetControlRect(style, options);
                return OnGUI(position, content, style);
            }

            /// <summary>[Editor-Only]
            /// Draws GUI controls for this pref and returns true if the value was changed.
            /// </summary>
            /// <param name="content">The content to draw.</param>
            /// <param name="style">The style to draw with.</param>
            /// <param name="doGUIField">T delegate(<see cref="Rect"/>, <see cref="GUIContent"/>, <see cref="GUIStyle"/>)</param>
            /// <param name="options">The layout options to use.</param>
            public bool OnGUI(GUIContent content, GUIStyle style, GUIFieldMethod doGUIField, params GUILayoutOption[] options)
            {
                var position = GetControlRect(style, options);
                return OnGUI(position, content, style, doGUIField);
            }

            /// <summary>[Editor-Only]
            /// Draws GUI controls for this pref and returns true if the value was changed.
            /// </summary>
            public bool OnGUI(string text, GUIStyle style, params GUILayoutOption[] options)
            {
                return OnGUI(new GUIContent(text), style, options);
            }

            /// <summary>[Editor-Only]
            /// Draws GUI controls for this pref and returns true if the value was changed.
            /// </summary>
            public bool OnGUI(GUIContent content, params GUILayoutOption[] options)
            {
                return OnGUI(content, DefaultStyle, options);
            }

            /// <summary>[Editor-Only]
            /// Draws GUI controls for this pref and returns true if the value was changed.
            /// </summary>
            /// <param name="content">The content to draw.</param>
            /// <param name="doGUIField">T delegate(<see cref="Rect"/>, <see cref="GUIContent"/>, <see cref="GUIStyle"/>)</param>
            /// <param name="options">The layout options to use.</param>
            public bool OnGUI(GUIContent content, GUIFieldMethod doGUIField, params GUILayoutOption[] options)
            {
                return OnGUI(content, DefaultStyle, doGUIField, options);
            }

            /// <summary>[Editor-Only]
            /// Draws GUI controls for this pref and returns true if the value was changed.
            /// </summary>
            public bool OnGUI(string text, params GUILayoutOption[] options)
            {
                return OnGUI(new GUIContent(text), options);
            }

            /************************************************************************************************************************/

            ///// <summary>[Editor-Only]
            ///// Draws a [R] button on the right side of the provided 'position' rect which resets the value of this
            ///// pref to its default value and returns true when it was clicked. The button is only drawn if the current
            ///// value is not the default, and the 'position' is adjusted accordingly.
            ///// </summary>
            //public bool DrawResetButton(ref Rect area)
            //{
            //    if (!Equals(Value, DefaultValue))
            //    {
            //        var resetStyle = InternalGUI.SmallButtonStyle;
            //        var resetPosition = new Rect(area.xMax - resetStyle.fixedWidth, area.y, resetStyle.fixedWidth, resetStyle.fixedHeight);
            //        area.width -= resetStyle.fixedWidth;

            //        if (GUI.Button(resetPosition, InternalGUI.Reset, resetStyle))
            //        {
            //            Value = DefaultValue;
            //            return true;
            //        }
            //    }
            //    else
            //    {
            //        GUIUtility.GetControlID(FocusType.Passive);
            //    }

            //    return false;
            //}

            /************************************************************************************************************************/
#endif
            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>Encapsulates a <see cref="bool"/> value stored in <see cref="PlayerPrefs"/>.</summary>
        public class Bool : AutoPref<bool>
        {
            /************************************************************************************************************************/
            #region Inherited Methods
            /************************************************************************************************************************/

            /// <summary>Constructs a <see cref="Bool"/> pref with the specified 'key' and 'defaultValue'.</summary>
            public Bool(string key, bool defaultValue = default(bool), Action<bool> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="PlayerPrefs"/>.</summary>
            protected override bool Load()
            {
                return PlayerPrefs.GetInt(Key, DefaultValue ? 1 : 0) > 0;
            }

            /// <summary>Saves the value of this pref to <see cref="PlayerPrefs"/>.</summary>
            protected override void Save()
            {
                PlayerPrefs.SetInt(Key, Value ? 1 : 0);
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="Bool"/> pref using the specified string as the key.</summary>
            public static implicit operator Bool(string key)
            {
                return new Bool(key);
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
            #region Utils
            /************************************************************************************************************************/

            /// <summary>Toggles the value of this pref from false to true or vice versa.</summary>
            public void Invert()
            {
                Value = !Value;
            }

            /************************************************************************************************************************/
#if UNITY_EDITOR
            /************************************************************************************************************************/

            /// <summary>[Editor-Only]
            /// Draws a GUI field for this pref and returns the value it is set to.
            /// </summary>
            public override bool DoGUIField(Rect area, GUIContent content, GUIStyle style)
            {
                return UnityEditor.EditorGUI.Toggle(area, content, Value, style);
            }

            /// <summary>[Editor-Only]
            /// Draws the default GUI style used by this pref if none is specified when calling
            /// <see cref="AutoPref{T}.OnGUI(Rect, GUIContent, GUIStyle)"/>.
            /// </summary>
            public override GUIStyle DefaultStyle { get { return UnityEditor.EditorStyles.toggle; } }

            /************************************************************************************************************************/
#endif
            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>Encapsulates a <see cref="float"/> value stored in <see cref="PlayerPrefs"/>.</summary>
        public class Float : AutoPref<float>
        {
            /************************************************************************************************************************/

            /// <summary>Constructs a <see cref="Float"/> pref with the specified 'key' and 'defaultValue'.</summary>
            public Float(string key, float defaultValue = default(float), Action<float> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="PlayerPrefs"/>.</summary>
            protected override float Load()
            {
                return PlayerPrefs.GetFloat(Key, DefaultValue);
            }

            /// <summary>Saves the value of this pref to <see cref="PlayerPrefs"/>.</summary>
            protected override void Save()
            {
                PlayerPrefs.SetFloat(Key, Value);
            }

            /************************************************************************************************************************/
            #region Operators
            /************************************************************************************************************************/

            /// <summary>Checks if the value of this pref is greater then the specified 'value'.</summary>
            public static bool operator >(Float pref, float value)
            {
                return pref.Value > value;
            }

            /// <summary>Checks if the value of this pref is less then the specified 'value'.</summary>
            public static bool operator <(Float pref, float value)
            {
                return pref.Value < value;
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="Float"/> pref using the specified string as the key.</summary>
            public static implicit operator Float(string key)
            {
                return new Float(key);
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
#if UNITY_EDITOR
            /************************************************************************************************************************/

            /// <summary>[Editor-Only]
            /// Draws a GUI field for this pref and returns the value it is set to.
            /// </summary>
            public override float DoGUIField(Rect area, GUIContent content, GUIStyle style)
            {
                return UnityEditor.EditorGUI.FloatField(area, content, Value, style);
            }

            /// <summary>[Editor-Only]
            /// Draws the default GUI style used by this pref if none is specified when calling
            /// <see cref="AutoPref{T}.OnGUI(Rect, GUIContent, GUIStyle)"/>.
            /// </summary>
            public override GUIStyle DefaultStyle { get { return UnityEditor.EditorStyles.numberField; } }

            /************************************************************************************************************************/
#endif
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>Encapsulates a <see cref="int"/> value stored in <see cref="PlayerPrefs"/>.</summary>
        public class Int : AutoPref<int>
        {
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="Int"/> pref with the specified 'key' and 'defaultValue'.</summary>
            public Int(string key, int defaultValue = default(int), Action<int> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="PlayerPrefs"/>.</summary>
            protected override int Load()
            {
                return PlayerPrefs.GetInt(Key, DefaultValue);
            }

            /// <summary>Saves the value of this pref to <see cref="PlayerPrefs"/>.</summary>
            protected override void Save()
            {
                PlayerPrefs.SetInt(Key, Value);
            }

            /************************************************************************************************************************/
            #region Operators
            /************************************************************************************************************************/

            /// <summary>Checks if the value of this pref is greater then the specified 'value'.</summary>
            public static bool operator >(Int pref, int value)
            {
                return pref.Value > value;
            }

            /// <summary>Checks if the value of this pref is less then the specified 'value'.</summary>
            public static bool operator <(Int pref, int value)
            {
                return pref.Value < value;
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="Int"/> pref using the specified string as the key.</summary>
            public static implicit operator Int(string key)
            {
                return new Int(key);
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
#if UNITY_EDITOR
            /************************************************************************************************************************/

            /// <summary>[Editor-Only]
            /// Draws a GUI field for this pref and returns the value it is set to.
            /// </summary>
            public override int DoGUIField(Rect area, GUIContent content, GUIStyle style)
            {
                return UnityEditor.EditorGUI.IntField(area, content, Value, style);
            }

            /// <summary>[Editor-Only]
            /// Draws the default GUI style used by this pref if none is specified when calling
            /// <see cref="AutoPref{T}.OnGUI(Rect, GUIContent, GUIStyle)"/>.
            /// </summary>
            public override GUIStyle DefaultStyle { get { return UnityEditor.EditorStyles.numberField; } }

            /************************************************************************************************************************/
#endif
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>Encapsulates a <see cref="string"/> value stored in <see cref="PlayerPrefs"/>.</summary>
        public class String : AutoPref<string>
        {
            /************************************************************************************************************************/

            /// <summary>Constructs a <see cref="String"/> pref with the specified 'key' and 'defaultValue'.</summary>
            public String(string key, string defaultValue = default(string), Action<string> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="PlayerPrefs"/>.</summary>
            protected override string Load()
            {
                return PlayerPrefs.GetString(Key, DefaultValue);
            }

            /// <summary>Saves the value of this pref to <see cref="PlayerPrefs"/>.</summary>
            protected override void Save()
            {
                PlayerPrefs.SetString(Key, Value);
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="String"/> pref using the specified string as the key.</summary>
            public static implicit operator String(string key)
            {
                return new String(key);
            }

            /************************************************************************************************************************/
#if UNITY_EDITOR
            /************************************************************************************************************************/

            /// <summary>[Editor-Only]
            /// Draws a GUI field for this pref and returns the value it is set to.
            /// </summary>
            public override string DoGUIField(Rect area, GUIContent content, GUIStyle style)
            {
                return UnityEditor.EditorGUI.TextField(area, content, Value, style);
            }

            /// <summary>[Editor-Only]
            /// Draws the default GUI style used by this pref if none is specified when calling
            /// <see cref="AutoPref{T}.OnGUI(Rect, GUIContent, GUIStyle)"/>.
            /// </summary>
            public override GUIStyle DefaultStyle { get { return UnityEditor.EditorStyles.textField; } }

            /************************************************************************************************************************/
#endif
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>Encapsulates a <see cref="UnityEngine.Vector2"/> value stored in <see cref="PlayerPrefs"/>.</summary>
        public class Vector2 : AutoPref<UnityEngine.Vector2>
        {
            /************************************************************************************************************************/

            // Key is used as KeyX.

            /// <summary>The key used to identify the y value of this pref.</summary>
            public readonly string KeyY;

            /************************************************************************************************************************/

            /// <summary>Constructs a <see cref="Vector2"/> pref with the specified 'key' and 'defaultValue'.</summary>
            public Vector2(string key,
                UnityEngine.Vector2 defaultValue = default(UnityEngine.Vector2),
                Action<UnityEngine.Vector2> onValueChanged = null)
                : base(key + "X", defaultValue, onValueChanged)
            {
                KeyY = key + "Y";
            }

            /// <summary>Loads the value of this pref from <see cref="PlayerPrefs"/>.</summary>
            protected override UnityEngine.Vector2 Load()
            {
                return new UnityEngine.Vector2(
                    PlayerPrefs.GetFloat(Key, DefaultValue.x),
                    PlayerPrefs.GetFloat(KeyY, DefaultValue.y));
            }

            /// <summary>Saves the value of this pref to <see cref="PlayerPrefs"/>.</summary>
            protected override void Save()
            {
                PlayerPrefs.SetFloat(Key, Value.x);
                PlayerPrefs.SetFloat(KeyY, Value.y);
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="PlayerPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
            {
                return
                    PlayerPrefs.HasKey(Key) &&
                    PlayerPrefs.HasKey(KeyY);
            }

            /// <summary>Deletes the value of this pref from <see cref="PlayerPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                PlayerPrefs.DeleteKey(Key);
                PlayerPrefs.DeleteKey(KeyY);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="Vector2"/> pref using the specified string as the key.</summary>
            public static implicit operator Vector2(string key)
            {
                return new Vector2(key);
            }

            /************************************************************************************************************************/
#if UNITY_EDITOR
            /************************************************************************************************************************/

            /// <summary>[Editor-Only]
            /// Draws a GUI field for this pref and returns the value it is set to.
            /// </summary>
            public override UnityEngine.Vector2 DoGUIField(Rect area, GUIContent content, GUIStyle style)
            {
                return UnityEditor.EditorGUI.Vector2Field(area, content, Value);
            }

            /// <summary>[Editor-Only]
            /// Draws the default GUI style used by this pref if none is specified when calling
            /// <see cref="AutoPref{T}.OnGUI(Rect, GUIContent, GUIStyle)"/>.
            /// </summary>
            public override GUIStyle DefaultStyle { get { return null; } }

            /************************************************************************************************************************/
#endif
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>Encapsulates a <see cref="UnityEngine.Vector3"/> value stored in <see cref="PlayerPrefs"/>.</summary>
        public class Vector3 : AutoPref<UnityEngine.Vector3>
        {
            /************************************************************************************************************************/

            // Key is used as KeyX.

            /// <summary>The key used to identify the y value of this pref.</summary>
            public readonly string KeyY;

            /// <summary>The key used to identify the z value of this pref.</summary>
            public readonly string KeyZ;

            /************************************************************************************************************************/

            /// <summary>Constructs a <see cref="Vector3"/> pref with the specified 'key' and 'defaultValue'.</summary>
            public Vector3(string key,
                UnityEngine.Vector3 defaultValue = default(UnityEngine.Vector3),
                Action<UnityEngine.Vector3> onValueChanged = null)
                : base(key + "X", defaultValue, onValueChanged)
            {
                KeyY = key + "Y";
                KeyZ = key + "Z";
            }

            /// <summary>Loads the value of this pref from <see cref="PlayerPrefs"/>.</summary>
            protected override UnityEngine.Vector3 Load()
            {
                return new UnityEngine.Vector3(
                    PlayerPrefs.GetFloat(Key, DefaultValue.x),
                    PlayerPrefs.GetFloat(KeyY, DefaultValue.y),
                    PlayerPrefs.GetFloat(KeyZ, DefaultValue.z));
            }

            /// <summary>Saves the value of this pref to <see cref="PlayerPrefs"/>.</summary>
            protected override void Save()
            {
                PlayerPrefs.SetFloat(Key, Value.x);
                PlayerPrefs.SetFloat(KeyY, Value.y);
                PlayerPrefs.SetFloat(KeyZ, Value.z);
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="PlayerPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
            {
                return
                    PlayerPrefs.HasKey(Key) &&
                    PlayerPrefs.HasKey(KeyY) &&
                    PlayerPrefs.HasKey(KeyZ);
            }

            /// <summary>Deletes the value of this pref from <see cref="PlayerPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                PlayerPrefs.DeleteKey(Key);
                PlayerPrefs.DeleteKey(KeyY);
                PlayerPrefs.DeleteKey(KeyZ);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="Vector3"/> pref using the specified string as the key.</summary>
            public static implicit operator Vector3(string key)
            {
                return new Vector3(key);
            }

            /// <summary>Returns a <see cref="Color"/> using the (x, y, z) of the pref as (r, g, b, a = 1).</summary>
            public static implicit operator Color(Vector3 pref)
            {
                var value = pref.Value;
                return new Color(value.x, value.y, value.z, 1);
            }

            /************************************************************************************************************************/
#if UNITY_EDITOR
            /************************************************************************************************************************/

            /// <summary>[Editor-Only]
            /// Draws a GUI field for this pref and returns the value it is set to.
            /// </summary>
            public override UnityEngine.Vector3 DoGUIField(Rect area, GUIContent content, GUIStyle style)
            {
                return UnityEditor.EditorGUI.Vector3Field(area, content, Value);
            }

            /// <summary>[Editor-Only]
            /// Draws the default GUI style used by this pref if none is specified when calling
            /// <see cref="AutoPref{T}.OnGUI(Rect, GUIContent, GUIStyle)"/>.
            /// </summary>
            public override GUIStyle DefaultStyle { get { return null; } }

            /************************************************************************************************************************/

            /// <summary>[Editor-Only]
            /// Draws a <see cref="Color"/> field for this pref and returns true if the value was changed.
            /// </summary>
            public bool DoColorGUIField(Rect area, GUIContent content)
            {
                return OnGUI(area, content, UnityEditor.EditorStyles.colorField, (area2, content2, style2) =>
                {
                    var color = (Color)this;
                    color = UnityEditor.EditorGUI.ColorField(area2, content2, color);
                    return new UnityEngine.Vector3(color.r, color.g, color.b);
                });
            }

            /// <summary>[Editor-Only]
            /// Draws a <see cref="Color"/> field for this pref and returns true if the value was changed.
            /// </summary>
            public bool DoColorGUIField(GUIContent content)
            {
                return OnGUI(content, UnityEditor.EditorStyles.colorField, (area2, content2, style2) =>
                {
                    var color = (Color)this;
                    color = UnityEditor.EditorGUI.ColorField(area2, content2, color);
                    return new UnityEngine.Vector3(color.r, color.g, color.b);
                });
            }

            /************************************************************************************************************************/
#endif
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>Encapsulates a <see cref="UnityEngine.Vector4"/> value stored in <see cref="PlayerPrefs"/>.</summary>
        public class Vector4 : AutoPref<UnityEngine.Vector4>
        {
            /************************************************************************************************************************/

            // Key is used as KeyX.

            /// <summary>The key used to identify the y value of this pref.</summary>
            public readonly string KeyY;

            /// <summary>The key used to identify the z value of this pref.</summary>
            public readonly string KeyZ;

            /// <summary>The key used to identify the w value of this pref.</summary>
            public readonly string KeyW;

            /************************************************************************************************************************/

            /// <summary>Constructs a <see cref="Vector4"/> pref with the specified 'key' and 'defaultValue'.</summary>
            public Vector4(string key,
                UnityEngine.Vector4 defaultValue = default(UnityEngine.Vector4),
                Action<UnityEngine.Vector4> onValueChanged = null)
                : base(key + "X", defaultValue, onValueChanged)
            {
                KeyY = key + "Y";
                KeyZ = key + "Z";
                KeyW = key + "W";
            }

            /// <summary>Loads the value of this pref from <see cref="PlayerPrefs"/>.</summary>
            protected override UnityEngine.Vector4 Load()
            {
                return new UnityEngine.Vector4(
                    PlayerPrefs.GetFloat(Key, DefaultValue.x),
                    PlayerPrefs.GetFloat(KeyY, DefaultValue.y),
                    PlayerPrefs.GetFloat(KeyZ, DefaultValue.z),
                    PlayerPrefs.GetFloat(KeyW, DefaultValue.w));
            }

            /// <summary>Saves the value of this pref to <see cref="PlayerPrefs"/>.</summary>
            protected override void Save()
            {
                PlayerPrefs.SetFloat(Key, Value.x);
                PlayerPrefs.SetFloat(KeyY, Value.y);
                PlayerPrefs.SetFloat(KeyZ, Value.z);
                PlayerPrefs.SetFloat(KeyW, Value.w);
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="PlayerPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
            {
                return
                    PlayerPrefs.HasKey(Key) &&
                    PlayerPrefs.HasKey(KeyY) &&
                    PlayerPrefs.HasKey(KeyZ) &&
                    PlayerPrefs.HasKey(KeyW);
            }

            /// <summary>Deletes the value of this pref from <see cref="PlayerPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                PlayerPrefs.DeleteKey(Key);
                PlayerPrefs.DeleteKey(KeyY);
                PlayerPrefs.DeleteKey(KeyZ);
                PlayerPrefs.DeleteKey(KeyW);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="Vector4"/> pref using the specified string as the key.</summary>
            public static implicit operator Vector4(string key)
            {
                return new Vector4(key);
            }

            /// <summary>Returns a <see cref="Color"/> using the (x, y, z, w) of the pref as (r, g, b, a).</summary>
            public static implicit operator Color(Vector4 pref)
            {
                var value = pref.Value;
                return new Color(value.x, value.y, value.z, value.w);
            }

            /************************************************************************************************************************/
#if UNITY_EDITOR
            /************************************************************************************************************************/

            /// <summary>[Editor-Only]
            /// Draws a GUI field for this pref and returns the value it is set to.
            /// </summary>
            public override UnityEngine.Vector4 DoGUIField(Rect area, GUIContent content, GUIStyle style)
            {
                return UnityEditor.EditorGUI.Vector4Field(area, content, Value);
            }

            /// <summary>[Editor-Only]
            /// Draws the default GUI style used by this pref if none is specified when calling
            /// <see cref="AutoPref{T}.OnGUI(Rect, GUIContent, GUIStyle)"/>.
            /// </summary>
            public override GUIStyle DefaultStyle { get { return null; } }

            /************************************************************************************************************************/

            /// <summary>[Editor-Only]
            /// Draws a <see cref="Color"/> field for this pref and returns true if the value was changed.
            /// </summary>
            public bool DoColorGUIField(Rect area, GUIContent content)
            {
                return OnGUI(area, content, UnityEditor.EditorStyles.colorField, (area2, content2, style2) =>
                {
                    var color = (Color)this;
                    color = UnityEditor.EditorGUI.ColorField(area2, content2, color);
                    return color;
                });
            }

            /// <summary>[Editor-Only]
            /// Draws a <see cref="Color"/> field for this pref and returns true if the value was changed.
            /// </summary>
            public bool DoColorGUIField(GUIContent content)
            {
                return OnGUI(content, UnityEditor.EditorStyles.colorField, (area2, content2, style2) =>
                {
                    var color = (Color)this;
                    color = UnityEditor.EditorGUI.ColorField(area2, content2, color);
                    return color;
                });
            }

            /************************************************************************************************************************/
#endif
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>Encapsulates a <see cref="UnityEngine.Quaternion"/> value stored in <see cref="PlayerPrefs"/>.</summary>
        public class Quaternion : AutoPref<UnityEngine.Quaternion>
        {
            /************************************************************************************************************************/

            // Key is used as KeyX.

            /// <summary>The key used to identify the y value of this pref.</summary>
            public readonly string KeyY;

            /// <summary>The key used to identify the z value of this pref.</summary>
            public readonly string KeyZ;

            /// <summary>The key used to identify the w value of this pref.</summary>
            public readonly string KeyW;

            /************************************************************************************************************************/

            /// <summary>Constructs a <see cref="Quaternion"/> pref with the specified 'key' and 'defaultValue'.</summary>
            public Quaternion(string key,
                UnityEngine.Quaternion defaultValue = default(UnityEngine.Quaternion),
                Action<UnityEngine.Quaternion> onValueChanged = null)
                : base(key + "X", defaultValue, onValueChanged)
            {
                KeyY = key + "Y";
                KeyZ = key + "Z";
                KeyW = key + "W";
            }

            /// <summary>Loads the value of this pref from <see cref="PlayerPrefs"/>.</summary>
            protected override UnityEngine.Quaternion Load()
            {
                return new UnityEngine.Quaternion(
                    PlayerPrefs.GetFloat(Key, DefaultValue.x),
                    PlayerPrefs.GetFloat(KeyY, DefaultValue.y),
                    PlayerPrefs.GetFloat(KeyZ, DefaultValue.z),
                    PlayerPrefs.GetFloat(KeyW, DefaultValue.w));
            }

            /// <summary>Saves the value of this pref to <see cref="PlayerPrefs"/>.</summary>
            protected override void Save()
            {
                PlayerPrefs.SetFloat(Key, Value.x);
                PlayerPrefs.SetFloat(KeyY, Value.y);
                PlayerPrefs.SetFloat(KeyZ, Value.z);
                PlayerPrefs.SetFloat(KeyW, Value.w);
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="PlayerPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
            {
                return
                    PlayerPrefs.HasKey(Key) &&
                    PlayerPrefs.HasKey(KeyY) &&
                    PlayerPrefs.HasKey(KeyZ) &&
                    PlayerPrefs.HasKey(KeyW);
            }

            /// <summary>Deletes the value of this pref from <see cref="PlayerPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                PlayerPrefs.DeleteKey(Key);
                PlayerPrefs.DeleteKey(KeyY);
                PlayerPrefs.DeleteKey(KeyZ);
                PlayerPrefs.DeleteKey(KeyW);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="Quaternion"/> pref using the specified string as the key.</summary>
            public static implicit operator Quaternion(string key)
            {
                return new Quaternion(key);
            }

            /************************************************************************************************************************/
#if UNITY_EDITOR
            /************************************************************************************************************************/

            /// <summary>[Editor-Only]
            /// Draws a GUI field for this pref and returns the value it is set to.
            /// </summary>
            public override UnityEngine.Quaternion DoGUIField(Rect area, GUIContent content, GUIStyle style)
            {
                return UnityEngine.Quaternion.Euler(UnityEditor.EditorGUI.Vector3Field(area, content, Value.eulerAngles));
            }

            /// <summary>[Editor-Only]
            /// Draws the default GUI style used by this pref if none is specified when calling
            /// <see cref="AutoPref{T}.OnGUI(Rect, GUIContent, GUIStyle)"/>.
            /// </summary>
            public override GUIStyle DefaultStyle { get { return null; } }

            /************************************************************************************************************************/
#endif
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
        #region Editor Prefs
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>Encapsulates a <see cref="bool"/> value stored in <see cref="UnityEditor.EditorPrefs"/>.</summary>
        public sealed class EditorBool : Bool
        {
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="EditorBool"/> pref with the specified 'key' and 'defaultValue'.</summary>
            public EditorBool(string key, bool defaultValue = default(bool), Action<bool> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="UnityEditor.EditorPrefs"/>.</summary>
            protected override bool Load()
            {
                return UnityEditor.EditorPrefs.GetBool(Key, DefaultValue);
            }

            /// <summary>Saves the value of this pref to <see cref="UnityEditor.EditorPrefs"/>.</summary>
            protected override void Save()
            {
                UnityEditor.EditorPrefs.SetBool(Key, Value);
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="UnityEditor.EditorPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
            {
                return UnityEditor.EditorPrefs.HasKey(Key);
            }

            /// <summary>Deletes the value of this pref from <see cref="UnityEditor.EditorPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                UnityEditor.EditorPrefs.DeleteKey(Key);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="EditorBool"/> pref using the specified string as the key.</summary>
            public static implicit operator EditorBool(string key)
            {
                return new EditorBool(key);
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>Encapsulates a <see cref="float"/> value stored in <see cref="UnityEditor.EditorPrefs"/>.</summary>
        public sealed class EditorFloat : Float
        {
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="EditorFloat"/> pref with the specified 'key' and 'defaultValue'.</summary>
            public EditorFloat(string key, float defaultValue = default(float), Action<float> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="UnityEditor.EditorPrefs"/>.</summary>
            protected override float Load()
            {
                return UnityEditor.EditorPrefs.GetFloat(Key, DefaultValue);
            }

            /// <summary>Saves the value of this pref to <see cref="UnityEditor.EditorPrefs"/>.</summary>
            protected override void Save()
            {
                UnityEditor.EditorPrefs.SetFloat(Key, Value);
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="UnityEditor.EditorPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
            {
                return UnityEditor.EditorPrefs.HasKey(Key);
            }

            /// <summary>Deletes the value of this pref from <see cref="UnityEditor.EditorPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                UnityEditor.EditorPrefs.DeleteKey(Key);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/
            #region Operators
            /************************************************************************************************************************/

            /// <summary>Checks if the value of this pref is greater then the specified 'value'.</summary>
            public static bool operator >(EditorFloat pref, float value)
            {
                return pref.Value > value;
            }

            /// <summary>Checks if the value of this pref is less then the specified 'value'.</summary>
            public static bool operator <(EditorFloat pref, float value)
            {
                return pref.Value < value;
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="EditorFloat"/> pref using the specified string as the key.</summary>
            public static implicit operator EditorFloat(string key)
            {
                return new EditorFloat(key);
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>Encapsulates a <see cref="int"/> value stored in <see cref="UnityEditor.EditorPrefs"/>.</summary>
        public sealed class EditorInt : Int
        {
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="EditorInt"/> pref with the specified 'key' and 'defaultValue'.</summary>
            public EditorInt(string key, int defaultValue = default(int), Action<int> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="UnityEditor.EditorPrefs"/>.</summary>
            protected override int Load()
            {
                return UnityEditor.EditorPrefs.GetInt(Key, DefaultValue);
            }

            /// <summary>Saves the value of this pref to <see cref="UnityEditor.EditorPrefs"/>.</summary>
            protected override void Save()
            {
                UnityEditor.EditorPrefs.SetInt(Key, Value);
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="UnityEditor.EditorPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
            {
                return UnityEditor.EditorPrefs.HasKey(Key);
            }

            /// <summary>Deletes the value of this pref from <see cref="UnityEditor.EditorPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                UnityEditor.EditorPrefs.DeleteKey(Key);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/
            #region Operators
            /************************************************************************************************************************/

            /// <summary>Checks if the value of this pref is greater then the specified 'value'.</summary>
            public static bool operator >(EditorInt pref, int value)
            {
                return pref.Value > value;
            }

            /// <summary>Checks if the value of this pref is less then the specified 'value'.</summary>
            public static bool operator <(EditorInt pref, int value)
            {
                return pref.Value < value;
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="EditorInt"/> pref using the specified string as the key.</summary>
            public static implicit operator EditorInt(string key)
            {
                return new EditorInt(key);
            }

            /************************************************************************************************************************/
            #endregion
            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>Encapsulates a <see cref="string"/> value stored in <see cref="UnityEditor.EditorPrefs"/>.</summary>
        public sealed class EditorString : String
        {
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="EditorString"/> pref with the specified 'key' and 'defaultValue'.</summary>
            public EditorString(string key, string defaultValue = default(string), Action<string> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="UnityEditor.EditorPrefs"/>.</summary>
            protected override string Load()
            {
                return UnityEditor.EditorPrefs.GetString(Key, DefaultValue);
            }

            /// <summary>Saves the value of this pref to <see cref="UnityEditor.EditorPrefs"/>.</summary>
            protected override void Save()
            {
                UnityEditor.EditorPrefs.SetString(Key, Value);
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="UnityEditor.EditorPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
            {
                return UnityEditor.EditorPrefs.HasKey(Key);
            }

            /// <summary>Deletes the value of this pref from <see cref="UnityEditor.EditorPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                UnityEditor.EditorPrefs.DeleteKey(Key);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="EditorString"/> pref using the specified string as the key.</summary>
            public static implicit operator EditorString(string key)
            {
                return new EditorString(key);
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>Encapsulates a <see cref="UnityEngine.Vector2"/> value stored in <see cref="UnityEditor.EditorPrefs"/>.</summary>
        public sealed class EditorVector2 : Vector2
        {
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="EditorString"/> pref with the specified 'key' and 'defaultValue'.</summary>
            public EditorVector2(string key,
                UnityEngine.Vector2 defaultValue = default(UnityEngine.Vector2),
                Action<UnityEngine.Vector2> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="UnityEditor.EditorPrefs"/>.</summary>
            protected override UnityEngine.Vector2 Load()
            {
                return new UnityEngine.Vector2(
                    UnityEditor.EditorPrefs.GetFloat(Key, DefaultValue.x),
                    UnityEditor.EditorPrefs.GetFloat(KeyY, DefaultValue.y));
            }

            /// <summary>Saves the value of this pref to <see cref="UnityEditor.EditorPrefs"/>.</summary>
            protected override void Save()
            {
                UnityEditor.EditorPrefs.SetFloat(Key, Value.x);
                UnityEditor.EditorPrefs.SetFloat(KeyY, Value.y);
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="UnityEditor.EditorPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
            {
                return
                    UnityEditor.EditorPrefs.HasKey(Key) &&
                    UnityEditor.EditorPrefs.HasKey(KeyY);
            }

            /// <summary>Deletes the value of this pref from <see cref="UnityEditor.EditorPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                UnityEditor.EditorPrefs.DeleteKey(Key);
                UnityEditor.EditorPrefs.DeleteKey(KeyY);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="EditorVector2"/> pref using the specified string as the key.</summary>
            public static implicit operator EditorVector2(string key)
            {
                return new EditorVector2(key);
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>Encapsulates a <see cref="UnityEngine.Vector3"/> value stored in <see cref="UnityEditor.EditorPrefs"/>.</summary>
        public class EditorVector3 : Vector3
        {
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="EditorVector3"/> pref.</summary>
            public EditorVector3(string key,
                UnityEngine.Vector3 defaultValue = default(UnityEngine.Vector3),
                Action<UnityEngine.Vector3> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="UnityEditor.EditorPrefs"/>.</summary>
            protected override UnityEngine.Vector3 Load()
            {
                return new UnityEngine.Vector3(
                    UnityEditor.EditorPrefs.GetFloat(Key, DefaultValue.x),
                    UnityEditor.EditorPrefs.GetFloat(KeyY, DefaultValue.y),
                    UnityEditor.EditorPrefs.GetFloat(KeyZ, DefaultValue.z));
            }

            /// <summary>Saves the value of this pref to <see cref="UnityEditor.EditorPrefs"/>.</summary>
            protected override void Save()
            {
                UnityEditor.EditorPrefs.SetFloat(Key, Value.x);
                UnityEditor.EditorPrefs.SetFloat(KeyY, Value.y);
                UnityEditor.EditorPrefs.SetFloat(KeyZ, Value.z);
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="UnityEditor.EditorPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
            {
                return
                    UnityEditor.EditorPrefs.HasKey(Key) &&
                    UnityEditor.EditorPrefs.HasKey(KeyY) &&
                    UnityEditor.EditorPrefs.HasKey(KeyZ);
            }

            /// <summary>Deletes the value of this pref from <see cref="UnityEditor.EditorPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                UnityEditor.EditorPrefs.DeleteKey(Key);
                UnityEditor.EditorPrefs.DeleteKey(KeyY);
                UnityEditor.EditorPrefs.DeleteKey(KeyZ);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="EditorVector3"/> pref using the specified string as the key.</summary>
            public static implicit operator EditorVector3(string key)
            {
                return new EditorVector3(key);
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>Encapsulates a <see cref="Vector4"/> value stored in <see cref="UnityEditor.EditorPrefs"/>.</summary>
        public class EditorVector4 : Vector4
        {
            /************************************************************************************************************************/

            /// <summary>Constructs an <see cref="EditorVector4"/> pref.</summary>
            public EditorVector4(string key,
                UnityEngine.Vector4 defaultValue = default(UnityEngine.Vector4),
                Action<UnityEngine.Vector4> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Loads the value of this pref from <see cref="UnityEditor.EditorPrefs"/>.</summary>
            protected override UnityEngine.Vector4 Load()
            {
                return new UnityEngine.Vector4(
                    UnityEditor.EditorPrefs.GetFloat(Key, DefaultValue.x),
                    UnityEditor.EditorPrefs.GetFloat(KeyY, DefaultValue.y),
                    UnityEditor.EditorPrefs.GetFloat(KeyZ, DefaultValue.z),
                    UnityEditor.EditorPrefs.GetFloat(KeyW, DefaultValue.w));
            }

            /// <summary>Saves the value of this pref to <see cref="UnityEditor.EditorPrefs"/>.</summary>
            protected override void Save()
            {
                UnityEditor.EditorPrefs.SetFloat(Key, Value.x);
                UnityEditor.EditorPrefs.SetFloat(KeyY, Value.y);
                UnityEditor.EditorPrefs.SetFloat(KeyZ, Value.z);
                UnityEditor.EditorPrefs.SetFloat(KeyW, Value.w);
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="UnityEditor.EditorPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
            {
                return
                    UnityEditor.EditorPrefs.HasKey(Key) &&
                    UnityEditor.EditorPrefs.HasKey(KeyY) &&
                    UnityEditor.EditorPrefs.HasKey(KeyZ) &&
                    UnityEditor.EditorPrefs.HasKey(KeyW);
            }

            /// <summary>Deletes the value of this pref from <see cref="UnityEditor.EditorPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                UnityEditor.EditorPrefs.DeleteKey(Key);
                UnityEditor.EditorPrefs.DeleteKey(KeyY);
                UnityEditor.EditorPrefs.DeleteKey(KeyZ);
                UnityEditor.EditorPrefs.DeleteKey(KeyW);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="EditorVector4"/> pref using the specified string as the key.</summary>
            public static implicit operator EditorVector4(string key)
            {
                return new EditorVector4(key);
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/

        /// <summary>Encapsulates a <see cref="UnityEngine.Quaternion"/> value stored in <see cref="UnityEditor.EditorPrefs"/>.</summary>
        public class EditorQuaternion : Quaternion
        {
            /************************************************************************************************************************/

            /// <summary>Constructs a <see cref="Quaternion"/> pref with the specified 'key' and 'defaultValue'.</summary>
            public EditorQuaternion(string key,
                UnityEngine.Quaternion defaultValue = default(UnityEngine.Quaternion),
                Action<UnityEngine.Quaternion> onValueChanged = null)
                : base(key, defaultValue, onValueChanged)
            { }

            /// <summary>Constructs a <see cref="Quaternion"/> pref with the specified 'key' and <see cref="UnityEngine.Quaternion.identity"/> as the default value.</summary>
            public EditorQuaternion(string key)
                : base(key, UnityEngine.Quaternion.identity)
            { }

            /// <summary>Loads the value of this pref from <see cref="UnityEditor.EditorPrefs"/>.</summary>
            protected override UnityEngine.Quaternion Load()
            {
                return new UnityEngine.Quaternion(
                    UnityEditor.EditorPrefs.GetFloat(Key, DefaultValue.x),
                    UnityEditor.EditorPrefs.GetFloat(KeyY, DefaultValue.y),
                    UnityEditor.EditorPrefs.GetFloat(KeyZ, DefaultValue.z),
                    UnityEditor.EditorPrefs.GetFloat(KeyW, DefaultValue.w));
            }

            /// <summary>Saves the value of this pref to <see cref="UnityEditor.EditorPrefs"/>.</summary>
            protected override void Save()
            {
                UnityEditor.EditorPrefs.SetFloat(Key, Value.x);
                UnityEditor.EditorPrefs.SetFloat(KeyY, Value.y);
                UnityEditor.EditorPrefs.SetFloat(KeyZ, Value.z);
                UnityEditor.EditorPrefs.SetFloat(KeyW, Value.w);
            }

            /************************************************************************************************************************/

            /// <summary>Returns true if <see cref="UnityEditor.EditorPrefs"/> currently contains a value for this pref.</summary>
            public override bool IsSaved()
            {
                return
                    UnityEditor.EditorPrefs.HasKey(Key) &&
                    UnityEditor.EditorPrefs.HasKey(KeyY) &&
                    UnityEditor.EditorPrefs.HasKey(KeyZ) &&
                    UnityEditor.EditorPrefs.HasKey(KeyW);
            }

            /// <summary>Deletes the value of this pref from <see cref="UnityEditor.EditorPrefs"/> and reverts to the default value.</summary>
            public override void DeletePref()
            {
                UnityEditor.EditorPrefs.DeleteKey(Key);
                UnityEditor.EditorPrefs.DeleteKey(KeyY);
                UnityEditor.EditorPrefs.DeleteKey(KeyZ);
                UnityEditor.EditorPrefs.DeleteKey(KeyW);
                RevertToDefaultValue();
            }

            /************************************************************************************************************************/

            /// <summary>Creates a new <see cref="EditorQuaternion"/> pref using the specified string as the key.</summary>
            public static implicit operator EditorQuaternion(string key)
            {
                return new EditorQuaternion(key);
            }

            /************************************************************************************************************************/
        }

        /************************************************************************************************************************/
#endif
        #endregion
        /************************************************************************************************************************/
    }
}

