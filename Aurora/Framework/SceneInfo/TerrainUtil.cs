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
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Aurora.Framework.Modules;

namespace Aurora.Framework.SceneInfo
{
    public static class TerrainUtil
    {
        public static float MetersToSphericalStrength(float size)
        {
            //return Math.Pow(2, size);
            return (size + 1)*1.35f; // MCP: a more useful brush size range
        }

        public static float SphericalFactor(float x, float y, float rx, float ry, float size)
        {
            return size*size - ((x - rx)*(x - rx) + (y - ry)*(y - ry));
        }

        public static float GetBilinearInterpolate(float x, float y, ITerrainChannel map)
        {
            int w = map.Width;
            int h = map.Height;

            if (x > w - 2)
                x = w - 2;
            if (y > h - 2)
                y = h - 2;
            if (x < 0.0)
                x = 1.0f;
            if (y < 0.0)
                y = 1.0f;

            if (x > map.Width - 2)
                x = map.Width - 2;
            if (x < 0)
                x = 0;
            if (y > map.Height - 2)
                y = map.Height - 2;
            if (y < 0)
                y = 0;

            const int stepSize = 1;
            float h00 = map[(int) x, (int) y];
            float h10 = map[(int) x + stepSize, (int) y];
            float h01 = map[(int) x, (int) y + stepSize];
            float h11 = map[(int) x + stepSize, (int) y + stepSize];
            float h1 = h00;
            float h2 = h10;
            float h3 = h01;
            float h4 = h11;
            float a00 = h1;
            float a10 = h2 - h1;
            float a01 = h3 - h1;
            float a11 = h1 - h2 - h3 + h4;
            float partialx = x - (int) x;
            float partialz = y - (int) y;
            float hi = a00 + (a10*partialx) + (a01*partialz) + (a11*partialx*partialz);
            return hi;
        }

        private static float Noise(float x, float y)
        {
            int n = (int) x + (int) (y*749);
            n = (n << 13) ^ n;
            return (1 - ((n*(n*n*15731 + 789221) + 1376312589) & 0x7fffffff)/1073741824);
        }

        private static float SmoothedNoise1(float x, float y)
        {
            float corners = (Noise(x - 1, y - 1) + Noise(x + 1, y - 1) + Noise(x - 1, y + 1) + Noise(x + 1, y + 1))/16;
            float sides = (Noise(x - 1, y) + Noise(x + 1, y) + Noise(x, y - 1) + Noise(x, y + 1))/8;
            float center = Noise(x, y)/4;
            return corners + sides + center;
        }

        private static float Interpolate(float x, float y, float z)
        {
            return (x*(1 - z)) + (y*z);
        }

        public static float InterpolatedNoise(float x, float y)
        {
            int integer_X = (int) (x);
            float fractional_X = x - integer_X;

            int integer_Y = (int) y;
            float fractional_Y = y - integer_Y;

            float v1 = SmoothedNoise1(integer_X, integer_Y);
            float v2 = SmoothedNoise1(integer_X + 1, integer_Y);
            float v3 = SmoothedNoise1(integer_X, integer_Y + 1);
            float v4 = SmoothedNoise1(integer_X + 1, integer_Y + 1);

            float i1 = Interpolate(v1, v2, fractional_X);
            float i2 = Interpolate(v3, v4, fractional_X);

            return Interpolate(i1, i2, fractional_Y);
        }

        public static float PerlinNoise2D(float x, float y, int octaves, float persistence)
        {
            float total = 0;

            for (int i = 0; i < octaves; i++)
            {
                float frequency = (float) Math.Pow(2, i);
                float amplitude = (float) Math.Pow(persistence, i);

                total += InterpolatedNoise(x*frequency, y*frequency)*amplitude;
            }
            return total;
        }
    }
}