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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Framework.Capabilities;
using OpenSim.Region.Framework.Interfaces;
using Aurora.Framework.Servers.HttpServer;
using OpenMetaverse.StructuredData;
using OpenSim.Services.Interfaces;
using RegionFlags = OpenMetaverse.RegionFlags;

namespace Aurora.Modules.Estate
{
    public class EstateManagementModule : IEstateModule
    {
        private delegate void LookupUUIDS(List<UUID> uuidLst);

        private IScene m_scene;

        private EstateTerrainXferHandler TerrainUploader;

        public event ChangeDelegate OnRegionInfoChange;
        public event ChangeDelegate OnEstateInfoChange;
        public event MessageDelegate OnEstateMessage;

        #region Packet Data Responders

        private void sendDetailedEstateData(IClientAPI remote_client, UUID invoice)
        {
            uint sun = 0;

            if (!m_scene.RegionInfo.EstateSettings.UseGlobalTime)
                sun=(uint)(m_scene.RegionInfo.EstateSettings.SunPosition*1024.0) + 0x1800;
            UUID estateOwner = m_scene.RegionInfo.EstateSettings.EstateOwner;

            //if (m_scene.Permissions.IsGod(remote_client.AgentId))
            //    estateOwner = remote_client.AgentId;

            remote_client.SendDetailedEstateData(invoice,
                    m_scene.RegionInfo.EstateSettings.EstateName,
                    m_scene.RegionInfo.EstateSettings.EstateID,
                    m_scene.RegionInfo.EstateSettings.ParentEstateID,
                    GetEstateFlags(),
                    sun,
                    m_scene.RegionInfo.RegionSettings.Covenant,
                    m_scene.RegionInfo.RegionSettings.CovenantLastUpdated,
                    m_scene.RegionInfo.EstateSettings.AbuseEmail,
                    estateOwner);

            remote_client.SendEstateList(invoice,
                    (int)EstateTools.EstateAccessReplyDelta.EstateManagers,
                    m_scene.RegionInfo.EstateSettings.EstateManagers,
                    m_scene.RegionInfo.EstateSettings.EstateID);

            remote_client.SendEstateList(invoice,
                    (int)EstateTools.EstateAccessReplyDelta.AllowedUsers,
                    m_scene.RegionInfo.EstateSettings.EstateAccess,
                    m_scene.RegionInfo.EstateSettings.EstateID);

            remote_client.SendEstateList(invoice,
                    (int)EstateTools.EstateAccessReplyDelta.AllowedGroups,
                    m_scene.RegionInfo.EstateSettings.EstateGroups,
                    m_scene.RegionInfo.EstateSettings.EstateID);

            remote_client.SendBannedUserList(invoice,
                    m_scene.RegionInfo.EstateSettings.EstateBans,
                    m_scene.RegionInfo.EstateSettings.EstateID);
        }

        private void estateSetRegionInfoHandler(IClientAPI remoteClient, bool blockTerraform, bool noFly, bool allowDamage, bool AllowLandResell, int maxAgents, float objectBonusFactor,
                                                int matureLevel, bool restrictPushObject, bool allowParcelChanges)
        {

            m_scene.RegionInfo.RegionSettings.BlockTerraform = blockTerraform;

            m_scene.RegionInfo.RegionSettings.BlockFly = noFly;

            m_scene.RegionInfo.RegionSettings.AllowDamage = allowDamage;

            m_scene.RegionInfo.RegionSettings.RestrictPushing = restrictPushObject;



            m_scene.RegionInfo.RegionSettings.AllowLandResell = AllowLandResell;

            m_scene.RegionInfo.RegionSettings.AgentLimit = (byte) maxAgents;

            m_scene.RegionInfo.RegionSettings.ObjectBonus = objectBonusFactor;

            if (matureLevel <= 13)
                m_scene.RegionInfo.RegionSettings.Maturity = 0;
            else if (matureLevel <= 21)
                m_scene.RegionInfo.RegionSettings.Maturity = 1;
            else
                m_scene.RegionInfo.RegionSettings.Maturity = 2;

            m_scene.RegionInfo.RegionSettings.AllowLandJoinDivide = allowParcelChanges;

            m_scene.RegionInfo.RegionSettings.Save();
            TriggerRegionInfoChange();

            sendRegionInfoPacketToAll();
        }

