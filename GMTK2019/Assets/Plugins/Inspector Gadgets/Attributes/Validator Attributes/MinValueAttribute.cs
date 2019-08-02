// Inspector Gadgets // Copyright 2019 Kybernetik //

namespace InspectorGadgets.Attributes
{
    /// <summary>[Pro-Only]
    /// Specifies the minimum value allowed by the attributed int or float field.
    /// See also: <see cref="MaxValueAttribute"/> and <see cref="ClampValueAttribute"/>.
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public sealed class MinValueAttribute : ValidatorAttribute
    {
        /************************************************************************************************************************/

        /// <summary>The minimum allowed integer value.</summary>
        public readonly long MinLong;

        /// <summary>The minimum allowed floating point value.</summary>
        public readonly double MinDouble;

        /************************************************************************************************************************/

        /// <summary>
        /// Constructs a new <see cref="MinValueAttribute"/> with the specified minimum value.
        /// </summary>
        public MinValueAttribute(int min) : this((long)min) { }

        /// <summary>
        /// Constructs a new <see cref="MinValueAttribute"/> with the specified minimum value.
        /// </summary>
        public MinValueAttribute(long min)
        {
            MinLong = min;
            MinDouble = min;
        }

        /// <summary>
        /// Constructs a new <see cref="MinValueAttribute"/> with the specified minimum value.
        /// </summary>
        public MinValueAttribute(float min) : this((double)min) { }

        /// <summary>
        /// Constructs a new <see cref="MinValueAttribute"/> with the specified minimum value.
        /// </summary>
        public MinValueAttribute(double min)
        {
            MinLong = (long)min;
            MinDouble = min;
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
                if (property.longValue < MinLong)
                    property.longValue = (int)MinLong;
            }
            else if (property.propertyType == UnityEditor.SerializedPropertyType.Float)
            {
                if (property.doubleValue < MinDouble)
                    property.doubleValue = MinDouble;
            }
        }

        /************************************************************************************************************************/
#endif
    }
}

