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

using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.Modules;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.SceneInfo.Entities;
using Aurora.Framework.Services.ClassHelpers.Profile;
using Aurora.Framework.Utilities;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using ProtoBuf;
using System.Collections.Generic;

namespace Aurora.Framework.Services
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class CachedUserInfo : IDataTransferable
    {
        public IAgentInfo AgentInfo;
        public UserAccount UserAccount;
        public GroupMembershipData ActiveGroup;
        public List<GroupMembershipData> GroupMemberships = new List<GroupMembershipData>();
        public List<GridInstantMessage> OfflineMessages = new List<GridInstantMessage>();
        public List<MuteList> MuteList = new List<MuteList>();
        public AvatarAppearance Appearance = null;
        public List<UUID> FriendOnlineStatuses = new List<UUID>();
        public List<FriendInfo> Friends = new List<FriendInfo>();

        public override void FromOSD(OSDMap map)
        {
            AgentInfo = new IAgentInfo();
            AgentInfo.FromOSD((OSDMap) (map["AgentInfo"]));
            UserAccount = new UserAccount();
            UserAccount.FromOSD((OSDMap)(map["UserAccount"]));
            if (!map.ContainsKey("ActiveGroup"))
                ActiveGroup = null;
            else
            {
                ActiveGroup = new GroupMembershipData();
                ActiveGroup.FromOSD((OSDMap)(map["ActiveGroup"]));
            }
            GroupMemberships = ((OSDArray) map["GroupMemberships"]).ConvertAll<GroupMembershipData>((o) =>
                                                                                                        {
                                                                                                            GroupMembershipData
                                                                                                                group =
                                                                                                                    new GroupMembershipData
                                                                                                                        ();
                                                                                                            group
                                                                                                                .FromOSD
                                                                                                                ((OSDMap
                                                                                                                 ) o);
                                                                                                            return group;
                                                                                                        });
            OfflineMessages = ((OSDArray) map["OfflineMessages"]).ConvertAll<GridInstantMessage>((o) =>
                                                                                                     {
                                                                                                         GridInstantMessage
                                                                                                             group =
                                                                                                                 new GridInstantMessage
                                                                                                                     ();
                                                                                                         group.FromOSD(
                                                                                                             (OSDMap) o);
                                                                                                         return group;
                                                                                                     });
            MuteList = ((OSDArray) map["MuteList"]).ConvertAll<MuteList>((o) =>
                                                                             {
                                                                                 MuteList group = new MuteList();
                                                                                 group.FromOSD((OSDMap) o);
                                                                                 return group;
                                                                             });

            if (map.ContainsKey("Appearance"))
            {
                Appearance = new AvatarAppearance();
                Appearance.FromOSD((OSDMap)map["Appearance"]);
            }
            if (map.ContainsKey("FriendOnlineStatuses"))
                FriendOnlineStatuses = ((OSDArray)map["FriendOnlineStatuses"]).ConvertAll<UUID>((o) => { return o; });
            if (map.ContainsKey("Friends"))
                Friends = ((OSDArray)map["Friends"]).ConvertAll<FriendInfo>((o) =>
                { 
                    FriendInfo f = new FriendInfo();
                    f.FromOSD((OSDMap)o);
                    return f; 
                });
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["AgentInfo"] = AgentInfo.ToOSD();
            map["UserAccount"] = UserAccount.ToOSD();
            if (ActiveGroup != null)
                map["ActiveGroup"] = ActiveGroup.ToOSD();
            map["GroupMemberships"] = GroupMemberships.ToOSDArray();
            map["OfflineMessages"] = OfflineMessages.ToOSDArray();
            map["MuteList"] = MuteList.ToOSDArray();
            if(Appearance != null)
                map["Appearance"] = Appearance.ToOSD();
            map["FriendOnlineStatuses"] = FriendOnlineStatuses.ToOSDArray();
            map["Friends"] = Friends.ToOSDArray();
            return map;
        }
    }

    public interface ISimulationService
    {
        #region Agents

        /// <summary>
        ///     Create an agent at the given destination
        ///     Grid Server only.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="aCircuit"></param>
        /// <param name="teleportFlags"></param>
        /// <param name="data"></param>
        /// <param name="requestedUDPPort"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        bool CreateAgent(GridRegion destination, AgentCircuitData aCircuit, uint teleportFlags,
                         out CreateAgentResponse response);

        /// <summary>
        ///     Full child agent update.
        ///     Grid Server only.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        bool UpdateAgent(GridRegion destination, AgentData data);

        /// <summary>
        ///     Short child agent update, mostly for position.
        ///     Grid Server only.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        bool UpdateAgent(GridRegion destination, AgentPosition data);

        /// <summary>
        ///     Pull the root agent info from the given destination
        ///     Grid Server only.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="id"></param>
        /// <param name="agentIsLeaving"></param>
        /// <param name="agent"></param>
        /// <param name="circuitData"></param>
        /// <returns></returns>
        bool RetrieveAgent(GridRegion destination, UUID id, bool agentIsLeaving, out AgentData agent,
                           out AgentCircuitData circuitData);

        /// <summary>
        ///     Close agent.
        ///     Grid Server only.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        bool CloseAgent(GridRegion destination, UUID id);

        /// <summary>
        ///     Makes a root agent into a child agent in the given region
        ///     DOES mark the agent as leaving (removes attachments among other things)
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="Region"></param>
        /// <param name="markAgentAsLeaving"></param>
        bool MakeChildAgent(UUID AgentID, GridRegion oldRegion, GridRegion Region, bool markAgentAsLeaving);

        /// <summary>
        ///     Sends that a teleport failed to the given user
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="failedRegionID"></param>
        /// <param name="agentID"></param>
        /// <param name="reason"></param>
        /// <param name="isCrossing"></param>
        bool FailedToTeleportAgent(GridRegion destination, UUID failedRegionID, UUID agentID, string reason,
                                   bool isCrossing);

        /// <summary>
        ///     Tells the region that the agent was not able to leave the region and needs to be resumed
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="RegionID"></param>
        bool FailedToMoveAgentIntoNewRegion(UUID AgentID, GridRegion destination);

        #endregion Agents

        #region Objects

        /// <summary>
        ///     Create an object in the destination region. This message is used primarily for prim crossing.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="sog"></param>
        /// <returns></returns>
        bool CreateObject(GridRegion destination, ISceneEntity sog);

        #endregion Objects
    }

    #region Simulation Transfer Classes

    [ProtoContract(UseProtoMembersOnly = true)]
    public class CreateAgentRequest : IDataTransferable
    {
        [ProtoMember(1)]
        public GridRegion Destination;
        [ProtoMember(2)]
        public uint TeleportFlags;
        [ProtoMember(3)]
        public AgentCircuitData CircuitData;

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["Method"] = "CreateAgentRequest";
            map["Destination"] = Destination.ToOSD();
            map["TeleportFlags"] = TeleportFlags;
            map["CircuitData"] = CircuitData.ToOSD();
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            Destination = new GridRegion();
            Destination.FromOSD((OSDMap)map["Destination"]);
            TeleportFlags = map["TeleportFlags"];
            CircuitData = new AgentCircuitData();
            CircuitData.FromOSD((OSDMap)map["CircuitData"]);
        }
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class CreateAgentResponse : IDataTransferable
    {
        [ProtoMember(1)]
        public bool Success;
        [ProtoMember(2)]
        public string Reason;
        [ProtoMember(3)]
        public int RequestedUDPPort;
        [ProtoMember(4)]
        public OSDMap CapsURIs;
        [ProtoMember(5)]
        public string OurIPForClient;

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["Method"] = "CreateAgentResponse";
            map["Success"] = Success;
            map["Reason"] = Reason;
            map["RequestedUDPPort"] = RequestedUDPPort;
            map["CapsURIs"] = CapsURIs ?? new OSDMap();
            map["OurIPForClient"] = OurIPForClient;
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            Success = map["Success"];
            Reason = map["Reason"];
            RequestedUDPPort = map["RequestedUDPPort"];
            if(map.ContainsKey("CapsURIs"))
                CapsURIs = (OSDMap)map["CapsURIs"];
            OurIPForClient = map["OurIPForClient"];
        }
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class UpdateAgentPositionRequest : IDataTransferable
    {
        [ProtoMember(1)]
        public GridRegion Destination;
        [ProtoMember(2)]
        public AgentPosition Update;

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["Method"] = "UpdateAgentPositionRequest";
            map["Destination"] = Destination.ToOSD();
            map["Update"] = Update.ToOSD();
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            Destination = new GridRegion();
            Destination.FromOSD((OSDMap)map["Destination"]);
            Update = new AgentPosition();
            Update.FromOSD((OSDMap)map["Update"]);
        }
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class UpdateAgentDataRequest : IDataTransferable
    {
        [ProtoMember(1)]
        public GridRegion Destination;
        [ProtoMember(2)]
        public AgentData Update;

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["Method"] = "UpdateAgentDataRequest";
            map["Destination"] = Destination.ToOSD();
            map["Update"] = Update.ToOSD();
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            Destination = new GridRegion();
            Destination.FromOSD((OSDMap)map["Destination"]);
            Update = new AgentData();
            Update.FromOSD((OSDMap)map["Update"]);
        }
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class FailedToMoveAgentIntoNewRegionRequest : IDataTransferable
    {
        [ProtoMember(1)]
        public UUID AgentID;
        [ProtoMember(2)]
        public UUID RegionID;

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["Method"] = "FailedToMoveAgentIntoNewRegionRequest";
            map["AgentID"] = AgentID;
            map["RegionID"] = RegionID;
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            AgentID = map["AgentID"];
            RegionID = map["RegionID"];
        }
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class CloseAgentRequest : IDataTransferable
    {
        [ProtoMember(1)]
        public UUID AgentID;
        [ProtoMember(2)]
        public GridRegion Destination;

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["Method"] = "CloseAgentRequest";
            map["AgentID"] = AgentID;
            map["Destination"] = Destination.ToOSD();
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            AgentID = map["AgentID"];
            Destination = new GridRegion();
            Destination.FromOSD((OSDMap)map["Destination"]);
        }
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class MakeChildAgentRequest : IDataTransferable
    {
        [ProtoMember(1)]
        public UUID AgentID;
        [ProtoMember(2)]
        public GridRegion Destination;
        [ProtoMember(3)]
        public GridRegion OldRegion;
        [ProtoMember(4)]
        public bool IsCrossing;

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["Method"] = "MakeChildAgentRequest";
            map["AgentID"] = AgentID;
            map["Destination"] = Destination.ToOSD();
            map["OldRegion"] = OldRegion.ToOSD();
            map["IsCrossing"] = IsCrossing;
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            AgentID = map["AgentID"];
            Destination = new GridRegion();
            Destination.FromOSD((OSDMap)map["Destination"]);
            OldRegion = new GridRegion();
            OldRegion.FromOSD((OSDMap)map["OldRegion"]);
            IsCrossing = map["IsCrossing"];
        }
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class FailedToTeleportAgentRequest : IDataTransferable
    {
        [ProtoMember(1)]
        public UUID AgentID;
        [ProtoMember(2)]
        public GridRegion Destination;
        [ProtoMember(3)]
        public bool IsCrossing;
        [ProtoMember(4)]
        public string Reason;
        [ProtoMember(5)]
        public UUID FailedRegionID;

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["Method"] = "FailedToTeleportAgentRequest";
            map["AgentID"] = AgentID;
            map["Destination"] = Destination.ToOSD();
            map["IsCrossing"] = IsCrossing;
            map["Reason"] = Reason;
            map["FailedRegionID"] = FailedRegionID;
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            AgentID = map["AgentID"];
            Destination = new GridRegion();
            Destination.FromOSD((OSDMap)map["Destination"]);
            IsCrossing = map["IsCrossing"];
            Reason = map["Reason"];
            FailedRegionID = map["FailedRegionID"];
        }
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class RetrieveAgentRequest : IDataTransferable
    {
        [ProtoMember(1)]
        public UUID AgentID;
        [ProtoMember(2)]
        public GridRegion Destination;
        [ProtoMember(3)]
        public bool AgentIsLeaving;

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["Method"] = "RetrieveAgentRequest";
            map["AgentID"] = AgentID;
            map["Destination"] = Destination.ToOSD();
            map["AgentIsLeaving"] = AgentIsLeaving;
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            AgentID = map["AgentID"];
            Destination = new GridRegion();
            Destination.FromOSD((OSDMap)map["Destination"]);
            AgentIsLeaving = map["AgentIsLeaving"];
        }
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class RetrieveAgentResponse : IDataTransferable
    {
        [ProtoMember(1)]
        public bool Success;
        [ProtoMember(2)]
        public AgentData AgentData;
        [ProtoMember(3)]
        public AgentCircuitData CircuitData;

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["Method"] = "RetrieveAgentResponse";
            map["Success"] = Success;
            if (AgentData != null)
                map["AgentData"] = AgentData.ToOSD();
            if (CircuitData != null)
                map["CircuitData"] = CircuitData.ToOSD();
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            Success = map["Success"];
            if (map.ContainsKey("AgentData"))
            {
                AgentData = new AgentData();
                AgentData.FromOSD((OSDMap)map["AgentData"]);
            }
            if (map.ContainsKey("CircuitData"))
            {
                CircuitData = new AgentCircuitData();
                CircuitData.FromOSD((OSDMap)map["CircuitData"]);
            }
        }
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class CreateObjectRequest : IDataTransferable
    {
        [ProtoMember(1)]
        public ISceneEntity Object;
        [ProtoMember(3)]
        public GridRegion Destination;
        private byte[] ObjectBlob = null;

        public IScene Scene;

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["Method"] = "CreateObjectRequest";
            map["Destination"] = Destination.ToOSD();
            map["Object"] = Object.ToBinaryXml2();
            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            Destination = new GridRegion();
            Destination.FromOSD((OSDMap)map["Destination"]);
            ObjectBlob = map["Object"].AsBinary();
        }

        public void DeserializeObject()
        {
            if (ObjectBlob == null || Scene == null)
                return;

            System.IO.MemoryStream stream = new System.IO.MemoryStream(ObjectBlob);
            Aurora.Framework.Serialization.SceneEntitySerializer.SceneObjectSerializer.FromXml2Format(ref stream, Scene);
            stream.Close();
            ObjectBlob = null;
        }
    }

    #endregion
}