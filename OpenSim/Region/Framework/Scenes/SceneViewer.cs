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
using System.Text;
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
        #region Declares

        protected ScenePresence m_presence;
        protected UpdateQueue m_partsUpdateQueue = new UpdateQueue();
        protected List<UUID> m_removeNextUpdateOf = new List<UUID>();
        protected bool m_SentInitialObjects = false;
        protected volatile bool m_inUse = false;
        protected volatile List<UUID> m_objectsInView = new List<UUID>();
        protected Prioritizer m_prioritizer;

        #endregion

        #region Constructor

        public SceneViewer(ScenePresence presence)
        {
            m_presence = presence;
            if(presence.Scene.CheckForObjectCulling) //Only do culling checks if enabled
                presence.Scene.EventManager.OnSignificantClientMovement += SignificantClientMovement;
            m_prioritizer = new Prioritizer(presence.Scene);
        }

        #endregion

        #region Enqueue/Remove updates for objects

        /// <summary>
        /// Add the objects to the queue for which we need to send an update to the client
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

        /// <summary>
        /// Clear the updates for this part in the next update loop
        /// </summary>
        /// <param name="part"></param>
        public void ClearUpdatesForPart(SceneObjectPart part)
        {
            lock (m_removeNextUpdateOf)
            {
                //Add it to the list to check and make sure that we do not send updates for this object
                m_removeNextUpdateOf.Add(part.UUID);
                //Make it check when the user comes around to it again
                if (m_objectsInView.Contains(part.UUID))
                    m_objectsInView.Remove(part.UUID);
            }
        }

        #endregion

        #region Object Culling by draw distance

        private void SignificantClientMovement(IClientAPI remote_client)
        {
            if (!m_presence.Scene.CheckForObjectCulling)
                return;

            //Only check our presence
            if (remote_client.AgentId != m_presence.UUID)
                return;

            //If the draw distance is 0, the client has gotten messed up or something and we can't do this...
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
                            SendUpdate(PrimUpdateFlags.FullUpdate, (SceneObjectGroup)entity); //New object, send full
                        }
                    }
                }
            }
            Entities = null;
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

        #endregion

        #region SendPrimUpdates

        /// <summary>
        /// This loops through all of the lists that we have for the client
        ///  as well as checking whether the client has ever entered the sim before
        ///  and sending the needed updates to them if they have just entered.
        /// </summary>
        public void SendPrimUpdates()
        {
            if (m_inUse)
                return;
            m_inUse = true;
            //This is for stats
            int AgentMS = Util.EnvironmentTickCount();

            #region New client entering the Scene, requires all objects in the Scene

            ///If we havn't started processing this client yet, we need to send them ALL the prims that we have in this Scene (and deal with culling as well...)
            if (!m_SentInitialObjects)
            {
                m_SentInitialObjects = true;
                //If they are not in this region, we check to make sure that we allow seeing into neighbors
                if (!m_presence.IsChildAgent || (m_presence.Scene.RegionInfo.SeeIntoThisSimFromNeighbor))
                {
                    EntityBase[] entities = m_presence.Scene.Entities.GetEntities();
                    //Use the PriorityQueue so that we can send them in the correct order
                    PriorityQueue entityUpdates = new PriorityQueue(entities.Length);

                    foreach (EntityBase e in entities)
                    {
                        if (e != null && e is SceneObjectGroup)
                        {
                            SceneObjectGroup grp = (SceneObjectGroup)e;

                            //Check for culling here!
                            if (!CheckForCulling(grp))
                                continue;

                            //Get the correct priority and add to the queue
                            double priority = m_prioritizer.GetUpdatePriority(m_presence.ControllingClient, grp);
                            entityUpdates.Enqueue(priority, new EntityUpdate(grp, PrimUpdateFlags.FullUpdate), grp.LocalId); //New object, send full
                        }
                    }
                    entities = null;
                    //Send all the updates to the client
                    EntityUpdate update;
                    while (entityUpdates.TryDequeue(out update))
                    {
                        SendUpdate(PrimUpdateFlags.FullUpdate, (SceneObjectGroup)update.Entity);
                    }
                }
            }

            #endregion

            #region Update loop that sends objects that have been recently added to the queue

            //Pull the parts out into a list first so that we don't lock the queue for too long
            List<PrimUpdate> updates = new List<PrimUpdate>();
            lock (m_partsUpdateQueue)
            {
                lock (m_removeNextUpdateOf)
                {
                    while (m_partsUpdateQueue.Count > 0)
                    {
                        PrimUpdate update = m_partsUpdateQueue.Dequeue();

                        if (update == null)
                            continue;
                        
                        //Make sure not to send deleted or null objects
                        if (update.Part.ParentGroup == null || update.Part.ParentGroup.IsDeleted)
                            continue;

                        //Make sure we are not supposed to remove it
                        if (m_removeNextUpdateOf.Contains(update.Part.UUID))
                            continue;

                        updates.Add(update);
                    }
                    //Clear this now that we are done with the batch of updates
                    m_removeNextUpdateOf.Clear();
                }
            }

            //Now loop through the list and send the updates
            foreach (PrimUpdate update in updates)
            {
                //Check for culling here!
                if (!CheckForCulling(update.Part.ParentGroup))
                    continue;

                // Attachment handling. Attachments are 'special' and we have to send the full group update when we send updates
                if (update.Part.ParentGroup.RootPart.Shape.PCode == 9 && update.Part.ParentGroup.RootPart.Shape.State != 0)
                {
                    if (update.Part != update.Part.ParentGroup.RootPart)
                        continue;

                    SendUpdate(update.UpdateFlags, update.Part.ParentGroup);
                    continue;
                }

                SendUpdate(update.Part,
                        m_presence.GenerateClientFlags(update.Part), update.UpdateFlags);
            }

            #endregion

            //Add the time to the stats tracker
            IAgentUpdateMonitor reporter = (IAgentUpdateMonitor)m_presence.Scene.RequestModuleInterface<IMonitorModule>().GetMonitor(m_presence.Scene.RegionInfo.RegionID.ToString(), "Agent Update Count");
            if (reporter != null)
                reporter.AddAgentTime(Util.EnvironmentTickCountSubtract(AgentMS));

            m_inUse = false;
        }

        #endregion

        #region Send the packets to the client handler

        /// <summary>
        /// Send a full update to the client for the given part
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="clientFlags"></param>
        protected internal void SendUpdate(SceneObjectPart part, uint clientFlags, PrimUpdateFlags changedFlags)
        {
            Vector3 lPos;
            if (part.IsRoot)
            {
                if (part.IsAttachment)
                {
                    lPos = part.AttachedPos;
                }
                else
                {
                    lPos = part.AbsolutePosition;
                }
            }
            else
            {
                lPos = part.OffsetPosition;
            }

            // Suppress full updates during attachment editing
            if (part.ParentGroup.IsSelected && part.IsAttachment)
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
            m_presence.ControllingClient.SendPrimUpdate(part, changedFlags);
        }

        /// <summary>
        /// This sends updates for all the prims in the group
        /// </summary>
        /// <param name="UpdateFlags"></param>
        /// <param name="grp"></param>
        protected internal void SendUpdate(PrimUpdateFlags UpdateFlags, SceneObjectGroup grp)
        {
            SendUpdate(
                grp.RootPart, m_presence.Scene.Permissions.GenerateClientFlags(m_presence.UUID, grp.RootPart), UpdateFlags);

            lock (grp.ChildrenList)
            {
                foreach (SceneObjectPart part in grp.ChildrenList)
                {
                    if (part != grp.RootPart)
                        SendUpdate(
                            part, m_presence.Scene.Permissions.GenerateClientFlags(m_presence.UUID, part), UpdateFlags);
                }
            }
        }

        #endregion

        #region Reset and Close

        /// <summary>
        /// Reset all lists that have to deal with what updates the viewer has
        /// </summary>
        public void Reset()
        {
            m_SentInitialObjects = false;
            m_objectsInView.Clear();
        }

        /// <summary>
        /// Destroy all lists, prepare to close the 
        /// </summary>
        public void Close()
        {
            lock (m_partsUpdateQueue)
            {
                m_partsUpdateQueue.Clear();
            }
            Reset();
        }

        #endregion
    }
}
