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
    public class EntityManager : IEnumerable<EntityBase>
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Dictionary<UUID,EntityBase> m_eb_uuid = new Dictionary<UUID, EntityBase>();
        private readonly Dictionary<uint, EntityBase> m_eb_localID = new Dictionary<uint, EntityBase>();
        private readonly Dictionary<UUID, UUID> m_child_uuid = new Dictionary<UUID, UUID>();
        private readonly Dictionary<uint, UUID> m_child_loc_id = new Dictionary<uint, UUID>();
        private readonly Dictionary<uint, ISceneEntity> m_eb_child_loc_id = new Dictionary<uint, ISceneEntity>();
        private readonly Object m_lock = new Object();

        public void Add(EntityBase entity)
        {
            lock (m_lock)
            {
                try
                {
                    m_eb_uuid.Add(entity.UUID, entity);
                    m_eb_localID.Add(entity.LocalId, entity);
                    if (entity is SceneObjectGroup)
                    {
                        foreach (SceneObjectPart part in (entity as SceneObjectGroup).ChildrenList)
                        {
                            m_child_uuid[part.UUID] = entity.UUID;
                            m_child_loc_id[part.LocalId] = entity.UUID;
                            m_eb_child_loc_id[part.LocalId] = part;
                        }
                    }
                }
                catch(Exception e)
                {
                    m_log.ErrorFormat("Add Entity failed: {0}", e.Message);
                }
            }
        }

        public void InsertOrReplace(EntityBase entity)
        {
            lock (m_lock)
            {
                try
                {
                    m_eb_uuid[entity.UUID] = entity;
                    m_eb_localID[entity.LocalId] = entity;
                    if (entity is SceneObjectGroup)
                    {
                        foreach (SceneObjectPart part in (entity as SceneObjectGroup).ChildrenList)
                        {
                            m_child_uuid[part.UUID] = entity.UUID;
                            m_child_loc_id[part.LocalId] = entity.UUID;
                            m_eb_child_loc_id[part.LocalId] = part;
                        }
                    }
                }
                catch(Exception e)
                {
                    m_log.ErrorFormat("Insert or Replace Entity failed: {0}", e.Message);
                }
            }
        }

        public void Clear()
        {
            lock (m_lock)
            {
                m_eb_uuid.Clear();
                m_eb_localID.Clear();
                m_child_uuid.Clear();
                m_child_loc_id.Clear();
            }
        }

        public int Count
        {
            get
            {
                return m_eb_uuid.Count;
            }
        }

        public bool ContainsKey(UUID id)
        {
            try
            {
                return m_eb_uuid.ContainsKey(id);
            }
            catch
            {
                return false;
            }
        }

        public bool ContainsKey(uint localID)
        {
            try
            {
                return m_eb_localID.ContainsKey(localID);
            }
            catch
            {
                return false;
            }
        }

        public bool Remove(uint localID)
        {
            lock (m_lock)
            {
                try
                {
                    bool a = false;
                    EntityBase entity;
                    if (m_eb_localID.TryGetValue(localID, out entity))
                        a = m_eb_uuid.Remove(entity.UUID);

                    if (entity != null && entity is SceneObjectGroup)
                    {
                        foreach (SceneObjectPart part in (entity as SceneObjectGroup).ChildrenList)
                        {
                            m_child_uuid.Remove(part.UUID);
                            m_child_loc_id.Remove(part.LocalId);
                            m_eb_child_loc_id.Remove(part.LocalId);
                        }
                    }

                    bool b = m_eb_localID.Remove(localID);
                    return a && b;
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
            lock (m_lock)
            {
                try 
                {
                    bool a = false;
                    EntityBase entity;
                    if (m_eb_uuid.TryGetValue(id, out entity))
                        a = m_eb_localID.Remove(entity.LocalId);

                    if (entity != null && entity is SceneObjectGroup)
                    {
                        foreach (SceneObjectPart part in (entity as SceneObjectGroup).ChildrenList)
                        {
                            m_child_uuid.Remove(part.UUID);
                            m_child_loc_id.Remove(part.LocalId);
                            m_eb_child_loc_id.Remove(part.LocalId);
                        }
                    }

                    bool b = m_eb_uuid.Remove(id);
                    return a && b;
                }
                catch (Exception e)
                {
                    m_log.ErrorFormat("Remove Entity failed for {0}", id, e);
                    return false;
                }
            }
        }

        public List<EntityBase> GetAllByType<T>()
        {
            List<EntityBase> tmp = new List<EntityBase>();

            lock (m_lock)
            {
                try
                {
                    foreach (KeyValuePair<UUID, EntityBase> pair in m_eb_uuid)
                    {
                        if (pair.Value is T)
                        {
                            tmp.Add(pair.Value);
                        }
                    }
                }
                catch (Exception e) 
                {
                    m_log.ErrorFormat("GetAllByType failed for {0}", e);
                    tmp = null;
                }
            }

            return tmp;
        }

        public List<EntityBase> GetEntities()
        {
            lock (m_lock)
            {
                return new List<EntityBase>(m_eb_uuid.Values);
            }
        }

        public EntityBase this[UUID id]
        {
            get
            {
                lock (m_lock)
                {
                    EntityBase entity;
                    if (m_eb_uuid.TryGetValue(id, out entity))
                        return entity;
                    else
                        return null;
                }
            }
            set
            {
                InsertOrReplace(value);
            }
        }

        public EntityBase this[uint localID]
        {
            get
            {
                lock (m_lock)
                {
                    EntityBase entity;
                    if (m_eb_localID.TryGetValue(localID, out entity))
                        return entity;
                    else
                        return null;
                }
            }
            set
            {
                InsertOrReplace(value);
            }
        }

        public bool TryGetValue(UUID key, out EntityBase obj)
        {
            lock (m_lock)
            {
                return m_eb_uuid.TryGetValue(key, out obj);
            }
        }

        public bool TryGetValue(uint key, out EntityBase obj)
        {
            lock (m_lock)
            {
                return m_eb_localID.TryGetValue(key, out obj);
            }
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
                if (m_child_uuid.TryGetValue(childkey, out ParentKey))
                    return TryGetValue(ParentKey, out obj);

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
                if (m_child_loc_id.TryGetValue(childkey, out ParentKey))
                    return TryGetValue(ParentKey, out obj);

                obj = null;
                return false;
            }
        }

        public bool TryGetChildPrim(uint childkey, out ISceneEntity child)
        {
            lock (m_lock)
            {
                if (m_eb_child_loc_id.TryGetValue(childkey, out child))
                    return true;

                child = null;
                return false;
            }
            
        }

        /// <summary>
        /// This could be optimised to work on the list 'live' rather than making a safe copy and iterating that.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<EntityBase> GetEnumerator()
        {
            return GetEntities().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }
}
