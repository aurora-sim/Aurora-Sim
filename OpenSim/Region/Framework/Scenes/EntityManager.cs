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
        private readonly Aurora.Framework.DoubleKeyDictionary<UUID, uint, EntityBase> m_entities = new Aurora.Framework.DoubleKeyDictionary<UUID, uint, EntityBase> ();
        private readonly Aurora.Framework.DoubleKeyDictionary<UUID, uint, ISceneEntity> m_objectEntities = new Aurora.Framework.DoubleKeyDictionary<UUID, uint, ISceneEntity> ();
        private readonly Dictionary<UUID, IScenePresence> m_presenceEntities = new Dictionary<UUID, IScenePresence> ();
        private readonly Aurora.Framework.DoubleKeyDictionary<UUID, uint, UUID> m_child_2_parent_entities = new Aurora.Framework.DoubleKeyDictionary<UUID, uint, UUID> ();
        private readonly Object m_lock = new Object();

        public int Count
        {
            get { return m_entities.Count; }
        }

        public void Add (IEntity entity)
        {
            if (entity.LocalId == 0)
                return;

            lock (m_lock)
            {
                try
                {
                    if (entity is ISceneEntity)
                    {
                        foreach (ISceneChildEntity part in (entity as ISceneEntity).ChildrenEntities())
                        {
                            m_child_2_parent_entities.Remove (part.UUID);
                            m_child_2_parent_entities.Remove (part.LocalId);
                            m_child_2_parent_entities.Add (part.UUID, part.LocalId, entity.UUID);
                        }
                        m_objectEntities.Add (entity.UUID, entity.LocalId, entity as ISceneEntity);
                    }
                    else
                    {
                        IScenePresence presence = (IScenePresence)entity;
                        m_presenceEntities.Add (presence.UUID, presence);
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

        public bool Remove(IEntity entity)
        {
            if (entity == null)
                return false;

            lock (m_lock)
            {
                try 
                {
                    if (entity is ISceneEntity)
                    {
                        //Remove all child entities
                        foreach (ISceneChildEntity part in (entity as ISceneEntity).ChildrenEntities ())
                        {
                            m_child_2_parent_entities.Remove (part.UUID);
                            m_child_2_parent_entities.Remove (part.LocalId);
                        }
                        m_objectEntities.Remove (entity.UUID);
                    }
                    else
                         m_presenceEntities.Remove (entity.UUID);
                    return true;
                }
                catch (Exception e)
                {
                    m_log.ErrorFormat ("Remove Entity failed for {0}", entity.UUID, e);
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

        public EntityBase[] GetEntities ()
        {
            List<EntityBase> tmp = new List<EntityBase> (m_entities.Count);
            m_entities.ForEach (delegate (EntityBase entity) { tmp.Add (entity); });
            return tmp.ToArray ();
        }

        public EntityBase[] GetEntities (Vector3 pos, float radius)
        {
            List<EntityBase> tmp = new List<EntityBase> (m_entities.Count);
            m_entities.ForEach (delegate (EntityBase entity)
            { 
                if((entity.AbsolutePosition - pos).LengthSquared() < radius * radius)
                    tmp.Add (entity); 
            });
            return tmp.ToArray ();
        }

        public void ForEach(Action<EntityBase> action)
        {
            m_entities.ForEach(action);
        }

        public IEntity this[UUID id]
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

        public IEntity this[uint localID]
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

        public bool TryGetValue(UUID key, out IEntity obj)
        {
            return InternalTryGetValue(key, true, out obj);
        }

        private bool InternalTryGetValue (UUID key, bool checkRecursive, out IEntity obj)
        {
            IScenePresence presence;
            if (!m_presenceEntities.TryGetValue (key, out presence) && checkRecursive)
            {
                ISceneEntity entity;
                if (!m_objectEntities.TryGetValue (key, out entity) && checkRecursive)
                {
                    //Deal with the possibility we may have been asked for a child prim
                    return TryGetChildPrimParent (key, out obj);
                }
                else obj = entity;
            }
            else obj = presence;
            if (!checkRecursive && obj == null)
                return false;
            return true;
        }

        public bool TryGetValue (uint key, out IEntity obj)
        {
            return InternalTryGetValue(key, true, out obj);
        }

        private bool InternalTryGetValue (uint key, bool checkRecursive, out IEntity obj)
        {
            ISceneEntity entity;
            if (!m_objectEntities.TryGetValue (key, out entity) && checkRecursive)
            {
                //Deal with the possibility we may have been asked for a child prim
                return TryGetChildPrimParent (key, out obj);
            }
            else obj = entity;
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
        public bool TryGetChildPrimParent (UUID childkey, out IEntity obj)
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
        public bool TryGetChildPrimParent (uint childkey, out IEntity obj)
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

        public bool TryGetChildPrim (uint childkey, out ISceneChildEntity child)
        {
            lock (m_lock)
            {
                child = null;

                IEntity entity;
                if (!TryGetChildPrimParent(childkey, out entity))
                    return false;
                if (!(entity is SceneObjectGroup))
                    return false;

                child = (entity as SceneObjectGroup).GetChildPart(childkey);

                return true;
            }
        }

        internal bool TryGetChildPrim (UUID objectID, out ISceneChildEntity childPrim)
        {
            lock (m_lock)
            {
                childPrim = null;

                IEntity entity;
                if (!TryGetChildPrimParent(objectID, out entity))
                    return false;

                childPrim = (entity as SceneObjectGroup).GetChildPart(objectID);

                return true;
            }
        }
    }
}
