using System;
using System.ComponentModel;
using System.Linq;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Screencap
{
    /// <summary>  
    /// Different types of transparency used in screenshots.
    /// </summary>  
    public enum AlphaMode
    {
        [Description("No transparency")]
        None = 0,
#if HS2 || AI
        [Description("Composite")]
        composite = 1
#else
        [Description("Cutout transparency (hard edges)")]
        blackout = 1,
        [Description("Gradual transparency (has issues with some effects)")]
        rgAlpha = 2,
#endif
    }

    internal static class AlphaModeUtils
    {
        private static string GetDisplayName(this AlphaMode mode)
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

        public static readonly AlphaMode Default =
#if HS2 || AI
            AlphaMode.composite;
#else
            AlphaMode.rgAlpha;
#endif

        public static readonly string[] AllModes = Enum.GetValues(typeof(AlphaMode)).Cast<AlphaMode>().OrderBy(x => (int)x).Select(x => x.GetDisplayName()).ToArray();
    }
}
