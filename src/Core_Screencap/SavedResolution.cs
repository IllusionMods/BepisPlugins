using System;

namespace Screencap
{
    public readonly struct SavedResolution : IEquatable<SavedResolution>
    {
        public readonly int Width;
        public readonly int Height;

        public SavedResolution(int resolutionXValue, int resolutionYValue)
        {
            Width = resolutionXValue;
            Height = resolutionYValue;
        }

        public bool Equals(SavedResolution other)
        {
            return Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            return obj is SavedResolution other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Width * 397) ^ Height;
            }
        }
    }
}
