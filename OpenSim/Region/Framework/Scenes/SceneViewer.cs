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
using System.Collections.Specialized;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading;
using OpenMetaverse;
using log4net;
using OpenSim.Framework;
using OpenSim.Framework.Client;
using OpenSim.Region.Framework.Interfaces;
using Mischel.Collections;
using Nini.Config;

namespace OpenSim.Region.Framework.Scenes
{
    public class SceneViewer : ISceneViewer
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        private const double MINVIEWDSTEP = 16;
        private const double MINVIEWDSTEPSQ = MINVIEWDSTEP * MINVIEWDSTEP;

        protected IScenePresence m_presence;
        /// <summary>
        /// Have we sent all of the objects in the sim that the client can see for the first time?
        /// </summary>
        protected bool m_SentInitialObjects = false;
        protected volatile bool m_queueing = false;
        protected volatile bool m_inUse = false;
        protected Prioritizer m_prioritizer;
        protected Culler m_culler;
        protected bool m_forceCullCheck = false;
        private OrderedDictionary/*<UUID, EntityUpdate>*/ m_presenceUpdatesToSend = new OrderedDictionary/*<UUID, EntityUpdate>*/ ();
        private OrderedDictionary/*<UUID, AnimationGroup>*/ m_presenceAnimationsToSend = new OrderedDictionary/*<UUID, AnimationGroup>*/ ();
        private OrderedDictionary/*<UUID, EntityUpdate>*/ m_objectUpdatesToSend = new OrderedDictionary/*<UUID, EntityUpdate>*/ ();
        private OrderedDictionary/*<UUID, ISceneChildEntity>*/ m_objectPropertiesToSend = new OrderedDictionary/*<UUID, ISceneChildEntity>*/ ();
        private List<UUID> m_EntitiesInPacketQueue = new List<UUID>();
        private List<UUID> m_AnimationsInPacketQueue = new List<UUID>();
        private List<UUID> m_PropertiesInPacketQueue = new List<UUID>();
        private HashSet<ISceneEntity> lastGrpsInView = new HashSet<ISceneEntity> ();
        private HashSet<IScenePresence> lastPresencesInView = new HashSet<IScenePresence> ();
        private Vector3 m_lastUpdatePos;
        private int m_numberOfLoops = 0;
        private const int NUMBER_OF_LOOPS_TO_WAIT = 30;

        private const float PresenceSendPercentage = 0.60f;
        private const float PrimSendPercentage = 0.40f;
        /// <summary>
        /// If this is set, we start inserting other presences updates at 1 so that our updates go before theirs
        /// </summary>
        private volatile bool m_ourPresenceHasUpdate = false;

        public IPrioritizer Prioritizer
        {
            get { return m_prioritizer; }
        }

        public ICuller Culler
        {
            get { return m_culler; }
        }

        #endregion

        #region Constructor

        public SceneViewer (IScenePresence presence)
        {
            m_presence = presence;
            m_presence.Scene.EventManager.OnSignificantClientMovement += SignificantClientMovement;
            m_presence.Scene.AuroraEventManager.OnGenericEvent += AuroraEventManager_OnGenericEvent;
            m_prioritizer = new Prioritizer (presence.Scene);
            m_culler = new Culler (presence.Scene);
        }

        object AuroraEventManager_OnGenericEvent (string FunctionName, object parameters)
        {
            if (Culler != null && Culler.UseCulling && FunctionName == "DrawDistanceChanged")
            {
                IScenePresence sp = (IScenePresence)parameters;
                if (sp.UUID != m_presence.UUID)
                    return null; //Only want our av

                //Draw Distance chagned, force a cull check
                m_forceCullCheck = true;
                //Don't do this immediately
                //SignificantClientMovement (m_presence.ControllingClient);
            }
            else if (FunctionName == "SignficantCameraMovement")
            {
                //Camera chagned, do a cull check
                SignificantClientMovement(m_presence.ControllingClient);
            }
            return null;
        }

        #endregion

        #region Enqueue/Remove updates for entities

