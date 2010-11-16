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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using OpenMetaverse;
using OpenSim.Framework;

namespace OpenSim.Region.Framework.Scenes
{
    public class EntityManager
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Aurora.Framework.DoubleKeyDictionary<UUID, uint, EntityBase> m_entities = new Aurora.Framework.DoubleKeyDictionary<UUID, uint, EntityBase>();
        private readonly Aurora.Framework.DoubleKeyDictionary<UUID, uint, UUID> m_child_2_parent_entities = new Aurora.Framework.DoubleKeyDictionary<UUID, uint, UUID>();
        private readonly Object m_lock = new Object();

        public int Count
        {
            get { return m_entities.Count; }
        }

        public void Add(EntityBase entity)
        {
            if (entity.LocalId == 0)
                return;

            lock (m_lock)
            {
                try
                {
                    m_entities.Remove(entity.UUID);
                    m_entities.Remove(entity.LocalId);
                    m_entities.Add(entity.UUID, entity.LocalId, entity);
                    if (entity is SceneObjectGroup)
                    {
                        foreach (SceneObjectPart part in (entity as SceneObjectGroup).ChildrenList)
                        {
                            m_child_2_parent_entities.Remove(part.UUID);
                            m_child_2_parent_entities.Remove(part.LocalId);
                            m_child_2_parent_entities.Add(part.UUID, part.LocalId, entity.UUID);
                        }
                    }
                }
                catch(Exception e)
                {
                    m_log.ErrorFormat("Add Entity failed: {0}", e.Message);
                }
            }
        }

        public void Clear()
        {
            lock (m_lock)
            {
                m_entities.Clear();
                m_child_2_parent_entities.Clear();
            }
        }

        public bool ContainsKey(UUID id)
        {
            return m_entities.ContainsKey(id);
        }

        public bool ContainsKey(uint localID)
        {
            return m_entities.ContainsKey(localID);
        }

        public bool Remove(uint localID)
        {
            if (localID == 0)
                return false;
            lock (m_lock)
            {
                try
                {
                    EntityBase entity;
                    m_entities.TryGetValue(localID, out entity);

                    if (entity != null && entity is SceneObjectGroup)
                    {
                        foreach (SceneObjectPart part in (entity as SceneObjectGroup).ChildrenList)
                        {
                            m_child_2_parent_entities.Remove(part.UUID);
                            m_child_2_parent_entities.Remove(part.LocalId);
                        }
                        m_entities.Remove(entity.UUID);
                    }
                    else if (m_child_2_parent_entities.ContainsKey(localID))
                    {
                        return m_child_2_parent_entities.Remove(localID);
                    }
                    return m_entities.Remove(localID);
                }
                catch (Exception e)
                {
                    m_log.ErrorFormat("Remove Entity failed for {0}", localID, e);
                    return false;
                }
            }
        }

        public bool Remove(UUID id)
        {
            if (id == UUID.Zero)
                return false;

            lock (m_lock)
            {
                try 
                {
                    EntityBase entity;
                    m_entities.TryGetValue(id, out entity);

                    if (entity != null && entity is SceneObjectGroup)
                    {
                        foreach (SceneObjectPart part in (entity as SceneObjectGroup).ChildrenList)
                        {
                            m_child_2_parent_entities.Remove(part.UUID);
                            m_child_2_parent_entities.Remove(part.LocalId);
                        }
                        m_entities.Remove(entity.LocalId);
                    }
                    else if(m_child_2_parent_entities.ContainsKey(id)) 
                    {
                        return m_child_2_parent_entities.Remove(id);
                    }
                    return m_entities.Remove(id);
                }
                catch (Exception e)
                {
                    m_log.ErrorFormat("Remove Entity failed for {0}", id, e);
                    return false;
                }
            }
        }

