/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 * Adapted for Aurora from an article in gamedev magazine, with some additional ideas
 * from Christopher Breinholt: http://breinygames.blogspot.com.au
 * Rowan D <greythane@gmail.com> , Oct 2013
 * 
 * Functions for generating Perlin noise. To run the demos, put "grass.png" 
 * and "sand.png" in the executable folder.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using Aurora.Framework.Modules;

using Aurora.Framework.ConsoleFramework;

//namespace PerlinNoise
namespace Aurora.Framework.SceneInfo
{
    class PerlinNoise
    {
        #region Feilds
        static Random random = new Random();
        #endregion


        #region Reusable Functions

        public static float[][] GenerateWhiteNoise(int width, int height)
        {            
            float[][] noise = GetEmptyArray<float>(width, height);

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    noise[i][j] = (float)random.NextDouble() % 1;
                }
            }

            return noise;
        }

        public static float Interpolate(float x0, float x1, float alpha)
        {
            return x0 * (1 - alpha) + alpha * x1;
        }

        public static Color Interpolate(Color col0, Color col1, float alpha)
        {
            float beta = 1 - alpha;
            return Color.FromArgb(
                255,
                (int)(col0.R * alpha + col1.R * beta),
                (int)(col0.G * alpha + col1.G * beta),
                (int)(col0.B * alpha + col1.B * beta));
        }

        public static Color GetColor(Color gradientStart, Color gradientEnd, float t)
        {        
            float u = 1 - t;

            Color color = Color.FromArgb(
                255,
                (int)(gradientStart.R * u + gradientEnd.R * t),
                (int)(gradientStart.G * u + gradientEnd.G * t),
                (int)(gradientStart.B * u + gradientEnd.B * t));

            return color;
        }

        public static Color[][] MapGradient(Color gradientStart, Color gradientEnd, float[][] perlinNoise)
        {
            int width = perlinNoise.Length;
            int height = perlinNoise[0].Length;

            Color[][] image = GetEmptyArray<Color>(width, height); //an array of colours

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    image[i][j] = GetColor(gradientStart, gradientEnd, perlinNoise[i][j]);
                }
            }

            return image;
        }


		public static T[][] GetEmptyArray<T>(int width, int height)
        {
            T[][] image = new T[width][];

            for (int i = 0; i < width; i++)
            {
                image[i] = new T[height];
            }

            return image;
        }

        public static float[][] GenerateSmoothNoise(float[][] baseNoise, int octave)
        {
            int width = baseNoise.Length;
            int height = baseNoise[0].Length;

            float[][] smoothNoise = GetEmptyArray<float>(width, height);

            int samplePeriod = 1 << octave; // calculates 2 ^ k
            float sampleFrequency = 1.0f / samplePeriod;

            for (int i = 0; i < width; i++)
            {
                //calculate the horizontal sampling indices
                int sample_i0 = (i / samplePeriod) * samplePeriod;
                int sample_i1 = (sample_i0 + samplePeriod) % width; //wrap around
                float horizontal_blend = (i - sample_i0) * sampleFrequency;

                for (int j = 0; j < height; j++)
                {
                    //calculate the vertical sampling indices
                    int sample_j0 = (j / samplePeriod) * samplePeriod;
                    int sample_j1 = (sample_j0 + samplePeriod) % height; //wrap around
                    float vertical_blend = (j - sample_j0) * sampleFrequency;

                    //blend the top two corners
                    float top = Interpolate(baseNoise[sample_i0][sample_j0],
                        baseNoise[sample_i1][sample_j0], horizontal_blend);

                    //blend the bottom two corners
                    float bottom = Interpolate(baseNoise[sample_i0][sample_j1],
                        baseNoise[sample_i1][sample_j1], horizontal_blend);

                    //final blend
                    smoothNoise[i][j] = Interpolate(top, bottom, vertical_blend);                    
                }
            }
            
            return smoothNoise;
        }

        public static float[][] GeneratePerlinNoise(float[][] baseNoise, int octaveCount)
        {
            int width = baseNoise.Length;
            int height = baseNoise[0].Length;

            float[][][] smoothNoise = new float[octaveCount][][]; //an array of 2D arrays containing

            float persistance = 0.7f;

            //generate smooth noise
            for (int i = 0; i < octaveCount; i++)
            {
                smoothNoise[i] = GenerateSmoothNoise(baseNoise, i);
            }

            float[][] perlinNoise = GetEmptyArray<float>(width, height); //an array of floats initialised to 0

            float amplitude = 1.0f;
            float totalAmplitude = 0.0f;
            
            //blend noise together
            for (int octave = octaveCount - 1; octave >= 0; octave--)
            {
                amplitude *= persistance;
                totalAmplitude += amplitude;

                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        perlinNoise[i][j] += smoothNoise[octave][i][j] * amplitude;

                    }
                }
            }

            //normalisation
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    perlinNoise[i][j] /= totalAmplitude;
                }
            }        

            return perlinNoise;
        }

        public static float[][] GeneratePerlinNoise(int width, int height, int octaveCount)
        {
            float[][] baseNoise = GenerateWhiteNoise(width, height);

            return GeneratePerlinNoise(baseNoise, octaveCount);
        }

        public static Color[][] MapToGrey(float[][] greyValues)
        {
            int width = greyValues.Length;
            int height = greyValues[0].Length;

            Color[][] image = GetEmptyArray<Color>(width, height);

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int grey = (int)(255 * greyValues[i][j]);
                    Color color = Color.FromArgb(255, grey, grey, grey);

                    image[i][j] = color;
                }
            }

            return image;
        }

		public static float[][] MapToGreyScale(float[][] greyValues)
		{
			int width = greyValues.Length;
			int height = greyValues[0].Length;

			float[][] gScale = GetEmptyArray<float>(width, height);

			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < height; j++)
				{
					float grey = (float)(128 * greyValues[i][j]);
					gScale[i][j] = grey;
				}
			}

			return gScale;
		}

		public static float[][] Rescale ( float[][] greyValues, float min, float max)
		{
			// determine desired scaling factor
			float desiredRange = max - min;

			//work out current heightmap range
			float currMin = float.MaxValue;
			float currMax = float.MinValue;


			int width = greyValues.Length;
			int height = greyValues[0].Length;

			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					float currHeight = greyValues [x][y];
					if (currHeight < currMin) 
					{
						currMin = currHeight;
					} 
					else if (currHeight > currMax) 
					{
						currMax = currHeight;
					}
				}
			}

			float currRange = currMax - currMin;
			float scale = desiredRange / currRange;

			// scale the heightmap accordingly
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					float currHeight = greyValues [x][y] - currMin;
					greyValues [x][y] = min + (currHeight * scale);
				}
			}

			return greyValues;
		}


