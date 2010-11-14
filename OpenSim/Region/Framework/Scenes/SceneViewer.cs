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
        protected Queue<SceneObjectGroup> m_pendingObjects;
        protected volatile List<UUID> m_objectsInView = new List<UUID>();

        protected Dictionary<UUID, ScenePartUpdate> m_updateTimes = new Dictionary<UUID, ScenePartUpdate>();

        public SceneViewer(ScenePresence presence)
        {
            m_presence = presence;
            //if(presence.Scene.CheckForObjectCulling) //Only do culling checks if enabled
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

        void SignificantClientMovement(IClientAPI remote_client)
        {
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
                            if (m_pendingObjects != null)
                            {
                                //Update the list
                                m_objectsInView.Add(entity.UUID);
                                //Enqueue them for an update
                                m_pendingObjects.Enqueue((SceneObjectGroup)entity);
                            }
                        }
                    }
                }
            }
            Entities = null;
        }

        public void SendPrimUpdates()
        {
            if (m_pendingObjects == null)
            {
                if (!m_presence.IsChildAgent || (m_presence.Scene.RegionInfo.SeeIntoThisSimFromNeighbor))
                {
                    m_pendingObjects = new Queue<SceneObjectGroup>();
                    EntityBase[] entities = m_presence.Scene.Entities.GetEntities();

                    lock (m_pendingObjects)
                    {
                        foreach (EntityBase e in entities)
                        {
                            if (e != null && e is SceneObjectGroup)
                                m_pendingObjects.Enqueue((SceneObjectGroup)e);
                        }
                    }
                    entities = null;
                }
            }

            lock (m_pendingObjects)
            {
                while (m_pendingObjects != null && m_pendingObjects.Count > 0)
                {
                    SceneObjectGroup g = m_pendingObjects.Dequeue();
                    // Yes, this can really happen
                    if (g == null)
                        continue;

                    // This is where we should check for draw distance
                    // do culling and stuff. Problem with that is that until
                    // we recheck in movement, that won't work right.
                    // So it's not implemented now.
                    // - This note is from OS core, and has since been implemented as seen below

                    if (m_presence.Scene.CheckForObjectCulling)
                    {
                        //Check for part position against the av and the camera position
                        if (!Util.DistanceLessThan(m_presence.CameraPosition, g.AbsolutePosition, m_presence.DrawDistance))
                            if (m_presence.DrawDistance != 0)
                                continue;
                    }

                    // Don't even queue if we have sent this one
                    //
                    if (!m_updateTimes.ContainsKey(g.UUID))
                        g.SendFullUpdateToClient(m_presence.ControllingClient, PrimUpdateFlags.FullUpdate); //New object, send full
                }
                //Do this HERE so that all those updates added are prioritized.
                m_presence.ControllingClient.ReprioritizeUpdates();
            }

            while (m_partsUpdateQueue.Count > 0)
            {
                PrimUpdate update = m_partsUpdateQueue.Dequeue();

                if (update == null)
                    continue;

                if (update.Part.ParentGroup == null || update.Part.ParentGroup.IsDeleted)
                    continue;

                if (m_presence.Scene.CheckForObjectCulling)
                {
                    //Check for part position against the av and the camera position
                    if ((!Util.DistanceLessThan(m_presence.AbsolutePosition, update.Part.AbsolutePosition, m_presence.DrawDistance) &&
                        !Util.DistanceLessThan(m_presence.CameraPosition, update.Part.AbsolutePosition, m_presence.DrawDistance)))
                        if (m_presence.DrawDistance != 0)
                            continue;
                }

                if (m_updateTimes.ContainsKey(update.Part.UUID))
                {
                    ScenePartUpdate partupdate = m_updateTimes[update.Part.UUID];

                    // We deal with the possibility that two updates occur at
                    // the same unix time at the update point itself.

                    if ((partupdate.LastFullUpdateTime < update.Part.TimeStampFull) ||
                            update.Part.IsAttachment)
                    {
                        //                            m_log.DebugFormat(
                        //                                "[SCENE PRESENCE]: Fully   updating prim {0}, {1} - part timestamp {2}",
                        //                                part.Name, part.UUID, part.TimeStampFull);

                        update.Part.SendFullUpdate(m_presence.ControllingClient,
                               m_presence.GenerateClientFlags(update.Part), update.UpdateFlags);

                        // We'll update to the part's timestamp rather than
                        // the current time to avoid the race condition
                        // whereby the next tick occurs while we are doing
                        // this update. If this happened, then subsequent
                        // updates which occurred on the same tick or the
                        // next tick of the last update would be ignored.

                        partupdate.LastFullUpdateTime = update.Part.TimeStampFull;

                    }
                    else if (partupdate.LastTerseUpdateTime <= update.Part.TimeStampTerse)
                    {
                        //                            m_log.DebugFormat(
                        //                                "[SCENE PRESENCE]: Tersely updating prim {0}, {1} - part timestamp {2}",
                        //                                part.Name, part.UUID, part.TimeStampTerse);

                        update.Part.SendTerseUpdateToClient(m_presence.ControllingClient);

                        partupdate.LastTerseUpdateTime = update.Part.TimeStampTerse;
                    }
                }
                else
                {
                    //never been sent to client before so do full update
                    ScenePartUpdate partupdate = new ScenePartUpdate();
                    partupdate.FullID = update.Part.UUID;
                    partupdate.LastFullUpdateTime = update.Part.TimeStampFull;
                    m_updateTimes.Add(update.Part.UUID, partupdate);

                    // Attachment handling
                    //
                    if (update.Part.ParentGroup.RootPart.Shape.PCode == 9 && update.Part.ParentGroup.RootPart.Shape.State != 0)
                    {
                        if (update.Part != update.Part.ParentGroup.RootPart)
                            continue;

                        update.Part.ParentGroup.SendFullUpdateToClient(m_presence.ControllingClient, update.UpdateFlags);
                        continue;
                    }

                    update.Part.SendFullUpdate(m_presence.ControllingClient,
                            m_presence.GenerateClientFlags(update.Part), update.UpdateFlags);
                }
            }
        }

        public void Reset()
        {
            if (m_pendingObjects != null && m_pendingObjects.Count != 0)
            {
                lock (m_pendingObjects)
                {
                    m_pendingObjects.Clear();
                    m_pendingObjects = null;
                }
            }
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
