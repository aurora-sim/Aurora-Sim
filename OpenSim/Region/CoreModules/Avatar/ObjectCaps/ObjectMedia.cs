
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
using Caps = OpenSim.Framework.Capabilities.Caps;
using Aurora.DataManager;
using Aurora.Framework;
using Mono.Addins;

namespace OpenSim.Region.CoreModules.Avatar.ObjectCaps
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]
    public class ObjectMedia : INonSharedRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Scene m_scene;
        IAssetConnector AC = null;

        public void Initialise(IConfigSource pSource)
        {

        }

        public void AddRegion(Scene scene)
        {
            m_scene = scene;
            m_scene.EventManager.OnRegisterCaps += RegisterCaps;
        }

        public void RemoveRegion(Scene scene)
        {

        }

        public void RegionLoaded(Scene scene)
        {
            AC = Aurora.DataManager.DataManager.IAssetConnector;
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void PostInitialise()
        {
        }

        public void RegisterCaps(UUID agentID, Caps caps)
        {
            UUID capuuid = UUID.Random();

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
            OSDMap rm = (OSDMap)OSDParser.DeserializeLLSDXml((string)mDhttpMethod["requestbody"]);
            string currentURL = rm["current_url"].AsString();
            string currentSide = rm["texture_index"].AsString();
            string objectUUID = rm["object_id"].AsString();
            
            //Sends out an update for everyone else
            SceneObjectPart part = m_scene.GetSceneObjectPart(UUID.Parse(objectUUID));
            
            //Update the database.
            ObjectMediaURL media = AC.GetObjectMediaInfo(objectUUID, int.Parse(currentSide));
            if (media == null)
            {
                media = new ObjectMediaURL();
                media.Side = int.Parse(currentSide);
                media.OwnerID = part.OwnerID;
                media.ObjectID = part.UUID;
                media.object_media_version = "x-mv:0000000001/00000000-0000-0000-0000-000000000000";
            }
            media.current_url = currentURL;
            AC.UpdateObjectMediaInfo(media, media.Side, media.ObjectID);
            Primitive.TextureEntry textures = part.Shape.Textures;
            if (textures.FaceTextures[media.Side] == null)
            {
                Primitive.TextureEntryFace texface = part.Shape.Textures.CreateFace((uint)media.Side);
                textures.FaceTextures[media.Side] = texface;
            }
            textures.FaceTextures[media.Side].MediaFlags = true;
            part.Shape.Textures = textures;

            string Version = part.CurrentMediaVersion.Remove(0, 14);
            Version = Version.Remove(1, Version.Length - 1);
            int version = int.Parse(Version);
            version++;
            Version = "x-mv:000000000" + version + "/00000000-0000-0000-0000-000000000000";
            part.CurrentMediaVersion = Version;

            part.SendFullUpdateToAllClients();

            //Send back data
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = "";
            return responsedata;
        }

        private Hashtable ProcessObjectMedia(Hashtable mDhttpMethod, UUID objectID)
        {
            //Response for CAPS
            Hashtable responsedata = new Hashtable();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "application/llsd+xml";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = "";

            //Deserialize request
            OSDMap rm = (OSDMap)OSDParser.DeserializeLLSDXml((string)mDhttpMethod["requestbody"]);
            
            if (rm.ContainsKey("verb"))
            {
                #region Get
                if (rm["verb"].ToString() == "GET")
                {
                    OSDMap MainMap = new OSDMap();
                    OSD osd = new OSD();

                    SceneObjectPart part = m_scene.GetSceneObjectPart(new UUID(rm["object_id"].ToString()));
                    ObjectMediaURL info1 = AC.GetObjectMediaInfo(rm["object_id"].ToString(), 0);
                    ObjectMediaURL info2 = AC.GetObjectMediaInfo(rm["object_id"].ToString(), 1);
                    ObjectMediaURL info3 = AC.GetObjectMediaInfo(rm["object_id"].ToString(), 2);
                    ObjectMediaURL info4 = AC.GetObjectMediaInfo(rm["object_id"].ToString(), 3);
                    ObjectMediaURL info5 = AC.GetObjectMediaInfo(rm["object_id"].ToString(), 4);
                    ObjectMediaURL info6 = AC.GetObjectMediaInfo(rm["object_id"].ToString(), 5);

                    List<ObjectMediaURL> infos = new List<ObjectMediaURL>();
                    infos.Add(info1);
                    infos.Add(info2);
                    infos.Add(info3);
                    infos.Add(info4);
                    infos.Add(info5);
                    infos.Add(info6);

                    OSDArray array = new OSDArray(6);
                    foreach (ObjectMediaURL info in infos)
                    {
                        if (info == null)
                        {
                            OSD nullMap = new OSD();
                            array.Add(nullMap);
                            continue;
                        }

                        OSDMap mediadataMap = new OSDMap();
                        osd = new OSDBoolean(info.alt_image_enable);
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

                        osd = new OSDInteger(info.perms_interact);
                        mediadataMap.Add("perms_interact", osd);

                        osd = new OSDBoolean(info.whitelist_enable);
                        mediadataMap.Add("whitelist_enable", osd);

                        osd = new OSDInteger(info.width_pixels);
                        mediadataMap.Add("width_pixels", osd);
                        array.Add(mediadataMap);
                    }
                    osd = new OSDUUID(objectID);
                    MainMap.Add("object_id", osd);

                    MainMap.Add("object_media_data", array);

                    osd = new OSDString(part.CurrentMediaVersion);
                    MainMap.Add("object_media_version", osd);


                    string response = OSDParser.SerializeLLSDXmlString(MainMap);
                    responsedata["str_response_string"] = response; //String.Format(@"<llsd><map><key>object_id</key><uuid>{0}</uuid><key>object_media_data</key><array><map><key>alt_image_enable</key><boolean>0</boolean><key>auto_loop</key><boolean>0</boolean><key>auto_play</key><boolean>1</boolean><key>auto_scale</key><boolean>1</boolean><key>auto_zoom</key><boolean>0</boolean><key>controls</key><integer>0</integer><key>current_url</key><string>http://v01.wwweb3d.net/dahliaUnityPlayer/dahliaWebPlayer03.html</string><key>first_click_interact</key><boolean>0</boolean><key>height_pixels</key><integer>0</integer><key>home_url</key><string>http://v01.wwweb3d.net/dahliaUnityPlayer/dahliaWebPlayer03.html</string><key>perms_control</key><integer>7</integer><key>perms_interact</key><integer>7</integer><key>whitelist_enable</key><boolean>0</boolean><key>width_pixels</key><integer>0</integer></map><map><key>alt_image_enable</key><boolean>0</boolean><key>auto_loop</key><boolean>0</boolean><key>auto_play</key><boolean>1</boolean><key>auto_scale</key><boolean>1</boolean><key>auto_zoom</key><boolean>0</boolean><key>controls</key><integer>0</integer><key>current_url</key><string>http://www.google.com/</string><key>first_click_interact</key><boolean>0</boolean><key>height_pixels</key><integer>0</integer><key>home_url</key><string>http://www.google.com</string><key>perms_control</key><integer>7</integer><key>perms_interact</key><integer>7</integer><key>whitelist_enable</key><boolean>0</boolean><key>width_pixels</key><integer>0</integer></map><undef /><undef /><undef /><undef /></array><key>object_media_version</key><string>x-mv:0000000042/79e7c4ad-3361-4736-bced-1f72e6c3dbd4</string></map></llsd>",uuid.ToString());
                }
                #endregion

                #region Update
                else if (rm["verb"].ToString() == "UPDATE")
                {
                    OSDArray media_data_map = (OSDArray)rm["object_media_data"];
                    SceneObjectPart part = m_scene.GetSceneObjectPart(new UUID(rm["object_id"].ToString()));
                    int side = 0;
                    foreach (OSD osd in media_data_map)
                    {
                        string type = osd.Type.ToString();
                        if (type == "Unknown")
                        {
                            // Remove the media
                            AC.UpdateObjectMediaInfo(null, side, part.UUID);
                            Primitive.TextureEntry textures = part.Shape.Textures;
                            if (textures.FaceTextures[side] != null)
                            {
                                textures.FaceTextures[side].MediaFlags = false;
                            }
                            part.Shape.Textures = textures;
                            side++;
                            continue;
                        }
                        else
                        {
                            // Add/Update the media
                            List<string> Values = new List<string>();
                            OSDMap map = (OSDMap)osd;

                            ObjectMediaURL info = AC.GetObjectMediaInfo(rm["object_id"].ToString(), side);
                            if (info == null)
                                info = new ObjectMediaURL();

                            info.ObjectID = new UUID(rm["object_id"].ToString());
                            info.OwnerID = new UUID(part.OwnerID.ToString());
                            info.Side = side;
                            info.alt_image_enable = map["alt_image_enable"].AsInteger() == 1;
                            info.auto_loop = map["auto_loop"].AsInteger() == 1;
                            info.auto_play = map["auto_play"].AsInteger() == 1;
                            info.auto_scale = map["auto_scale"].AsInteger() == 1;
                            info.auto_zoom = map["auto_zoom"].AsInteger() == 1;
                            string controls = map["controls"].ToString();
                            if (controls != "")
                                info.controls = int.Parse(map["controls"].ToString());
                            info.current_url = map["current_url"].ToString();
                            info.first_click_interact = map["first_click_interact"].AsInteger() == 1;
                            info.height_pixels = int.Parse(map["height_pixels"].ToString());
                            info.home_url = map["home_url"].ToString();
                            info.perms_control = int.Parse(map["perms_control"].ToString());
                            info.perms_interact = int.Parse(map["perms_interact"].ToString());
                            info.whitelist = map["whitelist"].ToString();
                            info.whitelist_enable = map["whitelist_enable"].AsInteger() == 1;
                            info.width_pixels = int.Parse(map["width_pixels"].ToString());

                            if (info.object_media_version == null || info.object_media_version == "")
                                info.object_media_version = "x-mv:0000000001/00000000-0000-0000-0000-000000000000";

                            AC.UpdateObjectMediaInfo(info, info.Side, info.ObjectID);
                            Primitive.TextureEntry textures = part.Shape.Textures;
                            if (textures.FaceTextures[info.Side] == null)
                            {
                                Primitive.TextureEntryFace texface = part.Shape.Textures.CreateFace((uint)info.Side);
                                textures.FaceTextures[info.Side] = texface;
                            }
                            textures.FaceTextures[info.Side].MediaFlags = true;
                            part.Shape.Textures = textures;

                            side++;
                        }
                    }
                    ObjectMediaURL info1 = AC.GetObjectMediaInfo(rm["object_id"].ToString(), 0);
                    ObjectMediaURL info2 = AC.GetObjectMediaInfo(rm["object_id"].ToString(), 1);
                    ObjectMediaURL info3 = AC.GetObjectMediaInfo(rm["object_id"].ToString(), 2);
                    ObjectMediaURL info4 = AC.GetObjectMediaInfo(rm["object_id"].ToString(), 3);
                    ObjectMediaURL info5 = AC.GetObjectMediaInfo(rm["object_id"].ToString(), 4);
                    ObjectMediaURL info6 = AC.GetObjectMediaInfo(rm["object_id"].ToString(), 5);
                    if (info1 == null && info2 == null && info3 == null &&
                        info4 == null && info5 == null && info6 == null)
                        part.CurrentMediaVersion = "";
                    else
                    {
                        string Version = part.CurrentMediaVersion.Remove(0, 14);
                        Version = Version.Remove(1, Version.Length - 1);
                        int version = int.Parse(Version);
                        version++;
                        Version = "x-mv:000000000" + version + "/00000000-0000-0000-0000-000000000000";
                        part.CurrentMediaVersion = Version;
                    }

                    part.SendFullUpdateToAllClients();
                }
                #endregion
                else
                {
                    m_log.Warn("[OBJECTMEDIA]: UNKNOWN VERB IN OBJECT MEDIA " + rm["verb"].ToString());
                }
            }

            return responsedata;
        }

        public void Close()
        {

        }

        public string Name
        {
            get { return "ObjectMediaModule"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }
    }
}
