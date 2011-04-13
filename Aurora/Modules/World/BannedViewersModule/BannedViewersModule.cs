using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Xml;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Interfaces;
using Aurora.Framework;
using Nini.Config;

namespace Aurora.Modules
{
    public class BannedViewersModule: ISharedRegionModule, IBanViewersModule
    {
        private List<string> m_bannedViewers = new List<string> ();
        private List<string> m_allowedViewers = new List<string> ();
        private bool m_banEvilViewersByDefault = true;
        private bool m_enabled = true;
        private bool m_useIncludeList = false;
        private OSDMap m_map = null;
        private string m_viewerTagURL = "http://viewertags.com/app/client_list.xml";

        public List<string> BannedViewers
        {
            get
            {
                return m_bannedViewers;
            }
        }

        public List<string> AllowedViewers
        {
            get
            {
                return m_allowedViewers;
            }
        }

        public void Initialise(IConfigSource source)
        {
            IConfig config = source.Configs["BanViewersModule"];
            if (config != null)
            {
                string bannedViewers = config.GetString ("ViewersToBan", "");
                m_bannedViewers = new List<string> (bannedViewers.Split (new string[1] { "," }, StringSplitOptions.RemoveEmptyEntries));
                string allowedViewers = config.GetString ("ViewersToAllow", "");
                m_allowedViewers = new List<string> (allowedViewers.Split (new string[1] { "," }, StringSplitOptions.RemoveEmptyEntries));
                m_banEvilViewersByDefault = config.GetBoolean ("BanKnownEvilViewers", true);
                m_viewerTagURL = config.GetString ("ViewerXMLURL", m_viewerTagURL);
                m_enabled = config.GetBoolean ("Enabled", true);
                m_useIncludeList = config.GetBoolean ("UseAllowListInsteadOfBanList", false);
            }
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            if(m_enabled)
                scene.RegisterModuleInterface<IBanViewersModule>(this);
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void RegionLoaded(Scene scene)
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

        /// <summary>
        /// Check to see if the client has baked textures that belong to banned clients
        /// </summary>
        /// <param name="client"></param>
        /// <param name="textureEntry"></param>
        public void CheckForBannedViewer(IClientAPI client, Primitive.TextureEntry textureEntry)
        {
            try
            {
                //Read the website once!
                if (m_map == null)
                    m_map = (OSDMap)OSDParser.Deserialize (Utilities.ReadExternalWebsite (m_viewerTagURL));
                
                //This is the givaway texture!
                for (int i = 0; i < textureEntry.FaceTextures.Length; i++)
                {
                    if (textureEntry.FaceTextures[i] != null)
                    {
                        if (m_map.ContainsKey(textureEntry.FaceTextures[i].TextureID.ToString()))
                        {
                            OSDMap viewerMap = (OSDMap)m_map[textureEntry.FaceTextures[i].TextureID.ToString()];
                            //Check the names
                            if (IsViewerBanned (viewerMap["name"].ToString (), viewerMap["evil"].AsBoolean ()))
                            {
                                client.Kick("You cannot use " + viewerMap["name"] + " in this sim.");
                                IEntityTransferModule transferModule = client.Scene.RequestModuleInterface<IEntityTransferModule> ();
                                if (transferModule != null)
                                    transferModule.IncomingCloseAgent (((Scene)client.Scene), client.AgentId);
                                break;
                            }
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        public bool IsViewerBanned(string name, bool isEvil)
        {
            if (m_useIncludeList)
            {
                if (!m_allowedViewers.Contains (name))
                    return true;
            }
            else
            {
                if (BannedViewers.Contains (name))
                    return true;
                else if (m_banEvilViewersByDefault && isEvil)
                    return true;
            }
            return false;
        }
    }
}
