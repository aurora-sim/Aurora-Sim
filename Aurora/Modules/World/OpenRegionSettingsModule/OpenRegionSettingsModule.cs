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
using System.IO;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework.Capabilities;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.OpenRegionSettingsModule
{
    public class OpenRegionSettingsModule : INonSharedRegionModule, IOpenRegionSettingsModule
    {
        #region IOpenRegionSettingsModule

        public float TerrainDetailScale
        {
            get { return m_settings.TerrainDetailScale; }
            set { m_settings.TerrainDetailScale = value; }
        }

        public bool OffsetOfUTCDST
        {
            get { return m_settings.OffsetOfUTCDST; }
            set { m_settings.OffsetOfUTCDST = value; }
        }

        public bool SetTeenMode
        {
            get { return m_settings.SetTeenMode; }
            set { m_settings.SetTeenMode = value; }
        }

        public float MaxDragDistance
        {
            get { return m_settings.MaxDragDistance; }
            set { m_settings.MaxDragDistance = value; }
        }

        public float DefaultDrawDistance
        {
            get { return m_settings.DefaultDrawDistance; }
            set { m_settings.DefaultDrawDistance = value; }
        }

        public float MaximumPrimScale
        {
            get { return m_settings.MaximumPrimScale; }
            set { m_settings.MaximumPrimScale = value; }
        }

        public float MinimumPrimScale
        {
            get { return m_settings.MinimumPrimScale; }
            set { m_settings.MinimumPrimScale = value; }
        }

        public float MaximumPhysPrimScale
        {
            get { return m_settings.MaximumPhysPrimScale; }
            set { m_settings.MaximumPhysPrimScale = value; }
        }

        public float MaximumHollowSize
        {
            get { return m_settings.MaximumHollowSize; }
            set { m_settings.MaximumHollowSize = value; }
        }

        public float MinimumHoleSize
        {
            get { return m_settings.MinimumHoleSize; }
            set { m_settings.MinimumHoleSize = value; }
        }

        public int MaximumLinkCount
        {
            get { return m_settings.MaximumLinkCount; }
            set { m_settings.MaximumLinkCount = value; }
        }

        public int MaximumLinkCountPhys
        {
            get { return m_settings.MaximumLinkCountPhys; }
            set { m_settings.MaximumLinkCountPhys = value; }
        }

        public OSDArray LSLCommands
        {
            get { return m_settings.LSLCommands; }
            set { m_settings.LSLCommands = value; }
        }

        public float WhisperDistance
        {
            get { return m_settings.WhisperDistance; }
            set { m_settings.WhisperDistance = value; }
        }

        public float SayDistance
        {
            get { return m_settings.SayDistance; }
            set { m_settings.SayDistance = value; }
        }

        public float ShoutDistance
        {
            get { return m_settings.ShoutDistance; }
            set { m_settings.ShoutDistance = value; }
        }

        public bool RenderWater
        {
            get { return m_settings.RenderWater; }
            set { m_settings.RenderWater = value; }
        }

        public int MaximumInventoryItemsTransfer
        {
            get { return m_settings.MaximumInventoryItemsTransfer; }
            set { m_settings.MaximumInventoryItemsTransfer = value; }
        }

        public bool DisplayMinimap
        {
            get { return m_settings.DisplayMinimap; }
            set { m_settings.DisplayMinimap = value; }
        }

        public bool AllowPhysicalPrims
        {
            get { return m_settings.AllowPhysicalPrims; }
            set { m_settings.AllowPhysicalPrims = value; }
        }

        public int OffsetOfUTC
        {
            get { return m_settings.OffsetOfUTC; }
            set { m_settings.OffsetOfUTC = value; }
        }

        public bool EnableTeenMode
        {
            get { return m_settings.EnableTeenMode; }
            set { m_settings.EnableTeenMode = value; }
        }

        public UUID DefaultUnderpants
        {
            get { return m_settings.DefaultUnderpants; }
            set { m_settings.DefaultUnderpants = value; }
        }

        public UUID DefaultUndershirt
        {
            get { return m_settings.DefaultUndershirt; }
            set { m_settings.DefaultUndershirt = value; }
        }

        public bool ClampPrimSizes
        {
            get { return m_settings.ClampPrimSizes; }
            set { m_settings.ClampPrimSizes = value; }
        }

        public bool ForceDrawDistance
        {
            get { return m_settings.ForceDrawDistance; }
            set { m_settings.ForceDrawDistance = value; }
        }

        public int ShowTags
        {
            get { return m_settings.ShowTags; }
            set { m_settings.ShowTags = value; }
        }

        public int MaxGroups
        {
            get { return m_settings.MaxGroups; }
            set { m_settings.MaxGroups = value; }
        }

        public bool AllowParcelWindLight
        {
            get { return m_settings.AllowParcelWindLight; }
            set { m_settings.AllowParcelWindLight = value; }
        }

        public void RegisterGenericValue(string key, string value)
        {
            additionalKVPs.Add(key, value);
        }

        #endregion

        #region Declares

        private readonly Dictionary<string, string> additionalKVPs = new Dictionary<string, string>();
        private IScene m_scene;
        private OpenRegionSettings m_settings;

        #endregion

        #region INonSharedRegionModule

        public void Initialise(IConfigSource source)
        {
        }

        public void Close()
        {
        }

        public void AddRegion(IScene scene)
        {
            m_scene = scene;
            scene.EventManager.OnMakeRootAgent += OnNewClient;
            scene.EventManager.OnRegisterCaps += OnRegisterCaps;
            scene.RegisterModuleInterface<IOpenRegionSettingsModule>(this);
            IOpenRegionSettingsConnector orsc = DataManager.DataManager.RequestPlugin<IOpenRegionSettingsConnector>();
            if (orsc != null)
                m_settings = orsc.GetSettings(scene.RegionInfo.RegionID);
            ReadConfig(scene);
        }

        public void RemoveRegion(IScene scene)
        {
        }

        public void RegionLoaded(IScene scene)
        {
            IChatModule chatmodule = scene.RequestModuleInterface<IChatModule>();
            if (chatmodule != null)
            {
                //Set default chat ranges
                m_settings.WhisperDistance = chatmodule.WhisperDistance;
                m_settings.SayDistance = chatmodule.SayDistance;
                m_settings.ShoutDistance = chatmodule.ShoutDistance;
            }
            /*IScriptModule scriptmodule = scene.RequestModuleInterface<IScriptModule>();
            if (scriptmodule != null)
            {
                List<string> FunctionNames = scriptmodule.GetAllFunctionNames();
                foreach (string FunctionName in FunctionNames)
                {
                    m_settings.LSLCommands.Add(OSD.FromString(FunctionName));
                }
            }*/
        }

        public string Name
        {
            get { return "WorldSettingsModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        #region CAPS

        public OSDMap OnRegisterCaps(UUID agentID, IHttpServer server)
        {
            OSDMap retVal = new OSDMap();
            retVal["DispatchOpenRegionSettings"] = CapsUtil.CreateCAPS("DispatchOpenRegionSettings", "");

            //Sets the OpenRegionSettings
            server.AddStreamHandler(new GenericStreamHandler("POST", retVal["DispatchOpenRegionSettings"],
                                                      delegate(string path, Stream request,
                                                        OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                      {
                                                          return DispatchOpenRegionSettings(request, agentID);
                                                      }));
            return retVal;
        }

        private byte[] DispatchOpenRegionSettings(Stream request, UUID agentID)
        {
            IScenePresence SP = m_scene.GetScenePresence(agentID);
            if (SP == null || !SP.Scene.Permissions.CanIssueEstateCommand(SP.UUID, false))
                return new byte[0];

            OSDMap rm = (OSDMap) OSDParser.DeserializeLLSDXml(request);

            m_settings.DefaultDrawDistance = rm["draw_distance"].AsInteger();
            m_settings.ForceDrawDistance = rm["force_draw_distance"].AsBoolean();
            m_settings.DisplayMinimap = rm["allow_minimap"].AsBoolean();
            m_settings.AllowPhysicalPrims = rm["allow_physical_prims"].AsBoolean();
            m_settings.MaxDragDistance = (float) rm["max_drag_distance"].AsReal();
            m_settings.MinimumHoleSize = (float) rm["min_hole_size"].AsReal();
            m_settings.MaximumHollowSize = (float) rm["max_hollow_size"].AsReal();
            m_settings.MaximumInventoryItemsTransfer = rm["max_inventory_items_transfer"].AsInteger();
            m_settings.MaximumLinkCount = (int) rm["max_link_count"].AsReal();
            m_settings.MaximumLinkCountPhys = (int) rm["max_link_count_phys"].AsReal();
            m_settings.MaximumPhysPrimScale = (float) rm["max_phys_prim_scale"].AsReal();
            m_settings.MaximumPrimScale = (float) rm["max_prim_scale"].AsReal();
            m_settings.MinimumPrimScale = (float) rm["min_prim_scale"].AsReal();
            m_settings.RenderWater = rm["render_water"].AsBoolean();
            m_settings.TerrainDetailScale = (float) rm["terrain_detail_scale"].AsReal();
            m_settings.ShowTags = (int) rm["show_tags"].AsReal();
            m_settings.MaxGroups = (int) rm["max_groups"].AsReal();
            m_settings.AllowParcelWindLight = rm["allow_parcel_windlight"].AsBoolean();
            m_settings.EnableTeenMode = rm["enable_teen_mode"].AsBoolean();
            m_settings.ClampPrimSizes = rm["enforce_max_build"].AsBoolean();

            IOpenRegionSettingsConnector connector =
                DataManager.DataManager.RequestPlugin<IOpenRegionSettingsConnector>();

            //Update the database
            if (connector != null)
                connector.SetSettings(m_scene.RegionInfo.RegionID, m_settings);

            //Update all clients about changes
            SendToAllClients();

            return new byte[0];
        }

        #endregion

        #region Setup

        private void ReadConfig(IScene scene)
        {
            //Set up the instance first
            //DEPRECATED (rSmythe 11/4/11) - Now set in viewer and Region Manager
            /*IConfig instanceSettings = m_source.Configs["InstanceSettings"];
            if (instanceSettings != null)
            {
                ReadSettings(instanceSettings);
            }
            IConfig regionSettings = m_source.Configs[scene.RegionInfo.RegionName];
            if (regionSettings != null)
            {
                ReadSettings(regionSettings);
            }*/
        }

        private void ReadSettings(IConfig instanceSettings)
        {
            m_settings.MaxDragDistance = instanceSettings.GetFloat("MaxDragDistance", MaxDragDistance);
            m_settings.DefaultDrawDistance = instanceSettings.GetFloat("DefaultDrawDistance", DefaultDrawDistance);


            m_settings.MaximumPrimScale = instanceSettings.GetFloat("MaximumPrimScale", MaximumPrimScale);
            m_settings.MinimumPrimScale = instanceSettings.GetFloat("MinimumPrimScale", MinimumPrimScale);
            m_settings.MaximumPhysPrimScale = instanceSettings.GetFloat("MaximumPhysPrimScale", MaximumPhysPrimScale);


            m_settings.MaximumHollowSize = instanceSettings.GetFloat("MaximumHollowSize", MaximumHollowSize);
            m_settings.MinimumHoleSize = instanceSettings.GetFloat("MinimumHoleSize", MinimumHoleSize);


            m_settings.MaximumLinkCount = instanceSettings.GetInt("MaximumLinkCount", MaximumLinkCount);
            m_settings.MaximumLinkCountPhys = instanceSettings.GetInt("MaximumLinkCountPhys", MaximumLinkCountPhys);


            m_settings.RenderWater = instanceSettings.GetBoolean("RenderWater", RenderWater);
            m_settings.MaximumInventoryItemsTransfer = instanceSettings.GetInt("MaximumInventoryItemsTransfer",
                                                                               MaximumInventoryItemsTransfer);
            m_settings.DisplayMinimap = instanceSettings.GetBoolean("DisplayMinimap", DisplayMinimap);
            m_settings.AllowPhysicalPrims = instanceSettings.GetBoolean("AllowPhysicalPrims", AllowPhysicalPrims);
            m_settings.ClampPrimSizes = instanceSettings.GetBoolean("ClampPrimSizes", ClampPrimSizes);
            m_settings.ForceDrawDistance = instanceSettings.GetBoolean("ForceDrawDistance", ForceDrawDistance);

            string offset = instanceSettings.GetString("OffsetOfUTC", OffsetOfUTC.ToString());
            int off;
            if (!int.TryParse(offset, out off))
            {
                if (offset == "SLT" || offset == "PST" || offset == "PDT")
                    off = -8;
                else if (offset == "UTC" || offset == "GMT")
                    off = 0;
            }
            m_settings.OffsetOfUTC = off;
            m_settings.OffsetOfUTCDST = instanceSettings.GetBoolean("OffsetOfUTCDST", OffsetOfUTCDST);
            m_settings.EnableTeenMode = instanceSettings.GetBoolean("EnableTeenMode", EnableTeenMode);
            m_settings.ShowTags = instanceSettings.GetInt("ShowTags", ShowTags);
            m_settings.MaxGroups = instanceSettings.GetInt("MaxGroups", MaxGroups);
            m_settings.AllowParcelWindLight = instanceSettings.GetBoolean("AllowParcelWindLight", AllowParcelWindLight);

            string defaultunderpants = instanceSettings.GetString("DefaultUnderpants",
                                                                  m_settings.DefaultUnderpants.ToString());
            UUID.TryParse(defaultunderpants, out m_settings.m_DefaultUnderpants);
            string defaultundershirt = instanceSettings.GetString("DefaultUndershirt",
                                                                  m_settings.DefaultUndershirt.ToString());
            UUID.TryParse(defaultundershirt, out m_settings.m_DefaultUndershirt);
        }

        #endregion

        #region Client and Event Queue

        public void OnNewClient(IScenePresence presence)
        {
            OpenRegionInfo(presence);
        }

        public void SendToAllClients()
        {
            m_scene.ForEachScenePresence(OpenRegionInfo);
        }

        public void OpenRegionInfo(IScenePresence presence)
        {
            OSD item = BuildOpenRegionInfo(presence);
            IEventQueueService eq = presence.Scene.RequestModuleInterface<IEventQueueService>();
            if (eq != null)
                eq.Enqueue(item, presence.UUID, presence.Scene.RegionInfo.RegionHandle);
        }

        public OSD BuildOpenRegionInfo(IScenePresence sp)
        {
            OSDMap map = new OSDMap();

            OSDMap body = new OSDMap();


            IOpenRegionSettingsConnector orsc = DataManager.DataManager.RequestPlugin<IOpenRegionSettingsConnector>();
            if (orsc != null)
            {
                if (sp.Scene.Permissions.CanIssueEstateCommand(sp.UUID, false))
                    body.Add("EditURL", OSD.FromString(orsc.AddOpenRegionSettingsHTMLPage(sp.Scene.RegionInfo.RegionID)));
            }

            if (m_settings.MaxDragDistance != -1)
                body.Add("MaxDragDistance", OSD.FromReal(m_settings.MaxDragDistance));

            if (m_settings.DefaultDrawDistance != -1)
            {
                body.Add("DrawDistance", OSD.FromReal(m_settings.DefaultDrawDistance));
                body.Add("ForceDrawDistance", OSD.FromInteger(m_settings.ForceDrawDistance ? 1 : 0));
            }

            if (m_settings.MaximumPrimScale != -1)
                body.Add("MaxPrimScale", OSD.FromReal(m_settings.MaximumPrimScale));
            if (m_settings.MinimumPrimScale != -1)
                body.Add("MinPrimScale", OSD.FromReal(m_settings.MinimumPrimScale));
            if (m_settings.MaximumPhysPrimScale != -1)
                body.Add("MaxPhysPrimScale", OSD.FromReal(m_settings.MaximumPhysPrimScale));

            if (m_settings.MaximumHollowSize != -1)
                body.Add("MaxHollowSize", OSD.FromReal(m_settings.MaximumHollowSize));
            if (m_settings.MinimumHoleSize != -1)
                body.Add("MinHoleSize", OSD.FromReal(m_settings.MinimumHoleSize));
            body.Add("EnforceMaxBuild", OSD.FromInteger(m_settings.ClampPrimSizes ? 1 : 0));

            if (m_settings.MaximumLinkCount != -1)
                body.Add("MaxLinkCount", OSD.FromInteger(m_settings.MaximumLinkCount));
            if (m_settings.MaximumLinkCountPhys != -1)
                body.Add("MaxLinkCountPhys", OSD.FromInteger(m_settings.MaximumLinkCountPhys));

            body.Add("LSLFunctions", m_settings.LSLCommands);

            body.Add("WhisperDistance", OSD.FromReal(m_settings.WhisperDistance));
            body.Add("SayDistance", OSD.FromReal(m_settings.SayDistance));
            body.Add("ShoutDistance", OSD.FromReal(m_settings.ShoutDistance));

            body.Add("RenderWater", OSD.FromInteger(m_settings.RenderWater ? 1 : 0));

            body.Add("TerrainDetailScale", OSD.FromReal(m_settings.TerrainDetailScale));

            if (m_settings.MaximumInventoryItemsTransfer != -1)
                body.Add("MaxInventoryItemsTransfer", OSD.FromInteger(m_settings.MaximumInventoryItemsTransfer));

            body.Add("AllowMinimap", OSD.FromInteger(m_settings.DisplayMinimap ? 1 : 0));
            body.Add("AllowPhysicalPrims", OSD.FromInteger(m_settings.AllowPhysicalPrims ? 1 : 0));
            body.Add("OffsetOfUTC", OSD.FromInteger(m_settings.OffsetOfUTC));
            body.Add("OffsetOfUTCDST", OSD.FromInteger(m_settings.OffsetOfUTCDST ? 1 : 0));
            body.Add("ToggleTeenMode", OSD.FromInteger(m_settings.EnableTeenMode ? 1 : 0));
            body.Add("SetTeenMode", OSD.FromInteger(m_settings.SetTeenMode ? 1 : 0));

            body.Add("ShowTags", OSD.FromInteger(m_settings.ShowTags));
            if (m_settings.MaxGroups != -1)
                body.Add("MaxGroups", OSD.FromInteger(m_settings.MaxGroups));
            body.Add("AllowParcelWindLight", OSD.FromInteger(m_settings.AllowParcelWindLight ? 1 : 0));

            //Add all the generic ones
            foreach (KeyValuePair<string, string> KVP in additionalKVPs)
            {
                body.Add(KVP.Key, OSD.FromString(KVP.Value));
            }

            map.Add("body", body);
            map.Add("message", OSD.FromString("OpenRegionInfo"));
            return map;
        }

        #endregion
    }
}