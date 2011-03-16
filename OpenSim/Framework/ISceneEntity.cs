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
using OpenMetaverse;

namespace OpenSim.Framework
{
    public delegate void AddPhysics ();
    public interface IScenePresence : IEntity, IRegistryCore
    {
        public event AddPhysics OnAddPhysics;

        string m_callbackURI;
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
        /// The scene this client is in
        /// </summary>
        IScene Scene { get; }

        PhysicsActor PhysicsActor { get; set; }

        /// <summary>
        /// The appearance that this agent has
        /// </summary>
        AvatarAppearance Appearance { get; set; }

        /// <summary>
        /// Is this client really in this region?
        /// </summary>
        bool IsChildAgent { get; set; }

        /// <summary>
        /// Where this client is looking
        /// </summary>
        Vector3 Lookat { get; }

        Vector3 CameraPosition { get; set; }

        Quaternion CameraRotation { get; set; }

        /// <summary>
        /// The offset from an object the avatar may be sitting on
        /// </summary>
        Vector3 OffsetPosition { get; set; }

        /// <summary>
        /// If the avatar is sitting on something, this is the object it is sitting on's UUID
        /// </summary>
        UUID ParentID { get; }

        /// <summary>
        /// Can this entity move?
        /// </summary>
        bool AllowMovement { get; set; }

        bool FlyDisabled { get; set; }

        bool ForceFly { get; set; }

        int GodLevel { get; set; }

        bool SetAlwaysRun { get; set; }

        bool IsBusy { get; set; }

        byte State { get; set; }

        uint AgentControlFlags { get; set; }

        float SpeedModifier { get; set; }

        Vector4 CollisionPlane { get; set; }

        bool Frozen { get; set; }

        int UserLevel { get; set; }

        void Teleport (Vector3 Pos);

        Vector3 lastKnownAllowedPosition { get; set; }

        OpenMetaverse.UUID currentParcelUUID { get; set; }

        bool Invulnerable { get; set; }

        float DrawDistance { get; set; }

        void ChildAgentDataUpdate (AgentData agentData);

        void DoAutoPilot (int p, Vector3 pos, IClientAPI avatar);

        void SendAppearanceToAllOtherAgents ();

        void TeleportWithMomentum (Vector3 value);

        void SendAvatarDataToAllAgents ();

        void SendTerseUpdateToAllClients ();

        void Update ();

        void SendCoarseLocations (List<Vector3> coarseLocations, List<OpenMetaverse.UUID> avatarUUIDs);

        Vector3 CameraAtAxis { get; set; }

        void AddNewMovement (Vector3 jumpForce, Quaternion quaternion);

        Vector3 PreJumpForce { get; set; }

        void AddToPhysicalScene (bool m_flying, bool p);

        void NotInTransit ();

        void CrossSittingAgent (IClientAPI iClientAPI, OpenMetaverse.UUID uUID);

        void DoMoveToPosition (IScenePresence avatar, string p, List<string> coords);

        uint GenerateClientFlags (SceneObjectPart p);

        ScriptControllers GetScriptControler (OpenMetaverse.UUID uUID);

        void UnRegisterControlEventsToScript (uint p, OpenMetaverse.UUID uUID);

        bool IsDeleted { get; set; }

        bool IsJumping { get; set; }

        void PushForce (Vector3 impulse);

        void RegisterScriptController (ScriptControllers SC);

        void SendAppearanceToAgent (IScenePresence sp);

        void SendAvatarDataToAgent (IScenePresence sp);

        void SetHeight (float p);

        bool SitGround { get; set; }

        void StandUp ();

        void ChildAgentDataUpdate (AgentPosition cAgentData, int tRegionX, int tRegionY, int p, int p_2);

        void CopyTo (AgentData agent);

        Vector3 ParentPosition { get; set; }

        void MakeChildAgent ();

        void SendOtherAgentsAppearanceToMe ();

        bool m_InitialHasWearablesBeenSent { get; set; }

        void Close ();

        void AddUpdateToAvatar (ISceneChildEntity entity, PrimUpdateFlags PostUpdateFlags);

        void RegisterControlEventsToScript (int controls, int accept, int pass_on, ISceneChildEntity m_host, OpenMetaverse.UUID m_itemID);
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
        Quaternion Rotation { get; set; }
        string Name { get; }
    }

    public interface ISceneChildEntity : IEntity
    {
        ISceneEntity ParentEntity { get; }
        void ResetEntityIDs ();
    }

    public enum PIDHoverType
    {
        Ground
        ,
        GroundAndWater
            ,
        Water
            , Absolute
    }

    public delegate void PositionUpdate (Vector3 position);
    public delegate void VelocityUpdate (Vector3 velocity);
    public delegate void OrientationUpdate (Quaternion orientation);

    public enum ActorTypes : int
    {
        Unknown = 0,
        Agent = 1,
        Prim = 2,
        Ground = 3,
        Water = 4
    }

    public abstract class PhysicsActor
    {
        public delegate void RequestTerseUpdate ();
        public delegate void CollisionUpdate (EventArgs e);
        public delegate void OutOfBounds (Vector3 pos);

