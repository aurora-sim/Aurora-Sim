/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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

using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using System;
using System.Text;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using OpenMetaverse;

namespace OpenSim.Region.Framework.Scenes
{
    /// <summary>
    /// A new version of the old Channel class, simplified
    /// </summary>
    public class TerrainChannel : ITerrainChannel
    {
        private bool[,] taint;
        private double[,] map;
        private float[] m_cachedMap;
        private bool m_tainted = false;
        private IScene m_scene;

        public TerrainChannel(IScene scene)
        {
            m_scene = scene;
            CreateDefaultTerrain();
        }

        private void CreateDefaultTerrain()
        {
            map = new double[m_scene.RegionInfo.RegionSizeX, m_scene.RegionInfo.RegionSizeY];
            taint = new bool[m_scene.RegionInfo.RegionSizeX / Constants.TerrainPatchSize, m_scene.RegionInfo.RegionSizeY / Constants.TerrainPatchSize];

            int x;
            for (x = 0; x < m_scene.RegionInfo.RegionSizeX; x++)
            {
                int y;
                for (y = 0; y < m_scene.RegionInfo.RegionSizeY; y++)
                {
                    map[x, y] = TerrainUtil.PerlinNoise2D(x, y, 2, 0.125) * 10;
                    double spherFacA = TerrainUtil.SphericalFactor(x, y, m_scene.RegionInfo.RegionSizeX / 2.0, m_scene.RegionInfo.RegionSizeY / 2.0, 50) * 0.01;
                    double spherFacB = TerrainUtil.SphericalFactor(x, y, m_scene.RegionInfo.RegionSizeX / 2.0, m_scene.RegionInfo.RegionSizeY / 2.0, 100) * 0.001;
                    if (map[x, y] < spherFacA)
                        map[x, y] = spherFacA;
                    if (map[x, y] < spherFacB)
                        map[x, y] = spherFacB;
                }
            }
        }

        public TerrainChannel(double[,] import, IScene scene)
        {
            m_scene = scene;
            map = import;
            taint = new bool[Width, Height];
            if ((Width != scene.RegionInfo.RegionSizeX ||
                Height != scene.RegionInfo.RegionSizeY) &&
                (scene.RegionInfo.RegionSizeX != int.MaxValue) && //Child regions of a mega-region
                (scene.RegionInfo.RegionSizeY != int.MaxValue))
            {
                //We need to fix the map then
                CreateDefaultTerrain();
            }
        }

        public TerrainChannel(bool createMap, IScene scene)
        {
            m_scene = scene;
            if (createMap)
            {
                int width = Constants.RegionSize;
                int height = Constants.RegionSize;
                if (scene != null)
                {
                    width = (int)scene.RegionInfo.RegionSizeX;
                    height = (int)scene.RegionInfo.RegionSizeY;
                }
                map = new double[width, height];
                taint = new bool[width / Constants.TerrainPatchSize, height / Constants.TerrainPatchSize];
            }
        }

        public TerrainChannel(int w, int h, IScene scene)
        {
            m_scene = scene;
            map = new double[w, h];
            taint = new bool[w / Constants.TerrainPatchSize, h / Constants.TerrainPatchSize];
        }

        #region ITerrainChannel Members

        public int Width
        {
            get { return map.GetLength(0); }
        }

        public int Height
        {
            get { return map.GetLength(1); }
        }

        public IScene Scene
        {
            get { return m_scene; }
            set { m_scene = value; }
        }

        public float[] GetFloatsSerialised(IScene scene)
        {
            if (m_cachedMap != null && !m_tainted)
                return m_cachedMap;
            // Move the member variables into local variables, calling
            // member variables 256*256 times gets expensive
            int w = Width;
            int h = Height;
            float[] heights = new float[w * h];

            int i, j; // map coordinates
            int idx = 0; // index into serialized array
            for (i = 0; i < h; i++)
            {
                for (j = 0; j < w; j++)
                {
                    heights[idx++] = (float)map[j, i];
                }
            }
            m_cachedMap = heights;
            m_tainted = false;
            return heights;
        }

        public double[,] GetDoubles(IScene scene)
        {
            return map;
        }

        public double this[int x, int y]
        {
            get
            {
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                    return map[x, y];
                else
                    return 0;
            }
            set
            {
                // Will "fix" terrain hole problems. Although not fantastically.
                if (Double.IsNaN(value) || Double.IsInfinity(value))
                    return;

                if (map[x, y] != value)
                {
                    taint[x / Constants.TerrainPatchSize, y / Constants.TerrainPatchSize] = true;
                    //m_tainted = true;
                    map[x, y] = value;
                    if(m_cachedMap != null)
                        m_cachedMap[y * map.GetLength(0) + x] = (float)value;
                }
            }
        }

        public bool Tainted(int x, int y)
        {
            if (taint[x / Constants.TerrainPatchSize, y / Constants.TerrainPatchSize])
            {
                taint[x / Constants.TerrainPatchSize, y / Constants.TerrainPatchSize] = false;
                return true;
            }
            return false;
        }

        #endregion

        public ITerrainChannel MakeCopy()
        {
            TerrainChannel copy = new TerrainChannel(false, m_scene);
            copy.map = (double[,])map.Clone();
            copy.taint = (bool[,])taint.Clone();
            return copy;
        }

        /// <summary>
        /// Gets the average height of the area +2 in both the X and Y directions from the given position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public float GetNormalizedGroundHeight(float x, float y)
        {
            if (x < 0)
                x = 0;
            if (x >= Width)
                x = Width - 1;
            if (y < 0)
                y = 0;
            if (y >= Height)
                y = Height - 1;

            Vector3 p0 = new Vector3(x, y, (float)this[(int)x, (int)y]);
            Vector3 p1 = new Vector3(p0);
            Vector3 p2 = new Vector3(p0);

            p1.X += 1.0f;
            if (p1.X < Width)
                p1.Z = (float)this[(int)p1.X, (int)p1.Y];

            p2.Y += 1.0f;
            if (p2.Y < Height)
                p2.Z = (float)this[(int)p2.X, (int)p2.Y];

            Vector3 v0 = new Vector3(p1.X - p0.X, p1.Y - p0.Y, p1.Z - p0.Z);
            Vector3 v1 = new Vector3(p2.X - p0.X, p2.Y - p0.Y, p2.Z - p0.Z);

            v0.Normalize();
            v1.Normalize();

            Vector3 vsn = new Vector3();
            vsn.X = (v0.Y * v1.Z) - (v0.Z * v1.Y);
            vsn.Y = (v0.Z * v1.X) - (v0.X * v1.Z);
            vsn.Z = (v0.X * v1.Y) - (v0.Y * v1.X);
            vsn.Normalize();

            float xdiff = x - (float)((int)x);
            float ydiff = y - (float)((int)y);

            return (((vsn.X * xdiff) + (vsn.Y * ydiff)) / (-1 * vsn.Z)) + p0.Z;
        }
    }
}
