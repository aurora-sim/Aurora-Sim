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
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using OpenMetaverse;
using OpenMetaverse.Packets;
using log4net;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes.Types;
using OpenSim.Region.Physics.Manager;
using OpenSim.Region.Framework.Interfaces;

namespace OpenSim.Region.Framework.Scenes
{
    public delegate void PhysicsCrash();

    public delegate void ObjectDuplicateDelegate(EntityBase original, EntityBase clone);

    public delegate void ObjectCreateDelegate(EntityBase obj);

    public delegate void ObjectDeleteDelegate(EntityBase obj);

    /// <summary>
    /// This class used to be called InnerScene and may not yet truly be a SceneGraph.  The non scene graph components
    /// should be migrated out over time.
    /// </summary>
    public class SceneGraph
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Events

        public event ObjectDuplicateDelegate OnObjectDuplicate;
        public event ObjectCreateDelegate OnObjectCreate;
        public event ObjectDeleteDelegate OnObjectRemove;

        #endregion

        #region Fields

        protected object m_presenceLock = new object();
        protected Dictionary<UUID, ScenePresence> m_scenePresenceMap = new Dictionary<UUID, ScenePresence>();
        protected List<ScenePresence> m_scenePresenceArray = new List<ScenePresence>();

        protected internal EntityManager Entities = new EntityManager();
        
        protected RegionInfo m_regInfo;
        protected Scene m_parentScene;
        protected List<UUID> m_updateList = new List<UUID>();
        protected int m_numRootAgents = 0;
        protected int m_numPrim = 0;
        protected int m_numChildAgents = 0;
        protected int m_physicalPrim = 0;

        protected int m_activeScripts = 0;
        protected int m_scriptEPS = 0;

        protected internal object m_syncRoot = new object();

        protected internal PhysicsScene _PhyScene;

        private Object m_updateLock = new Object();

        #endregion

        protected internal SceneGraph(Scene parent, RegionInfo regInfo)
        {
            m_parentScene = parent;
            m_regInfo = regInfo;
        }

        public PhysicsScene PhysicsScene
        {
            get { return _PhyScene; }
            set
            {
                _PhyScene = value;
            }
        }

        protected internal void Close()
        {
            lock (m_presenceLock)
            {
                Dictionary<UUID, ScenePresence> newmap = new Dictionary<UUID, ScenePresence>();
                List<ScenePresence> newlist = new List<ScenePresence>();
                m_scenePresenceMap = newmap;
                m_scenePresenceArray = newlist;
            }

            Entities.Clear();
        }

        #region Update Methods

        protected internal void UpdatePreparePhysics()
        {
            // If we are using a threaded physics engine
            // grab the latest scene from the engine before
            // trying to process it.

            // PhysX does this (runs in the background).

            if (_PhyScene.IsThreaded)
            {
                _PhyScene.GetResults();
            }
        }

        protected internal void UpdatePresences()
        {
            ForEachScenePresence(delegate(ScenePresence presence)
            {
                presence.Update();
            });
        }

        protected internal float UpdatePhysics(double elapsed)
        {
            lock (m_syncRoot)
            {
                // Update DisableCollisions 
                _PhyScene.DisableCollisions = m_regInfo.RegionSettings.DisableCollisions;
               
                // Here is where the Scene calls the PhysicsScene. This is a one-way
                // interaction; the PhysicsScene cannot access the calling Scene directly.
                // But with joints, we want a PhysicsActor to be able to influence a
                // non-physics SceneObjectPart. In particular, a PhysicsActor that is connected
                // with a joint should be able to move the SceneObjectPart which is the visual
                // representation of that joint (for editing and serialization purposes).
                // However the PhysicsActor normally cannot directly influence anything outside
                // of the PhysicsScene, and the non-physical SceneObjectPart which represents
                // the joint in the Scene does not exist in the PhysicsScene.
                //
                // To solve this, we have an event in the PhysicsScene that is fired when a joint
                // has changed position (because one of its associated PhysicsActors has changed 
                // position).
                //
                // Therefore, JointMoved and JointDeactivated events will be fired as a result of the following Simulate().

                return _PhyScene.Simulate((float)elapsed);
            }
        }

        protected internal void UpdateScenePresenceMovement()
        {
            ForEachScenePresence(delegate(ScenePresence presence)
            {
                presence.UpdateMovement();
            });
        }

        public void GetCoarseLocations(out List<Vector3> coarseLocations, out List<UUID> avatarUUIDs, uint maxLocations)
        {
            coarseLocations = new List<Vector3>();
            avatarUUIDs = new List<UUID>();

            List<ScenePresence> presences = GetScenePresences();
            for (int i = 0; i < Math.Min(presences.Count, maxLocations); i++)
            {
                ScenePresence sp = presences[i];
                // If this presence is a child agent, we don't want its coarse locations
                if (sp.IsChildAgent)
                    continue;

                if (sp.ParentID != UUID.Zero)
                {
                    // sitting avatar
                    SceneObjectPart sop = m_parentScene.GetSceneObjectPart(sp.ParentID);
                    if (sop != null)
                    {
                        coarseLocations.Add(sop.AbsolutePosition + sp.OffsetPosition);
                        avatarUUIDs.Add(sp.UUID);
                    }
                    else
                    {
                        // we can't find the parent..  ! arg!
                        m_log.Warn("Could not find parent prim for avatar " + sp.Name);
                        coarseLocations.Add(sp.AbsolutePosition);
                        avatarUUIDs.Add(sp.UUID);
                    }
                }
                else
                {
                    coarseLocations.Add(sp.AbsolutePosition);
                    avatarUUIDs.Add(sp.UUID);
                }
            }
        }

        #endregion

        #region Entity Methods

        /// <summary>
        /// Add an object into the scene that has come from storage
        /// </summary>
        /// <param name="sceneObject"></param>
        /// <param name="attachToBackup">
        /// If true, changes to the object will be reflected in its persisted data
        /// If false, the persisted data will not be changed even if the object in the scene is changed
        /// </param>
        /// <param name="alreadyPersisted">
        /// If true, we won't persist this object until it changes
        /// If false, we'll persist this object immediately
        /// </param>
        /// <param name="sendClientUpdates">
        /// If true, we send updates to the client to tell it about this object
        /// If false, we leave it up to the caller to do this
        /// </param>
        /// <returns>
        /// true if the object was added, false if an object with the same uuid was already in the scene
        /// </returns>
        protected internal bool AddRestoredSceneObject(
            SceneObjectGroup sceneObject, bool attachToBackup, bool alreadyPersisted, bool sendClientUpdates)
        {
            if (!alreadyPersisted)
            {
                sceneObject.ForceInventoryPersistence();
                sceneObject.HasGroupChanged = true;
            }

            return AddSceneObject(sceneObject, attachToBackup, sendClientUpdates);
        }
                
        /// <summary>
        /// Add a newly created object to the scene.  This will both update the scene, and send information about the
        /// new object to all clients interested in the scene.
        /// </summary>
        /// <param name="sceneObject"></param>
        /// <param name="attachToBackup">
        /// If true, the object is made persistent into the scene.
        /// If false, the object will not persist over server restarts
        /// </param>
        /// <returns>
        /// true if the object was added, false if an object with the same uuid was already in the scene
        /// </returns>
        protected internal bool AddNewSceneObject(SceneObjectGroup sceneObject, bool attachToBackup, bool sendClientUpdates)
        {
            // Ensure that we persist this new scene object
            sceneObject.HasGroupChanged = true;

            return AddSceneObject(sceneObject, attachToBackup, sendClientUpdates);
        }
        
