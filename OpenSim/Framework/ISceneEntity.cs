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
using System.Net;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Client;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using OpenMetaverse.StructuredData;
using OpenSim.Framework.Servers.HttpServer;

namespace OpenSim.Framework
{
    public delegate void AddPhysics ();
    public delegate void RemovePhysics ();
    public interface IScenePresence : IEntity, IRegistryCore
    {
        event AddPhysics OnAddPhysics;

        event RemovePhysics OnRemovePhysics;
        IScene Scene { get; set; }

        string CallbackURI { get; set; }
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

        ISceneViewer SceneViewer { get; }

        IAnimator Animator { get; }

        PhysicsActor PhysicsActor { get; set; }

        /// <summary>
        /// Is this client really in this region?
        /// </summary>
        bool IsChildAgent { get; set; }

        /// <summary>
        /// Where this client is looking
        /// </summary>
        Vector3 Lookat { get; }

        Vector3 CameraPosition { get; }

        Quaternion CameraRotation { get; }

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

        int UserLevel { get; }

        void Teleport (Vector3 Pos);

        Vector3 LastKnownAllowedPosition { get; set; }

        UUID CurrentParcelUUID { get; set; }

        bool Invulnerable { get; set; }

        float DrawDistance { get; set; }
        Vector3 CameraAtAxis { get; }

        Vector3 PreJumpForce { get; set; }

        bool IsDeleted { get; set; }

        bool IsJumping { get; set; }

        void PushForce (Vector3 impulse);
        bool SitGround { get; set; }
        Vector3 ParentPosition { get; set; }


        void Update ();
        void ChildAgentDataUpdate (AgentData agentData);
        void ChildAgentDataUpdate (AgentPosition cAgentData, int tRegionX, int tRegionY, int p, int p_2);
        void CopyTo (AgentData agent);
        void MakeRootAgent(bool isFlying);
        void MakeChildAgent ();
        void Close ();


        void DoAutoPilot (uint p, Vector3 pos, IClientAPI avatar);
        void TeleportWithMomentum (Vector3 value);
        void DoMoveToPosition (object iClientAPI, string p, List<string> coords);
        void NotInTransit ();
        void CrossSittingAgent (IClientAPI iClientAPI, OpenMetaverse.UUID uUID);
        void AddNewMovement (Vector3 jumpForce, Quaternion quaternion);
        void AddToPhysicalScene (bool m_flying, bool p);

        void SendTerseUpdateToAllClients ();
        uint GenerateClientFlags (ISceneChildEntity p);

        void SendCoarseLocations (List<Vector3> coarseLocations, List<OpenMetaverse.UUID> avatarUUIDs);

        void AddUpdateToAvatar (ISceneChildEntity entity, PrimUpdateFlags PostUpdateFlags);


        void StandUp ();

        void SetHeight (float p);
    }

    public interface IAvatarAppearanceModule
    {
        /// <summary>
        /// The appearance that this agent has
        /// </summary>
        AvatarAppearance Appearance { get; set; }
        bool InitialHasWearablesBeenSent { get; set; }
        void SendAppearanceToAgent (IScenePresence sp);
        void SendAvatarDataToAgent (IScenePresence sp);
        void SendOtherAgentsAppearanceToMe ();
        void SendAppearanceToAllOtherAgents ();
        void SendAvatarDataToAllAgents ();
    }

    public interface IScriptControllerModule
    {
        ScriptControllers GetScriptControler (UUID uUID);

        void RegisterScriptController (ScriptControllers SC);

        void UnRegisterControlEventsToScript (uint p, UUID uUID);

        void RegisterControlEventsToScript (int controls, int accept, int pass_on, ISceneChildEntity m_host, UUID m_itemID);

        void OnNewMovement (ref AgentManager.ControlFlags flags);

        void RemoveAllScriptControllers (ISceneChildEntity part);

        ControllerData[] Serialize ();

        void Deserialize (ControllerData[] controllerData);
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
    }

    public interface ISceneEntity : IEntity
    {
        #region Get/Set

        IScene Scene { get; set; }
        UUID LastParcelUUID { get; set; }
        Vector3 LastSignificantPosition{ get; }
        bool IsDeleted { get; set; }
        Vector3 GroupScale ();
        Quaternion GroupRotation { get; }
        UUID OwnerID { get; set; }
        float Damage { get; set; }
        int PrimCount { get; }
        bool HasGroupChanged { get; set; }
        bool IsAttachment { get; }
        UUID GroupID { get; set; }
        bool IsSelected { get; set; }
        ISceneChildEntity LoopSoundMasterPrim { get; set; }
        List<ISceneChildEntity> LoopSoundSlavePrims { get; set; }

        #endregion

        #region Children

        ISceneChildEntity RootChild { get; set; }
        List<ISceneChildEntity> ChildrenEntities ();
        void ClearChildren ();
        bool AddChild (ISceneChildEntity child, int linkNum);
        bool LinkChild (ISceneChildEntity child);
        bool RemoveChild (ISceneChildEntity child);
        bool GetChildPrim (uint LocalID, out ISceneChildEntity entity);
        bool GetChildPrim (UUID UUID, out ISceneChildEntity entity);
        ISceneChildEntity GetChildPart (UUID objectID);
        ISceneChildEntity GetChildPart (uint childkey);
        void LinkToGroup (ISceneEntity childPrim);
        IEntity GetLinkNumPart (int linkType);

        #endregion

        void ClearUndoState ();

        void AttachToScene (IScene m_parentScene);

        ISceneEntity Copy (bool copyPhysicsRepresentation);

        void ForcePersistence ();

        void ApplyPhysics (bool allowPhysicalPrims);


        void ScheduleGroupTerseUpdate ();


        

        void TriggerScriptChangedEvent (Changed changed);


        void ScheduleGroupUpdate (PrimUpdateFlags primUpdateFlags);

        void GetProperties (IClientAPI client);

        ISceneEntity DelinkFromGroup (ISceneChildEntity part, bool p);

        void UpdateGroupPosition (Vector3 vector3, bool p);

        void ResetChildPrimPhysicsPositions ();

        Vector3 GetAttachmentPos ();

        byte GetAttachmentPoint ();

        byte GetSavedAttachmentPoint ();

        void SetAttachmentPoint (byte p);

        void CreateScriptInstances (int p, bool p_2, int p_3, OpenMetaverse.UUID uUID);

        void ResumeScripts ();

        void SetFromItemID (OpenMetaverse.UUID uUID);

        void FireAttachmentCollisionEvents (EventArgs e);

        void DetachToInventoryPrep ();

        TaskInventoryItem GetInventoryItem (uint localID, OpenMetaverse.UUID itemID);

        int RemoveInventoryItem (uint localID, OpenMetaverse.UUID itemID);

        bool AddInventoryItem (IClientAPI remoteClient, uint primLocalID, InventoryItemBase item, OpenMetaverse.UUID copyID);

        void ScheduleGroupUpdateToAvatar (IScenePresence SP, PrimUpdateFlags primUpdateFlags);

        void SetOwnerId (OpenMetaverse.UUID uUID);

        uint GetEffectivePermissions ();

        void SetRootPartOwner (ISceneChildEntity part, OpenMetaverse.UUID uUID, OpenMetaverse.UUID uUID_2);

        void SetGroup (OpenMetaverse.UUID groupID, IClientAPI remoteClient);

        void ApplyNextOwnerPermissions ();

        bool UpdateInventoryItem (TaskInventoryItem item);

        void DetachToGround ();

        void UpdatePermissions (OpenMetaverse.UUID agentID, byte field, uint localId, uint mask, byte set);

