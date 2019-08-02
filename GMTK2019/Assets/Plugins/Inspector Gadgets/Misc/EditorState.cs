// Inspector Gadgets // Copyright 2019 Kybernetik //

namespace InspectorGadgets
{
    /// <summary>
    /// Represents an editor state which can be used as a condition: play mode, edit mode, or ayways.
    /// </summary>
    public enum EditorState
    {
        /// <summary>All the time, regardless of the current state of the Unity editor.</summary>
        Always,

        /// <summary>When the Unity editor is in play mode.</summary>
        Playing,

        /// <summary>When the Unity editor is not in play mode.</summary>
        Editing,
    }

    public static partial class IGUtils
    {
        /************************************************************************************************************************/

        /// <summary>
        /// Returns true if the Unity editor is currently in the specified 'state'.
        /// </summary>
        public static bool IsNow(this EditorState state)
        {
            switch (state)
            {
#if UNITY_EDITOR
                case EditorState.Playing:
                    return UnityEditor.EditorApplication.isPlaying;
                case EditorState.Editing:
                    return !UnityEditor.EditorApplication.isPlaying;
#endif
                case EditorState.Always:
                default:
                    return true;
            }
        }

        /************************************************************************************************************************/
    }
}

