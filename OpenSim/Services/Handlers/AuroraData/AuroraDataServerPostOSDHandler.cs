using Nini.Config;
using log4net;
using System;
using System.Reflection;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Services.DataService;

namespace OpenSim.Services
{
    public class AuroraDataServerPostOSDHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private ProfileInfoHandler ProfileHandler = new ProfileInfoHandler();
        private OfflineMessagesInfoHandler OfflineMessagesHandler = new OfflineMessagesInfoHandler ();
        
        protected ulong m_regionHandle;
        protected IRegistryCore m_registry;

        public AuroraDataServerPostOSDHandler(string url, ulong regionHandle, IRegistryCore registry) :
            base("POST", url)
        {
            m_regionHandle = regionHandle;
            m_registry = registry;
        }

        public override byte[] Handle (string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader (requestData);
            string body = sr.ReadToEnd ();
            sr.Close ();
            body = body.Trim ();

            OSDMap args = WebUtils.GetOSDMap (body);
            if (args.ContainsKey ("Method"))
            {
                IGridRegistrationService urlModule =
                            m_registry.RequestModuleInterface<IGridRegistrationService> ();
                string method = args["Method"].AsString ();
                switch (method)
                {
                    #region Profile
                    case "getprofile":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel ("", m_regionHandle, method, ThreatLevel.None))
                                return FailureResult ();
                        return ProfileHandler.GetProfile (args);
                    case "updateprofile":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel ("", m_regionHandle, method, ThreatLevel.High))
                                return FailureResult ();
                        return ProfileHandler.UpdateProfile (args);
                    case "getclassified":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel ("", m_regionHandle, method, ThreatLevel.High))
                                return FailureResult ();
                        return ProfileHandler.GetClassifed (args);
                    case "getclassifieds":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel ("", m_regionHandle, method, ThreatLevel.High))
                                return FailureResult ();
                        return ProfileHandler.GetClassifieds (args);
                    case "getpick":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel ("", m_regionHandle, method, ThreatLevel.High))
                                return FailureResult ();
                        return ProfileHandler.GetPick (args);
                    case "getpicks":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel ("", m_regionHandle, method, ThreatLevel.High))
                                return FailureResult ();
                        return ProfileHandler.GetPicks (args);
                    case "removepick":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel ("", m_regionHandle, method, ThreatLevel.High))
                                return FailureResult ();
                        return ProfileHandler.RemovePick (args);
                    case "removeclassified":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel ("", m_regionHandle, method, ThreatLevel.High))
                                return FailureResult ();
                        return ProfileHandler.RemoveClassified (args);
                    case "addclassified":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel ("", m_regionHandle, method, ThreatLevel.High))
                                return FailureResult ();
                        return ProfileHandler.AddClassified (args);
                    case "addpick":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel ("", m_regionHandle, method, ThreatLevel.High))
                                return FailureResult ();
                        return ProfileHandler.AddPick (args);
                    #endregion
                    #region Offline Messages
                    case "addofflinemessage":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel ("", m_regionHandle, method, ThreatLevel.Low))
                                return FailureResult ();
                        return OfflineMessagesHandler.AddOfflineMessage (args);
                    case "getofflinemessages":
                        if (urlModule != null)
                            if (!urlModule.CheckThreatLevel ("", m_regionHandle, method, ThreatLevel.Medium))
                                return FailureResult ();
                        return OfflineMessagesHandler.GetOfflineMessages (args);
                    #endregion
                }
            }

            return FailureResult ();
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
            XmlTextWriter xw = new XmlTextWriter(ms, null);
            xw.Formatting = Formatting.Indented;
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }

        #endregion
    }

    public class ProfileInfoHandler
    {
        IProfileConnector ProfileConnector;
        public ProfileInfoHandler()
        {
            ProfileConnector = DataManager.RequestPlugin<IProfileConnector>("IProfileConnectorLocal");
        }

        public byte[] GetProfile (OSDMap request)
        {
            UUID principalID = request["PrincipalID"].AsUUID ();

            IUserProfileInfo UserProfile = ProfileConnector.GetUserProfile(principalID);
            OSDMap result = UserProfile != null ? UserProfile.ToOSD () : new OSDMap ();

            string xmlString = OSDParser.SerializeJsonString (result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] UpdateProfile(OSDMap request)
        {
            IUserProfileInfo UserProfile = new IUserProfileInfo();
            UserProfile.FromOSD((OSDMap)request["Profile"]);
            ProfileConnector.UpdateUserProfile(UserProfile);
            OSDMap result = new OSDMap ();
            result["result"] = "Successful";

            string xmlString = OSDParser.SerializeJsonString (result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        public byte[] GetClassifed (OSDMap request)
        {
            UUID principalID = request["ClassifiedUUID"].AsUUID ();

            Classified Classified = ProfileConnector.GetClassified (principalID);
            OSDMap result = Classified != null ? Classified.ToOSD () : new OSDMap ();

            string xmlString = OSDParser.SerializeJsonString (result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding ();
            return encoding.GetBytes (xmlString);
        }

        public byte[] GetClassifieds (OSDMap request)
        {
            UUID principalID = request["PrincipalID"].AsUUID ();

            List<Classified> Classified = ProfileConnector.GetClassifieds (principalID);
            OSDMap result = new OSDMap ();
            OSDArray array = new OSDArray ();
            foreach (Classified info in Classified)
            {
                array.Add (info.ToOSD ());
            }
            result["Result"] = array;

            string xmlString = OSDParser.SerializeJsonString (result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding ();
            return encoding.GetBytes (xmlString);
        }

        public byte[] GetPick (OSDMap request)
        {
            UUID principalID = request["PickUUID"].AsUUID ();

            ProfilePickInfo Pick = ProfileConnector.GetPick (principalID);
            OSDMap result = Pick != null ? Pick.ToOSD () : new OSDMap ();

            string xmlString = OSDParser.SerializeJsonString (result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding ();
            return encoding.GetBytes (xmlString);
        }

        public byte[] GetPicks (OSDMap request)
        {
            UUID principalID = request["PrincipalID"].AsUUID ();

            List<ProfilePickInfo> Pick = ProfileConnector.GetPicks (principalID);
            OSDMap result = new OSDMap ();
            OSDArray array = new OSDArray ();
            foreach (ProfilePickInfo info in Pick)
            {
                array.Add (info.ToOSD ());
            }
            result["Result"] = array;

            string xmlString = OSDParser.SerializeJsonString (result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding ();
            return encoding.GetBytes (xmlString);
        }

        public byte[] RemovePick (OSDMap request)
        {
            UUID principalID = request["PickUUID"].AsUUID ();

            ProfileConnector.RemovePick (principalID);

            string xmlString = OSDParser.SerializeJsonString (new OSDMap ());
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding ();
            return encoding.GetBytes (xmlString);
        }

        public byte[] RemoveClassified (OSDMap request)
        {
            UUID principalID = request["ClassifiedUUID"].AsUUID ();

            ProfileConnector.RemoveClassified (principalID);

            string xmlString = OSDParser.SerializeJsonString (new OSDMap ());
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding ();
            return encoding.GetBytes (xmlString);
        }

        public byte[] AddPick (OSDMap request)
        {
            ProfilePickInfo info = new ProfilePickInfo ();
            info.FromOSD ((OSDMap)request["Pick"]);

            ProfileConnector.AddPick (info);

            string xmlString = OSDParser.SerializeJsonString (new OSDMap ());
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding ();
            return encoding.GetBytes (xmlString);
        }

        public byte[] AddClassified (OSDMap request)
        {
            Classified info = new Classified ();
            info.FromOSD ((OSDMap)request["Classified"]);

            ProfileConnector.AddClassified (info);

            string xmlString = OSDParser.SerializeJsonString (new OSDMap ());
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding ();
            return encoding.GetBytes (xmlString);
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
            XmlTextWriter xw = new XmlTextWriter(ms, null);
            xw.Formatting = Formatting.Indented;
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }
    }

    public class OfflineMessagesInfoHandler
    {
        IOfflineMessagesConnector OfflineMessagesConnector;
        public OfflineMessagesInfoHandler ()
        {
            OfflineMessagesConnector = DataManager.RequestPlugin<IOfflineMessagesConnector> ("IOfflineMessagesConnectorLocal");
        }

        public byte[] GetOfflineMessages (OSDMap request)
        {
            OSDArray result = new OSDArray ();

            UUID PrincipalID = request["PrincipalID"].AsUUID ();
            GridInstantMessage[] Messages = OfflineMessagesConnector.GetOfflineMessages (PrincipalID);

            int i = 0;
            foreach (GridInstantMessage Message in Messages)
            {
                result.Add (Message.ToOSD ());
                i++;
            }

            string xmlString = OSDParser.SerializeJsonString (result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding ();
            return encoding.GetBytes (xmlString);
        }

        public byte[] AddOfflineMessage (OSDMap request)
        {
            GridInstantMessage message = new GridInstantMessage ();
            message.FromOSD (request);
            OfflineMessagesConnector.AddOfflineMessage (message);

            return SuccessResult ();
        }

        private byte[] SuccessResult ()
        {
            XmlDocument doc = new XmlDocument ();

            XmlNode xmlnode = doc.CreateNode (XmlNodeType.XmlDeclaration,
                    "", "");

            doc.AppendChild (xmlnode);

            XmlElement rootElement = doc.CreateElement ("", "ServerResponse",
                    "");

            doc.AppendChild (rootElement);

            XmlElement result = doc.CreateElement ("", "Result", "");
            result.AppendChild (doc.CreateTextNode ("Success"));

            rootElement.AppendChild (result);

            return DocToBytes (doc);
        }

        private byte[] DocToBytes (XmlDocument doc)
        {
            MemoryStream ms = new MemoryStream ();
            XmlTextWriter xw = new XmlTextWriter (ms, null);
            xw.Formatting = Formatting.Indented;
            doc.WriteTo (xw);
            xw.Flush ();

            return ms.ToArray ();
        }

        // http://social.msdn.microsoft.com/forums/en-US/csharpgeneral/thread/68f7ca38-5cd1-411f-b8d4-e4f7a688bc03
        // By: A Million Lemmings
        public string ConvertDecString (int dvalue)
        {

            string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            string retVal = string.Empty;

            double value = Convert.ToDouble (dvalue);

            do
            {

                double remainder = value - (26 * Math.Truncate (value / 26));

                retVal = retVal + CHARS.Substring ((int)remainder, 1);

                value = Math.Truncate (value / 26);

            }
            while (value > 0);



            return retVal;

        }
    }
}
