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

using System;
using System.Collections.Generic;
using Aurora.Framework.Physics;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.Services.ClassHelpers.Inventory;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using ProtoBuf;

namespace Aurora.Framework.SceneInfo.Entities
{
    public delegate void BlankHandler();

    public enum StateSource
    {
        NewRez = 0,
        PrimCrossing = 1,
        ScriptedRez = 2,
        AttachedRez = 3,
        RegionStart = 4
    }

    [Serializable, ProtoContract()]
    public class KeyframeAnimation
    {
        public enum Modes
        {
            Loop = 4,
            Forward = 16,
            Reverse = 8,
            PingPong = 32
        }

        public enum Commands
        {
            Pause = 2048,
            Play = 1024,
            Stop = 512
        }

        public enum Data
        {
            Translation = 64,
            Rotation = 128,
            Both = 192
        }

        [ProtoMember(1)] public int CurrentAnimationPosition = 0;
        [ProtoMember(2)] public bool PingPongForwardMotion = true;
        [ProtoMember(3)] public Modes CurrentMode = Modes.Forward;
        [ProtoMember(4)] public int CurrentFrame = 0;
        [ProtoMember(5)] public int[] TimeList = new int[0];
        [ProtoMember(6)] public Vector3 InitialPosition = Vector3.Zero;
        [ProtoMember(7)] public Vector3[] PositionList = new Vector3[0];
        [ProtoMember(8)] public Quaternion InitialRotation = Quaternion.Identity;
        [ProtoMember(9)] public Quaternion[] RotationList = new Quaternion[0];

        public OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["CurrentAnimationPosition"] = CurrentAnimationPosition;
            map["CurrentMode"] = (int) CurrentMode;
            OSDArray times = new OSDArray();
            foreach (int time in TimeList)
                times.Add(time);
            map["TimeList"] = times;
            OSDArray positions = new OSDArray();
            foreach (Vector3 v in PositionList)
                positions.Add(v);
            map["PositionList"] = positions;
            OSDArray rotations = new OSDArray();
            foreach (Quaternion v in RotationList)
                rotations.Add(v);
            map["RotationList"] = rotations;
            return map;
        }

