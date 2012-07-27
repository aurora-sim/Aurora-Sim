using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Packets;

namespace Aurora.Framework
{
    #region Enumerations

    public enum PIDHoverType
    {
        Ground,
        GroundAndWater,
        Water,
        Absolute
    }

    /// <summary>
    /// Only used internally to schedule client updates.
    /// </summary>
    /// 
    public enum InternalUpdateFlags : byte
    {
        /// <summary>
        /// no update is scheduled
        /// </summary>
        NoUpdate = 0,

        /// <summary>
        /// terse update scheduled
        /// </summary>
        TerseUpdate = 1,

        /// <summary>
        /// full update scheduled
        /// </summary>
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

    public enum PrimType
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

    public interface ISceneChildEntity : IEntity
    {
        event AddPhysics OnAddPhysics;
        event RemovePhysics OnRemovePhysics;
        ISceneEntity ParentEntity { get; }
        IEntityInventory Inventory { get; }
        void ResetEntityIDs();

        string GenericData { get; }

        PrimFlags Flags { get; set; }

        int UseSoundQueue { get; set; }

        UUID OwnerID { get; set; }
        UUID LastOwnerID { get; set; }

        bool VolumeDetectActive { get; set; }

        UUID GroupID { get; set; }

        UUID CreatorID { get; set; }

        string CreatorData { get; set; }

        Quaternion GetWorldRotation();

        PhysicsObject PhysActor { get; set; }

        TaskInventoryDictionary TaskInventory { get; set; }

        Vector3 GetWorldPosition();

        void RemoveAvatarOnSitTarget(UUID UUID);

        UUID ParentUUID { get; set; }

        float GetMass();

        int AttachmentPoint { get; set; }

        bool CreateSelected { get; set; }

        bool IsAttachment { get; set; }

        void ApplyImpulse(Vector3 applied_linear_impulse, bool p);

        string Description { get; set; }

        Quaternion RotationOffset { get; set; }

        void ScheduleUpdate(PrimUpdateFlags primUpdateFlags);

        string SitAnimation { get; set; }

        Vector3 SitTargetPosition { get; set; }

        Quaternion SitTargetOrientation { get; set; }

        void SetAvatarOnSitTarget(UUID UUID);

        PrimType GetPrimType();

        Vector3 CameraAtOffset { get; set; }

        Vector3 CameraEyeOffset { get; set; }

        bool ForceMouselook { get; set; }

        Vector3 Scale { get; set; }

        uint GetEffectiveObjectFlags();

        int GetNumberOfSides();

        string Text { get; set; }

        Color4 GetTextColor();

        PrimitiveBaseShape Shape { get; set; }

        uint ParentID { get; set; }

        int Material { get; set; }

        UUID AttachedAvatar { get; set; }

        uint OwnerMask { get; set; }

        uint GroupMask { get; set; }

        uint EveryoneMask { get; set; }

        void SetScriptEvents(UUID ItemID, long events);

        UUID FromUserInventoryItemID { get; set; }

        UUID FromUserInventoryAssetID { get; set; }

        Vector3 AngularVelocity { get; set; }

        Vector3 OmegaAxis { get; set; }

        double OmegaSpinRate { get; set; }

        double OmegaGain { get; set; }

        void SetParentLocalId(uint p);

        void SetParent(ISceneEntity grp);

        Vector3 OffsetPosition { get; set; }

        Vector3 AttachedPos { get; set; }

        bool IsRoot { get; }

        void SetConeOfSilence(double p);

        byte SoundFlags { get; set; }

        double SoundGain { get; set; }

        UUID Sound { get; set; }

        double SoundRadius { get; set; }

        void SendSound(string p, double volume, bool p_2, byte p_3, float p_4, bool p_5, bool p_6);

        void PreloadSound(string sound);

        void SetBuoyancy(float p);

        void SetHoverHeight(float p, PIDHoverType hoverType, float p_2);

        void ScheduleTerseUpdate();

        void StopLookAt();

        void RotLookAt(Quaternion rot, float p, float p_2);

        void startLookAt(Quaternion rotation, float p, float p_2);

        Vector3 Acceleration { get; set; }

        void SetAngularImpulse(Vector3 vector3, bool p);

        void ApplyAngularImpulse(Vector3 vector3, bool p);

        void MoveToTarget(Vector3 vector3, float p);

        void StopMoveToTarget();

        void unregisterRotTargetWaypoint(int number);

        int registerRotTargetWaypoint(Quaternion quaternion, float p);

        Vector3 GetForce();

        bool AddFlag(PrimFlags primFlags);

        void AdjustSoundGain(double volume);

        uint BaseMask { get; set; }

        byte ClickAction { get; set; }