        public void QueuePresenceForUpdate (IScenePresence presence, PrimUpdateFlags flags)
        {
            if (Culler != null && !Culler.ShowEntityToClient(m_presence, presence))
                return; // if 2 far ignore

            lock (m_presenceUpdatesToSend)
            {
                EntityUpdate o = (EntityUpdate)m_presenceUpdatesToSend[presence.UUID];
                if (o == null)
                    o = new EntityUpdate(presence, flags);
                else
                {
                    if ((o.Flags & flags) == o.Flags)
                        return; //Same, leave it alone!
                    o.Flags = o.Flags & flags;
                    m_presenceUpdatesToSend.Remove(presence.UUID);
                }

                if (m_presence.UUID == presence.UUID)
                {
                    //Its us, set us first!
                    m_ourPresenceHasUpdate = true;
                    m_presenceUpdatesToSend.Insert(0, presence.UUID, o);
                }
                else if (m_ourPresenceHasUpdate) //If this is set, we start inserting at 1 so that our updates go first
                    // We can also safely assume that 1 is fine, because there has to be 0 already there set by us
                    m_presenceUpdatesToSend.Insert(m_presenceUpdatesToSend.Count, presence.UUID, o);
                else //Set us at 0, no updates from us
                    m_presenceUpdatesToSend.Insert(m_presenceUpdatesToSend.Count, presence.UUID, o);
            }
        }

        public void QueuePresenceForAnimationUpdate(IScenePresence presence, AnimationGroup animation)
        {
            if (Culler != null && !Culler.ShowEntityToClient(m_presence, presence))
                return; // if 2 far ignore

            lock (m_presenceAnimationsToSend)
            {
                m_presenceAnimationsToSend.Remove(animation.AvatarID);
                //Insert at the end
                m_presenceAnimationsToSend.Insert(m_presenceAnimationsToSend.Count, animation.AvatarID, animation);
            }
        }

        /// <summary>
        /// Add the objects to the queue for which we need to send an update to the client
        /// </summary>
        /// <param name="part"></param>
        public void QueuePartForUpdate (ISceneChildEntity part, PrimUpdateFlags flags)
        {
            if (Culler != null && !Culler.ShowEntityToClient(m_presence, part.ParentEntity))
                return; // if 2 far ignore

            EntityUpdate o = new EntityUpdate (part, flags);
            QueueEntityUpdate (o);
        }

        private void QueueEntityUpdate(EntityUpdate update)
        {
            lock (m_objectUpdatesToSend)
            {
                EntityUpdate o = (EntityUpdate)m_objectUpdatesToSend[update.Entity.UUID];
                if (o == null)
                    o = update;
                else
                {
                    if (o.Flags == update.Flags)
                        return; //Same, leave it alone!
                    o.Flags = o.Flags & update.Flags;
                    m_objectUpdatesToSend.Remove(update.Entity.UUID);
                }
                m_objectUpdatesToSend.Insert(m_objectUpdatesToSend.Count, update.Entity.UUID, update);
            }
        }

        public void QueuePartsForPropertiesUpdate(ISceneChildEntity[] entities)
        {
            lock (m_objectPropertiesToSend)
            {
                foreach (ISceneChildEntity entity in entities)
                {
                    if (Culler != null && !Culler.ShowEntityToClient(m_presence, entity))
                        continue; // if 2 far ignore

                    m_objectPropertiesToSend.Remove(entity.UUID);
                    //Insert at the end
                    m_objectPropertiesToSend.Insert(m_objectPropertiesToSend.Count, entity.UUID, entity);
                }
            }
        }

        #endregion

        #region Object Culling by draw distance

