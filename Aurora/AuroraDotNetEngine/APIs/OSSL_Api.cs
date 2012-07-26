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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Text.RegularExpressions;
using Aurora.Framework;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using Group = System.Text.RegularExpressions.Group;
using LSL_Float = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLFloat;
using LSL_Integer = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLInteger;
using LSL_Key = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_List = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.list;
using LSL_Rotation = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Quaternion;
using LSL_String = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_Vector = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Vector3;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.APIs
{
    [Serializable]
    public class OSSL_Api : MarshalByRefObject, IOSSL_Api, IScriptApi
    {
        internal ScriptProtectionModule ScriptProtection;
        internal ILSL_Api m_LSL_Api; // get a reference to the LSL API so we can call methods housed there
        internal bool m_OSFunctionsEnabled;
        internal float m_ScriptDelayFactor = 1.0f;
        internal float m_ScriptDistanceFactor = 1.0f;
        internal IScriptModulePlugin m_ScriptEngine;
        internal ISceneChildEntity m_host;
        internal UUID m_itemID;
        internal uint m_localID;
        public IScene World
        {
            get { return m_host.ParentEntity.Scene; }
        }

        //
        // OpenSim functions
        //

        #region IOSSL_Api Members

        public LSL_Integer osSetTerrainHeight(int x, int y, double val)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osTerrainSetHeight", m_host, "OSSL", m_itemID))
                return new LSL_Integer();

            if (x > (World.RegionInfo.RegionSizeX - 1) || x < 0 || y > (World.RegionInfo.RegionSizeY - 1) || y < 0)
                OSSLError("osTerrainSetHeight: Coordinate out of bounds");

            if (World.Permissions.CanTerraformLand(m_host.OwnerID, new Vector3(x, y, 0)))
            {
                ITerrainChannel heightmap = World.RequestModuleInterface<ITerrainChannel>();
                heightmap[x, y] = (float)val;
                ITerrainModule terrainModule = World.RequestModuleInterface<ITerrainModule>();
                if (terrainModule != null) terrainModule.TaintTerrain();
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public LSL_Float osGetTerrainHeight(int x, int y)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osTerrainGetHeight", m_host, "OSSL", m_itemID))
                return new LSL_Float();


            if (x > (World.RegionInfo.RegionSizeX - 1) || x < 0 || y > (World.RegionInfo.RegionSizeY - 1) || y < 0)
                OSSLError("osTerrainGetHeight: Coordinate out of bounds");

            ITerrainChannel heightmap = World.RequestModuleInterface<ITerrainChannel>();
            return heightmap[x, y];
        }

        public void osTerrainFlush()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.VeryLow, "osTerrainFlush", m_host, "OSSL", m_itemID))
                return;

            ITerrainModule terrainModule = World.RequestModuleInterface<ITerrainModule>();
            if (terrainModule != null) terrainModule.TaintTerrain();
        }

        public int osRegionRestart(double seconds)
        {
            // This is High here because region restart is not reliable
            // it may result in the region staying down or becoming
            // unstable. This should be changed to Low or VeryLow once
            // The underlying functionality is fixed, since the security
            // as such is sound
            //
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osRegionRestart", m_host, "OSSL", m_itemID))
                return new int();

            IRestartModule restartModule = World.RequestModuleInterface<IRestartModule>();
            if (World.Permissions.CanIssueEstateCommand(m_host.OwnerID, false) && (restartModule != null))
            {
                if (seconds < 15)
                {
                    restartModule.AbortRestart("Restart aborted");
                    return 1;
                }

                List<int> times = new List<int>();
                while (seconds > 0)
                {
                    times.Add((int)seconds);
                    if (seconds > 300)
                        seconds -= 120;
                    else if (seconds > 30)
                        seconds -= 30;
                    else
                        seconds -= 15;
                }

                restartModule.ScheduleRestart(UUID.Zero, "Region will restart in {0}", times.ToArray(), true);
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public void osShutDown()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osShutDown", m_host, "OSSL", m_itemID)) return;

            if (World.Permissions.CanIssueEstateCommand(m_host.OwnerID, false))
            {
                MainConsole.Instance.RunCommand("shutdown");
            }
        }

        public void osReturnObjects(LSL_Float Parameter)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "osReturnObjects", m_host, "OSSL", m_itemID))
                return;

            Dictionary<UUID, List<ISceneEntity>> returns =
                new Dictionary<UUID, List<ISceneEntity>>();
            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                ILandObject LO = parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y);
                IPrimCountModule primCountModule = World.RequestModuleInterface<IPrimCountModule>();
                IPrimCounts primCounts = primCountModule.GetPrimCounts(LO.LandData.GlobalID);
                if (Parameter == 0) // Owner objects
                {
#if (!ISWIN)
                    foreach (ISceneEntity obj in primCounts.Objects)
                    {
                        if (obj.OwnerID == LO.LandData.OwnerID)
                        {
                            if (!returns.ContainsKey(obj.OwnerID))
                                returns[obj.OwnerID] = new List<ISceneEntity>();

                            returns[obj.OwnerID].Add(obj);
                        }
                    }
#else
                    foreach (ISceneEntity obj in primCounts.Objects.Where(obj => obj.OwnerID == LO.LandData.OwnerID))
                    {
                        if (!returns.ContainsKey(obj.OwnerID))
                            returns[obj.OwnerID] =
                                new List<ISceneEntity>();

                        returns[obj.OwnerID].Add(obj);
                    }
#endif
                }
                if (Parameter == 1) //Everyone elses
                {
#if (!ISWIN)
                    foreach (ISceneEntity obj in primCounts.Objects)
                    {
                        if (obj.OwnerID != LO.LandData.OwnerID && (obj.GroupID != LO.LandData.GroupID || LO.LandData.GroupID == UUID.Zero))
                        {
                            if (!returns.ContainsKey(obj.OwnerID))
                                returns[obj.OwnerID] = new List<ISceneEntity>();

                            returns[obj.OwnerID].Add(obj);
                        }
                    }
#else
                    foreach (ISceneEntity obj in primCounts.Objects.Where(obj => obj.OwnerID != LO.LandData.OwnerID &&
                                                                                 (obj.GroupID != LO.LandData.GroupID ||
                                                                                  LO.LandData.GroupID == UUID.Zero)))
                    {
                        if (!returns.ContainsKey(obj.OwnerID))
                            returns[obj.OwnerID] =
                                new List<ISceneEntity>();

                        returns[obj.OwnerID].Add(obj);
                    }
#endif
                }
                if (Parameter == 2) // Group
                {
#if (!ISWIN)
                    foreach (ISceneEntity obj in primCounts.Objects)
                    {
                        if (obj.GroupID == LO.LandData.GroupID)
                        {
                            if (!returns.ContainsKey(obj.OwnerID))
                                returns[obj.OwnerID] = new List<ISceneEntity>();

                            returns[obj.OwnerID].Add(obj);
                        }
                    }
#else
                    foreach (ISceneEntity obj in primCounts.Objects.Where(obj => obj.GroupID == LO.LandData.GroupID))
                    {
                        if (!returns.ContainsKey(obj.OwnerID))
                            returns[obj.OwnerID] =
                                new List<ISceneEntity>();

                        returns[obj.OwnerID].Add(obj);
                    }
#endif
                }

                foreach (List<ISceneEntity> ol in returns.Values)
                {
                    if (World.Permissions.CanReturnObjects(LO, m_host.OwnerID, ol))
                    {
                        ILLClientInventory inventoryModule = World.RequestModuleInterface<ILLClientInventory>();
                        if (inventoryModule != null)
                            inventoryModule.ReturnObjects(ol.ToArray(), m_host.OwnerID);
                    }
                }
            }
        }

        public void osReturnObject(LSL_Key userID)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "osReturnObjects", m_host, "OSSL", m_itemID))
                return;

            Dictionary<UUID, List<ISceneEntity>> returns =
                new Dictionary<UUID, List<ISceneEntity>>();
            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                ILandObject LO = parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y);

                IPrimCountModule primCountModule = World.RequestModuleInterface<IPrimCountModule>();
                IPrimCounts primCounts = primCountModule.GetPrimCounts(LO.LandData.GlobalID);
#if (!ISWIN)
                foreach (ISceneEntity obj in primCounts.Objects)
                {
                    if (obj.OwnerID == new UUID(userID.m_string))
                    {
                        if (!returns.ContainsKey(obj.OwnerID))
                            returns[obj.OwnerID] = new List<ISceneEntity>();

                        returns[obj.OwnerID].Add(obj);
                    }
                }
#else
                foreach (ISceneEntity obj in primCounts.Objects.Where(obj => obj.OwnerID == new UUID(userID.m_string)))
                {
                    if (!returns.ContainsKey(obj.OwnerID))
                        returns[obj.OwnerID] =
                            new List<ISceneEntity>();

                    returns[obj.OwnerID].Add(obj);
                }
