using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Animation
{
    [CreateAssetMenu(menuName = "Animation Collection", fileName = "Animations", order = 400)]
    public class AnimationCollection : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        private string serializedType;
        [SerializeField]
        private string[] enums;
        [SerializeField]
        private AnimationClip[] animations;

        private Type enumType;
        private Dictionary<Enum, AnimationClip> valueMap;

        public bool IsType(Type type)
        {
            return enumType == type;
        }

        public AnimationClip GetClip(Enum at)
        {
            if (!valueMap.ContainsKey(at))
                return null;

            return valueMap[at];
        }
        
        public void OnBeforeSerialize()
        {
            if (enumType == null)
                return;
            
            serializedType = enumType.AssemblyQualifiedName;

            if (valueMap != null)
            {
                enums = valueMap.Keys.Select(x => x.ToString()).ToArray();
                animations = valueMap.Values.ToArray();
            }
            else
            {
                enums = new string[0];
                animations = new AnimationClip[0];
            }
        }

        public void OnAfterDeserialize()
        {
            enumType = ParseType(serializedType);

            if (enumType == null)
                return;
            
            valueMap = Enum.GetValues(enumType).Cast<Enum>().ToDictionary(x => x, x => (AnimationClip)null);

            if (animations == null || enums == null)
                return;

            for (int i = 0; i < animations.Length && i < enums.Length; i++)
            {
                try
                {
                    Enum e = (Enum) Enum.Parse(enumType, enums[i]);
                    valueMap[e] = animations[i];
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        public static Type ParseType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return null;
            
            Type enumType = Type.GetType(type);
            if (enumType == null)
                return null;
            if (!enumType.IsEnum)
                throw new ArgumentException($"Specified type {type} is not enum");

            return enumType;
        }

        [AttributeUsage(AttributeTargets.Enum)]
        public sealed class EnumAttribute : Attribute
        {
            public readonly string name;

            public EnumAttribute(string name = "")
            {
                this.name = name;
            }
        }

        public sealed class ValidateAttribute : PropertyAttribute
        {
            public readonly Type type;

            public ValidateAttribute(Type type)
            {
                this.type = type;
            }
        }
    }
}