/*		public float Turbulence(float value, float x, float y, float size)
		{
			float initialSize = size;

			while (size >= 1)
			{
				value += value(x / size, y / size) * size;
				size /= 2.0;
			}

			value = (128.0 * value / initialSize);

			return value;
		}
*/

		public static float[][] SmoothHeightMap( float[][] greyValues)
		{
			int width = greyValues.Length;
			int height = greyValues[0].Length;


			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					float average = 0.0f;
					int points = 0;

					if (x - 1 >= 0)
					{
						average += greyValues [x - 1] [y];
						points += 1;
					}
					if (x + 1 < width - 1)
					{
						average += greyValues [x + 1] [y];
						points += 1;
					}
					if (y - 1 >= 0)
					{
						average += greyValues [x] [y - 1];
						points += 1;
					}
					if (y + 1 < height - 1)
					{
						average += greyValues [x] [y + 1];
						points += 1;
					}
					if ((x - 1 >= 0) && (y - 1 >= 0))
					{
						average += greyValues[x-1][y-1];
						points += 1;
					}
					if ((x + 1 < width) && (y - 1 >= 0))
					{
						average += greyValues [x + 1] [y - 1] ;
						points += 1;
					}
					if ((x - 1 >= 0) && (y + 1 < height))
					{
						average += greyValues [x - 1] [y + 1];
						points += 1;
					}
					if ((x + 1 < width) && (y + 1 < height))
					{
						average += greyValues [x + 1] [y + 1] ;
						points += 1;
					}

					average += greyValues [x] [y];
					points += 1;
					average /= points;

					greyValues [x] [y] = average;
				}
			}

			return greyValues;
		}


		public static void SaveMapImage(Color[][] image, string fileName)
		{
            int width = image.Length;
            int height = image[0].Length;

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    bitmap.SetPixel(i, j, image[i][j]);
                }
            }

            bitmap.Save(fileName);
        }

        public static Color[][] LoadMapImage(string fileName)
        {
            Bitmap bitmap = new Bitmap(fileName);
            
            int width = bitmap.Width;
            int height = bitmap.Height;

            Color[][] image = GetEmptyArray<Color>(width, height);

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    image[i][j] = bitmap.GetPixel(i, j);
                }
            }

            return image;
        }

        public static Color[][] BlendImages(Color[][] image1, Color[][] image2, float[][] perlinNoise)
        {
            int width = image1.Length;
            int height = image1[0].Length;

	        Color[][] image = GetEmptyArray<Color>(width, height); //an array of colours for the new image
            
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    image[i][j] = Interpolate(image1[i][j], image2[i][j], perlinNoise[i][j]);
                }
            }

	        return image;
        }
        


		/// <summary>
		/// Adjusts the levels.
		/// </summary>
		/// <returns>The levels.</returns>
		/// <param name="image">Image.</param>
		/// <param name="low">Low.</param>
		/// <param name="high">High.</param>
		public static float[][] AdjustLevels(float[][] image, float low, float high)
        {
            int width = image.Length;
            int height = image[0].Length;

            float[][] newImage = GetEmptyArray<float>(width, height);

            for (int i = 0; i < width; i++)
            {
                for(int j = 0; j < height; j++)
                {
                    float col = image[i][j];

                    if (col <= low)
                    {
                        newImage[i][j] = 0;
                    }
                    else if (col >= high)
                    {
                        newImage[i][j] = 1;
                    }
                    else
                    {
                        newImage[i][j] = (col - low) / (high - low);
                    }
                }
            }

            return newImage;
        }


		/// <summary>
		/// Generates a gradient map than can be used as a mask.
		/// </summary>
		/// <returns>The gradient map.</returns>
		/// <param name="width">Width.</param>
		/// <param name="height">Height.</param>
		public static float[][]  GenerateGradientMap(int width, int height)
		{
			int wVar = (int)(width * .13);
			int hVar = (int)(height * .11);

			float cx = (width / 2) + random.Next(-1*wVar, wVar);
			float cy = (height / 2) + random.Next(-1*hVar, hVar);
			float minRad = (float) Math.Sqrt((cx*cx)+(cy*cy))* 0.55f;
			float maxRad = minRad * 1.33f;
	
			float [][] gradient_map = GetEmptyArray<float>(width, height);
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					float rx = cx - x;
					float ry = cy - y;
					float rad = (float) Math.Sqrt(Math.Pow(rx,2) + Math.Pow(ry,2));
					float tRad = minRad * (float) (.47 + (random.NextDouble ()/1.3));

					float edgeDistance = maxRad - rad; 
					if( edgeDistance >=  tRad ) {
						gradient_map[x][y] = 1;
					} else if( edgeDistance <= 1 ) {
						gradient_map[x][y] = 0.001f;
					} else {
						float factor = (minRad/ (random.Next(3,9)*(edgeDistance)+minRad));
						gradient_map[x][y] = 1 - factor;
					}
				}
			}

			return gradient_map;
		}



		/// <summary>
		/// Generates a height map for a 'Island' style area (Edges of the map under water)
		/// </summary>
		/// <returns>The island height map.</returns>
		/// <param name="width">Width of the map area.</param>
		/// <param name="height">Height of the map area.</param>
		/// <param name="octaveCount">Octave count for the PerlinNoise generation.</param>
		/// <param name="min">Minimum land height.</param>
		/// <param name="max">Max imum land height.</param>
		/// <param name="smoothing">Smoothing passes.</param>
		public static float[][] GenerateIslandMap(int width, int height, int octaveCount, float min, float max, int smoothing)
		{
			if (width <= 0)
			{
				width = 256;
			}
			if (height <= 0)
			{
				height = 256;
			}


			float[][] perlinNoiseMap = GeneratePerlinNoise(width, height, octaveCount);
			perlinNoiseMap = AdjustLevels (perlinNoiseMap, 0.2f, 0.8f);
			float [][] perlinMap = MapToGreyScale (perlinNoiseMap);


			// mask the edges to an 'Island' shape
			float[][] map_mask = GenerateGradientMap(width, height);
			int x;
			int y;
			for (x=0; x < width; x++)
			{
				for (y=0; y < height; y++)
				{
					perlinMap[x][y] = (float) (perlinMap[x][y] * map_mask[x][y]);
				}
			}

			for (x=0; x < smoothing; x++)
			{
				perlinMap = SmoothHeightMap (perlinMap);
			}

			perlinMap = Rescale (perlinMap, min, max);

			return perlinMap;
		}


		/// <summary>
		/// Generates a height map for a 'Mainland' style area (Terrain out to the edges of the map).
		/// </summary>
		/// <returns>The height map.</returns>
		/// <param name="width">Width of the map area.</param>
		/// <param name="height">Height of the map area.</param>
		/// <param name="octaveCount">Octave count for the PerlinNoise generation.</param>
		/// <param name="min">Minimum land height.</param>
		/// <param name="max">Max imum land height.</param>
		/// <param name="smoothing">Smoothing passes.</param>
		public static float[][] GenerateHeightMap(int width, int height, int octaveCount, float min, float max, int smoothing)
		{
			if (width <= 0)
			{
				width = 256;
			}
			if (height <= 0)
			{
				height = 256;
			}
			if (octaveCount <= 0)
			{
				octaveCount = 8;
			}

			float[][] perlinNoiseMap = GeneratePerlinNoise(width, height, octaveCount);
			perlinNoiseMap = AdjustLevels (perlinNoiseMap, 0.2f, 0.8f);
			float [][] perlinMap = MapToGreyScale (perlinNoiseMap);

			for (int x=0; x < smoothing; x++)
			{
				perlinMap = SmoothHeightMap (perlinMap);
			}

			perlinMap = Rescale (perlinMap, min, max);

			return perlinMap;
		}

        #endregion
    }
}