/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Copyright 2009 Brian Becker <bjbdragon@gmail.com>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of 
 * the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

/// Original source at https://github.com/vgaessler/whisper_server
using System;
using System.IO;
using System.Web;
using System.Collections;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Framework.Capabilities;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules
{
    public class GenericVoiceModule : ISharedRegionModule
    {
        private string configToSend = "SLVoice";
        private bool m_enabled = true;

        public void Initialise(IConfigSource config)
        {
            IConfig m_config = config.Configs["GenericVoice"];

            if (null == m_config)
                return;

            m_enabled = m_config.GetBoolean("Enabled", true);
            if (!m_enabled)
                return;

            configToSend = m_config.GetString("ModuleToSend", configToSend);
        }

        public void AddRegion(Scene scene)
        {
            if (m_enabled)
            {
                scene.EventManager.OnRegisterCaps += delegate(UUID agentID, IHttpServer server)
                {
                    return OnRegisterCaps(scene, agentID, server);
                };
                //Add this to the OpenRegionSettings module so we can inform the client about it
                IOpenRegionSettingsModule ORSM = scene.RequestModuleInterface<IOpenRegionSettingsModule>();
                if (ORSM != null)
                    ORSM.RegisterGenericValue("Voice", "Mumble");
            }
        }

        // Called to indicate that all loadable modules have now been added
        public void RegionLoaded(Scene scene)
        {
            // Do nothing.
        }

        // Called to indicate that the region is going away.
        public void RemoveRegion(Scene scene)
        {
        }

        public void PostInitialise()
        {
            // Do nothing.
        }

        public void Close()
        {
            // Do nothing.
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return "GenericVoiceModule"; }
        }

        // OnRegisterCaps is invoked via the scene.EventManager
        // everytime OpenSim hands out capabilities to a client
        // (login, region crossing). We contribute two capabilities to
        // the set of capabilities handed back to the client:
        // ProvisionVoiceAccountRequest and ParcelVoiceInfoRequest.
        // 
        // ProvisionVoiceAccountRequest allows the client to obtain
        // the voice account credentials for the avatar it is
        // controlling (e.g., user name, password, etc).
        // 
        // ParcelVoiceInfoRequest is invoked whenever the client
        // changes from one region or parcel to another.
        //
        // Note that OnRegisterCaps is called here via a closure
        // delegate containing the scene of the respective region (see
        // Initialise()).
        public OSDMap OnRegisterCaps(Scene scene, UUID agentID, IHttpServer caps)
        {
            OSDMap retVal = new OSDMap();
            retVal["ProvisionVoiceAccountRequest"] = CapsUtil.CreateCAPS("ProvisionVoiceAccountRequest", "");
            caps.AddStreamHandler(new RestStreamHandler("POST", retVal["ProvisionVoiceAccountRequest"],
                                                       delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                       {
                                                           return ProvisionVoiceAccountRequest(scene, request, path, param,
                                                                                               agentID);
                                                       }));
            retVal["ParcelVoiceInfoRequest"] = CapsUtil.CreateCAPS("ParcelVoiceInfoRequest", "");
            caps.AddStreamHandler(new RestStreamHandler("POST", retVal["ParcelVoiceInfoRequest"],
                                                       delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                       {
                                                           return ParcelVoiceInfoRequest(scene, request, path, param,
                                                                                         agentID);
                                                       }));

            return retVal;
        }

        /// Callback for a client request for Voice Account Details.
        public string ProvisionVoiceAccountRequest(Scene scene, string request, string path, string param,
                                                   UUID agentID)
        {
            try
            {
                OSDMap response = new OSDMap();
                response["username"] = "";
                response["password"] = "";
                response["voice_sip_uri_hostname"] = "";
                response["voice_account_server_name"] = "";

                return OSDParser.SerializeLLSDXmlString(response);
            }
            catch (Exception)
            {
                return "<llsd><undef /></llsd>";
            }
        }

        /// Callback for a client request for ParcelVoiceInfo
        public string ParcelVoiceInfoRequest(Scene scene, string request, string path, string param,
                                             UUID agentID)
        {
            OSDMap response = new OSDMap();
            response["region_name"] = scene.RegionInfo.RegionName;
            response["parcel_local_id"] = 0;
            response["voice_credentials"] = new OSDMap();
            ((OSDMap)response["voice_credentials"])["channel_uri"] = "";
            return OSDParser.SerializeLLSDXmlString(response);
        }
    }
}
