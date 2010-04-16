/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Caps=OpenSim.Framework.Capabilities.Caps;
using Aurora.DataManager;
using Aurora.Framework;

namespace OpenSim.Region.CoreModules.Avatar.ObjectCaps
{
    public class ObjectAdd : IRegionModule
    {
        private IGenericData GenericData = null;
        private IRegionData RegionData = null;
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Scene m_scene;
        #region IRegionModule Members

        public void Initialise(Scene pScene, IConfigSource pSource)
        {
            m_scene = pScene;
            m_scene.EventManager.OnRegisterCaps += RegisterCaps;
        }

        public void PostInitialise()
        {
            RegionData = Aurora.DataManager.DataManager.GetRegionPlugin();
            GenericData = Aurora.DataManager.DataManager.GetGenericPlugin();
        }

        public void RegisterCaps(UUID agentID, Caps caps)
        {
            UUID capuuid = UUID.Random();
            
            m_log.InfoFormat("[OBJECTADD]: {0}", "/CAPS/OA/" + capuuid + "/");

            caps.RegisterHandler("ObjectAdd",
                                 new RestHTTPHandler("POST", "/CAPS/OA/" + capuuid + "/",
                                                       delegate(Hashtable m_dhttpMethod)
                                                       {
                                                           return ProcessAdd(m_dhttpMethod, agentID, caps);
                                                       }));
            
            caps.RegisterHandler("ObjectMedia",
                                new RestHTTPHandler("POST", "/CAPS/ObjectMedia/" + capuuid + "/",
                                                      delegate(Hashtable m_dhttpMethod)
                                                      {
                                                          return ProcessObjectMedia(m_dhttpMethod, capuuid);
                                                      }));

            caps.RegisterHandler("ObjectMediaNavigate",
                                new RestHTTPHandler("POST", "/CAPS/ObjectMediaNavigate/" + capuuid + "/",
                                                      delegate(Hashtable m_dhttpMethod)
                                                      {
                                                          return ProcessObjectMediaNavigate(m_dhttpMethod, capuuid);
                                                      }));
        }

        private Hashtable ProcessObjectMediaNavigate(Hashtable mDhttpMethod, UUID capuuid)
        {
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            return responsedata;
        }

