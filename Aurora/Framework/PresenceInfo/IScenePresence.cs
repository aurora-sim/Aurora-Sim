using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Aurora.Framework
{
    [Flags]
    public enum PresenceTaint
    {
        TerseUpdate = 1,
        SignificantMovement = 2,
        ObjectUpdates = 4,
        Movement = 8,
        Other = 16
    }

    public delegate void AddPhysics();
    public delegate void RemovePhysics();
    public interface IScenePresence : IEntity, IRegistryCore
    {
        event AddPhysics OnAddPhysics;
        event RemovePhysics OnRemovePhysics;
        event AddPhysics OnSignificantClientMovement;

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

        PhysicsCharacter PhysicsActor { get; set; }

        /// <summary>
        /// Is this client really in this region?
        /// </summary>
        bool IsChildAgent { get; set; }

        /// <summary>
        /// Gets the region handle of the region the root agent is in
        /// </summary>
        ulong RootAgentHandle { get; set; }

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

        /// <summary>
        /// Has this entity just hit the ground and is playing the "STANDUP" animation which freezes the client while it is happening
        /// </summary>
        bool FallenStandUp { get; set; }

        /// <summary>
        /// Is the agent able to fly at all?
        /// </summary>
        bool FlyDisabled { get; set; }

        /// <summary>
        /// Forces the agent to only be able to fly
        /// </summary>
        bool ForceFly { get; set; }

        /// <summary>
        /// What the current (not the ability) god level is set to
        /// </summary>
        int GodLevel { get; set; }

        /// <summary>
        /// Whether the client is running or not
        /// </summary>
        bool SetAlwaysRun { get; set; }

        /// <summary>
        /// Whether the client has busy mode set
        /// </summary>
        bool IsBusy { get; set; }

        /// <summary>
        /// What state the avatar is in (has some OpenMetaverse enum for it)
        /// </summary>
        byte State { get; set; }

        /// <summary>
        /// What flags we have been passed for how the agent is to move in the sim
        /// </summary>
        uint AgentControlFlags { get; set; }

        /// <summary>
        /// How fast the avatar is supposed to be moving (1 is default speeds)
        /// </summary>
        float SpeedModifier { get; set; }

        /// <summary>
        /// Plane generated with what the client is standing on
        /// </summary>
        Vector4 CollisionPlane { get; set; }

        /// <summary>
        /// Whether the agent is able to move at all
        /// </summary>
        bool Frozen { get; set; }

        /// <summary>
        /// What god level the client is 'able' to be (not currently is)
        /// </summary>
        int UserLevel { get; }

        /// <summary>
        /// Teleports the agent to the given pos
        /// </summary>
        /// <param name="Pos"></param>
        void Teleport(Vector3 Pos);

        /// <summary>
        /// The last known allowed position in the sim
        /// </summary>
        Vector3 LastKnownAllowedPosition { get; set; }

        /// <summary>
        /// The GlobalID of the parcel the agent is currently in
        /// </summary>
        UUID CurrentParcelUUID { get; set; }
        /// <summary>
        /// The parcel itself that the agent is currently in
        /// </summary>
        ILandObject CurrentParcel { get; set; }

        /// <summary>
        /// Whether the agent is able to be hurt (whether damage is enabled)
        /// </summary>
        bool Invulnerable { get; set; }

        /// <summary>
        /// How far the agent can see in the region
        /// </summary>
        float DrawDistance { get; set; }

        /// <summary>
        /// Where the camera is at
        /// </summary>
        Vector3 CameraAtAxis { get; }

        /// <summary>
        /// Whether the agent has been removed from the region
        /// </summary>
        bool IsDeleted { get; set; }

        /// <summary>
        /// Whether or not the agent is sitting on the ground
        /// </summary>
        bool SitGround { get; set; }

        /// <summary>
        /// Whether the agent is currently trying to teleport or cross into another region
        /// </summary>
        bool IsInTransit { get; set; }

        /// <summary>
        /// Where the prim that the agent is sitting on is located
        /// </summary>
        Vector3 ParentPosition { get; set; }

        /// <summary>
        /// Pushes the avatar with the given impulse (for llPushObject)
        /// </summary>
        /// <param name="impulse"></param>
        void PushForce(Vector3 impulse);

        /// <summary>
        /// The main update call by the heartbeat
        /// </summary>
        void Update();

        /// <summary>
        /// Update the agent with info from another region
        /// </summary>
        /// <param name="agentData"></param>
        void ChildAgentDataUpdate(AgentData agentData);

        /// <summary>
        /// Update the agent with info from another region
        /// </summary>
        /// <param name="cAgentData"></param>
        /// <param name="regionX"></param>
        /// <param name="regionY"></param>
        /// <param name="globalX"></param>
        /// <param name="globalY"></param>
        void ChildAgentDataUpdate(AgentPosition cAgentData, int regionX, int regionY, int globalX, int globalY);

        /// <summary>
        /// Copies our info to the AgentData class for sending out
        /// </summary>
        /// <param name="agent"></param>
        void CopyTo(AgentData agent);

        /// <summary>
        /// Turns the agent from a child agent into a full root agent
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="isFlying"></param>
        /// <param name="makePhysicalActor"></param>
        void MakeRootAgent(Vector3 pos, bool isFlying, bool makePhysicalActor);

        /// <summary>
        /// Turns the agent into a child agent
        /// </summary>
        /// <param name="destindation"></param>
        void MakeChildAgent(GridRegion destindation);

        /// <summary>
        /// Closes the agent (called when they leave the region)
        /// </summary>
        void Close();

        /// <summary>
        /// Automatically moves the avatar to the given object's position
        /// </summary>
        /// <param name="objectLocalID"></param>
        /// <param name="pos"></param>
        /// <param name="avatar"></param>
        void DoAutoPilot(uint objectLocalID, Vector3 pos, IClientAPI avatar);

        /// <summary>
        /// Teleports the agent (and keeps velocity)
        /// </summary>
        /// <param name="value"></param>
        void TeleportWithMomentum(Vector3 value);

        /// <summary>
        /// Moves the agent to the given position
        /// </summary>
        /// <param name="iClientAPI"></param>
        /// <param name="p"></param>
        /// <param name="coords"></param>
        void DoMoveToPosition(object iClientAPI, string p, List<string> coords);

        /// <summary>
        /// The agent successfully teleported to another region
        /// </summary>
        void SuccessfulTransit();

        /// <summary>
        /// The agent successfully crossed into the given region
        /// </summary>
        /// <param name="CrossingRegion"></param>
        void SuccessfulCrossingTransit(GridRegion CrossingRegion);

        /// <summary>
        /// The agent failed to teleport into another region
        /// </summary>
        void FailedTransit();

        /// <summary>
        /// The agent failed to cross into the given region
        /// </summary>
        /// <param name="failedCrossingRegion"></param>
        void FailedCrossingTransit(GridRegion failedCrossingRegion);

        /// <summary>
        /// Adds a force in the given direction to the avatar
        /// </summary>
        /// <param name="force"></param>
        /// <param name="quaternion"></param>
        void AddNewMovement(Vector3 force, Quaternion quaternion);

        /// <summary>
        /// Adds the agent to the physical scene
        /// </summary>
        /// <param name="m_flying"></param>
        /// <param name="p"></param>
        void AddToPhysicalScene(bool m_flying, bool p);

        /// <summary>
        /// Sends a terse (basic position/rotation/velocity) update to all agents
        /// </summary>
        void SendTerseUpdateToAllClients();

        /// <summary>
        /// Sends locations of all the avies in the region to the client
        /// </summary>
        /// <param name="coarseLocations"></param>
        /// <param name="avatarUUIDs"></param>
        void SendCoarseLocations(List<Vector3> coarseLocations, List<UUID> avatarUUIDs);

        /// <summary>
        /// Tells the client about a new update for the given entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="PostUpdateFlags"></param>
        void AddUpdateToAvatar(ISceneChildEntity entity, PrimUpdateFlags PostUpdateFlags);

        /// <summary>
        /// Makes the avatar stand up
        /// </summary>
        void StandUp();

        /// <summary>
        /// Sets how tall the avatar is in the physics engine (only)
        /// </summary>
        /// <param name="height"></param>
        void SetHeight(float height);

        /// <summary>
        /// What object the avatar is sitting on
        /// </summary>
        UUID SittingOnUUID { get; }

        /// <summary>
        /// Whether the agent is currently sitting
        /// </summary>
        bool Sitting { get; }

        /// <summary>
        /// Clears the saved velocity so that the agent doesn't keep moving if it has no PhysActor
        /// </summary>
        void ClearSavedVelocity();

        /// <summary>
        /// Sits the avatar on the given objectID (targetID) with the given offset (normally ignored though)
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="targetID"></param>
        /// <param name="offset"></param>
        void HandleAgentRequestSit(IClientAPI remoteClient, UUID targetID, Vector3 offset);

        /// <summary>
        /// Whether this avatar is tainted for a scene update
        /// </summary>
        bool IsTainted { get; set; }

        /// <summary>
        /// All taints associated with the avatar
        /// </summary>
        PresenceTaint Taints { get; set; }

        /// <summary>
        /// Gets the absolute position of the avatar
        /// </summary>
        /// <returns></returns>
        Vector3 GetAbsolutePosition();

        /// <summary>
        /// Adds a child agent update taint to the agent
        /// </summary>
        void AddChildAgentUpdateTaint(int seconds);

        /// <summary>
        /// Sets what attachments are on the agent (internal use only)
        /// </summary>
        void SetAttachments(ISceneEntity[] groups);

        /// <summary>
        /// The user has moved a significant (by physics engine standards) amount
        /// </summary>
        void TriggerSignificantClientMovement();

        /// <summary>
        /// The agent is attempting to leave the region for another region
        /// </summary>
        /// <param name="destindation">Where they are going (if null, they are logging out)</param>
        void SetAgentLeaving(GridRegion destindation);

        /// <summary>
        /// The agent failed to make it to the region they were attempting to go (resets SetAgentLeaving)
        /// </summary>
        void AgentFailedToLeave();

        /// <summary>
        /// Whether the agent has fully been moved into the region as a root agent (is cleared if they leave or become a child agent)
        /// </summary>
        bool SuccessfullyMadeRootAgent { get; }

        /// <summary>
        /// Are attachments loaded for this user yet?
        /// </summary>
        bool AttachmentsLoaded { get; set; }
    }
}
