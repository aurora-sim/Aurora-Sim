using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using Nini.Config;
using OpenMetaverse;
using System.Timers;

namespace Aurora.Modules
{
    public class PhysicsStateModule : INonSharedRegionModule, IPhysicsStateModule
    {
        public class WorldPhysicsState
        {
            public class PhysicsState
            {
                public Vector3 Position;
                public Vector3 LinearVelocity;
                public Vector3 AngularVelocity;
                public Quaternion Rotation;
            }
            private Dictionary<UUID, PhysicsState> m_activePrims = new Dictionary<UUID, PhysicsState> ();

            public void AddPrim (PhysicsObject prm)
            {
                PhysicsState state = new PhysicsState ();
                state.Position = prm.Position;
                state.AngularVelocity = prm.RotationalVelocity;
                state.LinearVelocity = prm.Velocity;
                state.Rotation = prm.Orientation;
                m_activePrims[prm.UUID] = state;
            }

            public void Reload (IScene scene, float direction)
            {
                foreach (KeyValuePair<UUID, PhysicsState> kvp in m_activePrims)
                {
                    ISceneChildEntity childPrim = scene.GetSceneObjectPart (kvp.Key);
                    if (childPrim != null && childPrim.PhysActor != null)
                        ResetPrim (childPrim.PhysActor, kvp.Value, direction);
                }
            }

            private void ResetPrim (PhysicsObject physicsObject, PhysicsState physicsState, float direction)
            {
                physicsObject.Position = physicsState.Position;
                physicsObject.Orientation = physicsState.Rotation;
                physicsObject.RotationalVelocity = physicsState.AngularVelocity * direction;
                physicsObject.Velocity = physicsState.LinearVelocity * direction;
                physicsObject.ForceSetVelocity (physicsState.LinearVelocity * direction);
                physicsObject.RequestPhysicsterseUpdate ();
            }
        }

        private WorldPhysicsState m_lastWorldPhysicsState = null;
        private IScene m_scene;
        private int m_lastRevertedTo = -100;
        private bool m_isReversing = false;
        private bool m_isSavingRevertStates = false;
        private List<WorldPhysicsState> m_timeReversal = new List<WorldPhysicsState> ();

        public void Initialise (IConfigSource source)
        {
        }

        public void AddRegion (IScene scene)
        {
            scene.RegisterModuleInterface<IPhysicsStateModule> (this);
            m_scene = scene;
            Timer timeReversal = new Timer (250);
            timeReversal.Elapsed += new ElapsedEventHandler (timeReversal_Elapsed);
            timeReversal.Start ();
        }

        public void RegionLoaded (IScene scene)
        {
        }

        public void RemoveRegion (IScene scene)
        {
        }

        public void Close ()
        {
        }

        public string Name
        {
            get { return "PhysicsState"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void SavePhysicsState ()
        {
            if (m_isReversing)
                m_lastWorldPhysicsState = null;
            else
                m_lastWorldPhysicsState = MakePhysicsState ();
        }

        private WorldPhysicsState MakePhysicsState ()
        {
            WorldPhysicsState state = new WorldPhysicsState ();
            //Add all active objects in the scene
            foreach (PhysicsObject prm in m_scene.PhysicsScene.ActiveObjects)
            {
                state.AddPrim (prm);
            }
            return state;
        }

        public void ResetToLastSavedState ()
        {
            if(m_lastWorldPhysicsState != null)
                m_lastWorldPhysicsState.Reload (m_scene, 1);
        }

        void timeReversal_Elapsed (object sender, ElapsedEventArgs e)
        {
            if (!m_isSavingRevertStates)
                return;//Only save if we are running this
            if(!m_isReversing)//Only save new states if we are going forward
                m_timeReversal.Add (MakePhysicsState ());
            else
            {
                if (m_lastRevertedTo == -100)
                    m_lastRevertedTo = m_timeReversal.Count - 1;
                m_timeReversal[m_lastRevertedTo].Reload (m_scene, -1f);//Do the velocity in reverse with -1
                m_lastRevertedTo--;
                if (m_lastRevertedTo < 0)
                {
                    m_isSavingRevertStates = false;
                    m_lastRevertedTo = -100;
                    m_isReversing = false;
                    m_scene.StopPhysicsScene ();//Stop physics from moving too
                    m_scene.RegionInfo.RegionSettings.DisablePhysics = true;
                }
            }
        }

        public void StartSavingPhysicsTimeReversalStates ()
        {
            m_isSavingRevertStates = true;
        }

        public void StopSavingPhysicsTimeReversalStates ()
        {
            m_isSavingRevertStates = false;
            m_timeReversal.Clear ();
        }

        public void StartPhysicsTimeReversal ()
        {
            m_lastRevertedTo = -100;
            m_isReversing = true;
            m_scene.RegionInfo.RegionSettings.DisablePhysics = true;
        }

        public void StopPhysicsTimeReversal ()
        {
            m_lastRevertedTo = -100;
            m_scene.RegionInfo.RegionSettings.DisablePhysics = false;
            m_isReversing = false;
        }
    }
}
