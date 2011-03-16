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

using System.Collections.Generic;
using OpenMetaverse;

namespace OpenSim.Framework
{
    public interface IScenePresence : IEntity
    {
        /// <summary>
        /// First name of the client
        /// </summary>
        string Firstname { get; }

        /// <summary>
        /// Last name of the client
        /// </summary>
        string Lastname { get; }

        /// <summary>
        /// The actual client base (it sends and recieves packets)
        /// </summary>
        IClientAPI ControllingClient { get; }

        /// <summary>
        /// Is this client really in this region?
        /// </summary>
        bool IsChildAgent { get; set; }

        /// <summary>
        /// Where this client is looking
        /// </summary>
        Vector3 Lookat { get; }
    }

    public interface ISceneObject : ISceneEntity
    {
        /// <summary>
        /// Returns an XML based document that represents this object
        /// </summary>
        /// <returns></returns>
        string ToXml2 ();

        /// <summary>
        /// Adds the FromInventoryItemID to the xml
        /// </summary>
        /// <returns></returns>
        string ExtraToXmlString ();
        void ExtraFromXmlString (string xmlstr);

        /// <summary>
        /// State snapshots (for script state transfer)
        /// </summary>
        /// <returns></returns>
        string GetStateSnapshot ();
        void SetState (string xmlstr);
    }

    public interface ISceneEntity : IEntity
    {
        bool IsDeleted { get; set; }
        Vector3 GroupScale ();
        Quaternion GroupRotation { get; }
        List<ISceneChildEntity> ChildrenEntities ();
        void ClearChildren ();
        bool AddChild (ISceneChildEntity child, int linkNum);
        bool LinkChild (ISceneChildEntity child);
        bool RemoveChild (ISceneChildEntity child);
        bool GetChildPrim (uint LocalID, out ISceneChildEntity entity);
        bool GetChildPrim (UUID UUID, out ISceneChildEntity entity);

        void ClearUndoState ();

        void AttachToScene (IScene m_parentScene);

        ISceneEntity Copy (bool copyPhysicsRepresentation);

        void ForcePersistence ();

        void ApplyPhysics (bool allowPhysicalPrims);
    }

    public interface IEntity
    {
        UUID UUID { get; set; }
        uint LocalId { get; set; }
        int LinkNum { get; set; }
        Vector3 AbsolutePosition { get; set; }
        Vector3 Velocity { get; set; }
        string Name { get; }
    }

    public interface ISceneChildEntity : IEntity
    {
        ISceneEntity ParentEntity { get; }
        void ResetEntityIDs ();
    }
}
