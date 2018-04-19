using System;
namespace Scaling
{
	internal class Lanczos
	{
	 
		public class Kernel1D
		{
			public int size;
			public float normalizer; 
			public float[] coefs;
			
			public Kernel1D(int size,  float[] c, float n)
			{
				this.size=size;
                coefs = c;
                normalizer = n;
			}
		}
		
		private int minsupport = 0;
	 
		private SColor[] m_input;
		private SColor[] m_output;
		int m_width;
		int m_height;
		int m_targetWidth;
		int m_targetHeight;
		
		private Kernel1D[] m_kernelsCacheX = null; 
		private Kernel1D[] m_kernelsCacheY = null; 
		
		
		
		public Lanczos( SColor[] _originalImage, int _width)
		{
			m_input = _originalImage;
			m_width = _width;
			m_height = _originalImage.Length / _width;
		}
		
		public SColor[] Filter(int _targetWidth, int _targetHeight) {
	
			double xfactor = _targetWidth / (double)m_width;
			double yfactor = _targetHeight / (double)m_height;
            minsupport = 5;

            // reset kernel cache
            m_kernelsCacheX = new Kernel1D[100];
            m_kernelsCacheY = new Kernel1D[100]; 
			
			// create empty output image
			m_targetWidth  = (int)(0.5+m_width*xfactor);
			m_targetHeight = (int)(0.5+m_height*yfactor);
			
			m_output = new SColor[m_targetWidth * m_targetHeight]; 
			
			
			// for each pixel in output image
			for (int y=0; y<m_targetHeight; y++)
			{
				for (int x=0; x<m_targetWidth; x++)
				{
					
					// ideal sample point in the source image
					double xo = x/xfactor;
					double yo = y/yfactor;
					
					// separate integer part and fractionnal part
					int x_int = (int)xo; 
					double x_frac = xo - x_int;
					int y_int = (int)yo; 
					double y_frac = yo - y_int;
					
					// get/compute resampling Kernels 
					Kernel1D kx = getKernelX(xfactor, x_frac);
					Kernel1D ky = getKernelY(yfactor, y_frac);
	 
					// compute resampled value
					SColor rgb = fastconvolve( x_int, y_int, kx, ky);
					
					// set to output image
					m_output[x + y * m_targetWidth] = rgb;
				}
			}
			return m_output;
		}
		
		// ---------------------------------------------------------------------------
		//                    Compute and store Lanczos kernels
		// ---------------------------------------------------------------------------
	 
		/**
		 *  Lanczos function: sinc(d.pi)*sinc(d.pi/support)
		 * 
		 * @param support,d input parameters
		 * @return value of the function
		 */
		private double lanczos(double support, double d) {
			if (d==0) return 1.0;
			if (d>=support) return 0.0;
			double t = d * Math.PI;
			return support* Math.Sin(t)* Math.Sin(t/support)/(t*t);
		}
		
		/**
		 * Compute a Lanczos resampling kernel
		 * 
		 * @param scale scale to apply on the original image 
		 * @param frac fractionnal part of ideal sample point
		 * @return the Lanczos resampling kernel
		 */
		private Kernel1D precompute(double scale, double frac) {
	 
			// compute support size = how many source pixels for 1 sampled pixel ?
			int support=(int)(1+1.0/scale);
			// minimum support
			if (support<minsupport) support=minsupport;
			// support size must be odd
			if (support%2==0) support++;
			
			// scale limiter (minimum unit = 1 pixel)
			scale = Math.Min(scale, 1.0);
	 
			// construct an empty kernel
			Kernel1D kernel = new Kernel1D(support,new float[support],0);
			
			int i=0;
			int halfwindow = support/2;
			for(int dx=-halfwindow;dx<=halfwindow;dx++) {
					
				// ideal sample points (in the source image)
				double x = scale*(dx+frac);
					
				// corresponding weight (=contribution) of closest source pixel
				double coef = lanczos( halfwindow , x );
					
				// store to kernel
				float c = (float)(1000*coef+0.5);
				kernel.coefs[i++]  = c; 
				kernel.normalizer += c;
			}
			
			return kernel;
		}
	 