        private Hashtable ProcessObjectMedia(Hashtable mDhttpMethod, UUID objectID)
        {
            OSD r = OSDParser.DeserializeLLSDXml((string)mDhttpMethod["requestbody"]);
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            OSDMap rm = (OSDMap)r;

            if (rm.ContainsKey("verb"))
            {
                #region Get
                if (rm["verb"].ToString() == "GET")
                {
                    OSDMap MainMap = new OSDMap();
                    OSD osd = new OSD();
                    ObjectMediaURLInfo[] infos = RegionData.getObjectMediaInfo(rm["object_id"].ToString());
                    OSDArray array = new OSDArray(6);
                    foreach (ObjectMediaURLInfo info in infos)
                    {
                        if (info == null)
                        {
                            array.Add(new OSD());
                            continue;
                        }

                        OSDMap mediadataMap = new OSDMap();
                        osd = new OSDString(info.alt_image_enable);
                        mediadataMap.Add("alt_image_enable", osd);

                        osd = new OSDBoolean(info.auto_loop);
                        mediadataMap.Add("auto_loop", osd);

                        osd = new OSDBoolean(info.auto_play);
                        mediadataMap.Add("auto_play", osd);

                        osd = new OSDBoolean(info.auto_scale);
                        mediadataMap.Add("auto_scale", osd);

                        osd = new OSDBoolean(info.auto_zoom);
                        mediadataMap.Add("auto_zoom", osd);

                        osd = new OSDInteger(info.controls);
                        mediadataMap.Add("controls", osd);

                        osd = new OSDString(info.current_url);
                        mediadataMap.Add("current_url", osd);

                        osd = new OSDBoolean(info.first_click_interact);
                        mediadataMap.Add("first_click_interact", osd);

                        osd = new OSDInteger(info.height_pixels);
                        mediadataMap.Add("height_pixels", osd);

                        osd = new OSDString(info.home_url);
                        mediadataMap.Add("home_url", osd);

                        osd = new OSDInteger(info.perms_control);
                        mediadataMap.Add("perms_control", osd);

                        osd = new OSDBoolean(info.whitelist_enable);
                        mediadataMap.Add("whitelist_enable", osd);

                        osd = new OSDInteger(info.width_pixels);
                        mediadataMap.Add("width_pixels", osd);
                        array.Add(mediadataMap);
                    }
                    osd = new OSDString(objectID.ToString());
                    MainMap.Add("object_id", osd);

                    MainMap.Add("object_media_data", array);

                    osd = new OSDString("x-mv:000000000" + "1"/*infos[1].object_media_version*/+"/"+new Guid().ToString());
                    MainMap.Add("object_media_version", osd);


                    string response = OSDParser.SerializeLLSDXmlString(MainMap);
                    responsedata["str_response_string"] = response; //String.Format(@"<llsd><map><key>object_id</key><uuid>{0}</uuid><key>object_media_data</key><array><map><key>alt_image_enable</key><boolean>0</boolean><key>auto_loop</key><boolean>0</boolean><key>auto_play</key><boolean>1</boolean><key>auto_scale</key><boolean>1</boolean><key>auto_zoom</key><boolean>0</boolean><key>controls</key><integer>0</integer><key>current_url</key><string>http://v01.wwweb3d.net/dahliaUnityPlayer/dahliaWebPlayer03.html</string><key>first_click_interact</key><boolean>0</boolean><key>height_pixels</key><integer>0</integer><key>home_url</key><string>http://v01.wwweb3d.net/dahliaUnityPlayer/dahliaWebPlayer03.html</string><key>perms_control</key><integer>7</integer><key>perms_interact</key><integer>7</integer><key>whitelist_enable</key><boolean>0</boolean><key>width_pixels</key><integer>0</integer></map><map><key>alt_image_enable</key><boolean>0</boolean><key>auto_loop</key><boolean>0</boolean><key>auto_play</key><boolean>1</boolean><key>auto_scale</key><boolean>1</boolean><key>auto_zoom</key><boolean>0</boolean><key>controls</key><integer>0</integer><key>current_url</key><string>http://www.google.com/</string><key>first_click_interact</key><boolean>0</boolean><key>height_pixels</key><integer>0</integer><key>home_url</key><string>http://www.google.com</string><key>perms_control</key><integer>7</integer><key>perms_interact</key><integer>7</integer><key>whitelist_enable</key><boolean>0</boolean><key>width_pixels</key><integer>0</integer></map><undef /><undef /><undef /><undef /></array><key>object_media_version</key><string>x-mv:0000000042/79e7c4ad-3361-4736-bced-1f72e6c3dbd4</string></map></llsd>",uuid.ToString());
                }
                #endregion
                #region Update
                else if (rm["verb"].ToString() == "UPDATE")
                {
                    OSDArray media_data_map = (OSDArray)rm["object_media_data"];
                    int i = 0;
                    foreach (OSD osd in media_data_map)
                    {
                        List<string> Values = new List<string>();
                        SceneObjectPart part = m_scene.GetSceneObjectPart(new UUID(rm["object_id"].ToString()));
                        OSDMap map = null;
                        try
                        {
                            map = (OSDMap)osd;
                        }
                        catch (Exception)
                        {
                            Values.Add(rm["object_id"].ToString());
                            Values.Add(part.OwnerID.ToString());
                            Values.Add("0");
                            Values.Add("0");
                            Values.Add("0");
                            Values.Add("0");
                            Values.Add("0");
                            Values.Add("0");
                            Values.Add("");
                            Values.Add("0");
                            Values.Add("0");
                            Values.Add("");
                            Values.Add("0");
                            Values.Add("0");
                            Values.Add("");
                            Values.Add("0");
                            Values.Add("0");
                            Values.Add("1");
                            Values.Add(i.ToString());
                            GenericData.Insert("assetMediaURL", Values.ToArray());
                            continue;
                        }

                        ObjectMediaURLInfo[] info = RegionData.getObjectMediaInfo(rm["object_id"].ToString());
                        Values.Add(rm["object_id"].ToString());
                        Values.Add(part.OwnerID.ToString());
                        Values.Add(map["alt_image_enable"].ToString());
                        Values.Add(map["auto_loop"].ToString());
                        Values.Add(map["auto_play"].ToString());
                        Values.Add(map["auto_scale"].ToString());
                        Values.Add(map["auto_zoom"].ToString());
                        Values.Add(map["controls"].ToString());
                        Values.Add(map["current_url"].ToString());
                        Values.Add(map["first_click_interact"].ToString());
                        Values.Add(map["height_pixels"].ToString());
                        Values.Add(map["home_url"].ToString());
                        Values.Add(map["perms_control"].ToString());
                        Values.Add(map["perms_interact"].ToString());
                        Values.Add(map["whitelist"].ToString());
                        Values.Add(map["whitelist_enable"].ToString());
                        Values.Add(map["width_pixels"].ToString());
                        if (info[i] == null || info[i].object_media_version == null)
                            Values.Add("1");
                        else
                        {
                            int version = (Convert.ToInt32(info[i].object_media_version) + 1);
                            Values.Add(version.ToString());
                        }
                        Values.Add(i.ToString());
                        try
                        {
                            GenericData.Insert("assetMediaURL", Values.ToArray());
                        }
                        catch (Exception)
                        {
                        }
                        i++;
                    }
                }
                #endregion
                else
                {
                    m_log.Error("[OBJECTADD] Unknown verb in ObjectMedia: " + rm["verb"].ToString());
                }
            }

            return responsedata;
        }