        /// <summary>
        /// Add a newly created object to the scene.
        /// </summary>
        /// 
        /// This method does not send updates to the client - callers need to handle this themselves.
        /// <param name="sceneObject"></param>
        /// <param name="attachToBackup"></param>
        /// <param name="pos">Position of the object</param>
        /// <param name="rot">Rotation of the object</param>
        /// <param name="vel">Velocity of the object.  This parameter only has an effect if the object is physical</param>
        /// <returns></returns>
        public bool AddNewSceneObject(
            SceneObjectGroup sceneObject, bool attachToBackup, Vector3 pos, Quaternion rot, Vector3 vel)
        {
            AddNewSceneObject(sceneObject, true, false);

            // we set it's position in world.
            sceneObject.AbsolutePosition = pos;

            if (sceneObject.RootPart.Shape.PCode == (byte)PCode.Prim)
            {
                sceneObject.ClearPartAttachmentData();
            }
            
            sceneObject.UpdateGroupRotationR(rot);
            
            //group.ApplyPhysics(m_physicalPrim);
            if (sceneObject.RootPart.PhysActor != null && sceneObject.RootPart.PhysActor.IsPhysical && vel != Vector3.Zero)
            {
                sceneObject.RootPart.ApplyImpulse((vel * sceneObject.GetMass()), false);
                sceneObject.Velocity = vel;
            }
        
            return true;
        }

        /// <summary>
        /// Add an object to the scene.  This will both update the scene, and send information about the
        /// new object to all clients interested in the scene.
        /// </summary>
        /// <param name="sceneObject"></param>
        /// <param name="attachToBackup">
        /// If true, the object is made persistent into the scene.
        /// If false, the object will not persist over server restarts
        /// </param>
        /// <param name="sendClientUpdates">
        /// If true, updates for the new scene object are sent to all viewers in range.
        /// If false, it is left to the caller to schedule the update
        /// </param>
        /// <returns>
        /// true if the object was added, false if an object with the same uuid was already in the scene
        /// </returns>
        protected bool AddSceneObject(SceneObjectGroup sceneObject, bool attachToBackup, bool sendClientUpdates)
        {
            if (sceneObject == null || sceneObject.RootPart == null || sceneObject.RootPart.UUID == UUID.Zero)
                return false;

            //if (Entities.ContainsKey(sceneObject.UUID))
            //{
            //    m_log.WarnFormat(
            //            "[SCENE GRAPH]: Scene object {0} {1} was already in region {2} on add request",
            //            sceneObject.Name, sceneObject.UUID, m_parentScene.RegionInfo.RegionName);
            //    return false;
            //}

            IOpenRegionSettingsModule WSModule = sceneObject.Scene.RequestModuleInterface<IOpenRegionSettingsModule>();
            if (WSModule != null)
            {
                foreach (SceneObjectPart part in sceneObject.ChildrenList)
                {
                    if (part.Shape == null)
                        continue;

                    if (WSModule.MaximumPhysPrimScale == -1 ||
                        WSModule.MinimumPrimScale == -1 ||
                        WSModule.MaximumPrimScale == -1)
                        break;

                    Vector3 scale = part.Shape.Scale;

                    if (scale.X < WSModule.MinimumPrimScale)
                        scale.X = WSModule.MinimumPrimScale;
                    if (scale.Y < WSModule.MinimumPrimScale)
                        scale.Y = WSModule.MinimumPrimScale;
                    if (scale.Z < WSModule.MinimumPrimScale)
                        scale.Z = WSModule.MinimumPrimScale;

                    if (part.ParentGroup.RootPart.PhysActor != null && part.ParentGroup.RootPart.PhysActor.IsPhysical)
                    {
                        if (scale.X > WSModule.MaximumPhysPrimScale)
                            scale.X = WSModule.MaximumPhysPrimScale;
                        if (scale.Y > WSModule.MaximumPhysPrimScale)
                            scale.Y = WSModule.MaximumPhysPrimScale;
                        if (scale.Z > WSModule.MaximumPhysPrimScale)
                            scale.Z = WSModule.MaximumPhysPrimScale;
                    }

                    if (scale.X > WSModule.MaximumPrimScale)
                        scale.X = WSModule.MaximumPrimScale;
                    if (scale.Y > WSModule.MaximumPrimScale)
                        scale.Y = WSModule.MaximumPrimScale;
                    if (scale.Z > WSModule.MaximumPrimScale)
                        scale.Z = WSModule.MaximumPrimScale;

                    part.Shape.Scale = scale;
                }
            }
            sceneObject.AttachToScene(m_parentScene);
            Entities.Add(sceneObject);

            if (sendClientUpdates)
                sceneObject.ScheduleGroupForFullUpdate(PrimUpdateFlags.FullUpdate);

            m_numPrim += sceneObject.ChildrenList.Count;
            return true;
        }

        /// <summary>
        /// Delete an object from the scene
        /// </summary>
        /// <returns>true if the object was deleted, false if there was no object to delete</returns>
        public bool DeleteSceneObject(UUID uuid, bool resultOfObjectLinked)
        {
            EntityBase entity;
            if (!Entities.TryGetValue(uuid, out entity) && entity is SceneObjectGroup)
                return false;

            if (entity == null)
                return false;

            SceneObjectGroup grp = (SceneObjectGroup)entity;

            if (!resultOfObjectLinked)
            {
                m_numPrim -= grp.PrimCount;

                if ((grp.RootPart.Flags & PrimFlags.Physics) == PrimFlags.Physics)
                    RemovePhysicalPrim(grp.PrimCount);
            }

            if (OnObjectRemove != null)
            {
                OnObjectRemove(Entities[uuid]);
                Entities.Remove(uuid);
            }

            return true;
        }

        /// <summary>
        /// Add an object to the list of prims to process on the next update
        /// </summary>
        /// <param name="obj">
        /// <see cref="SceneObjectGroup"/>
        /// </param>
        protected internal void AddToUpdateList(SceneObjectGroup obj)
        {
            lock (m_updateList)
                if(!m_updateList.Contains(obj.UUID))
                    m_updateList.Add(obj.UUID);
        }

        /// <summary>
        /// Process all pending updates
        /// </summary>
        protected internal void UpdateObjectGroups()
        {
            if (!Monitor.TryEnter(m_updateLock))
                return;
            try
            {
                List<UUID> updates;

                // Some updates add more updates to the updateList. 
                // Get the current list of updates and clear the list before iterating
                lock (m_updateList)
                {
                    updates = new List<UUID>(m_updateList);
                    m_updateList.Clear();
                }

                // Go through all updates
                for (int i = 0; i < updates.Count; i++)
                {
                    UUID ID = updates[i];

                    // Don't abort the whole update if one entity happens to give us an exception.
                    try
                    {
                        EntityBase e = null;
                        Entities.TryGetValue(ID, out e);
                        if(e != null)
                            ((SceneObjectGroup)e).Update();
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat(
                            "[INNER SCENE]: Failed to update {0} - {2}", ID, e);
                    }
                }
            }
            finally
            {
                Monitor.Exit(m_updateLock);
            }
        }

