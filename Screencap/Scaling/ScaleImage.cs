using System;
using System.Collections.Generic;
namespace Scaling
{
	public class ScaleImage
	{
		SColor[] m_originalColors;
		int m_width;
		int m_height;
		
		float m_scaleX;
		float m_scaleY;
		
		int m_scaleSizeX;
		int m_scaleSizeY;
		
		SColor[] m_outArray;
		
		SColor[] m_buffer;
		
		public ScaleImage(SColor[] _colors, int _width)
		{
			m_originalColors = _colors;
			
			m_width = _width;
			m_height = m_originalColors.Length / _width;
		}
	#region linear
		
		public SColor[] ScaleLinear( int _targetWidth, int _targetHeight)
		{
			m_scaleX = m_width / (float) _targetWidth;
			m_scaleY = m_height / (float) _targetHeight;
			
			// 500 / 250 = 2
			
			m_scaleSizeX = (int)System.Math.Round(m_scaleX / 2.0f);
			m_scaleSizeY = (int)System.Math.Round(m_scaleY / 2.0f);
			
			
			m_outArray = new SColor[_targetWidth * _targetHeight];
			
			
			for(int y = 0;y <_targetHeight;y++)
			{
				for(int x = 0;x < _targetWidth;x++)
				{
					m_buffer = GetPixelsForTargetPixel(x, y);
					SColor result = MergePixels();
					m_outArray [ y * _targetWidth + x ] = result;
				}	
			}
			return m_outArray;
		}
		SColor MergePixels()
		{
			SColor outColor = new SColor(0,0,0,0);
			for(int i = 0;i<m_buffer.Length;i++)
			{
				outColor.Add(m_buffer[i]);
			}
			outColor.Divide(m_buffer.Length);
			return outColor;
		}
		SColor[] GetPixelsForTargetPixel(int _x, int _y)
		{
			Queue<SColor> outArray = new Queue<SColor>(m_scaleSizeX * m_scaleSizeY);
			int idx =0;
			for(int y = -m_scaleSizeY;y < (m_scaleSizeY +1);y++)
			{
				for(int x = -m_scaleSizeX;x < (m_scaleSizeX +1);x++)
				{
					int pX = (int)System.Math.Round( _x * m_scaleX + x);
					int pY = (int)System.Math.Round( _y * m_scaleY + y);
					//image bounds check
					if(pX >= 0 && pY >= 0)
					{
						if(pX < m_width && pY < m_height)
						{
							idx = pX + pY * m_width;
							outArray.Enqueue( m_originalColors[idx] );
						}
					}
				}
			}
			return outArray.ToArray();		
		}
	#endregion
	#region lanczos
		public SColor[] ScaleLanczos( int _targetWidth, int _targetHeight)
		{
			Lanczos l = new Lanczos(m_originalColors, m_width);
			return l.Filter(_targetWidth, _targetHeight);
		}
	#endregion
	#region point 
		public SColor[] ScalePoint( int _targetWidth, int _targetHeight)
		{	
			m_scaleX =  (float)m_width / (float)_targetWidth ;
			m_scaleY =  (float)m_height / (float)_targetHeight ;
			
			// 500 / 250 = 2
			
			
			m_outArray = new SColor[_targetWidth * _targetHeight];
			
			for(int y = 0;y < _targetHeight;y++)
			{
				for(int x = 0;x < _targetWidth;x++)
				{
					
					int tIdx = GetOrgIndex((int)(x * m_scaleX),(int)(y * m_scaleY),_targetWidth);
					m_outArray[x + y * _targetWidth] = m_originalColors[ tIdx ];
					
				}
			}
			return m_outArray;
		}
		int GetOrgIndex(int _x, int _y, float _targetWidth)
		{
			return _x + _y * m_width;
		}
	#endregion
	}
}