        public Hashtable ProcessAdd(Hashtable request, UUID AgentId, Caps cap)
        {
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 400; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = "Request wasn't what was expected";
            ScenePresence avatar;
            
            if (!m_scene.TryGetScenePresence(AgentId, out avatar))
                return responsedata;


            OSD r = OSDParser.DeserializeLLSDXml((string)request["requestbody"]);
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
                return responsedata;
            
            OSDMap rm = (OSDMap)r;

            if (rm.ContainsKey("ObjectData")) //v2
            {
                if (rm["ObjectData"].Type != OSDType.Map)
                {
                    responsedata["str_response_string"] = "Has ObjectData key, but data not in expected format";
                    return responsedata;
                }

                OSDMap ObjMap = (OSDMap) rm["ObjectData"];

                bypass_raycast = ObjMap["BypassRaycast"].AsBoolean();
                everyone_mask = readuintval(ObjMap["EveryoneMask"]);
                flags = readuintval(ObjMap["Flags"]);
                group_mask = readuintval(ObjMap["GroupMask"]);
                material = ObjMap["Material"].AsInteger();
                next_owner_mask = readuintval(ObjMap["NextOwnerMask"]);
                p_code = ObjMap["PCode"].AsInteger();

                if (ObjMap.ContainsKey("Path"))
                {
                    if (ObjMap["Path"].Type != OSDType.Map)
                    {
                        responsedata["str_response_string"] = "Has Path key, but data not in expected format";
                        return responsedata;
                    }

                    OSDMap PathMap = (OSDMap)ObjMap["Path"];
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
                    {
                        responsedata["str_response_string"] = "Has Profile key, but data not in expected format";
                        return responsedata;
                    }
                        
                    OSDMap ProfileMap = (OSDMap)ObjMap["Profile"];

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
                    ray_end = ((OSDArray) ObjMap["RayEnd"]).AsVector3();
                    ray_start = ((OSDArray) ObjMap["RayStart"]).AsVector3();
                    scale = ((OSDArray) ObjMap["Scale"]).AsVector3();
                    rotation = ((OSDArray)ObjMap["Rotation"]).AsQuaternion();
                }
                catch (Exception)
                {
                    responsedata["str_response_string"] = "RayEnd, RayStart, Scale or Rotation wasn't in the expected format";
                    return responsedata;
                }

                if (rm.ContainsKey("AgentData"))
                {
                    if (rm["AgentData"].Type != OSDType.Map)
                    {
                        responsedata["str_response_string"] = "Has AgentData key, but data not in expected format";
                        return responsedata;
                    }

                    OSDMap AgentDataMap = (OSDMap) rm["AgentData"];

                    //session_id = AgentDataMap["SessionId"].AsUUID();
                    group_id = AgentDataMap["GroupId"].AsUUID();
                }

            }
            else
            { //v1
                bypass_raycast = rm["bypass_raycast"].AsBoolean();

                everyone_mask = readuintval(rm["everyone_mask"]);
                flags = readuintval(rm["flags"]);
                group_id = rm["group_id"].AsUUID();
                group_mask = readuintval(rm["group_mask"]);
                hollow = rm["hollow"].AsInteger();
                material = rm["material"].AsInteger();
                next_owner_mask = readuintval(rm["next_owner_mask"]);
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
                    ray_end = ((OSDArray)rm["ray_end"]).AsVector3();
                    ray_start = ((OSDArray)rm["ray_start"]).AsVector3();
                    rotation = ((OSDArray)rm["rotation"]).AsQuaternion();
                    scale = ((OSDArray)rm["scale"]).AsVector3();
                } 
                catch (Exception)
                {
                    responsedata["str_response_string"] = "RayEnd, RayStart, Scale or Rotation wasn't in the expected format";
                    return responsedata;
                }
            }

           