        /// <summary>
        /// When the client moves enough to trigger this, make sure that we have sent
        ///  the client all of the objects that have just entered their FOV in their draw distance.
        /// </summary>
        /// <param name="remote_client"></param>
        private void SignificantClientMovement (IClientAPI remote_client)
        {
            if (Culler == null)
                return;

            if (!Culler.UseCulling)
                return;

            //Only check our presence
            if (remote_client.AgentId != m_presence.UUID)
                return;

            if (!m_forceCullCheck && m_presence.DrawDistance > m_presence.Scene.RegionInfo.RegionSizeX &&
                    m_presence.DrawDistance > m_presence.Scene.RegionInfo.RegionSizeY)
            {
                m_forceCullCheck = false; //Make sure to reset it
                return;
            }

            if (m_presence.DrawDistance < 32)
            {
                //If the draw distance is small, the client has gotten messed up or something and we can't do this...
                m_presence.DrawDistance = 32; //Force give them a draw distance
            }

            if (!m_presence.IsChildAgent || m_presence.Scene.RegionInfo.SeeIntoThisSimFromNeighbor)
            {
                Vector3 pos = m_presence.CameraPosition;
                float distsq = Vector3.DistanceSquared (pos, m_lastUpdatePos);
                distsq += 0.2f * m_presence.Velocity.LengthSquared ();
                if (distsq < MINVIEWDSTEPSQ && !m_forceCullCheck) //They havn't moved enough to trigger another update, so just quit
                    return;
                m_forceCullCheck = false;
                Util.FireAndForget (DoSignificantClientMovement);
            }
        }

        private void DoSignificantClientMovement (object o)
        {
            ISceneEntity[] entities = m_presence.Scene.Entities.GetEntities (m_presence.AbsolutePosition, m_presence.DrawDistance);
            PriorityQueue<EntityUpdate, double> m_entsqueue = new PriorityQueue<EntityUpdate, double> (entities.Length);

            // build a prioritized list of things we need to send

            HashSet<ISceneEntity> NewGrpsInView = new HashSet<ISceneEntity> ();

            foreach (ISceneEntity e in entities)
            {
                if (e != null)
                {
                    if (e.IsDeleted)
                        continue;

                    //Check for culling here!
                    if (!Culler.ShowEntityToClient(m_presence, e))
                        continue; // if 2 far ignore

                    double priority = m_prioritizer.GetUpdatePriority (m_presence, e);
                    NewGrpsInView.Add (e);

                    if (lastGrpsInView.Contains (e))
                        continue;

                    //Send the root object first!
                    EntityUpdate rootupdate = new EntityUpdate (e.RootChild, PrimUpdateFlags.FullUpdate);
                    PriorityQueueItem<EntityUpdate, double> rootitem = new PriorityQueueItem<EntityUpdate, double> ();
                    rootitem.Value = rootupdate;
                    rootitem.Priority = priority;
                    m_entsqueue.Enqueue (rootitem);

                    foreach (ISceneChildEntity child in e.ChildrenEntities ())
                    {
                        if (child == e.RootChild)
                            continue; //Already sent
                        EntityUpdate update = new EntityUpdate (child, PrimUpdateFlags.FullUpdate);
                        PriorityQueueItem<EntityUpdate, double> item = new PriorityQueueItem<EntityUpdate, double> ();
                        item.Value = update;
                        item.Priority = priority;
                        m_entsqueue.Enqueue (item);
                    }
                }
            }
            entities = null;
            lastGrpsInView.Clear ();
            lastGrpsInView.UnionWith (NewGrpsInView);
            NewGrpsInView.Clear ();

            // send them 
            SendQueued (m_entsqueue);

            HashSet<IScenePresence> NewPresencesInView = new HashSet<IScenePresence>();

            //Check for scenepresences as well
            IScenePresence[] presences = m_presence.Scene.Entities.GetPresences(m_presence.AbsolutePosition, m_presence.DrawDistance);
            foreach (IScenePresence presence in presences)
            {
                if (presence != null && presence.UUID != m_presence.UUID)
                {
                    //Check for culling here!
                    if (!Culler.ShowEntityToClient(m_presence, presence))
                        continue; // if 2 far ignore

                    NewPresencesInView.Add(presence);

                    if (lastPresencesInView.Contains(presence))
                        continue; //Don't resend the update

                    m_presence.ControllingClient.SendAvatarDataImmediate(presence);
                    //Send the animations too
                    presence.Animator.SendAnimPackToClient(m_presence.ControllingClient);
                }
            }
            presences = null;
            lastPresencesInView.Clear();
            lastPresencesInView.UnionWith(NewPresencesInView);
            NewPresencesInView.Clear();
        }