#endif

                foreach (List<ISceneEntity> ol in returns.Values)
                {
                    if (World.Permissions.CanReturnObjects(LO, m_host.OwnerID, ol))
                    {
                        ILLClientInventory inventoryModule = World.RequestModuleInterface<ILLClientInventory>();
                        if (inventoryModule != null)
                            inventoryModule.ReturnObjects(ol.ToArray(), m_host.OwnerID);
                    }
                }
            }
        }

        public void osRegionNotice(string msg)
        {
            // This implementation provides absolutely no security
            // It's high griefing potential makes this classification
            // necessary
            //
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.VeryHigh, "osRegionNotice", m_host, "OSSL", m_itemID))
                return;


            IDialogModule dm = World.RequestModuleInterface<IDialogModule>();

            if (dm != null)
                dm.SendGeneralAlert(msg);
        }

        public string osSetDynamicTextureURL(string dynamicID, string contentType, string url, string extraParams,
                                             int timer)
        {
            // This may be upgraded depending on the griefing or DOS
            // potential, or guarded with a delay
            //
            if (
                !ScriptProtection.CheckThreatLevel(ThreatLevel.VeryLow, "osSetDynamicTextureURL", m_host, "OSSL",
                                                   m_itemID)) return "";


            IDynamicTextureManager textureManager = World.RequestModuleInterface<IDynamicTextureManager>();
            if (dynamicID == String.Empty)
            {
                UUID createdTexture =
                    textureManager.AddDynamicTextureURL(World.RegionInfo.RegionID, m_host.UUID, UUID.Zero, contentType,
                                                        url,
                                                        extraParams, timer);
                return createdTexture.ToString();
            }
            else
            {
                UUID oldAssetID = UUID.Parse(dynamicID);
                UUID createdTexture =
                    textureManager.AddDynamicTextureURL(World.RegionInfo.RegionID, m_host.UUID, oldAssetID, contentType,
                                                        url,
                                                        extraParams, timer);
                return createdTexture.ToString();
            }
        }

        public string osSetDynamicTextureURLBlend(string dynamicID, string contentType, string url, string extraParams,
                                                  int timer, int alpha)
        {
            if (
                !ScriptProtection.CheckThreatLevel(ThreatLevel.VeryLow, "osSetDynamicTextureURLBlend", m_host, "OSSL",
                                                   m_itemID)) return "";


            IDynamicTextureManager textureManager = World.RequestModuleInterface<IDynamicTextureManager>();
            if (dynamicID == String.Empty)
            {
                UUID createdTexture =
                    textureManager.AddDynamicTextureURL(World.RegionInfo.RegionID, m_host.UUID, UUID.Zero, contentType,
                                                        url,
                                                        extraParams, timer, true, (byte)alpha);
                return createdTexture.ToString();
            }
            else
            {
                UUID oldAssetID = UUID.Parse(dynamicID);
                UUID createdTexture =
                    textureManager.AddDynamicTextureURL(World.RegionInfo.RegionID, m_host.UUID, oldAssetID, contentType,
                                                        url,
                                                        extraParams, timer, true, (byte)alpha);
                return createdTexture.ToString();
            }
        }

        public string osSetDynamicTextureURLBlendFace(string dynamicID, string contentType, string url,
                                                      string extraParams,
                                                      bool blend, int disp, int timer, int alpha, int face)
        {
            if (
                !ScriptProtection.CheckThreatLevel(ThreatLevel.VeryLow, "osSetDynamicTextureURLBlendFace", m_host,
                                                   "OSSL", m_itemID)) return "";


            IDynamicTextureManager textureManager = World.RequestModuleInterface<IDynamicTextureManager>();
            if (dynamicID == String.Empty)
            {
                UUID createdTexture =
                    textureManager.AddDynamicTextureURL(World.RegionInfo.RegionID, m_host.UUID, UUID.Zero, contentType,
                                                        url,
                                                        extraParams, timer, blend, disp, (byte)alpha, face);
                return createdTexture.ToString();
            }
            else
            {
                UUID oldAssetID = UUID.Parse(dynamicID);
                UUID createdTexture =
                    textureManager.AddDynamicTextureURL(World.RegionInfo.RegionID, m_host.UUID, oldAssetID, contentType,
                                                        url,
                                                        extraParams, timer, blend, disp, (byte)alpha, face);
                return createdTexture.ToString();
            }
        }

        public string osSetDynamicTextureData(string dynamicID, string contentType, string data, string extraParams,
                                              int timer)
        {
            if (
                !ScriptProtection.CheckThreatLevel(ThreatLevel.VeryLow, "osSetDynamicTextureData", m_host, "OSSL",
                                                   m_itemID)) return "";


            IDynamicTextureManager textureManager = World.RequestModuleInterface<IDynamicTextureManager>();
            if (textureManager != null)
            {
                if (extraParams == String.Empty)
                {
                    extraParams = "256";
                }
                if (dynamicID == String.Empty)
                {
                    UUID createdTexture =
                        textureManager.AddDynamicTextureData(World.RegionInfo.RegionID, m_host.UUID, UUID.Zero,
                                                             contentType, data,
                                                             extraParams, timer);
                    return createdTexture.ToString();
                }
                else
                {
                    UUID oldAssetID = UUID.Parse(dynamicID);
                    UUID createdTexture =
                        textureManager.AddDynamicTextureData(World.RegionInfo.RegionID, m_host.UUID, oldAssetID,
                                                             contentType, data,
                                                             extraParams, timer);
                    return createdTexture.ToString();
                }
            }

            return UUID.Zero.ToString();
        }

        public string osSetDynamicTextureDataBlend(string dynamicID, string contentType, string data, string extraParams,
                                                   int timer, int alpha)
        {
            if (
                !ScriptProtection.CheckThreatLevel(ThreatLevel.VeryLow, "osSetDynamicTextureDataBlend", m_host, "OSSL",
                                                   m_itemID)) return "";


            IDynamicTextureManager textureManager = World.RequestModuleInterface<IDynamicTextureManager>();
            if (textureManager != null)
            {
                if (extraParams == String.Empty)
                {
                    extraParams = "256";
                }
                if (dynamicID == String.Empty)
                {
                    UUID createdTexture =
                        textureManager.AddDynamicTextureData(World.RegionInfo.RegionID, m_host.UUID, UUID.Zero,
                                                             contentType, data,
                                                             extraParams, timer, true, (byte)alpha);
                    return createdTexture.ToString();
                }
                else
                {
                    UUID oldAssetID = UUID.Parse(dynamicID);
                    UUID createdTexture =
                        textureManager.AddDynamicTextureData(World.RegionInfo.RegionID, m_host.UUID, oldAssetID,
                                                             contentType, data,
                                                             extraParams, timer, true, (byte)alpha);
                    return createdTexture.ToString();
                }
            }

            return UUID.Zero.ToString();
        }

        private enum InfoType
        {
            Nick,
            Name,
            Login,
            Home,
            Custom
        };

        private string GridUserInfo(InfoType type)
        {
            return GridUserInfo(type, "");
        }

        private string GridUserInfo(InfoType type, string key)
        {
            string retval = String.Empty;
            IConfigSource config = m_ScriptEngine.ConfigSource;
            string url = config.Configs["GridInfo"].GetString("GridInfoURI", String.Empty);

            if (String.IsNullOrEmpty(url))
                return "Configuration Error!";

            string verb = "/json_grid_info";
            OSDMap json = new OSDMap();

            OSDMap info = (OSDMap) Util.CombineParams(new[] {String.Format("{0}{1}", url, verb)}, 3000);

            if (info["Success"] != true)
                return "Get GridInfo Failed!";

            json = (OSDMap)OSDParser.DeserializeJson(info["_RawResult"].AsString());

            switch (type)
            {
                case InfoType.Nick:
                    retval = json["gridnick"];
                    break;

                case InfoType.Name:
                    retval = json["gridname"];
                    break;

                case InfoType.Login:
                    retval = json["login"];
                    break;

                case InfoType.Home:
                    retval = json["home"];
                    break;

                case InfoType.Custom:
                    retval = json[key];
                    break;

                default:
                    retval = "error";
                    break;
            }

            return retval;
        }

        public string osGetGridHomeURI() //patched from OpenSim, you can remove this comment after pull
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osGetGridHomeURI", m_host, "OSSL",
                                                   m_itemID)) return "";

            string HomeURI = String.Empty;

            if (m_ScriptEngine.Config.GetString("GridInfoService") != null)
                HomeURI = MainServer.Instance.ServerURI + "/";

            return HomeURI;
        }

        public string osGetGridCustom(string key) //patched from OpenSim, you can remove this comment after pull
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osGetGridCustom", m_host, "OSSL",
                                                   m_itemID)) return "";

            string retval = String.Empty;

            if (m_ScriptEngine.Config.GetString("GridInfoService") != null)
                retval = m_ScriptEngine.Config.GetString("gridnick", retval);

            return retval;
        }

        public string osGetThreatLevel(string key)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osGetThreatLevel", m_host, "OSSL",
                                                   m_itemID)) return "";

            string retval = String.Empty;

            if (m_ScriptEngine.Config.GetString("AllowedAPIs").Contains("os"))
                retval = m_ScriptEngine.Config.GetString("FunctionThreatLevel", retval);

            return retval;
        }

        public string osGetGridGatekeeperURI() //patched from OpenSim, you can remove this comment after pull
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osGetGridGatekeeperURI", m_host, "OSSL", m_itemID)) return "";
            string gatekeeperURI = String.Empty;
            IConfigSource config = m_ScriptEngine.ConfigSource;
            if (config.Configs["GridService"] != null)
               gatekeeperURI = MainServer.Instance.ServerURI + "/"; 
               return gatekeeperURI;
        }
 
        public void osForceAttachToAvatar(int attachmentPoint)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osForceAttachToAvatar", m_host, "OSSL", m_itemID)) return;
            InitLSL();
            ((LSL_Api)m_LSL_Api).AttachToAvatar(attachmentPoint);
        }

        public void osForceDetachFromAvatar()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osForceDetachFromAvatar", m_host, "OSSL", m_itemID)) return;
            InitLSL();
            ((LSL_Api)m_LSL_Api).DetachFromAvatar();
        }

        public string osSetDynamicTextureDataBlendFace(string dynamicID, string contentType, string data,
                                                       string extraParams,
                                                       bool blend, int disp, int timer, int alpha, int face)
        {
            if (
                !ScriptProtection.CheckThreatLevel(ThreatLevel.VeryLow, "osSetDynamicTextureDataBlendFace", m_host,
                                                   "OSSL", m_itemID)) return "";


            IDynamicTextureManager textureManager = World.RequestModuleInterface<IDynamicTextureManager>();
            if (textureManager != null)
            {
                if (extraParams == String.Empty)
                {
                    extraParams = "256";
                }
                if (dynamicID == String.Empty)
                {
                    UUID createdTexture =
                        textureManager.AddDynamicTextureData(World.RegionInfo.RegionID, m_host.UUID, UUID.Zero,
                                                             contentType, data,
                                                             extraParams, timer, blend, disp, (byte)alpha, face);
                    return createdTexture.ToString();
                }
                else
                {
                    UUID oldAssetID = UUID.Parse(dynamicID);
                    UUID createdTexture =
                        textureManager.AddDynamicTextureData(World.RegionInfo.RegionID, m_host.UUID, oldAssetID,
                                                             contentType, data,
                                                             extraParams, timer, blend, disp, (byte)alpha, face);
                    return createdTexture.ToString();
                }
            }

            return UUID.Zero.ToString();
        }

        public bool osConsoleCommand(string command)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Severe, "osConsoleCommand", m_host, "OSSL", m_itemID))
                return false;


            if (m_ScriptEngine.Config.GetBoolean("AllowosConsoleCommand", false))
            {
                if (World.Permissions.CanRunConsoleCommand(m_host.OwnerID))
                {
                    MainConsole.Instance.RunCommand(command);
                    return true;
                }
            }
            return false;
        }

        public void osSetPrimFloatOnWater(int floatYN)
        {
            if (
                !ScriptProtection.CheckThreatLevel(ThreatLevel.VeryLow, "osSetPrimFloatOnWater", m_host, "OSSL",
                                                   m_itemID)) return;

            if (m_host.ParentEntity != null)
            {
                if (m_host.ParentEntity.RootChild != null)
                {
                    m_host.ParentEntity.RootChild.SetFloatOnWater(floatYN);
                }
            }
        }

        public DateTime osTeleportOwner(string regionName, LSL_Vector position, LSL_Vector lookat)
        {
            // Threat level None because this is what can already be done with the World Map in the viewer
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osTeleportOwner", m_host, "OSSL", m_itemID))
                return DateTime.Now;

            List<GridRegion> regions = World.GridService.GetRegionsByName(World.RegionInfo.AllScopeIDs, regionName, 0, 1);
            // Try to link the region
            if (regions != null && regions.Count > 0)
            {
                GridRegion regInfo = regions[0];

                ulong regionHandle = regInfo.RegionHandle;
                return TeleportAgent(m_host.OwnerID, regionHandle,
                                     new Vector3((float)position.x, (float)position.y, (float)position.z),
                                     new Vector3((float)lookat.x, (float)lookat.y, (float)lookat.z));
            }
            return DateTime.Now;
        }

        public DateTime osTeleportOwner(LSL_Vector position, LSL_Vector lookat)
        {
            return osTeleportOwner(World.RegionInfo.RegionName, position, lookat);
        }

        public DateTime osTeleportOwner(int regionX, int regionY, LSL_Vector position, LSL_Vector lookat)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osTeleportOwner", m_host, "OSSL", m_itemID))
                return DateTime.Now;

            GridRegion regInfo = World.GridService.GetRegionByPosition(World.RegionInfo.AllScopeIDs,
                                                                       (regionX * Constants.RegionSize),
                                                                       (regionY * Constants.RegionSize));
            // Try to link the region
            if (regInfo != null)
            {
                ulong regionHandle = regInfo.RegionHandle;
                return TeleportAgent(m_host.OwnerID, regionHandle,
                                     new Vector3((float)position.x, (float)position.y, (float)position.z),
                                     new Vector3((float)lookat.x, (float)lookat.y, (float)lookat.z));
            }
            return DateTime.Now;
        }

        // Teleport functions
        public DateTime osTeleportAgent(string agent, string regionName, LSL_Vector position, LSL_Vector lookat)
        {
            // High because there is no security check. High griefer potential
            //
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osTeleportAgent", m_host, "OSSL", m_itemID))
                return DateTime.Now;

            UUID AgentID;
            if (UUID.TryParse(agent, out AgentID))
            {
                List<GridRegion> regions = World.GridService.GetRegionsByName(World.RegionInfo.AllScopeIDs, regionName, 0, 1);
                // Try to link the region
                if (regions != null && regions.Count > 0)
                {
                    GridRegion regInfo = regions[0];

                    ulong regionHandle = regInfo.RegionHandle;
                    return TeleportAgent(AgentID, regionHandle,
                                         new Vector3((float)position.x, (float)position.y, (float)position.z),
                                         new Vector3((float)lookat.x, (float)lookat.y, (float)lookat.z));
                }
            }
            return DateTime.Now;
        }

        // Teleport functions
        public DateTime osTeleportAgent(string agent, int regionX, int regionY, LSL_Vector position, LSL_Vector lookat)
        {
            // High because there is no security check. High griefer potential
            //
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osTeleportAgent", m_host, "OSSL", m_itemID))
                return DateTime.Now;

            ulong regionHandle = Utils.UIntsToLong(((uint)regionX * Constants.RegionSize),
                                                   ((uint)regionY * Constants.RegionSize));


            UUID agentId = new UUID();
            if (UUID.TryParse(agent, out agentId))
            {
                return TeleportAgent(agentId, regionHandle,
                                     new Vector3((float)position.x, (float)position.y, (float)position.z),
                                     new Vector3((float)lookat.x, (float)lookat.y, (float)lookat.z));
            }
            return DateTime.Now;
        }

        public DateTime osTeleportAgent(string agent, LSL_Vector position, LSL_Vector lookat)
        {
            return osTeleportAgent(agent, World.RegionInfo.RegionName, position, lookat);
        }

        // Functions that get information from the agent itself.
        //
        // osGetAgentIP - this is used to determine the IP address of
        //the client.  This is needed to help configure other in world
        //resources based on the IP address of the clients connected.
        //I think High is a good risk level for this, as it is an
        //information leak.
        public LSL_String osGetAgentIP(string agent)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osGetAgentIP", m_host, "OSSL", m_itemID))
                return new LSL_String();

            UUID avatarID = (UUID)agent;

            IScenePresence target;
            if (World.TryGetScenePresence(avatarID, out target))
            {
                EndPoint ep = target.ControllingClient.GetClientEP();
                if (ep is IPEndPoint)
                {
                    IPEndPoint ip = (IPEndPoint)ep;
                    return new LSL_String(ip.Address.ToString());
                }
            }
            // fall through case, just return nothing
            return new LSL_String("");
        }

        // Get a list of all the avatars/agents in the region
        public LSL_List osGetAgents()
        {
            // threat level is None as we could get this information with an
            // in-world script as well, just not as efficient
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osGetAgents", m_host, "OSSL", m_itemID))
                return new LSL_List();

            LSL_List result = new LSL_List();
            World.ForEachScenePresence(delegate(IScenePresence sp)
            {
                if (!sp.IsChildAgent)
                    result.Add(new LSL_String(sp.Name));
            });
            return result;
        }

        // Adam's super super custom animation functions
        public void osAvatarPlayAnimation(string avatar, string animation)
        {
            if (
                !ScriptProtection.CheckThreatLevel(ThreatLevel.VeryHigh, "osAvatarPlayAnimation", m_host, "OSSL",
                                                   m_itemID)) return;

            UUID avatarID = (UUID)avatar;


            IScenePresence target;
            if (World.TryGetScenePresence(avatarID, out target))
            {
                if (target != null)
                {
                    UUID animID = new UUID();
                    if (!UUID.TryParse(animation, out animID))
                    {

                        animID = UUID.Zero;
                        lock (m_host.TaskInventory)
                        {
#if (!ISWIN)
                            foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory)
                            {
                                if (inv.Value.Name == animation)
 	                            {
 	                               if (inv.Value.Type == (int)AssetType.Animation)
 	                                   animID = inv.Value.AssetID;
 	                               continue;
 	                            }
                            }

#else
                            foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory.Where(inv => inv.Value.Name == animation))
                            {
                                if (inv.Value.Type == (int)AssetType.Animation)
                                    animID = inv.Value.AssetID;
                                continue;
                            }
#endif
                        }
                    }

                    if (animID == UUID.Zero)
                        target.Animator.AddAnimation(animation, m_host.UUID);
                    else
                        target.Animator.AddAnimation(animID, m_host.UUID);
                }
            }
        }

        public void osAvatarStopAnimation(string avatar, string animation)
        {
            if (
                !ScriptProtection.CheckThreatLevel(ThreatLevel.VeryHigh, "osAvatarStopAnimation", m_host, "OSSL",
                                                   m_itemID)) return;

            UUID avatarID = (UUID)avatar;


            IScenePresence target;
            if (World.TryGetScenePresence(avatarID, out target))
            {
                if (target != null)
                {
                    UUID animID = UUID.Zero;
                    lock (m_host.TaskInventory)
                    {
#if (!ISWIN)
                        foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory)
                        {
                            if (inv.Value.Name == animation)
                            {
                                if (inv.Value.Type == (int)AssetType.Animation)
                                    animID = inv.Value.AssetID;
                                continue;
                            }
                        }
#else
                        foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory.Where(inv => inv.Value.Name == animation))
                        {
                            if (inv.Value.Type == (int) AssetType.Animation)
                                animID = inv.Value.AssetID;
                            continue;
                        }
#endif
                    }

                    if (animID == UUID.Zero)
                        target.Animator.RemoveAnimation(animation);
                    else
                        target.Animator.RemoveAnimation(animID);
                }
            }
        }

        //Texture draw functions
        public string osMovePen(string drawList, int x, int y)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osMovePen", m_host, "OSSL", m_itemID)) return "";


            drawList += "MoveTo " + x + "," + y + ";";
            return new LSL_String(drawList);
        }

        public string osDrawLine(string drawList, int startX, int startY, int endX, int endY)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osDrawLine", m_host, "OSSL", m_itemID)) return "";


            drawList += "MoveTo " + startX + "," + startY + "; LineTo " + endX + "," + endY + "; ";
            return new LSL_String(drawList);
        }

        public string osDrawLine(string drawList, int endX, int endY)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osDrawLine", m_host, "OSSL", m_itemID)) return "";


            drawList += "LineTo " + endX + "," + endY + "; ";
            return new LSL_String(drawList);
        }

        public string osDrawText(string drawList, string text)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osDrawText", m_host, "OSSL", m_itemID)) return "";


            drawList += "Text " + text + "; ";
            return new LSL_String(drawList);
        }

        public string osDrawEllipse(string drawList, int width, int height)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osDrawEllipse", m_host, "OSSL", m_itemID))
                return "";


            drawList += "Ellipse " + width + "," + height + "; ";
            return new LSL_String(drawList);
        }

        public string osDrawRectangle(string drawList, int width, int height)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osDrawRectangle", m_host, "OSSL", m_itemID))
                return "";


            drawList += "Rectangle " + width + "," + height + "; ";
            return new LSL_String(drawList);
        }

        public string osDrawFilledRectangle(string drawList, int width, int height)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osDrawFilledRectangle", m_host, "OSSL", m_itemID))
                return "";


            drawList += "FillRectangle " + width + "," + height + "; ";
            return new LSL_String(drawList);
        }

        public string osDrawFilledPolygon(string drawList, LSL_List x, LSL_List y)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osDrawFilledPolygon", m_host, "OSSL", m_itemID))
                return "";


            if (x.Length != y.Length || x.Length < 3)
            {
                return new LSL_String("");
            }
            drawList += "FillPolygon " + x.GetLSLStringItem(0) + "," + y.GetLSLStringItem(0);
            for (int i = 1; i < x.Length; i++)
            {
                drawList += "," + x.GetLSLStringItem(i) + "," + y.GetLSLStringItem(i);
            }
            drawList += "; ";
            return new LSL_String(drawList);
        }

        public string osDrawPolygon(string drawList, LSL_List x, LSL_List y)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osDrawFilledPolygon", m_host, "OSSL", m_itemID))
                return "";


            if (x.Length != y.Length || x.Length < 3)
            {
                return new LSL_String("");
            }
            drawList += "Polygon " + x.GetLSLStringItem(0) + "," + y.GetLSLStringItem(0);
            for (int i = 1; i < x.Length; i++)
            {
                drawList += "," + x.GetLSLStringItem(i) + "," + y.GetLSLStringItem(i);
            }
            drawList += "; ";
            return new LSL_String(drawList);
        }

        public string osSetFontSize(string drawList, int fontSize)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osSetFontSize", m_host, "OSSL", m_itemID))
                return "";


            drawList += "FontSize " + fontSize + "; ";
            return drawList;
        }

        public string osSetFontName(string drawList, string fontName)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osSetFontName", m_host, "OSSL", m_itemID))
                return "";


            drawList += "FontName " + fontName + "; ";
            return new LSL_String(drawList);
        }

        public string osSetPenSize(string drawList, int penSize)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osSetPenSize", m_host, "OSSL", m_itemID))
                return "";


            drawList += "PenSize " + penSize + "; ";
            return new LSL_String(drawList);
        }

        public string osSetPenColor(string drawList, string colour)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osSetPenColor", m_host, "OSSL", m_itemID))
                return "";


            drawList += "PenColour " + colour + "; ";
            return new LSL_String(drawList);
        }

        public string osSetPenCap(string drawList, string direction, string type)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osSetPenColor", m_host, "OSSL", m_itemID))
                return "";


            drawList += "PenCap " + direction + "," + type + "; ";
            return new LSL_String(drawList);
        }

        public string osDrawImage(string drawList, int width, int height, string imageUrl)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osDrawImage", m_host, "OSSL", m_itemID))
                return "";


            drawList += "Image " + width + "," + height + "," + imageUrl + "; ";
            return new LSL_String(drawList);
        }

        public LSL_Vector osGetDrawStringSize(string contentType, string text, string fontName, int fontSize)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.VeryLow, "osGetDrawStringSize", m_host, "OSSL", m_itemID))
                return new LSL_Vector();


            LSL_Vector vec = new LSL_Vector(0, 0, 0);
            IDynamicTextureManager textureManager = World.RequestModuleInterface<IDynamicTextureManager>();
            if (textureManager != null)
            {
                double xSize, ySize;
                textureManager.GetDrawStringSize(contentType, text, fontName, fontSize,
                                                 out xSize, out ySize);
                vec.x = xSize;
                vec.y = ySize;
            }
            return vec;
        }

        public void osSetRegionWaterHeight(double height)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osSetRegionWaterHeight", m_host, "OSSL", m_itemID))
                return;


            //Check to make sure that the script's owner is the estate manager/master
            //World.Permissions.GenericEstatePermission(
            if (World.Permissions.IsGod(m_host.OwnerID))
            {
                World.EventManager.TriggerRequestChangeWaterHeight((float)height);
            }
        }

        /// <summary>
        ///   Changes the Region Sun Settings, then Triggers a Sun Update
        /// </summary>
        /// <param name = "useEstateSun">True to use Estate Sun instead of Region Sun</param>
        /// <param name = "sunFixed">True to keep the sun stationary</param>
        /// <param name = "sunHour">The "Sun Hour" that is desired, 0...24, with 0 just after SunRise</param>
        public void osSetRegionSunSettings(bool useEstateSun, bool sunFixed, double sunHour)
        {
            if (
                !ScriptProtection.CheckThreatLevel(ThreatLevel.Nuisance, "osSetRegionSunSettings", m_host, "OSSL",
                                                   m_itemID)) return;


            //Check to make sure that the script's owner is the estate manager/master
            //World.Permissions.GenericEstatePermission(
            if (World.Permissions.IsGod(m_host.OwnerID))
            {
                while (sunHour > 24.0)
                    sunHour -= 24.0;

                while (sunHour < 0)
                    sunHour += 24.0;


                World.RegionInfo.RegionSettings.UseEstateSun = useEstateSun;
                World.RegionInfo.RegionSettings.SunPosition = sunHour + 6; // LL Region Sun Hour is 6 to 30
                World.RegionInfo.RegionSettings.FixedSun = sunFixed;
                World.RegionInfo.RegionSettings.Save();

                World.EventManager.TriggerEstateToolsSunUpdate(World.RegionInfo.RegionHandle, sunFixed, useEstateSun,
                                                               (float)sunHour);
            }
        }

        /// <summary>
        ///   Changes the Estate Sun Settings, then Triggers a Sun Update
        /// </summary>
        /// <param name = "sunFixed">True to keep the sun stationary, false to use global time</param>
        /// <param name = "sunHour">The "Sun Hour" that is desired, 0...24, with 0 just after SunRise</param>
        public void osSetEstateSunSettings(bool sunFixed, double sunHour)
        {
            if (
                !ScriptProtection.CheckThreatLevel(ThreatLevel.Nuisance, "osSetEstateSunSettings", m_host, "OSSL",
                                                   m_itemID)) return;


            //Check to make sure that the script's owner is the estate manager/master
            //World.Permissions.GenericEstatePermission(
            if (World.Permissions.IsGod(m_host.OwnerID))
            {
                while (sunHour > 24.0)
                    sunHour -= 24.0;

                while (sunHour < 0)
                    sunHour += 24.0;

                World.RegionInfo.EstateSettings.UseGlobalTime = !sunFixed;
                World.RegionInfo.EstateSettings.SunPosition = sunHour;
                World.RegionInfo.EstateSettings.FixedSun = sunFixed;
                World.RegionInfo.EstateSettings.Save();

                World.EventManager.TriggerEstateToolsSunUpdate(World.RegionInfo.RegionHandle, sunFixed,
                                                               World.RegionInfo.RegionSettings.UseEstateSun,
                                                               (float)sunHour);
            }
        }

        /// <summary>
        ///   Return the current Sun Hour 0...24, with 0 being roughly sun-rise
        /// </summary>
        /// <returns></returns>
        public double osGetCurrentSunHour()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osGetCurrentSunHour", m_host, "OSSL", m_itemID))
                return 0;


            // Must adjust for the fact that Region Sun Settings are still LL offset
            double sunHour = World.RegionInfo.RegionSettings.SunPosition - 6;

            // See if the sun module has registered itself, if so it's authoritative
            ISunModule module = World.RequestModuleInterface<ISunModule>();
            if (module != null)
            {
                sunHour = module.GetCurrentSunHour();
            }

            return sunHour;
        }

        public double osGetSunParam(string param) 
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osGetSunParam", m_host, "OSSL", m_itemID))
                return 0;
            return GetSunParam(param);
        }

        private double GetSunParam(string param)
        {
            double value = 0.0;

            ISunModule module = World.RequestModuleInterface<ISunModule>();
            if (module != null)
            {
                value = module.GetSunParameter(param);
            }

            return value;
        }

        public double osSunGetParam(string param) //patched from OpenSim, you can remove this comment after pull
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osSunGetParam", m_host, "OSSL", m_itemID))
                return 0;


            double value = 0.0;

            ISunModule module = World.RequestModuleInterface<ISunModule>();
            if (module != null)
            {
                value = module.GetSunParameter(param);
            }

            return value;
        }

        public void osSunSetParam(string param, double value) //patched from OpenSim, you can remove this comment after pull
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osSunSetParam", m_host, "OSSL", m_itemID)) return;


            ISunModule module = World.RequestModuleInterface<ISunModule>();
            if (module != null)
            {
                module.SetSunParameter(param, value);
            }
        }

        public void osSetSunParam(string param, double value)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osSetSunParam", m_host, "OSSL", m_itemID)) return;
            SetSunParam(param, value);
        }

        private void SetSunParam(string param, double value)
        {
            ISunModule module = World.RequestModuleInterface<ISunModule>();
            if (module != null)
            {
                module.SetSunParameter(param, value);
            }
        }
        
        public string osWindActiveModelPluginName()
        {
            if (
                !ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osWindActiveModelPluginName", m_host, "OSSL",
                                                   m_itemID)) return "";


            IWindModule module = World.RequestModuleInterface<IWindModule>();
            if (module != null)
            {
                return new LSL_String(module.WindActiveModelPluginName);
            }

            return new LSL_String("");
        }

        public void osSetWindParam(string plugin, string param, LSL_Float value)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.VeryLow, "osSetWindParam", m_host, "OSSL", m_itemID))
                return;


            IWindModule module = World.RequestModuleInterface<IWindModule>();
            if (module != null)
            {
                try
                {
                    module.WindParamSet(plugin, param, (float)value.value);
                }
                catch (Exception)
                {
                }
            }
        }

        public LSL_Float osGetWindParam(string plugin, string param)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.VeryLow, "osWindParamGet", m_host, "OSSL", m_itemID))
                return new LSL_Float();

            IWindModule module = World.RequestModuleInterface<IWindModule>();
            if (module != null)
            {
                return module.WindParamGet(plugin, param);
            }

            return 0.0f;
        }

        // Routines for creating and managing parcels programmatically
        public void osParcelJoin(LSL_Vector pos1, LSL_Vector pos2)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osParcelJoin", m_host, "OSSL", m_itemID)) return;


            int startx = (int)(pos1.x < pos2.x ? pos1.x : pos2.x);
            int starty = (int)(pos1.y < pos2.y ? pos1.y : pos2.y);
            int endx = (int)(pos1.x > pos2.x ? pos1.x : pos2.x);
            int endy = (int)(pos1.y > pos2.y ? pos1.y : pos2.y);

            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                parcelManagement.Join(startx, starty, endx, endy, m_host.OwnerID);
            }
        }

        public void osParcelSubdivide(LSL_Vector pos1, LSL_Vector pos2)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osParcelSubdivide", m_host, "OSSL", m_itemID))
                return;


            int startx = (int)(pos1.x < pos2.x ? pos1.x : pos2.x);
            int starty = (int)(pos1.y < pos2.y ? pos1.y : pos2.y);
            int endx = (int)(pos1.x > pos2.x ? pos1.x : pos2.x);
            int endy = (int)(pos1.y > pos2.y ? pos1.y : pos2.y);

            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                parcelManagement.Subdivide(startx, starty, endx, endy, m_host.OwnerID);
            }
        }

        public void osSetParcelDetails(LSL_Vector pos, LSL_List rules)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osSetParcelDetails", m_host, "OSSL", m_itemID))
                return;


            // Get a reference to the land data and make sure the owner of the script
            // can modify it

            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                ILandObject startLandObject = parcelManagement.GetLandObject((int)pos.x, (int)pos.y);
                if (startLandObject == null)
                {
                    OSSLShoutError("There is no land at that location");
                    return;
                }

                if (!World.Permissions.CanEditParcel(m_host.OwnerID, startLandObject))
                {
                    OSSLShoutError("You do not have permission to modify the parcel");
                    return;
                }

                // Create a new land data object we can modify

                // Process the rules, not sure what the impact would be of changing owner or group
                for (int idx = 0; idx < rules.Length; )
                {
                    int code = rules.GetLSLIntegerItem(idx++);
                    string arg = rules.GetLSLStringItem(idx++);
                    UUID uuid;
                    switch (code)
                    {
                        case 0:
                            startLandObject.LandData.Name = arg;
                            break;

                        case 1:
                            startLandObject.LandData.Description = arg;
                            break;

                        case 2:
                            if (
                                !ScriptProtection.CheckThreatLevel(ThreatLevel.VeryHigh, "osSetParcelDetails", m_host,
                                                                   "OSSL", m_itemID)) return;
                            if (UUID.TryParse(arg, out uuid))
                                startLandObject.LandData.OwnerID = uuid;
                            break;

                        case 3:
                            if (
                                !ScriptProtection.CheckThreatLevel(ThreatLevel.VeryHigh, "osSetParcelDetails", m_host,
                                                                   "OSSL", m_itemID)) return;
                            if (UUID.TryParse(arg, out uuid))
                                startLandObject.LandData.GroupID = uuid;
                            break;
                    }
                }

                parcelManagement.UpdateLandObject(startLandObject);
            }
        }

        /// <summary>
        /// Sets terrain estate texture
        /// </summary>
        /// <param name="level"></param>
        /// <param name="texture"></param>
        /// <returns></returns>
        public void osSetTerrainTexture(int level, LSL_Key texture)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osSetParcelDetails", m_host,"OSSL", m_itemID)) return;
            //Check to make sure that the script's owner is the estate manager/master
            //World.Permissions.GenericEstatePermission(
            if (World.Permissions.IsGod(m_host.OwnerID))
           {
                if (level < 0 || level > 3)
                    return;
                UUID textureID = new UUID();
                if (!UUID.TryParse(texture, out textureID))
                   return;
                // estate module is required
                 IEstateModule estate = World.RequestModuleInterface<IEstateModule>();
                if (estate != null)
                    estate.setEstateTerrainBaseTexture(level, textureID);
           }
        }

       /// <summary>
        /// Sets terrain heights of estate
        /// </summary>
       /// <param name="corner"></param>
        /// <param name="low"></param>
        /// <param name="high"></param>
        /// <returns></returns>
        public void osSetTerrainTextureHeight(int corner, double low, double high)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osSetParcelDetails", m_host,"OSSL", m_itemID)) return;
 
            //Check to make sure that the script's owner is the estate manager/master
            //World.Permissions.GenericEstatePermission(
            if (World.Permissions.IsGod(m_host.OwnerID))
           {
               if (corner < 0 || corner > 3)
                    return;
                // estate module is required
                IEstateModule estate = World.RequestModuleInterface<IEstateModule>();
                if (estate != null)
                   estate.setEstateTerrainTextureHeights(corner, (float)low, (float)high);
            }
        }
 
        public double osList2Double(LSL_List src, int index)
        {
            // There is really no double type in OSSL. C# and other
            // have one, but the current implementation of LSL_Types.list
            // is not allowed to contain any.
            // This really should be removed.
            //
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osList2Double", m_host, "OSSL", m_itemID))
                return 0;


            if (index < 0)
            {
                index = src.Length + index;
            }
            if (index >= src.Length)
            {
                return 0.0;
            }
            return Convert.ToDouble(src.Data[index]);
        }

        public void osSetParcelMediaURL(string url)
        {
            // What actually is the difference to the LL function?
            //
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.VeryLow, "osSetParcelMediaURL", m_host, "OSSL", m_itemID))
                return;

            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                ILandObject land
                    = parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y);

                if (land == null || land.LandData.OwnerID != m_host.OwnerID)
                    return;

                land.SetMediaUrl(url);
            }
        }

        public void osSetParcelSIPAddress(string SIPAddress)
        {
            // What actually is the difference to the LL function?
            //
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.VeryLow, "osSetParcelMediaURL", m_host, "OSSL", m_itemID))
                return;

            IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                ILandObject land
                    = parcelManagement.GetLandObject(m_host.AbsolutePosition.X, m_host.AbsolutePosition.Y);

                if (land == null || land.LandData.OwnerID != m_host.OwnerID)
                {
                    OSSLError("osSetParcelSIPAddress: Sorry, you need to own the land to use this function");
                    return;
                }

                // get the voice module
                IVoiceModule voiceModule = World.RequestModuleInterface<IVoiceModule>();

                if (voiceModule != null)
                    voiceModule.setLandSIPAddress(SIPAddress, land.LandData.GlobalID);
                else
                    OSSLError("osSetParcelSIPAddress: No voice module enabled for this land");
            }
        }

        public string osGetScriptEngineName()
        {
            // This gets a "high" because knowing the engine may be used
            // to exploit engine-specific bugs or induce usage patterns
            // that trigger engine-specific failures.
            // Besides, public grid users aren't supposed to know.
            //
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osGetScriptEngineName", m_host, "OSSL", m_itemID))
                return "";


            int scriptEngineNameIndex = 0;

            if (!String.IsNullOrEmpty(m_ScriptEngine.ScriptEngineName))
            {
                // parse off the "ScriptEngine."
                scriptEngineNameIndex = m_ScriptEngine.ScriptEngineName.IndexOf(".", scriptEngineNameIndex, System.StringComparison.Ordinal);
                scriptEngineNameIndex++; // get past delimiter

                int scriptEngineNameLength = m_ScriptEngine.ScriptEngineName.Length - scriptEngineNameIndex;

                // create char array then a string that is only the script engine name
                Char[] scriptEngineNameCharArray = m_ScriptEngine.ScriptEngineName.ToCharArray(scriptEngineNameIndex,
                                                                                               scriptEngineNameLength);
                String scriptEngineName = new String(scriptEngineNameCharArray);

                return scriptEngineName;
            }
            else
            {
                return String.Empty;
            }
        }

        public string osGetSimulatorVersion()
        {
            // High because it can be used to target attacks to known weaknesses
            // This would allow a new class of griefer scripts that don't even
            // require their user to know what they are doing (see script
            // kiddie)
            //
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osGetSimulatorVersion", m_host, "OSSL", m_itemID))
                return "";

            ISimulationBase simulationBase = World.RequestModuleInterface<ISimulationBase>();
            if (simulationBase != null)
                return simulationBase.Version;
            return "";
        }

        private Hashtable osdToHashtable(OSDMap map)
        {
            Hashtable result = new Hashtable();
            foreach (KeyValuePair<string, OSD> item in map)
            {
                result.Add(item.Key, osdToObject(item.Value));
            }
            return result;
        }

        private ArrayList osdToArray(OSDArray list)
        {
            ArrayList result = new ArrayList();
            foreach (OSD item in list)
            {
                result.Add(osdToObject(item));
            }
            return result;
        }

        private Object osdToObject(OSD decoded)
        {
            if (decoded is OSDString)
            {
                return (string)decoded.AsString();
            }
            else if (decoded is OSDInteger)
            {
                return (int)decoded.AsInteger();
            }
            else if (decoded is OSDReal)
            {
                return (float)decoded.AsReal();
            }
            else if (decoded is OSDBoolean)
            {
                return (bool)decoded.AsBoolean();
            }
            else if (decoded is OSDMap)
            {
                return osdToHashtable((OSDMap)decoded);
            }
            else if (decoded is OSDArray)
            {
                return osdToArray((OSDArray)decoded);
            }
            else
            {
                return null;
            }
        }

        public Object osParseJSONNew(string JSON) //patched from OpenSim, you can remove this comment after pull
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osParseJSONNew", m_host, "OSSL", m_itemID))
                return new object();
            try
            {
                OSD decoded = OSDParser.DeserializeJson(JSON);
                return osdToObject(decoded);
            }
            catch (Exception e)
            {
                OSSLError("osParseJSONNew: Problems decoding JSON string " + JSON + " : " + e.Message);
                return null;
            }
        }
        
        public Hashtable osParseJSON(string JSON)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osParseJSON", m_host, "OSSL", m_itemID))
                return new Hashtable();
            
            // see http://www.json.org/ for more details on JSON

            string currentKey = null;
            Stack objectStack = new Stack(); // objects in JSON can be nested so we need to keep a track of this
            Hashtable jsondata = new Hashtable(); // the hashtable to be returned
            try
            {
                // iterate through the serialised stream of tokens and store at the right depth in the hashtable
                // the top level hashtable may contain more nested hashtables within it each containing an objects representation
                int i = 0;
                for (i = 0; i < JSON.Length; i++)
                {
                    // MainConsole.Instance.Debug(""+JSON[i]);
                    switch (JSON[i])
                    {
                        case '{':
                            // create hashtable and add it to the stack or array if we are populating one, we can have a lot of nested objects in JSON

                            Hashtable currentObject = new Hashtable();
                            if (objectStack.Count == 0) // the stack should only be empty for the first outer object
                            {
                                objectStack.Push(jsondata);
                            }
                            else if (objectStack.Peek().ToString() == "System.Collections.ArrayList")
                            {
                                // add it to the parent array
                                ((ArrayList)objectStack.Peek()).Add(currentObject);
                                objectStack.Push(currentObject);
                            }
                            else
                            {
                                // add it to the parent hashtable
                                if (currentKey != null) ((Hashtable)objectStack.Peek()).Add(currentKey, currentObject);
                                objectStack.Push(currentObject);
                            }

                            // clear the key
                            currentKey = null;
                            break;

                        case '}':
                            // pop the hashtable off the stack
                            objectStack.Pop();
                            break;

                        case '"': // string boundary

                            string tokenValue = "";
                            i++; // move to next char

                            // just loop through until the next quote mark storing the string, ignore quotes with pre-ceding \
                            while (JSON[i] != '"')
                            {
                                tokenValue += JSON[i];

                                // handle escaped double quotes \"
                                if (JSON[i] == '\\' && JSON[i + 1] == '"')
                                {
                                    tokenValue += JSON[i + 1];
                                    i++;
                                }
                                i++;
                            }

                            // ok we've got a string, if we've got an array on the top of the stack then we store it
                            if (objectStack.Peek().ToString() == "System.Collections.ArrayList")
                            {
                                ((ArrayList)objectStack.Peek()).Add(tokenValue);
                            }
                            else if (currentKey == null)
                            // no key stored and its not an array this must be a key so store it
                            {
                                currentKey = tokenValue;
                            }
                            else
                            {
                                // we have a key so lets store this value
                                ((Hashtable)objectStack.Peek()).Add(currentKey, tokenValue);
                                // now lets clear the key, we're done with it and moving on
                                currentKey = null;
                            }

                            break;

                        case ':': // key : value separator
                            // just ignore
                            break;

                        case ' ': // spaces
                            // just ignore
                            break;

                        case '[': // array start
                            ArrayList currentArray = new ArrayList();

                            if (objectStack.Peek().ToString() == "System.Collections.ArrayList")
                            {
                                ((ArrayList)objectStack.Peek()).Add(currentArray);
                            }
                            else
                            {
                                if (currentKey != null) ((Hashtable)objectStack.Peek()).Add(currentKey, currentArray);
                                // clear the key
                                currentKey = null;
                            }
                            objectStack.Push(currentArray);

                            break;

                        case ',': // seperator
                            // just ignore
                            break;

                        case ']': //Array end
                            // pop the array off the stack
                            objectStack.Pop();
                            break;

                        case 't': // we've found a character start not in quotes, it must be a boolean true

                            if (objectStack.Peek().ToString() == "System.Collections.ArrayList")
                            {
                                ((ArrayList)objectStack.Peek()).Add(true);
                            }
                            else
                            {
                                if (currentKey != null) ((Hashtable)objectStack.Peek()).Add(currentKey, true);
                                currentKey = null;
                            }

                            //advance the counter to the letter 'e'
                            i = i + 3;
                            break;

                        case 'f': // we've found a character start not in quotes, it must be a boolean false

                            if (objectStack.Peek().ToString() == "System.Collections.ArrayList")
                            {
                                ((ArrayList)objectStack.Peek()).Add(false);
                            }
                            else
                            {
                                if (currentKey != null) ((Hashtable)objectStack.Peek()).Add(currentKey, false);
                                currentKey = null;
                            }
                            //advance the counter to the letter 'e'
                            i = i + 4;
                            break;

                        case '\n': // carriage return
                            // just ignore
                            break;

                        case '\r': // carriage return
                            // just ignore
                            break;

                        default:
                            // ok here we're catching all numeric types int,double,long we might want to spit these up mr accurately
                            // but for now we'll just do them as strings

                            string numberValue = "";

                            // just loop through until the next known marker quote mark storing the string
                            while (JSON[i] != '"' && JSON[i] != ',' && JSON[i] != ']' && JSON[i] != '}' &&
                                   JSON[i] != ' ')
                            {
                                numberValue += "" + JSON[i++];
                            }

                            i--; // we want to process this caracter that marked the end of this string in the main loop

                            // ok we've got a string, if we've got an array on the top of the stack then we store it
                            if (objectStack.Peek().ToString() == "System.Collections.ArrayList")
                            {
                                ((ArrayList)objectStack.Peek()).Add(numberValue);
                            }
                            else
                            {
                                // we have a key so lets store this value
                                if (currentKey != null) ((Hashtable)objectStack.Peek()).Add(currentKey, numberValue);
                                // now lets clear the key, we're done with it and moving on
                                currentKey = null;
                            }

                            break;
                    }
                }
            }
            catch (Exception)
            {
                OSSLError("osParseJSON: The JSON string is not valid " + JSON);
            }

            return jsondata;
        }

        // send a message to to object identified by the given UUID, a script in the object must implement the dataserver function
        // the dataserver function is passed the ID of the calling function and a string message
        public void osMessageObject(LSL_Key objectUUID, string message)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "osMessageObject", m_host, "OSSL", m_itemID))
                return;


            object[] resobj = new object[] { new LSL_Key(m_host.UUID.ToString()), new LSL_Key(message) };

            ISceneChildEntity sceneOP = World.GetSceneObjectPart(objectUUID);

            m_ScriptEngine.PostObjectEvent(sceneOP.UUID, "dataserver", resobj);
        }


        // This needs ThreatLevel high. It is an excellent griefer tool,
        // In a loop, it can cause asset bloat and DOS levels of asset
        // writes.
        //
        public void osMakeNotecard(string notecardName, LSL_List contents)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osMakeNotecard", m_host, "OSSL", m_itemID))
                return;


            // Create new asset
            AssetBase asset = new AssetBase(UUID.Random(), notecardName, AssetType.Notecard, m_host.OwnerID) { Description = "Script Generated Notecard" };
            string notecardData = String.Empty;

            for (int i = 0; i < contents.Length; i++)
            {
                notecardData += contents.GetLSLStringItem(i) + "\n";
            }

            int textLength = notecardData.Length;
            notecardData = "Linden text version 2\n{\nLLEmbeddedItems version 1\n{\ncount 0\n}\nText length "
                           + textLength.ToString(CultureInfo.InvariantCulture) + "\n" + notecardData + "}\n";

            asset.Data = Util.UTF8.GetBytes(notecardData);
            asset.FillHash();
            asset.ID = World.AssetService.Store(asset);

            // Create Task Entry
            TaskInventoryItem taskItem = new TaskInventoryItem();

            taskItem.ResetIDs(m_host.UUID);
            taskItem.ParentID = m_host.UUID;
            taskItem.CreationDate = (uint)Util.UnixTimeSinceEpoch();
            taskItem.Name = asset.Name;
            taskItem.Description = asset.Description;
            taskItem.Type = (int)AssetType.Notecard;
            taskItem.InvType = (int)InventoryType.Notecard;
            taskItem.OwnerID = m_host.OwnerID;
            taskItem.CreatorID = m_host.OwnerID;
            taskItem.BasePermissions = (uint)PermissionMask.All;
            taskItem.CurrentPermissions = (uint)PermissionMask.All;
            taskItem.EveryonePermissions = 0;
            taskItem.NextPermissions = (uint)PermissionMask.All;
            taskItem.GroupID = m_host.GroupID;
            taskItem.GroupPermissions = 0;
            taskItem.Flags = 0;
            taskItem.SalePrice = 0;
            taskItem.SaleType = 0;
            taskItem.PermsGranter = UUID.Zero;
            taskItem.PermsMask = 0;
            taskItem.AssetID = asset.ID;

            m_host.Inventory.AddInventoryItem(taskItem, false);
        }


        /*Instead of using the LSL Dataserver event to pull notecard data,
                 this will simply read the requested line and return its data as a string.

                 Warning - due to the synchronous method this function uses to fetch assets, its use
                           may be dangerous and unreliable while running in grid mode.
                */

        public string osGetNotecardLine(string name, int line)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.VeryHigh, "osGetNotecardLine", m_host, "OSSL", m_itemID))
                return "";


            UUID assetID = UUID.Zero;

            if (!UUID.TryParse(name, out assetID))
            {
#if (!ISWIN)
                foreach (TaskInventoryItem item in m_host.TaskInventory.Values)
                {
                    if (item.Type == 7 && item.Name == name)
                    {
                        assetID = item.AssetID;
                    }
                }
#else
                foreach (TaskInventoryItem item in m_host.TaskInventory.Values.Where(item => item.Type == 7 && item.Name == name))
                {
                    assetID = item.AssetID;
                }
#endif
            }

            if (assetID == UUID.Zero)
            {
                OSSLShoutError("Notecard '" + name + "' could not be found.");
                return "ERROR!";
            }

            if (!NotecardCache.IsCached(assetID))
            {
                AssetBase a = World.AssetService.Get(assetID.ToString());
                if (a != null)
                {
                    UTF8Encoding enc = new UTF8Encoding();
                    string data = enc.GetString(a.Data);
                    NotecardCache.Cache(assetID, data);
                }
                else
                {
                    OSSLShoutError("Notecard '" + name + "' could not be found.");
                    return "ERROR!";
                }
            }

            return NotecardCache.GetLine(assetID, line, 255);
        }

        /*Instead of using the LSL Dataserver event to pull notecard data line by line,
          this will simply read the entire notecard and return its data as a string.

          Warning - due to the synchronous method this function uses to fetch assets, its use
                    may be dangerous and unreliable while running in grid mode.
         */

        public string osGetNotecard(string name)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.VeryHigh, "osGetNotecard", m_host, "OSSL", m_itemID))
                return "";


            UUID assetID = UUID.Zero;
            string NotecardData = "";

            if (!UUID.TryParse(name, out assetID))
            {
#if (!ISWIN)
                foreach (TaskInventoryItem item in m_host.TaskInventory.Values)
                {
                    if (item.Type == 7 && item.Name == name)
                    {
                        assetID = item.AssetID;
                    }
                }
#else
                foreach (TaskInventoryItem item in m_host.TaskInventory.Values.Where(item => item.Type == 7 && item.Name == name))
                {
                    assetID = item.AssetID;
                }
#endif
            }

            if (assetID == UUID.Zero)
            {
                OSSLShoutError("Notecard '" + name + "' could not be found.");
                return "ERROR!";
            }

            if (!NotecardCache.IsCached(assetID))
            {
                AssetBase a = World.AssetService.Get(assetID.ToString());
                if (a != null)
                {
                    UTF8Encoding enc = new UTF8Encoding();
                    string data = enc.GetString(a.Data);
                    NotecardCache.Cache(assetID, data);
                }
                else
                {
                    OSSLShoutError("Notecard '" + name + "' could not be found.");
                    return "ERROR!";
                }
            }

            for (int count = 0; count < NotecardCache.GetLines(assetID); count++)
            {
                NotecardData += NotecardCache.GetLine(assetID, count, 255) + "\n";
            }

            return NotecardData;
        }

        /*Instead of using the LSL Dataserver event to pull notecard data,
          this will simply read the number of note card lines and return this data as an integer.

          Warning - due to the synchronous method this function uses to fetch assets, its use
                    may be dangerous and unreliable while running in grid mode.
         */

        public int osGetNumberOfNotecardLines(string name)
        {
            if (
                !ScriptProtection.CheckThreatLevel(ThreatLevel.VeryHigh, "osGetNumberOfNotecardLines", m_host, "OSSL",
                                                   m_itemID)) return 0;


            UUID assetID = UUID.Zero;

            if (!UUID.TryParse(name, out assetID))
            {
#if (!ISWIN)
                foreach (TaskInventoryItem item in m_host.TaskInventory.Values)
                {
                    if (item.Type == 7 && item.Name == name)
                    {
                        assetID = item.AssetID;
                    }
                }
#else
                foreach (TaskInventoryItem item in m_host.TaskInventory.Values.Where(item => item.Type == 7 && item.Name == name))
                {
                    assetID = item.AssetID;
                }
#endif
            }

            if (assetID == UUID.Zero)
            {
                OSSLShoutError("Notecard '" + name + "' could not be found.");
                return -1;
            }

            if (!NotecardCache.IsCached(assetID))
            {
                AssetBase a = World.AssetService.Get(assetID.ToString());
                if (a != null)
                {
                    UTF8Encoding enc = new UTF8Encoding();
                    string data = enc.GetString(a.Data);
                    NotecardCache.Cache(assetID, data);
                }
                else
                {
                    OSSLShoutError("Notecard '" + name + "' could not be found.");
                    return -1;
                }
            }

            return NotecardCache.GetLines(assetID);
        }

        public string osAvatarName2Key(string firstname, string lastname)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "osAvatarName2Key", m_host, "OSSL", m_itemID))
                return "";

            UserAccount account = World.UserAccountService.GetUserAccount(World.RegionInfo.AllScopeIDs,
                                                                          firstname + " " + lastname);
            if (null == account)
            {
                return UUID.Zero.ToString();
            }
            else
            {
                return account.PrincipalID.ToString();
            }
        }

        public string osKey2Name(string id)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "osKey2Name", m_host, "OSSL", m_itemID)) return "";
            UUID key = new UUID();

            if (UUID.TryParse(id, out key))
            {
                UserAccount account = World.UserAccountService.GetUserAccount(World.RegionInfo.AllScopeIDs, key);
                if (null == account)
                {
                    return "";
                }
                else
                {
                    return account.Name;
                }
            }
            else
            {
                return "";
            }
        }

        ///Threat level is Moderate because intentional abuse, for instance
        ///scripts that are written to be malicious only on one grid,
        ///for instance in a HG scenario, are a distinct possibility.
        ///
        ///Use value from the config file and return it.
        public string osGetGridNick()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "osGetGridNick", m_host, "OSSL", m_itemID))
                return "";

            string nick = "hippogrid";
            IConfigSource config = m_ScriptEngine.ConfigSource;
            if (config.Configs["GridInfo"] != null)
                nick = config.Configs["GridInfo"].GetString("gridnick", nick);
            return nick;
        }

        public string osGetGridName()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "osGetGridName", m_host, "OSSL", m_itemID))
                return "";

            string name = "the lost continent of hippo";
            IConfigSource config = m_ScriptEngine.ConfigSource;
            if (config.Configs["GridInfo"] != null)
                name = config.Configs["GridInfo"].GetString("gridname", name);
            return name;
        }

        public string osGetGridLoginURI()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "osGetGridLoginURI", m_host, "OSSL", m_itemID))
                return "";

            string loginURI = "http://127.0.0.1:9000/";
            IConfigSource config = m_ScriptEngine.ConfigSource;
            if (config.Configs["GridInfo"] != null)
                loginURI = config.Configs["GridInfo"].GetString("login", loginURI);
            return loginURI;
        }

        public LSL_String osFormatString(string str, LSL_List strings)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "osFormatString", m_host, "OSSL", m_itemID))
                return new LSL_String();


            return String.Format(str, strings.Data);
        }

        public LSL_List osMatchString(string src, string pattern, int start)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osMatchString", m_host, "OSSL", m_itemID))
                return new LSL_List();


            LSL_List result = new LSL_List();

            // Normalize indices (if negative).
            // After normlaization they may still be
            // negative, but that is now relative to
            // the start, rather than the end, of the
            // sequence.
            if (start < 0)
            {
                start = src.Length + start;
            }

            if (start < 0 || start >= src.Length)
            {
                return result; // empty list
            }

            // Find matches beginning at start position
            Regex matcher = new Regex(pattern);
            Match match = matcher.Match(src, start);
            while (match.Success)
            {
                foreach (Group g in match.Groups)
                {
                    if (g.Success)
                    {
                        result.Add(new LSL_Integer(g.Value));
                        result.Add(new LSL_Integer(g.Index));
                    }
                }
                match = match.NextMatch();
            }

            return result;
        }

        public string osLoadedCreationDate()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "osLoadedCreationDate", m_host, "OSSL", m_itemID))
                return "";


            return World.RegionInfo.RegionSettings.LoadedCreationDate;
        }

        public string osLoadedCreationTime()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "osLoadedCreationTime", m_host, "OSSL", m_itemID))
                return "";


            return World.RegionInfo.RegionSettings.LoadedCreationTime;
        }

        public string osLoadedCreationID()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "osLoadedCreationID", m_host, "OSSL", m_itemID))
                return "";


            return World.RegionInfo.RegionSettings.LoadedCreationID;
        }

        // Threat level is 'Low' because certain users could possibly be tricked into
        // dropping an unverified script into one of their own objects, which could
        // then gather the physical construction details of the object and transmit it
        // to an unscrupulous third party, thus permitting unauthorized duplication of
        // the object's form.
        //
        public LSL_List osGetLinkPrimitiveParams(int linknumber, LSL_List rules)
        {
            if (
                !ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osGetLinkPrimitiveParams", m_host, "OSSL",
                                                   m_itemID)) return new LSL_List();

            InitLSL();
            LSL_List retVal = new LSL_List();
            //Assign requested part directly
            ISceneChildEntity part = m_host.ParentEntity.GetLinkNumPart(linknumber) as ISceneChildEntity;

            //Check to see if the requested part exists (NOT null) and if so, get it's rules
            if (part != null) retVal = ((LSL_Api)m_LSL_Api).GetLinkPrimitiveParams(part, rules);

            //Will retun rules for specific part, or an empty list if part == null
            return retVal;
        }

        /// <summary>
        ///   Get current region's map texture UUID
        /// </summary>
        /// <returns></returns>
        public LSL_Key osGetMapTexture()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osGetMapTexture", m_host, "OSSL", m_itemID))
                return new LSL_Key();
            return World.RegionInfo.RegionSettings.TerrainImageID.ToString();
        }

        /// <summary>
        ///   Get a region's map texture UUID by region UUID or name.
        /// </summary>
        /// <param name = "regionName"></param>
        /// <returns></returns>
        public LSL_Key osGetRegionMapTexture(string regionName)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osGetRegionMapTexture", m_host, "OSSL", m_itemID))
                return new LSL_Key();
            IScene scene = m_host.ParentEntity.Scene;
            UUID key = UUID.Zero;

            //If string is a key, use it. Otherwise, try to locate region by name.
            GridRegion region = UUID.TryParse(regionName, out key)
                                    ? scene.GridService.GetRegionByUUID(scene.RegionInfo.AllScopeIDs, key)
                                    : scene.GridService.GetRegionByName(scene.RegionInfo.AllScopeIDs, regionName);

            // If region was found, return the regions map texture key.
            if (region != null)
                key = region.TerrainImage;

            return key.ToString();
        }

        /// <summary>
        ///   Return information regarding various simulator statistics (sim fps, physics fps, time
        ///   dilation, total number of prims, total number of active scripts, script lps, various
        ///   timing data, packets in/out, etc. Basically much the information that's shown in the
        ///   client's Statistics Bar (Ctrl-Shift-1)
        /// </summary>
        /// <returns>List of floats</returns>
        public LSL_List osGetRegionStats()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "osGetRegionStats", m_host, "OSSL", m_itemID))
                return new LSL_List();

            LSL_List ret = new LSL_List();
            IMonitorModule mod = World.RequestModuleInterface<IMonitorModule>();
            if (mod != null)
            {
                float[] stats = mod.GetRegionStats(World.RegionInfo.RegionID.ToString());

                for (int i = 0; i < 21; i++)
                {
                    ret.Add(new LSL_Float(stats[i]));
                }
            }
            return ret;
        }

        public int osGetSimulatorMemory()
        {
            if (
                !ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "osGetSimulatorMemory", m_host, "OSSL",
                                                   m_itemID)) return 0;

            long pws = Process.GetCurrentProcess().WorkingSet64;

            if (pws > Int32.MaxValue)
                return Int32.MaxValue;
            if (pws < 0)
                return 0;

            return (int)pws;
        }

        public void osSetSpeed(LSL_Key UUID, LSL_Float SpeedModifier)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "osSetSpeed", m_host, "OSSL", m_itemID))
                return;

            IScenePresence avatar = World.GetScenePresence(UUID);
            if (avatar != null)
            {
                if (avatar.UUID != m_host.OwnerID)
                {
                    //We need to make sure that they can do this then
                    if (!World.Permissions.IsGod(m_host.OwnerID))
                        return;
                }
                avatar.SpeedModifier = (float)SpeedModifier;
            }
        }

        public void osKickAvatar(LSL_String FirstName, LSL_String SurName, LSL_String alert)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Severe, "osKickAvatar", m_host, "OSSL", m_itemID))
                return;
            World.ForEachScenePresence(delegate(IScenePresence sp)
            {
                if (!sp.IsChildAgent &&
                    sp.Firstname == FirstName &&
                    sp.Lastname == SurName)
                {
                    // kick client...
                    sp.ControllingClient.Kick(alert);

                    // ...and close on our side
                    IEntityTransferModule transferModule =
                        sp.Scene.RequestModuleInterface<IEntityTransferModule>();
                    if (transferModule != null)
                        transferModule.IncomingCloseAgent(sp.Scene, sp.UUID);
                }
            });
        }

        public LSL_List osGetPrimitiveParams(LSL_Key prim, LSL_List rules)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osGetPrimitiveParams", m_host, "OSSL", m_itemID))
                return new LSL_List();

            InitLSL();
            return m_LSL_Api.GetLinkPrimitiveParamsEx(prim, rules);
        }

        public void osSetPrimitiveParams(LSL_Key prim, LSL_List rules)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osSetPrimitiveParams", m_host, "OSSL", m_itemID))
                return;

            InitLSL();
            m_LSL_Api.SetPrimitiveParamsEx(prim, rules);
        }

        /// <summary>
        ///   Set parameters for light projection in host prim
        /// </summary>
        public void osSetProjectionParams(bool projection, LSL_Key texture, double fov, double focus, double amb)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osSetProjectionParams", m_host, "OSSL", m_itemID))
                return;

            osSetProjectionParams(UUID.Zero.ToString(), projection, texture, fov, focus, amb);
        }

        /// <summary>
        ///   Set parameters for light projection with uuid of target prim
        /// </summary>
        public void osSetProjectionParams(LSL_Key prim, bool projection, LSL_Key texture, double fov, double focus,
                                          double amb)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osSetProjectionParams", m_host, "OSSL", m_itemID))
                return;

            ISceneChildEntity obj = null;
            if (prim == UUID.Zero.ToString())
            {
                obj = m_host;
            }
            else
            {
                obj = World.GetSceneObjectPart(prim);
                if (obj == null)
                    return;
            }

            obj.Shape.ProjectionEntry = projection;
            obj.Shape.ProjectionTextureUUID = texture;
            obj.Shape.ProjectionFOV = (float)fov;
            obj.Shape.ProjectionFocus = (float)focus;
            obj.Shape.ProjectionAmbiance = (float)amb;


            obj.ParentEntity.HasGroupChanged = true;
            obj.ScheduleUpdate(PrimUpdateFlags.FullUpdate);
        }

        /// <summary>
        ///   Like osGetAgents but returns enough info for a radar
        /// </summary>
        /// <returns>Strided list of the UUID, position and name of each avatar in the region</returns>
        public LSL_List osGetAvatarList()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "osGetAvatarList", m_host, "OSSL", m_itemID))
                return new LSL_List();

            LSL_List result = new LSL_List();
            World.ForEachScenePresence(delegate(IScenePresence avatar)
            {
                if (avatar != null && avatar.UUID != m_host.OwnerID)
                {
                    if (!avatar.IsChildAgent)
                    {
                        result.Add(new LSL_Key(avatar.UUID.ToString()));
                        result.Add(new LSL_Vector(avatar.AbsolutePosition.X,
                                                  avatar.AbsolutePosition.Y,
                                                  avatar.AbsolutePosition.Z));
                        result.Add(new LSL_String(avatar.Name));
                    }
                }
            });
            return result;
        }

        public LSL_Integer osAddAgentToGroup(LSL_Key AgentID, LSL_String GroupName, LSL_String RequestedRole)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "osAddAgentToGroup", m_host, "OSSL", m_itemID))
                return new LSL_Integer();

            IGroupsServicesConnector m_groupData = World.RequestModuleInterface<IGroupsServicesConnector>();

            // No groups module, no functionality
            if (m_groupData == null)
            {
                OSSLShoutError("No Groups Module found for osAddAgentToGroup.");
                return 0;
            }

            UUID roleID = UUID.Zero;
            GroupRecord groupRecord = m_groupData.GetGroupRecord(m_host.OwnerID, UUID.Zero, GroupName.m_string);
            if (groupRecord == null)
            {
                OSSLShoutError("Could not find the group.");
                return 0;
            }

            List<GroupRolesData> roles = m_groupData.GetGroupRoles(m_host.OwnerID, groupRecord.GroupID);
