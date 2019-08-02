// Inspector Gadgets // Copyright 2019 Kybernetik //

//#define LOG_DETAILS

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InspectorGadgets.Editor
{
    /// <summary>[Editor-Only]
    /// A serializable reference to a <see cref="Object"/>.
    /// </summary>
    [Serializable]
    public sealed class SerializedObjectReference
    {
        /************************************************************************************************************************/

        [SerializeField] private Object _Object;
        [SerializeField] private int _InstanceID;

        /************************************************************************************************************************/

        /// <summary>The referenced <see cref="SerializedObject"/>.</summary>
        public Object Object
        {
            get
            {
                Initialise();
                return _Object;
            }
        }

        /// <summary>The <see cref="Object.GetInstanceID"/>.</summary>
        public int InstanceID { get { return _InstanceID; } }

        /************************************************************************************************************************/

        /// <summary>
        /// Constructs a new <see cref="SerializedObjectReference"/> which wraps the specified
        /// <see cref="UnityEngine.Object"/>.
        /// </summary>
        public SerializedObjectReference(Object obj)
        {
            _Object = obj;
            if (obj != null)
                _InstanceID = obj.GetInstanceID();
        }

        /************************************************************************************************************************/

        private void Initialise()
        {
            if (_Object == null)
                _Object = EditorUtility.InstanceIDToObject(_InstanceID);
            else
                _InstanceID = _Object.GetInstanceID();

        }

        /************************************************************************************************************************/

        /// <summary>
        /// Constructs a new <see cref="SerializedObjectReference"/> which wraps the specified
        /// <see cref="UnityEngine.Object"/>.
        /// </summary>
        public static implicit operator SerializedObjectReference(Object obj)
        {
            return new SerializedObjectReference(obj);
        }

        /// <summary>
        /// Returns the target <see cref="Object"/>.
        /// </summary>
        public static implicit operator Object(SerializedObjectReference reference)
        {
            return reference.Object;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Creates a new array of <see cref="SerializedObjectReference"/>s representing the 'objects'.
        /// </summary>
        public static SerializedObjectReference[] Convert(params Object[] objects)
        {
            var references = new SerializedObjectReference[objects.Length];
            for (int i = 0; i < objects.Length; i++)
                references[i] = objects[i];
            return references;
        }

        /// <summary>
        /// Creates a new array of <see cref="UnityEngine.Object"/>s containing the target <see cref="Object"/> of each
        /// of the 'references'.
        /// </summary>
        public static Object[] Convert(params SerializedObjectReference[] references)
        {
            var objects = new Object[references.Length];
            for (int i = 0; i < references.Length; i++)
                objects[i] = references[i];
            return objects;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Indicates whether both arrays refer to the same set of objects.
        /// </summary>
        public static bool AreSameObjects(SerializedObjectReference[] references, Object[] objects)
        {
            if (references == null)
                return objects == null;

            if (objects == null)
                return false;

            if (references.Length != objects.Length)
                return false;

            for (int i = 0; i < references.Length; i++)
            {
                if (references[i] != objects[i])
                    return false;
            }

            return true;
        }

        /************************************************************************************************************************/

        /// <summary>Returns a string describing this object.</summary>
        public override string ToString()
        {
            return "SerializedObjectReference [" + _InstanceID + "] " + _Object;
        }

        /************************************************************************************************************************/
    }
}

#endif
