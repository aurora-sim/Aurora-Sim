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
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Framework.Capabilities;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using FriendInfo = OpenSim.Services.Interfaces.FriendInfo;
using Aurora.DataManager;

namespace OpenSim.Services.LLLoginService
{
    public class LLLoginService : ILoginService, IService
    {
        private static bool Initialized = false;
        // Global Textures
        private const string sunTexture = "cce0f112-878f-4586-a2e2-a8f104bba271";
        private const string cloudTexture = "dc4b9f0b-d008-45c6-96a4-01dd947ac621";
        private const string moonTexture = "ec4b9f0b-d008-45c6-96a4-01dd947ac621";

        protected IUserAccountService m_UserAccountService;
        protected IAgentInfoService m_agentInfoService;
        protected IAuthenticationService m_AuthenticationService;
        protected IInventoryService m_InventoryService;
        protected IGridService m_GridService;
        protected ISimulationService m_SimulationService;
        protected ILibraryService m_LibraryService;
        protected IFriendsService m_FriendsService;
        protected IAvatarService m_AvatarService;
        protected IAssetService m_AssetService;
        protected ICapsService m_CapsService;
        protected IAvatarAppearanceArchiver m_ArchiveService;
        protected IRegistryCore m_registry;

        protected string m_DefaultRegionName;
        protected string m_WelcomeMessage;
        protected string m_WelcomeMessageURL;
        protected bool m_RequireInventory;
        protected int m_MinLoginLevel;
        protected bool m_AllowRemoteSetLoginLevel;

        protected IConfig m_loginServerConfig;
        protected IConfigSource m_config;
        protected bool m_AllowAnonymousLogin = false;
        protected bool m_AllowDuplicateLogin = false;
        protected bool m_UseTOS = false;
        protected string m_TOSLocation = "";
        protected string m_DefaultUserAvatarArchive = "DefaultAvatar.aa";
        protected string m_DefaultHomeRegion = "";
        protected ArrayList eventCategories = new ArrayList();
        protected ArrayList classifiedCategories = new ArrayList();
        protected List<ILoginModule> LoginModules = new List<ILoginModule>();