#if (!ISWIN)
            foreach (GroupRolesData role in roles)
            {
                if (role.Name == RequestedRole.m_string)
                {
                    roleID = role.RoleID;
                }
            }
#else
            foreach (GroupRolesData role in roles.Where(role => role.Name == RequestedRole.m_string))
            {
                roleID = role.RoleID;
            }
#endif

            //It takes care of permission checks in the module
            m_groupData.AddAgentToGroup(m_host.OwnerID, UUID.Parse(AgentID.m_string), groupRecord.GroupID, roleID);
            return 1;
        }

        public DateTime osRezObject(string inventory, LSL_Vector pos, LSL_Vector vel, LSL_Rotation rot, int param,
                                    LSL_Integer isRezAtRoot, LSL_Integer doRecoil, LSL_Integer SetDieAtEdge,
                                    LSL_Integer CheckPos)
        {
            InitLSL();
            return m_LSL_Api.llRezPrim(inventory, pos, vel, rot, param, isRezAtRoot == 1, doRecoil == 1,
                                       SetDieAtEdge == 1, CheckPos == 1);
        }

        /// <summary>
        ///   Convert a unix time to a llGetTimestamp() like string
        /// </summary>
        /// <returns></returns>
        public LSL_String osUnixTimeToTimestamp(long time)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.VeryLow, "osUnixTimeToTimestamp", m_host, "OSSL",
                                                   m_itemID)) return new LSL_String();
            const long baseTicks = 621355968000000000;
            const long tickResolution = 10000000;
            long epochTicks = (time * tickResolution) + baseTicks;
            DateTime date = new DateTime(epochTicks);

            return date.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
        }

        /// <summary>
        /// Get the description from an inventory item
        /// </summary>
        /// <returns>Item description</returns>
        public LSL_String osGetInventoryDesc(string item)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.VeryLow, "osGetInventoryDesc", m_host, "OSSL",
                                                   m_itemID)) return new LSL_String();

            lock (m_host.TaskInventory)
            {
                foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory)
                {
                    if (inv.Value.Name == item)
                    {
                        return inv.Value.Description.ToString(CultureInfo.InvariantCulture);
                    }
                }
            }

            return new LSL_String();
        }

        /// <summary>
        /// Invite user to the group this object is set to
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        public LSL_Integer osInviteToGroup(LSL_Key agentId)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.VeryLow, "osInviteToGroup", m_host, "OSSL",m_itemID)) return new LSL_Integer();
            UUID agent = new UUID((string) agentId);
            // groups module is required
            IGroupsModule groupsModule = World.RequestModuleInterface<IGroupsModule>();
            if (groupsModule == null) return ScriptBaseClass.FALSE;
            // object has to be set to a group, but not group owned
            if (m_host.GroupID == UUID.Zero || m_host.GroupID == m_host.OwnerID) return ScriptBaseClass.FALSE;
            // object owner has to be in that group and required permissions
            GroupMembershipData member = groupsModule.GetMembershipData(m_host.GroupID, m_host.OwnerID);
            if (member == null || (member.GroupPowers & (ulong)GroupPowers.Invite) == 0) return ScriptBaseClass.FALSE;
            // check if agent is in that group already
            //member = groupsModule.GetMembershipData(agent, m_host.GroupID, agent);
            //if (member != null) return ScriptBaseClass.FALSE;
            // invited agent has to be present in this scene
            if (World.GetScenePresence(agent) == null) return ScriptBaseClass.FALSE;
            groupsModule.InviteGroup(null, m_host.OwnerID, m_host.GroupID, agent, UUID.Zero);
            return ScriptBaseClass.TRUE;
        }
        /// <summary>
        /// Eject user from the group this object is set to
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        public LSL_Integer osEjectFromGroup(LSL_Key agentId)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.VeryLow, "osInviteToGroup", m_host, "OSSL", m_itemID)) return new LSL_Integer();
            UUID agent = new UUID((string) agentId);
            // groups module is required
            IGroupsModule groupsModule = World.RequestModuleInterface<IGroupsModule>();
            if (groupsModule == null) return ScriptBaseClass.FALSE;
            // object has to be set to a group, but not group owned
            if (m_host.GroupID == UUID.Zero || m_host.GroupID == m_host.OwnerID) return ScriptBaseClass.FALSE;
            // object owner has to be in that group and required permissions
            GroupMembershipData member = groupsModule.GetMembershipData(m_host.GroupID, m_host.OwnerID);
            if (member == null || (member.GroupPowers & (ulong)GroupPowers.Eject) == 0) return ScriptBaseClass.FALSE;
            // agent has to be in that group
            //member = groupsModule.GetMembershipData(agent, m_host.GroupID, agent);
            //if (member == null) return ScriptBaseClass.FALSE;
            // ejectee can be offline
            groupsModule.EjectGroupMember(null, m_host.OwnerID, m_host.GroupID, agent);
            return ScriptBaseClass.TRUE;
        }
 
        public void osCauseDamage(string avatar, double damage)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osCauseDamage", m_host, "OSSL", m_itemID)) return;

            UUID avatarId = new UUID(avatar);
            Vector3 pos = m_host.GetWorldPosition();

            IScenePresence presence = World.GetScenePresence(avatarId);
            if (presence != null)
            {
                IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
                if (parcelManagement != null)
                {
                    LandData land = parcelManagement.GetLandObject(pos.X, pos.Y).LandData;
                    if ((land.Flags & (uint)ParcelFlags.AllowDamage) == (uint)ParcelFlags.AllowDamage)
                    {
                        ICombatPresence cp = presence.RequestModuleInterface<ICombatPresence>();
                        cp.IncurDamage(World.GetScenePresence(m_host.OwnerID), damage);
                    }
                }
            }
        }

        public void osCauseDamage(string avatar, double damage, string regionName, LSL_Vector position,
                                  LSL_Vector lookat)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osCauseDamage", m_host, "OSSL", m_itemID)) return;

            UUID avatarId = new UUID(avatar);
            Vector3 pos = m_host.GetWorldPosition();

            IScenePresence presence = World.GetScenePresence(avatarId);
            if (presence != null)
            {
                IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
                if (parcelManagement != null)
                {
                    LandData land = parcelManagement.GetLandObject(pos.X, pos.Y).LandData;
                    if ((land.Flags & (uint)ParcelFlags.AllowDamage) == (uint)ParcelFlags.AllowDamage)
                    {
                        ICombatPresence cp = presence.RequestModuleInterface<ICombatPresence>();
                        cp.IncurDamage(World.GetScenePresence(m_host.OwnerID), damage, regionName,
                                       new Vector3((float)position.x, (float)position.y, (float)position.z),
                                       new Vector3((float)lookat.x, (float)lookat.y, (float)lookat.z));
                    }
                }
            }
        }

        public void osCauseHealing(string avatar, double healing)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "osCauseHealing", m_host, "OSSL", m_itemID))
                return;


            UUID avatarId = new UUID(avatar);
            IScenePresence presence = World.GetScenePresence(avatarId);
            if (presence != null)
            {
                Vector3 pos = m_host.GetWorldPosition();
                IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
                if (parcelManagement != null)
                {
                    LandData land = parcelManagement.GetLandObject(pos.X, pos.Y).LandData;
                    if ((land.Flags & (uint)ParcelFlags.AllowDamage) == (uint)ParcelFlags.AllowDamage)
                    {
                        ICombatPresence cp = presence.RequestModuleInterface<ICombatPresence>();
                        cp.IncurHealing(healing);
                    }
                }
            }
        }

        #endregion

        #region IScriptApi Members

        public void Initialize(IScriptModulePlugin ScriptEngine, ISceneChildEntity host, uint localID, UUID itemID,
                               ScriptProtectionModule module)
        {
            m_ScriptEngine = ScriptEngine;
            m_host = host;
            m_localID = localID;
            m_itemID = itemID;

            if (m_ScriptEngine.Config.GetBoolean("AllowOSFunctions", false))
                m_OSFunctionsEnabled = true;

            m_ScriptDelayFactor =
                m_ScriptEngine.Config.GetFloat("ScriptDelayFactor", 1.0f);
            m_ScriptDistanceFactor =
                m_ScriptEngine.Config.GetFloat("ScriptDistanceLimitFactor", 1.0f);
            ScriptProtection = module;
        }

        public IScriptApi Copy()
        {
            return new OSSL_Api();
        }

        public string Name
        {
            get { return "os"; }
        }

        public string InterfaceName
        {
            get { return "IOSSL_Api"; }
        }

        /// <summary>
        ///   We don't have to add any assemblies here
        /// </summary>
        public string[] ReferencedAssemblies
        {
            get { return new string[0]; }
        }

        /// <summary>
        ///   We use the default namespace, so we don't have any to add
        /// </summary>
        public string[] NamespaceAdditions
        {
            get { return new string[0]; }
        }

        #endregion

        public void Dispose()
        {
        }

        public override Object InitializeLifetimeService()
        {
            ILease lease = (ILease)base.InitializeLifetimeService();

            if (lease.CurrentState == LeaseState.Initial)
            {
                lease.InitialLeaseTime = TimeSpan.FromMinutes(0);
                //                lease.RenewOnCallTime = TimeSpan.FromSeconds(10.0);
                //                lease.SponsorshipTimeout = TimeSpan.FromMinutes(1.0);
            }
            return lease;
        }

        internal void OSSLError(string msg)
        {
            throw new Exception("OSSL Runtime Error: " + msg);
        }

        private void InitLSL()
        {
            if (m_LSL_Api != null)
                return;

            m_LSL_Api = (ILSL_Api)m_ScriptEngine.GetApi(m_itemID, "ll");
        }

        //
        //Dumps an error message on the debug console.
        //

        internal void OSSLShoutError(string message)
        {
            if (message.Length > 1023)
                message = message.Substring(0, 1023);

            IChatModule chatModule = World.RequestModuleInterface<IChatModule>();
            if (chatModule != null)
                chatModule.SimChat(message, ChatTypeEnum.Shout, ScriptBaseClass.DEBUG_CHANNEL,
                                   m_host.ParentEntity.RootChild.AbsolutePosition, m_host.Name, m_host.UUID, true, World);

            IWorldComm wComm = World.RequestModuleInterface<IWorldComm>();
            wComm.DeliverMessage(ChatTypeEnum.Shout, ScriptBaseClass.DEBUG_CHANNEL, m_host.Name, m_host.UUID, message);
        }

        /// <summary>
        ///   This is the new sleep implementation that allows for us to not freeze the script thread while we run
        /// </summary>
        /// <param name = "delay"></param>
        /// <returns></returns>
        protected DateTime PScriptSleep(int delay)
        {
            delay = (int)(delay * m_ScriptDelayFactor);
            if (delay == 0)
                return DateTime.Now;

            return DateTime.Now.AddMilliseconds(delay);
        }

        public void osSetRot(UUID target, Quaternion rotation)
        {
            // This function has no security. It can be used to destroy
            // arbitrary builds the user would normally have no rights to
            //
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.VeryHigh, "osSetRot", m_host, "OSSL", m_itemID)) return;


            IEntity entity;
            if (World.Entities.TryGetValue(target, out entity))
            {
                if (entity is SceneObjectGroup)
                    ((SceneObjectGroup)entity).Rotation = rotation;
                else if (entity is IScenePresence)
                    (entity).Rotation = rotation;
            }
            else
            {
                OSSLError("osSetRot: Invalid target");
            }
        }

        public DateTime TeleportAgent(UUID agentID, ulong regionHandle, Vector3 position, Vector3 lookAt)
        {
            IScenePresence presence = World.GetScenePresence(agentID);
            if (presence != null)
            {
                // agent must be over owners land to avoid abuse
                IParcelManagementModule parcelManagement = World.RequestModuleInterface<IParcelManagementModule>();
                if (parcelManagement != null)
                {
                    if (m_host.OwnerID != parcelManagement.GetLandObject(
                        presence.AbsolutePosition.X, presence.AbsolutePosition.Y).LandData.OwnerID &&
                        !World.Permissions.CanIssueEstateCommand(m_host.OwnerID, false))
                    {
                        return DateTime.Now;
                    }
                }
                presence.ControllingClient.SendTeleportStart((uint)TeleportFlags.ViaLocation);

                IEntityTransferModule entityTransfer = World.RequestModuleInterface<IEntityTransferModule>();
                if (entityTransfer != null)
                {
                    entityTransfer.RequestTeleportLocation(presence.ControllingClient,
                                                           regionHandle,
                                                           position,
                                                           lookAt, (uint)TeleportFlags.ViaLocation);
                }

                return PScriptSleep(5000);
            }
            return DateTime.Now;
        }
    }
}