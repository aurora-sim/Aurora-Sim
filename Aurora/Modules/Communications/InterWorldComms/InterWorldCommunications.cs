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
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using OpenSim.Framework.Servers.HttpServer;
using Nini.Config;
using Aurora.Framework;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Aurora.Simulation.Base;
using log4net;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using DataManager = Aurora.DataManager.DataManager;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Modules
{
    public class InterWorldCommunications : IService, ICommunicationService
    {
        #region Declares

        protected static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        /// <summary>
        /// The 'AuroraInterWorldConnectors' config 
        /// </summary>
        protected IConfig m_config;
        /// <summary>
        /// The config source
        /// </summary>
        protected IConfigSource m_source;
        /// <summary>
        /// The registry where we can get services
        /// </summary>
        protected IRegistryCore m_registry;
        /// <summary>
        /// Whether we are enabled or not
        /// </summary>
        protected bool m_Enabled = false;
        /// <summary>
        /// Should connections that come to us that are not authenticated be allowed to connect?
        /// </summary>
        public bool m_allowUntrustedConnections = false;
        /// <summary>
        /// Untrusted connections automatically get this trust level
        /// </summary>
        public ThreatLevel m_untrustedConnectionsDefaultTrust = ThreatLevel.Low;
        /// <summary>
        /// All connections that we have to other hosts
        /// (Before sending the initial connection requests, 
        ///   this MAY contain connections that we do not currently have)
        /// </summary>
        protected List<string> Connections = new List<string>();
        /// <summary>
        /// The class that sends requests to other hosts
        /// </summary>
        public IWCOutgoingConnections OutgoingPublicComms;

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
            MainConsole.Instance.Commands.AddCommand ("iwc add connection", "iwc add connection",
                "Add an IWC connection to another host.", AddIWCConnection);
            MainConsole.Instance.Commands.AddCommand("iwc remove connection", "iwc remove connection",
                "Remove an IWC connection from another host.", RemoveIWCConnection);
            MainConsole.Instance.Commands.AddCommand("iwc show connections", "iwc show connections",
                "Shows all active IWC connections.", ShowIWCConnections);
        }

        #region Commands

        /// <summary>
        /// Add a certificate for the given connection
        /// </summary>
        /// <param name="module"></param>
        /// <param name="cmds"></param>
        private void AddIWCConnection(string[] cmds)
        {
            string Url = MainConsole.Instance.CmdPrompt("Url to the connection");
            //Be user friendly, add the http:// if needed as well as the final /
            Url = (Url.StartsWith("http://") || Url.StartsWith("https://")) ? Url : "http://" + Url;
            Url = Url.EndsWith("/") ? Url + "iwcconnection" : Url + "/iwcconnection";

            bool success = this.OutgoingPublicComms.AttemptConnection (Url);
            if (success)
                Connections.Add (Url);
        }

        private void RemoveIWCConnection(string[] cmds)
        {
            string Url = MainConsole.Instance.CmdPrompt("Url to the connection");
            
        }

        private void ShowIWCConnections(string[] cmds)
        {
            m_log.InfoFormat("Showing {0} active IWC connections.", Connections.Count);
            for (int i = 0; i < Connections.Count; i++)
            {
                m_log.Info ("Url: " + Connections[i]);
            }
        }

        #endregion

        #endregion

        #region IService Members

        public void Initialize(IConfigSource source, IRegistryCore registry)
        {
            m_source = source;
            m_config = source.Configs["AuroraInterWorldConnectors"];
            if (m_config != null)
            {
                m_Enabled = m_config.GetBoolean("Enabled", false);
                m_allowUntrustedConnections = m_config.GetBoolean("AllowUntrustedConnections", m_allowUntrustedConnections);
                m_untrustedConnectionsDefaultTrust = (ThreatLevel)Enum.Parse(typeof(ThreatLevel), m_config.GetString("UntrustedConnectionsDefaultTrust", m_untrustedConnectionsDefaultTrust.ToString()));
                registry.RegisterModuleInterface<InterWorldCommunications>(this);
                registry.StackModuleInterface<ICommunicationService> (this);
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

        private void ContactOtherServers ()
        {
        }

        #endregion

        internal void AddNewConnectionFromRequest (OSDMap args)
        {
            //Add the other servers IP to our connections=
            IConfigurationService configService = Registry.RequestModuleInterface<IConfigurationService> ();
            if (configService != null)
            {
                //Add the URLs they sent us
                configService.RemoveUrls (args["OurIdentifier"]);
                configService.AddNewUrls (args["OurIdentifier"], args);
            }
        }

        public string GetOurIP ()
        {
            return "http://" + Utilities.GetExternalIp () + ":" + Registry.RequestModuleInterface<ISimulationBase> ().GetHttpServer (0).Port;
        }

        public GridRegion GetRegionForGrid (string regionName, string url)
        {
            bool found = Connections.Contains (url);
            if (found)
            {
                //If we are already connected, the grid services are together, so we already know of the region if it exists, therefore, it does not exist
                return null;
            }
            else
            {
                //Be user friendly, add the http:// if needed as well as the final /
                url = (url.StartsWith ("http://") || url.StartsWith ("https://")) ? url : "http://" + url;
                url = url.EndsWith ("/") ? url + "iwcconnection" : url + "/iwcconnection";
                bool success = this.OutgoingPublicComms.AttemptConnection (url);
                if (success)
                {
                    IGridService service = m_registry.RequestModuleInterface<IGridService> ();
                    if (service != null)
                        return service.GetRegionByName (UUID.Zero, regionName);
                }
            }
            return null;
        }

        public OSDMap GetUrlsForUser (GridRegion region, UUID userID)
        {
            string host = userID.ToString ();
            IGridRegistrationService module = Registry.RequestModuleInterface<IGridRegistrationService> ();
            if (module != null)
            {
                module.RemoveUrlsForClient (host);
                return module.GetUrlForRegisteringClient (host);
            }

            return null;
        }
    }

    /// <summary>
    /// This class deals with sending requests to other hosts
    /// </summary>
    public class IWCOutgoingConnections
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private InterWorldCommunications IWC;

        public IWCOutgoingConnections(InterWorldCommunications iwc)
        {
            IWC = iwc;
        }

        /// <summary>
        /// Query the given host (by connection) and verify that we can connect to it.
        /// </summary>
        /// <param name="connector">The host to connect to</param>
        /// <returns>The connection that has been recieved from the host</returns>
        public bool AttemptConnection (string host)
        {
            IGridRegistrationService module = IWC.Registry.RequestModuleInterface<IGridRegistrationService> ();
            if (module != null)
            {
                module.RemoveUrlsForClient (host);
                OSDMap callThem = module.GetUrlForRegisteringClient (host);
                callThem["OurIdentifier"] = IWC.GetOurIP();

                callThem["Method"] = "ConnectionRequest";
                OSDMap result = WebUtils.PostToService (host, callThem, true, false, true);
                if (result["Success"])
                {
                    //Add their URLs back again
                    m_log.Warn ("Successfully Connected to " + host);
                    IWC.AddNewConnectionFromRequest (result);
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// This class deals with incoming requests (secure and insecure) from other hosts
    /// </summary>
    public class IWCIncomingConnections : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private InterWorldCommunications IWC;

        public IWCIncomingConnections(InterWorldCommunications iwc) :
            base("POST", "/iwcconnection")
        {
            IWC = iwc;
        }

        public override byte[] Handle (string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader (requestData);
            string body = sr.ReadToEnd ();
            sr.Close ();
            body = body.Trim ();

            OSDMap args = WebUtils.GetOSDMap (body);
            if (args == null)
            {
                //No data or not an json OSDMap
                return new byte[0];
            }
            else
            {
                if (args.ContainsKey ("Method"))
                {
                    string Method = args["Method"].AsString ();
                    if (Method == "ConnectionRequest")
                        return ConnectionRequest (args);
                }
            }
            return new byte[0];
        }

        private byte[] ConnectionRequest (OSDMap args)
        {
            IGridRegistrationService module = IWC.Registry.RequestModuleInterface<IGridRegistrationService> ();
            OSDMap result = new OSDMap ();
            if (module != null)
            {
                //Add our URLs for them so that they can connect too
                string theirIdent = args["OurIdentifier"];
                ulong handle;
                if (ulong.TryParse (theirIdent, out handle))
                {
                    //Fu**in hackers
                    //No making region handle sessionIDs!
                    result["Success"] = false;
                }
                else
                {
                    module.RemoveUrlsForClient (theirIdent);
                    result = module.GetUrlForRegisteringClient (theirIdent);
                    result["OurIdentifier"] = IWC.GetOurIP ();
                    m_log.Warn (theirIdent + " successfully connected to us");
                    IWC.AddNewConnectionFromRequest (args);
                    result["Success"] = true;
                }
            }

            string json = OSDParser.SerializeJsonString (result);
            UTF8Encoding enc = new UTF8Encoding ();
            return enc.GetBytes (json);
        }
    }
}