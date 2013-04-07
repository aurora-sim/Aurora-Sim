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
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Aurora.Framework.ConsoleFramework;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using ProtoBuf;

namespace Aurora.Framework.SceneInfo
{
    public enum ProfileShape : byte
    {
        Circle = 0,
        Square = 1,
        IsometricTriangle = 2,
        EquilateralTriangle = 3,
        RightTriangle = 4,
        HalfCircle = 5
    }

    public enum HollowShape : byte
    {
        Same = 0,
        Circle = 16,
        Square = 32,
        Triangle = 48
    }

    public enum PCodeEnum : byte
    {
        Primitive = 9,
        Avatar = 47,
        Grass = 95,
        NewTree = 111,
        ParticleSystem = 143,
        Tree = 255
    }

    public enum Extrusion : byte
    {
        Straight = 16,
        Curve1 = 32,
        Curve2 = 48,
        Flexible = 128
    }

    [Serializable, ProtoContract(UseProtoMembersOnly = false)]
    public class PrimitiveBaseShape
    {
        private static readonly byte[] DEFAULT_TEXTURE =
            new Primitive.TextureEntry(new UUID("89556747-24cb-43ed-920b-47caed15465f")).GetBytes();

        [XmlIgnore] private float _flexiDrag;
        [XmlIgnore] private bool _flexiEntry;
        [XmlIgnore] private float _flexiForceX;
        [XmlIgnore] private float _flexiForceY;
        [XmlIgnore] private float _flexiForceZ;
        [XmlIgnore] private float _flexiGravity;
        [XmlIgnore] private int _flexiSoftness;
        [XmlIgnore] private float _flexiTension;
        [XmlIgnore] private float _flexiWind;
        private HollowShape _hollowShape;
        [XmlIgnore] private float _lightColorA = 1.0f;
        [XmlIgnore] private float _lightColorB;
        [XmlIgnore] private float _lightColorG;
        [XmlIgnore] private float _lightColorR;
        [XmlIgnore] private float _lightCutoff;
        [XmlIgnore] private bool _lightEntry;
        [XmlIgnore] private float _lightFalloff;
        [XmlIgnore] private float _lightIntensity = 1.0f;
        [XmlIgnore] private float _lightRadius;
        private byte _pCode;

        private ushort _pathBegin;
        private byte _pathCurve;
        private ushort _pathEnd;
        private sbyte _pathRadiusOffset;
        private byte _pathRevolutions;
        private byte _pathScaleX;
        private byte _pathScaleY;
        private byte _pathShearX;
        private byte _pathShearY;
        private sbyte _pathSkew;
        private sbyte _pathTaperX;
        private sbyte _pathTaperY;
        private sbyte _pathTwist;
        private sbyte _pathTwistBegin;
        private ushort _profileBegin;
        private ushort _profileEnd;
        private ushort _profileHollow;
        private ProfileShape _profileShape;
        [XmlIgnore] private float _projectionAmb;

        // Light Projection Filter
        [XmlIgnore] private bool _projectionEntry;
        [XmlIgnore] private float _projectionFOV;
        [XmlIgnore] private float _projectionFocus;
        [XmlIgnore] private UUID _projectionTextureID;
        private Vector3 _scale;
        [XmlIgnore] private byte[] _sculptData = Utils.EmptyBytes;
        [XmlIgnore] private bool _sculptEntry;
        [XmlIgnore] private UUID _sculptTexture;
        [XmlIgnore] private byte _sculptType;
        private byte[] m_textureEntry;

        public PrimitiveBaseShape()
        {
            PCode = (byte) PCodeEnum.Primitive;
            ExtraParams = new byte[1];
            m_textureEntry = DEFAULT_TEXTURE;
        }

        public PrimitiveBaseShape(bool noShape)
        {
            if (noShape)
                return;

            PCode = (byte) PCodeEnum.Primitive;
            ExtraParams = new byte[1];
            m_textureEntry = DEFAULT_TEXTURE;
        }

        /// <summary>
        ///     Construct a PrimitiveBaseShape object from a OpenMetaverse.Primitive object
        /// </summary>
        /// <param name="prim"></param>
        public PrimitiveBaseShape(Primitive prim)
        {
            PCode = (byte) prim.PrimData.PCode;
            ExtraParams = new byte[1];

            State = prim.PrimData.State;
            PathBegin = Primitive.PackBeginCut(prim.PrimData.PathBegin);
            PathEnd = Primitive.PackEndCut(prim.PrimData.PathEnd);
            PathScaleX = Primitive.PackPathScale(prim.PrimData.PathScaleX);
            PathScaleY = Primitive.PackPathScale(prim.PrimData.PathScaleY);
            PathShearX = (byte) Primitive.PackPathShear(prim.PrimData.PathShearX);
            PathShearY = (byte) Primitive.PackPathShear(prim.PrimData.PathShearY);
            PathSkew = Primitive.PackPathTwist(prim.PrimData.PathSkew);
            ProfileBegin = Primitive.PackBeginCut(prim.PrimData.ProfileBegin);
            ProfileEnd = Primitive.PackEndCut(prim.PrimData.ProfileEnd);
            Scale = prim.Scale;
            PathCurve = (byte) prim.PrimData.PathCurve;
            ProfileCurve = (byte) prim.PrimData.ProfileCurve;
            ProfileHollow = Primitive.PackProfileHollow(prim.PrimData.ProfileHollow);
            PathRadiusOffset = Primitive.PackPathTwist(prim.PrimData.PathRadiusOffset);
            PathRevolutions = Primitive.PackPathRevolutions(prim.PrimData.PathRevolutions);
            PathTaperX = Primitive.PackPathTaper(prim.PrimData.PathTaperX);
            PathTaperY = Primitive.PackPathTaper(prim.PrimData.PathTaperY);
            PathTwist = Primitive.PackPathTwist(prim.PrimData.PathTwist);
            PathTwistBegin = Primitive.PackPathTwist(prim.PrimData.PathTwistBegin);

            m_textureEntry = prim.Textures.GetBytes();

            SculptEntry = (prim.Sculpt.Type != OpenMetaverse.SculptType.None);
            SculptData = prim.Sculpt.GetBytes();
            SculptTexture = prim.Sculpt.SculptTexture;
            SculptType = (byte) prim.Sculpt.Type;
        }

