/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Copyright 2009 Brian Becker <bjbdragon@gmail.com>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of 
 * the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

/// Original source at https://github.com/vgaessler/whisper_server
using System;
using System.IO;
using System.Web;
using System.Collections;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Framework.Capabilities;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Caps = OpenSim.Framework.Capabilities.Caps;
using Murmur;
using Glacier2;

namespace MurmurVoice
{
    #region Other classes

    public class MetaCallbackImpl : MetaCallbackDisp_
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public MetaCallbackImpl() { }
        public override void started(ServerPrx srv, Ice.Current current) { m_log.Info("[MurmurVoice] Server started."); }
        public override void stopped(ServerPrx srv, Ice.Current current) { m_log.Info("[MurmurVoice] Server stopped."); }
    }

    public class ServerCallbackImpl : ServerCallbackDisp_
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private ServerManager m_manager;

        public ServerCallbackImpl(ServerManager manager)
        {
            m_manager = manager;
        }

        public void AddUserToChan(User state, int channel)
        {
            if (state.channel != channel)
            {
                state.channel = channel;
                m_manager.Server.setState(state);
            }
        }

        public override void userConnected(User state, Ice.Current current)
        {
            if (state.userid < 0)
            {
                try
                {
                    m_manager.Server.kickUser(state.session, "This server requires registration to connect.");
                }
                catch (InvalidSessionException)
                {
                    m_log.DebugFormat("[MurmurVoice] Couldn't kick session {0}", state.session);
                }
                return;
            }

            try
            {
                Agent agent = m_manager.Agent.Get(state.name);
                agent.session = state.session;
                AddUserToChan(state, agent.channel);
            }
            catch (KeyNotFoundException)
            {
                m_log.DebugFormat("[MurmurVoice]: User {0} with userid {1} not registered in murmur manager, ignoring.", state.name, state.userid);
            }
        }

        public override void userDisconnected(User state, Ice.Current current)
        {
            try
            {
                m_manager.Agent.Get(state.name).session = -1;
            }
            catch (KeyNotFoundException)
            {
                m_log.DebugFormat("[MurmurVoice]: Userid {0} not handled by murmur manager", state.userid);
            }
        }

        public override void userStateChanged(User state, Ice.Current current) { }
        public override void channelCreated(Channel state, Ice.Current current) { }
        public override void channelRemoved(Channel state, Ice.Current current) { }
        public override void channelStateChanged(Channel state, Ice.Current current) { }
    }

    public class ServerManager : IDisposable
    {
        private ServerPrx m_server;
        private AgentManager m_agent_manager;
        private ChannelManager m_channel_manager;
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public AgentManager Agent
        {
            get { return m_agent_manager; }
        }

        public ChannelManager Channel
        {
            get { return m_channel_manager; }
        }

        public ServerPrx Server
        {
            get { return m_server; }
        }

        public ServerManager(ServerPrx server, string channel)
        {
            m_server = server;

            // Try to start the server
            try
            {
                m_server.start();
            }
            catch (Exception)
            {
                m_log.DebugFormat("[MurmurVoice]: Server already started.");
            }

            // Create the Agent Manager
            m_agent_manager = new AgentManager(m_server);

            // Create the Channel Manager
            m_channel_manager = new ChannelManager(m_server, channel);
        }

        public void Dispose() { }

    }

    public class ChannelManager
    {
        private Dictionary<string, int> chan_ids = new Dictionary<string, int>();
        private ServerPrx m_server;
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        int parent_chan;

        public ChannelManager(ServerPrx server, string channel)
        {
            m_server = server;

            // Update list of channels
            lock (chan_ids)
                foreach (var child in m_server.getTree().children)
                    chan_ids[child.c.name] = child.c.id;

            // Set channel if it was found, create it if it wasn't
            lock (chan_ids)
                if (chan_ids.ContainsKey(channel))
                    parent_chan = chan_ids[channel];
                else
                    parent_chan = m_server.addChannel(channel, 0);

            // Set permissions on channels
            Murmur.ACL[] acls = new Murmur.ACL[1];
            acls[0] = new Murmur.ACL(true, true, false, -1, "all",
                Murmur.PermissionSpeak.value, Murmur.PermissionEnter.value);

            m_log.InfoFormat("[MurmurVoice] Setting ACLs on channel");
            m_server.setACL(parent_chan, acls, null, true);
        }

        public int GetOrCreate(string name)
        {
            lock (chan_ids)
            {
                if (chan_ids.ContainsKey(name))
                    return chan_ids[name];
                m_log.InfoFormat("[MurmurVoice] Channel '{0}' not found. Creating.", name);
                return chan_ids[name] = m_server.addChannel(name, parent_chan);
            }
        }

        public void Remove(string name)
        {
            int channelID = 0;
            lock (chan_ids)
            {
                if (chan_ids.TryGetValue(name, out channelID))
                    chan_ids.Remove(name);
                else
                    return;
            }
            m_server.removeChannel(channelID);
        }

        public void Close()
        {
            lock (chan_ids)
            {
                foreach(int channel in chan_ids.Values)
                {
                    try
                    {
                        m_server.removeChannel(channel);
                    }
                    catch
                    {
                    }
                }
                chan_ids.Clear();
            }
        }
    }

    public class Agent
    {
        public int channel = -1;
        public int session = -1;
        public int userid = -1;
        public UUID uuid;
        public string pass;

        public Agent(UUID uuid)
        {
            this.uuid = uuid;
            this.pass = "u" + UUID.Random().ToString().Replace("-", "").Substring(0, 16);
        }

        public string name
        {
            get { return Agent.Name(uuid); }
        }

        public static string Name(UUID uuid)
        {
            return "x" + Convert.ToBase64String(uuid.GetBytes()).Replace('+', '-').Replace('/', '_');
        }

        public Dictionary<UserInfo, string> user_info
        {
            get
            {
                Dictionary<UserInfo, string> user_info = new Dictionary<UserInfo, string>();
                user_info[UserInfo.UserName] = this.name;
                user_info[UserInfo.UserPassword] = this.pass;
                return user_info;
            }
        }

    }

    public class AgentManager
    {
        private Dictionary<string, Agent> name_to_agent = new Dictionary<string, Agent>();
        private ServerPrx m_server;
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public AgentManager(ServerPrx server)
        {
            m_server = server;
        }

        public Agent GetOrCreate(UUID uuid)
        {
            string name = Agent.Name(uuid);
            lock (name_to_agent)
                if (name_to_agent.ContainsKey(name))
                    return name_to_agent[name];
                else
                {
                    Agent a = Add(uuid);
                    return a;
                }
        }

        public void RemoveAgent(UUID uuid)
        {
            string name = Agent.Name(uuid);
            Agent user = Get(name);
            if (user != null)
            {
                m_log.InfoFormat("[MurmurVoice] Removing registered user {0}", user.name);
                m_server.unregisterUser(user.userid);
                lock (name_to_agent)
                    name_to_agent.Remove(user.name);
            }
        }

        private Agent Add(UUID uuid)
        {
            Agent agent = new Agent(uuid);

            foreach (var user in m_server.getRegisteredUsers(agent.name))
                if (user.Value == agent.name)
                {
                    m_log.InfoFormat("[MurmurVoice] Found previously registered user {0}", user.Value);
                    m_server.unregisterUser(user.Key);
                    //break;
                }

            agent.userid = m_server.registerUser(agent.user_info);
            m_log.InfoFormat("[MurmurVoice] Registered {0} (uid {1}) identified by {2}", agent.uuid.ToString(), agent.userid, agent.pass);

            lock (name_to_agent)
                name_to_agent[agent.name] = agent;

            return agent;
        }

        public Agent Get(string name)
        {
            lock (name_to_agent)
            {
                if (!name_to_agent.ContainsKey(name))
                    return null;
                return name_to_agent[name];
            }
        }

    }

    public class KeepAlive
    {
        public bool running = true;
        public ServerPrx m_server;
        public KeepAlive(ServerPrx prx)
        {
            m_server = prx;
        }

        public void StartPinging()
        {
            if (running)
            {
                m_server.ice_ping();
                Thread.Sleep(30);
            }
        }
    }

    #endregion

    public class MurmurVoiceModule : ISharedRegionModule
    {
        // ICE
        private ServerCallbackImpl m_callback;
        private static KeepAlive m_keepalive;
        private static Thread m_keepalive_t;

        // Infrastructure
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // Capability strings
        private static readonly string m_parcelVoiceInfoRequestPath = "0107/";
        private static readonly string m_provisionVoiceAccountRequestPath = "0108/";
        private static readonly string m_chatSessionRequestPath = "0109/";

        // Configuration
        private IConfig m_config;
        private static string m_murmurd_host;
        private static int m_murmurd_port;
        private static ServerManager m_manager;
        private static string m_server_version = "";
        private static bool m_started = false;
        private static bool m_enabled = false;

        public void Initialise(IConfigSource config)
        {
            if (m_started)
                return;
            m_started = true;

            m_config = config.Configs["MurmurVoice"];

            if (null == m_config)
            {
                return;
            }

            if (!m_config.GetBoolean("enabled", false))
            {
                return;
            }

            try
            {
                // retrieve configuration variables
                string meta_ice = "Meta:" + m_config.GetString("murmur_ice", String.Empty);
                m_murmurd_host = m_config.GetString("murmur_host", String.Empty);
                int server_id = m_config.GetInt("murmur_sid", 1);
                m_server_version = m_config.GetString("server_version", String.Empty);

                // Admin interface required values
                if (String.IsNullOrEmpty(meta_ice) ||
                    String.IsNullOrEmpty(m_murmurd_host))
                {
                    m_log.Error("[MurmurVoice] plugin disabled: incomplete configuration");
                    return;
                }

                Ice.Communicator comm = Ice.Util.initialize();
                
                bool glacier_enabled = m_config.GetBoolean("glacier", false);

                Glacier2.RouterPrx router = null;
                if (glacier_enabled)
                {
                    router = RouterPrxHelper.uncheckedCast(comm.stringToProxy(m_config.GetString("glacier_ice", String.Empty)));
                    comm.setDefaultRouter(router);
                    router.createSession(m_config.GetString("glacier_user", "admin"), m_config.GetString("glacier_pass", "password"));
                }

                MetaPrx meta = MetaPrxHelper.checkedCast(comm.stringToProxy(meta_ice));

                // Create the adapter
                comm.getProperties().setProperty("Ice.PrintAdapterReady", "0");
                Ice.ObjectAdapter adapter;
                if (glacier_enabled)
                    adapter = comm.createObjectAdapterWithRouter("Callback.Client", comm.getDefaultRouter());
                else
                    adapter = comm.createObjectAdapterWithEndpoints("Callback.Client", m_config.GetString("murmur_ice_cb", "tcp -h 127.0.0.1"));
                adapter.activate();

                // Create identity and callback for Metaserver
                Ice.Identity metaCallbackIdent = new Ice.Identity();
                metaCallbackIdent.name = "metaCallback";
                if (router != null)
                    metaCallbackIdent.category = router.getCategoryForClient();
                MetaCallbackPrx meta_callback = MetaCallbackPrxHelper.checkedCast(adapter.add(new MetaCallbackImpl(), metaCallbackIdent));
                meta.addCallback(meta_callback);

                m_log.InfoFormat("[MurmurVoice] using murmur server ice '{0}'", meta_ice);

                // create a server and figure out the port name
                Dictionary<string, string> defaults = meta.getDefaultConf();
                ServerPrx server = ServerPrxHelper.checkedCast(meta.getServer(server_id));

                // start thread to ping glacier2 router and/or determine if con$
                m_keepalive = new KeepAlive(server);
                ThreadStart ka_d = new ThreadStart(m_keepalive.StartPinging);
                m_keepalive_t = new Thread(ka_d);
                m_keepalive_t.Start();

                // first check the conf for a port, if not then use server id and default port to find the right one.
                string conf_port = server.getConf("port");
                if (!String.IsNullOrEmpty(conf_port))
                    m_murmurd_port = Convert.ToInt32(conf_port);
                else
                    m_murmurd_port = Convert.ToInt32(defaults["port"]) + server_id - 1;

                // starts the server and gets a callback
                m_manager = new ServerManager(server, m_config.GetString("channel_name", "Channel"));

                // Create identity and callback for this current server
                m_callback = new ServerCallbackImpl(m_manager);
                Ice.Identity serverCallbackIdent = new Ice.Identity();
                serverCallbackIdent.name = "serverCallback";
                if (router != null)
                    serverCallbackIdent.category = router.getCategoryForClient();
                server.addCallback(ServerCallbackPrxHelper.checkedCast(adapter.add(m_callback, serverCallbackIdent)));

                // Show information on console for debugging purposes
                m_log.InfoFormat("[MurmurVoice] using murmur server '{0}:{1}', sid '{2}'", m_murmurd_host, m_murmurd_port, server_id);
                m_log.Info("[MurmurVoice] plugin enabled");
                m_enabled = true;
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[MurmurVoice] plugin initialization failed: {0}", e.ToString());
                return;
            }
        }

        public void AddRegion(Scene scene)
        {
            if (m_enabled)
            {
                scene.EventManager.OnNewClient += OnNewClient;
                scene.EventManager.OnClosingClient += OnClosingClient;
                scene.EventManager.OnRegisterCaps += delegate(UUID agentID, Caps caps)
                {
                    OnRegisterCaps(scene, agentID, caps);
                };
                //Add this to the OpenRegionSettings module so we can inform the client about it
                IOpenRegionSettingsModule ORSM = scene.RequestModuleInterface<IOpenRegionSettingsModule>();
                if (ORSM != null)
                    ORSM.RegisterGenericValue("Voice", "Mumble.exe");
            }
        }

        public void OnNewClient(IClientAPI client)
        {
            client.OnConnectionClosed += OnConnectionClose;
        }

        private void OnClosingClient(IClientAPI client)
        {
            client.OnConnectionClosed -= OnConnectionClose;
        }

        public void OnConnectionClose(IClientAPI client)
        {
            if (client.IsLoggingOut)
            {
                m_manager.Agent.RemoveAgent(client.AgentId);
            }
        }

        // Called to indicate that all loadable modules have now been added
        public void RegionLoaded(Scene scene)
        {
            // Do nothing.
        }

        // Called to indicate that the region is going away.
        public void RemoveRegion(Scene scene)
        {
            if (m_enabled)
            {
                m_keepalive.running = false;
                m_manager.Channel.Close();
                m_manager.Dispose();
            }
        }

        public void PostInitialise()
        {
            // Do nothing.
        }

        public void Close()
        {
            // Do nothing.
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "MurmurVoiceModule"; }
        }

        private string ChannelName(Scene scene, LandData land)
        {
            // Create parcel voice channel. If no parcel exists, then the voice channel ID is the same
            // as the directory ID. Otherwise, it reflects the parcel's ID.
            if (land.LocalID != 1 && (land.Flags & (uint)ParcelFlags.UseEstateVoiceChan) == 0)
            {
                m_log.DebugFormat("[MurmurVoice] Region:Parcel \"{0}:{1}\": parcel id {2}",
                                  scene.RegionInfo.RegionName, land.Name, land.LocalID);
                return land.GlobalID.ToString().Replace("-", "");
            }
            else
            {
                m_log.DebugFormat("[MurmurVoice] Region:Parcel \"{0}:{1}\": parcel id {2}",
                                  scene.RegionInfo.RegionName, scene.RegionInfo.RegionName, land.LocalID);
                return scene.RegionInfo.RegionID.ToString().Replace("-", "");
            }
        }

        // OnRegisterCaps is invoked via the scene.EventManager
        // everytime OpenSim hands out capabilities to a client
        // (login, region crossing). We contribute two capabilities to
        // the set of capabilities handed back to the client:
        // ProvisionVoiceAccountRequest and ParcelVoiceInfoRequest.
        // 
        // ProvisionVoiceAccountRequest allows the client to obtain
        // the voice account credentials for the avatar it is
        // controlling (e.g., user name, password, etc).
        // 
        // ParcelVoiceInfoRequest is invoked whenever the client
        // changes from one region or parcel to another.
        //
        // Note that OnRegisterCaps is called here via a closure
        // delegate containing the scene of the respective region (see
        // Initialise()).
        public void OnRegisterCaps(Scene scene, UUID agentID, Caps caps)
        {
            m_log.DebugFormat("[MurmurVoice] OnRegisterCaps: agentID {0} caps {1}", agentID, caps);

            string capsBase = "/CAPS/" + caps.CapsObjectPath;
            caps.RegisterHandler("ProvisionVoiceAccountRequest",
                                 new RestStreamHandler("POST", capsBase + m_provisionVoiceAccountRequestPath,
                                                       delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                       {
                                                           return ProvisionVoiceAccountRequest(scene, request, path, param,
                                                                                               agentID, caps);
                                                       }));
            caps.RegisterHandler("ParcelVoiceInfoRequest",
                                 new RestStreamHandler("POST", capsBase + m_parcelVoiceInfoRequestPath,
                                                       delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                       {
                                                           return ParcelVoiceInfoRequest(scene, request, path, param,
                                                                                         agentID, caps);
                                                       }));
            caps.RegisterHandler("ChatSessionRequest",
                                 new RestStreamHandler("POST", capsBase + m_chatSessionRequestPath,
                                                       delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                       {
                                                           return ChatSessionRequest(scene, request, path, param,
                                                                                     agentID, caps);
                                                       }));

            //For naali
            UUID capID = UUID.Random();
            caps.RegisterHandler("mumble_server_info", 
                                new RestStreamHandler("GET", "/CAPS/" + capID,
                                                        delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                       {
                                                           return RestGetMumbleServerInfo(scene, request, path, param, httpRequest, httpResponse);
                                                       }));
        }

        /// Callback for a client request for Voice Account Details.
        public string ProvisionVoiceAccountRequest(Scene scene, string request, string path, string param,
                                                   UUID agentID, Caps caps)
        {
            try
            {
                m_log.Info("[MurmurVoice] Calling ProvisionVoiceAccountRequest...");

                if (scene == null) throw new Exception("[MurmurVoice] Invalid scene.");

                Agent agent = m_manager.Agent.GetOrCreate(agentID);

                OSDMap response = new OSDMap();
                response["username"] = agent.name;
                response["password"] = agent.pass;
                response["voice_sip_uri_hostname"] = m_murmurd_host;
                response["voice_account_server_name"] = String.Format("tcp://{0}:{1}", m_murmurd_host, m_murmurd_port);

                string r = OSDParser.SerializeLLSDXmlString(response);
                m_log.InfoFormat("[MurmurVoice] VoiceAccount: {0}", r);
                return r;
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[MurmurVoice] {0} failed", e.ToString());
                return "<llsd><undef /></llsd>";
            }
        }

        /// <summary>
        /// Returns information about a mumble server via a REST Request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="path"></param>
        /// <param name="param">A string representing the sim's UUID</param>
        /// <param name="httpRequest">HTTP request header object</param>
        /// <param name="httpResponse">HTTP response header object</param>
        /// <returns>Information about the mumble server in http response headers</returns>
        public string RestGetMumbleServerInfo(Scene scene, string request, string path, string param,
                                       OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            if (m_murmurd_host == null)
            {
                httpResponse.StatusCode = 404;
                httpResponse.StatusDescription = "Not Found";

                string message = "[MUMBLE VOIP]: Server info request from " + httpRequest.RemoteIPEndPoint.Address + ". Cannot send response, module is not configured properly.";
                m_log.Warn(message);
                return "Mumble server info is not available.";
            }
            if (httpRequest.Headers.GetValues("avatar_uuid") == null)
            {
                httpResponse.StatusCode = 400;
                httpResponse.StatusDescription = "Bad Request";

                string message = "[MUMBLE VOIP]: Invalid server info request from " + httpRequest.RemoteIPEndPoint.Address + "";
                m_log.Warn(message);
                return "avatar_uuid header is missing";
            }
                
            string avatar_uuid = httpRequest.Headers.GetValues("avatar_uuid")[0];
            string responseBody = String.Empty;
            UUID avatarId;
            if (UUID.TryParse(avatar_uuid, out avatarId))
            {
                if (scene == null) throw new Exception("[MurmurVoice] Invalid scene.");

                Agent agent = m_manager.Agent.GetOrCreate(avatarId);

                string channel_uri;

                ScenePresence avatar = scene.GetScenePresence(avatarId);
                
                // get channel_uri: check first whether estate
                // settings allow voice, then whether parcel allows
                // voice, if all do retrieve or obtain the parcel
                // voice channel
                LandData land = scene.LandChannel.GetLandObject(avatar.AbsolutePosition.X, avatar.AbsolutePosition.Y).LandData;

                m_log.DebugFormat("[MurmurVoice] region \"{0}\": Parcel \"{1}\" ({2}): avatar \"{3}\": request: {4}, path: {5}, param: {6}",
                                  scene.RegionInfo.RegionName, land.Name, land.LocalID, avatar.Name, request, path, param);

                if (((land.Flags & (uint)ParcelFlags.AllowVoiceChat) > 0) && scene.RegionInfo.EstateSettings.AllowVoice)
                {
                    agent.channel = m_manager.Channel.GetOrCreate(ChannelName(scene, land));

                    // Host/port pair for voice server
                    channel_uri = String.Format("{0}:{1}", m_murmurd_host, m_murmurd_port);

                    if (agent.session > 0)
                    {
                        Murmur.User state = m_manager.Server.getState(agent.session);
                        m_callback.AddUserToChan(state, agent.channel);
                    }

                    m_log.InfoFormat("[MurmurVoice] {0}", channel_uri);
                }
                else
                {
                    m_log.DebugFormat("[MurmurVoice] Voice not enabled.");
                    channel_uri = "";
                }
                string m_context = "Mumble voice system";

                httpResponse.AddHeader("Mumble-Server", m_murmurd_host);
                httpResponse.AddHeader("Mumble-Version", m_server_version);
                httpResponse.AddHeader("Mumble-Channel", channel_uri);
                httpResponse.AddHeader("Mumble-User", avatar_uuid);
                httpResponse.AddHeader("Mumble-Password", agent.pass);
                httpResponse.AddHeader("Mumble-Avatar-Id", avatar_uuid);
                httpResponse.AddHeader("Mumble-Context-Id", m_context);

                responseBody += "Mumble-Server: " + m_murmurd_host + "\n";
                responseBody += "Mumble-Version: " + m_server_version + "\n";
                responseBody += "Mumble-Channel: " + channel_uri + "\n";
                responseBody += "Mumble-User: " + avatar_uuid + "\n";
                responseBody += "Mumble-Password: " + agent.pass + "\n";
                responseBody += "Mumble-Avatar-Id: " + avatar_uuid + "\n";
                responseBody += "Mumble-Context-Id: " + m_context + "\n";

                string log_message = "[MUMBLE VOIP]: Server info request handled for " + httpRequest.RemoteIPEndPoint.Address + "";
                m_log.Info(log_message);
            }
            else
            {
                httpResponse.StatusCode = 400;
                httpResponse.StatusDescription = "Bad Request";

                m_log.Warn("[MUMBLE VOIP]: Could not parse avatar uuid from request");
                return "could not parse avatar_uuid header";
            }

            return responseBody;
        }

        /// Callback for a client request for ParcelVoiceInfo
        public string ParcelVoiceInfoRequest(Scene scene, string request, string path, string param,
                                             UUID agentID, Caps caps)
        {
            m_log.Debug("[MurmurVoice] Calling ParcelVoiceInfoRequest...");
            try
            {
                ScenePresence avatar = scene.GetScenePresence(agentID);

                string channel_uri = String.Empty;

                if (null == scene.LandChannel)
                    throw new Exception(String.Format("region \"{0}\": avatar \"{1}\": land data not yet available",
                                                      scene.RegionInfo.RegionName, avatar.Name));

                // get channel_uri: check first whether estate
                // settings allow voice, then whether parcel allows
                // voice, if all do retrieve or obtain the parcel
                // voice channel
                LandData land = scene.LandChannel.GetLandObject(avatar.AbsolutePosition.X, avatar.AbsolutePosition.Y).LandData;

                m_log.DebugFormat("[MurmurVoice] region \"{0}\": Parcel \"{1}\" ({2}): avatar \"{3}\": request: {4}, path: {5}, param: {6}",
                                  scene.RegionInfo.RegionName, land.Name, land.LocalID, avatar.Name, request, path, param);

                if (((land.Flags & (uint)ParcelFlags.AllowVoiceChat) > 0) && scene.RegionInfo.EstateSettings.AllowVoice)
                {
                    Agent agent = m_manager.Agent.GetOrCreate(agentID);
                    agent.channel = m_manager.Channel.GetOrCreate(ChannelName(scene, land));

                    // Host/port pair for voice server
                    channel_uri = String.Format("{0}:{1}", m_murmurd_host, m_murmurd_port);

                    if (agent.session > 0)
                    {
                        Murmur.User state = m_manager.Server.getState(agent.session);
                        m_callback.AddUserToChan(state, agent.channel);
                    }

                    m_log.InfoFormat("[MurmurVoice] {0}", channel_uri);
                }
                else
                {
                    m_log.DebugFormat("[MurmurVoice] Voice not enabled.");
                }

                OSDMap response = new OSDMap();
                response["region_name"] = scene.RegionInfo.RegionName;
                response["parcel_local_id"] = land.LocalID;
                response["voice_credentials"] = new OSDMap();
                ((OSDMap)response["voice_credentials"])["channel_uri"] = channel_uri;
                string r = OSDParser.SerializeLLSDXmlString(response);
                m_log.InfoFormat("[MurmurVoice] Parcel: {0}", r);

                return r;
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[MurmurVoice] Exception: " + e.ToString());
                return "<llsd><undef /></llsd>";
            }
        }

        /// Callback for a client request for a private chat channel
        public string ChatSessionRequest(Scene scene, string request, string path, string param,
                                         UUID agentID, Caps caps)
        {
            ScenePresence avatar = scene.GetScenePresence(agentID);
            string avatarName = avatar.Name;

            m_log.DebugFormat("[MurmurVoice] Chat Session: avatar \"{0}\": request: {1}, path: {2}, param: {3}",
                              avatarName, request, path, param);
            return "<llsd>true</llsd>";
        }
    }
}
