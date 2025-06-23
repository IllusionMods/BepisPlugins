using System;

namespace Screencap
{
    /// <summary>
    /// A readonly struct that encapsulates screen resolution dimensions.
    /// </summary>
    public readonly struct SavedResolution : IEquatable<SavedResolution>
    {
        /// <summary>
        /// Screen width.
        /// </summary>
        public readonly int Width;

        /// <summary>
        /// Screen height.
        /// </summary>
        public readonly int Height;

        /// <summary>
        /// Initializes a new instance of the <see cref="SavedResolution"/> struct with specified width and height.
        /// </summary>
        /// <param name="resolutionXValue">Screen width.</param>
        /// <param name="resolutionYValue">Screen height.</param>
        public SavedResolution(int resolutionXValue, int resolutionYValue)
        {
            Width = resolutionXValue;
            Height = resolutionYValue;
        }

        /// <summary>
        /// Determines whether the specified <see cref="SavedResolution"/> is equal to the current instance.
        /// </summary>
        /// <param name="other">The other <see cref="SavedResolution"/> to compare.</param>
        /// <returns><c>true</c> if the resolutions are equal; otherwise, <c>false</c>.</returns>
        public bool Equals(SavedResolution other)
        {
            return Width == other.Width && Height == other.Height;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current instance.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns><c>true</c> if the object is a <see cref="SavedResolution"/> and is equal; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return obj is SavedResolution other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code for the current instance.
        /// </summary>
        /// <returns>A hash code for the current instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Width * 397) ^ Height;
            }
        }
    }
}