        float BSphereRadiusSQ { get; }

        /// <summary>
        /// Prepares the object to be serialized
        /// </summary>
        void BackupPreparation();
    }

    public interface IEntity
    {
        UUID UUID { get; set; }
        uint LocalId { get; set; }
        int LinkNum { get; set; }
        Vector3 AbsolutePosition { get; set; }
        Vector3 Velocity { get; set; }
        Quaternion Rotation { get; set; }
        string Name { get; set; }
    }

    public interface ISceneChildEntity : IEntity
    {
        ISceneEntity ParentEntity { get; }
        IEntityInventory Inventory { get; }
        void ResetEntityIDs ();

        string GenericData { get; }

        PrimFlags Flags { get; set; }

        int UseSoundQueue { get; set; }

        UUID OwnerID { get; set; }
        UUID LastOwnerID { get; set; }

        bool VolumeDetectActive { get; set; }

        void UpdatePrimFlags (bool p, bool wasTemporary, bool wasPhantom, bool wasVD);

        UUID GroupID { get; set; }

        UUID CreatorID { get; set; }

        Quaternion GetWorldRotation ();

        PhysicsActor PhysActor { get; set; }

        TaskInventoryDictionary TaskInventory { get; set; }

        Vector3 GetWorldPosition ();

        void RemoveAvatarOnSitTarget (OpenMetaverse.UUID UUID);

        List<UUID> GetAvatarOnSitTarget ();

        OpenMetaverse.UUID ParentUUID { get; set; }

        float GetMass ();

        int AttachmentPoint { get; set; }

        bool CreateSelected { get; set; }

        bool IsAttachment { get; set; }

        void ApplyImpulse (Vector3 applied_linear_impulse, bool p);

        string Description { get; set; }

        Quaternion RotationOffset { get; set; }

        void ScheduleUpdate (PrimUpdateFlags primUpdateFlags);

        string SitAnimation { get; set; }

        Vector3 SitTargetPosition { get; set; }

        Quaternion SitTargetOrientation { get; set; }

        void SetAvatarOnSitTarget (OpenMetaverse.UUID UUID);

        PrimType GetPrimType ();

        Vector3 CameraAtOffset { get; set; }

        Vector3 CameraEyeOffset { get; set; }

        bool ForceMouselook { get; set; }

        Vector3 Scale { get; set; }

        uint GetEffectiveObjectFlags ();

        int GetNumberOfSides ();

        string Text { get; set; }

        Color4 GetTextColor ();

        PrimitiveBaseShape Shape { get; set; }

        uint ParentID { get; set; }

        int Material { get; set; }

        UUID AttachedAvatar { get; set; }

        uint OwnerMask { get; set; }

        uint GroupMask { get; set; }

        uint EveryoneMask { get; set; }

        void SetScriptEvents (UUID ItemID, long events);

        UUID FromUserInventoryItemID { get; set; }

        Vector3 AngularVelocity { get; set; }

        void SetParentLocalId (uint p);

        void SetParent (ISceneEntity grp);

        Vector3 OffsetPosition { get; }

        Vector3 AttachedPos { get; set; }

        bool IsRoot { get; }

        void SetConeOfSilence (double p);

        byte SoundFlags { get; set; }

        double SoundGain { get; set; }

        OpenMetaverse.UUID Sound { get; set; }

        double SoundRadius { get; set; }

        void SendSound (string p, double volume, bool p_2, byte p_3, float p_4, bool p_5, bool p_6);

        void PreloadSound (string sound);

        void SetBuoyancy (float p);

        void SetHoverHeight (float p, PIDHoverType hoverType, float p_2);

        void ScheduleTerseUpdate ();

        void StopLookAt ();

        void RotLookAt (Quaternion rot, float p, float p_2);

        void startLookAt (Quaternion rotation, float p, float p_2);

        Vector3 Acceleration { get; set; }

        Vector3 GetTorque ();

        void SetAngularImpulse (Vector3 vector3, bool p);

        void ApplyAngularImpulse (Vector3 vector3, bool p);

        void MoveToTarget (Vector3 vector3, float p);

        void StopMoveToTarget ();

        void unregisterRotTargetWaypoint (int number);

        int registerRotTargetWaypoint (Quaternion quaternion, float p);

        Vector3 GetForce ();

        bool AddFlag (PrimFlags primFlags);

        void AdjustSoundGain (double volume);

        uint BaseMask { get; set; }

        byte ClickAction { get; set; }

        OpenMetaverse.UUID CollisionSound { get; set; }

        float CollisionSoundVolume { get; set; }

        OpenMetaverse.UUID CollisionSprite { get; set; }

        int GetAxisRotation (int p);

        bool GetDieAtEdge ();

        Vector3 GetGeometricCenter ();

        bool GetReturnAtEdge ();

        bool GetStatusSandbox ();

        uint NextOwnerMask { get; set; }

        void SetVehicleType (int type);

        void SetVehicleVectorParam (int param, Vector3 vector3);

        void SetVehicleRotationParam (int param, Quaternion quaternion);

        void SetVehicleFlags (int flags, bool p);

        void ScriptSetVolumeDetect (bool p);

        void ScriptSetPhysicsStatus (bool p);

        void SetForce (Vector3 vector3);

        int PassCollisions { get; set; }

        int PassTouch { get; set; }

        int registerTargetWaypoint (Vector3 vector3, float p);

        void unregisterTargetWaypoint (int number);

        void ScriptSetPhantomStatus (bool p);

        bool AllowedDrop { get; set; }

        void aggregateScriptEvents ();

        int[] PayPrice { get; }

        void SetAxisRotation (int statusrotationaxis, int value);

        void SetStatusSandbox (bool p);

        void SetDieAtEdge (bool p);

        void SetReturnAtEdge (bool p);

        void SetBlockGrab (bool p);

        void SetVehicleFloatParam (int param, float p);

        void SetFaceColor (Vector3 vector3, int face);

        void SetSoundQueueing (int queue);

        void FixOffsetPosition (Vector3 vector3, bool p);

        void UpdateOffSet (Vector3 vector3);

        void UpdateRotation (Quaternion rot);

        void AddTextureAnimation (Primitive.TextureAnimation pTexAnim);

        void RemoveParticleSystem ();

        void AddNewParticleSystem (Primitive.ParticleSystem prules);

        string SitName { get; set; }

        string TouchName { get; set; }

        int ScriptAccessPin { get; set; }

        void SetFloatOnWater (int floatYN);

        void UpdateTexture (Primitive.TextureEntry tex);

        void SetText (string text, Vector3 av3, double p);

        List<UUID> SitTargetAvatar { get; }
        Dictionary<int, string> CollisionFilter { get; }

        bool GetBlockGrab ();

        bool RemFlag (PrimFlags primFlags);

        void GetProperties (IClientAPI iClientAPI);

        string MediaUrl { get; set; }

        void TriggerScriptChangedEvent (Changed changed);

        int SavedAttachmentPoint { get; set; }

        Vector3 SavedAttachedPos { get; set; }

        bool IsSelected { get; set; }

        DateTime Rezzed { get; set; }

        byte ObjectSaleType { get; set; }

        int SalePrice { get; set; }

        void ApplyNextOwnerPermissions ();

        void ClearUpdateScheduleOnce ();

        void StoreUndoState ();

        EntityIntersection TestIntersectionOBB (Ray NewRay, Quaternion quaternion, bool frontFacesOnly, bool CopyCenters);

        void UpdateShape (OpenMetaverse.Packets.ObjectShapePacket.ObjectDataBlock shapeBlock);

        void Undo ();

        void Redo ();