        public EntityBase[] GetAllByType<T>()
        {
            List<EntityBase> tmp = new List<EntityBase>();

            m_entities.ForEach(
                delegate(EntityBase entity)
                {
                    if (entity is T)
                        tmp.Add(entity);
                }
            );

            return tmp.ToArray();
        }

        public EntityBase[] GetEntities()
        {
            List<EntityBase> tmp = new List<EntityBase>(m_entities.Count);
            m_entities.ForEach(delegate(EntityBase entity) { tmp.Add(entity); });
            return tmp.ToArray();
        }

        public void ForEach(Action<EntityBase> action)
        {
            m_entities.ForEach(action);
        }

        public EntityBase Find(Predicate<EntityBase> predicate)
        {
            return m_entities.FindValue(predicate);
        }

        public EntityBase this[UUID id]
        {
            get
            {
                EntityBase entity;
                m_entities.TryGetValue(id, out entity);
                return entity;
            }
            set
            {
                Add(value);
            }
        }

        public EntityBase this[uint localID]
        {
            get
            {
                EntityBase entity;
                m_entities.TryGetValue(localID, out entity);
                return entity;
            }
            set
            {
                Add(value);
            }
        }

        public bool TryGetValue(UUID key, out EntityBase obj)
        {
            return InternalTryGetValue(key, true, out obj);
        }

        private bool InternalTryGetValue(UUID key, bool checkRecursive, out EntityBase obj)
        {
            if (!m_entities.TryGetValue(key, out obj) && checkRecursive)
            {
                //Deal with the possibility we may have been asked for a child prim
                return TryGetChildPrimParent(key, out obj);
            }
            if (!checkRecursive && obj == null)
                return false;
            return true;
        }

        public bool TryGetValue(uint key, out EntityBase obj)
        {
            return InternalTryGetValue(key, true, out obj);
        }

        private bool InternalTryGetValue(uint key, bool checkRecursive, out EntityBase obj)
        {
            if (!m_entities.TryGetValue(key, out obj) && checkRecursive)
            {
                //Deal with the possibility we may have been asked for a child prim
                return TryGetChildPrimParent(key, out obj);
            }
            if (!checkRecursive && obj == null)
                return false;
            return true;
        }

        /// <summary>
        /// Retrives the SceneObjectGroup of this child
        /// </summary>
        /// <param name="childkey"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool TryGetChildPrimParent(UUID childkey, out EntityBase obj)
        {
            lock (m_lock)
            {
                UUID ParentKey = UUID.Zero;
                if (m_child_2_parent_entities.TryGetValue(childkey, out ParentKey))
                    return InternalTryGetValue(ParentKey, false, out obj);

                obj = null;
                return false;
            }
        }

        /// <summary>
        /// Retrives the SceneObjectGroup of this child
        /// </summary>
        /// <param name="childkey"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool TryGetChildPrimParent(uint childkey, out EntityBase obj)
        {
            lock (m_lock)
            {
                UUID ParentKey = UUID.Zero;
                if (m_child_2_parent_entities.TryGetValue(childkey, out ParentKey))
                    return InternalTryGetValue(ParentKey, false, out obj);

                obj = null;
                return false;
            }
        }

        public bool TryGetChildPrim(uint childkey, out ISceneEntity child)
        {
            lock (m_lock)
            {
                child = null;

                EntityBase entity;
                if (!TryGetChildPrimParent(childkey, out entity))
                    return false;
                if (!(entity is SceneObjectGroup))
                    return false;

                child = (entity as SceneObjectGroup).GetChildPart(childkey);

                return true;
            }
        }

        internal bool TryGetChildPrim(UUID objectID, out ISceneEntity childPrim)
        {
            lock (m_lock)
            {
                childPrim = null;

                EntityBase entity;
                if (!TryGetChildPrimParent(objectID, out entity))
                    return false;
                if (!(entity is SceneObjectGroup))
                    return false;

                childPrim = (entity as SceneObjectGroup).GetChildPart(objectID);

                return true;
            }
        }
    }
}
