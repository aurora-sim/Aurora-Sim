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
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Servers;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Servers.HttpServer.Interfaces;
using Aurora.Framework.Utilities;
using Aurora.Region;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Messages.Linden;
using OpenMetaverse.StructuredData;
using System;
using System.IO;
using System.Text;
using ExtraParamType = OpenMetaverse.ExtraParamType;

namespace Aurora.Modules.Caps
{
    public class UploadObjectAssetModule : INonSharedRegionModule
    {
        private IScene m_scene;

        #region INonSharedRegionModule Members

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion(IScene pScene)
        {
            m_scene = pScene;
        }

        public void RemoveRegion(IScene scene)
        {
            m_scene.EventManager.OnRegisterCaps -= RegisterCaps;
            m_scene = null;
        }

        public void RegionLoaded(IScene scene)
        {
            m_scene.EventManager.OnRegisterCaps += RegisterCaps;
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "UploadObjectAssetModuleModule"; }
        }

        #endregion

        public OSDMap RegisterCaps(UUID agentID, IHttpServer server)
        {
            OSDMap retVal = new OSDMap();
            retVal["UploadObjectAsset"] = CapsUtil.CreateCAPS("UploadObjectAsset", "");

            server.AddStreamHandler(new GenericStreamHandler("POST", retVal["UploadObjectAsset"],
                                                             delegate(string path, Stream request,
                                                                      OSHttpRequest httpRequest,
                                                                      OSHttpResponse httpResponse)
                                                                 { return ProcessAdd(request, httpResponse, agentID); }));
            return retVal;
        }

