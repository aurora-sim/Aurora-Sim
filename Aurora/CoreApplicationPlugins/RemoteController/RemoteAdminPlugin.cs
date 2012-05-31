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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Net;
using System.Reflection;
using System.Timers;
using System.Threading;
using Nini.Config;
using Nwc.XmlRpc;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.CoreApplicationPlugins
{
    public class RemoteAdminPlugin : IApplicationPlugin
    {
        private static bool m_defaultAvatarsLoaded = false;
        private static readonly Object   m_requestLock = new Object();
        private static readonly Object   m_saveOarLock = new Object();

        private ISimulationBase m_application;
        private ISceneManager manager;
        private IHttpServer m_httpServer;
        private IConfig m_config;
        private IConfigSource m_configSource;
        private string m_requiredPassword = String.Empty;

        private const string m_name = "RemoteAdminPlugin";
        private const string m_version = "0.0";
        private bool m_enabled = false;

        //guard for XmlRpc-related methods
        private void FailIfRemoteAdminDisabled(string requestName)
        {
            if (m_config == null)
            {
                string errorMessage = String.Format("[RADMIN] {0}: Remote admin request denied! Please set [RemoteAdmin]enabled=true in aurora.ini in order to enable remote admin functionality", requestName);
                MainConsole.Instance.Error(errorMessage);
                throw new ApplicationException(errorMessage);
            }
        }

        public string Version
        {
            get { return m_version; }
        }

        public string Name
        {
            get { return m_name; }
        }

        public void PreStartup(ISimulationBase simBase)
        {
        }

        public void Initialize(ISimulationBase openSim)
        {
            IConfig handlerConfig = openSim.ConfigSource.Configs["ApplicationPlugins"];
            if (handlerConfig.GetString("RemoteAdminPlugin", "") != Name)
                return;

            m_configSource = openSim.ConfigSource;
            try
            {
                if (m_configSource.Configs["RemoteAdmin"] == null ||
                    !m_configSource.Configs["RemoteAdmin"].GetBoolean("enabled", false))
                {
                    // No config or disabled
                }
                else
                {
                    m_enabled = true;
                    m_config = m_configSource.Configs["RemoteAdmin"];
                    MainConsole.Instance.Info("[RADMIN]: Remote Admin Plugin Enabled");
                    m_requiredPassword = m_config.GetString("access_password", String.Empty);
                    int port = m_config.GetInt("port", 0);

                    m_application = openSim;
                    m_httpServer = m_application.GetHttpServer((uint)port);

                    Dictionary<string, XmlRpcMethod> availableMethods = new Dictionary<string, XmlRpcMethod>();
                    availableMethods["admin_create_region"] = XmlRpcCreateRegionMethod;
                    availableMethods["admin_delete_region"] = XmlRpcDeleteRegionMethod;
                    availableMethods["admin_close_region"] = XmlRpcCloseRegionMethod;
                    availableMethods["admin_modify_region"] = XmlRpcModifyRegionMethod;
                    availableMethods["admin_region_query"] = XmlRpcRegionQueryMethod;
                    availableMethods["admin_shutdown"] = XmlRpcShutdownMethod;
                    availableMethods["admin_broadcast"] = XmlRpcAlertMethod;
                    availableMethods["admin_restart"] = XmlRpcRestartMethod;
                    availableMethods["admin_load_heightmap"] = XmlRpcLoadHeightmapMethod;
                    // User management
                    availableMethods["admin_create_user"] = XmlRpcCreateUserMethod;
                    availableMethods["admin_create_user_email"] = XmlRpcCreateUserMethod;
                    availableMethods["admin_exists_user"] = XmlRpcUserExistsMethod;
                    availableMethods["admin_update_user"] = XmlRpcUpdateUserAccountMethod;
                    // Region state management
                    availableMethods["admin_load_xml"] = XmlRpcLoadXMLMethod;
                    availableMethods["admin_save_xml"] = XmlRpcSaveXMLMethod;
                    availableMethods["admin_load_oar"] = XmlRpcLoadOARMethod;
                    availableMethods["admin_save_oar"] = XmlRpcSaveOARMethod;
                    // Estate access list management
                    availableMethods["admin_acl_clear"] = XmlRpcAccessListClear;
                    availableMethods["admin_acl_add"] = XmlRpcAccessListAdd;
                    availableMethods["admin_acl_remove"] = XmlRpcAccessListRemove;
                    availableMethods["admin_acl_list"] = XmlRpcAccessListList;

                    // Either enable full remote functionality or just selected features
                    string enabledMethods = m_config.GetString("enabled_methods", "all");

                    // To get this, you must explicitly specify "all" or
                    // mention it in a whitelist. It won't be available
                    // If you just leave the option out!
                    //
                    if (!String.IsNullOrEmpty(enabledMethods))
                        availableMethods["admin_console_command"] = XmlRpcConsoleCommandMethod;

                    // The assumption here is that simply enabling Remote Admin as before will produce the same
                    // behavior - enable all methods unless the whitelist is in place for backward-compatibility.
                    if (enabledMethods != null && (enabledMethods.ToLower() == "all" || String.IsNullOrEmpty(enabledMethods)))
                    {
                        foreach (string method in availableMethods.Keys)
                        {
                            m_httpServer.AddXmlRPCHandler(method, availableMethods[method], false);
                        }
                    }
                    else
                    {
                        if (enabledMethods != null)
                            foreach (string enabledMethod in enabledMethods.Split('|'))
                            {
                                m_httpServer.AddXmlRPCHandler(enabledMethod, availableMethods[enabledMethod]);
                            }
                    }
                }
            }
            catch (NullReferenceException)
            {
                // Ignore.
            }
        }

        public void ReloadConfiguration(IConfigSource config)
        {
        }

        public void PostInitialise()
        {
        }

        public void Start()
        {
        }

        public void PostStart()
        {
            if (m_enabled)
            {
                manager = m_application.ApplicationRegistry.RequestModuleInterface<ISceneManager>();
                if (!CreateDefaultAvatars())
                {
                    MainConsole.Instance.Info("[RADMIN]: Default avatars not loaded");
                }
            }
        }

        public void Close()
        {
        }

        public XmlRpcResponse XmlRpcRestartMethod(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            try
            {
                Hashtable requestData = (Hashtable)request.Params[0];

                MainConsole.Instance.Info("[RADMIN]: Request to restart Region.");
                CheckStringParameters(request, new[] { "password", "regionID" });

                if (m_requiredPassword != String.Empty &&
                    (!requestData.Contains("password") || (string)requestData["password"] != m_requiredPassword))
                {
                    throw new Exception("wrong password");
                }

                UUID regionID = new UUID((string)requestData["regionID"]);

                IScene rebootedScene;

                responseData["success"] = false;
                responseData["accepted"] = true;
                if (!manager.TryGetScene(regionID, out rebootedScene))
                    throw new Exception("region not found");

                responseData["rebooting"] = true;

                IRestartModule restartModule = rebootedScene.RequestModuleInterface<IRestartModule>();
                if (restartModule != null)
                {
                    List<int> times = new List<int> { 30, 15 };

                    restartModule.ScheduleRestart(UUID.Zero, "Region will restart in {0}", times.ToArray(), true);
                    responseData["success"] = true;
                }
                response.Value = responseData;
            }
            catch (Exception e)
            {
                MainConsole.Instance.ErrorFormat("[RADMIN]: Restart region: failed: {0}", e.Message);
                MainConsole.Instance.DebugFormat("[RADMIN]: Restart region: failed: {0}", e);
                responseData["accepted"] = false;
                responseData["success"] = false;
                responseData["rebooting"] = false;
                responseData["error"] = e.Message;
                response.Value = responseData;
            }

            MainConsole.Instance.Info("[RADMIN]: Restart Region request complete");
            return response;
        }

        public XmlRpcResponse XmlRpcAlertMethod(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            MainConsole.Instance.Info("[RADMIN]: Alert request started");

            try
            {
                Hashtable requestData = (Hashtable) request.Params[0];

                CheckStringParameters(request, new[] {"password", "message"});

                if (m_requiredPassword != String.Empty &&
                    (!requestData.Contains("password") || (string) requestData["password"] != m_requiredPassword))
                    throw new Exception("wrong password");

                string message = (string) requestData["message"];
                MainConsole.Instance.InfoFormat("[RADMIN]: Broadcasting: {0}", message);

                responseData["accepted"] = true;
                responseData["success"] = true;
                response.Value = responseData;

                foreach (IScene scene in manager.GetAllScenes())
                {
                    IDialogModule dialogModule = scene.RequestModuleInterface<IDialogModule>();
                    if (dialogModule != null)
                        dialogModule.SendGeneralAlert(message);
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.ErrorFormat("[RADMIN]: Broadcasting: failed: {0}", e.Message);
                MainConsole.Instance.DebugFormat("[RADMIN]: Broadcasting: failed: {0}", e);

                responseData["accepted"] = false;
                responseData["success"] = false;
                responseData["error"] = e.Message;
                response.Value = responseData;
            }

            MainConsole.Instance.Info("[RADMIN]: Alert request complete");
            return response;
        }

        public XmlRpcResponse XmlRpcLoadHeightmapMethod(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            MainConsole.Instance.Info("[RADMIN]: Load height maps request started");

            try
            {
                Hashtable requestData = (Hashtable) request.Params[0];

                MainConsole.Instance.DebugFormat("[RADMIN]: Load Terrain: XmlRpc {0}", request);
                // foreach (string k in requestData.Keys)
                // {
                //     MainConsole.Instance.DebugFormat("[RADMIN]: Load Terrain: XmlRpc {0}: >{1}< {2}",
                //                       k, (string)requestData[k], ((string)requestData[k]).Length);
                // }

                CheckStringParameters(request, new[] {"password", "filename", "regionid"});

                if (m_requiredPassword != String.Empty &&
                    (!requestData.Contains("password") || (string) requestData["password"] != m_requiredPassword))
                    throw new Exception("wrong password");

                string file = (string) requestData["filename"];
                UUID regionID = (UUID) (string) requestData["regionid"];
                MainConsole.Instance.InfoFormat("[RADMIN]: Terrain Loading: {0}", file);

                responseData["accepted"] = true;

                IScene region = null;

                if (!manager.TryGetScene(regionID, out region))
                    throw new Exception("1: unable to get a scene with that name");

                ITerrainModule terrainModule = region.RequestModuleInterface<ITerrainModule>();
                if (null == terrainModule) throw new Exception("terrain module not available");
                if (Uri.IsWellFormedUriString(file, UriKind.Absolute))
                {
                    MainConsole.Instance.Info("[RADMIN]: Terrain path is URL");
                    Uri result;
                    if (Uri.TryCreate(file, UriKind.RelativeOrAbsolute, out result))
                    {
                        // the url is valid
                        string fileType = file.Substring(file.LastIndexOf('/') + 1);
                        terrainModule.LoadFromStream(fileType, result);
                    }
                }
                else
                {
                    terrainModule.LoadFromFile(file, 0, 0);
                }
                responseData["success"] = false;

                response.Value = responseData;
            }
            catch (Exception e)
            {
                MainConsole.Instance.ErrorFormat("[RADMIN]: Terrain Loading: failed: {0}", e.Message);
                MainConsole.Instance.DebugFormat("[RADMIN]: Terrain Loading: failed: {0}", e);

                responseData["success"] = false;
                responseData["error"] = e.Message;
            }

            MainConsole.Instance.Info("[RADMIN]: Load height maps request complete");

            return response;
        }

        public XmlRpcResponse XmlRpcShutdownMethod(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            MainConsole.Instance.Info("[RADMIN]: Received Shutdown Administrator Request");

            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            try
            {
                Hashtable requestData = (Hashtable) request.Params[0];

                if (m_requiredPassword != String.Empty &&
                    (!requestData.Contains("password") || (string) requestData["password"] != m_requiredPassword))
                    throw new Exception("wrong password");

                responseData["accepted"] = true;
                response.Value = responseData;

                int timeout = 2000;
                string message;

                if (requestData.ContainsKey("shutdown")
                    && ((string) requestData["shutdown"] == "delayed")
                    && requestData.ContainsKey("milliseconds"))
                {
                    timeout = Int32.Parse(requestData["milliseconds"].ToString());

                    message
                        = "Region is going down in " + ((int) (timeout/1000)).ToString()
                          + " second(s). Please save what you are doing and log out.";
                }
                else
                {
                    message = "Region is going down now.";
                }

                foreach (IScene scene in manager.GetAllScenes())
                {
                    IDialogModule dialogModule = scene.RequestModuleInterface<IDialogModule>();
                    if (dialogModule != null)
                        dialogModule.SendGeneralAlert(message);
                }

                // Perform shutdown
                System.Timers.Timer shutdownTimer = new System.Timers.Timer(timeout) {AutoReset = false};
                    // Wait before firing
                shutdownTimer.Elapsed += shutdownTimer_Elapsed;
                lock (shutdownTimer)
                {
                    shutdownTimer.Start();
                }

                responseData["success"] = true;
            }
            catch (Exception e)
            {
                MainConsole.Instance.ErrorFormat("[RADMIN] Shutdown: failed: {0}", e.Message);
                MainConsole.Instance.DebugFormat("[RADMIN] Shutdown: failed: {0}", e);

                responseData["accepted"] = false;
                responseData["error"] = e.Message;

                response.Value = responseData;
            }
            MainConsole.Instance.Info("[RADMIN]: Shutdown Administrator Request complete");
            return response;
        }

        private void shutdownTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            m_application.Shutdown(true);
        }

        /// <summary>
        /// Create a new region.
        /// <summary>
        /// <param name="request">incoming XML RPC request</param>
        /// <remarks>
        /// XmlRpcCreateRegionMethod takes the following XMLRPC
        /// parameters
        /// <list type="table">
        /// <listheader><term>parameter name</term><description>description</description></listheader>
        /// <item><term>password</term>
        ///       <description>admin password as set in Aurora.ini</description></item>
        /// <item><term>region_name</term>
        ///       <description>desired region name</description></item>
        /// <item><term>region_id</term>
        ///       <description>(optional) desired region UUID</description></item>
        /// <item><term>region_x</term>
        ///       <description>desired region X coordinate (integer)</description></item>
        /// <item><term>region_y</term>
        ///       <description>desired region Y coordinate (integer)</description></item>
        /// <item><term>estate_owner_first</term>
        ///       <description>firstname of estate owner (formerly region master)
        ///       (required if new estate is being created, optional otherwise)</description></item>
        /// <item><term>estate_owner_last</term>
        ///       <description>lastname of estate owner (formerly region master)
        ///       (required if new estate is being created, optional otherwise)</description></item>
        /// <item><term>estate_owner_uuid</term>
        ///       <description>explicit UUID to use for estate owner (optional)</description></item>
        /// <item><term>listen_ip</term>
        ///       <description>internal IP address (dotted quad)</description></item>
        /// <item><term>listen_port</term>
        ///       <description>internal port (integer)</description></item>
        /// <item><term>external_address</term>
        ///       <description>external IP address</description></item>
        /// <item><term>persist</term>
        ///       <description>if true, persist the region info
        ///       ('true' or 'false')</description></item>
        /// <item><term>public</term>
        ///       <description>if true, the region is public
        ///       ('true' or 'false') (optional, default: true)</description></item>
        /// <item><term>enable_voice</term>
        ///       <description>if true, enable voice on all parcels,
        ///       ('true' or 'false') (optional, default: false)</description></item>
        /// <item><term>estate_name</term>
        ///       <description>the name of the estate to join (or to create if it doesn't
        ///       already exist)</description></item>
        /// <item><term>region_file</term>
        ///       <description>The name of the file to persist the region specifications to.
        /// If omitted, the region_file_template setting from Aurora.ini will be used. (optional)</description></item>
        /// </list>
        ///
        /// XmlRpcCreateRegionMethod returns
        /// <list type="table">
        /// <listheader><term>name</term><description>description</description></listheader>
        /// <item><term>success</term>
        ///       <description>true or false</description></item>
        /// <item><term>error</term>
        ///       <description>error message if success is false</description></item>
        /// <item><term>region_uuid</term>
        ///       <description>UUID of the newly created region</description></item>
        /// <item><term>region_name</term>
        ///       <description>name of the newly created region</description></item>
        /// </list>
        /// </remarks>
        public XmlRpcResponse XmlRpcCreateRegionMethod(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            MainConsole.Instance.Info("[RADMIN]: CreateRegion: new request");

            FailIfRemoteAdminDisabled("CreateRegion");

            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            lock (m_requestLock)
            {
                int  m_regionLimit = m_config.GetInt("region_limit", 0);
                bool m_enableVoiceForNewRegions = m_config.GetBoolean("create_region_enable_voice", false);
                bool m_publicAccess = m_config.GetBoolean("create_region_public", true);

                try
                {
                    Hashtable requestData = (Hashtable)request.Params[0];

                    CheckStringParameters(request, new[]
                                                       {
                                                           "password",
                                                           "region_name",
                                                           "listen_ip", "external_address",
                                                           "estate_name"
                                                       });
                    CheckIntegerParams(request, new[] { "region_x", "region_y", "listen_port" });

                    // check password
                    if (!String.IsNullOrEmpty(m_requiredPassword) &&
                        (string)requestData["password"] != m_requiredPassword) throw new Exception("wrong password");

                    // check whether we still have space left (iff we are using limits)
                    if (m_regionLimit != 0 && manager.AllRegions >= m_regionLimit)
                        throw new Exception(String.Format("cannot instantiate new region, server capacity {0} already reached; delete regions first",
                                                          m_regionLimit));
                    // extract or generate region ID now
                    IScene scene = null;
                    UUID regionID = UUID.Zero;
                    if (requestData.ContainsKey("region_id") &&
                        !String.IsNullOrEmpty((string)requestData["region_id"]))
                    {
                        regionID = (UUID)(string)requestData["region_id"];
                        if (manager.TryGetScene(regionID, out scene))
                            throw new Exception(
                                String.Format("region UUID already in use by region {0}, UUID {1}, <{2},{3}>",
                                              scene.RegionInfo.RegionName, scene.RegionInfo.RegionID,
                                              scene.RegionInfo.RegionLocX / Constants.RegionSize, scene.RegionInfo.RegionLocY / Constants.RegionSize));
                    }
                    else
                    {
                        regionID = UUID.Random();
                        MainConsole.Instance.DebugFormat("[RADMIN] CreateRegion: new region UUID {0}", regionID);
                    }

                    // create volatile or persistent region info
                    RegionInfo region = new RegionInfo
                                            {
                                                RegionID = regionID,
                                                RegionName = (string) requestData["region_name"],
                                                RegionLocX =
                                                    Convert.ToInt32(requestData["region_x"])*Constants.RegionSize,
                                                RegionLocY =
                                                    Convert.ToInt32(requestData["region_y"])*Constants.RegionSize
                                            };


                    // check for collisions: region name, region UUID,
                    // region location
                    if (manager.TryGetScene(region.RegionName, out scene))
                        throw new Exception(
                            String.Format("region name already in use by region {0}",
                                          scene));

                    if (manager.TryGetScene(region.RegionLocX, region.RegionLocY, out scene))
                        throw new Exception(
                            String.Format("region location <{0},{1}> already in use by region {2}",
                                          region.RegionLocX / Constants.RegionSize, region.RegionLocY / Constants.RegionSize,
                                          scene));

                    region.InternalEndPoint =
                        new IPEndPoint(IPAddress.Parse((string) requestData["listen_ip"]), 0)
                            {Port = Convert.ToInt32(requestData["listen_port"])};

                    if (0 == region.InternalEndPoint.Port) throw new Exception("listen_port is 0");
                    
                    // default place for region configuration files is in the
                    // Regions directory of the config dir (aka /bin)
                    string regionConfigPath = Path.Combine(Util.configDir(), "Regions");
                    try
                    {
                        // Aurora.ini can specify a different regions dir
                        IConfig config = m_configSource.Configs["RegionStartup"];
                        if (config != null)
                        {
                            regionConfigPath = config.GetString("RegionsDirectory", regionConfigPath).Trim();
                        }
                    }
                    catch (Exception)
                    {
                        // No INI setting recorded.
                    }

                    string regionIniPath;

                    if (requestData.Contains("region_file"))
                    {
                        // Make sure that the file to be created is in a subdirectory of the region storage directory.
                        string requestedFilePath = Path.Combine(regionConfigPath, (string)requestData["region_file"]);
                        string requestedDirectory = Path.GetDirectoryName(Util.BasePathCombine(requestedFilePath));
                        if (requestedDirectory != null && requestedDirectory.StartsWith(Util.BasePathCombine(regionConfigPath)))
                            regionIniPath = requestedFilePath;
                        else
                            throw new Exception("Invalid location for region file.");
                    }
                    else
                    {
                        regionIniPath = Path.Combine(regionConfigPath,
                                                        String.Format(
                                                            m_config.GetString("region_file_template",
                                                                               "{0}x{1}-{2}.ini"),
                                                            (region.RegionLocX / Constants.RegionSize).ToString(),
                                                            (region.RegionLocY / Constants.RegionSize).ToString(),
                                                            regionID.ToString(),
                                                            region.InternalEndPoint.Port.ToString(),
                                                            region.RegionName.Replace(" ", "_").Replace(":", "_").
                                                                Replace("/", "_")));
                    }

                    MainConsole.Instance.DebugFormat("[RADMIN] CreateRegion: persisting region {0} to {1}",
                                      region.RegionID, regionIniPath);
                    region.SaveRegionToFile("dynamic region", regionIniPath);

                    // Set the estate

                    // Check for an existing estate
                    Aurora.Framework.IEstateConnector estateService = Aurora.DataManager.DataManager.RequestPlugin<Aurora.Framework.IEstateConnector>();
                    if (estateService == null)
                        throw new Exception("No estate service available.");
                    UUID userID = UUID.Zero;
                    if (requestData.ContainsKey("estate_owner_uuid"))
                    {
                        // ok, client wants us to use an explicit UUID
                        // regardless of what the avatar name provided
                        userID = new UUID((string)requestData["estate_owner_uuid"]);
                    }
                    else if (requestData.ContainsKey("estate_owner_first") & requestData.ContainsKey("estate_owner_last"))
                    {
                        // We need to look up the UUID for the avatar with the provided name.
                        string ownerFirst = (string)requestData["estate_owner_first"];
                        string ownerLast = (string)requestData["estate_owner_last"];

                        IScene currentOrFirst = manager.GetCurrentOrFirstScene();
                        IUserAccountService accountService = currentOrFirst.UserAccountService;
                        UserAccount user = accountService.GetUserAccount(currentOrFirst.RegionInfo.ScopeID,
                                                                           ownerFirst, ownerLast);
                        userID = user.PrincipalID;
                    }
                    else
                    {
                        throw new Exception("Estate owner details not provided.");
                    }
                    int estateID = estateService.GetEstate(userID, (string)requestData["estate_name"]);
                    if (estateID != 0)
                    {
                        // Create a new estate with the name provided
                        //region.EstateSettings = estateService.LoadEstateSettings(region.RegionID);

                        region.EstateSettings.EstateName = (string)requestData["estate_name"];
                        region.EstateSettings.EstateOwner = userID;
                        // Persistence does not seem to effect the need to save a new estate
                        region.EstateSettings.Save();
                    }

                    // Create the region and perform any initial initialization

                    IScene newScene = manager.StartNewRegion(region);

                    // If an access specification was provided, use it.
                    // Otherwise accept the default.
                    newScene.RegionInfo.EstateSettings.PublicAccess = GetBoolean(requestData, "public", m_publicAccess);
                    newScene.RegionInfo.EstateSettings.Save();

                    // enable voice on newly created region if
                    // requested by either the XmlRpc request or the
                    // configuration
                    if (GetBoolean(requestData, "enable_voice", m_enableVoiceForNewRegions))
                    {
                        IParcelManagementModule parcelManagement = newScene.RequestModuleInterface<IParcelManagementModule>();
                        if (parcelManagement != null)
                        {
                            List<ILandObject> parcels = parcelManagement.AllParcels();

                            foreach (ILandObject parcel in parcels)
                            {
                                parcel.LandData.Flags |= (uint)ParcelFlags.AllowVoiceChat;
                                parcel.LandData.Flags |= (uint)ParcelFlags.UseEstateVoiceChan;
                                parcelManagement.UpdateLandObject(parcel);
                            }
                        }
                    }

                    responseData["success"] = true;
                    responseData["region_name"] = region.RegionName;
                    responseData["region_uuid"] = region.RegionID.ToString();

                    response.Value = responseData;
                }
                catch (Exception e)
                {
                    MainConsole.Instance.ErrorFormat("[RADMIN] CreateRegion: failed {0}", e.Message);
                    MainConsole.Instance.DebugFormat("[RADMIN] CreateRegion: failed {0}", e);

                    responseData["success"] = false;
                    responseData["error"] = e.Message;

                    response.Value = responseData;
                }

                MainConsole.Instance.Info("[RADMIN]: CreateRegion: request complete");
                return response;
            }
        }

        /// <summary> Delete a new region. </summary>
        /// <param name="request">incoming XML RPC request</param>
        ///<param name="remoteClient"></param>
        ///<remarks>
        /// XmlRpcDeleteRegionMethod takes the following XMLRPC
        /// parameters
        /// <list type="table">
        /// <listheader><term>parameter name</term><description>description</description></listheader>
        /// <item><term>password</term>
        ///       <description>admin password as set in Aurora.ini</description></item>
        /// <item><term>region_name</term>
        ///       <description>desired region name</description></item>
        /// <item><term>region_id</term>
        ///       <description>(optional) desired region UUID</description></item>
        /// </list>
        ///
        /// XmlRpcDeleteRegionMethod returns
        /// <list type="table">
        /// <listheader><term>name</term><description>description</description></listheader>
        /// <item><term>success</term>
        ///       <description>true or false</description></item>
        /// <item><term>error</term>
        ///       <description>error message if success is false</description></item>
        /// </list>
        /// </remarks>
        public XmlRpcResponse XmlRpcDeleteRegionMethod(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            MainConsole.Instance.Info("[RADMIN]: DeleteRegion: new request");
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            lock (m_requestLock)
            {
                try
                {
                    Hashtable requestData = (Hashtable) request.Params[0];
                    CheckStringParameters(request, new[] {"password", "region_name"});

                    IScene scene = null;
                    string regionName = (string) requestData["region_name"];
                    if (!manager.TryGetScene(regionName, out scene))
                        throw new Exception(String.Format("region \"{0}\" does not exist", regionName));

                    manager.RemoveRegion(scene, true);

                    responseData["success"] = true;
                    responseData["region_name"] = regionName;

                    response.Value = responseData;
                }
                catch (Exception e)
                {
                    MainConsole.Instance.ErrorFormat("[RADMIN] DeleteRegion: failed {0}", e.Message);
                    MainConsole.Instance.DebugFormat("[RADMIN] DeleteRegion: failed {0}", e);

                    responseData["success"] = false;
                    responseData["error"] = e.Message;

                    response.Value = responseData;
                }

                MainConsole.Instance.Info("[RADMIN]: DeleteRegion: request complete");
                return response;
            }
        }

        /// <summary> Close a region. </summary>
        /// <param name="request">incoming XML RPC request</param>
        ///<param name="remoteClient"></param>
        ///<remarks>
        /// XmlRpcCloseRegionMethod takes the following XMLRPC
        /// parameters
        /// <list type="table">
        /// <listheader><term>parameter name</term><description>description</description></listheader>
        /// <item><term>password</term>
        ///       <description>admin password as set in Aurora.ini</description></item>
        /// <item><term>region_name</term>
        ///       <description>desired region name</description></item>
        /// <item><term>region_id</term>
        ///       <description>(optional) desired region UUID</description></item>
        /// </list>
        ///
        /// XmlRpcShutdownRegionMethod returns
        /// <list type="table">
        /// <listheader><term>name</term><description>description</description></listheader>
        /// <item><term>success</term>
        ///       <description>true or false</description></item>
        /// <item><term>region_name</term>
        ///       <description>the region name if success is true</description></item>
        /// <item><term>error</term>
        ///       <description>error message if success is false</description></item>
        /// </list>
        /// </remarks>
        public XmlRpcResponse XmlRpcCloseRegionMethod(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            MainConsole.Instance.Info("[RADMIN]: CloseRegion: new request");
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            lock (m_requestLock)
            {
                try
                {
                    Hashtable requestData = (Hashtable) request.Params[0];
                    CheckStringParameters(request, new[] {"password"});

                    IScene scene = null;
                    if (requestData.ContainsKey("region_id") &&
                        !String.IsNullOrEmpty((string) requestData["region_id"]))
                    {
                        // Region specified by UUID
                        UUID regionID = (UUID) (string) requestData["region_id"];
                        if (!manager.TryGetScene(regionID, out scene))
                            throw new Exception(String.Format("region \"{0}\" does not exist", regionID));

                        manager.CloseRegion (scene, ShutdownType.Immediate, 0);

                        responseData["success"] = true;
                        responseData["region_id"] = regionID;

                        response.Value = responseData;
                    }
                    else if (requestData.ContainsKey("region_name") &&
                        !String.IsNullOrEmpty((string) requestData["region_name"]))
                    {
                        // Region specified by name

                        string regionName = (string) requestData["region_name"];
                        if (!manager.TryGetScene(regionName, out scene))
                            throw new Exception(String.Format("region \"{0}\" does not exist", regionName));

                        manager.CloseRegion (scene, ShutdownType.Immediate, 0);

                        responseData["success"] = true;
                        responseData["region_name"] = regionName;

                        response.Value = responseData;
                    }
                    else
                        throw new Exception("no region specified");
                }
                catch (Exception e)
                {
                    MainConsole.Instance.ErrorFormat("[RADMIN] CloseRegion: failed {0}", e.Message);
                    MainConsole.Instance.DebugFormat("[RADMIN] CloseRegion: failed {0}", e);

                    responseData["success"] = false;
                    responseData["error"] = e.Message;

                    response.Value = responseData;
                }

                MainConsole.Instance.Info("[RADMIN]: CloseRegion: request complete");
                return response;
            }
        }

        /// <summary> Change characteristics of an existing region. </summary>
        /// <param name="request">incoming XML RPC request</param>
        ///<param name="remoteClient"></param>
        ///<remarks>
        /// XmlRpcModifyRegionMethod takes the following XMLRPC
        /// parameters
        /// <list type="table">
        /// <listheader><term>parameter name</term><description>description</description></listheader>
        /// <item><term>password</term>
        ///       <description>admin password as set in Aurora.ini</description></item>
        /// <item><term>region_name</term>
        ///       <description>desired region name</description></item>
        /// <item><term>region_id</term>
        ///       <description>(optional) desired region UUID</description></item>
        /// <item><term>public</term>
        ///       <description>if true, set the region to public
        ///       ('true' or 'false'), else to private</description></item>
        /// <item><term>enable_voice</term>
        ///       <description>if true, enable voice on all parcels of
        ///       the region, else disable</description></item>
        /// </list>
        ///
        /// XmlRpcModifyRegionMethod returns
        /// <list type="table">
        /// <listheader><term>name</term><description>description</description></listheader>
        /// <item><term>success</term>
        ///       <description>true or false</description></item>
        /// <item><term>error</term>
        ///       <description>error message if success is false</description></item>
        /// </list>
        /// </remarks>
        public XmlRpcResponse XmlRpcModifyRegionMethod(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            MainConsole.Instance.Info("[RADMIN]: ModifyRegion: new request");
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            lock (m_requestLock)
            {
                try
                {
                    Hashtable requestData = (Hashtable) request.Params[0];
                    CheckStringParameters(request, new[] {"password", "region_name"});

                    IScene scene = null;
                    string regionName = (string) requestData["region_name"];
                    if (!manager.TryGetScene(regionName, out scene))
                        throw new Exception(String.Format("region \"{0}\" does not exist", regionName));

                    // Modify access
                    scene.RegionInfo.EstateSettings.PublicAccess =
                        GetBoolean(requestData,"public", scene.RegionInfo.EstateSettings.PublicAccess);
                    scene.RegionInfo.EstateSettings.Save();

                    if (requestData.ContainsKey("enable_voice"))
                    {
                        bool enableVoice = GetBoolean(requestData, "enable_voice", true);
                        IParcelManagementModule parcelManagement = scene.RequestModuleInterface<IParcelManagementModule>();
                        if (parcelManagement != null)
                        {
                            List<ILandObject> parcels = parcelManagement.AllParcels();

                            foreach (ILandObject parcel in parcels)
                            {
                                if (enableVoice)
                                {
                                    parcel.LandData.Flags |= (uint)ParcelFlags.AllowVoiceChat;
                                    parcel.LandData.Flags |= (uint)ParcelFlags.UseEstateVoiceChan;
                                }
                                else
                                {
                                    parcel.LandData.Flags &= ~(uint)ParcelFlags.AllowVoiceChat;
                                    parcel.LandData.Flags &= ~(uint)ParcelFlags.UseEstateVoiceChan;
                                }
                                parcelManagement.UpdateLandObject(parcel);
                            }
                        }
                    }

                    responseData["success"] = true;
                    responseData["region_name"] = regionName;

                    response.Value = responseData;
                }
                catch (Exception e)
                {
                    MainConsole.Instance.ErrorFormat("[RADMIN] ModifyRegion: failed {0}", e.Message);
                    MainConsole.Instance.DebugFormat("[RADMIN] ModifyRegion: failed {0}", e);

                    responseData["success"] = false;
                    responseData["error"] = e.Message;

                    response.Value = responseData;
                }

                MainConsole.Instance.Info("[RADMIN]: ModifyRegion: request complete");
                return response;
            }
        }

        /// <summary> Create a new user account. </summary>
        /// <param name="request">incoming XML RPC request</param>
        ///<param name="remoteClient"></param>
        ///<remarks>
        /// XmlRpcCreateUserMethod takes the following XMLRPC
        /// parameters
        /// <list type="table">
        /// <listheader><term>parameter name</term><description>description</description></listheader>
        /// <item><term>password</term>
        ///       <description>admin password as set in Aurora.ini</description></item>
        /// <item><term>user_firstname</term>
        ///       <description>avatar's first name</description></item>
        /// <item><term>user_lastname</term>
        ///       <description>avatar's last name</description></item>
        /// <item><term>user_password</term>
        ///       <description>avatar's password</description></item>
        /// <item><term>user_email</term>
        ///       <description>email of the avatar's owner (optional)</description></item>
        /// <item><term>start_region_x</term>
        ///       <description>avatar's start region coordinates, X value</description></item>
        /// <item><term>start_region_y</term>
        ///       <description>avatar's start region coordinates, Y value</description></item>
        /// </list>
        ///
        /// XmlRpcCreateUserMethod returns
        /// <list type="table">
        /// <listheader><term>name</term><description>description</description></listheader>
        /// <item><term>success</term>
        ///       <description>true or false</description></item>
        /// <item><term>error</term>
        ///       <description>error message if success is false</description></item>
        /// <item><term>avatar_uuid</term>
        ///       <description>UUID of the newly created avatar
        ///                    account; UUID.Zero if failed.
        ///       </description></item>
        /// </list>
        /// </remarks>
        public XmlRpcResponse XmlRpcCreateUserMethod(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            MainConsole.Instance.Info("[RADMIN]: CreateUser: new request");

            FailIfRemoteAdminDisabled("CreateUser");

            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            lock (m_requestLock)
            {
                try
                {
                    Hashtable requestData = (Hashtable) request.Params[0];

                    // check completeness
                    CheckStringParameters(request, new[]
                                                       {
                                                           "password", "user_firstname",
                                                           "user_lastname", "user_password"
                                                       });
                    CheckIntegerParams(request, new[] {"start_region_x", "start_region_y"});

                    // check password
                    if (!String.IsNullOrEmpty(m_requiredPassword) &&
                        (string) requestData["password"] != m_requiredPassword) throw new Exception("wrong password");

                    // do the job
                    string firstName = (string) requestData["user_firstname"];
                    string lastName = (string) requestData["user_lastname"];
                    string password = (string) requestData["user_password"];

                    uint regionXLocation = Convert.ToUInt32((Int32) requestData["start_region_x"]);
                    uint regionYLocation = Convert.ToUInt32((Int32) requestData["start_region_y"]);

                    string email = ""; // empty string for email
                    if (requestData.Contains("user_email"))
                        email = (string)requestData["user_email"];

                    IScene scene = manager.GetCurrentOrFirstScene();
                    UUID scopeID = scene.RegionInfo.ScopeID;

                    UserAccount account = CreateUser(scopeID, firstName, lastName, password, email);

                    if (null == account)
                        throw new Exception(String.Format("failed to create new user {0} {1}",
                                                          firstName, lastName));

                    // Set home position

                    GridRegion home = scene.GridService.GetRegionByPosition(scopeID, 
                        (int)(regionXLocation * Constants.RegionSize), (int)(regionYLocation * Constants.RegionSize));
                    if (null == home) {
                        MainConsole.Instance.WarnFormat("[RADMIN]: Unable to set home region for newly created user account {0} {1}", firstName, lastName);
                    } else {
                        IAgentInfoService agentInfoService = scene.RequestModuleInterface<IAgentInfoService>();
                        agentInfoService.SetHomePosition(account.PrincipalID.ToString(), home.RegionID, new Vector3(128, 128, 0), new Vector3(0, 1, 0));
                        MainConsole.Instance.DebugFormat("[RADMIN]: Set home region {0} for updated user account {1} {2}", home.RegionID, firstName, lastName);
                    }

                    // Establish the avatar's initial appearance

                    UpdateUserAppearance(responseData, requestData, account.PrincipalID);

                    responseData["success"] = true;
                    responseData["avatar_uuid"] = account.PrincipalID.ToString();

                    response.Value = responseData;

                    MainConsole.Instance.InfoFormat("[RADMIN]: CreateUser: User {0} {1} created, UUID {2}", firstName, lastName, account.PrincipalID);
                }
                catch (Exception e)
                {
                    MainConsole.Instance.ErrorFormat("[RADMIN] CreateUser: failed: {0}", e.Message);
                    MainConsole.Instance.DebugFormat("[RADMIN] CreateUser: failed: {0}", e);

                    responseData["success"] = false;
                    responseData["avatar_uuid"] = UUID.Zero.ToString();
                    responseData["error"] = e.Message;

                    response.Value = responseData;
                }
                MainConsole.Instance.Info("[RADMIN]: CreateUser: request complete");
                return response;
            }
        }

        /// <summary> Check whether a certain user account exists. </summary>
        /// <param name="request">incoming XML RPC request</param>
        ///<param name="remoteClient"></param>
        ///<remarks>
        /// XmlRpcUserExistsMethod takes the following XMLRPC
        /// parameters
        /// <list type="table">
        /// <listheader><term>parameter name</term><description>description</description></listheader>
        /// <item><term>password</term>
        ///       <description>admin password as set in Aurora.ini</description></item>
        /// <item><term>user_firstname</term>
        ///       <description>avatar's first name</description></item>
        /// <item><term>user_lastname</term>
        ///       <description>avatar's last name</description></item>
        /// </list>
        ///
        /// XmlRpcCreateUserMethod returns
        /// <list type="table">
        /// <listheader><term>name</term><description>description</description></listheader>
        /// <item><term>user_firstname</term>
        ///       <description>avatar's first name</description></item>
        /// <item><term>user_lastname</term>
        ///       <description>avatar's last name</description></item>
        /// <item><term>user_lastlogin</term>
        ///       <description>avatar's last login time (secs since UNIX epoch)</description></item>
        /// <item><term>success</term>
        ///       <description>true or false</description></item>
        /// <item><term>error</term>
        ///       <description>error message if success is false</description></item>
        /// </list>
        /// </remarks>
        public XmlRpcResponse XmlRpcUserExistsMethod(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            MainConsole.Instance.Info("[RADMIN]: UserExists: new request");

            FailIfRemoteAdminDisabled("UserExists");

            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            try
            {
                Hashtable requestData = (Hashtable) request.Params[0];

                // check completeness
                CheckStringParameters(request, new[] {"password", "user_firstname", "user_lastname"});

                string firstName = (string) requestData["user_firstname"];
                string lastName = (string) requestData["user_lastname"];

                responseData["user_firstname"] = firstName;
                responseData["user_lastname"] = lastName;

                UUID scopeID = manager.GetCurrentOrFirstScene().RegionInfo.ScopeID;

                UserAccount account = manager.GetCurrentOrFirstScene().UserAccountService.GetUserAccount(scopeID, firstName, lastName);

                if (null == account)
                {
                    responseData["success"] = false;
                    responseData["lastlogin"] = 0;
                }
                else
                {
                    UserInfo userInfo = manager.GetCurrentOrFirstScene().RequestModuleInterface<IAgentInfoService>().GetUserInfo(account.PrincipalID.ToString());
                    if (userInfo != null)
                        responseData["lastlogin"] = userInfo.LastLogin;
                    else
                        responseData["lastlogin"] = 0;

                    responseData["success"] = true;
                }

                response.Value = responseData;
            }
            catch (Exception e)
            {
                MainConsole.Instance.ErrorFormat("[RADMIN] UserExists: failed: {0}", e.Message);
                MainConsole.Instance.DebugFormat("[RADMIN] UserExists: failed: {0}", e);

                responseData["success"] = false;
                responseData["error"] = e.Message;

                response.Value = responseData;
            }

            MainConsole.Instance.Info("[RADMIN]: UserExists: request complete");
            return response;
        }

        /// <summary> Update a user account. </summary>
        /// <param name="request">incoming XML RPC request</param>
        ///<param name="remoteClient"></param>
        ///<remarks>
        /// XmlRpcUpdateUserAccountMethod takes the following XMLRPC
        /// parameters (changeable ones are optional)
        /// <list type="table">
        /// <listheader><term>parameter name</term><description>description</description></listheader>
        /// <item><term>password</term>
        ///       <description>admin password as set in Aurora.ini</description></item>
        /// <item><term>user_firstname</term>
        ///       <description>avatar's first name (cannot be changed)</description></item>
        /// <item><term>user_lastname</term>
        ///       <description>avatar's last name (cannot be changed)</description></item>
        /// <item><term>user_password</term>
        ///       <description>avatar's password (changeable)</description></item>
        /// <item><term>start_region_x</term>
        ///       <description>avatar's start region coordinates, X
        ///                    value (changeable)</description></item>
        /// <item><term>start_region_y</term>
        ///       <description>avatar's start region coordinates, Y
        ///                    value (changeable)</description></item>
        /// <item><term>about_real_world (not implemented yet)</term>
        ///       <description>"about" text of avatar owner (changeable)</description></item>
        /// <item><term>about_virtual_world (not implemented yet)</term>
        ///       <description>"about" text of avatar (changeable)</description></item>
        /// </list>
        ///
        /// XmlRpcCreateUserMethod returns
        /// <list type="table">
        /// <listheader><term>name</term><description>description</description></listheader>
        /// <item><term>success</term>
        ///       <description>true or false</description></item>
        /// <item><term>error</term>
        ///       <description>error message if success is false</description></item>
        /// <item><term>avatar_uuid</term>
        ///       <description>UUID of the updated avatar
        ///                    account; UUID.Zero if failed.
        ///       </description></item>
        /// </list>
        /// </remarks>
        public XmlRpcResponse XmlRpcUpdateUserAccountMethod(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            MainConsole.Instance.Info("[RADMIN]: UpdateUserAccount: new request");
            MainConsole.Instance.Warn("[RADMIN]: This method needs update for 0.7");

            FailIfRemoteAdminDisabled("UpdateUserAccount");

            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            lock (m_requestLock)
            {
                try
                {
                    Hashtable requestData = (Hashtable) request.Params[0];

                    // check completeness
                    CheckStringParameters(request, new[] {
                            "password", "user_firstname",
                            "user_lastname"});

                    // check password
                    if (!String.IsNullOrEmpty(m_requiredPassword) &&
                        (string) requestData["password"] != m_requiredPassword) throw new Exception("wrong password");

                    // do the job
                    string firstName = (string) requestData["user_firstname"];
                    string lastName = (string) requestData["user_lastname"];

                    string password = String.Empty;
                    uint? regionXLocation = null;
                    uint? regionYLocation = null;
            //        uint? ulaX = null;
            //        uint? ulaY = null;
            //        uint? ulaZ = null;
            //        uint? usaX = null;
            //        uint? usaY = null;
            //        uint? usaZ = null;
            //        string aboutFirstLive = String.Empty;
            //        string aboutAvatar = String.Empty;

                    if (requestData.ContainsKey("user_password")) password = (string) requestData["user_password"];
                    if (requestData.ContainsKey("start_region_x"))
                        regionXLocation = Convert.ToUInt32((Int32) requestData["start_region_x"]);
                    if (requestData.ContainsKey("start_region_y"))
                        regionYLocation = Convert.ToUInt32((Int32) requestData["start_region_y"]);

            //        if (requestData.ContainsKey("start_lookat_x"))
            //            ulaX = Convert.ToUInt32((Int32) requestData["start_lookat_x"]);
            //        if (requestData.ContainsKey("start_lookat_y"))
            //            ulaY = Convert.ToUInt32((Int32) requestData["start_lookat_y"]);
            //        if (requestData.ContainsKey("start_lookat_z"))
            //            ulaZ = Convert.ToUInt32((Int32) requestData["start_lookat_z"]);

            //        if (requestData.ContainsKey("start_standat_x"))
            //            usaX = Convert.ToUInt32((Int32) requestData["start_standat_x"]);
            //        if (requestData.ContainsKey("start_standat_y"))
            //            usaY = Convert.ToUInt32((Int32) requestData["start_standat_y"]);
            //        if (requestData.ContainsKey("start_standat_z"))
            //            usaZ = Convert.ToUInt32((Int32) requestData["start_standat_z"]);
            //        if (requestData.ContainsKey("about_real_world"))
            //            aboutFirstLive = (string)requestData["about_real_world"];
            //        if (requestData.ContainsKey("about_virtual_world"))
            //            aboutAvatar = (string)requestData["about_virtual_world"];

                    IScene scene = manager.GetCurrentOrFirstScene();
                    UUID scopeID = scene.RegionInfo.ScopeID;
                    UserAccount account = scene.UserAccountService.GetUserAccount(scopeID, firstName, lastName);

                    if (null == account)
                        throw new Exception(String.Format("avatar {0} {1} does not exist", firstName, lastName));

                    if (!String.IsNullOrEmpty(password))
                    {
                        MainConsole.Instance.DebugFormat("[RADMIN]: UpdateUserAccount: updating password for avatar {0} {1}", firstName, lastName);
                        ChangeUserPassword(firstName, lastName, password);
                    }

            //        if (null != usaX) userProfile.HomeLocationX = (uint) usaX;
            //        if (null != usaY) userProfile.HomeLocationY = (uint) usaY;
            //        if (null != usaZ) userProfile.HomeLocationZ = (uint) usaZ;

            //        if (null != ulaX) userProfile.HomeLookAtX = (uint) ulaX;
            //        if (null != ulaY) userProfile.HomeLookAtY = (uint) ulaY;
            //        if (null != ulaZ) userProfile.HomeLookAtZ = (uint) ulaZ;

            //        if (String.Empty != aboutFirstLive) userProfile.FirstLifeAboutText = aboutFirstLive;
            //        if (String.Empty != aboutAvatar) userProfile.AboutText = aboutAvatar;

                    // Set home position

                    if ((null != regionXLocation) && (null != regionYLocation))
                    {
                        GridRegion home = scene.GridService.GetRegionByPosition(scopeID, 
                            (int)(regionXLocation * Constants.RegionSize), (int)(regionYLocation * Constants.RegionSize));
                        if (null == home) {
                            MainConsole.Instance.WarnFormat("[RADMIN]: Unable to set home region for updated user account {0} {1}", firstName, lastName);
                        } else {
                            IAgentInfoService agentInfoService = scene.RequestModuleInterface<IAgentInfoService>();
                            agentInfoService.SetHomePosition(account.PrincipalID.ToString(), home.RegionID, new Vector3(128, 128, 0), new Vector3(0, 1, 0));
                            MainConsole.Instance.DebugFormat("[RADMIN]: Set home region {0} for updated user account {1} {2}", home.RegionID, firstName, lastName);
                        }
                    }

                    // User has been created. Now establish gender and appearance.

                    UpdateUserAppearance(responseData, requestData, account.PrincipalID);

                    responseData["success"] = true;
                    responseData["avatar_uuid"] = account.PrincipalID.ToString();

                    response.Value = responseData;

                    MainConsole.Instance.InfoFormat("[RADMIN]: UpdateUserAccount: account for user {0} {1} updated, UUID {2}",
                                     firstName, lastName,
                                     account.PrincipalID);
                }
                catch (Exception e)
                {

                    MainConsole.Instance.ErrorFormat("[RADMIN] UpdateUserAccount: failed: {0}", e.Message);
                    MainConsole.Instance.DebugFormat("[RADMIN] UpdateUserAccount: failed: {0}", e);

                    responseData["success"] = false;
                    responseData["avatar_uuid"] = UUID.Zero.ToString();
                    responseData["error"] = e.Message;

                    response.Value = responseData;
                }
                MainConsole.Instance.Info("[RADMIN]: UpdateUserAccount: request complete");
                return response;
            }
        }

        /// <summary>
        /// This method is called by the user-create and user-modify methods to establish
        /// or change, the user's appearance. Default avatar names can be specified via
        /// the config file, but must correspond to avatars in the default appearance
        /// file, or pre-existing in the user database.
        /// This should probably get moved into somewhere more core eventually.
        /// </summary>

        private void UpdateUserAppearance(Hashtable responseData, Hashtable requestData, UUID userid)
        {
            MainConsole.Instance.DebugFormat("[RADMIN] updateUserAppearance");

            string defaultMale   = m_config.GetString("default_male", "Default Male");
            string defaultFemale = m_config.GetString("default_female", "Default Female");
            string defaultNeutral   = m_config.GetString("default_female", "Default Default");
            string model   = String.Empty;

            // Has a gender preference been supplied?

            if (requestData.Contains("gender"))
            {
                switch ((string)requestData["gender"])
                {
                    case "m" :
                    case "male" :
                        model = defaultMale;
                        break;
                    case "f" :
                    case "female" :
                        model = defaultFemale;
                        break;
                    case "n" :
                    case "neutral" :
                    default :
                        model = defaultNeutral;
                        break;
                }
            }

            // Has an explicit model been specified?

            if (requestData.Contains("model") && (String.IsNullOrEmpty((string)requestData["gender"])))
            {
                model = (string)requestData["model"];
            }

            // No appearance attributes were set

            if (String.IsNullOrEmpty(model))
            {
                MainConsole.Instance.DebugFormat("[RADMIN] Appearance update not requested");
                return;
            }

            MainConsole.Instance.DebugFormat("[RADMIN] Setting appearance for avatar {0}, using model <{1}>", userid, model);

            string[] modelSpecifiers = model.Split();
            if (modelSpecifiers.Length != 2)
            {
                MainConsole.Instance.WarnFormat("[RADMIN] User appearance not set for {0}. Invalid model name : <{1}>", userid, model);
                // modelSpecifiers = dmodel.Split();
                return;
            }

            IScene scene = manager.GetCurrentOrFirstScene();
            UUID scopeID = scene.RegionInfo.ScopeID;
            UserAccount modelProfile = scene.UserAccountService.GetUserAccount(scopeID, modelSpecifiers[0], modelSpecifiers[1]);

            if (modelProfile == null)
            {
                MainConsole.Instance.WarnFormat("[RADMIN] Requested model ({0}) not found. Appearance unchanged", model);
                return;
            }

            // Set current user's appearance. This bit is easy. The appearance structure is populated with
            // actual asset ids, however to complete the magic we need to populate the inventory with the
            // assets in question.

            EstablishAppearance(userid, modelProfile.PrincipalID);

            MainConsole.Instance.DebugFormat("[RADMIN] Finished setting appearance for avatar {0}, using model {1}",
                              userid, model);
        }

        /// <summary>
        /// This method is called by updateAvatarAppearance once any specified model has been
        /// ratified, or an appropriate default value has been adopted. The intended prototype
        /// is known to exist, as is the target avatar.
        /// </summary>

        private void EstablishAppearance(UUID destination, UUID source)
        {
            MainConsole.Instance.DebugFormat("[RADMIN] Initializing inventory for {0} from {1}", destination, source);
            IScene scene = manager.GetCurrentOrFirstScene();

            // If the model has no associated appearance we're done.
            AvatarAppearance avatarAppearance = scene.AvatarService.GetAppearance(source);
            if (avatarAppearance == null)
                return;

            // Simple appearance copy or copy Clothing and Bodyparts folders?
            bool copyFolders = m_config.GetBoolean("copy_folders", false);

            if (!copyFolders)
            {
                // Simple copy of wearables and appearance update
                try
                {
                    CopyWearablesAndAttachments(destination, source, avatarAppearance);

                    scene.AvatarService.SetAppearance(destination, avatarAppearance);
                }
                catch (Exception e)
                {
                    MainConsole.Instance.WarnFormat("[RADMIN] Error transferring appearance for {0} : {1}",
                                      destination, e.Message);
                }

                return;
            }

            // Copy Clothing and Bodypart folders and appearance update
            try
            {
                Dictionary<UUID, UUID> inventoryMap = new Dictionary<UUID, UUID>();
                CopyInventoryFolders(destination, source, AssetType.Clothing, inventoryMap, avatarAppearance);
                CopyInventoryFolders(destination, source, AssetType.Bodypart, inventoryMap, avatarAppearance);

                AvatarWearable[] wearables = avatarAppearance.Wearables;

                for (int i = 0; i < wearables.Length; i++)
                {
                    if (inventoryMap.ContainsKey(wearables[i][0].ItemID))
                    {
                        AvatarWearable wearable = new AvatarWearable();
                        wearable.Wear(inventoryMap[wearables[i][0].ItemID],
                                wearables[i][0].AssetID);
                        avatarAppearance.SetWearable(i, wearable);
                    }
                }

                scene.AvatarService.SetAppearance(destination, avatarAppearance);
            }
            catch (Exception e)
            {
                MainConsole.Instance.WarnFormat("[RADMIN] Error transferring appearance for {0} : {1}",
                                   destination, e.Message);
            }

            return;
        }

        /// <summary>
        /// This method is called by establishAppearance to do a copy all inventory items
        /// worn or attached to the Clothing inventory folder of the receiving avatar.
        /// In parallel the avatar wearables and attachments are updated.
        /// </summary>

        private void CopyWearablesAndAttachments(UUID destination, UUID source, AvatarAppearance avatarAppearance)
        {
            IInventoryService inventoryService = manager.GetCurrentOrFirstScene().InventoryService;

            // Get Clothing folder of receiver
            InventoryFolderBase destinationFolder = inventoryService.GetFolderForType (destination, InventoryType.Wearable, AssetType.Clothing);

            if (destinationFolder == null)
                throw new Exception("Cannot locate folder(s)");

            // Missing destination folder? This should *never* be the case
            if (destinationFolder.Type != (short)AssetType.Clothing)
            {
                destinationFolder = new InventoryFolderBase
                                        {
                                            ID = UUID.Random(),
                                            Name = "Clothing",
                                            Owner = destination,
                                            Type = (short) AssetType.Clothing,
                                            ParentID = inventoryService.GetRootFolder(destination).ID,
                                            Version = 1
                                        };

                inventoryService.AddFolder(destinationFolder);     // store base record
                MainConsole.Instance.ErrorFormat("[RADMIN] Created folder for destination {0}", source);
            }

            // Wearables
            AvatarWearable[] wearables = avatarAppearance.Wearables;

            for (int i = 0; i < wearables.Length; i++)
            {
                AvatarWearable wearable = wearables[i];
                if (wearable[0].ItemID != UUID.Zero)
                {
                    // Get inventory item and copy it
                    InventoryItemBase item = new InventoryItemBase(wearable[0].ItemID, source);
                    item = inventoryService.GetItem(item);

                    if (item != null)
                    {
                        InventoryItemBase destinationItem = new InventoryItemBase(UUID.Random(), destination)
                                                                {
                                                                    Name = item.Name,
                                                                    Description = item.Description,
                                                                    InvType = item.InvType,
                                                                    CreatorId = item.CreatorId,
                                                                    CreatorData = item.CreatorData,
                                                                    CreatorIdAsUuid = item.CreatorIdAsUuid,
                                                                    NextPermissions = item.NextPermissions,
                                                                    CurrentPermissions = item.CurrentPermissions,
                                                                    BasePermissions = item.BasePermissions,
                                                                    EveryOnePermissions = item.EveryOnePermissions,
                                                                    GroupPermissions = item.GroupPermissions,
                                                                    AssetType = item.AssetType,
                                                                    AssetID = item.AssetID,
                                                                    GroupID = item.GroupID,
                                                                    GroupOwned = item.GroupOwned,
                                                                    SalePrice = item.SalePrice,
                                                                    SaleType = item.SaleType,
                                                                    Flags = item.Flags,
                                                                    CreationDate = item.CreationDate,
                                                                    Folder = destinationFolder.ID
                                                                };
                        ILLClientInventory inventoryModule = manager.GetCurrentOrFirstScene().RequestModuleInterface<ILLClientInventory>();
                        if (inventoryModule != null)
                            inventoryModule.AddInventoryItem(destinationItem);
                        MainConsole.Instance.DebugFormat("[RADMIN]: Added item {0} to folder {1}", destinationItem.ID, destinationFolder.ID);

                        // Wear item
                        AvatarWearable newWearable = new AvatarWearable();
                        newWearable.Wear(destinationItem.ID, wearable[0].AssetID);
                        avatarAppearance.SetWearable(i, newWearable);
                    }
                    else
                    {
                        MainConsole.Instance.WarnFormat("[RADMIN]: Error transferring {0} to folder {1}", wearable[0].ItemID, destinationFolder.ID);
                    }
                }
            }

            // Attachments
            List<AvatarAttachment> attachments = avatarAppearance.GetAttachments();

            foreach (AvatarAttachment attachment in attachments)
            {
                int attachpoint = attachment.AttachPoint;
                UUID itemID = attachment.ItemID;

                if (itemID != UUID.Zero)
                {
                    // Get inventory item and copy it
                    InventoryItemBase item = new InventoryItemBase(itemID, source);
                    item = inventoryService.GetItem(item);

                    if (item != null)
                    {
                        InventoryItemBase destinationItem = new InventoryItemBase(UUID.Random(), destination)
                                                                {
                                                                    Name = item.Name,
                                                                    Description = item.Description,
                                                                    InvType = item.InvType,
                                                                    CreatorId = item.CreatorId,
                                                                    CreatorData = item.CreatorData,
                                                                    CreatorIdAsUuid = item.CreatorIdAsUuid,
                                                                    NextPermissions = item.NextPermissions,
                                                                    CurrentPermissions = item.CurrentPermissions,
                                                                    BasePermissions = item.BasePermissions,
                                                                    EveryOnePermissions = item.EveryOnePermissions,
                                                                    GroupPermissions = item.GroupPermissions,
                                                                    AssetType = item.AssetType,
                                                                    AssetID = item.AssetID,
                                                                    GroupID = item.GroupID,
                                                                    GroupOwned = item.GroupOwned,
                                                                    SalePrice = item.SalePrice,
                                                                    SaleType = item.SaleType,
                                                                    Flags = item.Flags,
                                                                    CreationDate = item.CreationDate,
                                                                    Folder = destinationFolder.ID
                                                                };
                        ILLClientInventory inventoryModule = manager.GetCurrentOrFirstScene().RequestModuleInterface<ILLClientInventory>();
                        if (inventoryModule != null)
                            inventoryModule.AddInventoryItem(destinationItem);
                        MainConsole.Instance.DebugFormat("[RADMIN]: Added item {0} to folder {1}", destinationItem.ID, destinationFolder.ID);

                        // Attach item
                        avatarAppearance.SetAttachment(attachpoint, destinationItem.ID, destinationItem.AssetID);
                        MainConsole.Instance.DebugFormat("[RADMIN]: Attached {0}", destinationItem.ID);
                    }
                    else
                    {
                        MainConsole.Instance.WarnFormat("[RADMIN]: Error transferring {0} to folder {1}", itemID, destinationFolder.ID);
                    }
                }
            }
        }

        /// <summary>
        /// This method is called by establishAppearance to copy inventory folders to make
        /// copies of Clothing and Bodyparts inventory folders and attaches worn attachments
        /// </summary>

        private void CopyInventoryFolders(UUID destination, UUID source, AssetType assetType, Dictionary<UUID,UUID> inventoryMap,
                                          AvatarAppearance avatarAppearance)
        {
            IInventoryService inventoryService = manager.GetCurrentOrFirstScene().InventoryService;

            InventoryFolderBase sourceFolder = inventoryService.GetFolderForType(source, InventoryType.Unknown, assetType);
            InventoryFolderBase destinationFolder = inventoryService.GetFolderForType (destination, InventoryType.Unknown, assetType);

            if (sourceFolder == null || destinationFolder == null)
                throw new Exception("Cannot locate folder(s)");

            // Missing source folder? This should *never* be the case
            if (sourceFolder.Type != (short)assetType)
            {
                sourceFolder = new InventoryFolderBase
                                   {
                                       ID = UUID.Random(),
                                       Name = assetType == AssetType.Clothing ? "Clothing" : "Body Parts",
                                       Owner = source,
                                       Type = (short) assetType,
                                       ParentID = inventoryService.GetRootFolder(source).ID,
                                       Version = 1
                                   };
                inventoryService.AddFolder(sourceFolder);     // store base record
                MainConsole.Instance.ErrorFormat("[RADMIN] Created folder for source {0}", source);
            }

            // Missing destination folder? This should *never* be the case
            if (destinationFolder.Type != (short)assetType)
            {
                destinationFolder = new InventoryFolderBase
                                        {
                                            ID = UUID.Random(),
                                            Name = assetType.ToString(),
                                            Owner = destination,
                                            Type = (short) assetType,
                                            ParentID = inventoryService.GetRootFolder(destination).ID,
                                            Version = 1
                                        };
                inventoryService.AddFolder(destinationFolder);     // store base record
                MainConsole.Instance.ErrorFormat("[RADMIN] Created folder for destination {0}", source);
            }

            List<InventoryFolderBase> folders = inventoryService.GetFolderContent(source, sourceFolder.ID).Folders;

            foreach (InventoryFolderBase folder in folders)
            {

                InventoryFolderBase extraFolder = new InventoryFolderBase
                                                      {
                                                          ID = UUID.Random(),
                                                          Name = folder.Name,
                                                          Owner = destination,
                                                          Type = folder.Type,
                                                          Version = folder.Version,
                                                          ParentID = destinationFolder.ID
                                                      };
                inventoryService.AddFolder(extraFolder);

                MainConsole.Instance.DebugFormat("[RADMIN] Added folder {0} to folder {1}", extraFolder.ID, sourceFolder.ID);

                List<InventoryItemBase> items = inventoryService.GetFolderContent(source, folder.ID).Items;

                foreach (InventoryItemBase item in items)
                {
                    InventoryItemBase destinationItem = new InventoryItemBase(UUID.Random(), destination)
                                                            {
                                                                Name = item.Name,
                                                                Description = item.Description,
                                                                InvType = item.InvType,
                                                                CreatorId = item.CreatorId,
                                                                CreatorData = item.CreatorData,
                                                                CreatorIdAsUuid = item.CreatorIdAsUuid,
                                                                NextPermissions = item.NextPermissions,
                                                                CurrentPermissions = item.CurrentPermissions,
                                                                BasePermissions = item.BasePermissions,
                                                                EveryOnePermissions = item.EveryOnePermissions,
                                                                GroupPermissions = item.GroupPermissions,
                                                                AssetType = item.AssetType,
                                                                AssetID = item.AssetID,
                                                                GroupID = item.GroupID,
                                                                GroupOwned = item.GroupOwned,
                                                                SalePrice = item.SalePrice,
                                                                SaleType = item.SaleType,
                                                                Flags = item.Flags,
                                                                CreationDate = item.CreationDate,
                                                                Folder = extraFolder.ID
                                                            };

                    ILLClientInventory inventoryModule = manager.GetCurrentOrFirstScene().RequestModuleInterface<ILLClientInventory>();
                    if (inventoryModule != null)
                        inventoryModule.AddInventoryItem(destinationItem);
                    inventoryMap.Add(item.ID, destinationItem.ID);
                    MainConsole.Instance.DebugFormat("[RADMIN]: Added item {0} to folder {1}", destinationItem.ID, extraFolder.ID);

                    // Attach item, if original is attached
                    int attachpoint = avatarAppearance.GetAttachpoint(item.ID);
                    if (attachpoint != 0)
                    {
                        avatarAppearance.SetAttachment(attachpoint, destinationItem.ID, destinationItem.AssetID);
                        MainConsole.Instance.DebugFormat("[RADMIN]: Attached {0}", destinationItem.ID);
                    }
                }
            }
        }

        /// <summary>
        /// This method is called if a given model avatar name can not be found. If the external
        /// file has already been loaded once, then control returns immediately. If not, then it
        /// looks for a default appearance file. This file contains XML definitions of zero or more named
        /// avatars, each avatar can specify zero or more "outfits". Each outfit is a collection
        /// of items that together, define a particular ensemble for the avatar. Each avatar should
        /// indicate which outfit is the default, and this outfit will be automatically worn. The
        /// other outfits are provided to allow "real" avatars a way to easily change their outfits.
        /// </summary>

        private bool CreateDefaultAvatars()
        {
            // Only load once
            if (m_defaultAvatarsLoaded)
            {
                return false;
            }

            MainConsole.Instance.DebugFormat("[RADMIN] Creating default avatar entries");

            m_defaultAvatarsLoaded = true;

            // Load processing starts here...

            try
            {
                string defaultAppearanceFileName = null;

                //m_config may be null if RemoteAdmin configuration secition is missing or disabled in Aurora.ini
                if (m_config != null)
                {
                    defaultAppearanceFileName = m_config.GetString("default_appearance", "default_appearance.xml");
                }

                if (File.Exists(defaultAppearanceFileName))
                {
                    XmlDocument doc = new XmlDocument();
                    string name     = "*unknown*";
                    string email    = "anon@anon";
                    uint   regionXLocation     = 1000;
                    uint   regionYLocation     = 1000;
                    string password   = UUID.Random().ToString(); // No requirement to sign-in.
                    UUID ID = UUID.Zero;
                    XmlNode perms = null;

                    IScene scene = manager.GetCurrentOrFirstScene();
                    IInventoryService inventoryService = scene.InventoryService;
                    IAssetService assetService = scene.AssetService;

                    doc.LoadXml(File.ReadAllText(defaultAppearanceFileName));

                    // Load up any included assets. Duplicates will be ignored
                    XmlNodeList assets = doc.GetElementsByTagName("RequiredAsset");
                    foreach (XmlNode assetNode in assets)
                    {
                        AssetBase asset = new AssetBase(UUID.Random(), GetStringAttribute(assetNode, "name", ""),
                                                        (AssetType)
                                                        SByte.Parse(GetStringAttribute(assetNode, "type", "")),
                                                        UUID.Zero)
                                              {
                                                  Description = GetStringAttribute(assetNode, "desc", ""),
                                                  Data = Convert.FromBase64String(assetNode.InnerText),
                                                  Flags = ((Boolean.Parse(GetStringAttribute(assetNode, "local", "")))
                                                               ? AssetFlags.Local
                                                               : AssetFlags.Normal) |
                                                          ((Boolean.Parse(GetStringAttribute(assetNode, "temporary", "")))
                                                               ? AssetFlags.Temporary
                                                               : AssetFlags.Normal)
                                              };

                        asset.FillHash();
                        asset.ID = assetService.Store(asset);
                    }

                    XmlNodeList avatars = doc.GetElementsByTagName("Avatar");

                    // The document may contain multiple avatars

                    foreach (XmlElement avatar in avatars)
                    {
                        MainConsole.Instance.DebugFormat("[RADMIN] Loading appearance for {0}, gender = {1}",
                            GetStringAttribute(avatar,"name","?"), GetStringAttribute(avatar,"gender","?"));

                        // Create the user identified by the avatar entry

                        bool include = false;
                        try
                        {
                            // Only the name value is mandatory
                            name   = GetStringAttribute(avatar,"name",name);
                            email  = GetStringAttribute(avatar,"email",email);
                            regionXLocation   = GetUnsignedAttribute(avatar,"regx",regionXLocation);
                            regionYLocation   = GetUnsignedAttribute(avatar,"regy",regionYLocation);
                            password = GetStringAttribute(avatar,"password",password);

                            string[] names = name.Split();
                            UUID scopeID = scene.RegionInfo.ScopeID;
                            UserAccount account = scene.UserAccountService.GetUserAccount(scopeID, names[0], names[1]);
                            if (null == account)
                            {
                                account = CreateUser(scopeID, names[0], names[1], password, email);
                                if (null == account)
                                {
                                    MainConsole.Instance.ErrorFormat("[RADMIN] Avatar {0} {1} was not created", names[0], names[1]);
                                    return false;
                                }
                            }

                            // Set home position

                            GridRegion home = scene.GridService.GetRegionByPosition(scopeID, 
                                (int)(regionXLocation * Constants.RegionSize), (int)(regionYLocation * Constants.RegionSize));
                            if (null == home) {
                                MainConsole.Instance.WarnFormat("[RADMIN]: Unable to set home region for newly created user account {0} {1}", names[0], names[1]);
                            } else {
                                IAgentInfoService agentInfoService = scene.RequestModuleInterface<IAgentInfoService>();
                                agentInfoService.SetHomePosition(account.PrincipalID.ToString(), home.RegionID, new Vector3(128, 128, 0), new Vector3(0, 1, 0));
                                MainConsole.Instance.DebugFormat("[RADMIN]: Set home region {0} for updated user account {1} {2}", home.RegionID, names[0], names[1]);
                            }

                            ID = account.PrincipalID;

                            MainConsole.Instance.DebugFormat("[RADMIN] User {0}[{1}] created or retrieved", name, ID);
                            include = true;
                        }
                        catch (Exception e)
                        {
                            MainConsole.Instance.DebugFormat("[RADMIN] Error creating user {0} : {1}", name, e.Message);
                            include = false;
                        }

                        // OK, User has been created OK, now we can install the inventory.
                        // First retrieve the current inventory (the user may already exist)
                        // Note that althought he inventory is retrieved, the hierarchy has
                        // not been interpreted at all.

                        if (include)
                        {
                            // Setup for appearance processing
                            AvatarData avatarData = scene.AvatarService.GetAvatar(ID);
                            AvatarAppearance avatarAppearance = avatarData != null
                                                                    ? avatarData.ToAvatarAppearance(ID)
                                                                    : new AvatarAppearance();

                            AvatarWearable[] wearables = avatarAppearance.Wearables;
                            for (int i=0; i<wearables.Length; i++)
                            {
                                wearables[i] = new AvatarWearable();
                            }

                            try
                            {
                                // MainConsole.Instance.DebugFormat("[RADMIN] {0} folders, {1} items in inventory",
                                //   uic.folders.Count, uic.items.Count);

                                InventoryFolderBase clothingFolder = inventoryService.GetFolderForType (ID, InventoryType.Wearable, AssetType.Clothing);

                                // This should *never* be the case
                                if (clothingFolder == null || clothingFolder.Type != (short)AssetType.Clothing)
                                {
                                    clothingFolder = new InventoryFolderBase
                                                         {
                                                             ID = UUID.Random(),
                                                             Name = "Clothing",
                                                             Owner = ID,
                                                             Type = (short) AssetType.Clothing,
                                                             ParentID = inventoryService.GetRootFolder(ID).ID,
                                                             Version = 1
                                                         };
                                    inventoryService.AddFolder(clothingFolder);     // store base record
                                    MainConsole.Instance.ErrorFormat("[RADMIN] Created clothing folder for {0}/{1}", name, ID);
                                }

                                // OK, now we have an inventory for the user, read in the outfits from the
                                // default appearance XMl file.

                                XmlNodeList outfits = avatar.GetElementsByTagName("Ensemble");

                                foreach (XmlElement outfit in outfits)
                                {
                                    MainConsole.Instance.DebugFormat("[RADMIN] Loading outfit {0} for {1}",
                                        GetStringAttribute(outfit,"name","?"), GetStringAttribute(avatar,"name","?"));

                                    string outfitName = GetStringAttribute(outfit,"name","");
                                    bool select  = (GetStringAttribute(outfit,"default","no") == "yes");

                                    // If the folder already exists, re-use it. The defaults may
                                    // change over time. Augment only.

                                    List<InventoryFolderBase> folders = inventoryService.GetFolderContent(ID, clothingFolder.ID).Folders;
#if (!ISWIN)
                                    InventoryFolderBase extraFolder = null;
                                    foreach (InventoryFolderBase folder in folders)
                                    {
                                        if (folder.Name == outfitName)
                                        {
                                            extraFolder = folder;
                                            break;
                                        }
                                    }
#else
                                    InventoryFolderBase extraFolder = folders.FirstOrDefault(folder => folder.Name == outfitName);
#endif

                                    // Otherwise, we must create the folder.
                                    if (extraFolder == null)
                                    {
                                        MainConsole.Instance.DebugFormat("[RADMIN] Creating outfit folder {0} for {1}", outfitName, name);
                                        extraFolder = new InventoryFolderBase
                                                          {
                                                              ID = UUID.Random(),
                                                              Name = outfitName,
                                                              Owner = ID,
                                                              Type = (short) AssetType.Clothing,
                                                              Version = 1,
                                                              ParentID = clothingFolder.ID
                                                          };
                                        inventoryService.AddFolder(extraFolder);
                                        MainConsole.Instance.DebugFormat("[RADMIN] Adding outfile folder {0} to folder {1}", extraFolder.ID, clothingFolder.ID);
                                    }

                                    // Now get the pieces that make up the outfit
                                    XmlNodeList items = outfit.GetElementsByTagName("Item");

                                    foreach (XmlElement item in items)
                                    {
                                        UUID assetid = UUID.Zero;
                                        XmlNodeList children = item.ChildNodes;
                                        foreach (XmlNode child in children)
                                        {
                                            switch (child.Name)
                                            {
                                                case "Permissions" :
                                                    MainConsole.Instance.DebugFormat("[RADMIN] Permissions specified");
                                                    perms = child;
                                                    break;
                                                case "Asset" :
                                                    assetid = new UUID(child.InnerText);
                                                    break;
                                            }
                                        }

                                        InventoryItemBase inventoryItem = null;

                                        // Check if asset is in inventory already
                                        List<InventoryItemBase> inventoryItems = inventoryService.GetFolderContent(ID, extraFolder.ID).Items;

#if (!ISWIN)
                                        inventoryItem = null;
                                        foreach (InventoryItemBase listItem in inventoryItems)
                                        {
                                            if (listItem.AssetID == assetid)
                                            {
                                                inventoryItem = listItem;
                                                break;
                                            }
                                        }
#else
                                        inventoryItem = inventoryItems.FirstOrDefault(listItem => listItem.AssetID == assetid);
#endif

                                        // Create inventory item
                                        if (inventoryItem == null)
                                        {
                                            inventoryItem = new InventoryItemBase(UUID.Random(), ID)
                                                                {
                                                                    Name = GetStringAttribute(item, "name", ""),
                                                                    Description = GetStringAttribute(item, "desc", ""),
                                                                    InvType = GetIntegerAttribute(item, "invtype", -1),
                                                                    CreatorId =
                                                                        GetStringAttribute(item, "creatorid", ""),
                                                                    CreatorIdAsUuid =
                                                                        (UUID)
                                                                        GetStringAttribute(item, "creatoruuid", ""),
                                                                    NextPermissions =
                                                                        GetUnsignedAttribute(perms, "next", 0x7fffffff),
                                                                    CurrentPermissions =
                                                                        GetUnsignedAttribute(perms, "current",
                                                                                             0x7fffffff),
                                                                    BasePermissions =
                                                                        GetUnsignedAttribute(perms, "base", 0x7fffffff),
                                                                    EveryOnePermissions =
                                                                        GetUnsignedAttribute(perms, "everyone",
                                                                                             0x7fffffff),
                                                                    GroupPermissions =
                                                                        GetUnsignedAttribute(perms, "group", 0x7fffffff),
                                                                    AssetType =
                                                                        GetIntegerAttribute(item, "assettype", -1),
                                                                    AssetID = assetid,
                                                                    GroupID =
                                                                        (UUID) GetStringAttribute(item, "groupid", ""),
                                                                    GroupOwned =
                                                                        (GetStringAttribute(item, "groupowned", "false") ==
                                                                         "true"),
                                                                    SalePrice =
                                                                        GetIntegerAttribute(item, "saleprice", 0),
                                                                    SaleType =
                                                                        (byte) GetIntegerAttribute(item, "saletype", 0),
                                                                    Flags = GetUnsignedAttribute(item, "flags", 0),
                                                                    CreationDate =
                                                                        GetIntegerAttribute(item, "creationdate",
                                                                                            Util.UnixTimeSinceEpoch()),
                                                                    Folder = extraFolder.ID
                                                                };
                                            // associated asset
                                            // Parent folder

                                            ILLClientInventory inventoryModule = manager.GetCurrentOrFirstScene().RequestModuleInterface<ILLClientInventory>();
                                            if (inventoryModule != null)
                                                inventoryModule.AddInventoryItem(inventoryItem);
                                            MainConsole.Instance.DebugFormat("[RADMIN] Added item {0} to folder {1}", inventoryItem.ID, extraFolder.ID);
                                        }

                                        // Attach item, if attachpoint is specified
                                        int attachpoint = GetIntegerAttribute(item,"attachpoint",0);
                                        if (attachpoint != 0)
                                        {
                                            avatarAppearance.SetAttachment(attachpoint, inventoryItem.ID, inventoryItem.AssetID);
                                            MainConsole.Instance.DebugFormat("[RADMIN] Attached {0}", inventoryItem.ID);
                                        }

                                        // Record whether or not the item is to be initially worn
                                        try
                                        {
                                            if (select && (GetStringAttribute(item, "wear", "false") == "true"))
                                            {
                                                avatarAppearance.Wearables[inventoryItem.Flags].Wear(inventoryItem.ID, inventoryItem.AssetID);
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            MainConsole.Instance.WarnFormat("[RADMIN] Error wearing item {0} : {1}", inventoryItem.ID, e.Message);
                                        }
                                    } // foreach item in outfit
                                    MainConsole.Instance.DebugFormat("[RADMIN] Outfit {0} load completed", outfitName);
                                } // foreach outfit
                                MainConsole.Instance.DebugFormat("[RADMIN] Inventory update complete for {0}", name);
                                AvatarData avatarData2 = new AvatarData(avatarAppearance);
                                scene.AvatarService.SetAvatar(ID, avatarData2);
                            }
                            catch (Exception e)
                            {
                                MainConsole.Instance.WarnFormat("[RADMIN] Inventory processing incomplete for user {0} : {1}",
                                    name, e.Message);
                            }
                        } // End of include
                    }
                    MainConsole.Instance.DebugFormat("[RADMIN] Default avatar loading complete");
                }
                else
                {
                    MainConsole.Instance.DebugFormat("[RADMIN] No default avatar information available");
                    return false;
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.WarnFormat("[RADMIN] Exception whilst loading default avatars ; {0}", e.Message);
                return false;
            }

            return true;
        }

        /// <summary> Load an OAR file into a region.. </summary>
        /// <param name="request">incoming XML RPC request</param>
        ///<param name="remoteClient"></param>
        ///<remarks>
        /// XmlRpcLoadOARMethod takes the following XMLRPC
        /// parameters
        /// <list type="table">
        /// <listheader><term>parameter name</term><description>description</description></listheader>
        /// <item><term>password</term>
        ///       <description>admin password as set in Aurora.ini</description></item>
        /// <item><term>filename</term>
        ///       <description>file name of the OAR file</description></item>
        /// <item><term>region_uuid</term>
        ///       <description>UUID of the region</description></item>
        /// <item><term>region_name</term>
        ///       <description>region name</description></item>
        /// </list>
        ///
        /// <code>region_uuid</code> takes precedence over
        /// <code>region_name</code> if both are present; one of both
        /// must be present.
        ///
        /// XmlRpcLoadOARMethod returns
        /// <list type="table">
        /// <listheader><term>name</term><description>description</description></listheader>
        /// <item><term>success</term>
        ///       <description>true or false</description></item>
        /// <item><term>error</term>
        ///       <description>error message if success is false</description></item>
        /// </list>
        /// </remarks>
        public XmlRpcResponse XmlRpcLoadOARMethod(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            MainConsole.Instance.Info("[RADMIN]: Received Load OAR Administrator Request");

            FailIfRemoteAdminDisabled("Load OAR");

            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            lock (m_requestLock)
            {
                try
                {
                    Hashtable requestData = (Hashtable) request.Params[0];

                    // check completeness
                    foreach (string parameter in new[] {"password", "filename"})
                    {
                        if (!requestData.Contains(parameter))
                            throw new Exception(String.Format("missing parameter {0}", parameter));
                        if (String.IsNullOrEmpty((string) requestData[parameter]))
                            throw new Exception(String.Format("parameter {0} is empty", parameter));
                    }

                    // check password
                    if (!String.IsNullOrEmpty(m_requiredPassword) &&
                        (string) requestData["password"] != m_requiredPassword) throw new Exception("wrong password");

                    string filename = (string) requestData["filename"];
                    IScene scene = null;
                    if (requestData.Contains("region_uuid"))
                    {
                        UUID region_uuid = (UUID) (string) requestData["region_uuid"];
                        if (!manager.TryGetScene(region_uuid, out scene))
                            throw new Exception(String.Format("failed to switch to region {0}", region_uuid.ToString()));
                    }
                    else if (requestData.Contains("region_name"))
                    {
                        string region_name = (string) requestData["region_name"];
                        if (!manager.TryGetScene(region_name, out scene))
                            throw new Exception(String.Format("failed to switch to region {0}", region_name));
                    }
                    else throw new Exception("neither region_name nor region_uuid given");

                    IRegionArchiverModule archiver = scene.RequestModuleInterface<IRegionArchiverModule>();
                    if (archiver != null)
                        archiver.DearchiveRegion(filename);
                    else
                        throw new Exception("Archiver module not present for scene");

                    responseData["loaded"] = true;

                    response.Value = responseData;
                }
                catch (Exception e)
                {
                    MainConsole.Instance.InfoFormat("[RADMIN] LoadOAR: {0}", e.Message);
                    MainConsole.Instance.DebugFormat("[RADMIN] LoadOAR: {0}", e);

                    responseData["loaded"] = false;
                    responseData["error"] = e.Message;

                    response.Value = responseData;
                }

                MainConsole.Instance.Info("[RADMIN]: Load OAR Administrator Request complete");
                return response;
            }
        }

        /// <summary> Save a region to an OAR file </summary>
        /// <param name="request">incoming XML RPC request</param>
        ///<param name="remoteClient"></param>
        ///<remarks>
        /// XmlRpcSaveOARMethod takes the following XMLRPC
        /// parameters
        /// <list type="table">
        /// <listheader><term>parameter name</term><description>description</description></listheader>
        /// <item><term>password</term>
        ///       <description>admin password as set in Aurora.ini</description></item>
        /// <item><term>filename</term>
        ///       <description>file name for the OAR file</description></item>
        /// <item><term>region_uuid</term>
        ///       <description>UUID of the region</description></item>
        /// <item><term>region_name</term>
        ///       <description>region name</description></item>
        /// </list>
        ///
        /// <code>region_uuid</code> takes precedence over
        /// <code>region_name</code> if both are present; one of both
        /// must be present.
        ///
        /// XmlRpcLoadOARMethod returns
        /// <list type="table">
        /// <listheader><term>name</term><description>description</description></listheader>
        /// <item><term>success</term>
        ///       <description>true or false</description></item>
        /// <item><term>error</term>
        ///       <description>error message if success is false</description></item>
        /// </list>
        /// </remarks>
        public XmlRpcResponse XmlRpcSaveOARMethod(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            MainConsole.Instance.Info("[RADMIN]: Received Save OAR Administrator Request");

            FailIfRemoteAdminDisabled("Save OAR");

            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            try
            {
                Hashtable requestData = (Hashtable) request.Params[0];

                // check completeness
                foreach (string p in new string[] {"password", "filename"})
                {
                    if (!requestData.Contains(p))
                        throw new Exception(String.Format("missing parameter {0}", p));
                    if (String.IsNullOrEmpty((string) requestData[p]))
                        throw new Exception(String.Format("parameter {0} is empty"));
                }

                // check password
                if (!String.IsNullOrEmpty(m_requiredPassword) &&
                    (string) requestData["password"] != m_requiredPassword) throw new Exception("wrong password");

                string filename = (string) requestData["filename"];
                IScene scene = null;
                if (requestData.Contains("region_uuid"))
                {
                    UUID region_uuid = (UUID) (string) requestData["region_uuid"];
                    if (!manager.TryGetScene(region_uuid, out scene))
                        throw new Exception(String.Format("failed to switch to region {0}", region_uuid.ToString()));
                }
                else if (requestData.Contains("region_name"))
                {
                    string region_name = (string) requestData["region_name"];
                    if (!manager.TryGetScene(region_name, out scene))
                        throw new Exception(String.Format("failed to switch to region {0}", region_name));
                }
                else throw new Exception("neither region_name nor region_uuid given");

                IRegionArchiverModule archiver = scene.RequestModuleInterface<IRegionArchiverModule>();

                if (archiver != null)
                {
                    scene.EventManager.OnOarFileSaved += RemoteAdminOarSaveCompleted;
                    archiver.ArchiveRegion(filename);
                    lock (m_saveOarLock) Monitor.Wait(m_saveOarLock,5000);
                    scene.EventManager.OnOarFileSaved -= RemoteAdminOarSaveCompleted;
                }
                else
                    throw new Exception("Archiver module not present for scene");

                responseData["saved"] = true;

                response.Value = responseData;
            }
            catch (Exception e)
            {
                MainConsole.Instance.InfoFormat("[RADMIN] SaveOAR: {0}", e.Message);
                MainConsole.Instance.DebugFormat("[RADMIN] SaveOAR: {0}", e);

                responseData["saved"] = false;
                responseData["error"] = e.Message;

                response.Value = responseData;
            }

            MainConsole.Instance.Info("[RADMIN]: Save OAR Administrator Request complete");
            return response;
        }

        private void RemoteAdminOarSaveCompleted(Guid uuid, string name)
        {
            MainConsole.Instance.DebugFormat("[RADMIN] File processing complete for {0}", name);
            lock (m_saveOarLock) Monitor.Pulse(m_saveOarLock);
        }

        public XmlRpcResponse XmlRpcLoadXMLMethod(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            MainConsole.Instance.Info("[RADMIN]: Received Load XML Administrator Request");

            FailIfRemoteAdminDisabled("Load XML");

            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            responseData["loaded"] = false;
            responseData["switched"] = false;
            responseData["error"] = "Not Supported";

            response.Value = responseData;

            MainConsole.Instance.Info("[RADMIN]: Load XML Administrator Request complete");
            return response;
        }

        public XmlRpcResponse XmlRpcSaveXMLMethod(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            MainConsole.Instance.Info("[RADMIN]: Received Save XML Administrator Request");

            FailIfRemoteAdminDisabled("Save XML");

            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            responseData["saved"] = false;

            response.Value = responseData;

            MainConsole.Instance.Info("[RADMIN]: Save XML Administrator Request complete");
            return response;
        }

        public XmlRpcResponse XmlRpcRegionQueryMethod(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            MainConsole.Instance.Info("[RADMIN]: Received Query XML Administrator Request");

            FailIfRemoteAdminDisabled("Query XML");

            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            try
            {
                responseData["success"] = true;

                Hashtable requestData = (Hashtable) request.Params[0];

                // check completeness
                if (!requestData.Contains("password"))
                    throw new Exception(String.Format("missing required parameter"));
                if (!String.IsNullOrEmpty(m_requiredPassword) &&
                    (string) requestData["password"] != m_requiredPassword) throw new Exception("wrong password");

                if (requestData.Contains("region_uuid"))
                {
                    UUID region_uuid = (UUID) (string) requestData["region_uuid"];
                    IScene scene;
                    if (manager.TryGetScene (region_uuid, out scene))
                        manager.ChangeConsoleRegion (scene.RegionInfo.RegionName);
                    else
                        throw new Exception(String.Format("failed to switch to region {0}", region_uuid.ToString()));
                    MainConsole.Instance.InfoFormat("[RADMIN] Switched to region {0}", region_uuid.ToString());
                }
                else if (requestData.Contains("region_name"))
                {
                    string region_name = (string) requestData["region_name"];
                    if (!manager.ChangeConsoleRegion (region_name))
                        throw new Exception(String.Format("failed to switch to region {0}", region_name));
                    MainConsole.Instance.InfoFormat("[RADMIN] Switched to region {0}", region_name);
                }
                else throw new Exception("neither region_name nor region_uuid given");

                responseData["health"] = 0;

                response.Value = responseData;
            }
            catch (Exception e)
            {
                MainConsole.Instance.InfoFormat("[RADMIN] RegionQuery: {0}", e.Message);

                responseData["success"] = false;
                responseData["error"] = e.Message;

                response.Value = responseData;
            }

            MainConsole.Instance.Info("[RADMIN]: Query XML Administrator Request complete");
            return response;
        }

        public XmlRpcResponse XmlRpcConsoleCommandMethod(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            MainConsole.Instance.Info("[RADMIN]: Received Command XML Administrator Request");

            FailIfRemoteAdminDisabled("Command XML");

            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            try
            {
                responseData["success"] = true;

                Hashtable requestData = (Hashtable) request.Params[0];

                // check completeness
                if (!requestData.Contains("password"))
                    throw new Exception(String.Format("missing required parameter"));
                if (!String.IsNullOrEmpty(m_requiredPassword) &&
                    (string) requestData["password"] != m_requiredPassword) throw new Exception("wrong password");

                if (!requestData.Contains("command"))
                    throw new Exception(String.Format("missing required parameter"));
                MainConsole.Instance.RunCommand(requestData["command"].ToString());

                response.Value = responseData;
            }
            catch (Exception e)
            {
                MainConsole.Instance.InfoFormat("[RADMIN] ConsoleCommand: {0}", e.Message);

                responseData["success"] = false;
                responseData["error"] = e.Message;

                response.Value = responseData;
            }

            MainConsole.Instance.Info("[RADMIN]: Command XML Administrator Request complete");
            return response;
        }

        public XmlRpcResponse XmlRpcAccessListClear(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            MainConsole.Instance.Info("[RADMIN]: Received Access List Clear Request");

            FailIfRemoteAdminDisabled("Access List Clear");

            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            try
            {
                responseData["success"] = true;

                Hashtable requestData = (Hashtable) request.Params[0];

                if (!requestData.Contains("password"))
                    throw new Exception(String.Format("missing required parameter"));
                if (!String.IsNullOrEmpty(m_requiredPassword) &&
                    (string) requestData["password"] != m_requiredPassword) throw new Exception("wrong password");

                IScene scene;
                if (requestData.Contains ("region_uuid"))
                {
                    UUID region_uuid = (UUID) (string) requestData["region_uuid"];

                    if (manager.TryGetScene (region_uuid, out scene))
                        manager.ChangeConsoleRegion (scene.RegionInfo.RegionName);
                    else
                        throw new Exception(String.Format("failed to switch to region {0}", region_uuid.ToString()));
                    MainConsole.Instance.InfoFormat("[RADMIN] Switched to region {0}", region_uuid.ToString());
                }
                else if (requestData.Contains("region_name"))
                {
                    string region_name = (string) requestData["region_name"];
                    if (!manager.ChangeConsoleRegion (region_name))
                        throw new Exception(String.Format("failed to switch to region {0}", region_name));
                    MainConsole.Instance.InfoFormat("[RADMIN] Switched to region {0}", region_name);
                }
                else throw new Exception("neither region_name nor region_uuid given");

                scene = manager.GetCurrentOrFirstScene();
                scene.RegionInfo.EstateSettings.EstateAccess = new UUID[]{};
                scene.RegionInfo.EstateSettings.Save();
            }
            catch (Exception e)
            {
                MainConsole.Instance.InfoFormat("[RADMIN] Access List Clear Request: {0}", e.Message);

                responseData["success"] = false;
                responseData["error"] = e.Message;
            }
            finally
            {
                response.Value = responseData;
            }

            MainConsole.Instance.Info("[RADMIN]: Access List Clear Request complete");
            return response;
        }

        public XmlRpcResponse XmlRpcAccessListAdd(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            MainConsole.Instance.Info("[RADMIN]: Received Access List Add Request");

            FailIfRemoteAdminDisabled("Access List Add");

            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            try
            {
                responseData["success"] = true;

                Hashtable requestData = (Hashtable) request.Params[0];

                if (!requestData.Contains("password"))
                    throw new Exception(String.Format("missing required parameter"));
                if (!String.IsNullOrEmpty(m_requiredPassword) &&
                    (string) requestData["password"] != m_requiredPassword) throw new Exception("wrong password");

                if (requestData.Contains("region_uuid"))
                {
                    UUID region_uuid = (UUID) (string) requestData["region_uuid"];
                    IScene scene;
                    if (manager.TryGetScene (region_uuid, out scene))
                        manager.ChangeConsoleRegion (scene.RegionInfo.RegionName);
                    else
                        throw new Exception(String.Format("failed to switch to region {0}", region_uuid.ToString()));
                    MainConsole.Instance.InfoFormat("[RADMIN] Switched to region {0}", region_uuid.ToString());
                }
                else if (requestData.Contains("region_name"))
                {
                    string region_name = (string) requestData["region_name"];
                    if (!manager.ChangeConsoleRegion (region_name))
                        throw new Exception(String.Format("failed to switch to region {0}", region_name));
                    MainConsole.Instance.InfoFormat("[RADMIN] Switched to region {0}", region_name);
                }
                else throw new Exception("neither region_name nor region_uuid given");

                int addedUsers = 0;

                if (requestData.Contains("users"))
                {
                    UUID scopeID = manager.GetCurrentOrFirstScene().RegionInfo.ScopeID;
                    IUserAccountService userService = manager.GetCurrentOrFirstScene().UserAccountService;
                    IScene scene = manager.GetCurrentOrFirstScene();
                    Hashtable users = (Hashtable) requestData["users"];
                    List<UUID> uuids = new List<UUID>();
                    foreach (string name in users.Values)
                    {
                        string[] parts = name.Split();
                        UserAccount account = userService.GetUserAccount(scopeID, parts[0], parts[1]);
                        if (account != null)
                        {
                            uuids.Add(account.PrincipalID);
                            MainConsole.Instance.DebugFormat("[RADMIN] adding \"{0}\" to ACL for \"{1}\"", name, scene.RegionInfo.RegionName);
                        }
                    }
                    List<UUID> accessControlList = new List<UUID>(scene.RegionInfo.EstateSettings.EstateAccess);
                    foreach (UUID uuid in uuids)
                    {
                       if (!accessControlList.Contains(uuid))
                        {
                            accessControlList.Add(uuid);
                            addedUsers++;
                        }
                    }
                    scene.RegionInfo.EstateSettings.EstateAccess = accessControlList.ToArray();
                    scene.RegionInfo.EstateSettings.Save();
                }

                responseData["added"] = addedUsers;
            }
            catch (Exception e)
            {
                MainConsole.Instance.InfoFormat("[RADMIN] Access List Add Request: {0}", e.Message);

                responseData["success"] = false;
                responseData["error"] = e.Message;
            }
            finally
            {
                response.Value = responseData;
            }

            MainConsole.Instance.Info("[RADMIN]: Access List Add Request complete");
            return response;
        }

        public XmlRpcResponse XmlRpcAccessListRemove(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            MainConsole.Instance.Info("[RADMIN]: Received Access List Remove Request");

            FailIfRemoteAdminDisabled("Access List Remove");

            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            try
            {
                responseData["success"] = true;

                Hashtable requestData = (Hashtable) request.Params[0];

                if (!requestData.Contains("password"))
                    throw new Exception(String.Format("missing required parameter"));
                if (!String.IsNullOrEmpty(m_requiredPassword) &&
                    (string) requestData["password"] != m_requiredPassword) throw new Exception("wrong password");

                if (requestData.Contains("region_uuid"))
                {
                    UUID region_uuid = (UUID) (string) requestData["region_uuid"];
                    IScene scene;
                    if (manager.TryGetScene (region_uuid, out scene))
                        manager.ChangeConsoleRegion (scene.RegionInfo.RegionName);
                    else
                        throw new Exception(String.Format("failed to switch to region {0}", region_uuid.ToString()));
                    MainConsole.Instance.InfoFormat("[RADMIN] Switched to region {0}", region_uuid.ToString());
                }
                else if (requestData.Contains("region_name"))
                {
                    string region_name = (string) requestData["region_name"];
                    if (!manager.ChangeConsoleRegion (region_name))
                        throw new Exception(String.Format("failed to switch to region {0}", region_name));
                    MainConsole.Instance.InfoFormat("[RADMIN] Switched to region {0}", region_name);
                }
                else throw new Exception("neither region_name nor region_uuid given");

                int removedUsers = 0;

                if (requestData.Contains("users"))
                {
                    UUID scopeID = manager.GetCurrentOrFirstScene().RegionInfo.ScopeID;
                    IUserAccountService userService = manager.GetCurrentOrFirstScene().UserAccountService;
                    //UserProfileCacheService ups = m_application.CommunicationsManager.UserProfileCacheService;
                    IScene scene = manager.GetCurrentOrFirstScene();
                    Hashtable users = (Hashtable) requestData["users"];
                    List<UUID> uuids = (from string name in users.Values
                                        select name.Split()
                                        into parts select userService.GetUserAccount(scopeID, parts[0], parts[1])
                                        into account where account != null select account.PrincipalID).ToList();
                    List<UUID> accessControlList = new List<UUID>(scene.RegionInfo.EstateSettings.EstateAccess);
                    foreach (UUID uuid in uuids)
                    {
                       if (accessControlList.Contains(uuid))
                        {
                            accessControlList.Remove(uuid);
                            removedUsers++;
                        }
                    }
                    scene.RegionInfo.EstateSettings.EstateAccess = accessControlList.ToArray();
                    scene.RegionInfo.EstateSettings.Save();
                }

                responseData["removed"] = removedUsers;
            }
            catch (Exception e)
            {
                MainConsole.Instance.InfoFormat("[RADMIN] Access List Remove Request: {0}", e.Message);

                responseData["success"] = false;
                responseData["error"] = e.Message;
            }
            finally
            {
                response.Value = responseData;
            }

            MainConsole.Instance.Info("[RADMIN]: Access List Remove Request complete");
            return response;
        }

        public XmlRpcResponse XmlRpcAccessListList(XmlRpcRequest request, IPEndPoint remoteClient)
        {
            MainConsole.Instance.Info("[RADMIN]: Received Access List List Request");

            FailIfRemoteAdminDisabled("Access List List");

            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseData = new Hashtable();

            try
            {
                responseData["success"] = true;

                Hashtable requestData = (Hashtable) request.Params[0];

                if (!requestData.Contains("password"))
                    throw new Exception(String.Format("missing required parameter"));
                if (!String.IsNullOrEmpty(m_requiredPassword) &&
                    (string) requestData["password"] != m_requiredPassword) throw new Exception("wrong password");

                IScene scene;
                if (requestData.Contains ("region_uuid"))
                {
                    UUID region_uuid = (UUID) (string) requestData["region_uuid"];
                    if (manager.TryGetScene (region_uuid, out scene))
                        manager.ChangeConsoleRegion (scene.RegionInfo.RegionName);
                    else
                        throw new Exception(String.Format("failed to switch to region {0}", region_uuid.ToString()));
                    MainConsole.Instance.InfoFormat("[RADMIN] Switched to region {0}", region_uuid.ToString());
                }
                else if (requestData.Contains("region_name"))
                {
                    string region_name = (string) requestData["region_name"];
                    if (!manager.ChangeConsoleRegion (region_name))
                        throw new Exception(String.Format("failed to switch to region {0}", region_name));
                    MainConsole.Instance.InfoFormat("[RADMIN] Switched to region {0}", region_name);
                }
                else throw new Exception("neither region_name nor region_uuid given");

                scene = manager.GetCurrentOrFirstScene();
                UUID[] accessControlList = scene.RegionInfo.EstateSettings.EstateAccess;
                Hashtable users = new Hashtable();

                foreach (UUID user in accessControlList)
                {
                    UUID scopeID = manager.GetCurrentOrFirstScene().RegionInfo.ScopeID;
                    UserAccount account = manager.GetCurrentOrFirstScene().UserAccountService.GetUserAccount(scopeID, user);
                    if (account != null)
                    {
                        users[user.ToString()] = account.FirstName + " " + account.LastName;
                    }
                }

                responseData["users"] = users;
            }
            catch (Exception e)
            {
                MainConsole.Instance.InfoFormat("[RADMIN] Acces List List: {0}", e.Message);

                responseData["success"] = false;
                responseData["error"] = e.Message;
            }
            finally
            {
                response.Value = responseData;
            }

            MainConsole.Instance.Info("[RADMIN]: Access List List Request complete");
            return response;
        }

        private static void CheckStringParameters(XmlRpcRequest request, IEnumerable<string> param)
        {
            Hashtable requestData = (Hashtable) request.Params[0];
            foreach (string parameter in param)
            {
                if (!requestData.Contains(parameter))
                    throw new Exception(String.Format("missing string parameter {0}", parameter));
                if (String.IsNullOrEmpty((string) requestData[parameter]))
                    throw new Exception(String.Format("parameter {0} is empty", parameter));
            }
        }

        private static void CheckIntegerParams(XmlRpcRequest request, IEnumerable<string> param)
        {
            Hashtable requestData = (Hashtable) request.Params[0];
            foreach (string parameter in param)
            {
                if (!requestData.Contains(parameter))
                    throw new Exception(String.Format("missing integer parameter {0}", parameter));
            }
        }

        private bool GetBoolean(Hashtable requestData, string tag, bool defaultValue)
        {
            // If an access value has been provided, apply it.
            if (requestData.Contains(tag))
            {
                switch (((string)requestData[tag]).ToLower())
                {
                    case "true" :
                    case "t" :
                    case "1" :
                        return true;
                    case "false" :
                    case "f" :
                    case "0" :
                        return false;
                    default :
                        return defaultValue;
                }
            }
            return defaultValue;
        }

        private int GetIntegerAttribute(XmlNode node, string attribute, int defaultValue)
        {
            try {
                if (node.Attributes != null) return Convert.ToInt32(node.Attributes[attribute].Value);
            } catch{}
            return defaultValue;
        }

        private uint GetUnsignedAttribute(XmlNode node, string attribute, uint defaultValue)
        {
            try {
                if (node.Attributes != null) return Convert.ToUInt32(node.Attributes[attribute].Value);
            } catch{}
            return defaultValue;
        }

        private string GetStringAttribute(XmlNode node, string attribute, string defaultValue)
        {
            try {
                if (node.Attributes != null) return node.Attributes[attribute].Value;
            } catch{}
            return defaultValue;
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Create a user
        /// </summary>
        /// <param name="scopeID"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="password"></param>
        /// <param name="email"></param>
        private UserAccount CreateUser(UUID scopeID, string firstName, string lastName, string password, string email)
        {
            IScene scene = manager.GetCurrentOrFirstScene();
            IUserAccountService userAccountService = scene.UserAccountService;
            IGridService gridService = scene.GridService;
            IAuthenticationService authenticationService = scene.AuthenticationService;
            IInventoryService inventoryService = scene.InventoryService;

            UserAccount account = userAccountService.GetUserAccount(scopeID, firstName, lastName);
            if (null == account)
            {
                account = new UserAccount(scopeID, firstName + " " + lastName, email);

                if (userAccountService.StoreUserAccount(account))
                {
                    bool success;
                    if (authenticationService != null)
                    {
                        success = authenticationService.SetPassword (account.PrincipalID, "UserAccount", password);
                        if (!success)
                            MainConsole.Instance.WarnFormat("[RADMIN]: Unable to set password for account {0} {1}.",
                                firstName, lastName);
                    }

                    GridRegion home = null;
                    if (gridService != null)
                    {
                        List<GridRegion> defaultRegions = gridService.GetDefaultRegions(UUID.Zero);
                        if (defaultRegions != null && defaultRegions.Count >= 1)
                            home = defaultRegions[0];

                        IAgentInfoService agentInfoService = scene.RequestModuleInterface<IAgentInfoService>();
                        if (agentInfoService != null && home != null)
                        {
                            agentInfoService.SetHomePosition(account.PrincipalID.ToString(), home.RegionID, new Vector3(128, 128, 0), new Vector3(0, 1, 0));
                        }
                        else
                            MainConsole.Instance.WarnFormat("[RADMIN]: Unable to set home for account {0} {1}.",
                               firstName, lastName);
                    }
                    else
                        MainConsole.Instance.WarnFormat("[RADMIN]: Unable to retrieve home region for account {0} {1}.",
                           firstName, lastName);

                    if (inventoryService != null)
                    {
                        success = inventoryService.CreateUserInventory(account.PrincipalID, false);
                        if (!success)
                            MainConsole.Instance.WarnFormat("[RADMIN]: Unable to create inventory for account {0} {1}.",
                                firstName, lastName);
                    }

                    MainConsole.Instance.InfoFormat("[RADMIN]: Account {0} {1} created successfully", firstName, lastName);
                    return account;
                 }
                MainConsole.Instance.ErrorFormat("[RADMIN]: Account creation failed for account {0} {1}", firstName, lastName);
            }
            else
            {
                MainConsole.Instance.ErrorFormat("[RADMIN]: A user with the name {0} {1} already exists!", firstName, lastName);
            }
            return null;
        }

        /// <summary>
        /// Change password
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="password"></param>
        private bool ChangeUserPassword(string firstName, string lastName, string password)
        {
            IScene scene = manager.GetCurrentOrFirstScene();
            IUserAccountService userAccountService = scene.UserAccountService;
            IAuthenticationService authenticationService = scene.AuthenticationService;

            UserAccount account = userAccountService.GetUserAccount(UUID.Zero, firstName, lastName);
            if (null != account)
            {
                bool success = false;
                if (authenticationService != null)
                    success = authenticationService.SetPassword (account.PrincipalID, "UserAccount", password);
                if (!success) {
                    MainConsole.Instance.WarnFormat("[RADMIN]: Unable to set password for account {0} {1}.",
                       firstName, lastName);
                    return false;
                }
                return true;
            }
            MainConsole.Instance.ErrorFormat("[RADMIN]: No such user");
            return false;
        }
    }
}
