/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using OpenMetaverse;
using log4net;
using OpenSim.Framework;
using OpenSim.Framework.Client;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes.Types;

namespace OpenSim.Region.Framework.Scenes
{
    public class SceneViewer : ISceneViewer
    {
        protected ScenePresence m_presence;
        protected UpdateQueue m_partsUpdateQueue = new UpdateQueue();
        protected bool m_SentInitialObjects = false;
        protected volatile List<UUID> m_objectsInView = new List<UUID>();

        protected Dictionary<UUID, ScenePartUpdate> m_updateTimes = new Dictionary<UUID, ScenePartUpdate>();

        public SceneViewer(ScenePresence presence)
        {
            m_presence = presence;
            if(presence.Scene.CheckForObjectCulling) //Only do culling checks if enabled
                presence.Scene.EventManager.OnSignificantClientMovement += SignificantClientMovement;
        }

        /// <summary>
        /// Add the part to the queue of parts for which we need to send an update to the client
        /// </summary>
        /// <param name="part"></param>
        public void QueuePartForUpdate(SceneObjectPart part, PrimUpdateFlags UpdateFlags)
        {
            lock (m_partsUpdateQueue)
            {
                PrimUpdate update = new PrimUpdate();
                update.Part = part;
                update.UpdateFlags = UpdateFlags;
                m_partsUpdateQueue.Enqueue(update);

                //Make it check when the user comes around to it again
                if (m_objectsInView.Contains(part.UUID))
                    m_objectsInView.Remove(part.UUID);
            }
        }

        private void SignificantClientMovement(IClientAPI remote_client)
        {
            if (!m_presence.Scene.CheckForObjectCulling)
                return;

            if (remote_client.AgentId != m_presence.UUID)
                return;

            if(m_presence.DrawDistance == 0)
                return;

            //This checks to see if we need to send more updates to the avatar since they last moved
            EntityBase[] Entities = m_presence.Scene.Entities.GetEntities();

            foreach (EntityBase entity in Entities)
            {
                if (entity is SceneObjectGroup) //Only objects
                {
                    //Check to see if they are in range
                    if (Util.DistanceLessThan(m_presence.CameraPosition, entity.AbsolutePosition, m_presence.DrawDistance))
                    {
                        //Check if we already have sent them an update
                        if (!m_objectsInView.Contains(entity.UUID))
                        {
                            //Update the list
                            m_objectsInView.Add(entity.UUID);
                            //Send the update
                            SendFullUpdateToClient(PrimUpdateFlags.FullUpdate, (SceneObjectGroup)entity); //New object, send full
                        }
                    }
                }
            }
            Entities = null;
        }

        public void SendPrimUpdates()
        {
            #region New client entering the Scene, requires all objects in the Scene

            ///If we havn't started processing this client yet, we need to send them ALL the prims that we have in this Scene (and deal with culling as well...)
            if (!m_SentInitialObjects)
            {
                //If they are not in this region, we check to make sure that we allow seeing into neighbors
                if (!m_presence.IsChildAgent || (m_presence.Scene.RegionInfo.SeeIntoThisSimFromNeighbor))
                {
                    EntityBase[] entities = m_presence.Scene.Entities.GetEntities();

                    foreach (EntityBase e in entities)
                    {
                        if (e != null && e is SceneObjectGroup)
                        {
                            SceneObjectGroup grp = (SceneObjectGroup)e;

                            //Check for culling here!
                            if (!CheckForCulling(grp))
                                continue;

                            // Don't even queue if we have sent this one
                            //
                            if (!m_updateTimes.ContainsKey(grp.UUID))
                                SendFullUpdateToClient(PrimUpdateFlags.FullUpdate, grp); //New object, send full
                        }
                    }
                    entities = null;
                }
                //Do this HERE so that all those updates added are prioritized correctly.
                m_presence.ControllingClient.ReprioritizeUpdates();
            }

            #endregion

            #region stuff

            while (m_partsUpdateQueue.Count > 0)
            {
                PrimUpdate update = m_partsUpdateQueue.Dequeue();

                if (update == null)
                    continue;

                if (update.Part.ParentGroup == null || update.Part.ParentGroup.IsDeleted)
                    continue;

                //Check for culling here!
                if (!CheckForCulling(update.Part.ParentGroup))
                    continue;

                if (m_updateTimes.ContainsKey(update.Part.UUID))
                {
                    ScenePartUpdate partupdate = m_updateTimes[update.Part.UUID];

                    // We deal with the possibility that two updates occur at
                    // the same unix time at the update point itself.

                    //if ((partupdate.LastFullUpdateTime < update.Part.TimeStampFull) ||
                    //        update.Part.IsAttachment)
                    //{
                        //m_log.DebugFormat(
                        //  "[SCENE PRESENCE]: Fully   updating prim {0}, {1} - part timestamp {2}",
                        //  part.Name, part.UUID, part.TimeStampFull);

                        SendFullUpdate(update.Part,
                               m_presence.GenerateClientFlags(update.Part), update.UpdateFlags);

                        // We'll update to the part's timestamp rather than
                        // the current time to avoid the race condition
                        // whereby the next tick occurs while we are doing
                        // this update. If this happened, then subsequent
                        // updates which occurred on the same tick or the
                        // next tick of the last update would be ignored.

                        //partupdate.LastFullUpdateTime = update.Part.TimeStampFull;

                    //}
                    //else if (partupdate.LastTerseUpdateTime <= update.Part.TimeStampTerse)
                    //{
                        //m_log.DebugFormat(
                        //  "[SCENE PRESENCE]: Tersely updating prim {0}, {1} - part timestamp {2}",
                        //  part.Name, part.UUID, part.TimeStampTerse);

                        SendTerseUpdateToClient(m_presence.ControllingClient, update.UpdateFlags, update.Part);

                    //    partupdate.LastTerseUpdateTime = update.Part.TimeStampTerse;
                    //}
                }
                else
                {
                    //never been sent to client before so do full update
                    ScenePartUpdate partupdate = new ScenePartUpdate();
                    partupdate.FullID = update.Part.UUID;
                    //partupdate.LastFullUpdateTime = update.Part.TimeStampFull;
                    m_updateTimes.Add(update.Part.UUID, partupdate);

                    // Attachment handling
                    //
                    if (update.Part.ParentGroup.RootPart.Shape.PCode == 9 && update.Part.ParentGroup.RootPart.Shape.State != 0)
                    {
                        if (update.Part != update.Part.ParentGroup.RootPart)
                            continue;

                        SendFullUpdateToClient(update.UpdateFlags, update.Part.ParentGroup);
                        continue;
                    }

                    SendFullUpdate(update.Part,
                            m_presence.GenerateClientFlags(update.Part), update.UpdateFlags);
                }
            }

            #endregion
        }

