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
                case AlphaMode.Default:
                    return "Yes";
#if !(HS2 || AI)
                case AlphaMode.blackout:
                    return "Cutout";
                case AlphaMode.rgAlpha:
                    return "Gradual";
#endif
            };
        }
    }

    public enum AlphaMode
    {
        [Description("No transparency")]
        None = 0,
        [Description("Transparency (default method)")]
        Default = 1,
#if !(HS2 || AI)
        [Description("Cutout transparency (hard edges)")]
        blackout = 2,
        [Description("Gradual transparency (has issues with some effects)")]
        rgAlpha = 3,
#endif
    }
}
