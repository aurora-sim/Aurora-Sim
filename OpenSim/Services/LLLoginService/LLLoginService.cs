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
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Framework.Capabilities;
using OpenSim.Framework.Console;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using FriendInfo = OpenSim.Services.Interfaces.FriendInfo;
using Aurora.DataManager;
using Aurora.Framework;
using AvatarArchives;

namespace OpenSim.Services.LLLoginService
{
    public interface ILoginModule
    {
        void Initialize(LLLoginService service, IConfigSource config, IUserAccountService UAService);
        bool Login(Hashtable request, UUID User, out string message);
    }

    public class LLLoginService : ILoginService, IService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static bool Initialized = false;

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

        protected string m_DefaultRegionName;
        protected string m_WelcomeMessage;
        protected bool m_RequireInventory;
        protected int m_MinLoginLevel;
        protected string m_GatekeeperURL;
        protected bool m_AllowRemoteSetLoginLevel;
        protected string m_MapTileURL;
        protected string m_SearchURL;

        protected IConfig m_LoginServerConfig;
        protected IConfigSource m_config;
        protected bool m_AllowAnonymousLogin = false;
        protected bool m_UseTOS = false;
        protected string m_TOSLocation = "";
        protected string m_DefaultUserAvatarArchive = "DefaultAvatar.aa";
        protected string m_DefaultHomeRegion = "";
        protected bool m_AllowFirstLife = true;
        protected string m_TutorialURL = "";
        protected ArrayList eventCategories = new ArrayList();
        protected ArrayList classifiedCategories = new ArrayList();
        protected GridAvatarArchiver archiver;
        protected List<ILoginModule> LoginModules = new List<ILoginModule>();
        protected bool allowExportPermission = true;

        public int MinLoginLevel
        {
            get { return m_MinLoginLevel; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_config = config;
            m_LoginServerConfig = config.Configs["LoginService"];
            if (m_LoginServerConfig == null)
                throw new Exception(String.Format("No section LoginService in config file"));

            m_UseTOS = m_LoginServerConfig.GetBoolean("UseTermsOfServiceOnFirstLogin", false);
            m_DefaultHomeRegion = m_LoginServerConfig.GetString("DefaultHomeRegion", "");
            m_DefaultUserAvatarArchive = m_LoginServerConfig.GetString("DefaultAvatarArchiveForNewUser", m_DefaultUserAvatarArchive);
            m_AllowAnonymousLogin = m_LoginServerConfig.GetBoolean("AllowAnonymousLogin", false);
            m_TOSLocation = m_LoginServerConfig.GetString("FileNameOfTOS", "");
            m_AllowFirstLife = m_LoginServerConfig.GetBoolean("AllowFirstLifeInProfile", true);
            m_TutorialURL = m_LoginServerConfig.GetString("TutorialURL", m_TutorialURL);
            ReadEventValues(m_LoginServerConfig);
            ReadClassifiedValues(m_LoginServerConfig);
            allowExportPermission = m_LoginServerConfig.GetBoolean("AllowUseageOfExportPermissions", true);

            m_DefaultRegionName = m_LoginServerConfig.GetString("DefaultRegion", String.Empty);
            m_WelcomeMessage = m_LoginServerConfig.GetString("WelcomeMessage", "Welcome to OpenSim!");
            m_RequireInventory = m_LoginServerConfig.GetBoolean("RequireInventory", true);
            m_AllowRemoteSetLoginLevel = m_LoginServerConfig.GetBoolean("AllowRemoteSetLoginLevel", false);
            m_MinLoginLevel = m_LoginServerConfig.GetInt("MinLoginLevel", 0);
            m_GatekeeperURL = m_LoginServerConfig.GetString("GatekeeperURI", string.Empty);
            m_MapTileURL = m_LoginServerConfig.GetString("MapTileURL", string.Empty);
            m_SearchURL = m_LoginServerConfig.GetString("SearchURL", string.Empty);
            // if [LoginService] doesn't have the Search URL, try to get it from [GridInfoService]
            if (m_SearchURL == string.Empty)
            {
                IConfig gridInfo = config.Configs["GridInfoService"];
                m_SearchURL = gridInfo.GetString("search", string.Empty);
            }
            registry.RegisterModuleInterface<ILoginService>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            m_UserAccountService = registry.RequestModuleInterface<IUserAccountService>();
            m_agentInfoService = registry.RequestModuleInterface<IAgentInfoService>();
            m_AuthenticationService = registry.RequestModuleInterface<IAuthenticationService>();
            m_InventoryService = registry.RequestModuleInterface<IInventoryService>();
            m_GridService = registry.RequestModuleInterface<IGridService>();
            m_AvatarService = registry.RequestModuleInterface<IAvatarService>();
            m_FriendsService = registry.RequestModuleInterface<IFriendsService>();
            m_SimulationService = registry.RequestModuleInterface<ISimulationService>();
            m_AssetService = registry.RequestModuleInterface<IAssetService>();
            m_LibraryService = registry.RequestModuleInterface<ILibraryService>();
            m_CapsService = registry.RequestModuleInterface<ICapsService>();

            if (!Initialized)
            {
                Initialized = true;
                RegisterCommands();
            }
            //Start the grid profile archiver.
            new GridAvatarProfileArchiver(m_UserAccountService);
            archiver = new GridAvatarArchiver(m_UserAccountService, m_AvatarService, m_InventoryService, m_AssetService);

            LoginModules = Aurora.Framework.AuroraModuleLoader.PickupModules<ILoginModule>();
            foreach (ILoginModule module in LoginModules)
            {
                module.Initialize(this, m_config, m_UserAccountService);
            }

            m_log.DebugFormat("[LLOGIN SERVICE]: Starting...");
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
                    m_log.InfoFormat("[LLOGIN SERVICE]: Set Level failed, user {0} {1} not found", firstName, lastName);
                    return response;
                }

                if (account.UserLevel < 200)
                {
                    m_log.InfoFormat("[LLOGIN SERVICE]: Set Level failed, reason: user level too low");
                    return response;
                }

                //
                // Authenticate this user
                //
                // We don't support clear passwords here
                //
                string token = m_AuthenticationService.Authenticate(account.PrincipalID, passwd, 30);
                UUID secureSession = UUID.Zero;
                if ((token == string.Empty) || (token != string.Empty && !UUID.TryParse(token, out secureSession)))
                {
                    m_log.InfoFormat("[LLOGIN SERVICE]: SetLevel failed, reason: authentication failed");
                    return response;
                }
            }
            catch (Exception e)
            {
                m_log.Error("[LLOGIN SERVICE]: SetLevel failed, exception " + e.ToString());
                return response;
            }

