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
using System.Collections;
using System.IO;
using System.Text;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework.Capabilities;
using Aurora.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace Aurora.Modules.Caps
{
    public class ObjectAdd : INonSharedRegionModule
    {
        private IScene m_scene;

        #region INonSharedRegionModule Members

        public void Initialise(IConfigSource pSource)
        {
        }

        public void AddRegion(IScene scene)
        {
            m_scene = scene;
            m_scene.EventManager.OnRegisterCaps += RegisterCaps;
        }

        public void RemoveRegion(IScene scene)
        {
        }

        public void RegionLoaded(IScene scene)
        {
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "ObjectAddModule"; }
        }

        #endregion

        public OSDMap RegisterCaps(UUID agentID, IHttpServer server)
        {
            OSDMap retVal = new OSDMap();
            retVal["ObjectAdd"] = CapsUtil.CreateCAPS("ObjectAdd", "");
            server.AddStreamHandler(new GenericStreamHandler("POST", retVal["ObjectAdd"],
                                                       delegate(string path, Stream request,
                                                        OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                       {
                                                           return ProcessAdd(request, agentID);
                                                       }));
            return retVal;
        }

        public byte[] ProcessAdd(Stream request, UUID AgentId)
        {
            IScenePresence avatar;

            if (!m_scene.TryGetScenePresence(AgentId, out avatar))
                return MainServer.BadRequest;


            OSD r = OSDParser.DeserializeLLSDXml(request);
            //UUID session_id = UUID.Zero;
            bool bypass_raycast = false;
            uint everyone_mask = 0;
            uint group_mask = 0;
            uint next_owner_mask = 0;
            uint flags = 0;
            UUID group_id = UUID.Zero;
            int hollow = 0;
            int material = 0;
            int p_code = 0;
            int path_begin = 0;
            int path_curve = 0;
            int path_end = 0;
            int path_radius_offset = 0;
            int path_revolutions = 0;
            int path_scale_x = 0;
            int path_scale_y = 0;
            int path_shear_x = 0;
            int path_shear_y = 0;
            int path_skew = 0;
            int path_taper_x = 0;
            int path_taper_y = 0;
            int path_twist = 0;
            int path_twist_begin = 0;
            int profile_begin = 0;
            int profile_curve = 0;
            int profile_end = 0;
            Vector3 ray_end = Vector3.Zero;
            bool ray_end_is_intersection = false;
            Vector3 ray_start = Vector3.Zero;
            UUID ray_target_id = UUID.Zero;
            Quaternion rotation = Quaternion.Identity;
            Vector3 scale = Vector3.Zero;
            int state = 0;

            if (r.Type != OSDType.Map) // not a proper req
                return MainServer.BadRequest;

            OSDMap rm = (OSDMap) r;

            if (rm.ContainsKey("ObjectData")) //v2
            {
                if (rm["ObjectData"].Type != OSDType.Map)
                    return Encoding.UTF8.GetBytes("Has ObjectData key, but data not in expected format");

                OSDMap ObjMap = (OSDMap) rm["ObjectData"];

                bypass_raycast = ObjMap["BypassRaycast"].AsBoolean();
                everyone_mask = ObjMap["EveryoneMask"];
                flags = ObjMap["Flags"];
                group_mask = ObjMap["GroupMask"];
                material = ObjMap["Material"].AsInteger();
                next_owner_mask = ObjMap["NextOwnerMask"];
                p_code = ObjMap["PCode"].AsInteger();

                if (ObjMap.ContainsKey("Path"))
                {
                    if (ObjMap["Path"].Type != OSDType.Map)
                        return Encoding.UTF8.GetBytes("Has Path key, but data not in expected format");

                    OSDMap PathMap = (OSDMap) ObjMap["Path"];
                    path_begin = PathMap["Begin"].AsInteger();
                    path_curve = PathMap["Curve"].AsInteger();
                    path_end = PathMap["End"].AsInteger();
                    path_radius_offset = PathMap["RadiusOffset"].AsInteger();
                    path_revolutions = PathMap["Revolutions"].AsInteger();
                    path_scale_x = PathMap["ScaleX"].AsInteger();
                    path_scale_y = PathMap["ScaleY"].AsInteger();
                    path_shear_x = PathMap["ShearX"].AsInteger();
                    path_shear_y = PathMap["ShearY"].AsInteger();
                    path_skew = PathMap["Skew"].AsInteger();
                    path_taper_x = PathMap["TaperX"].AsInteger();
                    path_taper_y = PathMap["TaperY"].AsInteger();
                    path_twist = PathMap["Twist"].AsInteger();
                    path_twist_begin = PathMap["TwistBegin"].AsInteger();
                }

                if (ObjMap.ContainsKey("Profile"))
                {
                    if (ObjMap["Profile"].Type != OSDType.Map)
                        return Encoding.UTF8.GetBytes("Has Profile key, but data not in expected format");

                    OSDMap ProfileMap = (OSDMap) ObjMap["Profile"];

                    profile_begin = ProfileMap["Begin"].AsInteger();
                    profile_curve = ProfileMap["Curve"].AsInteger();
                    profile_end = ProfileMap["End"].AsInteger();
                    hollow = ProfileMap["Hollow"].AsInteger();
                }
                ray_end_is_intersection = ObjMap["RayEndIsIntersection"].AsBoolean();

                ray_target_id = ObjMap["RayTargetId"].AsUUID();
                state = ObjMap["State"].AsInteger();
                try
                {
                    ray_end = (ObjMap["RayEnd"]).AsVector3();
                    ray_start = (ObjMap["RayStart"]).AsVector3();
                    scale = (ObjMap["Scale"]).AsVector3();
                    rotation = (ObjMap["Rotation"]).AsQuaternion();
                }
                catch (Exception)
                {
                    return Encoding.UTF8.GetBytes("RayEnd, RayStart, Scale or Rotation wasn't in the expected format");
                }

                if (rm.ContainsKey("AgentData"))
                {
                    if (rm["AgentData"].Type != OSDType.Map)
                    {
                        return Encoding.UTF8.GetBytes("Has AgentData key, but data not in expected format");
                    }

                    OSDMap AgentDataMap = (OSDMap) rm["AgentData"];

                    //session_id = AgentDataMap["SessionId"].AsUUID();
                    group_id = AgentDataMap["GroupId"].AsUUID();
                }
            }
            else
            {
                //v1
                bypass_raycast = rm["bypass_raycast"].AsBoolean();

                everyone_mask = rm["everyone_mask"];
                flags = rm["flags"];
                group_id = rm["group_id"].AsUUID();
                group_mask = rm["group_mask"];
                hollow = rm["hollow"].AsInteger();
                material = rm["material"].AsInteger();
                next_owner_mask = rm["next_owner_mask"];
                hollow = rm["hollow"].AsInteger();
                p_code = rm["p_code"].AsInteger();
                path_begin = rm["path_begin"].AsInteger();
                path_curve = rm["path_curve"].AsInteger();
                path_end = rm["path_end"].AsInteger();
                path_radius_offset = rm["path_radius_offset"].AsInteger();
                path_revolutions = rm["path_revolutions"].AsInteger();
                path_scale_x = rm["path_scale_x"].AsInteger();
                path_scale_y = rm["path_scale_y"].AsInteger();
                path_shear_x = rm["path_shear_x"].AsInteger();
                path_shear_y = rm["path_shear_y"].AsInteger();
                path_skew = rm["path_skew"].AsInteger();
                path_taper_x = rm["path_taper_x"].AsInteger();
                path_taper_y = rm["path_taper_y"].AsInteger();
                path_twist = rm["path_twist"].AsInteger();
                path_twist_begin = rm["path_twist_begin"].AsInteger();
                profile_begin = rm["profile_begin"].AsInteger();
                profile_curve = rm["profile_curve"].AsInteger();
                profile_end = rm["profile_end"].AsInteger();

                ray_end_is_intersection = rm["ray_end_is_intersection"].AsBoolean();

                ray_target_id = rm["ray_target_id"].AsUUID();


                //session_id = rm["session_id"].AsUUID();
                state = rm["state"].AsInteger();
                try
                {
                    ray_end = (rm["ray_end"]).AsVector3();
                    ray_start = (rm["ray_start"]).AsVector3();
                    rotation = (rm["rotation"]).AsQuaternion();
                    scale = (rm["scale"]).AsVector3();
                }
                catch (Exception)
                {
                    return Encoding.UTF8.GetBytes("RayEnd, RayStart, Scale or Rotation wasn't in the expected format");
                }
            }


            Vector3 pos = m_scene.SceneGraph.GetNewRezLocation(ray_start, ray_end, ray_target_id, rotation,
                                                               (bypass_raycast) ? (byte) 1 : (byte) 0,
                                                               (ray_end_is_intersection) ? (byte) 1 : (byte) 0, true,
                                                               scale, false);

            PrimitiveBaseShape pbs = PrimitiveBaseShape.CreateBox();

            pbs.PathBegin = (ushort) path_begin;
            pbs.PathCurve = (byte) path_curve;
            pbs.PathEnd = (ushort) path_end;
            pbs.PathRadiusOffset = (sbyte) path_radius_offset;
            pbs.PathRevolutions = (byte) path_revolutions;
            pbs.PathScaleX = (byte) path_scale_x;
            pbs.PathScaleY = (byte) path_scale_y;
            pbs.PathShearX = (byte) path_shear_x;
            pbs.PathShearY = (byte) path_shear_y;
            pbs.PathSkew = (sbyte) path_skew;
            pbs.PathTaperX = (sbyte) path_taper_x;
            pbs.PathTaperY = (sbyte) path_taper_y;
            pbs.PathTwist = (sbyte) path_twist;
            pbs.PathTwistBegin = (sbyte) path_twist_begin;
            pbs.HollowShape = (HollowShape) hollow;
            pbs.PCode = (byte) p_code;
            pbs.ProfileBegin = (ushort) profile_begin;
            pbs.ProfileCurve = (byte) profile_curve;
            pbs.ProfileEnd = (ushort) profile_end;
            pbs.Scale = scale;
            pbs.State = (byte) state;

            SceneObjectGroup obj = null;

            string reason;
            if (m_scene.Permissions.CanRezObject(1, avatar.UUID, pos, out reason))
            {
                // rez ON the ground, not IN the ground
                // pos.Z += 0.25F;

                obj = (SceneObjectGroup) m_scene.SceneGraph.AddNewPrim(avatar.UUID, group_id, pos, rotation, pbs);
            }
            else
            {
                avatar.ControllingClient.SendAlertMessage("You do not have permission to rez objects here: " + reason);
            }


            if (obj == null)
                return MainServer.BadRequest;

            SceneObjectPart rootpart = obj.RootPart;
            rootpart.Shape = pbs;
            rootpart.Flags |= (PrimFlags) flags;
            rootpart.EveryoneMask = everyone_mask;
            rootpart.GroupID = group_id;
            rootpart.GroupMask = group_mask;
            rootpart.NextOwnerMask = next_owner_mask;
            rootpart.Material = (byte) material;

            OSDMap map = new OSDMap();
            map["local_id"] = obj.LocalId;
            return OSDParser.SerializeLLSDXmlBytes(map);
        }
    }
}