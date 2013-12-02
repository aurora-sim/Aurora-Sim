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
using System.Drawing;
using Aurora.Framework.Modules;
using Aurora.Framework.Utilities;
using OpenMetaverse;

namespace Aurora.Framework.SceneInfo
{
    /// <summary>
    ///     A new version of the old Channel class, simplified
    /// </summary>
    public class TerrainChannel : ITerrainChannel
    {
        private int m_Width;

        /// <summary>
        ///     NOTE: This is NOT a normal map, it has a resolution of 10x
        /// </summary>
        private short[] m_map;

        private IScene m_scene;
        private bool[,] taint;

        public TerrainChannel(IScene scene)
        {
            m_scene = scene;
            m_Width = m_scene.RegionInfo.RegionSizeX;
			CreateDefaultTerrain(m_scene.RegionInfo.RegionType);
        }

		public TerrainChannel(String landType, IScene scene)
		{
			m_scene = scene;
			m_Width = Constants.RegionSize;
			if (scene != null)
			{
				m_Width = scene.RegionInfo.RegionSizeX;
			}

			CreateDefaultTerrain(landType);
		}

		public TerrainChannel(short[] import, IScene scene)
        {
            m_scene = scene;
            m_map = import;
            m_Width = (int) Math.Sqrt(import.Length);
            taint = new bool[m_Width,m_Width];
            if ((m_Width != scene.RegionInfo.RegionSizeX ||
                 m_Width != scene.RegionInfo.RegionSizeY) &&
                (scene.RegionInfo.RegionSizeX != int.MaxValue) && //Child regions of a mega-region
                (scene.RegionInfo.RegionSizeY != int.MaxValue))
            {
                //We need to fix the map then
				CreateDefaultTerrain(m_scene.RegionInfo.RegionType);
            }
        }

        public TerrainChannel(bool createMap, IScene scene)
        {
            m_scene = scene;
            m_Width = Constants.RegionSize;
            if (scene != null)
            {
                m_Width = scene.RegionInfo.RegionSizeX;
            }
            if (createMap)
            {
                m_map = new short[m_Width*m_Width];
                taint = new bool[m_Width/Constants.TerrainPatchSize,m_Width/Constants.TerrainPatchSize];
            }
        }

        public TerrainChannel(int w, int h, IScene scene)
        {
            m_scene = scene;
            m_Width = w;

			// the basic assumption is that regions are square so make sure...
			if (m_scene != null)
			{
				if (w != m_scene.RegionInfo.RegionSizeX)
					w = m_scene.RegionInfo.RegionSizeX;
				if (h != m_scene.RegionInfo.RegionSizeY)
					h = m_scene.RegionInfo.RegionSizeY;

			}
			else
			{
				if (w != Constants.RegionSize)
					w = Constants.RegionSize;
				if (h != Constants.RegionSize)
					h = Constants.RegionSize;
			}


            m_map = new short[w*h];
            taint = new bool[w/Constants.TerrainPatchSize,h/Constants.TerrainPatchSize];
        }

        #region ITerrainChannel Members

        public int Width
        {
            get { return m_Width; }
        }

        public int Height
        {
            get { return m_Width; }
        }

        public IScene Scene
        {
            get { return m_scene; }
            set { m_scene = value; }
        }

        public short[] GetSerialised()
        {
            return m_map;
        }

        public float this[int x, int y]
        {
            get
            {
                if (x >= 0 && x < m_Width && y >= 0 && y < m_Width)
                    return (m_map[y*m_Width + x])/Constants.TerrainCompression;
                else
                {
                    //Get the nearest one so that things don't get screwed up near borders
                    int betterX = x < 0 ? 0 : x >= m_Width ? m_Width - 1 : x;
                    int betterY = y < 0 ? 0 : y >= m_Width ? m_Width - 1 : y;
                    return (m_map[betterY*m_Width + betterX])/Constants.TerrainCompression;
                }
            }
            set
            {
                // Will "fix" terrain hole problems. Although not fantastically.
                if (value*Constants.TerrainCompression > short.MaxValue)
                    value = short.MaxValue;
                if (value*Constants.TerrainCompression < short.MinValue)
                    value = short.MinValue;

                if (m_map[y*m_Width + x] != value*Constants.TerrainCompression)
                {
                    taint[x/Constants.TerrainPatchSize, y/Constants.TerrainPatchSize] = true;
                    m_map[y*m_Width + x] = (short) (value*Constants.TerrainCompression);
                }
            }
        }

