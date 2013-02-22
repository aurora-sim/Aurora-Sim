using System;
using System.Collections.Generic;
using System.Linq;
using OpenMetaverse;
using Aurora.Framework;

namespace OpenSim.Region.Framework.Scenes
{
    public class AsyncEntityManager : EntityManager
    {
        protected readonly object m_changeLock = new object();
        protected volatile bool m_isChanging;

        public override bool Add(IEntity entity)
        {
            if (entity.LocalId == 0)
            {
                MainConsole.Instance.Warn("Entity with 0 localID!");
                return false;
            }
            m_isChanging = true;
            lock (m_changeLock)
            {
                try
                {
                    if (entity is ISceneEntity)
                    {
                        foreach (ISceneChildEntity part in (entity as ISceneEntity).ChildrenEntities())
                        {
                            m_child_2_parent_entities.Remove(part.UUID);
                            m_child_2_parent_entities.Remove(part.LocalId);
                            m_child_2_parent_entities.Add(part.UUID, part.LocalId, entity.UUID);
                        }
                        m_objectEntities.Add(entity.UUID, entity.LocalId, entity as ISceneEntity);
                    }
                    else
                    {
                        IScenePresence presence = (IScenePresence) entity;
                        m_presenceEntities.Add(presence.UUID, presence);
                        m_presenceEntitiesList.Add(presence);
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.ErrorFormat("Add Entity failed: {0}", e.Message);
                }
            }
            m_isChanging = false;
            return true;
        }

        public override bool Remove(IEntity entity)
        {
            if (entity == null)
                return false;

            m_isChanging = true;
            lock (m_changeLock)
            {
                try
                {
                    if (entity is ISceneEntity)
                    {
                        //Remove all child entities
                        foreach (ISceneChildEntity part in (entity as ISceneEntity).ChildrenEntities())
                        {
                            m_child_2_parent_entities.Remove(part.UUID);
                            m_child_2_parent_entities.Remove(part.LocalId);
                        }
                        m_objectEntities.Remove(entity.UUID);
                        m_objectEntities.Remove(entity.LocalId);
                    }
                    else
                    {
                        m_presenceEntitiesList.Remove((IScenePresence) entity);
                        m_presenceEntities.Remove(entity.UUID);
                    }
                }
                catch (Exception e)
                {
                    MainConsole.Instance.ErrorFormat("Remove Entity failed for {0}", entity.UUID, e);
                }
            }
            m_isChanging = false;
            return true;
        }

        public override void Clear()
        {
            if (m_isChanging)
            {
                lock (m_changeLock)
                {
                    m_objectEntities.Clear();
                    m_presenceEntitiesList.Clear();
                    m_presenceEntities.Clear();
                    m_child_2_parent_entities.Clear();
                }
            }
            else
            {
                m_objectEntities.Clear();
                m_presenceEntitiesList.Clear();
                m_presenceEntities.Clear();
                m_child_2_parent_entities.Clear();
            }
        }

        public override ISceneEntity[] GetEntities()
        {
            if (m_isChanging)
            {
                lock (m_changeLock)
                {
                    List<ISceneEntity> tmp = new List<ISceneEntity>(m_objectEntities.Count);
                    m_objectEntities.ForEach(tmp.Add);
                    return tmp.ToArray();
                }
            }
            else
            {
                List<ISceneEntity> tmp = new List<ISceneEntity>(m_objectEntities.Count);
                m_objectEntities.ForEach(tmp.Add);
                return tmp.ToArray();
            }
        }

        public override ISceneEntity[] GetEntities(Vector3 pos, float radius)
        {
            if (m_isChanging)
            {
                lock (m_changeLock)
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
            else
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

        public override IScenePresence[] GetPresences(Vector3 pos, float radius)
        {
            if (m_isChanging)
            {
                lock (m_changeLock)
                {
                    List<IScenePresence> tmp = new List<IScenePresence>(m_presenceEntities.Count);
#if (!ISWIN)
                    foreach (IScenePresence entity in m_presenceEntities.Values)
                    {
                        if ((entity.AbsolutePosition - pos).LengthSquared() < radius * radius)
                            tmp.Add(entity);
                    }
#else
                    tmp.AddRange(m_presenceEntities.Values.Where(entity => (entity.AbsolutePosition - pos).LengthSquared() < radius*radius));
#endif

                    return tmp.ToArray();
                }
            }
            else
            {
                List<IScenePresence> tmp = new List<IScenePresence>(m_presenceEntities.Count);
#if (!ISWIN)
                foreach (IScenePresence entity in m_presenceEntities.Values)
                {
                    if ((entity.AbsolutePosition - pos).LengthSquared() < radius * radius)
                        tmp.Add(entity);
                }
#else
                tmp.AddRange(m_presenceEntities.Values.Where(entity => (entity.AbsolutePosition - pos).LengthSquared() < radius*radius));
#endif

                return tmp.ToArray();
            }
        }

        protected override bool InternalTryGetValue(UUID key, bool checkRecursive, out IEntity obj)
        {
            if (m_isChanging)
            {
                lock (m_changeLock)
                {
                    return InnerInternalTryGetValue(ref key, checkRecursive, out obj);
                }
            }
            else
                return InnerInternalTryGetValue(ref key, checkRecursive, out obj);
        }

        private bool InnerInternalTryGetValue(ref UUID key, bool checkRecursive, out IEntity obj)
        {
            IScenePresence presence;
            bool gotit = m_presenceEntities.TryGetValue(key, out presence);
            if (!gotit)
            {
                ISceneEntity presence2;
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

        protected override bool InternalTryGetValue(uint key, bool checkRecursive, out IEntity obj)
        {
            if (m_isChanging)
            {
                lock (m_changeLock)
                {
                    return InnerInternalTryGetValue(key, checkRecursive, out obj);
                }
            }
            else
                return InnerInternalTryGetValue(key, checkRecursive, out obj);
        }

        private bool InnerInternalTryGetValue(uint key, bool checkRecursive, out IEntity obj)
        {
            ISceneEntity entity;
            bool gotit = m_objectEntities.TryGetValue(key, out entity);

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

        public override bool TryGetChildPrimParent(UUID childkey, out IEntity obj)
        {
            if (m_isChanging)
            {
                lock (m_changeLock)
                {
                    return InnerTryGetChildPrimParent(ref childkey, out obj);
                }
            }
            else
                return InnerTryGetChildPrimParent(ref childkey, out obj);
        }

        public override bool TryGetChildPrimParent(uint childkey, out IEntity obj)
        {
            if (m_isChanging)
            {
                lock (m_changeLock)
                {
                    return InnerTryGetChildPrimParent(childkey, out obj);
                }
            }
            else
                return InnerTryGetChildPrimParent(childkey, out obj);
        }

        private bool InnerTryGetChildPrimParent(uint childkey, out IEntity obj)
        {
            UUID ParentKey = UUID.Zero;
            bool gotit = m_child_2_parent_entities.TryGetValue(childkey, out ParentKey);

            if (gotit)
                return InternalTryGetValue(ParentKey, false, out obj);

            obj = null;
            return false;
        }

        private bool InnerTryGetChildPrimParent(ref UUID childkey, out IEntity obj)
        {
            UUID ParentKey = UUID.Zero;
            bool gotit = m_child_2_parent_entities.TryGetValue(childkey, out ParentKey);

            if (gotit)
                return InternalTryGetValue(ParentKey, false, out obj);

            obj = null;
            return false;
        }

        public override bool TryGetPresenceValue(UUID key, out IScenePresence presence)
        {
            if (m_isChanging)
            {
                lock (m_changeLock)
                {
                    return m_presenceEntities.TryGetValue(key, out presence);
                }
            }
            else
                return m_presenceEntities.TryGetValue(key, out presence);
        }
    }
}