        DateTime Expires { get; set; }

        uint CRC { get; set; }
    }

    public interface ISceneGraph
    {
        ISceneEntity AddNewPrim (
            UUID ownerID, UUID groupID, Vector3 pos, Quaternion rot, PrimitiveBaseShape shape);
        Vector3 GetNewRezLocation (Vector3 RayStart, Vector3 RayEnd, UUID RayTargetID, Quaternion rot, byte bypassRayCast, byte RayEndIsIntersection, bool frontFacesOnly, Vector3 scale, bool FaceCenter);
        void GetCoarseLocations (out List<Vector3> coarseLocations, out List<UUID> avatarUUIDs, uint maxLocations);
        IScenePresence GetScenePresence (string firstName, string lastName);
        IScenePresence GetScenePresence (uint localID);
        void ForEachScenePresence (Action<IScenePresence> action);
        bool LinkPartToSOG (ISceneEntity grp, ISceneChildEntity part, int linkNum);
        ISceneEntity DuplicateEntity (ISceneEntity entity);
        bool LinkPartToEntity (ISceneEntity entity, ISceneChildEntity part);
        bool DeLinkPartFromEntity (ISceneEntity entity, ISceneChildEntity part);
        void UpdateEntity (ISceneEntity entity, UUID newID);
        bool TryGetEntity (UUID ID, out IEntity entity);
        bool TryGetPart (uint LocalID, out ISceneChildEntity entity);
        bool TryGetEntity (uint LocalID, out IEntity entity);
        bool TryGetPart (UUID ID, out ISceneChildEntity entity);
        void PrepPrimForAdditionToScene (ISceneEntity entity);
        bool AddPrimToScene (ISceneEntity entity);
        bool RestorePrimToScene (ISceneEntity entity);
        void DelinkPartToScene (ISceneEntity entity);
        bool DeleteEntity (IEntity entity);
        void CheckAllocationOfLocalIds (ISceneEntity group);
        uint AllocateLocalId ();
        int LinkSetSorter (ISceneChildEntity a, ISceneChildEntity b);

        List<EntityIntersection> GetIntersectingPrims(Ray hray, float length, int count, bool frontFacesOnly, bool faceCenters, bool getAvatars, bool getLand, bool getPrims);
        void RegisterEntityCreatorModule (IEntityCreator entityCreator);
    }

    /// <summary>
    /// Interface to a class that is capable of creating entities
    /// </summary>
    public interface IEntityCreator
    {
        /// <summary>
        /// The entities that this class is capable of creating.  These match the PCode format.
        /// </summary>
        /// <returns></returns>
        PCode[] CreationCapabilities { get; }

        /// <summary>
        /// Create an entity
        /// </summary>
        /// <param name="ownerID"></param>
        /// <param name="groupID"></param>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="shape"></param>
        /// <returns>The entity created, or null if the creation failed</returns>
        ISceneEntity CreateEntity (ISceneEntity baseEntity, UUID ownerID, UUID groupID, Vector3 pos, Quaternion rot, PrimitiveBaseShape shape);
    }

    public enum PIDHoverType
    {
        Ground,
        GroundAndWater,
        Water,
        Absolute
    }

    #region Enumerations

    /// <summary>
    /// Only used internally to schedule client updates.
    /// 0 - no update is scheduled
    /// 1 - terse update scheduled
    /// 2 - full update scheduled
    /// </summary>
    /// 
    public enum InternalUpdateFlags : byte
    {
        NoUpdate = 0,
        TerseUpdate = 1,
        FullUpdate = 2
    }

    [Flags]
    public enum Changed : uint
    {
        INVENTORY = 1,
        COLOR = 2,
        SHAPE = 4,
        SCALE = 8,
        TEXTURE = 16,
        LINK = 32,
        ALLOWED_DROP = 64,
        OWNER = 128,
        REGION = 256,
        TELEPORT = 512,
        REGION_RESTART = 1024,
        MEDIA = 2048,
        ANIMATION = 16384,
        STATE = 32768
    }

    // I don't really know where to put this except here.
    // Can't access the OpenSim.Region.ScriptEngine.Common.LSL_BaseClass.Changed constants
    [Flags]
    public enum ExtraParamType
    {
        Something1 = 1,
        Something2 = 2,
        Something3 = 4,
        Something4 = 8,
        Flexible = 16,
        Light = 32,
        Sculpt = 48,
        Something5 = 64,
        Something6 = 128
    }

    [Flags]
    public enum TextureAnimFlags : byte
    {
        NONE = 0x00,
        ANIM_ON = 0x01,
        LOOP = 0x02,
        REVERSE = 0x04,
        PING_PONG = 0x08,
        SMOOTH = 0x10,
        ROTATE = 0x20,
        SCALE = 0x40
    }

    public enum PrimType : int
    {
        BOX = 0,
        CYLINDER = 1,
        PRISM = 2,
        SPHERE = 3,
        TORUS = 4,
        TUBE = 5,
        RING = 6,
        SCULPT = 7
    }

    #endregion Enumerations

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

    /// <summary>
    /// A class for triggering remote scene events.
    /// </summary>
    public class EventManager
    {
        private static readonly ILog m_log = LogManager.GetLogger (MethodBase.GetCurrentMethod ().DeclaringType);

        public delegate void OnFrameDelegate ();

        public event OnFrameDelegate OnFrame;

        public delegate void ClientMovement (IScenePresence client);

        public event ClientMovement OnClientMovement;

        public delegate void OnClientConnectCoreDelegate (IClientCore client);

        public event OnClientConnectCoreDelegate OnClientConnect;

        public delegate void OnNewClientDelegate (IClientAPI client);

        /// <summary>
        /// Deprecated in favour of OnClientConnect.
        /// Will be marked Obsolete after IClientCore has 100% of IClientAPI interfaces.
        /// </summary>
        public event OnNewClientDelegate OnNewClient;
        public event OnNewClientDelegate OnClosingClient;

        public delegate void OnClientLoginDelegate (IClientAPI client);
        public event OnClientLoginDelegate OnClientLogin;

        public delegate void OnNewPresenceDelegate (IScenePresence presence);

        public event OnNewPresenceDelegate OnNewPresence;

        public event OnNewPresenceDelegate OnRemovePresence;

        public delegate void OnPluginConsoleDelegate (string[] args);

        public event OnPluginConsoleDelegate OnPluginConsole;

        public delegate void OnPermissionErrorDelegate (UUID user, string reason);

        /// <summary>
        /// Fired when an object is touched/grabbed.
        /// </summary>
        /// The child is the part that was actually touched.
        public event ObjectGrabDelegate OnObjectGrab;
        public delegate void ObjectGrabDelegate (ISceneChildEntity part, ISceneChildEntity child, Vector3 offsetPos, IClientAPI remoteClient, SurfaceTouchEventArgs surfaceArgs);

        public event ObjectGrabDelegate OnObjectGrabbing;
        public event ObjectDeGrabDelegate OnObjectDeGrab;
        public delegate void ObjectDeGrabDelegate (ISceneChildEntity part, ISceneChildEntity child, IClientAPI remoteClient, SurfaceTouchEventArgs surfaceArgs);

        public event OnPermissionErrorDelegate OnPermissionError;

        /// <summary>
        /// Fired when a new script is created.
        /// </summary>
        public event NewRezScripts OnRezScripts;
        public delegate void NewRezScripts (ISceneChildEntity part, TaskInventoryItem[] taskInventoryItem, int startParam, bool postOnRez, int stateSource, UUID RezzedFrom);

        public delegate void RemoveScript (uint localID, UUID itemID);
        public event RemoveScript OnRemoveScript;

