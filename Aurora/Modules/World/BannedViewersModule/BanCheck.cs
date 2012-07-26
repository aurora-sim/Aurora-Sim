/*
 * Copyright 2011 Matthew Beardmore
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
using System.Net;
using System.Reflection;
using System.Text;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Nini.Config;
using Aurora.DataManager;
using OpenMetaverse;
using OpenSim.Services.Interfaces;
using OpenSim.Services.LLLoginService;

namespace Aurora.Modules.Ban
{
    #region Grid BanCheck

    public class LoginBanCheck : ILoginModule
    {
        #region Declares

        BanCheck m_module;

        public string Name
        {
            get { return GetType().Name; }
        }

        #endregion

        #region ILoginModule Members

        public void Initialize(ILoginService service, IConfigSource source, IRegistryCore registry)
        {
            m_module = new BanCheck(source, registry.RequestModuleInterface<IUserAccountService>());
        }

        public LoginResponse Login(Hashtable request, UserAccount account, IAgentInfo agentInfo, string authType, string password, out object data)
        {
            data = null;

            string ip = "";
            string version = "";
            string platform = "";
            string mac = "";
            string id0 = "";

            if (request != null)
            {
                ip = request.ContainsKey("ip") ? (string)request["ip"] : "";
                version = request.ContainsKey("version") ? (string)request["version"] : "";
                platform = request.ContainsKey("platform") ? (string)request["platform"] : "";
                mac = request.ContainsKey("mac") ? (string)request["mac"] : "";
                id0 = request.ContainsKey("id0") ? (string)request["id0"] : "";
            }

            string message;
            if(!m_module.CheckUser(account.PrincipalID, ip,
                version,
                platform,
                mac,
                id0, out message))
            {
                return new LLFailedLoginResponse(LoginResponseEnum.Indeterminant, message, false);
            }
            return null;
        }

        #endregion
    }

    #endregion

    #region BanCheck base

    public class BanCheck
    {
        #region Declares

        private IPresenceInfo presenceInfo = null;

        private AllowLevel GrieferAllowLevel = AllowLevel.AllowCleanOnly;
        private IUserAccountService m_accountService = null;
        private List<string> m_bannedViewers = new List<string>();
        private List<string> m_allowedViewers = new List<string>();
        private bool m_useIncludeList = false;
        private bool m_debug = false;
        private bool m_checkOnLogin = false;
        private bool m_checkOnTimer = true;
        private bool m_enabled = false;

        private Aurora.Framework.ListCombiningTimedSaving<PresenceInfo> _checkForSimilaritiesLater = new ListCombiningTimedSaving<PresenceInfo>();

        #endregion

        #region Enums

        public enum AllowLevel : int
        {
            AllowCleanOnly = 0,
            AllowSuspected = 1,
            AllowKnown = 2
        }

        #endregion

        #region Constructor

        public BanCheck (IConfigSource source, IUserAccountService UserAccountService)
        {
            IConfig config = source.Configs["GrieferProtection"];
            if (config == null)
                return;

            m_enabled = config.GetBoolean ("Enabled", true);

            if (!m_enabled)
                return;

            string bannedViewers = config.GetString("ViewersToBan", "");
            m_bannedViewers = Util.ConvertToList(bannedViewers);
            string allowedViewers = config.GetString("ViewersToAllow", "");
            m_allowedViewers = Util.ConvertToList(allowedViewers);
            m_useIncludeList = config.GetBoolean("UseAllowListInsteadOfBanList", false);

            m_checkOnLogin = config.GetBoolean ("CheckForSimilaritiesOnLogin", m_checkOnLogin);
            m_checkOnTimer = config.GetBoolean ("CheckForSimilaritiesOnTimer", m_checkOnTimer);

            if (m_checkOnTimer)
                _checkForSimilaritiesLater.Start(5, CheckForSimilaritiesMultiple);

            GrieferAllowLevel = (AllowLevel)Enum.Parse (typeof (AllowLevel), config.GetString ("GrieferAllowLevel", "AllowKnown"));

            presenceInfo = Aurora.DataManager.DataManager.RequestPlugin<IPresenceInfo> ();
            m_accountService = UserAccountService;

            if (MainConsole.Instance != null)
            {
                MainConsole.Instance.Commands.AddCommand(
                    "show user info", "show user info [UUID] or [First] [Last]", "Info on a given user", UserInfo);
                MainConsole.Instance.Commands.AddCommand(
                    "set user info", "set user info [UUID] or [First] [Last] [Flags]", "Sets the info of the given user [Flags]: Clean, Suspected, Known, Banned", SetUserInfo);
                MainConsole.Instance.Commands.AddCommand(
                    "block user", "block user [UUID] or [Name]", "Blocks a given user from connecting anymore", BlockUser);
                MainConsole.Instance.Commands.AddCommand(
                    "ban user", "ban user [UUID] or [Name]", "Blocks a given user from connecting anymore", BlockUser);
                MainConsole.Instance.Commands.AddCommand(
                    "unblock user", "unblock user [UUID] or [Name]", "Removes the block for logging in on a given user", UnBlockUser);
                MainConsole.Instance.Commands.AddCommand(
                    "unban user", "unban user [UUID] or [Name]", "Removes the block for logging in on a given user", UnBlockUser);
            }
        }

        #endregion

        #region Private and Protected members

        private void CheckForSimilaritiesMultiple(UUID agentID, List<PresenceInfo> info)
        {
            foreach(PresenceInfo i in info)
                presenceInfo.Check(i, m_useIncludeList ? m_allowedViewers : m_bannedViewers, m_useIncludeList);
        }

        private void CheckForSimilarities(PresenceInfo info)
        {
            presenceInfo.Check(info, m_useIncludeList ? m_allowedViewers : m_bannedViewers, m_useIncludeList);
        }

        private PresenceInfo UpdatePresenceInfo(UUID AgentID, PresenceInfo oldInfo, string ip, string version, string platform, string mac, string id0)
        {
            PresenceInfo info = new PresenceInfo();
            info.AgentID = AgentID;
            if(!string.IsNullOrEmpty(ip))
                info.LastKnownIP = ip;
            if (!string.IsNullOrEmpty(version))
                info.LastKnownViewer = version;
            if (!string.IsNullOrEmpty(platform))
                info.Platform = platform;
            if (!string.IsNullOrEmpty(mac))
                info.LastKnownMac = mac;
            if (!string.IsNullOrEmpty(id0))
                info.LastKnownID0 = id0;

            if (!oldInfo.KnownID0s.Contains(info.LastKnownID0))
                oldInfo.KnownID0s.Add(info.LastKnownID0);
            if (!oldInfo.KnownIPs.Contains(info.LastKnownIP))
                oldInfo.KnownIPs.Add(info.LastKnownIP);
            if (!oldInfo.KnownMacs.Contains(info.LastKnownMac))
                oldInfo.KnownMacs.Add(info.LastKnownMac);
            if (!oldInfo.KnownViewers.Contains(info.LastKnownViewer))
                oldInfo.KnownViewers.Add(info.LastKnownViewer);

            info.KnownViewers = oldInfo.KnownViewers;
            info.KnownMacs = oldInfo.KnownMacs;
            info.KnownIPs = oldInfo.KnownIPs;
            info.KnownID0s = oldInfo.KnownID0s;
            info.KnownAlts = oldInfo.KnownAlts;

            info.Flags = oldInfo.Flags;

            presenceInfo.UpdatePresenceInfo(info);

            return info;
        }

        private PresenceInfo GetInformation(UUID AgentID)
        {
            PresenceInfo oldInfo = presenceInfo.GetPresenceInfo(AgentID);
            if (oldInfo == null)
            {
                PresenceInfo info = new PresenceInfo();
                info.AgentID = AgentID;
                info.Flags = PresenceInfo.PresenceInfoFlags.Clean;
                presenceInfo.UpdatePresenceInfo(info);
                oldInfo = presenceInfo.GetPresenceInfo(AgentID);
            }

            return oldInfo;
        }

        protected void UserInfo(string[] cmdparams)
        {
            if (cmdparams.Length < 4)
            {
                MainConsole.Instance.Info("Wrong number of parameters for show user info");
                return;
            }
            UUID AgentID;
            PresenceInfo info;
            if (!UUID.TryParse(cmdparams[3], out AgentID))
            {
                UserAccount account = m_accountService.GetUserAccount(null, Util.CombineParams(cmdparams, 3));
                if (account == null)
                {
                    MainConsole.Instance.Warn("Cannot find user.");
                    return;
                }
                AgentID = account.PrincipalID;
            }
            info = GetInformation(AgentID);
            if (info == null)
            {
                MainConsole.Instance.Warn("Cannot find user.");
                return;
            }
            DisplayUserInfo(info);
        }

        protected void BlockUser(string[] cmdparams)
        {
            if (cmdparams.Length < 3)
            {
                MainConsole.Instance.Info("Wrong number of parameters for block user");
                return;
            }
            UUID AgentID;
            PresenceInfo info;
            if (!UUID.TryParse(cmdparams[2], out AgentID))
            {
                UserAccount account = m_accountService.GetUserAccount(null, Util.CombineParams(cmdparams, 2));
                if (account == null)
                {
                    MainConsole.Instance.Warn("Cannot find user.");
                    return;
                }
                AgentID = account.PrincipalID;
            }
            info = GetInformation(AgentID);
            if (info == null)
            {
                MainConsole.Instance.Warn("Cannot find user.");
                return;
            }

            if (MainConsole.Instance.Prompt("Do you want to have this only be a temporary ban?", "no", new List<string>() { "yes", "no" }).ToLower() == "yes")
            {
                var conn = Aurora.DataManager.DataManager.RequestPlugin<IAgentConnector>();
                IAgentInfo agentInfo = conn.GetAgent(AgentID);
                float days = float.Parse(MainConsole.Instance.Prompt("How long (in days) should this ban last?", "5.0"));

                agentInfo.Flags |= IAgentFlags.TempBan;

                agentInfo.OtherAgentInformation["TemperaryBanInfo"] = DateTime.Now.AddDays(days);

                conn.UpdateAgent(agentInfo);
            }
            else
            {
                info.Flags |= PresenceInfo.PresenceInfoFlags.Banned;
                presenceInfo.UpdatePresenceInfo(info);
            }
            MainConsole.Instance.Fatal("User blocked from logging in");
        }

        protected void UnBlockUser(string[] cmdparams)
        {
            if (cmdparams.Length < 3)
            {
                MainConsole.Instance.Info("Wrong number of parameters for block user");
                return;
            }
            UUID AgentID;
            PresenceInfo info;
            if (!UUID.TryParse(cmdparams[2], out AgentID))
            {
                UserAccount account = m_accountService.GetUserAccount(null, Util.CombineParams(cmdparams, 2));
                if (account == null)
                {
                    MainConsole.Instance.Warn("Cannot find user.");
                    return;
                }
                AgentID = account.PrincipalID;
            }
            info = GetInformation(AgentID);
            if (info == null)
            {
                MainConsole.Instance.Warn("Cannot find user.");
                return;
            }
            info.Flags = PresenceInfo.PresenceInfoFlags.Clean;
            presenceInfo.UpdatePresenceInfo(info);

            var conn = Aurora.DataManager.DataManager.RequestPlugin<IAgentConnector>();
            IAgentInfo agentInfo = conn.GetAgent(AgentID);

            agentInfo.Flags &= IAgentFlags.TempBan;
            agentInfo.Flags &= IAgentFlags.PermBan;
            conn.UpdateAgent(agentInfo);

            MainConsole.Instance.Fatal("User block removed");
        }

        protected void SetUserInfo(string[] cmdparams)
        {
            if (cmdparams.Length < 5)
            {
                MainConsole.Instance.Info("Wrong number of parameters for set user info");
                return;
            }
            UUID AgentID;
            PresenceInfo info;
            int Num = 4;
            if (!UUID.TryParse(cmdparams[3], out AgentID))
            {
                UserAccount account = m_accountService.GetUserAccount(null, Util.CombineParams(cmdparams, 3, 5));
                if (account == null)
                {
                    MainConsole.Instance.Warn("Cannot find user.");
                    return;
                }
                AgentID = account.PrincipalID;
                Num += 1;
            }
            info = GetInformation(AgentID);
            if (info == null)
            {
                MainConsole.Instance.Warn("Cannot find user.");
                return;
            }
            try
            {
                info.Flags = (PresenceInfo.PresenceInfoFlags)Enum.Parse(typeof(PresenceInfo.PresenceInfoFlags), cmdparams[Num]);
            }
            catch
            {
                MainConsole.Instance.Warn("Please choose a valid flag: Clean, Suspected, Known, Banned");
                return;
            }
            MainConsole.Instance.Info("Set Flags for " + info.AgentID.ToString() + " to " + info.Flags.ToString());
            presenceInfo.UpdatePresenceInfo(info);
        }

        private void DisplayUserInfo(PresenceInfo info)
        {
            UserAccount account = m_accountService.GetUserAccount(null, info.AgentID);
            if (account != null)
                MainConsole.Instance.Info("User Info for " + account.Name);
            else
                MainConsole.Instance.Info("User Info for " + info.AgentID);
            MainConsole.Instance.Info("   AgentID: " + info.AgentID);
            MainConsole.Instance.Info("   Flags: " + info.Flags);
            MainConsole.Instance.Info("   ID0: " + info.LastKnownID0);
            MainConsole.Instance.Info("   IP: " + info.LastKnownIP);
            //MainConsole.Instance.Info("   Mac: " + info.LastKnownMac);
            MainConsole.Instance.Info("   Viewer: " + info.LastKnownViewer);
            MainConsole.Instance.Info("   Platform: " + info.Platform);
            if (info.KnownAlts.Count > 0)
            {
                MainConsole.Instance.Info("   Known Alt Accounts: ");
                foreach (var acc in info.KnownAlts)
                {
                    account = m_accountService.GetUserAccount(null, UUID.Parse(acc));
                    if (account != null)
                        MainConsole.Instance.Info("   " + account.Name);
                    else
                        MainConsole.Instance.Info("   " + acc);
                }
            }
        }

        private bool CheckClient(UUID AgentID, out string message)
        {
            message = "";

            PresenceInfo info = GetInformation(AgentID);

            if (m_checkOnLogin)
                CheckForSimilarities(info);
            else
                _checkForSimilaritiesLater.Add(AgentID, info);

            if (!CheckThreatLevel(info, out message))
                return false;

            return CheckViewer(info, out message);
        }

        private bool CheckViewer(PresenceInfo info, out string reason)
        {
            //Check for banned viewers
            if (IsViewerBanned(info.LastKnownViewer))
            {
                reason = "Viewer is banned";
                return false;
            }
            //Overkill, and perm-bans people who only log in with a bad viewer once
            //foreach (string mac in info.KnownMacs)
            {
                if (info.LastKnownMac.Contains("000"))
                {
                    //Ban this asshole
                    reason = "Viewer is blocked (MAC)";
                    return false;
                }
            }
            //foreach (string id0 in info.KnownID0s)
            {
                if (info.LastKnownID0.Contains("000"))
                {
                    //Ban this asshole
                    reason = "Viewer is blocked (IO)";
                    return false;
                }
            }

            reason = "";
            return true;
        }

        public bool IsViewerBanned(string name)
        {
            if (m_useIncludeList)
            {
                if (!m_allowedViewers.Contains(name))
                    return true;
            }
            else
            {
                if (m_bannedViewers.Contains(name))
                    return true;
            }
            return false;
        }

        private bool CheckThreatLevel(PresenceInfo info, out string message)
        {
            message = "";
            if ((info.Flags & PresenceInfo.PresenceInfoFlags.Banned) == PresenceInfo.PresenceInfoFlags.Banned)
            {
                message = "Banned agent.";
                return false;
            }
            if (GrieferAllowLevel == AllowLevel.AllowKnown)
                return true; //Allow all
            else if (GrieferAllowLevel == AllowLevel.AllowCleanOnly)
            { 
                //Allow people with only clean flag or suspected alt
                if ((info.Flags & PresenceInfo.PresenceInfoFlags.Suspected) == PresenceInfo.PresenceInfoFlags.Suspected ||
                    (info.Flags & PresenceInfo.PresenceInfoFlags.Known) == PresenceInfo.PresenceInfoFlags.Known ||
                    (info.Flags & PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfKnown) == PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfKnown ||
                    (info.Flags & PresenceInfo.PresenceInfoFlags.KnownAltAccountOfKnown) == PresenceInfo.PresenceInfoFlags.KnownAltAccountOfKnown ||
                    (info.Flags & PresenceInfo.PresenceInfoFlags.Banned) == PresenceInfo.PresenceInfoFlags.Banned)
                {
                    message = "Not a Clean agent and have been denied access.";
                    return false;
                }
            }
            else if (GrieferAllowLevel == AllowLevel.AllowSuspected)
            {
                //Block all alts of knowns, and suspected alts of knowns
                if ((info.Flags & PresenceInfo.PresenceInfoFlags.Known) == PresenceInfo.PresenceInfoFlags.Known ||
                    (info.Flags & PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfKnown) == PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfKnown ||
                    (info.Flags & PresenceInfo.PresenceInfoFlags.KnownAltAccountOfKnown) == PresenceInfo.PresenceInfoFlags.KnownAltAccountOfKnown ||
                    (info.Flags & PresenceInfo.PresenceInfoFlags.Banned) == PresenceInfo.PresenceInfoFlags.Banned)
                {
                    message = "Not a Clean agent and have been denied access.";
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Public members

        public bool CheckUser(UUID AgentID, string ip, string version, string platform, string mac, string id0, out string message)
        {
            message = "";
            if (!m_enabled)
                return true;

            PresenceInfo oldInfo = GetInformation(AgentID);
            oldInfo = UpdatePresenceInfo(AgentID, oldInfo, ip, version, platform, mac, id0);
            if (m_debug)
                DisplayUserInfo(oldInfo);

            return CheckClient(AgentID, out message);
        }

        public void SetUserLevel(UUID AgentID, PresenceInfo.PresenceInfoFlags presenceInfoFlags)
        {
            if (!m_enabled)
                return;
            //Get
            PresenceInfo info = GetInformation(AgentID);
            //Set the flags
            info.Flags = presenceInfoFlags;
            //Save
            presenceInfo.UpdatePresenceInfo(info);
        }

        #endregion
    }

    #endregion

    #region IP block check

    public class IPBanCheck : ILoginModule
    {
        #region Declares

        private List<IPAddress> IPBans = new List<IPAddress>();
        private List<string> IPRangeBans = new List<string>();

        public string Name
        {
            get { return GetType().Name; }
        }

        #endregion

        #region ILoginModule Members

        public void Initialize(ILoginService service, IConfigSource source, IRegistryCore registry)
        {
            IConfig config = source.Configs["GrieferProtection"];
            if (config != null)
            {
                List<string> iPBans = Util.ConvertToList(config.GetString("IPBans", ""));
                foreach (string ip in iPBans)
                {
                    IPAddress ipa;
                    if(IPAddress.TryParse(ip, out ipa))
                        IPBans.Add(ipa);
                }
                IPRangeBans = Util.ConvertToList(config.GetString("IPRangeBans", ""));
            }
        }

        public LoginResponse Login(Hashtable request, UserAccount account, IAgentInfo agentInfo, string authType, string password, out object data)
        {
            data = null;
            string ip = request != null && request.ContainsKey("ip") ? (string)request["ip"] : "127.0.0.1";
            ip = ip.Split(':')[0];//Remove the port
            IPAddress userIP = IPAddress.Parse(ip);
            if (IPBans.Contains(userIP))
                return new LLFailedLoginResponse(LoginResponseEnum.Indeterminant, "Your account cannot be accessed on this computer.", false);
            foreach (string ipRange in IPRangeBans)
            {
                string[] split = ipRange.Split('-');
                if (split.Length != 2)
                    continue;
                IPAddress low = IPAddress.Parse(ip);
                IPAddress high = IPAddress.Parse(ip);
                NetworkUtils.IPAddressRange range = new NetworkUtils.IPAddressRange(low, high);
                if (range.IsInRange(userIP))
                    return new LLFailedLoginResponse(LoginResponseEnum.Indeterminant, "Your account cannot be accessed on this computer.", false);
            }
            return null;
        }

        #endregion
    }

    #endregion
}
