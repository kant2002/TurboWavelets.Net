using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace TurboWavelets.Benchmarks
{
	[SimpleJob(RuntimeMoniker.NetCoreApp50, baseline: true)]
	[SimpleJob(RuntimeMoniker.CoreRt50)]
	//[RPlotExporter]
	public class Md5VsSha256
    {
        private Bitmap bmp;
		Bitmap bmp2;
		float[,] yArray;
		float[,] cbArray;
		float[,] crArray;
		float[,] aArray;
		float[,] yArray2;
		float[,] cbArray2;
		float[,] crArray2;
		float[,] aArray2;


		public Md5VsSha256()
        {
            bmp = new Bitmap("sample.png");

			yArray = new float[bmp.Width, bmp.Height];
			cbArray = new float[bmp.Width, bmp.Height];
			crArray = new float[bmp.Width, bmp.Height];
			aArray = new float[bmp.Width, bmp.Height];
			ImageArrayConverter.BitmapToAYCbCrArrays(bmp, aArray, yArray, cbArray, crArray);

			bmp2 = new Bitmap(2 * bmp.Width, 2 * bmp.Height, PixelFormat.Format32bppArgb);
			yArray2 = new float[bmp2.Width, bmp2.Height];
			cbArray2 = new float[bmp2.Width, bmp2.Height];
			crArray2 = new float[bmp2.Width, bmp2.Height];
			aArray2 = new float[bmp2.Width, bmp2.Height];
			ImageArrayConverter.BitmapToAYCbCrArrays(bmp, aArray2, yArray2, cbArray2, crArray2);
		}

        [Benchmark]
        public Bitmap Waveletimageupscaling()
        {
			Biorthogonal53Wavelet2D wavelet = new Biorthogonal53Wavelet2D(bmp.Width, bmp.Height);
			Biorthogonal53Wavelet2D wavelet2 = new Biorthogonal53Wavelet2D(bmp2.Width, bmp2.Height);

			wavelet.TransformIsotropic2D(aArray2);
			wavelet.TransformIsotropic2D(yArray2);
			wavelet.TransformIsotropic2D(cbArray2);
			wavelet.TransformIsotropic2D(crArray2);

			wavelet2.BacktransformIsotropic2D(aArray2);
			wavelet2.BacktransformIsotropic2D(yArray2);
			wavelet2.BacktransformIsotropic2D(cbArray2);
			wavelet2.BacktransformIsotropic2D(crArray2);
			for (int y = 0; y < bmp2.Height; y++)
			{
				for (int x = 0; x < bmp2.Width; x++)
				{
					aArray2[x, y] *= 4.0f;
					yArray2[x, y] *= 4.0f;
					cbArray2[x, y] *= 4.0f;
					crArray2[x, y] *= 4.0f;
				}
			}

			// ImageArrayConverter.AYCbCrArraysToBitmap(aArray2, yArray2, cbArray2, crArray2, bmp2);
			return bmp2;
		}

		[Benchmark]
		public void AdaptiveSharpening()
        {
			applyAdaptiveShapening(yArray, 5.0f);
			// ImageArrayConverter.AYCbCrArraysToBitmap(aArray, yArray, cbArray, crArray, bmp);
		}

		[Benchmark]
		public void AdaptiveDeadzone()
		{
			//setting ~95% of luminance coefficients to zero
			applyAdaptiveDeadzone(yArray, 3);
			//compress chroma even more (98,4%)
			applyAdaptiveDeadzone(cbArray, 1);
			applyAdaptiveDeadzone(crArray, 1);
		}

		private static void applyAdaptiveShapening(float[,] array, float position)
		{
			int width = array.GetLength(0);
			int height = array.GetLength(1);
			Biorthogonal53Wavelet2D wavelet53 = new Biorthogonal53Wavelet2D(width, height);
			OrderWavelet2D waveletOrder = new OrderWavelet2D(width, height);

			wavelet53.EnableCaching = true;
			waveletOrder.EnableCaching = true;
			wavelet53.TransformIsotropic2D(array);
			//Reverse the ordering of the coefficients
			waveletOrder.BacktransformIsotropic2D(array);
			float[] scale = new float[8 * 8];

			for (int x = 0; x < 8 * 8; x++)
			{
				scale[x] = 1.0f + 2.0f / ((position - x) * (position - x) + 1.0f);
			}
			waveletOrder.ScaleCoefficients(array, scale, 8);
			waveletOrder.TransformIsotropic2D(array);
			wavelet53.BacktransformIsotropic2D(array);
		}

		private static void applyAdaptiveDeadzone(float[,] array, int numCoeffs)
		{
			int width = array.GetLength(0);
			int height = array.GetLength(1);
			Biorthogonal53Wavelet2D wavelet53 = new Biorthogonal53Wavelet2D(width, height);
			OrderWavelet2D waveletOrder = new OrderWavelet2D(width, height);

			wavelet53.EnableCaching = true;
			waveletOrder.EnableCaching = true;
			wavelet53.TransformIsotropic2D(array);
			//Reverse the ordering of the coefficients
			waveletOrder.BacktransformIsotropic2D(array);
			//Use numCoeffs cofficient out of 64 (8x8) -> for e.g. numCoeffs = 5 this
			//means a compression to 7,8% of the original size
			waveletOrder.CropCoefficients(array, 7, 8);
			waveletOrder.TransformIsotropic2D(array);
			wavelet53.BacktransformIsotropic2D(array);
		}
	}
    
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner
                .Run<Md5VsSha256>();
        }
    }
}