            Vector3 pos = m_scene.GetNewRezLocation(ray_start, ray_end, ray_target_id, rotation, (bypass_raycast) ? (byte)1 : (byte)0,  (ray_end_is_intersection) ? (byte)1 : (byte)0, true, scale, false);

            PrimitiveBaseShape pbs = PrimitiveBaseShape.CreateBox();

            pbs.PathBegin = (ushort)path_begin;
            pbs.PathCurve = (byte)path_curve;
            pbs.PathEnd = (ushort)path_end;
            pbs.PathRadiusOffset = (sbyte)path_radius_offset;
            pbs.PathRevolutions = (byte)path_revolutions;
            pbs.PathScaleX = (byte)path_scale_x;
            pbs.PathScaleY = (byte)path_scale_y;
            pbs.PathShearX = (byte) path_shear_x;
            pbs.PathShearY = (byte)path_shear_y;
            pbs.PathSkew = (sbyte)path_skew;
            pbs.PathTaperX = (sbyte)path_taper_x;
            pbs.PathTaperY = (sbyte)path_taper_y;
            pbs.PathTwist = (sbyte)path_twist;
            pbs.PathTwistBegin = (sbyte)path_twist_begin;
            pbs.HollowShape = (HollowShape) hollow;
            pbs.PCode = (byte)p_code;
            pbs.ProfileBegin = (ushort) profile_begin;
            pbs.ProfileCurve = (byte) profile_curve;
            pbs.ProfileEnd = (ushort)profile_end;
            pbs.Scale = scale;
            pbs.State = (byte)state;

            SceneObjectGroup obj = null; ;

            if (m_scene.Permissions.CanRezObject(1, avatar.UUID, pos))
            {
                // rez ON the ground, not IN the ground
               // pos.Z += 0.25F;

                obj = m_scene.AddNewPrim(avatar.UUID, group_id, pos, rotation, pbs);
            }


            if (obj == null)
                return responsedata;

            SceneObjectPart rootpart = obj.RootPart;
            rootpart.Shape = pbs;
            rootpart.Flags |= (PrimFlags)flags;
            rootpart.EveryoneMask = everyone_mask;
            rootpart.GroupID = group_id;
            rootpart.GroupMask = group_mask;
            rootpart.NextOwnerMask = next_owner_mask;
            rootpart.Material = (byte)material;
            
            
            
            m_scene.PhysicsScene.AddPhysicsActorTaint(rootpart.PhysActor);
            
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = String.Format("<llsd><map><key>local_id</key>{0}</map></llsd>",ConvertUintToBytes(obj.LocalId));

            return responsedata;
        }

        private uint readuintval(OSD obj)
        {
            byte[] tmp = obj.AsBinary();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(tmp);
            return OpenMetaverse.Utils.BytesToUInt(tmp);

        }
        private string ConvertUintToBytes(uint val)
        {
            byte[] resultbytes = OpenMetaverse.Utils.UIntToBytes(val);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(resultbytes);
            return String.Format("<binary encoding=\"base64\">{0}</binary>",Convert.ToBase64String(resultbytes));
        }

        public void Close()
        {
            
        }

        public string Name
        {
            get { return "ObjectAddModule"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        #endregion
    }
}