            m_MinLoginLevel = level;
            m_log.InfoFormat("[LLOGIN SERVICE]: Login level set to {0} by {1} {2}", level, firstName, lastName);

            response["success"] = true;
            return response;
        }

        public LoginResponse VerifyClient(string firstName, string lastName, string passwd, UUID scopeID, bool tosExists, string tosAccepted, string mac, string clientVersion, out UUID secureSession)
        {
            m_log.InfoFormat("[LLOGIN SERVICE]: Login verification request for {0} {1}",
                firstName, lastName);

            //
            // Get the account and check that it exists
            //
            UserAccount account = m_UserAccountService.GetUserAccount(scopeID, firstName, lastName);
            if (!passwd.StartsWith("$1$"))
                passwd = "$1$" + Util.Md5Hash(passwd);
            passwd = passwd.Remove(0, 3); //remove $1$

            secureSession = UUID.Zero;
            if (account == null)
            {
                if (!m_AllowAnonymousLogin)
                {
                    m_log.InfoFormat("[LLOGIN SERVICE]: Login failed, reason: user not found");
                    return LLFailedLoginResponse.AccountProblem;
                }
                else
                {
                    m_UserAccountService.CreateUser(firstName, lastName, passwd, "");
                    account = m_UserAccountService.GetUserAccount(scopeID, firstName, lastName);
                }
            }

            //Set the scopeID for the user
            scopeID = account.ScopeID;
            //
            // Authenticate this user
            //
            string token = m_AuthenticationService.Authenticate(account.PrincipalID, passwd, 30);
            if ((token == string.Empty) || (token != string.Empty && !UUID.TryParse(token, out secureSession)))
            {
                m_log.InfoFormat("[LLOGIN SERVICE]: Login failed, reason: authentication failed");
                return LLFailedLoginResponse.AuthenticationProblem;
            }

            IAgentInfo agent = null;

            IAgentConnector agentData = DataManager.RequestPlugin<IAgentConnector>("IAgentConnectorLocal");
            IProfileConnector profileData = DataManager.RequestPlugin<IProfileConnector>("IProfileConnectorLocal");
            //Already tried to find it before this, so its not there at all.
            if (agentData != null)
            {
                agent = agentData.GetAgent(account.PrincipalID);
                if (agent == null)
                {
                    agentData.CreateNewAgent(account.PrincipalID);
                    agent = agentData.GetAgent(account.PrincipalID);
                }
                if (agentData != null && mac != "")
                {
                    string reason = "";
                    if (!agentData.CheckMacAndViewer(mac, clientVersion, out reason))
                        return new LLFailedLoginResponse(LoginResponseEnum.PasswordIncorrect,
                            reason, false);
                }
                else
                {
                    //We tried... might as well skip it
                }
                bool AcceptedNewTOS = false;
                //This gets if the viewer has accepted the new TOS
                if (tosExists)
                {
                    if (tosAccepted == "0")
                        AcceptedNewTOS = false;
                    else if (tosAccepted == "1")
                        AcceptedNewTOS = true;
                    else
                        AcceptedNewTOS = bool.Parse(tosAccepted);

                    agent.AcceptTOS = AcceptedNewTOS;
                    agentData.UpdateAgent(agent);
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
                    m_log.InfoFormat("[LLOGIN SERVICE]: Login failed, reason: login is blocked for user level {0}", account.UserLevel);
                    return LLFailedLoginResponse.LoginBlockedProblem;
                }

                if ((agent.Flags & IAgentFlags.PermBan) == IAgentFlags.PermBan)
                {
                    m_log.Info("[LLOGIN SERVICE]: Login failed, reason: user is permanently banned.");
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
                        m_log.Info(string.Format("[LLOGIN SERVICE]: Login failed, reason: user is temporarily banned {0}.", until));
                        return new LLFailedLoginResponse(LoginResponseEnum.MessagePopup, string.Format("You are blocked from connecting to this service{0}.", until), false);
                    }
                }
            }
            return null;
        }

        public LoginResponse Login(string firstName, string lastName, string passwd, string startLocation, UUID scopeID,
            string clientVersion, string channel, string mac, string id0, IPEndPoint clientIP, Hashtable requestData, UUID secureSession)
        {
            UUID session = UUID.Random();

            m_log.InfoFormat("[LLOGIN SERVICE]: Login request for {0} {1} from {2} with user agent {3} starting in {4}",
                firstName, lastName, clientIP.Address.ToString(), clientVersion, startLocation);
            try
            {
                UserAccount account = m_UserAccountService.GetUserAccount(scopeID, firstName, lastName);
                IAgentInfo agent = null;

                IAgentConnector agentData = DataManager.RequestPlugin<IAgentConnector>();
                IProfileConnector profileData = DataManager.RequestPlugin<IProfileConnector>();
                if (agentData != null)
                    agent = agentData.GetAgent(account.PrincipalID);

                requestData["ip"] = clientIP.ToString();
                foreach (ILoginModule module in LoginModules)
                {
                    string message;
                    if (module.Login(requestData, account.PrincipalID, out message) == false)
                    {
                        LLFailedLoginResponse resp = new LLFailedLoginResponse(LoginResponseEnum.PasswordIncorrect,
                            message, false);
                        return resp;
                    }
                }
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
                        archiver.LoadAvatarArchive(archiveName, account.FirstName, account.LastName);
                        UPI.AArchiveName = "";
                    }
                    if (UPI.IsNewUser)
                    {
                        UPI.IsNewUser = false;
                        profileData.UpdateUserProfile(UPI);
                    }
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

                // Get active gestures
                List<InventoryItemBase> gestures = m_InventoryService.GetActiveGestures(account.PrincipalID);
                m_log.DebugFormat("[LLOGIN SERVICE]: {0} active gestures", gestures.Count);

                //
                // Clear out any existing CAPS the user may have
                //
                if (m_CapsService != null)
                {
                    m_CapsService.RemoveCAPS(account.PrincipalID);
                }

                //
                // Change Online status and get the home region
                //
                GridRegion home = null;
                UserInfo guinfo = m_agentInfoService.GetUserInfo(account.PrincipalID.ToString());
                m_agentInfoService.SetLoggedIn(account.PrincipalID.ToString(), true);
                if (guinfo != null && (guinfo.HomeRegionID != UUID.Zero) && m_GridService != null)
                {
                    home = m_GridService.GetRegionByUUID(scopeID, guinfo.HomeRegionID);
                }
                bool GridUserInfoFound = true;
                if (guinfo == null)
                {
                    GridUserInfoFound = false;
                    // something went wrong, make something up, so that we don't have to test this anywhere else
                    guinfo = new UserInfo();
                    guinfo.CurrentPosition = guinfo.HomePosition = new Vector3(128, 128, 30);
                }

                //
                // Find the destination region/grid
                //
                string where = string.Empty;
                Vector3 position = Vector3.Zero;
                Vector3 lookAt = Vector3.Zero;
                GridRegion destination = FindDestination(account, scopeID, guinfo, session, startLocation, home, out where, out position, out lookAt);
                if (destination == null)
                {
                    m_log.InfoFormat("[LLOGIN SERVICE]: Login failed, reason: destination not found");
                    return LLFailedLoginResponse.GridProblem;
                }

                if (!GridUserInfoFound || guinfo.HomeRegionID == UUID.Zero) //Give them a default home and last
                {
                    List<GridRegion> DefaultRegions = m_GridService.GetDefaultRegions(account.ScopeID);
                    GridRegion DefaultRegion = null;
                    if (DefaultRegions.Count == 0)
                        DefaultRegion = destination;
                    else
                        DefaultRegion = DefaultRegions[0];

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

                    //Z = 0 so that it fixes it on the region server and puts it on the ground
                    guinfo.CurrentPosition = guinfo.HomePosition = new Vector3(128, 128, 25);

                    guinfo.HomeLookAt = guinfo.CurrentLookAt = new Vector3(0, 0, 0);

                    m_agentInfoService.SetLastPosition(guinfo.UserID, guinfo.CurrentRegionID, guinfo.CurrentPosition, guinfo.CurrentLookAt);
                    m_agentInfoService.SetHomePosition(guinfo.UserID, guinfo.HomeRegionID, guinfo.HomePosition, guinfo.HomeLookAt);
                }

                //
                // Get the avatar
                //
                AvatarAppearance avappearance = new AvatarAppearance(account.PrincipalID);
                if (m_AvatarService != null)
                {
                    avappearance = m_AvatarService.GetAppearance(account.PrincipalID);
                    if (avappearance == null)
                    {
                        //Create an appearance for the user if one doesn't exist
                        if (m_DefaultUserAvatarArchive != "")
                        {
                            m_log.Error("[LLoginService]: Cannot find an appearance for user " + account.Name +
                                ", loading the default avatar from " + m_DefaultUserAvatarArchive + ".");
                            archiver.LoadAvatarArchive(m_DefaultUserAvatarArchive, firstName, lastName);
                        }
                        else
                        {
                            m_log.Error("[LLoginService]: Cannot find an appearance for user " + account.Name + ", setting to the default avatar.");
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
                                    m_log.Warn("Missing avatar appearance asset for user " + account.Name + " for item " + item.Value + ", asset should be " + item.Key + "!");
                                    messedUp = true;
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
                                m_log.Warn("Bad texture index for user " + account.Name + " for " + BakedTextureIndex + "!");
                                avappearance.Texture.FaceTextures[BakedTextureIndex] = avappearance.Texture.CreateFace(BakedTextureIndex);
                                avappearance.Texture.FaceTextures[BakedTextureIndex].TextureID = AppearanceManager.DEFAULT_AVATAR_TEXTURE;
                                m_AvatarService.SetAppearance(account.PrincipalID, avappearance);
                            }
                        }
                    }
                }

                //
                // Instantiate/get the simulation interface and launch an agent at the destination
                //
                string reason = string.Empty;
                AgentCircuitData aCircuit = LaunchAgentAtGrid(destination, account, avappearance, session, secureSession, position, where,
                    clientIP, out where, out reason, out destination);

                if (aCircuit == null)
                {
                    m_log.InfoFormat("[LLOGIN SERVICE]: Login failed, reason: {0}", reason);
                    return new LLFailedLoginResponse(LoginResponseEnum.PasswordIncorrect, reason, false);
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


                LLLoginResponse response = new LLLoginResponse(account, aCircuit, guinfo, destination, inventorySkel, friendsList, m_LibraryService,
                    where, startLocation, position, lookAt, gestures, m_WelcomeMessage, home, clientIP, MaxMaturity, MaturityRating, m_MapTileURL, m_SearchURL,
                    m_AllowFirstLife ? "Y" : "N", m_TutorialURL, eventCategories, classifiedCategories, FillOutSeedCap(aCircuit, destination, clientIP, account.PrincipalID), allowExportPermission, m_config);

                m_log.InfoFormat("[LLOGIN SERVICE]: All clear. Sending login response to client to login to region " + destination.RegionName + ", tried to login to " + startLocation + " at " + position.ToString() + ".");
                return response;
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[LLOGIN SERVICE]: Exception processing login for {0} {1}: {2}", firstName, lastName, e.ToString());
                return LLFailedLoginResponse.InternalError;
            }
        }

        protected string FillOutSeedCap(AgentCircuitData aCircuit, GridRegion destination, IPEndPoint ipepClient, UUID AgentID)
        {
            string capsSeedPath = String.Empty;
            string SimcapsSeedPath = String.Empty;

            #region IP Translation for NAT
            if (ipepClient != null)
            {
                SimcapsSeedPath
                    = "http://"
                      + NetworkUtil.GetHostFor(ipepClient.Address, destination.ExternalHostName)
                      + ":" + destination.HttpPort
                      + CapsUtil.GetCapsSeedPath(aCircuit.CapsPath);
            }
            else
            {
                SimcapsSeedPath
                    = destination.ServerURI
                      + CapsUtil.GetCapsSeedPath(aCircuit.CapsPath);
            }
            #endregion

            // Don't use the following!  It Fails for logging into any region not on the same port as the http server!
            // Kept here so it doesn't happen again!
            // response.SeedCapability = regionInfo.ServerURI + capsSeedPath;

            if(m_CapsService != null)
            {
                //Remove any previous users
                string CapsBase = CapsUtil.GetRandomCapsObjectPath();
                capsSeedPath = m_CapsService.CreateCAPS(AgentID, SimcapsSeedPath, CapsUtil.GetCapsSeedPath(CapsBase), destination.RegionHandle, true, aCircuit);
                m_log.Debug("[NewAgentConnection]: Adding Caps Url for grid" +
                     " @" + capsSeedPath + " calling URL " + SimcapsSeedPath + " for agent " + aCircuit.AgentID);
            }
            else
            {
                capsSeedPath = SimcapsSeedPath;
            }
            
            return capsSeedPath;
        }

        protected GridRegion FindDestination(UserAccount account, UUID scopeID, UserInfo pinfo, UUID sessionID, string startLocation, GridRegion home, out string where, out Vector3 position, out Vector3 lookAt)
        {
            m_log.DebugFormat("[LLOGIN SERVICE]: FindDestination for start location {0}", startLocation);

            where = "home";
            position = new Vector3(128, 128, 25);
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

                if (home == null)
                {
                    m_log.WarnFormat(
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
                    List<GridRegion> defaults = m_GridService.GetDefaultRegions(scopeID);
                    if (defaults != null && defaults.Count > 0)
                    {
                        region = defaults[0];
                        where = "safe";
                    }
                    else
                    {
                        m_log.WarnFormat("[LLOGIN SERVICE]: User {0} {1} does not have a valid home and this grid does not have default locations. Attempting to find random region",
                            account.FirstName, account.LastName);
                        defaults = m_GridService.GetRegionsByName(scopeID, "", 1);
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

                if (pinfo.CurrentRegionID.Equals(UUID.Zero) || (region = m_GridService.GetRegionByUUID(scopeID, pinfo.CurrentRegionID)) == null)
                {
                    List<GridRegion> defaults = m_GridService.GetDefaultRegions(scopeID);
                    if (defaults != null && defaults.Count > 0)
                    {
                        region = defaults[0];
                        where = "safe";
                    }
                    else
                    {
                        defaults = m_GridService.GetFallbackRegions(scopeID, 0, 0);
                        if (defaults != null && defaults.Count > 0)
                        {
                            region = defaults[0];
                            where = "safe";
                        }
                        else
                        {
                            defaults = m_GridService.GetSafeRegions(scopeID, 0, 0);
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
                if (uriMatch == null)
                {
                    m_log.InfoFormat("[LLLOGIN SERVICE]: Got Custom Login URI {0}, but can't process it", startLocation);
                    return null;
                }
                else
                {
                    position = new Vector3(float.Parse(uriMatch.Groups["x"].Value, Culture.NumberFormatInfo),
                                           float.Parse(uriMatch.Groups["y"].Value, Culture.NumberFormatInfo),
                                           float.Parse(uriMatch.Groups["z"].Value, Culture.NumberFormatInfo));

                    string regionName = uriMatch.Groups["region"].ToString();
                    if (regionName != null)
                    {
                        if (!regionName.Contains("@"))
                        {
                            List<GridRegion> regions = m_GridService.GetRegionsByName(scopeID, regionName, 1);
                            if ((regions == null) || (regions != null && regions.Count == 0))
                            {
                                m_log.InfoFormat("[LLLOGIN SERVICE]: Got Custom Login URI {0}, can't locate region {1}. Trying defaults.", startLocation, regionName);
                                regions = m_GridService.GetDefaultRegions(scopeID);
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
                           /*string[] parts = regionName.Split(new char[] { '@' });
                            if (parts.Length < 2)
                            {
                                m_log.InfoFormat("[LLLOGIN SERVICE]: Got Custom Login URI {0}, can't locate region {1}", startLocation, regionName);
                                return null;
                            }
                            // Valid specification of a remote grid

                            regionName = parts[0];
                            string domainLocator = parts[1];
                            parts = domainLocator.Split(new char[] { ':' });
                            string domainName = parts[0];
                            uint port = 0;
                            if (parts.Length > 1)
                                UInt32.TryParse(parts[1], out port);

                            return region;*/
                            return null;
                        }
                    }
                    else
                    {
                        List<GridRegion> defaults = m_GridService.GetDefaultRegions(scopeID);
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

        protected AgentCircuitData LaunchAgentAtGrid(GridRegion destination, UserAccount account, AvatarAppearance appearance,
            UUID session, UUID secureSession, Vector3 position, string currentWhere,
            IPEndPoint clientIP, out string where, out string reason, out GridRegion dest)
        {
            where = currentWhere;
            reason = string.Empty;
            uint circuitCode = 0;
            AgentCircuitData aCircuit = null;
            dest = destination;

            bool success = false;

            #region Launch Agent Directly (No HG)

            circuitCode = (uint)Util.RandomClass.Next(); ;
            aCircuit = MakeAgent(destination, account, appearance, session, secureSession, circuitCode, position, clientIP.Address.ToString());
            success = LaunchAgentDirectly(m_SimulationService, destination, aCircuit, out reason);
            if (!success && m_GridService != null)
            {
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
                        }
                    }
                }
            }

            #endregion

            if (success)
                return aCircuit;
            else
                return null;
        }

        protected bool TryFindGridRegionForAgentLogin(List<GridRegion> regions, UserAccount account,
            AvatarAppearance appearance, UUID session, UUID secureSession, uint circuitCode, Vector3 position,
            IPEndPoint clientIP, AgentCircuitData aCircuit, out GridRegion destination)
        {
            foreach (GridRegion r in regions)
            {
                string reason;
                bool success = LaunchAgentDirectly(m_SimulationService, r, aCircuit, out reason);
                if (success)
                {
                    aCircuit = MakeAgent(r, account, appearance, session, secureSession, circuitCode, position, clientIP.Address.ToString());
                    destination = r;
                    return true;
                }
                else
                    m_GridService.SetRegionUnsafe(r.RegionID);
            }
            destination = null;
            return false;
        }

        protected AgentCircuitData MakeAgent(GridRegion region, UserAccount account,
            AvatarAppearance appearance, UUID session, UUID secureSession, uint circuit, Vector3 position,
            string ipaddress)
        {
            AgentCircuitData aCircuit = new AgentCircuitData();

            aCircuit.AgentID = account.PrincipalID;
            if (appearance != null)
                aCircuit.Appearance = appearance;
            else
            {
                m_log.Warn("Could not find appearance for acircuit!");
                aCircuit.Appearance = new AvatarAppearance(account.PrincipalID);
            }

            aCircuit.CapsPath = CapsUtil.GetRandomCapsObjectPath();
            aCircuit.child = false; // the first login agent is root
            aCircuit.circuitcode = circuit;
            aCircuit.SecureSessionID = secureSession;
            aCircuit.SessionID = session;
            aCircuit.startpos = position;
            aCircuit.IPAddress = ipaddress;

            return aCircuit;

            //m_UserAgentService.LoginAgentToGrid(aCircuit, GatekeeperServiceConnector, region, out reason);
            //if (simConnector.CreateAgent(region, aCircuit, 0, out reason))
            //    return aCircuit;

            //return null;

        }

        protected bool LaunchAgentDirectly(ISimulationService simConnector, GridRegion region, AgentCircuitData aCircuit, out string reason)
        {
            // The client is in the region, we need to make sure it gets the right Caps
            // If CreateAgent is successful, it passes back a OSDMap of params that the client 
            //    wants to inform us about, and it includes the Caps SEED url for the region
            IRegionClientCapsService regionClientCaps = null;
            if (m_CapsService != null)
            {
                //Remove any previous users
                string ServerCapsBase = CapsUtil.GetRandomCapsObjectPath();
                string ServerCapsSeedPath = m_CapsService.CreateCAPS(aCircuit.AgentID, "", CapsUtil.GetCapsSeedPath(ServerCapsBase), region.RegionHandle, true, aCircuit);

                regionClientCaps = m_CapsService.GetClientCapsService(aCircuit.AgentID).GetCapsService(region.RegionHandle);
            }

            // As we are creating the agent, we must also initialize the CapsService for the agent
            bool success = simConnector.CreateAgent(region, aCircuit, (int)TeleportFlags.ViaLogin, null, out reason);
            if (!success) // If it failed, do not set up any CapsService for the client
            {
                //Delete the Caps!
                m_CapsService.RemoveCAPS(aCircuit.AgentID);
                return success;
            }

            if (m_CapsService != null && reason != "")
            {
                OSDMap responseMap = (OSDMap)OSDParser.DeserializeJson(reason);
                string SimcapsSeedPath = responseMap["CapsUrl"].AsString();
                regionClientCaps.AddSEEDCap("", SimcapsSeedPath);
                m_log.Info("[NewAgentConnection]: Adding Caps Url for grid" +
                     " @" + regionClientCaps.CapsUrl + " calling URL " + SimcapsSeedPath +
                     " for agent " + aCircuit.AgentID);
            }

            return success;
        }

        #region Console Commands

        protected void RegisterCommands()
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

        protected void HandleLoginCommand(string module, string[] cmd)
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

namespace AvatarArchives
{
    using OpenMetaverse.StructuredData;
    public class GridAvatarArchiver : IAvatarAppearanceArchiver
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IUserAccountService UserAccountService;
        private IAvatarService AvatarService;
        private IInventoryService InventoryService;
        private IAssetService AssetService;

        public GridAvatarArchiver(IUserAccountService ACS, IAvatarService AS, IInventoryService IS, IAssetService AService)
        {
            UserAccountService = ACS;
            AvatarService = AS;
            InventoryService = IS;
            AssetService = AService;
            MainConsole.Instance.Commands.AddCommand("region", false, "save avatar archive", "save avatar archive <First> <Last> <Filename> <FolderNameToSaveInto> [<Snapshot UUID>] [<Public>]", "Saves appearance to an avatar archive archive (Note: put \"\" around the FolderName if you need more than one word. To save to the database ensure the name ends with .database. Snapshot is a uuid of the a snapshot of this archive. IsPublic should be a 0 or 1. IsPublic and Snapshot are only saved when saving to the database. )", HandleSaveAvatarArchive);
            MainConsole.Instance.Commands.AddCommand("region", false, "load avatar archive", "load avatar archive <First> <Last> <Filename>", "Loads appearance from an avatar archive archive", HandleLoadAvatarArchive);
        }

        protected void HandleLoadAvatarArchive(string module, string[] cmdparams)
        {
            if (cmdparams.Length != 6)
            {
                m_log.Info("[AvatarArchive] Not enough parameters!");
                return;
            }
            LoadAvatarArchive(cmdparams[5], cmdparams[3], cmdparams[4]);
        }

        public void LoadAvatarArchive(string FileName, string First, string Last)
        {
            UserAccount account = UserAccountService.GetUserAccount(UUID.Zero, First, Last);
            m_log.Info("[AvatarArchive] Loading archive from " + FileName);
            if (account == null)
            {
                m_log.Error("[AvatarArchive] User not found!");
                return;
            }
            string file = "";
            if (FileName.EndsWith(".database"))
            {
                m_log.Info("[AvatarArchive] Loading archive from the database " + FileName);

                FileName = FileName.Substring(0, FileName.Length - 9);

                Aurora.Framework.IAvatarArchiverConnector avarchiver = DataManager.RequestPlugin<IAvatarArchiverConnector>();
                AvatarArchive archive = avarchiver.GetAvatarArchive(FileName);

                file = archive.ArchiveXML;
            }
            else
            {
                m_log.Info("[AvatarArchive] Loading archive from " + FileName);
                StreamReader reader = new StreamReader(FileName);
                file = reader.ReadToEnd();
                reader.Close();
                reader.Dispose();
            }

            string FolderNameToLoadInto = "";

            OSD m = OSDParser.DeserializeLLSDXml(file);
            if (m.Type != OSDType.Map)
            {
                m_log.Warn("[AvatarArchiver]: Failed to load AA from " + FileName + ", text: " + file);
                return;
            }
            OSDMap map = ((OSDMap)m);

            OSDMap assetsMap = ((OSDMap)map["Assets"]);
            OSDMap itemsMap = ((OSDMap)map["Items"]);
            OSDMap bodyMap = ((OSDMap)map["Body"]);

            AvatarAppearance appearance = ConvertXMLToAvatarAppearance(bodyMap, out FolderNameToLoadInto);

            appearance.Owner = account.PrincipalID;

            List<InventoryItemBase> items = new List<InventoryItemBase>();

            InventoryFolderBase AppearanceFolder = InventoryService.GetFolderForType(account.PrincipalID, AssetType.Clothing);

            InventoryFolderBase folderForAppearance
                = new InventoryFolderBase(
                    UUID.Random(), FolderNameToLoadInto, account.PrincipalID,
                    -1, AppearanceFolder.ID, 1);

            InventoryService.AddFolder(folderForAppearance);

            folderForAppearance = InventoryService.GetFolder(folderForAppearance);

            try
            {
                LoadAssets(assetsMap, appearance);
                LoadItems(itemsMap, account.PrincipalID, folderForAppearance, appearance, out items);
            }
            catch (Exception ex)
            {
                m_log.Warn("[AvatarArchiver]: Error loading assets and items, " + ex.ToString());
            }

            AvatarData adata = new AvatarData(appearance);
            AvatarService.SetAvatar(account.PrincipalID, adata);

            m_log.Info("[AvatarArchive] Loaded archive from " + FileName);
        }

        private InventoryItemBase GiveInventoryItem(UUID senderId, UUID recipient, InventoryItemBase item, InventoryFolderBase parentFolder)
        {
            InventoryItemBase itemCopy = new InventoryItemBase();
            itemCopy.Owner = recipient;
            itemCopy.CreatorId = item.CreatorId;
            itemCopy.ID = UUID.Random();
            itemCopy.AssetID = item.AssetID;
            itemCopy.Description = item.Description;
            itemCopy.Name = item.Name;
            itemCopy.AssetType = item.AssetType;
            itemCopy.InvType = item.InvType;
            itemCopy.Folder = UUID.Zero;

            //Give full permissions for them
            itemCopy.NextPermissions = (uint)PermissionMask.All;
            itemCopy.GroupPermissions = (uint)PermissionMask.All;
            itemCopy.EveryOnePermissions = (uint)PermissionMask.All;
            itemCopy.CurrentPermissions = (uint)PermissionMask.All;

            if (parentFolder == null)
            {
                InventoryFolderBase folder = InventoryService.GetFolderForType(recipient, (AssetType)itemCopy.AssetType);

                if (folder != null)
                    itemCopy.Folder = folder.ID;
                else
                {
                    InventoryFolderBase root = InventoryService.GetRootFolder(recipient);

                    if (root != null)
                        itemCopy.Folder = root.ID;
                    else
                        return null; // No destination
                }
            }
            else
                itemCopy.Folder = parentFolder.ID; //We already have a folder to put it in

            itemCopy.GroupID = UUID.Zero;
            itemCopy.GroupOwned = false;
            itemCopy.Flags = item.Flags;
            itemCopy.SalePrice = item.SalePrice;
            itemCopy.SaleType = item.SaleType;

            InventoryService.AddItem(itemCopy);
            return itemCopy;
        }

        private AvatarAppearance ConvertXMLToAvatarAppearance(OSDMap map, out string FolderNameToPlaceAppearanceIn)
        {
            AvatarAppearance appearance = new AvatarAppearance();

            appearance.Unpack(map);
            FolderNameToPlaceAppearanceIn = map["FolderName"].AsString();
            return appearance;
        }

        protected void HandleSaveAvatarArchive(string module, string[] cmdparams)
        {
            if (cmdparams.Length < 7)
            {
                m_log.Info("[AvatarArchive] Not enough parameters!");
                return;
            }
            UserAccount account = UserAccountService.GetUserAccount(UUID.Zero, cmdparams[3], cmdparams[4]);
            if (account == null)
            {
                m_log.Error("[AvatarArchive] User not found!");
                return;
            }

            AvatarAppearance appearance = AvatarService.GetAppearance(account.PrincipalID);
            OSDMap map = new OSDMap();
            OSDMap body = new OSDMap();
            OSDMap assets = new OSDMap();
            OSDMap items = new OSDMap();
            body = appearance.Pack();
            body.Add("FolderName", OSD.FromString(cmdparams[6]));

            try
            {
                foreach (AvatarWearable wear in appearance.Wearables)
                {
                    for (int i = 0; i < wear.Count; i++)
                    {
                        WearableItem w = wear[i];
                        if (w.AssetID != UUID.Zero)
                        {
                            SaveItem(w.ItemID, items, assets);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                m_log.Warn("Excpetion: " + ex.ToString());
            }
            try
            {
                List<AvatarAttachment> attachments = appearance.GetAttachments();
                foreach (AvatarAttachment a in attachments)
                {
                    if (a.AssetID != UUID.Zero)
                    {
                        SaveItem(a.ItemID, items, assets); 
                    }
                }
            }
            catch(Exception ex)
            {
                m_log.Warn("Excpetion: " + ex.ToString());
            }

            map.Add("Body", body);
            map.Add("Assets", assets);
            map.Add("Items", items);
            //Write the map

            if (cmdparams[5].EndsWith(".database"))
            {
                //Remove the .database
                string ArchiveName = cmdparams[5].Substring(0, cmdparams[5].Length - 9);
                string ArchiveXML = OSDParser.SerializeLLSDXmlString(map);

                AvatarArchive archive = new AvatarArchive();
                archive.ArchiveXML = ArchiveXML;
                archive.Name = ArchiveName;

                if ((cmdparams.Length >= 8) && (cmdparams[7].Length == 36)) archive.Snapshot = cmdparams[7];
                if ((cmdparams.Length >= 9) && (cmdparams[8].Length == 1)) archive.IsPublic = int.Parse(cmdparams[8]);
				
                DataManager.RequestPlugin<IAvatarArchiverConnector>().SaveAvatarArchive(archive);

                m_log.Info("[AvatarArchive] Saved archive to database " + cmdparams[5]);
            }
            else
            {
                m_log.Info("[AvatarArchive] Saving archive to " + cmdparams[5]);
                StreamWriter writer = new StreamWriter(cmdparams[5], false);
                writer.Write(OSDParser.SerializeLLSDXmlString(map));
                writer.Close();
                writer.Dispose();
                m_log.Info("[AvatarArchive] Saved archive to " + cmdparams[5]);
            }
        }

        private void SaveAsset(UUID AssetID, OSDMap assetMap)
        {
            AssetBase asset = AssetService.Get(AssetID.ToString());
            if (asset != null && AssetID != UUID.Zero)
            {
                OSDMap assetData = new OSDMap();
                m_log.Info("[AvatarArchive]: Saving asset " + asset.ID);
                CreateMetaDataMap(asset.Metadata, assetData);
                assetData.Add("AssetData", OSD.FromBinary(asset.Data));
                assetMap[asset.ID] = assetData;
            }
            else
            {
                m_log.Warn("[AvatarArchive]: Could not find asset to save: " + AssetID.ToString());
                return;
            }
        }

        private void CreateMetaDataMap(AssetMetadata data, OSDMap map)
        {
            map["ContentType"] = OSD.FromString(data.ContentType);
            map["CreationDate"] = OSD.FromDate(data.CreationDate);
            map["CreatorID"] = OSD.FromString(data.CreatorID);
            map["Description"] = OSD.FromString(data.Description);
            map["ID"] = OSD.FromString(data.ID);
            map["Name"] = OSD.FromString(data.Name);
            map["Type"] = OSD.FromInteger(data.Type);
        }

        private AssetBase LoadAssetBase(OSDMap map)
        {
            AssetBase asset = new AssetBase();
            asset.Data = map["AssetData"].AsBinary();

            AssetMetadata md = new AssetMetadata();
            md.ContentType = map["ContentType"].AsString();
            md.CreationDate = map["CreationDate"].AsDate();
            md.CreatorID = map["CreatorID"].AsString();
            md.Description = map["Description"].AsString();
            md.ID = map["ID"].AsString();
            md.Name = map["Name"].AsString();
            md.Type = (sbyte)map["Type"].AsInteger();

            asset.Metadata = md;
            asset.ID = md.ID;
            asset.FullID = UUID.Parse(md.ID);
            asset.Name = md.Name;
            asset.Type = md.Type;

            return asset;
        }

        private void SaveItem(UUID ItemID, OSDMap itemMap, OSDMap assets)
        {
            InventoryItemBase saveItem = InventoryService.GetItem(new InventoryItemBase(ItemID));
            if (saveItem == null)
            {
                m_log.Warn("[AvatarArchive]: Could not find item to save: " + ItemID.ToString());
                return;
            }
            m_log.Info("[AvatarArchive]: Saving item " + ItemID.ToString());
            string serialization = OpenSim.Framework.Serialization.External.UserInventoryItemSerializer.Serialize(saveItem);
            itemMap[ItemID.ToString()] = OSD.FromString(serialization);

            SaveAsset(saveItem.AssetID, assets);
        }

        private void LoadAssets(OSDMap assets, AvatarAppearance appearance)
        {
            foreach (KeyValuePair<string, OSD> kvp in assets)
            {
                UUID AssetID = UUID.Parse(kvp.Key);
                OSDMap assetMap = (OSDMap)kvp.Value;
                AssetBase asset = AssetService.Get(AssetID.ToString());
                m_log.Info("[AvatarArchive]: Loading asset " + AssetID.ToString());
                if (asset == null) //Don't overwrite
                {
                    asset = LoadAssetBase(assetMap);
                    UUID oldassetID = UUID.Parse(asset.ID);
                    UUID newAssetID = UUID.Parse(AssetService.Store(asset));
                    //Fix the IDs
                    AvatarWearable[] wearables = new AvatarWearable[appearance.Wearables.Length];
                    appearance.Wearables.CopyTo(wearables, 0);
                    for (int a = 0; a < wearables.Length; a++)
                    {
                        for (int i = 0; i < wearables[a].Count; i++)
                        {
                            if (wearables[a][i].AssetID == oldassetID)
                            {
                                //Fix the ItemID
                                AvatarWearable w = wearables[a];
                                UUID itemID = w.GetItem(oldassetID);
                                w.RemoveItem(oldassetID);
                                w.Add(itemID, newAssetID);
                                appearance.SetWearable(a, w);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void LoadItems(OSDMap items, UUID OwnerID, InventoryFolderBase folderForAppearance, AvatarAppearance appearance, out List<InventoryItemBase> litems)
        {
            litems = new List<InventoryItemBase>();
            foreach (KeyValuePair<string, OSD> kvp in items)
            {
                string serialization = kvp.Value.AsString();
                InventoryItemBase item = OpenSim.Framework.Serialization.External.UserInventoryItemSerializer.Deserialize(serialization);
                m_log.Info("[AvatarArchive]: Loading item " + item.ID.ToString());
                UUID oldID = item.ID;
                item = GiveInventoryItem(item.CreatorIdAsUuid, OwnerID, item, folderForAppearance);
                litems.Add(item);
                FixItemIDs(oldID, item, appearance);
            }
        }

        private void FixItemIDs(UUID oldID, InventoryItemBase item, AvatarAppearance appearance)
        {
            //Fix the IDs
            AvatarWearable[] wearables = new AvatarWearable[appearance.Wearables.Length];
            appearance.Wearables.CopyTo(wearables, 0);
            for (int a = 0; a < wearables.Length; a++)
            {
                for (int i = 0; i < wearables[a].Count; i++)
                {
                    if (wearables[a][i].ItemID == oldID)
                    {
                        //Fix the ItemID
                        AvatarWearable w = new AvatarWearable();
                        w.Unpack((OSDArray)wearables[a].Pack());
                        UUID assetID = w.GetAsset(oldID);
                        w.RemoveItem(oldID);
                        w.Add(item.ID, assetID);
                        appearance.SetWearable(a, w);
                        return;
                    }
                }
            }
        }
    }

    public class GridAvatarProfileArchiver
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IUserAccountService UserAccountService;
        public GridAvatarProfileArchiver(IUserAccountService UAS)
        {
            UserAccountService = UAS;
            MainConsole.Instance.Commands.AddCommand("region", false, "save avatar profile",
                                          "save avatar profile <First> <Last> <Filename>",
                                          "Saves profile and avatar data to an archive", HandleSaveAvatarProfile);
            MainConsole.Instance.Commands.AddCommand("region", false, "load avatar profile",
                                          "load avatar profile <First> <Last> <Filename>",
                                          "Loads profile and avatar data from an archive", HandleLoadAvatarProfile);
        }

        protected void HandleLoadAvatarProfile(string module, string[] cmdparams)
        {
            if (cmdparams.Length != 6)
            {
                m_log.Info("[AvatarProfileArchiver] Not enough parameters!");
                return;
            }
            StreamReader reader = new StreamReader(cmdparams[5]);
            string document = reader.ReadToEnd();
            reader.Close();
            reader.Dispose();

            string[] lines = document.Split('\n');
            List<string> file = new List<string>(lines);
            Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(file[1]);

            Dictionary<string, object> results = replyData["result"] as Dictionary<string, object>;
            UserAccount UDA = new UserAccount();
            UDA.FirstName = cmdparams[3];
            UDA.LastName = cmdparams[4];
            UDA.PrincipalID = UUID.Random();
            UDA.ScopeID = UUID.Zero;
            UDA.UserFlags = int.Parse(results["UserFlags"].ToString());
            UDA.UserLevel = 0; //For security... Don't want everyone loading full god mode.
            UDA.UserTitle = results["UserTitle"].ToString();
            UDA.Email = results["Email"].ToString();
            UDA.Created = int.Parse(results["Created"].ToString());
            if (results.ContainsKey("ServiceURLs") && results["ServiceURLs"] != null)
            {
                UDA.ServiceURLs = new Dictionary<string, object>();
                string str = results["ServiceURLs"].ToString();
                if (str != string.Empty)
                {
                    string[] parts = str.Split(new char[] { ';' });
                    Dictionary<string, object> dic = new Dictionary<string, object>();
                    foreach (string s in parts)
                    {
                        string[] parts2 = s.Split(new char[] { '*' });
                        if (parts2.Length == 2)
                            UDA.ServiceURLs[parts2[0]] = parts2[1];
                    }
                }
            }
            UserAccountService.StoreUserAccount(UDA);

            replyData = WebUtils.ParseXmlResponse(file[2]);
            IUserProfileInfo UPI = new IUserProfileInfo();
            UPI.FromKVP(replyData["result"] as Dictionary<string, object>);
            //Update the principle ID to the new user.
            UPI.PrincipalID = UDA.PrincipalID;

            IProfileConnector profileData = DataManager.RequestPlugin<IProfileConnector>();
            if (profileData.GetUserProfile(UPI.PrincipalID) == null)
                profileData.CreateNewProfile(UPI.PrincipalID);

            profileData.UpdateUserProfile(UPI);

            m_log.Info("[AvatarProfileArchiver] Loaded Avatar Profile from " + cmdparams[5]);
        }
        protected void HandleSaveAvatarProfile(string module, string[] cmdparams)
        {
            if (cmdparams.Length != 6)
            {
                m_log.Info("[AvatarProfileArchiver] Not enough parameters!");
                return;
            }
            UserAccount account = UserAccountService.GetUserAccount(UUID.Zero, cmdparams[3], cmdparams[4]);
            if (account == null)
            {
                m_log.Info("Account could not be found, stopping now.");
                return;
            }
            IProfileConnector data = DataManager.RequestPlugin<IProfileConnector>();
            string UPIxmlString = "";
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (data != null)
            {
                IUserProfileInfo profile = data.GetUserProfile(account.PrincipalID);
                if (profile != null)
                {
                    result["result"] = profile.ToKeyValuePairs();
                    UPIxmlString = WebUtils.BuildXmlResponse(result);
                }
            }

            result["result"] = account.ToKeyValuePairs();
            string UDAxmlString = WebUtils.BuildXmlResponse(result);

            StreamWriter writer = new StreamWriter(cmdparams[5]);
            writer.Write("<profile>\n");
            writer.Write(UDAxmlString + "\n");
            writer.Write(UPIxmlString + "\n");
            writer.Write("</profile>\n");
            writer.Close();
            writer.Dispose();
            m_log.Info("[AvatarProfileArchiver] Saved Avatar Profile to " + cmdparams[5]);
        }
    }
}