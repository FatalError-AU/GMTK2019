// Inspector Gadgets // Copyright 2019 Kybernetik //

namespace InspectorGadgets.Attributes
{
    /// <summary>[Pro-Only]
    /// Specifies the maximum value allowed by the attributed int or float field.
    /// See also: <see cref="MinValueAttribute"/> and <see cref="ClampValueAttribute"/>.
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public sealed class MaxValueAttribute : ValidatorAttribute
    {
        /************************************************************************************************************************/

        /// <summary>The maximum allowed integer value.</summary>
        public readonly long MaxLong;

        /// <summary>The maximum allowed floating point value.</summary>
        public readonly double MaxDouble;

        /************************************************************************************************************************/

        /// <summary>
        /// Constructs a new <see cref="MaxValueAttribute"/> with the specified maximum value.
        /// </summary>
        public MaxValueAttribute(int max) : this((long)max) { }

        /// <summary>
        /// Constructs a new <see cref="MaxValueAttribute"/> with the specified maximum value.
        /// </summary>
        public MaxValueAttribute(long max)
        {
            MaxLong = max;
            MaxDouble = max;
        }

        /// <summary>
        /// Constructs a new <see cref="MaxValueAttribute"/> with the specified maximum value.
        /// </summary>
        public MaxValueAttribute(float max) : this((double)max) { }

        /// <summary>
        /// Constructs a new <see cref="MaxValueAttribute"/> with the specified maximum value.
        /// </summary>
        public MaxValueAttribute(double max)
        {
            MaxLong = (long)max;
            MaxDouble = max;
        }

        /************************************************************************************************************************/
#if UNITY_EDITOR
        /************************************************************************************************************************/

        /// <summary>
        /// Validate the value of the specified 'property'.
        /// </summary>
        public override void Validate(UnityEditor.SerializedProperty property)
        {
            if (property.propertyType == UnityEditor.SerializedPropertyType.Integer)
            {
                if (property.longValue > MaxLong)
                    property.longValue = (int)MaxLong;
            }
            else if (property.propertyType == UnityEditor.SerializedPropertyType.Float)
            {
                if (property.doubleValue > MaxDouble)
                    property.doubleValue = MaxDouble;
            }
        }

        /************************************************************************************************************************/
#endif
    }
}

