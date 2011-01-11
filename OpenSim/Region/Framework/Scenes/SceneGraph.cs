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
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Region.Physics.Manager;
using OpenSim.Region.Framework.Interfaces;

namespace OpenSim.Region.Framework.Scenes
{
    /// <summary>
    /// This class used to be called InnerScene and may not yet truly be a SceneGraph.  The non scene graph components
    /// should be migrated out over time.
    /// </summary>
    public class SceneGraph
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected object m_presenceLock = new object();
        protected Dictionary<UUID, ScenePresence> m_scenePresenceMap = new Dictionary<UUID, ScenePresence>();
        protected List<ScenePresence> m_scenePresenceArray = new List<ScenePresence>();

        protected internal EntityManager Entities = new EntityManager();

        protected RegionInfo m_regInfo;
        protected Scene m_parentScene;
        protected int m_numRootAgents = 0;
        protected int m_numPrim = 0;
        protected int m_numChildAgents = 0;
        protected int m_physicalPrim = 0;
        protected bool EnableFakeRaycasting = false;

        /// <summary>
        /// The last allocated local prim id.  When a new local id is requested, the next number in the sequence is
        /// dispensed.
        /// </summary>
        protected uint m_lastAllocatedLocalId = 720000;

        private readonly Mutex _primAllocateMutex = new Mutex(false);

        protected internal object m_syncRoot = new object();

        protected internal PhysicsScene _PhyScene;

        private Object m_updateLock = new Object();

        public PhysicsScene PhysicsScene
        {
            get { return _PhyScene; }
            set
            {
                _PhyScene = value;
            }
        }

        #endregion

        #region Constructor and close