        protected internal void AddPhysicalPrim(int number)
        {
            m_physicalPrim++;
        }

        protected internal void RemovePhysicalPrim(int number)
        {
            m_physicalPrim--;
        }

        protected internal void AddToScriptEPS(int number)
        {
            m_scriptEPS += number;
        }

        protected internal void AddActiveScripts(int number)
        {
            m_activeScripts += number;
        }

        public void DropObject(uint objectLocalID, IClientAPI remoteClient)
        {
            SceneObjectGroup group = GetGroupByPrim(objectLocalID);
            if (group != null)
                m_parentScene.AttachmentsModule.DetachSingleAttachmentToGround(group.UUID, remoteClient);
        }

        protected internal void DetachObject(uint objectLocalID, IClientAPI remoteClient)
        {
            SceneObjectGroup group = GetGroupByPrim(objectLocalID);
            if (group != null)
            {
                m_parentScene.AttachmentsModule.ShowDetachInUserInventory(group.GetFromItemID(), remoteClient);
            }
        }

        protected internal void HandleUndo(IClientAPI remoteClient, UUID primId)
        {
            if (primId != UUID.Zero)
            {
                SceneObjectPart part =  m_parentScene.GetSceneObjectPart(primId);
                if (part != null)

                    if (m_parentScene.Permissions.CanEditObject(part.UUID, remoteClient.AgentId))
                        part.Undo();
            }
        }

        protected internal void HandleRedo(IClientAPI remoteClient, UUID primId)
        {
            if (primId != UUID.Zero)
            {
                SceneObjectPart part = m_parentScene.GetSceneObjectPart(primId);
                if (part != null)
                    if (m_parentScene.Permissions.CanEditObject(part.UUID, remoteClient.AgentId))
                        part.Redo();
            }
        }

        protected internal void HandleObjectGroupUpdate(
            IClientAPI remoteClient, UUID GroupID, uint objectLocalID, UUID Garbage)
        {
            if (!remoteClient.IsGroupMember(GroupID))
                return; // No settings to groups you arn't in
            SceneObjectGroup group = GetGroupByPrim(objectLocalID);
            if (group != null)
            { 
                if(m_parentScene.Permissions.CanEditObject(group.UUID, remoteClient.AgentId))
                    if (group.OwnerID == remoteClient.AgentId)
                        group.SetGroup(GroupID, remoteClient);
            }
        }

        protected internal ScenePresence CreateAndAddChildScenePresence(IClientAPI client, AvatarAppearance appearance)
        {
            ScenePresence newAvatar = null;

            newAvatar = new ScenePresence(client, m_parentScene, m_regInfo, appearance);
            newAvatar.IsChildAgent = true;

            AddScenePresence(newAvatar);

            return newAvatar;
        }

        /// <summary>
        /// Add a presence to the scene
        /// </summary>
        /// <param name="presence"></param>
        protected internal void AddScenePresence(ScenePresence presence)
        {
            bool child = presence.IsChildAgent;

            if (child)
            {
                m_numChildAgents++;
            }
            else
            {
                m_numRootAgents++;
                presence.AddToPhysicalScene(false, false);
            }

            Entities[presence.UUID] = presence;

            lock (m_presenceLock)
            {
                Dictionary<UUID, ScenePresence> newmap = new Dictionary<UUID, ScenePresence>(m_scenePresenceMap);
                List<ScenePresence> newlist = new List<ScenePresence>(m_scenePresenceArray);

                if (!newmap.ContainsKey(presence.UUID))
                {
                    newmap.Add(presence.UUID, presence);
                    newlist.Add(presence);
                }
                else
                {
                    // Remember the old presene reference from the dictionary
                    ScenePresence oldref = newmap[presence.UUID];
                    // Replace the presence reference in the dictionary with the new value
                    newmap[presence.UUID] = presence;
                    // Find the index in the list where the old ref was stored and update the reference
                    newlist[newlist.IndexOf(oldref)] = presence;
                }

                // Swap out the dictionary and list with new references
                m_scenePresenceMap = newmap;
                m_scenePresenceArray = newlist;
            }
        }

        /// <summary>
        /// Remove a presence from the scene
        /// </summary>
        protected internal void RemoveScenePresence(UUID agentID)
        {
            if (!Entities.Remove(agentID))
            {
                m_log.WarnFormat(
                    "[SCENE]: Tried to remove non-existent scene presence with agent ID {0} from scene Entities list",
                    agentID);
            }

            lock (m_presenceLock)
            {
                Dictionary<UUID, ScenePresence> newmap = new Dictionary<UUID, ScenePresence>(m_scenePresenceMap);
                List<ScenePresence> newlist = new List<ScenePresence>(m_scenePresenceArray);
                
                // Remove the presence reference from the dictionary
                if (newmap.ContainsKey(agentID))
                {
                    ScenePresence oldref = newmap[agentID];
                    newmap.Remove(agentID);

                    // Find the index in the list where the old ref was stored and remove the reference
                    newlist.RemoveAt(newlist.IndexOf(oldref));
                    // Swap out the dictionary and list with new references
                    m_scenePresenceMap = newmap;
                    m_scenePresenceArray = newlist;
                }
                else
                {
                    m_log.WarnFormat("[SCENE]: Tried to remove non-existent scene presence with agent ID {0} from scene ScenePresences list", agentID);
                }
            }
        }

        protected internal void SwapRootChildAgent(bool direction_RC_CR_T_F)
        {
            if (direction_RC_CR_T_F)
            {
                m_numRootAgents--;
                m_numChildAgents++;
            }
            else
            {
                m_numChildAgents--;
                m_numRootAgents++;
            }
        }

        public void removeUserCount(bool TypeRCTF)
        {
            if (TypeRCTF)
            {
                m_numRootAgents--;
            }
            else
            {
                m_numChildAgents--;
            }
        }

        public void RecalculateStats()
        {
            int rootcount = 0;
            int childcount = 0;

            ForEachScenePresence(delegate(ScenePresence presence)
            {
                if (presence.IsChildAgent)
                    ++childcount;
                else
                    ++rootcount;
            });

            m_numRootAgents = rootcount;
            m_numChildAgents = childcount;
        }

        public int GetChildAgentCount()
        {
            // some network situations come in where child agents get closed twice.
            if (m_numChildAgents < 0)
            {
                m_numChildAgents = 0;
            }

            return m_numChildAgents;
        }

        public int GetRootAgentCount()
        {
            return m_numRootAgents;
        }

        public int GetTotalObjectsCount()
        {
            return m_numPrim;
        }

        public int GetActiveObjectsCount()
        {
            return m_physicalPrim;
        }

        public int GetActiveScriptsCount()
        {
            return m_activeScripts;
        }

        public int GetScriptEPS()
        {
            int returnval = m_scriptEPS;
            m_scriptEPS = 0;
            return returnval;
        }

        #endregion

