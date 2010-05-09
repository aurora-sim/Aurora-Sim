using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Nini.Config;
using OpenSim.Framework.Console;
using OpenMetaverse;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;

namespace Aurora.Modules
{
    public class Auth: ISharedRegionModule, IIWCAuthenticationService
    {
        private List<string> CheckServers = new List<string>();
        private List<string> AuthServersBannedList = new List<string>();
        private IGenericData a_DataService = null;
        private Scene m_scene;

        #region IRegionModule Members

        public string Name
        {
            get { return "GenericAuthPlugin"; }
        }

        public void Close()
        {
        }

        public void PostInitialise()
        {
            
        }

        public void Initialise(IConfigSource source)
        {
            if (CheckServers.Count == 0)
            {
                string bannedAuthServers = source.Configs["AuroraAuth"].GetString("BannedAuthServers", "");
                if (bannedAuthServers == "")
                    return;
                string[] servers = bannedAuthServers.Split(',');
                foreach (string server in servers)
                {
                    CheckServers.Add(server);
                }
            }
        }

        public void AddRegion(Scene scene)
        {
            m_scene = scene;
            scene.RegisterModuleInterface<IIWCAuthenticationService>(this);
            a_DataService = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
        }

        public void RemoveRegion(Scene scene)
        {

        }

        public void RegionLoaded(Scene scene)
        {

        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        #region IIWCAuthenticationService

        /// <summary>
        /// This checks whether the user's homeserver is allowed to connect to the world.
        /// </summary>
        /// <param name="serverIP"></param>
        /// <returns></returns>
        public bool CheckAuthenticationServer(IPEndPoint serverIP)
        {
            if (AuthServersBannedList.Contains(serverIP.Address.ToString()))
                return false;
            return true;
        }

        /// <summary>
        /// This is called when a user attempts to connect to the world. This may be from another sim
        /// in the world, or a sim outside the world. This checks for banning before they are allowed to connect.
        /// </summary>
        /// <param name="Identifier"></param>
        /// <returns></returns>
        public bool CheckUserAccount(string Identifier)
        {
            return true;
        }

        #endregion
    }
}
