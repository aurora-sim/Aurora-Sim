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

using Aurora.Framework;
using System;
using System.Collections.Generic;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.Interfaces
{
    public class CachedUserInfo : IDataTransferable
    {
        public IAgentInfo AgentInfo;
        public UserAccount UserAccount;
        public GroupMembershipData ActiveGroup;
        public List<GroupMembershipData> GroupMemberships = new List<GroupMembershipData>();

        public override void FromOSD(OSDMap map)
        {
            AgentInfo = new IAgentInfo();
            AgentInfo.FromOSD((OSDMap)(map["AgentInfo"]));
            UserAccount = new UserAccount();
            UserAccount.FromOSD((OSDMap)(map["UserAccount"]));
            if (!map.ContainsKey("ActiveGroup"))
                ActiveGroup = null;
            else
            {
                ActiveGroup = new GroupMembershipData();
                ActiveGroup.FromOSD((OSDMap)(map["ActiveGroup"]));
            }
            GroupMemberships = ((OSDArray)map["GroupMemberships"]).ConvertAll<GroupMembershipData>((o) => 
                {
                    GroupMembershipData group = new GroupMembershipData();
                    group.FromOSD((OSDMap)o);
                    return group;
                });
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["AgentInfo"] = AgentInfo.ToOSD();
            map["UserAccount"] = UserAccount.ToOSD();
            if(ActiveGroup != null)
                map["ActiveGroup"] = ActiveGroup.ToOSD();
            map["GroupMemberships"] = GroupMemberships.ToOSDArray();
            return map;
        }
    }

    public interface ISimulationService
    {
        #region Local Initalization

        /// <summary>
        ///   Add and set up the scene for the simulation module
        /// </summary>
        /// <param name = "scene"></param>
        void Init(IScene scene);

        /// <summary>
        ///   Remove the scene from the list of known scenes
        /// </summary>
        /// <param name = "scene"></param>
        void RemoveScene(IScene scene);

        /// <summary>
        ///   Try to find a scene with the given region handle
        /// </summary>
        /// <param name = "regionHandle"></param>
        /// <returns></returns>
        IScene GetScene(ulong regionHandle);

        /// <summary>
        ///   Get the 'inner' simulation service.
        ///   For the remote simulation service, this gives the local simulation service.
        ///   For the local simulation service. this gives the same service as it is already the local service.
        /// </summary>
        /// <returns></returns>
        ISimulationService GetInnerService();

        #endregion

        #region Agents

        /// <summary>
        ///   Create an agent at the given destination
        ///   Grid Server only.
        /// </summary>
        /// <param name = "destination"></param>
        /// <param name = "aCircuit"></param>
        /// <param name = "teleportFlags"></param>
        /// <param name = "data"></param>
        /// <param name = "reason"></param>
        /// <returns></returns>
        bool CreateAgent(GridRegion destination, AgentCircuitData aCircuit, uint teleportFlags, AgentData data,
                         out int requestedUDPPort, out string reason);

        /// <summary>
        ///   Full child agent update.
        ///   Grid Server only.
        /// </summary>
        /// <param name = "regionHandle"></param>
        /// <param name = "data"></param>
        /// <returns></returns>
        bool UpdateAgent(GridRegion destination, AgentData data);

        /// <summary>
        ///   Short child agent update, mostly for position.
        ///   Grid Server only.
        /// </summary>
        /// <param name = "regionHandle"></param>
        /// <param name = "data"></param>
        /// <returns></returns>
        bool UpdateAgent(GridRegion destination, AgentPosition data);

        /// <summary>
        ///   Pull the root agent info from the given destination
        ///   Grid Server only.
        /// </summary>
        /// <param name = "destination"></param>
        /// <param name = "id"></param>
        /// <param name = "agent"></param>
        /// <param name = "circuitData"></param>
        /// <returns></returns>
        bool RetrieveAgent(GridRegion destination, UUID id, bool agentIsLeaving, out AgentData agent,
                           out AgentCircuitData circuitData);

        /// <summary>
        ///   Close agent.
        ///   Grid Server only.
        /// </summary>
        /// <param name = "regionHandle"></param>
        /// <param name = "id"></param>
        /// <returns></returns>
        bool CloseAgent(GridRegion destination, UUID id);

        /// <summary>
        ///   Makes a root agent into a child agent in the given region
        ///   DOES mark the agent as leaving (removes attachments among other things)
        /// </summary>
        /// <param name = "AgentID"></param>
        /// <param name = "Region"></param>
        bool MakeChildAgent(UUID AgentID, GridRegion Region);

        /// <summary>
        ///   Tells the region that the agent was not able to leave the region and needs to be resumed
        /// </summary>
        /// <param name = "AgentID"></param>
        /// <param name = "RegionID"></param>
        bool FailedToMoveAgentIntoNewRegion(UUID AgentID, UUID RegionID);

        #endregion Agents

        #region Objects

        /// <summary>
        ///   Create an object in the destination region. This message is used primarily for prim crossing.
        /// </summary>
        /// <param name = "regionHandle"></param>
        /// <param name = "sog"></param>
        /// <returns></returns>
        bool CreateObject(GridRegion destination, ISceneObject sog);

        /// <summary>
        ///   Create an object from the user's inventory in the destination region. 
        ///   This message is used primarily by clients for attachments.
        /// </summary>
        /// <param name = "regionHandle"></param>
        /// <param name = "userID"></param>
        /// <param name = "itemID"></param>
        /// <returns></returns>
        bool CreateObject(GridRegion destination, UUID userID, UUID itemID);

        #endregion Objects
    }
}