        #endregion

        #region SendPrimUpdates

        /// <summary>
        /// This method is called by the LLUDPServer and should never be called by anyone else
        /// It loops through the available updates and sends them out (no waiting)
        /// </summary>
        /// <param name="numUpdates">The number of updates to send</param>
        public void SendPrimUpdates(Int64 numUpdates)
        {
            if (m_inUse)
                return;

            m_numberOfLoops++;
            if (m_numberOfLoops < NUMBER_OF_LOOPS_TO_WAIT) //Wait for the client to finish connecting fully before sending out bunches of updates
                return;

            m_inUse = true;
            //This is for stats
            int AgentMS = Util.EnvironmentTickCount ();

            #region New client entering the Scene, requires all objects in the Scene

            ///If we havn't started processing this client yet, we need to send them ALL the prims that we have in this Scene (and deal with culling as well...)
            if (!m_SentInitialObjects && m_presence.DrawDistance != 0.0f)
            {
                //If they are not in this region, we check to make sure that we allow seeing into neighbors
                if (!m_presence.IsChildAgent || (m_presence.Scene.RegionInfo.SeeIntoThisSimFromNeighbor) && m_prioritizer != null)
                {
                    m_SentInitialObjects = true;
                    ISceneEntity[] entities = m_presence.Scene.Entities.GetEntities();
                    PriorityQueue<EntityUpdate, double> m_entsqueue = new PriorityQueue<EntityUpdate, double> (entities.Length);

                    // build a prioritized list of things we need to send

                    foreach (ISceneEntity e in entities)
                    {
                        if (e != null && e is SceneObjectGroup)
                        {
                            if (e.IsDeleted)
                                continue;

                            //Check for culling here!
                            if (Culler != null && !Culler.ShowEntityToClient(m_presence, e))
                                continue;

                            double priority = m_prioritizer.GetUpdatePriority (m_presence, e);
                            //Send the root object first!
                            EntityUpdate rootupdate = new EntityUpdate (e.RootChild, PrimUpdateFlags.FullUpdate);
                            PriorityQueueItem<EntityUpdate, double> rootitem = new PriorityQueueItem<EntityUpdate, double> ();
                            rootitem.Value = rootupdate;
                            rootitem.Priority = priority;
                            m_entsqueue.Enqueue (rootitem);

                            foreach (ISceneChildEntity child in e.ChildrenEntities ())
                            {
                                if (child == e.RootChild)
                                    continue; //Already sent
                                EntityUpdate update = new EntityUpdate (child, PrimUpdateFlags.FullUpdate);
                                PriorityQueueItem<EntityUpdate, double> item = new PriorityQueueItem<EntityUpdate, double> ();
                                item.Value = update;
                                item.Priority = priority;
                                m_entsqueue.Enqueue (item);
                            }
                        }
                    }
                    entities = null;
                    // send them 
                    SendQueued (m_entsqueue);
                }
            }

            int numberSent = 0;
            int presenceNumToSend = (int)(numUpdates * PresenceSendPercentage);
            lock (m_presenceUpdatesToSend)
            {
                //Send the numUpdates of them if that many
                // if we don't have that many, we send as many as possible, then switch to objects
                if (m_presenceUpdatesToSend.Count != 0)
                {
                    try
                    {
                        int count = m_presenceUpdatesToSend.Count > presenceNumToSend ? presenceNumToSend : m_presenceUpdatesToSend.Count;
                        List<EntityUpdate> updates = new List<EntityUpdate>();
                        for (int i = 0; i < count; i++)
                        {
                            EntityUpdate update = ((EntityUpdate)m_presenceUpdatesToSend[0]);
                            if (m_EntitiesInPacketQueue.Contains(update.Entity.UUID))
                            {
                                m_presenceUpdatesToSend.RemoveAt(0);
                                m_presenceUpdatesToSend.Insert(m_presenceUpdatesToSend.Count, update.Entity.UUID, update);
                                continue;
                            }
                            updates.Add(update);
                            m_EntitiesInPacketQueue.Add(update.Entity.UUID);
                            m_presenceUpdatesToSend.RemoveAt(0);
                        }
                        if (updates.Count != 0)
                        {
                            //Make sure we don't have an update for us
                            if(updates[0].Entity.UUID == m_presence.UUID)
                                m_ourPresenceHasUpdate = false;
                            m_presence.ControllingClient.SendPrimUpdate(updates);
                        }
                    }
                    catch(Exception ex)
                    {
                        m_log.WarnFormat("[SceneViewer]: Exception while running presence loop: {0}", ex.ToString());
                    }
                }
            }

            lock (m_presenceAnimationsToSend)
            {
                //Send the numUpdates of them if that many
                // if we don't have that many, we send as many as possible, then switch to objects
                if (m_presenceAnimationsToSend.Count != 0)
                {
                    try
                    {
                        int count = m_presenceAnimationsToSend.Count > presenceNumToSend ? presenceNumToSend : m_presenceAnimationsToSend.Count;
                        for (int i = 0; i < count; i++)
                        {
                            AnimationGroup update = ((AnimationGroup)m_presenceAnimationsToSend[0]);
                            if (m_AnimationsInPacketQueue.Contains(update.AvatarID))
                            {
                                m_presenceAnimationsToSend.RemoveAt(0);
                                m_presenceAnimationsToSend.Insert(m_presenceAnimationsToSend.Count, update.AvatarID, update);
                                continue;
                            }
                            m_AnimationsInPacketQueue.Add(update.AvatarID);
                            m_presenceAnimationsToSend.RemoveAt(0);
                            m_presence.ControllingClient.SendAnimations(update);
                        }
                    }
                    catch (Exception ex)
                    {
                        m_log.WarnFormat("[SceneViewer]: Exception while running presence loop: {0}", ex.ToString());
                    }
                }
            }

            lock (m_objectPropertiesToSend)
            {
                //Send the numUpdates of them if that many
                // if we don't have that many, we send as many as possible, then switch to objects
                if (m_objectPropertiesToSend.Count != 0)
                {
                    try
                    {
                        List<IEntity> entities = new List<IEntity>();
                        int count = m_objectPropertiesToSend.Count > (int)numUpdates ? (int)numUpdates : m_objectPropertiesToSend.Count;
                        for (int i = 0; i < count; i++)
                        {
                            ISceneChildEntity entity = ((ISceneChildEntity)m_objectPropertiesToSend[0]);
                            if (m_PropertiesInPacketQueue.Contains(entity.UUID))
                            {
                                m_objectPropertiesToSend.RemoveAt(0);
                                m_objectPropertiesToSend.Insert(m_objectPropertiesToSend.Count, entity.UUID, entity);
                                continue;
                            }
                            m_PropertiesInPacketQueue.Add(entity.UUID);
                            m_objectPropertiesToSend.RemoveAt(0);
                            entities.Add(entity);
                        }
                        m_presence.ControllingClient.SendObjectPropertiesReply(entities);
                    }
                    catch (Exception ex)
                    {
                        m_log.WarnFormat("[SceneViewer]: Exception while running presence loop: {0}", ex.ToString());
                    }
                }
            }

            lock (m_objectUpdatesToSend)
            {
                numberSent = presenceNumToSend - numberSent;
                int numToSend = (int)(numUpdates * PrimSendPercentage) + numberSent; //If we didn't send that many presence updates, send a few more
                if (m_objectUpdatesToSend.Count != 0)
                {
                    try
                    {
                        int count = m_objectUpdatesToSend.Count > numToSend ? numToSend : m_objectUpdatesToSend.Count;
                        List<EntityUpdate> updates = new List<EntityUpdate>();
                        for (int i = 0; i < count; i++)
                        {
                            EntityUpdate update = ((EntityUpdate)m_objectUpdatesToSend[0]);
                            if (m_EntitiesInPacketQueue.Contains(update.Entity.UUID))
                            {
                                m_objectUpdatesToSend.RemoveAt(0);
                                m_objectUpdatesToSend.Insert(m_objectUpdatesToSend.Count, update.Entity.UUID, update);
                                continue;
                            }
                            updates.Add((EntityUpdate)m_objectUpdatesToSend[0]);
                            m_EntitiesInPacketQueue.Add(((EntityUpdate)m_objectUpdatesToSend[0]).Entity.UUID);
                            m_objectUpdatesToSend.RemoveAt(0);
                        }
                        m_presence.ControllingClient.SendPrimUpdate(updates);
                    }
                    catch (Exception ex)
                    {
                        m_log.WarnFormat("[SceneViewer]: Exception while running object loop: {0}", ex.ToString());
                    }
                }
            }

            //Add the time to the stats tracker
            IAgentUpdateMonitor reporter = (IAgentUpdateMonitor)m_presence.Scene.RequestModuleInterface<IMonitorModule> ().GetMonitor (m_presence.Scene.RegionInfo.RegionID.ToString (), "Agent Update Count");
            if (reporter != null)
                reporter.AddAgentTime (Util.EnvironmentTickCountSubtract (AgentMS));

            m_inUse = false;
        }

