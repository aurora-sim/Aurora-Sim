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
using OpenSim.Server.Base;
using log4net;
using OpenSim.Services.Interfaces;
using OpenMetaverse;
using Aurora.DataManager;

namespace Aurora.Modules
{
    public class CrossRegionBanSystem : ISharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public string OurGetPassword = "";
        private List<Scene> m_scenes = new List<Scene>();
        private IConfigSource m_config;

        public void Initialise(IConfigSource source)
        {
            m_config = source;
            IConfig banSysConfig = source.Configs["BanSystem"];
            if (banSysConfig != null)
            {

            }
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            m_scenes.Add(scene);
        }

        public void RemoveRegion(Scene scene)
        {
            m_scenes.Remove(scene);
        }

        public void RegionLoaded(Scene scene)
        {
            //Set up the incoming handler
            MainServer.Instance.AddStreamHandler(new CRBSIncoming(this));

            //Call up other sims
            IConfig banSysConfig = m_config.Configs["BanSystem"];
            if (banSysConfig != null)
            {
                string URLlist = banSysConfig.GetString("URLs", string.Empty);
                if (URLlist == string.Empty)
                    return;
                List<string> URLS = new List<string>(URLlist.Split(','));

                string Passlist = banSysConfig.GetString("Passwords", string.Empty);
                if (Passlist == string.Empty)
                    return;
                List<string> Passwords = new List<string>(Passlist.Split(','));

                int i = 0;
                foreach (string URL in URLS)
                {
                    AskForeignServerForBans(URLS[i], Passwords[i]);
                    i++;
                }
            }
        }

        public void PostInitialise()
        {
        }

        public string Name
        {
            get { return "CrossRegionBanSystem"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public bool AskForeignServerForBans(string URL, string password)
        {
            Dictionary<string, object> sendData = new Dictionary<string,object>();

            sendData["Password"] = password;

            sendData["METHOD"] = "getbans";

            string reqString = ServerUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        URL + "/crbs",
                        reqString);

                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        if (!replyData.ContainsKey("result"))
                            return false;

                        if (replyData["result"].ToString() == "WrongPassword")
                        {
                            m_log.Warn("[CRBS]: Unable to connect successfully to " + URL + ", the foreign password was incorrect.");
                            return false;
                        }

                        if (replyData["result"].ToString() == "Successful")
                        {
                            UnpackBans(replyData);
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[CRBS]: Exception when contacting server: {0}", e.Message);
            }
            return false;
        }

        public void UnpackBans(Dictionary<string, object> replyData)
        {
            foreach (object f in replyData)
            {
                KeyValuePair<string, object> value = (KeyValuePair<string, object>)f;
                if (value.Value is Dictionary<string, object>)
                {
                    Dictionary<string, object> valuevalue = value.Value as Dictionary<string, object>;
                    foreach (object banUUID in valuevalue.Values)
                    {
                        UUID BanID = (UUID)banUUID;
                        foreach (Scene scene in m_scenes)
                        {
                            bool found = false;
                            foreach (EstateBan ban in scene.RegionInfo.EstateSettings.EstateBans)
                            {
                                if (ban.BannedUserID == BanID)
                                    found = true;
                            }
                            if (!found)
                            {
                                scene.RegionInfo.EstateSettings.EstateBans[scene.RegionInfo.EstateSettings.EstateBans.Length] = 
                                    new EstateBan(){
                                        BannedUserID = BanID,
                                        BannedHostAddress = "",
                                        BannedHostIPMask = "",
                                        BannedHostNameMask = "",
                                        EstateID = scene.RegionInfo.EstateSettings.EstateID};
                            }
                        }
                    }
                }
            }
        }

        public Dictionary<string, object> PackBans(Dictionary<string, object> result)
        {
            Dictionary<string, object> Bans = new Dictionary<string, object>();
            int i = 0;
            foreach (Scene scene in m_scenes)
            {
                foreach (EstateBan ban in scene.RegionInfo.EstateSettings.EstateBans)
                {
                    if (!Bans.ContainsValue(ban.BannedUserID))
                    {
                        Bans.Add(ConvertDecString(i), ban.BannedUserID);
                        i++;
                    }
                }
            }
            result["Bans"] = Bans;
            return result;
        }

        // http://social.msdn.microsoft.com/forums/en-US/csharpgeneral/thread/68f7ca38-5cd1-411f-b8d4-e4f7a688bc03
        // By: A Million Lemmings
        public string ConvertDecString(int dvalue)
        {
            string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string retVal = string.Empty;
            double value = Convert.ToDouble(dvalue);
            do
            {
                double remainder = value - (26 * Math.Truncate(value / 26));
                retVal = retVal + CHARS.Substring((int)remainder, 1);
                value = Math.Truncate(value / 26);
            }
            while (value > 0);
            return retVal;
        }
    }
    public class CRBSIncoming : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private CrossRegionBanSystem CRBS;

        public CRBSIncoming(CrossRegionBanSystem crbs) :
            base("POST", "/crbs")
        {
            CRBS = crbs;
        }

        public override byte[] Handle(string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            string method = "";
            try
            {
                Dictionary<string, object> request =
                        ServerUtils.ParseXmlResponse(body);

                if (!request.ContainsKey("METHOD"))
                    return FailureResult();

                method = request["METHOD"].ToString();

                switch (method)
                {
                    case "getbans":
                        return NewConnection(request);

                }
                m_log.DebugFormat("[IWCConnector]: unknown method {0} request {1}", method.Length, method);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[IWCConnector]: Exception {0} in " + method, e);
            }

            return FailureResult();

        }

        /// <summary>
        /// This deals with incoming requests to add this server to their map.
        /// This refuses or successfully allows the foreign server to interact with
        /// this region.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private byte[] NewConnection(Dictionary<string, object> request)
        {

            Dictionary<string, object> result = new Dictionary<string, object>();

            if (result["Password"].ToString() != CRBS.OurGetPassword)
            {
                result["result"] = "WrongPassword";
            }
            else
            {
                result["result"] = "Successful";
                result = CRBS.PackBans(result);
            }

            return Return(result);
        }

        #region Misc

        private byte[] Return(Dictionary<string, object> result)
        {
            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
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
