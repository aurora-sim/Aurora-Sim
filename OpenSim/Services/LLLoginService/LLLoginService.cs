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
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

using log4net;
using Nini.Config;
using OpenMetaverse;

using OpenSim.Framework;
using OpenSim.Framework.Capabilities;
using OpenSim.Framework.Console;
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using FriendInfo = OpenSim.Services.Interfaces.FriendInfo;
using OpenSim.Services.Connectors.Hypergrid;
using Aurora.DataManager;
using Aurora.Framework;

namespace OpenSim.Services.LLLoginService
{
    public class LLLoginService : ILoginService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static bool Initialized = false;

        protected IUserAccountService m_UserAccountService;
        protected IAuthenticationService m_AuthenticationService;
        protected IInventoryService m_InventoryService;
        protected IGridService m_GridService;
        protected IPresenceService m_PresenceService;
        private ISimulationService m_LocalSimulationService;
        private ISimulationService m_RemoteSimulationService;
        protected ILibraryService m_LibraryService;
        protected IFriendsService m_FriendsService;
        protected IAvatarService m_AvatarService;
        private IUserAgentService m_UserAgentService;

        private GatekeeperServiceConnector m_GatekeeperConnector;

        private string m_DefaultRegionName;
        protected string m_WelcomeMessage;
        private bool m_RequireInventory;
        protected int m_MinLoginLevel;
        private string m_GatekeeperURL;

        IConfig m_LoginServerConfig;
        IConfigSource m_config;
        IConfig m_AuroraLoginConfig;
        bool AllowAnonymousLogin;
        bool AuthenticateUsers;

        public LLLoginService(IConfigSource config, ISimulationService simService, ILibraryService libraryService)
        {
            m_config = config;
            m_AuroraLoginConfig = config.Configs["AuroraLoginService"];
            AllowAnonymousLogin = m_AuroraLoginConfig.GetBoolean("AllowAnonymousLogin");
            AuthenticateUsers = m_AuroraLoginConfig.GetBoolean("AuthenticateUsers");
            m_LoginServerConfig = config.Configs["LoginService"];
            if (m_LoginServerConfig == null)
                throw new Exception(String.Format("No section LoginService in config file"));

            string accountService = m_LoginServerConfig.GetString("UserAccountService", String.Empty);
            string agentService = m_LoginServerConfig.GetString("UserAgentService", String.Empty);
            string authService = m_LoginServerConfig.GetString("AuthenticationService", String.Empty);
            string invService = m_LoginServerConfig.GetString("InventoryService", String.Empty);
            string gridService = m_LoginServerConfig.GetString("GridService", String.Empty);
            string presenceService = m_LoginServerConfig.GetString("PresenceService", String.Empty);
            string libService = m_LoginServerConfig.GetString("LibraryService", String.Empty);
            string friendsService = m_LoginServerConfig.GetString("FriendsService", String.Empty);
            string avatarService = m_LoginServerConfig.GetString("AvatarService", String.Empty);
            string simulationService = m_LoginServerConfig.GetString("SimulationService", String.Empty);

            m_DefaultRegionName = m_LoginServerConfig.GetString("DefaultRegion", String.Empty);
            m_WelcomeMessage = m_LoginServerConfig.GetString("WelcomeMessage", "Welcome to OpenSim!");
            m_RequireInventory = m_LoginServerConfig.GetBoolean("RequireInventory", true);
            m_GatekeeperURL = m_LoginServerConfig.GetString("GatekeeperURI", string.Empty);

            // These are required; the others aren't
            if (accountService == string.Empty || authService == string.Empty)
                throw new Exception("LoginService is missing service specifications");

            Object[] args = new Object[] { config };
            m_UserAccountService = ServerUtils.LoadPlugin<IUserAccountService>(accountService, args);
            m_AuthenticationService = ServerUtils.LoadPlugin<IAuthenticationService>(authService, args);
            m_InventoryService = ServerUtils.LoadPlugin<IInventoryService>(invService, args);
            if (gridService != string.Empty)
                m_GridService = ServerUtils.LoadPlugin<IGridService>(gridService, args);
            if (presenceService != string.Empty)
                m_PresenceService = ServerUtils.LoadPlugin<IPresenceService>(presenceService, args);
            if (avatarService != string.Empty)
                m_AvatarService = ServerUtils.LoadPlugin<IAvatarService>(avatarService, args);
            if (friendsService != string.Empty)
                m_FriendsService = ServerUtils.LoadPlugin<IFriendsService>(friendsService, args);
            if (simulationService != string.Empty)
                m_RemoteSimulationService = ServerUtils.LoadPlugin<ISimulationService>(simulationService, args);
            if (agentService != string.Empty)
                m_UserAgentService = ServerUtils.LoadPlugin<IUserAgentService>(agentService, args);

            //
            // deal with the services given as argument
            //
            m_LocalSimulationService = simService;
            if (libraryService != null)
            {
                m_log.DebugFormat("[LLOGIN SERVICE]: Using LibraryService given as argument");
                m_LibraryService = libraryService;
            }
            else if (libService != string.Empty)
            {
                m_log.DebugFormat("[LLOGIN SERVICE]: Using instantiated LibraryService");
                m_LibraryService = ServerUtils.LoadPlugin<ILibraryService>(libService, args);
            }

            m_GatekeeperConnector = new GatekeeperServiceConnector();

            if (!Initialized)
            {
                Initialized = true;
                RegisterCommands();
            }

            m_log.DebugFormat("[LLOGIN SERVICE]: Starting...");

        }