        public int MinLoginLevel
        {
            get { return m_MinLoginLevel; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_config = config;
            m_loginServerConfig = config.Configs["LoginService"];
            if (m_loginServerConfig == null)
                return;

            m_UseTOS = m_loginServerConfig.GetBoolean("UseTermsOfServiceOnFirstLogin", false);
            m_DefaultHomeRegion = m_loginServerConfig.GetString("DefaultHomeRegion", "");
            m_DefaultUserAvatarArchive = m_loginServerConfig.GetString("DefaultAvatarArchiveForNewUser", m_DefaultUserAvatarArchive);
            m_AllowAnonymousLogin = m_loginServerConfig.GetBoolean("AllowAnonymousLogin", false);
            m_AllowDuplicateLogin = m_loginServerConfig.GetBoolean("AllowDuplicateLogin", false);
            m_TOSLocation = m_loginServerConfig.GetString("FileNameOfTOS", "");
            LLLoginResponseRegister.RegisterValue("AllowFirstLife", m_loginServerConfig.GetBoolean("AllowFirstLifeInProfile", true) ? "Y" : "N");
            LLLoginResponseRegister.RegisterValue("TutorialURL", m_loginServerConfig.GetString("TutorialURL", ""));
            LLLoginResponseRegister.RegisterValue("OpenIDURL", m_loginServerConfig.GetString("OpenIDURL", ""));
            LLLoginResponseRegister.RegisterValue("SnapshotConfigURL", m_loginServerConfig.GetString("SnapshotConfigURL", ""));
            LLLoginResponseRegister.RegisterValue("MaxAgentGroups", m_loginServerConfig.GetInt("MaxAgentGroups", 100));
            LLLoginResponseRegister.RegisterValue("HelpURL", m_loginServerConfig.GetString("HelpURL", ""));
            LLLoginResponseRegister.RegisterValue("VoiceServerType", m_loginServerConfig.GetString("VoiceServerType", "vivox"));
            ReadEventValues(m_loginServerConfig);
            ReadClassifiedValues(m_loginServerConfig);
            LLLoginResponseRegister.RegisterValue("AllowExportPermission", m_loginServerConfig.GetBoolean("AllowUseageOfExportPermissions", true));

            m_DefaultRegionName = m_loginServerConfig.GetString("DefaultRegion", String.Empty);
            m_WelcomeMessage = m_loginServerConfig.GetString("WelcomeMessage", "");
            m_WelcomeMessageURL = m_loginServerConfig.GetString("CustomizedMessageURL", "");
            if (m_WelcomeMessageURL != "")
            {
                WebClient client = new WebClient();
                m_WelcomeMessage = client.DownloadString(m_WelcomeMessageURL);
            }
            LLLoginResponseRegister.RegisterValue ("Message", m_WelcomeMessage);
            m_RequireInventory = m_loginServerConfig.GetBoolean("RequireInventory", true);
            m_AllowRemoteSetLoginLevel = m_loginServerConfig.GetBoolean("AllowRemoteSetLoginLevel", false);
            m_MinLoginLevel = m_loginServerConfig.GetInt("MinLoginLevel", 0);
            LLLoginResponseRegister.RegisterValue("MapTileURL", m_loginServerConfig.GetString("MapTileURL", string.Empty));
            LLLoginResponseRegister.RegisterValue("WebProfileURL", m_loginServerConfig.GetString("WebProfileURL", string.Empty));
            LLLoginResponseRegister.RegisterValue("SearchURL", m_loginServerConfig.GetString("SearchURL", string.Empty));
            // if [LoginService] doesn't have the Search URL, try to get it from [GridInfoService]
            if (LLLoginResponseRegister.GetValue("SearchURL").ToString() == string.Empty)
            {
                IConfig gridInfo = config.Configs["GridInfoService"];
                LLLoginResponseRegister.RegisterValue ("SearchURL", gridInfo.GetString("search", string.Empty));
            }
            LLLoginResponseRegister.RegisterValue("SunTexture", m_loginServerConfig.GetString("SunTexture", sunTexture));
            LLLoginResponseRegister.RegisterValue("MoonTexture", m_loginServerConfig.GetString("MoonTexture", moonTexture));
            LLLoginResponseRegister.RegisterValue("CloudTexture", m_loginServerConfig.GetString("CloudTexture", cloudTexture));
            registry.RegisterModuleInterface<ILoginService> (this);
            m_registry = registry;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_UserAccountService = registry.RequestModuleInterface<IUserAccountService>().InnerService;
            m_agentInfoService = registry.RequestModuleInterface<IAgentInfoService>().InnerService;
            m_AuthenticationService = registry.RequestModuleInterface<IAuthenticationService>();
            m_InventoryService = registry.RequestModuleInterface<IInventoryService>();
            m_GridService = registry.RequestModuleInterface<IGridService>();
            m_AvatarService = registry.RequestModuleInterface<IAvatarService>().InnerService;
            m_FriendsService = registry.RequestModuleInterface<IFriendsService>();
            m_SimulationService = registry.RequestModuleInterface<ISimulationService>();
            m_AssetService = registry.RequestModuleInterface<IAssetService> ().InnerService;
            m_LibraryService = registry.RequestModuleInterface<ILibraryService>();
            m_CapsService = registry.RequestModuleInterface<ICapsService>();
            m_ArchiveService = registry.RequestModuleInterface<IAvatarAppearanceArchiver>();

            if (!Initialized)
            {
                Initialized = true;
                RegisterCommands();
            }

            LoginModules = AuroraModuleLoader.PickupModules<ILoginModule>();
            foreach (ILoginModule module in LoginModules)
            {
                module.Initialize(this, m_config, m_UserAccountService);
            }

            MainConsole.Instance.DebugFormat("[LLOGIN SERVICE]: Starting...");
        }

        public void FinishedStartup()
        {
        }

        public void ReadEventValues(IConfig config)
        {
            SetEventCategories((Int32)DirectoryManager.EventCategories.Discussion, "Discussion");
            SetEventCategories((Int32)DirectoryManager.EventCategories.Sports, "Sports");
            SetEventCategories((Int32)DirectoryManager.EventCategories.LiveMusic, "Live Music");
            SetEventCategories((Int32)DirectoryManager.EventCategories.Commercial, "Commercial");
            SetEventCategories((Int32)DirectoryManager.EventCategories.Nightlife, "Nightlife/Entertainment");
            SetEventCategories((Int32)DirectoryManager.EventCategories.Games, "Games/Contests");
            SetEventCategories((Int32)DirectoryManager.EventCategories.Pageants, "Pageants");
            SetEventCategories((Int32)DirectoryManager.EventCategories.Education, "Education");
            SetEventCategories((Int32)DirectoryManager.EventCategories.Arts, "Arts and Culture");
            SetEventCategories((Int32)DirectoryManager.EventCategories.Charity, "Charity/Support Groups");
            SetEventCategories((Int32)DirectoryManager.EventCategories.Miscellaneous, "Miscellaneous");
        }

        public void ReadClassifiedValues(IConfig config)
        {
            AddClassifiedCategory((Int32)DirectoryManager.ClassifiedCategories.Shopping, "Shopping");
            AddClassifiedCategory((Int32)DirectoryManager.ClassifiedCategories.LandRental, "Land Rental");
            AddClassifiedCategory((Int32)DirectoryManager.ClassifiedCategories.PropertyRental, "Property Rental");
            AddClassifiedCategory((Int32)DirectoryManager.ClassifiedCategories.SpecialAttraction, "Special Attraction");
            AddClassifiedCategory((Int32)DirectoryManager.ClassifiedCategories.NewProducts, "New Products");
            AddClassifiedCategory((Int32)DirectoryManager.ClassifiedCategories.Employment, "Employment");
            AddClassifiedCategory((Int32)DirectoryManager.ClassifiedCategories.Wanted, "Wanted");
            AddClassifiedCategory((Int32)DirectoryManager.ClassifiedCategories.Service, "Service");
            AddClassifiedCategory((Int32)DirectoryManager.ClassifiedCategories.Personal, "Personal");
        }

        public void SetEventCategories(Int32 value, string categoryName)
        {
            Hashtable hash = new Hashtable();
            hash["category_name"] = categoryName;
            hash["category_id"] = value;
            eventCategories.Add(hash);
        }

        public void AddClassifiedCategory(Int32 ID, string categoryName)
        {
            Hashtable hash = new Hashtable();
            hash["category_name"] = categoryName;
            hash["category_id"] = ID;
            classifiedCategories.Add(hash);
        }

        public Hashtable SetLevel(string firstName, string lastName, string passwd, int level, IPEndPoint clientIP)
        {
            Hashtable response = new Hashtable();
            response["success"] = "false";

            if (!m_AllowRemoteSetLoginLevel)
                return response;

            try
            {
                UserAccount account = m_UserAccountService.GetUserAccount(UUID.Zero, firstName, lastName);
                if (account == null)
                {
                    MainConsole.Instance.InfoFormat("[LLOGIN SERVICE]: Set Level failed, user {0} {1} not found", firstName, lastName);
                    return response;
                }

                if (account.UserLevel < 200)
                {
                    MainConsole.Instance.InfoFormat("[LLOGIN SERVICE]: Set Level failed, reason: user level too low");
                    return response;
                }

                //
                // Authenticate this user
                //
                // We don't support clear passwords here
                //
                string token = m_AuthenticationService.Authenticate(account.PrincipalID, "UserAccount", passwd, 30);
                UUID secureSession = UUID.Zero;
                if ((token == string.Empty) || (token != string.Empty && !UUID.TryParse(token, out secureSession)))
                {
                    MainConsole.Instance.InfoFormat("[LLOGIN SERVICE]: SetLevel failed, reason: authentication failed");
                    return response;
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[LLOGIN SERVICE]: SetLevel failed, exception " + e);
                return response;
            }

            m_MinLoginLevel = level;
            MainConsole.Instance.InfoFormat("[LLOGIN SERVICE]: Login level set to {0} by {1} {2}", level, firstName, lastName);

            response["success"] = true;
            return response;
        }

        public LoginResponse VerifyClient(string Name, string authType, string passwd, UUID scopeID, bool tosExists, string tosAccepted, string mac, string clientVersion, out UUID secureSession)
        {
            MainConsole.Instance.InfoFormat("[LLOGIN SERVICE]: Login verification request for {0}",
                Name);

            //
            // Get the account and check that it exists
            //
            UserAccount account = m_UserAccountService.GetUserAccount(scopeID, Name);
            if (authType == "UserAccount")
            {
                if (!passwd.StartsWith ("$1$"))
                    passwd = "$1$" + Util.Md5Hash (passwd);
                passwd = passwd.Remove (0, 3); //remove $1$
            }

            secureSession = UUID.Zero;
            if (account == null)
            {
                if (!m_AllowAnonymousLogin)
                {
                    MainConsole.Instance.InfoFormat("[LLOGIN SERVICE]: Login failed, reason: user not found");
                    return LLFailedLoginResponse.AccountProblem;
                }
                m_UserAccountService.CreateUser(Name, passwd, "");
                account = m_UserAccountService.GetUserAccount(scopeID, Name);
            }

            //Set the scopeID for the user
            scopeID = account.ScopeID;
            return InnerVerifyClient (account, authType, passwd, tosExists, tosAccepted, mac, clientVersion, out secureSession);
        }

        protected LoginResponse InnerVerifyClient(UserAccount account, string authType, string passwd, bool tosExists, string tosAccepted, string mac, string clientVersion, out UUID secureSession)
        {
            secureSession = UUID.Zero;
            
            //
            // Authenticate this user
            //
            string token = m_AuthenticationService.Authenticate (account.PrincipalID, authType, passwd, 30);
            if ((token == string.Empty) || (token != string.Empty && !UUID.TryParse(token, out secureSession)))
            {
                MainConsole.Instance.InfoFormat("[LLOGIN SERVICE]: Login failed, reason: authentication failed");
                return LLFailedLoginResponse.AuthenticationProblem;
            }

            IAgentConnector agentData = DataManager.RequestPlugin<IAgentConnector>("IAgentConnectorLocal");
            //Already tried to find it before this, so its not there at all.
            if (agentData != null)
            {
                IAgentInfo agent = agentData.GetAgent(account.PrincipalID);
                if (agent == null)
                {
                    agentData.CreateNewAgent(account.PrincipalID);
                    agent = agentData.GetAgent(account.PrincipalID);
                }
                if (mac != "")
                {
                    string reason = "";
                    if (!agentData.CheckMacAndViewer(mac, clientVersion, out reason))
                        return new LLFailedLoginResponse(LoginResponseEnum.PasswordIncorrect,
                            reason, false);
                }
                bool AcceptedNewTOS = false;
                //This gets if the viewer has accepted the new TOS
                if (!agent.AcceptTOS && tosExists)
                {
                    if (tosAccepted == "0")
                        AcceptedNewTOS = false;
                    else if (tosAccepted == "1")
                        AcceptedNewTOS = true;
                    else
                        AcceptedNewTOS = bool.Parse(tosAccepted);

                    if (agent.AcceptTOS != AcceptedNewTOS)
                    {
                        agent.AcceptTOS = AcceptedNewTOS;
                        agentData.UpdateAgent (agent);
                    }
                }
                if (!AcceptedNewTOS && !agent.AcceptTOS && m_UseTOS)
                {
                    StreamReader reader = new StreamReader(Path.Combine(Environment.CurrentDirectory, m_TOSLocation));
                    string TOS = reader.ReadToEnd();
                    reader.Close();
                    reader.Dispose();
                    return new LLFailedLoginResponse(LoginResponseEnum.ToSNeedsSent, TOS, false);
                }

                if (account.UserLevel < m_MinLoginLevel)
                {
                    MainConsole.Instance.InfoFormat("[LLOGIN SERVICE]: Login failed, reason: login is blocked for user level {0}", account.UserLevel);
                    return LLFailedLoginResponse.LoginBlockedProblem;
                }

                if ((agent.Flags & IAgentFlags.PermBan) == IAgentFlags.PermBan)
                {
                    MainConsole.Instance.Info("[LLOGIN SERVICE]: Login failed, reason: user is permanently banned.");
                    return LLFailedLoginResponse.PermanentBannedProblem;
                }

                if ((agent.Flags & IAgentFlags.TempBan) == IAgentFlags.TempBan)
                {
                    bool IsBanned = true;
                    string until = "";

                    if (agent.OtherAgentInformation.ContainsKey("TemperaryBanInfo"))
                    {
                        DateTime bannedTime = agent.OtherAgentInformation["TemperaryBanInfo"].AsDate();
                        until = string.Format(" until {0}", bannedTime.ToLongTimeString());

                        //Check to make sure the time hasn't expired
                        if (bannedTime.Ticks < DateTime.Now.Ticks)
                        {
                            //The banned time is less than now, let the user in.
                            IsBanned = false;
                        }
                    }

                    if (IsBanned)
                    {
                        MainConsole.Instance.Info(string.Format("[LLOGIN SERVICE]: Login failed, reason: user is temporarily banned {0}.", until));
                        return new LLFailedLoginResponse(LoginResponseEnum.MessagePopup, string.Format("You are blocked from connecting to this service{0}.", until), false);
                    }
                }
            }
            return null;
        }

        public LoginResponse VerifyClient (UUID AgentID, string authType, string passwd, UUID scopeID, bool tosExists, string tosAccepted, string mac, string clientVersion, out UUID secureSession)
        {
            MainConsole.Instance.InfoFormat("[LLOGIN SERVICE]: Login verification request for {0}",
                AgentID);

            //
            // Get the account and check that it exists
            //
            UserAccount account = m_UserAccountService.GetUserAccount(scopeID, AgentID);
            if (authType == "UserAccount")
            {
                if (!passwd.StartsWith ("$1$"))
                    passwd = "$1$" + Util.Md5Hash (passwd);
                passwd = passwd.Remove (0, 3); //remove $1$
            }

            secureSession = UUID.Zero;
            if (account == null)
            {
                return null;
            }

            //Set the scopeID for the user
            scopeID = account.ScopeID;
            return InnerVerifyClient (account, authType, passwd, tosExists, tosAccepted, mac, clientVersion, out secureSession);
        }

        public LoginResponse Login(string Name, string passwd, string startLocation, UUID scopeID,
            string clientVersion, string channel, string mac, string id0, IPEndPoint clientIP, Hashtable requestData, UUID secureSession)
        {
            UUID session = UUID.Random();

            MainConsole.Instance.InfoFormat("[LLOGIN SERVICE]: Login request for {0} from {1} with user agent {2} starting in {3}",
                Name, clientIP.Address, clientVersion, startLocation);
            UserAccount account = m_UserAccountService.GetUserAccount (scopeID, Name);
            try
            {
                string DisplayName = account.Name;
                IAgentInfo agent = null;

                IAgentConnector agentData = DataManager.RequestPlugin<IAgentConnector>();
                IProfileConnector profileData = DataManager.RequestPlugin<IProfileConnector>();
                if (agentData != null)
                    agent = agentData.GetAgent(account.PrincipalID);

                requestData["ip"] = clientIP.ToString();
                foreach (ILoginModule module in LoginModules)
                {
                    string message;
                    if (!module.Login(requestData, account.PrincipalID, out message))
                    {
                        LLFailedLoginResponse resp = new LLFailedLoginResponse(LoginResponseEnum.PasswordIncorrect,
                            message, false);
                        return resp;
                    }
                }
                

                //
                // Get the user's inventory
                //
                if (m_RequireInventory && m_InventoryService == null)
                {
                    MainConsole.Instance.WarnFormat("[LLOGIN SERVICE]: Login failed, reason: inventory service not set up");
                    return LLFailedLoginResponse.InventoryProblem;
                }
                List<InventoryFolderBase> inventorySkel = m_InventoryService.GetInventorySkeleton(account.PrincipalID);
                if (m_RequireInventory && ((inventorySkel == null) || (inventorySkel.Count == 0)))
                {
                    m_InventoryService.CreateUserInventory(account.PrincipalID, m_DefaultUserAvatarArchive == "");
                    inventorySkel = m_InventoryService.GetInventorySkeleton(account.PrincipalID);
                    if (m_RequireInventory && ((inventorySkel == null) || (inventorySkel.Count == 0)))
                    {
                        MainConsole.Instance.InfoFormat("[LLOGIN SERVICE]: Login failed, reason: unable to retrieve user inventory");
                        return LLFailedLoginResponse.InventoryProblem;
                    }
                }
                if (m_InventoryService.CreateUserRootFolder (account.PrincipalID))
                    //Gotta refetch... since something went wrong
                    inventorySkel = m_InventoryService.GetInventorySkeleton (account.PrincipalID);

                if (profileData != null)
                {
                    IUserProfileInfo UPI = profileData.GetUserProfile(account.PrincipalID);
                    if (UPI == null)
                    {
                        profileData.CreateNewProfile(account.PrincipalID);
                        UPI = profileData.GetUserProfile(account.PrincipalID);
                        UPI.AArchiveName = m_DefaultUserAvatarArchive;
                        UPI.IsNewUser = true;
                        //profileData.UpdateUserProfile(UPI); //It gets hit later by the next thing
                    }
                    //Find which is set, if any
                    string archiveName = (UPI.AArchiveName != "" && UPI.AArchiveName != " ") ? UPI.AArchiveName : m_DefaultUserAvatarArchive;
                    if (UPI.IsNewUser && archiveName != "")
                    {
                        m_ArchiveService.LoadAvatarArchive(archiveName, account.Name);
                        UPI.AArchiveName = "";
                    }
                    if (UPI.IsNewUser)
                    {
                        UPI.IsNewUser = false;
                        profileData.UpdateUserProfile(UPI);
                    }
                    if(UPI.DisplayName != "")
                        DisplayName = UPI.DisplayName;
                }

                // Get active gestures
                List<InventoryItemBase> gestures = m_InventoryService.GetActiveGestures(account.PrincipalID);
                //MainConsole.Instance.DebugFormat("[LLOGIN SERVICE]: {0} active gestures", gestures.Count);

                //Reset logged in to true if the user was crashed, but don't fire the logged in event yet
                m_agentInfoService.SetLoggedIn (account.PrincipalID.ToString (), true, false, UUID.Zero);
                //Lock it as well
                m_agentInfoService.LockLoggedInStatus (account.PrincipalID.ToString (), true);
                //Now get the logged in status, then below make sure to kill the previous agent if we crashed before
                UserInfo guinfo = m_agentInfoService.GetUserInfo (account.PrincipalID.ToString ());
                //
                // Clear out any existing CAPS the user may have
                //
                if (m_CapsService != null)
                {
                    IAgentProcessing agentProcessor = m_registry.RequestModuleInterface<IAgentProcessing>();
                    if (agentProcessor != null)
                    {
                        IClientCapsService clientCaps = m_CapsService.GetClientCapsService(account.PrincipalID);
                        if (clientCaps != null)
                        {
                            IRegionClientCapsService rootRegionCaps = clientCaps.GetRootCapsService();
                            if (rootRegionCaps != null)
                                agentProcessor.LogoutAgent(rootRegionCaps, !m_AllowDuplicateLogin);
                        }
                    }
                    else
                        m_CapsService.RemoveCAPS(account.PrincipalID);
                }

                //
                // Change Online status and get the home region
                //
                GridRegion home = null;
                if (guinfo != null && (guinfo.HomeRegionID != UUID.Zero) && m_GridService != null)
                {
                    home = m_GridService.GetRegionByUUID(scopeID, guinfo.HomeRegionID);
                }
                bool GridUserInfoFound = true;
                if (guinfo == null)
                {
                    GridUserInfoFound = false;
                    // something went wrong, make something up, so that we don't have to test this anywhere else
                    guinfo = new UserInfo {UserID = account.PrincipalID.ToString()};
                    guinfo.CurrentPosition = guinfo.HomePosition = new Vector3(128, 128, 30);
                }

                //
                // Find the destination region/grid
                //
                string where = string.Empty;
                Vector3 position = Vector3.Zero;
                Vector3 lookAt = Vector3.Zero;
                TeleportFlags tpFlags = TeleportFlags.ViaLogin;
                GridRegion destination = FindDestination (account, scopeID, guinfo, session, startLocation, home, out tpFlags, out where, out position, out lookAt);
                if (destination == null)
                {
                    MainConsole.Instance.InfoFormat("[LLOGIN SERVICE]: Login failed, reason: destination not found");
                    return LLFailedLoginResponse.DeadRegionProblem;
                }
                if (!GridUserInfoFound || guinfo.HomeRegionID == UUID.Zero) //Give them a default home and last
                {
                    if (m_GridService != null)
                    {
                        List<GridRegion> DefaultRegions = m_GridService.GetDefaultRegions(account.ScopeID);
                        GridRegion DefaultRegion = null;
                        DefaultRegion = DefaultRegions.Count == 0 ? destination : DefaultRegions[0];

                        if (m_DefaultHomeRegion != "" && guinfo.HomeRegionID == UUID.Zero)
                        {
                            GridRegion newHomeRegion = m_GridService.GetRegionByName(account.ScopeID, m_DefaultHomeRegion);
                            if (newHomeRegion == null)
                                guinfo.HomeRegionID = guinfo.CurrentRegionID = DefaultRegion.RegionID;
                            else
                                guinfo.HomeRegionID = guinfo.CurrentRegionID = newHomeRegion.RegionID;
                        }
                        else if (guinfo.HomeRegionID == UUID.Zero)
                            guinfo.HomeRegionID = guinfo.CurrentRegionID = DefaultRegion.RegionID;
                    }

                    //Z = 0 so that it fixes it on the region server and puts it on the ground
                    guinfo.CurrentPosition = guinfo.HomePosition = new Vector3(128, 128, 25);

                    guinfo.HomeLookAt = guinfo.CurrentLookAt = new Vector3(0, 0, 0);

                    m_agentInfoService.SetLastPosition(guinfo.UserID, guinfo.CurrentRegionID, guinfo.CurrentPosition, guinfo.CurrentLookAt);
                    m_agentInfoService.SetHomePosition(guinfo.UserID, guinfo.HomeRegionID, guinfo.HomePosition, guinfo.HomeLookAt);
                }

                //
                // Get the avatar
                //
                AvatarAppearance avappearance;
                if (m_AvatarService != null)
                {
                    avappearance = m_AvatarService.GetAppearance(account.PrincipalID);
                    if (avappearance == null)
                    {
                        //Create an appearance for the user if one doesn't exist
                        if (m_DefaultUserAvatarArchive != "")
                        {
                            MainConsole.Instance.Error("[LLoginService]: Cannot find an appearance for user " + account.Name +
                                ", loading the default avatar from " + m_DefaultUserAvatarArchive + ".");
                            m_ArchiveService.LoadAvatarArchive(m_DefaultUserAvatarArchive, account.Name);
                        }
                        else
                        {
                            MainConsole.Instance.Error("[LLoginService]: Cannot find an appearance for user " + account.Name + ", setting to the default avatar.");
                            AvatarAppearance appearance = new AvatarAppearance(account.PrincipalID);
                            m_AvatarService.SetAvatar(account.PrincipalID, new AvatarData(appearance));
                        }
                        avappearance = m_AvatarService.GetAppearance(account.PrincipalID);
                    }
                    else
                    {
                        //Verify that all assets exist now
                        for (int i = 0; i < avappearance.Wearables.Length; i++)
                        {
                            bool messedUp = false;
                            foreach (KeyValuePair<UUID, UUID> item in avappearance.Wearables[i].GetItems())
                            {
                                AssetBase asset = m_AssetService.Get(item.Value.ToString());
                                if (asset == null)
                                {
                                    InventoryItemBase invItem = m_InventoryService.GetItem (new InventoryItemBase (item.Value));
                                    if (invItem == null)
                                    {
                                        MainConsole.Instance.Warn("[LLOGIN SERVICE]: Missing avatar appearance asset for user " + account.Name + " for item " + item.Value + ", asset should be " + item.Key + "!");
                                        messedUp = true;
                                    }
                                }
                            }
                            if (messedUp)
                                avappearance.Wearables[i] = AvatarWearable.DefaultWearables[i];
                        }
                        //Also verify that all baked texture indices exist
                        foreach (byte BakedTextureIndex in AvatarAppearance.BAKE_INDICES)
                        {
                            if (BakedTextureIndex == 19) //Skirt isn't used unless you have a skirt
                                continue;
                            if (avappearance.Texture.GetFace(BakedTextureIndex).TextureID == AppearanceManager.DEFAULT_AVATAR_TEXTURE)
                            {
                                MainConsole.Instance.Warn("[LLOGIN SERVICE]: Bad texture index for user " + account.Name + " for " + BakedTextureIndex + "!");
                                avappearance = new AvatarAppearance(account.PrincipalID);
                                m_AvatarService.SetAvatar(account.PrincipalID, new AvatarData(avappearance));
                                break;
                            }
                        }
                    }
                }
                else
                    avappearance = new AvatarAppearance(account.PrincipalID);

                //
                // Instantiate/get the simulation interface and launch an agent at the destination
                //
                string reason = string.Empty;
                AgentCircuitData aCircuit = LaunchAgentAtGrid (destination, tpFlags, account, avappearance, session, secureSession, position, where,
                    clientIP, out where, out reason, out destination);

                if (aCircuit == null)
                {
                    MainConsole.Instance.InfoFormat("[LLOGIN SERVICE]: Login failed, reason: {0}", reason);
                    return new LLFailedLoginResponse (LoginResponseEnum.InternalError, reason, false);
                }

                // Get Friends list 
                FriendInfo[] friendsList = new FriendInfo[0];
                if (m_FriendsService != null)
                {
                    friendsList = m_FriendsService.GetFriends(account.PrincipalID);
                    //MainConsole.Instance.DebugFormat("[LLOGIN SERVICE]: Retrieved {0} friends", friendsList.Length);
                }

                //Set them as logged in now, they are ready, and fire the logged in event now, as we're all done
                m_agentInfoService.SetLastPosition (account.PrincipalID.ToString (), destination.RegionID, position, lookAt);
                m_agentInfoService.LockLoggedInStatus (account.PrincipalID.ToString (), false); //Unlock it now
                m_agentInfoService.SetLoggedIn(account.PrincipalID.ToString(), true, true, destination.RegionID);
                
                //
                // Finally, fill out the response and return it
                //
                string MaturityRating = "A";
                string MaxMaturity = "A";
                if (agent != null)
                {
                    if (agent.MaturityRating == 0)
                        MaturityRating = "P";
                    else if (agent.MaturityRating == 1)
                        MaturityRating = "M";
                    else if (agent.MaturityRating == 2)
                        MaturityRating = "A";

                    if (agent.MaxMaturity == 0)
                        MaxMaturity = "P";
                    else if (agent.MaxMaturity == 1)
                        MaxMaturity = "M";
                    else if (agent.MaxMaturity == 2)
                        MaxMaturity = "A";
                }

                LLLoginResponse response = new LLLoginResponse(account, aCircuit, guinfo, destination, inventorySkel, friendsList, m_InventoryService, m_LibraryService,
                    where, startLocation, position, lookAt, gestures, home, clientIP, MaxMaturity, MaturityRating,
                    eventCategories, classifiedCategories, FillOutSeedCap (aCircuit, destination, clientIP, account.PrincipalID), m_config, DisplayName, m_registry);

                MainConsole.Instance.InfoFormat("[LLOGIN SERVICE]: All clear. Sending login response to client to login to region " + destination.RegionName + ", tried to login to " + startLocation + " at " + position.ToString() + ".");
                AddLoginSuccessNotification(account);
                return response;
            }
            catch (Exception e)
            {
                MainConsole.Instance.WarnFormat ("[LLOGIN SERVICE]: Exception processing login for {0} : {1}", Name, e);
                if (account != null)
                {
                    //Revert their logged in status if we got that far
                    m_agentInfoService.LockLoggedInStatus (account.PrincipalID.ToString (), false); //Unlock it now
                    m_agentInfoService.SetLoggedIn (account.PrincipalID.ToString (), false, false, UUID.Zero);
                }
                return LLFailedLoginResponse.InternalError;
            }
            
        }

        private void AddLoginSuccessNotification(UserAccount account)
        {
            if (MainConsole.NotificationService == null)
                return;
            MainConsole.NotificationService.AddNotification(AlertLevel.Low, account.Name + " has logged in successfully.", "LLLoginService",
                (messages) =>
                {
                    return messages.Count + " users have logged in successfully.";
                });
        }

        protected string FillOutSeedCap(AgentCircuitData aCircuit, GridRegion destination, IPEndPoint ipepClient, UUID AgentID)
        {
            if(m_CapsService != null)
            {
                //Remove any previous users
                string CapsBase = CapsUtil.GetRandomCapsObjectPath();
                return m_CapsService.CreateCAPS(AgentID, CapsUtil.GetCapsSeedPath(CapsBase), destination.RegionHandle, true, aCircuit);
            }
            return "";
        }

        protected GridRegion FindDestination (UserAccount account, UUID scopeID, UserInfo pinfo, UUID sessionID, string startLocation, GridRegion home, out TeleportFlags tpFlags, out string where, out Vector3 position, out Vector3 lookAt)
        {
            where = "home";
            position = new Vector3(128, 128, 25);
            lookAt = new Vector3(0, 1, 0);
            tpFlags = TeleportFlags.ViaLogin;

            if (m_GridService == null)
                return null;

            if (startLocation.Equals("home"))
            {
                tpFlags |= TeleportFlags.ViaLandmark;
                // logging into home region
                if (pinfo == null)
                    return null;

                GridRegion region = null;

                bool tryDefaults = false;

                if (home == null)
                {
                    MainConsole.Instance.WarnFormat(
                        "[LLOGIN SERVICE]: User {0} {1} tried to login to a 'home' start location but they have none set",
                        account.FirstName, account.LastName);

                    tryDefaults = true;
                }
                else
                {
                    region = home;

                    position = pinfo.HomePosition;
                    lookAt = pinfo.HomeLookAt;
                }

                if (tryDefaults)
                {
                    tpFlags &= ~TeleportFlags.ViaLandmark;
                    List<GridRegion> defaults = m_GridService.GetDefaultRegions(scopeID);
                    if (defaults != null && defaults.Count > 0)
                    {
                        region = defaults[0];
                        where = "safe";
                    }
                    else
                    {
                        List<GridRegion> fallbacks = m_GridService.GetFallbackRegions(account.ScopeID, 0, 0);
                        if (fallbacks != null && fallbacks.Count > 0)
                        {
                            region = fallbacks[0];
                            where = "safe";
                        }
                        else
                        {
                            //Try to find any safe region
                            List<GridRegion> safeRegions = m_GridService.GetSafeRegions(account.ScopeID, 0, 0);
                            if (safeRegions != null && safeRegions.Count > 0)
                            {
                                region = safeRegions[0];
                                where = "safe";
                            }
                            else
                            {
                                MainConsole.Instance.WarnFormat("[LLOGIN SERVICE]: User {0} {1} does not have a valid home and this grid does not have default locations. Attempting to find random region",
                                    account.FirstName, account.LastName);
                                defaults = m_GridService.GetRegionsByName(scopeID, "", 1);
                                if (defaults != null && defaults.Count > 0)
                                {
                                    region = defaults[0];
                                    where = "safe";
                                }
                            }
                        }
                    }
                }

                return region;
            }
            if (startLocation.Equals ("last"))
            {
                tpFlags |= TeleportFlags.ViaLandmark;
                // logging into last visited region
                where = "last";

                if (pinfo == null)
                    return null;

                GridRegion region = null;

                if (pinfo.CurrentRegionID.Equals (UUID.Zero) || (region = m_GridService.GetRegionByUUID (scopeID, pinfo.CurrentRegionID)) == null)
                {
                    tpFlags &= ~TeleportFlags.ViaLandmark;
                    List<GridRegion> defaults = m_GridService.GetDefaultRegions (scopeID);
                    if (defaults != null && defaults.Count > 0)
                    {
                        region = defaults[0];
                        where = "safe";
                    }
                    else
                    {
                        defaults = m_GridService.GetFallbackRegions (scopeID, 0, 0);
                        if (defaults != null && defaults.Count > 0)
                        {
                            region = defaults[0];
                            where = "safe";
                        }
                        else
                        {
                            defaults = m_GridService.GetSafeRegions (scopeID, 0, 0);
                            if (defaults != null && defaults.Count > 0)
                            {
                                region = defaults[0];
                                where = "safe";
                            }
                        }
                    }

                }
                else
                {
                    position = pinfo.CurrentPosition;
                    if (position.X < 0)
                        position.X = 0;
                    if (position.Y < 0)
                        position.Y = 0;
                    if (position.Z < 0)
                        position.Z = 0;
                    if (position.X > region.RegionSizeX)
                        position.X = region.RegionSizeX;
                    if (position.Y > region.RegionSizeY)
                        position.Y = region.RegionSizeY;


                    lookAt = pinfo.CurrentLookAt;
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
                position = new Vector3(float.Parse(uriMatch.Groups["x"].Value, Culture.NumberFormatInfo),
                                       float.Parse(uriMatch.Groups["y"].Value, Culture.NumberFormatInfo),
                                       float.Parse(uriMatch.Groups["z"].Value, Culture.NumberFormatInfo));

                string regionName = uriMatch.Groups["region"].ToString();
                if (!regionName.Contains("@"))
                {
                    List<GridRegion> regions = m_GridService.GetRegionsByName(scopeID, regionName, 1);
                    if ((regions == null) || (regions.Count == 0))
                    {
                        MainConsole.Instance.InfoFormat(
                            "[LLLOGIN SERVICE]: Got Custom Login URI {0}, can't locate region {1}. Trying defaults.",
                            startLocation, regionName);
                        regions = m_GridService.GetDefaultRegions(scopeID);
                        if (regions != null && regions.Count > 0)
                        {
                            where = "safe";
                            return regions[0];
                        }
                        List<GridRegion> fallbacks = m_GridService.GetFallbackRegions(account.ScopeID, 0, 0);
                        if (fallbacks != null && fallbacks.Count > 0)
                        {
                            where = "safe";
                            return fallbacks[0];
                        }
                        //Try to find any safe region
                        List<GridRegion> safeRegions = m_GridService.GetSafeRegions(account.ScopeID, 0, 0);
                        if (safeRegions != null && safeRegions.Count > 0)
                        {
                            where = "safe";
                            return safeRegions[0];
                        }
                        MainConsole.Instance.InfoFormat(
                            "[LLLOGIN SERVICE]: Got Custom Login URI {0}, Grid does not have any available regions.",
                            startLocation);
                        return null;
                    }
                    return regions[0];
                }
                //This is so that you can login to other grids via IWC (or HG), example"RegionTest@testingserver.com:8002". All this really needs to do is inform the other grid that we have a user who wants to connect. IWC allows users to login by default to other regions (without the host names), but if one is provided and we don't have a link, we need to create one here.
                string[] parts = regionName.Split(new char[] {'@'});
                if (parts.Length < 2)
                {
                    MainConsole.Instance.InfoFormat("[LLLOGIN SERVICE]: Got Custom Login URI {0}, can't locate region {1}",
                                     startLocation, regionName);
                    return null;
                }
                // Valid specification of a remote grid

                regionName = parts[0];
                string domainLocator = parts[1];

                //Try now that we removed the domain locator
                GridRegion region = m_GridService.GetRegionByName(scopeID, regionName);
                if (region != null && region.RegionName == regionName)
                    //Make sure the region name is right too... it could just be a similar name
                    return region;
                ICommunicationService service = m_registry.RequestModuleInterface<ICommunicationService>();
                if (service != null)
                {
                    region = service.GetRegionForGrid(regionName, domainLocator);

                    if (region != null)
                        return region;
                }
                List<GridRegion> defaults = m_GridService.GetDefaultRegions(scopeID);
                if (defaults != null && defaults.Count > 0)
                {
                    where = "safe";
                    return defaults[0];
                }
                else
                {
                    List<GridRegion> fallbacks = m_GridService.GetFallbackRegions(account.ScopeID, 0, 0);
                    if (fallbacks != null && fallbacks.Count > 0)
                    {
                        where = "safe";
                        return fallbacks[0];
                    }
                    else
                    {
                        //Try to find any safe region
                        List<GridRegion> safeRegions = m_GridService.GetSafeRegions(account.ScopeID, 0, 0);
                        if (safeRegions != null && safeRegions.Count > 0)
                        {
                            where = "safe";
                            return safeRegions[0];
                        }
                        MainConsole.Instance.InfoFormat(
                            "[LLLOGIN SERVICE]: Got Custom Login URI {0}, Grid does not have any available regions.",
                            startLocation);
                        return null;
                    }
                }
            }
        }

        protected AgentCircuitData LaunchAgentAtGrid(GridRegion destination, TeleportFlags tpFlags, UserAccount account, AvatarAppearance appearance,
            UUID session, UUID secureSession, Vector3 position, string currentWhere,
            IPEndPoint clientIP, out string where, out string reason, out GridRegion dest)
        {
            where = currentWhere;
            reason = string.Empty;
            uint circuitCode = 0;
            AgentCircuitData aCircuit = null;
            dest = destination;

            bool success = false;

            #region Launch Agent

            circuitCode = (uint)Util.RandomClass.Next();
            aCircuit = MakeAgent(destination, account, appearance, session, secureSession, circuitCode, position, clientIP);
            aCircuit.teleportFlags = (uint)tpFlags;
            success = LaunchAgentDirectly(destination, ref aCircuit, out reason);
            if (!success && m_GridService != null)
            {
                //Remove the landmark flag (landmark is used for ignoring the landing points in the region)
                aCircuit.teleportFlags &= ~(uint)TeleportFlags.ViaLandmark;
                m_GridService.SetRegionUnsafe(destination.RegionID);

                // Make sure the client knows this isn't where they wanted to land
                where = "safe";

                // Try the default regions
                List<GridRegion> defaultRegions = m_GridService.GetDefaultRegions(account.ScopeID);
                if (defaultRegions != null)
                {
                    success = TryFindGridRegionForAgentLogin(defaultRegions, account,
                        appearance, session, secureSession, circuitCode, position,
                        clientIP, aCircuit, out dest);
                }
                if (!success)
                {
                    // Try the fallback regions
                    List<GridRegion> fallbacks = m_GridService.GetFallbackRegions(account.ScopeID, destination.RegionLocX, destination.RegionLocY);
                    if (fallbacks != null)
                    {
                        success = TryFindGridRegionForAgentLogin(fallbacks, account,
                            appearance, session, secureSession, circuitCode, position,
                            clientIP, aCircuit, out dest);
                    }
                    if (!success)
                    {
                        //Try to find any safe region
                        List<GridRegion> safeRegions = m_GridService.GetSafeRegions(account.ScopeID, destination.RegionLocX, destination.RegionLocY);
                        if (safeRegions != null)
                        {
                            success = TryFindGridRegionForAgentLogin(safeRegions, account,
                                appearance, session, secureSession, circuitCode, position,
                                clientIP, aCircuit, out dest);
                            if (!success)
                                reason = "No Region Found";
                        }
                    }
                }
            }

            #endregion

            if (success)
            {
                //Set the region to safe since we got there
                m_GridService.SetRegionSafe (destination.RegionID);
                return aCircuit;
            }
            return null;
        }

        protected bool TryFindGridRegionForAgentLogin(List<GridRegion> regions, UserAccount account,
            AvatarAppearance appearance, UUID session, UUID secureSession, uint circuitCode, Vector3 position,
            IPEndPoint clientIP, AgentCircuitData aCircuit, out GridRegion destination)
        {
            foreach (GridRegion r in regions)
            {
                string reason;
                bool success = LaunchAgentDirectly(r, ref aCircuit, out reason);
                if (success)
                {
                    aCircuit = MakeAgent(r, account, appearance, session, secureSession, circuitCode, position, clientIP);
                    destination = r;
                    return true;
                }
                m_GridService.SetRegionUnsafe(r.RegionID);
            }
            destination = null;
            return false;
        }

        protected AgentCircuitData MakeAgent(GridRegion region, UserAccount account,
            AvatarAppearance appearance, UUID session, UUID secureSession, uint circuit, Vector3 position,
            IPEndPoint clientIP)
        {
            AgentCircuitData aCircuit = new AgentCircuitData
                                            {
                                                AgentID = account.PrincipalID,
                                                Appearance = appearance ?? new AvatarAppearance(account.PrincipalID),
                                                CapsPath = CapsUtil.GetRandomCapsObjectPath(),
                                                child = false,
                                                circuitcode = circuit,
                                                SecureSessionID = secureSession,
                                                SessionID = session,
                                                startpos = position,
                                                IPAddress = clientIP.Address.ToString(),
                                                ClientIPEndPoint = clientIP
                                            };


            // the first login agent is root

            return aCircuit;
        }

        protected bool LaunchAgentDirectly(GridRegion region, ref AgentCircuitData aCircuit, out string reason)
        {
            return m_registry.RequestModuleInterface<IAgentProcessing> ().LoginAgent (region, ref aCircuit, out reason);
        }

        #region Console Commands

        protected void RegisterCommands()
        {
            if (MainConsole.Instance == null)
                return;
            MainConsole.Instance.Commands.AddCommand("login level",
                    "login level <level>",
                    "Set the minimum user level to log in", HandleLoginCommand);

            MainConsole.Instance.Commands.AddCommand("login reset",
                    "login reset",
                    "Reset the login level to allow all users",
                    HandleLoginCommand);

            MainConsole.Instance.Commands.AddCommand("login text",
                    "login text <text>",
                    "Set the text users will see on login", HandleLoginCommand);
        }

        protected void HandleLoginCommand(string[] cmd)
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

        #endregion
    }
}