        public delegate bool SceneGroupMoved (UUID groupID, Vector3 delta);
        public event SceneGroupMoved OnSceneGroupMove;

        public delegate void SceneGroupGrabed (UUID groupID, Vector3 offset, UUID userID);
        public event SceneGroupGrabed OnSceneGroupGrab;

        public delegate bool SceneGroupSpinStarted (UUID groupID);
        public event SceneGroupSpinStarted OnSceneGroupSpinStart;

        public delegate bool SceneGroupSpun (UUID groupID, Quaternion rotation);
        public event SceneGroupSpun OnSceneGroupSpin;

        public delegate void LandObjectAdded (LandData newParcel);
        public event LandObjectAdded OnLandObjectAdded;

        public delegate void LandObjectRemoved (UUID RegionID, UUID globalID);
        public event LandObjectRemoved OnLandObjectRemoved;

        public delegate void AvatarEnteringNewParcel (IScenePresence avatar, int localLandID, UUID regionID);
        public event AvatarEnteringNewParcel OnAvatarEnteringNewParcel;

        public delegate void SignificantClientMovement (IClientAPI remote_client);
        public event SignificantClientMovement OnSignificantClientMovement;

        public delegate void SignificantObjectMovement (ISceneEntity group);
        public event SignificantObjectMovement OnSignificantObjectMovement;

        public delegate void IncomingInstantMessage (GridInstantMessage message);
        public event IncomingInstantMessage OnIncomingInstantMessage;

        public event IncomingInstantMessage OnUnhandledInstantMessage;

        public delegate void ClientClosed (UUID clientID, IScene scene);

        public event ClientClosed OnClientClosed;

        /// <summary>
        /// This is fired when a scene object property that a script might be interested in (such as color, scale or
        /// inventory) changes.  Only enough information is sent for the LSL changed event
        /// (see http://lslwiki.net/lslwiki/wakka.php?wakka=changed)
        /// </summary>
        public event ScriptChangedEvent OnScriptChangedEvent;
        public delegate void ScriptChangedEvent (ISceneChildEntity part, uint change);

        public event ScriptMovingStartEvent OnScriptMovingStartEvent;
        public delegate void ScriptMovingStartEvent (ISceneChildEntity part);

        public event ScriptMovingEndEvent OnScriptMovingEndEvent;
        public delegate void ScriptMovingEndEvent (ISceneChildEntity part);

        public delegate void ScriptControlEvent (ISceneChildEntity part, UUID item, UUID avatarID, uint held, uint changed);
        public event ScriptControlEvent OnScriptControlEvent;

        public delegate void ScriptAtTargetEvent (uint localID, uint handle, Vector3 targetpos, Vector3 atpos);
        public event ScriptAtTargetEvent OnScriptAtTargetEvent;

        public delegate void ScriptNotAtTargetEvent (uint localID);
        public event ScriptNotAtTargetEvent OnScriptNotAtTargetEvent;

        public delegate void ScriptAtRotTargetEvent (uint localID, uint handle, Quaternion targetrot, Quaternion atrot);
        public event ScriptAtRotTargetEvent OnScriptAtRotTargetEvent;

        public delegate void ScriptNotAtRotTargetEvent (uint localID);
        public event ScriptNotAtRotTargetEvent OnScriptNotAtRotTargetEvent;

        public delegate void ScriptColliding (ISceneChildEntity part, ColliderArgs colliders);
        public event ScriptColliding OnScriptColliderStart;
        public event ScriptColliding OnScriptColliding;
        public event ScriptColliding OnScriptCollidingEnd;
        public event ScriptColliding OnScriptLandColliderStart;
        public event ScriptColliding OnScriptLandColliding;
        public event ScriptColliding OnScriptLandColliderEnd;

        public delegate void OnMakeChildAgentDelegate (IScenePresence presence);
        public event OnMakeChildAgentDelegate OnMakeChildAgent;

        public delegate void OnMakeRootAgentDelegate (IScenePresence presence);
        public event OnMakeRootAgentDelegate OnMakeRootAgent;

        public delegate void NewInventoryItemUploadComplete (UUID avatarID, UUID assetID, string name, int userlevel);

        public event NewInventoryItemUploadComplete OnNewInventoryItemUploadComplete;

        public delegate void RequestChangeWaterHeight (float height);

        public event RequestChangeWaterHeight OnRequestChangeWaterHeight;

        public delegate void AddToStartupQueue (string name);
        public delegate void FinishedStartup (string name, List<string> data);
        public delegate void StartupComplete (IScene scene, List<string> data);
        public event FinishedStartup OnModuleFinishedStartup;
        public event AddToStartupQueue OnAddToStartupQueue;
        public event StartupComplete OnStartupComplete;
        //This is called after OnStartupComplete is done, it should ONLY be registered to the Scene
        public event StartupComplete OnStartupFullyComplete;

        public delegate void EstateToolsSunUpdate (ulong regionHandle, bool FixedTime, bool EstateSun, float LindenHour);
        public event EstateToolsSunUpdate OnEstateToolsSunUpdate;

        public delegate void ObjectBeingRemovedFromScene (ISceneEntity obj);
        public event ObjectBeingRemovedFromScene OnObjectBeingRemovedFromScene;

        public event ObjectBeingRemovedFromScene OnObjectBeingAddedToScene;

        public delegate void IncomingLandDataFromStorage (List<LandData> data);
        public event IncomingLandDataFromStorage OnIncomingLandDataFromStorage;

        /// <summary>
        /// RegisterCapsEvent is called by Scene after the Caps object
        /// has been instantiated and before it is return to the
        /// client and provides region modules to add their caps.
        /// </summary>
        public delegate OSDMap RegisterCapsEvent (UUID agentID, IHttpServer httpServer);
        public event RegisterCapsEvent OnRegisterCaps;

        /// <summary>
        /// DeregisterCapsEvent is called by Scene when the caps
        /// handler for an agent are removed.
        /// </summary>
        public delegate void DeregisterCapsEvent (UUID agentID, IRegionClientCapsService caps);
        public event DeregisterCapsEvent OnDeregisterCaps;

        /// <summary>
        /// ChatFromWorldEvent is called via Scene when a chat message
        /// from world comes in.
        /// </summary>
        public delegate void ChatFromWorldEvent (Object sender, OSChatMessage chat);
        public event ChatFromWorldEvent OnChatFromWorld;

        /// <summary>
        /// ChatFromClientEvent is triggered via ChatModule (or
        /// substitutes thereof) when a chat message
        /// from the client  comes in.
        /// </summary>
        public delegate void ChatFromClientEvent (Object sender, OSChatMessage chat);
        public event ChatFromClientEvent OnChatFromClient;

        /// <summary>
        /// ChatBroadcastEvent is called via Scene when a broadcast chat message
        /// from world comes in
        /// </summary>
        public delegate void ChatBroadcastEvent (Object sender, OSChatMessage chat);
        public event ChatBroadcastEvent OnChatBroadcast;

        /// <summary>
        /// Called when oar file has finished loading, although
        /// the scripts may not have started yet
        /// Message is non empty string if there were problems loading the oar file
        /// </summary>
        public delegate void OarFileLoaded (Guid guid, string message);
        public event OarFileLoaded OnOarFileLoaded;

        /// <summary>
        /// Called when an oar file has finished saving
        /// Message is non empty string if there were problems saving the oar file
        /// If a guid was supplied on the original call to identify, the request, this is returned.  Otherwise 
        /// Guid.Empty is returned.
        /// </summary>
        public delegate void OarFileSaved (Guid guid, string message);
        public event OarFileSaved OnOarFileSaved;

