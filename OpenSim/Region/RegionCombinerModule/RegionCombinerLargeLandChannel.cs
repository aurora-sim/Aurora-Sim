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

using System;
using System.Collections.Generic;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.CoreModules.World.Land;

namespace OpenSim.Region.RegionCombinerModule
{
    public class RegionCombinerLargeTerrainChannel : ITerrainChannel
    {
        // private static readonly ILog m_log =
        //     LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private RegionData RegData;
        private Dictionary<RegionData, ITerrainChannel> RegionConnections = new Dictionary<RegionData, ITerrainChannel>();

        public void AddRegion(RegionData rData, ITerrainChannel thisRegionTerrainChannel)
        {
            if (RegData == null)
            {
                //Root region
                RegData = rData;
            }
            RegionConnections.Add(rData, thisRegionTerrainChannel);
            RegData.RegionScene.Heightmap = this;
        }

        // We just overload the height
        public double this[int x, int y]
        {
            get
            {
                int offsetX = (int)(x / (int)Constants.RegionSize);
                int offsetY = (int)(y / (int)Constants.RegionSize);
                offsetX *= (int)Constants.RegionSize;
                offsetY *= (int)Constants.RegionSize;

                foreach (KeyValuePair<RegionData, ITerrainChannel> kvp in RegionConnections)
                {
                    if (kvp.Key.Offset.X == offsetX && kvp.Key.Offset.Y == offsetY)
                    {
                        return kvp.Value[x - offsetX, y - offsetY];
                    }
                }
                return 0;
            }
            set
            {
                int offsetX = (int)(x / (int)Constants.RegionSize);
                int offsetY = (int)(y / (int)Constants.RegionSize);
                offsetX *= (int)Constants.RegionSize;
                offsetY *= (int)Constants.RegionSize;

                foreach (KeyValuePair<RegionData, ITerrainChannel> kvp in RegionConnections)
                {
                    if (kvp.Key.Offset.X == offsetX && kvp.Key.Offset.Y == offsetY)
                    {
                        kvp.Value[x - offsetX, y - offsetY] = value;
                    }
                }
            }
        }

        public IScene Scene
        {
            get
            {
                return RegionConnections[RegData].Scene;
            }
            set
            {
                RegionConnections[RegData].Scene = value;
            }
        }

        public int Height
        {
            get 
            {
                int Height = 256;
                foreach (KeyValuePair<RegionData, ITerrainChannel> kvp in RegionConnections)
                {
                    if (kvp.Key.Offset.Y + (int)Constants.RegionSize > Height)
                        Height = (int)kvp.Key.Offset.Y + (int)Constants.RegionSize;
                }
                return Height;
            }
        }

        public int Width
        {
            get 
            {
                int Width = 256;
                foreach (KeyValuePair<RegionData, ITerrainChannel> kvp in RegionConnections)
                {
                    if (kvp.Key.Offset.X + (int)Constants.RegionSize > Width)
                        Width = (int)kvp.Key.Offset.X + (int)Constants.RegionSize;
                }
                return Width;
            }
        }

        public float[] GetFloatsSerialised(IScene scene)
        {
            foreach (KeyValuePair<RegionData, ITerrainChannel> kvp in RegionConnections)
            {
                if (kvp.Key.RegionId == scene.RegionInfo.RegionID)
                {
                    return kvp.Value.GetFloatsSerialised(scene);
                }
            }
            return null;
        }

        public double[,] GetDoubles(IScene scene)
        {
            foreach (KeyValuePair<RegionData, ITerrainChannel> kvp in RegionConnections)
            {
                if (kvp.Key.RegionId == scene.RegionInfo.RegionID)
                {
                    return kvp.Value.GetDoubles(scene);
                }
            }
            return null;
        }

        public bool Tainted(int x, int y)
        {
            int offsetX = (int)(x / (int)Constants.RegionSize);
            int offsetY = (int)(y / (int)Constants.RegionSize);
            offsetX *= (int)Constants.RegionSize;
            offsetY *= (int)Constants.RegionSize;

            foreach (KeyValuePair<RegionData, ITerrainChannel> kvp in RegionConnections)
            {
                if (kvp.Key.Offset.X == offsetX && kvp.Key.Offset.Y == offsetY)
                {
                    return kvp.Value.Tainted(x, y);
                }
            }
            return false;
        }

        public ITerrainChannel MakeCopy(IScene scene)
        {
            foreach (KeyValuePair<RegionData, ITerrainChannel> kvp in RegionConnections)
            {
                if (kvp.Key.RegionId == scene.RegionInfo.RegionID)
                {
                    return kvp.Value.MakeCopy(scene);
                }
            }
            return null;
        }

        public string SaveToXmlString(IScene scene)
        {
            foreach (KeyValuePair<RegionData, ITerrainChannel> kvp in RegionConnections)
            {
                if (kvp.Key.RegionId == scene.RegionInfo.RegionID)
                {
                    return kvp.Value.SaveToXmlString(scene);
                }
            }
            return null;
        }

        public void LoadFromXmlString(IScene scene, string data)
        {
            foreach (KeyValuePair<RegionData, ITerrainChannel> kvp in RegionConnections)
            {
                if (kvp.Key.RegionId == scene.RegionInfo.RegionID)
                {
                    kvp.Value.LoadFromXmlString(scene, data);
                }
            }
        }

        public float GetNormalizedGroundHeight(float x, float y)
        {
            if (x > 0 && x <= (int)Constants.RegionSize && y > 0 && y <= (int)Constants.RegionSize)
            {
                return RegionConnections[RegData].GetNormalizedGroundHeight(x, y);
            }
            else
            {
                int offsetX = (int)(x / (int)Constants.RegionSize);
                int offsetY = (int)(y / (int)Constants.RegionSize);
                offsetX *= (int)Constants.RegionSize;
                offsetY *= (int)Constants.RegionSize;

                foreach (RegionData regionData in RegionConnections.Keys)
                {
                    if (regionData.Offset.X == offsetX && regionData.Offset.Y == offsetY)
                    {
                        return RegionConnections[regionData].GetNormalizedGroundHeight(x - offsetX, y - offsetY);
                    }
                }

                return 0;
            }
        }
    }
}