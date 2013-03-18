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
using System.Linq;
using OpenMetaverse;

namespace Aurora.Framework
{
    public class EntityManager
    {
        protected readonly DoubleKeyDictionary<UUID, uint, UUID> m_child_2_parent_entities =
            new DoubleKeyDictionary<UUID, uint, UUID>();

        protected readonly DoubleKeyDictionary<UUID, uint, ISceneEntity> m_objectEntities =
            new DoubleKeyDictionary<UUID, uint, ISceneEntity>();

        protected readonly Dictionary<UUID, IScenePresence> m_presenceEntities = new Dictionary<UUID, IScenePresence>();
        protected readonly List<IScenePresence> m_presenceEntitiesList = new List<IScenePresence>();

        protected object m_child_2_parent_entitiesLock = new object();
        protected object m_objectEntitiesLock = new object();
        protected object m_presenceEntitiesLock = new object();

        public virtual int Count
        {
            get { return m_objectEntities.Count + this.GetPresenceCount(); }
        }

        public virtual bool Add(IEntity entity)
        {
            if (entity.LocalId == 0)
            {
                MainConsole.Instance.Warn("Entity with 0 localID!");
                return false;
            }

            try
            {
                if (entity is ISceneEntity)
                {
                    lock (m_child_2_parent_entitiesLock)
                    {
                        foreach (ISceneChildEntity part in (entity as ISceneEntity).ChildrenEntities())
                        {
                            m_child_2_parent_entities.Remove(part.UUID);
                            m_child_2_parent_entities.Remove(part.LocalId);
                            m_child_2_parent_entities.Add(part.UUID, part.LocalId, entity.UUID);
                        }
                    }
                    lock (m_objectEntitiesLock)
                        m_objectEntities.Add(entity.UUID, entity.LocalId, entity as ISceneEntity);
                }
                else
                {
                    IScenePresence presence = (IScenePresence) entity;

                    lock (m_presenceEntitiesLock)
                    {
                        m_presenceEntitiesList.Add(presence);
                        m_presenceEntities.Add(presence.UUID, presence);
                    }
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.ErrorFormat("Add Entity failed: {0}", e.Message);
                return false;
            }
            return true;
        }

        public virtual void Clear()
        {
            lock (m_objectEntitiesLock)
                m_objectEntities.Clear();
            lock (m_presenceEntitiesLock)
            {
                m_presenceEntitiesList.Clear();
                m_presenceEntities.Clear();
            }
            lock (m_child_2_parent_entitiesLock)
                m_child_2_parent_entities.Clear();
        }

        public virtual bool Remove(IEntity entity)
        {
            if (entity == null)
                return false;

            try
            {
                if (entity is ISceneEntity)
                {
                    lock (m_child_2_parent_entitiesLock)
                    {
                        //Remove all child entities
                        foreach (ISceneChildEntity part in (entity as ISceneEntity).ChildrenEntities())
                        {
                            m_child_2_parent_entities.Remove(part.UUID);
                            m_child_2_parent_entities.Remove(part.LocalId);
                        }
                    }
                    lock (m_objectEntitiesLock)
                    {
                        m_objectEntities.Remove(entity.UUID);
                        m_objectEntities.Remove(entity.LocalId);
                    }
                }
                else
                {
                    lock (m_presenceEntitiesLock)
                    {
                        m_presenceEntitiesList.Remove((IScenePresence) entity);
                        m_presenceEntities.Remove(entity.UUID);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                MainConsole.Instance.ErrorFormat("Remove Entity failed for {0}", entity.UUID, e);
                return false;
            }
        }

        public virtual List<IScenePresence> GetPresences()
        {
            return m_presenceEntitiesList;
        }

        public virtual int GetPresenceCount()
        {
            return m_presenceEntitiesList.Count;
        }

        public virtual IScenePresence[] GetPresences(Vector3 pos, float radius)
        {
            lock (m_presenceEntitiesLock)
            {
                List<IScenePresence> tmp = new List<IScenePresence>(m_presenceEntities.Count);
#if (!ISWIN)
                foreach (IScenePresence entity in m_presenceEntities.Values)
                {
                    if ((entity.AbsolutePosition - pos).LengthSquared() < radius * radius)
                        tmp.Add(entity);
                }
#else
                tmp.AddRange(m_presenceEntities.Values.Where(entity => (entity.AbsolutePosition - pos).LengthSquared() < radius * radius));
#endif

                return tmp.ToArray();
            }
        }

        public virtual ISceneEntity[] GetEntities()
        {
            lock (m_objectEntitiesLock)
            {
                return m_objectEntities.Values;
            }
        }

        public virtual ISceneEntity[] GetEntities(Vector3 pos, float radius)
        {
            lock (m_objectEntitiesLock)
            {
                List<ISceneEntity> tmp = new List<ISceneEntity>(m_objectEntities.Count);

                m_objectEntities.ForEach(delegate(ISceneEntity entity)
                                             {
                                                 //Add attachments as well, as they might be needed
                                                 if ((entity.AbsolutePosition - pos).LengthSquared() < radius*radius ||
                                                     entity.IsAttachment)
                                                     tmp.Add(entity);
                                             });
                return tmp.ToArray();
            }
        }

        public virtual bool TryGetPresenceValue(UUID key, out IScenePresence presence)
        {
            lock (m_presenceEntitiesLock)
            {
                return m_presenceEntities.TryGetValue(key, out presence);
            }
        }

        public virtual bool TryGetValue(UUID key, out IEntity obj)
        {
            return InternalTryGetValue(key, true, out obj);
        }

        protected virtual bool InternalTryGetValue(UUID key, bool checkRecursive, out IEntity obj)
        {
            IScenePresence presence;
            bool gotit;
            lock (m_presenceEntitiesLock)
                gotit = m_presenceEntities.TryGetValue(key, out presence);
            if (!gotit)
            {
                ISceneEntity presence2;
                lock (m_objectEntitiesLock)
                    gotit = m_objectEntities.TryGetValue(key, out presence2);

                //Deal with the possibility we may have been asked for a child prim
                if ((!gotit) && checkRecursive)
                    return TryGetChildPrimParent(key, out obj);
                else if (gotit)
                {
                    obj = presence2;
                    return true;
                }
            }
            else if (gotit)
            {
                obj = presence;
                return true;
            }
            obj = null;
            return false;
        }

        public virtual bool TryGetValue(uint key, out IEntity obj)
        {
            return InternalTryGetValue(key, true, out obj);
        }

        protected virtual bool InternalTryGetValue(uint key, bool checkRecursive, out IEntity obj)
        {
            ISceneEntity entity;
            bool gotit;
            lock (m_objectEntitiesLock)
                gotit = m_objectEntities.TryGetValue(key, out entity);

            //Deal with the possibility we may have been asked for a child prim
            if (!gotit && checkRecursive)
                return TryGetChildPrimParent(key, out obj);
            else if (gotit)
            {
                obj = entity;
                return true;
            }
            else
            {
                obj = null;
                return false;
            }
        }

        /// <summary>
        ///   Retrives the SceneObjectGroup of this child
        /// </summary>
        /// <param name = "childkey"></param>
        /// <param name = "obj"></param>
        /// <returns></returns>
        public virtual bool TryGetChildPrimParent(UUID childkey, out IEntity obj)
        {
            UUID ParentKey = UUID.Zero;
            bool gotit;
            lock (m_child_2_parent_entitiesLock)
                gotit = m_child_2_parent_entities.TryGetValue(childkey, out ParentKey);

            if (gotit)
                return InternalTryGetValue(ParentKey, false, out obj);

            obj = null;
            return false;
        }

        /// <summary>
        ///   Retrives the SceneObjectGroup of this child
        /// </summary>
        /// <param name = "childkey"></param>
        /// <param name = "obj"></param>
        /// <returns></returns>
        public virtual bool TryGetChildPrimParent(uint childkey, out IEntity obj)
        {
            bool gotit;
            UUID ParentKey = UUID.Zero;
            lock (m_child_2_parent_entitiesLock)
                gotit = m_child_2_parent_entities.TryGetValue(childkey, out ParentKey);

            if (gotit)
                return InternalTryGetValue(ParentKey, false, out obj);

            obj = null;
            return false;
        }

        public virtual bool TryGetChildPrim(uint childkey, out ISceneChildEntity child)
        {
            child = null;

            IEntity entity;
            if (!TryGetChildPrimParent(childkey, out entity))
                return false;
            if (!(entity is ISceneEntity))
                return false;

            child = (entity as ISceneEntity).GetChildPart(childkey);

            return true;
        }

        protected virtual bool TryGetChildPrim(UUID objectID, out ISceneChildEntity childPrim)
        {
            childPrim = null;

            IEntity entity;
            if (!TryGetChildPrimParent(objectID, out entity))
                return false;

            childPrim = (entity as ISceneEntity).GetChildPart(objectID);

            return true;
        }
    }
}