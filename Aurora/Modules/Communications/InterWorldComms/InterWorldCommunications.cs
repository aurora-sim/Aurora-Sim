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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Aurora.Framework;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using RegionFlags = Aurora.Framework.RegionFlags;

namespace Aurora.Modules
{
    /*public class InterWorldCommunications : IService, ICommunicationService
    {
        #region Declares

        /// <summary>
        ///   All connections that we have to other hosts
        ///   (Before sending the initial connection requests, 
        ///   this MAY contain connections that we do not currently have)
        /// </summary>
        protected List<string> Connections = new List<string>();

        public bool IsGettingUrlsForIWCConnection;

        /// <summary>
        ///   The class that sends requests to other hosts
        /// </summary>
        public IWCOutgoingConnections OutgoingPublicComms;

        /// <summary>
        ///   Whether we are enabled or not
        /// </summary>
        protected bool m_Enabled;

        /// <summary>
        ///   Should connections that come to us that are not authenticated be allowed to connect?
        /// </summary>
        public bool m_allowUntrustedConnections;

        /// <summary>
        ///   The 'AuroraInterWorldConnectors' config
        /// </summary>
        protected IConfig m_config;

        /// <summary>
        ///   The registry where we can get services
        /// </summary>
        protected IRegistryCore m_registry;

        /// <summary>
        ///   The config source
        /// </summary>
        protected IConfigSource m_source;

        /// <summary>
        ///   Untrusted connections automatically get this trust level
        /// </summary>
        public ThreatLevel m_untrustedConnectionsDefaultTrust = ThreatLevel.Low;

        public IRegistryCore Registry
        {
            get { return m_registry; }
        }

        #endregion

        #region Console Commands

        private void AddConsoleCommands()
        {
            if (MainConsole.Instance == null)
                return;
            MainConsole.Instance.Commands.AddCommand("iwc add connection", "iwc add connection",
                                                     "Add an IWC connection to another host.", AddIWCConnection);
            MainConsole.Instance.Commands.AddCommand("iwc remove connection", "iwc remove connection",
                                                     "Remove an IWC connection from another host.", RemoveIWCConnection);
            MainConsole.Instance.Commands.AddCommand("iwc show connections", "iwc show connections",
                                                     "Shows all active IWC connections.", ShowIWCConnections);
        }

        #region Commands

        /// <summary>
        ///   Add a certificate for the given connection
        /// </summary>
        /// <param name = "module"></param>
        /// <param name = "cmds"></param>
        private void AddIWCConnection(string[] cmds)
        {
            string Url = MainConsole.Instance.Prompt("Url to the connection");
            //Be user friendly, add the http:// if needed as well as the final /
            Url = (Url.StartsWith("http://") || Url.StartsWith("https://")) ? Url : "http://" + Url;
            Url = Url.EndsWith("/") ? Url + "iwcconnection" : Url + "/iwcconnection";

            bool success = this.OutgoingPublicComms.AttemptConnection(Url);
            if (success)
                Connections.Add(Url);
        }

        private void RemoveIWCConnection(string[] cmds)
        {
            string Url = MainConsole.Instance.Prompt("Url to the connection");
        }

        private void ShowIWCConnections(string[] cmds)
        {
            MainConsole.Instance.InfoFormat("Showing {0} active IWC connections.", Connections.Count);
            foreach (string t in Connections)
            {
                MainConsole.Instance.Info("Url: " + t);
            }
        }

        #endregion

        #endregion

        #region ICommunicationService Members

        public GridRegion GetRegionForGrid(string regionName, string url)
        {
            bool found = Connections.Contains(url);
            if (found)
            {
                //If we are already connected, the grid services are together, so we already know of the region if it exists, therefore, it does not exist
                return null;
            }
            else
            {
                //Be user friendly, add the http:// if needed as well as the final /
                url = (url.StartsWith("http://") || url.StartsWith("https://")) ? url : "http://" + url;
                url = url.EndsWith("/") ? url + "iwcconnection" : url + "/iwcconnection";
                bool success = this.OutgoingPublicComms.AttemptConnection(url);
                if (success)
                {
                    IGridService service = m_registry.RequestModuleInterface<IGridService>();
                    if (service != null)
                    {
                        List<GridRegion> regions = service.GetRegionsByName(UUID.Zero, regionName, 0, 3);
#if (!ISWIN)
                        foreach (GridRegion t in regions)
                        {
                            if (t.RegionName == regionName)
                            {
                                return t;
                            }
                        }
#else
                        foreach (GridRegion t in regions.Where(t => t.RegionName == regionName))
                        {
                            return t;
                        }
#endif
                        if (regions.Count > 0)
                            return regions[0];
                    }
                }
            }
            return null;
        }

        public OSDMap GetUrlsForUser(GridRegion region, UUID userID)
        {
            if ((((RegionFlags) region.Flags) & RegionFlags.Foreign) != RegionFlags.Foreign)
            {
                MainConsole.Instance.Debug("[IWC]: Not a foreign region");
                return null;
            }
            string host = userID.ToString();
            IGridRegistrationService module = Registry.RequestModuleInterface<IGridRegistrationService>();
            if (module != null)
            {
                module.RemoveUrlsForClient(host);
                module.RemoveUrlsForClient(host + "|" + region.RegionHandle);
                IsGettingUrlsForIWCConnection = true;
                OSDMap map = module.GetUrlForRegisteringClient(host + "|" + region.RegionHandle);
                IsGettingUrlsForIWCConnection = false;

                string url = region.GenericMap["URL"];
                if (url == "")
                {
                    MainConsole.Instance.Warn("[IWC]: Foreign region with no URL");
                    return null; //What the hell? Its a foreign region, it better have a URL!
                }
                //Remove the /Grid.... stuff
                url = url.Remove(url.Length - 5 - 36);
                OutgoingPublicComms.InformOfURLs(url + "/iwcconnection", map, userID, region.RegionHandle);

                return map;
            }

            return null;
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource source, IRegistryCore registry)
        {
            m_source = source;
            m_config = source.Configs["AuroraInterWorldConnectors"];
            if (m_config != null)
            {
                m_Enabled = m_config.GetBoolean("Enabled", false);
                m_allowUntrustedConnections = m_config.GetBoolean("AllowUntrustedConnections",
                                                                  m_allowUntrustedConnections);
                m_untrustedConnectionsDefaultTrust =
                    (ThreatLevel)
                    Enum.Parse(typeof (ThreatLevel),
                               m_config.GetString("UntrustedConnectionsDefaultTrust",
                                                  m_untrustedConnectionsDefaultTrust.ToString()));
                registry.RegisterModuleInterface(this);
                registry.StackModuleInterface<ICommunicationService>(this);
                m_registry = registry;
            }
        }

        public void Start(IConfigSource source, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
            if (m_Enabled)
            {
                //Set up the public connection
                MainServer.Instance.AddStreamHandler(new IWCIncomingConnections(this));

                //Startup outgoing
                OutgoingPublicComms = new IWCOutgoingConnections(this);

                //Make our connection strings.
                //Connections = BuildConnections();

                try
                {
                    ContactOtherServers();
                }
                catch
                {
                }

                AddConsoleCommands();
            }
        }

        #endregion

        private void ContactOtherServers()
        {
        }

        internal void AddNewConnectionFromRequest(string identifer, OSDMap args)
        {
            //Add the other servers IP to our connections=
            IConfigurationService configService = Registry.RequestModuleInterface<IConfigurationService>();
            if (configService != null)
            {
                //Add the URLs they sent us
                configService.RemoveUrls(identifer);
                configService.AddNewUrls(identifer, args);
            }
        }
    }

    /// <summary>
    ///   This class deals with sending requests to other hosts
    /// </summary>
    public class IWCOutgoingConnections
    {
        private readonly InterWorldCommunications IWC;

        public IWCOutgoingConnections(InterWorldCommunications iwc)
        {
            IWC = iwc;
        }

        /// <summary>
        ///   Query the given host (by connection) and verify that we can connect to it.
        /// </summary>
        /// <param name = "connector">The host to connect to</param>
        /// <returns>The connection that has been recieved from the host</returns>
        public bool AttemptConnection(string host)
        {
            IGridRegistrationService module = IWC.Registry.RequestModuleInterface<IGridRegistrationService>();
            if (module != null)
            {
                module.RemoveUrlsForClient(host);
                IWC.IsGettingUrlsForIWCConnection = true;
                OSDMap callThem = module.GetUrlForRegisteringClient(host);
                IWC.IsGettingUrlsForIWCConnection = false;
                callThem["OurIdentifier"] = Utilities.GetAddress();

                callThem["Method"] = "ConnectionRequest";
                string resultStr = WebUtils.PostToService(host, callThem);
                if (resultStr != "")
                {
                    OSDMap result = OSDParser.DeserializeJson(resultStr) as OSDMap;
                    //Add their URLs back again
                    MainConsole.Instance.Warn("Successfully Connected to " + host);
                    IWC.AddNewConnectionFromRequest(result["OurIdentifier"], result);
                    return true;
                }
            }
            return false;
        }

        public bool InformOfURLs(string url, OSDMap urls, UUID userID, ulong regionHandle)
        {
            urls["OurIdentifier"] = Utilities.GetAddress();
            urls["UserID"] = userID;
            urls["RegionHandle"] = regionHandle;

            urls["Method"] = "NewURLs";
            return WebUtils.PostToService(url, urls) != "";
        }
    }

    /// <summary>
    ///   This class deals with incoming requests (secure and insecure) from other hosts
    /// </summary>
    public class IWCIncomingConnections : BaseRequestHandler
    {
        private readonly InterWorldCommunications IWC;

        public IWCIncomingConnections(InterWorldCommunications iwc) :
            base("POST", "/iwcconnection")
        {
            IWC = iwc;
        }

        public override byte[] Handle(string path, Stream requestData,
                                      OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            OSDMap args = WebUtils.GetOSDMap(body);
            if (args == null)
            {
                //No data or not an json OSDMap
                return new byte[0];
            }
            else
            {
                if (args.ContainsKey("Method"))
                {
                    string Method = args["Method"].AsString();
                    if (Method == "ConnectionRequest")
                        return ConnectionRequest(args);
                    if (Method == "NewURLs")
                        return NewURLs(args);
                }
            }
            return new byte[0];
        }

        private byte[] NewURLs(OSDMap args)
        {
            UUID userID = args["UserID"];
            ulong regionhandle = args["RegionHandle"];
            string ident = userID + "|" + regionhandle;
            IWC.AddNewConnectionFromRequest(userID.ToString(), args);
            OSDMap result = new OSDMap();
            result["success"] = true;
            string json = OSDParser.SerializeJsonString(result);
            UTF8Encoding enc = new UTF8Encoding();
            return enc.GetBytes(json);
        }

        private byte[] ConnectionRequest(OSDMap args)
        {
            IGridRegistrationService module = IWC.Registry.RequestModuleInterface<IGridRegistrationService>();
            OSDMap result = new OSDMap();
            if (module != null)
            {
                //Add our URLs for them so that they can connect too
                string theirIdent = args["OurIdentifier"];
                ulong handle;
                if (ulong.TryParse(theirIdent, out handle))
                {
                    //Fu**in hackers
                    //No making region handle sessionIDs!
                    result["success"] = false;
                }
                else
                {
                    module.RemoveUrlsForClient(theirIdent);
                    IWC.IsGettingUrlsForIWCConnection = true;
                    result = module.GetUrlForRegisteringClient(theirIdent);
                    IWC.IsGettingUrlsForIWCConnection = false;
                    result["OurIdentifier"] = Utilities.GetAddress();
                    MainConsole.Instance.Warn(theirIdent + " successfully connected to us");
                    IWC.AddNewConnectionFromRequest(theirIdent, args);
                    result["success"] = true;
                }
            }

            string json = OSDParser.SerializeJsonString(result);
            UTF8Encoding enc = new UTF8Encoding();
            return enc.GetBytes(json);
        }
    }*/
}