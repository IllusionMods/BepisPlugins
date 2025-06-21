using System;
using System.ComponentModel;

namespace Screencap
{
    public static class AlphaModeExtensions
    {
        public static string GetDisplayName(this AlphaMode mode)
        {
            switch (mode)
            {
                default:
                case AlphaMode.None:
                    return "No";
#if AI || HS2
                case AlphaMode.Default:
                    return "Yes";
#else
                case AlphaMode.blackout:
                    return "Cutout";
                case AlphaMode.rgAlpha:
                    return "Gradual";
#endif
            };
        }

        public static AlphaMode GetDefault()
        {
#if AI || HS2
            return AlphaMode.Default;
#else
            return AlphaMode.blackout;
#endif
        }
    }

    public enum AlphaMode
    {
        [Description("No transparency")]
        None = 0,
#if AI || HS2
        [Description("Transparency (default method)")]
        Default = 1,
#else
        [Description("Cutout transparency (hard edges)")]
        blackout = 1,
        [Description("Gradual transparency (has issues with some effects)")]
        rgAlpha = 2,
#endif
    }
}
