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
using System.Net;
using System.Reflection;
using System.Threading;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Messages.Linden;
using OpenMetaverse.Packets;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using BlockingLLSDQueue = OpenSim.Framework.BlockingQueue<OpenMetaverse.StructuredData.OSD>;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework.Capabilities;

namespace OpenSim.Services.CapsService
{
    public class EventQueueServicesConnector : EventQueueMasterService, IEventQueueService, IService
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private List<string> m_ServerURIs = new List<string>();

        /// <summary>
        /// This holds events that havn't been sent yet as the client hasn't called the CapsHandler and sent the EventQueue password.
        /// Note: these already have been converted to LLSDXML, so do not duplicate this!
        /// </summary>
        private Dictionary<UUID, EventQueueClient> m_eventsNotSentPasswordDoesNotExist = new Dictionary<UUID, EventQueueClient>();
        
        #endregion

        #region IService Members

        public override string Name
        {
            get { return GetType().Name; }
        }

        public override void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public override void PostInitialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("EventQueueHandler", "") != Name)
                return;

            m_ServerURIs = registry.RequestModuleInterface<IConfigurationService>().FindValueOf("EventQueueServiceURI");
            registry.RegisterModuleInterface<IEventQueueService>(this);
        }

        public override void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public override void PostStart(IConfigSource config, IRegistryCore registry)
        {
            m_service = registry.RequestModuleInterface<ICapsService>();
        }

        public override void AddNewRegistry(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("EventQueueHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IEventQueueService>(this);
        }

        #endregion

        #region Find EQM Password

        /// <summary>
        /// Find the password of the user that we may have recieved in the CAPS seed request.
        /// </summary>
        /// <param name="agentID"></param>
        private bool FindAndPopulateEQMPassword(UUID agentID, ulong RegionHandle, out UUID Password)
        {
            if (m_service != null)
            {
                IClientCapsService clientCaps = m_service.GetClientCapsService(agentID);
                if (clientCaps != null)
                {
                    IRegionClientCapsService regionClientCaps = clientCaps.GetCapsService(RegionHandle);
                    if (regionClientCaps != null)
                    {
                        Password = regionClientCaps.Password;
                        return true;
                    }
                }
            }
            Password = UUID.Zero;
            return false;
        }

        #endregion

        #region IEventQueue Members

        /// <summary>
        /// Add an EQ message into the queue on the remote EventQueueService 
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="avatarID"></param>
        /// <param name="regionHandle"></param>
        /// <returns></returns>
        public override bool Enqueue(OSD ev, UUID avatarID, ulong regionHandle)
        {
            try
            {
                //Do this async so that we don't kill the sim while waiting for this to be sent
                //TODO: Maybe have a thread that runs through this and sends them off instead of doing fire and forget?
                Util.FireAndForget(delegate(object o)
                {
                    TryEnqueue(ev, avatarID, regionHandle);
                });
            } 
            catch(Exception e)
            {
                m_log.Error("[EVENTQUEUE] Caught exception: " + e);
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Remove all events that have not been sent yet
        /// </summary>
        /// <param name="avatarID"></param>
        /// <param name="RegionHandle"></param>
        public void ClearEventQueue(UUID avatarID, ulong RegionHandle)
        {
            lock (m_eventsNotSentPasswordDoesNotExist)
            {
                if (!m_eventsNotSentPasswordDoesNotExist.ContainsKey(avatarID))
                    m_eventsNotSentPasswordDoesNotExist.Add(avatarID, new EventQueueClient());
                m_eventsNotSentPasswordDoesNotExist[avatarID].ClearEvents(RegionHandle);
            }
        }

        /// <summary>
        /// Add an EQ message into the queue on the remote EventQueueService 
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="avatarID"></param>
        /// <param name="regionHandle"></param>
        /// <returns></returns>
        public override bool TryEnqueue(OSD ev, UUID avatarID, ulong regionHandle)
        {
            //m_log.DebugFormat("[EVENTQUEUE]: Enqueuing event for {0} in region {1}", avatarID, m_scene.RegionInfo.RegionName);
            try
            {
                if (ev == null)
                    return false;

                lock (m_eventsNotSentPasswordDoesNotExist)
                {
                    //Make sure these exist
                    if (!m_eventsNotSentPasswordDoesNotExist.ContainsKey(avatarID))
                        m_eventsNotSentPasswordDoesNotExist.Add(avatarID, new EventQueueClient());
                }

                UUID Password;
                if (!FindAndPopulateEQMPassword(avatarID, regionHandle, out Password))
                {
                    lock (m_eventsNotSentPasswordDoesNotExist)
                    {
                        m_eventsNotSentPasswordDoesNotExist[avatarID].AddNewEvent(regionHandle, OSDParser.SerializeLLSDXmlString(ev));
                    }
                    m_log.Info("[EventQueueServiceConnector]: Could not find password for agent " + avatarID +
                                ", all Caps will fail if this is not resolved!");
                    return false;
                }

                OSDMap request = new OSDMap();
                request.Add("AgentID", avatarID);
                request.Add("RegionHandle", regionHandle);
                request.Add("Password", Password);
                //Note: we HAVE to convert it to xml, otherwise things like byte[] arrays will not be passed through correctly!

                OSDArray events = new OSDArray();
                events.Add(OSDParser.SerializeLLSDXmlString(ev)); //Add this event

                //Clear the queue above if the password was just found now
                //Fire all of them sync for now... if this becomes a large problem, we can deal with it later
                lock (m_eventsNotSentPasswordDoesNotExist)
                {
                    foreach (OSD EQMessage in m_eventsNotSentPasswordDoesNotExist[avatarID].GetEvents(regionHandle))
                    {
                        try
                        {
                            if (EQMessage == null)
                                continue;
                            //We do NOT enqueue disable simulator as it will kill things badly, and they get in here
                            //  because we can't
                            OSDMap map = (OSDMap)OSDParser.DeserializeLLSDXml(EQMessage.AsString());
                            if (!map.ContainsKey("message") || (map.ContainsKey("message") && map["message"] != "DisableSimulator"))
                                events.Add(EQMessage);
                            else
                            {
                                m_log.Warn("[EventQueueServicesConnector]: Found DisableSimulator in the not sent queue, not sending");
                            }
                        }
                        catch (Exception e)
                        {
                            m_log.Error("[EVENTQUEUE] Caught event not found exception: " + e.ToString());
                            return false;
                        }
                    }
                    //Clear it for now... we'll readd if it fails
                    m_eventsNotSentPasswordDoesNotExist[avatarID].ClearEvents(regionHandle);
                }

                request.Add("Events", events);

                foreach (string m_ServerURI in m_ServerURIs)
                {
                    OSDMap reply = WebUtils.PostToService(m_ServerURI + "/CAPS/EQMPOSTER", request);
                    if (reply != null)
                    {
                        OSDMap result = null;
                        try
                        {
                            if (reply["_RawResult"] != "")
                                result = (OSDMap)OSDParser.DeserializeJson(reply["_RawResult"]);
                        }
                        catch
                        {
                        }

                        bool success = result == null ? false : result["success"].AsBoolean();
                        if (!success)
                        {
                            //We need to save the EQMs so that we can try again later
                            foreach (OSD o in events)
                            {
                                if (o != null)
                                    lock(m_eventsNotSentPasswordDoesNotExist)
                                        m_eventsNotSentPasswordDoesNotExist[avatarID].AddNewEvent(regionHandle, o);
                            }
                        }
                        else
                            return success;
                    }
                    return false;
                }
            }
            catch (Exception e)
            {
                m_log.Error("[EVENTQUEUE] Caught exception: " + e.ToString());
                return false;
            }

            return false;
        }

        #endregion

        #region Overrides

        public override void DisableSimulator(UUID avatarID, ulong RegionHandle)
        {
            OSD item = EventQueueHelper.DisableSimulator(RegionHandle);
            TryEnqueue(item, avatarID, RegionHandle);

            //WRONG, COMMENTS LEFT FOR FUTURE PEOPLE TO UNDERSTAND WHY IT IS WRONG
            //ALSO, do the base.Enqueue so the region Caps get the kill request as well
            //END WRONG COMMENTS

            //This is wrong because the client doesn't call the EventQueueHandler on the sim, 
            //  and we can be sure this is a sim, not the grid, as its the connector that posts 
            //  to the grid service. Instead, we must do the killing manually so
            //  that this region gets cleaned up.
            //base.Enqueue(item, avatarID, RegionHandle);
            IClientCapsService clientCaps = m_service.GetClientCapsService(avatarID);
            if(clientCaps != null)
                clientCaps.RemoveCAPS(RegionHandle); // DIE!!!

            //Remove all things in the queue as well as we can't have them staying around
            ClearEventQueue(avatarID, RegionHandle);
        }

        #endregion

        private class EventQueueClient
        {
            private Dictionary<ulong, List<OSD>> eventQueue = new Dictionary<ulong,List<OSD>>();

            public void AddNewEvent(ulong regionHandle, OSD ev)
            {
                lock (eventQueue)
                {
                    if (!eventQueue.ContainsKey(regionHandle))
                        eventQueue.Add(regionHandle, new List<OSD>());
                    eventQueue[regionHandle].Add(ev);
                }
            }

            public void ClearEvents(ulong RegionHandle)
            {
                lock (eventQueue)
                {
                    eventQueue.Remove(RegionHandle);
                }
            }

            public List<OSD> GetEvents(ulong regionHandle)
            {
                OSD[] retVal = new OSD[0];
                lock (eventQueue)
                {
                    if (eventQueue.ContainsKey(regionHandle))
                    {
                        retVal = new OSD[eventQueue[regionHandle].Count];
                        eventQueue[regionHandle].CopyTo(retVal);
                    }
                }
                return new List<OSD>(retVal);
            }
        }
    }
}
