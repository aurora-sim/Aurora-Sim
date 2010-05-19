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
    public class GodModifiers : ISharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private List<Scene> m_scenes = new List<Scene>();
        private IConfigSource m_config;
        private bool m_Enabled = true;

        public void Initialise(IConfigSource source)
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
            m_config = source;
        }

        public void AddRegion(Scene scene)
        {
            m_scenes.Add(scene);
        }

        public void RemoveRegion(Scene scene)
        {

        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_Enabled)
                return;
            scene.EventManager.OnNewClient += EventManager_OnNewClient;
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void PostInitialise()
        {
        }

        void EventManager_OnNewClient(IClientAPI client)
        {
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
                bool changed = ((Scene)client.Scene).EstateService.LinkRegion(((Scene)client.Scene).RegionInfo.RegionID, (int)EstateID, ((Scene)client.Scene).RegionInfo.EstateSettings.EstatePass);
                if (!changed)
                    SP.ControllingClient.SendAgentAlertMessage("Unable to connecto to the given estate.", false);

            }
        }
    }
}
