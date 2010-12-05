/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Timers;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository;
using log4net.Config;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Framework.Statistics;
using OpenSim.Region.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Physics.Manager;
using Aurora.Framework;
using Aurora.Simulation.Base;

namespace OpenSim
{
	/// <summary>
	/// Common OpenSimulator simulator code
	/// </summary>
    public class OpenSimBase: SimulationBase
    {
        protected SceneManager m_sceneManager = null;
        public override void Configuration(IConfigSource configSource)
        {
            IConfig startupConfig = m_config.Configs["Startup"];

            int stpMaxThreads = 15;

            if (startupConfig != null)
            {
                m_startupCommandsFile = startupConfig.GetString("startup_console_commands_file", "startup_commands.txt");
                m_shutdownCommandsFile = startupConfig.GetString("shutdown_console_commands_file", "shutdown_commands.txt");

                m_TimerScriptFileName = startupConfig.GetString("timer_Script", "disabled");
                m_TimerScriptTime = startupConfig.GetInt("timer_time", m_TimerScriptTime);
                if (m_TimerScriptTime < 5) //Limit for things like backup and etc...
                    m_TimerScriptTime = 5;

                string pidFile = startupConfig.GetString("PIDFile", String.Empty);
                if (pidFile != String.Empty)
                    CreatePIDFile(pidFile);
            }

            IConfig SystemConfig = m_config.Configs["System"];
            if (SystemConfig != null)
            {
                string asyncCallMethodStr = SystemConfig.GetString("AsyncCallMethod", String.Empty);
                FireAndForgetMethod asyncCallMethod;
                if (!String.IsNullOrEmpty(asyncCallMethodStr) && Utils.EnumTryParse<FireAndForgetMethod>(asyncCallMethodStr, out asyncCallMethod))
                    Util.FireAndForgetMethod = asyncCallMethod;

                stpMaxThreads = SystemConfig.GetInt("MaxPoolThreads", 15);
            }

            if (Util.FireAndForgetMethod == FireAndForgetMethod.SmartThreadPool)
                Util.InitThreadPool(stpMaxThreads);
        }

        public override void StartModules()
        {
            List<IApplicationPlugin> plugins = AuroraModuleLoader.PickupModules<IApplicationPlugin>();
            foreach (IApplicationPlugin plugin in plugins)
            {
                plugin.Initialize(this);
            }
            m_sceneManager = ApplicationRegistry.Get<SceneManager>();

            foreach (IApplicationPlugin plugin in plugins)
            {
                plugin.PostInitialise();
            }
        }

		#region Console Commands

        /// <summary>
        /// Register standard set of region console commands
        /// </summary>
        public override void RegisterConsoleCommands()
        {
            base.RegisterConsoleCommands();
            //m_console.Commands.AddCommand("region", false, "clear assets", "clear assets", "Clear the asset cache", HandleClearAssets);

            m_console.Commands.AddCommand("region", false, "force update", "force update", "Force the update of all objects on clients", HandleForceUpdate);

            m_console.Commands.AddCommand("region", false, "debug packet", "debug packet <level>", "Turn on packet debugging", Debug);

            m_console.Commands.AddCommand("region", false, "debug scene", "debug scene <cripting> <collisions> <physics>", "Turn on scene debugging", Debug);

            m_console.Commands.AddCommand("region", false, "change region", "change region <region name>", "Change current console region", ChangeSelectedRegion);

            m_console.Commands.AddCommand("region", false, "load xml2", "load xml2", "Load a region's data from XML2 format", LoadXml2);

            m_console.Commands.AddCommand("region", false, "save xml2", "save xml2", "Save a region's data in XML2 format", SaveXml2);

            m_console.Commands.AddCommand("region", false, "load oar", "load oar [--merge] [--skip-assets] <oar name>", "Load a region's data from OAR archive.  --merge will merge the oar with the existing scene.  --skip-assets will load the oar but ignore the assets it contains", LoadOar);

            m_console.Commands.AddCommand("region", false, "save oar", "save oar [-v|--version=N] [<OAR path>]", "Save a region's data to an OAR archive", "-v|--version=N generates scene objects as per older versions of the serialization (e.g. -v=0)" + Environment.NewLine
                                           + "The OAR path must be a filesystem path."
                                           + "  If this is not given then the oar is saved to region.oar in the current directory.", SaveOar);

            m_console.Commands.AddCommand("region", false, "kick user", "kick user <first> <last> [message]", "Kick a user off the simulator", KickUserCommand);

            m_console.Commands.AddCommand("region", false, "backup", "backup [all]", "Persist objects to the database now, if [all], will force the persistence of all prims", RunCommand);

            m_console.Commands.AddCommand("region", false, "reset region", "reset region", "Reset region to the default terrain, wipe all prims, etc.", RunCommand);

            m_console.Commands.AddCommand("region", false, "create region", "create region", "Create a new region.", HandleCreateRegion);

            m_console.Commands.AddCommand("region", false, "restart", "restart", "Restart the current sim selected (all if root)", RunCommand);

            m_console.Commands.AddCommand("region", false, "restart-instance", "restart-instance", "Restarts the instance (as if you closed and re-opened Aurora)", RunCommand);
            
            m_console.Commands.AddCommand("region", false, "command-script", "command-script <script>", "Run a command script from file", RunCommand);
            
            m_console.Commands.AddCommand("region", false, "remove-region", "remove-region <name>", "Remove a region from this simulator", RunCommand);

            m_console.Commands.AddCommand("region", false, "delete-region", "delete-region <name>", "Delete a region from disk", RunCommand);

            m_console.Commands.AddCommand("region", false, "modules", "modules help", "Info about simulator modules", HandleModules);

            m_console.Commands.AddCommand("region", false, "kill uuid", "kill uuid <UUID>", "Kill an object by UUID", KillUUID);

        }

