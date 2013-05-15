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

using Aurora.Framework;
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Servers;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using GridRegion = Aurora.Framework.Services.GridRegion;

namespace Aurora.Modules.Chat
{
    public class MessageTransferModule : INonSharedRegionModule, IMessageTransferModule
    {
        #region Delegates

        /// <summary>
        ///     delegate for sending a grid instant message asynchronously
        /// </summary>
        public delegate void GridInstantMessageDelegate(
            GridInstantMessage im, GridRegion prevRegion);

        #endregion

        /// <summary>
        ///     Param UUID - AgentID
        ///     Param string - HTTP path to the region the user is in, blank if not found
        /// </summary>
        public Dictionary<UUID, string> IMUsersCache = new Dictionary<UUID, string>();

        private bool m_Enabled;
        protected IScene m_Scene;

        #region IMessageTransferModule Members

        public event UndeliveredMessage OnUndeliveredMessage;

        public virtual void SendInstantMessages(GridInstantMessage im, List<UUID> AgentsToSendTo)
        {
            //Check for local users first
            List<UUID> RemoveUsers = new List<UUID>();
            foreach (UUID t in AgentsToSendTo)
            {
                IScenePresence user;
                if (!RemoveUsers.Contains(t) &&
                    m_Scene.TryGetScenePresence(t, out user))
                {
                    // Local message
                    user.ControllingClient.SendInstantMessage(im);
                    RemoveUsers.Add(t);
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
            UUID toAgentID = im.ToAgentID;

            //Look locally first
            IScenePresence user;
            if (m_Scene.TryGetScenePresence(toAgentID, out user))
            {
                user.ControllingClient.SendInstantMessage(im);
                return;
            }
            ISceneChildEntity childPrim = null;
            if ((childPrim = m_Scene.GetSceneObjectPart(toAgentID)) != null)
            {
                im.ToAgentID = childPrim.OwnerID;
                SendInstantMessage(im);
                return;
            }
            //MainConsole.Instance.DebugFormat("[INSTANT MESSAGE]: Delivering IM to {0} via XMLRPC", im.toAgentID);
            SendGridInstantMessageViaXMLRPC(im);
        }

        #endregion

        #region INonSharedRegionModule Members

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

            MainServer.Instance.AddStreamHandler(new GenericStreamHandler("POST", "/gridinstantmessages/", processGridInstantMessage));
        }

        public virtual void AddRegion(IScene scene)
        {
            if (!m_Enabled)
                return;

            m_Scene = scene;
            //MainConsole.Instance.Debug("[MESSAGE TRANSFER]: Message transfer module active");
            scene.RegisterModuleInterface<IMessageTransferModule>(this);
        }

        public virtual void RegionLoaded(IScene scene)
        {
        }

        public virtual void RemoveRegion(IScene scene)
        {
            if (!m_Enabled)
                return;

            m_Scene = null;
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

        protected virtual byte[] processGridInstantMessage(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            GridInstantMessage gim = ProtoBuf.Serializer.Deserialize<GridInstantMessage>(request);
            
            // Trigger the Instant message in the scene.
            IScenePresence user;
            bool successful = false;
            if (m_Scene.TryGetScenePresence(gim.ToAgentID, out user))
            {
                if (!user.IsChildAgent)
                {
                    m_Scene.EventManager.TriggerIncomingInstantMessage(gim);
                    successful = true;
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
                m_Scene.EventManager.TriggerUnhandledInstantMessage(gim);
            }

            //Send response back to region calling if it was successful
            // calling region uses this to know when to look up a user's location again.
            return new byte[1] { successful ? (byte)1 : (byte)0 };
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
                im.ToAgentID = kvp.Key;
                //We've tried to send an IM to them before, pull out their info
                //Send the IM to their last location
                if (!doIMSending(kvp.Value, im))
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
            List<string> Queries = users.Select(agentID => agentID.ToString()).ToList();

            if (Queries.Count == 0)
                return; //All done

            //Ask for the user new style first
            List<string> AgentLocations =
                m_Scene.RequestModuleInterface<IAgentInfoService>().GetAgentsLocations(im.FromAgentID.ToString(),
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
                        MainConsole.Instance.Debug("[GRID INSTANT MESSAGE]: Unable to deliver an instant message to " +
                                                   users[i] +
                                                   ", user was not online");
                        im.ToAgentID = users[i];
                        HandleUndeliveredMessage(im, "User is not set as online by presence service.");
                        continue;
                    }
                    if (AgentLocations[i] == "NonExistant")
                    {
                        IMUsersCache.Remove(users[i]);
                        MainConsole.Instance.Info("[GRID INSTANT MESSAGE]: Unable to deliver an instant message to " +
                                                  users[i] +
                                                  ", user does not exist");
                        im.ToAgentID = users[i];
                        HandleUndeliveredMessage(im, "User does not exist.");
                        continue;
                    }
                    HTTPPaths.Add(users[i], AgentLocations[i]);
                }
            }
            else
            {
                MainConsole.Instance.Info(
                    "[GRID INSTANT MESSAGE]: Unable to deliver an instant message, no users found.");
                return;
            }

            //We found the agent's location, now ask them about the user
            foreach (KeyValuePair<UUID, string> kvp in HTTPPaths)
            {
                if (kvp.Value != "")
                {
                    im.ToAgentID = kvp.Key;
                    if (!doIMSending(kvp.Value, im))
                    {
                        //It failed
                        lock (IMUsersCache)
                        {
                            //Remove them so we keep testing against the db
                            IMUsersCache.Remove(kvp.Key);
                        }
                        HandleUndeliveredMessage(im, "Failed to send IM to destination.");
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
        ///     Recursive SendGridInstantMessage over XMLRPC method.
        ///     This is called from within a dedicated thread.
        ///     The first time this is called, prevRegionHandle will be 0 Subsequent times this is called from
        ///     itself, prevRegionHandle will be the last region handle that we tried to send.
        ///     If the handles are the same, we look up the user's location using the grid.
        ///     If the handles are still the same, we end.  The send failed.
        /// </summary>
        /// <param name="im"></param>
        /// <param name="prevRegion">
        ///     Pass in 0 the first time this method is called.  It will be called recursively with the last
        ///     regionhandle tried
        /// </param>
        protected virtual void SendGridInstantMessageViaXMLRPCAsync(GridInstantMessage im,
                                                                    GridRegion prevRegion)
        {
            UUID toAgentID = im.ToAgentID;
            string HTTPPath = "";

            lock (IMUsersCache)
            {
                if (!IMUsersCache.TryGetValue(toAgentID, out HTTPPath))
                    HTTPPath = "";
            }

            if (HTTPPath != "")
            {
                //We've tried to send an IM to them before, pull out their info
                //Send the IM to their last location
                if (!doIMSending(HTTPPath, im))
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

            //Now query the grid server for the agent
            IAgentInfoService ais = m_Scene.RequestModuleInterface<IAgentInfoService>();
            List<string> AgentLocations = ais.GetAgentsLocations(im.FromAgentID.ToString(),
                                                                 new List<string>(new[] {toAgentID.ToString()}));
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
                    MainConsole.Instance.Info("[GRID INSTANT MESSAGE]: Unable to deliver an instant message to " +
                                              toAgentID +
                                              ", user does not exist");
                    HandleUndeliveredMessage(im, "User does not exist.");
                    return;
                }
                HTTPPath = AgentLocations[0];
            }

            //We found the agent's location, now ask them about the user
            if (HTTPPath != "")
            {
                if (!doIMSending(HTTPPath, im))
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
        ///     This actually does the XMLRPC Request
        /// </summary>
        /// <param name="httpInfo">RegionInfo we pull the data out of to send the request to</param>
        /// <param name="xmlrpcdata">The Instant Message data Hashtable</param>
        /// <returns>Bool if the message was successfully delivered at the other side.</returns>
        protected virtual bool doIMSending(string httpInfo, GridInstantMessage message)
        {
            MemoryStream stream = new MemoryStream();
            ProtoBuf.Serializer.Serialize(stream, message);
            byte[] data = WebUtils.PostToService(httpInfo + "/gridinstantmessages/", stream.ToArray());
            return data == null || data.Length == 0 || data[0] == 0 ? false : true;
        }
    }
}