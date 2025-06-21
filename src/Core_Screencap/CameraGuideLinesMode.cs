using System;
using System.ComponentModel;

namespace Screencap
{
    /// <summary>
    /// Available modes for camera guide lines.
    /// Can be combined using flags.
    /// </summary>
    [Flags]
    public enum CameraGuideLinesMode
    {
        [Description("No guide lines")]
        None = 0,
        [Description("Cropped area")]
        Framing = 1 << 0,
        [Description("Rule of thirds")]
        GridThirds = 1 << 1,
        [Description("Golden ratio")]
        GridPhi = 1 << 2,
        [Description("Grid border")]
        Border = 1 << 3,
        [Description("X lines")]
        CrossOut = 1 << 4,
        [Description("Side V lines")]
        SideV = 1 << 5,
        [Description("Center lines")]
        CenterLines = 1 << 6
    }
}
