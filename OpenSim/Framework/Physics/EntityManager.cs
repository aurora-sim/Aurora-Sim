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

namespace OpenSim.Framework
{
    public class EntityManager
    {
        private static readonly ILog m_log = LogManager.GetLogger (MethodBase.GetCurrentMethod ().DeclaringType);
        private readonly Aurora.Framework.DoubleKeyDictionary<UUID, uint, ISceneEntity> m_objectEntities = new Aurora.Framework.DoubleKeyDictionary<UUID, uint, ISceneEntity> ();
        private System.Threading.ReaderWriterLockSlim m_objectEntitiesLock = new System.Threading.ReaderWriterLockSlim (System.Threading.LockRecursionPolicy.SupportsRecursion);
        private readonly Dictionary<UUID, IScenePresence> m_presenceEntities = new Dictionary<UUID, IScenePresence> ();
        private System.Threading.ReaderWriterLockSlim m_presenceEntitiesLock = new System.Threading.ReaderWriterLockSlim (System.Threading.LockRecursionPolicy.SupportsRecursion);
        private readonly Aurora.Framework.DoubleKeyDictionary<UUID, uint, UUID> m_child_2_parent_entities = new Aurora.Framework.DoubleKeyDictionary<UUID, uint, UUID> ();
        private System.Threading.ReaderWriterLockSlim m_child_2_parent_entitiesLock = new System.Threading.ReaderWriterLockSlim (System.Threading.LockRecursionPolicy.SupportsRecursion);

        public int Count
        {
            get { return m_objectEntities.Count + this.GetPresenceCount(); }
        }