        /// <summary>
        /// Called when the script compile queue becomes empty
        /// Returns the number of scripts which failed to start
        /// </summary>
        public delegate void EmptyScriptCompileQueue (int numScriptsFailed, string message);
        public event EmptyScriptCompileQueue OnEmptyScriptCompileQueue;

        /// <summary>
        /// Called whenever an object is attached, or detached from an in-world presence.
        /// </summary>
        /// If the object is being attached, then the avatarID will be present.  If the object is being detached then
        /// the avatarID is UUID.Zero (I know, this doesn't make much sense but now it's historical).
        public delegate void Attach (uint localID, UUID itemID, UUID avatarID);
        public event Attach OnAttach;

        public delegate void RegionUp (GridRegion region);
        public event RegionUp OnRegionUp;
        public event RegionUp OnRegionDown;

        public class LandBuyArgs : EventArgs
        {
            public UUID agentId = UUID.Zero;

            public UUID groupId = UUID.Zero;

            public UUID parcelOwnerID = UUID.Zero;

            public bool final = false;
            public bool groupOwned = false;
            public bool removeContribution = false;
            public int parcelLocalID = 0;
            public int parcelArea = 0;
            public int parcelPrice = 0;
            public bool authenticated = false;
            public bool landValidated = false;
            public bool economyValidated = false;
            public int transactionID = 0;
            public int amountDebited = 0;

            public LandBuyArgs (UUID pagentId, UUID pgroupId, bool pfinal, bool pgroupOwned,
                bool premoveContribution, int pparcelLocalID, int pparcelArea, int pparcelPrice,
                bool pauthenticated)
            {
                agentId = pagentId;
                groupId = pgroupId;
                final = pfinal;
                groupOwned = pgroupOwned;
                removeContribution = premoveContribution;
                parcelLocalID = pparcelLocalID;
                parcelArea = pparcelArea;
                parcelPrice = pparcelPrice;
                authenticated = pauthenticated;
            }
        }

        public delegate bool LandBuy (LandBuyArgs e);

        public event LandBuy OnValidateBuyLand;

