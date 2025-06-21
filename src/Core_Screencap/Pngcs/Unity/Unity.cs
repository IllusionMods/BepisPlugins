using System.Collections;
using System.Threading;
using BepInEx;
using UnityEngine;

namespace Pngcs.Unity
{
    internal static class PNG
    {
        public static IEnumerator WriteAsync(Color[] pixels, int width, int height, int bitDepth, bool alpha, bool greyscale, string filePath)
        {
            Thread t;
            try
            {
                t = new Thread(() => Write(pixels, width, height, bitDepth, alpha, greyscale, filePath));
                t.Start();
            }
            catch (System.Exception ex)
            {
                Screencap.ScreenshotManager.Logger.LogError(ex);
                yield break;
            }

            while (t.IsAlive)
                yield return null; // wait for the thread to finish
        }
        public static IEnumerator WriteAsync(Color32[] pixels, int width, int height, int bitDepth, bool alpha, bool greyscale, string filePath)
        {
            Thread t;
            try
            {
                t = new Thread(() => Write(pixels, width, height, bitDepth, alpha, greyscale, filePath));
                t.Start();
            }
            catch (System.Exception ex)
            {
                Screencap.ScreenshotManager.Logger.LogError(ex);
                yield break;
            }

            while (t.IsAlive)
                yield return null; // wait for the thread to finish
        }

        public static void Write
        (
            Color[] pixels,
            int width,
            int height,
            int bitDepth,
            bool alpha,
            bool greyscale,
            string filePath
        )
        {
            var info = new ImageInfo(
                width,
                height,
                bitDepth,
                alpha,
                greyscale,
                false//not implemented here yet//bitDepth==4
            );

            // open image for writing:
            PngWriter writer = FileHelper.CreatePngWriter(filePath, info, true);
            // add some optional metadata (chunks)
            writer.GetMetadata();

            int numRows = info.Rows;
            int numCols = info.Cols;
            ImageLine imageline = new ImageLine(info);
            for (int row = 0; row < numRows; row++)
            {
                //fill line:
                if (greyscale == false)
                {
                    if (alpha)
                    {
                        for (int col = 0; col < numCols; col++)
                        {
                            RGBA rgba = ToRGBA(pixels[IndexPngToTexture(row, col, numRows, numCols)], bitDepth);
                            ImageLineHelper.SetPixel(imageline, col, rgba.r, rgba.g, rgba.b, rgba.a);
                        }
                    }
                    else
                    {
                        for (int col = 0; col < numCols; col++)
                        {
                            RGB rgb = ToRGB(pixels[IndexPngToTexture(row, col, numRows, numCols)], bitDepth);
                            ImageLineHelper.SetPixel(imageline, col, rgb.r, rgb.g, rgb.b);
                        }
                    }
                }
                else
                {
                    if (alpha == false)
                    {
                        for (int col = 0; col < numCols; col++)
                        {
                            int r = ToInt(pixels[IndexPngToTexture(row, col, numRows, numCols)].r, bitDepth);
                            ImageLineHelper.SetPixel(imageline, col, r);
                        }
                    }
                    else
                    {
                        for (int col = 0; col < numCols; col++)
                        {
                            int a = ToInt(pixels[IndexPngToTexture(row, col, numRows, numCols)].a, bitDepth);
                            ImageLineHelper.SetPixel(imageline, col, a);
                        }
                    }
                }

                //write line:
                writer.WriteRow(imageline, row);
            }
            writer.End();
        }

        public static void Write
        (
            Color32[] pixels,
            int width,
            int height,
            int bitDepth,
            bool alpha,
            bool greyscale,
            string filePath
        )
        {
            var info = new ImageInfo(
                width,
                height,
                bitDepth,
                alpha,
                greyscale,
                false//not implemented here yet//bitDepth==4
            );

            // open image for writing:
            PngWriter writer = FileHelper.CreatePngWriter(filePath, info, true);
            // add some optional metadata (chunks)
            writer.GetMetadata();

            int numRows = info.Rows;
            int numCols = info.Cols;
            ImageLine imageline = new ImageLine(info);
            for (int row = 0; row < numRows; row++)
            {
                //fill line:
                if (greyscale == false)
                {
                    if (alpha)
                    {
                        for (int col = 0; col < numCols; col++)
                        {
                            RGBA rgba = ToRGBA(pixels[IndexPngToTexture(row, col, numRows, numCols)], bitDepth);
                            ImageLineHelper.SetPixel(imageline, col, rgba.r, rgba.g, rgba.b, rgba.a);
                        }
                    }
                    else
                    {
                        for (int col = 0; col < numCols; col++)
                        {
                            RGB rgb = ToRGB(pixels[IndexPngToTexture(row, col, numRows, numCols)], bitDepth);
                            ImageLineHelper.SetPixel(imageline, col, rgb.r, rgb.g, rgb.b);
                        }
                    }
                }
                else
                {
                    if (alpha == false)
                    {
                        for (int col = 0; col < numCols; col++)
                        {
                            int r = ToInt(pixels[IndexPngToTexture(row, col, numRows, numCols)].r, bitDepth);
                            ImageLineHelper.SetPixel(imageline, col, r);
                        }
                    }
                    else
                    {
                        for (int col = 0; col < numCols; col++)
                        {
                            int a = ToInt(pixels[IndexPngToTexture(row, col, numRows, numCols)].a, bitDepth);
                            ImageLineHelper.SetPixel(imageline, col, a);
                        }
                    }
                }

                //write line:
                writer.WriteRow(imageline, row);
            }
            writer.End();
        }
        /// <summary> Texture2D's rows start from the bottom while PNG from the top. Hence inverted y/row. </summary>
        public static int IndexPngToTexture(int row, int col, int numRows, int numCols) => numCols * (numRows - 1 - row) + col;

        public static int ToInt(float color, int bitDepth)
        {
            float max = GetBitDepthMaxValue(bitDepth);
            return (int)(color * max);
        }

        public static RGB ToRGB(Color color, int bitDepth)
        {
            float max = GetBitDepthMaxValue(bitDepth);
            return new RGB
            {
                r = (int)(color.r * max),
                g = (int)(color.g * max),
                b = (int)(color.b * max)
            };
        }

        public static RGBA ToRGBA(Color color, int bitDepth)
        {
            float max = GetBitDepthMaxValue(bitDepth);
            return new RGBA
            {
                r = (int)(color.r * max),
                g = (int)(color.g * max),
                b = (int)(color.b * max),
                a = (int)(color.a * max)
            };
        }

        public static uint GetBitDepthMaxValue(int bitDepth)
        {
            switch (bitDepth)
            {
                case 1: return 1;
                case 2: return 3;
                case 4: return 15;
                case 8: return byte.MaxValue;
                case 16: return ushort.MaxValue;
                case 32: return uint.MaxValue;
                default: throw new System.Exception($"bitDepth '{bitDepth}' not implemented");
            }
        }

        public struct RGB { public int r, g, b; }

        public struct RGBA { public int r, g, b, a; }
    }
}
