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

using System.Collections.Generic;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.SceneInfo.Entities;
using OpenMetaverse;

namespace Aurora.Framework.PresenceInfo
{
    public interface ISceneViewer
    {
        /// <summary>
        ///     The instance of the prioritizer the SceneViewer uses
        /// </summary>
        IPrioritizer Prioritizer { get; }

        /// <summary>
        ///     The instance of the culler the SceneViewer uses
        /// </summary>
        ICuller Culler { get; }

        /// <summary>
        ///     Sends all the information about the client to this avatar
        /// </summary>
        /// <param name="presence"></param>
        /// <param name="forced"></param>
        void QueuePresenceForFullUpdate(IScenePresence presence, bool forced);

        /// <summary>
        ///     Send the full presence update immediately
        /// </summary>
        /// <param name="presence"></param>
        void SendPresenceFullUpdate(IScenePresence presence);

        /// <summary>
        ///     Send a presence terse update to all clients
        /// </summary>
        /// <param name="presence"></param>
        /// <param name="flags"></param>
        void QueuePresenceForUpdate(IScenePresence presence, PrimUpdateFlags flags);

        /// <summary>
        ///     Send a presence terse update to all clients
        /// </summary>
        /// <param name="presence"></param>
        /// <param name="animation"></param>
        void QueuePresenceForAnimationUpdate(IScenePresence presence, AnimationGroup animation);

        /// <summary>
        ///     Add the objects to the queue for which we need to send an update to the client
        /// </summary>
        /// <param name="part"></param>
        /// <param name="UpdateFlags"></param>
        void QueuePartForUpdate(ISceneChildEntity part, PrimUpdateFlags UpdateFlags);

        /// <summary>
        ///     Add the objects to the queue for which we need to send a properties update for
        /// </summary>
        /// <param name="entities"></param>
        void QueuePartsForPropertiesUpdate(ISceneChildEntity[] entities);

        /// <summary>
        ///     This method is called by the LLUDPServer and should never be called by anyone else
        ///     It loops through the available updates and sends them out (no waiting)
        /// </summary>
        /// <param name="numPrimUpdates">The number of prim updates to send</param>
        /// <param name="numAvaUpdates"> The number of avatar updates to send</param>
        void SendPrimUpdates(int numPrimUpdates, int numAvaUpdates);

        /// <summary>
        ///     Once the packet has been sent, allow newer updates to be sent for the given entity
        /// </summary>
        /// <param name="ID"></param>
        void FinishedEntityPacketSend(IEnumerable<EntityUpdate> ID);

        /// <summary>
        ///     Once the packet has been sent, allow newer animations to be sent for the given entity
        /// </summary>
        /// <param name="update"></param>
        void FinishedAnimationPacketSend(AnimationGroup update);

        /// <summary>
        ///     Once the packet has been sent, allow newer property updates to be sent for the given entity
        /// </summary>
        /// <param name="updates"></param>
        void FinishedPropertyPacketSend(IEnumerable<IEntity> updates);

        /// <summary>
        ///     The client has left this region and went into a child region, clean up anything required
        /// </summary>
        void Reset();

        /// <summary>
        ///     Closes the SceneViewer
        /// </summary>
        void Close();

        /// <summary>
        ///     Removes an avatar from the 'in-view' list
        /// </summary>
        /// <param name="sp"></param>
        void RemoveAvatarFromView(IScenePresence sp);

        /// <summary>
        ///     Remove updates for the given avatar
        /// </summary>
        /// <param name="presence"></param>
        void ClearPresenceUpdates(IScenePresence presence);
    }

    public class AnimationGroup
    {
        public UUID[] Animations;
        public UUID AvatarID;
        public UUID[] ObjectIDs;
        public int[] SequenceNums;
    }

    public interface IPrioritizer
    {
        /// <summary>
        ///     The distance before we send a child agent update to all neighbors
        /// </summary>
        double ChildReprioritizationDistance { get; }

        /// <summary>
        ///     Gets the priority for the given client/entity
        /// </summary>
        /// <param name="client"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        double GetUpdatePriority(IScenePresence client, IEntity entity);
    }

    public interface ICuller
    {
        /// <summary>
        ///     Is culling enabled?
        /// </summary>
        bool UseCulling { get; set; }

        /// <summary>
        ///     Should the given object be should to the given client?
        /// </summary>
        /// <param name="client"></param>
        /// <param name="entity"></param>
        /// <param name="scene"></param>
        /// <returns></returns>
        bool ShowEntityToClient(IScenePresence client, IEntity entity, IScene scene);
    }

    public interface IAnimator
    {
        AnimationSet Animations { get; }
        bool NeedsAnimationResent { get; set; }
        string CurrentMovementAnimation { get; }
        string GetMovementAnimation();
        void UpdateMovementAnimations(bool sendTerseUpdate);
        void AddAnimation(UUID animID, UUID objectID);
        bool AddAnimation(string name, UUID objectID);
        void RemoveAnimation(UUID animID);
        bool RemoveAnimation(string name);
        void ResetAnimations();
        void TrySetMovementAnimation(string anim);
        UUID[] GetAnimationArray();
        void SendAnimPack(UUID[] animations, int[] seqs, UUID[] objectIDs);
        void SendAnimPackToClient(IClientAPI client);
        void SendAnimPack();
        void Close();
    }
}