        public void FromOSD(OSDMap map)
        {
            CurrentAnimationPosition = map["CurrentAnimationPosition"];
            CurrentMode = (Modes) (int) map["CurrentMode"];
            OSDArray positions = (OSDArray) map["PositionList"];
            List<Vector3> pos = new List<Vector3>();
            foreach (OSD o in positions)
                pos.Add(o);
            PositionList = pos.ToArray();
            OSDArray rotations = (OSDArray) map["RotationList"];
            List<Quaternion> rot = new List<Quaternion>();
            foreach (OSD o in rotations)
                rot.Add(o);
            RotationList = rot.ToArray();
            OSDArray times = (OSDArray) map["TimeList"];
            List<int> time = new List<int>();
            foreach (OSD o in times)
                time.Add(o);
            TimeList = time.ToArray();
        }
    }

    public interface ISceneEntity : IEntity
    {
        #region Get/Set

        IScene Scene { get; set; }
        UUID LastParcelUUID { get; set; }
        Vector3 LastSignificantPosition { get; }
        bool IsDeleted { get; set; }
        Vector3 GroupScale();
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
        Vector3 OOBsize { get; }

        #endregion

        #region Children

        ISceneChildEntity RootChild { get; set; }
        List<ISceneChildEntity> ChildrenEntities();
        void ClearChildren();
        bool AddChild(ISceneChildEntity child, int linkNum);
        bool LinkChild(ISceneChildEntity child);
        bool RemoveChild(ISceneChildEntity child);
        bool GetChildPrim(uint LocalID, out ISceneChildEntity entity);
        bool GetChildPrim(UUID UUID, out ISceneChildEntity entity);
        ISceneChildEntity GetChildPart(UUID objectID);
        ISceneChildEntity GetChildPart(uint childkey);
        void LinkToGroup(ISceneEntity childPrim);
        IEntity GetLinkNumPart(int linkType);

        #endregion

        #region XML

        /// <summary>
        ///     Returns an XML based document that represents this object
        /// </summary>
        /// <returns></returns>
        string ToXml2();

        /// <summary>
        ///     Returns an XML based document that represents this object
        /// </summary>
        /// <returns></returns>
        byte[] ToBinaryXml2();

        #endregion

        Vector3 GetTorque();

        event BlankHandler OnFinishedPhysicalRepresentationBuilding;

        List<UUID> SitTargetAvatar { get; }

        void ClearUndoState();

        void AttachToScene(IScene m_parentScene);

        ISceneEntity Copy(bool copyPhysicsRepresentation);

        void ForcePersistence();

        void RebuildPhysicalRepresentation(bool keepSelectedStatus, Action actionToDoWhilePhysActorNull);

        void ScheduleGroupTerseUpdate();


        void TriggerScriptChangedEvent(Changed changed);


        void ScheduleGroupUpdate(PrimUpdateFlags primUpdateFlags);

        void GetProperties(IClientAPI client);

        ISceneEntity DelinkFromGroup(ISceneChildEntity part, bool p);

        void UpdateGroupPosition(Vector3 vector3, bool p);

        void ResetChildPrimPhysicsPositions();

        Vector3 GetAttachmentPos();

        byte GetAttachmentPoint();

        byte GetSavedAttachmentPoint();

        void SetAttachmentPoint(byte p);

        void CreateScriptInstances(int p, bool p_2, StateSource stateSource, UUID rezzedFrom, bool clearStateSaves);

        void ResumeScripts();

        void SetFromItemID(UUID itemID, UUID assetID);

        void FireAttachmentCollisionEvents(EventArgs e);

        void DetachToInventoryPrep();

        TaskInventoryItem GetInventoryItem(uint localID, UUID itemID);

        int RemoveInventoryItem(uint localID, UUID itemID);

        bool AddInventoryItem(IClientAPI remoteClient, uint primLocalID, InventoryItemBase item, UUID copyID);

        void ScheduleGroupUpdateToAvatar(IScenePresence SP, PrimUpdateFlags primUpdateFlags);

        void SetOwnerId(UUID uUID);

        uint GetEffectivePermissions();

        void SetRootPartOwner(ISceneChildEntity part, UUID uUID, UUID uUID_2);

        void SetGroup(UUID groupID, UUID attemptingUser, bool needsPropertyUpdateForuser);

        void ApplyNextOwnerPermissions();

        bool UpdateInventoryItem(TaskInventoryItem item);

        void DetachToGround();

        void UpdatePermissions(UUID agentID, byte field, uint localId, uint mask, byte set);

        float BSphereRadiusSQ { get; }

        /// <summary>
        ///     Prepares the object to be serialized
        /// </summary>
        void BackupPreparation();

        void RemoveScriptInstances(bool p);

        float GetMass();

        void AddKeyframedMotion(KeyframeAnimation animation, KeyframeAnimation.Commands command);

        void UpdateRootPosition(Vector3 pos);

        void GeneratedMesh(ISceneChildEntity _parent_entity, IMesh _mesh);

        void FinishedSerializingGenericProperties();

        void OffsetForNewRegion(Vector3 oldGroupPosition);

        void SetAbsolutePosition(bool p, Vector3 attemptedPos);

        bool IsInTransit { get; set; }

        void ScriptSetPhysicsStatus(bool p);

        Vector3 GetAxisAlignedBoundingBox(out float offsetHeight);

        void ClearPartAttachmentData();

        void UpdateGroupRotationR(Quaternion rot);

        void ApplyPermissions(uint p);

        EntityIntersection TestIntersection(Ray hRay, bool frontFacesOnly, bool faceCenters);

        void GetAxisAlignedBoundingBoxRaw(out float minX, out float maxX, out float minY, out float maxY,
                                                 out float minZ, out float maxZ);
    }
}