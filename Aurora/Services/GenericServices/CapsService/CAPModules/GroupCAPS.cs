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
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.DatabaseInterfaces;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.Collections.Generic;
using System.IO;

namespace Aurora.Services
{
    public class GroupCAPS : ICapsServiceConnector
    {
        protected IGroupsServiceConnector m_groupService;
        protected IRegionClientCapsService m_service;

        public void RegisterCaps(IRegionClientCapsService service)
        {
            m_service = service;
            m_groupService = Framework.Utilities.DataManager.RequestPlugin<IGroupsServiceConnector>();

            service.AddStreamHandler("GroupMemberData",
                                     new GenericStreamHandler("POST", service.CreateCAPS("GroupMemberData", ""),
                                                              GroupMemberData));
        }

        public void EnteringRegion()
        {
        }

        public void DeregisterCaps()
        {
            m_service.RemoveStreamHandler("GroupMemberData", "POST");
        }

        #region Group Members

        public byte[] GroupMemberData(string path, Stream request, OSHttpRequest httpRequest,
                                      OSHttpResponse httpResponse)
        {
            try
            {
                //MainConsole.Instance.Debug("[CAPS]: UploadBakedTexture Request in region: " +
                //        m_regionName);

                OSDMap rm = (OSDMap) OSDParser.DeserializeLLSDXml(request);
                UUID groupID = rm["group_id"].AsUUID();

                OSDMap defaults = new OSDMap();
                ulong EveryonePowers = (ulong) (GroupPowers.AllowSetHome |
                                                GroupPowers.Accountable |
                                                GroupPowers.JoinChat |
                                                GroupPowers.AllowVoiceChat |
                                                GroupPowers.ReceiveNotices |
                                                GroupPowers.StartProposal |
                                                GroupPowers.VoteOnProposal);
                defaults["default_powers"] = EveryonePowers;

                List<string> titles = new List<string>();
                OSDMap members = new OSDMap();
                int count = 0;
                foreach (GroupMembersData gmd in m_groupService.GetGroupMembers(m_service.AgentID, groupID))
                {
                    OSDMap member = new OSDMap();
                    member["donated_square_meters"] = gmd.Contribution;
                    member["owner"] = (gmd.IsOwner ? "Y" : "N");
                    member["last_login"] = gmd.OnlineStatus;
                    if (titles.Contains(gmd.Title))
                    {
                        member["title"] = titles.FindIndex((s) => s == gmd.Title);
                    }
                    else
                    {
                        titles.Add(gmd.Title);
                        member["title"] = titles.Count-1;
                    }
                    member["powers"] = gmd.AgentPowers;
                    count++;
                    members[gmd.AgentID.ToString()] = member;
                }

                OSDMap map = new OSDMap();
                map["member_count"] = count;
                map["group_id"] = groupID;
                map["defaults"] = defaults;
                map["titles"] = titles.ToOSDArray();
                map["members"] = members;
                return OSDParser.SerializeLLSDXmlBytes(map);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[CAPS]: " + e);
            }

            return null;
        }

        #endregion
    }
}