        public void TriggerOnAttach (uint localID, UUID itemID, UUID avatarID)
        {
            Attach handlerOnAttach = OnAttach;
            if (handlerOnAttach != null)
            {
                foreach (Attach d in handlerOnAttach.GetInvocationList ())
                {
                    try
                    {
                        d (localID, itemID, avatarID);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnAttach failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerOnScriptChangedEvent (ISceneChildEntity part, uint change)
        {
            ScriptChangedEvent handlerScriptChangedEvent = OnScriptChangedEvent;
            if (handlerScriptChangedEvent != null)
            {
                foreach (ScriptChangedEvent d in handlerScriptChangedEvent.GetInvocationList ())
                {
                    try
                    {
                        d (part, change);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnScriptChangedEvent failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerOnScriptMovingStartEvent (ISceneChildEntity part)
        {
            ScriptMovingStartEvent handlerScriptMovingStartEvent = OnScriptMovingStartEvent;
            if (handlerScriptMovingStartEvent != null)
            {
                foreach (ScriptMovingStartEvent d in handlerScriptMovingStartEvent.GetInvocationList ())
                {
                    try
                    {
                        d (part);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnScriptMovingStartEvent failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerOnScriptMovingEndEvent (ISceneChildEntity part)
        {
            ScriptMovingEndEvent handlerScriptMovingEndEvent = OnScriptMovingEndEvent;
            if (handlerScriptMovingEndEvent != null)
            {
                foreach (ScriptMovingEndEvent d in handlerScriptMovingEndEvent.GetInvocationList ())
                {
                    try
                    {
                        d (part);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnScriptMovingEndEvent failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerOnClientMovement (IScenePresence avatar)
        {
            ClientMovement handlerClientMovement = OnClientMovement;
            if (handlerClientMovement != null)
            {
                foreach (ClientMovement d in handlerClientMovement.GetInvocationList ())
                {
                    try
                    {
                        d (avatar);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnClientMovement failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerPermissionError (UUID user, string reason)
        {
            OnPermissionErrorDelegate handlerPermissionError = OnPermissionError;
            if (handlerPermissionError != null)
            {
                foreach (OnPermissionErrorDelegate d in handlerPermissionError.GetInvocationList ())
                {
                    try
                    {
                        d (user, reason);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerPermissionError failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerOnPluginConsole (string[] args)
        {
            OnPluginConsoleDelegate handlerPluginConsole = OnPluginConsole;
            if (handlerPluginConsole != null)
            {
                foreach (OnPluginConsoleDelegate d in handlerPluginConsole.GetInvocationList ())
                {
                    try
                    {
                        d (args);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnPluginConsole failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerOnFrame ()
        {
            OnFrameDelegate handlerFrame = OnFrame;
            if (handlerFrame != null)
            {
                foreach (OnFrameDelegate d in handlerFrame.GetInvocationList ())
                {
                    try
                    {
                        d ();
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnFrame failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerOnClosingClient (IClientAPI client)
        {
            OnNewClientDelegate handlerClosingClient = OnClosingClient;
            if (handlerClosingClient != null)
            {
                foreach (OnNewClientDelegate d in handlerClosingClient.GetInvocationList ())
                {
                    try
                    {
                        d (client);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnClosingClient failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerOnNewClient (IClientAPI client)
        {
            OnNewClientDelegate handlerNewClient = OnNewClient;
            if (handlerNewClient != null)
            {
                foreach (OnNewClientDelegate d in handlerNewClient.GetInvocationList ())
                {
                    try
                    {
                        d (client);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnNewClient failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }

            if (client is IClientCore)
            {
                OnClientConnectCoreDelegate handlerClientConnect = OnClientConnect;
                if (handlerClientConnect != null)
                {
                    foreach (OnClientConnectCoreDelegate d in handlerClientConnect.GetInvocationList ())
                    {
                        try
                        {
                            d ((IClientCore)client);
                        }
                        catch (Exception e)
                        {
                            m_log.ErrorFormat (
                                "[EVENT MANAGER]: Delegate for TriggerOnNewClient (IClientCore) failed - continuing.  {0} {1}",
                                e.ToString (), e.StackTrace);
                        }
                    }
                }
            }
        }

        public void TriggerOnClientLogin (IClientAPI client)
        {
            OnClientLoginDelegate handlerClientLogin = OnClientLogin;
            if (handlerClientLogin != null)
            {
                foreach (OnClientLoginDelegate d in handlerClientLogin.GetInvocationList ())
                {
                    try
                    {
                        d (client);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnClientLogin failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }

        }

        public void TriggerOnNewPresence (IScenePresence presence)
        {
            OnNewPresenceDelegate handlerNewPresence = OnNewPresence;
            if (handlerNewPresence != null)
            {
                foreach (OnNewPresenceDelegate d in handlerNewPresence.GetInvocationList ())
                {
                    try
                    {
                        d (presence);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnNewPresence failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerOnRemovePresence (IScenePresence presence)
        {
            OnNewPresenceDelegate handlerRemovePresence = OnRemovePresence;
            if (handlerRemovePresence != null)
            {
                foreach (OnNewPresenceDelegate d in handlerRemovePresence.GetInvocationList ())
                {
                    try
                    {
                        d (presence);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnRemovePresence failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerObjectBeingAddedToScene (ISceneEntity obj)
        {
            ObjectBeingRemovedFromScene handlerObjectBeingAddedToScene = OnObjectBeingAddedToScene;
            if (handlerObjectBeingAddedToScene != null)
            {
                foreach (ObjectBeingRemovedFromScene d in handlerObjectBeingAddedToScene.GetInvocationList ())
                {
                    try
                    {
                        d (obj);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerObjectBeingAddToScene failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerObjectBeingRemovedFromScene (ISceneEntity obj)
        {
            ObjectBeingRemovedFromScene handlerObjectBeingRemovedFromScene = OnObjectBeingRemovedFromScene;
            if (handlerObjectBeingRemovedFromScene != null)
            {
                foreach (ObjectBeingRemovedFromScene d in handlerObjectBeingRemovedFromScene.GetInvocationList ())
                {
                    try
                    {
                        d (obj);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerObjectBeingRemovedFromScene failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerObjectGrab (ISceneChildEntity part, ISceneChildEntity child, Vector3 offsetPos, IClientAPI remoteClient, SurfaceTouchEventArgs surfaceArgs)
        {
            ObjectGrabDelegate handlerObjectGrab = OnObjectGrab;
            if (handlerObjectGrab != null)
            {
                foreach (ObjectGrabDelegate d in handlerObjectGrab.GetInvocationList ())
                {
                    try
                    {
                        d (part, child, offsetPos, remoteClient, surfaceArgs);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerObjectGrab failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerObjectGrabbing (ISceneChildEntity part, ISceneChildEntity child, Vector3 offsetPos, IClientAPI remoteClient, SurfaceTouchEventArgs surfaceArgs)
        {
            ObjectGrabDelegate handlerObjectGrabbing = OnObjectGrabbing;
            if (handlerObjectGrabbing != null)
            {
                foreach (ObjectGrabDelegate d in handlerObjectGrabbing.GetInvocationList ())
                {
                    try
                    {
                        d (part, child, offsetPos, remoteClient, surfaceArgs);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerObjectGrabbing failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerObjectDeGrab (ISceneChildEntity part, ISceneChildEntity child, IClientAPI remoteClient, SurfaceTouchEventArgs surfaceArgs)
        {
            ObjectDeGrabDelegate handlerObjectDeGrab = OnObjectDeGrab;
            if (handlerObjectDeGrab != null)
            {
                foreach (ObjectDeGrabDelegate d in handlerObjectDeGrab.GetInvocationList ())
                {
                    try
                    {
                        d (part, child, remoteClient, surfaceArgs);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerObjectDeGrab failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerRezScripts (ISceneChildEntity part, TaskInventoryItem[] taskInventoryItem, int startParam, bool postOnRez, int stateSource, UUID RezzedFrom)
        {
            NewRezScripts handlerRezScripts = OnRezScripts;
            if (handlerRezScripts != null)
            {
                foreach (NewRezScripts d in handlerRezScripts.GetInvocationList ())
                {
                    try
                    {
                        d (part, taskInventoryItem, startParam, postOnRez, stateSource, RezzedFrom);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerRezScript failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerRemoveScript (uint localID, UUID itemID)
        {
            RemoveScript handlerRemoveScript = OnRemoveScript;
            if (handlerRemoveScript != null)
            {
                foreach (RemoveScript d in handlerRemoveScript.GetInvocationList ())
                {
                    try
                    {
                        d (localID, itemID);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerRemoveScript failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public bool TriggerGroupMove (UUID groupID, Vector3 delta)
        {
            bool result = true;

            SceneGroupMoved handlerSceneGroupMove = OnSceneGroupMove;
            if (handlerSceneGroupMove != null)
            {
                foreach (SceneGroupMoved d in handlerSceneGroupMove.GetInvocationList ())
                {
                    try
                    {
                        if (d (groupID, delta) == false)
                            result = false;
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnAttach failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }

            return result;
        }

        public bool TriggerGroupSpinStart (UUID groupID)
        {
            bool result = true;

            SceneGroupSpinStarted handlerSceneGroupSpinStarted = OnSceneGroupSpinStart;
            if (handlerSceneGroupSpinStarted != null)
            {
                foreach (SceneGroupSpinStarted d in handlerSceneGroupSpinStarted.GetInvocationList ())
                {
                    try
                    {
                        if (d (groupID) == false)
                            result = false;
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerGroupSpinStart failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }

            return result;
        }

        public bool TriggerGroupSpin (UUID groupID, Quaternion rotation)
        {
            bool result = true;

            SceneGroupSpun handlerSceneGroupSpin = OnSceneGroupSpin;
            if (handlerSceneGroupSpin != null)
            {
                foreach (SceneGroupSpun d in handlerSceneGroupSpin.GetInvocationList ())
                {
                    try
                    {
                        if (d (groupID, rotation) == false)
                            result = false;
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerGroupSpin failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }

            return result;
        }

        public void TriggerGroupGrab (UUID groupID, Vector3 offset, UUID userID)
        {
            SceneGroupGrabed handlerSceneGroupGrab = OnSceneGroupGrab;
            if (handlerSceneGroupGrab != null)
            {
                foreach (SceneGroupGrabed d in handlerSceneGroupGrab.GetInvocationList ())
                {
                    try
                    {
                        d (groupID, offset, userID);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerGroupGrab failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerLandObjectAdded (LandData newParcel)
        {
            LandObjectAdded handlerLandObjectAdded = OnLandObjectAdded;
            if (handlerLandObjectAdded != null)
            {
                foreach (LandObjectAdded d in handlerLandObjectAdded.GetInvocationList ())
                {
                    try
                    {
                        d (newParcel);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerLandObjectAdded failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerLandObjectRemoved (UUID regionID, UUID globalID)
        {
            LandObjectRemoved handlerLandObjectRemoved = OnLandObjectRemoved;
            if (handlerLandObjectRemoved != null)
            {
                foreach (LandObjectRemoved d in handlerLandObjectRemoved.GetInvocationList ())
                {
                    try
                    {
                        d (regionID, globalID);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerLandObjectRemoved failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerAvatarEnteringNewParcel (IScenePresence avatar, int localLandID, UUID regionID)
        {
            AvatarEnteringNewParcel handlerAvatarEnteringNewParcel = OnAvatarEnteringNewParcel;
            if (handlerAvatarEnteringNewParcel != null)
            {
                foreach (AvatarEnteringNewParcel d in handlerAvatarEnteringNewParcel.GetInvocationList ())
                {
                    try
                    {
                        d (avatar, localLandID, regionID);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerAvatarEnteringNewParcel failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerIncomingInstantMessage (GridInstantMessage message)
        {
            IncomingInstantMessage handlerIncomingInstantMessage = OnIncomingInstantMessage;
            if (handlerIncomingInstantMessage != null)
            {
                foreach (IncomingInstantMessage d in handlerIncomingInstantMessage.GetInvocationList ())
                {
                    try
                    {
                        d (message);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerIncomingInstantMessage failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerUnhandledInstantMessage (GridInstantMessage message)
        {
            IncomingInstantMessage handlerUnhandledInstantMessage = OnUnhandledInstantMessage;
            if (handlerUnhandledInstantMessage != null)
            {
                foreach (IncomingInstantMessage d in handlerUnhandledInstantMessage.GetInvocationList ())
                {
                    try
                    {
                        d (message);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnAttach failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerClientClosed (UUID ClientID, IScene scene)
        {
            ClientClosed handlerClientClosed = OnClientClosed;
            if (handlerClientClosed != null)
            {
                foreach (ClientClosed d in handlerClientClosed.GetInvocationList ())
                {
                    try
                    {
                        d (ClientID, scene);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerClientClosed failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerOnMakeChildAgent (IScenePresence presence)
        {
            OnMakeChildAgentDelegate handlerMakeChildAgent = OnMakeChildAgent;
            if (handlerMakeChildAgent != null)
            {
                foreach (OnMakeChildAgentDelegate d in handlerMakeChildAgent.GetInvocationList ())
                {
                    try
                    {
                        d (presence);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnMakeChildAgent failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerOnMakeRootAgent (IScenePresence presence)
        {
            OnMakeRootAgentDelegate handlerMakeRootAgent = OnMakeRootAgent;
            if (handlerMakeRootAgent != null)
            {
                foreach (OnMakeRootAgentDelegate d in handlerMakeRootAgent.GetInvocationList ())
                {
                    try
                    {
                        d (presence);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnMakeRootAgent failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public OSDMap TriggerOnRegisterCaps (UUID agentID)
        {
            OSDMap retVal = new OSDMap ();
            RegisterCapsEvent handlerRegisterCaps = OnRegisterCaps;
            if (handlerRegisterCaps != null)
            {
                foreach (RegisterCapsEvent d in handlerRegisterCaps.GetInvocationList ())
                {
                    try
                    {
                        OSDMap r = d (agentID, MainServer.Instance);
                        if (r != null)
                        {
                            foreach (KeyValuePair<string, OSD> kvp in r)
                            {
                                retVal[kvp.Key] = MainServer.Instance.HostName + ":" + MainServer.Instance.Port + kvp.Value;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnRegisterCaps failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
            return retVal;
        }

        public void TriggerOnDeregisterCaps (UUID agentID, IRegionClientCapsService caps)
        {
            DeregisterCapsEvent handlerDeregisterCaps = OnDeregisterCaps;
            if (handlerDeregisterCaps != null)
            {
                foreach (DeregisterCapsEvent d in handlerDeregisterCaps.GetInvocationList ())
                {
                    try
                    {
                        d (agentID, caps);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnDeregisterCaps failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerOnNewInventoryItemUploadComplete (UUID agentID, UUID AssetID, String AssetName, int userlevel)
        {
            NewInventoryItemUploadComplete handlerNewInventoryItemUpdateComplete = OnNewInventoryItemUploadComplete;
            if (handlerNewInventoryItemUpdateComplete != null)
            {
                foreach (NewInventoryItemUploadComplete d in handlerNewInventoryItemUpdateComplete.GetInvocationList ())
                {
                    try
                    {
                        d (agentID, AssetID, AssetName, userlevel);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnNewInventoryItemUploadComplete failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public bool TriggerValidateBuyLand (LandBuyArgs args)
        {
            LandBuy handlerLandBuy = OnValidateBuyLand;
            if (handlerLandBuy != null)
            {
                foreach (LandBuy d in handlerLandBuy.GetInvocationList ())
                {
                    try
                    {
                        if (!d (args))
                            return false;
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerLandBuy failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
            return true;
        }

        public void TriggerAtTargetEvent (uint localID, uint handle, Vector3 targetpos, Vector3 currentpos)
        {
            ScriptAtTargetEvent handlerScriptAtTargetEvent = OnScriptAtTargetEvent;
            if (handlerScriptAtTargetEvent != null)
            {
                foreach (ScriptAtTargetEvent d in handlerScriptAtTargetEvent.GetInvocationList ())
                {
                    try
                    {
                        d (localID, handle, targetpos, currentpos);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerAtTargetEvent failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerNotAtTargetEvent (uint localID)
        {
            ScriptNotAtTargetEvent handlerScriptNotAtTargetEvent = OnScriptNotAtTargetEvent;
            if (handlerScriptNotAtTargetEvent != null)
            {
                foreach (ScriptNotAtTargetEvent d in handlerScriptNotAtTargetEvent.GetInvocationList ())
                {
                    try
                    {
                        d (localID);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerNotAtTargetEvent failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerAtRotTargetEvent (uint localID, uint handle, Quaternion targetrot, Quaternion currentrot)
        {
            ScriptAtRotTargetEvent handlerScriptAtRotTargetEvent = OnScriptAtRotTargetEvent;
            if (handlerScriptAtRotTargetEvent != null)
            {
                foreach (ScriptAtRotTargetEvent d in handlerScriptAtRotTargetEvent.GetInvocationList ())
                {
                    try
                    {
                        d (localID, handle, targetrot, currentrot);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerAtRotTargetEvent failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerNotAtRotTargetEvent (uint localID)
        {
            ScriptNotAtRotTargetEvent handlerScriptNotAtRotTargetEvent = OnScriptNotAtRotTargetEvent;
            if (handlerScriptNotAtRotTargetEvent != null)
            {
                foreach (ScriptNotAtRotTargetEvent d in handlerScriptNotAtRotTargetEvent.GetInvocationList ())
                {
                    try
                    {
                        d (localID);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerNotAtRotTargetEvent failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerRequestChangeWaterHeight (float height)
        {
            RequestChangeWaterHeight handlerRequestChangeWaterHeight = OnRequestChangeWaterHeight;
            if (handlerRequestChangeWaterHeight != null)
            {
                foreach (RequestChangeWaterHeight d in handlerRequestChangeWaterHeight.GetInvocationList ())
                {
                    try
                    {
                        d (height);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerRequestChangeWaterHeight failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerSignificantClientMovement (IClientAPI client)
        {
            SignificantClientMovement handlerSignificantClientMovement = OnSignificantClientMovement;
            if (handlerSignificantClientMovement != null)
            {
                foreach (SignificantClientMovement d in handlerSignificantClientMovement.GetInvocationList ())
                {
                    try
                    {
                        d (client);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerSignificantClientMovement failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerSignificantObjectMovement (ISceneEntity group)
        {
            SignificantObjectMovement handlerSignificantObjectMovement = OnSignificantObjectMovement;
            if (handlerSignificantObjectMovement != null)
            {
                foreach (SignificantObjectMovement d in handlerSignificantObjectMovement.GetInvocationList ())
                {
                    try
                    {
                        d (group);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerSignificantObjectMovement failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerOnChatFromWorld (Object sender, OSChatMessage chat)
        {
            ChatFromWorldEvent handlerChatFromWorld = OnChatFromWorld;
            if (handlerChatFromWorld != null)
            {
                foreach (ChatFromWorldEvent d in handlerChatFromWorld.GetInvocationList ())
                {
                    try
                    {
                        d (sender, chat);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnChatFromWorld failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerOnChatFromClient (Object sender, OSChatMessage chat)
        {
            ChatFromClientEvent handlerChatFromClient = OnChatFromClient;
            if (handlerChatFromClient != null)
            {
                foreach (ChatFromClientEvent d in handlerChatFromClient.GetInvocationList ())
                {
                    try
                    {
                        d (sender, chat);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnChatFromClient failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerOnChatBroadcast (Object sender, OSChatMessage chat)
        {
            ChatBroadcastEvent handlerChatBroadcast = OnChatBroadcast;
            if (handlerChatBroadcast != null)
            {
                foreach (ChatBroadcastEvent d in handlerChatBroadcast.GetInvocationList ())
                {
                    try
                    {
                        d (sender, chat);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnChatBroadcast failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerControlEvent (ISceneChildEntity part, UUID scriptUUID, UUID avatarID, uint held, uint _changed)
        {
            ScriptControlEvent handlerScriptControlEvent = OnScriptControlEvent;
            if (handlerScriptControlEvent != null)
            {
                foreach (ScriptControlEvent d in handlerScriptControlEvent.GetInvocationList ())
                {
                    try
                    {
                        d (part, scriptUUID, avatarID, held, _changed);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerControlEvent failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerIncomingLandDataFromStorage (List<LandData> landData)
        {
            IncomingLandDataFromStorage handlerIncomingLandDataFromStorage = OnIncomingLandDataFromStorage;
            if (handlerIncomingLandDataFromStorage != null)
            {
                foreach (IncomingLandDataFromStorage d in handlerIncomingLandDataFromStorage.GetInvocationList ())
                {
                    try
                    {
                        d (landData);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerIncomingLandDataFromStorage failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the system as to how the position of the sun should be handled.
        /// </summary>
        /// <param name="regionHandle"></param>
        /// <param name="FixedTime">True if the Sun Position is fixed</param>
        /// <param name="useEstateTime">True if the Estate Settings should be used instead of region</param>
        /// <param name="FixedSunHour">The hour 0.0 <= FixedSunHour <= 24.0 at which the sun is fixed at. Sun Hour 0 is sun-rise, when Day/Night ratio is 1:1</param>
        public void TriggerEstateToolsSunUpdate (ulong regionHandle, bool FixedTime, bool useEstateTime, float FixedSunHour)
        {
            EstateToolsSunUpdate handlerEstateToolsSunUpdate = OnEstateToolsSunUpdate;
            if (handlerEstateToolsSunUpdate != null)
            {
                foreach (EstateToolsSunUpdate d in handlerEstateToolsSunUpdate.GetInvocationList ())
                {
                    try
                    {
                        d (regionHandle, FixedTime, useEstateTime, FixedSunHour);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerEstateToolsSunUpdate failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerOarFileLoaded (Guid requestId, string message)
        {
            OarFileLoaded handlerOarFileLoaded = OnOarFileLoaded;
            if (handlerOarFileLoaded != null)
            {
                foreach (OarFileLoaded d in handlerOarFileLoaded.GetInvocationList ())
                {
                    try
                    {
                        d (requestId, message);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOarFileLoaded failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerOarFileSaved (Guid requestId, string message)
        {
            OarFileSaved handlerOarFileSaved = OnOarFileSaved;
            if (handlerOarFileSaved != null)
            {
                foreach (OarFileSaved d in handlerOarFileSaved.GetInvocationList ())
                {
                    try
                    {
                        d (requestId, message);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOarFileSaved failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerEmptyScriptCompileQueue (int numScriptsFailed, string message)
        {
            EmptyScriptCompileQueue handlerEmptyScriptCompileQueue = OnEmptyScriptCompileQueue;
            if (handlerEmptyScriptCompileQueue != null)
            {
                foreach (EmptyScriptCompileQueue d in handlerEmptyScriptCompileQueue.GetInvocationList ())
                {
                    try
                    {
                        d (numScriptsFailed, message);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerEmptyScriptCompileQueue failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerScriptCollidingStart (ISceneChildEntity part, ColliderArgs colliders)
        {
            ScriptColliding handlerCollidingStart = OnScriptColliderStart;
            if (handlerCollidingStart != null)
            {
                foreach (ScriptColliding d in handlerCollidingStart.GetInvocationList ())
                {
                    try
                    {
                        d (part, colliders);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerScriptCollidingStart failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerScriptColliding (ISceneChildEntity part, ColliderArgs colliders)
        {
            ScriptColliding handlerColliding = OnScriptColliding;
            if (handlerColliding != null)
            {
                foreach (ScriptColliding d in handlerColliding.GetInvocationList ())
                {
                    try
                    {
                        d (part, colliders);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerScriptColliding failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerScriptCollidingEnd (ISceneChildEntity part, ColliderArgs colliders)
        {
            ScriptColliding handlerCollidingEnd = OnScriptCollidingEnd;
            if (handlerCollidingEnd != null)
            {
                foreach (ScriptColliding d in handlerCollidingEnd.GetInvocationList ())
                {
                    try
                    {
                        d (part, colliders);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerScriptCollidingEnd failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerScriptLandCollidingStart (ISceneChildEntity part, ColliderArgs colliders)
        {
            ScriptColliding handlerLandCollidingStart = OnScriptLandColliderStart;
            if (handlerLandCollidingStart != null)
            {
                foreach (ScriptColliding d in handlerLandCollidingStart.GetInvocationList ())
                {
                    try
                    {
                        d (part, colliders);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerScriptLandCollidingStart failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerScriptLandColliding (ISceneChildEntity part, ColliderArgs colliders)
        {
            ScriptColliding handlerLandColliding = OnScriptLandColliding;
            if (handlerLandColliding != null)
            {
                foreach (ScriptColliding d in handlerLandColliding.GetInvocationList ())
                {
                    try
                    {
                        d (part, colliders);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerScriptLandColliding failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerScriptLandCollidingEnd (ISceneChildEntity part, ColliderArgs colliders)
        {
            ScriptColliding handlerLandCollidingEnd = OnScriptLandColliderEnd;
            if (handlerLandCollidingEnd != null)
            {
                foreach (ScriptColliding d in handlerLandCollidingEnd.GetInvocationList ())
                {
                    try
                    {
                        d (part, colliders);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerScriptLandCollidingEnd failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerOnRegionUp (GridRegion otherRegion)
        {
            RegionUp handlerOnRegionUp = OnRegionUp;
            if (handlerOnRegionUp != null)
            {
                foreach (RegionUp d in handlerOnRegionUp.GetInvocationList ())
                {
                    try
                    {
                        d (otherRegion);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnRegionUp failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerOnRegionDown (GridRegion otherRegion)
        {
            RegionUp handlerOnRegionDown = OnRegionDown;
            if (handlerOnRegionDown != null)
            {
                foreach (RegionUp d in handlerOnRegionDown.GetInvocationList ())
                {
                    try
                    {
                        d (otherRegion);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for TriggerOnRegionUp failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerModuleFinishedStartup (string name, List<string> data)
        {
            FinishedStartup handlerOnFinishedStartup = OnModuleFinishedStartup;
            if (handlerOnFinishedStartup != null)
            {
                foreach (FinishedStartup d in handlerOnFinishedStartup.GetInvocationList ())
                {
                    try
                    {
                        d (name, data);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for FinishedStartup failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerAddToStartupQueue (string name)
        {
            AddToStartupQueue handlerOnAddToStartupQueue = OnAddToStartupQueue;
            if (handlerOnAddToStartupQueue != null)
            {
                foreach (AddToStartupQueue d in handlerOnAddToStartupQueue.GetInvocationList ())
                {
                    try
                    {
                        d (name);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for AddToStartupQueue failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
            }
        }

        public void TriggerStartupComplete (IScene scene, List<string> StartupData)
        {
            StartupComplete handlerOnStartupComplete = OnStartupComplete;
            StartupComplete handlerOnStartupFullyComplete = OnStartupFullyComplete;
            if (handlerOnStartupComplete != null)
            {
                foreach (StartupComplete d in handlerOnStartupComplete.GetInvocationList ())
                {
                    try
                    {
                        d (scene, StartupData);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat (
                            "[EVENT MANAGER]: Delegate for StartupComplete failed - continuing.  {0} {1}",
                            e.ToString (), e.StackTrace);
                    }
                }
                if (handlerOnStartupFullyComplete != null)
                {
                    foreach (StartupComplete d in handlerOnStartupFullyComplete.GetInvocationList ())
                    {
                        try
                        {
                            d (scene, StartupData);
                        }
                        catch (Exception e)
                        {
                            m_log.ErrorFormat (
                                "[EVENT MANAGER]: Delegate for StartupComplete failed - continuing.  {0} {1}",
                                e.ToString (), e.StackTrace);
                        }
                    }
                }
            }
        }
    }
}