        protected virtual List<string> GetHelpTopics()
        {
            List<string> topics = new List<string>();
            Scene s = m_sceneManager.CurrentOrFirstScene;
            if (s != null && s.GetCommanders() != null)
                topics.AddRange(s.GetCommanders().Keys);

            return topics;
        }

        /// <summary>
		/// Kicks users off the region
		/// </summary>
		/// <param name="module"></param>
		/// <param name="cmdparams">name of avatar to kick</param>
		private void KickUserCommand(string module, string[] cmdparams)
		{
            string alert = null;
            IList agents = m_sceneManager.GetCurrentSceneAvatars();

            if (cmdparams.Length < 4)
            {
                if (cmdparams.Length < 3)
                    return;
                if (cmdparams[2] == "all")
                {
                    foreach (ScenePresence presence in agents)
                    {
                        RegionInfo regionInfo = presence.Scene.RegionInfo;

                        MainConsole.Instance.Output(String.Format("Kicking user: {0,-16}{1,-16}{2,-37} in region: {3,-16}", presence.Firstname, presence.Lastname, presence.UUID, regionInfo.RegionName));

                            // kick client...
                            if (alert != null)
                                presence.ControllingClient.Kick(alert);
                            else
                                presence.ControllingClient.Kick("\nThe OpenSim manager kicked you out.\n");

                            // ...and close on our side
                            presence.Scene.IncomingCloseAgent(presence.UUID);
                    }
                }
            }

            if (cmdparams.Length > 4)
                alert = String.Format("\n{0}\n", String.Join(" ", cmdparams, 4, cmdparams.Length - 4));

            foreach (ScenePresence presence in agents)
            {
				RegionInfo regionInfo = presence.Scene.RegionInfo;

                if (presence.Firstname.ToLower().StartsWith(cmdparams[2].ToLower()) && presence.Lastname.ToLower().StartsWith(cmdparams[3].ToLower()))
                {
					MainConsole.Instance.Output(String.Format("Kicking user: {0,-16}{1,-16}{2,-37} in region: {3,-16}", presence.Firstname, presence.Lastname, presence.UUID, regionInfo.RegionName));

					// kick client...
					if (alert != null)
						presence.ControllingClient.Kick(alert);
					else
						presence.ControllingClient.Kick("\nThe OpenSim manager kicked you out.\n");

					// ...and close on our side
					presence.Scene.IncomingCloseAgent(presence.UUID);
				}
			}
			MainConsole.Instance.Output("");
		}

		private void HandleClearAssets(string module, string[] args)
		{
			MainConsole.Instance.Output("Not implemented.");
		}

		/// <summary>
		/// Force resending of all updates to all clients in active region(s)
		/// </summary>
		/// <param name="module"></param>
		/// <param name="args"></param>
		private void HandleForceUpdate(string module, string[] args)
		{
			MainConsole.Instance.Output("Updating all clients");
			m_sceneManager.ForceCurrentSceneClientUpdate();
		}

