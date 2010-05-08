using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using log4net;
using Aurora.DataManager;
using Aurora.Framework;

namespace Aurora.Modules
{
    class AvatarDataModule : IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        Scene m_scene;
        public void Initialise(Scene scene, Nini.Config.IConfigSource source)
        {
            if (m_scene == null)
                m_scene = scene;
            MainConsole.Instance.Commands.AddCommand("region", false, "save AD",
                                          "save AD <First> <Last> <Filename>",
                                          "Saves profile and avatar data to an archive", HandleSaveAD);
            MainConsole.Instance.Commands.AddCommand("region", false, "load AD",
                                          "load AD <First> <Last> <Filename>",
                                          "Loads profile and avatar data from an archive", HandleLoadAD);
        }

        public void PostInitialise() { }

        public void Close() { }

        public string Name { get { return "AuroraDataModule"; } }

        public bool IsSharedModule
        {
            get { return true; }
        }

        protected void HandleLoadAD(string module, string[] cmdparams)
        {
            if (cmdparams.Length != 5)
            {
                m_log.Debug("[AD] Not enough parameters!");
                return;
            }
        }
        protected void HandleSaveAD(string module, string[] cmdparams)
        {
            if (cmdparams.Length != 5)
            {
                m_log.Debug("[AD] Not enough parameters!");
                return;
            }
            UserAccount account = m_scene.UserAccountService.GetUserAccount(UUID.Zero, cmdparams[2], cmdparams[3]);
            Aurora.DataManager.Frontends.ProfileFrontend data = new Aurora.DataManager.Frontends.ProfileFrontend();
            IUserProfileInfo profile = data.GetUserProfile(account.PrincipalID);
            StreamWriter writer = new StreamWriter(cmdparams[4]);
            writer.Write("<profile>\n");
            writer.Write(account.Email + "\n");
            writer.Write(account.UserFlags.ToString() + "\n");
            writer.Write(account.UserLevel.ToString() + "\n");
            writer.Write(account.UserTitle + "\n");
            writer.Write(profile.ProfileAboutText + "\n");
            writer.Write(profile.AllowPublish + "\n");
            writer.Write(profile.Email + "\n");
            writer.Write(profile.ProfileFirstText + "\n");
            writer.Write(profile.ProfileFirstImage.ToString() + "\n");
            writer.Write(profile.ProfileImage.ToString() + "\n");
            writer.Write(profile.Interests.WantToMask);
            writer.Write(profile.MembershipGroup + "\n");
            writer.Write(profile.Notes);
            writer.Write(profile.Partner + "\n");
            writer.Write(profile.Picks);
            writer.Write(profile.ProfileURL+"\n");
            writer.Write("</profile>\n");
        }
    }
}