        public OSDMap OnRegisterCaps(UUID agentID, IHttpServer server)
        {
            OSDMap retVal = new OSDMap();
            retVal["DispatchRegionInfo"] = CapsUtil.CreateCAPS("DispatchRegionInfo", "");

            server.AddStreamHandler(new GenericStreamHandler("POST", retVal["DispatchRegionInfo"],
                                                      delegate(string path, Stream request,
                                                        OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                      {
                                                          return DispatchRegionInfo(request, agentID);
                                                      }));
            retVal["EstateChangeInfo"] = CapsUtil.CreateCAPS("EstateChangeInfo", "");
            server.AddStreamHandler(new GenericStreamHandler("POST", retVal["EstateChangeInfo"],
                                                      delegate(string path, Stream request,
                                                        OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                      {
                                                          return EstateChangeInfo(request, agentID);
                                                      }));
            return retVal;
        }

        private byte[] EstateChangeInfo(Stream request, UUID agentID)
        {
            if (!m_scene.Permissions.CanIssueEstateCommand(agentID, false))
                return new byte[0];

            OSDMap rm = (OSDMap)OSDParser.DeserializeLLSDXml(request);

            string estate_name = rm["estate_name"].AsString();
            bool allow_direct_teleport = rm["allow_direct_teleport"].AsBoolean();
            bool allow_voice_chat = rm["allow_voice_chat"].AsBoolean();
            bool deny_age_unverified = rm["deny_age_unverified"].AsBoolean();
            bool deny_anonymous = rm["deny_anonymous"].AsBoolean();
            UUID invoice = rm["invoice"].AsUUID();
            bool is_externally_visible = rm["is_externally_visible"].AsBoolean();
            bool is_sun_fixed = rm["is_sun_fixed"].AsBoolean();
            string owner_abuse_email = rm["owner_abuse_email"].AsString();
            double sun_hour = rm["sun_hour"].AsReal ();
            m_scene.RegionInfo.EstateSettings.EstateName = estate_name;
            m_scene.RegionInfo.EstateSettings.AllowDirectTeleport = allow_direct_teleport;
            m_scene.RegionInfo.EstateSettings.AllowVoice = allow_voice_chat;
            m_scene.RegionInfo.EstateSettings.DenyAnonymous = deny_anonymous;
            m_scene.RegionInfo.EstateSettings.DenyIdentified = deny_age_unverified;
            m_scene.RegionInfo.EstateSettings.PublicAccess = is_externally_visible;
            m_scene.RegionInfo.EstateSettings.FixedSun = is_sun_fixed;
            m_scene.RegionInfo.EstateSettings.AbuseEmail = owner_abuse_email;
            if (sun_hour == 0)
            {
                m_scene.RegionInfo.EstateSettings.UseGlobalTime = true;
                m_scene.RegionInfo.EstateSettings.SunPosition = 0;
            }
            else
            {
                m_scene.RegionInfo.EstateSettings.UseGlobalTime = false;
                m_scene.RegionInfo.EstateSettings.SunPosition = sun_hour;
            }
            m_scene.RegionInfo.EstateSettings.Save();
            TriggerEstateInfoChange();

            TriggerEstateSunUpdate();

            IClientAPI remoteClient;
            m_scene.ClientManager.TryGetValue(agentID, out remoteClient);

            sendDetailedEstateData(remoteClient, invoice);

            return OSDParser.SerializeLLSDXmlBytes(new OSDMap());
        }

        private byte[] DispatchRegionInfo(Stream request, UUID agentID)
        {
            if (!m_scene.Permissions.CanIssueEstateCommand(agentID, false))
                return new byte[0];

            OSDMap rm = (OSDMap)OSDParser.DeserializeLLSDXml(request);

            int agent_limit = rm["agent_limit"].AsInteger();
            bool allow_damage = rm["allow_damage"].AsBoolean();
            bool allow_land_resell = rm["allow_land_resell"].AsBoolean();
            bool allow_parcel_changes = rm["allow_parcel_changes"].AsBoolean();
            bool block_fly = rm["block_fly"].AsBoolean();
            bool block_parcel_search = rm["block_parcel_search"].AsBoolean();
            bool block_terraform = rm["block_terraform"].AsBoolean();
            long prim_bonus = rm["prim_bonus"].AsLong();
            bool restrict_pushobject = rm["restrict_pushobject"].AsBoolean();
            int sim_access = rm["sim_access"].AsInteger();
            int minimum_agent_age = 0;
            if (rm.ContainsKey("minimum_agent_age"))
                minimum_agent_age = rm["minimum_agent_age"].AsInteger();


            m_scene.RegionInfo.RegionSettings.BlockTerraform = block_terraform;
            m_scene.RegionInfo.RegionSettings.BlockFly = block_fly;
            m_scene.RegionInfo.RegionSettings.AllowDamage = allow_damage;
            m_scene.RegionInfo.RegionSettings.RestrictPushing = restrict_pushobject;
            m_scene.RegionInfo.RegionSettings.AllowLandResell = allow_land_resell;
            m_scene.RegionInfo.RegionSettings.AgentLimit = agent_limit;
            m_scene.RegionInfo.RegionSettings.ObjectBonus = prim_bonus;
            m_scene.RegionInfo.RegionSettings.MinimumAge = minimum_agent_age;
            if (sim_access <= 13)
                m_scene.RegionInfo.RegionSettings.Maturity = 0;
            else if (sim_access <= 21)
                m_scene.RegionInfo.RegionSettings.Maturity = 1;
            else
                m_scene.RegionInfo.RegionSettings.Maturity = 2;
            m_scene.RegionInfo.RegionSettings.AllowLandJoinDivide = allow_parcel_changes;
            m_scene.RegionInfo.RegionSettings.BlockShowInSearch = block_parcel_search;

            m_scene.RegionInfo.RegionSettings.Save();
            TriggerRegionInfoChange();

            sendRegionInfoPacketToAll();

            return new byte[0];
        }

        public void setEstateTerrainBaseTexture(int level, UUID texture)
        {
            setEstateTerrainBaseTexture(null, level, texture);
            sendRegionHandshakeToAll();
        }
 
        public void setEstateTerrainBaseTexture(IClientAPI remoteClient, int level, UUID texture)
        {
            if (texture == UUID.Zero)
                return;

            switch (level)
            {
                case 0:
                    m_scene.RegionInfo.RegionSettings.TerrainTexture1 = texture;
                    break;
                case 1:
                    m_scene.RegionInfo.RegionSettings.TerrainTexture2 = texture;
                    break;
                case 2:
                    m_scene.RegionInfo.RegionSettings.TerrainTexture3 = texture;
                    break;
                case 3:
                    m_scene.RegionInfo.RegionSettings.TerrainTexture4 = texture;
                    break;
            }
        }

        public void setEstateTerrainTextureHeights(int corner, float lowValue, float highValue)
        {
            setEstateTerrainTextureHeights(null, corner, lowValue, highValue);
        }
 
        public void setEstateTerrainTextureHeights(IClientAPI client, int corner, float lowValue, float highValue)
        {
            if (m_scene.Permissions.CanIssueEstateCommand(client.AgentId, true))
            {
                switch (corner)
                {
                    case 0:
                        m_scene.RegionInfo.RegionSettings.Elevation1SW = lowValue;
                        m_scene.RegionInfo.RegionSettings.Elevation2SW = highValue;
                        break;
                    case 1:
                        m_scene.RegionInfo.RegionSettings.Elevation1NW = lowValue;
                        m_scene.RegionInfo.RegionSettings.Elevation2NW = highValue;
                        break;
                    case 2:
                        m_scene.RegionInfo.RegionSettings.Elevation1SE = lowValue;
                        m_scene.RegionInfo.RegionSettings.Elevation2SE = highValue;
                        break;
                    case 3:
                        m_scene.RegionInfo.RegionSettings.Elevation1NE = lowValue;
                        m_scene.RegionInfo.RegionSettings.Elevation2NE = highValue;
                        break;
                }
            }
        }

        private void handleCommitEstateTerrainTextureRequest(IClientAPI remoteClient)
        {
            m_scene.RegionInfo.RegionSettings.Save ();
            TriggerRegionInfoChange ();
            sendRegionHandshakeToAll ();
            //sendRegionInfoPacketToAll ();
        }

        public void setRegionTerrainSettings(UUID AgentID, float WaterHeight,
                float TerrainRaiseLimit, float TerrainLowerLimit,
                bool UseEstateSun, bool UseFixedSun, float SunHour,
                bool UseGlobal, bool EstateFixedSun, float EstateSunHour)
        {
            if (AgentID == UUID.Zero || m_scene.Permissions.CanIssueEstateCommand(AgentID, false))
            {
                // Water Height
                m_scene.RegionInfo.RegionSettings.WaterHeight = WaterHeight;
                //Update physics so that the water stuff works after a height change.
                ITerrainModule terrainModule = m_scene.RequestModuleInterface<ITerrainModule>();
                if (terrainModule != null)
                    terrainModule.UpdateWaterHeight(m_scene.RegionInfo.RegionSettings.WaterHeight);

                // Terraforming limits
                m_scene.RegionInfo.RegionSettings.TerrainRaiseLimit = TerrainRaiseLimit;
                m_scene.RegionInfo.RegionSettings.TerrainLowerLimit = TerrainLowerLimit;

                // Time of day / fixed sun
                m_scene.RegionInfo.RegionSettings.UseEstateSun = UseEstateSun;
                m_scene.RegionInfo.RegionSettings.FixedSun = UseFixedSun;
                m_scene.RegionInfo.RegionSettings.SunPosition = SunHour;

                TriggerEstateSunUpdate();

                //MainConsole.Instance.Debug("[ESTATE]: UFS: " + UseFixedSun.ToString());
                //MainConsole.Instance.Debug("[ESTATE]: SunHour: " + SunHour.ToString());

                sendRegionInfoPacketToAll();
                m_scene.RegionInfo.RegionSettings.Save();
                TriggerRegionInfoChange();
            }
        }

        private void handleEstateRestartSimRequest(IClientAPI remoteClient, int timeInSeconds)
        {
            IRestartModule restartModule = m_scene.RequestModuleInterface<IRestartModule>();
            if (restartModule != null)
            {
                List<int> times = new List<int>();
                while (timeInSeconds > 0)
                {
                    times.Add(timeInSeconds);
                    if (timeInSeconds > 300)
                        timeInSeconds -= 120;
                    else if (timeInSeconds > 30)
                        timeInSeconds -= 30;
                    else
                        timeInSeconds -= 15;
                }

                restartModule.ScheduleRestart(UUID.Zero, "Region will restart in {0}", times.ToArray(), true);
            }
        }

        private void handleChangeEstateCovenantRequest(IClientAPI remoteClient, UUID estateCovenantID)
        {
            m_scene.RegionInfo.RegionSettings.Covenant = estateCovenantID;
            m_scene.RegionInfo.RegionSettings.CovenantLastUpdated = Util.UnixTimeSinceEpoch();
            m_scene.RegionInfo.RegionSettings.Save();
            TriggerRegionInfoChange();
        }

        private enum AccessDeltaRequest
        {
            ApplyToAllEstates = 1 << 0,
            ApplyToManagedEstates = 1 << 1,
            AddAllowedUser = 1 << 2,
            RemoveAllowedUser = 1 << 3,
            AddAllowedGroup = 1 << 4,
            RemoveAllowedGroup = 1 << 5,
            AddBannedUser = 1 << 6,
            RemoveBannedUser = 1 << 7,
            AddEstateManager = 1 << 8,
            RemoveEstateManager = 1 << 9,
            MoreToCome = 1 << 10
        }

        private void handleEstateAccessDeltaRequest(IClientAPI remote_client, UUID invoice, int estateAccessType, UUID user)
        {
            // EstateAccessDelta handles Estate Managers, Sim Access, Sim Banlist, allowed Groups..  etc.

            if (user == m_scene.RegionInfo.EstateSettings.EstateOwner)
                return; // never process EO

            if ((estateAccessType & (int)AccessDeltaRequest.AddAllowedUser) != 0) // User add
            {
                if (m_scene.Permissions.CanIssueEstateCommand(remote_client.AgentId, true))
                {
                    IEstateConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IEstateConnector> ();
                    if ((estateAccessType & 1) != 0 && connector != null) // All estates
                    {
                        List<EstateSettings> estateIDs = connector.GetEstates (remote_client.AgentId);
                        foreach (EstateSettings estate in estateIDs)
                        {
                            if (estate.EstateID != m_scene.RegionInfo.EstateSettings.EstateID)
                            {
                                estate.AddEstateUser (user);
                                estate.Save ();
                            }
                        }
                    }
                    m_scene.RegionInfo.EstateSettings.AddEstateUser(user);
                    if ((estateAccessType & (int)AccessDeltaRequest.MoreToCome) == 0) //1024 means more than one is being sent
                    {
                        m_scene.RegionInfo.EstateSettings.Save();
                        TriggerEstateInfoChange();
                    }
                }
                else
                {
                    remote_client.SendAlertMessage("Method EstateAccessDelta Failed, you don't have permissions");
                }
            }
            if ((estateAccessType & (int)AccessDeltaRequest.RemoveAllowedUser) != 0) // User remove
            {
                IEstateConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IEstateConnector> ();
                if ((estateAccessType & 1) != 0 && connector != null) // All estates
                {
                    List<EstateSettings> estateIDs = connector.GetEstates (remote_client.AgentId);
                    foreach (EstateSettings estate in estateIDs)
                    {
                        if (estate.EstateID != m_scene.RegionInfo.EstateSettings.EstateID)
                        {
                            estate.RemoveEstateUser (user);
                            estate.Save ();
                        }
                    }
                }
                if (m_scene.Permissions.CanIssueEstateCommand(remote_client.AgentId, true) || m_scene.Permissions.BypassPermissions())
                {
                    m_scene.RegionInfo.EstateSettings.RemoveEstateUser(user);
                    if ((estateAccessType & (int)AccessDeltaRequest.MoreToCome) == 0) //1024 means more than one is being sent
                    {
                        m_scene.RegionInfo.EstateSettings.Save();
                        TriggerEstateInfoChange();
                    }
                }
                else
                {
                    remote_client.SendAlertMessage("Method EstateAccessDelta Failed, you don't have permissions");
                }
            }
            if ((estateAccessType & (int)AccessDeltaRequest.AddAllowedGroup) != 0) // Group add
            {
                if (m_scene.Permissions.CanIssueEstateCommand(remote_client.AgentId, true) || m_scene.Permissions.BypassPermissions())
                {
                    IEstateConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IEstateConnector> ();
                    if ((estateAccessType & 1) != 0 && connector != null) // All estates
                    {
                        List<EstateSettings> estateIDs = connector.GetEstates (remote_client.AgentId);
                        foreach (EstateSettings estate in estateIDs)
                        {
                            if (estate.EstateID != m_scene.RegionInfo.EstateSettings.EstateID)
                            {
                                estate.AddEstateGroup (user);
                                estate.Save ();
                            }
                        }
                    }
                    m_scene.RegionInfo.EstateSettings.AddEstateGroup(user);
                    if ((estateAccessType & (int)AccessDeltaRequest.MoreToCome) == 0) //1024 means more than one is being sent
                    {
                        m_scene.RegionInfo.EstateSettings.Save();
                        TriggerEstateInfoChange();
                    }
                }
                else
                {
                    remote_client.SendAlertMessage("Method EstateAccessDelta Failed, you don't have permissions");
                }
            }
            if ((estateAccessType & (int)AccessDeltaRequest.RemoveAllowedGroup) != 0) // Group remove
            {
                if (m_scene.Permissions.CanIssueEstateCommand(remote_client.AgentId, true) || m_scene.Permissions.BypassPermissions())
                {
                    IEstateConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IEstateConnector> ();
                    if ((estateAccessType & 1) != 0 && connector != null) // All estates
                    {
                        List<EstateSettings> estateIDs = connector.GetEstates (remote_client.AgentId);
                        foreach (EstateSettings estate in estateIDs)
                        {
                            if (estate.EstateID != m_scene.RegionInfo.EstateSettings.EstateID)
                            {
                                estate.RemoveEstateGroup (user);
                                estate.Save ();
                            }
                        }
                    }
                    m_scene.RegionInfo.EstateSettings.RemoveEstateGroup(user);
                    if ((estateAccessType & (int)AccessDeltaRequest.MoreToCome) == 0) //1024 means more than one is being sent
                    {
                        m_scene.RegionInfo.EstateSettings.Save();
                        TriggerEstateInfoChange();
                    }
                }
                else
                {
                    remote_client.SendAlertMessage("Method EstateAccessDelta Failed, you don't have permissions");
                }
            }
            if ((estateAccessType & (int)AccessDeltaRequest.AddBannedUser) != 0) // Ban add
            {
                if (m_scene.Permissions.CanIssueEstateCommand(remote_client.AgentId, false) || m_scene.Permissions.BypassPermissions())
                {
                    IEstateConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IEstateConnector> ();
                    if ((estateAccessType & 1) != 0 && connector != null) // All estates
                    {
                        List<EstateSettings> estateIDs = connector.GetEstates (remote_client.AgentId);
                        foreach (EstateSettings estate in estateIDs)
                        {
                            if (estate.EstateID != m_scene.RegionInfo.EstateSettings.EstateID)
                            {
                                EstateBan[] innerbanlistcheck = estate.EstateBans;

#if (!ISWIN)
                                bool inneralreadyInList = false;
                                foreach (EstateBan t in innerbanlistcheck)
                                {
                                    if (user == t.BannedUserID)
                                    {
                                        inneralreadyInList = true;
                                        break;
                                    }
                                }
#else
                                bool inneralreadyInList = innerbanlistcheck.Any(t => user == t.BannedUserID);
#endif

                                if (!inneralreadyInList)
                                {
                                    EstateBan item = new EstateBan {BannedUserID = user, EstateID = estate.EstateID};

                                    IScenePresence SP = m_scene.GetScenePresence (user);
                                    item.BannedHostAddress = (SP != null) ? ((System.Net.IPEndPoint)SP.ControllingClient.GetClientEP ()).Address.ToString () : "0.0.0.0";
                                    item.BannedHostIPMask = (SP != null) ? ((System.Net.IPEndPoint)SP.ControllingClient.GetClientEP ()).Address.ToString () : "0.0.0.0";
                                    item.BannedHostNameMask = (SP != null) ? ((System.Net.IPEndPoint)SP.ControllingClient.GetClientEP ()).Address.ToString () : "0.0.0.0";
                                    estate.AddBan (item);
                                    estate.Save ();
                                }
                            }
                        }
                    }
                    EstateBan[] banlistcheck = m_scene.RegionInfo.EstateSettings.EstateBans;

#if (!ISWIN)
                    bool alreadyInList = false;
                    foreach (EstateBan t in banlistcheck)
                    {
                        if (user == t.BannedUserID)
                        {
                            alreadyInList = true;
                            break;
                        }
                    }
#else
                    bool alreadyInList = banlistcheck.Any(t => user == t.BannedUserID);
#endif

                    if (!alreadyInList)
                    {
                        EstateBan item = new EstateBan
                                             {
                                                 BannedUserID = user,
                                                 EstateID = m_scene.RegionInfo.EstateSettings.EstateID,
                                                 BannedHostAddress = "0.0.0.0"
                                             };

                        IScenePresence SP = m_scene.GetScenePresence (user);
                        item.BannedHostIPMask = (SP != null) ? ((System.Net.IPEndPoint)SP.ControllingClient.GetClientEP()).Address.ToString() : "0.0.0.0";

                        m_scene.RegionInfo.EstateSettings.AddBan(item);

                        //Trigger the event
                        m_scene.AuroraEventManager.FireGenericEventHandler("BanUser", user);

                        if ((estateAccessType & (int)AccessDeltaRequest.MoreToCome) == 0) //1024 means more than one is being sent
                        {
                            m_scene.RegionInfo.EstateSettings.Save();
                            TriggerEstateInfoChange();
                        }

                        if (SP != null)
                        {
                            if (!SP.IsChildAgent)
                            {
                                IEntityTransferModule transferModule = m_scene.RequestModuleInterface<IEntityTransferModule>();
                                if (transferModule != null)
                                    transferModule.TeleportHome(user, SP.ControllingClient);
                            }
                            else
                            {
                                //Close them in the sim
                                IEntityTransferModule transferModule = SP.Scene.RequestModuleInterface<IEntityTransferModule> ();
                                if (transferModule != null)
                                    transferModule.IncomingCloseAgent (SP.Scene, SP.UUID);
                            }
                        }
                    }
                    else
                    {
                        remote_client.SendAlertMessage("User is already on the region ban list");
                    }
                }
                else
                {
                    remote_client.SendAlertMessage("Method EstateAccessDelta Failed, you don't have permissions");
                }
            }
            if ((estateAccessType & (int)AccessDeltaRequest.RemoveBannedUser) != 0) // Ban remove
            {
                if (m_scene.Permissions.CanIssueEstateCommand(remote_client.AgentId, false) || m_scene.Permissions.BypassPermissions())
                {
                    IEstateConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IEstateConnector> ();
                    if ((estateAccessType & 1) != 0 && connector != null) // All estates
                    {
                        List<EstateSettings> estateIDs = connector.GetEstates (remote_client.AgentId);
                        foreach (EstateSettings estate in estateIDs)
                        {
                            if (estate.EstateID != m_scene.RegionInfo.EstateSettings.EstateID)
                            {
                                EstateBan[] innerbanlistcheck = m_scene.RegionInfo.EstateSettings.EstateBans;

                                bool inneralreadyInList = false;
                                EstateBan innerlistitem = null;

                                foreach (EstateBan t in innerbanlistcheck)
                                {
                                    if (user == t.BannedUserID)
                                    {
                                        inneralreadyInList = true;
                                        innerlistitem = t;
                                        break;
                                    }
                                }

                                if (inneralreadyInList)
                                {
                                    m_scene.RegionInfo.EstateSettings.RemoveBan (innerlistitem.BannedUserID);
                                }
                                estate.Save ();
                            }
                        }
                    }
                    EstateBan[] banlistcheck = m_scene.RegionInfo.EstateSettings.EstateBans;

                    bool alreadyInList = false;
                    EstateBan listitem = null;

                    foreach (EstateBan t in banlistcheck)
                    {
                        if (user == t.BannedUserID)
                        {
                            alreadyInList = true;
                            listitem = t;
                            break;
                        }
                    }

                    //Trigger the event
                    m_scene.AuroraEventManager.FireGenericEventHandler("UnBanUser", user);

                    if (alreadyInList)
                    {
                        m_scene.RegionInfo.EstateSettings.RemoveBan(listitem.BannedUserID);
                        if ((estateAccessType & (int)AccessDeltaRequest.MoreToCome) == 0) //1024 means more than one is being sent
                        {
                            m_scene.RegionInfo.EstateSettings.Save();
                            TriggerEstateInfoChange();
                        }
                    }
                    else
                    {
                        remote_client.SendAlertMessage("User is not on the region ban list");
                    }
                }
                else
                {
                    remote_client.SendAlertMessage("Method EstateAccessDelta Failed, you don't have permissions");
                }
            }
            if ((estateAccessType & (int)AccessDeltaRequest.AddEstateManager) != 0) // Manager add
            {
                if (m_scene.Permissions.CanIssueEstateCommand(remote_client.AgentId, true) || m_scene.Permissions.BypassPermissions())
                {
                    IEstateConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IEstateConnector> ();
                    if ((estateAccessType & 1) != 0 && connector != null) // All estates
                    {
                        List<EstateSettings> estateIDs = connector.GetEstates (remote_client.AgentId);
                        foreach (EstateSettings estate in estateIDs)
                        {
                            if (estate.EstateID != m_scene.RegionInfo.EstateSettings.EstateID)
                            {
                                estate.AddEstateManager (user);
                                estate.Save ();
                            }
                        }
                    }
                    m_scene.RegionInfo.EstateSettings.AddEstateManager(user);
                    if ((estateAccessType & (int)AccessDeltaRequest.MoreToCome) == 0) //1024 means more than one is being sent
                    {
                        m_scene.RegionInfo.EstateSettings.Save();
                        TriggerEstateInfoChange();
                    }
                }
                else
                {
                    remote_client.SendAlertMessage("Method EstateAccessDelta Failed, you don't have permissions");
                }
            }
            if ((estateAccessType & (int)AccessDeltaRequest.RemoveEstateManager) != 0) // Manager remove
            {
                if (m_scene.Permissions.CanIssueEstateCommand(remote_client.AgentId, true) || m_scene.Permissions.BypassPermissions())
                {
                    IEstateConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IEstateConnector> ();
                    if ((estateAccessType & 1) != 0 && connector != null) // All estates
                    {
                        List<EstateSettings> estateIDs = connector.GetEstates (remote_client.AgentId);
                        foreach (EstateSettings estate in estateIDs)
                        {
                            if (estate.EstateID != m_scene.RegionInfo.EstateSettings.EstateID)
                            {
                                estate.RemoveEstateManager (user);
                                estate.Save ();
                            }
                        }
                    }
                    m_scene.RegionInfo.EstateSettings.RemoveEstateManager(user);
                    if ((estateAccessType & (int)AccessDeltaRequest.MoreToCome) == 0) //1024 means more than one is being sent
                    {
                        m_scene.RegionInfo.EstateSettings.Save();
                        TriggerEstateInfoChange();
                    }
                }
                else
                {
                    remote_client.SendAlertMessage("Method EstateAccessDelta Failed, you don't have permissions");
                }
            }
            if ((estateAccessType & (int)AccessDeltaRequest.MoreToCome) == 0) //1024 means more than one is being sent
            {
                remote_client.SendEstateList(invoice, (int)EstateTools.EstateAccessReplyDelta.AllowedUsers, m_scene.RegionInfo.EstateSettings.EstateAccess, m_scene.RegionInfo.EstateSettings.EstateID);
                remote_client.SendEstateList(invoice, (int)EstateTools.EstateAccessReplyDelta.AllowedGroups, m_scene.RegionInfo.EstateSettings.EstateGroups, m_scene.RegionInfo.EstateSettings.EstateID);
                remote_client.SendBannedUserList(invoice, m_scene.RegionInfo.EstateSettings.EstateBans, m_scene.RegionInfo.EstateSettings.EstateID);
                remote_client.SendEstateList(invoice, (int)EstateTools.EstateAccessReplyDelta.EstateManagers, m_scene.RegionInfo.EstateSettings.EstateManagers, m_scene.RegionInfo.EstateSettings.EstateID);
            }
        }

        private void SendSimulatorBlueBoxMessage(
            IClientAPI remote_client, UUID invoice, UUID senderID, UUID sessionID, string senderName, string message)
        {
            IDialogModule dm = m_scene.RequestModuleInterface<IDialogModule>();
            
            if (dm != null)
                dm.SendNotificationToUsersInRegion(senderID, senderName, message);
        }

        private void SendEstateBlueBoxMessage(
            IClientAPI remote_client, UUID invoice, UUID senderID, UUID sessionID, string senderName, string message)
        {
            TriggerEstateMessage(senderID, senderName, message);
        }

        private void handleEstateDebugRegionRequest(IClientAPI remote_client, UUID invoice, UUID senderID, bool scripted, bool collisionEvents, bool physics)
        {
            m_scene.RegionInfo.RegionSettings.DisablePhysics = physics;

            m_scene.RegionInfo.RegionSettings.DisableScripts = scripted;

            m_scene.RegionInfo.RegionSettings.DisableCollisions = collisionEvents;


            m_scene.RegionInfo.RegionSettings.Save();
            TriggerRegionInfoChange();

            SetSceneCoreDebug(scripted, collisionEvents, physics);
        }

        public void SetSceneCoreDebug(bool ScriptEngine, bool CollisionEvents, bool PhysicsEngine)
        {
            if (m_scene.RegionInfo.RegionSettings.DisableScripts == !ScriptEngine)
            {
                if (ScriptEngine)
                {
                    MainConsole.Instance.Info("[SCENEDEBUG]: Stopping all Scripts in Scene");
                    IScriptModule mod = m_scene.RequestModuleInterface<IScriptModule>();
                    mod.StopAllScripts();
                }
                else
                {
                    MainConsole.Instance.Info("[SCENEDEBUG]: Starting all Scripts in Scene");

                    ISceneEntity[] entities = m_scene.Entities.GetEntities ();
                    foreach (ISceneEntity ent in entities)
                    {
                        ent.CreateScriptInstances(0, false, StateSource.NewRez, UUID.Zero, false);
                    }
                }
                m_scene.RegionInfo.RegionSettings.DisableScripts = !ScriptEngine;
            }

            if (m_scene.RegionInfo.RegionSettings.DisablePhysics == !PhysicsEngine)
            {
                m_scene.RegionInfo.RegionSettings.DisablePhysics = !PhysicsEngine;
            }

            if (m_scene.RegionInfo.RegionSettings.DisableCollisions == !CollisionEvents)
            {
                m_scene.RegionInfo.RegionSettings.DisableCollisions = !CollisionEvents;
                m_scene.PhysicsScene.DisableCollisions = m_scene.RegionInfo.RegionSettings.DisableCollisions;
            }
        }

        private void handleEstateTeleportOneUserHomeRequest(IClientAPI remover_client, UUID invoice, UUID senderID, UUID prey)
        {
            if (!m_scene.Permissions.CanIssueEstateCommand(remover_client.AgentId, false))
                return;

            if (prey != UUID.Zero)
            {
                IScenePresence s = m_scene.GetScenePresence (prey);
                if (s != null)
                {
                    IEntityTransferModule transferModule = m_scene.RequestModuleInterface<IEntityTransferModule>();
                    if (transferModule != null)
                        transferModule.TeleportHome(prey, s.ControllingClient);
                }
            }
        }

        private void handleEstateTeleportAllUsersHomeRequest(IClientAPI remover_client, UUID invoice, UUID senderID)
        {
            if (!m_scene.Permissions.CanIssueEstateCommand(remover_client.AgentId, false))
                return;

            m_scene.ForEachScenePresence(delegate(IScenePresence sp)
            {
                if (sp.UUID != senderID)
                {
                    IScenePresence p = m_scene.GetScenePresence (sp.UUID);
                    // make sure they are still there, we could be working down a long list
                    // Also make sure they are actually in the region
                    if (p != null && !p.IsChildAgent)
                    {
                        IEntityTransferModule transferModule = m_scene.RequestModuleInterface<IEntityTransferModule>();
                        if (transferModule != null)
                            transferModule.TeleportHome(p.UUID, p.ControllingClient);
                    }
                }
            });
        }
        private void AbortTerrainXferHandler(IClientAPI remoteClient, ulong XferID)
        {
            if (TerrainUploader != null)
            {
                lock (TerrainUploader)
                {
                    if (XferID == TerrainUploader.XferID)
                    {
                        remoteClient.OnXferReceive -= TerrainUploader.XferReceive;
                        remoteClient.OnAbortXfer -= AbortTerrainXferHandler;
                        TerrainUploader.TerrainUploadDone -= HandleTerrainApplication;

                        TerrainUploader = null;
                        remoteClient.SendAlertMessage("Terrain Upload aborted by the client");
                    }
                }
            }

        }
        private void HandleTerrainApplication(string filename, byte[] terrainData, IClientAPI remoteClient)
        {
            lock (TerrainUploader)
            {
                remoteClient.OnXferReceive -= TerrainUploader.XferReceive;
                remoteClient.OnAbortXfer -= AbortTerrainXferHandler;
                TerrainUploader.TerrainUploadDone -= HandleTerrainApplication;

                TerrainUploader = null;
            }
            remoteClient.SendAlertMessage("Terrain Upload Complete. Loading....");
            ITerrainModule terr = m_scene.RequestModuleInterface<ITerrainModule>();

            if (terr != null)
            {
                MainConsole.Instance.Warn("[CLIENT]: Got Request to Send Terrain in region " + m_scene.RegionInfo.RegionName);

                try
                {
                    FileInfo x = new FileInfo(filename);

                    if (x.Extension == ".oar") // It's an oar file
                    {
                        bool check = false;
                        while (!check)
                        {
                            if (File.Exists(filename))
                            {
                                filename = "duplicate" + filename;
                            }
                            else
                                check = true;
                        }
                        FileStream input = new FileStream(filename, FileMode.CreateNew);
                        input.Write(terrainData, 0, terrainData.Length);
                        input.Close();
                        MainConsole.Instance.RunCommand("load oar " + filename);
                        remoteClient.SendAlertMessage("Your oar file was loaded. It may take a few moments to appear.");
                    }
                    else
                    {
                        MemoryStream terrainStream = new MemoryStream(terrainData);
                        terr.LoadFromStream(filename, terrainStream);
                        terrainStream.Close();
                        remoteClient.SendAlertMessage("Your terrain was loaded as a ." + x.Extension + " file. It may take a few moments to appear.");
                    }
                }
                catch (IOException e)
                {
                    MainConsole.Instance.ErrorFormat("[TERRAIN]: Error Saving a terrain file uploaded via the estate tools.  It gave us the following error: {0}", e);
                    remoteClient.SendAlertMessage("There was an IO Exception loading your terrain.  Please check free space.");

                    return;
                }
                catch (SecurityException e)
                {
                    MainConsole.Instance.ErrorFormat("[TERRAIN]: Error Saving a terrain file uploaded via the estate tools.  It gave us the following error: {0}", e);
                    remoteClient.SendAlertMessage("There was a security Exception loading your terrain.  Please check the security on the simulator drive");

                    return;
                }
                catch (UnauthorizedAccessException e)
                {
                    MainConsole.Instance.ErrorFormat("[TERRAIN]: Error Saving a terrain file uploaded via the estate tools.  It gave us the following error: {0}", e);
                    remoteClient.SendAlertMessage("There was a security Exception loading your terrain.  Please check the security on the simulator drive");

                    return;
                }
                catch (Exception e)
                {
                    MainConsole.Instance.ErrorFormat("[TERRAIN]: Error loading a terrain file uploaded via the estate tools.  It gave us the following error: {0}", e);
                    remoteClient.SendAlertMessage("There was a general error loading your terrain.  Please fix the terrain file and try again");
                }
            }
            else
            {
                remoteClient.SendAlertMessage("Unable to apply terrain.  Cannot get an instance of the terrain module");
            }
        }

        private void handleUploadTerrain(IClientAPI remote_client, string clientFileName)
        {
            if (TerrainUploader == null)
            {
                remote_client.SendAlertMessage("Uploading terrain file...");
                TerrainUploader = new EstateTerrainXferHandler(remote_client, clientFileName);
                lock (TerrainUploader)
                {
                    remote_client.OnXferReceive += TerrainUploader.XferReceive;
                    remote_client.OnAbortXfer += AbortTerrainXferHandler;
                    TerrainUploader.TerrainUploadDone += HandleTerrainApplication;
                }
                TerrainUploader.RequestStartXfer(remote_client);
            }
            else
            {
                remote_client.SendAlertMessage("Another Terrain Upload is in progress.  Please wait your turn!");
            }

        }
        private void handleTerrainRequest(IClientAPI remote_client, string clientFileName)
        {
            // Save terrain here
            ITerrainModule terr = m_scene.RequestModuleInterface<ITerrainModule>();
            
            if (terr != null)
            {
                MainConsole.Instance.Warn("[CLIENT]: Got Request to Send Terrain in region " + m_scene.RegionInfo.RegionName);
                if (File.Exists(Util.dataDir() + "/terrain.raw"))
                {
                    File.Delete(Util.dataDir() + "/terrain.raw");
                }
                terr.SaveToFile(Util.dataDir() + "/terrain.raw");

                FileStream input = new FileStream(Util.dataDir() + "/terrain.raw", FileMode.Open);
                byte[] bdata = new byte[input.Length];
                input.Read(bdata, 0, (int)input.Length);
                remote_client.SendAlertMessage("Terrain file written, starting download...");
                IXfer xfer = m_scene.RequestModuleInterface<IXfer>();
                if(xfer != null)
                    xfer.AddNewFile("terrain.raw", bdata);
                // Tell client about it
                MainConsole.Instance.Warn("[CLIENT]: Sending Terrain to " + remote_client.Name);
                remote_client.SendInitiateDownload("terrain.raw", clientFileName);
            }
        }

        private void HandleRegionInfoRequest(IClientAPI remote_client)
        {
            RegionInfoForEstateMenuArgs args = new RegionInfoForEstateMenuArgs
                                                   {
                                                       billableFactor = m_scene.RegionInfo.EstateSettings.BillableFactor,
                                                       estateID = m_scene.RegionInfo.EstateSettings.EstateID,
                                                       maxAgents = (byte) m_scene.RegionInfo.RegionSettings.AgentLimit,
                                                       objectBonusFactor =
                                                           (float) m_scene.RegionInfo.RegionSettings.ObjectBonus,
                                                       parentEstateID = m_scene.RegionInfo.EstateSettings.ParentEstateID,
                                                       pricePerMeter = m_scene.RegionInfo.EstateSettings.PricePerMeter,
                                                       redirectGridX = m_scene.RegionInfo.EstateSettings.RedirectGridX,
                                                       redirectGridY = m_scene.RegionInfo.EstateSettings.RedirectGridY,
                                                       regionFlags = GetRegionFlags(),
                                                       simAccess = m_scene.RegionInfo.AccessLevel,
                                                       sunHour = (float) m_scene.RegionInfo.RegionSettings.SunPosition,
                                                       terrainLowerLimit =
                                                           (float) m_scene.RegionInfo.RegionSettings.TerrainLowerLimit,
                                                       terrainRaiseLimit =
                                                           (float) m_scene.RegionInfo.RegionSettings.TerrainRaiseLimit,
                                                       useEstateSun = m_scene.RegionInfo.RegionSettings.UseEstateSun,
                                                       waterHeight =
                                                           (float) m_scene.RegionInfo.RegionSettings.WaterHeight,
                                                       simName = m_scene.RegionInfo.RegionName,
                                                       regionType = m_scene.RegionInfo.RegionType
                                                   };

            remote_client.SendRegionInfoToEstateMenu(args);
        }

        private void HandleEstateCovenantRequest(IClientAPI remote_client)
        {
            remote_client.SendEstateCovenantInformation(m_scene.RegionInfo.RegionSettings.Covenant,
                m_scene.RegionInfo.RegionSettings.CovenantLastUpdated);
        }

        private void HandleLandStatRequest(int parcelID, uint reportType, uint requestFlags, string filter, IClientAPI remoteClient)
        {
            if (!m_scene.Permissions.CanIssueEstateCommand(remoteClient.AgentId, false))
                return;

            Dictionary<uint, float> SceneData = new Dictionary<uint,float>();
            
            if (reportType == (uint)EstateTools.LandStatReportType.TopColliders)
            {
                SceneData = m_scene.PhysicsScene.GetTopColliders();
            }
            else if (reportType == (uint)EstateTools.LandStatReportType.TopScripts)
            {
                IScriptModule scriptModule = m_scene.RequestModuleInterface<IScriptModule>();
                SceneData = scriptModule.GetTopScripts(m_scene.RegionInfo.RegionID);
            }

            List<LandStatReportItem> SceneReport = new List<LandStatReportItem>();
            lock (SceneData)
            {
                foreach (uint obj in SceneData.Keys)
                {
                    ISceneChildEntity prt = m_scene.GetSceneObjectPart (obj);
                    if (prt != null)
                    {
                        if (prt.ParentEntity != null)
                        {
                            ISceneEntity sog = prt.ParentEntity;
                            LandStatReportItem lsri = new LandStatReportItem
                                                          {
                                                              Location = sog.AbsolutePosition,
                                                              Score = SceneData[obj],
                                                              TaskID = sog.UUID,
                                                              TaskLocalID = sog.LocalId,
                                                              TaskName = prt.Name,
                                                              TimeModified = sog.RootChild.Rezzed
                                                          };
                            UserAccount account =
                                m_scene.UserAccountService.GetUserAccount(m_scene.RegionInfo.AllScopeIDs, sog.OwnerID);
                            lsri.OwnerName = account != null ? account.Name : "Unknown";

                            if (filter.Length != 0)
                            {
                                //Its in the filter, don't check it
                                if (requestFlags == 2) //Owner name
                                {
                                    if (!lsri.OwnerName.Contains(filter))
                                        continue;
                                }
                                if (requestFlags == 4) //Object name
                                {
                                    if (!lsri.TaskName.Contains(filter))
                                        continue;
                                }
                            }

                            SceneReport.Add(lsri);
                        }
                    }

                }
            }
            remoteClient.SendLandStatReply(reportType, requestFlags, (uint)SceneReport.Count,SceneReport.ToArray());
        }

        private static void LookupUUIDSCompleted(IAsyncResult iar)
        {
            LookupUUIDS icon = (LookupUUIDS)iar.AsyncState;
            icon.EndInvoke(iar);
        }
        private void LookupUUID(List<UUID> uuidLst)
        {
            LookupUUIDS d = LookupUUIDsAsync;

            d.BeginInvoke(uuidLst,
                          LookupUUIDSCompleted,
                          d);
        }
        private void LookupUUIDsAsync(List<UUID> uuidLst)
        {
            UUID[] uuidarr;

            lock (uuidLst)
            {
                uuidarr = uuidLst.ToArray();
            }

            foreach (UUID t in uuidarr)
            {
                m_scene.UserAccountService.GetUserAccount(m_scene.RegionInfo.AllScopeIDs, t);
                // we drop it.  It gets cached though...  so we're ready for the next request.
            }
        }

        private enum SimWideDeletesFlags
        {
            ReturnObjectsOtherEstate = 1,
            ReturnObjects = 2,
            OthersLandNotUserOnly = 3,
            ScriptedPrimsOnly = 4
        }

        public void SimWideDeletes(IClientAPI client, int flags, UUID targetID)
        {
            if (m_scene.Permissions.CanIssueEstateCommand(client.AgentId, false))
            {
                List<ISceneEntity> prims = new List<ISceneEntity> ();
                IParcelManagementModule parcelManagement = m_scene.RequestModuleInterface<IParcelManagementModule>();
                if (parcelManagement != null)
                {
                    int containsScript = (flags & (int)SimWideDeletesFlags.ScriptedPrimsOnly);
                    foreach (ILandObject selectedParcel in parcelManagement.AllParcels())
                    {
                        if ((flags & (int)SimWideDeletesFlags.OthersLandNotUserOnly) == (int)SimWideDeletesFlags.OthersLandNotUserOnly)
                        {
                            if (selectedParcel.LandData.OwnerID != targetID)//Check to make sure it isn't their land
                                prims.AddRange (selectedParcel.GetPrimsOverByOwner (targetID, containsScript));
                        }
                            //Other estates flag doesn't seem to get sent by the viewer, so don't touch it
                        //else if ((flags & (int)SimWideDeletesFlags.ReturnObjectsOtherEstate) == (int)SimWideDeletesFlags.ReturnObjectsOtherEstate)
                        //    prims.AddRange (selectedParcel.GetPrimsOverByOwner (targetID, containsScript));
                        else// if ((flags & (int)SimWideDeletesFlags.ReturnObjects) == (int)SimWideDeletesFlags.ReturnObjects)//Return them all
                            prims.AddRange (selectedParcel.GetPrimsOverByOwner (targetID, containsScript));
                    }
                }
                ILLClientInventory inventoryModule = m_scene.RequestModuleInterface<ILLClientInventory>();
                if (inventoryModule != null)
                    inventoryModule.ReturnObjects(prims.ToArray(), UUID.Zero);
            }
            else
            {
                client.SendAlertMessage("You do not have permissions to return objects in this sim.");
            }
        }

        #endregion

        #region Outgoing Packets

        public void sendRegionInfoPacketToAll()
        {
            m_scene.ForEachScenePresence(delegate(IScenePresence sp)
            {
                if (!sp.IsChildAgent)
                    HandleRegionInfoRequest(sp.ControllingClient);
            });
        }

        public void sendRegionHandshake(IClientAPI remoteClient)
        {
            RegionHandshakeArgs args = new RegionHandshakeArgs
                                           {isEstateManager = m_scene.Permissions.IsGod(remoteClient.AgentId)};

            if (m_scene.RegionInfo.EstateSettings.EstateOwner != UUID.Zero && m_scene.RegionInfo.EstateSettings.EstateOwner == remoteClient.AgentId)
                args.isEstateManager = true;
            else if (m_scene.RegionInfo.EstateSettings.IsEstateManager(remoteClient.AgentId))
                args.isEstateManager = true;

            args.billableFactor = m_scene.RegionInfo.EstateSettings.BillableFactor;
            args.terrainStartHeight0 = (float)m_scene.RegionInfo.RegionSettings.Elevation1SW;
            args.terrainHeightRange0 = (float)m_scene.RegionInfo.RegionSettings.Elevation2SW;
            args.terrainStartHeight1 = (float)m_scene.RegionInfo.RegionSettings.Elevation1NW;
            args.terrainHeightRange1 = (float)m_scene.RegionInfo.RegionSettings.Elevation2NW;
            args.terrainStartHeight2 = (float)m_scene.RegionInfo.RegionSettings.Elevation1SE;
            args.terrainHeightRange2 = (float)m_scene.RegionInfo.RegionSettings.Elevation2SE;
            args.terrainStartHeight3 = (float)m_scene.RegionInfo.RegionSettings.Elevation1NE;
            args.terrainHeightRange3 = (float)m_scene.RegionInfo.RegionSettings.Elevation2NE;
            args.simAccess = m_scene.RegionInfo.AccessLevel;
            args.waterHeight = (float)m_scene.RegionInfo.RegionSettings.WaterHeight;
            args.regionFlags = GetRegionFlags();
            args.regionName = m_scene.RegionInfo.RegionName;
            args.SimOwner = m_scene.RegionInfo.EstateSettings.EstateOwner;

            args.terrainBase0 = UUID.Zero;
            args.terrainBase1 = UUID.Zero;
            args.terrainBase2 = UUID.Zero;
            args.terrainBase3 = UUID.Zero;

            if (!m_scene.RegionInfo.RegionSettings.UsePaintableTerrain)
            {
                args.terrainDetail0 = m_scene.RegionInfo.RegionSettings.TerrainTexture1;
                args.terrainDetail1 = m_scene.RegionInfo.RegionSettings.TerrainTexture2;
                args.terrainDetail2 = m_scene.RegionInfo.RegionSettings.TerrainTexture3;
                args.terrainDetail3 = m_scene.RegionInfo.RegionSettings.TerrainTexture4;
            }
            else
            {
                args.terrainDetail0 = m_scene.RegionInfo.RegionSettings.PaintableTerrainTexture;
                args.terrainDetail1 = m_scene.RegionInfo.RegionSettings.PaintableTerrainTexture;
                args.terrainDetail2 = m_scene.RegionInfo.RegionSettings.PaintableTerrainTexture;
                args.terrainDetail3 = m_scene.RegionInfo.RegionSettings.PaintableTerrainTexture;

                AssetBase paintAsset = m_scene.AssetService.Get(m_scene.RegionInfo.RegionSettings.PaintableTerrainTexture.ToString());
                if (paintAsset == null)
                {
                    paintAsset = new AssetBase(m_scene.RegionInfo.RegionSettings.PaintableTerrainTexture,
                                               "PaintableTerrainTexture-" + m_scene.RegionInfo.RegionID,
                                               AssetType.Texture, UUID.Zero) {Flags = AssetFlags.Deletable};
                    AssetBase defaultTexture = m_scene.AssetService.Get(RegionSettings.DEFAULT_TERRAIN_TEXTURE_2.ToString());//Nice grass
                    if (defaultTexture == null)
                        //Erm... what to do!
                        return;

                    paintAsset.Data = defaultTexture.Data;//Eventually we need to replace this with an interpolation of the existing textures!
                    paintAsset.ID = m_scene.AssetService.Store(paintAsset);
                }
            }
            args.RegionType = Utils.StringToBytes(m_scene.RegionInfo.RegionType);

            remoteClient.SendRegionHandshake(m_scene.RegionInfo,args);
        }

        public void sendRegionHandshakeToAll()
        {
            m_scene.ForEachClient(sendRegionHandshake);
        }

        public void handleEstateChangeInfo(IClientAPI remoteClient, UUID invoice, UUID senderID, UInt32 parms1, UInt32 parms2)
        {
            if (parms2 == 0)
            {
                m_scene.RegionInfo.EstateSettings.UseGlobalTime = true;
                m_scene.RegionInfo.EstateSettings.SunPosition = 0.0;
            }
            else
            {
                m_scene.RegionInfo.EstateSettings.UseGlobalTime = false;
                m_scene.RegionInfo.EstateSettings.SunPosition = (parms2 - 0x1800)/1024.0;
            }

            m_scene.RegionInfo.EstateSettings.FixedSun = (parms1 & 0x00000010) != 0;

            m_scene.RegionInfo.EstateSettings.PublicAccess = (parms1 & 0x00008000) != 0;

            m_scene.RegionInfo.EstateSettings.AllowVoice = (parms1 & 0x10000000) != 0;

            m_scene.RegionInfo.EstateSettings.AllowDirectTeleport = (parms1 & 0x00100000) != 0;

            m_scene.RegionInfo.EstateSettings.DenyAnonymous = (parms1 & 0x00800000) != 0;

            m_scene.RegionInfo.EstateSettings.DenyIdentified = (parms1 & 0x01000000) != 0;

            m_scene.RegionInfo.EstateSettings.DenyTransacted = (parms1 & 0x02000000) != 0;

            m_scene.RegionInfo.EstateSettings.DenyMinors = (parms1 & 0x40000000) != 0;

            m_scene.RegionInfo.RegionSettings.BlockShowInSearch = (parms1 & (uint)RegionFlags.BlockParcelSearch) == (uint)RegionFlags.BlockParcelSearch;

            m_scene.RegionInfo.EstateSettings.Save();
            TriggerEstateInfoChange();

            TriggerEstateSunUpdate();

            sendDetailedEstateData(remoteClient, invoice);
        }

        #endregion

        #region IRegionModule Members

        public void Initialise(IConfigSource source)
        {
        }

        #region Console Commands

        public void consoleSetTerrainTexture(string[] args)
        {
            string num = args[3];
            string uuid = args[4];
            int x = (args.Length > 5 ? int.Parse(args[5]) : -1);
            int y = (args.Length > 6 ? int.Parse(args[6]) : -1);

            if (x == -1 || (m_scene.RegionInfo.RegionLocX / Constants.RegionSize) == x)
            {
                if (y == -1 || (m_scene.RegionInfo.RegionLocY / Constants.RegionSize) == y)
                {
                    int corner = int.Parse(num);
                    UUID texture = UUID.Parse(uuid);

                    MainConsole.Instance.Debug("[ESTATEMODULE] Setting terrain textures for " + m_scene.RegionInfo.RegionName +
                                string.Format(" (C#{0} = {1})", corner, texture));

                    switch (corner)
                    {
                        case 0:
                            m_scene.RegionInfo.RegionSettings.TerrainTexture1 = texture;
                            break;
                        case 1:
                            m_scene.RegionInfo.RegionSettings.TerrainTexture2 = texture;
                            break;
                        case 2:
                            m_scene.RegionInfo.RegionSettings.TerrainTexture3 = texture;
                            break;
                        case 3:
                            m_scene.RegionInfo.RegionSettings.TerrainTexture4 = texture;
                            break;
                    }
                    m_scene.RegionInfo.RegionSettings.Save();
                    TriggerRegionInfoChange();
                    sendRegionInfoPacketToAll();

                }
            }
         }
 
        public void consoleSetTerrainHeights(string[] args)
        {
            string num = args[3];
            string min = args[4];
            string max = args[5];
            int x = (args.Length > 6 ? int.Parse(args[6]) : -1);
            int y = (args.Length > 7 ? int.Parse(args[7]) : -1);

            if (x == -1 || (m_scene.RegionInfo.RegionLocX / Constants.RegionSize) == x)
            {
                if (y == -1 || (m_scene.RegionInfo.RegionLocY / Constants.RegionSize) == y)
                {
                    int corner = int.Parse(num);
                    float lowValue = float.Parse(min, Culture.NumberFormatInfo);
                    float highValue = float.Parse(max, Culture.NumberFormatInfo);

                    MainConsole.Instance.Debug("[ESTATEMODULE] Setting terrain heights " + m_scene.RegionInfo.RegionName +
                                string.Format(" (C{0}, {1}-{2}", corner, lowValue, highValue));

                    switch (corner)
                    {
                        case 0:
                            m_scene.RegionInfo.RegionSettings.Elevation1SW = lowValue;
                            m_scene.RegionInfo.RegionSettings.Elevation2SW = highValue;
                            break;
                        case 1:
                            m_scene.RegionInfo.RegionSettings.Elevation1NW = lowValue;
                            m_scene.RegionInfo.RegionSettings.Elevation2NW = highValue;
                            break;
                        case 2:
                            m_scene.RegionInfo.RegionSettings.Elevation1SE = lowValue;
                            m_scene.RegionInfo.RegionSettings.Elevation2SE = highValue;
                            break;
                        case 3:
                            m_scene.RegionInfo.RegionSettings.Elevation1NE = lowValue;
                            m_scene.RegionInfo.RegionSettings.Elevation2NE = highValue;
                            break;
                    }
                    m_scene.RegionInfo.RegionSettings.Save();
                    TriggerRegionInfoChange();
                    sendRegionHandshakeToAll();
                }
            }
        }

        #endregion



        public void AddRegion (IScene scene)
        {
            m_scene = scene;
            m_scene.RegisterModuleInterface<IEstateModule>(this);
            m_scene.EventManager.OnNewClient += EventManager_OnNewClient;
            m_scene.EventManager.OnRequestChangeWaterHeight += changeWaterHeight;
            scene.EventManager.OnRegisterCaps += OnRegisterCaps;
            scene.EventManager.OnClosingClient += OnClosingClient;

            if (MainConsole.Instance != null)
            {
                MainConsole.Instance.Commands.AddCommand (
                                    "set terrain texture",
                                    "set terrain texture [number] [uuid] [x] [y]",
                                    "Sets the terrain [number] to [uuid], if [x] or [y] are specified, it will only " +
                                    "set it on regions with a matching coordinate. Specify -1 in [x] or [y] to wildcard" +
                                    " that coordinate.",
                                    consoleSetTerrainTexture);

                MainConsole.Instance.Commands.AddCommand (
                                    "set terrain heights",
                                    "set terrain heights [corner] [min] [max] [x] [y]",
                                    "Sets the terrain texture heights on corner #[corner] to [min]/[max], if [x] or [y] are specified, it will only " +
                                    "set it on regions with a matching coordinate. Specify -1 in [x] or [y] to wildcard" +
                                    " that coordinate. Corner # SW = 0, NW = 1, SE = 2, NE = 3.",
                                    consoleSetTerrainHeights);
            }
        }

        public void RemoveRegion (IScene scene)
        {
            m_scene.UnregisterModuleInterface<IEstateModule>(this);
            m_scene.EventManager.OnNewClient -= EventManager_OnNewClient;
            m_scene.EventManager.OnRequestChangeWaterHeight -= changeWaterHeight;
            scene.EventManager.OnRegisterCaps -= OnRegisterCaps;
            scene.EventManager.OnClosingClient -= OnClosingClient;
        }

        public void RegionLoaded (IScene scene)
        {
            // Sets up the sun module based no the saved Estate and Region Settings
            // DO NOT REMOVE or the sun will stop working
            TriggerEstateSunUpdate();
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }
        
        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "EstateManagementModule"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        #endregion

        #region Other Functions

        public void changeWaterHeight(float height)
        {
            setRegionTerrainSettings(UUID.Zero, height,
                    (float)m_scene.RegionInfo.RegionSettings.TerrainRaiseLimit,
                    (float)m_scene.RegionInfo.RegionSettings.TerrainLowerLimit,
                    m_scene.RegionInfo.RegionSettings.UseEstateSun,
                    m_scene.RegionInfo.RegionSettings.FixedSun,
                    (float)m_scene.RegionInfo.RegionSettings.SunPosition,
                    m_scene.RegionInfo.EstateSettings.UseGlobalTime,
                    m_scene.RegionInfo.EstateSettings.FixedSun,
                    (float)m_scene.RegionInfo.EstateSettings.SunPosition);

            sendRegionInfoPacketToAll();
        }

        #endregion

        private void EventManager_OnNewClient(IClientAPI client)
        {
            client.OnDetailedEstateDataRequest += sendDetailedEstateData;
            client.OnSetEstateFlagsRequest += estateSetRegionInfoHandler;
//            client.OnSetEstateTerrainBaseTexture += setEstateTerrainBaseTexture;
            client.OnSetEstateTerrainDetailTexture += setEstateTerrainBaseTexture;
            client.OnSetEstateTerrainTextureHeights += setEstateTerrainTextureHeights;
            client.OnCommitEstateTerrainTextureRequest += handleCommitEstateTerrainTextureRequest;
            client.OnSetRegionTerrainSettings += setRegionTerrainSettings;
            client.OnEstateRestartSimRequest += handleEstateRestartSimRequest;
            client.OnEstateChangeCovenantRequest += handleChangeEstateCovenantRequest;
            client.OnEstateChangeInfo += handleEstateChangeInfo;
            client.OnUpdateEstateAccessDeltaRequest += handleEstateAccessDeltaRequest;
            client.OnSimulatorBlueBoxMessageRequest += SendSimulatorBlueBoxMessage;
            client.OnEstateBlueBoxMessageRequest += SendEstateBlueBoxMessage;
            client.OnEstateDebugRegionRequest += handleEstateDebugRegionRequest;
            client.OnEstateTeleportOneUserHomeRequest += handleEstateTeleportOneUserHomeRequest;
            client.OnEstateTeleportAllUsersHomeRequest += handleEstateTeleportAllUsersHomeRequest;
            client.OnRequestTerrain += handleTerrainRequest;
            client.OnUploadTerrain += handleUploadTerrain;
            client.OnSimWideDeletes += SimWideDeletes;

            client.OnRegionInfoRequest += HandleRegionInfoRequest;
            client.OnEstateCovenantRequest += HandleEstateCovenantRequest;
            client.OnLandStatRequest += HandleLandStatRequest;
            sendRegionHandshake(client);
        }

        private void OnClosingClient(IClientAPI client)
        {
            client.OnDetailedEstateDataRequest -= sendDetailedEstateData;
            client.OnSetEstateFlagsRequest -= estateSetRegionInfoHandler;
            //            client.OnSetEstateTerrainBaseTexture -= setEstateTerrainBaseTexture;
            client.OnSetEstateTerrainDetailTexture -= setEstateTerrainBaseTexture;
            client.OnSetEstateTerrainTextureHeights -= setEstateTerrainTextureHeights;
            client.OnCommitEstateTerrainTextureRequest -= handleCommitEstateTerrainTextureRequest;
            client.OnSetRegionTerrainSettings -= setRegionTerrainSettings;
            client.OnEstateRestartSimRequest -= handleEstateRestartSimRequest;
            client.OnEstateChangeCovenantRequest -= handleChangeEstateCovenantRequest;
            client.OnEstateChangeInfo -= handleEstateChangeInfo;
            client.OnUpdateEstateAccessDeltaRequest -= handleEstateAccessDeltaRequest;
            client.OnSimulatorBlueBoxMessageRequest -= SendSimulatorBlueBoxMessage;
            client.OnEstateBlueBoxMessageRequest -= SendEstateBlueBoxMessage;
            client.OnEstateDebugRegionRequest -= handleEstateDebugRegionRequest;
            client.OnEstateTeleportOneUserHomeRequest -= handleEstateTeleportOneUserHomeRequest;
            client.OnEstateTeleportAllUsersHomeRequest -= handleEstateTeleportAllUsersHomeRequest;
            client.OnRequestTerrain -= handleTerrainRequest;
            client.OnUploadTerrain -= handleUploadTerrain;
            client.OnSimWideDeletes -= SimWideDeletes;

            client.OnRegionInfoRequest -= HandleRegionInfoRequest;
            client.OnEstateCovenantRequest -= HandleEstateCovenantRequest;
            client.OnLandStatRequest -= HandleLandStatRequest;
        }

        public uint GetRegionFlags()
        {
            RegionFlags flags = RegionFlags.None;

            // Fully implemented
            //
            if (m_scene.RegionInfo.RegionSettings.AllowDamage)
                flags |= RegionFlags.AllowDamage;
            if (m_scene.RegionInfo.RegionSettings.BlockTerraform)
                flags |= RegionFlags.BlockTerraform;
            if (!m_scene.RegionInfo.RegionSettings.AllowLandResell)
                flags |= RegionFlags.BlockLandResell;
            if (m_scene.RegionInfo.RegionSettings.DisableCollisions)
                flags |= RegionFlags.SkipCollisions;
            if (m_scene.RegionInfo.RegionSettings.DisableScripts)
                flags |= RegionFlags.SkipScripts;
            if (m_scene.RegionInfo.RegionSettings.DisablePhysics)
                flags |= RegionFlags.SkipPhysics;
            if (m_scene.RegionInfo.RegionSettings.BlockFly)
                flags |= RegionFlags.NoFly;
            if (m_scene.RegionInfo.RegionSettings.RestrictPushing)
                flags |= RegionFlags.RestrictPushObject;
            if (m_scene.RegionInfo.RegionSettings.AllowLandJoinDivide)
                flags |= RegionFlags.AllowParcelChanges;
            if (m_scene.RegionInfo.RegionSettings.BlockShowInSearch)
                flags |= RegionFlags.BlockParcelSearch;

            if (m_scene.RegionInfo.RegionSettings.FixedSun)
                flags |= RegionFlags.SunFixed;
            if (m_scene.RegionInfo.RegionSettings.Sandbox)
                flags |= RegionFlags.Sandbox;

            if (m_scene.RegionInfo.EstateSettings.AllowLandmark)
                flags |= RegionFlags.AllowLandmark;
            if (m_scene.RegionInfo.EstateSettings.AllowSetHome)
                flags |= RegionFlags.AllowSetHome;
            if (m_scene.RegionInfo.EstateSettings.BlockDwell)
                flags |= RegionFlags.BlockDwell;
            if (m_scene.RegionInfo.EstateSettings.ResetHomeOnTeleport)
                flags |= RegionFlags.ResetHomeOnTeleport;

            

            // Omitted
            //
            // Omitted: SkipUpdateInterestList  Region does not update agent prim interest lists. Internal debugging option.
            // Omitted: NullLayer Unknown: Related to the availability of an overview world map tile.(Think mainland images when zoomed out.)
            // Omitted: SkipAgentAction Unknown: Related to region debug flags. Possibly to skip processing of agent interaction with world.

            return (uint)flags;
        }

        public uint GetEstateFlags()
        {
            RegionFlags flags = RegionFlags.None;

            if (m_scene.RegionInfo.EstateSettings.FixedSun)
                flags |= RegionFlags.SunFixed;
            if (m_scene.RegionInfo.EstateSettings.PublicAccess)
                flags |= (RegionFlags.PublicAllowed |
                          RegionFlags.ExternallyVisible);
            if (m_scene.RegionInfo.EstateSettings.AllowVoice)
                flags |= RegionFlags.AllowVoice;
            if (m_scene.RegionInfo.EstateSettings.AllowDirectTeleport)
                flags |= RegionFlags.AllowDirectTeleport;
            if (m_scene.RegionInfo.EstateSettings.DenyAnonymous)
                flags |= RegionFlags.DenyAnonymous;
            if (m_scene.RegionInfo.EstateSettings.DenyIdentified)
                flags |= RegionFlags.DenyIdentified;
            if (m_scene.RegionInfo.EstateSettings.DenyTransacted)
                flags |= RegionFlags.DenyTransacted;
            if (m_scene.RegionInfo.EstateSettings.AbuseEmailToEstateOwner)
                flags |= RegionFlags.AbuseEmailToEstateOwner;
            if (m_scene.RegionInfo.EstateSettings.BlockDwell)
                flags |= RegionFlags.BlockDwell;
            if (m_scene.RegionInfo.EstateSettings.EstateSkipScripts)
                flags |= RegionFlags.EstateSkipScripts;
            if (m_scene.RegionInfo.EstateSettings.ResetHomeOnTeleport)
                flags |= RegionFlags.ResetHomeOnTeleport;
            if (m_scene.RegionInfo.EstateSettings.TaxFree)
                flags |= RegionFlags.TaxFree;
            if (m_scene.RegionInfo.EstateSettings.AllowLandmark)
                flags |= RegionFlags.AllowLandmark;
            if (m_scene.RegionInfo.EstateSettings.AllowParcelChanges)
                flags |= RegionFlags.AllowParcelChanges;
            if (m_scene.RegionInfo.EstateSettings.AllowSetHome)
                flags |= RegionFlags.AllowSetHome;
            if (m_scene.RegionInfo.EstateSettings.DenyMinors)
                flags |= (RegionFlags)(1 << 30);

            return (uint)flags;
        }

        public bool IsManager(UUID avatarID)
        {
            if (avatarID == m_scene.RegionInfo.EstateSettings.EstateOwner)
                return true;

            List<UUID> ems = new List<UUID>(m_scene.RegionInfo.EstateSettings.EstateManagers);
            if (ems.Contains(avatarID))
                return true;

            return false;
        }

        protected void TriggerRegionInfoChange()
        {
            ChangeDelegate change = OnRegionInfoChange;

            if (change != null)
                change(m_scene.RegionInfo.RegionID);
        }

        protected void TriggerEstateInfoChange()
        {
            ChangeDelegate change = OnEstateInfoChange;

            if (change != null)
                change(m_scene.RegionInfo.RegionID);
        }

        protected void TriggerEstateMessage(UUID fromID, string fromName, string message)
        {
            MessageDelegate onmessage = OnEstateMessage;
            IDialogModule module = m_scene.RequestModuleInterface<IDialogModule>();
            if (onmessage != null)
                onmessage(m_scene.RegionInfo.RegionID, fromID, fromName, message);
            else if (module != null)
                module.SendGeneralAlert(message);
        }

        public void TriggerEstateSunUpdate()
        {
            if (m_scene.RegionInfo.EstateSettings == null)
                return;

            float sun;
            if (m_scene.RegionInfo.RegionSettings.UseEstateSun)
            {
                sun = (float)m_scene.RegionInfo.EstateSettings.SunPosition;
                if (m_scene.RegionInfo.EstateSettings.UseGlobalTime)
                {
                    ISunModule sunModule = m_scene.RequestModuleInterface<ISunModule>();
                    if(sunModule != null)
                        sun = sunModule.GetCurrentSunHour();
                }

                // 
                m_scene.EventManager.TriggerEstateToolsSunUpdate(
                        m_scene.RegionInfo.RegionHandle,
                        m_scene.RegionInfo.EstateSettings.FixedSun,
                        m_scene.RegionInfo.RegionSettings.UseEstateSun,
                        sun);
            }
            else
            {
                // Use the Sun Position from the Region Settings
                sun = (float)m_scene.RegionInfo.RegionSettings.SunPosition/* - 6.0f*/;

                m_scene.EventManager.TriggerEstateToolsSunUpdate(
                        m_scene.RegionInfo.RegionHandle,
                        m_scene.RegionInfo.RegionSettings.FixedSun,
                        m_scene.RegionInfo.RegionSettings.UseEstateSun,
                        sun);
            }
        }
    }
}
