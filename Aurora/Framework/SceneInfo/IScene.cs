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
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Interfaces;

namespace Aurora.Framework
{
    /// <value>
    ///   Indicate what action to take on an object derez request
    /// </value>
    public enum DeRezAction : byte
    {
        SaveToExistingUserInventoryItem = 0,

        /// <summary>
        /// try to leave copy in world
        /// </summary>
        AcquireToUserInventory = 1,
        SaveIntoTaskInventory = 2,
        Attachment = 3,
        Take = 4,

        /// <summary>
        /// force take copy
        /// </summary>
        GodTakeCopy = 5,
        Delete = 6,
        AttachmentToInventory = 7,
        AttachmentExists = 8,

        /// <summary>
        /// back to owner's inventory
        /// </summary>
        Return = 9,

        /// <summary>
        /// deeded object back to last owner's inventory
        /// </summary>
        ReturnToLastOwner = 10
    };

    public interface IScene : IRegistryCore
    {
        #region Core

        RegionInfo RegionInfo { get; set; }
        AuroraEventManager AuroraEventManager { get; }
        EntityManager Entities { get; }
        EventManager EventManager { get; }
        ScenePermissions Permissions { get; }
        PhysicsScene PhysicsScene { get; set; }
        ISceneGraph SceneGraph { get; }
        AgentCircuitManager AuthenticateHandler { get; }
        IConfigSource Config { get; set; }
        ISimulationDataStore SimulationDataService { get; }

        #endregion

        #region Initialize/Close

        bool ShouldRunHeartbeat { get; set; }
        void Initialize(RegionInfo regionInfo);
        void Initialize(RegionInfo regionInfo, AgentCircuitManager authen, List<IClientNetworkServer> clientServers);
        void StartHeartbeat();
        void FinishedStartup(string p, List<string> list);
        void Close();

        #endregion

        #region Physics methods

        /// <summary>
        ///   Reload the last saved physics state to the Physics Scene
        /// </summary>
        void StartPhysicsScene();

        /// <summary>
        ///   Takes a state save of the Physics Scene, then clears all velocity from it so that objects stop moving
        /// </summary>
        void StopPhysicsScene();

        #endregion

        #region Client Methods

        ClientManager ClientManager { get; }
        void AddNewClient(IClientAPI client, BlankHandler completed);
        IScenePresence GetScenePresence(UUID uUID);
        List<IScenePresence> GetScenePresences();
        int GetScenePresenceCount();
        IScenePresence GetScenePresence(uint localID);
        bool TryGetScenePresence(UUID agentID, out IScenePresence scenePresence);
        bool TryGetAvatarByName(string p, out IScenePresence NewSP);
        bool RemoveAgent(IScenePresence presence, bool forceClose);

        #endregion

        #region ForEach

        void ForEachClient(Action<IClientAPI> action);
        void ForEachScenePresence(Action<IScenePresence> action);
        void ForEachSceneEntity(Action<ISceneEntity> action);

        #endregion

        #region Parts

        ISceneChildEntity GetSceneObjectPart(uint localID);
        ISceneChildEntity GetSceneObjectPart(UUID objectID);
        ISceneEntity GetGroupByPrim(uint objectLocalID);
        bool TryGetPart(UUID objecUUID, out ISceneChildEntity SensedObject);

        #endregion

        #region FPS/stats

        float BaseSimFPS { get; }
        float BaseSimPhysFPS { get; }

        bool ShuttingDown { get; }
        object SyncRoot { get; }
        float TimeDilation { get; set; }
        uint Frame { get; }

        #endregion

        #region Services

        IAssetService AssetService { get; }
        IAuthenticationService AuthenticationService { get; }
        IAvatarService AvatarService { get; }
        IGridService GridService { get; }
        IInventoryService InventoryService { get; }
        ISimulationService SimulationService { get; }
        IUserAccountService UserAccountService { get; }

        #endregion

        #region Other

        List<ISceneEntity> PhysicsReturns { get; }

        #endregion
    }
}