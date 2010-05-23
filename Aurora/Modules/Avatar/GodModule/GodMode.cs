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
            UserAccount UA = ((Scene)client.Scene).UserAccountService.GetUserAccount(UUID.Zero, client.AgentId);
            if (UA.UserLevel == 0)
                return;
            ((Scene)client.Scene).RegionInfo.RegionName = OpenMetaverse.Utils.BytesToString(SimName);
            if(RedirectX != 0)
                ((Scene)client.Scene).RegionInfo.RegionLocX = (uint)RedirectX;
            if (RedirectY != 0)
                ((Scene)client.Scene).RegionInfo.RegionLocY = (uint)RedirectY;
            Aurora.DataManager.DataManager.IRegionInfoConnector.UpdateRegionInfo(((Scene)client.Scene).RegionInfo, false);

            if (((Scene)client.Scene).RegionInfo.EstateSettings.EstateID != EstateID)
            {
                //Not the best thing... this should have way more security... but it works for now.
                EstateSettings OtherEstate = ((Scene)client.Scene).EstateService.LoadEstateSettings((int)EstateID);
                bool changed = ((Scene)client.Scene).EstateService.LinkRegion(((Scene)client.Scene).RegionInfo.RegionID, (int)EstateID, OtherEstate.EstatePass);
                if (!changed)
                    client.SendAgentAlertMessage("Unable to connect to the given estate.", false);
            }
        }
    }
}
