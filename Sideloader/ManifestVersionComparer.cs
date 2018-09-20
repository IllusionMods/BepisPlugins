using System;
using System.Collections.Generic;
using System.Linq;

namespace Sideloader
{
    public class ManifestVersionComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            var version = new { First = GetVersion(x), Second = GetVersion(y) };
            var limit = Math.Max(version.First.Length, version.Second.Length);
            for (var i = 0; i < limit; i++)
            {
                var first = version.First.ElementAtOrDefault(i) ?? string.Empty;
                var second = version.Second.ElementAtOrDefault(i) ?? string.Empty;
                var result = first.CompareTo(second);
                if (result != 0)
                    return result;
            }
            return version.First.Length.CompareTo(version.Second.Length);
        }

        private IComparable[] GetVersion(string version)
        {
            return (from part in version.Trim().Split('.', ' ', '-', ',', '_')
                select Parse(part)).ToArray();
        }

        private IComparable Parse(string version)
        {
            if (int.TryParse(version, out var result))
                return result;
            return version;
        }
    }
}