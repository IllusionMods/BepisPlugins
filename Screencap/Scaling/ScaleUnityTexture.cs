using UnityEngine;

public class ScaleUnityTexture
{
	public static Color32[] ScaleLanczos(Color32[] _bytes, int _width, int _targetWidth , int _targetHeight)
	{
		Scaling.SColor[] colors = GetColors(_bytes);
		Scaling.ScaleImage l = new Scaling.ScaleImage(colors, _width);
		colors = l.ScaleLanczos(_targetWidth,_targetHeight);
		return GetColors(colors);
	}

    public static Color32[] GetColors( Scaling.SColor[] _colors)
	{
		Color32[] outArray = new Color32[_colors.Length];
		for(int i = 0;i< _colors.Length;i++)
		{
			outArray[i] = new Color32(
				(byte)(_colors[i].r * 255),
				(byte)(_colors[i].g * 255),
				(byte)(_colors[i].b * 255),
				(byte)(_colors[i].a * 255)
				);
		}
		return outArray;
	}
	
	public static Scaling.SColor[] GetColors (Color32[] _bytes)
	{
		Scaling.SColor[] outArray = new Scaling.SColor[_bytes.Length];
		for(int i = 0;i< _bytes.Length;i++)
		{
			outArray[i] = new Scaling.SColor(
				_bytes[i].r / 255.0f,
				_bytes[i].g / 255.0f,
				_bytes[i].b / 255.0f,
				_bytes[i].a / 255.0f
				);
		}
		return outArray;
	}
}

