using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Xml;
using Nwc.XmlRpc;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Framework.Client;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using Aurora.Framework;

namespace Aurora.Modules
{
    public class GodModifiers : IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private List<Scene> m_scenes = new List<Scene>();
        private IConfigSource m_config;
        private bool m_Enabled = true;

        public void Initialise(Scene scene, IConfigSource source)
        {
            if (source.Configs["GodModule"] != null)
            {
                if (source.Configs["GodModule"].GetString(
                        "GodModule", Name) !=
                        Name)
                {
                    m_Enabled = false;
                    return;
                }
            }
            m_scenes.Add(scene);
            m_config = source;
        }

        public void PostInitialise()
        {
            if (!m_Enabled)
                return;
            foreach (Scene scene in m_scenes)
            {
                scene.EventManager.OnNewClient += EventManager_OnNewClient;
            }
        }

        void EventManager_OnNewClient(IClientAPI client)
        {
            client.onGodlikeMessage += GodlikeMessage;
            client.OnGodUpdateRegionInfoUpdate += GodUpdateRegionInfoUpdate;
            client.OnSaveState += GodSaveState;
        }

        public string Name
        {
            get { return "AuroraGodModModule"; }
        }

        public bool IsSharedModule { get { return true; } }

        public void Close()
        {
        }

        public void GodSaveState(IClientAPI client, UUID agentID)
        {
            UserAccount UA = m_scenes[0].UserAccountService.GetUserAccount(UUID.Zero, client.AgentId);
            if (UA.UserFlags >= 0)
            {
                MainConsole.Instance.RunCommand("change region " + ((Scene)client.Scene).RegionInfo.RegionName);
                MainConsole.Instance.RunCommand("save oar " + ((Scene)client.Scene).RegionInfo.RegionName + Util.UnixTimeSinceEpoch().ToString() + ".ss");
                MainConsole.Instance.RunCommand("change region root");
            }
        }

        public void GodlikeMessage(IClientAPI client, UUID requester, byte[] Method, byte[] Parameter)
        {
            if (Method.ToString() == "telehub")
            {
                client.SendAgentAlertMessage("Please contact an administrator to help you with this function.", false);
            }
        }

