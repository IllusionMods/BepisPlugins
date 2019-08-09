using System;

namespace BepInEx
{
    public class AdvancedAttribute : Attribute
    {
        public AdvancedAttribute(bool isAdvanced) => IsAdvanced = isAdvanced;

        public bool IsAdvanced { get; }
    }
}