        protected internal SceneGraph(Scene parent, RegionInfo regInfo)
        {
            Random random = new Random();
            m_lastAllocatedLocalId = (uint)(random.NextDouble() * (double)(uint.MaxValue / 2)) + (uint)(uint.MaxValue / 4);
            m_parentScene = parent;
            m_regInfo = regInfo;

            IConfig aurorastartupConfig = parent.Config.Configs["AuroraStartup"];
            if (aurorastartupConfig != null)
            {
                EnableFakeRaycasting = aurorastartupConfig.GetBoolean("EnableFakeRaycasting", false);
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

        #endregion

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
            ForEachSOG(delegate(SceneObjectGroup grp)
            {
                grp.Update();
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

        protected internal void AddPhysicalPrim(int number)
        {
            m_physicalPrim++;
        }

        protected internal void RemovePhysicalPrim(int number)
        {
            m_physicalPrim--;
        }

        public void DropObject(uint LocalID, IClientAPI remoteClient)
        {
            EntityBase entity;
            IAttachmentsModule attachModule = m_parentScene.RequestModuleInterface<IAttachmentsModule>();
            if (TryGetEntity(LocalID, out entity) && attachModule != null)
                attachModule.DetachSingleAttachmentToGround(entity.UUID, remoteClient);
        }

        protected internal void DetachObject(uint LocalID, IClientAPI remoteClient)
        {
            EntityBase entity;
            IAttachmentsModule attachModule = m_parentScene.RequestModuleInterface<IAttachmentsModule>();
            if (TryGetEntity(LocalID, out entity) && attachModule != null)
                attachModule.ShowDetachInUserInventory(((SceneObjectGroup)entity).GetFromItemID(), remoteClient);
        }

        protected internal void HandleUndo(IClientAPI remoteClient, UUID primId)
        {
            if (primId != UUID.Zero)
            {
                SceneObjectPart part = m_parentScene.GetSceneObjectPart(primId);
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
            IGroupsModule module = m_parentScene.RequestModuleInterface<IGroupsModule>();
            if (module != null)
            {
                if (!module.GroupPermissionCheck(remoteClient.AgentId, GroupID, GroupPowers.None))
                    return; // No settings to groups you arn't in
                EntityBase entity;
                if (TryGetEntity(LocalID, out entity))
                {
                    if (m_parentScene.Permissions.CanEditObject(((SceneObjectGroup)entity).UUID, remoteClient.AgentId))
                        if (((SceneObjectGroup)entity).OwnerID == remoteClient.AgentId)
                            ((SceneObjectGroup)entity).SetGroup(GroupID, remoteClient);
                }
            }
        }

        protected internal ScenePresence CreateAndAddChildScenePresence(IClientAPI client)
        {
            ScenePresence newAvatar = null;

            newAvatar = new ScenePresence(client, m_parentScene, m_regInfo);
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
                return;
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

        /// <summary>
        /// Update user counts for this agent
        /// </summary>
        /// <param name="direction_RC_CR_T_F">If true, add a child agent to the count, remove a child agent.
        /// If false, add a root agent, subtract a child agent</param>
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

        /// <summary>
        /// Remove a user from the count
        /// </summary>
        /// <param name="TypeRCTF">If true, remove a root agent. If false, remove a child agent.</param>
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

        /// <summary>
        /// Force the rebuilding of the child and root agent stats
        /// </summary>
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

        /// <summary>
        /// Get the number of child agents in this region
        /// </summary>
        /// <returns></returns>
        public int GetChildAgentCount()
        {
            // some network situations come in where child agents get closed twice.
            if (m_numChildAgents < 0)
            {
                m_numChildAgents = 0;
            }

            return m_numChildAgents;
        }

        /// <summary>
        /// Get the number of root agents in this sim
        /// </summary>
        /// <returns></returns>
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

        #endregion

        #region Get Methods

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
        public ScenePresence GetScenePresence(string firstName, string lastName)
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
        public ScenePresence GetScenePresence(uint localID)
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
        /// Do NOT use this method! This is only kept around so that NINJA physics is not broken
        /// </summary>
        /// <param name="name"></param>
        /// <returns>null if the part was not found</returns>
        public SceneObjectPart GetSceneObjectPart(string name)
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

        #endregion

        #region ForEach* Methods

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
                    if (obj is SceneObjectGroup)
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

        #endregion ForEach* Methods

        #region Client Event handlers

        /// <summary>
        /// Gets a new rez location based on the raycast and the size of the object that is being rezzed.
        /// </summary>
        /// <param name="RayStart"></param>
        /// <param name="RayEnd"></param>
        /// <param name="RayTargetID"></param>
        /// <param name="rot"></param>
        /// <param name="bypassRayCast"></param>
        /// <param name="RayEndIsIntersection"></param>
        /// <param name="frontFacesOnly"></param>
        /// <param name="scale"></param>
        /// <param name="FaceCenter"></param>
        /// <returns></returns>
        public Vector3 GetNewRezLocation(Vector3 RayStart, Vector3 RayEnd, UUID RayTargetID, Quaternion rot, byte bypassRayCast, byte RayEndIsIntersection, bool frontFacesOnly, Vector3 scale, bool FaceCenter)
        {
            Vector3 pos = Vector3.Zero;
            if (RayEndIsIntersection == (byte)1)
            {
                pos = RayEnd;
                return pos;
            }

            if (RayTargetID != UUID.Zero)
            {
                SceneObjectPart target = m_parentScene.GetSceneObjectPart(RayTargetID);

                Vector3 direction = Vector3.Normalize(RayEnd - RayStart);
                Vector3 AXOrigin = new Vector3(RayStart.X, RayStart.Y, RayStart.Z);
                Vector3 AXdirection = new Vector3(direction.X, direction.Y, direction.Z);

                if (target != null)
                {
                    pos = target.AbsolutePosition;
                    //m_log.Info("[OBJECT_REZ]: TargetPos: " + pos.ToString() + ", RayStart: " + RayStart.ToString() + ", RayEnd: " + RayEnd.ToString() + ", Volume: " + Util.GetDistanceTo(RayStart,RayEnd).ToString() + ", mag1: " + Util.GetMagnitude(RayStart).ToString() + ", mag2: " + Util.GetMagnitude(RayEnd).ToString());

                    // TODO: Raytrace better here

                    //EntityIntersection ei = m_sceneGraph.GetClosestIntersectingPrim(new Ray(AXOrigin, AXdirection));
                    Ray NewRay = new Ray(AXOrigin, AXdirection);

                    // Ray Trace against target here
                    EntityIntersection ei = target.TestIntersectionOBB(NewRay, Quaternion.Identity, frontFacesOnly, FaceCenter);

                    // Un-comment out the following line to Get Raytrace results printed to the console.
                    //m_log.Info("[RAYTRACERESULTS]: Hit:" + ei.HitTF.ToString() + " Point: " + ei.ipoint.ToString() + " Normal: " + ei.normal.ToString());
                    float ScaleOffset = 0.5f;

                    // If we hit something
                    if (ei.HitTF)
                    {
                        Vector3 scaleComponent = new Vector3(ei.AAfaceNormal.X, ei.AAfaceNormal.Y, ei.AAfaceNormal.Z);
                        if (scaleComponent.X != 0) ScaleOffset = scale.X;
                        if (scaleComponent.Y != 0) ScaleOffset = scale.Y;
                        if (scaleComponent.Z != 0) ScaleOffset = scale.Z;
                        ScaleOffset = Math.Abs(ScaleOffset);
                        Vector3 intersectionpoint = new Vector3(ei.ipoint.X, ei.ipoint.Y, ei.ipoint.Z);
                        Vector3 normal = new Vector3(ei.normal.X, ei.normal.Y, ei.normal.Z);
                        // Set the position to the intersection point
                        Vector3 offset = (normal * (ScaleOffset / 2f));
                        pos = (intersectionpoint + offset);

                        //Seems to make no sense to do this as this call is used for rezzing from inventory as well, and with inventory items their size is not always 0.5f
                        //And in cases when we weren't rezzing from inventory we were re-adding the 0.25 straight after calling this method
                        // Un-offset the prim (it gets offset later by the consumer method)
                        //pos.Z -= 0.25F; 

                    }

                    return pos;
                }
                else
                {
                    // We don't have a target here, so we're going to raytrace all the objects in the scene.

                    EntityIntersection ei = GetClosestIntersectingPrim(new Ray(AXOrigin, AXdirection), true, false);

                    // Un-comment the following line to print the raytrace results to the console.
                    //m_log.Info("[RAYTRACERESULTS]: Hit:" + ei.HitTF.ToString() + " Point: " + ei.ipoint.ToString() + " Normal: " + ei.normal.ToString());

                    if (ei.HitTF)
                    {
                        pos = new Vector3(ei.ipoint.X, ei.ipoint.Y, ei.ipoint.Z);
                    }
                    else
                    {
                        // fall back to our stupid functionality
                        pos = RayEnd;
                    }

                    return pos;
                }
            }
            else
            {
                // fall back to our stupid functionality
                pos = RayEnd;

                //increase height so its above the ground.
                //should be getting the normal of the ground at the rez point and using that?
                pos.Z += scale.Z / 2f;
                return pos;
            }
        }

        public virtual void AddNewPrim(UUID ownerID, UUID groupID, Vector3 RayEnd, Quaternion rot, PrimitiveBaseShape shape,
                                       byte bypassRaycast, Vector3 RayStart, UUID RayTargetID,
                                       byte RayEndIsIntersection)
        {
            Vector3 pos = GetNewRezLocation(RayStart, RayEnd, RayTargetID, rot, bypassRaycast, RayEndIsIntersection, true, new Vector3(0.5f, 0.5f, 0.5f), false);

            string reason;
            if (m_parentScene.Permissions.CanRezObject(1, ownerID, pos, out reason))
            {
               AddNewPrim(ownerID, groupID, pos, rot, shape);
            }
            else
            {
                GetScenePresence(ownerID).ControllingClient.SendAlertMessage("You do not have permission to rez objects here: " + reason);
            }
        }

        /// <summary>
        /// Create a New SceneObjectGroup/Part by raycasting
        /// </summary>
        /// <param name="ownerID"></param>
        /// <param name="groupID"></param>
        /// <param name="RayEnd"></param>
        /// <param name="rot"></param>
        /// <param name="shape"></param>
        /// <param name="bypassRaycast"></param>
        /// <param name="RayStart"></param>
        /// <param name="RayTargetID"></param>
        /// <param name="RayEndIsIntersection"></param>
        public virtual SceneObjectGroup AddNewPrim(
            UUID ownerID, UUID groupID, Vector3 pos, Quaternion rot, PrimitiveBaseShape shape)
        {
            //m_log.DebugFormat(
            //    "[SCENE]: Scene.AddNewPrim() pcode {0} called for {1} in {2}", shape.PCode, ownerID, RegionInfo.RegionName);

            SceneObjectGroup sceneObject = null;

            // If an entity creator has been registered for this prim type then use that
            if (m_entityCreators.ContainsKey((PCode)shape.PCode))
            {
                sceneObject = (SceneObjectGroup)m_entityCreators[(PCode)shape.PCode].CreateEntity(ownerID, groupID, pos, rot, shape);
            }
            else
            {
                // Otherwise, use this default creation code;
                sceneObject = new SceneObjectGroup(ownerID, pos, rot, shape, m_parentScene);
                //This has to be set, otherwise it will break things like rezzing objects in an area where crossing is disabled, but rez isn't
                sceneObject.m_lastSignificantPosition = pos;

                AddPrimToScene(sceneObject);
                sceneObject.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);
                sceneObject.SetGroup(groupID, null);
            }


            return sceneObject;
        }

        /// <summary>
        /// Duplicates object specified by localID at position raycasted against RayTargetObject using 
        /// RayEnd and RayStart to determine what the angle of the ray is
        /// </summary>
        /// <param name="localID">ID of object to duplicate</param>
        /// <param name="dupeFlags"></param>
        /// <param name="AgentID">Agent doing the duplication</param>
        /// <param name="GroupID">Group of new object</param>
        /// <param name="RayTargetObj">The target of the Ray</param>
        /// <param name="RayEnd">The ending of the ray (farthest away point)</param>
        /// <param name="RayStart">The Beginning of the ray (closest point)</param>
        /// <param name="BypassRaycast">Bool to bypass raycasting</param>
        /// <param name="RayEndIsIntersection">The End specified is the place to add the object</param>
        /// <param name="CopyCenters">Position the object at the center of the face that it's colliding with</param>
        /// <param name="CopyRotates">Rotate the object the same as the localID object</param>
        public void doObjectDuplicateOnRay(uint localID, uint dupeFlags, UUID AgentID, UUID GroupID,
                                           UUID RayTargetObj, Vector3 RayEnd, Vector3 RayStart,
                                           bool BypassRaycast, bool RayEndIsIntersection, bool CopyCenters, bool CopyRotates)
        {
            Vector3 pos;
            const bool frontFacesOnly = true;
            //m_log.Info("HITTARGET: " + RayTargetObj.ToString() + ", COPYTARGET: " + localID.ToString());
            SceneObjectPart target = m_parentScene.GetSceneObjectPart(localID);
            SceneObjectPart target2 = m_parentScene.GetSceneObjectPart(RayTargetObj);
            ScenePresence Sp = GetScenePresence(AgentID);
            if (target != null && target2 != null)
            {
                if (EnableFakeRaycasting)
                {
                    RayStart = Sp.CameraPosition;
                    RayEnd = pos = target2.AbsolutePosition;
                }
                Vector3 direction = Vector3.Normalize(RayEnd - RayStart);
                Vector3 AXOrigin = new Vector3(RayStart.X, RayStart.Y, RayStart.Z);
                Vector3 AXdirection = new Vector3(direction.X, direction.Y, direction.Z);

                if (target2.ParentGroup != null)
                {
                    pos = target2.AbsolutePosition;
                    //m_log.Info("[OBJECTREZ]: TargetPos: " + pos.ToString() + ", RayStart: " + RayStart.ToString() + ", RayEnd: " + RayEnd.ToString() + ", Volume: " + Util.GetDistanceTo(RayStart,RayEnd).ToString() + ", mag1: " + Util.GetMagnitude(RayStart).ToString() + ", mag2: " + Util.GetMagnitude(RayEnd).ToString());
                    //m_log.Info("[OBJECTREZ]: AXOrigin: " + AXOrigin.ToString() + "AXdirection: " + AXdirection.ToString());
                    // TODO: Raytrace better here

                    //EntityIntersection ei = m_sceneGraph.GetClosestIntersectingPrim(new Ray(AXOrigin, AXdirection), false, false);
                    Ray NewRay = new Ray(AXOrigin, AXdirection);

                    // Ray Trace against target here
                    EntityIntersection ei = target2.TestIntersectionOBB(NewRay, Quaternion.Identity, frontFacesOnly, CopyCenters);

                    // Un-comment out the following line to Get Raytrace results printed to the console.
                    //m_log.Info("[RAYTRACERESULTS]: Hit:" + ei.HitTF.ToString() + " Point: " + ei.ipoint.ToString() + " Normal: " + ei.normal.ToString());
                    float ScaleOffset = 0.5f;

                    // If we hit something
                    if (ei.HitTF)
                    {
                        Vector3 scale = target.Scale;
                        Vector3 scaleComponent = new Vector3(ei.AAfaceNormal.X, ei.AAfaceNormal.Y, ei.AAfaceNormal.Z);
                        if (scaleComponent.X != 0) ScaleOffset = scale.X;
                        if (scaleComponent.Y != 0) ScaleOffset = scale.Y;
                        if (scaleComponent.Z != 0) ScaleOffset = scale.Z;
                        ScaleOffset = Math.Abs(ScaleOffset);
                        Vector3 intersectionpoint = new Vector3(ei.ipoint.X, ei.ipoint.Y, ei.ipoint.Z);
                        Vector3 normal = new Vector3(ei.normal.X, ei.normal.Y, ei.normal.Z);
                        Vector3 offset = normal * (ScaleOffset / 2f);
                        pos = intersectionpoint + offset;

                        // stick in offset format from the original prim
                        pos = pos - target.ParentGroup.AbsolutePosition;
                        if (CopyRotates)
                        {
                            Quaternion worldRot = target2.GetWorldRotation();

                            // SceneObjectGroup obj = m_sceneGraph.DuplicateObject(localID, pos, target.GetEffectiveObjectFlags(), AgentID, GroupID, worldRot);
                            DuplicateObject(localID, pos, target.GetEffectiveObjectFlags(), AgentID, GroupID, worldRot);
                            //obj.Rotation = worldRot;
                            //obj.UpdateGroupRotationR(worldRot);
                        }
                        else
                        {
                            DuplicateObject(localID, pos, target.GetEffectiveObjectFlags(), AgentID, GroupID, Quaternion.Identity);
                        }
                    }

                    return;
                }

                return;
            }
        }

        /// <value>
        /// Registered classes that are capable of creating entities.
        /// </value>
        protected Dictionary<PCode, IEntityCreator> m_entityCreators = new Dictionary<PCode, IEntityCreator>();

        public void RegisterEntityCreatorModule(IEntityCreator entityCreator)
        {
            lock (m_entityCreators)
            {
                foreach (PCode pcode in entityCreator.CreationCapabilities)
                {
                    m_entityCreators[pcode] = entityCreator;
                }
            }
        }

        /// <summary>
        /// Unregister a module commander and all its commands
        /// </summary>
        /// <param name="name"></param>
        public void UnregisterEntityCreatorCommander(IEntityCreator entityCreator)
        {
            lock (m_entityCreators)
            {
                foreach (PCode pcode in entityCreator.CreationCapabilities)
                {
                    m_entityCreators[pcode] = null;
                }
            }
        }

        protected internal void ObjectOwner(IClientAPI remoteClient, UUID ownerID, UUID groupID, List<uint> localIDs)
        {
            if (!m_parentScene.Permissions.IsGod(remoteClient.AgentId))
            {
                if (ownerID != UUID.Zero)
                    return;

                if (!m_parentScene.Permissions.CanDeedObject(remoteClient.AgentId, groupID))
                    return;
            }

            List<SceneObjectGroup> groups = new List<SceneObjectGroup>();

            foreach (uint localID in localIDs)
            {
                SceneObjectPart part = m_parentScene.GetSceneObjectPart(localID);
                if (!groups.Contains(part.ParentGroup))
                    groups.Add(part.ParentGroup);
            }

            foreach (SceneObjectGroup sog in groups)
            {
                if (ownerID != UUID.Zero)
                {
                    sog.SetOwnerId(ownerID);
                    sog.SetGroup(groupID, remoteClient);
                    sog.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);

                    foreach (SceneObjectPart child in sog.ChildrenList)
                        child.Inventory.ChangeInventoryOwner(ownerID);
                }
                else
                {
                    if (!m_parentScene.Permissions.CanEditObject(sog.UUID, remoteClient.AgentId))
                        continue;

                    if (sog.GroupID != groupID)
                        continue;

                    foreach (SceneObjectPart child in sog.ChildrenList)
                    {
                        child.LastOwnerID = child.OwnerID;
                        child.Inventory.ChangeInventoryOwner(groupID);
                    }

                    sog.SetOwnerId(groupID);
                    sog.ApplyNextOwnerPermissions();
                }
            }

            foreach (uint localID in localIDs)
            {
                SceneObjectPart part = m_parentScene.GetSceneObjectPart(localID);
                part.GetProperties(remoteClient);
            }
        }

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

        public void HandleObjectPermissionsUpdate(IClientAPI controller, UUID agentID, UUID sessionID, byte field, uint localId, uint mask, byte set)
        {
            // Check for spoofing..  since this is permissions we're talking about here!
            if ((controller.SessionId == sessionID) && (controller.AgentId == agentID))
            {
                // Tell the object to do permission update
                if (localId != 0)
                {
                    SceneObjectGroup chObjectGroup = m_parentScene.GetGroupByPrim(localId);
                    if (chObjectGroup != null)
                    {
                        if (m_parentScene.Permissions.CanEditObject(chObjectGroup.UUID, controller.AgentId))
                            chObjectGroup.UpdatePermissions(agentID, field, localId, mask, set);
                    }
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
                        IAttachmentsModule attachModule = m_parentScene.RequestModuleInterface<IAttachmentsModule>();
                        if (attachModule != null)
                            attachModule.UpdateAttachmentPosition(remoteClient, ((SceneObjectGroup)entity), pos);
                    }
                    else
                    {
                        ((SceneObjectGroup)entity).UpdateGroupPosition(pos, SaveUpdate);
                    }
                }
                else
                {
                    ScenePresence SP = GetScenePresence(remoteClient.AgentId);
                    ((SceneObjectGroup)entity).ScheduleGroupUpdateToAvatar(SP, PrimUpdateFlags.FullUpdate);
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
                ((SceneObjectGroup)group).ScheduleGroupTerseUpdate();
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
                    ((SceneObjectGroup)group).ScheduleGroupUpdate(PrimUpdateFlags.ClickAction);
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
                    ((SceneObjectGroup)group).ScheduleGroupUpdate(PrimUpdateFlags.ClickAction);
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
                    ((SceneObjectGroup)group).ScheduleGroupUpdate(PrimUpdateFlags.ClickAction);
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
                    ((SceneObjectGroup)group).ScheduleGroupUpdate(PrimUpdateFlags.ClickAction);
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

        /// <summary>
        /// Make this object be added to search
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="IncludeInSearch"></param>
        /// <param name="LocalID"></param>
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
            }
            else if (!IncludeInSearch && m_parentScene.Permissions.CanEditObject(objid, user))
            {
                grp.RootPart.RemFlag(PrimFlags.JointWheel);
            }
#pragma warning restore 0612
        }

        /// <summary>
        /// Duplicate the given entity and add it to the world
        /// </summary>
        /// <param name="LocalID">LocalID of the object to duplicate</param>
        /// <param name="offset">Duplicated objects position offset from the original entity</param>
        /// <param name="flags">Flags to give the Duplicated object</param>
        /// <param name="AgentID"></param>
        /// <param name="GroupID"></param>
        /// <param name="rot">Rotation to have the duplicated entity set to</param>
        /// <returns></returns>
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
                    duplicatedGroup.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);
                    duplicatedGroup.ResumeScripts();

                    // required for physics to update it's position
                    duplicatedGroup.AbsolutePosition = duplicatedGroup.AbsolutePosition;

                    m_parentScene.EventManager.TriggerParcelPrimCountTainted();
                    return true;
                }
            }
            return false;
        }