        [ProtoMember(1)]
        public byte ProfileCurve
        {
            get { return (byte) ((byte) HollowShape | (byte) ProfileShape); }

            set
            {
                // Handle hollow shape component
                byte hollowShapeByte = (byte) (value & 0xf0);

                if (!Enum.IsDefined(typeof (HollowShape), hollowShapeByte))
                {
                    MainConsole.Instance.WarnFormat(
                        "[SHAPE]: Attempt to set a ProfileCurve with a hollow shape value of {0}, which isn't a valid enum.  Replacing with default shape.",
                        hollowShapeByte);

                    this._hollowShape = HollowShape.Same;
                }
                else
                {
                    this._hollowShape = (HollowShape) hollowShapeByte;
                }

                // Handle profile shape component
                byte profileShapeByte = (byte) (value & 0xf);

                if (!Enum.IsDefined(typeof (ProfileShape), profileShapeByte))
                {
                    MainConsole.Instance.WarnFormat(
                        "[SHAPE]: Attempt to set a ProfileCurve with a profile shape value of {0}, which isn't a valid enum.  Replacing with square.",
                        profileShapeByte);

                    this._profileShape = ProfileShape.Square;
                }
                else
                {
                    this._profileShape = (ProfileShape) profileShapeByte;
                }
            }
        }

        /// <summary>
        ///     Entries to store media textures on each face
        /// </summary>
        /// Do not change this value directly - always do it through an IMoapModule.
        /// Lock before manipulating.
        [ProtoMember(2)]
        public MediaList Media { get; set; }

        [XmlIgnore]
        public Primitive.TextureEntry Textures
        {
            get
            {
                //MainConsole.Instance.DebugFormat("[SHAPE]: get m_textureEntry length {0}", m_textureEntry.Length);
                try
                {
                    return new Primitive.TextureEntry(m_textureEntry, 0, m_textureEntry.Length);
                }
                catch
                {
                }

                MainConsole.Instance.Warn("[SHAPE]: Failed to decode texture, length=" +
                                          ((m_textureEntry != null) ? m_textureEntry.Length : 0));
                return new Primitive.TextureEntry(UUID.Zero);
            }

            set { m_textureEntry = value.GetBytes(); }
        }

        [ProtoMember(3, OverwriteList = true)]
        public byte[] TextureEntry
        {
            get { return m_textureEntry; }

            set
            {
                if (value == null)
                    m_textureEntry = new byte[1];
                else
                    m_textureEntry = value;
            }
        }

        public static PrimitiveBaseShape Default
        {
            get
            {
                PrimitiveBaseShape boxShape = CreateBox();

                boxShape.SetScale(0.5f);

                return boxShape;
            }
        }

        [ProtoMember(4)]
        public ushort PathBegin
        {
            get { return _pathBegin; }
            set { _pathBegin = value; }
        }

        [ProtoMember(5)]
        public byte PathCurve
        {
            get { return _pathCurve; }
            set { _pathCurve = value; }
        }

        [ProtoMember(6)]
        public ushort PathEnd
        {
            get { return _pathEnd; }
            set { _pathEnd = value; }
        }

        [ProtoMember(7)]
        public sbyte PathRadiusOffset
        {
            get { return _pathRadiusOffset; }
            set { _pathRadiusOffset = value; }
        }

        [ProtoMember(8)]
        public byte PathRevolutions
        {
            get { return _pathRevolutions; }
            set { _pathRevolutions = value; }
        }

        [ProtoMember(9)]
        public byte PathScaleX
        {
            get { return _pathScaleX; }
            set { _pathScaleX = value; }
        }

        [ProtoMember(10)]
        public byte PathScaleY
        {
            get { return _pathScaleY; }
            set { _pathScaleY = value; }
        }

        [ProtoMember(11)]
        public byte PathShearX
        {
            get { return _pathShearX; }
            set { _pathShearX = value; }
        }

        [ProtoMember(12)]
        public byte PathShearY
        {
            get { return _pathShearY; }
            set { _pathShearY = value; }
        }

