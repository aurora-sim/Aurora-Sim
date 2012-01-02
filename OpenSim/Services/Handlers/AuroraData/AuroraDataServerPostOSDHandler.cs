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
using System.Reflection;
using System.Text;
using System.Xml;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services
{
    public class AuroraDataServerPostOSDHandler : BaseStreamHandler
    {
        private readonly DirectoryInfoOSDHandler DirectoryHandler;
        private readonly OfflineMessagesInfoHandler OfflineMessagesHandler = new OfflineMessagesInfoHandler();
        private readonly ProfileInfoHandler ProfileHandler = new ProfileInfoHandler();
        private readonly EmailHandler EmailHandler = new EmailHandler();

        protected string m_SessionID;
        protected IRegistryCore m_registry;

        public AuroraDataServerPostOSDHandler(string url, string SessionID, IRegistryCore registry) :
            base("POST", url)
        {
            DirectoryHandler =
                new DirectoryInfoOSDHandler(registry.RequestModuleInterface<ISimulationBase>().ConfigSource);
            m_SessionID = SessionID;
            m_registry = registry;
        }

        public override byte[] Handle(string path, Stream requestData,
                                      OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            OSDMap args = WebUtils.GetOSDMap(body);
            if (args.ContainsKey("Method"))
            {
                IGridRegistrationService urlModule =
                    m_registry.RequestModuleInterface<IGridRegistrationService>();
                string method = args["Method"].AsString();
                ulong handle;
                switch (method)
                {
                        #region Profile

                    case "getprofile":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.None))
                                return FailureResult();
                        return ProfileHandler.GetProfile(args);
                    case "updateprofile":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.High))
                                return FailureResult();
                        return ProfileHandler.UpdateProfile(args);
                    case "updateagent":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.High))
                                return FailureResult();
                        return ProfileHandler.UpdateAgent(args);
                    case "getclassified":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.High))
                                return FailureResult();
                        return ProfileHandler.GetClassifed(args);
                    case "getclassifieds":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.High))
                                return FailureResult();
                        return ProfileHandler.GetClassifieds(args);
                    case "getpick":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.High))
                                return FailureResult();
                        return ProfileHandler.GetPick(args);
                    case "getpicks":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.High))
                                return FailureResult();
                        return ProfileHandler.GetPicks(args);
                    case "removepick":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.High))
                                return FailureResult();
                        return ProfileHandler.RemovePick(args);
                    case "removeclassified":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.High))
                                return FailureResult();
                        return ProfileHandler.RemoveClassified(args);
                    case "addclassified":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.High))
                                return FailureResult();
                        return ProfileHandler.AddClassified(args);
                    case "addpick":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.High))
                                return FailureResult();
                        return ProfileHandler.AddPick(args);

                        #endregion

                        #region Offline Messages

                    case "addofflinemessage":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return OfflineMessagesHandler.AddOfflineMessage(args);
                    case "getofflinemessages":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return OfflineMessagesHandler.GetOfflineMessages(args);

                        #endregion

                        #region Emails
                        
                    case "insertemail":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return EmailHandler.InsertEmail(args);
                    case "getemails":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return EmailHandler.GetEmails(args);

                        #endregion

                    #region Directory Messages

                    case "addregion":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        if (ulong.TryParse(m_SessionID, out handle))
                        {
                            int x, y;
                            Util.UlongToInts(handle, out x, out y);
                            UUID regionID =
                                this.m_registry.RequestModuleInterface<IGridService>().GetRegionByPosition(UUID.Zero, x,
                                                                                                           y).RegionID;
                            return DirectoryHandler.AddRegion(args, regionID);
                        }
                        break;
                    case "clearregion":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        if (ulong.TryParse(m_SessionID, out handle))
                        {
                            int x, y;
                            Util.UlongToInts(handle, out x, out y);
                            UUID regionID =
                                this.m_registry.RequestModuleInterface<IGridService>().GetRegionByPosition(UUID.Zero, x,
                                                                                                           y).RegionID;
                            return DirectoryHandler.ClearRegion(regionID, args);
                        }
                        break;
                    case "getparcelinfo":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Low))
                                return FailureResult();
                        return DirectoryHandler.GetParcelInfo(args);
                    case "getparcelbyowner":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return DirectoryHandler.GetParcelByOwner(args);

                        #endregion
                }
            }

            return FailureResult();
        }

        #region Misc

        private byte[] FailureResult()
        {
            return FailureResult(String.Empty);
        }

        private byte[] FailureResult(string msg)
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                                             "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                                                       "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Failure"));

            rootElement.AppendChild(result);

            XmlElement message = doc.CreateElement("", "Message", "");
            message.AppendChild(doc.CreateTextNode(msg));

            rootElement.AppendChild(message);

            return DocToBytes(doc);
        }

        private byte[] DocToBytes(XmlDocument doc)
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xw = new XmlTextWriter(ms, null) {Formatting = Formatting.Indented};
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }

        #endregion
    }

    public class ProfileInfoHandler
    {
        private readonly IProfileConnector ProfileConnector;
        private readonly IAgentConnector AgentConnector;

        public ProfileInfoHandler()
        {
            ProfileConnector = DataManager.RequestPlugin<IProfileConnector>("IProfileConnectorLocal");
            AgentConnector = DataManager.RequestPlugin<IAgentConnector>("IAgentConnectorLocal");
        }

        public byte[] GetProfile(OSDMap request)
        {
            UUID principalID = request["PrincipalID"].AsUUID();

            IUserProfileInfo UserProfile = ProfileConnector.GetUserProfile(principalID);
            if (UserProfile != null) UserProfile.PrincipalID = principalID;
            OSDMap result = UserProfile != null ? UserProfile.ToOSD() : new OSDMap();

            string xmlString = OSDParser.SerializeJsonString(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] UpdateProfile(OSDMap request)
        {
            IUserProfileInfo UserProfile = new IUserProfileInfo();
            UserProfile.FromOSD((OSDMap)request["Profile"]);
            ProfileConnector.UpdateUserProfile(UserProfile);
            OSDMap result = new OSDMap();
            result["result"] = "Successful";

            string xmlString = OSDParser.SerializeJsonString(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] UpdateAgent(OSDMap request)
        {
            IAgentInfo UserProfile = new IAgentInfo();
            UserProfile.FromOSD((OSDMap)request["Agent"]);
            IAgentInfo oldAgent = AgentConnector.GetAgent(UserProfile.PrincipalID);
            foreach (KeyValuePair<string, OSD> kvp in UserProfile.OtherAgentInformation)
                if (!oldAgent.OtherAgentInformation.ContainsKey(kvp.Key))
                    oldAgent.OtherAgentInformation[kvp.Key] = kvp.Value;
            AgentConnector.UpdateAgent(oldAgent);
            OSDMap result = new OSDMap();
            result["result"] = "Successful";

            string xmlString = OSDParser.SerializeJsonString(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetClassifed(OSDMap request)
        {
            UUID principalID = request["ClassifiedUUID"].AsUUID();

            Classified Classified = ProfileConnector.GetClassified(principalID);
            OSDMap result = Classified != null ? Classified.ToOSD() : new OSDMap();

            string xmlString = OSDParser.SerializeJsonString(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetClassifieds(OSDMap request)
        {
            UUID principalID = request["PrincipalID"].AsUUID();

            List<Classified> Classified = ProfileConnector.GetClassifieds(principalID);
            OSDMap result = new OSDMap();
            OSDArray array = new OSDArray();
            foreach (Classified info in Classified)
            {
                array.Add(info.ToOSD());
            }
            result["Result"] = array;

            string xmlString = OSDParser.SerializeJsonString(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetPick(OSDMap request)
        {
            UUID principalID = request["PickUUID"].AsUUID();

            ProfilePickInfo Pick = ProfileConnector.GetPick(principalID);
            OSDMap result = Pick != null ? Pick.ToOSD() : new OSDMap();

            string xmlString = OSDParser.SerializeJsonString(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetPicks(OSDMap request)
        {
            UUID principalID = request["PrincipalID"].AsUUID();

            List<ProfilePickInfo> Pick = ProfileConnector.GetPicks(principalID);
            OSDMap result = new OSDMap();
            OSDArray array = new OSDArray();
            foreach (ProfilePickInfo info in Pick)
            {
                array.Add(info.ToOSD());
            }
            result["Result"] = array;

            string xmlString = OSDParser.SerializeJsonString(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] RemovePick(OSDMap request)
        {
            UUID principalID = request["PickUUID"].AsUUID();

            ProfileConnector.RemovePick(principalID);

            string xmlString = OSDParser.SerializeJsonString(new OSDMap());
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] RemoveClassified(OSDMap request)
        {
            UUID principalID = request["ClassifiedUUID"].AsUUID();

            ProfileConnector.RemoveClassified(principalID);

            string xmlString = OSDParser.SerializeJsonString(new OSDMap());
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] AddPick(OSDMap request)
        {
            ProfilePickInfo info = new ProfilePickInfo();
            info.FromOSD((OSDMap) request["Pick"]);

            ProfileConnector.AddPick(info);

            string xmlString = OSDParser.SerializeJsonString(new OSDMap());
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] AddClassified(OSDMap request)
        {
            Classified info = new Classified();
            info.FromOSD((OSDMap) request["Classified"]);

            ProfileConnector.AddClassified(info);

            string xmlString = OSDParser.SerializeJsonString(new OSDMap());
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] SuccessResult()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                                             "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                                                       "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Success"));

            rootElement.AppendChild(result);

            return DocToBytes(doc);
        }

        private byte[] DocToBytes(XmlDocument doc)
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xw = new XmlTextWriter(ms, null) {Formatting = Formatting.Indented};
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }
    }

    public class OfflineMessagesInfoHandler
    {
        private readonly IOfflineMessagesConnector OfflineMessagesConnector;

        public OfflineMessagesInfoHandler()
        {
            OfflineMessagesConnector =
                DataManager.RequestPlugin<IOfflineMessagesConnector>("IOfflineMessagesConnectorLocal");
        }

        public byte[] GetOfflineMessages(OSDMap request)
        {
            OSDArray result = new OSDArray();

            UUID PrincipalID = request["PrincipalID"].AsUUID();
            GridInstantMessage[] Messages = OfflineMessagesConnector.GetOfflineMessages(PrincipalID);

            int i = 0;
            foreach (GridInstantMessage Message in Messages)
            {
                result.Add(Message.ToOSD());
                i++;
            }

            string xmlString = OSDParser.SerializeJsonString(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] AddOfflineMessage(OSDMap request)
        {
            GridInstantMessage message = new GridInstantMessage();
            message.FromOSD(request);
            OSDMap map = new OSDMap();
            map["Result"] = OfflineMessagesConnector.AddOfflineMessage(message);

            string xmlString = OSDParser.SerializeJsonString(map);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] SuccessResult()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                                             "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                                                       "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Success"));

            rootElement.AppendChild(result);

            return DocToBytes(doc);
        }

        private byte[] DocToBytes(XmlDocument doc)
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xw = new XmlTextWriter(ms, null) {Formatting = Formatting.Indented};
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }
    }

    public class EmailHandler
    {
        private readonly IEmailConnector EmailConnector;

        public EmailHandler()
        {
            EmailConnector =
                DataManager.RequestPlugin<IEmailConnector>("IEmailConnectorLocal");
        }

        public byte[] GetEmails(OSDMap request)
        {
            OSDArray result = new OSDArray();

            UUID ObjectID = request["ObjectID"].AsUUID();
            List<Email> Messages = EmailConnector.GetEmails(ObjectID);

            int i = 0;
            foreach (Email Message in Messages)
            {
                result.Add(Message.ToOSD());
                i++;
            }

            string xmlString = OSDParser.SerializeJsonString(result);
            //MainConsole.Instance.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] InsertEmail(OSDMap request)
        {
            Email message = new Email();
            message.FromOSD(request);
            OSDMap map = new OSDMap();
            map["Result"] = true;
            EmailConnector.InsertEmail(message);

            string xmlString = OSDParser.SerializeJsonString(map);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] SuccessResult()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                                             "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                                                       "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Success"));

            rootElement.AppendChild(result);

            return DocToBytes(doc);
        }

        private byte[] DocToBytes(XmlDocument doc)
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xw = new XmlTextWriter(ms, null) { Formatting = Formatting.Indented };
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }
    }

    public class DirectoryInfoOSDHandler
    {
        private readonly IDirectoryServiceConnector DirectoryServiceConnector;
        private readonly int minTimeBeforeNextParcelUpdate = 60;
        private readonly Dictionary<UUID, int> timeBeforeNextUpdate = new Dictionary<UUID, int>();

        public DirectoryInfoOSDHandler(IConfigSource source)
        {
            if (source.Configs["IDirectoryServiceConnector"] != null)
                minTimeBeforeNextParcelUpdate =
                    source.Configs["IDirectoryServiceConnector"].GetInt("MinUpdateTimeForParcels",
                                                                        minTimeBeforeNextParcelUpdate);
            DirectoryServiceConnector =
                DataManager.RequestPlugin<IDirectoryServiceConnector>("IDirectoryServiceConnectorLocal");
        }

        public byte[] GetParcelInfo(OSDMap request)
        {
            UUID infoID = request["InfoUUID"].AsUUID();
            LandData parcel = DirectoryServiceConnector.GetParcelInfo(infoID);
            OSDMap result = parcel == null ? new OSDMap() : parcel.ToOSD();
            request["Success"] = parcel != null;
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(OSDParser.SerializeJsonString(request));
        }

        public byte[] GetParcelByOwner(OSDMap request)
        {
            UUID OwnerID = request["OwnerID"].AsUUID();
            LandData[] parcels = DirectoryServiceConnector.GetParcelByOwner(OwnerID);
            OSDMap result = new OSDMap();
            OSDArray array = new OSDArray();
            foreach (LandData land in parcels)
            {
                array.Add(land.ToOSD());
            }
            result["Parcels"] = array;
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(OSDParser.SerializeJsonString(request));
        }

        public byte[] AddRegion(OSDMap request, UUID regionID)
        {
            OSDArray requests = (OSDArray) request["Requests"];
            List<LandData> parcels = new List<LandData>();
            foreach (OSD o in requests)
            {
                LandData land = new LandData();
                land.FromOSD((OSDMap) o);
                land.RegionID = regionID;
                parcels.Add(land);
            }
            if (parcels.Count == 0)
                return new byte[1];
            //Check whether this region is just spamming add to search and stop them if they are
            if (timeBeforeNextUpdate.ContainsKey(parcels[0].RegionID) &&
                Util.UnixTimeSinceEpoch() < timeBeforeNextUpdate[parcels[0].RegionID])
                return new byte[1]; //Too soon to update

            //Update the time with now + the time to wait for the next update
            timeBeforeNextUpdate[parcels[0].RegionID] = Util.UnixTimeSinceEpoch() + (60*minTimeBeforeNextParcelUpdate);

            DirectoryServiceConnector.AddRegion(parcels);
            return new byte[1];
        }

        public byte[] ClearRegion(UUID regionID, OSDMap request)
        {
            DirectoryServiceConnector.ClearRegion(regionID);
            return new byte[1];
        }
    }
}