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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.Serialization;
using Aurora.Framework;
using OpenMetaverse;
using OpenMetaverse.Packets;
using OpenMetaverse.StructuredData;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes.Serialization;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Region.Framework.Scenes
{
    [Flags]
    public enum scriptEvents : long
    {
        None = 0,
        attach = 1,
        collision = 16,
        collision_end = 32,
        collision_start = 64,
        control = 128,
        dataserver = 256,
        email = 512,
        http_response = 1024,
        land_collision = 2048,
        land_collision_end = 4096,
        land_collision_start = 8192,
        at_target = 16384,
        at_rot_target = 16777216,
        listen = 32768,
        money = 65536,
        moving_end = 131072,
        moving_start = 262144,
        not_at_rot_target = 524288,
        not_at_target = 1048576,
        remote_data = 8388608,
        run_time_permissions = 268435456,
        state_entry = 1073741824,
        state_exit = 2,
        timer = 4,
        touch = 8,
        touch_end = 536870912,
        touch_start = 2097152,
        object_rez = 4194304,
        changed = 2147483648,
        link_message = 4294967296,
        no_sensor = 8589934592,
        on_rez = 17179869184,
        sensor = 34359738368
    }

    internal struct scriptPosTarget
    {
        public uint handle;
        public Vector3 targetPos;
        public float tolerance;
    }

    internal struct scriptRotTarget
    {
        public uint handle;
        public Quaternion targetRot;
        public float tolerance;
    }

    /// <summary>
    ///   A scene object group is conceptually an object in the scene.  The object is constituted of SceneObjectParts
    ///   (often known as prims), one of which is considered the root part.
    /// </summary>
    public partial class SceneObjectGroup : ISceneObject
        //(ISceneObject implements ISceneEntity and IEntity)
    {
        private readonly List<uint> m_lastColliders = new List<uint>();
        private readonly Dictionary<uint, scriptRotTarget> m_rotTargets = new Dictionary<uint, scriptRotTarget>();
        private readonly Dictionary<uint, scriptPosTarget> m_targets = new Dictionary<uint, scriptPosTarget>();
        [XmlIgnore] private bool m_ValidgrpOOB; // control recalcutation
        [XmlIgnore] private float m_grpBSphereRadiusSQ; // the square of the radius of a sphere containing the oob
        [XmlIgnore] private Vector3 m_grpOOBoffset; // the position center of the bounding box relative to it's Position

        [XmlIgnore] private Vector3 m_grpOOBsize;
                                    // the size of a bounding box oriented as prim, is future will consider cutted prims, meshs etc

        public bool m_inTransit;

        public bool m_isLoaded;
        private UUID m_lastParcelUUID = UUID.Zero;
        private Vector3 m_lastSignificantPosition = Vector3.Zero;
        protected Dictionary<UUID, SceneObjectPart> m_parts = new Dictionary<UUID, SceneObjectPart>();
        //Same as m_parts, but this is used for fast linear operations
        protected List<SceneObjectPart> m_partsList = new List<SceneObjectPart>();
        //This is the lock for m_parts and m_partsList
        protected object m_partsLock = new object();
        protected ulong m_regionHandle;
        protected SceneObjectPart m_rootPart;
        private bool m_scriptListens_atRotTarget;
        private bool m_scriptListens_atTarget;
        private bool m_scriptListens_notAtRotTarget;
        private bool m_scriptListens_notAtTarget;

        #region Properties

        private List<ISceneChildEntity> m_LoopSoundSlavePrims = new List<ISceneChildEntity>();
        private List<ISceneChildEntity> m_PlaySoundSlavePrims = new List<ISceneChildEntity>();

        /// <summary>
        ///   Added because the Parcel code seems to use it
        ///   but not sure a object should have this
        ///   as what does it tell us? that some avatar has selected it (but not what Avatar/user)
        ///   think really there should be a list (or whatever) in each scenepresence
        ///   saying what prim(s) that user has selected.
        /// </summary>
        protected bool m_isSelected;

        protected Quaternion m_rotation = Quaternion.Identity;

        public SceneObjectPart[] Parts
        {
            get { return m_partsList.ToArray(); }
        }

        /// <value>
        ///   The parts of this scene object group.  You must lock this property before using it.
        /// </value>
        public List<SceneObjectPart> ChildrenList
        {
            get { return m_partsList; }
        }

        /// <value>
        ///   The root part of this scene object
        /// </value>
        public SceneObjectPart RootPart
        {
            get { return m_rootPart; }
        }

        public Color Color
        {
            get { return m_rootPart.Color; }
            set
            {
                if (m_rootPart != null && m_rootPart.Color != value)
                    m_rootPart.Color = value;
            }
        }

        public KeyframeAnimation m_KeyframeAnimation = null;
        public KeyframeAnimation KeyframeAnimation
        {
            get 
            {
                OSDMap map = (OSDMap)m_rootPart.GetComponentState("KeyframeAnimation");
                m_KeyframeAnimation = new KeyframeAnimation();
                m_KeyframeAnimation.FromOSD(map);
                return m_KeyframeAnimation;
            }
            set
            {
                m_KeyframeAnimation = value;
                if (m_KeyframeAnimation == null)
                    m_rootPart.RemoveComponentState("KeyframeAnimation");
                else
                    m_rootPart.SetComponentState("KeyframeAnimation", m_KeyframeAnimation.ToOSD());
            }
        }

        public string Text
        {
            get
            {
                string returnstr = m_rootPart.Text;
                if (returnstr.Length > 255)
                {
                    returnstr = returnstr.Substring(0, 255);
                }
                return returnstr;
            }
            set
            {
                if (m_rootPart != null && m_rootPart.Text != value)
                    m_rootPart.Text = value;
            }
        }

        public ISceneChildEntity PlaySoundMasterPrim { get; set; }

        public List<ISceneChildEntity> PlaySoundSlavePrims
        {
            get { return m_PlaySoundSlavePrims; }
            set { m_PlaySoundSlavePrims = value; }
        }

        public UUID RegionUUID
        {
            get
            {
                if (m_scene != null)
                {
                    return m_scene.RegionInfo.RegionID;
                }
                return UUID.Zero;
            }
        }

        /// <summary>
        ///   The name of an object grouping is always the same as its root part
        /// </summary>
        public override string Name
        {
            get
            {
                if (RootPart == null)
                    return String.Empty;
                return RootPart.Name;
            }
            set { RootPart.Name = value; }
        }

        /// <summary>
        ///   Number of prims in this group
        /// </summary>
        public int PrimCount
        {
            get { return m_parts.Count; }
        }

        public override Quaternion Rotation
        {
            get { return m_rotation; }
            set
            {
                HasGroupChanged = true;
                m_rotation = value;
            }
        }

        public Quaternion GroupRotation
        {
            get { return m_rootPart.RotationOffset; }
        }

        public UUID GroupID
        {
            get { return m_rootPart.GroupID; }
            set
            {
                HasGroupChanged = true;
                m_rootPart.GroupID = value;
            }
        }

        public List<ISceneChildEntity> ChildrenEntities()
        {
            return new List<SceneObjectPart>(m_partsList).Cast<ISceneChildEntity>().ToList();
        }

        public List<UUID> SitTargetAvatar
        {
            get
            {
                List<UUID> sittingAvatars = new List<UUID>();
                foreach (var prim in ChildrenEntities())
                    sittingAvatars.AddRange(prim.SitTargetAvatar);
                return sittingAvatars;
            }
        }

        /// <summary>
        ///   The absolute position of this scene object in the scene
        /// </summary>
        public override Vector3 AbsolutePosition
        {
            get { return m_rootPart.GroupPosition; }
            set { SetAbsolutePosition(true, value); }
        }

        public override uint LocalId
        {
            get { return m_rootPart.LocalId; }
            set { m_rootPart.LocalId = value; }
        }

        public override UUID UUID
        {
            get { return m_rootPart.UUID; }
            set { Scene.SceneGraph.UpdateEntity(this, value); }
        }

        public UUID OwnerID
        {
            get { return m_rootPart.OwnerID; }
            set
            {
                HasGroupChanged = true;
                m_rootPart.OwnerID = value;
            }
        }

        public float Damage
        {
            get { return m_rootPart.Damage; }
            set
            {
                HasGroupChanged = true;
                m_rootPart.Damage = value;
            }
        }

        public bool IsSelected
        {
            get { return m_isSelected; }
            set
            {
                m_isSelected = value;
                // Tell physics engine that group is selected
                if (m_rootPart.PhysActor != null)
                {
                    m_rootPart.PhysActor.Selected = value;
                    // Pass it on to the children.
#if (!ISWIN)
                    foreach (SceneObjectPart child in ChildrenList)
                    {
                        if (child.PhysActor != null)
                        {
                            child.PhysActor.Selected = value;
                        }
                    }
#else
                    foreach (SceneObjectPart child in ChildrenList.Where(child => child.PhysActor != null))
                    {
                        child.PhysActor.Selected = value;
                    }
#endif
                }
            }
        }

        public ISceneChildEntity LoopSoundMasterPrim { get; set; }

        public List<ISceneChildEntity> LoopSoundSlavePrims
        {
            get { return m_LoopSoundSlavePrims; }
            set { m_LoopSoundSlavePrims = value; }
        }

        public bool ContainsPart(UUID partID)
        {
            return m_parts.ContainsKey(partID);
        }

        /// <summary>
        ///   Check both the attachment property and the relevant properties of the underlying root part.
        /// </summary>
        /// This is necessary in some cases, particularly when a scene object has just crossed into a region and doesn't
        /// have the IsAttachment property yet checked.
        /// 
        /// FIXME: However, this should be fixed so that this property
        /// propertly reflects the underlying status.
        /// <returns></returns>
        public bool IsAttachmentCheckFull()
        {
            return (IsAttachment || (m_rootPart.Shape.PCode == 9 && m_rootPart.Shape.State != 0));
        }

        // The UUID for the Region this Object is in.

        #endregion

        #region Constructors

        /// <summary>
        ///   THIS IS ONLY FOR SERIALIZATION AND AS A BASE CONSTRUCTOR
        /// </summary>
        public SceneObjectGroup(IScene scene)
        {
            m_scene = scene;
            m_isLoaded = true;
        }

        /// <summary>
        ///   This constructor creates a SceneObjectGroup using a pre-existing SceneObjectPart.
        ///   The original SceneObjectPart will be used rather than a copy, preserving
        ///   its existing localID and UUID.
        /// </summary>
        public SceneObjectGroup(SceneObjectPart part, IScene scene) : this(scene)
        {
            SetRootPart(part);
            part.Scale = part.Shape.Scale; // temporary hack to update oobb
            m_ValidgrpOOB = false;
        }

        public SceneObjectGroup(SceneObjectPart part, IScene scene, bool AddToScene)
            : this(scene)
        {
            if (!AddToScene)
            {
                m_isLoaded = false;
                m_isDeleted = true;
            }
            SetRootPart(part);
            part.Scale = part.Shape.Scale; // temporary hack to update oobb
            m_ValidgrpOOB = false;
        }

        /// <summary>
        ///   Constructor.  This object is added to the scene later via AttachToScene()
        /// </summary>
        public SceneObjectGroup(UUID ownerID, Vector3 pos, Quaternion rot, PrimitiveBaseShape shape, string name,
                                IScene scene) : this(scene)
        {
            SceneObjectPart part = new SceneObjectPart(ownerID, shape, pos, rot, Vector3.Zero, name, scene);
            SetRootPart(part);

            //This has to be set, otherwise it will break things like rezzing objects in an area where crossing is disabled, but rez isn't
            m_lastSignificantPosition = pos;

            m_ValidgrpOOB = false;
        }

        public void SetFromItemID(UUID itemID, UUID assetID)
        {
            foreach (SceneObjectPart part in m_partsList)
            {
                part.FromUserInventoryItemID = itemID;
                part.FromUserInventoryAssetID = assetID;
            }
        }

        /// <summary>
        ///   Attach this object to a scene.  It will also now apply to agents.
        /// </summary>
        /// <param name = "scene"></param>
        public void AttachToScene(IScene scene)
        {
            m_scene = scene;

            if (m_rootPart.Shape == null)
            {
                MainConsole.Instance.Warn("[SceneObjectGroup]: Found null shape for prim " + UUID + ", creating default box shape");
                m_rootPart.Shape = new PrimitiveBaseShape();
            }

            IOpenRegionSettingsModule WSModule = Scene.RequestModuleInterface<IOpenRegionSettingsModule>();
            if (WSModule != null)
            {
                foreach (SceneObjectPart part in ChildrenList)
                {
                    //It's being rezzed, add it to the scene if it doesn't already have a rez date
                    if (part.Rezzed != Util.ToDateTime(Util.EnvironmentTickCount()))
                        part.Rezzed = DateTime.UtcNow;
                    if (part.Shape == null)
                        continue;

                    Vector3 scale = part.Shape.Scale;

                    if (WSModule.MinimumPrimScale != -1)
                    {
                        if (scale.X < WSModule.MinimumPrimScale)
                            scale.X = WSModule.MinimumPrimScale;
                        if (scale.Y < WSModule.MinimumPrimScale)
                            scale.Y = WSModule.MinimumPrimScale;
                        if (scale.Z < WSModule.MinimumPrimScale)
                            scale.Z = WSModule.MinimumPrimScale;
                    }

                    if (part.ParentGroup.RootPart.PhysActor != null && part.ParentGroup.RootPart.PhysActor.IsPhysical &&
                        WSModule.MaximumPhysPrimScale != -1)
                    {
                        if (scale.X > WSModule.MaximumPhysPrimScale)
                            scale.X = WSModule.MaximumPhysPrimScale;
                        if (scale.Y > WSModule.MaximumPhysPrimScale)
                            scale.Y = WSModule.MaximumPhysPrimScale;
                        if (scale.Z > WSModule.MaximumPhysPrimScale)
                            scale.Z = WSModule.MaximumPhysPrimScale;
                    }

                    if (WSModule.MaximumPrimScale != -1)
                    {
                        if (scale.X > WSModule.MaximumPrimScale)
                            scale.X = WSModule.MaximumPrimScale;
                        if (scale.Y > WSModule.MaximumPrimScale)
                            scale.Y = WSModule.MaximumPrimScale;
                        if (scale.Z > WSModule.MaximumPrimScale)
                            scale.Z = WSModule.MaximumPrimScale;
                    }

                    part.Scale = scale;
                }
            }

            //Trigger our event
            Scene.EventManager.TriggerObjectBeingAddedToScene(this);

            RebuildPhysicalRepresentation(false);

            m_ValidgrpOOB = false;
        }

        public Vector3 GroupScale()
        {
            if (m_partsList.Count == 1)
                return RootPart.Scale;

            Vector3 minScale = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 maxScale = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            Vector3 finalScale;

            foreach (SceneObjectPart part in m_partsList)
            {
                Vector3 partscale = part.Scale*0.5f;

                // not assuming root is at index 0
                if (part.ParentID == 0) // root is in local frame of reference, partscale.? are positive, no rotations
                {
                    // if root is always at index 0 this can be just assigns

                    if (partscale.X > maxScale.X)
                        maxScale.X = partscale.X;
                    if (partscale.Y > maxScale.Y)
                        maxScale.Y = partscale.Y;
                    if (partscale.Z > maxScale.Z)
                        maxScale.Z = partscale.Z;

                    partscale = -partscale;
                    if (partscale.X < minScale.X)
                        minScale.X = partscale.X;
                    if (partscale.Y < minScale.Y)
                        minScale.Y = partscale.Y;
                    if (partscale.Z < minScale.Z)
                        minScale.Z = partscale.Z;
                }

                else // prims are in their local frame of reference
                {
                    Vector3 partoffset = part.OffsetPosition;
                    Quaternion partrot = part.RotationOffset;

                    // bring into this frame

                    partscale *= partrot;
                    partoffset *= partrot;
                    partoffset += part.OffsetPosition;

                    // now just 2 vertices in a diagonal 
                    Vector3 deltam = partoffset - partscale;
                    Vector3 deltaM = partoffset + partscale;

                    if (deltaM.X > deltam.X) // right vertices order for extrem X
                    {
                        if (deltam.X < minScale.X)
                            minScale.X = deltam.X;
                        if (deltaM.X > maxScale.X)
                            maxScale.X = deltaM.X;
                    }
                    else // nopes inverse one
                    {
                        if (deltaM.X < minScale.X)
                            minScale.X = deltaM.X;
                        if (deltam.X > maxScale.X)
                            maxScale.X = deltam.X;
                    }

                    if (deltaM.Y > deltam.Y)
                    {
                        if (deltam.Y < minScale.Y)
                            minScale.Y = deltam.Y;
                        if (deltaM.Y > maxScale.Y)
                            maxScale.Y = deltaM.Y;
                    }
                    else
                    {
                        if (deltaM.Y < minScale.Y)
                            minScale.Y = deltaM.Y;
                        if (deltam.Y > maxScale.Y)
                            maxScale.Y = deltam.Y;
                    }

                    if (deltaM.Z > deltam.Z)
                    {
                        if (deltam.Z < minScale.Z)
                            minScale.Z = deltam.Z;
                        if (deltaM.Z > maxScale.Z)
                            maxScale.Z = deltaM.Z;
                    }
                    else
                    {
                        if (deltaM.Z < minScale.Z)
                            minScale.Z = deltaM.Z;
                        if (deltam.Z > maxScale.Z)
                            maxScale.Z = deltam.Z;
                    }
                }
            }

            finalScale.X = Math.Abs(maxScale.X - minScale.X);
            finalScale.Y = Math.Abs(maxScale.Y - minScale.Y);
            finalScale.Z = Math.Abs(maxScale.Z - minScale.Z);
            return finalScale;
        }

        public override int GetHashCode()
        {
            return UUID.GetHashCode();
        }

        public bool IsPhysical()
        {
            return ((RootPart.Flags & PrimFlags.Physics) == PrimFlags.Physics);
        }

        public void UpdateOOBfromOOBs()
        {
            if (m_partsList.Count == 1)
            {
                SceneObjectPart part = m_partsList.First();
                m_grpOOBsize = part.OOBsize;
                m_grpOOBoffset = part.OOBoffset;
                m_grpBSphereRadiusSQ = part.BSphereRadiusSQ;
                m_ValidgrpOOB = true;
                return;
            }

            Vector3 minScale = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 maxScale = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (SceneObjectPart part in m_partsList)
            {
                Vector3 partscale = part.OOBsize; // (oobsize == vector with box vertice with all coords positive)
                Vector3 partoffset = part.OOBoffset;

                // not assuming root is at index 0
                Vector3 deltam;
                Vector3 deltaM;
                if (part.ParentID == 0) // root is in local frame of reference, partscale.? are positive, no rotations
                {
                    //2 vertices in the right extrem sides:
                    deltam = partoffset - partscale;
                    deltaM = partoffset + partscale;

                    // if root is always at index 0 this can be just assigns
                    if (deltam.X < minScale.X)
                        minScale.X = deltam.X;
                    if (deltam.Y < minScale.Y)
                        minScale.Y = deltam.Y;
                    if (deltam.Z < minScale.Z)
                        minScale.Z = deltam.Z;

                    if (deltaM.X > maxScale.X)
                        maxScale.X = deltaM.X;
                    if (deltaM.Y > maxScale.Y)
                        maxScale.Y = deltaM.Y;
                    if (deltaM.Z > maxScale.Z)
                        maxScale.Z = deltaM.Z;
                }

                else // prims are in their local frame of reference
                {
                    // bring into this frame
                    Quaternion partrot = part.RotationOffset;
                    partscale *= partrot;
                    partoffset *= partrot;
                    partoffset += part.OffsetPosition;

                    // now just 2 vertices in a diagonal 
                    deltam = partoffset - partscale;
                    deltaM = partoffset + partscale;

                    if (deltaM.X > deltam.X) // right vertices order for extrem X
                    {
                        if (deltam.X < minScale.X)
                            minScale.X = deltam.X;
                        if (deltaM.X > maxScale.X)
                            maxScale.X = deltaM.X;
                    }
                    else // nopes inverse one
                    {
                        if (deltaM.X < minScale.X)
                            minScale.X = deltaM.X;
                        if (deltam.X > maxScale.X)
                            maxScale.X = deltam.X;
                    }

                    if (deltaM.Y > deltam.Y)
                    {
                        if (deltam.Y < minScale.Y)
                            minScale.Y = deltam.Y;
                        if (deltaM.Y > maxScale.Y)
                            maxScale.Y = deltaM.Y;
                    }
                    else
                    {
                        if (deltaM.Y < minScale.Y)
                            minScale.Y = deltaM.Y;
                        if (deltam.Y > maxScale.Y)
                            maxScale.Y = deltam.Y;
                    }

                    if (deltaM.Z > deltam.Z)
                    {
                        if (deltam.Z < minScale.Z)
                            minScale.Z = deltam.Z;
                        if (deltaM.Z > maxScale.Z)
                            maxScale.Z = deltaM.Z;
                    }
                    else
                    {
                        if (deltaM.Z < minScale.Z)
                            minScale.Z = deltaM.Z;
                        if (deltam.Z > maxScale.Z)
                            maxScale.Z = deltam.Z;
                    }
                }
            }
            // size == the vertice of box with all coords positive
            m_grpOOBsize.X = 0.5f*Math.Abs(maxScale.X - minScale.X);
            m_grpOOBsize.Y = 0.5f*Math.Abs(maxScale.Y - minScale.Y);
            m_grpOOBsize.Z = 0.5f*Math.Abs(maxScale.Z - minScale.Z);
            // centroid:
            m_grpOOBoffset.X = 0.5f*(maxScale.X + minScale.X);
            m_grpOOBoffset.Y = 0.5f*(maxScale.Y + minScale.Y);
            m_grpOOBoffset.Z = 0.5f*(maxScale.Z + minScale.Z);
            // containing sphere:
            m_grpBSphereRadiusSQ = m_grpOOBsize.LengthSquared();

            m_ValidgrpOOB = true;
        }

        public EntityIntersection TestIntersection(Ray hRay, bool frontFacesOnly, bool faceCenters)
        {
            // We got a request from the inner_scene to raytrace along the Ray hRay
            // We're going to check all of the prim in this group for intersection with the ray
            // If we get a result, we're going to find the closest result to the origin of the ray
            // and send back the intersection information back to the innerscene.

            EntityIntersection result = new EntityIntersection();

            foreach (SceneObjectPart part in m_partsList)
            {
                // Temporary commented to stop compiler warning
                //Vector3 partPosition =
                //    new Vector3(part.AbsolutePosition.X, part.AbsolutePosition.Y, part.AbsolutePosition.Z);
                Quaternion parentrotation = GroupRotation;
                // Telling the prim to raytrace.
                //EntityIntersection inter = part.TestIntersection(hRay, parentrotation);
                EntityIntersection inter = part.TestIntersectionOBB(hRay, parentrotation, frontFacesOnly, faceCenters);

                // This may need to be updated to the maximum draw distance possible..
                // We might (and probably will) be checking for prim creation from other sims
                // when the camera crosses the border.
                if (m_scene != null)
                {
                    float idist = (m_scene.RegionInfo.RegionSizeX + m_scene.RegionInfo.RegionSizeY)/2;
                    if (inter.HitTF)
                    {
                        // We need to find the closest prim to return to the testcaller along the ray
                        if (inter.distance < idist)
                        {
                            result.HitTF = true;
                            result.ipoint = inter.ipoint;
                            result.obj = part;
                            result.normal = inter.normal;
                            result.distance = inter.distance;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///   Gets a vector representing the size of the bounding box containing all the prims in the group
        ///   Treats all prims as rectangular, so no shape (cut etc) is taken into account
        ///   offsetHeight is the offset in the Z axis from the centre of the bounding box to the centre of the root prim
        /// </summary>
        /// <returns></returns>
        public void GetAxisAlignedBoundingBoxRaw(out float minX, out float maxX, out float minY, out float maxY,
                                                 out float minZ, out float maxZ)
        {
            Vector3 pos = m_rootPart.AbsolutePosition;
            Quaternion rot = m_rootPart.RotationOffset;
//            Vector3 size = GroupScale();
            Vector3 minScale = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 maxScale = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            //limits in group frame
            foreach (SceneObjectPart part in m_partsList)
            {
                Vector3 partscale = part.Scale*0.5f;
                Vector3 partoffset = part.OffsetPosition;
                if (part.ParentID != 0) // prims are rotated in group
                {
                    partscale *= part.RotationOffset;
                    partscale.X = Math.Abs(partscale.X);
                    partscale.Y = Math.Abs(partscale.Y);
                    partscale.Z = Math.Abs(partscale.Z);
                }

                Vector3 deltam = partoffset - partscale;
                Vector3 deltaM = partoffset + partscale;

                if (deltam.X < minScale.X)
                    minScale.X = deltam.X;
                if (deltam.Y < minScale.Y)
                    minScale.Y = deltam.Y;
                if (deltam.Z < minScale.Z)
                    minScale.Z = deltam.Z;

                if (deltaM.X > maxScale.X)
                    maxScale.X = deltaM.X;
                if (deltaM.Y > maxScale.Y)
                    maxScale.Y = deltaM.Y;
                if (deltaM.Z > maxScale.Z)
                    maxScale.Z = deltaM.Z;
            }

            Vector3 tmp;
            tmp.X = 0.5f*Math.Abs(maxScale.X - minScale.X);
            tmp.Y = 0.5f*Math.Abs(maxScale.Y - minScale.Y);
            tmp.Z = 0.5f*Math.Abs(maxScale.Z - minScale.Z);
            // tmp has half scale

            // group rotation
            tmp = tmp*rot;
            // scale is positive
            tmp.X = Math.Abs(tmp.X);
            tmp.Y = Math.Abs(tmp.Y);
            tmp.Z = Math.Abs(tmp.Z);

            // group position
            minX = pos.X - tmp.X;
            minY = pos.Y - tmp.Y;
            minZ = pos.Z - tmp.Z;
            maxX = pos.X + tmp.X;
            maxY = pos.Y + tmp.Y;
            maxZ = pos.Z + tmp.Z;

/*
            maxX = -256f;
            maxY = -256f;
            maxZ = -256f;
            minX = 256f;
            minY = 256f;
            minZ = 8192f;

            foreach (SceneObjectPart part in m_partsList)
            {
                Vector3 worldPos = part.GetWorldPosition();
                Vector3 offset = worldPos - AbsolutePosition;
                Quaternion worldRot;
                if (part.ParentID == 0)
                    worldRot = part.RotationOffset;
                else
                    worldRot = part.GetWorldRotation();
                Vector3 frontTopLeft;
                Vector3 frontTopRight;
                Vector3 frontBottomLeft;
                Vector3 frontBottomRight;
                Vector3 backTopLeft;
                Vector3 backTopRight;
                Vector3 backBottomLeft;
                Vector3 backBottomRight;
                Vector3 orig = Vector3.Zero;

                frontTopLeft.X = orig.X - (part.Scale.X / 2);
                frontTopLeft.Y = orig.Y - (part.Scale.Y / 2);
                frontTopLeft.Z = orig.Z + (part.Scale.Z / 2);

                frontTopRight.X = orig.X - (part.Scale.X / 2);
                frontTopRight.Y = orig.Y + (part.Scale.Y / 2);
                frontTopRight.Z = orig.Z + (part.Scale.Z / 2);

                frontBottomLeft.X = orig.X - (part.Scale.X / 2);
                frontBottomLeft.Y = orig.Y - (part.Scale.Y / 2);
                frontBottomLeft.Z = orig.Z - (part.Scale.Z / 2);

                frontBottomRight.X = orig.X - (part.Scale.X / 2);
                frontBottomRight.Y = orig.Y + (part.Scale.Y / 2);
                frontBottomRight.Z = orig.Z - (part.Scale.Z / 2);

                backTopLeft.X = orig.X + (part.Scale.X / 2);
                backTopLeft.Y = orig.Y - (part.Scale.Y / 2);
                backTopLeft.Z = orig.Z + (part.Scale.Z / 2);

                backTopRight.X = orig.X + (part.Scale.X / 2);
                backTopRight.Y = orig.Y + (part.Scale.Y / 2);
                backTopRight.Z = orig.Z + (part.Scale.Z / 2);

                backBottomLeft.X = orig.X + (part.Scale.X / 2);
                backBottomLeft.Y = orig.Y - (part.Scale.Y / 2);
                backBottomLeft.Z = orig.Z - (part.Scale.Z / 2);

                backBottomRight.X = orig.X + (part.Scale.X / 2);
                backBottomRight.Y = orig.Y + (part.Scale.Y / 2);
                backBottomRight.Z = orig.Z - (part.Scale.Z / 2);

                frontTopLeft = frontTopLeft * worldRot;
                frontTopRight = frontTopRight * worldRot;
                frontBottomLeft = frontBottomLeft * worldRot;
                frontBottomRight = frontBottomRight * worldRot;

                backBottomLeft = backBottomLeft * worldRot;
                backBottomRight = backBottomRight * worldRot;
                backTopLeft = backTopLeft * worldRot;
                backTopRight = backTopRight * worldRot;

                frontTopLeft += offset;
                frontTopRight += offset;
                frontBottomLeft += offset;
                frontBottomRight += offset;

                backBottomLeft += offset;
                backBottomRight += offset;
                backTopLeft += offset;
                backTopRight += offset;

                if (frontTopRight.X > maxX)
                    maxX = frontTopRight.X;
                if (frontTopLeft.X > maxX)
                    maxX = frontTopLeft.X;
                if (frontBottomRight.X > maxX)
                    maxX = frontBottomRight.X;
                if (frontBottomLeft.X > maxX)
                    maxX = frontBottomLeft.X;

                if (backTopRight.X > maxX)
                    maxX = backTopRight.X;
                if (backTopLeft.X > maxX)
                    maxX = backTopLeft.X;
                if (backBottomRight.X > maxX)
                    maxX = backBottomRight.X;
                if (backBottomLeft.X > maxX)
                    maxX = backBottomLeft.X;

                if (frontTopRight.X < minX)
                    minX = frontTopRight.X;
                if (frontTopLeft.X < minX)
                    minX = frontTopLeft.X;
                if (frontBottomRight.X < minX)
                    minX = frontBottomRight.X;
                if (frontBottomLeft.X < minX)
                    minX = frontBottomLeft.X;

                if (backTopRight.X < minX)
                    minX = backTopRight.X;
                if (backTopLeft.X < minX)
                    minX = backTopLeft.X;
                if (backBottomRight.X < minX)
                    minX = backBottomRight.X;
                if (backBottomLeft.X < minX)
                    minX = backBottomLeft.X;

                if (frontTopRight.Y > maxY)
                    maxY = frontTopRight.Y;
                if (frontTopLeft.Y > maxY)
                    maxY = frontTopLeft.Y;
                if (frontBottomRight.Y > maxY)
                    maxY = frontBottomRight.Y;
                if (frontBottomLeft.Y > maxY)
                    maxY = frontBottomLeft.Y;

                if (backTopRight.Y > maxY)
                    maxY = backTopRight.Y;
                if (backTopLeft.Y > maxY)
                    maxY = backTopLeft.Y;
                if (backBottomRight.Y > maxY)
                    maxY = backBottomRight.Y;
                if (backBottomLeft.Y > maxY)
                    maxY = backBottomLeft.Y;

                if (backTopRight.Y < minY)
                    minY = backTopRight.Y;
                if (backTopLeft.Y < minY)
                    minY = backTopLeft.Y;
                if (backBottomRight.Y < minY)
                    minY = backBottomRight.Y;
                if (backBottomLeft.Y < minY)
                    minY = backBottomLeft.Y;

                if (backTopRight.Y < minY)
                    minY = backTopRight.Y;
                if (backTopLeft.Y < minY)
                    minY = backTopLeft.Y;
                if (backBottomRight.Y < minY)
                    minY = backBottomRight.Y;
                if (backBottomLeft.Y < minY)
                    minY = backBottomLeft.Y;

                if (frontTopRight.Z > maxZ)
                    maxZ = frontTopRight.Z;
                if (frontTopLeft.Z > maxZ)
                    maxZ = frontTopLeft.Z;
                if (frontBottomRight.Z > maxZ)
                    maxZ = frontBottomRight.Z;
                if (frontBottomLeft.Z > maxZ)
                    maxZ = frontBottomLeft.Z;

                if (backTopRight.Z > maxZ)
                    maxZ = backTopRight.Z;
                if (backTopLeft.Z > maxZ)
                    maxZ = backTopLeft.Z;
                if (backBottomRight.Z > maxZ)
                    maxZ = backBottomRight.Z;
                if (backBottomLeft.Z > maxZ)
                    maxZ = backBottomLeft.Z;

                if (frontTopRight.Z < minZ)
                    minZ = frontTopRight.Z;
                if (frontTopLeft.Z < minZ)
                    minZ = frontTopLeft.Z;
                if (frontBottomRight.Z < minZ)
                    minZ = frontBottomRight.Z;
                if (frontBottomLeft.Z < minZ)
                    minZ = frontBottomLeft.Z;

                if (backTopRight.Z < minZ)
                    minZ = backTopRight.Z;
                if (backTopLeft.Z < minZ)
                    minZ = backTopLeft.Z;
                if (backBottomRight.Z < minZ)
                    minZ = backBottomRight.Z;
                if (backBottomLeft.Z < minZ)
                    minZ = backBottomLeft.Z;
            }
 */
        }

        public Vector3 GetAxisAlignedBoundingBox(out float offsetHeight)
        {
            float minX;
            float maxX;
            float minY;
            float maxY;
            float minZ;
            float maxZ;

            GetAxisAlignedBoundingBoxRaw(out minX, out maxX, out minY, out maxY, out minZ, out maxZ);
            Vector3 boundingBox = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);


            offsetHeight = 0.5f*(maxZ + minZ);
            offsetHeight -= m_rootPart.AbsolutePosition.Z;

            /*
                        offsetHeight = 0;
                        float lower = (minZ * -1);
                        if (lower > maxZ)
                        {
                            offsetHeight = lower - (boundingBox.Z / 2);

                        }
                        else if (maxZ > lower)
                        {
                            offsetHeight = maxZ - (boundingBox.Z / 2);
                            offsetHeight *= -1;
                        }
            */
            // MainConsole.Instance.InfoFormat("BoundingBox is {0} , {1} , {2} ", boundingBox.X, boundingBox.Y, boundingBox.Z);
            return boundingBox;
        }

        #region Adding/Removing children from this group

        /// <summary>
        ///   Clear all children from this group
        /// </summary>
        public void ClearChildren()
        {
            lock (m_partsLock)
            {
                m_parts.Clear();
                m_partsList.Clear();
                m_ValidgrpOOB = false;
            }
        }

        /// <summary>
        ///   Add a child to the group, set the parent id's and then set the link number
        /// </summary>
        /// <param name = "child"></param>
        /// <param name="linkNum"></param>
        /// <returns></returns>
        public bool AddChild(ISceneChildEntity child, int linkNum)
        {
            lock (m_partsLock)
            {
                if (child is SceneObjectPart)
                {
                    SceneObjectPart part = (SceneObjectPart) child;
                    //Root part is first
                    if (m_partsList.Count == 0)
                    {
                        m_rootPart = part;
                    }
                    //Set the parent prim
                    part.SetParent(this);
                    if (m_rootPart.LocalId != 0 && !part.IsRoot)
                        part.SetParentLocalId(m_rootPart.LocalId);
                    else
                        part.SetParentLocalId(0);

                    //Fix the link num
                    part.LinkNum = linkNum;

                    if (!m_parts.ContainsKey(child.UUID))
                    {
                        m_parts.Add(child.UUID, part);
                        m_partsList.Add(part);
                        m_ValidgrpOOB = false;
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///   Add this child to the group and set the parent ID's,
        ///   but do NOT set the link number,
        ///   the caller wants to deal with it if they call this
        /// </summary>
        /// <param name = "child"></param>
        /// <returns></returns>
        public bool LinkChild(ISceneChildEntity child)
        {
            lock (m_partsLock)
            {
                if (child is SceneObjectPart)
                {
                    SceneObjectPart part = (SceneObjectPart) child;
                    //Root part is first
                    if (m_partsList.Count == 0)
                    {
                        m_rootPart = part;
                    }
                    //Set the parent prim
                    part.SetParent(this);
                    part.SetParentLocalId(m_rootPart.LocalId);

                    if (!m_parts.ContainsKey(child.UUID))
                    {
                        m_parts.Add(child.UUID, part);
                        m_partsList.Add(part);
                        m_ValidgrpOOB = false;
                    }
                    m_partsList.Sort(m_scene.SceneGraph.LinkSetSorter);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///   Remove this child from the group and then update the link numbers so that there is not a hole
        /// </summary>
        /// <param name = "child"></param>
        /// <returns></returns>
        public bool RemoveChild(ISceneChildEntity child)
        {
            lock (m_partsLock)
            {
                if (child is SceneObjectPart)
                {
                    SceneObjectPart part = (SceneObjectPart) child;
                    m_parts.Remove(part.UUID);
                    m_partsList.Remove(part);
                    m_ValidgrpOOB = false;

                    //Fix the link numbers now
                    FixLinkNumbers();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///   After a prim is removed, fix the link numbers so that they are correct
        /// </summary>
        private void FixLinkNumbers()
        {
            if (m_partsList.Count == 1)
            {
                m_partsList[0].LinkNum = 0;
                return;
            }

            lock (m_partsLock)
            {
                // has prims so starts at 1
                int lastSeenLinkNum = 1;
                m_partsList.Sort(Scene.SceneGraph.LinkSetSorter);
                foreach (SceneObjectPart t in m_partsList)
                {
//If it isn't the same as the last seen +1, fix it
                    if (t != null && t.LinkNum != lastSeenLinkNum)
                        t.LinkNum = lastSeenLinkNum;

                    //Go onto the next prim
                    lastSeenLinkNum++;
                }
            }
        }

        #endregion

        #endregion

        [XmlIgnore]
        public bool ValidgrpOOB
        {
            set { m_ValidgrpOOB = value; }
        }

        /// <summary>
        ///   The position center of the bounding box relative to it's Position
        /// </summary>
        [XmlIgnore]
        public Vector3 OOBoffset
        {
            get
            {
                if (!m_ValidgrpOOB)
                    UpdateOOBfromOOBs();
                return m_grpOOBoffset;
            }
        }

        public object ChildrenListLock
        {
            get { return m_partsLock; }
        }

        #region ISceneObject Members

        public event BlankHandler OnFinishedPhysicalRepresentationBuilding;

        public Vector3 LastSignificantPosition
        {
            get { return m_lastSignificantPosition; }
        }

        public UUID LastParcelUUID
        {
            get { return m_lastParcelUUID; }
            set { m_lastParcelUUID = value; }
        }

        /// <summary>
        ///   The size of a bounding box oriented as prim, is future will consider cutted prims, meshs etc
        /// </summary>
        [XmlIgnore]
        public Vector3 OOBsize
        {
            get
            {
                if (!m_ValidgrpOOB)
                    UpdateOOBfromOOBs();
                return m_grpOOBsize;
            }
        }

        /// <summary>
        ///   The square of the radius of a sphere containing the oob
        /// </summary>
        [XmlIgnore]
        public float BSphereRadiusSQ
        {
            get
            {
                if (!m_ValidgrpOOB)
                    UpdateOOBfromOOBs();
                return m_grpBSphereRadiusSQ;
            }
        }

        public override bool HasGroupChanged
        {
            get { return m_hasGroupChanged; }
            set
            {
                if (value)
                {
                    if (m_scene != null)
                    {
                        IBackupModule backup = m_scene.RequestModuleInterface<IBackupModule>();
                        if (backup != null)
                        {
                            if (m_isLoaded && !backup.LoadingPrims) //Do NOT add to backup while still loading prims
                                backup.AddPrimBackupTaint(this);
                        }
                    }
                }
                m_hasGroupChanged = value;
            }
        }

        /// <summary>
        ///   Force all prims in the scene object to persist
        /// </summary>
        public void ForcePersistence()
        {
            //Force normal backup
            HasGroupChanged = true;
            ForceInventoryPersistence();
        }

        /// <summary>
        ///   Clears all undo states from this group
        /// </summary>
        public void ClearUndoState()
        {
            foreach (SceneObjectPart child in ChildrenList)
            {
                child.ClearUndoState();
            }
        }

        ///<value>
        ///  Is this scene object acting as an attachment?
        /// 
        ///  We return false if the group has already been deleted.
        ///
        ///  TODO: At the moment set must be done on the part itself.  There may be a case for doing it here since I
        ///  presume either all or no parts in a linkset can be part of an attachment (in which
        ///  case the value would get proprogated down into all the descendent parts).
        ///</value>
        public bool IsAttachment
        {
            get { return m_rootPart.IsAttachment; }
        }

        //private bool m_isBackedUp = false;

        public byte GetAttachmentPoint()
        {
            return m_rootPart.Shape.State;
        }

        public Vector3 GetAttachmentPos()
        {
            return m_rootPart.SavedAttachedPos;
        }

        public byte GetSavedAttachmentPoint()
        {
            return (byte) m_rootPart.SavedAttachmentPoint;
        }

        public void DetachToGround()
        {
            IScenePresence avatar = m_scene.GetScenePresence(m_rootPart.AttachedAvatar);
            if (avatar == null)
                return;

            RootPart.FromUserInventoryItemID = UUID.Zero;

            AbsolutePosition = avatar.AbsolutePosition;
            m_rootPart.AttachedAvatar = UUID.Zero;
            //Anakin Lohner bug #3839 
            foreach (SceneObjectPart p in m_partsList)
            {
                p.AttachedAvatar = UUID.Zero;
            }

            m_rootPart.SetParentLocalId(0);
            SetAttachmentPoint(0);
            RebuildPhysicalRepresentation(false);
            HasGroupChanged = true;
            m_ValidgrpOOB = false;
            RootPart.Rezzed = DateTime.UtcNow;
            RootPart.RemFlag(PrimFlags.TemporaryOnRez);
            m_rootPart.ScheduleUpdate(PrimUpdateFlags.ForcedFullUpdate);
        }

        public void DetachToInventoryPrep()
        {
            m_rootPart.AttachedAvatar = UUID.Zero;
            //Anakin Lohner bug #3839 
            foreach (SceneObjectPart p in m_partsList)
            {
                p.AttachedAvatar = UUID.Zero;
            }

            m_rootPart.SetParentLocalId(0);
            //m_rootPart.SetAttachmentPoint((byte)0);
            m_rootPart.IsAttachment = false;
            AbsolutePosition = m_rootPart.AttachedPos;
            m_ValidgrpOOB = false;
            //m_rootPart.ApplyPhysics(m_rootPart.GetEffectiveObjectFlags(), m_scene.m_physicalPrim);
            //AttachToBackup();
            //m_rootPart.ScheduleFullUpdate();
        }

        // justincc: I don't believe this hack is needed any longer, especially since the physics
        // parts of set AbsolutePosition were already commented out.  By changing HasGroupChanged to false
        // this method was preventing proper reload of scene objects.

        // dahlia: I had to uncomment it, without it meshing was failing on some prims and objects
        // at region startup

        // teravus: After this was removed from the linking algorithm, Linked prims no longer collided 
        // properly when non-physical if they havn't been moved.   This breaks ALL builds.
        // see: http://opensimulator.org/mantis/view.php?id=3108

        // Here's the deal, this is ABSOLUTELY CRITICAL so the physics scene gets the update about the 
        // position of linkset prims.  IF YOU CHANGE THIS, YOU MUST TEST colliding with just linked and 
        // unmoved prims!  As soon as you move a Prim/group, it will collide properly because Absolute 
        // Position has been set!

        public void ResetChildPrimPhysicsPositions()
        {
            AbsolutePosition = AbsolutePosition; // could someone in the know please explain how this works?

            // teravus: AbsolutePosition is NOT a normal property!
            // the code in the getter of AbsolutePosition is significantly different then the code in the setter!
            // jhurliman: Then why is it a property instead of two methods?
        }

        public void SetOwnerId(UUID userId)
        {
            ForEachPart(delegate(SceneObjectPart part)
                            {
                                part.LastOwnerID = part.OwnerID;
                                part.OwnerID = userId;
                            });
        }

        public float GetMass()
        {
            return RootChild.GetMass();
        }

        /// <summary>
        ///   Set the user group to which this scene object belongs.
        /// </summary>
        /// <param name = "GroupID2"></param>
        /// <param name = "client"></param>
        public void SetGroup(UUID GroupID2, UUID attemptingUserID, bool needsUpdate)
        {
            IGroupsModule module = Scene.RequestModuleInterface<IGroupsModule>();
            if (module != null)
                if (!module.GroupPermissionCheck(attemptingUserID, GroupID2, GroupPowers.None))
                    return; // No settings to groups you arn't in
            foreach (SceneObjectPart part in m_partsList)
            {
                part.SetGroup(GroupID2);
                part.Inventory.ChangeInventoryGroup(GroupID2);
            }

            HasGroupChanged = true;
            IScenePresence sp = Scene.GetScenePresence(attemptingUserID);
            if (sp != null && needsUpdate)
                GetProperties(sp.ControllingClient);
        }

        public void TriggerScriptChangedEvent(Changed val)
        {
            foreach (SceneObjectPart part in ChildrenList)
            {
                part.TriggerScriptChangedEvent(val);
            }
        }

        public void SetAttachmentPoint(byte point)
        {
            foreach (SceneObjectPart part in m_partsList)
                part.SetAttachmentPoint(point);
        }

        public void FireAttachmentCollisionEvents(EventArgs e)
        {
            CollisionEventUpdate a = (CollisionEventUpdate) e;
            Dictionary<uint, ContactPoint> collissionswith = a.m_objCollisionList;
            List<uint> thisHitColliders = new List<uint>();
            List<uint> startedColliders = new List<uint>();

            // calculate things that started colliding this time
            // and build up list of colliders this time
            foreach (uint localid in collissionswith.Keys)
            {
                thisHitColliders.Add(localid);
                if (!m_lastColliders.Contains(localid))
                {
                    startedColliders.Add(localid);
                }
                //MainConsole.Instance.Debug("[OBJECT]: Collided with:" + localid.ToString() + " at depth of: " + collissionswith[localid].ToString());
            }

            // calculate things that ended colliding
#if (!ISWIN)
            List<uint> endedColliders = new List<uint>();
            foreach (uint localId in m_lastColliders)
            {
                if (!thisHitColliders.Contains(localId)) endedColliders.Add(localId);
            }
#else
            List<uint> endedColliders = m_lastColliders.Where(localID => !thisHitColliders.Contains(localID)).ToList();
#endif

            //add the items that started colliding this time to the last colliders list.
            foreach (uint localID in startedColliders)
            {
                m_lastColliders.Add(localID);
            }
            // remove things that ended colliding from the last colliders list
            foreach (uint localID in endedColliders)
            {
                m_lastColliders.Remove(localID);
            }

            if (IsDeleted)
                return;

            // play the sound.
            if (startedColliders.Count > 0 && RootPart.CollisionSound != UUID.Zero &&
                RootPart.CollisionSoundVolume > 0.0f)
            {
                RootPart.SendSound(RootPart.CollisionSound.ToString(), RootPart.CollisionSoundVolume, true, 0, 0, false,
                                   false);
            }
            if (RootPart.CollisionSprite != UUID.Zero && RootPart.CollisionSoundVolume > 0.0f)
                // The collision volume isn't a mistake, its an SL feature/bug
            {
                // TODO: make a sprite!
            }

            if ((RootPart.ScriptEvents & scriptEvents.collision_start) != 0)
            {
                // do event notification
                if (startedColliders.Count > 0)
                {
                    ColliderArgs StartCollidingMessage = new ColliderArgs();
                    List<DetectedObject> colliding = new List<DetectedObject>();
                    foreach (uint localId in startedColliders)
                    {
                        if (localId != 0)
                        {
                            if (Scene == null)
                                return;

                            ISceneChildEntity obj = Scene.GetSceneObjectPart(localId);
                            string data = "";
                            if (obj != null)
                            {
                                if (RootPart.CollisionFilter.ContainsValue(obj.UUID.ToString()) || RootPart.CollisionFilter.ContainsValue(obj.Name))
                                {
                                    bool found = RootPart.CollisionFilter.TryGetValue(1, out data);
                                    //If it is 1, it is to accept ONLY collisions from this object
                                    if (found)
                                    {
                                        DetectedObject detobj = new DetectedObject
                                                                    {
                                                                        keyUUID = obj.UUID, nameStr = obj.Name, ownerUUID = obj.OwnerID, posVector = obj.AbsolutePosition, rotQuat = obj.GetWorldRotation(), velVector = obj.Velocity, colliderType = 0, groupUUID = obj.GroupID
                                                                    };
                                        colliding.Add(detobj);
                                    }
                                    //If it is 0, it is to not accept collisions from this object
                                }
                                else
                                {
                                    bool found = RootPart.CollisionFilter.TryGetValue(1, out data);
                                    //If it is 1, it is to accept ONLY collisions from this object, so this other object will not work
                                    if (!found)
                                    {
                                        DetectedObject detobj = new DetectedObject
                                                                    {
                                                                        keyUUID = obj.UUID, nameStr = obj.Name, ownerUUID = obj.OwnerID, posVector = obj.AbsolutePosition, rotQuat = obj.GetWorldRotation(), velVector = obj.Velocity, colliderType = 0, groupUUID = obj.GroupID
                                                                    };
                                        colliding.Add(detobj);
                                    }
                                }
                            }
                            else
                            {
                                IScenePresence av = Scene.GetScenePresence(localId);
                                if (av.LocalId == localId)
                                {
                                    if (RootPart.CollisionFilter.ContainsValue(av.UUID.ToString()) || RootPart.CollisionFilter.ContainsValue(av.Name))
                                    {
                                        bool found = RootPart.CollisionFilter.TryGetValue(1, out data);
                                        //If it is 1, it is to accept ONLY collisions from this avatar
                                        if (found)
                                        {
                                            DetectedObject detobj = new DetectedObject
                                                                        {
                                                                            keyUUID = av.UUID, nameStr = av.ControllingClient.Name, ownerUUID = av.UUID, posVector = av.AbsolutePosition, rotQuat = av.Rotation, velVector = av.Velocity, colliderType = 0, groupUUID = av.ControllingClient.ActiveGroupId
                                                                        };
                                            colliding.Add(detobj);
                                        }
                                        //If it is 0, it is to not accept collisions from this avatar
                                    }
                                    else
                                    {
                                        bool found = RootPart.CollisionFilter.TryGetValue(1, out data);
                                        //If it is 1, it is to accept ONLY collisions from this avatar, so this other avatar will not work
                                        if (!found)
                                        {
                                            DetectedObject detobj = new DetectedObject
                                                                        {
                                                                            keyUUID = av.UUID, nameStr = av.ControllingClient.Name, ownerUUID = av.UUID, posVector = av.AbsolutePosition, rotQuat = av.Rotation, velVector = av.Velocity, colliderType = 0, groupUUID = av.ControllingClient.ActiveGroupId
                                                                        };
                                            colliding.Add(detobj);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (colliding.Count > 0)
                    {
                        StartCollidingMessage.Colliders = colliding;
                        // always running this check because if the user deletes the object it would return a null reference.

                        if (Scene == null)
                            return;
                        Scene.EventManager.TriggerScriptCollidingStart(RootPart, StartCollidingMessage);
                    }
                }
            }

            if ((RootPart.ScriptEvents & scriptEvents.collision) != 0)
            {
                if (m_lastColliders.Count > 0)
                {
                    ColliderArgs CollidingMessage = new ColliderArgs();
                    List<DetectedObject> colliding = new List<DetectedObject>();
                    foreach (uint localId in m_lastColliders)
                    {
                        if (localId != 0)
                        {
                            if (Scene == null)
                                return;

                            ISceneChildEntity obj = Scene.GetSceneObjectPart(localId);
                            string data = "";
                            if (obj != null)
                            {
                                if (RootPart.CollisionFilter.ContainsValue(obj.UUID.ToString()) || RootPart.CollisionFilter.ContainsValue(obj.Name))
                                {
                                    bool found = RootPart.CollisionFilter.TryGetValue(1, out data);
                                    //If it is 1, it is to accept ONLY collisions from this object
                                    if (found)
                                    {
                                        DetectedObject detobj = new DetectedObject
                                                                    {
                                                                        keyUUID = obj.UUID, nameStr = obj.Name, ownerUUID = obj.OwnerID, posVector = obj.AbsolutePosition, rotQuat = obj.GetWorldRotation(), velVector = obj.Velocity, colliderType = 0, groupUUID = obj.GroupID
                                                                    };
                                        colliding.Add(detobj);
                                    }
                                    //If it is 0, it is to not accept collisions from this object
                                }
                                else
                                {
                                    bool found = RootPart.CollisionFilter.TryGetValue(1, out data);
                                    //If it is 1, it is to accept ONLY collisions from this object, so this other object will not work
                                    if (!found)
                                    {
                                        DetectedObject detobj = new DetectedObject
                                                                    {
                                                                        keyUUID = obj.UUID, nameStr = obj.Name, ownerUUID = obj.OwnerID, posVector = obj.AbsolutePosition, rotQuat = obj.GetWorldRotation(), velVector = obj.Velocity, colliderType = 0, groupUUID = obj.GroupID
                                                                    };
                                        colliding.Add(detobj);
                                    }
                                }
                            }
                            else
                            {
                                IScenePresence av = Scene.GetScenePresence(localId);
                                if (av.LocalId == localId)
                                {
                                    if (RootPart.CollisionFilter.ContainsValue(av.UUID.ToString()) || RootPart.CollisionFilter.ContainsValue(av.Name))
                                    {
                                        bool found = RootPart.CollisionFilter.TryGetValue(1, out data);
                                        //If it is 1, it is to accept ONLY collisions from this avatar
                                        if (found)
                                        {
                                            DetectedObject detobj = new DetectedObject
                                                                        {
                                                                            keyUUID = av.UUID, nameStr = av.ControllingClient.Name, ownerUUID = av.UUID, posVector = av.AbsolutePosition, rotQuat = av.Rotation, velVector = av.Velocity, colliderType = 0, groupUUID = av.ControllingClient.ActiveGroupId
                                                                        };
                                            colliding.Add(detobj);
                                        }
                                        //If it is 0, it is to not accept collisions from this avatar
                                    }
                                    else
                                    {
                                        bool found = RootPart.CollisionFilter.TryGetValue(1, out data);
                                        //If it is 1, it is to accept ONLY collisions from this avatar, so this other avatar will not work
                                        if (!found)
                                        {
                                            DetectedObject detobj = new DetectedObject
                                                                        {
                                                                            keyUUID = av.UUID, nameStr = av.ControllingClient.Name, ownerUUID = av.UUID, posVector = av.AbsolutePosition, rotQuat = av.Rotation, velVector = av.Velocity, colliderType = 0, groupUUID = av.ControllingClient.ActiveGroupId
                                                                        };
                                            colliding.Add(detobj);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (colliding.Count > 0)
                    {
                        CollidingMessage.Colliders = colliding;

                        if (Scene == null)
                            return;

                        Scene.EventManager.TriggerScriptColliding(RootPart, CollidingMessage);
                    }
                }
            }

            if ((RootPart.ScriptEvents & scriptEvents.collision_end) != 0)
            {
                if (endedColliders.Count > 0)
                {
                    ColliderArgs EndCollidingMessage = new ColliderArgs();
                    List<DetectedObject> colliding = new List<DetectedObject>();
                    foreach (uint localId in endedColliders)
                    {
                        if (localId != 0)
                        {
                            // always running this check because if the user deletes the object it would return a null reference.
                            if (Scene == null)
                                return;
                            ISceneChildEntity obj = Scene.GetSceneObjectPart(localId);
                            string data = "";
                            if (obj != null)
                            {
                                if (RootPart.CollisionFilter.ContainsValue(obj.UUID.ToString()) || RootPart.CollisionFilter.ContainsValue(obj.Name))
                                {
                                    bool found = RootPart.CollisionFilter.TryGetValue(1, out data);
                                    //If it is 1, it is to accept ONLY collisions from this object
                                    if (found)
                                    {
                                        DetectedObject detobj = new DetectedObject
                                                                    {
                                                                        keyUUID = obj.UUID, nameStr = obj.Name, ownerUUID = obj.OwnerID, posVector = obj.AbsolutePosition, rotQuat = obj.GetWorldRotation(), velVector = obj.Velocity, colliderType = 0, groupUUID = obj.GroupID
                                                                    };
                                        colliding.Add(detobj);
                                    }
                                    //If it is 0, it is to not accept collisions from this object
                                }
                                else
                                {
                                    bool found = RootPart.CollisionFilter.TryGetValue(1, out data);
                                    //If it is 1, it is to accept ONLY collisions from this object, so this other object will not work
                                    if (!found)
                                    {
                                        DetectedObject detobj = new DetectedObject
                                                                    {
                                                                        keyUUID = obj.UUID, nameStr = obj.Name, ownerUUID = obj.OwnerID, posVector = obj.AbsolutePosition, rotQuat = obj.GetWorldRotation(), velVector = obj.Velocity, colliderType = 0, groupUUID = obj.GroupID
                                                                    };
                                        colliding.Add(detobj);
                                    }
                                }
                            }
                            else
                            {
                                IScenePresence av = Scene.GetScenePresence(localId);
                                if (av.LocalId == localId)
                                {
                                    if (RootPart.CollisionFilter.ContainsValue(av.UUID.ToString()) || RootPart.CollisionFilter.ContainsValue(av.Name))
                                    {
                                        bool found = RootPart.CollisionFilter.TryGetValue(1, out data);
                                        //If it is 1, it is to accept ONLY collisions from this avatar
                                        if (found)
                                        {
                                            DetectedObject detobj = new DetectedObject
                                                                        {
                                                                            keyUUID = av.UUID, nameStr = av.ControllingClient.Name, ownerUUID = av.UUID, posVector = av.AbsolutePosition, rotQuat = av.Rotation, velVector = av.Velocity, colliderType = 0, groupUUID = av.ControllingClient.ActiveGroupId
                                                                        };
                                            colliding.Add(detobj);
                                        }
                                        //If it is 0, it is to not accept collisions from this avatar
                                    }
                                    else
                                    {
                                        bool found = RootPart.CollisionFilter.TryGetValue(1, out data);
                                        //If it is 1, it is to accept ONLY collisions from this avatar, so this other avatar will not work
                                        if (!found)
                                        {
                                            DetectedObject detobj = new DetectedObject
                                                                        {
                                                                            keyUUID = av.UUID, nameStr = av.ControllingClient.Name, ownerUUID = av.UUID, posVector = av.AbsolutePosition, rotQuat = av.Rotation, velVector = av.Velocity, colliderType = 0, groupUUID = av.ControllingClient.ActiveGroupId
                                                                        };
                                            colliding.Add(detobj);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (colliding.Count > 0)
                    {
                        EndCollidingMessage.Colliders = colliding;
                        if (Scene == null)
                            return;

                        Scene.EventManager.TriggerScriptCollidingEnd(RootPart, EndCollidingMessage);
                    }
                }
            }
            if ((RootPart.ScriptEvents & scriptEvents.land_collision_start) != 0)
            {
                if (startedColliders.Count > 0)
                {
                    ColliderArgs LandStartCollidingMessage = new ColliderArgs();
                    List<DetectedObject> colliding = (from localId in startedColliders
                                                      where localId == 0
                                                      select new DetectedObject
                                                                 {
                                                                     keyUUID = UUID.Zero, nameStr = "", ownerUUID = UUID.Zero, posVector = RootPart.AbsolutePosition, rotQuat = Quaternion.Identity, velVector = Vector3.Zero, colliderType = 0, groupUUID = UUID.Zero
                                                                 }).ToList();

                    if (colliding.Count > 0)
                    {
                        LandStartCollidingMessage.Colliders = colliding;
                        if (Scene == null)
                            return;

                        Scene.EventManager.TriggerScriptLandCollidingStart(RootPart, LandStartCollidingMessage);
                    }
                }
            }
            if ((RootPart.ScriptEvents & scriptEvents.land_collision) != 0)
            {
                if (m_lastColliders.Count > 0)
                {
                    ColliderArgs LandCollidingMessage = new ColliderArgs();
                    List<DetectedObject> colliding = (from localId in startedColliders
                                                      where localId == 0
                                                      select new DetectedObject
                                                                 {
                                                                     keyUUID = UUID.Zero, nameStr = "", ownerUUID = UUID.Zero, posVector = RootPart.AbsolutePosition, rotQuat = Quaternion.Identity, velVector = Vector3.Zero, colliderType = 0, groupUUID = UUID.Zero
                                                                 }).ToList();

                    if (colliding.Count > 0)
                    {
                        LandCollidingMessage.Colliders = colliding;
                        if (Scene == null)
                            return;

                        Scene.EventManager.TriggerScriptLandColliding(RootPart, LandCollidingMessage);
                    }
                }
            }
            if ((RootPart.ScriptEvents & scriptEvents.land_collision_end) != 0)
            {
                if (endedColliders.Count > 0)
                {
                    ColliderArgs LandEndCollidingMessage = new ColliderArgs();
                    List<DetectedObject> colliding = (from localId in startedColliders
                                                      where localId == 0
                                                      select new DetectedObject
                                                                 {
                                                                     keyUUID = UUID.Zero, nameStr = "", ownerUUID = UUID.Zero, posVector = RootPart.AbsolutePosition, rotQuat = Quaternion.Identity, velVector = Vector3.Zero, colliderType = 0, groupUUID = UUID.Zero
                                                                 }).ToList();

                    if (colliding.Count > 0)
                    {
                        LandEndCollidingMessage.Colliders = colliding;
                        // always running this check because if the user deletes the object it would return a null reference.

                        if (Scene == null)
                            return;

                        Scene.EventManager.TriggerScriptLandCollidingEnd(RootPart, LandEndCollidingMessage);
                    }
                }
            }
        }

        public ISceneChildEntity RootChild
        {
            get { return m_rootPart; }
            set { m_rootPart = (SceneObjectPart) value; }
        }

        #endregion

        #region ISceneObject

        public virtual string ToXml2()
        {
            return SceneObjectSerializer.ToXml2Format(this);
        }

        public virtual byte[] ToBinaryXml2()
        {
            return SceneObjectSerializer.ToBinaryXml2Format(this);
        }

        public virtual string ExtraToXmlString()
        {
            return "<ExtraFromItemID>" + RootChild.FromUserInventoryItemID + "</ExtraFromItemID>" +
                   "<ExtraFromAssetID>" + RootChild.FromUserInventoryAssetID + "</ExtraFromAssetID>";
        }

        public virtual void ExtraFromXmlString(string xmlstr)
        {
            if (xmlstr == "")
                return;
            string id = xmlstr.Substring(xmlstr.IndexOf("<ExtraFromItemID>"));
            id = xmlstr.Replace("<ExtraFromItemID>", "");
            id = id.Replace("</ExtraFromItemID>", "");
            string assetid = xmlstr.Substring(xmlstr.IndexOf("<ExtraFromAssetID>"));
            assetid = xmlstr.Replace("<ExtraFromAssetID>", "");
            assetid = id.Replace("</ExtraFromAssetID>", "");

            UUID uuid = UUID.Zero;
            UUID.TryParse(id, out uuid);
            UUID assetuuid = UUID.Zero;
            UUID.TryParse(assetid, out assetuuid);

            SetFromItemID(uuid, assetuuid);
        }

        #endregion

        public void ClearPartAttachmentData()
        {
            SetAttachmentPoint(0);
        }

        /// <summary>
        ///   Set a part to act as the root part for this scene object
        /// </summary>
        /// <param name = "part"></param>
        public void SetRootPart(SceneObjectPart part)
        {
            if (part == null)
                throw new ArgumentNullException("part");

            m_rootPart = part;
            if (!IsAttachment)
                part.SetParentLocalId(0);
            AddChild(part, part.LinkNum);
        }

        public void ObjectGrabHandler(uint localId, Vector3 offsetPos, IClientAPI remoteClient)
        {
            if (m_rootPart.LocalId == localId)
            {
                OnGrabGroup(offsetPos, remoteClient);
            }
            else
            {
                SceneObjectPart part = (SceneObjectPart) GetChildPart(localId);
                OnGrabPart(part, offsetPos, remoteClient);
            }
        }

        public virtual void OnGrabPart(SceneObjectPart part, Vector3 offsetPos, IClientAPI remoteClient)
        {
            part.StoreUndoState();
            part.OnGrab(offsetPos, remoteClient);
        }

        public virtual void OnGrabGroup(Vector3 offsetPos, IClientAPI remoteClient)
        {
            m_scene.EventManager.TriggerGroupGrab(UUID, offsetPos, remoteClient.AgentId);
        }

        public void aggregateScriptEvents()
        {
            PrimFlags objectflagupdate = (PrimFlags) RootPart.GetEffectiveObjectFlags();

            scriptEvents aggregateScriptEvents = 0;

#if (!ISWIN)
            foreach (SceneObjectPart part in m_partsList)
            {
                if (part != null)
                {
                    if (part != RootPart)
                        part.Flags = objectflagupdate;
                    aggregateScriptEvents |= part.AggregateScriptEvents;
                }
            }
#else
            foreach (SceneObjectPart part in m_partsList.Where(part => part != null))
            {
                if (part != RootPart)
                    part.Flags = objectflagupdate;
                aggregateScriptEvents |= part.AggregateScriptEvents;
            }
#endif

            m_scriptListens_atTarget = ((aggregateScriptEvents & scriptEvents.at_target) != 0);
            m_scriptListens_notAtTarget = ((aggregateScriptEvents & scriptEvents.not_at_target) != 0);

            if (!m_scriptListens_atTarget && !m_scriptListens_notAtTarget)
            {
                lock (m_targets)
                    m_targets.Clear();
                RemoveGroupTarget(this);
            }
            m_scriptListens_atRotTarget = ((aggregateScriptEvents & scriptEvents.at_rot_target) != 0);
            m_scriptListens_notAtRotTarget = ((aggregateScriptEvents & scriptEvents.not_at_rot_target) != 0);

            if (!m_scriptListens_atRotTarget && !m_scriptListens_notAtRotTarget)
            {
                lock (m_rotTargets)
                    m_rotTargets.Clear();
                RemoveGroupTarget(this);
            }

            ScheduleGroupUpdate(PrimUpdateFlags.PrimFlags);
        }

        public void SetText(string text, Vector3 color, double alpha)
        {
            Color = Color.FromArgb(0xff - (int) (alpha*0xff),
                                   (int) (color.X*0xff),
                                   (int) (color.Y*0xff),
                                   (int) (color.Z*0xff));
            Text = text;

            m_rootPart.ScheduleUpdate(PrimUpdateFlags.Text);
        }

        public void ForEachPart(Action<SceneObjectPart> whatToDo)
        {
            lock (m_partsLock)
            {
                foreach (SceneObjectPart part in m_partsList)
                {
                    whatToDo(part);
                }
            }
        }

        internal void SetAxisRotation(int axis, int rotate10)
        {
            const int xaxis = 2;
            const int yaxis = 4;
            const int zaxis = 8;

            if (m_rootPart != null)
            {
                bool setX = ((axis & xaxis) != 0);
                bool setY = ((axis & yaxis) != 0);
                bool setZ = ((axis & zaxis) != 0);

                float setval = (rotate10 > 0) ? 1f : 0f;

                if (setX)
                    m_rootPart.RotationAxis.X = setval;
                if (setY)
                    m_rootPart.RotationAxis.Y = setval;
                if (setZ)
                    m_rootPart.RotationAxis.Z = setval;

                if (setX || setY || setZ)
                {
                    m_rootPart.SetPhysicsAxisRotation();
                }
            }
        }

        public int registerRotTargetWaypoint(Quaternion target, float tolerance)
        {
            scriptRotTarget waypoint = new scriptRotTarget {targetRot = target, tolerance = tolerance};
            uint handle = m_scene.SceneGraph.AllocateLocalId();
            waypoint.handle = handle;
            lock (m_rotTargets)
            {
                m_rotTargets.Add(handle, waypoint);
            }
            AddGroupTarget(this);
            return (int) handle;
        }

        public void unregisterRotTargetWaypoint(int handle)
        {
            lock (m_targets)
            {
                m_rotTargets.Remove((uint) handle);
                if (m_targets.Count == 0)
                    RemoveGroupTarget(this);
            }
        }

        public int registerTargetWaypoint(Vector3 target, float tolerance)
        {
            scriptPosTarget waypoint = new scriptPosTarget {targetPos = target, tolerance = tolerance};
            uint handle = m_scene.SceneGraph.AllocateLocalId();
            waypoint.handle = handle;
            lock (m_targets)
            {
                m_targets.Add(handle, waypoint);
            }
            AddGroupTarget(this);
            return (int) handle;
        }

        public void unregisterTargetWaypoint(int handle)
        {
            lock (m_targets)
            {
                m_targets.Remove((uint) handle);
                if (m_targets.Count == 0)
                    RemoveGroupTarget(this);
            }
        }

        public void AddGroupTarget(SceneObjectGroup grp)
        {
            m_scene.EventManager.OnFrame += checkAtTargets;
        }

        public void RemoveGroupTarget(SceneObjectGroup grp)
        {
            m_scene.EventManager.OnFrame -= checkAtTargets;
        }

        public void AddKeyframedMotion(KeyframeAnimation animation, KeyframeAnimation.Commands command)
        {
            if (command == KeyframeAnimation.Commands.Play)
            {
                KeyframeAnimation = animation;
                m_scene.EventManager.OnFrame += moveKeyframeMotion;
            }
            else
            {
                m_scene.EventManager.OnFrame -= moveKeyframeMotion;
                if (command == KeyframeAnimation.Commands.Stop)
                    KeyframeAnimation = null;
            }
        }

        public void moveKeyframeMotion()
        {
            if (m_KeyframeAnimation == null || m_KeyframeAnimation.TimeList.Length == 0)
            {
                m_scene.EventManager.OnFrame -= moveKeyframeMotion;
                return;
            }
            try
            {
                int currentTime = m_KeyframeAnimation.TimeList[m_KeyframeAnimation.CurrentAnimationPosition];
                float timeAmt = (1f / (float)currentTime);
                Vector3 currentTarget = m_KeyframeAnimation.PositionList.Length == 0 ? Vector3.Zero :
                    m_KeyframeAnimation.PositionList[m_KeyframeAnimation.CurrentAnimationPosition];
                Quaternion target = m_KeyframeAnimation.RotationList.Length == 0 ? Quaternion.Identity :
                    m_KeyframeAnimation.RotationList[m_KeyframeAnimation.CurrentAnimationPosition];
                m_KeyframeAnimation.CurrentFrame++; //Add one to the current frame so that we know when to stops
                bool AllDoneMoving = false;
                bool MadeItToCheckpoint = false;
                if (m_KeyframeAnimation.CurrentFrame == currentTime)
                {
                    if (m_KeyframeAnimation.CurrentMode == KeyframeAnimation.Modes.Forward)
                    {
                        m_KeyframeAnimation.CurrentAnimationPosition += 1;
                        if (m_KeyframeAnimation.CurrentAnimationPosition == m_KeyframeAnimation.TimeList.Length)
                        {
                            //All done moving...
                            AllDoneMoving = true;
                            m_scene.EventManager.OnFrame -= moveKeyframeMotion;
                        }
                    }
                    else if (m_KeyframeAnimation.CurrentMode == KeyframeAnimation.Modes.Reverse)
                    {
                        m_KeyframeAnimation.CurrentAnimationPosition -= 1;
                        if (m_KeyframeAnimation.CurrentAnimationPosition < 0)
                        {
                            //All done moving...
                            AllDoneMoving = true;
                            m_scene.EventManager.OnFrame -= moveKeyframeMotion;
                        }
                    }
                    else if (m_KeyframeAnimation.CurrentMode == KeyframeAnimation.Modes.Loop)
                    {
                        m_KeyframeAnimation.CurrentAnimationPosition += 1;
                        if (m_KeyframeAnimation.CurrentAnimationPosition == m_KeyframeAnimation.TimeList.Length)
                            m_KeyframeAnimation.CurrentAnimationPosition = 0;
                    }
                    else if (m_KeyframeAnimation.CurrentMode == KeyframeAnimation.Modes.PingPong)
                    {
                        if (m_KeyframeAnimation.PingPongForwardMotion)
                        {
                            m_KeyframeAnimation.CurrentAnimationPosition += 1;
                            if (m_KeyframeAnimation.CurrentAnimationPosition == m_KeyframeAnimation.TimeList.Length)
                            {
                                m_KeyframeAnimation.PingPongForwardMotion = !m_KeyframeAnimation.PingPongForwardMotion;
                                m_KeyframeAnimation.CurrentAnimationPosition -= 2;
                            }
                        }
                        else
                        {
                            m_KeyframeAnimation.CurrentAnimationPosition -= 1;
                            if (m_KeyframeAnimation.CurrentAnimationPosition < 0)
                            {
                                m_KeyframeAnimation.PingPongForwardMotion = !m_KeyframeAnimation.PingPongForwardMotion;
                                m_KeyframeAnimation.CurrentAnimationPosition += 2;
                            }
                        }
                    }
                    m_KeyframeAnimation.CurrentFrame = 0;
                    MadeItToCheckpoint = true;
                }

                if (m_KeyframeAnimation.PositionList.Length != 0)
                {
                    Vector3 _target_velocity =
                                new Vector3(
                                    (currentTarget.X - m_KeyframeAnimation.InitialPosition.X) * timeAmt,
                                    (currentTarget.Y - m_KeyframeAnimation.InitialPosition.Y) * timeAmt,
                                    (currentTarget.Z - m_KeyframeAnimation.InitialPosition.Z) * timeAmt
                                    );
                    if (MadeItToCheckpoint)
                    {
                        if(AllDoneMoving)
                            Velocity = Vector3.Zero;
                        SetAbsolutePosition(true, currentTarget);
                        m_KeyframeAnimation.InitialPosition = currentTarget;
                    }
                    else
                    {
                        SetAbsolutePosition(true, m_rootPart.AbsolutePosition + _target_velocity);
                        m_rootPart.Velocity = _target_velocity / 45f;
                    }
                }
                if (m_KeyframeAnimation.RotationList.Length != 0)
                {
                    Quaternion source = m_rootPart.RotationOffset;
                    Quaternion newInterpolation = Quaternion.Slerp(source, target, 1f / ((float)currentTime - (float)m_KeyframeAnimation.CurrentFrame));
                    m_rootPart.UpdateRotation(newInterpolation);
                    if (MadeItToCheckpoint)
                    {
                        //Force set it to the right position, just to be sure
                        m_rootPart.UpdateRotation(target);
                        m_KeyframeAnimation.InitialRotation = target;
                    }
                }
            }
            catch
            {
                m_scene.EventManager.OnFrame -= moveKeyframeMotion;
            }
            ScheduleGroupTerseUpdate();
        }

        public void checkAtTargets()
        {
            if (m_scriptListens_atTarget || m_scriptListens_notAtTarget)
            {
                if (m_targets.Count > 0)
                {
                    bool at_target = false;
                    //Vector3 targetPos;
                    //uint targetHandle;
                    Dictionary<uint, scriptPosTarget> atTargets = new Dictionary<uint, scriptPosTarget>();
                    lock (m_targets)
                    {
                        foreach (uint idx in m_targets.Keys)
                        {
                            scriptPosTarget target = m_targets[idx];
                            if (Util.GetDistanceTo(target.targetPos, m_rootPart.GroupPosition) <= target.tolerance)
                            {
                                // trigger at_target
                                if (m_scriptListens_atTarget)
                                {
                                    at_target = true;
                                    scriptPosTarget att = new scriptPosTarget
                                                              {
                                                                  targetPos = target.targetPos,
                                                                  tolerance = target.tolerance,
                                                                  handle = target.handle
                                                              };
                                    atTargets.Add(idx, att);
                                }
                            }
                        }
                    }

                    if (atTargets.Count > 0)
                    {
                        uint[] localids = new uint[0];
                        localids = new uint[m_parts.Count];
                        int cntr = 0;
                        foreach (SceneObjectPart part in m_partsList)
                        {
                            localids[cntr] = part.LocalId;
                            cntr++;
                        }

                        foreach (uint t in localids)
                        {
                            foreach(scriptPosTarget att in atTargets.Values)
                            {
                                m_scene.EventManager.TriggerAtTargetEvent(
                                    t, att.handle, att.targetPos, m_rootPart.GroupPosition);
                            }
                        }

                        return;
                    }

                    if (m_scriptListens_notAtTarget && !at_target)
                    {
                        //trigger not_at_target
                        uint[] localids = new uint[0];
                        localids = new uint[m_parts.Count];
                        int cntr = 0;
                        foreach (SceneObjectPart part in m_partsList)
                        {
                            localids[cntr] = part.LocalId;
                            cntr++;
                        }

                        foreach (uint t in localids)
                        {
                            m_scene.EventManager.TriggerNotAtTargetEvent(t);
                        }
                    }
                }
            }
            if (m_scriptListens_atRotTarget || m_scriptListens_notAtRotTarget)
            {
                if (m_rotTargets.Count > 0)
                {
                    bool at_Rottarget = false;
                    Dictionary<uint, scriptRotTarget> atRotTargets = new Dictionary<uint, scriptRotTarget>();
                    lock (m_rotTargets)
                    {
                        foreach (uint idx in m_rotTargets.Keys)
                        {
                            scriptRotTarget target = m_rotTargets[idx];
                            double angle =
                                Math.Acos(target.targetRot.X*m_rootPart.RotationOffset.X +
                                          target.targetRot.Y*m_rootPart.RotationOffset.Y +
                                          target.targetRot.Z*m_rootPart.RotationOffset.Z +
                                          target.targetRot.W*m_rootPart.RotationOffset.W)*2;
                            if (angle < 0) angle = -angle;
                            if (angle > Math.PI) angle = (Math.PI*2 - angle);
                            if (angle <= target.tolerance)
                            {
                                // trigger at_rot_target
                                if (m_scriptListens_atRotTarget)
                                {
                                    at_Rottarget = true;
                                    scriptRotTarget att = new scriptRotTarget
                                                              {
                                                                  targetRot = target.targetRot,
                                                                  tolerance = target.tolerance,
                                                                  handle = target.handle
                                                              };
                                    atRotTargets.Add(idx, att);
                                }
                            }
                        }
                    }

                    if (atRotTargets.Count > 0)
                    {
                        uint[] localids = new uint[0];
                        localids = new uint[m_parts.Count];
                        int cntr = 0;
                        foreach (SceneObjectPart part in m_partsList)
                        {
                            localids[cntr] = part.LocalId;
                            cntr++;
                        }

                        foreach (uint t in localids)
                        {
                            foreach (scriptRotTarget att in atRotTargets.Values)
                            {
                                m_scene.EventManager.TriggerAtRotTargetEvent(
                                    t, att.handle, att.targetRot, m_rootPart.RotationOffset);
                            }
                        }

                        return;
                    }

                    if (m_scriptListens_notAtRotTarget && !at_Rottarget)
                    {
                        //trigger not_at_target
                        uint[] localids = new uint[0];
                        localids = new uint[m_parts.Count];
                        int cntr = 0;
                        foreach (SceneObjectPart part in m_partsList)
                        {
                            localids[cntr] = part.LocalId;
                            cntr++;
                        }

                        foreach (uint t in localids)
                        {
                            m_scene.EventManager.TriggerNotAtRotTargetEvent(t);
                        }
                    }
                }
            }
        }

        public void CheckSculptAndLoad()
        {
            foreach (SceneObjectPart part in m_partsList)
            {
                if (part.Shape == null)
                    continue;
                if (!(RootPart.PhysicsType == (byte) PhysicsShapeType.None ||
                      part.PhysicsType == (byte) PhysicsShapeType.None ||
                      ((part.Flags & PrimFlags.Phantom) == PrimFlags.Phantom &&
                       !part.VolumeDetectActive) ||
                      ((RootPart.Flags & PrimFlags.Phantom) == PrimFlags.Phantom &&
                       !RootPart.VolumeDetectActive)))
                {
                    if (part.Shape.SculptEntry && part.Shape.SculptTexture != UUID.Zero)
                    {
                        // If no sculpt data exists, we need to get the data
                        m_scene.AssetService.Get(part.Shape.SculptTexture.ToString(), true, part.AssetReceived);
                        //In the mean time...
                        //part.Shape.SculptEntry = false;
                        part.Shape.SculptData = new byte[0];
                    }
                }
            }
        }

        public void GeneratedMesh(ISceneChildEntity part, IMesh mesh)
        {
            //This destroys the mesh if it is added... this needs added in a way that won't corrupt the mesh
            /*if (part.Shape.SculptType == (byte)SculptType.Mesh && !mesh.WasCached)//If it was cached, we don't want to resave it
            {
                //We can cache meshes into the mesh itself, saving time generating it next time around
                OSDMap meshOsd = (OSDMap)OSDParser.DeserializeLLSDBinary(part.Shape.SculptData);
                meshOsd["physics_cached"] = new OSDMap();
                mesh.Serialize();
                mesh.WasCached = true;
                UUID newSculptTexture;
                if (m_scene.AssetService.UpdateContent(part.Shape.SculptTexture,
                    OSDParser.SerializeLLSDBinary(meshOsd), out newSculptTexture))
                {
                    part.Shape.SculptTexture = newSculptTexture;
                    HasGroupChanged = true;
                }
            }*/
        }

        public void TriggerScriptMovingStartEvent()
        {
            foreach (SceneObjectPart part in ChildrenList)
            {
                part.TriggerScriptMovingStartEvent();
            }
        }

        public void TriggerScriptMovingEndEvent()
        {
            foreach (SceneObjectPart part in ChildrenList)
            {
                part.TriggerScriptMovingEndEvent();
            }
        }

        public override string ToString()
        {
            return String.Format("{0} {1} ({2})", Name, UUID, AbsolutePosition);
        }

        #region Copying

        /// <summary>
        ///   Make an exact copy of this group.
        ///   This does NOT reset any UUIDs, localIDs, or anything, as this is an EXACT copy.
        /// </summary>
        /// <returns></returns>
        public ISceneEntity Copy(bool clonePhys)
        {
            SceneObjectGroup dupe = (SceneObjectGroup) MemberwiseClone();

            //Block attempts to persist to the DB
            dupe.m_isLoaded = false;

            dupe.m_parts = new Dictionary<UUID, SceneObjectPart>();
            dupe.m_partsList = new List<SceneObjectPart>();

            dupe.m_scene = Scene;

            // Warning, The following code related to previousAttachmentStatus is needed so that clones of 
            // attachments do not bordercross while they're being duplicated.  This is hacktastic!
            // Normally, setting AbsolutePosition will bordercross a prim if it's outside the region!
            // unless IsAttachment is true!, so to prevent border crossing, we save it's attachment state 
            // (which should be false anyway) set it as an Attachment and then set it's Absolute Position, 
            // then restore it's attachment state

            // This is only necessary when userExposed is false!

            dupe.ClearChildren();

            dupe.AddChild(m_rootPart.Copy(dupe, clonePhys), m_rootPart.LinkNum);

            bool previousAttachmentStatus = dupe.RootPart.IsAttachment;

            dupe.RootPart.IsAttachment = true;

            dupe.AbsolutePosition = AbsolutePosition;

            dupe.RootPart.IsAttachment = previousAttachmentStatus;

            dupe.m_rootPart.TrimPermissions();

            List<SceneObjectPart> partList = new List<SceneObjectPart>();

            lock (m_partsLock)
            {
                partList.AddRange(m_partsList);
            }

            //Sort the list by link number so that we get them in the right order
            partList.Sort(Scene.SceneGraph.LinkSetSorter);

            foreach (SceneObjectPart part in partList)
            {
                if (part.UUID != m_rootPart.UUID)
                {
                    SceneObjectPart copy = part.Copy(dupe, clonePhys);
                    copy.LinkNum = part.LinkNum;
                    dupe.LinkChild(copy);
                }
            }
            dupe.m_ValidgrpOOB = false;
            //Reset the loaded setting
            dupe.m_isLoaded = true;

            return dupe;
        }

        /// <summary>
        ///   Rebuild the physical representation of all the prims.
        ///   This is used after copying the prim so that all of the object is readded to the physics scene.
        /// </summary>
        public void RebuildPhysicalRepresentation(bool keepSelectedStatuses)
        {
            // long lock or array copy?  in this case lets try array
            SceneObjectPart[] parts;
            SceneObjectPart part;
            int i;

            lock (m_partsLock)
                parts = m_partsList.ToArray();

            if (RootPart.PhysActor != null)
                RootPart.PhysActor.BlockPhysicalReconstruction = true;

            for (i = 0; i < parts.Length; i++)
            {
                part = parts[i];
//                PhysicsObject oldActor = part.PhysActor;
//                PrimitiveBaseShape pbs = part.Shape;
                if (part.PhysActor != null)
                {
                    part.PhysActor.RotationalVelocity = Vector3.Zero;
                    part.m_hasSubscribedToCollisionEvent = false;
                    part.PhysActor.OnCollisionUpdate -= part.PhysicsCollision;
                    part.PhysActor.OnRequestTerseUpdate -= part.PhysicsRequestingTerseUpdate;
                    part.PhysActor.OnSignificantMovement -= part.ParentGroup.CheckForSignificantMovement;
                    part.PhysActor.OnOutOfBounds -= part.PhysicsOutOfBounds;

                    //part.PhysActor.delink ();
                    //Remove the old one so that we don't have more than we should,
                    //  as when we copy, it readds it to the PhysicsScene somehow
                    //if (part.IsRoot)//The root removes all children
                    m_scene.PhysicsScene.RemovePrim(part.PhysActor);
                    part.FireOnRemovedPhysics();
                    part.PhysActor = null;
                }
                //Reset any old data that we have
                part.Velocity = Vector3.Zero;
                part.AngularVelocity = Vector3.Zero;
                part.Acceleration = Vector3.Zero;
                part.GenerateRotationalVelocityFromOmega();
            }

            //Check for meshes and stuff
            CheckSculptAndLoad();

            // check root part setting that make the entire object not having physics rep

            if (RootPart.PhysicsType == (byte) PhysicsShapeType.None ||
                ((RootPart.Flags & PrimFlags.Phantom) == PrimFlags.Phantom && !RootPart.VolumeDetectActive))
            {
                Scene.AuroraEventManager.FireGenericEventHandler("ObjectChangedPhysicalStatus", this);
                if (OnFinishedPhysicalRepresentationBuilding != null)
                    OnFinishedPhysicalRepresentationBuilding();
                return;
            }

            // create the root part
            RootPart.PhysActor = m_scene.PhysicsScene.AddPrimShape(RootPart);
            if (RootPart.PhysActor == null)
                return;
            //                    RootPart.PhysActor.BuildingRepresentation = true;
            RootPart.PhysActor.BlockPhysicalReconstruction = true;
                //Don't let it rebuild it until we have all the links done

            //Fix the localID!
            RootPart.PhysActor.LocalID = RootPart.LocalId;
            RootPart.PhysActor.UUID = RootPart.UUID;
            RootPart.PhysActor.VolumeDetect = RootPart.VolumeDetectActive;

            //Force deselection here so that it isn't stuck forever
            RootPart.PhysActor.Selected = keepSelectedStatuses && RootPart.IsSelected;

            RootPart.PhysActor.SetMaterial(RootPart.Material, false);

//            bool rootIsPhysical;

            if ((RootPart.Flags & PrimFlags.Physics) == PrimFlags.Physics)
            {
//                rootIsPhysical = true;
                RootPart.PhysActor.IsPhysical = true;
            }
//            else
//                rootIsPhysical = false;

            //Add collision updates
            //part.PhysActor.OnCollisionUpdate += RootPart.PhysicsCollision;
            RootPart.PhysActor.OnRequestTerseUpdate += RootPart.PhysicsRequestingTerseUpdate;
            RootPart.PhysActor.OnSignificantMovement += RootPart.ParentGroup.CheckForSignificantMovement;
            RootPart.PhysActor.OnOutOfBounds += RootPart.PhysicsOutOfBounds;

            RootPart.FireOnAddedPhysics();
            RootPart.aggregateScriptEvents();

            for (i = 0; i < parts.Length; i++)
            {
                part = parts[i];
                if (part == RootPart ||
                    part.PhysicsType == (byte) PhysicsShapeType.None ||
                    ((part.Flags & PrimFlags.Phantom) == PrimFlags.Phantom && !part.VolumeDetectActive))

                {
                    continue; // ignore phantom prims
                }

                //Now read the physics actor to the physics scene
                part.PhysActor = m_scene.PhysicsScene.AddPrimShape(part);
                if (part.PhysActor == null)
                    continue;
                //                    part.PhysActor.BuildingRepresentation = true;
                //                    if(part.IsRoot)
                part.PhysActor.BlockPhysicalReconstruction = true;
                    //Don't let it rebuild it until we have all the links done

                //Fix the localID!
                part.PhysActor.LocalID = part.LocalId;
                part.PhysActor.UUID = part.UUID;
                part.PhysActor.VolumeDetect = part.VolumeDetectActive;

                //Force deselection here so that it isn't stuck forever
                part.PhysActor.Selected = keepSelectedStatuses && part.IsSelected;

                part.PhysActor.SetMaterial(part.Material, false);
                if ((part.Flags & PrimFlags.Physics) == PrimFlags.Physics)
                    part.PhysActor.IsPhysical = true;

                //Add collision updates
                //part.PhysActor.OnCollisionUpdate += part.PhysicsCollision;
                part.PhysActor.OnRequestTerseUpdate += part.PhysicsRequestingTerseUpdate;
                part.PhysActor.OnSignificantMovement += part.ParentGroup.CheckForSignificantMovement;
                part.PhysActor.OnOutOfBounds += part.PhysicsOutOfBounds;

                part.FireOnAddedPhysics();
                part.aggregateScriptEvents();
                //Link the prim then
//                if(rootIsPhysical)
                part.PhysActor.link(RootPart.PhysActor);
            }

            Scene.AuroraEventManager.FireGenericEventHandler("ObjectChangedPhysicalStatus", this);

            RootPart.PhysActor.BlockPhysicalReconstruction = false; // this sets children also (in AODE at least)
//            RootPart.PhysActor.BuildingRepresentation = false;

            FixVehicleParams(RootPart);

            if (OnFinishedPhysicalRepresentationBuilding != null)
                OnFinishedPhysicalRepresentationBuilding();
        }

/*
            lock (m_partsLock)
            {
                foreach (SceneObjectPart part in m_partsList)
                {
                    PhysicsObject oldActor = part.PhysActor;
                    PrimitiveBaseShape pbs = part.Shape;
                    //Reset any old data that we have
                    part.Velocity = Vector3.Zero;
                    part.Acceleration = Vector3.Zero;
                    if (part.PhysActor != null)
                    {
                        part.PhysActor.RotationalVelocity = Vector3.Zero;
                        part.PhysActor.UnSubscribeEvents ();
                        part.m_hasSubscribedToCollisionEvent = false;
                        part.PhysActor.OnCollisionUpdate -= part.PhysicsCollision;
                        part.PhysActor.OnRequestTerseUpdate -= part.PhysicsRequestingTerseUpdate;
                        part.PhysActor.OnSignificantMovement -= part.ParentGroup.CheckForSignificantMovement;
                        part.PhysActor.OnOutOfBounds -= part.PhysicsOutOfBounds;

                        //part.PhysActor.delink ();
                        //Remove the old one so that we don't have more than we should,
                        //  as when we copy, it readds it to the PhysicsScene somehow
                        //if (part.IsRoot)//The root removes all children
                        m_scene.PhysicsScene.RemovePrim (part.PhysActor);

                        part.FireOnRemovedPhysics ();
                    }
                    part.AngularVelocity = Vector3.Zero;
                    part.GenerateRotationalVelocityFromOmega ();
                }
            }
            //Check for meshes and stuff
            CheckSculptAndLoad ();

            //This is a heavy operation... it is really bad to lock this, but if we don't, we could have multiple threads in here... which would be baaad
            lock (m_partsLock)
            {
                foreach (SceneObjectPart part in m_partsList)
                {
                    if (RootPart.PhysicsType == (byte)PhysicsShapeType.None ||
                        part.PhysicsType == (byte)PhysicsShapeType.None ||
                        ((part.Flags & PrimFlags.Phantom) == PrimFlags.Phantom &&
                        !part.VolumeDetectActive) ||
                        ((RootPart.Flags & PrimFlags.Phantom) == PrimFlags.Phantom &&
                        !RootPart.VolumeDetectActive))
                    {
                        part.PhysActor = null;
                        continue; //Don't rebuild! All phantom if the root is phantom
                    }
                    
                    //Now readd the physics actor to the physics scene
                    part.PhysActor = m_scene.PhysicsScene.AddPrimShape (part);
//                    part.PhysActor.BuildingRepresentation = true;
//                    if(part.IsRoot)
//                        part.PhysActor.BlockPhysicalReconstruction = true;//Don't let it rebuild it until we have all the links done

                    //Fix the localID!
                    part.PhysActor.LocalID = part.LocalId;
                    part.PhysActor.UUID = part.UUID;
                    part.PhysActor.VolumeDetect = part.VolumeDetectActive;
                    
                    //Force deselection here so that it isn't stuck forever
                    if (!keepSelectedStatuses)
                        part.PhysActor.Selected = false;
                    else
                        part.PhysActor.Selected = part.IsSelected;

                    part.PhysActor.SetMaterial (part.Material, false);
                    //Add collision updates
                    //part.PhysActor.OnCollisionUpdate += part.PhysicsCollision;
                    part.PhysActor.OnRequestTerseUpdate += part.PhysicsRequestingTerseUpdate;
                    part.PhysActor.OnSignificantMovement += part.ParentGroup.CheckForSignificantMovement;
                    part.PhysActor.OnOutOfBounds += part.PhysicsOutOfBounds;

                    part.FireOnAddedPhysics ();
                    part.aggregateScriptEvents ();
                }
                Scene.AuroraEventManager.FireGenericEventHandler ("ObjectChangedPhysicalStatus", this);
            }
            lock (m_partsLock)
            {
                foreach (SceneObjectPart part in m_partsList)
                {
                    if (!part.IsRoot && RootPart.PhysActor != null && part.PhysActor != null)//Link the prim then
                        part.PhysActor.link (RootPart.PhysActor);
                }
                foreach (SceneObjectPart part in m_partsList)
                {
                    if (part.PhysActor != null)
                    {
                        FixVehicleParams(part);
// *
                        if(part.IsRoot)
                        {
                            //All done linking, build the body
                            part.PhysActor.BlockPhysicalReconstruction = false;
                        }
                        part.PhysActor.BuildingRepresentation = false;
// *
                    }
                }
                if(OnFinishedPhysicalRepresentationBuilding != null)
                    OnFinishedPhysicalRepresentationBuilding();
            }
        }
*/

        /// <summary>
        ///   Fix all the vehicle params after rebuilding the representation
        /// </summary>
        /// <param name = "part"></param>
        private void FixVehicleParams(SceneObjectPart part)
        {
            part.PhysActor.VehicleType = part.VehicleType;

            // OSD o = part.GetComponentState("VehicleParameters");
            foreach (OSD param in part.VehicleFlags)
            {
                part.PhysActor.VehicleFlags(param.AsInteger(), false);
            }

            foreach (KeyValuePair<string, OSD> param in part.VehicleParameters)
            {
                if (param.Value.Type == OSDType.Real)
                    part.PhysActor.VehicleFloatParam(int.Parse(param.Key), (float) param.Value.AsReal());
                else if (param.Value.Type == OSDType.Array)
                {
                    OSDArray a = (OSDArray) param.Value;
                    if (a.Count == 3)
                        part.PhysActor.VehicleVectorParam(int.Parse(param.Key), param.Value.AsVector3());
                    else
                        part.PhysActor.VehicleRotationParam(int.Parse(param.Key), param.Value.AsQuaternion());
                }
            }
        }

        #endregion

        #region Script methods

        public Vector3 GetTorque()
        {
            // We check if rootpart is null here because scripts don't delete if you delete the host.
            // This means that unfortunately, we can pass a null physics actor to Simulate!
            // Make sure we don't do that!
            SceneObjectPart rootpart = m_rootPart;
            if (rootpart != null)
            {
                if (rootpart.PhysActor != null)
                {
                    if (!IsAttachment)
                    {
                        Vector3 torque = rootpart.PhysActor.Torque;
                        return torque;
                    }
                }
            }
            return Vector3.Zero;
        }

        /// <summary>
        ///   Set the owner of the root part.
        /// </summary>
        /// <param name = "part"></param>
        /// <param name = "cAgentID"></param>
        /// <param name = "cGroupID"></param>
        public void SetRootPartOwner(ISceneChildEntity part, UUID cAgentID, UUID cGroupID)
        {
            part.LastOwnerID = part.OwnerID;
            part.OwnerID = cAgentID;
            part.GroupID = cGroupID;

            if (part.OwnerID != cAgentID)
            {
                // Apply Next Owner Permissions if we're not bypassing permissions
                if (!m_scene.Permissions.BypassPermissions())
                    ApplyNextOwnerPermissions();
            }

            part.ScheduleUpdate(PrimUpdateFlags.ForcedFullUpdate);
        }

        public void ScriptSetPhysicsStatus(bool UsePhysics)
        {
            bool IsTemporary = ((RootPart.Flags & PrimFlags.TemporaryOnRez) != 0);
            bool IsPhantom = ((RootPart.Flags & PrimFlags.Phantom) != 0);
            bool IsVolumeDetect = RootPart.VolumeDetectActive;
            UpdatePrimFlags(RootPart.LocalId, UsePhysics, IsTemporary, IsPhantom, IsVolumeDetect, null);
        }

        public void ScriptSetTemporaryStatus(bool TemporaryStatus)
        {
            bool UsePhysics = ((RootPart.Flags & PrimFlags.Physics) != 0);
            bool IsPhantom = ((RootPart.Flags & PrimFlags.Phantom) != 0);
            bool IsVolumeDetect = RootPart.VolumeDetectActive;
            UpdatePrimFlags(RootPart.LocalId, UsePhysics, TemporaryStatus, IsPhantom, IsVolumeDetect, null);
        }

        public void ScriptSetPhantomStatus(bool PhantomStatus)
        {
            bool UsePhysics = ((RootPart.Flags & PrimFlags.Physics) != 0);
            bool IsTemporary = ((RootPart.Flags & PrimFlags.TemporaryOnRez) != 0);
            bool IsVolumeDetect = RootPart.VolumeDetectActive;
            UpdatePrimFlags(RootPart.LocalId, UsePhysics, IsTemporary, PhantomStatus, IsVolumeDetect, null);
        }

        public void ScriptSetVolumeDetect(bool VDStatus)
        {
            bool UsePhysics = ((RootPart.Flags & PrimFlags.Physics) != 0);
            bool IsTemporary = ((RootPart.Flags & PrimFlags.TemporaryOnRez) != 0);
            bool IsPhantom = ((RootPart.Flags & PrimFlags.Phantom) != 0);
            UpdatePrimFlags(RootPart.LocalId, UsePhysics, IsTemporary, IsPhantom, VDStatus, null);
        }

        public void applyImpulse(Vector3 impulse)
        {
            // We check if rootpart is null here because scripts don't delete if you delete the host.
            // This means that unfortunately, we can pass a null physics actor to Simulate!
            // Make sure we don't do that!
            SceneObjectPart rootpart = m_rootPart;
            if (rootpart != null)
            {
                if (IsAttachment)
                {
                    IScenePresence avatar = m_scene.GetScenePresence(rootpart.AttachedAvatar);
                    if (avatar != null)
                    {
                        avatar.PushForce(impulse);
                    }
                }
                else
                {
                    if (rootpart.PhysActor != null)
                        rootpart.PhysActor.AddForce(impulse, true);
                }
            }
        }

        public void applyAngularImpulse(Vector3 impulse)
        {
            // We check if rootpart is null here because scripts don't delete if you delete the host.
            // This means that unfortunately, we can pass a null physics actor to Simulate!
            // Make sure we don't do that!
            SceneObjectPart rootpart = m_rootPart;
            if (rootpart != null)
            {
                if (rootpart.PhysActor != null)
                {
                    if (!IsAttachment)
                        rootpart.PhysActor.AddAngularForce(impulse, true);
                }
            }
        }

        public void setAngularImpulse(Vector3 impulse)
        {
            // We check if rootpart is null here because scripts don't delete if you delete the host.
            // This means that unfortunately, we can pass a null physics actor to Simulate!
            // Make sure we don't do that!
            SceneObjectPart rootpart = m_rootPart;
            if (rootpart != null)
            {
                if (rootpart.PhysActor != null)
                {
                    if (!IsAttachment)
                        rootpart.PhysActor.Torque = impulse;
                }
            }
        }

        public void moveToTarget(Vector3 target, float tau)
        {
            SceneObjectPart rootpart = m_rootPart;
            if (rootpart != null)
            {
                if (IsAttachment)
                {
                    IScenePresence avatar = m_scene.GetScenePresence(rootpart.AttachedAvatar);
                    if (avatar != null)
                    {
                        List<string> coords = new List<string>();
                        uint regionX = 0;
                        uint regionY = 0;
                        Utils.LongToUInts(Scene.RegionInfo.RegionHandle, out regionX, out regionY);
                        target.X += regionX;
                        target.Y += regionY;
                        coords.Add(target.X.ToString());
                        coords.Add(target.Y.ToString());
                        coords.Add(target.Z.ToString());
                        avatar.DoMoveToPosition(avatar, "", coords);
                    }
                }
                else
                {
                    rootpart.SetMoveToTarget(true, target, tau);
                }
            }
        }

        public void stopMoveToTarget()
        {
            SceneObjectPart rootpart = m_rootPart;
            if (rootpart != null)
            {
                if (rootpart.PhysActor != null)
                {
                    rootpart.SetMoveToTarget(false, Vector3.Zero, 0);
                }
            }
        }

        /// <summary>
        ///   Uses a PID to attempt to clamp the object on the Z axis at the given height over tau seconds.
        /// </summary>
        /// <param name = "height">Height to hover.  Height of zero disables hover.</param>
        /// <param name = "hoverType">Determines what the height is relative to </param>
        /// <param name = "tau">Number of seconds over which to reach target</param>
        public void SetHoverHeight(float height, PIDHoverType hoverType, float tau)
        {
            SceneObjectPart rootpart = m_rootPart;
            if (rootpart != null)
            {
                if (rootpart.PhysActor != null)
                {
                    if (height != 0f)
                    {
                        rootpart.PIDHoverHeight = height;
                        rootpart.PIDHoverType = hoverType;
                        rootpart.PIDTau = tau;
                        rootpart.PIDHoverActive = true;
                    }
                    else
                    {
                        rootpart.PIDHoverActive = false;
                    }
                }
            }
        }

        public void SetPartOwner(SceneObjectPart part, UUID cAgentID, UUID cGroupID)
        {
            part.OwnerID = cAgentID;
            part.GroupID = cGroupID;
        }

        #endregion

        #region Scheduling

        /// <summary>
        ///   Send an update to all prims in the group to a specific avatar
        /// </summary>
        /// <param name = "presence"></param>
        /// <param name = "UpdateFlags"></param>
        public void ScheduleGroupUpdateToAvatar(IScenePresence presence, PrimUpdateFlags UpdateFlags)
        {
            //We have to send the root part first as the client wants it that way
            presence.AddUpdateToAvatar(RootPart, UpdateFlags);

#if (!ISWIN)
            foreach (SceneObjectPart part in m_partsList)
            {
                if (part != RootPart)
                {
                    presence.AddUpdateToAvatar(part, UpdateFlags);
                }
            }
#else
            foreach (SceneObjectPart part in m_partsList.Where(part => part != RootPart))
            {
                presence.AddUpdateToAvatar(part, UpdateFlags);
            }
#endif
        }

        /// <summary>
        ///   Send an update to all prims in the group
        /// </summary>
        /// <param name = "UpdateFlags"></param>
        public void ScheduleGroupUpdate(PrimUpdateFlags UpdateFlags)
        {
            //We have to send the root part first as the client wants it that way
            RootPart.ScheduleUpdate(UpdateFlags);

#if (!ISWIN)
            foreach (SceneObjectPart part in m_partsList)
            {
                if (part != RootPart)
                {
                    part.ScheduleUpdate(UpdateFlags);
                }
            }
#else
            foreach (SceneObjectPart part in m_partsList.Where(part => part != RootPart))
            {
                part.ScheduleUpdate(UpdateFlags);
            }
#endif
        }

        /// <summary>
        ///   Schedule a terse update (position, rotation, velocity, and rotational velocity update) for this object to all clients
        /// </summary>
        public void ScheduleGroupTerseUpdate()
        {
            //We have to send the root part first as the client wants it that way
            RootPart.ScheduleTerseUpdate();

#if (!ISWIN)
            foreach (SceneObjectPart part in m_partsList)
            {
                if (part != RootPart)
                {
                    part.ScheduleTerseUpdate();
                }
            }
#else
            foreach (SceneObjectPart part in m_partsList.Where(part => part != RootPart))
            {
                part.ScheduleTerseUpdate();
            }
#endif
        }

        public void Update()
        {
        }

        ///<summary>
        ///</summary>
        ///<param name="remoteClient"></param>
        ///<param name="AgentID"></param>
        ///<param name="RequestFlags"></param>
        public void ServiceObjectPropertiesFamilyRequest(IClientAPI remoteClient, UUID AgentID, uint RequestFlags)
        {
            remoteClient.SendObjectPropertiesFamilyData(RequestFlags, RootPart.UUID, RootPart.OwnerID, RootPart.GroupID,
                                                        RootPart.BaseMask,
                                                        RootPart.OwnerMask, RootPart.GroupMask, RootPart.EveryoneMask,
                                                        RootPart.NextOwnerMask,
                                                        RootPart.OwnershipCost, RootPart.ObjectSaleType,
                                                        RootPart.SalePrice, RootPart.Category,
                                                        RootPart.CreatorID, RootPart.Name, RootPart.Description);
        }

        /// <summary>
        ///   See if the object has moved enough to trigger the Significant Movement event
        /// </summary>
        protected internal void CheckForSignificantMovement()
        {
            m_scene.EventManager.TriggerSignificantObjectMovement(this);
            //Do this second! This is important, otherwise 
            // if the object isn't allowed, we will not be able
            // to reset its position to the last known good pos
            m_lastSignificantPosition = AbsolutePosition;
        }

        #endregion

        #region Get Children Methods

        /// <summary>
        ///   Get the child part by LinkNum
        /// </summary>
        /// <param name = "linknum"></param>
        /// <returns>null if no child part with that linknum or child part</returns>
        public IEntity GetLinkNumPart(int linknum)
        {
            if (linknum <= m_parts.Count)
            {
                if (m_parts.Count == 1)
                    return RootPart;
#if (!ISWIN)
                foreach (SceneObjectPart part in m_partsList)
                {
                    if (part.LinkNum == linknum)
                    {
                        return part;
                    }
                }
#else
                foreach (SceneObjectPart part in m_partsList.Where(part => part.LinkNum == linknum))
                {
                    return part;
                }
#endif
            }
            //Check sitting avatars
            int count = m_parts.Count + 1;
            foreach (UUID agentID in SitTargetAvatar)
            {
                if (count == linknum)
                {
                    return m_scene.GetScenePresence(agentID);
                }
                count++;
            }

            return null;
        }

        /// <summary>
        ///   Get a child prim of this group by LocalID
        /// </summary>
        /// <param name = "LocalID"></param>
        /// <param name = "entity"></param>
        /// <returns></returns>
        public bool GetChildPrim(uint LocalID, out ISceneChildEntity entity)
        {
            entity = GetChildPart(LocalID);
            return entity != null;
        }

        /// <summary>
        ///   Get a child prim of this group by UUID
        /// </summary>
        /// <param name = "UUID2"></param>
        /// <param name = "entity"></param>
        /// <returns></returns>
        public bool GetChildPrim(UUID UUID2, out ISceneChildEntity entity)
        {
            entity = GetChildPart(UUID2);
            return entity != null;
        }

        /// <summary>
        ///   Get a part with a given UUID
        /// </summary>
        /// <param name = "primID"></param>
        /// <returns>null if a child part with the primID was not found</returns>
        public ISceneChildEntity GetChildPart(UUID primID)
        {
            SceneObjectPart childPart = null;
            m_parts.TryGetValue(primID, out childPart);
            return childPart;
        }

        /// <summary>
        ///   Get a part with a given UUID
        /// </summary>
        /// <param name = "primID"></param>
        /// <returns>null if a child part with the primID was not found</returns>
        public ISceneChildEntity GetChildPart(uint primID)
        {
#if (!ISWIN)
            foreach (ISceneChildEntity part in m_partsList)
            {
                if (part.LocalId == primID) return part;
            }
            return null;
#else
            return m_partsList.Cast<ISceneChildEntity>().FirstOrDefault(part => part.LocalId == primID);
#endif
        }

        #endregion

        #region Packet Handlers

        #region Linking and Delinking

        /// <summary>
        ///   Link the prims in a given group to this group
        /// </summary>
        /// <param name = "grp">The group of prims which should be linked to this group</param>
        public void LinkToGroup(ISceneEntity grp)
        {
            //MainConsole.Instance.DebugFormat(
            //    "[SCENE OBJECT GROUP]: Linking group with root part {0}, {1} to group with root part {2}, {3}",
            //    objectGroup.RootPart.Name, objectGroup.RootPart.UUID, RootPart.Name, RootPart.UUID);

            if (!(grp is SceneObjectGroup))
                return;
            SceneObjectGroup objectGroup = (SceneObjectGroup) grp;

            if (m_rootPart.PhysActor != null)
                m_rootPart.PhysActor.BlockPhysicalReconstruction = true;

            SceneObjectPart linkPart = objectGroup.m_rootPart;

            Vector3 oldGroupPosition = linkPart.GroupPosition;
            Quaternion oldRootRotation = linkPart.RotationOffset;
            Quaternion parentRot = m_rootPart.RotationOffset;

            linkPart.SetGroupPosition(AbsolutePosition); // just change it without doing anything else

            Vector3 axPos = oldGroupPosition - AbsolutePosition;
            axPos *= Quaternion.Inverse(parentRot);
            linkPart.SetOffsetPosition(axPos);

            Quaternion newRot = Quaternion.Inverse(parentRot)*oldRootRotation;
            linkPart.SetRotationOffset(false, newRot, false);

            //Fix the link number for the root
            if (m_rootPart.LinkNum == 0)
                m_rootPart.LinkNum = 1;

            SceneObjectPart[] objectGroupChildren = new SceneObjectPart[objectGroup.ChildrenList.Count];
            objectGroup.ChildrenList.CopyTo(objectGroupChildren, 0);

            //Destroy the old group
            m_scene.SceneGraph.DeleteEntity(objectGroup);
            objectGroup.IsDeleted = true;
            objectGroup.ClearChildren();


            lock (m_partsLock)
            {
                int linkNum = 2;
                //Add the root part to our group!
                m_scene.SceneGraph.LinkPartToSOG(this, linkPart, linkNum++);
                linkPart.CreateSelected = true;
                linkPart.FixOffsetPosition(linkPart.OffsetPosition, true); // nasty let all know about where this is
                // let physics link it
                if (linkPart.PhysActor != null && m_rootPart.PhysActor != null)
                {
                    if (linkPart.PhysicsType != (byte) PhysicsShapeType.None)
                        linkPart.PhysActor.link(m_rootPart.PhysActor);
                }
                //rest of parts
#if (!ISWIN)
                foreach (SceneObjectPart part in objectGroupChildren)
                {
                    if (part.UUID != objectGroup.m_rootPart.UUID)
                    {
                        LinkNonRootPart(part, oldGroupPosition, oldRootRotation, linkNum++);
                        part.FixOffsetPosition(part.OffsetPosition, true);
                        if (part.PhysActor != null && m_rootPart.PhysActor != null)
                            part.PhysActor.link(m_rootPart.PhysActor);
                    }
                }
#else
                foreach (SceneObjectPart part in objectGroupChildren.Where(part => part.UUID != objectGroup.m_rootPart.UUID))
                {
                    LinkNonRootPart(part, oldGroupPosition, oldRootRotation, linkNum++);
                    part.FixOffsetPosition(part.OffsetPosition, true);
                    if (part.PhysActor != null && m_rootPart.PhysActor != null)
                        part.PhysActor.link(m_rootPart.PhysActor);
                }
#endif
            }
            // Here's the deal, this is ABSOLUTELY CRITICAL so the physics scene gets the update about the 
            // position of linkset prims.  IF YOU CHANGE THIS, YOU MUST TEST colliding with just linked and 
            // unmoved prims!
            m_ValidgrpOOB = false;
            ResetChildPrimPhysicsPositions();

            if (m_rootPart.PhysActor != null)
                m_rootPart.PhysActor.BlockPhysicalReconstruction = false;
        }

        /// <summary>
        ///   Delink the given prim from this group.  The delinked prim is established as
        ///   an independent SceneObjectGroup.
        /// </summary>
        /// <param name = "part"></param>
        /// <param name = "sendEvents"></param>
        /// <returns>The object group of the newly delinked prim.</returns>
        public ISceneEntity DelinkFromGroup(ISceneChildEntity part, bool sendEvents)
        {
            if (!(part is SceneObjectPart))
                return null;
            SceneObjectPart linkPart = part as SceneObjectPart;
//                MainConsole.Instance.DebugFormat(
//                    "[SCENE OBJECT GROUP]: Delinking part {0}, {1} from group with root part {2}, {3}",
//                    linkPart.Name, linkPart.UUID, RootPart.Name, RootPart.UUID);

            Quaternion worldRot = linkPart.GetWorldRotation();

            // Remove the part from this object
            m_scene.SceneGraph.DeLinkPartFromEntity(this, linkPart);
            linkPart.SetParentLocalId(0);
            linkPart.LinkNum = 0;

            if (linkPart.PhysActor != null)
            {
                m_scene.PhysicsScene.RemovePrim(linkPart.PhysActor);
            }

            // We need to reset the child part's position
            // ready for life as a separate object after being a part of another object
            Quaternion parentRot = m_rootPart.RotationOffset;

            Vector3 axPos = linkPart.OffsetPosition;

            axPos *= parentRot;
            linkPart.SetOffsetPosition(axPos);
            linkPart.FixGroupPosition(AbsolutePosition + linkPart.OffsetPosition, false);
            linkPart.FixOffsetPosition(Vector3.Zero, false);

            linkPart.RotationOffset = worldRot;

            SceneObjectGroup objectGroup = new SceneObjectGroup(linkPart, Scene);
            m_scene.SceneGraph.DelinkPartToScene(objectGroup);

            if (sendEvents)
                linkPart.TriggerScriptChangedEvent(Changed.LINK);

            linkPart.Rezzed = RootPart.Rezzed;


            //This is already set multiple places, no need to do it again
            //HasGroupChanged = true;
            //We need to send this so that we don't have issues with the client not realizing that the prims were unlinked
            ScheduleGroupUpdate(PrimUpdateFlags.ForcedFullUpdate);

            m_ValidgrpOOB = false;
            return objectGroup;
        }

        private void LinkNonRootPart(SceneObjectPart part, Vector3 oldGroupPosition, Quaternion oldGroupRotation,
                                     int linkNum)
        {
            Quaternion WorldRot = oldGroupRotation*part.RotationOffset;

            // first fix from old local to world 
            // position
            Vector3 axPos = part.OffsetPosition;
            axPos *= oldGroupRotation;
            part.SetGroupPosition(oldGroupPosition + axPos);
            //offset
            part.SetRotationOffset(false, WorldRot, false);

            // have it in world coords lets fix other things
            m_scene.SceneGraph.LinkPartToSOG(this, part, linkNum);
            part.CreateSelected = true;

            // now lets move to the new parent frame
            Quaternion rootRotation = m_rootPart.RotationOffset;

            Vector3 pos = part.GroupPosition - AbsolutePosition;
            pos *= Quaternion.Inverse(rootRotation);
            part.SetOffsetPosition(pos);

            Quaternion newRot = Quaternion.Inverse(rootRotation)*WorldRot;
            part.SetRotationOffset(false, newRot, false);
            // caller will tell the rest about this position changes..
        }

        #endregion

        /// <summary>
        ///   Return metadata about a prim (name, description, sale price, etc.)
        /// </summary>
        /// <param name = "client"></param>
        public void GetProperties(IClientAPI client)
        {
            m_rootPart.GetProperties(client);
        }

        public void UpdatePermissions(UUID AgentID, byte field, uint localID,
                                      uint mask, byte addRemTF)
        {
            foreach (SceneObjectPart part in m_partsList)
                part.UpdatePermissions(AgentID, field, localID, mask,
                                       addRemTF);

            HasGroupChanged = true;
        }

        /// <summary>
        ///   If object is physical, apply force to move it around
        ///   If object is not physical, just put it at the resulting location
        /// </summary>
        /// <param name = "offset">Always seems to be 0,0,0, so ignoring</param>
        /// <param name = "pos">New position.  We do the math here to turn it into a force</param>
        /// <param name = "remoteClient"></param>
        public void GrabMovement(Vector3 offset, Vector3 pos, IClientAPI remoteClient)
        {
            if (m_scene.EventManager.TriggerGroupMove(UUID, pos))
            {
                if (m_rootPart.PhysActor != null)
                {
                    if (m_rootPart.PhysActor.IsPhysical)
                    {
                        if (!m_rootPart.BlockGrab && !m_rootPart.BlockGrabObject)
                        {
                            Vector3 grabforce = pos - AbsolutePosition;
                            grabforce = grabforce * m_rootPart.PhysActor.Mass;
                            m_rootPart.PhysActor.AddForce(grabforce, true);
                            // This is outside the above permissions condition
                            // so that if the object is locked the client moving the object
                            // get's it's position on the simulator even if it was the same as before
                            // This keeps the moving user's client in sync with the rest of the world.
                            ScheduleGroupTerseUpdate();
                        }
                    }
                }
            }
        }

        public void NonPhysicalGrabMovement(Vector3 pos)
        {
            AbsolutePosition = pos;
            m_rootPart.ScheduleTerseUpdate();
        }

        /// <summary>
        ///   If object is physical, prepare for spinning torques (set flag to save old orientation)
        /// </summary>
        /// <param name = "remoteClient"></param>
        public void SpinStart(IClientAPI remoteClient)
        {
            if (m_scene.EventManager.TriggerGroupSpinStart(UUID))
            {
                if (m_rootPart.PhysActor != null)
                {
                    if (m_rootPart.PhysActor.IsPhysical)
                    {
                        m_rootPart.IsWaitingForFirstSpinUpdatePacket = true;
                    }
                }
            }
        }

        /// <summary>
        ///   If object is physical, apply torque to spin it around
        /// </summary>
        /// <param name="newOrientation">Rotation.  We do the math here to turn it into a torque</param>
        /// <param name = "remoteClient"></param>
        public void SpinMovement(Quaternion newOrientation, IClientAPI remoteClient)
        {
            // The incoming newOrientation, sent by the client, "seems" to be the 
            // desired target orientation. This needs further verification; in particular, 
            // one would expect that the initial incoming newOrientation should be
            // fairly close to the original prim's physical orientation, 
            // m_rootPart.PhysActor.Orientation. This however does not seem to be the
            // case (might just be an issue with different quaternions representing the
            // same rotation, or it might be a coordinate system issue).
            //
            // Since it's not clear what the relationship is between the PhysActor.Orientation
            // and the incoming orientations sent by the client, we take an alternative approach
            // of calculating the delta rotation between the orientations being sent by the 
            // client. (Since a spin is invoked by ctrl+shift+drag in the client, we expect
            // a steady stream of several new orientations coming in from the client.)
            // This ensures that the delta rotations are being calculated from self-consistent
            // pairs of old/new rotations. Given the delta rotation, we apply a torque around
            // the delta rotation axis, scaled by the object mass times an arbitrary scaling
            // factor (to ensure the resulting torque is not "too strong" or "too weak").
            // 
            // Ideally we need to calculate (probably iteratively) the exact torque or series
            // of torques needed to arrive exactly at the destination orientation. However, since 
            // it is not yet clear how to map the destination orientation (provided by the viewer)
            // into PhysActor orientations (needed by the physics engine), we omit this step. 
            // This means that the resulting torque will at least be in the correct direction, 
            // but it will result in over-shoot or under-shoot of the target orientation.
            // For the end user, this means that ctrl+shift+drag can be used for relative,
            // but not absolute, adjustments of orientation for physical prims.

            if (m_scene.EventManager.TriggerGroupSpin(UUID, newOrientation))
            {
                if (m_rootPart.PhysActor != null)
                {
                    if (m_rootPart.PhysActor.IsPhysical)
                    {
                        if (m_rootPart.IsWaitingForFirstSpinUpdatePacket)
                        {
                            // first time initialization of "old" orientation for calculation of delta rotations
                            m_rootPart.SpinOldOrientation = newOrientation;
                            m_rootPart.IsWaitingForFirstSpinUpdatePacket = false;
                        }
                        else
                        {
                            // save and update old orientation
                            Quaternion old = m_rootPart.SpinOldOrientation;
                            m_rootPart.SpinOldOrientation = newOrientation;
                            //MainConsole.Instance.Error("[SCENE OBJECT GROUP]: Old orientation is " + old);
                            //MainConsole.Instance.Error("[SCENE OBJECT GROUP]: Incoming new orientation is " + newOrientation);

                            // compute difference between previous old rotation and new incoming rotation
                            Quaternion minimalRotationFromQ1ToQ2 = Quaternion.Inverse(old)*newOrientation;

                            float rotationAngle;
                            Vector3 rotationAxis;
                            minimalRotationFromQ1ToQ2.GetAxisAngle(out rotationAxis, out rotationAngle);
                            rotationAxis.Normalize();

                            //MainConsole.Instance.Error("SCENE OBJECT GROUP]: rotation axis is " + rotationAxis);
                            Vector3 spinforce = new Vector3(rotationAxis.X, rotationAxis.Y, rotationAxis.Z);
                            spinforce = (spinforce/8)*m_rootPart.PhysActor.Mass;
                                // 8 is an arbitrary torque scaling factor
                            m_rootPart.PhysActor.AddAngularForce(spinforce, true);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///   Set the name of a prim
        /// </summary>
        /// <param name = "name"></param>
        /// <param name = "localID"></param>
        public void SetPartName(string name, uint localID)
        {
            ISceneChildEntity part = GetChildPart(localID);
            if (part != null)
            {
                part.Name = name;
            }
        }

        public void SetPartDescription(string des, uint localID)
        {
            ISceneChildEntity part = GetChildPart(localID);
            if (part != null)
            {
                part.Description = des;
            }
        }

        public void SetPartText(string text, uint localID)
        {
            SceneObjectPart part = (SceneObjectPart) GetChildPart(localID);
            if (part != null)
            {
                part.SetText(text);
            }
        }

        public void SetPartText(string text, UUID partID)
        {
            SceneObjectPart part = (SceneObjectPart) GetChildPart(partID);
            if (part != null)
            {
                part.SetText(text);
            }
        }

        public string GetPartName(uint localID)
        {
            ISceneChildEntity part = GetChildPart(localID);
            if (part != null)
            {
                return part.Name;
            }
            return String.Empty;
        }

        public string GetPartDescription(uint localID)
        {
            ISceneChildEntity part = GetChildPart(localID);
            if (part != null)
            {
                return part.Description;
            }
            return String.Empty;
        }

        /// <summary>
        ///   Update prim flags for this group.
        /// </summary>
        /// <param name = "localID"></param>
        /// <param name="UsePhysics"></param>
        /// <param name="IsTemporary"></param>
        /// <param name="IsPhantom"></param>
        /// <param name="IsVolumeDetect"></param>
        /// <param name="blocks"></param>
        public void UpdatePrimFlags(uint localID, bool UsePhysics, bool IsTemporary, bool IsPhantom, bool IsVolumeDetect,
                                    ObjectFlagUpdatePacket.ExtraPhysicsBlock[] blocks)
        {
            ISceneChildEntity selectionPart = GetChildPart(localID);

            if (IsTemporary)
            {
                // Remove from database and parcel prim count
                // Temporary objects arn't saved to the database ever, so we don't need to do anything
            }

            if (selectionPart != null)
            {
                foreach (SceneObjectPart part in m_partsList)
                {
                    IOpenRegionSettingsModule WSModule = Scene.RequestModuleInterface<IOpenRegionSettingsModule>();
                    if (WSModule != null)
                    {
                        if (WSModule.MaximumPhysPrimScale == -1)
                            break;

                        if (part.Scale.X > WSModule.MaximumPhysPrimScale ||
                            part.Scale.Y > WSModule.MaximumPhysPrimScale ||
                            part.Scale.Z > WSModule.MaximumPhysPrimScale)
                        {
                            UsePhysics = false; // Reset physics
                            break;
                        }
                    }
                }

                bool needsPhysicalRebuild = ((SceneObjectPart) selectionPart).UpdatePrimFlags(UsePhysics, IsTemporary,
                                                                                              IsPhantom, IsVolumeDetect,
                                                                                              blocks);
#if (!ISWIN)
                foreach (SceneObjectPart part in m_partsList)
                {
                    if (selectionPart != part)
                    {
                        if (needsPhysicalRebuild)
                            part.UpdatePrimFlags(UsePhysics, IsTemporary, IsPhantom, IsVolumeDetect, null);
                        else
                            needsPhysicalRebuild = part.UpdatePrimFlags(UsePhysics, IsTemporary, IsPhantom, IsVolumeDetect, null);
                    }
                }
#else
                foreach (SceneObjectPart part in m_partsList.Where(part => selectionPart != part))
                {
                    if (needsPhysicalRebuild)
                        part.UpdatePrimFlags(UsePhysics, IsTemporary, IsPhantom, IsVolumeDetect, null);
                    else
                        needsPhysicalRebuild = part.UpdatePrimFlags(UsePhysics, IsTemporary, IsPhantom,
                                                                    IsVolumeDetect, null);
                }
#endif
                if (needsPhysicalRebuild)
                    RebuildPhysicalRepresentation(true);
            }
        }

        public void UpdateExtraParam(uint localID, ushort type, bool inUse, byte[] data)
        {
            SceneObjectPart part = (SceneObjectPart) GetChildPart(localID);
            if (part != null)
            {
                part.UpdateExtraParam(type, inUse, data);
            }
        }

        /// <summary>
        ///   Update the texture entry for this part
        /// </summary>
        /// <param name = "localID"></param>
        /// <param name = "textureEntry"></param>
        public void UpdateTextureEntry(uint localID, byte[] textureEntry, bool sendChangedEvent)
        {
            SceneObjectPart part = (SceneObjectPart) GetChildPart(localID);
            if (part != null)
            {
                part.UpdateTextureEntry(textureEntry, sendChangedEvent);
            }
        }

        #endregion

        #region Shape

        ///<summary>
        ///</summary>
        ///<param name = "shapeBlock"></param>
        ///<param name="localID"></param>
        public void UpdateShape(ObjectShapePacket.ObjectDataBlock shapeBlock, uint localID)
        {
            SceneObjectPart part = (SceneObjectPart) GetChildPart(localID);
            if (part != null)
            {
                part.UpdateShape(shapeBlock);
                m_ValidgrpOOB = false;
            }
        }

        #endregion

        #region Resize

        /// <summary>
        ///   Resize the given part
        /// </summary>
        /// <param name = "scale"></param>
        /// <param name = "localID"></param>
        public void Resize(Vector3 scale, uint localID)
        {
            CheckSculptAndLoad();
                //Grab the mesh again if it is a sculpty/mesh as we remove it after the first mesh is built
            IOpenRegionSettingsModule WSModule = Scene.RequestModuleInterface<IOpenRegionSettingsModule>();
            if (WSModule != null)
            {
                if (WSModule.MinimumPrimScale != -1)
                {
                    if (scale.X < WSModule.MinimumPrimScale)
                        scale.X = WSModule.MinimumPrimScale;
                    if (scale.Y < WSModule.MinimumPrimScale)
                        scale.Y = WSModule.MinimumPrimScale;
                    if (scale.Z < WSModule.MinimumPrimScale)
                        scale.Z = WSModule.MinimumPrimScale;
                }

                if (RootPart.PhysActor != null && RootPart.PhysActor.IsPhysical &&
                    WSModule.MaximumPhysPrimScale != -1)
                {
                    if (scale.X > WSModule.MaximumPhysPrimScale)
                        scale.X = WSModule.MaximumPhysPrimScale;
                    if (scale.Y > WSModule.MaximumPhysPrimScale)
                        scale.Y = WSModule.MaximumPhysPrimScale;
                    if (scale.Z > WSModule.MaximumPhysPrimScale)
                        scale.Z = WSModule.MaximumPhysPrimScale;
                }

                if (WSModule.MaximumPrimScale != -1)
                {
                    if (scale.X > WSModule.MaximumPrimScale)
                        scale.X = WSModule.MaximumPrimScale;
                    if (scale.Y > WSModule.MaximumPrimScale)
                        scale.Y = WSModule.MaximumPrimScale;
                    if (scale.Z > WSModule.MaximumPrimScale)
                        scale.Z = WSModule.MaximumPrimScale;
                }
            }

            m_ValidgrpOOB = false;

            SceneObjectPart part = (SceneObjectPart) GetChildPart(localID);
            if (part != null)
            {
                part.Resize(scale);
                if (part.PhysActor != null)
                    part.PhysActor.Size = scale;
                //if (part.UUID != m_rootPart.UUID)

                HasGroupChanged = true;
                ScheduleGroupUpdate(PrimUpdateFlags.Shape);


                //if (part.UUID == m_rootPart.UUID)
                //{
                //if (m_rootPart.PhysActor != null)
                //{
                //m_rootPart.PhysActor.Size =
                //new PhysicsVector(m_rootPart.Scale.X, m_rootPart.Scale.Y, m_rootPart.Scale.Z);
                //m_scene.PhysicsScene.AddPhysicsActorTaint(m_rootPart.PhysActor);
                //}
                //}
            }
        }

        public void GroupResize(Vector3 scale, uint localID)
        {
            SceneObjectPart part = (SceneObjectPart) GetChildPart(localID);
            if (part != null)
            {
                CheckSculptAndLoad();
                    //Grab the mesh again if it is a sculpty/mesh as we remove it after the first mesh is built
                part.IgnoreUndoUpdate = true;

                IOpenRegionSettingsModule WSModule = Scene.RequestModuleInterface<IOpenRegionSettingsModule>();
                if (WSModule != null)
                {
                    if (WSModule.MinimumPrimScale != -1)
                    {
                        if (scale.X < WSModule.MinimumPrimScale)
                            scale.X = WSModule.MinimumPrimScale;
                        if (scale.Y < WSModule.MinimumPrimScale)
                            scale.Y = WSModule.MinimumPrimScale;
                        if (scale.Z < WSModule.MinimumPrimScale)
                            scale.Z = WSModule.MinimumPrimScale;
                    }

                    if (RootPart.PhysActor != null && RootPart.PhysActor.IsPhysical &&
                        WSModule.MaximumPhysPrimScale != -1)
                    {
                        if (scale.X > WSModule.MaximumPhysPrimScale)
                            scale.X = WSModule.MaximumPhysPrimScale;
                        if (scale.Y > WSModule.MaximumPhysPrimScale)
                            scale.Y = WSModule.MaximumPhysPrimScale;
                        if (scale.Z > WSModule.MaximumPhysPrimScale)
                            scale.Z = WSModule.MaximumPhysPrimScale;
                    }

                    if (WSModule.MaximumPrimScale != -1)
                    {
                        if (scale.X > WSModule.MaximumPrimScale)
                            scale.X = WSModule.MaximumPrimScale;
                        if (scale.Y > WSModule.MaximumPrimScale)
                            scale.Y = WSModule.MaximumPrimScale;
                        if (scale.Z > WSModule.MaximumPrimScale)
                            scale.Z = WSModule.MaximumPrimScale;
                    }
                }

                float x = (scale.X/part.Scale.X);
                float y = (scale.Y/part.Scale.Y);
                float z = (scale.Z/part.Scale.Z);

                foreach (SceneObjectPart obPart in m_partsList)
                {
                    obPart.StoreUndoState();
                }
                Vector3 prevScale = part.Scale;
                prevScale.X *= x;
                prevScale.Y *= y;
                prevScale.Z *= z;
                part.Resize(prevScale);

#if (!ISWIN)
                foreach (SceneObjectPart obPart in m_partsList)
                {
                    if (obPart.UUID != m_rootPart.UUID)
                    {
                        obPart.IgnoreUndoUpdate = true;
                        Vector3 currentpos = new Vector3(obPart.OffsetPosition);
                        currentpos.X *= x;
                        currentpos.Y *= y;
                        currentpos.Z *= z;
                        Vector3 newSize = new Vector3(obPart.Scale);
                        newSize.X *= x;
                        newSize.Y *= y;
                        newSize.Z *= z;
                        obPart.Resize(newSize);
                        obPart.UpdateOffSet(currentpos);
                        obPart.IgnoreUndoUpdate = false;
                    }
                }
#else
                foreach (SceneObjectPart obPart in m_partsList.Where(obPart => obPart.UUID != m_rootPart.UUID))
                {
                    obPart.IgnoreUndoUpdate = true;
                    Vector3 currentpos = new Vector3(obPart.OffsetPosition);
                    currentpos.X *= x;
                    currentpos.Y *= y;
                    currentpos.Z *= z;
                    Vector3 newSize = new Vector3(obPart.Scale);
                    newSize.X *= x;
                    newSize.Y *= y;
                    newSize.Z *= z;
                    obPart.Resize(newSize);
                    obPart.UpdateOffSet(currentpos);
                    obPart.IgnoreUndoUpdate = false;
                }
#endif

                if (part.PhysActor != null)
                    part.PhysActor.Size = prevScale;

                part.IgnoreUndoUpdate = false;
                m_rootPart.IgnoreUndoUpdate = false;
                HasGroupChanged = true;
                ScheduleGroupTerseUpdate();
                m_ValidgrpOOB = false;
            }
        }

        #endregion

        #region Position

        private Vector3 m_lastSigInfiniteRegionPos = Vector3.Zero;
        private List<GridRegion> m_nearbyInfiniteRegions = new List<GridRegion>();


        /// <summary>
        ///   Move this scene object
        /// </summary>
        /// <param name = "pos"></param>
        /// <param name="SaveUpdate"></param>
        public void UpdateGroupPosition(Vector3 pos, bool SaveUpdate)
        {
            if (SaveUpdate)
            {
                foreach (SceneObjectPart part in ChildrenList)
                {
                    part.StoreUndoState();
                }
            }
            if (m_scene.EventManager.TriggerGroupMove(UUID, pos))
            {
                if (IsAttachment)
                {
                    m_rootPart.AttachedPos = pos;
                }
                if (RootPart.GetStatusSandbox())
                {
                    if (Util.GetDistanceTo(RootPart.StatusSandboxPos, pos) > 10)
                    {
                        ScriptSetPhysicsStatus(false);
                        pos = AbsolutePosition;
                        IChatModule chatModule = Scene.RequestModuleInterface<IChatModule>();
                        if (chatModule != null)
                            chatModule.SimChat("Hit Sandbox Limit", ChatTypeEnum.DebugChannel, 0x7FFFFFFF,
                                               RootPart.AbsolutePosition, Name, UUID, false, Scene);
                    }
                }
                AbsolutePosition = pos;

                HasGroupChanged = true;
            }

            //we need to do a terse update even if the move wasn't allowed
            // so that the position is reset in the client (the object snaps back)
            ScheduleGroupTerseUpdate();
            if (SitTargetAvatar.Count != 0)
            {
                foreach (UUID clientID in SitTargetAvatar)
                {
                    //Send full updates to the avatar as well so that they move as well
                    IScenePresence SP;
                    if (m_scene.TryGetScenePresence(clientID, out SP))
                    {
                        SP.ParentPosition = AbsolutePosition;
                        SP.SendTerseUpdateToAllClients();
                    }
                }
            }
        }

        public void SetAbsolutePosition(bool UpdatePrimActor, Vector3 val)
        {
            if (!IsAttachment && RootPart != null && RootPart.Shape != null && Scene != null && Scene.RegionInfo != null &&
                RootPart.Shape.State == 0)
            {
                IBackupModule backup = Scene.RequestModuleInterface<IBackupModule>();
                if ((val.X < 0f || val.Y < 0f || val.Z < 0f ||
                     val.X > Scene.RegionInfo.RegionSizeX || val.Y > Scene.RegionInfo.RegionSizeY)
                    && !IsAttachmentCheckFull() && (backup != null && !backup.LoadingPrims))
                    //Don't do it when backup is loading prims, otherwise it lags the region out
                {
                    if (Scene.RegionInfo.InfiniteRegion)
                    {
                        double TargetX = Scene.RegionInfo.RegionLocX + (double) val.X;
                        double TargetY = Scene.RegionInfo.RegionLocY + (double) val.Y;
                        if (m_lastSigInfiniteRegionPos.X - AbsolutePosition.X > 256 ||
                            m_lastSigInfiniteRegionPos.X - AbsolutePosition.X < -256 ||
                            m_lastSigInfiniteRegionPos.Y - AbsolutePosition.Y > 256 ||
                            m_lastSigInfiniteRegionPos.Y - AbsolutePosition.Y < -256)
                        {
                            m_lastSigInfiniteRegionPos = AbsolutePosition;
                            m_nearbyInfiniteRegions = Scene.GridService.GetRegionRange(null,
                                (int)(TargetX - Scene.GridService.GetMaxRegionSize()),
                                (int)(TargetX + 256),
                                (int)(TargetY - Scene.GridService.GetMaxRegionSize()),
                                (int)(TargetY + 256));
                        }
#if (!ISWIN)
                        GridRegion neighborRegion = null;
                        foreach (GridRegion region in m_nearbyInfiniteRegions)
                        {
                            if (TargetX >= region.RegionLocX && TargetY >= region.RegionLocY && TargetX < (region.RegionLocX + region.RegionSizeX) && TargetY < (region.RegionLocY + region.RegionSizeY))
                            {
                                neighborRegion = region;
                                break;
                            }
                        }
#else
                        GridRegion neighborRegion = m_nearbyInfiniteRegions.FirstOrDefault(region => TargetX >= region.RegionLocX && TargetY >= region.RegionLocY && TargetX < (region.RegionLocX + region.RegionSizeX) && TargetY < (region.RegionLocY + region.RegionSizeY));
#endif

                        if (neighborRegion != null)
                        {
                            //Fix the location that the prim will land
                            if (val.X < 0)
                                val.X += neighborRegion.RegionSizeX;
                            if (val.X > Scene.RegionInfo.RegionSizeX)
                                val.X -= Scene.RegionInfo.RegionSizeX;
                            if (val.Y < 0)
                                val.Y += neighborRegion.RegionSizeY;
                            if (val.Y > Scene.RegionInfo.RegionSizeY)
                                val.Y -= Scene.RegionInfo.RegionSizeY;

                            IEntityTransferModule transferModule = Scene.RequestModuleInterface<IEntityTransferModule>();
                            if (transferModule != null)
                            {
                                if (transferModule.CrossGroupToNewRegion(this, val, neighborRegion))
                                {
                                    m_inTransit = false;
                                    return;
                                }
                            }
                        }
                        return;
                    }
                    //If we are headed out of the region, make sure we have a region there
                    IGridRegisterModule neighborService = Scene.RequestModuleInterface<IGridRegisterModule>();
                    if (neighborService != null && !m_inTransit)
                    {
                        m_inTransit = true;
                        List<GridRegion> neighbors = neighborService.GetNeighbors(Scene);

                        int RegionCrossX = Scene.RegionInfo.RegionLocX;
                        int RegionCrossY = Scene.RegionInfo.RegionLocY;

                        if (val.X < 0f)
                            RegionCrossX -= Constants.RegionSize;
                        if (val.Y < 0f)
                            RegionCrossY -= Constants.RegionSize;
                        if (val.X > Scene.RegionInfo.RegionSizeX)
                            RegionCrossX += Scene.RegionInfo.RegionSizeX;
                        if (val.Y > Scene.RegionInfo.RegionSizeY)
                            RegionCrossY += Scene.RegionInfo.RegionSizeY;
#if (!ISWIN)
                        GridRegion neighborRegion = null;
                        foreach (GridRegion region in neighbors)
                        {
                            if (region.RegionLocX == RegionCrossX && region.RegionLocY == RegionCrossY)
                            {
                                neighborRegion = region;
                                break;
                            }
                        }
#else
                        GridRegion neighborRegion = neighbors.FirstOrDefault(region => region.RegionLocX == RegionCrossX && region.RegionLocY == RegionCrossY);
#endif

                        if (neighborRegion != null)
                        {
                            //Fix the location that the prim will land
                            if (val.X < 0)
                                val.X += neighborRegion.RegionSizeX;
                            if (val.X > Scene.RegionInfo.RegionSizeX)
                                val.X -= Scene.RegionInfo.RegionSizeX;
                            if (val.Y < 0)
                                val.Y += neighborRegion.RegionSizeY;
                            if (val.Y > Scene.RegionInfo.RegionSizeY)
                                val.Y -= Scene.RegionInfo.RegionSizeY;

                            IEntityTransferModule transferModule =
                                Scene.RequestModuleInterface<IEntityTransferModule>();
                            if (transferModule != null)
                            {
                                if (transferModule.CrossGroupToNewRegion(this, val, neighborRegion))
                                {
                                    m_inTransit = false;
                                    return;
                                }
                            }
                        }
                        //The group should have crossed a region, but no region was found so return it instead
                        MainConsole.Instance.Info("[SceneObjectGroup]: Returning prim " + Name + " @ " + AbsolutePosition +
                                   " because it has gone out of bounds.");
                        ILLClientInventory inventoryModule = Scene.RequestModuleInterface<ILLClientInventory>();
                        if (inventoryModule != null)
                            inventoryModule.ReturnObjects(new ISceneEntity[] {this}, UUID.Zero);
                        return;
                    }
                }
            }

            if (RootPart != null && RootPart.GetStatusSandbox())
            {
                if (Util.GetDistanceTo(RootPart.StatusSandboxPos, val) > 10)
                {
                    ScriptSetPhysicsStatus(false);
                    if (Scene != null)
                    {
                        IChatModule chatModule = Scene.RequestModuleInterface<IChatModule>();
                        if (chatModule != null)
                            chatModule.SimChat("Hit Sandbox Limit",
                                               ChatTypeEnum.DebugChannel, 0x7FFFFFFF, RootPart.AbsolutePosition, Name, UUID,
                                               false, Scene);
                    }
                    return;
                }
            }
            foreach (SceneObjectPart part in m_partsList)
            {
                part.FixGroupPositionComum(UpdatePrimActor, val, false);
            }

            //if (m_rootPart.PhysActor != null)
            //{
            //m_rootPart.PhysActor.Position =
            //new PhysicsVector(m_rootPart.GroupPosition.X, m_rootPart.GroupPosition.Y,
            //m_rootPart.GroupPosition.Z);
            //m_scene.PhysicsScene.AddPhysicsActorTaint(m_rootPart.PhysActor);
            //}
        }

        /// <summary>
        ///   Update the position of a single part of this scene object
        /// </summary>
        /// <param name = "pos"></param>
        /// <param name = "localID"></param>
        /// <param name="SaveUpdate"></param>
        public void UpdateSinglePosition(Vector3 pos, uint localID, bool SaveUpdate)
        {
            SceneObjectPart part = (SceneObjectPart) GetChildPart(localID);
            if (part != null)
            {
                if (!SaveUpdate)
                    part.IgnoreUndoUpdate = true;
                if (part.UUID == m_rootPart.UUID)
                {
                    UpdateRootPosition(pos);
                }
                else
                {
                    part.UpdateOffSet(pos);
                }
                if (!SaveUpdate)
                    part.IgnoreUndoUpdate = false;

                HasGroupChanged = true;
            }
        }

        ///<summary>
        ///</summary>
        ///<param name = "pos"></param>
        public void UpdateRootPosition(Vector3 pos)
        {
            foreach (SceneObjectPart part in ChildrenList)
            {
                part.StoreUndoState();
            }
            Vector3 newPos = new Vector3(pos.X, pos.Y, pos.Z);
            Vector3 oldPos =
                IsAttachment
                    ? new Vector3(m_rootPart.OffsetPosition.X, m_rootPart.OffsetPosition.Y,
                                  m_rootPart.OffsetPosition.Z)
                    : new Vector3(AbsolutePosition.X + m_rootPart.OffsetPosition.X,
                                  AbsolutePosition.Y + m_rootPart.OffsetPosition.Y,
                                  AbsolutePosition.Z + m_rootPart.OffsetPosition.Z);
            Vector3 diff = oldPos - newPos;
            Vector3 axDiff = new Vector3(diff.X, diff.Y, diff.Z);
            Quaternion partRotation = m_rootPart.RotationOffset;
            axDiff *= Quaternion.Inverse(partRotation);
            diff = axDiff;
            if (IsAttachment)
            {
                m_rootPart.FixOffsetPosition((newPos), false);
                foreach (SceneObjectPart obPart in m_partsList)
                {
                    if (obPart.UUID != m_rootPart.UUID)
                    {
                        obPart.OffsetPosition += diff;
                        obPart.SetGroupPosition(AbsolutePosition);
                    }
                }
            }
            else
            {
                foreach (SceneObjectPart obPart in m_partsList)
                {
                    if (obPart.UUID != m_rootPart.UUID)
                        obPart.FixOffsetPosition((obPart.OffsetPosition + diff), false);
                }

                AbsolutePosition = newPos;
            }

            HasGroupChanged = true;
            ScheduleGroupTerseUpdate();
        }

        public void OffsetForNewRegion(Vector3 offset)
        {
            m_rootPart.FixGroupPosition(offset, false);
        }

        #endregion

        #region Rotation

        ///<summary>
        ///</summary>
        ///<param name = "rot"></param>
        public void UpdateGroupRotationR(Quaternion rot)
        {
            foreach (SceneObjectPart parts in ChildrenList)
            {
                parts.StoreUndoState();
            }
            m_rootPart.UpdateRotation(rot);

            PhysicsObject actor = m_rootPart.PhysActor;
            if (actor != null)
                actor.Orientation = m_rootPart.RotationOffset;

            HasGroupChanged = true;
            ScheduleGroupTerseUpdate();
        }

        ///<summary>
        ///</summary>
        ///<param name = "pos"></param>
        ///<param name = "rot"></param>
        public void UpdateGroupRotationPR(Vector3 pos, Quaternion rot)
        {
            foreach (SceneObjectPart part in ChildrenList)
            {
                part.StoreUndoState();
            }
            m_rootPart.UpdateRotation(rot);

            PhysicsObject actor = m_rootPart.PhysActor;
            if (actor != null)
                actor.Orientation = m_rootPart.RotationOffset;

            AbsolutePosition = pos;
            HasGroupChanged = true;
            ScheduleGroupTerseUpdate();
        }

        ///<summary>
        ///</summary>
        ///<param name = "rot"></param>
        ///<param name = "localID"></param>
        public void UpdateSingleRotation(Quaternion rot, uint localID)
        {
            SceneObjectPart part = (SceneObjectPart) GetChildPart(localID);
            foreach (SceneObjectPart parts in ChildrenList)
            {
                parts.StoreUndoState();
            }
            if (part != null)
            {
                if (part.UUID == m_rootPart.UUID)
                {
                    UpdateRootRotation(rot);
                }
                else
                {
                    part.UpdateRotation(rot);
                }
            }
        }

        ///<summary>
        ///</summary>
        ///<param name = "rot"></param>
        ///<param name="pos"></param>
        ///<param name = "localID"></param>
        public void UpdateSingleRotation(Quaternion rot, Vector3 pos, uint localID)
        {
            SceneObjectPart part = (SceneObjectPart) GetChildPart(localID);
            if (part != null)
            {
                if (part.UUID == m_rootPart.UUID)
                {
                    UpdateRootRotation(rot);
                    AbsolutePosition = pos;
                }
                else
                {
                    part.StoreUndoState();
                    part.IgnoreUndoUpdate = true;
                    part.UpdateRotation(rot);
                    part.FixOffsetPosition(pos, true);
                    part.IgnoreUndoUpdate = false;
                }
            }
        }

        ///<summary>
        ///</summary>
        ///<param name = "rot"></param>
        private void UpdateRootRotation(Quaternion rot)
        {
            Quaternion new_global_group_rot = rot;
            Quaternion old_global_group_rot = m_rootPart.RotationOffset;

            m_rootPart.UpdateRotation(rot);
            if (m_rootPart.PhysActor != null)
                m_rootPart.PhysActor.Orientation = m_rootPart.RotationOffset;

#if (!ISWIN)
            foreach (SceneObjectPart childPrim in m_partsList)
            {
                if (childPrim.UUID != m_rootPart.UUID)
                {
                    childPrim.StoreUndoState();
                    childPrim.IgnoreUndoUpdate = true;

                    // fix rotation
                    // get in world coords
                    Quaternion primsRot = old_global_group_rot*childPrim.RotationOffset;
                    // set new offset as inverse of the one on root
                    // so world is right
                    primsRot = Quaternion.Inverse(new_global_group_rot)*primsRot;
                    // just store it
                    childPrim.SetRotationOffset(false, primsRot, false);

                    // fix position offset
                    Vector3 axPos = childPrim.OffsetPosition;
                    axPos *= old_global_group_rot;
                    axPos *= Quaternion.Inverse(new_global_group_rot);
                    // store it and let physics know about both changes
                    childPrim.FixOffsetPosition(axPos, true);

                    childPrim.ScheduleTerseUpdate();
                    childPrim.IgnoreUndoUpdate = false;
                }
            }
#else
            foreach (SceneObjectPart childPrim in m_partsList.Where(childPrim => childPrim.UUID != m_rootPart.UUID))
            {
                childPrim.StoreUndoState();
                childPrim.IgnoreUndoUpdate = true;

                // fix rotation
                // get in world coords
                Quaternion primsRot = old_global_group_rot * childPrim.RotationOffset;
                // set new offset as inverse of the one on root
                // so world is right
                primsRot = Quaternion.Inverse(new_global_group_rot) * primsRot;
                // just store it
                childPrim.SetRotationOffset(false, primsRot, false);

                // fix position offset
                Vector3 axPos = childPrim.OffsetPosition;
                axPos *= old_global_group_rot;
                axPos *= Quaternion.Inverse(new_global_group_rot);
                // store it and let physics know about both changes
                childPrim.FixOffsetPosition(axPos, true);

                childPrim.ScheduleTerseUpdate();
                childPrim.IgnoreUndoUpdate = false;
            }
#endif

            m_rootPart.ScheduleTerseUpdate();
        }

        #endregion
    }
}