		private Kernel1D getKernel(Kernel1D[] cache,double scale, double frac) {
			int kid = (int)(frac*100);
			Kernel1D k = cache[kid];
			if(k==null) {
				k = precompute(scale, frac);
				cache[kid]=k;
				if (k.size>tmpbuffer_r.Length) tmpbuffer_r = new float[k.size];
				if (k.size>tmpbuffer_g.Length) tmpbuffer_g = new float[k.size];
				if (k.size>tmpbuffer_b.Length) tmpbuffer_b = new float[k.size];
				if (k.size>tmpbuffer_a.Length) tmpbuffer_a = new float[k.size];
			}
			return k;
		}
		private Kernel1D getKernelX(double scale, double frac) {
			return getKernel(m_kernelsCacheX,scale,frac);
		}
		private Kernel1D getKernelY(double scale, double frac) {
			return getKernel(m_kernelsCacheY,scale,frac);
		}
				
		// ---------------------------------------------------------------------------
		//              Fast 2D convolution (separation of the 2 kernels)
		// ---------------------------------------------------------------------------
	 
		// temporary buffer used for convolution
		private float[] tmpbuffer_r = new float[0];
		private float[] tmpbuffer_g = new float[0];
		private float[] tmpbuffer_b = new float[0];
		private float[] tmpbuffer_a = new float[0];
		
		private void ArrayFill<T>(T[]_array,T _value)
		{
			for(int i = 0;i<_array.Length;i++)
				_array[i] = _value;
		}
		
		/**
		 * convolve an image with a kernel for one pixel
		 * 
		 * @param c input image
		 * @param x,y coords of the pixel
		 * @param kernelx,kernely kernels to use
		 * @return new value of the pixel
		 */
		private SColor fastconvolve( int x, int y, Kernel1D kernelx, Kernel1D kernely) {
			int halfwindowy = kernely.size/2; // assume a odd size
			int halfwindowx = kernelx.size/2; // assume a odd size

            // empty tmpbuffer

            ArrayFill(tmpbuffer_r, 0);
            ArrayFill(tmpbuffer_g, 0);
            ArrayFill(tmpbuffer_b, 0);
            ArrayFill(tmpbuffer_a, 0);
			
			// pass 1 : horizontal convolution of image lines aree stored in tmpbuffer
			for(int dy=-halfwindowy;dy<=halfwindowy;dy++) {
				if (y+dy<0 || y+dy>=m_height) continue;
				for(int dx=-halfwindowx;dx<=halfwindowx;dx++) {
					if (x+dx<0 || x+dx>=m_width) continue;
					
					SColor rgb = m_input[x+dx + (y+dy) * m_width];
					
					tmpbuffer_r[halfwindowy+dy] += kernelx.coefs[halfwindowx-dx] * rgb.r;
					tmpbuffer_g[halfwindowy+dy] += kernelx.coefs[halfwindowx-dx] * rgb.g;
					tmpbuffer_b[halfwindowy+dy] += kernelx.coefs[halfwindowx-dx] * rgb.b;
					tmpbuffer_a[halfwindowy+dy] += kernelx.coefs[halfwindowx-dx] * rgb.a;
				}
			}
	 
			// pass 2 : vertical convolution of values stored in tmpbuffer
			double rc=0,gc=0,bc=0,ac=0;
			for(int dy=-halfwindowy;dy<=halfwindowy;dy++) {
				rc += kernely.coefs[halfwindowy-dy] * tmpbuffer_r[halfwindowy+dy];
				gc += kernely.coefs[halfwindowy-dy] * tmpbuffer_g[halfwindowy+dy];
				bc += kernely.coefs[halfwindowy-dy] * tmpbuffer_b[halfwindowy+dy];
				ac += kernely.coefs[halfwindowy-dy] * tmpbuffer_a[halfwindowy+dy];
			}
	 
			// normalization
			double norm = kernelx.normalizer*kernely.normalizer;
			
			rc/=norm;
			gc/=norm;
			bc/=norm;
			ac/=norm;
			
			// return in argb format
			float r = (float)Math.Min(1.0, Math.Max(0,rc));
			float g = (float)Math.Min(1, Math.Max(0,gc));
			float b = (float)Math.Min(1, Math.Max(0,bc));
			float a = (float)Math.Min(1, Math.Max(0,ac));
			
			return new SColor(r,g,b,a);
		}
	}
}