        [ProtoMember(13)]
        public sbyte PathSkew
        {
            get { return _pathSkew; }
            set { _pathSkew = value; }
        }

        [ProtoMember(14)]
        public sbyte PathTaperX
        {
            get { return _pathTaperX; }
            set { _pathTaperX = value; }
        }

        [ProtoMember(15)]
        public sbyte PathTaperY
        {
            get { return _pathTaperY; }
            set { _pathTaperY = value; }
        }

        [ProtoMember(16)]
        public sbyte PathTwist
        {
            get { return _pathTwist; }
            set { _pathTwist = value; }
        }

        [ProtoMember(17)]
        public sbyte PathTwistBegin
        {
            get { return _pathTwistBegin; }
            set { _pathTwistBegin = value; }
        }

        [ProtoMember(18)]
        public byte PCode
        {
            get { return _pCode; }
            set { _pCode = value; }
        }

        [ProtoMember(19)]
        public ushort ProfileBegin
        {
            get { return _profileBegin; }
            set { _profileBegin = value; }
        }

        [ProtoMember(20)]
        public ushort ProfileEnd
        {
            get { return _profileEnd; }
            set { _profileEnd = value; }
        }

        [ProtoMember(21)]
        public ushort ProfileHollow
        {
            get { return _profileHollow; }
            set { _profileHollow = value; }
        }

        [ProtoMember(22)]
        public Vector3 Scale
        {
            get { return _scale; }
            set { _scale = value; }
        }

        [ProtoMember(23)]
        public byte State { get; set; }

        [ProtoMember(24)]
        public ProfileShape ProfileShape
        {
            get { return _profileShape; }
            set { _profileShape = value; }
        }

        [ProtoMember(25)]
        public HollowShape HollowShape
        {
            get { return _hollowShape; }
            set { _hollowShape = value; }
        }

        [ProtoMember(26)]
        public UUID SculptTexture
        {
            get { return _sculptTexture; }
            set { _sculptTexture = value; }
        }

        [ProtoMember(27)]
        public byte SculptType
        {
            get { return _sculptType; }
            set { _sculptType = value; }
        }

        [ProtoMember(28, OverwriteList = true)]
        public byte[] SculptData
        {
            get { return _sculptData; }
            set { _sculptData = value; }
        }

        [ProtoMember(29)]
        public int FlexiSoftness
        {
            get { return _flexiSoftness; }
            set { _flexiSoftness = value; }
        }

        [ProtoMember(30)]
        public float FlexiTension
        {
            get { return _flexiTension; }
            set { _flexiTension = value; }
        }

        [ProtoMember(31)]
        public float FlexiDrag
        {
            get { return _flexiDrag; }
            set { _flexiDrag = value; }
        }

        [ProtoMember(32)]
        public float FlexiGravity
        {
            get { return _flexiGravity; }
            set { _flexiGravity = value; }
        }

        [ProtoMember(33)]
        public float FlexiWind
        {
            get { return _flexiWind; }
            set { _flexiWind = value; }
        }

        [ProtoMember(34)]
        public float FlexiForceX
        {
            get { return _flexiForceX; }
            set { _flexiForceX = value; }
        }

        [ProtoMember(35)]
        public float FlexiForceY
        {
            get { return _flexiForceY; }
            set { _flexiForceY = value; }
        }

        [ProtoMember(36)]
        public float FlexiForceZ
        {
            get { return _flexiForceZ; }
            set { _flexiForceZ = value; }
        }

        [ProtoMember(37)]
        public float LightColorR
        {
            get { return _lightColorR; }
            set { _lightColorR = value; }
        }

        [ProtoMember(38)]
        public float LightColorG
        {
            get { return _lightColorG; }
            set { _lightColorG = value; }
        }

        [ProtoMember(39)]
        public float LightColorB
        {
            get { return _lightColorB; }
            set { _lightColorB = value; }
        }

        [ProtoMember(40)]
        public float LightColorA
        {
            get { return _lightColorA; }
            set { _lightColorA = value; }
        }

        [ProtoMember(41)]
        public float LightRadius
        {
            get { return _lightRadius; }
            set { _lightRadius = value; }
        }

        [ProtoMember(42)]
        public float LightCutoff
        {
            get { return _lightCutoff; }
            set { _lightCutoff = value; }
        }

        [ProtoMember(43)]
        public float LightFalloff
        {
            get { return _lightFalloff; }
            set { _lightFalloff = value; }
        }

        [ProtoMember(44)]
        public float LightIntensity
        {
            get { return _lightIntensity; }
            set { _lightIntensity = value; }
        }

        [ProtoMember(45)]
        public bool FlexiEntry
        {
            get { return _flexiEntry; }
            set { _flexiEntry = value; }
        }

        [ProtoMember(46)]
        public bool LightEntry
        {
            get { return _lightEntry; }
            set { _lightEntry = value; }
        }

        [ProtoMember(47)]
        public bool SculptEntry
        {
            get { return _sculptEntry; }
            set { _sculptEntry = value; }
        }

        [ProtoMember(48)]
        public bool ProjectionEntry
        {
            get { return _projectionEntry; }
            set { _projectionEntry = value; }
        }

