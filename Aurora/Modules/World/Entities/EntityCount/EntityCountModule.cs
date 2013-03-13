/*
 * Copyright (c) Contributors, http://aurora-sim.org/
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
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using GridRegion = Aurora.Framework.GridRegion;

namespace Aurora.Modules.Entities.EntityCount
{
    public class EntityCountModule : INonSharedRegionModule, IEntityCountModule
    {
        #region Declares

        private readonly Dictionary<UUID, bool> m_lastAddedPhysicalStatus = new Dictionary<UUID, bool>();

        private readonly object m_objectsLock = new object();
        private int m_activeObjects;
        private int m_childAgents;
        private int m_objects;
        private int m_rootAgents;

        #endregion

        #region IEntityCountModule Members

        public int RootAgents
        {
            get { return m_rootAgents; }
        }

        public int ChildAgents
        {
            get { return m_childAgents; }
        }

        public int Objects
        {
            get { return m_objects; }
        }

        public int ActiveObjects
        {
            get { return m_activeObjects; }
        }

        #endregion

        #region INonSharedRegionModule Members

        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion(IScene scene)
        {
            scene.RegisterModuleInterface<IEntityCountModule>(this);

            scene.EventManager.OnMakeChildAgent += OnMakeChildAgent;
            scene.EventManager.OnMakeRootAgent += OnMakeRootAgent;
            scene.EventManager.OnNewPresence += OnNewPresence;
            scene.EventManager.OnRemovePresence += OnRemovePresence;

            scene.EventManager.OnObjectBeingAddedToScene += OnObjectBeingAddedToScene;
            scene.EventManager.OnObjectBeingRemovedFromScene += OnObjectBeingRemovedFromScene;

            scene.AuroraEventManager.RegisterEventHandler("ObjectChangedPhysicalStatus", OnGenericEvent);
        }

        public void RegionLoaded(IScene scene)
        {
        }

        public void RemoveRegion(IScene scene)
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "EntityCountModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        #region Events

        #region Agents

        protected void OnMakeChildAgent(IScenePresence presence, GridRegion destination)
        {
            //Switch child agent to root agent
            m_rootAgents--;
            m_childAgents++;
        }

        protected void OnMakeRootAgent(IScenePresence presence)
        {
            m_rootAgents++;
            m_childAgents--;
        }

        protected void OnNewPresence(IScenePresence presence)
        {
            // Why don't we check for root agents? We don't because it will be added in MakeRootAgent and removed from here
            m_childAgents++;
        }

        private void OnRemovePresence(IScenePresence presence)
        {
            if (presence.IsChildAgent)
                m_childAgents--;
            else
                m_rootAgents--;
        }

        #endregion

        #region Objects

        protected void OnObjectBeingAddedToScene(ISceneEntity obj)
        {
            lock (m_objectsLock)
            {
                foreach (ISceneChildEntity child in obj.ChildrenEntities())
                {
                    bool physicalStatus = (child.Flags & PrimFlags.Physics) == PrimFlags.Physics;
                    if (!m_lastAddedPhysicalStatus.ContainsKey(child.UUID))
                    {
                        m_objects++;

                        //Check physical status now
                        if (physicalStatus)
                            m_activeObjects++;
                        //Add it to the list so that we have a record of it
                        m_lastAddedPhysicalStatus.Add(child.UUID, physicalStatus);
                    }
                    else
                    {
                        //Its a dupe! Its a dupe!
                        //  Check that the physical status has changed
                        if (physicalStatus != m_lastAddedPhysicalStatus[child.UUID])
                        {
                            //It changed... fix the count
                            if (physicalStatus)
                                m_activeObjects++;
                            else
                                m_activeObjects--;
                            //Update the cache
                            m_lastAddedPhysicalStatus[child.UUID] = physicalStatus;
                        }
                    }
                }
            }
        }

        protected void OnObjectBeingRemovedFromScene(ISceneEntity obj)
        {
            lock (m_objectsLock)
            {
                foreach (ISceneChildEntity child in obj.ChildrenEntities())
                {
                    bool physicalStatus = (child.Flags & PrimFlags.Physics) == PrimFlags.Physics;
                    if (m_lastAddedPhysicalStatus.ContainsKey(child.UUID))
                    {
                        m_objects--;

                        //Check physical status now and remove if necessary
                        if (physicalStatus)
                            m_activeObjects--;
                        //Remove our record of it
                        m_lastAddedPhysicalStatus.Remove(child.UUID);
                    }
                }
            }
        }

        protected object OnGenericEvent(string FunctionName, object parameters)
        {
            //If the object changes physical status, we need to make sure to update the active objects count
            if (FunctionName == "ObjectChangedPhysicalStatus")
            {
                OnObjectBeingAddedToScene((ISceneEntity) parameters);
            }
            return null;
        }

        #endregion

        #endregion
    }
}