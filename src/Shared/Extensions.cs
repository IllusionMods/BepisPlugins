#if !RG
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
#else
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
#endif

// TODO cleanup
#if RG
internal static class NullCheck
{
    public static bool IsNullOrEmpty<T>(this IEnumerable<T> self) =>
        self is null || !self.Any();

#if false // unused
    public static bool IsNullOrEmpty<TKey, TValue>(this Il2CppSystem.Collections.Generic.Dictionary<TKey, TValue> self) =>
        self is null || self.Count <= 0;
#endif
}
#endif
internal static class Extensions
{
    /// <summary>
    /// Returns a new string in which all occurrences of a specified string in the current instance are replaced with another 
    /// specified string according the type of search to use for the specified string.
    /// Stolen from https://stackoverflow.com/a/45756981
    /// </summary>
    /// <param name="str">The string performing the replace method.</param>
    /// <param name="oldValue">The string to be replaced.</param>
    /// <param name="newValue">The string replace all occurrences of <paramref name="oldValue"/>. 
    /// If value is equal to <c>null</c>, than all occurrences of <paramref name="oldValue"/> will be removed from the <paramref name="str"/>.</param>
    /// <param name="comparisonType">One of the enumeration values that specifies the rules for the search.</param>
    /// <returns>A string that is equivalent to the current string except that all instances of <paramref name="oldValue"/> are replaced with <paramref name="newValue"/>. 
    /// If <paramref name="oldValue"/> is not found in the current instance, the method returns the current instance unchanged.</returns>
    internal static string Replace(this string str, string oldValue, string newValue, StringComparison comparisonType)
    {
        // Check inputs.
        if (str == null)
            // Same as original .NET C# string.Replace behavior.
            throw new ArgumentNullException(nameof(str));
        if (str.Length == 0)
            // Same as original .NET C# string.Replace behavior.
            return str;
        if (oldValue == null)
            // Same as original .NET C# string.Replace behavior.
            throw new ArgumentNullException(nameof(oldValue));
        if (oldValue.Length == 0)
            // Same as original .NET C# string.Replace behavior.
            throw new ArgumentException("String cannot be of zero length.");

        // Prepare string builder for storing the processed string.
        // Note: StringBuilder has a better performance than String by 30-40%.
        StringBuilder resultStringBuilder = new StringBuilder(str.Length);

        // Analyze the replacement: replace or remove.
        bool isReplacementNullOrEmpty = string.IsNullOrEmpty(newValue);

        // Replace all values.
        const int valueNotFound = -1;
        int foundAt;
        int startSearchFromIndex = 0;
        while ((foundAt = str.IndexOf(oldValue, startSearchFromIndex, comparisonType)) != valueNotFound)
        {

            // Append all characters until the found replacement.
            int charsUntilReplacment = foundAt - startSearchFromIndex;
            bool isNothingToAppend = charsUntilReplacment == 0;
            if (!isNothingToAppend)
                resultStringBuilder.Append(str, startSearchFromIndex, charsUntilReplacment);

            // Process the replacement.
            if (!isReplacementNullOrEmpty)
                resultStringBuilder.Append(newValue);

            // Prepare start index for the next search.
            // This needed to prevent infinite loop, otherwise method always start search 
            // from the start of the string. For example: if an oldValue == "EXAMPLE", newValue == "example"
            // and comparisonType == "any ignore case" will conquer to replacing:
            // "EXAMPLE" to "example" to "example" to "example" … infinite loop.
            startSearchFromIndex = foundAt + oldValue.Length;
            if (startSearchFromIndex == str.Length)
                // It is end of the input string: no more space for the next search.
                // The input string ends with a value that has already been replaced. 
                // Therefore, the string builder with the result is complete and no further action is required.
                return resultStringBuilder.ToString();
        }

        // Append the last part to the result.
        int charsUntilStringEnd = str.Length - startSearchFromIndex;
        resultStringBuilder.Append(str, startSearchFromIndex, charsUntilStringEnd);

        return resultStringBuilder.ToString();
    }

#if !RG
    /// <summary>
    /// Find first position of the byte sequence in the stream starting at current position.
    /// Returns position of first byte of the sequence.
    /// https://stackoverflow.com/questions/1550560/encoding-an-integer-in-7-bit-format-of-c-sharp-binaryreader-readstring
    /// </summary>
    internal static long FindPosition(this Stream stream, byte[] byteSequence)
    {
        int PadLeftSequence(byte[] bytes, byte[] seqBytes)
        {
            int i = 1;
            while (i < bytes.Length)
            {
                int n = bytes.Length - i;
                byte[] aux1 = new byte[n];
                byte[] aux2 = new byte[n];
                Array.Copy(bytes, i, aux1, 0, n);
                Array.Copy(seqBytes, aux2, n);
                if (aux1.SequenceEqual(aux2))
                    return i;
                i++;
            }
            return i;
        }

        if (byteSequence.Length > stream.Length)
            return -1;

        byte[] buffer = new byte[byteSequence.Length];

        // Do not dispose this stream or we'll dispose the base stream too
        var bufStream = new BufferedStream(stream, byteSequence.Length);

        while (bufStream.Read(buffer, 0, byteSequence.Length) == byteSequence.Length)
        {
            if (byteSequence.SequenceEqual(buffer))
                return bufStream.Position - byteSequence.Length;

            bufStream.Position -= byteSequence.Length - PadLeftSequence(buffer, byteSequence);
        }

        return -1;
    }
#else
    /// <summary>
    /// Find first position of the byte sequence in the stream starting at current position.
    /// Returns position of first byte of the sequence.
    /// </summary>
    internal static long FindPosition(this Il2CppSystem.IO.Stream stream, byte[] byteSequence)
    {
        if (stream.CanRead && stream.CanSeek && !byteSequence.IsNullOrEmpty())
        {
            var position = stream.Position;
            var length = stream.Length - position;

            if (byteSequence.Length <= length)
            {
                var buffer = new byte[length];
                stream.Read(buffer, 0, (int)length);
                stream.Position = position;

                var last = buffer.Length - byteSequence.Length;
                for (var i = 0; i <= last; i++)
                {
                    if (buffer[i] == byteSequence[0])
                    {
                        var j = 0;
                        do
                        {
                            if (++j == byteSequence.Length)
                            {
                                return position + i;
                            }
                        } while (buffer[i + j] == byteSequence[j]);
                    }
                }
            }
        }
        return -1;
    }
#endif
}