        public bool Tainted(int x, int y)
        {
            if (taint[x/Constants.TerrainPatchSize, y/Constants.TerrainPatchSize])
            {
                taint[x/Constants.TerrainPatchSize, y/Constants.TerrainPatchSize] = false;
                return true;
            }
            return false;
        }

        public ITerrainChannel MakeCopy()
        {
            TerrainChannel copy = new TerrainChannel(false, m_scene)
                                      {m_map = (short[]) m_map.Clone(), taint = (bool[,]) taint.Clone()};
            return copy;
        }

        /// <summary>
        ///     Gets the average height of the area +2 in both the X and Y directions from the given position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public float GetNormalizedGroundHeight(int x, int y)
        {
            if (x < 0)
                x = 0;
            if (x >= m_Width)
                x = m_Width - 1;
            if (y < 0)
                y = 0;
            if (y >= m_Width)
                y = m_Width - 1;

            Vector3 p0 = new Vector3(x, y, this[x, y]);
            Vector3 p1 = new Vector3(p0);
            Vector3 p2 = new Vector3(p0);

            p1.X += 1.0f;
            if (p1.X < m_Width)
                p1.Z = this[(int) p1.X, (int) p1.Y];

            p2.Y += 1.0f;
            if (p2.Y < m_Width)
                p2.Z = this[(int) p2.X, (int) p2.Y];

            Vector3 v0 = new Vector3(p1.X - p0.X, p1.Y - p0.Y, p1.Z - p0.Z);
            Vector3 v1 = new Vector3(p2.X - p0.X, p2.Y - p0.Y, p2.Z - p0.Z);

            v0.Normalize();
            v1.Normalize();

            Vector3 vsn = new Vector3
                              {
                                  X = (v0.Y*v1.Z) - (v0.Z*v1.Y),
                                  Y = (v0.Z*v1.X) - (v0.X*v1.Z),
                                  Z = (v0.X*v1.Y) - (v0.Y*v1.X)
                              };
            vsn.Normalize();

            return ((vsn.X + vsn.Y)/(-1*vsn.Z)) + p0.Z;
        }

		public void GenerateTerrain(String landType, float min, float max, int smoothing,  IScene scene)
		{
			m_scene = scene;
			m_Width = Constants.RegionSize;
			if (scene != null)
			{
				m_Width = scene.RegionInfo.RegionSizeX;
			}

			if (landType == null)
			{
				CreateFlatlandTerrain ();
				return;
			}

			// try for the land type
			landType = landType.ToLower ();
			if (landType.Equals ("mainland"))
				CreateMainlandTerrain (min, max, smoothing);
			else if (landType.Equals ("island"))
				CreateIslandTerrain (min, max, smoothing);
			else
				CreateFlatlandTerrain ();
		}
        #endregion

        /// <summary>
        /// Creates the default terrain, currently 'Flatland'
        /// </summary>
		private void CreateDefaultTerrain(string landType)
		{
			if (landType == null)
			{
				CreateFlatlandTerrain ();
				return;
			}

			// try for the land type
			landType = landType.ToLower ();
			if (landType.Equals ("mainland"))
				CreateMainlandTerrain ();
			else if (landType.Equals ("island"))
				CreateIslandTerrain ();
			else
				CreateFlatlandTerrain ();
		}

		private void CreateFlatlandTerrain()
		{

			m_map = null;
		 	taint = null;
            m_map = new short[m_scene.RegionInfo.RegionSizeX*m_scene.RegionInfo.RegionSizeX];
            taint =
                new bool[m_scene.RegionInfo.RegionSizeX/Constants.TerrainPatchSize,
                    m_scene.RegionInfo.RegionSizeY/Constants.TerrainPatchSize];
            m_Width = m_scene.RegionInfo.RegionSizeX;

            int x;
            for (x = 0; x < m_scene.RegionInfo.RegionSizeX; x++)
            {
                int y;
                for (y = 0; y < m_scene.RegionInfo.RegionSizeY; y++)
                {
                    this[x, y] = (float) m_scene.RegionInfo.RegionSettings.WaterHeight + 1;
                }
            }
        }


