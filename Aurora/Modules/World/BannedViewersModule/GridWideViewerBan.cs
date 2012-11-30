/*
 * Copyright 2011 Matthew Beardmore
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
using System.Linq;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.Ban
{
    public class GridWideViewerBan : IService
    {
        private List<string> m_bannedViewers = new List<string> ();
        private List<string> m_allowedViewers = new List<string> ();
        private bool m_enabled = true;
        private bool m_useIncludeList = false;
        private OSDMap m_map = null;
        private string m_viewerTagURL = "http://phoenixviewer.com/app/client_list.xml";
        private string m_viewerTagFile = "client_list.xml";
        private IRegistryCore m_registry;

        public void Initialize(IConfigSource source, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig config = source.Configs["GrieferProtection"];
            if (config != null)
            {
                string bannedViewers = config.GetString ("ViewersToBan", "");
                m_bannedViewers = Util.ConvertToList(bannedViewers);
                string allowedViewers = config.GetString ("ViewersToAllow", "");
                m_allowedViewers = Util.ConvertToList(allowedViewers);
                m_viewerTagURL = config.GetString("ViewerXMLURL", m_viewerTagURL);
                m_viewerTagFile = config.GetString("ViewerXMLFile", m_viewerTagFile);
                m_enabled = config.GetBoolean ("ViewerBanEnabled", true);
                m_useIncludeList = config.GetBoolean ("UseAllowListInsteadOfBanList", false);
                if (m_enabled)
                    registry.RequestModuleInterface<ISimulationBase> ().EventManager.RegisterEventHandler("SetAppearance", EventManager_OnGenericEvent);
            }
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        object EventManager_OnGenericEvent(string FunctionName, object parameters)
        {
            if (FunctionName == "SetAppearance")
            {
                object[] p = (object[])parameters;
                UUID avatarID = (UUID)p[0];
                AvatarData avatarData = (AvatarData)p[1];

                AvatarAppearance app = avatarData.ToAvatarAppearance (avatarID);
                CheckForBannedViewer (avatarID, app.Texture);
            }
            return null;
        }

        /// <summary>
        /// Check to see if the client has baked textures that belong to banned clients
        /// </summary>
        /// <param name="client"></param>
        /// <param name="textureEntry"></param>
        public void CheckForBannedViewer(UUID avatarID, Primitive.TextureEntry textureEntry)
        {
            try
            {
                //Read the website once!
                if (m_map == null)
                    m_map = OSDParser.Deserialize(Utilities.ReadExternalWebsite(m_viewerTagURL)) as OSDMap;
                if (m_map == null)
                    m_map = OSDParser.Deserialize(System.IO.File.ReadAllText(m_viewerTagFile)) as OSDMap;
                if(m_map == null)
                    return;//Can't find it

                //This is the givaway texture!
                for (int i = 0; i < textureEntry.FaceTextures.Length; i++)
                {
                    if (textureEntry.FaceTextures[i] != null)
                    {
                        if (m_map.ContainsKey (textureEntry.FaceTextures[i].TextureID.ToString ()))
                        {
                            OSDMap viewerMap = (OSDMap)m_map[textureEntry.FaceTextures[i].TextureID.ToString ()];
                            //Check the names
                            if (IsViewerBanned (viewerMap["name"].ToString ()))
                            {
                                IGridWideMessageModule messageModule = m_registry.RequestModuleInterface<IGridWideMessageModule> ();
                                if (messageModule != null)
                                    messageModule.KickUser (avatarID, "You cannot use " + viewerMap["name"] + " in this grid.");
                                break;
                            }
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        public bool IsViewerBanned(string name)
        {
            if (m_useIncludeList)
            {
                if (!m_allowedViewers.Contains (name))
                    return true;
            }
            else
            {
                if (m_bannedViewers.Contains (name))
                    return true;
            }
            return false;
        }
    }
}
