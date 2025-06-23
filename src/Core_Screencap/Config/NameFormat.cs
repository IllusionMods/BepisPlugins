#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Screencap
{
    /// <summary>
    /// Screenshot file name formats.
    /// Name - Name of the application. Cane be overriden by a setting.
    /// Date - Current date and time.
    /// Type - Type of the screenshot taken.
    /// </summary>
    public enum NameFormat
    {
        NameDate,
        NameTypeDate,
        NameDateType,
        TypeDate,
        TypeNameDate,
        Date
    }
}