        [ProtoMember(49)]
        public UUID ProjectionTextureUUID
        {
            get { return _projectionTextureID; }
            set { _projectionTextureID = value; }
        }

        [ProtoMember(50)]
        public float ProjectionFOV
        {
            get { return _projectionFOV; }
            set { _projectionFOV = value; }
        }

        [ProtoMember(51)]
        public float ProjectionFocus
        {
            get { return _projectionFocus; }
            set { _projectionFocus = value; }
        }

        [ProtoMember(52)]
        public float ProjectionAmbiance
        {
            get { return _projectionAmb; }
            set { _projectionAmb = value; }
        }

        [ProtoMember(53, OverwriteList = true)]
        public byte[] ExtraParams
        {
            get { return ExtraParamsToBytes(); }
            set { ReadInExtraParamsBytes(value); }
        }

        public static PrimitiveBaseShape Create()
        {
            PrimitiveBaseShape shape = new PrimitiveBaseShape();
            return shape;
        }

        public static PrimitiveBaseShape CreateBox()
        {
            PrimitiveBaseShape shape = Create();

            shape._pathCurve = (byte) Extrusion.Straight;
            shape._profileShape = ProfileShape.Square;
            shape._pathScaleX = 100;
            shape._pathScaleY = 100;

            return shape;
        }

        public static PrimitiveBaseShape CreateSphere()
        {
            PrimitiveBaseShape shape = Create();

            shape._pathCurve = (byte) Extrusion.Curve1;
            shape._profileShape = ProfileShape.HalfCircle;
            shape._pathScaleX = 100;
            shape._pathScaleY = 100;

            return shape;
        }

        public static PrimitiveBaseShape CreateCylinder()
        {
            PrimitiveBaseShape shape = Create();

            shape._pathCurve = (byte) Extrusion.Curve1;
            shape._profileShape = ProfileShape.Square;

            shape._pathScaleX = 100;
            shape._pathScaleY = 100;

            return shape;
        }

        public void SetScale(float side)
        {
            _scale = new Vector3(side, side, side);
        }

        public void SetHeigth(float heigth)
        {
            _scale.Z = heigth;
        }

        public void SetRadius(float radius)
        {
            _scale.X = _scale.Y = radius*2f;
        }

        /*void returns need to change of course
        public virtual void GetMesh()
        {
        }*/

        public PrimitiveBaseShape Copy()
        {
            PrimitiveBaseShape copy = (PrimitiveBaseShape) MemberwiseClone();
            if (Media != null)
            {
                MediaList dupeMedia = new MediaList();
                lock (Media)
                {
                    foreach (MediaEntry me in Media)
                    {
                        dupeMedia.Add(me != null ? MediaEntry.FromOSD(me.GetOSD()) : null);
                    }
                }

                copy.Media = dupeMedia;
            }
            return copy;
        }

        public static PrimitiveBaseShape CreateCylinder(float radius, float heigth)
        {
            PrimitiveBaseShape shape = CreateCylinder();

            shape.SetHeigth(heigth);
            shape.SetRadius(radius);

            return shape;
        }

        public void SetPathRange(Vector3 pathRange)
        {
            _pathBegin = Primitive.PackBeginCut(pathRange.X);
            _pathEnd = Primitive.PackEndCut(pathRange.Y);
        }

        public void SetPathRange(float begin, float end)
        {
            _pathBegin = Primitive.PackBeginCut(begin);
            _pathEnd = Primitive.PackEndCut(end);
        }

        public void SetSculptProperties(byte sculptType, UUID SculptTextureUUID)
        {
            _sculptType = sculptType;
            _sculptTexture = SculptTextureUUID;
        }

        public void SetProfileRange(Vector3 profileRange)
        {
            _profileBegin = Primitive.PackBeginCut(profileRange.X);
            _profileEnd = Primitive.PackEndCut(profileRange.Y);
        }

        public void SetProfileRange(float begin, float end)
        {
            _profileBegin = Primitive.PackBeginCut(begin);
            _profileEnd = Primitive.PackEndCut(end);
        }