        public void SendTerseUpdateToClient(IClientAPI remoteClient, PrimUpdateFlags UpdateFlags, SceneObjectPart part)
        {
            if (part.ParentGroup == null || part.ParentGroup.IsDeleted)
                return;

            if (part.IsAttachment && part.ParentGroup.RootPart != part)
                return;
            
            remoteClient.SendPrimUpdate(part, UpdateFlags);
        }

        /// <summary>
        /// Send a full update to the client for the given part
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="clientFlags"></param>
        protected internal void SendFullUpdate(SceneObjectPart part, uint clientFlags, PrimUpdateFlags changedFlags)
        {
            //            m_log.DebugFormat(
            //                "[SOG]: Sending part full update to {0} for {1} {2}", remoteClient.Name, part.Name, part.LocalId);

            if (part.IsRoot)
            {
                if (part.IsAttachment)
                {
                    SendFullUpdateToClient(part, part.AttachedPos, clientFlags, changedFlags);
                }
                else
                {
                    SendFullUpdateToClient(part, part.AbsolutePosition, clientFlags, changedFlags);
                }
            }
            else
            {
                SendFullUpdateToClient(part, part.OffsetPosition, clientFlags, changedFlags);
            }
        }

        /// <summary>
        /// Sends a full update to the client
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="lPos"></param>
        /// <param name="clientFlags"></param>
        public void SendFullUpdateToClient(SceneObjectPart part, Vector3 lPos, uint clientFlags, PrimUpdateFlags changedFlags)
        {
            // Suppress full updates during attachment editing
            //
            if (part.ParentGroup.IsSelected && part.IsAttachment)
                return;

            if (part.ParentGroup.IsDeleted)
                return;

            clientFlags &= ~(uint)PrimFlags.CreateSelected;

            if (m_presence.UUID == part.OwnerID)
            {
                if ((part.Flags & PrimFlags.CreateSelected) != 0)
                {
                    clientFlags |= (uint)PrimFlags.CreateSelected;
                    part.Flags &= ~PrimFlags.CreateSelected;
                }
            }
            //bool isattachment = IsAttachment;
            //if (LocalId != ParentGroup.RootPart.LocalId)
            //isattachment = ParentGroup.RootPart.IsAttachment;

            m_presence.ControllingClient.SendPrimUpdate(part, changedFlags);
        }

        public void SendFullUpdateToClient(PrimUpdateFlags UpdateFlags, SceneObjectGroup grp)
        {
            SendFullUpdate(
                grp.RootPart, m_presence.Scene.Permissions.GenerateClientFlags(m_presence.UUID, grp.RootPart), UpdateFlags);

            lock (grp.ChildrenList)
            {
                foreach (SceneObjectPart part in grp.ChildrenList)
                {
                    if (part != grp.RootPart)
                        SendFullUpdate(
                            part, m_presence.Scene.Permissions.GenerateClientFlags(m_presence.UUID, part), UpdateFlags);
                }
            }
        }

        /// <summary>
        /// Checks to see whether the object should be sent to the client
        /// Returns true if the client should be able to see the object, false if not
        /// </summary>
        /// <param name="grp"></param>
        /// <returns></returns>
        private bool CheckForCulling(SceneObjectGroup grp)
        {
            if (m_presence.Scene.CheckForObjectCulling)
            {
                //Check for part position against the av and the camera position
                if ((!Util.DistanceLessThan(m_presence.AbsolutePosition, grp.AbsolutePosition, m_presence.DrawDistance) &&
                    !Util.DistanceLessThan(m_presence.CameraPosition, grp.AbsolutePosition, m_presence.DrawDistance)))
                    if (m_presence.DrawDistance != 0)
                        return false;
            }
            return true;
        }

        /// <summary>
        /// Reset all lists that have to deal with what updates the viewer has
        /// </summary>
        public void Reset()
        {
            m_SentInitialObjects = false;
            m_objectsInView.Clear();
        }

        public void Close()
        {
            lock (m_updateTimes)
            {
                m_updateTimes.Clear();
            }
            lock (m_partsUpdateQueue)
            {
                m_partsUpdateQueue.Clear();
            }
            Reset();
        }

        public class ScenePartUpdate
        {
            public UUID FullID;
            public uint LastFullUpdateTime;
            public uint LastTerseUpdateTime;

            public ScenePartUpdate()
            {
                FullID = UUID.Zero;
                LastFullUpdateTime = 0;
                LastTerseUpdateTime = 0;
            }
        }
    }
}
