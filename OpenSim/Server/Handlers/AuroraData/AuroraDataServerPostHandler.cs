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
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Services.DataService;

namespace OpenSim.Server.Handlers.AuroraData
{
    public class AuroraDataServerPostHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IProfileConnector ProfileConnector = null;
        private IAgentConnector AgentConnector = null;
        private IGridConnector GridConnector = null;


        public AuroraDataServerPostHandler() :
            base("POST", "/auroradata")
        {
            ProfileConnector = DataManager.IProfileConnector;
            GridConnector = DataManager.IGridConnector;
            AgentConnector = DataManager.IAgentConnector;
        }

        public override byte[] Handle(string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            //m_log.DebugFormat("[AuroraDataServerPostHandler]: query String: {0}", body);
            string method = "";
            try
            {
                Dictionary<string, object> request =
                        ServerUtils.ParseQueryString(body);

                if (!request.ContainsKey("METHOD"))
                    return FailureResult();

                method = request["METHOD"].ToString();

                switch (method)
                {
                    case "getprofile":
                        return GetProfile(request);
                    case "updateprofile":
                        return UpdateProfile(request);
                    case "updateinterests":
                        return UpdateInterests(request);
                    case "createprofile":
                        return CreateProfile(request);
                    case "removefromcache":
                        return RemoveFromCache(request);
                    case "updateusernotes":
                        return UpdateUserNotes(request);
                    case "addclassified":
                        return AddClassified(request);
                    case "deleteclassified":
                        return DeleteClassified(request);
                    case "addpick":
                        return AddPick(request);
                    case "deletepick":
                        return DeletePick(request);
                    case "updatepick":
                        return UpdatePick(request);
                    case "getpick":
                        return RemoveFromCache(request);
                    case "getagent":
                        return GetAgent(request);
                    case "updateagent":
                        return UpdateAgent(request);
                    case "createagent":
                        return CreateAgent(request);
                    case "createregion":
                        return CreateRegion(request);
                    case "getregionflags":
                        return GetRegionFlags(request);
                    case "setregionflags":
                        return SetRegionFlags(request);
                    case "removetelehub":
                        return RemoveTelehub(request);

                }
                m_log.DebugFormat("[AuroraDataServerPostHandler]: unknown method {0} request {1}", method.Length, method);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraDataServerPostHandler]: Exception {0} in " + method, e);
            }