        public byte[] ExtraParamsToBytes()
        {
            ushort FlexiEP = (ushort) ExtraParamType.Flexible;
            ushort LightEP = (ushort) ExtraParamType.Light;
            ushort SculptEP = (ushort) ExtraParamType.Sculpt;
            ushort ProjectionEP = 0x40;

            int i = 0;
            uint TotalBytesLength = 1; // ExtraParamsNum

            uint ExtraParamsNum = 0;
            if (_flexiEntry)
            {
                ExtraParamsNum++;
                TotalBytesLength += 16; // data
                TotalBytesLength += 2 + 4; // type
            }
            if (_lightEntry)
            {
                ExtraParamsNum++;
                TotalBytesLength += 16; // data
                TotalBytesLength += 2 + 4; // type
            }
            if (_sculptEntry)
            {
                ExtraParamsNum++;
                TotalBytesLength += 17; // data
                TotalBytesLength += 2 + 4; // type
            }
            if (_projectionEntry)
            {
                ExtraParamsNum++;
                TotalBytesLength += 28; // data
                TotalBytesLength += 2 + 4; // type
            }

            byte[] returnbytes = new byte[TotalBytesLength];


            // uint paramlength = ExtraParamsNum;

            // Stick in the number of parameters
            returnbytes[i++] = (byte) ExtraParamsNum;

            if (_flexiEntry)
            {
                byte[] FlexiData = GetFlexiBytes();

                Utils.UInt16ToBytes(FlexiEP, returnbytes, i);
                i += 2;
                //returnbytes[i++] = (byte)(FlexiEP % 256);
                //returnbytes[i++] = (byte)((FlexiEP >> 8) % 256);

                Utils.UIntToBytes((uint) FlexiData.Length, returnbytes, i);
                i += 4;

                Array.Copy(FlexiData, 0, returnbytes, i, FlexiData.Length);
                i += FlexiData.Length;
            }
            if (_lightEntry)
            {
                byte[] LightData = GetLightBytes();

                Utils.UInt16ToBytes(LightEP, returnbytes, i);
                i += 2;

                Utils.UIntToBytes((uint) LightData.Length, returnbytes, i);
                i += 4;

                Array.Copy(LightData, 0, returnbytes, i, LightData.Length);
                i += LightData.Length;
            }
            if (_sculptEntry)
            {
                byte[] SculptData2 = GetSculptBytes();

                Utils.UInt16ToBytes(SculptEP, returnbytes, i);
                i += 2;

                Utils.UIntToBytes((uint) SculptData2.Length, returnbytes, i);
                i += 4;

                Array.Copy(SculptData2, 0, returnbytes, i, SculptData2.Length);
                i += SculptData2.Length;
            }
            if (_projectionEntry)
            {
                byte[] ProjectionData = GetProjectionBytes();

                returnbytes[i++] = (byte) (ProjectionEP%256);
                returnbytes[i++] = (byte) ((ProjectionEP >> 8)%256);
                returnbytes[i++] = (byte) ((ProjectionData.Length)%256);
                returnbytes[i++] = (byte) ((ProjectionData.Length >> 16)%256);
                returnbytes[i++] = (byte) ((ProjectionData.Length >> 20)%256);
                returnbytes[i++] = (byte) ((ProjectionData.Length >> 24)%256);
                Array.Copy(ProjectionData, 0, returnbytes, i, ProjectionData.Length);
                i += ProjectionData.Length;
            }
            if (!_flexiEntry && !_lightEntry && !_sculptEntry && !_projectionEntry)
            {
                byte[] returnbyte = new byte[1];
                returnbyte[0] = 0;
                return returnbyte;
            }


            return returnbytes;
            //MainConsole.Instance.Info("[EXTRAPARAMS]: Length = " + m_shape.ExtraParams.Length.ToString());
        }

        public void ReadInUpdateExtraParam(ushort type, bool inUse, byte[] data)
        {
            const ushort FlexiEP = 0x10;
            const ushort LightEP = 0x20;
            const ushort SculptEP = 0x30;
            const ushort ProjectionEP = 0x40;

            switch (type)
            {
                case FlexiEP:
                    if (!inUse)
                    {
                        _flexiEntry = false;
                        return;
                    }
                    ReadFlexiData(data, 0);
                    break;

                case LightEP:
                    if (!inUse)
                    {
                        _lightEntry = false;
                        return;
                    }
                    ReadLightData(data, 0);
                    break;

                case SculptEP:
                    if (!inUse)
                    {
                        _sculptEntry = false;
                        return;
                    }
                    ReadSculptData(data, 0);
                    break;
                case ProjectionEP:
                    if (!inUse)
                    {
                        _projectionEntry = false;
                        return;
                    }
                    ReadProjectionData(data, 0);
                    break;
            }
        }

        public void ReadInExtraParamsBytes(byte[] data)
        {
            if (data == null || data.Length == 1)
                return;

            const ushort FlexiEP = 0x10;
            const ushort LightEP = 0x20;
            const ushort SculptEP = 0x30;
            const ushort ProjectionEP = 0x40;

            bool lGotFlexi = false;
            bool lGotLight = false;
            bool lGotSculpt = false;
            bool lGotFilter = false;

            int i = 0;
            byte extraParamCount = 0;
            if (data.Length > 0)
            {
                extraParamCount = data[i++];
            }


            for (int k = 0; k < extraParamCount; k++)
            {
                ushort epType = Utils.BytesToUInt16(data, i);

                i += 2;
                // uint paramLength = Helpers.BytesToUIntBig(data, i);

                i += 4;
                switch (epType)
                {
                    case FlexiEP:
                        ReadFlexiData(data, i);
                        i += 16;
                        lGotFlexi = true;
                        break;

                    case LightEP:
                        ReadLightData(data, i);
                        i += 16;
                        lGotLight = true;
                        break;

                    case SculptEP:
                        ReadSculptData(data, i);
                        i += 17;
                        lGotSculpt = true;
                        break;
                    case ProjectionEP:
                        ReadProjectionData(data, i);
                        i += 28;
                        lGotFilter = true;
                        break;
                }
            }

            if (!lGotFlexi)
                _flexiEntry = false;
            if (!lGotLight)
                _lightEntry = false;
            if (!lGotSculpt)
                _sculptEntry = false;
            if (!lGotFilter)
                _projectionEntry = false;
        }

