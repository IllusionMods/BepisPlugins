using System;
using System.ComponentModel;
using System.Linq;

namespace Screencap
{
    internal static class Extensions
    {
        public static string GetDisplayName(this AlphaMode mode)
        {
            switch (mode)
            {
                case AlphaMode.None:
                    return "No";
#if HS2 || AI
                case AlphaMode.composite:
                    return "Composite";
#else
                case AlphaMode.blackout:
                    return "Cutout";
                case AlphaMode.rgAlpha:
                    return "Gradual";
#endif
                default:
                    return null;
            };
        }

        public static readonly AlphaMode MaxValue = (AlphaMode)Enum.GetValues(typeof(AlphaMode)).Cast<int>().Max();

        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
        }
    }

    public enum AlphaMode
    {
        [Description("No transparency")]
        None = 0,
        [Description("Transparency (default method)")]
#if HS2 || AI
        Default = 1,
        [Description("Composite")]
        composite = 1
#else
        Default = 2,
        [Description("Cutout transparency (hard edges)")]
        blackout = 1,
        [Description("Gradual transparency (has issues with some effects)")]
        rgAlpha = 2,
#endif
    }
}