		/// <summary>
		/// Creates a new region based on the parameters specified.   This will ask the user questions on the console
		/// </summary>
		/// <param name="module"></param>
		/// <param name="cmd">0,1,region name, region XML file</param>
		private void HandleCreateRegion(string module, string[] cmd)
		{
            List<IRegionLoader> regionLoaders = AuroraModuleLoader.PickupModules<IRegionLoader>();
            foreach (IRegionLoader loader in regionLoaders)
            {
                loader.Initialise(ConfigSource, null, this);
                loader.AddRegion(this, cmd);
			}
		}

		/// <summary>
		/// Load, Unload, and list Region modules in use
		/// </summary>
		/// <param name="module"></param>
		/// <param name="cmd"></param>
		private void HandleModules(string module, string[] cmd)
		{
            List<string> args = new List<string>(cmd);
			args.RemoveAt(0);
			string[] cmdparams = args.ToArray();

            IRegionModulesController controller = m_applicationRegistry.Get<IRegionModulesController>();
            if (cmdparams.Length > 0)
            {
				switch (cmdparams[0].ToLower()) 
                {
                    case "help":
                        MainConsole.Instance.Output("modules list - List modules", "noTimeStamp");
                        MainConsole.Instance.Output("modules unload - Unload a module", "noTimeStamp");
                        break;
					case "list":
                        foreach (IRegionModuleBase irm in controller.AllModules)
                        {
                            if(irm is ISharedRegionModule)
							    MainConsole.Instance.Output(String.Format("Shared region module: {0}", irm.Name));
                            else if (irm is INonSharedRegionModule)
                                MainConsole.Instance.Output(String.Format("Nonshared region module: {0}", irm.Name));
                            else
                                MainConsole.Instance.Output(String.Format("Unknown type " + irm.GetType().ToString() + " region module: {0}", irm.Name));
						}

						break;
					case "unload":
						if (cmdparams.Length > 1)
                        {
                            foreach (IRegionModuleBase irm in controller.AllModules)
                            {
                                if (irm.Name.ToLower() == cmdparams[1].ToLower())
                                {
									MainConsole.Instance.Output(String.Format("Unloading module: {0}", irm.Name));
                                    foreach (Scene scene in m_sceneManager.Scenes)
                                        irm.RemoveRegion(scene);
                                    irm.Close();
								}
							}
						}
						break;
				}
			}
		}

        /// <summary>
        /// Serialize region data to XML2Format
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cmdparams"></param>
        protected void SaveXml2(string module, string[] cmdparams)
        {
            if (cmdparams.Length > 2)
            {
                m_sceneManager.SaveCurrentSceneToXml2(cmdparams[2]);
            }
            else
            {
                m_log.Warn("Wrong number of parameters!");
            }
        }

		/// <summary>
		/// Runs commands issued by the server console from the operator
		/// </summary>
		/// <param name="command">The first argument of the parameter (the command)</param>
		/// <param name="cmdparams">Additional arguments passed to the command</param>
		public void RunCommand(string module, string[] cmdparams)
		{
			List<string> args = new List<string>(cmdparams);
			if (args.Count < 1)
				return;

			string command = args[0];
			args.RemoveAt(0);

			cmdparams = args.ToArray();

			switch (command) {
                case "reset":
                    if (cmdparams.Length > 0) 
						if(cmdparams[0] == "region")
                            m_sceneManager.ResetScene();
                    break;
				case "command-script":
					if (cmdparams.Length > 0) {
						RunCommandScript(cmdparams[0]);
					}
					break;

				case "backup":
                    m_sceneManager.BackupCurrentScene(args.Count == 1);
					break;

				case "remove-region":
					string regRemoveName = CombineParams(cmdparams, 0);

					Scene removeScene;
					if (m_sceneManager.TryGetScene(regRemoveName, out removeScene))
                        m_sceneManager.RemoveRegion(removeScene, false);
					else
						MainConsole.Instance.Output("no region with that name");
					break;

				case "delete-region":
					string regDeleteName = CombineParams(cmdparams, 0);

					Scene killScene;
					if (m_sceneManager.TryGetScene(regDeleteName, out killScene))
                        m_sceneManager.RemoveRegion(killScene, true);
					else
						MainConsole.Instance.Output("no region with that name");
					break;

				case "restart":
					m_sceneManager.RestartCurrentScene();
					break;
                case "restart-instance":
                    //This kills the instance and restarts it
                    MainConsole.Instance.EndConsoleProcessing();
                    break;
			}
		}

