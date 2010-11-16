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

#pragma warning disable 67

        public event ObjectCreateDelegate OnObjectCreate;

#pragma warning restore 67
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
                        if (TryGetEntity(ID, out e))
                            e.Update();
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

        public void DropObject(uint LocalID, IClientAPI remoteClient)
        {
            EntityBase entity;
            if (TryGetEntity(LocalID, out entity))
                m_parentScene.AttachmentsModule.DetachSingleAttachmentToGround(entity.UUID, remoteClient);
        }

        protected internal void DetachObject(uint LocalID, IClientAPI remoteClient)
        {
            EntityBase entity;
            if (TryGetEntity(LocalID, out entity))
                m_parentScene.AttachmentsModule.ShowDetachInUserInventory(((SceneObjectGroup)entity).GetFromItemID(), remoteClient);
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
            IClientAPI remoteClient, UUID GroupID, uint LocalID, UUID Garbage)
        {
            if (!remoteClient.IsGroupMember(GroupID))
                return; // No settings to groups you arn't in
            EntityBase entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (m_parentScene.Permissions.CanEditObject(((SceneObjectGroup)entity).UUID, remoteClient.AgentId))
                    if (((SceneObjectGroup)entity).OwnerID == remoteClient.AgentId)
                        ((SceneObjectGroup)entity).SetGroup(GroupID, remoteClient);
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

        protected internal Dictionary<uint, SceneObjectGroup> SceneObjectGroupsByLocalID = new Dictionary<uint, SceneObjectGroup>();
        
        /// <summary>
        /// Get a scene object group that contains the prim with the given uuid
        /// </summary>
        /// <param name="fullID"></param>
        /// <returns>null if no scene object group containing that prim is found</returns>

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
        protected internal void UpdatePrimScale(uint LocalID, Vector3 scale, IClientAPI remoteClient)
        {
            EntityBase entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (m_parentScene.Permissions.CanEditObject(((SceneObjectGroup)entity).UUID, remoteClient.AgentId))
                {
                    ((SceneObjectGroup)entity).Resize(scale, LocalID);
                }
            }
        }

        protected internal void UpdatePrimGroupScale(uint LocalID, Vector3 scale, IClientAPI remoteClient)
        {
            EntityBase entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (m_parentScene.Permissions.CanEditObject(entity.UUID, remoteClient.AgentId))
                {
                    ((SceneObjectGroup)entity).GroupResize(scale, LocalID);
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
            EntityBase group;
            if (TryGetEntity(ObjectID, out group))
            {
                ((SceneObjectGroup)group).ServiceObjectPropertiesFamilyRequest(remoteClient, AgentID, RequestFlags);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="rot"></param>
        /// <param name="remoteClient"></param>
        protected internal void UpdatePrimSingleRotation(uint LocalID, Quaternion rot, IClientAPI remoteClient)
        {
            EntityBase entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (m_parentScene.Permissions.CanMoveObject(((SceneObjectGroup)entity).UUID, remoteClient.AgentId))
                {
                    ((SceneObjectGroup)entity).UpdateSingleRotation(rot, LocalID);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="rot"></param>
        /// <param name="remoteClient"></param>
        protected internal void UpdatePrimSingleRotationPosition(uint LocalID, Quaternion rot, Vector3 pos, IClientAPI remoteClient)
        {
            EntityBase entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (m_parentScene.Permissions.CanMoveObject(((SceneObjectGroup)entity).UUID, remoteClient.AgentId))
                {
                    ((SceneObjectGroup)entity).UpdateSingleRotation(rot, pos, LocalID);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="rot"></param>
        /// <param name="remoteClient"></param>
        protected internal void UpdatePrimRotation(uint LocalID, Quaternion rot, IClientAPI remoteClient)
        {
            EntityBase entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (m_parentScene.Permissions.CanMoveObject(((SceneObjectGroup)entity).UUID, remoteClient.AgentId))
                {
                    ((SceneObjectGroup)entity).UpdateGroupRotationR(rot);
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
        protected internal void UpdatePrimRotation(uint LocalID, Vector3 pos, Quaternion rot, IClientAPI remoteClient)
        {
            EntityBase entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (m_parentScene.Permissions.CanMoveObject(((SceneObjectGroup)entity).UUID, remoteClient.AgentId))
                {
                    ((SceneObjectGroup)entity).UpdateGroupRotationPR(pos, rot);
                }
            }
        }

        /// <summary>
        /// Update the position of the given part
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="pos"></param>
        /// <param name="remoteClient"></param>
        protected internal void UpdatePrimSinglePosition(uint LocalID, Vector3 pos, IClientAPI remoteClient, bool SaveUpdate)
        {
            EntityBase entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (m_parentScene.Permissions.CanMoveObject(((SceneObjectGroup)entity).UUID, remoteClient.AgentId) || ((SceneObjectGroup)entity).IsAttachment)
                {
                    ((SceneObjectGroup)entity).UpdateSinglePosition(pos, LocalID, SaveUpdate);
                }
            }
        }

        /// <summary>
        /// Update the position of the given part
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="pos"></param>
        /// <param name="remoteClient"></param>
        protected internal void UpdatePrimPosition(uint LocalID, Vector3 pos, IClientAPI remoteClient, bool SaveUpdate)
        {
            EntityBase entity;
            if (TryGetEntity(LocalID, out entity))
            {
                //Move has edit permission as well
                if (m_parentScene.Permissions.CanMoveObject(((SceneObjectGroup)entity).UUID, remoteClient.AgentId))
                {
                    if (((SceneObjectGroup)entity).IsAttachment || (((SceneObjectGroup)entity).RootPart.Shape.PCode == 9 && ((SceneObjectGroup)entity).RootPart.Shape.State != 0))
                    {
                        if (m_parentScene.AttachmentsModule != null)
                            m_parentScene.AttachmentsModule.UpdateAttachmentPosition(remoteClient, ((SceneObjectGroup)entity), pos);
                    }
                    else
                    {
                        ((SceneObjectGroup)entity).UpdateGroupPosition(pos, SaveUpdate);
                    }
                }
                else
                {
                    ScenePresence SP = GetScenePresence(remoteClient.AgentId);
                    ((SceneObjectGroup)entity).ScheduleFullUpdateToAvatar(SP, PrimUpdateFlags.FullUpdate);
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
        protected internal void UpdatePrimTexture(uint LocalID, byte[] texture, IClientAPI remoteClient)
        {
            EntityBase entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (m_parentScene.Permissions.CanEditObject(((SceneObjectGroup)entity).UUID, remoteClient.AgentId))
                {
                    ((SceneObjectGroup)entity).UpdateTextureEntry(LocalID, texture);
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
        protected internal void UpdatePrimFlags(uint LocalID, bool UsePhysics, bool IsTemporary, bool IsPhantom, IClientAPI remoteClient)
        {
            EntityBase entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (m_parentScene.Permissions.CanEditObject(((SceneObjectGroup)entity).UUID, remoteClient.AgentId))
                {
                    ((SceneObjectGroup)entity).UpdatePrimFlags(LocalID, UsePhysics, IsTemporary, IsPhantom, false); // VolumeDetect can't be set via UI and will always be off when a change is made there
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
        protected internal void MoveObject(UUID ObjectID, Vector3 offset, Vector3 pos, IClientAPI remoteClient, List<SurfaceTouchEventArgs> surfaceArgs)
        {
            EntityBase group;
            if (TryGetEntity(ObjectID, out group))
            {
                if (m_parentScene.Permissions.CanMoveObject(group.UUID, remoteClient.AgentId))// && PermissionsMngr.)
                {
                    ((SceneObjectGroup)group).GrabMovement(offset, pos, remoteClient);
                }
                // This is outside the above permissions condition
                // so that if the object is locked the client moving the object
                // get's it's position on the simulator even if it was the same as before
                // This keeps the moving user's client in sync with the rest of the world.
                ((SceneObjectGroup)group).ScheduleGroupForTerseUpdate();
            }
        }

        /// <summary>
        /// Start spinning the given object
        /// </summary>
        /// <param name="objectID"></param>
        /// <param name="rotation"></param>
        /// <param name="remoteClient"></param>
        protected internal void SpinStart(UUID ObjectID, IClientAPI remoteClient)
        {
            EntityBase group;
            if (TryGetEntity(ObjectID, out group))
            {
                if (m_parentScene.Permissions.CanMoveObject(group.UUID, remoteClient.AgentId))// && PermissionsMngr.)
                {
                    ((SceneObjectGroup)group).SpinStart(remoteClient);
                }
            }
        }

        /// <summary>
        /// Spin the given object
        /// </summary>
        /// <param name="objectID"></param>
        /// <param name="rotation"></param>
        /// <param name="remoteClient"></param>
        protected internal void SpinObject(UUID ObjectID, Quaternion rotation, IClientAPI remoteClient)
        {
            EntityBase group;
            if (TryGetEntity(ObjectID, out group))
            {
                if (m_parentScene.Permissions.CanMoveObject(group.UUID, remoteClient.AgentId))// && PermissionsMngr.)
                {
                    ((SceneObjectGroup)group).SpinMovement(rotation, remoteClient);
                }
                // This is outside the above permissions condition
                // so that if the object is locked the client moving the object
                // get's it's position on the simulator even if it was the same as before
                // This keeps the moving user's client in sync with the rest of the world.
                ((SceneObjectGroup)group).SendGroupTerseUpdate();
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="primLocalID"></param>
        /// <param name="description"></param>
        protected internal void PrimName(IClientAPI remoteClient, uint LocalID, string name)
        {
            EntityBase group;
            if (TryGetEntity(LocalID, out group))
            {
                if (m_parentScene.Permissions.CanEditObject(group.UUID, remoteClient.AgentId))
                {
                    ((SceneObjectGroup)group).SetPartName(Util.CleanString(name), LocalID);
                    ((SceneObjectGroup)group).HasGroupChanged = true;
                    ((SceneObjectGroup)group).ScheduleGroupForFullUpdate(PrimUpdateFlags.ClickAction);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="primLocalID"></param>
        /// <param name="description"></param>
        protected internal void PrimDescription(IClientAPI remoteClient, uint LocalID, string description)
        {
            EntityBase group;
            if (TryGetEntity(LocalID, out group))
            {
                if (m_parentScene.Permissions.CanEditObject(group.UUID, remoteClient.AgentId))
                {
                    ((SceneObjectGroup)group).SetPartDescription(Util.CleanString(description), LocalID);
                    ((SceneObjectGroup)group).HasGroupChanged = true;
                    ((SceneObjectGroup)group).ScheduleGroupForFullUpdate(PrimUpdateFlags.ClickAction);
                }
            }
        }

        protected internal void PrimClickAction(IClientAPI remoteClient, uint LocalID, string clickAction)
        {
            EntityBase group;
            if (TryGetEntity(LocalID, out group))
            {
                if (m_parentScene.Permissions.CanEditObject(group.UUID, remoteClient.AgentId))
                {
                    SceneObjectPart part = m_parentScene.GetSceneObjectPart(LocalID);
                    part.ClickAction = Convert.ToByte(clickAction);
                    ((SceneObjectGroup)group).HasGroupChanged = true;
                    ((SceneObjectGroup)group).ScheduleGroupForFullUpdate(PrimUpdateFlags.ClickAction);
                }
            }
        }

        protected internal void PrimMaterial(IClientAPI remoteClient, uint LocalID, string material)
        {
            EntityBase group;
            if (TryGetEntity(LocalID, out group))
            {
                if (m_parentScene.Permissions.CanEditObject(group.UUID, remoteClient.AgentId))
                {
                    SceneObjectPart part = m_parentScene.GetSceneObjectPart(LocalID);
                    part.Material = Convert.ToByte(material);
                    ((SceneObjectGroup)group).HasGroupChanged = true;
                    ((SceneObjectGroup)group).ScheduleGroupForFullUpdate(PrimUpdateFlags.ClickAction);
                }
            }
        }

        protected internal void UpdateExtraParam(UUID agentID, uint LocalID, ushort type, bool inUse, byte[] data)
        {
            ISceneEntity part;
            if (TryGetPart(LocalID, out part))
            {
                if (m_parentScene.Permissions.CanEditObject(part.UUID, agentID))
                {
                    ((SceneObjectPart)part).UpdateExtraParam(type, inUse, data);
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="primLocalID"></param>
        /// <param name="shapeBlock"></param>
        protected internal void UpdatePrimShape(UUID agentID, uint LocalID, UpdateShapeArgs shapeBlock)
        {
            ISceneEntity part;
            if (TryGetPart(LocalID, out part))
            {
                if (m_parentScene.Permissions.CanEditObject(part.UUID, agentID))
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

                    ((SceneObjectPart)part).UpdateShape(shapeData);
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
                    parentGroup.LinkToGroup(child, true);

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

        protected internal void MakeObjectSearchable(IClientAPI remoteClient, bool IncludeInSearch, uint LocalID)
        {
            UUID user = remoteClient.AgentId;
            UUID objid = UUID.Zero;
            EntityBase entity;
            SceneObjectGroup grp;
            if (!TryGetEntity(LocalID, out entity))
                return;
            grp = (SceneObjectGroup)entity;
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
                grp.RootPart.AddFlag(PrimFlags.JointWheel);
                grp.HasGroupChanged = true;
            }
            else if (!IncludeInSearch && m_parentScene.Permissions.CanMoveObject(objid,user))
            {
                grp.RootPart.RemFlag(PrimFlags.JointWheel);
                grp.HasGroupChanged = true;
            }
            #pragma warning restore 0612
        }

        public bool DuplicateObject(uint LocalID, Vector3 offset, uint flags, UUID AgentID, UUID GroupID, Quaternion rot)
        {
            //m_log.DebugFormat("[SCENE]: Duplication of object {0} at offset {1} requested by agent {2}", originalPrim, offset, AgentID);
            SceneObjectGroup original;
            EntityBase entity;

            if (TryGetEntity(LocalID, out entity))
            {
                original = (SceneObjectGroup)entity;
                if (m_parentScene.Permissions.CanDuplicateObject(original.ChildrenList.Count, original.UUID, AgentID, original.AbsolutePosition))
                {
                    EntityBase duplicatedEntity = DuplicateEntity(original);
                    
                    duplicatedEntity.AbsolutePosition = duplicatedEntity.AbsolutePosition + offset;
                    
                    SceneObjectGroup duplicatedGroup = (SceneObjectGroup)duplicatedEntity;

                    if (original.OwnerID != AgentID)
                    {
                        duplicatedGroup.SetOwnerId(AgentID);
                        duplicatedGroup.SetRootPartOwner(duplicatedGroup.RootPart, AgentID, GroupID);

                        List<SceneObjectPart> partList =
                            new List<SceneObjectPart>(duplicatedGroup.ChildrenList);

                        if (m_parentScene.Permissions.PropagatePermissions())
                        {
                            foreach (SceneObjectPart child in partList)
                            {
                                child.Inventory.ChangeInventoryOwner(AgentID);
                                child.TriggerScriptChangedEvent(Changed.OWNER);
                                child.ApplyNextOwnerPermissions();
                            }
                        }

                        duplicatedGroup.RootPart.ObjectSaleType = 0;
                        duplicatedGroup.RootPart.SalePrice = 10;
                    }

                    // Since we copy from a source group that is in selected
                    // state, but the copy is shown deselected in the viewer,
                    // We need to clear the selection flag here, else that
                    // prim never gets persisted at all. The client doesn't
                    // think it's selected, so it will never send a deselect...
                    duplicatedGroup.IsSelected = false;

                    if (rot != Quaternion.Identity)
                    {
                        duplicatedGroup.UpdateGroupRotationR(rot);
                    }

                    duplicatedGroup.CreateScriptInstances(0, false, m_parentScene.DefaultScriptEngine, 0, UUID.Zero);
                    duplicatedGroup.HasGroupChanged = true;
                    duplicatedGroup.SendGroupFullUpdate(PrimUpdateFlags.FullUpdate);
                    duplicatedGroup.ResumeScripts();

                    // required for physics to update it's position
                    duplicatedGroup.AbsolutePosition = duplicatedGroup.AbsolutePosition;

                    m_parentScene.EventManager.TriggerParcelPrimCountUpdate();
                    return true;
                }
            }
            return false;
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


        #region New Scene Entity Manager Code

        #region Wrapper Methods

        public EntityBase DuplicateEntity(EntityBase entity)
        {
            //Make an exact copy of the entity
            EntityBase copiedEntity = entity.Copy();
            //Add the entity to the scene and back it up
            AddPrimToScene(copiedEntity);
            return copiedEntity;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Try to get an EntityBase as given by its UUID
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool TryGetEntity(UUID ID, out EntityBase entity)
        {
            return Entities.TryGetValue(ID, out entity);
        }
        
        /// <summary>
        /// Try to get an EntityBase as given by it's LocalID
        /// </summary>
        /// <param name="LocalID"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool TryGetEntity(uint LocalID, out EntityBase entity)
        {
            return Entities.TryGetValue(LocalID, out entity);
        }

        public bool TryGetPart(uint LocalID, out ISceneEntity entity)
        {
            EntityBase parent;
            if (Entities.TryGetValue(LocalID, out parent))
            {
                return parent.GetChildPrim(LocalID, out entity);
            }

            entity = null;
            return false;
        }

        public bool TryGetPart(UUID ID, out ISceneEntity entity)
        {
            EntityBase parent;
            if (Entities.TryGetValue(ID, out parent))
            {
                return parent.GetChildPrim(ID, out entity);
            }

            entity = null;
            return false;
        }

        /// <summary>
        /// Add the Entity to the Scene and back it up
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool AddPrimToScene(EntityBase entity)
        {
            //Reset the entity IDs
            ResetEntityIDs(entity);
            //Force the prim to backup now that it has been added
            entity.ForcePersistence();
            //Tell the entity that they are being added to a scene
            entity.AttachToScene(m_parentScene);
            //Update our prim count
            m_numPrim += entity.ChildrenEntities().Count;
            //Now save the entity that we have 
            return AddEntity(entity, false);
        }

        /// <summary>
        /// Destroy the entity and remove it from the scene
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool DeleteEntity(EntityBase entity)
        {
            m_numPrim -= entity.ChildrenEntities().Count;
            if (entity.IsPhysical())
                RemovePhysicalPrim(entity.ChildrenEntities().Count);
            return RemoveEntity(entity);
        }

        #endregion

        #region Private Methods

        private bool RemoveEntity(EntityBase entity)
        {
            Entities.Remove(entity.UUID);
            return Entities.Remove(entity.LocalId);
        }

        private bool AddEntity(EntityBase entity, bool AllowUpdate)
        {
            Entities.Add(entity);
            return true;
        }

        private void ResetEntityIDs(EntityBase entity)
        {
            //Keep this so we don't end up with two root parts at the end
            UUID oldrootID = entity.UUID;

            //Reset root first
            List<ISceneEntity> children = entity.ChildrenEntities();
            entity.ClearChildren();

            //Add the root part first so that it is recognized as it
            entity.ResetEntityIDs();
            entity.AddChild(entity);

            foreach (ISceneEntity child in children)
            {
                if (oldrootID != child.UUID) //Do not reset roots
                {
                    child.ResetEntityIDs();
                    entity.AddChild(child);
                }
            }
        }

        #endregion

        #endregion
    }
}
