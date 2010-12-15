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
        protected volatile List<UUID> m_objectsInView = new List<UUID>();
        protected Prioritizer m_prioritizer;

        protected Dictionary<UUID, ScenePartUpdate> m_updateTimes = new Dictionary<UUID, ScenePartUpdate>();

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
                            SendFullUpdate(PrimUpdateFlags.FullUpdate, (SceneObjectGroup)entity); //New object, send full
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

        private void Reprioritize()
        {
            m_presence.ControllingClient.ReprioritizeUpdates();
        }

        public void SendPrimUpdates()
        {
            #region New client entering the Scene, requires all objects in the Scene

            ///If we havn't started processing this client yet, we need to send them ALL the prims that we have in this Scene (and deal with culling as well...)
            if (!m_SentInitialObjects)
            {
                m_SentInitialObjects = true;
                //If they are not in this region, we check to make sure that we allow seeing into neighbors
                if (!m_presence.IsChildAgent || (m_presence.Scene.RegionInfo.SeeIntoThisSimFromNeighbor))
                {
                    EntityBase[] entities = m_presence.Scene.Entities.GetEntities();
                    PriorityQueue entityUpdates = new PriorityQueue(entities.Length);
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
                            double priority = m_prioritizer.GetUpdatePriority(m_presence.ControllingClient, grp);
                            if (!m_updateTimes.ContainsKey(grp.UUID))
                                entityUpdates.Enqueue(priority, new EntityUpdate(grp, PrimUpdateFlags.FullUpdate), grp.LocalId); //New object, send full
                        }
                    }
                    entities = null;
                    EntityUpdate update;
                    while (entityUpdates.TryDequeue(out update))
                    {
                        SendFullUpdate(PrimUpdateFlags.FullUpdate, ((SceneObjectGroup)update.Entity));
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

                        SendFullUpdate(update.UpdateFlags, update.Part.ParentGroup);
                        continue;
                    }

                    SendFullUpdate(update.Part,
                            m_presence.GenerateClientFlags(update.Part), update.UpdateFlags);
                }
            }

            #endregion
        }

        protected internal void SendTerseUpdateToClient(IClientAPI remoteClient, PrimUpdateFlags UpdateFlags, SceneObjectPart part)
        {
            if (part.IsAttachment && part.ParentGroup.RootPart != part)
                return;
            
            remoteClient.SendPrimUpdate(part, UpdateFlags);
        }


        /// <summary>
        /// Send a full update to the client for the given part
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="clientFlags"></param>
        public void SendFullUpdate(SceneObjectPart part, uint clientFlags, PrimUpdateFlags changedFlags)
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
            //
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
            //bool isattachment = IsAttachment;
            //if (LocalId != ParentGroup.RootPart.LocalId)
            //isattachment = ParentGroup.RootPart.IsAttachment;

            m_presence.ControllingClient.SendPrimUpdate(part, changedFlags);
        }

        public void SendFullUpdate(PrimUpdateFlags UpdateFlags, SceneObjectGroup grp)
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

        public class PriorityQueue
        {
            internal delegate bool UpdatePriorityHandler(ref double priority, uint local_id);

            private MinHeap<MinHeapItem>[] m_heaps = new MinHeap<MinHeapItem>[1];
            private Dictionary<uint, LookupItem> m_lookupTable;
            private Comparison<double> m_comparison;
            private object m_syncRoot = new object();

            internal PriorityQueue() :
                this(MinHeap<MinHeapItem>.DEFAULT_CAPACITY, Comparer<double>.Default) { }
            internal PriorityQueue(int capacity) :
                this(capacity, Comparer<double>.Default) { }
            internal PriorityQueue(IComparer<double> comparer) :
                this(new Comparison<double>(comparer.Compare)) { }
            internal PriorityQueue(Comparison<double> comparison) :
                this(MinHeap<MinHeapItem>.DEFAULT_CAPACITY, comparison) { }
            internal PriorityQueue(int capacity, IComparer<double> comparer) :
                this(capacity, new Comparison<double>(comparer.Compare)) { }
            internal PriorityQueue(int capacity, Comparison<double> comparison)
            {
                m_lookupTable = new Dictionary<uint, LookupItem>(capacity);

                for (int i = 0; i < m_heaps.Length; ++i)
                    m_heaps[i] = new MinHeap<MinHeapItem>(capacity);
                this.m_comparison = comparison;
            }

            public object SyncRoot { get { return this.m_syncRoot; } }
            internal int Count
            {
                get
                {
                    int count = 0;
                    for (int i = 0; i < m_heaps.Length; ++i)
                        count = m_heaps[i].Count;
                    return count;
                }
            }

            public bool Enqueue(double priority, EntityUpdate value, uint local_id)
            {
                LookupItem item;

                if (m_lookupTable.TryGetValue(local_id, out item))
                {
                    // Combine flags
                    value.Flags |= item.Heap[item.Handle].Value.Flags;

                    item.Heap[item.Handle] = new MinHeapItem(priority, value, local_id, this.m_comparison);
                    return false;
                }
                else
                {
                    item.Heap = m_heaps[0];
                    item.Heap.Add(new MinHeapItem(priority, value, local_id, this.m_comparison), ref item.Handle);
                    m_lookupTable.Add(local_id, item);
                    return true;
                }
            }

            internal EntityUpdate Peek()
            {
                for (int i = 0; i < m_heaps.Length; ++i)
                    if (m_heaps[i].Count > 0)
                        return m_heaps[i].Min().Value;
                throw new InvalidOperationException(string.Format("The {0} is empty", this.GetType().ToString()));
            }

            internal bool TryDequeue(out EntityUpdate value)
            {
                for (int i = 0; i < m_heaps.Length; ++i)
                {
                    if (m_heaps[i].Count > 0)
                    {
                        MinHeapItem item = m_heaps[i].RemoveMin();
                        m_lookupTable.Remove(item.LocalID);
                        value = item.Value;
                        return true;
                    }
                }

                value = default(EntityUpdate);
                return false;
            }

            internal void Reprioritize(UpdatePriorityHandler handler)
            {
                MinHeapItem item;
                double priority;

                foreach (LookupItem lookup in new List<LookupItem>(this.m_lookupTable.Values))
                {
                    if (lookup.Heap.TryGetValue(lookup.Handle, out item))
                    {
                        priority = item.Priority;
                        if (handler(ref priority, item.LocalID))
                        {
                            if (lookup.Heap.ContainsHandle(lookup.Handle))
                                lookup.Heap[lookup.Handle] =
                                    new MinHeapItem(priority, item.Value, item.LocalID, this.m_comparison);
                        }
                        else
                        {
                            lookup.Heap.Remove(lookup.Handle);
                            this.m_lookupTable.Remove(item.LocalID);
                        }
                    }
                }
            }

            #region MinHeapItem
            private struct MinHeapItem : IComparable<MinHeapItem>
            {
                private double priority;
                private EntityUpdate value;
                private uint local_id;
                private Comparison<double> comparison;

                internal MinHeapItem(double priority, EntityUpdate value, uint local_id) :
                    this(priority, value, local_id, Comparer<double>.Default) { }
                internal MinHeapItem(double priority, EntityUpdate value, uint local_id, IComparer<double> comparer) :
                    this(priority, value, local_id, new Comparison<double>(comparer.Compare)) { }
                internal MinHeapItem(double priority, EntityUpdate value, uint local_id, Comparison<double> comparison)
                {
                    this.priority = priority;
                    this.value = value;
                    this.local_id = local_id;
                    this.comparison = comparison;
                }

                internal double Priority { get { return this.priority; } }
                internal EntityUpdate Value { get { return this.value; } }
                internal uint LocalID { get { return this.local_id; } }

                public override string ToString()
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("[");
                    sb.Append(this.priority.ToString());
                    sb.Append(",");
                    if (this.value != null)
                        sb.Append(this.value.ToString());
                    sb.Append("]");
                    return sb.ToString();
                }

                public int CompareTo(MinHeapItem other)
                {
                    return this.comparison(this.priority, other.priority);
                }
            }
            #endregion

            #region LookupItem
            private struct LookupItem
            {
                internal MinHeap<MinHeapItem> Heap;
                internal IHandle Handle;
            }
            #endregion
        }
    }
}
