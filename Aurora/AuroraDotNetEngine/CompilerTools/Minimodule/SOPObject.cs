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
using System.Security;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Packets;
using OpenSim.Region.Framework.Interfaces;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.MiniModule
{
    internal class SOPObject : MarshalByRefObject, IObject, IObjectPhysics, IObjectShape, IObjectSound
    {
        private readonly uint m_localID;
        private readonly IScene m_rootScene;
        private readonly ISecurityCredential m_security;

        [Obsolete("Replace with 'credential' constructor [security]")]
        public SOPObject(IScene rootScene, uint localID)
        {
            m_rootScene = rootScene;
            m_localID = localID;
        }

        public SOPObject(IScene rootScene, uint localID, ISecurityCredential credential)
        {
            m_rootScene = rootScene;
            m_localID = localID;
            m_security = credential;
        }

        #region IObject Members

        public bool Exists
        {
            get { return GetSOP() != null; }
        }

        public uint LocalID
        {
            get { return m_localID; }
        }

        public UUID GlobalID
        {
            get { return GetSOP().UUID; }
        }

        public string Name
        {
            get { return GetSOP().Name; }
            set
            {
                if (CanEdit())
                    GetSOP().Name = value;
            }
        }

        public string Description
        {
            get { return GetSOP().Description; }
            set
            {
                if (CanEdit())
                    GetSOP().Description = value;
            }
        }

        public UUID OwnerId
        {
            get { return GetSOP().OwnerID; }
        }

        public UUID CreatorId
        {
            get { return GetSOP().CreatorID; }
        }

        public IObject[] Children
        {
            get
            {
                ISceneChildEntity my = GetSOP();
                int total = my.ParentEntity.PrimCount;

                IObject[] rets = new IObject[total];

                int i = 0;
                foreach (ISceneChildEntity child in my.ParentEntity.ChildrenEntities())
                {
                    rets[i++] = new SOPObject(m_rootScene, child.LocalId, m_security);
                }

                return rets;
            }
        }

        public IObject Root
        {
            get { return new SOPObject(m_rootScene, GetSOP().ParentEntity.RootChild.LocalId, m_security); }
        }

        public IObjectMaterial[] Materials
        {
            get
            {
                ISceneChildEntity sop = GetSOP();
                IObjectMaterial[] rets = new IObjectMaterial[getNumberOfSides(sop)];

                for (int i = 0; i < rets.Length; i++)
                {
                    rets[i] = new SOPObjectMaterial(i, sop);
                }

                return rets;
            }
        }

        public Vector3 Scale
        {
            get { return GetSOP().Scale; }
            set
            {
                if (CanEdit())
                    GetSOP().Scale = value;
            }
        }

        public Quaternion WorldRotation
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public Quaternion OffsetRotation
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public Vector3 WorldPosition
        {
            get { return GetSOP().AbsolutePosition; }
            set
            {
                if (CanEdit())
                {
                    ISceneChildEntity pos = GetSOP();
                    pos.UpdateOffSet(value - pos.AbsolutePosition);
                }
            }
        }

        public Vector3 OffsetPosition
        {
            get { return GetSOP().OffsetPosition; }
            set
            {
                if (CanEdit())
                {
                    GetSOP().FixOffsetPosition(value, false);
                }
            }
        }

        public Vector3 SitTarget
        {
            get { return GetSOP().SitTargetPosition; }
            set
            {
                if (CanEdit())
                {
                    GetSOP().SitTargetPosition = value;
                }
            }
        }

        public string SitTargetText
        {
            get { return GetSOP().SitName; }
            set
            {
                if (CanEdit())
                {
                    GetSOP().SitName = value;
                }
            }
        }

        public string TouchText
        {
            get { return GetSOP().TouchName; }
            set
            {
                if (CanEdit())
                {
                    GetSOP().TouchName = value;
                }
            }
        }

        public string Text
        {
            get { return GetSOP().Text; }
            set
            {
                if (CanEdit())
                {
                    GetSOP().SetText(value, new Vector3(1.0f, 1.0f, 1.0f), 1.0f);
                }
            }
        }

        public bool IsRotationLockedX
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool IsRotationLockedY
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool IsRotationLockedZ
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool IsSandboxed
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool IsImmotile
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool IsAlwaysReturned
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool IsTemporary
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool IsFlexible
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public PhysicsMaterial PhysicsMaterial
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public IObjectPhysics Physics
        {
            get { return this; }
        }

        public IObjectShape Shape
        {
            get { return this; }
        }

        public IObjectInventory Inventory
        {
            get { return new SOPObjectInventory(m_rootScene, GetSOP().TaskInventory); }
        }

        #endregion

        #region Public Functions

        public void Say(string msg)
        {
            if (!CanEdit())
                return;

            ISceneChildEntity sop = GetSOP();
            IChatModule chatModule = m_rootScene.RequestModuleInterface<IChatModule>();
            if (chatModule != null)
                chatModule.SimChat(msg, ChatTypeEnum.Say, 0, sop.AbsolutePosition,
                                   sop.Name, sop.UUID, false, m_rootScene);
        }

        public void Say(string msg, int channel)
        {
            if (!CanEdit())
                return;

            ISceneChildEntity sop = GetSOP();
            IChatModule chatModule = m_rootScene.RequestModuleInterface<IChatModule>();
            if (chatModule != null)
                chatModule.SimChat(msg, ChatTypeEnum.Say, channel, sop.AbsolutePosition,
                                   sop.Name, sop.UUID, false, m_rootScene);
        }

        public void Dialog(UUID avatar, string message, string[] buttons, int chat_channel)
        {
            if (!CanEdit())
                return;

            IDialogModule dm = m_rootScene.RequestModuleInterface<IDialogModule>();

            if (dm == null)
                return;

            if (buttons.Length < 1)
            {
                Say("ERROR: No less than 1 button can be shown", 2147483647);
                return;
            }
            if (buttons.Length > 12)
            {
                Say("ERROR: No more than 12 buttons can be shown", 2147483647);
                return;
            }

            foreach (string button in buttons)
            {
                if (button == String.Empty)
                {
                    Say("ERROR: button label cannot be blank", 2147483647);
                    return;
                }
                if (button.Length > 24)
                {
                    Say("ERROR: button label cannot be longer than 24 characters", 2147483647);
                    return;
                }
            }

            dm.SendDialogToUser(
                avatar, GetSOP().Name, GetSOP().UUID, GetSOP().OwnerID,
                message, new UUID("00000000-0000-2222-3333-100000001000"), chat_channel, buttons);
        }

        #endregion

        #region Supporting Functions

        // Helper functions to understand if object has cut, hollow, dimple, and other affecting number of faces
        private static void hasCutHollowDimpleProfileCut(int primType, PrimitiveBaseShape shape, out bool hasCut,
                                                         out bool hasHollow,
                                                         out bool hasDimple, out bool hasProfileCut)
        {
            if (primType == (int)PrimType.Box
                ||
                primType == (int)PrimType.Cylinder
                ||
                primType == (int)PrimType.Prism)

                hasCut = (shape.ProfileBegin > 0) || (shape.ProfileEnd > 0);
            else
                hasCut = (shape.PathBegin > 0) || (shape.PathEnd > 0);

            hasHollow = shape.ProfileHollow > 0;
            hasDimple = (shape.ProfileBegin > 0) || (shape.ProfileEnd > 0); // taken from llSetPrimitiveParms
            hasProfileCut = hasDimple; // is it the same thing?
        }

        private static int getScriptPrimType(PrimitiveBaseShape primShape)
        {
            if (primShape.SculptEntry)
                return (int)PrimType.Sculpt;
            if ((primShape.ProfileCurve & 0x07) == (byte)ProfileShape.Square)
            {
                if (primShape.PathCurve == (byte)Extrusion.Straight)
                    return (int)PrimType.Box;
                if (primShape.PathCurve == (byte)Extrusion.Curve1)
                    return (int)PrimType.Tube;
            }
            else if ((primShape.ProfileCurve & 0x07) == (byte)ProfileShape.Circle)
            {
                if (primShape.PathCurve == (byte)Extrusion.Straight)
                    return (int)PrimType.Cylinder;
                if (primShape.PathCurve == (byte)Extrusion.Curve1)
                    return (int)PrimType.Torus;
            }
            else if ((primShape.ProfileCurve & 0x07) == (byte)ProfileShape.HalfCircle)
            {
                if (primShape.PathCurve == (byte)Extrusion.Curve1 || primShape.PathCurve == (byte)Extrusion.Curve2)
                    return (int)PrimType.Sphere;
            }
            else if ((primShape.ProfileCurve & 0x07) == (byte)ProfileShape.EquilateralTriangle)
            {
                if (primShape.PathCurve == (byte)Extrusion.Straight)
                    return (int)PrimType.Prism;
                if (primShape.PathCurve == (byte)Extrusion.Curve1)
                    return (int)PrimType.Ring;
            }
            return (int)PrimType.NotPrimitive;
        }

        private static int getNumberOfSides(ISceneChildEntity part)
        {
            int ret;
            bool hasCut;
            bool hasHollow;
            bool hasDimple;
            bool hasProfileCut;

            int primType = getScriptPrimType(part.Shape);
            hasCutHollowDimpleProfileCut(primType, part.Shape, out hasCut, out hasHollow, out hasDimple,
                                         out hasProfileCut);

            switch (primType)
            {
                default:
                case (int)PrimType.Box:
                    ret = 6;
                    if (hasCut) ret += 2;
                    if (hasHollow) ret += 1;
                    break;
                case (int)PrimType.Cylinder:
                    ret = 3;
                    if (hasCut) ret += 2;
                    if (hasHollow) ret += 1;
                    break;
                case (int)PrimType.Prism:
                    ret = 5;
                    if (hasCut) ret += 2;
                    if (hasHollow) ret += 1;
                    break;
                case (int)PrimType.Sphere:
                    ret = 1;
                    if (hasCut) ret += 2;
                    if (hasDimple) ret += 2;
                    if (hasHollow)
                        ret += 1; // GOTCHA: LSL shows 2 additional sides here. 
                    // This has been fixed, but may cause porting issues.
                    break;
                case (int)PrimType.Torus:
                    ret = 1;
                    if (hasCut) ret += 2;
                    if (hasProfileCut) ret += 2;
                    if (hasHollow) ret += 1;
                    break;
                case (int)PrimType.Tube:
                    ret = 4;
                    if (hasCut) ret += 2;
                    if (hasProfileCut) ret += 2;
                    if (hasHollow) ret += 1;
                    break;
                case (int)PrimType.Ring:
                    ret = 3;
                    if (hasCut) ret += 2;
                    if (hasProfileCut) ret += 2;
                    if (hasHollow) ret += 1;
                    break;
                case (int)PrimType.Sculpt:
                    ret = 1;
                    break;
            }
            return ret;
        }

        #endregion

        #region IObjectPhysics

        public bool Enabled
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool Phantom
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public bool PhantomCollisions
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public double Density
        {
            get { return (GetSOP().PhysActor.Mass / Scale.X * Scale.Y / Scale.Z); }
            set { throw new NotImplementedException(); }
        }

        public double Mass
        {
            get { return GetSOP().PhysActor.Mass; }
            set { throw new NotImplementedException(); }
        }

        public double Buoyancy
        {
            get { return GetSOP().PhysActor.Buoyancy; }
            set { GetSOP().PhysActor.Buoyancy = (float)value; }
        }

        public Vector3 GeometricCenter
        {
            get { return Vector3.Zero; }
        }

        public Vector3 CenterOfMass
        {
            get
            {
                Vector3 tmp = GetSOP().PhysActor.CenterOfMass;
                return tmp;
            }
        }

        public Vector3 RotationalVelocity
        {
            get
            {
                Vector3 tmp = GetSOP().PhysActor.RotationalVelocity;
                return tmp;
            }
            set
            {
                if (!CanEdit())
                    return;

                GetSOP().PhysActor.RotationalVelocity = value;
            }
        }

        public Vector3 Velocity
        {
            get
            {
                Vector3 tmp = GetSOP().PhysActor.Velocity;
                return tmp;
            }
            set
            {
                if (!CanEdit())
                    return;

                GetSOP().PhysActor.Velocity = value;
            }
        }

        public Vector3 Torque
        {
            get
            {
                Vector3 tmp = GetSOP().PhysActor.Torque;
                return tmp;
            }
            set
            {
                if (!CanEdit())
                    return;

                GetSOP().PhysActor.Torque = value;
            }
        }

        public Vector3 Acceleration
        {
            get
            {
                Vector3 tmp = GetSOP().PhysActor.Acceleration;
                return tmp;
            }
        }

        public Vector3 Force
        {
            get
            {
                Vector3 tmp = GetSOP().PhysActor.Force;
                return tmp;
            }
            set
            {
                if (!CanEdit())
                    return;

                GetSOP().PhysActor.Force = value;
            }
        }

        public bool FloatOnWater
        {
            set
            {
                if (!CanEdit())
                    return;
                GetSOP().PhysActor.FloatOnWater = value;
            }
        }

        public void AddForce(Vector3 force, bool pushforce)
        {
            if (!CanEdit())
                return;

            GetSOP().PhysActor.AddForce(force, pushforce);
        }

        public void AddAngularForce(Vector3 force, bool pushforce)
        {
            if (!CanEdit())
                return;

            GetSOP().PhysActor.AddAngularForce(force, pushforce);
        }

        #endregion

        #region Implementation of IObjectShape

        private UUID m_sculptMap = UUID.Zero;

        private SculptType m_sculptType = SculptType.Default;

        public UUID SculptMap
        {
            get { return m_sculptMap; }
            set
            {
                if (!CanEdit())
                    return;

                m_sculptMap = value;
                SetPrimitiveSculpted(SculptMap, (byte)SculptType);
            }
        }

        public SculptType SculptType
        {
            get { return m_sculptType; }
            set
            {
                if (!CanEdit())
                    return;

                m_sculptType = value;
                SetPrimitiveSculpted(SculptMap, (byte)SculptType);
            }
        }

        public HoleShape HoleType
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public double HoleSize
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public PrimType PrimType
        {
            get { return (PrimType)getScriptPrimType(GetSOP().Shape); }
            set { throw new NotImplementedException(); }
        }

        private void SetPrimitiveSculpted(UUID map, byte type)
        {
            ObjectShapePacket.ObjectDataBlock shapeBlock = new ObjectShapePacket.ObjectDataBlock();

            ISceneChildEntity part = GetSOP();

            UUID sculptId = map;

            shapeBlock.ObjectLocalID = part.LocalId;
            shapeBlock.PathScaleX = 100;
            shapeBlock.PathScaleY = 150;

            // retain pathcurve
            shapeBlock.PathCurve = part.Shape.PathCurve;

            part.Shape.SetSculptProperties(type, sculptId);
            part.Shape.SculptEntry = true;
            part.UpdateShape(shapeBlock);
        }

        #endregion

        #region Implementation of IObjectSound

        #region IObject Members

        public IObjectSound Sound
        {
            get { return this; }
        }

        #endregion

        #region IObjectSound Members

        public void Play(UUID asset, double volume)
        {
            if (!CanEdit())
                return;

            GetSOP().SendSound(asset.ToString(), volume, true, 0, 0, false, false);
        }

        #endregion

        #endregion

        /// <summary>
        ///   This needs to run very, very quickly.
        ///   It is utilized in nearly every property and method.
        /// </summary>
        /// <returns></returns>
        private ISceneChildEntity GetSOP()
        {
            return m_rootScene.GetSceneObjectPart(m_localID);
        }

        private bool CanEdit()
        {
            if (!m_security.CanEditObject(this))
            {
                throw new SecurityException("Insufficient Permission to edit object with UUID [" + GetSOP().UUID + "]");
            }
            return true;
        }

        #region OnTouch

        private bool _OnTouchActive;

        public event OnTouchDelegate OnTouch
        {
            add
            {
                if (CanEdit())
                {
                    if (!_OnTouchActive)
                    {
                        GetSOP().Flags |= PrimFlags.Touch;
                        _OnTouchActive = true;
                        m_rootScene.EventManager.OnObjectGrab += EventManager_OnObjectGrab;
                    }

                    _OnTouch += value;
                }
            }
            remove
            {
                _OnTouch -= value;

                if (_OnTouch == null)
                {
                    GetSOP().Flags &= ~PrimFlags.Touch;
                    _OnTouchActive = false;
                    m_rootScene.EventManager.OnObjectGrab -= EventManager_OnObjectGrab;
                }
            }
        }

        private event OnTouchDelegate _OnTouch;

        private void EventManager_OnObjectGrab(ISceneChildEntity part, ISceneChildEntity child, Vector3 offsetPos,
                                               IClientAPI remoteClient, SurfaceTouchEventArgs surfaceArgs)
        {
            if (_OnTouchActive && m_localID == part.LocalId)
            {
                TouchEventArgs e = new TouchEventArgs
                                       {
                                           Avatar = new SPAvatar(m_rootScene, remoteClient.AgentId, m_security),
                                           TouchBiNormal = surfaceArgs.Binormal,
                                           TouchMaterialIndex = surfaceArgs.FaceIndex,
                                           TouchNormal = surfaceArgs.Normal,
                                           TouchPosition = surfaceArgs.Position,
                                           TouchST = new Vector2(surfaceArgs.STCoord.X, surfaceArgs.STCoord.Y),
                                           TouchUV = new Vector2(surfaceArgs.UVCoord.X, surfaceArgs.UVCoord.Y)
                                       };

                IObject sender = this;

                if (_OnTouch != null)
                    _OnTouch(sender, e);
            }
        }

        #endregion
    }
}