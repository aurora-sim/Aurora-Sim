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
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Services.Connectors.SimianGrid
{
    /// <summary>
    ///   Connects region registration and neighbor lookups to the SimianGrid
    ///   backend
    /// </summary>
    public class SimianGridServiceConnector : IGridService, IService
    {
        private readonly Dictionary<UUID, IScene> m_scenes = new Dictionary<UUID, IScene>();
        private string m_serverUrl = String.Empty;

        public string Name
        {
            get { return GetType().Name; }
        }

        #region IGridService Members

        public int MaxRegionSize
        {
            get { return 0; }
        }

        public int RegionViewSize
        {
            get { return 256; }
        }

        public virtual IGridService InnerService
        {
            get { return this; }
        }

        public void Configure(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            CheckForScenes(registry);
        }

        public void FinishedStartup()
        {
        }

        public string UpdateMap(GridRegion region, UUID sessionID)
        {
            return RegisterRegion(region, UUID.Zero).Error;
        }

        public List<GridRegion> GetNeighbors(GridRegion r)
        {
            return new List<GridRegion>();
        }

        public multipleMapItemReply GetMapItems(ulong regionHandle, GridItemType gridItemType)
        {
            return new multipleMapItemReply();
        }

        public void SetRegionUnsafe(UUID r)
        {
        }

        public void SetRegionSafe(UUID regionID)
        {
        }

        public bool VerifyRegionSessionID(GridRegion r, UUID SessionID)
        {
            return r.SessionID == SessionID;
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("GridHandler", "") != Name)
                return;

            CommonInit(config);
            registry.RegisterModuleInterface<IGridService>(this);
        }

        #endregion

        protected void CheckForScenes(IRegistryCore registry)
        {
            //This is a dirty nasty hack of a way to pull the Scene out of an IRegistryCore interface
            // as this isn't a Scene module anymore. This is called by both AddNewRegistry and PostStart as
            // both of those pass the Scene down to register interfaces into.
            SceneManager manager = registry.RequestModuleInterface<SceneManager>();
            if (manager != null)
            {
                foreach (IScene scene in manager.Scenes)
                {
                    m_scenes[scene.RegionInfo.RegionID] = scene;
                }
            }
            if (registry is IScene)
            {
                IScene s = (IScene) registry;
                m_scenes[s.RegionInfo.RegionID] = s;
            }
        }

        private void CommonInit(IConfigSource source)
        {
            IConfig gridConfig = source.Configs["GridService"];
            if (gridConfig != null)
            {
                string serviceUrl = gridConfig.GetString("GridServerURI");
                if (!String.IsNullOrEmpty(serviceUrl))
                {
                    if (!serviceUrl.EndsWith("/") && !serviceUrl.EndsWith("="))
                        serviceUrl = serviceUrl + '/';
                    m_serverUrl = serviceUrl;
                }
            }

            if (String.IsNullOrEmpty(m_serverUrl))
                MainConsole.Instance.Info("[SIMIAN GRID CONNECTOR]: No GridServerURI specified, disabling connector");
        }

        private void UploadMapTile(IScene scene)
        {
            string errorMessage = null;

            // Create a PNG map tile and upload it to the AddMapTile API
            byte[] pngData = Utils.EmptyBytes;
            IMapImageGenerator tileGenerator = scene.RequestModuleInterface<IMapImageGenerator>();
            if (tileGenerator == null)
            {
                MainConsole.Instance.Warn("[SIMIAN GRID CONNECTOR]: Cannot upload PNG map tile without an IMapImageGenerator");
                return;
            }

            Bitmap mapTile, terrainTile;
            tileGenerator.CreateMapTile(out terrainTile, out mapTile);
            using (MemoryStream stream = new MemoryStream())
            {
                mapTile.Save(stream, ImageFormat.Png);
                pngData = stream.ToArray();
            }
            mapTile.Dispose();
            terrainTile.Dispose();

            List<MultipartForm.Element> postParameters = new List<MultipartForm.Element>
                                                             {
                                                                 new MultipartForm.Parameter("X",
                                                                                             (scene.RegionInfo.
                                                                                                  RegionLocX/
                                                                                              Constants.RegionSize).
                                                                                                 ToString()),
                                                                 new MultipartForm.Parameter("Y",
                                                                                             (scene.RegionInfo.
                                                                                                  RegionLocY/
                                                                                              Constants.RegionSize).
                                                                                                 ToString()),
                                                                 new MultipartForm.File("Tile", "tile.png", "image/png",
                                                                                        pngData)
                                                             };

            // Make the remote storage request
            try
            {
                HttpWebRequest request = (HttpWebRequest) WebRequest.Create(m_serverUrl);

                HttpWebResponse response = MultipartForm.Post(request, postParameters);
                using (Stream responseStream = response.GetResponseStream())
                {
                    string responseStr = null;

                    try
                    {
                        responseStr = responseStream.GetStreamString();
                        OSD responseOSD = OSDParser.Deserialize(responseStr);
                        if (responseOSD.Type == OSDType.Map)
                        {
                            OSDMap responseMap = (OSDMap) responseOSD;
                            if (responseMap["Success"].AsBoolean())
                                MainConsole.Instance.Info("[SIMIAN GRID CONNECTOR]: Uploaded " + pngData.Length +
                                           " byte PNG map tile to AddMapTile");
                            else
                                errorMessage = "Upload failed: " + responseMap["Message"].AsString();
                        }
                        else
                        {
                            errorMessage = "Response format was invalid:\n" + responseStr;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!String.IsNullOrEmpty(responseStr))
                            errorMessage = "Failed to parse the response:\n" + responseStr;
                        else
                            errorMessage = "Failed to retrieve the response: " + ex.Message;
                    }
                }
            }
            catch (WebException ex)
            {
                errorMessage = ex.Message;
            }

            if (!String.IsNullOrEmpty(errorMessage))
            {
                MainConsole.Instance.WarnFormat("[SIMIAN GRID CONNECTOR]: Failed to store {0} byte PNG map tile for {1}: {2}",
                                 pngData.Length, scene.RegionInfo.RegionName, errorMessage.Replace('\n', ' '));
            }
        }

        private GridRegion GetNearestRegion(Vector3d position, bool onlyEnabled)
        {
            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "GetScene"},
                                                      {"Position", position.ToString()},
                                                      {"FindClosest", "1"}
                                                  };
            if (onlyEnabled)
                requestArgs["Enabled"] = "1";

            OSDMap response = WebUtils.PostToService(m_serverUrl, requestArgs);
            if (response["Success"].AsBoolean())
            {
                return ResponseToGridRegion(response);
            }
            else
            {
                MainConsole.Instance.Warn("[SIMIAN GRID CONNECTOR]: Grid service did not find a match for region at " + position);
                return null;
            }
        }

        private GridRegion ResponseToGridRegion(OSDMap response)
        {
            if (response == null)
                return null;

            OSDMap extraData = response["ExtraData"] as OSDMap;
            if (extraData == null)
                return null;

            GridRegion region = new GridRegion
                                    {RegionID = response["SceneID"].AsUUID(), RegionName = response["Name"].AsString()};


            Vector3d minPosition = response["MinPosition"].AsVector3d();
            region.RegionLocX = (int) minPosition.X;
            region.RegionLocY = (int) minPosition.Y;

            Uri httpAddress = response["Address"].AsUri();
            region.ExternalHostName = httpAddress.Host;
            region.HttpPort = (uint) httpAddress.Port;

            region.ServerURI = extraData["ServerURI"].AsString();

            IPAddress internalAddress;
            IPAddress.TryParse(extraData["InternalAddress"].AsString(), out internalAddress);
            if (internalAddress == null)
                internalAddress = IPAddress.Any;

            region.InternalEndPoint = new IPEndPoint(internalAddress, extraData["InternalPort"].AsInteger());
            region.TerrainImage = extraData["MapTexture"].AsUUID();
            region.Access = (byte) extraData["Access"].AsInteger();
            region.EstateOwner = extraData["EstateOwner"].AsUUID();
            region.AuthToken = extraData["Token"].AsString();

            return region;
        }

        #region IGridService

        public RegisterRegion RegisterRegion(GridRegion regionInfo, UUID oldSessionID)
        {
            // Generate and upload our map tile in PNG format to the SimianGrid AddMapTile service
            IScene scene;
            if (m_scenes.TryGetValue(regionInfo.RegionID, out scene))
                UploadMapTile(scene);
            else
                MainConsole.Instance.Warn("Registering region " + regionInfo.RegionName + " (" + regionInfo.RegionID +
                           ") that we are not tracking");

            Vector3d minPosition = new Vector3d(regionInfo.RegionLocX, regionInfo.RegionLocY, 0.0);
            Vector3d maxPosition = minPosition + new Vector3d(Constants.RegionSize, Constants.RegionSize, 4096.0);

            string httpAddress = "http://" + regionInfo.ExternalHostName + ":" + regionInfo.HttpPort + "/";

            OSDMap extraData = new OSDMap
                                   {
                                       {"ServerURI", OSD.FromString(regionInfo.ServerURI)},
                                       {
                                           "InternalAddress",
                                           OSD.FromString(regionInfo.InternalEndPoint.Address.ToString())
                                           },
                                       {"InternalPort", OSD.FromInteger(regionInfo.InternalEndPoint.Port)},
                                       {
                                           "ExternalAddress",
                                           OSD.FromString(regionInfo.ExternalEndPoint.Address.ToString())
                                           },
                                       {"ExternalPort", OSD.FromInteger(regionInfo.ExternalEndPoint.Port)},
                                       {"MapTexture", OSD.FromUUID(regionInfo.TerrainMapImage)},
                                       {"Access", OSD.FromInteger(regionInfo.Access)},
                                       {"EstateOwner", OSD.FromUUID(regionInfo.EstateOwner)},
                                       {"Token", OSD.FromString(regionInfo.AuthToken)}
                                   };

            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "AddScene"},
                                                      {"SceneID", regionInfo.RegionID.ToString()},
                                                      {"Name", regionInfo.RegionName},
                                                      {"MinPosition", minPosition.ToString()},
                                                      {"MaxPosition", maxPosition.ToString()},
                                                      {"Address", httpAddress},
                                                      {"Enabled", "1"},
                                                      {"ExtraData", OSDParser.SerializeJsonString(extraData)}
                                                  };

            OSDMap response = WebUtils.PostToService(m_serverUrl, requestArgs);
            if (response["Success"].AsBoolean())
                return new RegisterRegion() { Neighbors = GetNeighbors(regionInfo) };
            else
                return new RegisterRegion() { Error = "Region registration for " + regionInfo.RegionName + " failed: " + response["Message"].AsString() };
        }

        public bool DeregisterRegion(GridRegion region)
        {
            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "AddScene"},
                                                      {"SceneID", region.RegionID.ToString()},
                                                      {"Enabled", "0"}
                                                  };

            OSDMap response = WebUtils.PostToService(m_serverUrl, requestArgs);
            bool success = response["Success"].AsBoolean();

            if (!success)
                MainConsole.Instance.Warn("[SIMIAN GRID CONNECTOR]: Region deregistration for " + region.RegionID + " failed: " +
                           response["Message"].AsString());

            return success;
        }

        public GridRegion GetRegionByUUID(UUID scopeID, UUID regionID)
        {
            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "GetScene"},
                                                      {"SceneID", regionID.ToString()}
                                                  };

            OSDMap response = WebUtils.PostToService(m_serverUrl, requestArgs);
            if (response["Success"].AsBoolean())
            {
                return ResponseToGridRegion(response);
            }
            else
            {
                MainConsole.Instance.Warn("[SIMIAN GRID CONNECTOR]: Grid service did not find a match for region " + regionID);
                return null;
            }
        }

        public GridRegion GetRegionByPosition(UUID scopeID, int x, int y)
        {
            // Go one meter in from the requested x/y coords to avoid requesting a position
            // that falls on the border of two sims
            Vector3d position = new Vector3d(x + 1, y + 1, 0.0);

            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "GetScene"},
                                                      {"Position", position.ToString()},
                                                      {"Enabled", "1"}
                                                  };

            OSDMap response = WebUtils.PostToService(m_serverUrl, requestArgs);
            if (response["Success"].AsBoolean())
            {
                return ResponseToGridRegion(response);
            }
            else
            {
                //MainConsole.Instance.InfoFormat("[SIMIAN GRID CONNECTOR]: Grid service did not find a match for region at {0},{1}",
                //    x / Constants.RegionSize, y / Constants.RegionSize);
                return null;
            }
        }

        public GridRegion GetRegionByName(UUID scopeID, string regionName)
        {
            List<GridRegion> regions = GetRegionsByName(scopeID, regionName, 1);

            MainConsole.Instance.Debug("[SIMIAN GRID CONNECTOR]: Got " + regions.Count + " matches for region name " + regionName);

            if (regions.Count > 0)
                return regions[0];

            return null;
        }

        public List<GridRegion> GetRegionsByName(UUID scopeID, string name, int maxNumber)
        {
            List<GridRegion> foundRegions = new List<GridRegion>();

            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "GetScenes"},
                                                      {"NameQuery", name},
                                                      {"Enabled", "1"}
                                                  };
            if (maxNumber > 0)
                requestArgs["MaxNumber"] = maxNumber.ToString();

            OSDMap response = WebUtils.PostToService(m_serverUrl, requestArgs);
            if (response["Success"].AsBoolean())
            {
                OSDArray array = response["Scenes"] as OSDArray;
                if (array != null)
                {
#if (!ISWIN)
                    for (int i = 0; i < array.Count; i++)
                    {
                        GridRegion region = ResponseToGridRegion(array[i] as OSDMap);
                        if (region != null)
                            foundRegions.Add(region);
                    }
#else
                    foundRegions.AddRange(array.Select(t => ResponseToGridRegion(t as OSDMap)).Where(region => region != null));
#endif
                }
            }

            return foundRegions;
        }

        public List<GridRegion> GetRegionRange(UUID scopeID, int xmin, int xmax, int ymin, int ymax)
        {
            List<GridRegion> foundRegions = new List<GridRegion>();

            Vector3d minPosition = new Vector3d(xmin, ymin, 0.0);
            Vector3d maxPosition = new Vector3d(xmax, ymax, 4096.0);

            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "GetScenes"},
                                                      {"MinPosition", minPosition.ToString()},
                                                      {"MaxPosition", maxPosition.ToString()},
                                                      {"Enabled", "1"}
                                                  };

            OSDMap response = WebUtils.PostToService(m_serverUrl, requestArgs);
            if (response["Success"].AsBoolean())
            {
                OSDArray array = response["Scenes"] as OSDArray;
                if (array != null)
                {
#if (!ISWIN)
                    for (int i = 0; i < array.Count; i++)
                    {
                        GridRegion region = ResponseToGridRegion(array[i] as OSDMap);
                        if (region != null)
                            foundRegions.Add(region);
                    }
#else
                    foundRegions.AddRange(array.Select(t => ResponseToGridRegion(t as OSDMap)).Where(region => region != null));
#endif
                }
            }

            return foundRegions;
        }

        public List<GridRegion> GetDefaultRegions(UUID scopeID)
        {
            // TODO: Allow specifying the default grid location
            const int DEFAULT_X = 1000*256;
            const int DEFAULT_Y = 1000*256;

            GridRegion defRegion = GetNearestRegion(new Vector3d(DEFAULT_X, DEFAULT_Y, 0.0), true);
            if (defRegion != null)
                return new List<GridRegion>(1) {defRegion};
            else
                return new List<GridRegion>(0);
        }

        public List<GridRegion> GetFallbackRegions(UUID scopeID, int x, int y)
        {
            GridRegion defRegion = GetNearestRegion(new Vector3d(x, y, 0.0), true);
            if (defRegion != null)
                return new List<GridRegion>(1) {defRegion};
            else
                return new List<GridRegion>(0);
        }

        public List<GridRegion> GetSafeRegions(UUID scopeID, int x, int y)
        {
            return new List<GridRegion>(0);
        }

        public int GetRegionFlags(UUID scopeID, UUID regionID)
        {
            const int REGION_ONLINE = 4;

            NameValueCollection requestArgs = new NameValueCollection
                                                  {
                                                      {"RequestMethod", "GetScene"},
                                                      {"SceneID", regionID.ToString()}
                                                  };

            OSDMap response = WebUtils.PostToService(m_serverUrl, requestArgs);
            if (response["Success"].AsBoolean())
            {
                return response["Enabled"].AsBoolean() ? REGION_ONLINE : 0;
            }
            else
            {
                MainConsole.Instance.Warn("[SIMIAN GRID CONNECTOR]: Grid service did not find a match for region " + regionID +
                           " during region flags check");
                return -1;
            }
        }

        #endregion IGridService
    }
}