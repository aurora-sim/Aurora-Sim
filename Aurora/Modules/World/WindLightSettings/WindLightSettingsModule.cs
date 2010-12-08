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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using log4net;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Region.CoreModules.Framework.InterfaceCommander;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Caps = OpenSim.Framework.Capabilities.Caps;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules
{
    public class WindLightSettingsModule : INonSharedRegionModule, IWindLightSettingsModule
    {
        #region Declarations

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Scene m_scene;
        private bool m_enableWindlight = true;
        private Dictionary<float, RegionLightShareData> m_WindlightSettings = new Dictionary<float, RegionLightShareData>();
        private Dictionary<UUID, UUID> m_preivouslySentWindLight = new Dictionary<UUID, UUID>();
        //ONLY create this once so that the UUID stays constant so that it isn't repeatedly sent to the client
        private RegionLightShareData m_defaultWindLight = new RegionLightShareData();

        public bool EnableWindLight
        {
            get { return m_enableWindlight; }
        }

        #endregion

        #region INonSharedRegionModule Members

        public void Initialise(IConfigSource config)
        {
            IConfig LightShareConfig = config.Configs["WindLightSettings"];
            if(LightShareConfig != null)
                m_enableWindlight = LightShareConfig.GetBoolean("Enable", true);
        }

        public void AddRegion(Scene scene)
        {
            if (!m_enableWindlight)
                return;

            m_scene = scene;
            m_scene.RegisterModuleInterface<IWindLightSettingsModule>(this);
            IRegionInfoConnector RegionInfoConnector = Aurora.DataManager.DataManager.RequestPlugin<IRegionInfoConnector>();
            if (RegionInfoConnector != null)
                m_WindlightSettings = RegionInfoConnector.LoadRegionWindlightSettings(m_scene.RegionInfo.RegionID);

            scene.EventManager.OnClientClosed += new EventManager.ClientClosed(EventManager_OnClientClosed);
            scene.EventManager.OnRegisterCaps += OnRegisterCaps;
            scene.EventManager.OnMakeRootAgent += SendProfileToClient;
            scene.EventManager.OnSignificantClientMovement += OnSignificantClientMovement;
            scene.EventManager.OnAvatarEnteringNewParcel += AvatarEnteringNewParcel;
        }

        void EventManager_OnClientClosed(UUID clientID, Scene scene)
        {
            //They are leaving, clear it out
            m_preivouslySentWindLight.Remove(clientID);
        }

        public void RemoveRegion(Scene scene)
        {
            m_scene.UnregisterModuleInterface<IWindLightSettingsModule>(this);
            
            scene.EventManager.OnClientClosed -= new EventManager.ClientClosed(EventManager_OnClientClosed);
            scene.EventManager.OnRegisterCaps -= OnRegisterCaps;
            scene.EventManager.OnMakeRootAgent -= SendProfileToClient;
            scene.EventManager.OnSignificantClientMovement -= OnSignificantClientMovement;
            scene.EventManager.OnAvatarEnteringNewParcel -= AvatarEnteringNewParcel;
        }

        public void RegionLoaded(Scene scene){}

        public Type ReplaceableInterface{get { return null; }}

        public void PostInitialise(){}

        public void Close(){}

        public string Name
        {
            get { return "LightShare2Module"; }
        }

        #endregion

        #region IWindLightModule Members

        public void SendWindlightProfileTargeted(RegionLightShareData wl, UUID pUUID)
        {
            ScenePresence Sc;
            if (m_scene.TryGetScenePresence(pUUID, out Sc))
            {
                SendProfileToClient(Sc, wl);
            }
        }

        public void SaveWindLightSettings(float MinEffectiveHeight, RegionLightShareData wl)
        {
            UUID oldUUID = UUID.Random();
            if(m_WindlightSettings.ContainsKey(wl.minEffectiveAltitude))
                oldUUID = m_WindlightSettings[wl.minEffectiveAltitude].UUID;

            m_WindlightSettings[wl.minEffectiveAltitude] = wl;
            IRegionInfoConnector RegionInfoConnector = Aurora.DataManager.DataManager.RequestPlugin<IRegionInfoConnector>();
            if (RegionInfoConnector != null)
            {
                RegionInfoConnector.StoreRegionWindlightSettings(wl.regionID, oldUUID, wl);
            }
            m_scene.ForEachScenePresence(SendProfileToClient);
        }

        #endregion

        public void OnRegisterCaps(UUID agentID, Caps caps)
        {
            UUID capuuid = UUID.Random();

            //Sets the windlight settings
            caps.RegisterHandler("DispatchWindLightSettings",
                                new RestHTTPHandler("POST", "/CAPS/" + capuuid + "/",
                                                      delegate(Hashtable m_dhttpMethod)
                                                      {
                                                          return DispatchWindLightSettings(m_dhttpMethod, capuuid, agentID);
                                                      }));

            capuuid = UUID.Random();
            //Retrieves the windlight settings for a specifc parcel or region
            caps.RegisterHandler("RetrieveWindLightSettings",
                                new RestHTTPHandler("POST", "/CAPS/" + capuuid + "/",
                                                      delegate(Hashtable m_dhttpMethod)
                                                      {
                                                          return RetrieveWindLightSettings(m_dhttpMethod, capuuid, agentID);
                                                      }));
        }

        private Hashtable RetrieveWindLightSettings(Hashtable m_dhttpMethod, UUID capuuid, UUID agentID)
        {
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = "";

            ScenePresence SP = m_scene.GetScenePresence(agentID);
            if (SP == null)
                return responsedata; //They don't exist

            OSDMap rm = (OSDMap)OSDParser.DeserializeLLSDXml((string)m_dhttpMethod["requestbody"]);
            OSDMap retVal = new OSDMap();
            if (rm.ContainsKey("RegionID"))
            {
                //For the region, just add all of them
                OSDArray array = new OSDArray();
                foreach (RegionLightShareData rlsd in m_WindlightSettings.Values)
                {
                    OSDMap m = rlsd.ToOSD();
                    m.Add("Name", OSD.FromString("(Region Settings), Min: " + rlsd.minEffectiveAltitude + ", Max: " + rlsd.maxEffectiveAltitude));
                    array.Add(m);
                }
                retVal.Add("WindLight", array);
                retVal.Add("Type", OSD.FromInteger(1));
            }
            else if (rm.ContainsKey("ParcelID"))
            {
                OSDArray retVals = new OSDArray();
                //-1 is all parcels
                if (rm["ParcelID"].AsInteger() == -1)
                {
                    //All parcels
                    foreach(ILandObject land in SP.Scene.LandChannel.AllParcels())
                    {
                        OSDMap map = land.LandData.GenericDataMap;
                        if (map.ContainsKey("WindLight"))
                        {
                            OSDMap parcelWindLight = (OSDMap)map["WindLight"];
                            foreach (OSD innerMap in parcelWindLight.Values)
                            {
                                RegionLightShareData rlsd = new RegionLightShareData();
                                rlsd.FromOSD((OSDMap)innerMap);
                                OSDMap imap = new OSDMap();
                                imap = rlsd.ToOSD();
                                imap.Add("Name", OSD.FromString(land.LandData.Name + ", Min: " + rlsd.minEffectiveAltitude + ", Max: " + rlsd.maxEffectiveAltitude));
                                retVals.Add(imap);
                            }
                        }
                    }
                }
                else
                {
                    //Only the given parcel parcel given by localID
                    ILandObject land = SP.Scene.LandChannel.GetLandObject(rm["ParcelID"].AsInteger());
                    OSDMap map = land.LandData.GenericDataMap;
                    if (map.ContainsKey("WindLight"))
                    {
                        OSDMap parcelWindLight = (OSDMap)map["WindLight"];
                        foreach (OSD innerMap in parcelWindLight.Values)
                        {
                            RegionLightShareData rlsd = new RegionLightShareData();
                            rlsd.FromOSD((OSDMap)innerMap);
                            OSDMap imap = new OSDMap();
                            imap = rlsd.ToOSD();
                            imap.Add("Name", OSD.FromString(land.LandData.Name + ", Min: " + rlsd.minEffectiveAltitude + ", Max: " + rlsd.maxEffectiveAltitude));
                            retVals.Add(imap);
                        }
                    }
                }
                retVal.Add("WindLight", retVals);
                retVal.Add("Type", OSD.FromInteger(2));
            }

            responsedata["str_response_string"] = OSDParser.SerializeLLSDXmlString(retVal);
            return responsedata;
        }

        private Hashtable DispatchWindLightSettings(Hashtable m_dhttpMethod, UUID capuuid, UUID agentID)
        {
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = "";

            ScenePresence SP = m_scene.GetScenePresence(agentID);
            if(SP == null)
                return responsedata; //They don't exist

            m_log.Info("[WindLightSettings]: Got a request to update WindLight from " + SP.Name);

            OSDMap rm = (OSDMap)OSDParser.DeserializeLLSDXml((string)m_dhttpMethod["requestbody"]);

            RegionLightShareData lsd = new RegionLightShareData();
            lsd.FromOSD(rm);
            lsd.regionID = SP.Scene.RegionInfo.RegionID;
            bool remove = false;
            if (rm.ContainsKey("remove"))
                remove = rm["remove"].AsBoolean();

            if (remove)
            {
                if (lsd.type == 0) //Region
                {
                    if (!SP.Scene.Permissions.CanIssueEstateCommand(SP.UUID, false))
                        return responsedata; // No permissions
                    bool found = false;
                    foreach (RegionLightShareData regionLSD in m_WindlightSettings.Values)
                    {
                        if (lsd.minEffectiveAltitude == regionLSD.minEffectiveAltitude &&
                            lsd.maxEffectiveAltitude == regionLSD.maxEffectiveAltitude)
                        {
                            //it exists
                            found = true;
                            break;
                        }
                    }

                    //Set to default
                    if(found)
                        SaveWindLightSettings(lsd.minEffectiveAltitude, new RegionLightShareData());
                }
                else if (lsd.type == 1) //Parcel
                {
                    ILandObject land = SP.Scene.LandChannel.GetLandObject((int)SP.AbsolutePosition.X, (int)SP.AbsolutePosition.Y);
                    if (!SP.Scene.Permissions.GenericParcelPermission(SP.UUID, land, (ulong)GroupPowers.LandOptions))
                        return responsedata; // No permissions
                    IOpenRegionSettingsModule ORSM = SP.Scene.RequestModuleInterface<IOpenRegionSettingsModule>();
                    if (ORSM == null || !ORSM.AllowParcelWindLight)
                    {
                        SP.ControllingClient.SendAlertMessage("Parcel WindLight is disabled in this region.");
                        return responsedata;
                    }

                    OSDMap map = land.LandData.GenericDataMap;

                    OSDMap innerMap = new OSDMap();
                    if (land.LandData.GenericDataMap.ContainsKey("WindLight"))
                        innerMap = (OSDMap)map["WindLight"];

                    if (innerMap.ContainsKey(lsd.minEffectiveAltitude.ToString()))
                    {
                        innerMap.Remove(lsd.minEffectiveAltitude.ToString());
                    }

                    land.LandData.AddGenericData("WindLight", innerMap);
                    //Update the client
                    SendProfileToClient(SP);
                }
            }
            else
            {
                if (lsd.type == 0) //Region
                {
                    if (!SP.Scene.Permissions.CanIssueEstateCommand(SP.UUID, false))
                        return responsedata; // No permissions

                    foreach (RegionLightShareData regionLSD in m_WindlightSettings.Values)
                    {
                        string message = "";
                        if (checkAltitude(lsd, regionLSD, out message))
                        {
                            SP.ControllingClient.SendAlertMessage(message);
                            return responsedata;
                        }
                    }
                    SaveWindLightSettings(lsd.minEffectiveAltitude, lsd);
                }
                else if (lsd.type == 1) //Parcel
                {
                    ILandObject land = SP.Scene.LandChannel.GetLandObject((int)SP.AbsolutePosition.X, (int)SP.AbsolutePosition.Y);
                    if (!SP.Scene.Permissions.GenericParcelPermission(SP.UUID, land, (ulong)GroupPowers.LandOptions))
                        return responsedata; // No permissions
                    IOpenRegionSettingsModule ORSM = SP.Scene.RequestModuleInterface<IOpenRegionSettingsModule>();
                    if (ORSM == null || !ORSM.AllowParcelWindLight)
                    {
                        SP.ControllingClient.SendAlertMessage("Parcel WindLight is disabled in this region.");
                        return responsedata;
                    }

                    OSDMap map = land.LandData.GenericDataMap;

                    OSDMap innerMap = new OSDMap();
                    if (land.LandData.GenericDataMap.ContainsKey("WindLight"))
                        innerMap = (OSDMap)map["WindLight"];

                    string removeThisMap = "";

                    foreach (KeyValuePair<string, OSD> kvp in innerMap)
                    {
                        OSDMap lsdMap = (OSDMap)kvp.Value;
                        RegionLightShareData parcelLSD = new RegionLightShareData();
                        parcelLSD.FromOSD(lsdMap);

                        string message = "";
                        if (checkAltitude(lsd, parcelLSD, out message))
                        {
                            SP.ControllingClient.SendAlertMessage(message);
                            return responsedata;
                        }
                    }

                    if (removeThisMap != "")
                        innerMap.Remove(removeThisMap);

                    innerMap[lsd.minEffectiveAltitude.ToString()] = lsd.ToOSD();

                    land.LandData.AddGenericData("WindLight", innerMap);
                    //Update the client
                    SendProfileToClient(SP, lsd);
                }
            }
            SP.ControllingClient.SendAlertMessage("WindLight Settings updated.");
            return responsedata;
        }

        public bool checkAltitude(RegionLightShareData lsd, RegionLightShareData regionLSD, out string message)
        {
            message = "";
            if (lsd.minEffectiveAltitude == regionLSD.minEffectiveAltitude &&
                            lsd.maxEffectiveAltitude == regionLSD.maxEffectiveAltitude)
            {
                //Updating, break
                return false;
            }
            //Ex lsd.min = 0, regionLSD.min = 100
            if (lsd.minEffectiveAltitude < regionLSD.minEffectiveAltitude)
            {
                //new one is below somewhere
                //Ex lsd.max = 100, regionLSD.min = 101
                //Ex lsd.max = 100, regionLSD.min = 200
                if (lsd.maxEffectiveAltitude <= regionLSD.minEffectiveAltitude)
                {
                    return false;
                }
                else
                {
                    //Ex lsd.max = 100, regionLSD.min = 99 ERROR
                    message = "Altitudes collide. Set maximum height lower than " + regionLSD.minEffectiveAltitude + ".";
                    return true;
                }
            }// Ex. lsd.min = 200, regionLSD.min = 100
            else if (lsd.minEffectiveAltitude > regionLSD.minEffectiveAltitude)
            {
                //Check against max
                // Ex. lsd.min = 200, regionLSD.max = 200
                if (lsd.minEffectiveAltitude >= regionLSD.maxEffectiveAltitude)
                {
                    return false;
                }
                else
                {
                    //Ex lsd.min = 200, regionLSD.max = 201 ERROR
                    message = "Altitudes collide. Set min height higher than " + regionLSD.maxEffectiveAltitude + ".";
                    return true;
                }
            }
            else
            {
                //Equal min val, fail
                message = "Altitudes collide. There is another setting that already uses this minimum.";
                return true;
            }
        }

        private void AvatarEnteringNewParcel(ScenePresence SP, int localLandID, UUID regionID)
        {
            //Send on new parcel
            SendProfileToClient(SP);
        }

        void OnSignificantClientMovement(IClientAPI remote_client)
        {
            //Send on movement as this checks for altitude
            SendProfileToClient(m_scene.GetScenePresence(remote_client.AgentId));
        }

        //Find the correct WL settings to send to the client
        public void SendProfileToClient(ScenePresence presence)
        {
            if (presence == null)
                return;
            ILandObject land = presence.Scene.LandChannel.GetLandObject(presence.currentLocalParcelID);
            OSDMap map = land != null ? land.LandData.GenericDataMap : new OSDMap();
            if (map.ContainsKey("WindLight"))
            {
                IOpenRegionSettingsModule ORSM = presence.Scene.RequestModuleInterface<IOpenRegionSettingsModule>();
                if (ORSM != null && ORSM.AllowParcelWindLight)
                {
                    if (CheckOverRideParcels(presence))
                    {
                        //Overrides all
                        SendProfileToClient(presence, FindRegionWindLight(presence));
                    }
                    else
                    {
                        OSDMap innerMap = (OSDMap)map["WindLight"];
                        foreach (KeyValuePair<string, OSD> kvp in innerMap)
                        {
                            int minEffectiveAltitude = int.Parse(kvp.Key);
                            if (presence.AbsolutePosition.Z > minEffectiveAltitude)
                            {
                                OSDMap lsdMap = (OSDMap)kvp.Value;
                                RegionLightShareData parcelLSD = new RegionLightShareData();
                                parcelLSD.FromOSD(lsdMap);
                                if (presence.AbsolutePosition.Z < parcelLSD.maxEffectiveAltitude)
                                {
                                    //They are between both altitudes
                                    SendProfileToClient(presence, parcelLSD);
                                    return; //End it
                                }
                            }
                        }
                        //Send region since no parcel claimed the user
                        SendProfileToClient(presence, FindRegionWindLight(presence));
                    }
                }
                else
                {
                    //Only region allowed 
                    SendProfileToClient(presence, FindRegionWindLight(presence));
                }
            }
            else
            {
                //Send the region by default to override any previous settings
                SendProfileToClient(presence, FindRegionWindLight(presence));
            }
        }

        public bool CheckOverRideParcels(ScenePresence presence)
        {
            foreach (RegionLightShareData parcelLSD in m_WindlightSettings.Values)
            {
                if (presence.AbsolutePosition.Z > parcelLSD.minEffectiveAltitude)
                {
                    if (presence.AbsolutePosition.Z < parcelLSD.maxEffectiveAltitude)
                    {
                        return parcelLSD.overrideParcels;
                    }
                }
            }
            //Noone claims this area, so default is no
            return false;
        }

        public RegionLightShareData FindRegionWindLight(ScenePresence presence)
        {
            foreach (RegionLightShareData parcelLSD in m_WindlightSettings.Values)
            {
                if (presence.AbsolutePosition.Z > parcelLSD.minEffectiveAltitude)
                {
                    if (presence.AbsolutePosition.Z < parcelLSD.maxEffectiveAltitude)
                    {
                        return parcelLSD;
                    }
                }
            }
            //Return the default then
            return m_defaultWindLight;
        }

        public void SendProfileToClient(ScenePresence presence, RegionLightShareData wl)
        {
            if (m_enableWindlight)
            {
                if (!presence.IsChildAgent)
                {
                    //Check the cache so that we don't kill the client with updates as this can lag the client 
                    if (m_preivouslySentWindLight.ContainsKey(presence.UUID))
                    {
                        if (m_preivouslySentWindLight[presence.UUID] == wl.UUID)
                            return;
                    }
                    //Update the cache
                    m_preivouslySentWindLight[presence.UUID] = wl.UUID;
                    SendProfileToClientEQ(presence, wl);
                }
            }
        }

        public void SendProfileToClientEQ(ScenePresence presence, RegionLightShareData wl)
        {
            OSD item = BuildSendEQMessage(wl.ToOSD());
            IEventQueueService eq = presence.Scene.RequestModuleInterface<IEventQueueService>();
            if (eq != null)
                eq.Enqueue(item, presence.UUID, presence.Scene.RegionInfo.RegionHandle);
        }

        private OSD BuildSendEQMessage(OSDMap body)
        {
            OSDMap map = new OSDMap();
            map.Add("body", body);
            map.Add("message", OSD.FromString("WindLightSettingsUpdate"));
            return map;
        }
    }
}

