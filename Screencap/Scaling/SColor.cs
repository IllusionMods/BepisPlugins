using System;

namespace Scaling
{
	public struct SColor
	{
		public float r;
		public float g;
		public float b;
		public float a;
		
		public SColor(float _r, float _g, float _b, float _a)
		{
			r = _r;
			g = _g;
			b = _b;
			a = _a;
		}
		
		public void Add(SColor _c)
		{
			r += _c.r;
			g += _c.g;
			b += _c.b;
			a += _c.a;
		}
		public void Divide(float _divisor)
		{
			r = r / _divisor;
			g = g / _divisor;
			b = b / _divisor;
			a = a / _divisor;
			
		}
		public override bool Equals (object obj)
		{
			if(obj is SColor)
			{
				return ((SColor)obj).r == r &&
				((SColor)obj).r == g &&
				((SColor)obj).r == b &&
				((SColor)obj).r == a;
			}
			return base.Equals (obj);
		}
		public override int GetHashCode ()
		{
			int o = (int)(a * 255);
			o += (int)(g * 255) << 8;
			o += (int)(b * 255) << 16;
			o += (int)(r * 255) << 24;
			
			return o;
		}
	}
}

