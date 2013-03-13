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
using System.Reflection;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Messages.Linden;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Framework.Capabilities;
using Aurora.Framework.Servers.HttpServer;

namespace Aurora.Modules.Entities.Media
{
    public class MoapModule : INonSharedRegionModule, IMoapModule
    {
        /// <summary>
        ///   Is this module enabled?
        /// </summary>
        protected bool m_isEnabled = true;

        /// <summary>
        ///   Track the ObjectMedia capabilities given to users keyed by agent.  Lock m_omCapUsers to manipulate.
        /// </summary>
        protected Dictionary<UUID, string> m_omCapUrls = new Dictionary<UUID, string>();

        /// <summary>
        ///   Track the ObjectMedia capabilities given to users keyed by path
        /// </summary>
        protected Dictionary<string, UUID> m_omCapUsers = new Dictionary<string, UUID>();

        /// <summary>
        ///   Track the ObjectMediaUpdate capabilities given to users keyed by agent.  Lock m_omuCapUsers to manipulate
        /// </summary>
        protected Dictionary<UUID, string> m_omuCapUrls = new Dictionary<UUID, string>();

        /// <summary>
        ///   Track the ObjectMediaUpdate capabilities given to users keyed by path
        /// </summary>
        protected Dictionary<string, UUID> m_omuCapUsers = new Dictionary<string, UUID>();

        /// <summary>
        ///   The scene to which this module is attached
        /// </summary>
        protected IScene m_scene;

        #region IMoapModule Members

        public MediaEntry GetMediaEntry(ISceneChildEntity part, int face)
        {
            MediaEntry me = null;

            CheckFaceParam(part, face);

            List<MediaEntry> media = part.Shape.Media;

            if (null == media)
            {
                me = null;
            }
            else
            {
                lock (media)
                    me = media[face];

                // TODO: Really need a proper copy constructor down in libopenmetaverse
                if (me != null)
                    me = MediaEntry.FromOSD(me.GetOSD());
            }

//            MainConsole.Instance.DebugFormat("[MOAP]: GetMediaEntry for {0} face {1} found {2}", part.Name, face, me);

            return me;
        }

        public void SetMediaEntry(ISceneChildEntity part, int face, MediaEntry me)
        {
            CheckFaceParam(part, face);

            if (null == part.Shape.Media)
            {
                if (me == null)
                    return;
                else
                {
                    part.Shape.Media = new PrimitiveBaseShape.MediaList(new MediaEntry[part.GetNumberOfSides()]);
                }
            }

            if (part.Shape.Media[face] == null) //If it doesn't exist, set the default parameters for it
                me.InteractPermissions = MediaPermission.All;
            lock (part.Shape.Media)
                part.Shape.Media[face] = me;

            UpdateMediaUrl(part, UUID.Zero);

            SetPartMediaFlags(part, face, me != null);

            part.ScheduleUpdate(PrimUpdateFlags.FullUpdate);
            part.TriggerScriptChangedEvent(Changed.MEDIA);
        }

        public void ClearMediaEntry(ISceneChildEntity part, int face)
        {
            SetMediaEntry(part, face, null);
        }

        #endregion

        #region INonSharedRegionModule Members