        #region Get Methods
        /// <summary>
        /// Get the controlling client for the given avatar, if there is one.
        ///
        /// FIXME: The only user of the method right now is Caps.cs, in order to resolve a client API since it can't
        /// use the ScenePresence.  This could be better solved in a number of ways - we could establish an
        /// OpenSim.Framework.IScenePresence, or move the caps code into a region package (which might be the more
        /// suitable solution).
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns>null if either the avatar wasn't in the scene, or
        /// they do not have a controlling client</returns>
        /// <remarks>this used to be protected internal, but that
        /// prevents CapabilitiesModule from accessing it</remarks>
        public IClientAPI GetControllingClient(UUID agentId)
        {
            ScenePresence presence = GetScenePresence(agentId);

            if (presence != null)
            {
                return presence.ControllingClient;
            }

            return null;
        }

        /// <summary>
        /// Get a reference to the scene presence list. Changes to the list will be done in a copy
        /// There is no guarantee that presences will remain in the scene after the list is returned.
        /// This list should remain private to SceneGraph. Callers wishing to iterate should instead
        /// pass a delegate to ForEachScenePresence.
        /// </summary>
        /// <returns></returns>
        private List<ScenePresence> GetScenePresences()
        {
            return m_scenePresenceArray;
        }

        public List<ScenePresence> ScenePresences
        {
            get { return m_scenePresenceArray; }
        }

        /// <summary>
        /// Request a scene presence by UUID. Fast, indexed lookup.
        /// </summary>
        /// <param name="agentID"></param>
        /// <returns>null if the presence was not found</returns>
        protected internal ScenePresence GetScenePresence(UUID agentID)
        {
            Dictionary<UUID, ScenePresence> presences = m_scenePresenceMap;
            ScenePresence presence;
            presences.TryGetValue(agentID, out presence);
            return presence;
        }

        /// <summary>
        /// Request the scene presence by name.
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <returns>null if the presence was not found</returns>
        protected internal ScenePresence GetScenePresence(string firstName, string lastName)
        {
            List<ScenePresence> presences = GetScenePresences();
            foreach (ScenePresence presence in presences)
            {
                if (presence.Firstname == firstName && presence.Lastname == lastName)
                    return presence;
            }
            return null;
        }

        /// <summary>
        /// Request the scene presence by localID.
        /// </summary>
        /// <param name="localID"></param>
        /// <returns>null if the presence was not found</returns>
        protected internal ScenePresence GetScenePresence(uint localID)
        {
            List<ScenePresence> presences = GetScenePresences();
            foreach (ScenePresence presence in presences)
                if (presence.LocalId == localID)
                    return presence;
            return null;
        }

        protected internal bool TryGetScenePresence(UUID agentID, out ScenePresence avatar)
        {
            Dictionary<UUID, ScenePresence> presences = m_scenePresenceMap;
            presences.TryGetValue(agentID, out avatar);
            return (avatar != null);
        }

        protected internal bool TryGetAvatarByName(string name, out ScenePresence avatar)
        {
            avatar = null;
            foreach (ScenePresence presence in GetScenePresences())
            {
                if (String.Compare(name, presence.ControllingClient.Name, true) == 0)
                {
                    avatar = presence;
                    break;
                }
            }
            return (avatar != null);
        }

