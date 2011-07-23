using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using Nini.Config;
using OpenMetaverse;

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

            public void Reload (IScene scene)
            {
                foreach (KeyValuePair<UUID, PhysicsState> kvp in m_activePrims)
                {
                    ISceneChildEntity childPrim = scene.GetSceneObjectPart (kvp.Key);
                    if (childPrim != null && childPrim.PhysActor != null)
                        ResetPrim (childPrim.PhysActor, kvp.Value);
                }
            }

            private void ResetPrim (PhysicsObject physicsObject, PhysicsState physicsState)
            {
                physicsObject.Position = physicsState.Position;
                physicsObject.Orientation = physicsState.Rotation;
                physicsObject.RotationalVelocity = physicsState.AngularVelocity;
                physicsObject.Velocity = physicsState.LinearVelocity;
            }
        }

        private WorldPhysicsState m_lastWorldPhysicsState = null;
        private IScene m_scene;

        public void Initialise (IConfigSource source)
        {
        }

        public void AddRegion (IScene scene)
        {
            scene.RegisterModuleInterface<IPhysicsStateModule> (this);
            m_scene = scene;
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
            m_lastWorldPhysicsState = new WorldPhysicsState ();
            //Add all active objects in the scene
            foreach (PhysicsObject prm in m_scene.PhysicsScene.ActiveObjects)
            {
                m_lastWorldPhysicsState.AddPrim (prm);
            }
        }

        public void ResetToLastSavedState ()
        {
            if(m_lastWorldPhysicsState != null)
                m_lastWorldPhysicsState.Reload (m_scene);
        }
    }
}