        #region Linking and Delinking

        public void DelinkObjects(List<uint> primIds, IClientAPI client)
        {
            List<SceneObjectPart> parts = new List<SceneObjectPart>();

            foreach (uint localID in primIds)
            {
                SceneObjectPart part = m_parentScene.GetSceneObjectPart(localID);

                if (part == null)
                    continue;

                if (m_parentScene.Permissions.CanDelinkObject(client.AgentId, part.ParentGroup.RootPart.UUID))
                    parts.Add(part);
            }

            DelinkObjects(parts);
        }

        public void LinkObjects(IClientAPI client, uint parentPrimId, List<uint> childPrimIds)
        {
            List<UUID> owners = new List<UUID>();

            List<SceneObjectPart> children = new List<SceneObjectPart>();
            SceneObjectPart root = m_parentScene.GetSceneObjectPart(parentPrimId);

            if (root == null)
            {
                m_log.DebugFormat("[LINK]: Can't find linkset root prim {0}", parentPrimId);
                return;
            }

            if (!m_parentScene.Permissions.CanLinkObject(client.AgentId, root.ParentGroup.RootPart.UUID))
            {
                m_log.DebugFormat("[LINK]: Refusing link. No permissions on root prim");
                return;
            }

            foreach (uint localID in childPrimIds)
            {
                SceneObjectPart part = m_parentScene.GetSceneObjectPart(localID);

                if (part == null)
                    continue;

                if (!owners.Contains(part.OwnerID))
                    owners.Add(part.OwnerID);

                if (m_parentScene.Permissions.CanLinkObject(client.AgentId, part.ParentGroup.RootPart.UUID))
                    children.Add(part);
            }

            // Must be all one owner
            //
            if (owners.Count > 1)
            {
                m_log.DebugFormat("[LINK]: Refusing link. Too many owners");
                client.SendAlertMessage("Permissions: Cannot link, too many owners.");
                return;
            }

            if (children.Count == 0)
            {
                m_log.DebugFormat("[LINK]: Refusing link. No permissions to link any of the children");
                client.SendAlertMessage("Permissions: Cannot link, not enough permissions.");
                return;
            }
            int LinkCount = 0;
            foreach (SceneObjectPart part in children)
            {
                LinkCount += part.ParentGroup.ChildrenList.Count;
            }

            IOpenRegionSettingsModule module = m_parentScene.RequestModuleInterface<IOpenRegionSettingsModule>();
            if (module != null)
            {
                if (LinkCount > module.MaximumLinkCount &&
                    module.MaximumLinkCount != -1)
                {
                    client.SendAlertMessage("You cannot link more than " + module.MaximumLinkCount + " prims. Please try again with fewer prims.");
                    return;
                }
                if ((root.Flags & PrimFlags.Physics) == PrimFlags.Physics)
                {
                    //We only check the root here because if the root is physical, it will be applied to all during the link
                    if (LinkCount > module.MaximumLinkCountPhys &&
                        module.MaximumLinkCountPhys != -1)
                    {
                        client.SendAlertMessage("You cannot link more than " + module.MaximumLinkCountPhys + " physical prims. Please try again with fewer prims.");
                        return;
                    }
                }
            }

            LinkObjects(root, children);
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

                // We need to explicitly resend the newly link prim's object properties since no other actions
                // occur on link to invoke this elsewhere (such as object selection)
                parentGroup.RootPart.CreateSelected = true;
                parentGroup.HasGroupChanged = true;
                //parentGroup.RootPart.SendFullUpdateToAllClients(PrimUpdateFlags.FullUpdate);
                //parentGroup.ScheduleGroupForFullUpdate(PrimUpdateFlags.FullUpdate);
                parentGroup.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);
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
                    child.ParentGroup.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);
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
                                newChild.ClearUpdateScheduleOnce();

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
                    g.ScheduleGroupUpdate(PrimUpdateFlags.FullUpdate);
                }
                //Fix undo states now that the linksets have been changed
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

        /// <summary>
        /// Sorts a list of Parts by Link Number so they end up in the correct order
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public int linkSetSorter(ISceneEntity a, ISceneEntity b)
        {
            return a.LinkNum.CompareTo(b.LinkNum);
        }

        #endregion

        #endregion

        #region New Scene Entity Manager Code

        #region Non EntityBase methods that need cleaned up later

        public bool LinkPartToSOG(SceneObjectGroup grp, SceneObjectPart part, int linkNum)
        {
            part.SetParentLocalId(grp.RootPart.LocalId);
            part.SetParent(grp);
            // Insert in terms of link numbers, the new links
            // before the current ones (with the exception of 
            // the root prim. Shuffle the old ones up
            foreach (ISceneEntity otherPart in grp.ChildrenEntities())
            {
                if (otherPart.LinkNum >= linkNum)
                {
                    // Don't update root prim link number
                    otherPart.LinkNum += 1;
                }
            }
            part.LinkNum = linkNum;
            return LinkPartToEntity(grp, part);
        }

        #endregion

        #region Wrapper Methods

        /// <summary>
        /// Dupliate the entity and add it to the Scene
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public EntityBase DuplicateEntity(EntityBase entity)
        {
            //Make an exact copy of the entity
            EntityBase copiedEntity = entity.Copy(false);
            //Add the entity to the scene and back it up
            AddPrimToScene(copiedEntity);
            //Fix physics representation now
//            entity.RebuildPhysicalRepresentation();
            return copiedEntity;
        }

        /// <summary>
        /// Add the new part to the group in the EntityManager
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="part"></param>
        /// <returns></returns>
        public bool LinkPartToEntity(EntityBase entity, ISceneEntity part)
        {
            //Remove the entity so that we can rebuild
            RemoveEntity(entity);
            bool RetVal = entity.LinkChild(part);
            AddEntity(entity, false);
            //Now that everything is linked, destroy the undo states because it will fry the link otherwise
            entity.ClearUndoState();
            return RetVal;
        }

        /// <summary>
        /// Delinks the object from the group in the EntityManager
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="part"></param>
        /// <returns></returns>
        public bool DeLinkPartFromEntity(EntityBase entity, ISceneEntity part)
        {
            //Remove the entity so that we can rebuild
            RemoveEntity(entity);
            bool RetVal = entity.RemoveChild(part);
            AddEntity(entity, false);
            //Now that everything is linked, destroy the undo states because it will fry the object otherwise
            entity.ClearUndoState();
            return RetVal;
        }

        /// <summary>
        /// THIS IS TO ONLY BE CALLED WHEN AN OBJECT UUID IS UPDATED!!!
        /// This method is HIGHLY unsafe and destroys the integrity of the checks above!
        /// This is NOT to be used lightly! Do NOT use this unless you have to!
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="newID">new UUID to set the root part to</param>
        public void UpdateEntity(SceneObjectGroup entity, UUID newID)
        {
            RemoveEntity(entity);
            //Set it to the root so that we don't create an infinite loop as the ONLY place this should be being called is from the setter in SceneObjectGroup.UUID
            entity.RootPart.UUID = newID;
            AddEntity(entity, false);
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

        /// <summary>
        /// Get a part (SceneObjectPart) from the EntityManager by LocalID
        /// </summary>
        /// <param name="LocalID"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get a part (SceneObjectPart) from the EntityManager by UUID
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
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
        /// Get this prim ready to add to the scene
        /// </summary>
        /// <param name="entity"></param>
        public void PrepPrimForAdditionToScene(EntityBase entity)
        {
            ResetEntityIDs(entity);
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
            //Now save the entity that we have 
            return AddEntity(entity, false);
        }

        /// <summary>
        /// Add the Entity to the Scene and back it up, but do NOT reset its ID's
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool RestorePrimToScene(EntityBase entity)
        {
            List<ISceneEntity> children = entity.ChildrenEntities();
            //Sort so that we rebuild in the same order and the root being first
            children.Sort(linkSetSorter);

            entity.ClearChildren();

            foreach (ISceneEntity child in children)
            {
                if (((SceneObjectPart)child).PhysActor != null)
                    ((SceneObjectPart)child).PhysActor.LocalID = child.LocalId;
                if (child.LocalId == 0)
                    child.LocalId = AllocateLocalId();
                entity.AddChild(child, child.LinkNum);
            }
            //Force the prim to backup now that it has been added
            entity.ForcePersistence();
            //Tell the entity that they are being added to a scene
            entity.AttachToScene(m_parentScene);
            //Now save the entity that we have 
            return AddEntity(entity, false);
        }

        /// <summary>
        /// Move this group from inside of another group into the Scene as a full member
        ///  This does not reset IDs so that it is updated correctly in the client
        /// </summary>
        /// <param name="entity"></param>
        public void DelinkPartToScene(EntityBase entity)
        {
            //Force the prim to backup now that it has been added
            entity.ForcePersistence();
            //Tell the entity that they are being added to a scene
            entity.AttachToScene(m_parentScene);
            //Now save the entity that we have 
            AddEntity(entity, false);
        }

        /// <summary>
        /// Destroy the entity and remove it from the scene
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool DeleteEntity(EntityBase entity)
        {
            if (entity.IsPhysical())
                RemovePhysicalPrim(entity.ChildrenEntities().Count);
            return RemoveEntity(entity);
        }

        #endregion

        #region Private Methods

        ///These methods are UNSAFE to be accessed from outside this manager, if they are, BAD things WILL happen.
        /// If these are changed so that they can be accessed from the outside, ghost prims and other nasty things will occur unless you are EXTREMELY careful.
        /// If more changes need to occur in this area, you must use public methods to safely add/update/remove objects from the EntityManager

        /// <summary>
        /// Remove this entity fully from the EntityManager
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private bool RemoveEntity(EntityBase entity)
        {
            m_numPrim -= entity.ChildrenEntities().Count;
            Entities.Remove(entity.UUID);
            Entities.Remove(entity.LocalId);
            return true;
        }

        /// <summary>
        /// Add this entity to the EntityManager
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="AllowUpdate"></param>
        /// <returns></returns>
        private bool AddEntity(EntityBase entity, bool AllowUpdate)
        {
            //Update our prim count
            m_numPrim += entity.ChildrenEntities().Count;
            Entities.Add(entity);
            return true;
        }

        /// <summary>
        /// Reset all of the UUID's, localID's, etc in this group (includes children)
        /// </summary>
        /// <param name="entity"></param>
        private void ResetEntityIDs(EntityBase entity)
        {
            List<ISceneEntity> children = entity.ChildrenEntities();
            //Sort so that we rebuild in the same order and the root being first
            children.Sort(linkSetSorter);

            entity.ClearChildren();

            foreach (ISceneEntity child in children)
            {
                child.ResetEntityIDs();
                entity.AddChild(child, child.LinkNum);
            }
        }

        /// <summary>
        /// Returns a new unallocated local ID
        /// </summary>
        /// <returns>A brand new local ID</returns>
        public uint AllocateLocalId()
        {
            uint myID;

            _primAllocateMutex.WaitOne();
            myID = ++m_lastAllocatedLocalId;
            _primAllocateMutex.ReleaseMutex();

            return myID;
        }

        /// <summary>
        /// Check all the localIDs in this group to make sure that they have not been used previously
        /// </summary>
        /// <param name="group"></param>
        public void CheckAllocationOfLocalIds(SceneObjectGroup group)
        {
            foreach (SceneObjectPart part in group.ChildrenList)
            {
                if (part.LocalId != 0)
                    CheckAllocationOfLocalId(part.LocalId);
            }
        }

        /// <summary>
        /// Make sure that this localID has not been used earlier in the Scene Startup
        /// </summary>
        /// <param name="LocalID"></param>
        private void CheckAllocationOfLocalId(uint LocalID)
        {
            _primAllocateMutex.WaitOne();
            if (LocalID > m_lastAllocatedLocalId)
                m_lastAllocatedLocalId = LocalID;
            _primAllocateMutex.ReleaseMutex();
        }

        #endregion

        #endregion
    }
}