#if RG
internal static partial class Il2CppListExtensions
{
    public static Il2CppSystem.Collections.Generic.List<T> ToIl2CppList<T>(this IEnumerable<T> enumerable)
    {
        Il2CppSystem.Collections.Generic.List<T> il2CppList = null;
        if (enumerable is not null)
        {
            il2CppList = new((enumerable as ICollection<T>)?.Count ?? 0);
            foreach (T item in enumerable)
                il2CppList.Add(item);
        }
        return il2CppList;
    }

    public static List<T> ToManagedList<T>(this Il2CppSystem.Collections.Generic.List<T> il2CppList)
    {
        List<T> list = null;
        if (il2CppList is not null)
        {
            list = new(il2CppList.Count);
            foreach (T item in il2CppList)
                list.Add(item);
        }
        return list;
    }

#if false   // unused
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Il2CppSystem.Collections.Generic.IEnumerable<T> AsIl2CppEnumerable<T>(this Il2CppSystem.Collections.Generic.List<T> il2CppList) =>
        il2CppList is not null ? new Il2CppListEnumerable<T>(il2CppList) : null;

    private class Il2CppListEnumerable<T> : Il2CppSystem.Collections.Generic.IEnumerable<T>
    {
        private Il2CppSystem.Collections.Generic.List<T> il2CppList;

        public Il2CppListEnumerable(Il2CppSystem.Collections.Generic.List<T> il2CppList) : base(il2CppList.Pointer) =>
            this.il2CppList = il2CppList;

        public override Il2CppSystem.Collections.Generic.IEnumerator<T> GetEnumerator() =>
            il2CppList.System_Collections_Generic_IEnumerable_T__GetEnumerator();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> AsEnumerable<T>(this Il2CppSystem.Collections.Generic.List<T> il2CppList) =>
        il2CppList is not null ? Il2CppListIterator(il2CppList) : null;

    private static IEnumerable<T> Il2CppListIterator<T>(Il2CppSystem.Collections.Generic.List<T> il2CppList)
    {
        foreach (T item in il2CppList)
            yield return item;
    }
#endif
}

#if false   // unused
internal static class Il2CppDictionaryExtensions
{
    public static bool TryGetValue_<TKey, TValue>(this Il2CppSystem.Collections.Generic.Dictionary<TKey, TValue> il2CppDictionary, TKey key, out TValue value)
    {
        var result = il2CppDictionary.ContainsKey(key);
        value = result ? il2CppDictionary[key] : default;
        return result;
    }
}
#endif

//internal static partial class Il2CppExceptionExtensions
//{
//    public static Type GetTypeFromMessage(this UnhollowerBaseLib.Il2CppException exception)
//    {
//        const string ignorePrefix = "UnhollowerBaseLib.Il2CppException: ";
//        const int ignorePrefixLength = 35;
//
//        var s = exception.Message;
//        while (s.StartsWith(ignorePrefix))
//            s = s.Substring(ignorePrefixLength);
//        var index = s.IndexOf(':');
//        return index != -1 ? Type.GetType(s.Remove(index)) : null;
//    }
//}

#if false   // unused
internal static partial class MonoBehaviourExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Coroutine StartCoroutine(this MonoBehaviour monoBehaviour, IEnumerator routine) =>
        monoBehaviour.StartCoroutine(routine.WrapToIl2Cpp());
}
#endif
#endif