        public string Name
        {
            get { return "MoapModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(IConfigSource configSource)
        {
            IConfig config = configSource.Configs["MediaOnAPrim"];

            if (config != null && !config.GetBoolean("Enabled", false))
                m_isEnabled = false;
//            else
//                MainConsole.Instance.Debug("[MOAP]: Initialised module.")l
        }

        public void AddRegion(IScene scene)
        {
            if (!m_isEnabled)
                return;

            m_scene = scene;
            m_scene.RegisterModuleInterface<IMoapModule>(this);
        }

        public void RemoveRegion(IScene scene)
        {
        }

        public void RegionLoaded(IScene scene)
        {
            if (!m_isEnabled)
                return;

            m_scene.EventManager.OnRegisterCaps += OnRegisterCaps;
            m_scene.EventManager.OnDeregisterCaps += OnDeregisterCaps;
        }

        public void Close()
        {
            if (!m_isEnabled)
                return;

            m_scene.EventManager.OnRegisterCaps -= OnRegisterCaps;
            m_scene.EventManager.OnDeregisterCaps -= OnDeregisterCaps;
        }

        #endregion

        public OSDMap OnRegisterCaps(UUID agentID, IHttpServer server)
        {
//            MainConsole.Instance.DebugFormat(
//                "[MOAP]: Registering ObjectMedia and ObjectMediaNavigate capabilities for agent {0}", agentID);

            OSDMap retVal = new OSDMap();
            retVal["ObjectMedia"] = CapsUtil.CreateCAPS("ObjectMedia", "");

            lock (m_omCapUsers)
            {
                m_omCapUsers[retVal["ObjectMedia"]] = agentID;
                m_omCapUrls[agentID] = retVal["ObjectMedia"];

                // Even though we're registering for POST we're going to get GETS and UPDATES too
                server.AddStreamHandler(new GenericStreamHandler("POST", retVal["ObjectMedia"], HandleObjectMediaMessage));
            }

            retVal["ObjectMediaNavigate"] = CapsUtil.CreateCAPS("ObjectMediaNavigate", "");

            lock (m_omuCapUsers)
            {
                m_omuCapUsers[retVal["ObjectMediaNavigate"]] = agentID;
                m_omuCapUrls[agentID] = retVal["ObjectMediaNavigate"];

                // Even though we're registering for POST we're going to get GETS and UPDATES too
                server.AddStreamHandler(new GenericStreamHandler("POST", retVal["ObjectMediaNavigate"],
                                                              HandleObjectMediaNavigateMessage));
            }
            return retVal;
        }

        public void OnDeregisterCaps(UUID agentID, IRegionClientCapsService caps)
        {
            lock (m_omCapUsers)
            {
                string path = m_omCapUrls[agentID];
                m_omCapUrls.Remove(agentID);
                m_omCapUsers.Remove(path);
            }

            lock (m_omuCapUsers)
            {
                string path = m_omuCapUrls[agentID];
                m_omuCapUrls.Remove(agentID);
                m_omuCapUsers.Remove(path);
            }
        }

        /// <summary>
        ///   Set the media flags on the texture face of the given part.
        /// </summary>
        /// <remarks>
        ///   The fact that we need a separate function to do what should be a simple one line operation is BUTT UGLY.
        /// </remarks>
        /// <param name = "part"></param>
        /// <param name = "face"></param>
        /// <param name = "flag"></param>
        protected void SetPartMediaFlags(ISceneChildEntity part, int face, bool flag)
        {
            Primitive.TextureEntry te = part.Shape.Textures;
            Primitive.TextureEntryFace teFace = te.CreateFace((uint) face);
            teFace.MediaFlags = flag;
            part.Shape.Textures = te;
        }

        /// <summary>
        ///   Sets or gets per face media textures.
        /// </summary>
        /// <param name = "request"></param>
        /// <param name = "path"></param>
        /// <param name = "param"></param>
        /// <param name = "httpRequest"></param>
        /// <param name = "httpResponse"></param>
        /// <returns></returns>
        protected byte[] HandleObjectMediaMessage(string path, Stream request, OSHttpRequest httpRequest,
                                                                    OSHttpResponse httpResponse)
        {
//            MainConsole.Instance.DebugFormat("[MOAP]: Got ObjectMedia path [{0}], raw request [{1}]", path, request);

            OSDMap osd = (OSDMap) OSDParser.DeserializeLLSDXml(request);
            ObjectMediaMessage omm = new ObjectMediaMessage();
            omm.Deserialize(osd);

            if (omm.Request is ObjectMediaRequest)
                return HandleObjectMediaRequest(omm.Request as ObjectMediaRequest);
            else if (omm.Request is ObjectMediaUpdate)
                return HandleObjectMediaUpdate(path, omm.Request as ObjectMediaUpdate);

            throw new Exception(
                string.Format(
                    "[MOAP]: ObjectMediaMessage has unrecognized ObjectMediaBlock of {0}",
                    omm.Request.GetType()));
        }

        /// <summary>
        ///   Handle a fetch request for media textures
        /// </summary>
        /// <param name = "omr"></param>
        /// <returns></returns>
        protected byte[] HandleObjectMediaRequest(ObjectMediaRequest omr)
        {
            UUID primId = omr.PrimID;

            ISceneChildEntity part = m_scene.GetSceneObjectPart(primId);

            if (null == part)
            {
                MainConsole.Instance.WarnFormat(
                    "[MOAP]: Received a GET ObjectMediaRequest for prim {0} but this doesn't exist in region {1}",
                    primId, m_scene.RegionInfo.RegionName);
                return MainServer.NoResponse;
            }

            ObjectMediaResponse resp = new ObjectMediaResponse
                                           {
                                               PrimID = primId,
                                               FaceMedia = new PrimitiveBaseShape.MediaList().ToArray(),
                                               Version = "x-mv:0000000001/00000000-0000-0000-0000-000000000000"
                                           };

            if (null != part.Shape.Media)
            {
                lock (part.Shape.Media)
                    resp.FaceMedia = part.Shape.Media.ToArray();

                resp.Version = part.MediaUrl;
            }

            return OSDParser.SerializeLLSDXmlBytes(resp.Serialize());
        }

        /// <summary>
        ///   Handle an update of media textures.
        /// </summary>
        /// <param name = "path">Path on which this request was made</param>
        /// <param name = "omu">/param>
        ///   <returns></returns>
        protected byte[] HandleObjectMediaUpdate(string path, ObjectMediaUpdate omu)
        {
            UUID primId = omu.PrimID;

            ISceneChildEntity part = m_scene.GetSceneObjectPart(primId);

            if (null == part)
            {
                MainConsole.Instance.WarnFormat(
                    "[MOAP]: Received an UPDATE ObjectMediaRequest for prim {0} but this doesn't exist in region {1}",
                    primId, m_scene.RegionInfo.RegionName);
                return MainServer.NoResponse;
            }

//            MainConsole.Instance.DebugFormat("[MOAP]: Received {0} media entries for prim {1}", omu.FaceMedia.Length, primId);

//            for (int i = 0; i < omu.FaceMedia.Length; i++)
//            {
//                MediaEntry me = omu.FaceMedia[i];
//                string v = (null == me ? "null": OSDParser.SerializeLLSDXmlString(me.GetOSD()));
//                MainConsole.Instance.DebugFormat("[MOAP]: Face {0} [{1}]", i, v);
//            }

            if (omu.FaceMedia.Length > part.GetNumberOfSides())
            {
                MainConsole.Instance.WarnFormat(
                    "[MOAP]: Received {0} media entries from client for prim {1} {2} but this prim has only {3} faces.  Dropping request.",
                    omu.FaceMedia.Length, part.Name, part.UUID, part.GetNumberOfSides());
                return MainServer.NoResponse;
            }

            UUID agentId = default(UUID);

            lock (m_omCapUsers)
                agentId = m_omCapUsers[path];

            List<MediaEntry> media = part.Shape.Media;

            if (null == media)
            {
//                MainConsole.Instance.DebugFormat("[MOAP]: Setting all new media list for {0}", part.Name);
                part.Shape.Media = new PrimitiveBaseShape.MediaList(omu.FaceMedia);

                for (int i = 0; i < omu.FaceMedia.Length; i++)
                {
                    if (omu.FaceMedia[i] != null)
                    {
                        // FIXME: Race condition here since some other texture entry manipulator may overwrite/get
                        // overwritten.  Unfortunately, PrimitiveBaseShape does not allow us to change texture entry
                        // directly.
                        SetPartMediaFlags(part, i, true);
//                        MainConsole.Instance.DebugFormat(
//                            "[MOAP]: Media flags for face {0} is {1}", 
//                            i, part.Shape.Textures.FaceTextures[i].MediaFlags);
                    }
                }
            }
            else
            {
                // We need to go through the media textures one at a time to make sure that we have permission 
                // to change them

                // FIXME: Race condition here since some other texture entry manipulator may overwrite/get
                // overwritten.  Unfortunately, PrimitiveBaseShape does not allow us to change texture entry
                // directly.
                Primitive.TextureEntry te = part.Shape.Textures;

                lock (media)
                {
                    for (int i = 0; i < media.Count; i++)
                    {
                        if (m_scene.Permissions.CanControlPrimMedia(agentId, part.UUID, i))
                        {
                            media[i] = omu.FaceMedia[i];

                            // When a face is cleared this is done by setting the MediaFlags in the TextureEntry via a normal
                            // texture update, so we don't need to worry about clearing MediaFlags here.
                            if (null == media[i])
                                continue;

                            SetPartMediaFlags(part, i, true);

                            //                        MainConsole.Instance.DebugFormat(
                            //                            "[MOAP]: Media flags for face {0} is {1}", 
                            //                            i, face.MediaFlags);
                            //                        MainConsole.Instance.DebugFormat("[MOAP]: Set media entry for face {0} on {1}", i, part.Name);
                        }
                    }
                }

                part.Shape.Textures = te;

//                for (int i2 = 0; i2 < part.Shape.Textures.FaceTextures.Length; i2++)
//                    MainConsole.Instance.DebugFormat("[MOAP]: FaceTexture[{0}] is {1}", i2, part.Shape.Textures.FaceTextures[i2]);
            }

            UpdateMediaUrl(part, agentId);

            // Arguably, we could avoid sending a full update to the avatar that just changed the texture.
            part.ScheduleUpdate(PrimUpdateFlags.FullUpdate);

            part.TriggerScriptChangedEvent(Changed.MEDIA);

            return MainServer.NoResponse;
        }

        /// <summary>
        ///   Received from the viewer if a user has changed the url of a media texture.
        /// </summary>
        /// <param name = "request"></param>
        /// <param name = "path"></param>
        /// <param name = "param"></param>
        /// <param name = "httpRequest">/param>
        ///   <param name = "httpResponse">/param>
        ///     <returns></returns>
        protected byte[] HandleObjectMediaNavigateMessage(string path, Stream request, OSHttpRequest httpRequest,
                                                                    OSHttpResponse httpResponse)
        {
//            MainConsole.Instance.DebugFormat("[MOAP]: Got ObjectMediaNavigate request [{0}]", request);

            OSDMap osd = (OSDMap) OSDParser.DeserializeLLSDXml(request);
            ObjectMediaNavigateMessage omn = new ObjectMediaNavigateMessage();
            omn.Deserialize(osd);

            UUID primId = omn.PrimID;

            ISceneChildEntity part = m_scene.GetSceneObjectPart(primId);

            if (null == part)
            {
                MainConsole.Instance.WarnFormat(
                    "[MOAP]: Received an ObjectMediaNavigateMessage for prim {0} but this doesn't exist in region {1}",
                    primId, m_scene.RegionInfo.RegionName);
                return MainServer.NoResponse;
            }

            UUID agentId = default(UUID);

            lock (m_omuCapUsers)
                agentId = m_omuCapUsers[path];

            if (!m_scene.Permissions.CanInteractWithPrimMedia(agentId, part.UUID, omn.Face))
                return MainServer.NoResponse;

//            MainConsole.Instance.DebugFormat(
//                "[MOAP]: Received request to update media entry for face {0} on prim {1} {2} to {3}", 
//                omn.Face, part.Name, part.UUID, omn.URL);

            // If media has never been set for this prim, then just return.
            if (null == part.Shape.Media)
                return MainServer.NoResponse;

            MediaEntry me = null;

            lock (part.Shape.Media)
                me = part.Shape.Media[omn.Face];

            // Do the same if media has not been set up for a specific face
            if (null == me)
                return MainServer.NoResponse;

            if (me.EnableWhiteList)
            {
                if (!CheckUrlAgainstWhitelist(omn.URL, me.WhiteList))
                {
//                    MainConsole.Instance.DebugFormat(
//                        "[MOAP]: Blocking change of face {0} on prim {1} {2} to {3} since it's not on the enabled whitelist", 
//                        omn.Face, part.Name, part.UUID, omn.URL);

                    return MainServer.NoResponse;
                }
            }

            me.CurrentURL = omn.URL;

            UpdateMediaUrl(part, agentId);

            part.ScheduleUpdate(PrimUpdateFlags.FullUpdate);

            part.TriggerScriptChangedEvent(Changed.MEDIA);

            return OSDParser.SerializeLLSDXmlBytes(new OSD());
        }

        /// <summary>
        ///   Check that the face number is valid for the given prim.
        /// </summary>
        /// <param name = "part"></param>
        /// <param name = "face"></param>
        protected void CheckFaceParam(ISceneChildEntity part, int face)
        {
            if (face < 0)
                throw new ArgumentException("Face cannot be less than zero");

            int maxFaces = part.GetNumberOfSides() - 1;
            if (face > maxFaces)
                throw new ArgumentException(
                    string.Format("Face argument was {0} but max is {1}", face, maxFaces));
        }

        /// <summary>
        ///   Update the media url of the given part
        /// </summary>
        /// <param name = "part"></param>
        /// <param name = "updateId">
        ///   The id to attach to this update.  Normally, this is the user that changed the
        ///   texture
        /// </param>
        protected void UpdateMediaUrl(ISceneChildEntity part, UUID updateId)
        {
            if (null == part.MediaUrl)
            {
                // TODO: We can't set the last changer until we start tracking which cap we give to which agent id
                part.MediaUrl = "x-mv:0000000000/" + updateId;
            }
            else
            {
                string rawVersion = part.MediaUrl.Substring(5, 10);
                int version = int.Parse(rawVersion);
                part.MediaUrl = string.Format("x-mv:{0:D10}/{1}", ++version, updateId);
            }

//            MainConsole.Instance.DebugFormat("[MOAP]: Storing media url [{0}] in prim {1} {2}", part.MediaUrl, part.Name, part.UUID);
        }

        /// <summary>
        ///   Check the given url against the given whitelist.
        /// </summary>
        /// <param name = "rawUrl"></param>
        /// <param name = "whitelist"></param>
        /// <returns>true if the url matches an entry on the whitelist, false otherwise</returns>
        protected bool CheckUrlAgainstWhitelist(string rawUrl, string[] whitelist)
        {
            Uri url = new Uri(rawUrl);

            foreach (string origWlUrl in whitelist)
            {
                string wlUrl = origWlUrl;

                // Deal with a line-ending wildcard
                if (wlUrl.EndsWith("*"))
                    wlUrl = wlUrl.Remove(wlUrl.Length - 1);

//                MainConsole.Instance.DebugFormat("[MOAP]: Checking whitelist URL pattern {0}", origWlUrl);

                // Handle a line starting wildcard slightly differently since this can only match the domain, not the path
                if (wlUrl.StartsWith("*"))
                {
                    wlUrl = wlUrl.Substring(1);

                    if (url.Host.Contains(wlUrl))
                    {
//                        MainConsole.Instance.DebugFormat("[MOAP]: Whitelist URL {0} matches {1}", origWlUrl, rawUrl);
                        return true;
                    }
                }
                else
                {
                    string urlToMatch = url.Authority + url.AbsolutePath;

                    if (urlToMatch.StartsWith(wlUrl))
                    {
//                        MainConsole.Instance.DebugFormat("[MOAP]: Whitelist URL {0} matches {1}", origWlUrl, rawUrl);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}