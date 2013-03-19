/*
 * Copyright (c) Contributors, http://aurora-sim.org/
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
using System.Drawing;
using System.Drawing.Imaging;

namespace Aurora.Framework.Utilities
{
    /// <summary>
    ///     Keeps track of data and builds a graph out of the given data
    /// </summary>
    public class Profiler
    {
        private readonly Color BackgroundColor = Color.LightGray;
        private readonly Color BarColor = Color.Aqua;
        private readonly int[] GraphBarsEnd = new[] {35, 50, 65, 80, 95, 110, 125, 140, 155, 170};
        private readonly int[] GraphBarsStart = new[] {25, 40, 55, 70, 85, 100, 115, 130, 145, 160};
        private readonly Color LineColor = Color.DarkGray;
        private readonly Dictionary<string, ProfilerValueManager> Stats = new Dictionary<string, ProfilerValueManager>();

        public void AddStat(string Name, double value)
        {
            if (!Stats.ContainsKey(Name))
                Stats[Name] = new ProfilerValueManager();
            Stats[Name].AddStat(value);
        }

        public ProfilerValueManager GetStat(string Name)
        {
            ProfilerValueManager manager = null;
            Stats.TryGetValue(Name, out manager);
            return manager;
        }

        public FastBitmap DrawGraph(string StatName)
        {
            Bitmap bitmap = new Bitmap(200, 200, PixelFormat.Format24bppRgb);
            FastBitmap bmp = new FastBitmap(bitmap);
            bmp.LockBitmap();

            ProfilerValueManager statManager = GetStat(StatName);
            double MaxVal = 0;
            if (statManager != null)
                MaxVal = statManager.GetMaxValue();

            double ScaleFactor = 1/(MaxVal/200); //We multiply by this so that the graph uses the full space

            double[] Stats2 = new double[0];
            if (statManager != null)
                Stats2 = statManager.GetInfos();

            for (int i = 0; i < Stats2.Length; i++)
            {
                //Update the scales
                Stats2[i] = Stats2[i]*ScaleFactor;
            }

            for (int x = 200; x > 0; x--)
            {
                for (int y = 200; y > 0; y--)
                {
                    //Note: we do 200-y to flip the graph on the Y axis
                    if (IsInGraphBar(x, y, Stats2, ScaleFactor))
                        bmp.SetPixel(x, 200 - y, BarColor);
                    else
                    {
                        //Check whether the line needs drawn
                        bmp.SetPixel(x, 200 - y, DrawLine(y, ScaleFactor) ? LineColor : BackgroundColor);
                    }
                }
            }
            bmp.UnlockBitmap();

            return bmp;
        }

        public FastBitmap DrawGraph(string StatName, double MaxVal)
        {
            Bitmap bitmap = new Bitmap(200, 200, PixelFormat.Format24bppRgb);
            FastBitmap bmp = new FastBitmap(bitmap);
            bmp.LockBitmap();

            ProfilerValueManager statManager = GetStat(StatName);

            double ScaleFactor = 1/(MaxVal/200); //We multiply by this so that the graph uses the full space

            double[] Stats2 = new double[0];
            if (statManager != null)
                Stats2 = statManager.GetInfos();

            for (int x = 200; x > 0; x--)
            {
                for (int y = 200; y > 0; y--)
                {
                    //Note: we do 200-y to flip the graph on the Y axis
                    if (IsInGraphBar(x, y, Stats2, ScaleFactor))
                        bmp.SetPixel(x, 200 - y, BarColor);
                    else
                    {
                        //Check whether the line needs drawn
                        bmp.SetPixel(x, 200 - y, DrawLine(y, ScaleFactor) ? LineColor : BackgroundColor);
                    }
                }
            }
            bmp.UnlockBitmap();

            return bmp;
        }

        private bool DrawLine(double y, double ScaleFactor)
        {
            return (y%10) == 0;
        }

        private bool IsInGraphBar(int x, int y, double[] Stats, double scaleFactor)
        {
            for (int i = Math.Min(GraphBarsStart.Length - 1, Stats.Length - 1); i >= 0; i--)
            {
                //Check whether it is between both the start and end
                if (x > GraphBarsStart[i] && x < GraphBarsEnd[i])
                {
                    if (Stats[i] >= (y/scaleFactor))
                        return true;
                }
            }
            return false;
        }
    }

    public class ProfilerValueManager
    {
        private readonly double[] infos = new double[10];
        private int lastSet;
        private int zero;

        public void AddStat(double value)
        {
            lock (infos)
            {
                if (lastSet != 10)
                {
                    infos[lastSet] = value;
                    lastSet++;
                }
                else
                {
                    //Move the 0 value around
                    infos[zero] = value;
                    //Now increment 0 as it isn't where it was before
                    zero++;
                    if (zero == 10)
                        zero = 0;
                }
            }
        }

        public double[] GetInfos()
        {
            lock (infos)
            {
                double[] copy = new double[lastSet];
                int ii = zero;
                for (int i = 0; i < lastSet; i++)
                {
                    copy[i] = infos[ii];
                    ii++;
                    if (ii == lastSet)
                        ii = 0;
                }
                return copy;
            }
        }

        public double GetMaxValue()
        {
            double MaxVal = 0;
            lock (infos)
            {
                for (int i = 0; i < lastSet; i++)
                {
                    if (infos[i] > MaxVal)
                        MaxVal = infos[i];
                }
            }
            return MaxVal;
        }
    }

    public class ProfilerManager
    {
        private static Profiler profiler;

        public static Profiler GetProfiler()
        {
            if (profiler == null)
                profiler = new Profiler();
            return profiler;
        }
    }
}