        /// <summary>
        ///     Parses ad request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <param name="AgentId"></param>
        /// <returns></returns>
        public byte[] ProcessAdd(Stream request, OSHttpResponse response, UUID AgentId)
        {
            IScenePresence avatar;

            if (!m_scene.TryGetScenePresence(AgentId, out avatar))
                return MainServer.NoResponse;

            OSDMap r = (OSDMap) OSDParser.Deserialize(request);
            UploadObjectAssetMessage message = new UploadObjectAssetMessage();
            try
            {
                message.Deserialize(r);
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Error("[UploadObjectAssetModule]: Error deserializing message " + ex);
                message = null;
            }

            if (message == null)
            {
                response.StatusCode = 400; //501; //410; //404;
                return
                    Encoding.UTF8.GetBytes(
                        "<llsd><map><key>error</key><string>Error parsing Object</string></map></llsd>");
            }

            Vector3 pos = avatar.AbsolutePosition + (Vector3.UnitX*avatar.Rotation);
            Quaternion rot = Quaternion.Identity;
            Vector3 rootpos = Vector3.Zero;

            SceneObjectGroup rootGroup = null;
            SceneObjectGroup[] allparts = new SceneObjectGroup[message.Objects.Length];
            for (int i = 0; i < message.Objects.Length; i++)
            {
                UploadObjectAssetMessage.Object obj = message.Objects[i];
                PrimitiveBaseShape pbs = PrimitiveBaseShape.CreateBox();

                if (i == 0)
                {
                    rootpos = obj.Position;
                }


                // Combine the extraparams data into it's ugly blob again....
                //int bytelength = 0;
                //for (int extparams = 0; extparams < obj.ExtraParams.Length; extparams++)
                //{
                //    bytelength += obj.ExtraParams[extparams].ExtraParamData.Length;
                //}
                //byte[] extraparams = new byte[bytelength];
                //int position = 0;


                //for (int extparams = 0; extparams < obj.ExtraParams.Length; extparams++)
                //{
                //    Buffer.BlockCopy(obj.ExtraParams[extparams].ExtraParamData, 0, extraparams, position,
                //                     obj.ExtraParams[extparams].ExtraParamData.Length);
                //
                //    position += obj.ExtraParams[extparams].ExtraParamData.Length;
                // }

                //pbs.ExtraParams = extraparams;
                foreach (UploadObjectAssetMessage.Object.ExtraParam extraParam in obj.ExtraParams)
                {
                    switch ((ushort) extraParam.Type)
                    {
                        case (ushort) ExtraParamType.Sculpt:
                            Primitive.SculptData sculpt = new Primitive.SculptData(extraParam.ExtraParamData, 0);

                            pbs.SculptEntry = true;

                            pbs.SculptTexture = obj.SculptID;
                            pbs.SculptType = (byte) sculpt.Type;

                            break;
                        case (ushort) ExtraParamType.Flexible:
                            Primitive.FlexibleData flex = new Primitive.FlexibleData(extraParam.ExtraParamData, 0);
                            pbs.FlexiEntry = true;
                            pbs.FlexiDrag = flex.Drag;
                            pbs.FlexiForceX = flex.Force.X;
                            pbs.FlexiForceY = flex.Force.Y;
                            pbs.FlexiForceZ = flex.Force.Z;
                            pbs.FlexiGravity = flex.Gravity;
                            pbs.FlexiSoftness = flex.Softness;
                            pbs.FlexiTension = flex.Tension;
                            pbs.FlexiWind = flex.Wind;
                            break;
                        case (ushort) ExtraParamType.Light:
                            Primitive.LightData light = new Primitive.LightData(extraParam.ExtraParamData, 0);
                            pbs.LightColorA = light.Color.A;
                            pbs.LightColorB = light.Color.B;
                            pbs.LightColorG = light.Color.G;
                            pbs.LightColorR = light.Color.R;
                            pbs.LightCutoff = light.Cutoff;
                            pbs.LightEntry = true;
                            pbs.LightFalloff = light.Falloff;
                            pbs.LightIntensity = light.Intensity;
                            pbs.LightRadius = light.Radius;
                            break;
                        case 0x40:
                            pbs.ReadProjectionData(extraParam.ExtraParamData, 0);
                            break;
                    }
                }
                pbs.PathBegin = (ushort) obj.PathBegin;
                pbs.PathCurve = (byte) obj.PathCurve;
                pbs.PathEnd = (ushort) obj.PathEnd;
                pbs.PathRadiusOffset = (sbyte) obj.RadiusOffset;
                pbs.PathRevolutions = (byte) obj.Revolutions;
                pbs.PathScaleX = (byte) obj.ScaleX;
                pbs.PathScaleY = (byte) obj.ScaleY;
                pbs.PathShearX = (byte) obj.ShearX;
                pbs.PathShearY = (byte) obj.ShearY;
                pbs.PathSkew = (sbyte) obj.Skew;
                pbs.PathTaperX = (sbyte) obj.TaperX;
                pbs.PathTaperY = (sbyte) obj.TaperY;
                pbs.PathTwist = (sbyte) obj.Twist;
                pbs.PathTwistBegin = (sbyte) obj.TwistBegin;
                pbs.HollowShape = (HollowShape) obj.ProfileHollow;
                pbs.PCode = (byte) PCode.Prim;
                pbs.ProfileBegin = (ushort) obj.ProfileBegin;
                pbs.ProfileCurve = (byte) obj.ProfileCurve;
                pbs.ProfileEnd = (ushort) obj.ProfileEnd;
                pbs.Scale = obj.Scale;
                pbs.State = 0;
                SceneObjectPart prim = new SceneObjectPart(AgentId, pbs, obj.Position, obj.Rotation,
                                                           Vector3.Zero, obj.Name)
                                           {
                                               UUID = UUID.Random(),
                                               CreatorID = AgentId,
                                               OwnerID = AgentId,
                                               GroupID = obj.GroupID
                                           };
                prim.LastOwnerID = prim.OwnerID;
                prim.CreationDate = Util.UnixTimeSinceEpoch();
                prim.Name = obj.Name;
                prim.Description = "";

                prim.PayPrice[0] = -2;
                prim.PayPrice[1] = -2;
                prim.PayPrice[2] = -2;
                prim.PayPrice[3] = -2;
                prim.PayPrice[4] = -2;
                Primitive.TextureEntry tmp =
                    new Primitive.TextureEntry(UUID.Parse("89556747-24cb-43ed-920b-47caed15465f"));

                for (int j = 0; j < obj.Faces.Length; j++)
                {
                    UploadObjectAssetMessage.Object.Face face = obj.Faces[j];

                    Primitive.TextureEntryFace primFace = tmp.CreateFace((uint) j);

                    primFace.Bump = face.Bump;
                    primFace.RGBA = face.Color;
                    primFace.Fullbright = face.Fullbright;
                    primFace.Glow = face.Glow;
                    primFace.TextureID = face.ImageID;
                    primFace.Rotation = face.ImageRot;
                    primFace.MediaFlags = ((face.MediaFlags & 1) != 0);

                    primFace.OffsetU = face.OffsetS;
                    primFace.OffsetV = face.OffsetT;
                    primFace.RepeatU = face.ScaleS;
                    primFace.RepeatV = face.ScaleT;
                    primFace.TexMapType = (MappingType) (face.MediaFlags & 6);
                }
                pbs.TextureEntry = tmp.GetBytes();
                prim.Shape = pbs;
                prim.Scale = obj.Scale;

                SceneObjectGroup grp = new SceneObjectGroup(prim, m_scene);

                prim.ParentID = 0;
                if (i == 0)
                    rootGroup = grp;

                grp.AbsolutePosition = obj.Position;
                prim.SetRotationOffset(true, obj.Rotation, true);

                grp.RootPart.IsAttachment = false;

                string reason;
                if (m_scene.Permissions.CanRezObject(1, avatar.UUID, pos, out reason))
                {
                    m_scene.SceneGraph.AddPrimToScene(grp);
                    grp.AbsolutePosition = obj.Position;
                }
                else
                {
                    //Stop now then
                    avatar.ControllingClient.SendAlertMessage("You do not have permission to rez objects here: " +
                                                              reason);
                    return MainServer.NoResponse;
                }
                allparts[i] = grp;
            }

            for (int j = 1; j < allparts.Length; j++)
            {
                rootGroup.LinkToGroup(allparts[j]);
            }

            rootGroup.ScheduleGroupUpdate(PrimUpdateFlags.ForcedFullUpdate);
            pos = m_scene.SceneGraph.GetNewRezLocation(Vector3.Zero, rootpos, UUID.Zero, rot, 1, 1, true,
                                                       allparts[0].GroupScale(), false);

            OSDMap map = new OSDMap();
            map["local_id"] = allparts[0].LocalId;
            return OSDParser.SerializeLLSDXmlBytes(map);
        }

        private string ConvertUintToBytes(uint val)
        {
            byte[] resultbytes = Utils.UIntToBytes(val);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(resultbytes);
            return String.Format("<binary encoding=\"base64\">{0}</binary>", Convert.ToBase64String(resultbytes));
        }
    }
}