        public void ReadSculptData(byte[] data, int pos)
        {
            byte[] SculptTextureUUID = new byte[16];
            UUID SculptUUID = UUID.Zero;
            byte SculptTypel = data[16 + pos];

            if (data.Length + pos >= 17)
            {
                _sculptEntry = true;
                SculptTextureUUID = new byte[16];
                SculptTypel = data[16 + pos];
                Array.Copy(data, pos, SculptTextureUUID, 0, 16);
                SculptUUID = new UUID(SculptTextureUUID, 0);
            }
            else
            {
                _sculptEntry = false;
                SculptUUID = UUID.Zero;
                SculptTypel = 0x00;
            }

            if (_sculptEntry)
            {
                if (_sculptType != 1 && _sculptType != 2 && _sculptType != 3 && _sculptType != 4)
                    _sculptType = 4;
            }
            _sculptTexture = SculptUUID;
            _sculptType = SculptTypel;
            //MainConsole.Instance.Info("[SCULPT]:" + SculptUUID.ToString());
        }

        public byte[] GetSculptBytes()
        {
            byte[] data = new byte[17];

            _sculptTexture.GetBytes().CopyTo(data, 0);
            data[16] = _sculptType;

            return data;
        }

        public void ReadFlexiData(byte[] data, int pos)
        {
            if (data.Length - pos >= 16)
            {
                _flexiEntry = true;
                _flexiSoftness = ((data[pos] & 0x80) >> 6) | ((data[pos + 1] & 0x80) >> 7);

                _flexiTension = (data[pos++] & 0x7F)/10.0f;
                _flexiDrag = (data[pos++] & 0x7F)/10.0f;
                _flexiGravity = (data[pos++]/10.0f) - 10.0f;
                _flexiWind = data[pos++]/10.0f;
                Vector3 lForce = new Vector3(data, pos);
                _flexiForceX = lForce.X;
                _flexiForceY = lForce.Y;
                _flexiForceZ = lForce.Z;
            }
            else
            {
                _flexiEntry = false;
                _flexiSoftness = 0;

                _flexiTension = 0.0f;
                _flexiDrag = 0.0f;
                _flexiGravity = 0.0f;
                _flexiWind = 0.0f;
                _flexiForceX = 0f;
                _flexiForceY = 0f;
                _flexiForceZ = 0f;
            }
        }

        public byte[] GetFlexiBytes()
        {
            byte[] data = new byte[16];
            int i = 0;

            // Softness is packed in the upper bits of tension and drag
            data[i] = (byte) ((_flexiSoftness & 2) << 6);
            data[i + 1] = (byte) ((_flexiSoftness & 1) << 7);

            data[i++] |= (byte) ((byte) (_flexiTension*10.01f) & 0x7F);
            data[i++] |= (byte) ((byte) (_flexiDrag*10.01f) & 0x7F);
            data[i++] = (byte) ((_flexiGravity + 10.0f)*10.01f);
            data[i++] = (byte) (_flexiWind*10.01f);
            Vector3 lForce = new Vector3(_flexiForceX, _flexiForceY, _flexiForceZ);
            lForce.GetBytes().CopyTo(data, i);

            return data;
        }

        public void ReadLightData(byte[] data, int pos)
        {
            if (data.Length - pos >= 16)
            {
                _lightEntry = true;
                Color4 lColor = new Color4(data, pos, false);
                _lightIntensity = lColor.A;
                _lightColorA = 1f;
                _lightColorR = lColor.R;
                _lightColorG = lColor.G;
                _lightColorB = lColor.B;

                _lightRadius = Utils.BytesToFloat(data, pos + 4);
                _lightCutoff = Utils.BytesToFloat(data, pos + 8);
                _lightFalloff = Utils.BytesToFloat(data, pos + 12);
            }
            else
            {
                _lightEntry = false;
                _lightColorA = 1f;
                _lightColorR = 0f;
                _lightColorG = 0f;
                _lightColorB = 0f;
                _lightRadius = 0f;
                _lightCutoff = 0f;
                _lightFalloff = 0f;
                _lightIntensity = 0f;
            }
        }

        public byte[] GetLightBytes()
        {
            byte[] data = new byte[16];

            try
            {
                if (_lightIntensity > 1)
                    _lightIntensity = 1;

                // Alpha channel in color is intensity
                Color4 tmpColor = new Color4(_lightColorR, _lightColorG, _lightColorB, _lightIntensity);

                tmpColor.GetBytes().CopyTo(data, 0);
                Utils.FloatToBytes(_lightRadius).CopyTo(data, 4);
                Utils.FloatToBytes(_lightCutoff).CopyTo(data, 8);
                Utils.FloatToBytes(_lightFalloff).CopyTo(data, 12);
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Warn("Error GetLightBytes: " + ex);
            }

            return data;
        }