        public LLLoginService(IConfigSource config) : this(config, null, null)
        {
        }

        public LoginResponse Login(string firstName, string lastName, string passwd, string startLocation, IPEndPoint clientIP)
        {
            bool success = false;
            UUID session = UUID.Random();

            try
            {
                //
                // Get the account and check that it exists
                //
                UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, firstName, lastName);
                if (account == null)
                {
                    if (!AllowAnonymousLogin)
                    {
                        m_log.InfoFormat("[LLOGIN SERVICE]: Login failed, reason: user not found");
                        return LLFailedLoginResponse.UserProblem;
                    }
                    else
                    {
                        account = new UserAccount();
                        account.Created = (int)DateTime.Now.ToFileTime();
                        account.Email = "";
                        account.FirstName = firstName;
                        account.LastName = lastName;
                        account.PrincipalID = new UUID(new Guid().ToString());
                        account.ScopeID = UUID.Zero;
                        account.ServiceURLs = new Dictionary<string,object>();
                        account.UserFlags = 0;
                        account.UserLevel = 0;
                        account.UserTitle = "";
                        m_UserAccountService.StoreUserAccount(account);
                        m_AuthenticationService.SetPasswordHashed(account.PrincipalID, passwd);
                        m_InventoryService.CreateUserInventory(account.PrincipalID);
                    }
                }

                IProfileData data = Aurora.DataManager.DataManager.GetDefaultProfilePlugin();
                AuroraProfileData profile = data.GetProfileInfo(account.PrincipalID);
                if (profile == null)
                {
                    CreateUserAuth(account.PrincipalID.ToString(), account.FirstName, account.LastName);
                    profile = data.GetProfileInfo(account.PrincipalID);
                }
                if(profile.PermaBanned == 1 || profile.TempBanned == 1)
                {
                    m_log.Info("[LLOGIN SERVICE]: Login failed, reason: user is banned.");
                    return LLFailedLoginResponse.UserProblem;
                }

                if (account.UserLevel < m_MinLoginLevel)
                {
                    m_log.InfoFormat("[LLOGIN SERVICE]: Login failed, reason: login is blocked for user level {0}", account.UserLevel);
                    return LLFailedLoginResponse.LoginBlockedProblem;
                }
                UUID secureSession = UUID.Zero;
                if (AuthenticateUsers)
                {
                    //
                    // Authenticate this user
                    //
                    if (!passwd.StartsWith("$1$"))
                        passwd = "$1$" + Util.Md5Hash(passwd);
                    passwd = passwd.Remove(0, 3); //remove $1$
                    string token = m_AuthenticationService.Authenticate(account.PrincipalID, passwd, 30);
                    if ((token == string.Empty) || (token != string.Empty && !UUID.TryParse(token, out secureSession)))
                    {
                        m_log.InfoFormat("[LLOGIN SERVICE]: Login failed, reason: authentication failed");
                        return LLFailedLoginResponse.UserProblem;
                    }
                }
                else
                {
                    string token = m_AuthenticationService.GetToken(account.PrincipalID, 30);
                    UUID.TryParse(token, out secureSession);
                }

                //
                // Get the user's inventory
                //
                if (m_RequireInventory && m_InventoryService == null)
                {
                    m_log.WarnFormat("[LLOGIN SERVICE]: Login failed, reason: inventory service not set up");
                    return LLFailedLoginResponse.InventoryProblem;
                }
                List<InventoryFolderBase> inventorySkel = m_InventoryService.GetInventorySkeleton(account.PrincipalID);
                if (m_RequireInventory && ((inventorySkel == null) || (inventorySkel != null && inventorySkel.Count == 0)))
                {
                    m_log.InfoFormat("[LLOGIN SERVICE]: Login failed, reason: unable to retrieve user inventory");
                    return LLFailedLoginResponse.InventoryProblem;
                }

                //
                // Login the presence
                //
                PresenceInfo presence = null;
                GridRegion home = null;
                if (m_PresenceService != null)
                {
                    success = m_PresenceService.LoginAgent(account.PrincipalID.ToString(), session, secureSession);
                    if (!success)
                    {
                        m_log.InfoFormat("[LLOGIN SERVICE]: Login failed, reason: could not login presence");
                        return LLFailedLoginResponse.GridProblem;
                    }

                    // Get the updated presence info
                    presence = m_PresenceService.GetAgent(session);

                    // Get the home region
                    if ((presence.HomeRegionID != UUID.Zero) && m_GridService != null)
                    {
                        home = m_GridService.GetRegionByUUID(account.ScopeID, presence.HomeRegionID);
                    }
                }

                //
                // Find the destination region/grid
                //
                string where = string.Empty;
                Vector3 position = Vector3.Zero;
                Vector3 lookAt = Vector3.Zero;
                GridRegion gatekeeper = null;
                GridRegion destination = FindDestination(account, presence, session, startLocation, out gatekeeper, out where, out position, out lookAt);
                if (destination == null)
                {
                    m_PresenceService.LogoutAgent(session, presence.Position, presence.LookAt);
                    m_log.InfoFormat("[LLOGIN SERVICE]: Login failed, reason: destination not found");
                    return LLFailedLoginResponse.GridProblem;
                }

                //
                // Get the avatar
                //
                AvatarData avatar = null;
                if (m_AvatarService != null)
                {
                    avatar = m_AvatarService.GetAvatar(account.PrincipalID);
                }

                //
                // Instantiate/get the simulation interface and launch an agent at the destination
                //
                string reason = string.Empty;
                AgentCircuitData aCircuit = LaunchAgentAtGrid(gatekeeper, destination, account, avatar, session, secureSession, position, where, out where, out reason);

                if (aCircuit == null)
                {
                    m_PresenceService.LogoutAgent(session, presence.Position, presence.LookAt);
                    m_log.InfoFormat("[LLOGIN SERVICE]: Login failed, reason: {0}", reason);
                    return LLFailedLoginResponse.AuthorizationProblem;

                }
                // Get Friends list 
                FriendInfo[] friendsList = new FriendInfo[0];
                if (m_FriendsService != null)
                {
                    friendsList = m_FriendsService.GetFriends(account.PrincipalID);
                    m_log.DebugFormat("[LLOGIN SERVICE]: Retrieved {0} friends", friendsList.Length);
                }

                //
                // Finally, fill out the response and return it
                //
                string adult = "A";
                if(profile.Mature == 0)
                    adult = "P";
                if(profile.Mature == 1)
                    adult = "M";
                if(profile.Mature == 2)
                    adult = "A";
                LLLoginResponse response = new LLLoginResponse(account, aCircuit, presence, destination, inventorySkel, friendsList, m_LibraryService,
                    where, startLocation, position, lookAt, m_WelcomeMessage, home, clientIP, "A", adult);

                m_log.DebugFormat("[LLOGIN SERVICE]: All clear. Sending login response to client.");
                return response;
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[LLOGIN SERVICE]: Exception processing login for {0} {1}: {2} {3}", firstName, lastName, e.ToString(), e.StackTrace);
                if (m_PresenceService != null)
                    m_PresenceService.LogoutAgent(session, new Vector3(128, 128, 0), new Vector3(0, 1, 0));
                return LLFailedLoginResponse.InternalError;
            }
        }

        protected GridRegion FindDestination(UserAccount account, PresenceInfo pinfo, UUID sessionID, string startLocation, out GridRegion gatekeeper, out string where, out Vector3 position, out Vector3 lookAt)
        {
            m_log.DebugFormat("[LLOGIN SERVICE]: FindDestination for start location {0}", startLocation);

            gatekeeper = null;
            where = "home";
            position = new Vector3(128, 128, 0);
            lookAt = new Vector3(0, 1, 0);

            if (m_GridService == null)
                return null;

            if (startLocation.Equals("home"))
            {
                // logging into home region
                if (pinfo == null)
                    return null;

                GridRegion region = null;

                bool tryDefaults = false;

                if (pinfo.HomeRegionID.Equals(UUID.Zero))
                {
                    m_log.WarnFormat(
                        "[LLOGIN SERVICE]: User {0} {1} tried to login to a 'home' start location but they have none set",
                        account.FirstName, account.LastName);
                    
                    tryDefaults = true;
                }
                else
                {
                    region = m_GridService.GetRegionByUUID(account.ScopeID, pinfo.HomeRegionID);

                    if (null == region)
                    {
                        m_log.WarnFormat(
                            "[LLOGIN SERVICE]: User {0} {1} has a recorded home region of {2} but this cannot be found by the grid service", 
                            account.FirstName, account.LastName, pinfo.HomeRegionID);
                        
                        tryDefaults = true;
                    }
                }
                
                if (tryDefaults)
                {
                    List<GridRegion> defaults = m_GridService.GetDefaultRegions(account.ScopeID);
                    if (defaults != null && defaults.Count > 0)
                    {
                        region = defaults[0];
                        where = "safe";
                    }
                    else
                    {
                        m_log.WarnFormat("[LLOGIN SERVICE]: User {0} {1} does not have a valid home and this grid does not have default locations. Attempting to find random region",
                            account.FirstName, account.LastName);
                        defaults = m_GridService.GetRegionsByName(account.ScopeID, "", 1);
                        if (defaults != null && defaults.Count > 0)
                        {
                            region = defaults[0];
                            where = "safe";
                        }
                    }
                }

                return region;
            }
            else if (startLocation.Equals("last"))
            {
                // logging into last visited region
                where = "last";

                if (pinfo == null)
                    return null;

                GridRegion region = null;

                if (pinfo.RegionID.Equals(UUID.Zero) || (region = m_GridService.GetRegionByUUID(account.ScopeID, pinfo.RegionID)) == null)
                {
                    List<GridRegion> defaults = m_GridService.GetDefaultRegions(account.ScopeID);
                    if (defaults != null && defaults.Count > 0)
                    {
                        region = defaults[0];
                        where = "safe";
                    }
                    else
                    {
                        m_log.Info("[LLOGIN SERVICE]: Last Region Not Found Attempting to find random region");
                        defaults = m_GridService.GetRegionsByName(account.ScopeID, "", 1);
                        if (defaults != null && defaults.Count > 0)
                        {
                            region = defaults[0];
                            where = "safe";
                        }
                    }

                }
                else
                {
                    position = pinfo.Position;
                    lookAt = pinfo.LookAt;
                }
                
                return region;
            }
            else
            {
                // free uri form
                // e.g. New Moon&135&46  New Moon@osgrid.org:8002&153&34
                where = "url";
                Regex reURI = new Regex(@"^uri:(?<region>[^&]+)&(?<x>\d+)&(?<y>\d+)&(?<z>\d+)$");
                Match uriMatch = reURI.Match(startLocation);
                if (uriMatch == null)
                {
                    m_log.InfoFormat("[LLLOGIN SERVICE]: Got Custom Login URI {0}, but can't process it", startLocation);
                    return null;
                }
                else
                {
                    position = new Vector3(float.Parse(uriMatch.Groups["x"].Value),
                                           float.Parse(uriMatch.Groups["y"].Value),
                                           float.Parse(uriMatch.Groups["z"].Value));

                    string regionName = uriMatch.Groups["region"].ToString();
                    if (regionName != null)
                    {
                        if (!regionName.Contains("@"))
                        {
                            List<GridRegion> regions = m_GridService.GetRegionsByName(account.ScopeID, regionName, 1);
                            if ((regions == null) || (regions != null && regions.Count == 0))
                            {
                                m_log.InfoFormat("[LLLOGIN SERVICE]: Got Custom Login URI {0}, can't locate region {1}. Trying defaults.", startLocation, regionName);
                                regions = m_GridService.GetDefaultRegions(UUID.Zero);
                                if (regions != null && regions.Count > 0)
                                {
                                    where = "safe"; 
                                    return regions[0];
                                }
                                else
                                {
                                    m_log.InfoFormat("[LLLOGIN SERVICE]: Got Custom Login URI {0}, Grid does not provide default regions.", startLocation);
                                    return null;
                                }
                            }
                            return regions[0];
                        }
                        else
                        {
                            if (m_UserAgentService == null)
                            {
                                m_log.WarnFormat("[LLLOGIN SERVICE]: This llogin service is not running a user agent service, as such it can't lauch agents at foreign grids");
                                return null;
                            }
                            string[] parts = regionName.Split(new char[] { '@' });
                            if (parts.Length < 2)
                            {
                                m_log.InfoFormat("[LLLOGIN SERVICE]: Got Custom Login URI {0}, can't locate region {1}", startLocation, regionName);
                                return null;
                            }
                            // Valid specification of a remote grid
                            
                            regionName = parts[0];
                            string domainLocator = parts[1];
                            parts = domainLocator.Split(new char[] {':'});
                            string domainName = parts[0];
                            uint port = 0;
                            if (parts.Length > 1)
                                UInt32.TryParse(parts[1], out port);

                            GridRegion region = FindForeignRegion(domainName, port, regionName, out gatekeeper);
                            return region;
                        }
                    }
                    else
                    {
                        List<GridRegion> defaults = m_GridService.GetDefaultRegions(account.ScopeID);
                        if (defaults != null && defaults.Count > 0)
                        {
                            where = "safe"; 
                            return defaults[0];
                        }
                        else
                            return null;
                    }
                }
                //response.LookAt = "[r0,r1,r0]";
                //// can be: last, home, safe, url
                //response.StartLocation = "url";

            }

        }

        private GridRegion FindForeignRegion(string domainName, uint port, string regionName, out GridRegion gatekeeper)
        {
            gatekeeper = new GridRegion();
            gatekeeper.ExternalHostName = domainName;
            gatekeeper.HttpPort = port;
            gatekeeper.RegionName = regionName;
            gatekeeper.InternalEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 0);

            UUID regionID;
            ulong handle;
            string imageURL = string.Empty, reason = string.Empty;
            if (m_GatekeeperConnector.LinkRegion(gatekeeper, out regionID, out handle, out domainName, out imageURL, out reason))
            {
                GridRegion destination = m_GatekeeperConnector.GetHyperlinkRegion(gatekeeper, regionID);
                return destination;
            }

            return null;
        }

        private string hostName = string.Empty;
        private int port = 0;

        private void SetHostAndPort(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                hostName = uri.Host;
                port = uri.Port;
            }
            catch
            {
                m_log.WarnFormat("[LLLogin SERVICE]: Unable to parse GatekeeperURL {0}", url);
            }
        }

        protected AgentCircuitData LaunchAgentAtGrid(GridRegion gatekeeper, GridRegion destination, UserAccount account, AvatarData avatar,
            UUID session, UUID secureSession, Vector3 position, string currentWhere, out string where, out string reason)
        {
            where = currentWhere;
            ISimulationService simConnector = null;
            reason = string.Empty;
            uint circuitCode = 0;
            AgentCircuitData aCircuit = null;

            if (m_UserAgentService == null)
            {
                // HG standalones have both a localSimulatonDll and a remoteSimulationDll
                // non-HG standalones have just a localSimulationDll
                // independent login servers have just a remoteSimulationDll
                if (m_LocalSimulationService != null)
                    simConnector = m_LocalSimulationService;
                else if (m_RemoteSimulationService != null)
                    simConnector = m_RemoteSimulationService;
            }
            else // User Agent Service is on
            {
                if (gatekeeper == null) // login to local grid
                {
                    if (hostName == string.Empty)
                        SetHostAndPort(m_GatekeeperURL);

                    gatekeeper = new GridRegion(destination);
                    gatekeeper.ExternalHostName = hostName;
                    gatekeeper.HttpPort = (uint)port;

                }
                else // login to foreign grid
                {
                }
            }

            bool success = false;

            if (m_UserAgentService == null && simConnector != null)
            {
                circuitCode = (uint)Util.RandomClass.Next(); ;
                aCircuit = MakeAgent(destination, account, avatar, session, secureSession, circuitCode, position);
                success = LaunchAgentDirectly(simConnector, destination, aCircuit, out reason);
                if (!success && m_GridService != null)
                {
                    // Try the fallback regions
                    List<GridRegion> fallbacks = m_GridService.GetFallbackRegions(account.ScopeID, destination.RegionLocX, destination.RegionLocY);
                    if (fallbacks != null)
                    {
                        foreach (GridRegion r in fallbacks)
                        {
                            success = LaunchAgentDirectly(simConnector, r, aCircuit, out reason);
                            if (success)
                            {
                                where = "safe";
                                destination = r;
                                break;
                            }
                        }
                    }
                }
            }

            if (m_UserAgentService != null)
            {
                circuitCode = (uint)Util.RandomClass.Next(); ;
                aCircuit = MakeAgent(destination, account, avatar, session, secureSession, circuitCode, position);
                success = LaunchAgentIndirectly(gatekeeper, destination, aCircuit, out reason);
                if (!success && m_GridService != null)
                {
                    // Try the fallback regions
                    List<GridRegion> fallbacks = m_GridService.GetFallbackRegions(account.ScopeID, destination.RegionLocX, destination.RegionLocY);
                    if (fallbacks != null)
                    {
                        foreach (GridRegion r in fallbacks)
                        {
                            success = LaunchAgentIndirectly(gatekeeper, r, aCircuit, out reason);
                            if (success)
                            {
                                where = "safe";
                                destination = r;
                                break;
                            }
                        }
                    }
                }
            }

            if (success)
                return aCircuit;
            else
                return null;
        }

        private AgentCircuitData MakeAgent(GridRegion region, UserAccount account, 
            AvatarData avatar, UUID session, UUID secureSession, uint circuit, Vector3 position)
        {
            AgentCircuitData aCircuit = new AgentCircuitData();

            aCircuit.AgentID = account.PrincipalID;
            if (avatar != null)
                aCircuit.Appearance = avatar.ToAvatarAppearance(account.PrincipalID);
            else
                aCircuit.Appearance = new AvatarAppearance(account.PrincipalID);

            //aCircuit.BaseFolder = irrelevant
            aCircuit.CapsPath = CapsUtil.GetRandomCapsObjectPath();
            aCircuit.child = false; // the first login agent is root
            aCircuit.ChildrenCapSeeds = new Dictionary<ulong, string>();
            aCircuit.circuitcode = circuit;
            aCircuit.firstname = account.FirstName;
            //aCircuit.InventoryFolder = irrelevant
            aCircuit.lastname = account.LastName;
            aCircuit.SecureSessionID = secureSession;
            aCircuit.SessionID = session;
            aCircuit.startpos = position;
            SetServiceURLs(aCircuit, account);

            return aCircuit;

            //m_UserAgentService.LoginAgentToGrid(aCircuit, GatekeeperServiceConnector, region, out reason);
            //if (simConnector.CreateAgent(region, aCircuit, 0, out reason))
            //    return aCircuit;

            //return null;

        }

        private void SetServiceURLs(AgentCircuitData aCircuit, UserAccount account)
        {
            aCircuit.ServiceURLs = new Dictionary<string, object>();
            if (account.ServiceURLs == null)
                return;

            foreach (KeyValuePair<string, object> kvp in account.ServiceURLs)
            {
                if (kvp.Value == null || (kvp.Value != null && kvp.Value.ToString() == string.Empty))
                {
                    aCircuit.ServiceURLs[kvp.Key] = m_LoginServerConfig.GetString(kvp.Key, string.Empty);
                }
                else
                {
                    aCircuit.ServiceURLs[kvp.Key] = kvp.Value;
                }
            }
        }

        private bool LaunchAgentDirectly(ISimulationService simConnector, GridRegion region, AgentCircuitData aCircuit, out string reason)
        {
            return simConnector.CreateAgent(region, aCircuit, (int)Constants.TeleportFlags.ViaLogin, out reason);
        }

        private bool LaunchAgentIndirectly(GridRegion gatekeeper, GridRegion destination, AgentCircuitData aCircuit, out string reason)
        {
            m_log.Debug("[LLOGIN SERVICE] Launching agent at " + destination.RegionName);
            return m_UserAgentService.LoginAgentToGrid(aCircuit, gatekeeper, destination, out reason);
        }

        #region Console Commands
        private void RegisterCommands()
        {
            //MainConsole.Instance.Commands.AddCommand
            MainConsole.Instance.Commands.AddCommand("loginservice", false, "login level",
                    "login level <level>",
                    "Set the minimum user level to log in", HandleLoginCommand);

            MainConsole.Instance.Commands.AddCommand("loginservice", false, "login reset",
                    "login reset",
                    "Reset the login level to allow all users",
                    HandleLoginCommand);

            MainConsole.Instance.Commands.AddCommand("loginservice", false, "login text",
                    "login text <text>",
                    "Set the text users will see on login", HandleLoginCommand);

        }

        private void HandleLoginCommand(string module, string[] cmd)
        {
            string subcommand = cmd[1];

            switch (subcommand)
            {
                case "level":
                    // Set the minimum level to allow login 
                    // Useful to allow grid update without worrying about users.
                    // or fixing critical issues
                    //
                    if (cmd.Length > 2)
                        Int32.TryParse(cmd[2], out m_MinLoginLevel);
                    break;
                case "reset":
                    m_MinLoginLevel = 0;
                    break;
                case "text":
                    if (cmd.Length > 2)
                        m_WelcomeMessage = cmd[2];
                    break;
            }
        }


        public void CreateUserAuth(string UUID, string firstName, string lastName)
        {
            List<string> values = new List<string>();
            values.Add(UUID);
            values.Add(firstName + " " + lastName);
            values.Add(firstName);
            values.Add(lastName);
            values.Add(" ");
            values.Add(" ");
            values.Add("0");
            values.Add(" ");
            values.Add(" ");
            values.Add("0");
            values.Add("0");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add("0");
            values.Add("0");
            values.Add("1");
            values.Add("0");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add("0");
            values.Add(" ");
            values.Add("0");
            values.Add(" ");
            values.Add(" ");
            values.Add("0");
            values.Add("1");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add(" ");
            values.Add("true");
            values.Add("false");
            values.Add("2");
            values.Add("1");
            values.Add("en");
            var GD = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
            GD.Insert("usersauth", values.ToArray());
        }


    }

    #endregion
}
