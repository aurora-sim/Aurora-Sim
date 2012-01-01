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

using System.Collections;
using Aurora.DataManager;
using Aurora.Framework;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.CapsService
{
    public class MeshUploadFlag : ICapsServiceConnector
    {
        private IProfileConnector m_profileConnector;
        private IRegionClientCapsService m_service;
        private IUserAccountService m_userService;

        #region ICapsServiceConnector Members

        public void RegisterCaps(IRegionClientCapsService service)
        {
            m_service = service;
            m_userService = service.Registry.RequestModuleInterface<IUserAccountService>();
            m_profileConnector = DataManager.RequestPlugin<IProfileConnector>();
            m_service.AddStreamHandler("MeshUploadFlag",
                                       new RestHTTPHandler("GET", m_service.CreateCAPS("MeshUploadFlag", ""),
                                                           MeshUploadFlagCAP));
        }

        public void DeregisterCaps()
        {
            m_service.RemoveStreamHandler("MeshUploadFlag", "GET");
        }

        public void EnteringRegion()
        {
        }

        #endregion

        private Hashtable MeshUploadFlagCAP(Hashtable mDhttpMethod)
        {
            OSDMap data = new OSDMap();
            UserAccount acct = m_userService.GetUserAccount(UUID.Zero, m_service.AgentID);
            IUserProfileInfo info = m_profileConnector.GetUserProfile(m_service.AgentID);

            data["id"] = m_service.AgentID;
            data["username"] = acct.Name;
            data["display_name"] = info.DisplayName;
            data["display_name_next_update"] = Utils.UnixTimeToDateTime(0);
            data["legacy_first_name"] = acct.FirstName;
            data["legacy_last_name"] = acct.LastName;
            data["mesh_upload_status"] = "valid"; // add if account has ability to upload mesh?
            bool isDisplayNameNDefault = (info.DisplayName == acct.Name) ||
                                         (info.DisplayName == acct.FirstName + "." + acct.LastName);
            data["is_display_name_default"] = isDisplayNameNDefault;

            //Send back data
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = OSDParser.SerializeLLSDXmlString(data);
            return responsedata;
        }
    }
}