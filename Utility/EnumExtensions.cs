﻿using System.ComponentModel;

namespace Roll20AggregatorHosted.Utility {
    public static class EnumExtensions {
        public static string GetDescription(this Enum enumValue) {
            var field = enumValue.GetType().GetField(enumValue.ToString());

            if (field == null)
                return enumValue.ToString();

            if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute) {
                return attribute.Description;
            }

            return enumValue.ToString();
        }
    }
}
