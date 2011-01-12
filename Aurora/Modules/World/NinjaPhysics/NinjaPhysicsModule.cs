using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Physics.Manager;
using Aurora.Framework;

namespace Aurora.Modules
{
    public class NinjaPhysicsModule : INonSharedRegionModule, INinjaPhysicsModule
    {
        #region IRegionModuleBase Members

        protected Scene m_scene;

        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion(Scene scene)
        {
            if (scene.PhysicsScene != null && scene.PhysicsScene.SupportsNINJAJoints)
            {
                m_scene = scene;
                scene.RegisterModuleInterface<INinjaPhysicsModule>(this);
                // register event handlers to respond to joint movement/deactivation
                scene.PhysicsScene.OnJointMoved += jointMoved;
                scene.PhysicsScene.OnJointDeactivated += jointDeactivated;
                scene.PhysicsScene.OnJointErrorMessage += jointErrorMessage;
            }
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
            if (scene.PhysicsScene != null && scene.PhysicsScene.SupportsNINJAJoints)
            {
                m_scene = null;
                scene.UnregisterModuleInterface<INinjaPhysicsModule>(this);
                // register event handlers to respond to joint movement/deactivation
                scene.PhysicsScene.OnJointMoved -= jointMoved;
                scene.PhysicsScene.OnJointDeactivated -= jointDeactivated;
                scene.PhysicsScene.OnJointErrorMessage -= jointErrorMessage;
            }
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "NinjaPhysicsModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        #region Joints

        // This callback allows the PhysicsScene to call back to its caller (the SceneGraph) and
        // update non-physical objects like the joint proxy objects that represent the position
        // of the joints in the scene.

        // This routine is normally called from within a lock (OdeLock) from within the OdePhysicsScene
        // WARNING: be careful of deadlocks here if you manipulate the scene. Remember you are being called
        // from within the OdePhysicsScene.

        protected internal void jointMoved(PhysicsJoint joint)
        {
            // m_parentScene.PhysicsScene.DumpJointInfo(); // non-thread-locked version; we should already be in a lock (OdeLock) when this callback is invoked
            SceneObjectPart jointProxyObject = m_scene.SceneGraph.GetSceneObjectPart(joint.ObjectNameInScene);
            if (jointProxyObject == null)
            {
                jointErrorMessage(joint, "WARNING, joint proxy not found, name " + joint.ObjectNameInScene);
                return;
            }

            // now update the joint proxy object in the scene to have the position of the joint as returned by the physics engine
            SceneObjectPart trackedBody = m_scene.SceneGraph.GetSceneObjectPart(joint.TrackedBodyName); // FIXME: causes a sequential lookup
            if (trackedBody == null) return; // the actor may have been deleted but the joint still lingers around a few frames waiting for deletion. during this time, trackedBody is NULL to prevent further motion of the joint proxy.
            jointProxyObject.Velocity = trackedBody.Velocity;
            jointProxyObject.AngularVelocity = trackedBody.AngularVelocity;
            switch (joint.Type)
            {
                case PhysicsJointType.Ball:
                    {
                        Vector3 jointAnchor = m_scene.PhysicsScene.GetJointAnchor(joint);
                        Vector3 proxyPos = new Vector3(jointAnchor.X, jointAnchor.Y, jointAnchor.Z);
                        jointProxyObject.ParentGroup.UpdateGroupPosition(proxyPos, true); // schedules the entire group for a terse update
                    }
                    break;

                case PhysicsJointType.Hinge:
                    {
                        Vector3 jointAnchor = m_scene.PhysicsScene.GetJointAnchor(joint);

                        // Normally, we would just ask the physics scene to return the axis for the joint.
                        // Unfortunately, ODE sometimes returns <0,0,0> for the joint axis, which should
                        // never occur. Therefore we cannot rely on ODE to always return a correct joint axis.
                        // Therefore the following call does not always work:
                        //PhysicsVector phyJointAxis = _PhyScene.GetJointAxis(joint);

                        // instead we compute the joint orientation by saving the original joint orientation
                        // relative to one of the jointed bodies, and applying this transformation
                        // to the current position of the jointed bodies (the tracked body) to compute the
                        // current joint orientation.

                        if (joint.TrackedBodyName == null)
                        {
                            jointErrorMessage(joint, "joint.TrackedBodyName is null, joint " + joint.ObjectNameInScene);
                        }

                        Vector3 proxyPos = new Vector3(jointAnchor.X, jointAnchor.Y, jointAnchor.Z);
                        Quaternion q = trackedBody.RotationOffset * joint.LocalRotation;

                        jointProxyObject.ParentGroup.UpdateGroupPosition(proxyPos, true); // schedules the entire group for a terse update
                        jointProxyObject.ParentGroup.UpdateGroupRotationR(q); // schedules the entire group for a terse update
                    }
                    break;
            }
        }

        public void jointCreate(SceneObjectPart part)
        {
            // by turning a joint proxy object physical, we cause creation of a joint in the ODE scene.
            // note that, as a special case, joints have no bodies or geoms in the physics scene, even though they are physical.

            PhysicsJointType jointType;
            if (part.IsHingeJoint())
            {
                jointType = PhysicsJointType.Hinge;
            }
            else if (part.IsBallJoint())
            {
                jointType = PhysicsJointType.Ball;
            }
            else
            {
                jointType = PhysicsJointType.Ball;
            }

            List<string> bodyNames = new List<string>();
            string RawParams = part.Description;
            string[] jointParams = RawParams.Split(" ".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries);
            string trackedBodyName = null;
            if (jointParams.Length >= 2)
            {
                for (int iBodyName = 0; iBodyName < 2; iBodyName++)
                {
                    string bodyName = jointParams[iBodyName];
                    bodyNames.Add(bodyName);
                    if (bodyName != "NULL")
                    {
                        if (trackedBodyName == null)
                        {
                            trackedBodyName = bodyName;
                        }
                    }
                }
            }

            SceneObjectPart trackedBody = m_scene.SceneGraph.GetSceneObjectPart(trackedBodyName); // FIXME: causes a sequential lookup
            Quaternion localRotation = Quaternion.Identity;
            if (trackedBody != null)
            {
                localRotation = Quaternion.Inverse(trackedBody.RotationOffset) * part.RotationOffset;
            }
            else
            {
                // error, output it below
            }

            PhysicsJoint joint;

            joint = m_scene.PhysicsScene.RequestJointCreation(part.Name, jointType,
                part.AbsolutePosition,
                part.RotationOffset,
                part.Description,
                bodyNames,
                trackedBodyName,
                localRotation);

            if (trackedBody == null)
            {
                jointErrorMessage(joint, "warning: tracked body name not found! joint location will not be updated properly. joint: " + part.Name);
            }
        }

        // This callback allows the PhysicsScene to call back to its caller (the SceneGraph) and
        // update non-physical objects like the joint proxy objects that represent the position
        // of the joints in the scene.

        // This routine is normally called from within a lock (OdeLock) from within the OdePhysicsScene
        // WARNING: be careful of deadlocks here if you manipulate the scene. Remember you are being called
        // from within the OdePhysicsScene.
        protected internal void jointDeactivated(PhysicsJoint joint)
        {
            //m_log.Debug("[NINJA] SceneGraph.jointDeactivated, joint:" + joint.ObjectNameInScene);
            SceneObjectPart jointProxyObject = m_scene.SceneGraph.GetSceneObjectPart(joint.ObjectNameInScene);
            if (jointProxyObject == null)
            {
                jointErrorMessage(joint, "WARNING, trying to deactivate (stop interpolation of) joint proxy, but not found, name " + joint.ObjectNameInScene);
                return;
            }

            // turn the proxy non-physical, which also stops its client-side interpolation
            bool wasUsingPhysics = ((jointProxyObject.Flags & PrimFlags.Physics) != 0);
            if (wasUsingPhysics)
            {
                jointProxyObject.UpdatePrimFlags(false, false, true, false); // FIXME: possible deadlock here; check to make sure all the scene alterations set into motion here won't deadlock
            }
        }

        // This callback allows the PhysicsScene to call back to its caller (the SceneGraph) and
        // alert the user of errors by using the debug channel in the same way that scripts alert
        // the user of compile errors.

        // This routine is normally called from within a lock (OdeLock) from within the OdePhysicsScene
        // WARNING: be careful of deadlocks here if you manipulate the scene. Remember you are being called
        // from within the OdePhysicsScene.
        public void jointErrorMessage(PhysicsJoint joint, string message)
        {
            if (joint != null)
            {
                if (joint.ErrorMessageCount > PhysicsJoint.maxErrorMessages)
                    return;

                SceneObjectPart jointProxyObject = m_scene.SceneGraph.GetSceneObjectPart(joint.ObjectNameInScene);
                if (jointProxyObject != null)
                {
                    IChatModule chatModule = m_scene.RequestModuleInterface<IChatModule>();
                    if (chatModule != null)
                        chatModule.SimChat("[NINJA]: " + message,
                        ChatTypeEnum.DebugChannel,
                        2147483647,
                        jointProxyObject.AbsolutePosition,
                        jointProxyObject.Name,
                        jointProxyObject.UUID,
                        false, m_scene);

                    joint.ErrorMessageCount++;

                    if (joint.ErrorMessageCount > PhysicsJoint.maxErrorMessages)
                    {
                        if (chatModule != null)
                            chatModule.SimChat("[NINJA]: Too many messages for this joint, suppressing further messages.",
                            ChatTypeEnum.DebugChannel,
                            2147483647,
                            jointProxyObject.AbsolutePosition,
                            jointProxyObject.Name,
                            jointProxyObject.UUID,
                            false, m_scene);
                    }
                }
                else
                {
                    // couldn't find the joint proxy object; the error message is silently suppressed
                }
            }
        }

        #endregion
    }
}
