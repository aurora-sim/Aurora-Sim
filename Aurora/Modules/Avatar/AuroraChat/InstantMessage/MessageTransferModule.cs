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
using System.Linq;
using System.Net;
using System.Reflection;
using Nini.Config;
using Nwc.XmlRpc;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Modules.Chat
{
    public class MessageTransferModule : ISharedRegionModule, IMessageTransferModule
    {
        #region Delegates

        /// <summary>
        ///   delegate for sending a grid instant message asynchronously
        /// </summary>
        public delegate void GridInstantMessageDelegate(
            GridInstantMessage im, GridRegion prevRegion);

        #endregion

        /// <summary>
        ///   Param UUID - AgentID
        ///   Param string - HTTP path to the region the user is in, blank if not found
        /// </summary>
        public Dictionary<UUID, string> IMUsersCache = new Dictionary<UUID, string>();

        private bool m_Enabled;
        protected List<IScene> m_Scenes = new List<IScene>();

        #region IMessageTransferModule Members

        public event UndeliveredMessage OnUndeliveredMessage;

        public virtual void SendInstantMessages(GridInstantMessage im, List<UUID> AgentsToSendTo)
        {
            //Check for local users first
            List<UUID> RemoveUsers = new List<UUID>();
            foreach (IScene scene in m_Scenes)
            {
                foreach (UUID t in AgentsToSendTo)
                {
                    IScenePresence user;
                    if (!RemoveUsers.Contains(t) &&
                        scene.TryGetScenePresence(t, out user))
                    {
                        // Local message
                        user.ControllingClient.SendInstantMessage(im);
                        RemoveUsers.Add(t);
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

        public virtual void SendInstantMessage(GridInstantMessage im)
        {
            UUID toAgentID = im.toAgentID;

            //Look locally first
            foreach (IScene scene in m_Scenes)
            {
                IScenePresence user;
                if (scene.TryGetScenePresence(toAgentID, out user))
                {
                    user.ControllingClient.SendInstantMessage(im);
                    return;
                }
                ISceneChildEntity childPrim = null;
                if ((childPrim = scene.GetSceneObjectPart(toAgentID)) != null)
                {
                    im.toAgentID = childPrim.OwnerID;
                    SendInstantMessage(im);
                    return;
                }
            }
            //MainConsole.Instance.DebugFormat("[INSTANT MESSAGE]: Delivering IM to {0} via XMLRPC", im.toAgentID);
            SendGridInstantMessageViaXMLRPC(im);
        }

        #endregion

        #region ISharedRegionModule Members

        public virtual void Initialise(IConfigSource config)
        {
            IConfig cnf = config.Configs["Messaging"];
            if (cnf != null && cnf.GetString(
                "MessageTransferModule", "MessageTransferModule") !=
                "MessageTransferModule")
            {
                MainConsole.Instance.Debug("[MESSAGE TRANSFER]: Disabled by configuration");
                return;
            }

            m_Enabled = true;
        }

        public virtual void AddRegion(IScene scene)
        {
            if (!m_Enabled)
                return;

            lock (m_Scenes)
            {
                //MainConsole.Instance.Debug("[MESSAGE TRANSFER]: Message transfer module active");
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

        public virtual void RegionLoaded(IScene scene)
        {
        }

        public virtual void RemoveRegion(IScene scene)
        {
            if (!m_Enabled)
                return;

            lock (m_Scenes)
                m_Scenes.Remove(scene);
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

        #endregion

        private void HandleUndeliveredMessage(GridInstantMessage im, string reason)
        {
            UndeliveredMessage handlerUndeliveredMessage = OnUndeliveredMessage;

            // If this event has handlers, then an IM from an agent will be
            // considered delivered. This will suppress the error message.
            //
            if (handlerUndeliveredMessage != null)
            {
                handlerUndeliveredMessage(im, reason);
                return;
            }

            //MainConsole.Instance.DebugFormat("[INSTANT MESSAGE]: Undeliverable");
        }

        /// <summary>
        ///   Process a XMLRPC Grid Instant Message
        /// </summary>
        /// <param name = "request">XMLRPC parameters
        /// </param>
        /// <param name="remoteClient"> </param>
        /// <returns>Nothing much</returns>
        protected virtual XmlRpcResponse processXMLRPCGridInstantMessage(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            bool successful = false;
            GridInstantMessage gim = new GridInstantMessage();
            Hashtable requestData = (Hashtable) request.Params[0];

            if (requestData.ContainsKey("message"))
            {
                try
                {
                    //Deserialize it
                    gim.FromOSD((OSDMap) OSDParser.DeserializeJson(requestData["message"].ToString()));
                }
                catch
                {
                    UUID fromAgentID = UUID.Zero;
                    UUID toAgentID = UUID.Zero;
                    UUID imSessionID = UUID.Zero;
                    uint timestamp = 0;
                    string fromAgentName = "";
                    string message = "";
                    byte dialog = (byte) 0;
                    bool fromGroup = false;
                    uint ParentEstateID = 0;
                    Vector3 Position = Vector3.Zero;
                    UUID RegionID = UUID.Zero;
                    byte[] binaryBucket = new byte[0];

                    float pos_x = 0;
                    float pos_y = 0;
                    float pos_z = 0;
                    if (requestData.ContainsKey("from_agent_id")
                        && requestData.ContainsKey("to_agent_id") && requestData.ContainsKey("im_session_id")
                        && requestData.ContainsKey("timestamp") && requestData.ContainsKey("from_agent_name")
                        && requestData.ContainsKey("message") && requestData.ContainsKey("dialog")
                        && requestData.ContainsKey("from_group")
                        && requestData.ContainsKey("offline") && requestData.ContainsKey("parent_estate_id")
                        && requestData.ContainsKey("position_x") && requestData.ContainsKey("position_y")
                        && requestData.ContainsKey("position_z") && requestData.ContainsKey("region_id")
                        && requestData.ContainsKey("binary_bucket"))
                    {
                        // Do the easy way of validating the UUIDs
                        UUID.TryParse((string) requestData["from_agent_id"], out fromAgentID);
                        UUID.TryParse((string) requestData["to_agent_id"], out toAgentID);
                        UUID.TryParse((string) requestData["im_session_id"], out imSessionID);
                        UUID.TryParse((string) requestData["region_id"], out RegionID);

                        try
                        {
                            timestamp = (uint) Convert.ToInt32((string) requestData["timestamp"]);
                        }
                        catch (ArgumentException)
                        {
                        }
                        catch (FormatException)
                        {
                        }
                        catch (OverflowException)
                        {
                        }

                        fromAgentName = (string) requestData["from_agent_name"];
                        message = (string) requestData["message"];
                        if (message == null)
                            message = string.Empty;

                        // Bytes don't transfer well over XMLRPC, so, we Base64 Encode them.
                        string requestData1 = (string) requestData["dialog"];
                        if (string.IsNullOrEmpty(requestData1))
                        {
                            dialog = 0;
                        }
                        else
                        {
                            byte[] dialogdata = Convert.FromBase64String(requestData1);
                            dialog = dialogdata[0];
                        }

                        if ((string) requestData["from_group"] == "TRUE")
                            fromGroup = true;

                        try
                        {
                            ParentEstateID = (uint) Convert.ToInt32((string) requestData["parent_estate_id"]);
                        }
                        catch (ArgumentException)
                        {
                        }
                        catch (FormatException)
                        {
                        }
                        catch (OverflowException)
                        {
                        }

                        try
                        {
                            pos_x = float.Parse((string) requestData["position_x"]);
                        }
                        catch (ArgumentException)
                        {
                        }
                        catch (FormatException)
                        {
                        }
                        catch (OverflowException)
                        {
                        }
                        try
                        {
                            pos_y = float.Parse((string)requestData["position_y"]);
                        }
                        catch (ArgumentException)
                        {
                        }
                        catch (FormatException)
                        {
                        }
                        catch (OverflowException)
                        {
                        }
                        try
                        {
                            pos_z = float.Parse((string)requestData["position_z"]);
                        }
                        catch (ArgumentException)
                        {
                        }
                        catch (FormatException)
                        {
                        }
                        catch (OverflowException)
                        {
                        }

                        Position = new Vector3(pos_x, pos_y, pos_z);

                        string requestData3 = (string) requestData["binary_bucket"];
                        binaryBucket = string.IsNullOrEmpty(requestData3) ? new byte[0] : Convert.FromBase64String(requestData3);

                        // Create a New GridInstantMessageObject the the data
                        gim.fromAgentID = fromAgentID;
                        gim.fromAgentName = fromAgentName;
                        gim.fromGroup = fromGroup;
                        gim.imSessionID = imSessionID;
                        gim.RegionID = UUID.Zero; // RegionID.Guid;
                        gim.timestamp = timestamp;
                        gim.toAgentID = toAgentID;
                        gim.message = message;
                        gim.dialog = dialog;
                        gim.offline = 0;
                        gim.ParentEstateID = ParentEstateID;
                        gim.Position = Position;
                        gim.binaryBucket = binaryBucket;
                    }
                }

                if (gim.dialog == (byte) InstantMessageDialog.GodLikeRequestTeleport)
                    gim.dialog = (byte) InstantMessageDialog.RequestTeleport;

                // Trigger the Instant message in the scene.
                foreach (IScene scene in m_Scenes)
                {
                    IScenePresence user;
                    if (scene.TryGetScenePresence(gim.toAgentID, out user))
                    {
                        if (!user.IsChildAgent)
                        {
                            scene.EventManager.TriggerIncomingInstantMessage(gim);
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
                    m_Scenes[0].EventManager.TriggerUnhandledInstantMessage(gim);
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

        protected virtual void GridInstantMessageCompleted(IAsyncResult iar)
        {
            GridInstantMessageDelegate icon =
                (GridInstantMessageDelegate) iar.AsyncState;
            icon.EndInvoke(iar);
        }

        protected virtual void SendGridInstantMessageViaXMLRPC(GridInstantMessage im)
        {
            GridInstantMessageDelegate d = SendGridInstantMessageViaXMLRPCAsync;

            d.BeginInvoke(im, null, GridInstantMessageCompleted, d);
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
                    msgdata = ConvertGridInstantMessageToXMLRPCXML(im);
                    if (!doIMSending(kvp.Value, msgdata))
                    {
                        //If this fails, the user has either moved from their stored location or logged out
                        //Since it failed, let it look them up again and rerun
                        lock (IMUsersCache)
                        {
                            IMUsersCache.Remove(kvp.Key);
                        }
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
#if (!ISWIN)
            List<string> Queries = new List<string>();
            foreach (UUID agentId in users)
                Queries.Add(agentId.ToString());
#else
            List<string> Queries = users.Select(agentID => agentID.ToString()).ToList();
#endif

            if (Queries.Count == 0)
                return; //All done

            //Ask for the user new style first
            List<string> AgentLocations =
                m_Scenes[0].RequestModuleInterface<IAgentInfoService>().GetAgentsLocations(im.fromAgentID.ToString(),
                                                                                           Queries);
            //If this is false, this doesn't exist on the presence server and we use the legacy way
            if (AgentLocations.Count != 0)
            {
                for (int i = 0; i < users.Count; i++)
                {
                    //No agents, so this user is offline
                    if (AgentLocations[i] == "NotOnline")
                    {
                        IMUsersCache.Remove(users[i]);
                        MainConsole.Instance.Debug("[GRID INSTANT MESSAGE]: Unable to deliver an instant message to " + users[i] +
                                    ", user was not online");
                        im.toAgentID = users[i];
                        HandleUndeliveredMessage(im, "User is not set as online by presence service.");
                        continue;
                    }
                    if (AgentLocations[i] == "NonExistant")
                    {
                        IMUsersCache.Remove(users[i]);
                        MainConsole.Instance.Info("[GRID INSTANT MESSAGE]: Unable to deliver an instant message to " + users[i] +
                                                  ", user does not exist");
                        im.toAgentID = users[i];
                        HandleUndeliveredMessage(im, "User does not exist.");
                        continue;
                    }
                    HTTPPaths.Add(users[i], AgentLocations[i]);
                }
            }
            else
            {
                MainConsole.Instance.Info("[GRID INSTANT MESSAGE]: Unable to deliver an instant message, no users found.");
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
                        msgdata = ConvertGridInstantMessageToXMLRPCXML(im);
                        if (!doIMSending(kvp.Value, msgdata))
                        {
                            //It failed
                            lock (IMUsersCache)
                            {
                                //Remove them so we keep testing against the db
                                IMUsersCache.Remove(kvp.Key);
                            }
                            HandleUndeliveredMessage(im, "Failed to send IM to destination.");
                        }
                    }
                    else
                    {
                        //Add to the cache
                        if (!IMUsersCache.ContainsKey(kvp.Key))
                            IMUsersCache.Add(kvp.Key, kvp.Value);
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
                    HandleUndeliveredMessage(im, "Agent Location was blank.");
                }
            }
        }

        /// <summary>
        ///   Recursive SendGridInstantMessage over XMLRPC method.
        ///   This is called from within a dedicated thread.
        ///   The first time this is called, prevRegionHandle will be 0 Subsequent times this is called from 
        ///   itself, prevRegionHandle will be the last region handle that we tried to send.
        ///   If the handles are the same, we look up the user's location using the grid.
        ///   If the handles are still the same, we end.  The send failed.
        /// </summary>
        /// <param name = "prevRegionHandle">
        ///   Pass in 0 the first time this method is called.  It will be called recursively with the last 
        ///   regionhandle tried
        /// </param>
        protected virtual void SendGridInstantMessageViaXMLRPCAsync(GridInstantMessage im,
                                                                    GridRegion prevRegion)
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
                    msgdata = ConvertGridInstantMessageToXMLRPCXML(im);
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
                        return;
                    }
                }
                else
                {
                    //Send the IM, and it made it to the user, return true
                    return;
                }
            }

            var userManagement = m_Scenes[0].RequestModuleInterface<IUserFinder>();
            if (userManagement != null && !userManagement.IsLocalGridUser(toAgentID)) // foreign user
                HTTPPath = userManagement.GetUserServerURL(toAgentID, "IMServerURI");

            if (HTTPPath != "")
            {
                //We've tried to send an IM to them before, pull out their info
                //Send the IM to their last location
                if (!doIMSending(HTTPPath, msgdata))
                {
                    msgdata = ConvertGridInstantMessageToXMLRPCXML(im);
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
                        //Add to the cache
                        if (!IMUsersCache.ContainsKey(toAgentID))
                            IMUsersCache.Add(toAgentID, HTTPPath);
                        //Send the IM, and it made it to the user, return true
                        return;
                    }
                }
                else
                {
                    //Add to the cache
                    if (!IMUsersCache.ContainsKey(toAgentID))
                        IMUsersCache.Add(toAgentID, HTTPPath);
                    //Send the IM, and it made it to the user, return true
                    return;
                }
            }

            //Now query the grid server for the agent
            IAgentInfoService ais = m_Scenes[0].RequestModuleInterface<IAgentInfoService>();
            List<string> AgentLocations = ais.GetAgentsLocations(im.fromAgentID.ToString(), new List<string>(new[] { toAgentID.ToString() }));
            if (AgentLocations.Count > 0)
            {
                //No agents, so this user is offline
                if (AgentLocations[0] == "NotOnline")
                {
                    lock (IMUsersCache)
                    {
                        //Remove them so we keep testing against the db
                        IMUsersCache.Remove(toAgentID);
                    }
                    MainConsole.Instance.Info("[GRID INSTANT MESSAGE]: Unable to deliver an instant message");
                    HandleUndeliveredMessage(im, "User is not set as online by presence service.");
                    return;
                }
                if (AgentLocations[0] == "NonExistant")
                {
                    IMUsersCache.Remove(toAgentID);
                    MainConsole.Instance.Info("[GRID INSTANT MESSAGE]: Unable to deliver an instant message to " + toAgentID +
                                              ", user does not exist");
                    HandleUndeliveredMessage(im, "User does not exist.");
                    return;
                }
                HTTPPath = AgentLocations[0];
            }

            //We found the agent's location, now ask them about the user
            if (HTTPPath != "")
            {
                if (!doIMSending(HTTPPath, msgdata))
                {
                    msgdata = ConvertGridInstantMessageToXMLRPCXML(im);
                    if (!doIMSending(HTTPPath, msgdata))
                    {
                        //It failed, stop now
                        lock (IMUsersCache)
                        {
                            //Remove them so we keep testing against the db
                            IMUsersCache.Remove(toAgentID);
                        }
                        MainConsole.Instance.Info(
                            "[GRID INSTANT MESSAGE]: Unable to deliver an instant message as the region could not be found");
                        HandleUndeliveredMessage(im, "Failed to send IM to destination.");
                        return;
                    }
                    else
                    {
                        //Add to the cache
                        if (!IMUsersCache.ContainsKey(toAgentID))
                            IMUsersCache.Add(toAgentID, HTTPPath);
                        //Send the IM, and it made it to the user, return true
                        return;
                    }
                }
                else
                {
                    //Add to the cache
                    if (!IMUsersCache.ContainsKey(toAgentID))
                        IMUsersCache.Add(toAgentID, HTTPPath);
                    //Send the IM, and it made it to the user, return true
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
                MainConsole.Instance.Info(
                    "[GRID INSTANT MESSAGE]: Unable to deliver an instant message as the region could not be found");
                HandleUndeliveredMessage(im, "Agent Location was blank.");
            }
        }

        /// <summary>
        ///   This actually does the XMLRPC Request
        /// </summary>
        /// <param name = "httpInfo">RegionInfo we pull the data out of to send the request to</param>
        /// <param name = "xmlrpcdata">The Instant Message data Hashtable</param>
        /// <returns>Bool if the message was successfully delivered at the other side.</returns>
        protected virtual bool doIMSending(string httpInfo, Hashtable xmlrpcdata)
        {
            ArrayList SendParams = new ArrayList {xmlrpcdata};
            XmlRpcRequest GridReq = new XmlRpcRequest("grid_instant_message", SendParams);
            try
            {
                XmlRpcResponse GridResp = GridReq.Send(httpInfo, 7000);

                Hashtable responseData = (Hashtable) GridResp.Value;

                if (responseData.ContainsKey("success"))
                {
                    if ((string) responseData["success"] == "TRUE")
                        return true;
                    return false;
                }
                return false;
            }
            catch (WebException e)
            {
                MainConsole.Instance.ErrorFormat("[GRID INSTANT MESSAGE]: Error sending message to " + httpInfo, e.Message);
            }

            return false;
        }

        /// <summary>
        ///   Takes a GridInstantMessage and converts it into a Hashtable for XMLRPC
        /// </summary>
        /// <param name = "msg">The GridInstantMessage object</param>
        /// <returns>Hashtable containing the XMLRPC request</returns>
        protected virtual Hashtable ConvertGridInstantMessageToXMLRPC(GridInstantMessage msg)
        {
            Hashtable gim = new Hashtable();
            gim["message"] = OSDParser.SerializeJsonString(msg.ToOSD());
            return gim;
        }

        protected virtual Hashtable ConvertGridInstantMessageToXMLRPCXML(GridInstantMessage msg)
        {
            Hashtable gim = new Hashtable();
            gim["from_agent_id"] = msg.fromAgentID.ToString();
            // Kept for compatibility
            gim["from_agent_session"] = UUID.Zero.ToString();
            gim["to_agent_id"] = msg.toAgentID.ToString();
            gim["im_session_id"] = msg.imSessionID.ToString();
            gim["timestamp"] = msg.timestamp.ToString();
            gim["from_agent_name"] = msg.fromAgentName;
            gim["message"] = msg.message;
            byte[] dialogdata = new byte[1];
            dialogdata[0] = msg.dialog;
            gim["dialog"] = Convert.ToBase64String(dialogdata, Base64FormattingOptions.None);

            if (msg.fromGroup)
                gim["from_group"] = "TRUE";
            else
                gim["from_group"] = "FALSE";
            byte[] offlinedata = new byte[1];
            offlinedata[0] = msg.offline;
            gim["offline"] = Convert.ToBase64String(offlinedata, Base64FormattingOptions.None);
            gim["parent_estate_id"] = msg.ParentEstateID.ToString();
            gim["position_x"] = msg.Position.X.ToString();
            gim["position_y"] = msg.Position.Y.ToString();
            gim["position_z"] = msg.Position.Z.ToString();
            gim["region_id"] = msg.RegionID.ToString();
            gim["binary_bucket"] = Convert.ToBase64String(msg.binaryBucket, Base64FormattingOptions.None);
            return gim;
        }
    }
}