        UUID CollisionSound { get; set; }

        float CollisionSoundVolume { get; set; }

        UUID CollisionSprite { get; set; }

        int GetAxisRotation(int p);

        bool GetDieAtEdge();

        Vector3 GetGeometricCenter();

        bool GetReturnAtEdge();

        bool GetStatusSandbox();

        uint NextOwnerMask { get; set; }

        void SetVehicleType(int type);

        void SetVehicleVectorParam(int param, Vector3 vector3);

        void SetVehicleRotationParam(int param, Quaternion quaternion);

        void SetVehicleFlags(int flags, bool p);

        void ScriptSetVolumeDetect(bool p);

        void SetForce(Vector3 vector3);

        int PassCollisions { get; set; }

        int PassTouch { get; set; }

        int registerTargetWaypoint(Vector3 vector3, float p);

        void unregisterTargetWaypoint(int number);

        void ScriptSetPhantomStatus(bool p);

        bool AllowedDrop { get; set; }

        void aggregateScriptEvents();

        int[] PayPrice { get; }

        void SetAxisRotation(int statusrotationaxis, int value);

        void SetStatusSandbox(bool p);

        void SetDieAtEdge(bool p);

        void SetReturnAtEdge(bool p);

        void SetBlockGrab(bool block, bool wholeObject);

        void SetVehicleFloatParam(int param, float p);

        void SetFaceColor(Vector3 vector3, int face);

        void SetSoundQueueing(int queue);

        void FixOffsetPosition(Vector3 vector3, bool p);

        void UpdateOffSet(Vector3 vector3);

        void UpdateRotation(Quaternion rot);

        void AddTextureAnimation(Primitive.TextureAnimation pTexAnim);

        void RemoveParticleSystem();

        void AddNewParticleSystem(Primitive.ParticleSystem prules);

        string SitName { get; set; }

        string TouchName { get; set; }

        int ScriptAccessPin { get; set; }

        void SetFloatOnWater(int floatYN);

        void UpdateTexture(Primitive.TextureEntry tex, bool sendChangedEvent);

        void SetText(string text, Vector3 av3, double p);

        bool UpdatePrimFlags(bool UsePhysics, bool IsTemporary, bool IsPhantom, bool IsVolumeDetect, ObjectFlagUpdatePacket.ExtraPhysicsBlock[] blocks);

        List<UUID> SitTargetAvatar { get; }
        Dictionary<int, string> CollisionFilter { get; }

        bool GetBlockGrab(bool wholeObjectBlock);

        bool RemFlag(PrimFlags primFlags);

        void GetProperties(IClientAPI iClientAPI);

        string MediaUrl { get; set; }

        void TriggerScriptChangedEvent(Changed changed);

        int SavedAttachmentPoint { get; set; }

        Vector3 SavedAttachedPos { get; set; }

        bool IsSelected { get; set; }

        DateTime Rezzed { get; set; }

        byte ObjectSaleType { get; set; }

        int SalePrice { get; set; }

        void ApplyNextOwnerPermissions();

        void StoreUndoState();

        EntityIntersection TestIntersectionOBB(Ray NewRay, Quaternion quaternion, bool frontFacesOnly, bool CopyCenters);

        void UpdateShape(ObjectShapePacket.ObjectDataBlock shapeBlock);

        void Undo();

        void Redo();

        DateTime Expires { get; set; }

        uint CRC { get; set; }

        byte[] ParticleSystem { get; set; }

        int CreationDate { get; set; }

        bool DIE_AT_EDGE { get; set; }

        byte[] TextureAnimation { get; set; }

        Vector3 GroupPosition { get; }
        Vector3 GetGroupPosition();

        Color Color { get; set; }

        void TrimPermissions();

        byte PhysicsType
        {
            get;
            set;
        }

        float Density
        {
            get;
            set;
        }

        float Friction
        {
            get;
            set;
        }

        float Restitution
        {
            get;
            set;
        }

        float GravityMultiplier
        {
            get;
            set;
        }

        float PIDTau
        {
            get;
            set;
        }

        Vector3 PIDTarget
        {
            get;
            set;
        }

        bool PIDActive
        {
            get;
            set;
        }

        float PIDHoverTau
        {
            get;
            set;
        }

        float PIDHoverHeight
        {
            get;
            set;
        }

        bool PIDHoverActive
        {
            get;
            set;
        }

        PIDHoverType PIDHoverType
        {
            get;
            set;
        }

        void GenerateRotationalVelocityFromOmega();

        void ScriptSetTemporaryStatus(bool tempOnRez);

        uint InventorySerial { get; set; }
		void UpdateShape(ObjectShapePacket.ObjectDataBlock shapeBlock, bool b);
    }
}