        // disable warning: public events
#pragma warning disable 67
        public event RequestTerseUpdate OnRequestTerseUpdate;
        public event RequestTerseUpdate OnSignificantMovement;
        public event RequestTerseUpdate OnPositionAndVelocityUpdate;
        public event CollisionUpdate OnCollisionUpdate;
        public event OutOfBounds OnOutOfBounds;
#pragma warning restore 67

        public abstract bool Stopped { get; }

        public abstract Vector3 Size { get; set; }

        public abstract PrimitiveBaseShape Shape { set; }

        public abstract uint LocalID { get; set; }

        public abstract bool Grabbed { set; }

        public abstract bool Selected { set; }

        public string SOPName;
        public string SOPDescription;

        public abstract void CrossingFailure ();

        public abstract void link (PhysicsActor obj);

        public abstract void delink ();

        public abstract void LockAngularMotion (Vector3 axis);

        public virtual void RequestPhysicsterseUpdate ()
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            RequestTerseUpdate handler = OnRequestTerseUpdate;

            if (handler != null)
            {
                handler ();
            }
        }

        public virtual void RaiseOutOfBounds (Vector3 pos)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            OutOfBounds handler = OnOutOfBounds;

            if (handler != null)
            {
                handler (pos);
            }
        }

        public virtual void SendCollisionUpdate (EventArgs e)
        {
            CollisionUpdate handler = OnCollisionUpdate;

            if (handler != null)
            {
                handler (e);
            }
        }

        public virtual void TriggerSignificantMovement ()
        {
            //Call significant movement
            RequestTerseUpdate significantMovement = OnSignificantMovement;

            if (significantMovement != null)
                significantMovement ();
        }

        public virtual void TriggerMovementUpdate ()
        {
            //Call significant movement
            RequestTerseUpdate movementUpdate = OnPositionAndVelocityUpdate;

            if (movementUpdate != null)
                movementUpdate ();
        }

        public virtual void SetMaterial (int material)
        {

        }

        public abstract Vector3 Position { get; set; }
        public abstract float Mass { get; set; }
        public abstract Vector3 Force { get; set; }

        public abstract int VehicleType { get; set; }
        public abstract void VehicleFloatParam (int param, float value);
        public abstract void VehicleVectorParam (int param, Vector3 value);
        public abstract void VehicleRotationParam (int param, Quaternion rotation);
        public abstract void VehicleFlags (int param, bool remove);
        public abstract void SetCameraPos (Vector3 CameraRotation);
        public virtual void AddMovementForce (Vector3 force) { }
        public virtual void SetMovementForce (Vector3 force) { }

        public abstract void SetVolumeDetect (int param);    // Allows the detection of collisions with inherently non-physical prims. see llVolumeDetect for more

        public abstract Vector3 GeometricCenter { get; }
        public abstract Vector3 CenterOfMass { get; }
        public abstract Vector3 Velocity { get; set; }
        public abstract Vector3 Torque { get; set; }
        public abstract float CollisionScore { get; set; }
        public abstract Vector3 Acceleration { get; }
        public abstract Quaternion Orientation { get; set; }
        public abstract int PhysicsActorType { get; set; }
        public abstract bool IsPhysical { get; set; }
        public abstract bool Flying { get; set; }
        public abstract bool SetAlwaysRun { get; set; }
        public abstract bool ThrottleUpdates { get; set; }
        public abstract bool IsColliding { get; set; }
        public abstract bool CollidingGround { get; set; }
        public abstract bool CollidingObj { get; set; }
        public abstract bool FloatOnWater { set; }
        public abstract Vector3 RotationalVelocity { get; set; }
        public abstract float Buoyancy { get; set; }

        // Used for MoveTo
        public abstract Vector3 PIDTarget { get; set; }
        public abstract bool PIDActive { get; set; }
        public abstract float PIDTau { get; set; }

        // Used for llSetHoverHeight and maybe vehicle height
        // Hover Height will override MoveTo target's Z
        public abstract bool PIDHoverActive { set; }
        public abstract float PIDHoverHeight { set; }
        public abstract PIDHoverType PIDHoverType { set; }
        public abstract float PIDHoverTau { set; }

        public abstract bool VolumeDetect { get; }

        // For RotLookAt
        public abstract Quaternion APIDTarget { set; }
        public abstract bool APIDActive { set; }
        public abstract float APIDStrength { set; }
        public abstract float APIDDamping { set; }

        public abstract void AddForce (Vector3 force, bool pushforce);
        public abstract void AddAngularForce (Vector3 force, bool pushforce);
        public abstract void SetMomentum (Vector3 momentum);
        public abstract void SubscribeEvents (int ms);
        public abstract void UnSubscribeEvents ();
        public abstract bool SubscribedEvents ();
    }

    public enum ScriptControlled : uint
    {
        CONTROL_ZERO = 0,
        CONTROL_FWD = 1,
        CONTROL_BACK = 2,
        CONTROL_LEFT = 4,
        CONTROL_RIGHT = 8,
        CONTROL_UP = 16,
        CONTROL_DOWN = 32,
        CONTROL_ROT_LEFT = 256,
        CONTROL_ROT_RIGHT = 512,
        CONTROL_LBUTTON = 268435456,
        CONTROL_ML_LBUTTON = 1073741824
    }

    public struct ScriptControllers
    {
        public UUID itemID;
        public ISceneChildEntity part;
        public ScriptControlled ignoreControls;
        public ScriptControlled eventControls;
    }
}
