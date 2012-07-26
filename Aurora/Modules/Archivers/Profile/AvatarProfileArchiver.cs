/*
 * Copyright (c) Contributors, http://aurora-sim.org/
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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Aurora.Framework;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.Archivers
{
    /// <summary>
    ///   This module loads/saves the avatar's profile from/into a "AvatarProfile Archive"
    /// </summary>
    public class AuroraAvatarProfileArchiver : ISharedRegionModule
    {
        private IScene m_scene;

        private IUserAccountService UserAccountService
        {
            get { return m_scene.UserAccountService; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        #region ISharedRegionModule Members

        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion(IScene scene)
        {
            if (m_scene == null)
                m_scene = scene;

            if (MainConsole.Instance != null)
            {
                MainConsole.Instance.Commands.AddCommand("save avatar profile",
                                                         "save avatar profile <First> <Last> <Filename>",
                                                         "Saves profile and avatar data to an archive",
                                                         HandleSaveAvatarProfile);
                MainConsole.Instance.Commands.AddCommand("load avatar profile",
                                                         "load avatar profile <First> <Last> <Filename>",
                                                         "Loads profile and avatar data from an archive",
                                                         HandleLoadAvatarProfile);
            }
        }

        public void RemoveRegion(IScene scene)
        {
        }

        public void RegionLoaded(IScene scene)
        {
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "AvatarProfileArchiver"; }
        }

        #endregion

        protected void HandleLoadAvatarProfile(string[] cmdparams)
        {
            if (cmdparams.Length != 6)
            {
                MainConsole.Instance.Info("[AvatarProfileArchiver] Not enough parameters!");
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
            UserAccount UDA = new UserAccount
                                  {
                                      Name = cmdparams[3] + cmdparams[4],
                                      PrincipalID = UUID.Random(),
                                      ScopeID = UUID.Zero,
                                      UserFlags = int.Parse(results["UserFlags"].ToString()),
                                      UserLevel = 0,
                                      UserTitle = results["UserTitle"].ToString(),
                                      Email = results["Email"].ToString(),
                                      Created = int.Parse(results["Created"].ToString())
                                  };
            //For security... Don't want everyone loading full god mode.
            UserAccountService.StoreUserAccount(UDA);

            replyData = WebUtils.ParseXmlResponse(file[2]);
            IUserProfileInfo UPI = new IUserProfileInfo();
            UPI.FromKVP(replyData["result"] as Dictionary<string, object>);
            //Update the principle ID to the new user.
            UPI.PrincipalID = UDA.PrincipalID;

            IProfileConnector profileData = DataManager.DataManager.RequestPlugin<IProfileConnector>();
            if (profileData.GetUserProfile(UPI.PrincipalID) == null)
                profileData.CreateNewProfile(UPI.PrincipalID);

            profileData.UpdateUserProfile(UPI);

            MainConsole.Instance.Info("[AvatarProfileArchiver] Loaded Avatar Profile from " + cmdparams[5]);
        }

        protected void HandleSaveAvatarProfile(string[] cmdparams)
        {
            if (cmdparams.Length != 6)
            {
                MainConsole.Instance.Info("[AvatarProfileArchiver] Not enough parameters!");
                return;
            }
            UserAccount account = UserAccountService.GetUserAccount(null, cmdparams[3] + " " + cmdparams[4]);
            if (account == null)
            {
                MainConsole.Instance.Info("Account could not be found, stopping now.");
                return;
            }
            IProfileConnector data = DataManager.DataManager.RequestPlugin<IProfileConnector>();
            string UPIxmlString = "";
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (data != null)
            {
                IUserProfileInfo profile = data.GetUserProfile(account.PrincipalID);
                if (profile != null)
                {
                    result["result"] = profile.ToKVP();
                    UPIxmlString = WebUtils.BuildXmlResponse(result);
                }
            }

            result["result"] = account.ToKVP();
            string UDAxmlString = WebUtils.BuildXmlResponse(result);

            StreamWriter writer = new StreamWriter(cmdparams[5]);
            writer.Write("<profile>\n");
            writer.Write(UDAxmlString + "\n");
            writer.Write(UPIxmlString + "\n");
            writer.Write("</profile>\n");
            writer.Close();
            writer.Dispose();
            MainConsole.Instance.Info("[AvatarProfileArchiver] Saved Avatar Profile to " + cmdparams[5]);
        }
    }
}