		/// <summary>
		/// Change the currently selected region.  The selected region is that operated upon by single region commands.
		/// </summary>
		/// <param name="cmdParams"></param>
		protected void ChangeSelectedRegion(string module, string[] cmdparams)
		{
			if (cmdparams.Length > 2)
            {
                string newRegionName = CombineParams(cmdparams, 2);
                m_sceneManager.ChangeSelectedRegion(newRegionName);
			} 
            else 
            {
				MainConsole.Instance.Output("Usage: change region <region name>");
			}
		}

		/// <summary>
		/// Turn on some debugging values for OpenSim.
		/// </summary>
		/// <param name="args"></param>
		protected void Debug(string module, string[] args)
		{
			if (args.Length == 1)
				return;

			switch (args[1]) {
				case "packet":
					if (args.Length > 2) {
						int newDebug;
						if (int.TryParse(args[2], out newDebug)) {
							m_sceneManager.SetDebugPacketLevelOnCurrentScene(newDebug);
						} else {
							MainConsole.Instance.Output("packet debug should be 0..255");
						}
						MainConsole.Instance.Output(String.Format("New packet debug: {0}", newDebug));
					}

					break;

				case "scene":
					if (args.Length == 5) {
						if (m_sceneManager.CurrentScene == null) {
							MainConsole.Instance.Output("Please use 'change region <regioname>' first");
						} else {
							bool scriptingOn = !Convert.ToBoolean(args[2]);
							bool collisionsOn = !Convert.ToBoolean(args[3]);
							bool physicsOn = !Convert.ToBoolean(args[4]);
							m_sceneManager.CurrentScene.SetSceneCoreDebug(scriptingOn, collisionsOn, physicsOn);

							MainConsole.Instance.Output(String.Format("Set debug scene scripting = {0}, collisions = {1}, physics = {2}", !scriptingOn, !collisionsOn, !physicsOn));
						}
					} else {
						MainConsole.Instance.Output("debug scene <scripting> <collisions> <physics> (where inside <> is true/false)");
					}

					break;
				default:

					MainConsole.Instance.Output("Unknown debug");
					break;
			}
		}

        /// <summary>
		/// Many commands list objects for debugging.  Some of the types are listed  here
		/// </summary>
		/// <param name="mod"></param>
		/// <param name="cmd"></param>
        public override void HandleShow(string mod, string[] cmd)
		{
            base.HandleShow(mod, cmd);
            if (cmd.Length == 1)
            {
                m_log.Warn("Incorrect number of parameters!");
                return;
            }
			List<string> args = new List<string>(cmd);
			args.RemoveAt(0);
			string[] showParams = args.ToArray();
            switch (showParams[0]) 
            {
                case "assets":
					MainConsole.Instance.Output("Not implemented.");
					break;

				case "users":
					IList agents;
					if (showParams.Length > 1 && showParams[1] == "full") {
						agents = m_sceneManager.GetCurrentScenePresences();
					} else {
						agents = m_sceneManager.GetCurrentSceneAvatars();
					}

					MainConsole.Instance.Output(String.Format("\nAgents connected: {0}\n", agents.Count));

					MainConsole.Instance.Output(String.Format("{0,-16}{1,-16}{2,-37}{3,-11}{4,-16}{5,-30}", "Firstname", "Lastname", "Agent ID", "Root/Child", "Region", "Position"));

					foreach (ScenePresence presence in agents) {
						RegionInfo regionInfo = presence.Scene.RegionInfo;
						string regionName;

						if (regionInfo == null) {
							regionName = "Unresolvable";
						} else {
							regionName = regionInfo.RegionName;
						}

						MainConsole.Instance.Output(String.Format("{0,-16}{1,-16}{2,-37}{3,-11}{4,-16}{5,-30}", presence.Firstname, presence.Lastname, presence.UUID, presence.IsChildAgent ? "Child" : "Root", regionName, presence.AbsolutePosition.ToString()));
					}


                    MainConsole.Instance.Output(String.Empty);
                    MainConsole.Instance.Output(String.Empty);
					break;

				case "connections":
					System.Text.StringBuilder connections = new System.Text.StringBuilder("Connections:\n");
					m_sceneManager.ForEachScene(delegate(Scene scene) { scene.ForEachClient(delegate(IClientAPI client) { connections.AppendFormat("{0}: {1} ({2}) from {3} on circuit {4}\n", scene.RegionInfo.RegionName, client.Name, client.AgentId, client.RemoteEndPoint, client.CircuitCode); }); });

					MainConsole.Instance.Output(connections.ToString());
					break;

				case "regions":
					m_sceneManager.ForEachScene(delegate(Scene scene) { MainConsole.Instance.Output(String.Format("Region Name: {0}, Region XLoc: {1}, Region YLoc: {2}, Region Port: {3}", scene.RegionInfo.RegionName, scene.RegionInfo.RegionLocX, scene.RegionInfo.RegionLocY, scene.RegionInfo.InternalEndPoint.Port)); });
					break;

				case "queues":
                    m_console.Output((GetQueuesReport(showParams)));
					break;

				case "maturity":
					m_sceneManager.ForEachScene(delegate(Scene scene) {
						string rating = "";
						if (scene.RegionInfo.RegionSettings.Maturity == 1) {
							rating = "MATURE";
						} else if (scene.RegionInfo.RegionSettings.Maturity == 2) {
							rating = "ADULT";
						} else {
							rating = "PG";
						}
						MainConsole.Instance.Output(String.Format("Region Name: {0}, Region Rating {1}", scene.RegionInfo.RegionName, rating));
					});
					break;
			}
		}

