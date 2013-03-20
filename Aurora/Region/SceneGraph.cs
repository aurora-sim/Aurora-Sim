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

using Aurora.Framework;
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.Physics;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.SceneInfo.Entities;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Aurora.Region
{
    /// <summary>
    ///     This class used to be called InnerScene and may not yet truly be a SceneGraph.  The non scene graph components
    ///     should be migrated out over time.
    /// </summary>
    public class SceneGraph : ISceneGraph
    {
        #region Declares

        protected internal EntityManager Entities = new EntityManager();

        protected RegionInfo m_regInfo;
        protected IScene m_parentScene;
        protected bool EnableFakeRaycasting = false;
        protected string m_DefaultObjectName = "Primitive";

        /// <summary>
        ///     The last allocated local prim id.  When a new local id is requested, the next number in the sequence is
        ///     dispensed.
        /// </summary>
        protected uint m_lastAllocatedLocalId = 720000;

        private readonly object _primAllocateLock = new object();

        protected internal object m_syncRoot = new object();

        protected internal PhysicsScene _PhyScene;

        private readonly Object m_updateLock = new Object();

        public PhysicsScene PhysicsScene
        {
            get { return _PhyScene; }
            set { _PhyScene = value; }
        }

        #endregion

        #region Constructor and close

        protected internal SceneGraph(IScene parent, RegionInfo regInfo)
        {
            Random random = new Random();
            m_lastAllocatedLocalId = (uint) (random.NextDouble()*(uint.MaxValue/2)) + uint.MaxValue/4;
            m_parentScene = parent;
            m_regInfo = regInfo;

            //Subscript to the scene events
            m_parentScene.EventManager.OnNewClient += SubscribeToClientEvents;
            m_parentScene.EventManager.OnClosingClient += UnSubscribeToClientEvents;

            IConfig aurorastartupConfig = parent.Config.Configs["AuroraStartup"];
            if (aurorastartupConfig != null)
            {
                m_DefaultObjectName = aurorastartupConfig.GetString("DefaultObjectName", m_DefaultObjectName);
                EnableFakeRaycasting = aurorastartupConfig.GetBoolean("EnableFakeRaycasting", false);
            }
        }

        protected internal void Close()
        {
            Entities.Clear();
            //Remove the events
            m_parentScene.EventManager.OnNewClient -= SubscribeToClientEvents;
            m_parentScene.EventManager.OnClosingClient -= UnSubscribeToClientEvents;
        }

        #endregion

        #region Update Methods

        protected internal void UpdatePreparePhysics()
        {
            // If we are using a threaded physics engine
            // grab the latest scene from the engine before
            // trying to process it.

            // PhysX does this (runs in the background).

            if (_PhyScene != null && _PhyScene.IsThreaded)
            {
                _PhyScene.GetResults();
            }
        }

        private readonly object m_taintedPresencesLock = new object();
        private readonly List<IScenePresence> m_taintedPresences = new List<IScenePresence>();

        public void TaintPresenceForUpdate(IScenePresence presence, PresenceTaint taint)
        {
            lock (m_taintedPresencesLock)
            {
                if (!presence.IsTainted) //We ONLY set the IsTainted under this lock, so we can trust it
                    m_taintedPresences.Add(presence);
                presence.Taints |= taint;
            }
        }

        protected internal void UpdateEntities()
        {
            IScenePresence[] presences;
            lock (m_taintedPresencesLock)
            {
                presences = new IScenePresence[m_taintedPresences.Count];
                m_taintedPresences.CopyTo(presences);
                m_taintedPresences.Clear();
            }
            foreach (IScenePresence presence in presences)
            {
                presence.IsTainted = false;
                    //We set this first so that it is cleared out, but also so that the method can re-taint us
                presence.Update();
            }
        }

        protected internal void UpdatePhysics(double elapsed)
        {
            if (_PhyScene == null)
                return;
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

                _PhyScene.Simulate((float) elapsed);
            }
        }

        private List<Vector3> m_oldCoarseLocations = new List<Vector3>();
        private List<UUID> m_oldAvatarUUIDs = new List<UUID>();

        public bool GetCoarseLocations(out List<Vector3> coarseLocations, out List<UUID> avatarUUIDs, uint maxLocations)
        {
            coarseLocations = new List<Vector3>();
            avatarUUIDs = new List<UUID>();

            List<IScenePresence> presences = GetScenePresences();
            for (int i = 0; i < Math.Min(presences.Count, maxLocations); i++)
            {
                IScenePresence sp = presences[i];
                // If this presence is a child agent, we don't want its coarse locations
                if (sp.IsChildAgent)
                    continue;

                if (sp.ParentID != UUID.Zero)
                {
                    // sitting avatar
                    ISceneChildEntity sop = m_parentScene.GetSceneObjectPart(sp.ParentID);
                    if (sop != null)
                    {
                        coarseLocations.Add(sop.AbsolutePosition + sp.OffsetPosition);
                        avatarUUIDs.Add(sp.UUID);
                    }
                    else
                    {
                        // we can't find the parent..  ! arg!
                        MainConsole.Instance.Warn("Could not find parent prim for avatar " + sp.Name);
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

            if (m_oldCoarseLocations.Count == coarseLocations.Count)
            {
                List<UUID> foundAvies = new List<UUID>(m_oldAvatarUUIDs);
                foreach (UUID t in avatarUUIDs)
                {
                    foundAvies.Remove(t);
                }
                if (foundAvies.Count == 0)
                {
                    //All avies are still the same, check their locations now
                    for (int i = 0; i < avatarUUIDs.Count; i++)
                    {
                        if (m_oldCoarseLocations[i].ApproxEquals(coarseLocations[i], 5))
                            continue;
                        m_oldCoarseLocations = coarseLocations;
                        m_oldAvatarUUIDs = avatarUUIDs;
                        return true;
                    }
                    //Things are still close enough to the same
                    return false;
                }
            }
            m_oldCoarseLocations = coarseLocations;
            m_oldAvatarUUIDs = avatarUUIDs;
            //Its changed, tell it to send new
            return true;
        }

        #endregion

        #region Entity Methods

        protected internal void HandleUndo(IClientAPI remoteClient, UUID primId)
        {
            if (primId != UUID.Zero)
            {
                ISceneChildEntity part = m_parentScene.GetSceneObjectPart(primId);
                if (part != null)
                    if (m_parentScene.Permissions.CanEditObject(part.UUID, remoteClient.AgentId))
                        part.Undo();
            }
        }

        protected internal void HandleRedo(IClientAPI remoteClient, UUID primId)
        {
            if (primId != UUID.Zero)
            {
                ISceneChildEntity part = m_parentScene.GetSceneObjectPart(primId);
                if (part != null)
                    if (m_parentScene.Permissions.CanEditObject(part.UUID, remoteClient.AgentId))
                        part.Redo();
            }
        }

        protected internal void HandleObjectGroupUpdate(
            IClientAPI remoteClient, UUID GroupID, uint LocalID, UUID Garbage)
        {
            IEntity entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (m_parentScene.Permissions.CanEditObject(entity.UUID, remoteClient.AgentId))
                    if (((ISceneEntity) entity).OwnerID == remoteClient.AgentId)
                        ((ISceneEntity) entity).SetGroup(GroupID, remoteClient.AgentId, true);
            }
        }

        /// <summary>
        ///     Add a presence to the scene
        /// </summary>
        /// <param name="presence"></param>
        protected internal void AddScenePresence(IScenePresence presence)
        {
            AddEntity(presence, true);
        }

        /// <summary>
        ///     Remove a presence from the scene
        /// </summary>
        protected internal void RemoveScenePresence(IEntity agent)
        {
            if (!Entities.Remove(agent))
            {
                MainConsole.Instance.WarnFormat(
                    "[SCENE]: Tried to remove non-existent scene presence with agent ID {0} from scene Entities list",
                    agent.UUID);
                return;
            }
        }

        #endregion

        #region Get Methods

        /// <summary>
        ///     Get a reference to the scene presence list. Changes to the list will be done in a copy
        ///     There is no guarantee that presences will remain in the scene after the list is returned.
        ///     This list should remain private to SceneGraph. Callers wishing to iterate should instead
        ///     pass a delegate to ForEachScenePresence.
        /// </summary>
        /// <returns></returns>
        public List<IScenePresence> GetScenePresences()
        {
            return Entities.GetPresences();
        }

        /// <summary>
        ///     Request a scene presence by UUID. Fast, indexed lookup.
        /// </summary>
        /// <param name="agentID"></param>
        /// <returns>null if the presence was not found</returns>
        protected internal IScenePresence GetScenePresence(UUID agentID)
        {
            IScenePresence sp;
            Entities.TryGetPresenceValue(agentID, out sp);
            return sp;
        }

        /// <summary>
        ///     Request the scene presence by name.
        ///     NOTE: Depricated, use the ScenePresence GetScenePresence (string Name) instead!
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <returns>null if the presence was not found</returns>
        public IScenePresence GetScenePresence(string firstName, string lastName)
        {
            List<IScenePresence> presences = GetScenePresences();
            return presences.FirstOrDefault(presence => presence.Firstname == firstName && presence.Lastname == lastName);
        }

        /// <summary>
        ///     Request the scene presence by localID.
        /// </summary>
        /// <param name="localID"></param>
        /// <returns>null if the presence was not found</returns>
        public IScenePresence GetScenePresence(uint localID)
        {
            List<IScenePresence> presences = GetScenePresences();
            return presences.FirstOrDefault(presence => presence.LocalId == localID);
        }

        protected internal bool TryGetScenePresence(UUID agentID, out IScenePresence avatar)
        {
            return Entities.TryGetPresenceValue(agentID, out avatar);
        }

        protected internal bool TryGetAvatarByName(string name, out IScenePresence avatar)
        {
            avatar =
                GetScenePresences()
                    .FirstOrDefault(presence => String.Compare(name, presence.ControllingClient.Name, true) == 0);

            return (avatar != null);
        }

        /// <summary>
        ///     Get a scene object group that contains the prim with the given uuid
        /// </summary>
        /// <param name="hray"></param>
        /// <param name="frontFacesOnly"></param>
        /// <param name="faceCenters"></param>
        /// <returns>null if no scene object group containing that prim is found</returns>
        protected internal EntityIntersection GetClosestIntersectingPrim(Ray hray, bool frontFacesOnly, bool faceCenters)
        {
            // Primitive Ray Tracing
            float closestDistance = 280f;
            EntityIntersection result = new EntityIntersection();
            ISceneEntity[] EntityList = Entities.GetEntities(hray.Origin, closestDistance);
            foreach (ISceneEntity ent in EntityList)
            {
                if (ent is SceneObjectGroup)
                {
                    SceneObjectGroup reportingG = (SceneObjectGroup) ent;
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
        ///     Gets a list of scene object group that intersect with the given ray
        /// </summary>
        public List<EntityIntersection> GetIntersectingPrims(Ray hray, float length, int count,
                                                             bool frontFacesOnly, bool faceCenters, bool getAvatars,
                                                             bool getLand, bool getPrims)
        {
            // Primitive Ray Tracing
            List<EntityIntersection> result = new List<EntityIntersection>(count);
            if (getPrims)
            {
                ISceneEntity[] EntityList = Entities.GetEntities(hray.Origin, length);

                result.AddRange(
                    EntityList.OfType<SceneObjectGroup>()
                              .Select(reportingG => reportingG.TestIntersection(hray, frontFacesOnly, faceCenters))
                              .Where(inter => inter.HitTF));
            }
            if (getAvatars)
            {
                List<IScenePresence> presenceList = Entities.GetPresences();
                foreach (IScenePresence ent in presenceList)
                {
                    //Do rough approximation and keep the # of loops down
                    Vector3 newPos = hray.Origin;
                    for (int i = 0; i < 100; i++)
                    {
                        newPos += ((Vector3.One*(length*(i/100)))*hray.Direction);
                        if (ent.AbsolutePosition.ApproxEquals(newPos, ent.PhysicsActor.Size.X*2))
                        {
                            EntityIntersection intersection = new EntityIntersection();
                            intersection.distance = length*(i/100);
                            intersection.face = 0;
                            intersection.HitTF = true;
                            intersection.obj = ent;
                            intersection.ipoint = newPos;
                            intersection.normal = newPos;
                            result.Add(intersection);
                            break;
                        }
                    }
                }
            }
            if (getLand)
            {
                //TODO
            }

            result.Sort((a, b) => a.distance.CompareTo(b.distance));

            if (result.Count > count)
                result.RemoveRange(count, result.Count - count);
            return result;
        }

        #endregion

        #region ForEach* Methods

        /// <summary>
        ///     Performs action on all scene object groups.
        /// </summary>
        /// <param name="action"></param>
        protected internal void ForEachSceneEntity(Action<ISceneEntity> action)
        {
            ISceneEntity[] objlist = Entities.GetEntities();
            foreach (ISceneEntity obj in objlist)
            {
                try
                {
                    action(obj);
                }
                catch (Exception e)
                {
                    // Catch it and move on. This includes situations where splist has inconsistent info
                    MainConsole.Instance.WarnFormat("[SCENE]: Problem processing action in ForEachSOG: {0}",
                                                    e.ToString());
                }
            }
        }

        /// <summary>
        ///     Performs action on all scene presences. This can ultimately run the actions in parallel but
        ///     any delegates passed in will need to implement their own locking on data they reference and
        ///     modify outside of the scope of the delegate.
        /// </summary>
        /// <param name="action"></param>
        public void ForEachScenePresence(Action<IScenePresence> action)
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
                        MainConsole.Instance.Info("[BUG] in " + m_parentScene.RegionInfo.RegionName + ": " + e.ToString());
                        MainConsole.Instance.Info("[BUG] Stack Trace: " + e.StackTrace);
                    }
                });
            Parallel.ForEach<ScenePresence>(GetScenePresences(), protectedAction);
            */
            // For now, perform actions serially
            List<IScenePresence> presences = new List<IScenePresence>(GetScenePresences());
            foreach (IScenePresence sp in presences)
            {
                try
                {
                    action(sp);
                }
                catch (Exception e)
                {
                    MainConsole.Instance.Info("[BUG] in " + m_parentScene.RegionInfo.RegionName + ": " + e.ToString());
                    MainConsole.Instance.Info("[BUG] Stack Trace: " + e.StackTrace);
                }
            }
        }

        #endregion ForEach* Methods

        #region Client Event handlers

        public void SubscribeToClientEvents(IClientAPI client)
        {
            client.OnUpdatePrimGroupPosition += UpdatePrimPosition;
            client.OnUpdatePrimSinglePosition += UpdatePrimSinglePosition;
            client.OnUpdatePrimGroupRotation += UpdatePrimRotation;
            client.OnUpdatePrimGroupMouseRotation += UpdatePrimRotation;
            client.OnUpdatePrimSingleRotation += UpdatePrimSingleRotation;
            client.OnUpdatePrimSingleRotationPosition += UpdatePrimSingleRotationPosition;
            client.OnUpdatePrimScale += UpdatePrimScale;
            client.OnUpdatePrimGroupScale += UpdatePrimGroupScale;
            client.OnUpdateExtraParams += UpdateExtraParam;
            client.OnUpdatePrimShape += UpdatePrimShape;
            client.OnUpdatePrimTexture += UpdatePrimTexture;
            client.OnGrabUpdate += MoveObject;
            client.OnSpinStart += SpinStart;
            client.OnSpinUpdate += SpinObject;

            client.OnObjectName += PrimName;
            client.OnObjectClickAction += PrimClickAction;
            client.OnObjectMaterial += PrimMaterial;
            client.OnLinkObjects += LinkObjects;
            client.OnDelinkObjects += DelinkObjects;
            client.OnObjectDuplicate += DuplicateObject;
            client.OnUpdatePrimFlags += UpdatePrimFlags;
            client.OnRequestObjectPropertiesFamily += RequestObjectPropertiesFamily;
            client.OnObjectPermissions += HandleObjectPermissionsUpdate;
            client.OnGrabObject += ProcessObjectGrab;
            client.OnGrabUpdate += ProcessObjectGrabUpdate;
            client.OnDeGrabObject += ProcessObjectDeGrab;
            client.OnUndo += HandleUndo;
            client.OnRedo += HandleRedo;
            client.OnObjectDescription += PrimDescription;
            client.OnObjectIncludeInSearch += MakeObjectSearchable;
            client.OnObjectOwner += ObjectOwner;
            client.OnObjectGroupRequest += HandleObjectGroupUpdate;

            client.OnAddPrim += AddNewPrim;
            client.OnObjectDuplicateOnRay += doObjectDuplicateOnRay;
        }

        public void UnSubscribeToClientEvents(IClientAPI client)
        {
            client.OnUpdatePrimGroupPosition -= UpdatePrimPosition;
            client.OnUpdatePrimSinglePosition -= UpdatePrimSinglePosition;
            client.OnUpdatePrimGroupRotation -= UpdatePrimRotation;
            client.OnUpdatePrimGroupMouseRotation -= UpdatePrimRotation;
            client.OnUpdatePrimSingleRotation -= UpdatePrimSingleRotation;
            client.OnUpdatePrimSingleRotationPosition -= UpdatePrimSingleRotationPosition;
            client.OnUpdatePrimScale -= UpdatePrimScale;
            client.OnUpdatePrimGroupScale -= UpdatePrimGroupScale;
            client.OnUpdateExtraParams -= UpdateExtraParam;
            client.OnUpdatePrimShape -= UpdatePrimShape;
            client.OnUpdatePrimTexture -= UpdatePrimTexture;
            client.OnGrabUpdate -= MoveObject;
            client.OnSpinStart -= SpinStart;
            client.OnSpinUpdate -= SpinObject;
            client.OnObjectName -= PrimName;
            client.OnObjectClickAction -= PrimClickAction;
            client.OnObjectMaterial -= PrimMaterial;
            client.OnLinkObjects -= LinkObjects;
            client.OnDelinkObjects -= DelinkObjects;
            client.OnObjectDuplicate -= DuplicateObject;
            client.OnUpdatePrimFlags -= UpdatePrimFlags;
            client.OnRequestObjectPropertiesFamily -= RequestObjectPropertiesFamily;
            client.OnObjectPermissions -= HandleObjectPermissionsUpdate;
            client.OnGrabObject -= ProcessObjectGrab;
            client.OnGrabUpdate -= ProcessObjectGrabUpdate;
            client.OnDeGrabObject -= ProcessObjectDeGrab;
            client.OnUndo -= HandleUndo;
            client.OnRedo -= HandleRedo;
            client.OnObjectDescription -= PrimDescription;
            client.OnObjectIncludeInSearch -= MakeObjectSearchable;
            client.OnObjectOwner -= ObjectOwner;
            client.OnObjectGroupRequest -= HandleObjectGroupUpdate;
            client.OnAddPrim -= AddNewPrim;
            client.OnObjectDuplicateOnRay -= doObjectDuplicateOnRay;
        }

        public virtual void ProcessObjectGrab(uint localID, Vector3 offsetPos, IClientAPI remoteClient,
                                              List<SurfaceTouchEventArgs> surfaceArgs)
        {
            SurfaceTouchEventArgs surfaceArg = null;
            if (surfaceArgs != null && surfaceArgs.Count > 0)
                surfaceArg = surfaceArgs[0];
            ISceneChildEntity childPrim;
            if (TryGetPart(localID, out childPrim))
            {
                SceneObjectPart part = childPrim as SceneObjectPart;
                if (part != null)
                {
                    SceneObjectGroup obj = part.ParentGroup;
                    if (obj.RootPart.BlockGrab || obj.RootPart.BlockGrabObject)
                        return;
                    // Currently only grab/touch for the single prim
                    // the client handles rez correctly
                    obj.ObjectGrabHandler(localID, offsetPos, remoteClient);

                    // If the touched prim handles touches, deliver it
                    // If not, deliver to root prim
                    m_parentScene.EventManager.TriggerObjectGrab(part, part, part.OffsetPosition, remoteClient,
                                                                 surfaceArg);
                    // Deliver to the root prim if the touched prim doesn't handle touches
                    // or if we're meant to pass on touches anyway. Don't send to root prim
                    // if prim touched is the root prim as we just did it
                    if ((part.LocalId != obj.RootPart.LocalId))
                    {
                        const int PASS_IF_NOT_HANDLED = 0;
                        const int PASS_ALWAYS = 1;
                        const int PASS_NEVER = 2;
                        if (part.PassTouch == PASS_NEVER)
                        {
                        }
                        if (part.PassTouch == PASS_ALWAYS)
                        {
                            m_parentScene.EventManager.TriggerObjectGrab(obj.RootPart, part, part.OffsetPosition,
                                                                         remoteClient, surfaceArg);
                        }
                        else if (((part.ScriptEvents & scriptEvents.touch_start) == 0) &&
                                 part.PassTouch == PASS_IF_NOT_HANDLED) //If no event in this prim, pass to parent
                        {
                            m_parentScene.EventManager.TriggerObjectGrab(obj.RootPart, part, part.OffsetPosition,
                                                                         remoteClient, surfaceArg);
                        }
                    }
                }
            }
        }

        public virtual void ProcessObjectGrabUpdate(UUID objectID, Vector3 offset, Vector3 pos, IClientAPI remoteClient,
                                                    List<SurfaceTouchEventArgs> surfaceArgs)
        {
            SurfaceTouchEventArgs surfaceArg = null;
            if (surfaceArgs != null && surfaceArgs.Count > 0)
                surfaceArg = surfaceArgs[0];

            ISceneChildEntity childPrim;

            if (TryGetPart(objectID, out childPrim))
            {
                SceneObjectPart part = childPrim as SceneObjectPart;
                if (part != null)
                {
                    SceneObjectGroup obj = part.ParentGroup;
                    if (obj.RootPart.BlockGrab || obj.RootPart.BlockGrabObject)
                        return;

                    // If the touched prim handles touches, deliver it
                    // If not, deliver to root prim
                    m_parentScene.EventManager.TriggerObjectGrabbing(part, part, part.OffsetPosition, remoteClient,
                                                                     surfaceArg);
                    // Deliver to the root prim if the touched prim doesn't handle touches
                    // or if we're meant to pass on touches anyway. Don't send to root prim
                    // if prim touched is the root prim as we just did it

                    if ((part.LocalId != obj.RootPart.LocalId))
                    {
                        const int PASS_IF_NOT_HANDLED = 0;
                        const int PASS_ALWAYS = 1;
                        const int PASS_NEVER = 2;
                        if (part.PassTouch == PASS_NEVER)
                        {
                        }
                        if (part.PassTouch == PASS_ALWAYS)
                        {
                            m_parentScene.EventManager.TriggerObjectGrabbing(obj.RootPart, part, part.OffsetPosition,
                                                                             remoteClient, surfaceArg);
                        }
                        else if ((((part.ScriptEvents & scriptEvents.touch_start) == 0) ||
                                  ((part.ScriptEvents & scriptEvents.touch) == 0)) &&
                                 part.PassTouch == PASS_IF_NOT_HANDLED) //If no event in this prim, pass to parent
                        {
                            m_parentScene.EventManager.TriggerObjectGrabbing(obj.RootPart, part, part.OffsetPosition,
                                                                             remoteClient, surfaceArg);
                        }
                    }
                }
            }
        }

        public virtual void ProcessObjectDeGrab(uint localID, IClientAPI remoteClient,
                                                List<SurfaceTouchEventArgs> surfaceArgs)
        {
            SurfaceTouchEventArgs surfaceArg = null;
            if (surfaceArgs != null && surfaceArgs.Count > 0)
                surfaceArg = surfaceArgs[0];

            ISceneChildEntity childPrim;
            if (TryGetPart(localID, out childPrim))
            {
                SceneObjectPart part = childPrim as SceneObjectPart;
                if (part != null)
                {
                    SceneObjectGroup obj = part.ParentGroup;
                    // If the touched prim handles touches, deliver it
                    // If not, deliver to root prim
                    m_parentScene.EventManager.TriggerObjectDeGrab(part, part, remoteClient, surfaceArg);

                    if ((part.LocalId != obj.RootPart.LocalId))
                    {
                        const int PASS_IF_NOT_HANDLED = 0;
                        const int PASS_ALWAYS = 1;
                        const int PASS_NEVER = 2;
                        if (part.PassTouch == PASS_NEVER)
                        {
                        }
                        if (part.PassTouch == PASS_ALWAYS)
                        {
                            m_parentScene.EventManager.TriggerObjectDeGrab(obj.RootPart, part, remoteClient, surfaceArg);
                        }
                        else if ((((part.ScriptEvents & scriptEvents.touch_start) == 0) ||
                                  ((part.ScriptEvents & scriptEvents.touch_end) == 0)) &&
                                 part.PassTouch == PASS_IF_NOT_HANDLED) //If no event in this prim, pass to parent
                        {
                            m_parentScene.EventManager.TriggerObjectDeGrab(obj.RootPart, part, remoteClient,
                                                                           surfaceArg);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Gets a new rez location based on the raycast and the size of the object that is being rezzed.
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
        public Vector3 GetNewRezLocation(Vector3 RayStart, Vector3 RayEnd, UUID RayTargetID, Quaternion rot,
                                         byte bypassRayCast, byte RayEndIsIntersection, bool frontFacesOnly,
                                         Vector3 scale, bool FaceCenter)
        {
            Vector3 pos = Vector3.Zero;
            if (RayEndIsIntersection == 1)
            {
                pos = RayEnd;
                return pos;
            }

            if (RayTargetID != UUID.Zero)
            {
                ISceneChildEntity target = m_parentScene.GetSceneObjectPart(RayTargetID);

                Vector3 direction = Vector3.Normalize(RayEnd - RayStart);
                Vector3 AXOrigin = new Vector3(RayStart.X, RayStart.Y, RayStart.Z);
                Vector3 AXdirection = new Vector3(direction.X, direction.Y, direction.Z);

                if (target != null)
                {
                    pos = target.AbsolutePosition;
                    //MainConsole.Instance.Info("[OBJECT_REZ]: TargetPos: " + pos.ToString() + ", RayStart: " + RayStart.ToString() + ", RayEnd: " + RayEnd.ToString() + ", Volume: " + Util.GetDistanceTo(RayStart,RayEnd).ToString() + ", mag1: " + Util.GetMagnitude(RayStart).ToString() + ", mag2: " + Util.GetMagnitude(RayEnd).ToString());

                    // TODO: Raytrace better here

                    //EntityIntersection ei = m_sceneGraph.GetClosestIntersectingPrim(new Ray(AXOrigin, AXdirection));
                    Ray NewRay = new Ray(AXOrigin, AXdirection);

                    // Ray Trace against target here
                    EntityIntersection ei = target.TestIntersectionOBB(NewRay, Quaternion.Identity, frontFacesOnly,
                                                                       FaceCenter);

                    // Un-comment out the following line to Get Raytrace results printed to the console.
                    //MainConsole.Instance.Info("[RAYTRACERESULTS]: Hit:" + ei.HitTF.ToString() + " Point: " + ei.ipoint.ToString() + " Normal: " + ei.normal.ToString());
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
                        Vector3 offset = (normal*(ScaleOffset/2f));
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
                    //MainConsole.Instance.Info("[RAYTRACERESULTS]: Hit:" + ei.HitTF.ToString() + " Point: " + ei.ipoint.ToString() + " Normal: " + ei.normal.ToString());

                    pos = ei.HitTF ? new Vector3(ei.ipoint.X, ei.ipoint.Y, ei.ipoint.Z) : RayEnd;

                    return pos;
                }
            }
            // fall back to our stupid functionality
            pos = RayEnd;

            //increase height so its above the ground.
            //should be getting the normal of the ground at the rez point and using that?
            pos.Z += scale.Z/2f;
            return pos;
        }

        public virtual void AddNewPrim(UUID ownerID, UUID groupID, Vector3 RayEnd, Quaternion rot,
                                       PrimitiveBaseShape shape,
                                       byte bypassRaycast, Vector3 RayStart, UUID RayTargetID,
                                       byte RayEndIsIntersection)
        {
            Vector3 pos = GetNewRezLocation(RayStart, RayEnd, RayTargetID, rot, bypassRaycast, RayEndIsIntersection,
                                            true, new Vector3(0.5f, 0.5f, 0.5f), false);

            string reason;
            if (m_parentScene.Permissions.CanRezObject(1, ownerID, pos, out reason))
            {
                AddNewPrim(ownerID, groupID, pos, rot, shape);
            }
            else
            {
                GetScenePresence(ownerID)
                    .ControllingClient.SendAlertMessage("You do not have permission to rez objects here: " + reason);
            }
        }

        /// <summary>
        ///     Create a New SceneObjectGroup/Part by raycasting
        /// </summary>
        /// <param name="ownerID"></param>
        /// <param name="groupID"></param>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="shape"></param>
        public virtual ISceneEntity AddNewPrim(
            UUID ownerID, UUID groupID, Vector3 pos, Quaternion rot, PrimitiveBaseShape shape)
        {
            SceneObjectGroup sceneObject = new SceneObjectGroup(ownerID, pos, rot, shape, m_DefaultObjectName,
                                                                m_parentScene);

            // If an entity creator has been registered for this prim type then use that
            if (m_entityCreators.ContainsKey((PCode) shape.PCode))
            {
                sceneObject =
                    (SceneObjectGroup)
                    m_entityCreators[(PCode) shape.PCode].CreateEntity(sceneObject, ownerID, groupID, pos, rot, shape);
            }
            else
            {
                // Otherwise, use this default creation code;
                sceneObject.SetGroup(groupID, ownerID, false);
                AddPrimToScene(sceneObject);
                sceneObject.ScheduleGroupUpdate(PrimUpdateFlags.ForcedFullUpdate);
            }


            return sceneObject;
        }

        /// <summary>
        ///     Duplicates object specified by localID at position raycasted against RayTargetObject using
        ///     RayEnd and RayStart to determine what the angle of the ray is
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
                                           bool BypassRaycast, bool RayEndIsIntersection, bool CopyCenters,
                                           bool CopyRotates)
        {
            const bool frontFacesOnly = true;
            //MainConsole.Instance.Info("HITTARGET: " + RayTargetObj.ToString() + ", COPYTARGET: " + localID.ToString());
            ISceneChildEntity target = m_parentScene.GetSceneObjectPart(localID);
            ISceneChildEntity target2 = m_parentScene.GetSceneObjectPart(RayTargetObj);
            IScenePresence Sp = GetScenePresence(AgentID);
            if (target != null && target2 != null)
            {
                Vector3 pos;
                if (EnableFakeRaycasting)
                {
                    RayStart = Sp.CameraPosition;
                    RayEnd = pos = target2.AbsolutePosition;
                }
                Vector3 direction = Vector3.Normalize(RayEnd - RayStart);
                Vector3 AXOrigin = new Vector3(RayStart.X, RayStart.Y, RayStart.Z);
                Vector3 AXdirection = new Vector3(direction.X, direction.Y, direction.Z);

                if (target2.ParentEntity != null)
                {
                    pos = target2.AbsolutePosition;
                    // TODO: Raytrace better here

                    //EntityIntersection ei = m_sceneGraph.GetClosestIntersectingPrim(new Ray(AXOrigin, AXdirection), false, false);
                    Ray NewRay = new Ray(AXOrigin, AXdirection);

                    // Ray Trace against target here
                    EntityIntersection ei = target2.TestIntersectionOBB(NewRay, Quaternion.Identity, frontFacesOnly,
                                                                        CopyCenters);

                    // Un-comment out the following line to Get Raytrace results printed to the console.
                    //MainConsole.Instance.Info("[RAYTRACERESULTS]: Hit:" + ei.HitTF.ToString() + " Point: " + ei.ipoint.ToString() + " Normal: " + ei.normal.ToString());
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
                        Vector3 offset = normal*(ScaleOffset/2f);
                        pos = intersectionpoint + offset;

                        // stick in offset format from the original prim
                        pos = pos - target.ParentEntity.AbsolutePosition;
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
                            DuplicateObject(localID, pos, target.GetEffectiveObjectFlags(), AgentID, GroupID,
                                            Quaternion.Identity);
                        }
                    }

                    return;
                }

                return;
            }
        }

        /// <value>
        ///     Registered classes that are capable of creating entities.
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
        ///     Unregister a module commander and all its commands
        /// </summary>
        /// <param name="entityCreator"></param>
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

            List<ISceneEntity> groups = new List<ISceneEntity>();

            foreach (uint localID in localIDs)
            {
                ISceneChildEntity part = m_parentScene.GetSceneObjectPart(localID);
                if (!groups.Contains(part.ParentEntity))
                    groups.Add(part.ParentEntity);
            }

            foreach (ISceneEntity sog in groups)
            {
                if (ownerID != UUID.Zero)
                {
                    sog.SetOwnerId(ownerID);
                    sog.SetGroup(groupID, remoteClient.AgentId, true);
                    sog.ScheduleGroupUpdate(PrimUpdateFlags.ForcedFullUpdate);

                    foreach (ISceneChildEntity child in sog.ChildrenEntities())
                        child.Inventory.ChangeInventoryOwner(ownerID);
                }
                else
                {
                    if (!m_parentScene.Permissions.CanEditObject(sog.UUID, remoteClient.AgentId))
                        continue;

                    if (sog.GroupID != groupID)
                        continue;

                    foreach (ISceneChildEntity child in sog.ChildrenEntities())
                    {
                        child.LastOwnerID = child.OwnerID;
                        child.Inventory.ChangeInventoryOwner(groupID);
                    }

                    sog.SetOwnerId(groupID);
                    sog.ApplyNextOwnerPermissions();
                }
                //Trigger the prim count event so that this parcel gets changed!
                m_parentScene.AuroraEventManager.FireGenericEventHandler("ObjectChangedOwner", sog);
            }

            foreach (uint localID in localIDs)
            {
                ISceneChildEntity part = m_parentScene.GetSceneObjectPart(localID);
                part.GetProperties(remoteClient);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="LocalID"></param>
        /// <param name="scale"></param>
        /// <param name="remoteClient"></param>
        protected internal void UpdatePrimScale(uint LocalID, Vector3 scale, IClientAPI remoteClient)
        {
            IEntity entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (m_parentScene.Permissions.CanEditObject(((SceneObjectGroup) entity).UUID, remoteClient.AgentId))
                {
                    ((SceneObjectGroup) entity).Resize(scale, LocalID);
                }
            }
        }

        protected internal void UpdatePrimGroupScale(uint LocalID, Vector3 scale, IClientAPI remoteClient)
        {
            IEntity entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (m_parentScene.Permissions.CanEditObject(entity.UUID, remoteClient.AgentId))
                {
                    ((SceneObjectGroup) entity).GroupResize(scale, LocalID);
                }
            }
        }

        public void HandleObjectPermissionsUpdate(IClientAPI controller, UUID agentID, UUID sessionID, byte field,
                                                  uint localId, uint mask, byte set)
        {
            // Check for spoofing..  since this is permissions we're talking about here!
            if ((controller.SessionId == sessionID) && (controller.AgentId == agentID))
            {
                // Tell the object to do permission update
                if (localId != 0)
                {
                    ISceneEntity chObjectGroup = m_parentScene.GetGroupByPrim(localId);
                    if (chObjectGroup != null)
                    {
                        if (m_parentScene.Permissions.CanEditObject(chObjectGroup.UUID, controller.AgentId))
                            chObjectGroup.UpdatePermissions(agentID, field, localId, mask, set);
                    }
                }
            }
        }

        /// <summary>
        ///     This handles the nifty little tool tip that you get when you drag your mouse over an object
        ///     Send to the Object Group to process.  We don't know enough to service the request
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="AgentID"></param>
        /// <param name="RequestFlags"></param>
        /// <param name="ObjectID"></param>
        protected internal void RequestObjectPropertiesFamily(
            IClientAPI remoteClient, UUID AgentID, uint RequestFlags, UUID ObjectID)
        {
            IEntity group;
            if (TryGetEntity(ObjectID, out group))
            {
                if (group is SceneObjectGroup)
                    ((SceneObjectGroup) group).ServiceObjectPropertiesFamilyRequest(remoteClient, AgentID, RequestFlags);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="LocalID"></param>
        /// <param name="rot"></param>
        /// <param name="remoteClient"></param>
        protected internal void UpdatePrimSingleRotation(uint LocalID, Quaternion rot, IClientAPI remoteClient)
        {
            IEntity entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (m_parentScene.Permissions.CanMoveObject(((SceneObjectGroup) entity).UUID, remoteClient.AgentId))
                {
                    ((SceneObjectGroup) entity).UpdateSingleRotation(rot, LocalID);
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="LocalID"></param>
        /// <param name="rot"></param>
        /// <param name="pos"></param>
        /// <param name="remoteClient"></param>
        protected internal void UpdatePrimSingleRotationPosition(uint LocalID, Quaternion rot, Vector3 pos,
                                                                 IClientAPI remoteClient)
        {
            IEntity entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (m_parentScene.Permissions.CanMoveObject(((SceneObjectGroup) entity).UUID, remoteClient.AgentId))
                {
                    ((SceneObjectGroup) entity).UpdateSingleRotation(rot, pos, LocalID);
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="LocalID"></param>
        /// <param name="rot"></param>
        /// <param name="remoteClient"></param>
        protected internal void UpdatePrimRotation(uint LocalID, Quaternion rot, IClientAPI remoteClient)
        {
            IEntity entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (m_parentScene.Permissions.CanMoveObject(((SceneObjectGroup) entity).UUID, remoteClient.AgentId))
                {
                    ((SceneObjectGroup) entity).UpdateGroupRotationR(rot);
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="LocalID"></param>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        /// <param name="remoteClient"></param>
        protected internal void UpdatePrimRotation(uint LocalID, Vector3 pos, Quaternion rot, IClientAPI remoteClient)
        {
            IEntity entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (m_parentScene.Permissions.CanMoveObject(((SceneObjectGroup) entity).UUID, remoteClient.AgentId))
                {
                    ((SceneObjectGroup) entity).UpdateGroupRotationPR(pos, rot);
                }
            }
        }

        /// <summary>
        ///     Update the position of the given part
        /// </summary>
        /// <param name="LocalID"></param>
        /// <param name="pos"></param>
        /// <param name="remoteClient"></param>
        /// <param name="SaveUpdate"></param>
        protected internal void UpdatePrimSinglePosition(uint LocalID, Vector3 pos, IClientAPI remoteClient,
                                                         bool SaveUpdate)
        {
            IEntity entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (m_parentScene.Permissions.CanMoveObject(((SceneObjectGroup) entity).UUID, remoteClient.AgentId) ||
                    ((SceneObjectGroup) entity).IsAttachment)
                {
                    ((SceneObjectGroup) entity).UpdateSinglePosition(pos, LocalID, SaveUpdate);
                }
            }
        }

        /// <summary>
        ///     Update the position of the given part
        /// </summary>
        /// <param name="LocalID"></param>
        /// <param name="pos"></param>
        /// <param name="remoteClient"></param>
        /// <param name="SaveUpdate"></param>
        protected internal void UpdatePrimPosition(uint LocalID, Vector3 pos, IClientAPI remoteClient, bool SaveUpdate)
        {
            IEntity entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (((SceneObjectGroup) entity).IsAttachment ||
                    (((SceneObjectGroup) entity).RootPart.Shape.PCode == 9 &&
                     ((SceneObjectGroup) entity).RootPart.Shape.State != 0))
                {
                    //We don't deal with attachments, they handle themselves in the IAttachmentModule
                }
                else
                {
                    //Move has edit permission as well
                    if (
                        m_parentScene.Permissions.CanMoveObject(((SceneObjectGroup) entity).UUID, remoteClient.AgentId) &&
                        m_parentScene.Permissions.CanObjectEntry(((SceneObjectGroup) entity).UUID, false, pos,
                                                                 remoteClient.AgentId))
                    {
                        ((SceneObjectGroup) entity).UpdateGroupPosition(pos, SaveUpdate);
                    }
                    else
                    {
                        IScenePresence SP = GetScenePresence(remoteClient.AgentId);
                        ((SceneObjectGroup) entity).ScheduleGroupUpdateToAvatar(SP, PrimUpdateFlags.FullUpdate);
                    }
                }
            }
        }

        /// <summary>
        ///     Update the texture entry of the given prim.
        /// </summary>
        /// A texture entry is an object that contains details of all the textures of the prim's face.  In this case,
        /// the texture is given in its byte serialized form.
        /// <param name="LocalID"></param>
        /// <param name="texture"></param>
        /// <param name="remoteClient"></param>
        protected internal void UpdatePrimTexture(uint LocalID, byte[] texture, IClientAPI remoteClient)
        {
            IEntity entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (m_parentScene.Permissions.CanEditObject(((SceneObjectGroup) entity).UUID, remoteClient.AgentId))
                {
                    ((SceneObjectGroup) entity).UpdateTextureEntry(LocalID, texture, true);
                }
            }
        }

        /// <summary>
        ///     A user has changed an object setting
        /// </summary>
        /// <param name="LocalID"></param>
        /// <param name="blocks"></param>
        /// <param name="remoteClient"></param>
        /// <param name="UsePhysics"></param>
        /// <param name="IsTemporary"></param>
        /// <param name="IsPhantom"></param>
        protected internal void UpdatePrimFlags(uint LocalID, bool UsePhysics, bool IsTemporary, bool IsPhantom,
                                                ObjectFlagUpdatePacket.ExtraPhysicsBlock[] blocks,
                                                IClientAPI remoteClient)
        {
            IEntity entity;
            if (TryGetEntity(LocalID, out entity))
            {
                if (m_parentScene.Permissions.CanEditObject(entity.UUID, remoteClient.AgentId))
                {
                    ((SceneObjectGroup) entity).UpdatePrimFlags(LocalID, UsePhysics, IsTemporary, IsPhantom, false,
                                                                blocks);
                        // VolumeDetect can't be set via UI and will always be off when a change is made there
                }
            }
        }

        /// <summary>
        ///     Move the given object
        /// </summary>
        /// <param name="ObjectID"></param>
        /// <param name="offset"></param>
        /// <param name="pos"></param>
        /// <param name="remoteClient"></param>
        /// <param name="surfaceArgs"></param>
        protected internal void MoveObject(UUID ObjectID, Vector3 offset, Vector3 pos, IClientAPI remoteClient,
                                           List<SurfaceTouchEventArgs> surfaceArgs)
        {
            IEntity group;
            if (TryGetEntity(ObjectID, out group))
            {
                if (m_parentScene.Permissions.CanMoveObject(group.UUID, remoteClient.AgentId)) // && PermissionsMngr.)
                {
                    ((SceneObjectGroup) group).GrabMovement(offset, pos, remoteClient);
                }
            }
        }

        /// <summary>
        ///     Start spinning the given object
        /// </summary>
        /// <param name="ObjectID"></param>
        /// <param name="remoteClient"></param>
        protected internal void SpinStart(UUID ObjectID, IClientAPI remoteClient)
        {
            IEntity group;
            if (TryGetEntity(ObjectID, out group))
            {
                if (m_parentScene.Permissions.CanMoveObject(group.UUID, remoteClient.AgentId)) // && PermissionsMngr.)
                {
                    ((SceneObjectGroup) group).SpinStart(remoteClient);
                }
            }
        }

        /// <summary>
        ///     Spin the given object
        /// </summary>
        /// <param name="ObjectID"></param>
        /// <param name="rotation"></param>
        /// <param name="remoteClient"></param>
        protected internal void SpinObject(UUID ObjectID, Quaternion rotation, IClientAPI remoteClient)
        {
            IEntity group;
            if (TryGetEntity(ObjectID, out group))
            {
                if (m_parentScene.Permissions.CanMoveObject(group.UUID, remoteClient.AgentId)) // && PermissionsMngr.)
                {
                    ((SceneObjectGroup) group).SpinMovement(rotation, remoteClient);
                }
                // This is outside the above permissions condition
                // so that if the object is locked the client moving the object
                // get's it's position on the simulator even if it was the same as before
                // This keeps the moving user's client in sync with the rest of the world.
                ((SceneObjectGroup) group).ScheduleGroupTerseUpdate();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="LocalID"></param>
        /// <param name="name"></param>
        protected internal void PrimName(IClientAPI remoteClient, uint LocalID, string name)
        {
            IEntity group;
            if (TryGetEntity(LocalID, out group))
            {
                if (m_parentScene.Permissions.CanEditObject(group.UUID, remoteClient.AgentId))
                {
                    ((SceneObjectGroup) group).SetPartName(Util.CleanString(name), LocalID);
                    ((SceneObjectGroup) group).ScheduleGroupUpdate(PrimUpdateFlags.FindBest);
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="LocalID"></param>
        /// <param name="description"></param>
        /// <param name="remoteClient"></param>
        protected internal void PrimDescription(IClientAPI remoteClient, uint LocalID, string description)
        {
            IEntity group;
            if (TryGetEntity(LocalID, out group))
            {
                if (m_parentScene.Permissions.CanEditObject(group.UUID, remoteClient.AgentId))
                {
                    ((SceneObjectGroup) group).SetPartDescription(Util.CleanString(description), LocalID);
                    ((SceneObjectGroup) group).ScheduleGroupUpdate(PrimUpdateFlags.ClickAction);
                }
            }
        }

        protected internal void PrimClickAction(IClientAPI remoteClient, uint LocalID, string clickAction)
        {
            IEntity group;
            if (TryGetEntity(LocalID, out group))
            {
                if (m_parentScene.Permissions.CanEditObject(group.UUID, remoteClient.AgentId))
                {
                    ISceneChildEntity part = m_parentScene.GetSceneObjectPart(LocalID);
                    part.ClickAction = Convert.ToByte(clickAction);
                    ((ISceneEntity) group).ScheduleGroupUpdate(PrimUpdateFlags.ClickAction);
                }
            }
        }

        protected internal void PrimMaterial(IClientAPI remoteClient, uint LocalID, string material)
        {
            IEntity group;
            if (TryGetEntity(LocalID, out group))
            {
                if (m_parentScene.Permissions.CanEditObject(group.UUID, remoteClient.AgentId))
                {
                    ISceneChildEntity part = m_parentScene.GetSceneObjectPart(LocalID);
                    part.UpdateMaterial(Convert.ToInt32(material));
                    //Update the client here as well... we changed restitution and friction in the physics engine probably
                    IEventQueueService eqs = m_parentScene.RequestModuleInterface<IEventQueueService>();
                    if (eqs != null)
                        eqs.ObjectPhysicsProperties(new[] {part}, remoteClient.AgentId,
                                                    m_parentScene.RegionInfo.RegionID);

                    ((ISceneEntity) group).ScheduleGroupUpdate(PrimUpdateFlags.ClickAction);
                }
            }
        }

        protected internal void UpdateExtraParam(UUID agentID, uint LocalID, ushort type, bool inUse, byte[] data)
        {
            ISceneChildEntity part;
            if (TryGetPart(LocalID, out part))
            {
                if (m_parentScene.Permissions.CanEditObject(part.UUID, agentID))
                {
                    ((SceneObjectPart) part).UpdateExtraParam(type, inUse, data);
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="LocalID"></param>
        /// <param name="shapeBlock"></param>
        /// <param name="agentID"></param>
        protected internal void UpdatePrimShape(UUID agentID, uint LocalID, UpdateShapeArgs shapeBlock)
        {
            ISceneChildEntity part;
            if (TryGetPart(LocalID, out part))
            {
                if (m_parentScene.Permissions.CanEditObject(part.UUID, agentID))
                {
                    ObjectShapePacket.ObjectDataBlock shapeData = new ObjectShapePacket.ObjectDataBlock
                                                                      {
                                                                          ObjectLocalID = shapeBlock.ObjectLocalID,
                                                                          PathBegin = shapeBlock.PathBegin,
                                                                          PathCurve = shapeBlock.PathCurve,
                                                                          PathEnd = shapeBlock.PathEnd,
                                                                          PathRadiusOffset = shapeBlock.PathRadiusOffset,
                                                                          PathRevolutions = shapeBlock.PathRevolutions,
                                                                          PathScaleX = shapeBlock.PathScaleX,
                                                                          PathScaleY = shapeBlock.PathScaleY,
                                                                          PathShearX = shapeBlock.PathShearX,
                                                                          PathShearY = shapeBlock.PathShearY,
                                                                          PathSkew = shapeBlock.PathSkew,
                                                                          PathTaperX = shapeBlock.PathTaperX,
                                                                          PathTaperY = shapeBlock.PathTaperY,
                                                                          PathTwist = shapeBlock.PathTwist,
                                                                          PathTwistBegin = shapeBlock.PathTwistBegin,
                                                                          ProfileBegin = shapeBlock.ProfileBegin,
                                                                          ProfileCurve = shapeBlock.ProfileCurve,
                                                                          ProfileEnd = shapeBlock.ProfileEnd,
                                                                          ProfileHollow = shapeBlock.ProfileHollow
                                                                      };

                    ((SceneObjectPart) part).UpdateShape(shapeData);
                }
            }
        }

        /// <summary>
        ///     Make this object be added to search
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="IncludeInSearch"></param>
        /// <param name="LocalID"></param>
        protected internal void MakeObjectSearchable(IClientAPI remoteClient, bool IncludeInSearch, uint LocalID)
        {
            UUID user = remoteClient.AgentId;
            UUID objid = UUID.Zero;
            IEntity entity;
            if (!TryGetEntity(LocalID, out entity))
                return;
            SceneObjectGroup grp = (SceneObjectGroup) entity;
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
        ///     Duplicate the given entity and add it to the world
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
            //MainConsole.Instance.DebugFormat("[SCENE]: Duplication of object {0} at offset {1} requested by agent {2}", originalPrim, offset, AgentID);
            IEntity entity;

            if (TryGetEntity(LocalID, out entity))
            {
                SceneObjectGroup original = (SceneObjectGroup) entity;
                string reason = "You cannot duplicate this object.";
                if (
                    m_parentScene.Permissions.CanDuplicateObject(original.ChildrenList.Count, original.UUID, AgentID,
                                                                 original.AbsolutePosition) &&
                    m_parentScene.Permissions.CanRezObject(1, AgentID, original.AbsolutePosition + offset, out reason))
                {
                    ISceneEntity duplicatedEntity = DuplicateEntity(original);

                    duplicatedEntity.AbsolutePosition = duplicatedEntity.AbsolutePosition + offset;

                    SceneObjectGroup duplicatedGroup = (SceneObjectGroup) duplicatedEntity;

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

                    duplicatedGroup.CreateScriptInstances(0, true, StateSource.NewRez, UUID.Zero, false);
                    duplicatedGroup.HasGroupChanged = true;
                    duplicatedGroup.ScheduleGroupUpdate(PrimUpdateFlags.ForcedFullUpdate);

                    // required for physics to update it's position
                    duplicatedGroup.AbsolutePosition = duplicatedGroup.AbsolutePosition;
                    return true;
                }
                else
                    GetScenePresence(AgentID).ControllingClient.SendAlertMessage(reason);
            }
            return false;
        }

        #region Linking and Delinking

        public void DelinkObjects(List<uint> primIds, IClientAPI client)
        {
            List<ISceneChildEntity> parts =
                primIds.Select(localID => m_parentScene.GetSceneObjectPart(localID)).Where(part => part != null).Where(
                    part => m_parentScene.Permissions.CanDelinkObject(client.AgentId, part.ParentEntity.UUID)).ToList();

            DelinkObjects(parts);
        }

        public void LinkObjects(IClientAPI client, uint parentPrimId, List<uint> childPrimIds)
        {
            List<UUID> owners = new List<UUID>();

            List<ISceneChildEntity> children = new List<ISceneChildEntity>();
            ISceneChildEntity root = m_parentScene.GetSceneObjectPart(parentPrimId);

            if (root == null)
            {
                MainConsole.Instance.DebugFormat("[LINK]: Can't find linkset root prim {0}", parentPrimId);
                return;
            }

            if (!m_parentScene.Permissions.CanLinkObject(client.AgentId, root.ParentEntity.UUID))
            {
                MainConsole.Instance.DebugFormat("[LINK]: Refusing link. No permissions on root prim");
                return;
            }

            foreach (uint localID in childPrimIds)
            {
                ISceneChildEntity part = m_parentScene.GetSceneObjectPart(localID);

                if (part == null)
                    continue;

                if (!owners.Contains(part.OwnerID))
                    owners.Add(part.OwnerID);

                if (m_parentScene.Permissions.CanLinkObject(client.AgentId, part.ParentEntity.UUID))
                    children.Add(part);
            }

            // Must be all one owner
            //
            if (owners.Count > 1)
            {
                MainConsole.Instance.DebugFormat("[LINK]: Refusing link. Too many owners");
                client.SendAlertMessage("Permissions: Cannot link, too many owners.");
                return;
            }

            if (children.Count == 0)
            {
                MainConsole.Instance.DebugFormat("[LINK]: Refusing link. No permissions to link any of the children");
                client.SendAlertMessage("Permissions: Cannot link, not enough permissions.");
                return;
            }
            int LinkCount = children.Cast<SceneObjectPart>().Sum(part => part.ParentGroup.ChildrenList.Count);

            IOpenRegionSettingsModule module = m_parentScene.RequestModuleInterface<IOpenRegionSettingsModule>();
            if (module != null)
            {
                if (LinkCount > module.MaximumLinkCount &&
                    module.MaximumLinkCount != -1)
                {
                    client.SendAlertMessage("You cannot link more than " + module.MaximumLinkCount +
                                            " prims. Please try again with fewer prims.");
                    return;
                }
                if ((root.Flags & PrimFlags.Physics) == PrimFlags.Physics)
                {
                    //We only check the root here because if the root is physical, it will be applied to all during the link
                    if (LinkCount > module.MaximumLinkCountPhys &&
                        module.MaximumLinkCountPhys != -1)
                    {
                        client.SendAlertMessage("You cannot link more than " + module.MaximumLinkCountPhys +
                                                " physical prims. Please try again with fewer prims.");
                        return;
                    }
                }
            }

            LinkObjects(root, children);
        }

        /// <summary>
        ///     Initial method invoked when we receive a link objects request from the client.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="children"></param>
        protected internal void LinkObjects(ISceneChildEntity root, List<ISceneChildEntity> children)
        {
            Monitor.Enter(m_updateLock);
            try
            {
                ISceneEntity parentGroup = root.ParentEntity;

                List<ISceneEntity> childGroups = new List<ISceneEntity>();
                if (parentGroup != null)
                {
                    // We do this in reverse to get the link order of the prims correct
                    for (int i = children.Count - 1; i >= 0; i--)
                    {
                        ISceneEntity child = children[i].ParentEntity;

                        if (child != null)
                        {
                            // Make sure no child prim is set for sale
                            // So that, on delink, no prims are unwittingly
                            // left for sale and sold off
                            child.RootChild.ObjectSaleType = 0;
                            child.RootChild.SalePrice = 10;
                            childGroups.Add(child);
                        }
                    }
                }
                else
                {
                    return; // parent is null so not in this region
                }

                foreach (ISceneEntity child in childGroups)
                {
                    parentGroup.LinkToGroup(child);

                    // this is here so physics gets updated!
                    // Don't remove!  Bad juju!  Stay away! or fix physics!
                    child.AbsolutePosition = child.AbsolutePosition;
                }

                // We need to explicitly resend the newly link prim's object properties since no other actions
                // occur on link to invoke this elsewhere (such as object selection)
                parentGroup.RootChild.CreateSelected = true;
                parentGroup.HasGroupChanged = true;
                //parentGroup.RootPart.SendFullUpdateToAllClients(PrimUpdateFlags.FullUpdate);
                //parentGroup.ScheduleGroupForFullUpdate(PrimUpdateFlags.FullUpdate);
                parentGroup.ScheduleGroupUpdate(PrimUpdateFlags.ForcedFullUpdate);
                parentGroup.TriggerScriptChangedEvent(Changed.LINK);
            }
            finally
            {
                Monitor.Exit(m_updateLock);
            }
        }

        /// <summary>
        ///     Delink a linkset
        /// </summary>
        /// <param name="prims"></param>
        protected internal void DelinkObjects(List<ISceneChildEntity> prims)
        {
            Monitor.Enter(m_updateLock);
            try
            {
                List<ISceneChildEntity> childParts = new List<ISceneChildEntity>();
                List<ISceneChildEntity> rootParts = new List<ISceneChildEntity>();
                List<ISceneEntity> affectedGroups = new List<ISceneEntity>();
                // Look them all up in one go, since that is comparatively expensive
                //
                foreach (ISceneChildEntity part in prims)
                {
                    if (part != null)
                    {
                        if (part.ParentEntity.PrimCount != 1) // Skip single
                        {
                            if (part.LinkNum < 2) // Root
                                rootParts.Add(part);
                            else
                                childParts.Add(part);

                            ISceneEntity group = part.ParentEntity;
                            if (!affectedGroups.Contains(group))
                                affectedGroups.Add(group);
                        }
                    }
                }

                foreach (ISceneChildEntity child in childParts)
                {
                    // Unlink all child parts from their groups
                    //
                    child.ParentEntity.DelinkFromGroup(child, true);

                    // These are not in affected groups and will not be
                    // handled further. Do the honors here.
                    child.ParentEntity.HasGroupChanged = true;
                    if (!affectedGroups.Contains(child.ParentEntity))
                        affectedGroups.Add(child.ParentEntity);
                }

                foreach (ISceneChildEntity root in rootParts)
                {
                    // In most cases, this will run only one time, and the prim
                    // will be a solo prim
                    // However, editing linked parts and unlinking may be different
                    //
                    ISceneEntity group = root.ParentEntity;
                    List<ISceneChildEntity> newSet = new List<ISceneChildEntity>(group.ChildrenEntities());
                    int numChildren = group.PrimCount;

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

                        foreach (ISceneChildEntity p in newSet)
                        {
                            if (p != group.RootChild)
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
                            newSet.Sort(LinkSetSorter);

                            // Determine new root
                            //
                            ISceneChildEntity newRoot = newSet[0];
                            newSet.RemoveAt(0);

                            LinkObjects(newRoot, newSet);
                            if (!affectedGroups.Contains(newRoot.ParentEntity))
                                affectedGroups.Add(newRoot.ParentEntity);
                        }
                    }
                }

                // Finally, trigger events in the roots
                //
                foreach (ISceneEntity g in affectedGroups)
                {
                    g.TriggerScriptChangedEvent(Changed.LINK);
                    g.HasGroupChanged = true; // Persist
                    g.ScheduleGroupUpdate(PrimUpdateFlags.ForcedFullUpdate);
                }
                //Fix undo states now that the linksets have been changed
                foreach (ISceneChildEntity part in prims)
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
        ///     Sorts a list of Parts by Link Number so they end up in the correct order
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public int LinkSetSorter(ISceneChildEntity a, ISceneChildEntity b)
        {
            return a.LinkNum.CompareTo(b.LinkNum);
        }

        #endregion

        #endregion

        #region New Scene Entity Manager Code

        public bool LinkPartToSOG(ISceneEntity grp, ISceneChildEntity part, int linkNum)
        {
            part.SetParentLocalId(grp.RootChild.LocalId);
            part.SetParent(grp);
            // Insert in terms of link numbers, the new links
            // before the current ones (with the exception of 
            // the root prim. Shuffle the old ones up
            foreach (ISceneChildEntity otherPart in grp.ChildrenEntities())
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

        /// <summary>
        ///     Dupliate the entity and add it to the Scene
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public ISceneEntity DuplicateEntity(ISceneEntity entity)
        {
            //Make an exact copy of the entity
            ISceneEntity copiedEntity = entity.Copy(false);
            //Add the entity to the scene and back it up
            //Reset the entity IDs
            ResetEntityIDs(copiedEntity);

            //Force the prim to backup now that it has been added
            copiedEntity.ForcePersistence();
            //Tell the entity that they are being added to a scene
            copiedEntity.AttachToScene(m_parentScene);
            //Now save the entity that we have 
            AddEntity(copiedEntity, false);
            //Fix physics representation now
//            entity.RebuildPhysicalRepresentation();
            return copiedEntity;
        }

        /// <summary>
        ///     Add the new part to the group in the EntityManager
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="part"></param>
        /// <returns></returns>
        public bool LinkPartToEntity(ISceneEntity entity, ISceneChildEntity part)
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
        ///     Delinks the object from the group in the EntityManager
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="part"></param>
        /// <returns></returns>
        public bool DeLinkPartFromEntity(ISceneEntity entity, ISceneChildEntity part)
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
        ///     THIS IS TO ONLY BE CALLED WHEN AN OBJECT UUID IS UPDATED!!!
        ///     This method is HIGHLY unsafe and destroys the integrity of the checks above!
        ///     This is NOT to be used lightly! Do NOT use this unless you have to!
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="newID">new UUID to set the root part to</param>
        public void UpdateEntity(ISceneEntity entity, UUID newID)
        {
            RemoveEntity(entity);
            //Set it to the root so that we don't create an infinite loop as the ONLY place this should be being called is from the setter in SceneObjectGroup.UUID
            entity.RootChild.UUID = newID;
            AddEntity(entity, false);
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Try to get an EntityBase as given by its UUID
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool TryGetEntity(UUID ID, out IEntity entity)
        {
            return Entities.TryGetValue(ID, out entity);
        }

        /// <summary>
        ///     Try to get an EntityBase as given by it's LocalID
        /// </summary>
        /// <param name="LocalID"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool TryGetEntity(uint LocalID, out IEntity entity)
        {
            return Entities.TryGetValue(LocalID, out entity);
        }

        /// <summary>
        ///     Get a part (SceneObjectPart) from the EntityManager by LocalID
        /// </summary>
        /// <param name="LocalID"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool TryGetPart(uint LocalID, out ISceneChildEntity entity)
        {
            return Entities.TryGetChildPrim(LocalID, out entity);
        }

        /// <summary>
        ///     Get a part (SceneObjectPart) from the EntityManager by UUID
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool TryGetPart(UUID ID, out ISceneChildEntity entity)
        {
            IEntity ent;
            if (Entities.TryGetValue(ID, out ent))
            {
                if (ent is ISceneEntity)
                {
                    ISceneEntity parent = (ISceneEntity) ent;
                    return parent.GetChildPrim(ID, out entity);
                }
            }

            entity = null;
            return false;
        }

        /// <summary>
        ///     Get this prim ready to add to the scene
        /// </summary>
        /// <param name="entity"></param>
        public void PrepPrimForAdditionToScene(ISceneEntity entity)
        {
            ResetEntityIDs(entity);
        }

        /// <summary>
        ///     Add the Entity to the Scene and back it up
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool AddPrimToScene(ISceneEntity entity)
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
        ///     Add the Entity to the Scene and back it up, but do NOT reset its ID's
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        public bool RestorePrimToScene(ISceneEntity entity, bool force)
        {
            List<ISceneChildEntity> children = entity.ChildrenEntities();
            //Sort so that we rebuild in the same order and the root being first
            children.Sort(LinkSetSorter);

            entity.ClearChildren();

            foreach (ISceneChildEntity child in children)
            {
                if (child.LocalId == 0)
                    child.LocalId = AllocateLocalId();
                if (((SceneObjectPart) child).PhysActor != null)
                {
                    ((SceneObjectPart) child).PhysActor.LocalID = child.LocalId;
                    ((SceneObjectPart) child).PhysActor.UUID = child.UUID;
                }
                child.Flags &= ~PrimFlags.Scripted;
                child.TrimPermissions();
                entity.AddChild(child, child.LinkNum);
            }
            //Tell the entity that they are being added to a scene
            entity.AttachToScene(m_parentScene);
            //Now save the entity that we have 
            bool success = AddEntity(entity, false);

            if (force && !success)
            {
                IBackupModule backup = m_parentScene.RequestModuleInterface<IBackupModule>();
                backup.DeleteSceneObjects(new ISceneEntity[1] {entity}, false, true);
                return RestorePrimToScene(entity, false);
            }
            return success;
        }

        /// <summary>
        ///     Move this group from inside of another group into the Scene as a full member
        ///     This does not reset IDs so that it is updated correctly in the client
        /// </summary>
        /// <param name="entity"></param>
        public void DelinkPartToScene(ISceneEntity entity)
        {
            //Force the prim to backup now that it has been added
            entity.ForcePersistence();
            //Tell the entity that they are being added to a scene
            entity.RebuildPhysicalRepresentation(true);
            //Now save the entity that we have 
            AddEntity(entity, false);
        }

        /// <summary>
        ///     Destroy the entity and remove it from the scene
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool DeleteEntity(IEntity entity)
        {
            return RemoveEntity(entity);
        }

        #endregion

        #region Private Methods

        /// These methods are UNSAFE to be accessed from outside this manager, if they are, BAD things WILL happen.
        /// If these are changed so that they can be accessed from the outside, ghost prims and other nasty things will occur unless you are EXTREMELY careful.
        /// If more changes need to occur in this area, you must use public methods to safely add/update/remove objects from the EntityManager
        /// <summary>
        ///     Remove this entity fully from the EntityManager
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private bool RemoveEntity(IEntity entity)
        {
            return Entities.Remove(entity);
        }

        /// <summary>
        ///     Add this entity to the EntityManager
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="AllowUpdate"></param>
        /// <returns></returns>
        private bool AddEntity(IEntity entity, bool AllowUpdate)
        {
            return Entities.Add(entity);
        }

        /// <summary>
        ///     Reset all of the UUID's, localID's, etc in this group (includes children)
        /// </summary>
        /// <param name="entity"></param>
        private void ResetEntityIDs(ISceneEntity entity)
        {
            List<ISceneChildEntity> children = entity.ChildrenEntities();
            //Sort so that we rebuild in the same order and the root being first
            children.Sort(LinkSetSorter);

            entity.ClearChildren();
            foreach (ISceneChildEntity child in children)
            {
                UUID oldID = child.UUID;
                child.ResetEntityIDs();
                entity.AddChild(child, child.LinkNum);
            }
            //This clears the xml file, which will need rebuilt now that we have changed the UUIDs
            entity.HasGroupChanged = true;
            foreach (ISceneChildEntity child in children)
            {
                if (!child.IsRoot)
                {
                    child.SetParentLocalId(entity.RootChild.LocalId);
                }
            }
        }

        /// <summary>
        ///     Returns a new unallocated local ID
        /// </summary>
        /// <returns>A brand new local ID</returns>
        public uint AllocateLocalId()
        {
            lock (_primAllocateLock)
                return ++m_lastAllocatedLocalId;
        }

        /// <summary>
        ///     Check all the localIDs in this group to make sure that they have not been used previously
        /// </summary>
        /// <param name="group"></param>
        public void CheckAllocationOfLocalIds(ISceneEntity group)
        {
            foreach (ISceneChildEntity part in group.ChildrenEntities())
            {
                if (part.LocalId != 0)
                    CheckAllocationOfLocalId(part.LocalId);
            }
        }

        /// <summary>
        ///     Make sure that this localID has not been used earlier in the Scene Startup
        /// </summary>
        /// <param name="LocalID"></param>
        private void CheckAllocationOfLocalId(uint LocalID)
        {
            lock (_primAllocateLock)
            {
                if (LocalID > m_lastAllocatedLocalId)
                    m_lastAllocatedLocalId = LocalID + 1;
            }
        }

        #endregion
    }
}