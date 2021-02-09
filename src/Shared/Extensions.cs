using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;

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

    /// <summary>
    /// Apply a function to a collection of data by spreading the work on multiple threads.
    /// Outputs of the functions are returned to the current thread and yielded one by one.
    /// todo: Use the version built into bepinex whenever it's updated
    /// </summary>
    /// <typeparam name="TIn">Type of the input values.</typeparam>
    /// <typeparam name="TOut">Type of the output values.</typeparam>
    /// <param name="data">Input values for the work function.</param>
    /// <param name="work">Function to apply to the data on multiple threads at once.</param>
    /// <param name="workerCount">Number of worker threads. By default SystemInfo.processorCount is used.</param>
    /// <exception cref="TargetInvocationException">An exception was thrown inside one of the threads, and the operation was aborted.</exception>
    /// <exception cref="ArgumentException">Need at least 1 workerCount.</exception>
    internal static IEnumerable<TOut> RunParallel<TIn, TOut>(this IList<TIn> data, Func<TIn, TOut> work, int workerCount = -1)
    {
        if (workerCount < 0)
            workerCount = Mathf.Max(2, SystemInfo.processorCount);
        else if (workerCount == 0)
            throw new ArgumentException("Need at least 1 worker", nameof(workerCount));

        var perThreadCount = Mathf.CeilToInt(data.Count / (float)workerCount);
        var doneCount = 0;

        var lockObj = new object();
        var are = new ManualResetEvent(false);
        IEnumerable<TOut> doneItems = null;
        Exception exceptionThrown = null;

        // Start threads to process the data
        for (var i = 0; i < workerCount; i++)
        {
            int first = i * perThreadCount;
            int last = Mathf.Min(first + perThreadCount, data.Count);
            ThreadingHelper.Instance.StartAsyncInvoke(
                () =>
                {
                    var results = new List<TOut>(perThreadCount);

                    try
                    {
                        for (int dataIndex = first; dataIndex < last; dataIndex++)
                        {
                            if (exceptionThrown != null) break;
                            results.Add(work(data[dataIndex]));
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptionThrown = ex;
                    }

                    lock (lockObj)
                    {
                        doneItems = doneItems == null ? results : results.Concat(doneItems);
                        doneCount++;
                        are.Set();
                    }

                    return null;
                });
        }

        // Main thread waits for results and returns them until all threads finish
        while (true)
        {
            are.WaitOne();

            IEnumerable<TOut> toOutput;
            bool isDone;
            lock (lockObj)
            {
                toOutput = doneItems;
                doneItems = null;
                isDone = doneCount == workerCount;
            }

            if (toOutput != null)
            {
                foreach (var doneItem in toOutput)
                    yield return doneItem;
            }

            if (isDone)
                break;
        }

        if (exceptionThrown != null)
            throw new TargetInvocationException("An exception was thrown inside one of the threads", exceptionThrown);
    }
}
