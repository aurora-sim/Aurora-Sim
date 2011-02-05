using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace Aurora.Modules
{
    public class EntityCountModule : INonSharedRegionModule, IEntityCountModule
    {
        #region Declares

        private int m_rootAgents = 0;
        private int m_childAgents = 0;
        private int m_objects = 0;
        private int m_activeObjects = 0;

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

        #region IRegionModuleBase Members

        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion(Scene scene)
        {
            scene.RegisterModuleInterface<IEntityCountModule>(this);

            scene.EventManager.OnMakeChildAgent += OnMakeChildAgent;
            scene.EventManager.OnMakeRootAgent += OnMakeRootAgent;
            scene.EventManager.OnNewPresence += OnNewPresence;
            scene.EventManager.OnRemovePresence += OnRemovePresence;

            scene.EventManager.OnObjectBeingAddedToScene += OnObjectBeingAddedToScene;
            scene.EventManager.OnObjectBeingRemovedFromScene += OnObjectBeingRemovedFromScene;

            scene.AuroraEventManager.OnGenericEvent += OnGenericEvent;
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
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

        protected void OnMakeChildAgent(ScenePresence presence)
        {
            //Switch child agent to root agent
            m_rootAgents--;
            m_childAgents++;
        }

        protected void OnMakeRootAgent(ScenePresence presence)
        {
            //The root agents was already added via OnNewPresence so do not repeat
        }

        protected void OnNewPresence(ScenePresence presence)
        {
            if (presence.IsChildAgent)
                m_childAgents++;
            else
                m_rootAgents++;
        }

        void OnRemovePresence(ScenePresence presence)
        {
            if (presence.IsChildAgent)
                m_childAgents--;
            else
                m_rootAgents--;
        }

        #endregion

        protected void OnObjectBeingAddedToScene(SceneObjectGroup obj)
        {
        }

        protected void OnObjectBeingRemovedFromScene(SceneObjectGroup obj)
        {
        }

        protected void OnGenericEvent(string FunctionName, object parameters)
        {
            if (FunctionName == "ObjectChangedPhysicalStatus")
            {
            }
        }

        #endregion
    }
}
