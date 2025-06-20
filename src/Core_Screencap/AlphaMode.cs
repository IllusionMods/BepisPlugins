using System.ComponentModel;

namespace Screencap
{
    public enum AlphaMode
    {
        [Description("No transparency")]
        None = 0,
        [Description("Cutout transparency (hard edges)")]
        blackout = 1,
        [Description("Gradual transparency (has issues with some effects)")]
        rgAlpha = 2,
        [Description("Transparency (default method)")]
        Default = 2,
    }
}
