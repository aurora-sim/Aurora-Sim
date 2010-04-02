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

namespace Aurora.Modules
{
	public class GodModifiers : IRegionModule
	{
		private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private List<Scene> m_scenes = new List<Scene>();
		private IConfigSource m_config;
		
		public void Initialise(Scene scene, IConfigSource config)
		{
			m_scenes.Add(scene);
			m_config = config;
		}

		public void PostInitialise()
		{
			foreach(Scene scene in m_scenes)
			{
				scene.EventManager.OnNewClient += EventManager_OnNewClient;
			}
		}

        void  EventManager_OnNewClient(IClientAPI client)
        {
            client.onGodlikeMessage += GodlikeMessage;
            client.OnGodUpdateRegionInfoUpdate += GodUpdateRegionInfoUpdate;
            client.OnSaveState += GodSaveState;
        }

		public string Name
		{
			get { return "AuroraGodModModule"; }
		}

		public bool IsSharedModule
		{
			get { return true; }
		}
		
		public void Close()
		{
		}
		public void GodSaveState(IClientAPI client,UUID agentID)
		{
			ScenePresence tPresence = null;
			ScenePresence tPresenceTemp = null;
			
			foreach(Scene scene in m_scenes)
			{
				scene.TryGetAvatar(client.AgentId, out tPresenceTemp);
				if(tPresenceTemp != null)
				{
					if(tPresenceTemp.IsChildAgent == true)
					{
					}
					else
					{
						tPresence = tPresenceTemp;
						if(tPresence.GodLevel >= 0)
						{
							MainConsole.Instance.RunCommand("change region "+scene.RegionInfo.RegionName);
							MainConsole.Instance.RunCommand("save oar " + tPresence.Scene.RegionInfo.RegionName + Util.UnixTimeSinceEpoch().ToString() + ".ss");
							MainConsole.Instance.RunCommand("change region root");
						}
						else
						{
							tPresence.ControllingClient.SendAgentAlertMessage("You can not access the god tools.",false);
						}
					}
				}
			}
			
		}
		public void GodlikeMessage(IClientAPI client, UUID requester, byte[] Method, byte[] Parameter)
		{
			if(Method.ToString() == "telehub")
			{
				client.SendAgentAlertMessage("Please contact an administrator to help you with this function.", false);
			}
		}
		public void GodUpdateRegionInfoUpdate(IClientAPI client, float BillableFactor, ulong EstateID, ulong RegionFlags, byte[] SimName,int RedirectX, int RedirectY)
		{
			string regionConfigPath = Path.Combine(Util.configDir(), "Regions");
			string[] iniFiles = Directory.GetFiles(regionConfigPath, "*.ini");
			int i = 0;
			foreach (string file in iniFiles)
			{
				ScenePresence tPresence = null;
				ScenePresence tPresenceTemp = null;
				Scene theScene;
				foreach(Scene scene in m_scenes)
				{
					scene.TryGetAvatar(client.AgentId, out tPresenceTemp);
					if(tPresenceTemp != null)
					{
						if(tPresenceTemp.IsChildAgent == true)
						{
						}
						else
						{
							if(tPresence.GodLevel >= 0)
							{
								tPresence = tPresenceTemp;
								theScene = scene;
							}
							else
							{
								tPresenceTemp.ControllingClient.SendAgentAlertMessage("You can not access the god tools.",false);
								return;
							}
						}
					}
				}
				IConfigSource source = new IniConfigSource(file);
				IConfig cnf = source.Configs[tPresence.Scene.RegionInfo.RegionName];
				if(cnf != null)
				{
					IConfig check = source.Configs[Utils.BytesToString(SimName)];
					if(check == null)
					{
						source.AddConfig(Utils.BytesToString(SimName));
						IConfig cfgNew = source.Configs[Utils.BytesToString(SimName)];
						IConfig cfgOld = source.Configs[tPresence.Scene.RegionInfo.RegionName];
						string[] oldRegionValues = cfgOld.GetValues();
						string[] oldRegionKeys = cfgOld.GetKeys();
						int next = 0;
						foreach(string oldkey in oldRegionKeys)
						{
							cfgNew.Set(oldRegionKeys[next],oldRegionValues[next]);
							next++;
						}
						source.Configs.Remove(cfgOld);
						if(RedirectX != 0)
						{
							if(RedirectY != 0)
							{
								cfgNew.Set("Location",RedirectX.ToString() + "," + RedirectY.ToString());
								client.Scene.RegionInfo.RegionLocX = Convert.ToUInt32(RedirectX);
								client.Scene.RegionInfo.RegionLocY = Convert.ToUInt32(RedirectY);
							}
						}
						tPresence.Scene.RegionInfo.RegionName = Utils.BytesToString(SimName);
						source.Save();
						
					}
					else
					{
						if(RedirectX != 0)
						{
							if(RedirectY != 0)
							{
								check.Set("Location",RedirectX.ToString() + "," + RedirectY.ToString());
								tPresence.Scene.RegionInfo.RegionLocX = Convert.ToUInt32(RedirectX);
								tPresence.Scene.RegionInfo.RegionLocY = Convert.ToUInt32(RedirectY);
							}
						}
					}
				}
				else
				{
				}
				i++;
			}
			
		}
	}
}