        public void ReadProjectionData(byte[] data, int pos)
        {
            byte[] ProjectionTextureUUID2 = new byte[16];

            if (data.Length - pos >= 28)
            {
                _projectionEntry = true;
                Array.Copy(data, pos, ProjectionTextureUUID2, 0, 16);
                _projectionTextureID = new UUID(ProjectionTextureUUID2, 0);

                _projectionFOV = Utils.BytesToFloat(data, pos + 16);
                _projectionFocus = Utils.BytesToFloat(data, pos + 20);
                _projectionAmb = Utils.BytesToFloat(data, pos + 24);
            }
            else
            {
                _projectionEntry = false;
                _projectionTextureID = UUID.Zero;
                _projectionFOV = 0f;
                _projectionFocus = 0f;
                _projectionAmb = 0f;
            }
        }

        public byte[] GetProjectionBytes()
        {
            byte[] data = new byte[28];

            _projectionTextureID.GetBytes().CopyTo(data, 0);
            Utils.FloatToBytes(_projectionFOV).CopyTo(data, 16);
            Utils.FloatToBytes(_projectionFocus).CopyTo(data, 20);
            Utils.FloatToBytes(_projectionAmb).CopyTo(data, 24);

            return data;
        }


        /// <summary>
        ///     Creates a OpenMetaverse.Primitive and populates it with converted PrimitiveBaseShape values
        /// </summary>
        /// <returns></returns>
        public Primitive ToOmvPrimitive()
        {
            // position and rotation defaults here since they are not available in PrimitiveBaseShape
            return ToOmvPrimitive(new Vector3(0.0f, 0.0f, 0.0f),
                                  new Quaternion(0.0f, 0.0f, 0.0f, 1.0f));
        }


        /// <summary>
        ///     Creates a OpenMetaverse.Primitive and populates it with converted PrimitiveBaseShape values
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        public Primitive ToOmvPrimitive(Vector3 position, Quaternion rotation)
        {
            Primitive prim = new Primitive {Scale = this.Scale, Position = position, Rotation = rotation};


            if (this.SculptEntry)
            {
                prim.Sculpt = new Primitive.SculptData
                                  {Type = (SculptType) this.SculptType, SculptTexture = this.SculptTexture};
            }

            prim.PrimData.PathShearX = this.PathShearX < 128 ? this.PathShearX*0.01f : (this.PathShearX - 256)*0.01f;
            prim.PrimData.PathShearY = this.PathShearY < 128 ? this.PathShearY*0.01f : (this.PathShearY - 256)*0.01f;
            prim.PrimData.PathBegin = this.PathBegin*2.0e-5f;
            prim.PrimData.PathEnd = 1.0f - this.PathEnd*2.0e-5f;

            prim.PrimData.PathScaleX = (200 - this.PathScaleX)*0.01f;
            prim.PrimData.PathScaleY = (200 - this.PathScaleY)*0.01f;

            prim.PrimData.PathTaperX = this.PathTaperX*0.01f;
            prim.PrimData.PathTaperY = this.PathTaperY*0.01f;

            prim.PrimData.PathTwistBegin = this.PathTwistBegin*0.01f;
            prim.PrimData.PathTwist = this.PathTwist*0.01f;

            prim.PrimData.ProfileBegin = this.ProfileBegin*2.0e-5f;
            prim.PrimData.ProfileEnd = 1.0f - this.ProfileEnd*2.0e-5f;
            prim.PrimData.ProfileHollow = this.ProfileHollow*2.0e-5f;

            prim.PrimData.profileCurve = this.ProfileCurve;
            prim.PrimData.ProfileHole = (HoleType) this.HollowShape;

            prim.PrimData.PathCurve = (PathCurve) this.PathCurve;
            prim.PrimData.PathRadiusOffset = 0.01f*this.PathRadiusOffset;
            prim.PrimData.PathRevolutions = 1.0f + 0.015f*this.PathRevolutions;
            prim.PrimData.PathSkew = 0.01f*this.PathSkew;

            prim.PrimData.PCode = OpenMetaverse.PCode.Prim;
            prim.PrimData.State = 0;

            if (this.FlexiEntry)
            {
                prim.Flexible = new Primitive.FlexibleData
                                    {
                                        Drag = this.FlexiDrag,
                                        Force = new Vector3(this.FlexiForceX, this.FlexiForceY, this.FlexiForceZ),
                                        Gravity = this.FlexiGravity,
                                        Softness = this.FlexiSoftness,
                                        Tension = this.FlexiTension,
                                        Wind = this.FlexiWind
                                    };
            }

            if (this.LightEntry)
            {
                prim.Light = new Primitive.LightData
                                 {
                                     Color =
                                         new Color4(this.LightColorR, this.LightColorG, this.LightColorB,
                                                    this.LightColorA),
                                     Cutoff = this.LightCutoff,
                                     Falloff = this.LightFalloff,
                                     Intensity = this.LightIntensity,
                                     Radius = this.LightRadius
                                 };
            }

            prim.Textures = this.Textures;

            prim.Properties = new Primitive.ObjectProperties
                                  {
                                      Name = "Primitive",
                                      Description = "",
                                      CreatorID = UUID.Zero,
                                      GroupID = UUID.Zero,
                                      OwnerID = UUID.Zero,
                                      Permissions = new Permissions(),
                                      SalePrice = 10,
                                      SaleType = new SaleType()
                                  };

            return prim;
        }