            return FailureResult();

        }

        #region Methods

        byte[] UpdateUserNotes(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
            {
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to get profile");
                result["result"] = "null";
                string FailedxmlString = ServerUtils.BuildXmlResponse(result);
                //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", FailedxmlString);
                UTF8Encoding Failedencoding = new UTF8Encoding();
                return Failedencoding.GetBytes(FailedxmlString);
            }
            UUID targetID = UUID.Zero;
            if (request.ContainsKey("TARGETID"))
                UUID.TryParse(request["TARGETID"].ToString(), out targetID);
            string notes = "";
            if (request.ContainsKey("NOTES"))
                notes = request["NOTES"].ToString();

            IUserProfileInfo UserProfile = new IUserProfileInfo(request);
            ProfileConnector.UpdateUserNotes(principalID, targetID, notes, UserProfile);
            result["result"] = "Successful";

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] GetPick(Dictionary<string, object> request)
        {
            string pickID = "";
            if (request.ContainsKey("PICKID"))
                pickID = request["PICKID"].ToString();
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no pickID in request to get pick");

            ProfilePickInfo Pick = ProfileConnector.FindPick(pickID);
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (Pick == null)
                result["result"] = "null";
            else
            {
                result["result"] = Pick.ToKeyValuePairs();
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);

        }

        byte[] DeletePick(Dictionary<string, object> request)
        {
            string pickID = request["PICKID"].ToString();
            string principalID = request["PRINCIPALID"].ToString();

            ProfileConnector.DeletePick(new UUID(pickID), new UUID(principalID));
            return SuccessResult();
        }

        byte[] UpdatePick(Dictionary<string, object> request)
        {
            ProfilePickInfo pick = new ProfilePickInfo(request);
            ProfileConnector.UpdatePick(pick);

            return SuccessResult();
        }

        byte[] AddPick(Dictionary<string, object> request)
        {
            ProfilePickInfo pick = new ProfilePickInfo(request);
            ProfileConnector.AddPick(pick);

            return SuccessResult();
        }

        byte[] GetClassified(Dictionary<string, object> request)
        {
            string classifiedID = "";
            if (request.ContainsKey("CLASSIFIEDID"))
                classifiedID = request["CLASSIFIEDID"].ToString();
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no classifiedID in request to get classifed");

            Classified Classified = ProfileConnector.FindClassified(classifiedID);
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (Classified == null)
                result["result"] = "null";
            else
            {
                result["result"] = Classified.ToKeyValuePairs();
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] DeleteClassified(Dictionary<string, object> request)
        {
            string classifiedID = request["CLASSIFIEDID"].ToString();
            string principalID = request["PRINCIPALID"].ToString();

            ProfileConnector.DeleteClassified(new UUID(classifiedID), new UUID(principalID));
            return SuccessResult();
        }

        byte[] AddClassified(Dictionary<string, object> request)
        {
            Classified Classified = new Classified(request);
            ProfileConnector.AddClassified(Classified);

            return SuccessResult();
        }

        byte[] GetProfile(Dictionary<string, object> request)
        {
            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to get profile");

            IUserProfileInfo UserProfile = ProfileConnector.GetUserProfile(principalID);
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (UserProfile == null)
                result["result"] = "null";
            else
            {
                result["result"] = UserProfile.ToKeyValuePairs();
            }
             
            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] UpdateProfile(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            
            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
            {
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to get profile");
                result["result"] = "null";
                string FailedxmlString = ServerUtils.BuildXmlResponse(result);
                m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", FailedxmlString);
                UTF8Encoding Failedencoding = new UTF8Encoding();
                return Failedencoding.GetBytes(FailedxmlString);
            }

            IUserProfileInfo UserProfile = new IUserProfileInfo(request);
            ProfileConnector.UpdateUserProfile(UserProfile);
            result["result"] = "Successful";

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] UpdateInterests(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
            {
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to get profile");
                result["result"] = "null";
                string FailedxmlString = ServerUtils.BuildXmlResponse(result);
                m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", FailedxmlString);
                UTF8Encoding Failedencoding = new UTF8Encoding();
                return Failedencoding.GetBytes(FailedxmlString);
            }

            IUserProfileInfo UserProfile = new IUserProfileInfo(request);
            ProfileConnector.UpdateUserInterests(UserProfile);
            result["result"] = "Successful";

            string xmlString = ServerUtils.BuildXmlResponse(result);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] CreateProfile(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to get profile");

            ProfileConnector.CreateNewProfile(principalID);
            result["result"] = "Successful";

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] RemoveFromCache(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to get profile");

            ProfileConnector.RemoveFromCache(principalID);
            result["result"] = "Successful";

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] GetAgent(Dictionary<string, object> request)
        {
            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to get agent");

            IAgentInfo Agent = AgentConnector.GetAgent(principalID);
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (Agent == null)
                result["result"] = "null";
            else
            {
                result["result"] = Agent.ToKeyValuePairs();
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] UpdateAgent(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
            {
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to update agent");
                result["result"] = "null";
                string FailedxmlString = ServerUtils.BuildXmlResponse(result);
                m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", FailedxmlString);
                UTF8Encoding Failedencoding = new UTF8Encoding();
                return Failedencoding.GetBytes(FailedxmlString);
            }

            IAgentInfo Agent = new IAgentInfo(request);
            AgentConnector.UpdateAgent(Agent);
            result["result"] = "Successful";

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] CreateAgent(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to get profile");

            AgentConnector.CreateNewAgent(principalID);
            result["result"] = "Successful";

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] CreateRegion(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            UUID regionID = UUID.Zero;
            if (request.ContainsKey("REGIONID"))
                UUID.TryParse(request["REGIONID"].ToString(), out regionID);
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no regionID in request to create region");

            GridConnector.CreateRegion(regionID);
            result["result"] = "Successful";

            string xmlString = ServerUtils.BuildXmlResponse(result);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] SetRegionFlags(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            UUID regionID = UUID.Zero;
            if (request.ContainsKey("REGIONID"))
                UUID.TryParse(request["REGIONID"].ToString(), out regionID);
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no regionID in request to set region flags");

            GridRegionFlags flags = (GridRegionFlags)0;
            if (request.ContainsKey("FLAGS"))
                flags = (GridRegionFlags)Convert.ToInt32(request["REGIONID"].ToString());

            GridConnector.SetRegionFlags(regionID, flags);
            result["result"] = "Successful";

            string xmlString = ServerUtils.BuildXmlResponse(result);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] RemoveTelehub(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            UUID regionID = UUID.Zero;
            if (request.ContainsKey("REGIONID"))
                UUID.TryParse(request["REGIONID"].ToString(), out regionID);
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no regionID in request to remove telehub");

            GridConnector.RemoveTelehub(regionID);
            result["result"] = "Successful";

            string xmlString = ServerUtils.BuildXmlResponse(result);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] GetRegionFlags(Dictionary<string, object> request)
        {
            UUID regionID = UUID.Zero;
            if (request.ContainsKey("REGIONID"))
                UUID.TryParse(request["REGIONID"].ToString(), out regionID);
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no regionID in request to get region flags");

            GridRegionFlags flags = GridConnector.GetRegionFlags(regionID);
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (flags == null || flags == (GridRegionFlags)(-1))
                result["result"] = "null";
            else
            {
                result["result"] = flags;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        #endregion

        #region Misc

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
}
