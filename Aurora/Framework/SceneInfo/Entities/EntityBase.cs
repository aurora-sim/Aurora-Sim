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

using OpenMetaverse;

namespace Aurora.Framework
{
    public abstract class EntityBase : RegistryCore, IEntity
    {
        /// <summary>
        ///     Signal whether the non-inventory attributes of any prims in the group have changed
        ///     since the group's last persistent backup
        /// </summary>
        protected bool m_hasGroupChanged;

        protected bool m_isDeleted;
        protected uint m_localId;
        protected string m_name;
        protected Vector3 m_pos;
        protected Quaternion m_rot;

        protected IScene m_scene;

        protected UUID m_uuid;

        /// <summary>
        ///     Creates a new Entity (should not occur on it's own)
        /// </summary>
        public EntityBase()
        {
            m_name = "(basic entity)";
        }

        /// <summary>
        ///     The scene to which this entity belongs
        /// </summary>
        public IScene Scene
        {
            get { return m_scene; }
            set { m_scene = value; }
        }

        /// <summary>
        ///     Signals whether this entity was in a scene but has since been removed from it.
        /// </summary>
        public bool IsDeleted
        {
            get { return m_isDeleted; }
            set { m_isDeleted = value; }
        }

        public virtual bool HasGroupChanged
        {
            get { return m_hasGroupChanged; }
            set { m_hasGroupChanged = value; }
        }

        #region IEntity Members

        public virtual UUID UUID
        {
            get { return m_uuid; }
            set { m_uuid = value; }
        }

        /// <summary>
        ///     The name of this entity
        /// </summary>
        public virtual string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        public int LinkNum { get; set; }

        /// <summary>
        /// </summary>
        public virtual Vector3 AbsolutePosition
        {
            get { return m_pos; }
            set { m_pos = value; }
        }

        /// <summary>
        /// </summary>
        public virtual Quaternion Rotation
        {
            get { return m_rot; }
            set { m_rot = value; }
        }

        /// <summary>
        ///     Current velocity of the entity.
        /// </summary>
        public virtual Vector3 Velocity
        {
            get { return Vector3.Zero; }
            set { }
        }

        public virtual uint LocalId
        {
            get { return m_localId; }
            set { m_localId = value; }
        }

        #endregion
    }

    //Nested Classes
    public class EntityIntersection
    {
        public Vector3 AAfaceNormal = new Vector3(0, 0, 0);
        public bool HitTF;
        public float distance;
        public int face = -1;
        public Vector3 ipoint = new Vector3(0, 0, 0);
        public Vector3 normal = new Vector3(0, 0, 0);
        public IEntity obj;

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