		private void CreateMainlandTerrain()
		{
			float minHeight = (float) m_scene.RegionInfo.RegionSettings.WaterHeight - 5;
			float maxHeight = 30;

			CreateMainlandTerrain (minHeight, maxHeight,2);
		}

		private void CreateMainlandTerrain (float minHeight, float maxHeight, int smoothing)
		{
			m_map = null;
			taint = null;
			m_map = new short[m_scene.RegionInfo.RegionSizeX*m_scene.RegionInfo.RegionSizeX];
			taint =
				new bool[m_scene.RegionInfo.RegionSizeX/Constants.TerrainPatchSize,
				         m_scene.RegionInfo.RegionSizeY/Constants.TerrainPatchSize];

			int rWidth = m_scene.RegionInfo.RegionSizeX;
			int rHeight = m_scene.RegionInfo.RegionSizeY;
			m_Width = rWidth; 

			int octaveCount = 8;
			float[][] heightMap = PerlinNoise.GenerateHeightMap(rWidth, rHeight, octaveCount, minHeight, maxHeight, smoothing);

			int x;
			for (x = 0; x < rWidth; x++)
			{
				int y;
				for (y = 0; y < rHeight; y++)
				{
					this[x, y] = heightMap[x][y];
				}
			}
		}

		private void CreateIslandTerrain()
		{
			float minHeight = (float) m_scene.RegionInfo.RegionSettings.WaterHeight - 5;
			float maxHeight = minHeight + 3;

			CreateIslandTerrain (minHeight, maxHeight,2);
		}

		private void CreateIslandTerrain(float minHeight, float maxHeight, int smoothing)
		{
			m_map = null;
			taint = null;
			m_map = new short[m_scene.RegionInfo.RegionSizeX*m_scene.RegionInfo.RegionSizeX];
			taint =
				new bool[m_scene.RegionInfo.RegionSizeX/Constants.TerrainPatchSize,
				         m_scene.RegionInfo.RegionSizeY/Constants.TerrainPatchSize];

			int rWidth = m_scene.RegionInfo.RegionSizeX;
			int rHeight = m_scene.RegionInfo.RegionSizeY;
			m_Width = rWidth; 

			int octaveCount = 8;
			float[][] heightMap = PerlinNoise.GenerateIslandMap(rWidth, rHeight, octaveCount, minHeight, maxHeight, smoothing);

			int x;
			for (x = 0; x < rWidth; x++)
			{
				int y;
				for (y = 0; y < rHeight; y++)
				{
					this[x, y] = heightMap[x][y];
				}
			}
		}

		// original island from opensim
		private void CreateAtolIslandTerrain()
		{
			m_map = null;
			taint = null;
			m_map = new short[m_scene.RegionInfo.RegionSizeX*m_scene.RegionInfo.RegionSizeX];
			taint =
				new bool[m_scene.RegionInfo.RegionSizeX/Constants.TerrainPatchSize,
				         m_scene.RegionInfo.RegionSizeY/Constants.TerrainPatchSize];
			m_Width = m_scene.RegionInfo.RegionSizeX;

			int x;
			int y;
			float regionDiv = (float) (Constants.RegionSize / 2.0);

			for (x = 0; x < m_scene.RegionInfo.RegionSizeX; x++)
			{
				for (y = 0; y < m_scene.RegionInfo.RegionSizeY; y++)
				{
					this [x, y] = (float)TerrainUtil.PerlinNoise2D (x, y, 2, (float)  0.125) * 10;
					float spherFacA = (float) (TerrainUtil.SphericalFactor ( x, y, regionDiv, regionDiv, 50) * 0.01);
					float spherFacB = (float) (TerrainUtil.SphericalFactor ( x, y, regionDiv, regionDiv, 100) * 0.001);
					if (this [x, y] < spherFacA)
						this [x, y] = spherFacA;
					if (this [x, y] < spherFacB)
						this [x, y] = spherFacB;
				}
			}
		}

	}
}