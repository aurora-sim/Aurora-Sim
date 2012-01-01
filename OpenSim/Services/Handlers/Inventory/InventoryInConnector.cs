/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using OpenSim.Services.Interfaces;

namespace OpenSim.Services
{
    public class InventoryInConnector : IService, IGridRegistrationUrlModule
    {
        private IRegistryCore m_registry;

        public string Name
        {
            get { return GetType().Name; }
        }

        #region IGridRegistrationUrlModule Members

        public string UrlName
        {
            get { return "InventoryServerURI"; }
        }

        public void AddExistingUrlForClient(string SessionID, string url, uint port)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);

            server.AddStreamHandler(new InventoryConnectorPostHandler(url, GetInventoryService(SessionID != ""),
                                                                      SessionID, m_registry));
        }

        public string GetUrlForRegisteringClient(string SessionID, uint port)
        {
            string url = "/xinventory" + UUID.Random();

            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);

            server.AddStreamHandler(new InventoryConnectorPostHandler(url, GetInventoryService(SessionID != ""),
                                                                      SessionID, m_registry));

            return url;
        }

        public void RemoveUrlForClient(string sessionID, string url, uint port)
        {
            IHttpServer server = m_registry.RequestModuleInterface<ISimulationBase>().GetHttpServer(port);
            server.RemoveHTTPHandler("POST", url);
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("InventoryInHandler", "") != Name)
                return;

            m_registry = registry;
            m_registry.RegisterModuleInterface(this);
            m_registry.RequestModuleInterface<IGridRegistrationService>().RegisterModule(this);
        }

        public void FinishedStartup()
        {
        }

        #endregion

        public IInventoryService GetInventoryService(bool isSecure)
        {
            //Try the external service first!
            IInventoryService service = m_registry.RequestModuleInterface<IExternalInventoryService>();
            if (!isSecure && service != null)
                return service;
            return m_registry.RequestModuleInterface<IInventoryService>();
        }
    }

    public class InventoryConnectorPostHandler : BaseStreamHandler
    {
        private readonly IInventoryService m_InventoryService;
        protected string m_SessionID;
        protected IRegistryCore m_registry;

        public InventoryConnectorPostHandler(string url, IInventoryService service, string SessionID,
                                             IRegistryCore registry) :
                                                 base("POST", url)
        {
            m_InventoryService = service;
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

            //MainConsole.Instance.DebugFormat("[XXX]: query String: {0}", body);

            try
            {
                Dictionary<string, object> request =
                    WebUtils.ParseQueryString(body);

                if (!request.ContainsKey("METHOD"))
                    return FailureResult();

                string method = request["METHOD"].ToString();
                request.Remove("METHOD");
                IGridRegistrationService urlModule =
                    m_registry.RequestModuleInterface<IGridRegistrationService>();
                switch (method)
                {
                    case "GETROOTFOLDER":
                        if (m_SessionID != "" && urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return HandleGetRootFolder(request);
                    case "GETFOLDERFORTYPE":
                        if (m_SessionID != "" && urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return HandleGetFolderForType(request);
                    case "GETFOLDERCONTENT":
                        if (m_SessionID != "" && urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return HandleGetFolderContent(request);
                    case "GETFOLDERITEMS":
                        if (m_SessionID != "" && urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return HandleGetFolderItems(request);
                    case "ADDFOLDER":
                        if (m_SessionID != "" && urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return HandleAddFolder(request);
                    case "UPDATEFOLDER":
                        if (m_SessionID != "" && urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.High))
                                return FailureResult();
                        return HandleUpdateFolder(request);
                    case "MOVEFOLDER":
                        if (m_SessionID != "" && urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return HandleMoveFolder(request);
                    case "DELETEFOLDERS":
                        if (m_SessionID != "" && urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.High))
                                return FailureResult();
                        return HandleDeleteFolders(request);
                    case "PURGEFOLDER":
                        if (m_SessionID != "" && urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.High))
                                return FailureResult();
                        return HandlePurgeFolder(request);
                    case "ADDITEM":
                        if (m_SessionID != "" && urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return HandleAddItem(request);
                    case "UPDATEITEM":
                        if (m_SessionID != "" && urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.High))
                                return FailureResult();
                        return HandleUpdateItem(request);
                    case "MOVEITEMS":
                        if (m_SessionID != "" && urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return HandleMoveItems(request);
                    case "DELETEITEMS":
                        if (m_SessionID != "" && urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.High))
                                return FailureResult();
                        return HandleDeleteItems(request);
                    case "GETITEM":
                        if (m_SessionID != "" && urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return HandleGetItem(request);
                    case "GETFOLDER":
                        if (m_SessionID != "" && urlModule != null)
                            if (!urlModule.CheckThreatLevel(m_SessionID, method, ThreatLevel.Medium))
                                return FailureResult();
                        return HandleGetFolder(request);
                }
                MainConsole.Instance.DebugFormat("[XINVENTORY HANDLER]: unknown method request: {0}", method);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Debug("[XINVENTORY HANDLER]: Exception {0}", e);
            }

            return FailureResult();
        }

        private byte[] FailureResult()
        {
            return BoolResult(false);
        }

        private byte[] SuccessResult()
        {
            return BoolResult(true);
        }

        private byte[] BoolResult(bool value)
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                                             "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                                                       "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "RESULT", "");
            result.AppendChild(doc.CreateTextNode(value.ToString()));

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

        private byte[] HandleGetRootFolder(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID principal = UUID.Zero;
            UUID.TryParse(request["PRINCIPAL"].ToString(), out principal);
            InventoryFolderBase rfolder = m_InventoryService.GetRootFolder(principal);
            if (rfolder != null)
                result["folder"] = EncodeFolder(rfolder);

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[XXX]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] HandleGetFolderForType(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            UUID principal = UUID.Zero;
            UUID.TryParse(request["PRINCIPAL"].ToString(), out principal);
            int type = 0;
            Int32.TryParse(request["TYPE"].ToString(), out type);
            int invtype = 0;
            if (request.ContainsKey("INVTYPE"))
                Int32.TryParse(request["INVTYPE"].ToString(), out invtype);
            InventoryFolderBase folder = m_InventoryService.GetFolderForType(principal, (InventoryType) invtype,
                                                                             (AssetType) type);
            if (folder != null)
                result["folder"] = EncodeFolder(folder);

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[XXX]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] HandleGetFolderContent(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            UUID principal = UUID.Zero;
            UUID.TryParse(request["PRINCIPAL"].ToString(), out principal);
            UUID folderID = UUID.Zero;
            UUID.TryParse(request["FOLDER"].ToString(), out folderID);

            InventoryCollection icoll = m_InventoryService.GetFolderContent(principal, folderID);
            if (icoll != null)
            {
                Dictionary<string, object> folders = new Dictionary<string, object>();
                int i = 0;
                foreach (InventoryFolderBase f in icoll.Folders)
                {
                    folders["folder_" + i.ToString()] = EncodeFolder(f);
                    i++;
                }
                result["FOLDERS"] = folders;

                i = 0;
                Dictionary<string, object> items = new Dictionary<string, object>();
                foreach (InventoryItemBase it in icoll.Items)
                {
                    items["item_" + i.ToString()] = EncodeItem(it);
                    i++;
                }
                result["ITEMS"] = items;
            }

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[XXX]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] HandleGetFolderItems(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            UUID principal = UUID.Zero;
            UUID.TryParse(request["PRINCIPAL"].ToString(), out principal);
            UUID folderID = UUID.Zero;
            UUID.TryParse(request["FOLDER"].ToString(), out folderID);

            List<InventoryItemBase> items = m_InventoryService.GetFolderItems(principal, folderID);
            Dictionary<string, object> sitems = new Dictionary<string, object>();

            if (items != null)
            {
                int i = 0;
                foreach (InventoryItemBase item in items)
                {
                    sitems["item_" + i.ToString()] = EncodeItem(item);
                    i++;
                }
            }
            result["ITEMS"] = sitems;

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[XXX]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] HandleAddFolder(Dictionary<string, object> request)
        {
            InventoryFolderBase folder = BuildFolder(request);

            if (m_InventoryService.AddFolder(folder))
                return SuccessResult();
            else
                return FailureResult();
        }

        private byte[] HandleUpdateFolder(Dictionary<string, object> request)
        {
            InventoryFolderBase folder = BuildFolder(request);

            if (m_InventoryService.UpdateFolder(folder))
                return SuccessResult();
            else
                return FailureResult();
        }

        private byte[] HandleMoveFolder(Dictionary<string, object> request)
        {
            UUID parentID = UUID.Zero;
            UUID.TryParse(request["ParentID"].ToString(), out parentID);
            UUID folderID = UUID.Zero;
            UUID.TryParse(request["ID"].ToString(), out folderID);
            UUID principal = UUID.Zero;
            UUID.TryParse(request["PRINCIPAL"].ToString(), out principal);

            InventoryFolderBase folder = new InventoryFolderBase(folderID, "", principal, parentID);
            if (m_InventoryService.MoveFolder(folder))
                return SuccessResult();
            else
                return FailureResult();
        }

        byte[] HandleDeleteFolders(Dictionary<string, object> request)
        {
            UUID principal = UUID.Zero;
            UUID.TryParse(request["PRINCIPAL"].ToString(), out principal);
            List<string> slist = (List<string>)request["FOLDERS"];
            List<UUID> uuids = new List<UUID>();
            foreach (string s in slist)
            {
                UUID u = UUID.Zero;
                if (UUID.TryParse(s, out u))
                    uuids.Add(u);
            }

            return m_InventoryService.DeleteFolders(principal, uuids) ? SuccessResult() : FailureResult();
        }

        private byte[] HandlePurgeFolder(Dictionary<string, object> request)
        {
            UUID folderID = UUID.Zero;
            UUID.TryParse(request["ID"].ToString(), out folderID);

            InventoryFolderBase folder = new InventoryFolderBase(folderID);
            if (m_InventoryService.PurgeFolder(folder))
                return SuccessResult();
            else
                return FailureResult();
        }

        private byte[] HandleAddItem(Dictionary<string, object> request)
        {
            InventoryItemBase item = BuildItem(request);

            if (m_InventoryService.AddItem(item))
                return SuccessResult();
            else
                return FailureResult();
        }

        private byte[] HandleUpdateItem(Dictionary<string, object> request)
        {
            InventoryItemBase item = BuildItem(request);

            if (m_InventoryService.UpdateItem(item))
                return SuccessResult();
            else
                return FailureResult();
        }

        byte[] HandleMoveItems(Dictionary<string, object> request)
        {
            List<string> idlist = (List<string>)request["IDLIST"];
            List<string> destlist = (List<string>)request["DESTLIST"];
            UUID principal = UUID.Zero;
            UUID.TryParse(request["PRINCIPAL"].ToString(), out principal);

            List<InventoryItemBase> items = new List<InventoryItemBase>();
            int n = 0;
            try
            {
                foreach (string s in idlist)
                {
                    UUID u = UUID.Zero;
                    if (UUID.TryParse(s, out u))
                    {
                        UUID fid = UUID.Zero;
                        if (UUID.TryParse(destlist[n++], out fid))
                        {
                            InventoryItemBase item = new InventoryItemBase(u, principal) {Folder = fid};
                            items.Add(item);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[XINVENTORY IN CONNECTOR]: Exception in HandleMoveItems: {0}", e.Message);
                return FailureResult();
            }

            if (m_InventoryService.MoveItems(principal, items))
                return SuccessResult();
            else
                return FailureResult();
        }

        byte[] HandleDeleteItems(Dictionary<string, object> request)
        {
            UUID principal = UUID.Zero;
            UUID.TryParse(request["PRINCIPAL"].ToString(), out principal);
            List<string> slist = (List<string>)request["ITEMS"];
            List<UUID> uuids = new List<UUID>();
            foreach (string s in slist)
            {
                UUID u = UUID.Zero;
                if (UUID.TryParse(s, out u))
                    uuids.Add(u);
            }

            if (m_InventoryService.DeleteItems(principal, uuids))
                return SuccessResult();
            return
                FailureResult();
        }

        private byte[] HandleGetItem(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            UUID id = UUID.Zero;
            UUID.TryParse(request["ID"].ToString(), out id);

            InventoryItemBase item = new InventoryItemBase(id);
            item = m_InventoryService.GetItem(item);
            if (item != null)
                result["item"] = EncodeItem(item);

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[XXX]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] HandleGetFolder(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            UUID id = UUID.Zero;
            UUID.TryParse(request["ID"].ToString(), out id);

            InventoryFolderBase folder = new InventoryFolderBase(id);
            folder = m_InventoryService.GetFolder(folder);
            if (folder != null)
                result["folder"] = EncodeFolder(folder);

            string xmlString = WebUtils.BuildXmlResponse(result);
            //MainConsole.Instance.DebugFormat("[XXX]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private Dictionary<string, object> EncodeFolder(InventoryFolderBase f)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();

            ret["ParentID"] = f.ParentID.ToString();
            ret["Type"] = f.Type.ToString();
            ret["Version"] = f.Version.ToString();
            ret["Name"] = f.Name;
            ret["Owner"] = f.Owner.ToString();
            ret["ID"] = f.ID.ToString();

            return ret;
        }

        private Dictionary<string, object> EncodeItem(InventoryItemBase item)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();

            ret["AssetID"] = item.AssetID.ToString();
            ret["AssetType"] = item.AssetType.ToString();
            ret["BasePermissions"] = item.BasePermissions.ToString();
            ret["CreationDate"] = item.CreationDate.ToString();
            if (item.CreatorId != null)
                ret["CreatorId"] = item.CreatorId;
            else
                ret["CreatorId"] = String.Empty;
            if (item.CreatorData != null)
                ret["CreatorData"] = item.CreatorData;
            else
                ret["CreatorData"] = String.Empty;
            ret["CreatorData"] = item.CreatorData;
            ret["CurrentPermissions"] = item.CurrentPermissions.ToString();
            ret["Description"] = item.Description;
            ret["EveryOnePermissions"] = item.EveryOnePermissions.ToString();
            ret["Flags"] = item.Flags.ToString();
            ret["Folder"] = item.Folder.ToString();
            ret["GroupID"] = item.GroupID.ToString();
            ret["GroupOwned"] = item.GroupOwned.ToString();
            ret["GroupPermissions"] = item.GroupPermissions.ToString();
            ret["ID"] = item.ID.ToString();
            ret["InvType"] = item.InvType.ToString();
            ret["Name"] = item.Name;
            ret["NextPermissions"] = item.NextPermissions.ToString();
            ret["Owner"] = item.Owner.ToString();
            ret["SalePrice"] = item.SalePrice.ToString();
            ret["SaleType"] = item.SaleType.ToString();

            return ret;
        }

        private InventoryFolderBase BuildFolder(Dictionary<string, object> data)
        {
            InventoryFolderBase folder = new InventoryFolderBase
                                             {
                                                 ParentID = new UUID(data["ParentID"].ToString()),
                                                 Type = short.Parse(data["Type"].ToString()),
                                                 Version = ushort.Parse(data["Version"].ToString()),
                                                 Name = data["Name"].ToString(),
                                                 Owner = new UUID(data["Owner"].ToString()),
                                                 ID = new UUID(data["ID"].ToString())
                                             };


            return folder;
        }

        private InventoryItemBase BuildItem(Dictionary<string, object> data)
        {
            InventoryItemBase item = new InventoryItemBase
                                         {
                                             AssetID = new UUID(data["AssetID"].ToString()),
                                             AssetType = int.Parse(data["AssetType"].ToString()),
                                             Name = data["Name"].ToString(),
                                             Owner = new UUID(data["Owner"].ToString()),
                                             ID = new UUID(data["ID"].ToString()),
                                             InvType = int.Parse(data["InvType"].ToString()),
                                             Folder = new UUID(data["Folder"].ToString()),
                                             CreatorId = data["CreatorId"].ToString(),
                                             CreatorData = data["CreatorData"].ToString(),
                                             Description = data["Description"].ToString(),
                                             NextPermissions = uint.Parse(data["NextPermissions"].ToString()),
                                             CurrentPermissions = uint.Parse(data["CurrentPermissions"].ToString()),
                                             BasePermissions = uint.Parse(data["BasePermissions"].ToString()),
                                             EveryOnePermissions = uint.Parse(data["EveryOnePermissions"].ToString()),
                                             GroupPermissions = uint.Parse(data["GroupPermissions"].ToString()),
                                             GroupID = new UUID(data["GroupID"].ToString()),
                                             GroupOwned = bool.Parse(data["GroupOwned"].ToString()),
                                             SalePrice = int.Parse(data["SalePrice"].ToString()),
                                             SaleType = byte.Parse(data["SaleType"].ToString()),
                                             Flags = uint.Parse(data["Flags"].ToString()),
                                             CreationDate = int.Parse(data["CreationDate"].ToString())
                                         };


            return item;
        }
    }
}