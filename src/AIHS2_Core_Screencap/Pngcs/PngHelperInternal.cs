using Pngcs.Zlib;
using System;
using System.IO;
using System.Text;

namespace Pngcs
{
    /// <summary>
    /// Some utility static methods for internal use.
    /// </summary>
    internal class PngHelperInternal
    {
        [ThreadStatic]
        private static CRC32 crc32Engine = null;

        /// <summary>
        /// thread-singleton crc engine 
        /// </summary>
        ///
        public static CRC32 GetCRC()
        {
            if (crc32Engine == null) crc32Engine = new CRC32();
            return crc32Engine;
        }

        public static readonly byte[] PNG_ID_SIGNATURE = { 256 - 119, 80, 78, 71, 13, 10, 26, 10 }; // png magic

        public static Encoding charsetLatin1 = Encoding.GetEncoding("ISO-8859-1"); // charset
        public static Encoding charsetUtf8 = Encoding.GetEncoding("UTF-8"); // charset used for some chunks

        public static bool DEBUG = false;

        public static int DoubleToInt100000(double d)
        {
            return (int)(d * 100000.0 + 0.5);
        }

        public static double IntToDouble100000(int i)
        {
            return i / 100000.0;
        }

        public static void WriteInt2(Stream os, int n)
        {
            byte[] temp = { (byte)((n >> 8) & 0xff), (byte)(n & 0xff) };
            WriteBytes(os, temp);
        }

        public static void WriteInt2tobytes(int n, byte[] b, int offset)
        {
            b[offset] = (byte)((n >> 8) & 0xff);
            b[offset + 1] = (byte)(n & 0xff);
        }

        public static void WriteInt4tobytes(int n, byte[] b, int offset)
        {
            b[offset] = (byte)((n >> 24) & 0xff);
            b[offset + 1] = (byte)((n >> 16) & 0xff);
            b[offset + 2] = (byte)((n >> 8) & 0xff);
            b[offset + 3] = (byte)(n & 0xff);
        }

        public static void WriteInt4(Stream os, int n)
        {
            byte[] temp = new byte[4];
            WriteInt4tobytes(n, temp, 0);
            WriteBytes(os, temp);
            //Console.WriteLine("writing int " + n + " b=" + (sbyte)temp[0] + "," + (sbyte)temp[1] + "," + (sbyte)temp[2] + "," + (sbyte)temp[3]);
        }

        public static void WriteBytes(Stream os, byte[] b)
        {
            try
            {
                os.Write(b, 0, b.Length);
            }
            catch (IOException e)
            {
                throw new PngjOutputException(e);
            }
        }

        public static void WriteBytes(Stream os, byte[] b, int offset, int n)
        {
            try
            {
                os.Write(b, offset, n);
            }
            catch (IOException e)
            {
                throw new PngjOutputException(e);
            }
        }

        public static int ReadByte(Stream mask0)
        {
            try
            {
                return mask0.ReadByte();
            }
            catch (IOException e)
            {
                throw new PngjOutputException(e);
            }
        }

        public static void WriteByte(Stream os, byte b)
        {
            try
            {
                os.WriteByte(b);
            }
            catch (IOException e)
            {
                throw new PngjOutputException(e);
            }
        }

        public static int UnfilterRowPaeth(int r, int a, int b, int c)
        { // a = left, b = above, c = upper left
            return (r + FilterPaethPredictor(a, b, c)) & 0xFF;
        }

        public static int FilterPaethPredictor(int a, int b, int c)
        {
            // from http://www.libpng.org/pub/png/spec/1.2/PNG-Filters.html
            // a = left, b = above, c = upper left
            int p = a + b - c;// ; initial estimate
            int pa = p >= a ? p - a : a - p;
            int pb = p >= b ? p - b : b - p;
            int pc = p >= c ? p - c : c - p;
            // ; return nearest of a,b,c,
            // ; breaking ties in order a,b,c.
            if (pa <= pb && pa <= pc)
                return a;
            else if (pb <= pc)
                return b;
            else
                return c;
        }

        public static void Logdebug(string msg)
        {
            if (DEBUG)
                Console.Out.WriteLine(msg);
        }

        internal static void Log(string p)
        {
            Console.Error.WriteLine(p);
        }
    }
}