		/// <summary>
		/// print UDP Queue data for each client
		/// </summary>
		/// <returns></returns>
        private string GetQueuesReport(string[] showParams)
		{
            bool showChildren = false;

            if (showParams.Length > 1 && showParams[1] == "full")
                showChildren = true;

			StringBuilder report = new StringBuilder();

            int columnPadding = 2;
            int maxNameLength = 18;
            int maxRegionNameLength = 14;
            int maxTypeLength = 4;
            int totalInfoFieldsLength = maxNameLength + columnPadding + maxRegionNameLength + columnPadding + maxTypeLength + columnPadding;

            report.AppendFormat("{0,-" + maxNameLength +  "}{1,-" + columnPadding + "}", "User", "");
            report.AppendFormat("{0,-" + maxRegionNameLength +  "}{1,-" + columnPadding + "}", "Region", "");
            report.AppendFormat("{0,-" + maxTypeLength +  "}{1,-" + columnPadding + "}", "Type", "");

            report.AppendFormat(
                "{0,9} {1,9} {2,9} {3,8} {4,7} {5,7} {6,7} {7,7} {8,9} {9,7} {10,7}\n",
                "Packets",
                "Packets",
                "Packets",
                "Bytes",
                "Bytes",
                "Bytes",
                "Bytes",
                "Bytes",
                "Bytes",
                "Bytes",
                "Bytes");

            report.AppendFormat("{0,-" + totalInfoFieldsLength +  "}", "");
            report.AppendFormat(
                "{0,9} {1,9} {2,9} {3,8} {4,7} {5,7} {6,7} {7,7} {8,9} {9,7} {10,7}\n",
                "Out",
                "In",
                "Unacked",
                "Resend",
                "Land",
                "Wind",
                "Cloud",
                "Task",
                "Texture",
                "Asset",
                "State");

            m_sceneManager.ForEachScene(
                delegate(Scene scene)
                {
                    scene.ForEachClient(
                        delegate(IClientAPI client)
                        {
                            if (client is IStatsCollector)
                            {
                                bool isChild = scene.PresenceChildStatus(client.AgentId);
                                if (isChild && !showChildren)
                                    return;

                                string name = client.Name;
                                string regionName = scene.RegionInfo.RegionName;

                                report.AppendFormat(
                                    "{0,-" + maxNameLength + "}{1,-" + columnPadding + "}",
                                    name.Length > maxNameLength ? name.Substring(0, maxNameLength) : name, "");
                                report.AppendFormat(
                                    "{0,-" + maxRegionNameLength + "}{1,-" + columnPadding + "}",
                                    regionName.Length > maxRegionNameLength ? regionName.Substring(0, maxRegionNameLength) : regionName, "");
                                report.AppendFormat(
                                    "{0,-" + maxTypeLength + "}{1,-" + columnPadding + "}",
                                    isChild ? "Child" : "Root", "");

                                IStatsCollector stats = (IStatsCollector)client;

                                report.AppendLine(stats.Report());
                            }
                        });
                });

            return report.ToString();
		}

		/// <summary>
		/// Load region data from Xml2Format
		/// </summary>
		/// <param name="module"></param>
		/// <param name="cmdparams"></param>
		protected void LoadXml2(string module, string[] cmdparams)
		{
			if (cmdparams.Length > 2) {
				try {
					m_sceneManager.LoadCurrentSceneFromXml2(cmdparams[2]);
				} catch (FileNotFoundException) {
					MainConsole.Instance.Output("Specified xml not found. Usage: load xml2 <filename>");
				}
			} else {
				m_log.Warn("Not enough parameters!");
			}
		}

