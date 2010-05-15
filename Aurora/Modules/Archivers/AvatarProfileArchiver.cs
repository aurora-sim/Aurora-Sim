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
using OpenSim.Server.Base;

namespace Aurora.Modules
{
    public class AuroraAvatarProfileArchiver : IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        Scene m_scene;
        public void Initialise(Scene scene, Nini.Config.IConfigSource source)
        {
            if (m_scene == null)
                m_scene = scene;
            MainConsole.Instance.Commands.AddCommand("region", false, "save avatar profile",
                                          "save avatar profile <First> <Last> <Filename>",
                                          "Saves profile and avatar data to an archive", HandleSaveAvatarProfile);
            MainConsole.Instance.Commands.AddCommand("region", false, "load avatar profile",
                                          "load avatar profile <First> <Last> <Filename>",
                                          "Loads profile and avatar data from an archive", HandleLoadAvatarProfile);
        }

        public void PostInitialise() { }

        public void Close() { }

        public string Name { get { return "AvatarProfileArchiver"; } }

        public bool IsSharedModule
        {
            get { return true; }
        }

        protected void HandleLoadAvatarProfile(string module, string[] cmdparams)
        {
            if (cmdparams.Length != 6)
            {
                m_log.Debug("[AvatarProfileArchiver] Not enough parameters!");
                return;
            }
            StreamReader reader = new StreamReader(cmdparams[5]);

            string document = reader.ReadToEnd();
            string[] lines = document.Split('\n');
            List<string> file = new List<string>(lines);

            List<string> newFile = new List<string>();
            foreach (string line in file)
            {
                string newLine = line.TrimStart('<');
                newFile.Add(newLine.TrimEnd('>'));
            }
            Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(newFile[0]);
            UserAccount UDA = new UserAccount(replyData);
            m_scene.UserAccountService.StoreUserAccount(UDA);

            replyData = ServerUtils.ParseXmlResponse(newFile[1]);
            IUserProfileInfo UPI = new IUserProfileInfo(replyData);
            IProfileConnector profileData = DataManager.DataManager.IProfileConnector;
            if(profileData.GetUserProfile(UPI.PrincipalID) == null)
                profileData.CreateNewProfile(UPI.PrincipalID);

            profileData.UpdateUserProfile(UPI);

            reader.Close();
            reader.Dispose();

            m_log.Debug("[AvatarProfileArchiver] Loaded Avatar Profile from " + cmdparams[5]);
        }
        protected void HandleSaveAvatarProfile(string module, string[] cmdparams)
        {
            if (cmdparams.Length != 6)
            {
                m_log.Debug("[AvatarProfileArchiver] Not enough parameters!");
                return;
            }
            UserAccount account = m_scene.UserAccountService.GetUserAccount(UUID.Zero, cmdparams[3], cmdparams[4]);
            IProfileConnector data = DataManager.DataManager.IProfileConnector;
            IUserProfileInfo profile = data.GetUserProfile(account.PrincipalID);
            
            Dictionary<string, object> result = new Dictionary<string, object>();
            result["result"] = profile.ToKeyValuePairs();
            string UPIxmlString = ServerUtils.BuildXmlResponse(result);

            result["result"] = account.ToKeyValuePairs();
            string UDAxmlString = ServerUtils.BuildXmlResponse(result);

            StreamWriter writer = new StreamWriter(cmdparams[5]);
            writer.Write("<profile>\n");
            writer.Write("<" + UDAxmlString + ">\n");
            writer.Write("<" + UPIxmlString + ">\n");
            writer.Write("</profile>\n");
            m_log.Debug("[AvatarProfileArchiver] Saved Avatar Profile to " + cmdparams[5]);
            writer.Close();
            writer.Dispose();
        }
    }
}