        #region Nested type: MediaList

        /// <summary>
        ///     Encapsulates a list of media entries.
        /// </summary>
        /// This class is necessary because we want to replace auto-serialization of MediaEntry with something more
        /// OSD like and less vulnerable to change.
        public class MediaList : List<MediaEntry>, IXmlSerializable
        {
            public const string MEDIA_TEXTURE_TYPE = "sl";

            public MediaList()
            {
            }

            public MediaList(IEnumerable<MediaEntry> collection) : base(collection)
            {
            }

            public MediaList(int capacity) : base(capacity)
            {
            }

            #region IXmlSerializable Members

            public XmlSchema GetSchema()
            {
                return null;
            }

            public void WriteXml(XmlWriter writer)
            {
                writer.WriteRaw(ToXml());
            }

            public void ReadXml(XmlReader reader)
            {
                if (reader.IsEmptyElement)
                    return;

                ReadXml(reader.ReadInnerXml());
            }

            #endregion

            public string ToXml()
            {
                lock (this)
                {
                    using (StringWriter sw = new StringWriter())
                    {
                        using (XmlTextWriter xtw = new XmlTextWriter(sw))
                        {
                            xtw.WriteStartElement("OSMedia");
                            xtw.WriteAttributeString("type", MEDIA_TEXTURE_TYPE);
                            xtw.WriteAttributeString("version", "0.1");

                            OSDArray meArray = new OSDArray();

                            foreach (OSD osd in this.Select(me => (null == me ? new OSD() : me.GetOSD())))
                            {
                                meArray.Add(osd);
                            }

                            xtw.WriteStartElement("OSData");
                            xtw.WriteRaw(OSDParser.SerializeLLSDXmlString(meArray));
                            xtw.WriteEndElement();

                            xtw.WriteEndElement();

                            xtw.Flush();
                            return sw.ToString();
                        }
                    }
                }
            }

            public static MediaList FromXml(string rawXml)
            {
                MediaList ml = new MediaList();
                ml.ReadXml(rawXml);
                return ml;
            }

            public void ReadXml(string rawXml)
            {
                using (StringReader sr = new StringReader(rawXml))
                {
                    using (XmlTextReader xtr = new XmlTextReader(sr))
                    {
                        xtr.MoveToContent();

                        string type = xtr.GetAttribute("type");
                        //MainConsole.Instance.DebugFormat("[MOAP]: Loaded media texture entry with type {0}", type);

                        if (type != MEDIA_TEXTURE_TYPE)
                            return;

                        xtr.ReadStartElement("OSMedia");

                        OSDArray osdMeArray = (OSDArray) OSDParser.DeserializeLLSDXml(xtr.ReadInnerXml());

                        foreach (
                            MediaEntry me in
                                osdMeArray.Select(
                                    osdMe => (osdMe is OSDMap ? MediaEntry.FromOSD(osdMe) : new MediaEntry())))
                        {
                            Add(me);
                        }

                        xtr.ReadEndElement();
                    }
                }
            }
        }

        #endregion

        public ulong GetMeshKey (Vector3 size, float lod)
        {
            ulong hash = 5381;

            hash = djb2(hash, PathCurve);
            hash = djb2(hash, (byte)((byte)HollowShape | (byte)ProfileShape));
            hash = djb2(hash, PathBegin);
            hash = djb2(hash, PathEnd);
            hash = djb2(hash, PathScaleX);
            hash = djb2(hash, PathScaleY);
            hash = djb2(hash, PathShearX);
            hash = djb2(hash, PathShearY);
            hash = djb2(hash, (byte)PathTwist);
            hash = djb2(hash, (byte)PathTwistBegin);
            hash = djb2(hash, (byte)PathRadiusOffset);
            hash = djb2(hash, (byte)PathTaperX);
            hash = djb2(hash, (byte)PathTaperY);
            hash = djb2(hash, PathRevolutions);
            hash = djb2(hash, (byte)PathSkew);
            hash = djb2(hash, ProfileBegin);
            hash = djb2(hash, ProfileEnd);
            hash = djb2(hash, ProfileHollow);

            // TODO: Separate scale out from the primitive shape data (after
            // scaling is supported at the physics engine level)
            byte[] scaleBytes = size.GetBytes();
            hash = scaleBytes.Aggregate(hash, djb2);

            // Include LOD in hash, accounting for endianness
            byte[] lodBytes = new byte[4];
            Buffer.BlockCopy(BitConverter.GetBytes(lod), 0, lodBytes, 0, 4);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(lodBytes, 0, 4);
            }
            hash = lodBytes.Aggregate(hash, djb2);

            // include sculpt UUID
            if (SculptEntry)
            {
                scaleBytes = SculptTexture.GetBytes();
                hash = scaleBytes.Aggregate(hash, djb2);
            }

            return hash;
        }

        private ulong djb2(ulong hash, byte c)
        {
            return ((hash << 5) + hash) + c;
        }

        private ulong djb2(ulong hash, ushort c)
        {
            hash = ((hash << 5) + hash) + ((byte)c);
            return ((hash << 5) + hash) + (ulong)(c >> 8);
        }
    }
}