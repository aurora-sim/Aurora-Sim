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
using System.Xml;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;

namespace Aurora.Modules.CrossRegionBanSystem
{
    public class CrossRegionBanSystem : ISharedRegionModule
    {
        private readonly List<IScene> m_scenes = new List<IScene>();
        public string OurGetPassword = "";
        private IConfigSource m_config;

        #region ISharedRegionModule Members

        public void Initialise(IConfigSource source)
        {
            m_config = source;
        }

        public void Close()
        {
        }

        public void AddRegion(IScene scene)
        {
            m_scenes.Add(scene);
        }

        public void RemoveRegion(IScene scene)
        {
            m_scenes.Remove(scene);
        }

        public void RegionLoaded(IScene scene)
        {
            //Set up the incoming handler
            MainServer.Instance.AddStreamHandler(new CRBSIncoming(this));

            //Call up other sims
            IConfig banSysConfig = m_config.Configs["CrossRegionBanSystem"];
            if (banSysConfig != null)
            {
                if (!banSysConfig.GetBoolean("Enabled", false))
                    return;

                OurGetPassword = banSysConfig.GetString("OurPassword", "");

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
                    AskForeignServerForBans(URL, Passwords[i]);
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

        #endregion

        public bool AskForeignServerForBans(string URL, string password)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();

            sendData["Password"] = password;

            sendData["METHOD"] = "getbans";

            string reqString = WebUtils.BuildXmlResponse(sendData);

            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                                                                         URL + "/crbs",
                                                                         reqString);

                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = WebUtils.ParseXmlResponse(reply);

                    if (replyData != null)
                    {
                        if (!replyData.ContainsKey("result"))
                            return false;

                        if (replyData["result"].ToString() == "WrongPassword")
                        {
                            MainConsole.Instance.Warn("[CRBS]: Unable to connect successfully to " + URL +
                                       ", the foreign password was incorrect.");
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
                MainConsole.Instance.DebugFormat("[CRBS]: Exception when contacting server: {0}", e);
            }
            return false;
        }

        public void UnpackBans(Dictionary<string, object> replyData)
        {
            foreach (object f in replyData)
            {
                KeyValuePair<string, object> value = (KeyValuePair<string, object>) f;
                if (value.Value is Dictionary<string, object>)
                {
                    Dictionary<string, object> valuevalue = value.Value as Dictionary<string, object>;
                    foreach (object banUUID in valuevalue.Values)
                    {
                        UUID BanID = (UUID) banUUID;
                        foreach (IScene scene in m_scenes)
                        {
                            bool found = false;
#if (!ISWIN)
                            foreach (EstateBan ban in scene.RegionInfo.EstateSettings.EstateBans)
                            {
                                if (ban.BannedUserID == BanID)
                                {
                                    found = true;
                                }
                            }
#else
                            foreach (EstateBan ban in scene.RegionInfo.EstateSettings.EstateBans.Where(ban => ban.BannedUserID == BanID))
                            {
                                found = true;
                            }
#endif
                            if (!found)
                            {
                                scene.RegionInfo.EstateSettings.EstateBans[
                                    scene.RegionInfo.EstateSettings.EstateBans.Length] =
                                    new EstateBan
                                        {
                                            BannedUserID = BanID,
                                            BannedHostAddress = "",
                                            BannedHostIPMask = "",
                                            BannedHostNameMask = "",
                                            EstateID = scene.RegionInfo.EstateSettings.EstateID
                                        };
                            }
                        }
                    }
                }
            }
            //Update all the databases
            foreach (IScene scene in m_scenes)
            {
                scene.RegionInfo.EstateSettings.Save();
            }
        }

        public Dictionary<string, object> PackBans(Dictionary<string, object> result)
        {
            Dictionary<string, object> Bans = new Dictionary<string, object>();
            int i = 0;
            foreach (EstateBan ban in from scene in m_scenes from ban in scene.RegionInfo.EstateSettings.EstateBans where !Bans.ContainsValue(ban.BannedUserID) select ban)
            {
                Bans.Add(Util.ConvertDecString(i), ban.BannedUserID);
                i++;
            }
            result["Bans"] = Bans;
            return result;
        }
    }

    public class CRBSIncoming : BaseStreamHandler
    {
        private readonly CrossRegionBanSystem CRBS;

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
                    WebUtils.ParseXmlResponse(body);

                if (!request.ContainsKey("METHOD"))
                    return FailureResult();

                method = request["METHOD"].ToString();

                switch (method)
                {
                    case "getbans":
                        return NewConnection(request);
                }
                MainConsole.Instance.DebugFormat("[IWCConnector]: unknown method {0} request {1}", method.Length, method);
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[IWCConnector]: Exception {0} in " + method, e);
            }

            return FailureResult();
        }

        /// <summary>
        ///   This deals with incoming requests to add this server to their map.
        ///   This refuses or successfully allows the foreign server to interact with
        ///   this region.
        /// </summary>
        /// <param name = "request"></param>
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
            string xmlString = WebUtils.BuildXmlResponse(result);
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
}