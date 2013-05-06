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

using Aurora.Framework;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Servers;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Servers.HttpServer.Interfaces;
using Aurora.Framework.Services;
using Aurora.Framework.Services.ClassHelpers.Assets;
using Aurora.Framework.Utilities;
using Aurora.Modules.AbuseReportsGUI;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Aurora.Modules.AbuseReports
{
    public class AbuseReportsGUIService : IService
    {
        #region IService Members

        private IRegistryCore m_registry;

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            if (MainConsole.Instance != null)
                MainConsole.Instance.Commands.AddCommand("open abusereportsGUI",
                                                         "open abusereportsGUI",
                                                         "Opens the abuse reports GUI", OpenGUI);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        #endregion

        #region GUI Code

        protected void OpenGUI(string[] cmdparams)
        {
            Thread t = new Thread(ThreadProcARGUI);
            t.Start();
        }

        public void ThreadProcARGUI()
        {
            Culture.SetCurrentCulture();
            Application.Run(new Abuse(m_registry.RequestModuleInterface<IAssetService>(),
                                      m_registry.RequestModuleInterface<IJ2KDecoder>()));
        }

        #endregion
    }

    /// <summary>
    ///     Enables the saving of abuse reports to the database
    /// </summary>
    public class AbuseReportsModule : INonSharedRegionModule
    {
        //private static readonly ILog MainConsole.Instance = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IScene m_Scene;
        private bool m_enabled;

        #region INonSharedRegionModule Members

        public void Initialise(IConfigSource source)
        {
            IConfig cnf = source.Configs["AbuseReports"];
            if (cnf != null)
                m_enabled = cnf.GetBoolean("Enabled", true);
        }

        public void AddRegion(IScene scene)
        {
            if (!m_enabled)
                return;

            m_Scene = scene;

            scene.EventManager.OnNewClient += OnNewClient;
            scene.EventManager.OnClosingClient += OnClosingClient;
            //Disabled until complete
            scene.EventManager.OnRegisterCaps += OnRegisterCaps;
        }

        public void RemoveRegion(IScene scene)
        {
            if (!m_enabled)
                return;


            m_Scene = null;

            scene.EventManager.OnNewClient -= OnNewClient;
            scene.EventManager.OnClosingClient -= OnClosingClient;
            //Disabled until complete
            //scene.EventManager.OnRegisterCaps -= OnRegisterCaps;
        }

        public void RegionLoaded(IScene scene)
        {
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "AbuseReportsModule"; }
        }

        public void Close()
        {
        }

        #endregion

        private void OnClosingClient(IClientAPI client)
        {
            client.OnUserReport -= UserReport;
        }

        private void OnNewClient(IClientAPI client)
        {
            client.OnUserReport += UserReport;
        }

        /// <summary>
        ///     This deals with saving the report into the database.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="regionName"></param>
        /// <param name="abuserID"></param>
        /// <param name="catagory"></param>
        /// <param name="checkflags"></param>
        /// <param name="details"></param>
        /// <param name="objectID"></param>
        /// <param name="position"></param>
        /// <param name="reportType"></param>
        /// <param name="screenshotID"></param>
        /// <param name="summery"></param>
        /// <param name="reporter"></param>
        private void UserReport(IClientAPI client, string regionName, UUID abuserID, byte catagory, byte checkflags,
                                string details, UUID objectID, Vector3 position, byte reportType, UUID screenshotID,
                                string summery, UUID reporter)
        {
            AbuseReport report = new AbuseReport
                                     {
                                         ObjectUUID = objectID,
                                         ObjectPosition = position.ToString(),
                                         Active = true,
                                         Checked = false,
                                         Notes = "",
                                         AssignedTo = "No One",
                                         ScreenshotID = screenshotID
                                     };

            if (objectID != UUID.Zero)
            {
                ISceneChildEntity Object = client.Scene.GetSceneObjectPart(objectID);
                report.ObjectName = Object.Name;
            }
            else
                report.ObjectName = "";

            string[] detailssplit = details.Split('\n');

            string AbuseDetails = detailssplit[detailssplit.Length - 1];

            report.AbuseDetails = AbuseDetails;

            report.ReporterName = client.Name;

            string[] findRegion = summery.Split('|');
            report.RegionName = findRegion[1];

            string[] findLocation = summery.Split('(');
            string[] findLocationend = findLocation[1].Split(')');
            report.AbuseLocation = findLocationend[0];

            string[] findCategory = summery.Split('[');
            string[] findCategoryend = findCategory[1].Split(']');
            report.Category = findCategoryend[0];

            string[] findAbuserName = summery.Split('{');
            string[] findAbuserNameend = findAbuserName[1].Split('}');
            report.AbuserName = findAbuserNameend[0];

            string[] findSummary = summery.Split('\"');

            string abuseSummary = findSummary[1];
            if (findSummary.Length != 0)
            {
                abuseSummary = findSummary[1];
            }

            report.AbuseSummary = abuseSummary;


            report.Number = (-1);

            EstateSettings ES = client.Scene.RegionInfo.EstateSettings;
            //If the abuse email is set up and the email module is available, send the email
            if (ES.AbuseEmailToEstateOwner && ES.AbuseEmail != "")
            {
                IEmailModule Email = m_Scene.RequestModuleInterface<IEmailModule>();
                if (Email != null)
                    Email.SendEmail(UUID.Zero, ES.AbuseEmail, "Abuse Report", "This abuse report was submitted by " +
                                                                              report.ReporterName + " against " +
                                                                              report.AbuserName + " at " +
                                                                              report.AbuseLocation + " in your region " +
                                                                              report.RegionName +
                                                                              ". Summary: " + report.AbuseSummary +
                                                                              ". Details: " + report.AbuseDetails + ".",
                                    client.Scene);
            }
            //Tell the DB about it
            IAbuseReports conn = m_Scene.RequestModuleInterface<IAbuseReports>();
            if (conn != null)
                conn.AddAbuseReport(report);
        }

        #region Disabled CAPS code

        private OSDMap OnRegisterCaps(UUID agentID, IHttpServer server)
        {
            OSDMap retVal = new OSDMap();
            retVal["SendUserReportWithScreenshot"] = CapsUtil.CreateCAPS("SendUserReportWithScreenshot", "");
            retVal["SendUserReport"] = retVal["SendUserReportWithScreenshot"];

            //Region Server bound
            server.AddStreamHandler(new GenericStreamHandler("POST", retVal["SendUserReportWithScreenshot"],
                                                             delegate(string path, Stream request,
                                                                      OSHttpRequest httpRequest,
                                                                      OSHttpResponse httpResponse)
                                                             {
                                                                 return ProcessSendUserReportWithScreenshot(agentID, path, request,
                                                                                            httpRequest,
                                                                                            httpResponse);
                                                             }));

            return retVal;
        }

        private  byte[] ProcessSendUserReportWithScreenshot(UUID AgentID, string path, Stream request, OSHttpRequest httpRequest,
                                          OSHttpResponse httpResponse) 
        {
            IScenePresence SP = findScenePresence(AgentID);
            OSDMap map = (OSDMap)OSDParser.DeserializeLLSDXml(HttpServerHandlerHelpers.ReadFully(request));
            string RegionName = map["abuse-region-name"];
            UUID AbuserID = map["abuser-id"];
            uint Category = map["category"];
            uint CheckFlags = map["check-flags"];
            string details = map["details"];
            UUID objectID = map["object-id"];
            Vector3 position = map["position"];
            uint ReportType = map["report-type"];
            UUID ScreenShotID = map["screenshot-id"];
            string summary = map["summary"];
            UserReport(SP.ControllingClient, SP.Scene.RegionInfo.RegionName, AbuserID, (byte)Category, (byte)CheckFlags,
                       details, objectID, position, (byte)ReportType, ScreenShotID, summary, SP.UUID);

            if (ScreenShotID != UUID.Zero)
            {
                string uploadpath = "/CAPS/Upload/" + UUID.Random() + "/";
                AbuseTextureUploader uploader = new AbuseTextureUploader(uploadpath, SP.UUID, ScreenShotID);
                uploader.OnUpLoad += AbuseTextureUploaded;

                MainServer.Instance.AddStreamHandler(new GenericStreamHandler("POST", uploadpath,
                                                                    uploader.uploaderCaps));

                string uploaderURL = MainServer.Instance.ServerURI + uploadpath;
                OSDMap resp = new OSDMap();
                resp["uploader"] = uploaderURL;
                resp["state"] = "upload";
                return OSDParser.SerializeLLSDXmlBytes(resp);
            }
            else
                return MainServer.BlankResponse;
        }


        public delegate void UploadedAbuseTexture(UUID agentID, UUID assetID, byte[] data);

        public class AbuseTextureUploader
        {
            public event UploadedAbuseTexture OnUpLoad;
            private UploadedAbuseTexture handlerUpLoad = null;
            private UUID m_agentID, m_assetID;

            private readonly string uploaderPath = String.Empty;

            public AbuseTextureUploader(string path, UUID agentID, UUID assetID)
            {
                uploaderPath = path;
                m_agentID = agentID;
                m_assetID = assetID;
            }

            /// <summary>
            /// </summary>
            /// <param name="path"></param>
            /// <param name="request"></param>
            /// <param name="httpRequest"></param>
            /// <param name="httpResponse"></param>
            /// <returns></returns>
            public byte[] uploaderCaps(string path, Stream request,
                                       OSHttpRequest httpRequest, OSHttpResponse httpResponse)
            {
                handlerUpLoad = OnUpLoad;
                handlerUpLoad(m_agentID, m_assetID, HttpServerHandlerHelpers.ReadFully(request));

                OSDMap map = new OSDMap();
                map["new_asset"] = m_assetID.ToString();
                map["item_id"] = UUID.Zero;
                map["state"] = "complete";
                MainServer.Instance.RemoveStreamHandler("POST", uploaderPath);

                return OSDParser.SerializeLLSDXmlBytes(map);
            }
        }

        public void AbuseTextureUploaded(UUID agentID, UUID assetID, byte[] data)
        {
            //MainConsole.Instance.InfoFormat("[AssetCAPS]: Received baked texture {0}", assetID.ToString());
            AssetBase asset = new AssetBase(assetID, "Abuse Texture", AssetType.Texture, agentID) { Data = data };
            asset.ID = m_Scene.AssetService.Store(asset);
            MainConsole.Instance.DebugFormat("[AbuseCAPS]: texture new id {0}", assetID.ToString());
        }

        #endregion

        #region Helpers

        public IScenePresence findScenePresence(UUID avID)
        {
            return m_Scene.GetScenePresence(avID);
        }

        #endregion
    }
}