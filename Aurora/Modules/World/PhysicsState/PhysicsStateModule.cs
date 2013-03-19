using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Aurora.Framework.Modules;
using Aurora.Framework.Physics;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.Modules.PhysicsState
{
    public class PhysicsStateModule : INonSharedRegionModule, IPhysicsStateModule
    {
        private readonly List<WorldPhysicsState> m_timeReversal = new List<WorldPhysicsState>();
        private bool m_isReversing;
        private bool m_isSavingRevertStates;
        private int m_lastRevertedTo = -100;
        private WorldPhysicsState m_lastWorldPhysicsState;
        private IScene m_scene;

        #region INonSharedRegionModule Members

        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion(IScene scene)
        {
            scene.RegisterModuleInterface<IPhysicsStateModule>(this);
            m_scene = scene;
            Timer timeReversal = new Timer(250);
            timeReversal.Elapsed += timeReversal_Elapsed;
            timeReversal.Start();
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
            get { return "PhysicsState"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        #region IPhysicsStateModule Members

        public void SavePhysicsState()
        {
            m_lastWorldPhysicsState = m_isReversing ? null : MakePhysicsState();
        }

        public void ResetToLastSavedState()
        {
            if (m_lastWorldPhysicsState != null)
                m_lastWorldPhysicsState.Reload(m_scene, 1);
            m_lastWorldPhysicsState = null;
        }

        public void StartSavingPhysicsTimeReversalStates()
        {
            m_isSavingRevertStates = true;
        }

        public void StopSavingPhysicsTimeReversalStates()
        {
            m_isSavingRevertStates = false;
            m_timeReversal.Clear();
        }

        public void StartPhysicsTimeReversal()
        {
            m_lastRevertedTo = -100;
            m_isReversing = true;
            m_scene.RegionInfo.RegionSettings.DisablePhysics = true;
        }

        public void StopPhysicsTimeReversal()
        {
            m_lastRevertedTo = -100;
            m_scene.RegionInfo.RegionSettings.DisablePhysics = false;
            m_isReversing = false;
        }

        #endregion

        private WorldPhysicsState MakePhysicsState()
        {
            WorldPhysicsState state = new WorldPhysicsState();
            //Add all active objects in the scene
            foreach (PhysicsObject prm in m_scene.PhysicsScene.ActiveObjects)
            {
                state.AddPrim(prm);
            }
#if (!ISWIN)
            foreach (IScenePresence sp in m_scene.GetScenePresences())
            {
                if (!sp.IsChildAgent)
                {
                    state.AddAvatar(sp.PhysicsActor);
                }
            }
#else
            foreach (IScenePresence sp in m_scene.GetScenePresences().Where(sp => !sp.IsChildAgent))
            {
                state.AddAvatar(sp.PhysicsActor);
            }
#endif
            return state;
        }

        private void timeReversal_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!m_isSavingRevertStates)
                return; //Only save if we are running this
            if (!m_isReversing) //Only save new states if we are going forward
                m_timeReversal.Add(MakePhysicsState());
            else
            {
                if (m_lastRevertedTo == -100)
                    m_lastRevertedTo = m_timeReversal.Count - 1;
                m_timeReversal[m_lastRevertedTo].Reload(m_scene, -1f); //Do the velocity in reverse with -1
                m_lastRevertedTo--;
                if (m_lastRevertedTo < 0)
                {
                    m_isSavingRevertStates = false;
                    m_lastRevertedTo = -100;
                    m_isReversing = false;
                    m_scene.StopPhysicsScene(); //Stop physics from moving too
                    m_scene.RegionInfo.RegionSettings.DisablePhysics = true; //Freeze the scene
                    m_timeReversal.Clear(); //Remove the states we have as well, we've played them
                }
            }
        }

        #region Nested type: WorldPhysicsState

        public class WorldPhysicsState
        {
            private readonly Dictionary<UUID, PhysicsState> m_activePrims = new Dictionary<UUID, PhysicsState>();

            public void AddPrim(PhysicsObject prm)
            {
                PhysicsState state = new PhysicsState
                                         {
                                             Position = prm.Position,
                                             AngularVelocity = prm.RotationalVelocity,
                                             LinearVelocity = prm.Velocity,
                                             Rotation = prm.Orientation
                                         };
                m_activePrims[prm.UUID] = state;
            }

            public void AddAvatar(PhysicsCharacter prm)
            {
                PhysicsState state = new PhysicsState
                                         {
                                             Position = prm.Position,
                                             AngularVelocity = prm.RotationalVelocity,
                                             LinearVelocity = prm.Velocity,
                                             Rotation = prm.Orientation
                                         };
                m_activePrims[prm.UUID] = state;
            }

            public void Reload(IScene scene, float direction)
            {
                foreach (KeyValuePair<UUID, PhysicsState> kvp in m_activePrims)
                {
                    ISceneChildEntity childPrim = scene.GetSceneObjectPart(kvp.Key);
                    if (childPrim != null && childPrim.PhysActor != null)
                        ResetPrim(childPrim.PhysActor, kvp.Value, direction);
                    else
                    {
                        IScenePresence sp = scene.GetScenePresence(kvp.Key);
                        if (sp != null)
                            ResetAvatar(sp.PhysicsActor, kvp.Value, direction);
                    }
                }
            }

            private void ResetPrim(PhysicsObject physicsObject, PhysicsState physicsState, float direction)
            {
                physicsObject.Position = physicsState.Position;
                physicsObject.Orientation = physicsState.Rotation;
                physicsObject.RotationalVelocity = physicsState.AngularVelocity*direction;
                physicsObject.Velocity = physicsState.LinearVelocity*direction;
                physicsObject.ForceSetVelocity(physicsState.LinearVelocity*direction);
                physicsObject.RequestPhysicsterseUpdate();
            }

            private void ResetAvatar(PhysicsCharacter physicsObject, PhysicsState physicsState, float direction)
            {
                physicsObject.Position = physicsState.Position;
                physicsObject.ForceSetPosition(physicsState.Position);
                physicsObject.Orientation = physicsState.Rotation;
                physicsObject.RotationalVelocity = physicsState.AngularVelocity*direction;
                physicsObject.Velocity = physicsState.LinearVelocity*direction;
                physicsObject.ForceSetVelocity(physicsState.LinearVelocity*direction);
                physicsObject.RequestPhysicsterseUpdate();
            }

            #region Nested type: PhysicsState

            public class PhysicsState
            {
                public Vector3 AngularVelocity;
                public Vector3 LinearVelocity;
                public Vector3 Position;
                public Quaternion Rotation;
            }

            #endregion
        }

        #endregion
    }
}