        public void GodUpdateRegionInfoUpdate(IClientAPI client, float BillableFactor, ulong EstateID, ulong RegionFlags, byte[] SimName, int RedirectX, int RedirectY)
        {
            string regionConfigPath = Path.Combine(Util.configDir(), "Regions");
            string[] iniFiles = Directory.GetFiles(regionConfigPath, "*.ini");
            int i = 0;
            UserAccount UA = m_scenes[0].UserAccountService.GetUserAccount(UUID.Zero, client.AgentId);
            ScenePresence SP;
            m_scenes[0].TryGetScenePresence(client.AgentId, out SP);
            //if (UA.UserLevel == 0)
            //    return;
            if (SP.GodLevel == 0)
                return;
            foreach (string file in iniFiles)
            {
                IConfigSource source = new IniConfigSource(file);
                IConfig cnf = source.Configs[((Scene)client.Scene).RegionInfo.RegionName];
                if (cnf != null)
                {
                    OpenSim.Services.Interfaces.GridRegion region = ((Scene)client.Scene).GridService.GetRegionByName(UUID.Zero, ((Scene)client.Scene).RegionInfo.RegionName);
                    ((Scene)client.Scene).GridService.DeregisterRegion(region.RegionID);
                    IConfig check = source.Configs[OpenMetaverse.Utils.BytesToString(SimName)];
                    if (check == null)
                    {
                        source.AddConfig(OpenMetaverse.Utils.BytesToString(SimName));
                        IConfig cfgNew = source.Configs[OpenMetaverse.Utils.BytesToString(SimName)];
                        IConfig cfgOld = source.Configs[((Scene)client.Scene).RegionInfo.RegionName];
                        string[] oldRegionValues = cfgOld.GetValues();
                        string[] oldRegionKeys = cfgOld.GetKeys();
                        int next = 0;
                        foreach (string oldkey in oldRegionKeys)
                        {
                            cfgNew.Set(oldRegionKeys[next], oldRegionValues[next]);
                            next++;
                        }
                        source.Configs.Remove(cfgOld);
                        if (RedirectX != 0 || RedirectY != 0)
                        {
                            if (RedirectX == 0)
                                RedirectX = (int)client.Scene.RegionInfo.RegionLocX;
                            if (RedirectY == 0)
                                RedirectY = (int)client.Scene.RegionInfo.RegionLocY;

                            check.Set("Location", RedirectX.ToString() + "," + RedirectY.ToString());
                            client.Scene.RegionInfo.RegionLocX = Convert.ToUInt32(RedirectX);
                            client.Scene.RegionInfo.RegionLocY = Convert.ToUInt32(RedirectY);
                            region.RegionLocX = RedirectX;
                            region.RegionLocY = RedirectY;
                        }
                        ((Scene)client.Scene).RegionInfo.RegionName = OpenMetaverse.Utils.BytesToString(SimName);
                        source.Save();
                        region.RegionName = OpenMetaverse.Utils.BytesToString(SimName);
                    }
                    else
                    {
                        if (RedirectX != 0 || RedirectY != 0)
                        {
                            if (RedirectX == 0)
                                RedirectX = (int)client.Scene.RegionInfo.RegionLocX;
                            if (RedirectY == 0)
                                RedirectY = (int)client.Scene.RegionInfo.RegionLocY;

                            check.Set("Location", RedirectX.ToString() + "," + RedirectY.ToString());
                            client.Scene.RegionInfo.RegionLocX = Convert.ToUInt32(RedirectX);
                            client.Scene.RegionInfo.RegionLocY = Convert.ToUInt32(RedirectY);
                            region.RegionLocX = RedirectX;
                            region.RegionLocY = RedirectY;
                        }
                    }
                    ((Scene)client.Scene).GridService.RegisterRegion(UUID.Zero, region);
                }
                i++;
            }
            if (((Scene)client.Scene).RegionInfo.EstateSettings.EstateID != EstateID)
            {
                bool changed = ((Scene)client.Scene).EstateService.LinkRegion(((Scene)client.Scene).RegionInfo.RegionID, (int)EstateID);
                if (!changed)
                    SP.ControllingClient.SendAgentAlertMessage("Unable to connecto to the given estate.", false);

            }
        }
    }

    public class EstateSettingsModule : IRegionModule, IEstateSettingsModule
    {
        Scene m_scene;
        IProfileData PD;

        public void Initialise(Scene scene, IConfigSource source)
        {
            scene.RegisterModuleInterface<IEstateSettingsModule>(this);
            m_scene = scene;
        }

        public void PostInitialise()
        {
            PD = Aurora.DataManager.DataManager.GetDefaultProfilePlugin();
        }

        public void Close() { }

        public string Name { get { return "EstateSettingsModule"; } }

        public bool IsSharedModule { get { return true; } }

        public bool AllowTeleport(IScene scene, UUID userID, Vector3 Position, out Vector3 newPosition)
        {
            newPosition = Position;
            EstateSettings ES = m_scene.EstateService.LoadEstateSettings(scene.RegionInfo.RegionID, false);
            AuroraProfileData Profile = PD.GetProfileInfo(userID);

            if (scene.RegionInfo.RegionSettings.Maturity > Profile.Mature)
                return false;

            if (ES.DenyMinors && Profile.Minor)
                return false;

            if (!ES.PublicAccess)
            {
                if (!new List<UUID>(ES.EstateManagers).Contains(userID) || ES.EstateOwner != userID)
                    return false;
            }
            if (!ES.AllowDirectTeleport)
            {
                IGenericData GenericData = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
                List<string> Telehubs = GenericData.Query("regionUUID", scene.RegionInfo.RegionID.ToString(), "auroraregions", "telehubX,telehubY");
                newPosition = new Vector3(Convert.ToInt32(Telehubs[0]), Convert.ToInt32(Telehubs[1]), Position.Z);
            }

            return true;
        }
    }
}