        /// <summary>
        /// Once the packet has been sent, allow newer updates to be sent for the given entity
        /// </summary>
        /// <param name="ID"></param>
        public void FinishedEntityPacketSend(IEnumerable<EntityUpdate> updates)
        {
            foreach (EntityUpdate update in updates)
            {
                m_EntitiesInPacketQueue.Remove(update.Entity.UUID);
            }
        }

        /// <summary>
        /// Once the packet has been sent, allow newer updates to be sent for the given entity
        /// </summary>
        /// <param name="ID"></param>
        public void FinishedPropertyPacketSend(IEnumerable<IEntity> updates)
        {
            foreach (IEntity update in updates)
            {
                m_PropertiesInPacketQueue.Remove(update.UUID);
            }
        }

        /// <summary>
        /// Once the packet has been sent, allow newer animations to be sent for the given entity
        /// </summary>
        /// <param name="ID"></param>
        public void FinishedAnimationPacketSend(AnimationGroup update)
        {
            m_AnimationsInPacketQueue.Remove(update.AvatarID);
        }

        private void SendQueued (PriorityQueue<EntityUpdate, double> m_entsqueue)
        {
            PriorityQueueItem<EntityUpdate, double> up;

            //Enqueue them all
            while (m_entsqueue.TryDequeue (out up))
            {
                QueueEntityUpdate (up.Value);
            }
            m_entsqueue.Clear ();

            m_lastUpdatePos = (m_presence.IsChildAgent) ?
                m_presence.AbsolutePosition :
                m_presence.CameraPosition;
        }

        #endregion

        #endregion

        #region Reset and Close

        /// <summary>
        /// The client has left this region and went into a child region
        /// </summary>
        public void Reset ()
        {
            //Don't reset the prim... the client is just in a child region now, we don't want to resent them all the prims
            //Reset the culler so that it doesn't cache too much
            m_culler.Reset ();
        }

        /// <summary>
        /// Reset all lists that have to deal with what updates the viewer has
        /// </summary>
        public void Close ()
        {
            m_SentInitialObjects = false;
            m_prioritizer = null;
            m_culler = null;
            m_inUse = false;
            m_queueing = false;
            m_objectUpdatesToSend.Clear ();
            m_presenceUpdatesToSend.Clear ();
            m_presence.Scene.EventManager.OnSignificantClientMovement -= SignificantClientMovement;
            m_presence.Scene.AuroraEventManager.OnGenericEvent -= AuroraEventManager_OnGenericEvent;
            m_presence = null;
        }

        #endregion
    }
}