        /// <summary>
        /// Get a scene object group that contains the prim with the given local id
        /// </summary>
        /// <param name="localID"></param>
        /// <returns>null if no scene object group containing that prim is found</returns>
        public SceneObjectGroup GetGroupByPrim(uint localID)
        {
            if (localID == 0)
            {
                m_log.Warn("[SCENEGRAPH]: Found localID 0 in GetGroupByPrim " + Environment.StackTrace);
                return null;
            }
            EntityBase entity;
            if (Entities.TryGetValue(localID, out entity))
                return entity as SceneObjectGroup;

            if (Entities.TryGetChildPrimParent(localID, out entity))
                return entity as SceneObjectGroup;

            m_log.DebugFormat("Entered deprecated GetGroupByPrim with localID {0}", localID);

            EntityBase[] EntityList = GetEntities();
            foreach (EntityBase ent in EntityList)
            {
                //m_log.DebugFormat("Looking at entity {0}", ent.UUID);
                if (ent is SceneObjectGroup)
                {
                    {
                        return (SceneObjectGroup)ent;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get a scene object group that contains the prim with the given uuid
        /// </summary>
        /// <param name="fullID"></param>
        /// <returns>null if no scene object group containing that prim is found</returns>
        private SceneObjectGroup GetGroupByPrim(UUID fullID)
        {
            EntityBase entity = null;
            if (Entities.TryGetChildPrimParent(fullID, out entity))
                return entity as SceneObjectGroup;

            if (fullID == UUID.Zero)
                return null; //This isn't an entity.

            //m_log.Warn("Falling back on deprecated method of finding child prim! Mantis this!");
            EntityBase[] EntityList = GetEntities();

            foreach (EntityBase ent in EntityList)
            {
                if (ent is SceneObjectGroup)
                {
                    if(((SceneObjectGroup)ent).UUID == fullID)
                        return (SceneObjectGroup)ent;
                }
            }

            return null;
        }

        protected internal EntityIntersection GetClosestIntersectingPrim(Ray hray, bool frontFacesOnly, bool faceCenters)
        {
            // Primitive Ray Tracing
            float closestDistance = 280f;
            EntityIntersection result = new EntityIntersection();
            EntityBase[] EntityList = GetEntities();
            foreach (EntityBase ent in EntityList)
            {
                if (ent is SceneObjectGroup)
                {
                    SceneObjectGroup reportingG = (SceneObjectGroup)ent;
                    EntityIntersection inter = reportingG.TestIntersection(hray, frontFacesOnly, faceCenters);
                    if (inter.HitTF && inter.distance < closestDistance)
                    {
                        closestDistance = inter.distance;
                        result = inter;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Get a part contained in this scene.
        /// </summary>
        /// <param name="localID"></param>
        /// <returns>null if the part was not found</returns>
        protected internal SceneObjectPart GetSceneObjectPart(uint localID)
        {
            SceneObjectGroup group = GetGroupByPrim(localID);
            if (group == null)
                return null;
            return group.GetChildPart(localID);
        }
        
        /// <summary>
        /// Get a named prim contained in this scene (will return the first 
        /// found, if there are more than one prim with the same name)
        /// </summary>
        /// <param name="name"></param>
        /// <returns>null if the part was not found</returns>
        protected internal SceneObjectPart GetSceneObjectPart(string name)
        {
            SceneObjectPart sop = null;

            Entities.Find(
                delegate(EntityBase entity)
                {
                    if (entity is SceneObjectGroup)
                    {
                        foreach (SceneObjectPart p in ((SceneObjectGroup)entity).Parts)
                        {
                            if (p.Name == name)
                            {
                                sop = p;
                                return true;
                            }
                        }
                    }

                    return false;
                }
            );

            return sop;
        }

        /// <summary>
        /// Get a part contained in this scene.
        /// </summary>
        /// <param name="fullID"></param>
        /// <returns>null if the part was not found</returns>
        protected internal SceneObjectPart GetSceneObjectPart(UUID fullID)
        {
            SceneObjectGroup group = GetGroupByPrim(fullID);
            if (group == null)
                return null;
            return group.GetChildPart(fullID);
        }

        /// <summary>
        /// Returns a list of the entities in the scene.  This is a new list so no locking is required to iterate over
        /// it
        /// </summary>
        /// <returns></returns>
        protected internal EntityBase[] GetEntities()
        {
            return Entities.GetEntities();
        }

        public Dictionary<uint, float> GetTopScripts()
        {
            Dictionary<uint, float> topScripts = new Dictionary<uint, float>();

            EntityBase[] EntityList = GetEntities();
            int limit = 0;
            foreach (EntityBase ent in EntityList)
            {
                if (ent is SceneObjectGroup)
                {
                    SceneObjectGroup grp = (SceneObjectGroup)ent;
                    if ((grp.RootPart.GetEffectiveObjectFlags() & (uint)PrimFlags.Scripted) != 0)
                    {
                        if (grp.scriptScore >= 0.01)
                        {
                            topScripts.Add(grp.LocalId, grp.scriptScore);
                            limit++;
                            if (limit >= 100)
                            {
                                break;
                            }
                        }
                        grp.scriptScore = 0;
                    }
                }
            }

            return topScripts;
        }

        #endregion

        #region Other Methods

        protected internal UUID ConvertLocalIDToFullID(uint localID)
        {
            SceneObjectGroup group = GetGroupByPrim(localID);
            if (group != null)
                return group.GetPartsFullID(localID);
            else
                return UUID.Zero;
        }

        /// <summary>
        /// Performs action on all scene object groups.
        /// </summary>
        /// <param name="action"></param>
        protected internal void ForEachSOG(Action<SceneObjectGroup> action)
        {
            EntityBase[] objlist = Entities.GetEntities();
            foreach (EntityBase obj in objlist)
            {
                try
                {
                    if(obj is SceneObjectGroup)
                        action(obj as SceneObjectGroup);
                }
                catch (Exception e)
                {
                    // Catch it and move on. This includes situations where splist has inconsistent info
                    m_log.WarnFormat("[SCENE]: Problem processing action in ForEachSOG: {0}", e.Message);
                }
            }
        }

        
        /// <summary>
        /// Performs action on all scene presences. This can ultimately run the actions in parallel but
        /// any delegates passed in will need to implement their own locking on data they reference and
        /// modify outside of the scope of the delegate. 
        /// </summary>
        /// <param name="action"></param>
        public void ForEachScenePresence(Action<ScenePresence> action)
        {
            // Once all callers have their delegates configured for parallelism, we can unleash this
            /*
            Action<ScenePresence> protectedAction = new Action<ScenePresence>(delegate(ScenePresence sp)
                {
                    try
                    {
                        action(sp);
                    }
                    catch (Exception e)
                    {
                        m_log.Info("[BUG] in " + m_parentScene.RegionInfo.RegionName + ": " + e.ToString());
                        m_log.Info("[BUG] Stack Trace: " + e.StackTrace);
                    }
                });
            Parallel.ForEach<ScenePresence>(GetScenePresences(), protectedAction);
            */
            // For now, perform actions serially
            List<ScenePresence> presences = GetScenePresences();
            foreach (ScenePresence sp in presences)
            {
                try
                {
                    action(sp);
                }
                catch (Exception e)
                {
                    m_log.Info("[BUG] in " + m_parentScene.RegionInfo.RegionName + ": " + e.ToString());
                    m_log.Info("[BUG] Stack Trace: " + e.StackTrace);
                }
            }
        }

        #endregion

        #region Client Event handlers

        /// <summary>
        ///
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="scale"></param>
        /// <param name="remoteClient"></param>
        protected internal void UpdatePrimScale(uint localID, Vector3 scale, IClientAPI remoteClient)
        {
            SceneObjectGroup group = GetGroupByPrim(localID);
            if (group != null)
            {
                if (m_parentScene.Permissions.CanEditObject(group.UUID, remoteClient.AgentId))
                {
                    group.Resize(scale, localID);
                }
            }
        }

        protected internal void UpdatePrimGroupScale(uint localID, Vector3 scale, IClientAPI remoteClient)
        {
            SceneObjectGroup group = GetGroupByPrim(localID);
            if (group != null)
            {
                if (m_parentScene.Permissions.CanEditObject(group.UUID, remoteClient.AgentId))
                {
                    group.GroupResize(scale, localID);
                }
            }
        }

        /// <summary>
        /// This handles the nifty little tool tip that you get when you drag your mouse over an object
        /// Send to the Object Group to process.  We don't know enough to service the request
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="AgentID"></param>
        /// <param name="RequestFlags"></param>
        /// <param name="ObjectID"></param>
        protected internal void RequestObjectPropertiesFamily(
             IClientAPI remoteClient, UUID AgentID, uint RequestFlags, UUID ObjectID)
        {
            SceneObjectGroup group = GetGroupByPrim(ObjectID);
            if (group != null)
            {
                group.ServiceObjectPropertiesFamilyRequest(remoteClient, AgentID, RequestFlags);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="rot"></param>
        /// <param name="remoteClient"></param>
        protected internal void UpdatePrimSingleRotation(uint localID, Quaternion rot, IClientAPI remoteClient)
        {
            SceneObjectGroup group = GetGroupByPrim(localID);
            if (group != null)
            {
                if (m_parentScene.Permissions.CanMoveObject(group.UUID, remoteClient.AgentId))
                {
                    group.UpdateSingleRotation(rot, localID);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="rot"></param>
        /// <param name="remoteClient"></param>
        protected internal void UpdatePrimSingleRotationPosition(uint localID, Quaternion rot, Vector3 pos, IClientAPI remoteClient)
        {
            SceneObjectGroup group = GetGroupByPrim(localID);
            if (group != null)
            {
                if (m_parentScene.Permissions.CanMoveObject(group.UUID, remoteClient.AgentId))
                {
                    group.UpdateSingleRotation(rot,pos, localID);
                }
            }
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="rot"></param>
        /// <param name="remoteClient"></param>
        protected internal void UpdatePrimRotation(uint localID, Quaternion rot, IClientAPI remoteClient)
        {
            SceneObjectGroup group = GetGroupByPrim(localID);
            if (group != null)
            {
                if (m_parentScene.Permissions.CanMoveObject(group.UUID, remoteClient.AgentId))
                {
                    group.UpdateGroupRotationR(rot);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="remoteClient"></param>
        protected internal void UpdatePrimRotation(uint localID, Vector3 pos, Quaternion rot, IClientAPI remoteClient)
        {
            SceneObjectGroup group = GetGroupByPrim(localID);
            if (group != null)
            {
                if (m_parentScene.Permissions.CanMoveObject(group.UUID, remoteClient.AgentId))
                {
                    group.UpdateGroupRotationPR(pos, rot);
                }
            }
        }

        /// <summary>
        /// Update the position of the given part
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="pos"></param>
        /// <param name="remoteClient"></param>
        protected internal void UpdatePrimSinglePosition(uint localID, Vector3 pos, IClientAPI remoteClient, bool SaveUpdate)
        {
            SceneObjectGroup group = GetGroupByPrim(localID);
            if (group != null)
            {
                if (m_parentScene.Permissions.CanMoveObject(group.UUID, remoteClient.AgentId) || group.IsAttachment)
                {
                    group.UpdateSinglePosition(pos, localID, SaveUpdate);
                }
            }
        }

        /// <summary>
        /// Update the position of the given part
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="pos"></param>
        /// <param name="remoteClient"></param>
        protected internal void UpdatePrimPosition(uint localID, Vector3 pos, IClientAPI remoteClient, bool SaveUpdate)
        {
            SceneObjectGroup group = GetGroupByPrim(localID);
            
            if (group != null)
            {
                //Move has edit permission as well
                if (m_parentScene.Permissions.CanMoveObject(group.UUID, remoteClient.AgentId))
                {
                    if (group.IsAttachment || (group.RootPart.Shape.PCode == 9 && group.RootPart.Shape.State != 0))
                    {
                        if (m_parentScene.AttachmentsModule != null)
                            m_parentScene.AttachmentsModule.UpdateAttachmentPosition(remoteClient, group, pos);
                    }
                    else
                    {
                        group.UpdateGroupPosition(pos, SaveUpdate);
                    }
                }
                else
                {
                    ScenePresence SP = GetScenePresence(remoteClient.AgentId);
                    group.ScheduleFullUpdateToAvatar(SP, PrimUpdateFlags.FullUpdate);
                }
            }
        }

        /// <summary>
        /// Update the texture entry of the given prim.
        /// </summary>
        /// 
        /// A texture entry is an object that contains details of all the textures of the prim's face.  In this case,
        /// the texture is given in its byte serialized form.
        /// 
        /// <param name="localID"></param>
        /// <param name="texture"></param>
        /// <param name="remoteClient"></param>
        protected internal void UpdatePrimTexture(uint localID, byte[] texture, IClientAPI remoteClient)
        {
            SceneObjectGroup group = GetGroupByPrim(localID);
            
            if (group != null)
            {
                if (m_parentScene.Permissions.CanEditObject(group.UUID,remoteClient.AgentId))
                {
                    group.UpdateTextureEntry(localID, texture);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="packet"></param>
        /// <param name="remoteClient"></param>
        /// This routine seems to get called when a user changes object settings in the viewer.
        /// If some one can confirm that, please change the comment according.
        protected internal void UpdatePrimFlags(uint localID, bool UsePhysics, bool IsTemporary, bool IsPhantom, IClientAPI remoteClient)
        {
            SceneObjectGroup group = GetGroupByPrim(localID);
            if (group != null)
            {
                if (m_parentScene.Permissions.CanEditObject(group.UUID, remoteClient.AgentId))
                {
                    group.UpdatePrimFlags(localID, UsePhysics, IsTemporary, IsPhantom, false); // VolumeDetect can't be set via UI and will always be off when a change is made there
                }
            }
        }

        /// <summary>
        /// Move the given object
        /// </summary>
        /// <param name="objectID"></param>
        /// <param name="offset"></param>
        /// <param name="pos"></param>
        /// <param name="remoteClient"></param>
        protected internal void MoveObject(UUID objectID, Vector3 offset, Vector3 pos, IClientAPI remoteClient, List<SurfaceTouchEventArgs> surfaceArgs)
        {
            SceneObjectGroup group = GetGroupByPrim(objectID);
            if (group != null)
            {
                if (m_parentScene.Permissions.CanMoveObject(group.UUID, remoteClient.AgentId))// && PermissionsMngr.)
                {
                    group.GrabMovement(offset, pos, remoteClient);
                }
                // This is outside the above permissions condition
                // so that if the object is locked the client moving the object
                // get's it's position on the simulator even if it was the same as before
                // This keeps the moving user's client in sync with the rest of the world.
                group.ScheduleGroupForTerseUpdate();
            }
        }

        /// <summary>
        /// Start spinning the given object
        /// </summary>
        /// <param name="objectID"></param>
        /// <param name="rotation"></param>
        /// <param name="remoteClient"></param>
        protected internal void SpinStart(UUID objectID, IClientAPI remoteClient)
        {
            SceneObjectGroup group = GetGroupByPrim(objectID);
            if (group != null)
            {
                if (m_parentScene.Permissions.CanMoveObject(group.UUID, remoteClient.AgentId))// && PermissionsMngr.)
                {
                    group.SpinStart(remoteClient);
                }
            }
        }

        /// <summary>
        /// Spin the given object
        /// </summary>
        /// <param name="objectID"></param>
        /// <param name="rotation"></param>
        /// <param name="remoteClient"></param>
        protected internal void SpinObject(UUID objectID, Quaternion rotation, IClientAPI remoteClient)
        {
            SceneObjectGroup group = GetGroupByPrim(objectID);
            if (group != null)
            {
                if (m_parentScene.Permissions.CanMoveObject(group.UUID, remoteClient.AgentId))// && PermissionsMngr.)
                {
                    group.SpinMovement(rotation, remoteClient);
                }
                // This is outside the above permissions condition
                // so that if the object is locked the client moving the object
                // get's it's position on the simulator even if it was the same as before
                // This keeps the moving user's client in sync with the rest of the world.
                group.SendGroupTerseUpdate();
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="primLocalID"></param>
        /// <param name="description"></param>
        protected internal void PrimName(IClientAPI remoteClient, uint primLocalID, string name)
        {
            SceneObjectGroup group = GetGroupByPrim(primLocalID);
            if (group != null)
            {
                if (m_parentScene.Permissions.CanEditObject(group.UUID, remoteClient.AgentId))
                {
                    group.SetPartName(Util.CleanString(name), primLocalID);
                    group.HasGroupChanged = true;
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="primLocalID"></param>
        /// <param name="description"></param>
        protected internal void PrimDescription(IClientAPI remoteClient, uint primLocalID, string description)
        {
            SceneObjectGroup group = GetGroupByPrim(primLocalID);
            if (group != null)
            {
                if (m_parentScene.Permissions.CanEditObject(group.UUID, remoteClient.AgentId))
                {
                    group.SetPartDescription(Util.CleanString(description), primLocalID);
                    group.HasGroupChanged = true;
                }
            }
        }

        protected internal void PrimClickAction(IClientAPI remoteClient, uint primLocalID, string clickAction)
        {
            SceneObjectGroup group = GetGroupByPrim(primLocalID);
            if (group != null)
            {
                if (m_parentScene.Permissions.CanEditObject(group.UUID, remoteClient.AgentId))
                {
                    SceneObjectPart part = m_parentScene.GetSceneObjectPart(primLocalID);
                    part.ClickAction = Convert.ToByte(clickAction);
                    group.HasGroupChanged = true;
                }
            }
        }

        protected internal void PrimMaterial(IClientAPI remoteClient, uint primLocalID, string material)
        {
            SceneObjectGroup group = GetGroupByPrim(primLocalID);
            if (group != null)
            {
                if (m_parentScene.Permissions.CanEditObject(group.UUID, remoteClient.AgentId))
                {
                    SceneObjectPart part = m_parentScene.GetSceneObjectPart(primLocalID);
                    part.Material = Convert.ToByte(material);
                    group.HasGroupChanged = true;
                }
            }
        }

        protected internal void UpdateExtraParam(UUID agentID, uint primLocalID, ushort type, bool inUse, byte[] data)
        {
            SceneObjectGroup group = GetGroupByPrim(primLocalID);

            if (group != null)
            {
                if (m_parentScene.Permissions.CanEditObject(group.UUID,agentID))
                {
                    group.UpdateExtraParam(primLocalID, type, inUse, data);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="primLocalID"></param>
        /// <param name="shapeBlock"></param>
        protected internal void UpdatePrimShape(UUID agentID, uint primLocalID, UpdateShapeArgs shapeBlock)
        {
            SceneObjectGroup group = GetGroupByPrim(primLocalID);
            if (group != null)
            {
                if (m_parentScene.Permissions.CanEditObject(group.GetPartsFullID(primLocalID), agentID))
                {
                    ObjectShapePacket.ObjectDataBlock shapeData = new ObjectShapePacket.ObjectDataBlock();
                    shapeData.ObjectLocalID = shapeBlock.ObjectLocalID;
                    shapeData.PathBegin = shapeBlock.PathBegin;
                    shapeData.PathCurve = shapeBlock.PathCurve;
                    shapeData.PathEnd = shapeBlock.PathEnd;
                    shapeData.PathRadiusOffset = shapeBlock.PathRadiusOffset;
                    shapeData.PathRevolutions = shapeBlock.PathRevolutions;
                    shapeData.PathScaleX = shapeBlock.PathScaleX;
                    shapeData.PathScaleY = shapeBlock.PathScaleY;
                    shapeData.PathShearX = shapeBlock.PathShearX;
                    shapeData.PathShearY = shapeBlock.PathShearY;
                    shapeData.PathSkew = shapeBlock.PathSkew;
                    shapeData.PathTaperX = shapeBlock.PathTaperX;
                    shapeData.PathTaperY = shapeBlock.PathTaperY;
                    shapeData.PathTwist = shapeBlock.PathTwist;
                    shapeData.PathTwistBegin = shapeBlock.PathTwistBegin;
                    shapeData.ProfileBegin = shapeBlock.ProfileBegin;
                    shapeData.ProfileCurve = shapeBlock.ProfileCurve;
                    shapeData.ProfileEnd = shapeBlock.ProfileEnd;
                    shapeData.ProfileHollow = shapeBlock.ProfileHollow;

                    group.UpdateShape(shapeData, primLocalID);
                }
            }
        }

        protected internal void CheckParcelReturns()
        {
            ForEachSOG(delegate(SceneObjectGroup sog)
            {
                // Don't abort the whole thing if one entity happens to give us an exception.
                try
                {
                    sog.CheckParcelReturn();
                }
                catch (Exception e)
                {
                    m_log.ErrorFormat(
                        "[INNER SCENE]: Failed to update {0}, {1} - {2}", sog.Name, sog.UUID, e);
                }
            });
        }

        /// <summary>
        /// Initial method invoked when we receive a link objects request from the client.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="parentPrim"></param>
        /// <param name="childPrims"></param>
        protected internal void LinkObjects(SceneObjectPart root, List<SceneObjectPart> children)
        {
            Monitor.Enter(m_updateLock);
            try
            {
                SceneObjectGroup parentGroup = root.ParentGroup;

                List<SceneObjectGroup> childGroups = new List<SceneObjectGroup>();
                if (parentGroup != null)
                {
                    // We do this in reverse to get the link order of the prims correct
                    for (int i = children.Count - 1; i >= 0; i--)
                    {
                        SceneObjectGroup child = children[i].ParentGroup;

                        if (child != null)
                        {
                            // Make sure no child prim is set for sale
                            // So that, on delink, no prims are unwittingly
                            // left for sale and sold off
                            child.RootPart.ObjectSaleType = 0;
                            child.RootPart.SalePrice = 10;
                            childGroups.Add(child);
                        }
                    }
                }
                else
                {
                    return; // parent is null so not in this region
                }

                foreach (SceneObjectGroup child in childGroups)
                {
                    parentGroup.LinkToGroup(child);

                    // this is here so physics gets updated!
                    // Don't remove!  Bad juju!  Stay away! or fix physics!
                    child.AbsolutePosition = child.AbsolutePosition;
                }

                foreach (SceneObjectGroup child in childGroups)
                {
                    DeleteSceneObject(child.UUID, true);
                }

                // We need to explicitly resend the newly link prim's object properties since no other actions
                // occur on link to invoke this elsewhere (such as object selection)
                parentGroup.RootPart.CreateSelected = true;
                parentGroup.HasGroupChanged = true;
                parentGroup.RootPart.SendFullUpdateToAllClients(PrimUpdateFlags.FullUpdate);
                //parentGroup.ScheduleGroupForFullUpdate(PrimUpdateFlags.FullUpdate);
                parentGroup.SendGroupFullUpdate(PrimUpdateFlags.FullUpdate);
                parentGroup.TriggerScriptChangedEvent(Changed.LINK);
            }
            finally
            {
                Monitor.Exit(m_updateLock);
            }
        }

        /// <summary>
        /// Delink a linkset
        /// </summary>
        /// <param name="prims"></param>
        protected internal void DelinkObjects(List<SceneObjectPart> prims)
        {
            Monitor.Enter(m_updateLock);
            try
            {
                List<SceneObjectPart> childParts = new List<SceneObjectPart>();
                List<SceneObjectPart> rootParts = new List<SceneObjectPart>();
                List<SceneObjectGroup> affectedGroups = new List<SceneObjectGroup>();
                // Look them all up in one go, since that is comparatively expensive
                //
                foreach (SceneObjectPart part in prims)
                {
                    if (part != null)
                    {
                        if (part.ParentGroup.ChildrenList.Count != 1) // Skip single
                        {
                            if (part.LinkNum < 2) // Root
                                rootParts.Add(part);
                            else
                                childParts.Add(part);

                            SceneObjectGroup group = part.ParentGroup;
                            if (!affectedGroups.Contains(group))
                                affectedGroups.Add(group);
                        }
                    }
                }

                foreach (SceneObjectPart child in childParts)
                {
                    // Unlink all child parts from their groups
                    //
                    child.ParentGroup.DelinkFromGroup(child, true);

                    // These are not in affected groups and will not be
                    // handled further. Do the honors here.
                    child.ParentGroup.HasGroupChanged = true;
                    child.ParentGroup.ScheduleGroupForFullUpdate(PrimUpdateFlags.FullUpdate);
                }

                foreach (SceneObjectPart root in rootParts)
                {
                    // In most cases, this will run only one time, and the prim
                    // will be a solo prim
                    // However, editing linked parts and unlinking may be different
                    //
                    SceneObjectGroup group = root.ParentGroup;
                    List<SceneObjectPart> newSet = new List<SceneObjectPart>(group.ChildrenList);
                    int numChildren = group.ChildrenList.Count;

                    // If there are prims left in a link set, but the root is
                    // slated for unlink, we need to do this
                    //
                    if (numChildren != 1)
                    {
                        // Unlink the remaining set
                        //
                        bool sendEventsToRemainder = true;
                        if (numChildren > 1)
                            sendEventsToRemainder = false;

                        foreach (SceneObjectPart p in newSet)
                        {
                            if (p != group.RootPart)
                                group.DelinkFromGroup(p, sendEventsToRemainder);
                        }

                        // If there is more than one prim remaining, we
                        // need to re-link
                        //
                        if (numChildren > 2)
                        {
                            // Remove old root
                            //
                            if (newSet.Contains(root))
                                newSet.Remove(root);

                            // Preserve link ordering
                            //
                            newSet.Sort(linkSetSorter);

                            // Determine new root
                            //
                            SceneObjectPart newRoot = newSet[0];
                            newSet.RemoveAt(0);

                            foreach (SceneObjectPart newChild in newSet)
                                newChild.UpdateFlag = InternalUpdateFlags.NoUpdate;

                            LinkObjects(newRoot, newSet);
                            if (!affectedGroups.Contains(newRoot.ParentGroup))
                                affectedGroups.Add(newRoot.ParentGroup);
                        }
                    }
                }

                // Finally, trigger events in the roots
                //
                foreach (SceneObjectGroup g in affectedGroups)
                {
                    g.TriggerScriptChangedEvent(Changed.LINK);
                    g.HasGroupChanged = true; // Persist
                    g.ScheduleGroupForFullUpdate(PrimUpdateFlags.FullUpdate);
                    g.SendGroupFullUpdate(PrimUpdateFlags.FullUpdate);
                }
                foreach (SceneObjectPart part in prims)
                {
                    part.StoreUndoState();
                }
            }
            finally
            {
                Monitor.Exit(m_updateLock);
            }
        }

        private int linkSetSorter(SceneObjectPart a, SceneObjectPart b)
        {
             return a.LinkNum.CompareTo(b.LinkNum);
        }

        protected internal void MakeObjectSearchable(IClientAPI remoteClient, bool IncludeInSearch, uint localID)
        {
            UUID user = remoteClient.AgentId;
            UUID objid = UUID.Zero;
            SceneObjectPart obj = null;
            ISceneEntity entity = null;

            if (!Entities.TryGetChildPrim(localID, out entity))
                return;

            if (!(entity is SceneObjectPart))
                return;

            obj = entity as SceneObjectPart;

            //Protip: In my day, we didn't call them searchable objects, we called them limited point-to-point joints
            //aka ObjectFlags.JointWheel = IncludeInSearch

            //Permissions model: Object can be REMOVED from search IFF:
            // * User owns object
            //use CanEditObject

            //Object can be ADDED to search IFF:
            // * User owns object
            // * Asset/DRM permission bit "modify" is enabled
            //use CanEditObjectPosition

            // libomv will complain about PrimFlags.JointWheel being
            // deprecated, so we
            #pragma warning disable 0612
            if (IncludeInSearch && m_parentScene.Permissions.CanEditObject(objid, user))
            {
                obj.ParentGroup.RootPart.AddFlag(PrimFlags.JointWheel);
                obj.ParentGroup.HasGroupChanged = true;
            }
            else if (!IncludeInSearch && m_parentScene.Permissions.CanMoveObject(objid,user))
            {
                obj.ParentGroup.RootPart.RemFlag(PrimFlags.JointWheel);
                obj.ParentGroup.HasGroupChanged = true;
            }
            #pragma warning restore 0612
        }

        /// <summary>
        /// Duplicate the given object, Fire and Forget, No rotation, no return wrapper
        /// </summary>
        /// <param name="originalPrim"></param>
        /// <param name="offset"></param>
        /// <param name="flags"></param>
        /// <param name="AgentID"></param>
        /// <param name="GroupID"></param>
        protected internal void DuplicateObject(uint originalPrim, Vector3 offset, uint flags, UUID AgentID, UUID GroupID)
        {
            //m_log.DebugFormat("[SCENE]: Duplication of object {0} at offset {1} requested by agent {2}", originalPrim, offset, AgentID);

            // SceneObjectGroup dupe = DuplicateObject(originalPrim, offset, flags, AgentID, GroupID, Quaternion.Zero);
            DuplicateObject(originalPrim, offset, flags, AgentID, GroupID, Quaternion.Identity);
        }
        
        /// <summary>
        /// Duplicate the given object.
        /// </summary>
        /// <param name="originalPrim"></param>
        /// <param name="offset"></param>
        /// <param name="flags"></param>
        /// <param name="AgentID"></param>
        /// <param name="GroupID"></param>
        /// <param name="rot"></param>
        public SceneObjectGroup DuplicateObject(uint originalPrimID, Vector3 offset, uint flags, UUID AgentID, UUID GroupID, Quaternion rot)
        {
            //m_log.DebugFormat("[SCENE]: Duplication of object {0} at offset {1} requested by agent {2}", originalPrim, offset, AgentID);
            SceneObjectGroup original = GetGroupByPrim(originalPrimID);
            if (original != null)
            {
                if (m_parentScene.Permissions.CanDuplicateObject(original.ChildrenList.Count, original.UUID, AgentID, original.AbsolutePosition))
                {
                    SceneObjectGroup copy = original.Copy(AgentID, GroupID, true, original.Scene, false);
                    copy.AbsolutePosition = copy.AbsolutePosition + offset;

                    if (original.OwnerID != AgentID)
                    {
                        copy.SetOwnerId(AgentID);
                        copy.SetRootPartOwner(copy.RootPart, AgentID, GroupID);

                        List<SceneObjectPart> partList =
                            new List<SceneObjectPart>(copy.ChildrenList);

                        if (m_parentScene.Permissions.PropagatePermissions())
                        {
                            foreach (SceneObjectPart child in partList)
                            {
                                child.Inventory.ChangeInventoryOwner(AgentID);
                                child.TriggerScriptChangedEvent(Changed.OWNER);
                                child.ApplyNextOwnerPermissions();
                            }
                        }

                        copy.RootPart.ObjectSaleType = 0;
                        copy.RootPart.SalePrice = 10;
                    }

                    Entities.Add(copy);

                    // Since we copy from a source group that is in selected
                    // state, but the copy is shown deselected in the viewer,
                    // We need to clear the selection flag here, else that
                    // prim never gets persisted at all. The client doesn't
                    // think it's selected, so it will never send a deselect...
                    copy.IsSelected = false;

                    m_numPrim += copy.ChildrenList.Count;

                    if (rot != Quaternion.Identity)
                    {
                        copy.UpdateGroupRotationR(rot);
                    }

                    copy.CreateScriptInstances(0, false, m_parentScene.DefaultScriptEngine, 0, UUID.Zero);
                    copy.HasGroupChanged = true;
                    copy.SendGroupFullUpdate(PrimUpdateFlags.FullUpdate);
                    copy.ResumeScripts();

                    // required for physics to update it's position
                    copy.AbsolutePosition = copy.AbsolutePosition;

                    if (OnObjectDuplicate != null)
                        OnObjectDuplicate(original, copy);

                    m_parentScene.EventManager.TriggerParcelPrimCountUpdate();

                    return copy;
                }
                else
                {
                    GetScenePresence(AgentID).ControllingClient.SendAlertMessage("You do not have permission to rez objects here.");
                }
            }
            else
            {
                m_log.WarnFormat("[SCENE]: Attempted to duplicate nonexistant prim id {0}", GroupID);
            }
            
            return null;
        }
        
        /// <summary>
        /// Calculates the distance between two Vector3s
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        protected internal float Vector3Distance(Vector3 v1, Vector3 v2)
        {
            // We don't really need the double floating point precision...
            // so casting it to a single

            return
                (float)
                Math.Sqrt((v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y) + (v1.Z - v2.Z) * (v1.Z - v2.Z));
        }

        #endregion
    }
}
