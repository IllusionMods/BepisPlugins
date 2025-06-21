using System.ComponentModel;

namespace Screencap
{
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
