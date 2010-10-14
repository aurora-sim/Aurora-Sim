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
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using Aurora.Framework;

namespace Aurora.Modules
{
    public class GodModifiers : ISharedRegionModule
    {
        #region Declares 

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private List<Scene> m_scenes = new List<Scene>();
        private IConfigSource m_config;
        private bool m_Enabled = true;
        private string m_oar_directory = "";

        #endregion

        #region ISharedRegionModule

        public void Initialise(IConfigSource source)
        {
            m_config = source;
            if (source.Configs["GodModule"] != null)
            {
                if (source.Configs["GodModule"].GetString(
                        "GodModule", Name) !=
                        Name)
                {
                    m_Enabled = false;
                    return;
                }
                m_oar_directory = source.Configs["GodModule"].GetString("DirectoryToSaveOARs", m_oar_directory);
            }
        }

        public void AddRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            if(!m_scenes.Contains(scene))
                m_scenes.Add(scene);

            scene.EventManager.OnNewClient += OnNewClient;
        }

        public void RemoveRegion(Scene scene)
        {
            if (!m_Enabled)
                return;

            if (m_scenes.Contains(scene))
                m_scenes.Remove(scene);

            scene.EventManager.OnNewClient -= OnNewClient;
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void PostInitialise()
        {
        }

        public string Name
        {
            get { return "AuroraGodModModule"; }
        }

        public void Close()
        {
        }

        #endregion

        #region Client

        private void OnNewClient(IClientAPI client)
        {
            client.OnGodUpdateRegionInfoUpdate += GodUpdateRegionInfoUpdate;
            client.OnGodlikeMessage += onGodlikeMessage;
            client.OnSaveState += GodSaveState;
        }

        void onGodlikeMessage(IClientAPI client, UUID requester, string Method, List<string> Parameter)
        {
            if (Method == "refreshmapvisibility")
            {
                ScenePresence Sp = ((Scene)client.Scene).GetScenePresence(client.AgentId);
                if (Sp.GodLevel >= 0)
                {
                    //Rebuild the map tile
                    Sp.Scene.CreateTerrainTexture();
                }
            }
        }

        public void GodSaveState(IClientAPI client, UUID agentID)
        {
            ScenePresence Sp = ((Scene)client.Scene).GetScenePresence(client.AgentId);
            if (Sp.GodLevel >= 0)
            {
                Scene scene = (Scene)MainConsole.Instance.ConsoleScene; //Switch back later
                MainConsole.Instance.RunCommand("change region " + ((Scene)client.Scene).RegionInfo.RegionName);
                MainConsole.Instance.RunCommand("save oar " + m_oar_directory + ((Scene)client.Scene).RegionInfo.RegionName + Util.UnixTimeSinceEpoch().ToString() + ".ss");
                MainConsole.Instance.RunCommand("change region " + scene.RegionInfo.RegionName);
            }
        }

        public void GodUpdateRegionInfoUpdate(IClientAPI client, float BillableFactor, int PricePerMeter, ulong EstateID, ulong RegionFlags, byte[] SimName, int RedirectX, int RedirectY)
        {
            ScenePresence Sp = ((Scene)client.Scene).GetScenePresence(client.AgentId);
            if (Sp.GodLevel == 0)
                return;

            string oldRegionName = ((Scene)client.Scene).RegionInfo.RegionName;
            ((Scene)client.Scene).RegionInfo.RegionName = OpenMetaverse.Utils.BytesToString(SimName);
            if(RedirectX != 0)
                ((Scene)client.Scene).RegionInfo.RegionLocX = (uint)RedirectX;
            if (RedirectY != 0)
                ((Scene)client.Scene).RegionInfo.RegionLocY = (uint)RedirectY;

            if (((Scene)client.Scene).RegionInfo.EstateSettings.EstateID != EstateID)
            {
                string Password = "";
                IWorldComm comm = ((Scene)client.Scene).RequestModuleInterface<IWorldComm>();
                IDialogModule dialog = ((Scene)client.Scene).RequestModuleInterface<IDialogModule>();
                if (comm != null && dialog != null)
                {
                    int Channel = new Random().Next(1000, 100000);
                    comm.AddBlockedChannel(Channel);
                    ChannelDirectory.Add(client.AgentId, new EstateChange() { Channel = Channel, EstateID = (uint)EstateID, OldEstateID = ((Scene)client.Scene).RegionInfo.EstateSettings.EstateID });
                    //Set the ID temperarily, if it doesn't work, we will revert it later
                    ((Scene)client.Scene).RegionInfo.EstateSettings.EstateID = (uint)EstateID;
                    client.OnChatFromClient += OnChatFromClient;
                    dialog.SendTextBoxToUser(client.AgentId, "Please type the password for the estate you wish to join. (Note: this channel is secured and will not be able to be listened in on)", Channel, "Server", UUID.Zero, UUID.Zero);
                }
                else
                {
                    bool changed = ((Scene)client.Scene).EstateService.LinkRegion(((Scene)client.Scene).RegionInfo.RegionID, (int)EstateID, Util.Md5Hash(Password));
                    if (!changed)
                        client.SendAgentAlertMessage("Unable to connect to the given estate.", false);
                    else
                    {
                        ((Scene)client.Scene).RegionInfo.EstateSettings.EstateID = (uint)EstateID;
                        ((Scene)client.Scene).RegionInfo.EstateSettings.Save();
                    }
                }
            }

            client.Scene.RegionInfo.EstateSettings.BillableFactor = BillableFactor;
            client.Scene.RegionInfo.EstateSettings.PricePerMeter = PricePerMeter;
            client.Scene.RegionInfo.EstateSettings.SetFromFlags(RegionFlags);

            client.Scene.RegionInfo.RegionSettings.AllowDamage = ((RegionFlags & (ulong)OpenMetaverse.RegionFlags.AllowDamage) == (ulong)OpenMetaverse.RegionFlags.AllowDamage);
            client.Scene.RegionInfo.RegionSettings.FixedSun = ((RegionFlags & (ulong)OpenMetaverse.RegionFlags.SunFixed) == (ulong)OpenMetaverse.RegionFlags.SunFixed);
            client.Scene.RegionInfo.RegionSettings.BlockTerraform = ((RegionFlags & (ulong)OpenMetaverse.RegionFlags.BlockTerraform) == (ulong)OpenMetaverse.RegionFlags.BlockTerraform);
            client.Scene.RegionInfo.RegionSettings.Sandbox = ((RegionFlags & (ulong)OpenMetaverse.RegionFlags.Sandbox) == (ulong)OpenMetaverse.RegionFlags.Sandbox);
            
            //Update skipping scripts/physics/collisions
            ((Scene)client.Scene).SetSceneCoreDebug(((RegionFlags & (ulong)OpenMetaverse.RegionFlags.SkipScripts) == (ulong)OpenMetaverse.RegionFlags.SkipScripts),
                ((RegionFlags & (ulong)OpenMetaverse.RegionFlags.SkipCollisions) == (ulong)OpenMetaverse.RegionFlags.SkipCollisions),
                ((RegionFlags & (ulong)OpenMetaverse.RegionFlags.SkipPhysics) == (ulong)OpenMetaverse.RegionFlags.SkipPhysics));

            //Save the changes
            client.Scene.RegionInfo.EstateSettings.Save();
            client.Scene.RegionInfo.RegionSettings.Save();

            //Save the changes
            IConfig config = m_config.Configs["RegionStartup"];
            if (config != null)
            {
                //TERRIBLE! Needs to be modular, but we can't access the module from a scene module!
                if (config.GetString("Default") == "RegionLoaderDataBaseSystem")
                    SaveChangesDatabase(((Scene)client.Scene).RegionInfo);
                else
                    SaveChangesFile(oldRegionName, ((Scene)client.Scene).RegionInfo);
            }
            else
                SaveChangesFile(oldRegionName, ((Scene)client.Scene).RegionInfo);
                

            //Tell the clients to update all references to the new name
            foreach (ScenePresence sp in ((Scene)client.Scene).ScenePresences)
            {
                HandleRegionInfoRequest(sp.ControllingClient, ((Scene)client.Scene));
            }
        }

        private class EstateChange
        {
            public int Channel;
            public uint OldEstateID;
            public uint EstateID;
        }

        private Dictionary<UUID, EstateChange> ChannelDirectory = new Dictionary<UUID, EstateChange>();

        public void OnChatFromClient(object sender, OSChatMessage e)
        {
             //For Estate Password
            EstateChange Change = null;
            if (ChannelDirectory.TryGetValue(e.Sender.AgentId, out Change))
            {
                if (Change.Channel == e.Channel)
                {
                    ((IClientAPI)sender).OnChatFromClient -= OnChatFromClient;
                    ChannelDirectory.Remove(e.Sender.AgentId);
                    IWorldComm comm = ((Scene)((IClientAPI)sender).Scene).RequestModuleInterface<IWorldComm>();
                    comm.RemoveBlockedChannel(Change.Channel);

                    string Password = Util.Md5Hash(e.Message);
                    bool changed = ((Scene)((IClientAPI)sender).Scene).EstateService.LinkRegion(((Scene)((IClientAPI)sender).Scene).RegionInfo.RegionID, (int)Change.EstateID, Password);
                    if (!changed)
                    {
                        //Revert it, it didn't work
                        ((Scene)((IClientAPI)sender).Scene).RegionInfo.EstateSettings.EstateID = Change.OldEstateID;
                        ((IClientAPI)sender).SendAgentAlertMessage("Unable to connect to the given estate.", false);
                    }
                    else
                    {
                        ((Scene)((IClientAPI)sender).Scene).RegionInfo.EstateSettings.EstateID = Change.EstateID;
                        ((Scene)((IClientAPI)sender).Scene).RegionInfo.EstateSettings.Save();
                        ((IClientAPI)sender).SendAgentAlertMessage("Estate Updated.", false);
                    }
                    //Tell the clients to update all references to the new name
                    foreach (ScenePresence sp in ((Scene)((IClientAPI)sender).Scene).ScenePresences)
                    {
                        HandleRegionInfoRequest(sp.ControllingClient, ((Scene)((IClientAPI)sender).Scene));
                    }
                }
            }
        }

        #endregion

        #region Helpers

        private void SaveChangesFile(string OldRegionName, RegionInfo regionInfo)
        {
            string regionConfigPath = Path.Combine(Util.configDir(), "Regions");

            try
            {
                IConfig startupConfig = (IConfig)m_config.Configs["Startup"];
                regionConfigPath = startupConfig.GetString("regionload_regionsdir", regionConfigPath).Trim();
            }
            catch (Exception)
            {
                // No INI setting recorded.
            }
            if (!Directory.Exists(regionConfigPath))
                return;

            string[] iniFiles = Directory.GetFiles(regionConfigPath, "*.ini");
            int i = 0;
            foreach (string file in iniFiles)
            {
                IConfigSource source = new IniConfigSource(file, Nini.Ini.IniFileType.AuroraStyle);
                IConfig cnf = source.Configs[OldRegionName];
                if (cnf != null) //Does the old one exist in this file?
                {
                    IConfig check = source.Configs[regionInfo.RegionName];
                    if (check == null) //Is the new name non existant as well?
                    {
                        cnf.Set("Location", regionInfo.RegionLocX.ToString() + "," + regionInfo.RegionLocY.ToString());
                        cnf.Name = regionInfo.RegionName;
                        source.Save();
                    }
                    else
                    {
                        //The new region exists too, no name change
                        check.Set("Location", regionInfo.RegionLocX.ToString() + "," + regionInfo.RegionLocY.ToString());
                    }
                }
                i++;
            }
        }

        private void SaveChangesDatabase(RegionInfo regionInfo)
        {
            IRegionInfoConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IRegionInfoConnector>();
            if (connector != null)
                connector.UpdateRegionInfo(regionInfo, false);
        }

        private void HandleRegionInfoRequest(IClientAPI remote_client, Scene m_scene)
        {
            RegionInfoForEstateMenuArgs args = new RegionInfoForEstateMenuArgs();
            args.billableFactor = m_scene.RegionInfo.EstateSettings.BillableFactor;
            args.estateID = m_scene.RegionInfo.EstateSettings.EstateID;
            args.maxAgents = (byte)m_scene.RegionInfo.RegionSettings.AgentLimit;
            args.objectBonusFactor = (float)m_scene.RegionInfo.RegionSettings.ObjectBonus;
            args.parentEstateID = m_scene.RegionInfo.EstateSettings.ParentEstateID;
            args.pricePerMeter = m_scene.RegionInfo.EstateSettings.PricePerMeter;
            args.redirectGridX = m_scene.RegionInfo.EstateSettings.RedirectGridX;
            args.redirectGridY = m_scene.RegionInfo.EstateSettings.RedirectGridY;

            IEstateModule estate = m_scene.RequestModuleInterface<IEstateModule>();
            if (estate == null)
                args.regionFlags = 0;
            args.regionFlags = estate.GetRegionFlags();

            args.simAccess = m_scene.RegionInfo.AccessLevel;
            args.sunHour = (float)m_scene.RegionInfo.RegionSettings.SunPosition;
            args.terrainLowerLimit = (float)m_scene.RegionInfo.RegionSettings.TerrainLowerLimit;
            args.terrainRaiseLimit = (float)m_scene.RegionInfo.RegionSettings.TerrainRaiseLimit;
            args.useEstateSun = m_scene.RegionInfo.RegionSettings.UseEstateSun;
            args.waterHeight = (float)m_scene.RegionInfo.RegionSettings.WaterHeight;
            args.simName = m_scene.RegionInfo.RegionName;
            args.RegionType = m_scene.RegionInfo.RegionType;
            args.MaxAgents = (uint)m_scene.RegionInfo.RegionSettings.AgentLimit;

            remote_client.SendRegionInfoToEstateMenu(args);
        }

        #endregion
    }
}
