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
                    return "Default";
#if HS2 || AI
                case AlphaMode.composite:
                    return "Composite";
#else
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
#if HS2 || AI
        [Description("Transparency (default method)")]
        Default = 1,
        [Description("Composite")]
        composite = 1
#else
        [Description("Transparency (default method)")]
        Default = 2,
        [Description("Cutout transparency (hard edges)")]
        blackout = 1,
        [Description("Gradual transparency (has issues with some effects)")]
        rgAlpha = 2,
#endif
    }
}
