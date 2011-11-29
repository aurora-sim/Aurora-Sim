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
using System.Collections.Generic;
using System.Linq;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;

namespace Aurora.Modules
{
    public class BannedViewersModule : ISharedRegionModule, IBanViewersModule
    {
        private static OSDMap m_map;
        private List<string> m_allowedViewers = new List<string>();
        private bool m_banEvilViewersByDefault = true;
        private List<string> m_bannedViewers = new List<string>();
        private bool m_enabled = true;
        private bool m_useIncludeList;
        private string m_viewerTagURL = "http://viewertags.com/app/client_list.xml";

        public List<string> BannedViewers
        {
            get { return m_bannedViewers; }
        }

        public List<string> AllowedViewers
        {
            get { return m_allowedViewers; }
        }

        #region IBanViewersModule Members

        /// <summary>
        ///   Check to see if the client has baked textures that belong to banned clients
        /// </summary>
        /// <param name = "client"></param>
        /// <param name = "textureEntry"></param>
        public void CheckForBannedViewer(IClientAPI client, Primitive.TextureEntry textureEntry)
        {
            try
            {
                //Read the website once!
                if (m_map == null)
                    m_map = OSDParser.Deserialize(Utilities.ReadExternalWebsite(m_viewerTagURL)) as OSDMap;
                if (m_map == null)
                    return;

                //This is the givaway texture!
                foreach (OSDMap viewerMap in from t in textureEntry.FaceTextures where t != null where m_map.ContainsKey(t.TextureID.ToString()) select (OSDMap) m_map[t.TextureID.ToString()])
                {
                    //Check the names
                    if (IsViewerBanned(viewerMap["name"].ToString(), viewerMap["evil"].AsBoolean()))
                    {
                        client.Kick("You cannot use " + viewerMap["name"] + " in this sim.");
                        IEntityTransferModule transferModule =
                            client.Scene.RequestModuleInterface<IEntityTransferModule>();
                        if (transferModule != null)
                            transferModule.IncomingCloseAgent(client.Scene, client.AgentId);
                        break;
                    }
                    break;
                }
            }
            catch
            {
            }
        }

        public bool IsViewerBanned(string name, bool isEvil)
        {
            if (m_useIncludeList)
            {
                if (!m_allowedViewers.Contains(name))
                    return true;
            }
            else
            {
                if (BannedViewers.Contains(name))
                    return true;
                else if (m_banEvilViewersByDefault && isEvil)
                    return true;
            }
            return false;
        }

        #endregion

        #region ISharedRegionModule Members

        public void Initialise(IConfigSource source)
        {
            IConfig config = source.Configs["BanViewersModule"];
            if (config != null)
            {
                string bannedViewers = config.GetString("ViewersToBan", "");
                m_bannedViewers =
                    new List<string>(bannedViewers.Split(new string[1] {","}, StringSplitOptions.RemoveEmptyEntries));
                string allowedViewers = config.GetString("ViewersToAllow", "");
                m_allowedViewers =
                    new List<string>(allowedViewers.Split(new string[1] {","}, StringSplitOptions.RemoveEmptyEntries));
                m_banEvilViewersByDefault = config.GetBoolean("BanKnownEvilViewers", true);
                m_viewerTagURL = config.GetString("ViewerXMLURL", m_viewerTagURL);
                m_enabled = config.GetBoolean("Enabled", true);
                m_useIncludeList = config.GetBoolean("UseAllowListInsteadOfBanList", false);
            }
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public void AddRegion(IScene scene)
        {
            if (m_enabled)
                scene.RegisterModuleInterface<IBanViewersModule>(this);
        }

        public void RemoveRegion(IScene scene)
        {
        }

        public void RegionLoaded(IScene scene)
        {
        }

        public string Name
        {
            get { return "BanViewersModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion
    }
}