		/// <summary>
		/// Load a whole region from an opensimulator archive.
		/// </summary>
		/// <param name="cmdparams"></param>
		protected void LoadOar(string module, string[] cmdparams)
		{
			try {
				m_sceneManager.LoadArchiveToCurrentScene(cmdparams);
			} catch (Exception e) {
				MainConsole.Instance.Output(e.Message);
			}
		}

		/// <summary>
		/// Save a region to a file, including all the assets needed to restore it.
		/// </summary>
		/// <param name="cmdparams"></param>
		protected void SaveOar(string module, string[] cmdparams)
		{
			m_sceneManager.SaveCurrentSceneToArchive(cmdparams);
		}

		private static string CombineParams(string[] commandParams, int pos)
		{
			string result = String.Empty;
			for (int i = pos; i < commandParams.Length; i++) {
				result += commandParams[i] + " ";
			}
			result = result.TrimEnd(' ');
			return result;
		}

		/// <summary>
		/// Kill an object given its UUID.
		/// </summary>
		/// <param name="cmdparams"></param>
		protected void KillUUID(string module, string[] cmdparams)
		{
			if (cmdparams.Length > 2) {
				UUID id = UUID.Zero;
				SceneObjectGroup grp = null;
				Scene sc = null;

				if (!UUID.TryParse(cmdparams[2], out id)) {
					MainConsole.Instance.Output("[KillUUID]: Error bad UUID format!");
					return;
				}

				m_sceneManager.ForEachScene(delegate(Scene scene) {
					SceneObjectPart part = scene.GetSceneObjectPart(id);
					if (part == null)
						return;

					grp = part.ParentGroup;
					sc = scene;
				});

				if (grp == null) {
					MainConsole.Instance.Output(String.Format("[KillUUID]: Given UUID {0} not found!", id));
				} else {
					MainConsole.Instance.Output(String.Format("[KillUUID]: Found UUID {0} in scene {1}", id, sc.RegionInfo.RegionName));
					try {
                        sc.DeleteSceneObject(grp, false, true);
					} catch (Exception e) {
						m_log.ErrorFormat("[KillUUID]: Error while removing objects from scene: " + e);
					}
				}
			} else {
				MainConsole.Instance.Output("[KillUUID]: Usage: kill uuid <UUID>");
			}
		}

        public override void AddPluginCommands()
        {
            // If console exists add plugin commands.
            if (m_console != null)
            {
                List<string> topics = GetHelpTopics();

                foreach (string topic in topics)
                {
                    m_console.Commands.AddCommand("plugin", false, "help " + topic, "help " + topic, "Get help on plugin command '" + topic + "'", HandleCommanderHelp);

                    m_console.Commands.AddCommand("plugin", false, topic, topic, "Execute subcommand for plugin '" + topic + "'", null);

                    ICommander commander = null;

                    Scene s = m_sceneManager.CurrentOrFirstScene;

                    if (s != null && s.GetCommanders() != null)
                    {
                        if (s.GetCommanders().ContainsKey(topic))
                            commander = s.GetCommanders()[topic];
                    }

                    if (commander == null)
                        continue;

                    foreach (string command in commander.Commands.Keys)
                    {
                        m_console.Commands.AddCommand(topic, false, topic + " " + command, topic + " " + commander.Commands[command].ShortHelp(), String.Empty, HandleCommanderCommand);
                    }
                }
            }
        }

        private void HandleCommanderCommand(string module, string[] cmd)
        {
            m_sceneManager.SendCommandToPluginModules(cmd);
        }

        private void HandleCommanderHelp(string module, string[] cmd)
        {
            // Only safe for the interactive console, since it won't
            // let us come here unless both scene and commander exist
            //
            ICommander moduleCommander = m_sceneManager.CurrentOrFirstScene.GetCommander(cmd[1]);
            if (moduleCommander != null)
                m_console.Output(moduleCommander.Help);
        }


        #endregion

        /// <summary>
        /// Should be overriden and referenced by descendents if they need to perform extra shutdown processing
        /// Performs any last-minute sanity checking and shuts down the region server
        /// </summary>
        public override void Shutdown(bool close)
        {
            try
            {
                //Close the thread pool
                Util.CloseThreadPool();
            }
            catch
            {
                //Just shut down already
            }
            base.Shutdown(close);
        }
    }
}
