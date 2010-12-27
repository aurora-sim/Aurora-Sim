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
using System.Runtime.Serialization;
using System.Security.Permissions;
using OpenSim.Framework;
using OpenMetaverse;

namespace OpenSim.Region.Framework.Scenes
{
    public abstract class EntityBase : ISceneEntity
    {
        protected Scene m_scene;
        /// <summary>
        /// The scene to which this entity belongs
        /// </summary>
        public Scene Scene
        {
            get { return m_scene; }
            set { m_scene = value; }
        }

        protected UUID m_uuid;

        public virtual UUID UUID
        {
            get { return m_uuid; }
            set { m_uuid = value; }
        }

        protected string m_name;

        /// <summary>
        /// The name of this entity
        /// </summary>
        public virtual string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        /// <summary>
        /// Signals whether this entity was in a scene but has since been removed from it.
        /// </summary>
        public bool IsDeleted
        {
            get { return m_isDeleted; }
            set { m_isDeleted = value; }
        }
        protected bool m_isDeleted;

        public int LinkNum
        {
            get
            {
                return m_linkNum;
            }
            set
            {
                m_linkNum = value;
            }
        }
        private int m_linkNum;

        public virtual bool HasGroupChanged
        {
            get { return m_hasGroupChanged; }
            set { m_hasGroupChanged = value; }
        }
        /// <summary>
        /// Signal whether the non-inventory attributes of any prims in the group have changed
        /// since the group's last persistent backup
        /// </summary>
        protected bool m_hasGroupChanged = false;

        /// <summary>
        /// Force this prim to be added to backup
        /// </summary>
        public virtual void ForcePersistence()
        {
            HasGroupChanged = true;
        }

        protected Vector3 m_pos;

        /// <summary>
        ///
        /// </summary>
        public virtual Vector3 AbsolutePosition
        {
            get { return m_pos; }
            set { m_pos = value; }
        }

        /// <summary>
        /// Current velocity of the entity.
        /// </summary>
        public virtual Vector3 Velocity
        {
            get { return Vector3.Zero; }
            set { }
        }

        protected uint m_localId;

        public virtual uint LocalId
        {
            get { return m_localId; }
            set { m_localId = value; }
        }

        public virtual bool IsPhysical()
        {
            return false;
        }

        /// <summary>
        /// Creates a new Entity (should not occur on it's own)
        /// </summary>
        public EntityBase()
        {
            m_name = "(basic entity)";
        }

        /// <summary>
        /// Performs any updates that need to be done at each frame, as opposed to immediately.
        /// These included scheduled updates and updates that occur due to physics processing.
        /// </summary>
        public abstract void Update();

        /// <summary>
        /// Copies the entity
        /// </summary>
        /// <returns></returns>
        public virtual EntityBase Copy()
        {
            return (EntityBase) MemberwiseClone();
        }

        public virtual List<ISceneEntity> ChildrenEntities()
        {
            return new List<ISceneEntity>();
        }

        public virtual void ResetEntityIDs()
        {
        }

        public virtual void AttachToScene(Scene m_parentScene)
        {
        }

        public virtual void ClearChildren()
        {
        }

        public virtual bool AddChild(ISceneEntity child, int linkNum)
        {
            return false;
        }

        public virtual bool LinkChild(ISceneEntity child)
        {
            return false;
        }

        public virtual bool RemoveChild(ISceneEntity child)
        {
            return false;
        }

        public virtual bool GetChildPrim(uint LocalID, out ISceneEntity entity)
        {
            entity = null;
            return false;
        }

        public virtual bool GetChildPrim(UUID UUID, out ISceneEntity entity)
        {
            entity = null;
            return false;
        }

        public virtual void ClearUndoState()
        {
        }

        public virtual void RebuildPhysicalRepresentation()
        {
        }
    }

    //Nested Classes
    public class EntityIntersection
    {
        public Vector3 ipoint = new Vector3(0, 0, 0);
        public Vector3 normal = new Vector3(0, 0, 0);
        public Vector3 AAfaceNormal = new Vector3(0, 0, 0);
        public int face = -1;
        public bool HitTF = false;
        public SceneObjectPart obj;
        public float distance = 0;

        public EntityIntersection()
        {
        }

        public EntityIntersection(Vector3 _ipoint, Vector3 _normal, bool _HitTF)
        {
            ipoint = _ipoint;
            normal = _normal;
            HitTF = _HitTF;
        }
    }
}