        public void Add (IEntity entity)
        {
            if (entity.LocalId == 0)
                return;

            try
            {
                Util.GetWriterLock (m_child_2_parent_entitiesLock);
                Util.GetWriterLock (m_objectEntitiesLock);
                Util.GetWriterLock (m_presenceEntitiesLock);
                if (entity is ISceneEntity)
                {
                    foreach (ISceneChildEntity part in (entity as ISceneEntity).ChildrenEntities ())
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
            catch (Exception e)
            {
                m_log.ErrorFormat ("Add Entity failed: {0}", e.Message);
            }
            finally
            {
                Util.ReleaseWriterLock (m_child_2_parent_entitiesLock);
                Util.ReleaseWriterLock (m_objectEntitiesLock);
                Util.ReleaseWriterLock (m_presenceEntitiesLock);
            }
        }

        public void Clear ()
        {
            Util.GetWriterLock (m_objectEntitiesLock);
            m_objectEntities.Clear ();
            Util.ReleaseWriterLock (m_objectEntitiesLock);
            Util.GetWriterLock (m_presenceEntitiesLock);
            m_presenceEntities.Clear ();
            Util.ReleaseWriterLock (m_presenceEntitiesLock);
            Util.GetWriterLock (m_child_2_parent_entitiesLock);
            m_child_2_parent_entities.Clear ();
            Util.ReleaseWriterLock (m_child_2_parent_entitiesLock);
        }

        public bool Remove (IEntity entity)
        {
            if (entity == null)
                return false;

            try
            {
                Util.GetWriterLock (m_child_2_parent_entitiesLock);
                Util.GetWriterLock (m_objectEntitiesLock);
                Util.GetWriterLock (m_presenceEntitiesLock);
                if (entity is ISceneEntity)
                {
                    //Remove all child entities
                    foreach (ISceneChildEntity part in (entity as ISceneEntity).ChildrenEntities ())
                    {
                        m_child_2_parent_entities.Remove (part.UUID);
                        m_child_2_parent_entities.Remove (part.LocalId);
                    }
                    m_objectEntities.Remove (entity.UUID);
                    m_objectEntities.Remove (entity.LocalId);
                }
                else
                {
                    m_presenceEntities.Remove (entity.UUID);
                }
                return true;
            }
            catch (Exception e)
            {
                m_log.ErrorFormat ("Remove Entity failed for {0}", entity.UUID, e);
                return false;
            }
            finally
            {
                Util.ReleaseWriterLock (m_objectEntitiesLock);
                Util.ReleaseWriterLock (m_child_2_parent_entitiesLock);
                Util.ReleaseWriterLock (m_presenceEntitiesLock);
            }
        }

        public IScenePresence[] GetPresences()
        {
            try
            {
                Util.GetReaderLock (m_presenceEntitiesLock);
                return new List<IScenePresence> (m_presenceEntities.Values).ToArray ();
            }
            finally
            {
                Util.ReleaseReaderLock (m_presenceEntitiesLock);
            }
        }

        public int GetPresenceCount ()
        {
            try
            {
                Util.GetReaderLock (m_presenceEntitiesLock);
                return m_presenceEntities.Count;
            }
            finally
            {
                Util.ReleaseReaderLock (m_presenceEntitiesLock);
            }
        }

        public IScenePresence[] GetPresences (Vector3 pos, float radius)
        {
            Util.GetReaderLock (m_presenceEntitiesLock);
            List<IScenePresence> tmp = new List<IScenePresence> (m_presenceEntities.Count);

            foreach (IScenePresence entity in m_presenceEntities.Values)
            {
                if ((entity.AbsolutePosition - pos).LengthSquared () < radius * radius)
                    tmp.Add (entity);
            }
            Util.ReleaseReaderLock (m_presenceEntitiesLock);
            return tmp.ToArray ();
        }

        public ISceneEntity[] GetEntities ()
        {
            Util.GetReaderLock (m_objectEntitiesLock);
            List<ISceneEntity> tmp = new List<ISceneEntity> (m_objectEntities.Count);
            m_objectEntities.ForEach (delegate (ISceneEntity entity)
            {
                tmp.Add (entity);
            });
            Util.ReleaseReaderLock (m_objectEntitiesLock);
            return tmp.ToArray ();
        }

        public ISceneEntity[] GetEntities (Vector3 pos, float radius)
        {
            Util.GetReaderLock (m_objectEntitiesLock);
            List<ISceneEntity> tmp = new List<ISceneEntity> (m_objectEntities.Count);

            m_objectEntities.ForEach (delegate (ISceneEntity entity)
            {
                //Add attachments as well, as they might be needed
                if ((entity.AbsolutePosition - pos).LengthSquared () < radius * radius || entity.IsAttachment)
                    tmp.Add (entity);
            });
            Util.ReleaseReaderLock (m_objectEntitiesLock);
            return tmp.ToArray ();
        }

        public bool TryGetPresenceValue (UUID key, out IScenePresence presence)
        {
            try
            {
                Util.GetReaderLock (m_presenceEntitiesLock);
                return m_presenceEntities.TryGetValue (key, out presence);
            }
            finally
            {
                Util.ReleaseReaderLock (m_presenceEntitiesLock);
            }
        }

        public bool TryGetValue (UUID key, out IEntity obj)
        {
            return InternalTryGetValue (key, true, out obj);
        }

        private bool InternalTryGetValue (UUID key, bool checkRecursive, out IEntity obj)
        {
            IScenePresence presence;
            bool gotit;
            Util.GetReaderLock (m_presenceEntitiesLock);
            gotit = m_presenceEntities.TryGetValue (key, out presence);
            Util.ReleaseReaderLock (m_presenceEntitiesLock);
            if (!gotit)
            {
                ISceneEntity presence2;
                Util.GetReaderLock (m_objectEntitiesLock);
                gotit = m_objectEntities.TryGetValue (key, out presence2);
                Util.ReleaseReaderLock (m_objectEntitiesLock);

                //Deal with the possibility we may have been asked for a child prim
                if ((!gotit) && checkRecursive)
                    return TryGetChildPrimParent (key, out obj);
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

        public bool TryGetValue (uint key, out IEntity obj)
        {
            return InternalTryGetValue (key, true, out obj);
        }

        private bool InternalTryGetValue (uint key, bool checkRecursive, out IEntity obj)
        {
            ISceneEntity entity;
            bool gotit;
            Util.GetReaderLock (m_objectEntitiesLock);
            gotit = m_objectEntities.TryGetValue (key, out entity);
            Util.ReleaseReaderLock (m_objectEntitiesLock);

            //Deal with the possibility we may have been asked for a child prim
            if (!gotit && checkRecursive)
                return TryGetChildPrimParent (key, out obj);
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
        /// Retrives the SceneObjectGroup of this child
        /// </summary>
        /// <param name="childkey"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool TryGetChildPrimParent (UUID childkey, out IEntity obj)
        {
            UUID ParentKey = UUID.Zero;
            bool gotit;
            Util.GetReaderLock (m_child_2_parent_entitiesLock);
            gotit = m_child_2_parent_entities.TryGetValue (childkey, out ParentKey);
            Util.ReleaseReaderLock (m_child_2_parent_entitiesLock);

            if (gotit)
                return InternalTryGetValue (ParentKey, false, out obj);

            obj = null;
            return false;
        }

        /// <summary>
        /// Retrives the SceneObjectGroup of this child
        /// </summary>
        /// <param name="childkey"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool TryGetChildPrimParent (uint childkey, out IEntity obj)
        {
            bool gotit;
            UUID ParentKey = UUID.Zero;
            Util.GetReaderLock (m_child_2_parent_entitiesLock);
            gotit = m_child_2_parent_entities.TryGetValue (childkey, out ParentKey);
            Util.ReleaseReaderLock (m_child_2_parent_entitiesLock);

            if (gotit)
                return InternalTryGetValue (ParentKey, false, out obj);

            obj = null;
            return false;
        }

        public bool TryGetChildPrim (uint childkey, out ISceneChildEntity child)
        {
            child = null;

            IEntity entity;
            if (!TryGetChildPrimParent (childkey, out entity))
                return false;
            if (!(entity is ISceneEntity))
                return false;

            child = (entity as ISceneEntity).GetChildPart (childkey);

            return true;
        }

        internal bool TryGetChildPrim (UUID objectID, out ISceneChildEntity childPrim)
        {
            childPrim = null;

            IEntity entity;
            if (!TryGetChildPrimParent (objectID, out entity))
                return false;

            childPrim = (entity as ISceneEntity).GetChildPart (objectID);

            return true;
        }
    }
}
