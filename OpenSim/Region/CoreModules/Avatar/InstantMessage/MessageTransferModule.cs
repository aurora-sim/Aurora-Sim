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
using log4net;
using Nini.Config;
using Nwc.XmlRpc;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using OpenSim.Services.Interfaces;

namespace OpenSim.Region.CoreModules.Avatar.InstantMessage
{
    public class MessageTransferModule : ISharedRegionModule, IMessageTransferModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private bool m_Enabled = false;
        protected List<Scene> m_Scenes = new List<Scene>();
        
        public event UndeliveredMessage OnUndeliveredMessage;
        private MessageResultNotification m_result = delegate(bool success) { };
        public virtual void Initialise(IConfigSource config)
        {
            IConfig cnf = config.Configs["Messaging"];
            if (cnf != null && cnf.GetString(
                    "MessageTransferModule", "MessageTransferModule") !=
                    "MessageTransferModule")
            {
                m_log.Debug("[MESSAGE TRANSFER]: Disabled by configuration");
                return;
            }

            m_Enabled = true;
        }

        public virtual void AddRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            lock (m_Scenes)
            {
                //m_log.Debug("[MESSAGE TRANSFER]: Message transfer module active");
                scene.RegisterModuleInterface<IMessageTransferModule>(this);
                m_Scenes.Add(scene);
            }
        }

        public virtual void PostInitialise()
        {
            if (!m_Enabled)
                return;

            MainServer.Instance.AddXmlRPCHandler(
                "grid_instant_message", processXMLRPCGridInstantMessage);
        }

        public virtual void RegionLoaded(Scene scene)
        {
        }

        public virtual void RemoveRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            lock (m_Scenes)
            {
                m_Scenes.Remove(scene);
            }
        }

        public virtual void Close()
        {
        }

        public virtual string Name
        {
            get { return "MessageTransferModule"; }
        }

        public virtual Type ReplaceableInterface
        {
            get { return null; }
        }

        public virtual void SendInstantMessage(GridInstantMessage im)
        {
            SendInstantMessage(im, m_result);
        }

        public virtual void SendInstantMessages(GridInstantMessage im, List<UUID> AgentsToSendTo)
        {
            //Check for local users first
            List<UUID> RemoveUsers = new List<UUID>();
            foreach (Scene scene in m_Scenes)
            {
                for(int i = 0; i < AgentsToSendTo.Count; i++)
                {
                    IScenePresence user;
                    if (scene.TryGetScenePresence (AgentsToSendTo[i], out user))
                    {
                        // Local message
                        user.ControllingClient.SendInstantMessage (im);
                        RemoveUsers.Add (AgentsToSendTo[i]);
                    }
                }
            }
            //Clear the local users out
            foreach (UUID agentID in RemoveUsers)
            {
                AgentsToSendTo.Remove(agentID);
            }

            SendMultipleGridInstantMessageViaXMLRPC(im, AgentsToSendTo);
        }

        public virtual void SendInstantMessage(GridInstantMessage im, MessageResultNotification result)
        {
            UUID toAgentID = im.toAgentID;

            // Try root avatar only first - incorrect now, see below
            foreach (Scene scene in m_Scenes)
            {
                IScenePresence user;
                if (scene.TryGetScenePresence (toAgentID, out user))
                {
                    user.ControllingClient.SendInstantMessage (im);
                    return;
                }
            }
            //m_log.DebugFormat("[INSTANT MESSAGE]: Delivering IM to {0} via XMLRPC", im.toAgentID);
            SendGridInstantMessageViaXMLRPC(im, result);
        }

        private void HandleUndeliveredMessage(GridInstantMessage im, MessageResultNotification result)
        {
            UndeliveredMessage handlerUndeliveredMessage = OnUndeliveredMessage;

            // If this event has handlers, then an IM from an agent will be
            // considered delivered. This will suppress the error message.
            //
            if (handlerUndeliveredMessage != null)
            {
                handlerUndeliveredMessage(im);
                if (im.dialog == (byte)InstantMessageDialog.MessageFromAgent)
                    result(true);
                else
                    result(false);
                return;
            }

            //m_log.DebugFormat("[INSTANT MESSAGE]: Undeliverable");
            result(false);
        }

        /// <summary>
        /// Process a XMLRPC Grid Instant Message
        /// </summary>
        /// <param name="request">XMLRPC parameters
        /// </param>
        /// <returns>Nothing much</returns>
        protected virtual XmlRpcResponse processXMLRPCGridInstantMessage(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            bool successful = false;
            GridInstantMessage gim = new GridInstantMessage ();
            Hashtable requestData = (Hashtable)request.Params[0];

            if (requestData.ContainsKey ("message"))
            {
                //Deserialize it
                gim.FromOSD ((OSDMap)OSDParser.DeserializeJson (requestData["message"].ToString ()));

                if (gim.dialog == (byte)InstantMessageDialog.GodLikeRequestTeleport)
                    gim.dialog = (byte)InstantMessageDialog.RequestTeleport;

                // Trigger the Instant message in the scene.
                foreach (Scene scene in m_Scenes)
                {
                    IScenePresence user;
                    if (scene.TryGetScenePresence (gim.toAgentID, out user))
                    {
                        if (!user.IsChildAgent)
                        {
                            scene.EventManager.TriggerIncomingInstantMessage (gim);
                            successful = true;
                        }
                    }
                }
                if (!successful)
                {
                    // If the message can't be delivered to an agent, it
                    // is likely to be a group IM. On a group IM, the
                    // imSessionID = toAgentID = group id. Raise the
                    // unhandled IM event to give the groups module
                    // a chance to pick it up. We raise that in a random
                    // scene, since the groups module is shared.
                    //
                    m_Scenes[0].EventManager.TriggerUnhandledInstantMessage (gim);
                }
            }

            //Send response back to region calling if it was successful
            // calling region uses this to know when to look up a user's location again.
            XmlRpcResponse resp = new XmlRpcResponse();
            Hashtable respdata = new Hashtable();
            if (successful)
                respdata["success"] = "TRUE";
            else
                respdata["success"] = "FALSE";
            resp.Value = respdata;

            return resp;
        }

        /// <summary>
        /// delegate for sending a grid instant message asynchronously
        /// </summary>
        public delegate void GridInstantMessageDelegate(GridInstantMessage im, MessageResultNotification result, GridRegion prevRegion);

        protected virtual void GridInstantMessageCompleted(IAsyncResult iar)
        {
            GridInstantMessageDelegate icon =
                    (GridInstantMessageDelegate)iar.AsyncState;
            icon.EndInvoke(iar);
        }

        protected virtual void SendGridInstantMessageViaXMLRPC(GridInstantMessage im, MessageResultNotification result)
        {
            GridInstantMessageDelegate d = SendGridInstantMessageViaXMLRPCAsync;

            d.BeginInvoke(im, result, null, GridInstantMessageCompleted, d);
        }

        protected virtual void SendMultipleGridInstantMessageViaXMLRPC(GridInstantMessage im, List<UUID> users)
        {
            Dictionary<UUID, string> HTTPPaths = new Dictionary<UUID, string>();

            foreach (UUID agentID in users)
            {
                lock (IMUsersCache)
                {
                    string HTTPPath = "";
                    if (!IMUsersCache.TryGetValue(agentID, out HTTPPath))
                        HTTPPath = "";
                    else
                        HTTPPaths.Add(agentID, HTTPPath);
                }
            }
            List<UUID> CompletedUsers = new List<UUID>();
            foreach (KeyValuePair<UUID, string> kvp in HTTPPaths)
            {
                //Fix the agentID
                im.toAgentID = kvp.Key;
                Hashtable msgdata = ConvertGridInstantMessageToXMLRPC(im);
                //We've tried to send an IM to them before, pull out their info
                //Send the IM to their last location
                if (!doIMSending(kvp.Value, msgdata))
                {
                    //If this fails, the user has either moved from their stored location or logged out
                    //Since it failed, let it look them up again and rerun
                    lock (IMUsersCache)
                    {
                        IMUsersCache.Remove(kvp.Key);
                    }
                }
                else
                {
                    //Send the IM, and it made it to the user, return true
                    CompletedUsers.Add(kvp.Key);
                }
            }

            //Remove the finished users
            foreach (UUID agentID in CompletedUsers)
            {
                users.Remove(agentID);
            }
            HTTPPaths.Clear();

            //Now query the grid server for the agents
            List<string> Queries = new List<string>();
            foreach (UUID agentID in users)
            {
                Queries.Add(agentID.ToString());
            }

            //Ask for the user new style first
            string[] AgentLocations = m_Scenes[0].RequestModuleInterface<IAgentInfoService>().GetAgentsLocations(Queries.ToArray());
            //If this is false, this doesn't exist on the presence server and we use the legacy way
            if (AgentLocations.Length != 0)
            {
                for (int i = 0; i < users.Count; i++)
                {
                    //No agents, so this user is offline
                    if (AgentLocations[i] == "NotOnline")
                    {
                        IMUsersCache.Remove(users[i]);
                        m_log.Debug("[GRID INSTANT MESSAGE]: Unable to deliver an instant message to " + users[i] + ", user was not online");
                        HandleUndeliveredMessage(im, m_result);
                        continue;
                    }
                    else
                        HTTPPaths.Add(users[i], AgentLocations[i]);
                }
            }
            else
            {
                m_log.Info("[GRID INSTANT MESSAGE]: Unable to deliver an instant message, no users found.");
                return;
            }

            //We found the agent's location, now ask them about the user
            foreach (KeyValuePair<UUID, string> kvp in HTTPPaths)
            {
                if (kvp.Value != "")
                {
                    im.toAgentID = kvp.Key;
                    Hashtable msgdata = ConvertGridInstantMessageToXMLRPC(im);
                    if (!doIMSending(kvp.Value, msgdata))
                    {
                        //It failed
                        lock (IMUsersCache)
                        {
                            //Remove them so we keep testing against the db
                            IMUsersCache.Remove(kvp.Key);
                        }
                        HandleUndeliveredMessage(im, m_result);
                    }
                    else
                    {
                        //Add to the cache
                        if (!IMUsersCache.ContainsKey (kvp.Key))
                            IMUsersCache.Add (kvp.Key, kvp.Value);
                        //Send the IM, and it made it to the user, return true
                        continue;
                    }
                }
                else
                {
                    lock (IMUsersCache)
                    {
                        //Remove them so we keep testing against the db
                        IMUsersCache.Remove(kvp.Key);
                    }
                    HandleUndeliveredMessage(im, m_result);
                }
            }
        }

        /// <summary>
        /// Param UUID - AgentID
        /// Param string - HTTP path to the region the user is in, blank if not found
        /// </summary>
        public Dictionary<UUID, string> IMUsersCache = new Dictionary<UUID, string>();

        /// <summary>
        /// Recursive SendGridInstantMessage over XMLRPC method.
        /// This is called from within a dedicated thread.
        /// The first time this is called, prevRegionHandle will be 0 Subsequent times this is called from 
        /// itself, prevRegionHandle will be the last region handle that we tried to send.
        /// If the handles are the same, we look up the user's location using the grid.
        /// If the handles are still the same, we end.  The send failed.
        /// </summary>
        /// <param name="prevRegionHandle">
        /// Pass in 0 the first time this method is called.  It will be called recursively with the last 
        /// regionhandle tried
        /// </param>
        protected virtual void SendGridInstantMessageViaXMLRPCAsync(GridInstantMessage im, MessageResultNotification result, GridRegion prevRegion)
        {
            UUID toAgentID = im.toAgentID;
            string HTTPPath = "";

            Hashtable msgdata = ConvertGridInstantMessageToXMLRPC(im);

            lock (IMUsersCache)
            {
                if (!IMUsersCache.TryGetValue(toAgentID, out HTTPPath))
                    HTTPPath = "";
            }

            if (HTTPPath != "")
            {
                //We've tried to send an IM to them before, pull out their info
                //Send the IM to their last location
                if (!doIMSending(HTTPPath, msgdata))
                {
                    //If this fails, the user has either moved from their stored location or logged out
                    //Since it failed, let it look them up again and rerun
                    lock (IMUsersCache)
                    {
                        IMUsersCache.Remove(toAgentID);
                    }
                    //Clear the path and let it continue trying again.
                    HTTPPath = "";
                }
                else
                {
                    //Send the IM, and it made it to the user, return true
                    result(true);
                    return;
                }
            }

            //Now query the grid server for the agent

            //Ask for the user new style first
            string[] AgentLocations = m_Scenes[0].RequestModuleInterface<IAgentInfoService>().GetAgentsLocations(new string[] { toAgentID.ToString() });
            //If this is false, this doesn't exist on the presence server and we use the legacy way
            if (AgentLocations.Length > 0) 
            {
                //No agents, so this user is offline
                if (AgentLocations[0] == "NotOnline")
                {
                    lock (IMUsersCache)
                    {
                        //Remove them so we keep testing against the db
                        IMUsersCache.Remove(toAgentID);
                    }
                    m_log.Info("[GRID INSTANT MESSAGE]: Unable to deliver an instant message");
                    HandleUndeliveredMessage(im, result);
                    return;
                }
                else //Found the agent, use this location
                    HTTPPath = AgentLocations[0];
            }

            //We found the agent's location, now ask them about the user
            if (HTTPPath != "")
            {
                if (!doIMSending(HTTPPath, msgdata))
                {
                    //It failed, stop now
                    lock (IMUsersCache)
                    {
                        //Remove them so we keep testing against the db
                        IMUsersCache.Remove(toAgentID);
                    }
                    m_log.Info("[GRID INSTANT MESSAGE]: Unable to deliver an instant message as the region could not be found");
                    HandleUndeliveredMessage(im, result);
                    return;
                }
                else
                {
                    //Add to the cache
                    if(!IMUsersCache.ContainsKey(toAgentID))
                        IMUsersCache.Add (toAgentID, HTTPPath);
                    //Send the IM, and it made it to the user, return true
                    result(true);
                    return;
                }
            }
            else
            {
                //Couldn't find them, stop for now
                lock (IMUsersCache)
                {
                    //Remove them so we keep testing against the db
                    IMUsersCache.Remove(toAgentID);
                }
                m_log.Info("[GRID INSTANT MESSAGE]: Unable to deliver an instant message as the region could not be found");
                HandleUndeliveredMessage(im, result);
            }
        }

        /// <summary>
        /// This actually does the XMLRPC Request
        /// </summary>
        /// <param name="reginfo">RegionInfo we pull the data out of to send the request to</param>
        /// <param name="xmlrpcdata">The Instant Message data Hashtable</param>
        /// <returns>Bool if the message was successfully delivered at the other side.</returns>
        protected virtual bool doIMSending(string httpInfo, Hashtable xmlrpcdata)
        {

            ArrayList SendParams = new ArrayList();
            SendParams.Add(xmlrpcdata);
            XmlRpcRequest GridReq = new XmlRpcRequest("grid_instant_message", SendParams);
            try
            {
                XmlRpcResponse GridResp = GridReq.Send(httpInfo, 7000);

                Hashtable responseData = (Hashtable)GridResp.Value;

                if (responseData.ContainsKey("success"))
                {
                    if ((string)responseData["success"] == "TRUE")
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (WebException e)
            {
                m_log.ErrorFormat("[GRID INSTANT MESSAGE]: Error sending message to " + httpInfo, e.Message);
            }

            return false;
        }

        /// <summary>
        /// Takes a GridInstantMessage and converts it into a Hashtable for XMLRPC
        /// </summary>
        /// <param name="msg">The GridInstantMessage object</param>
        /// <returns>Hashtable containing the XMLRPC request</returns>
        protected virtual Hashtable ConvertGridInstantMessageToXMLRPC(GridInstantMessage msg)
        {
            Hashtable gim = new Hashtable();
            gim["message"] = OSDParser.SerializeJsonString (msg.ToOSD ());
            return gim;
        }
    }
}
