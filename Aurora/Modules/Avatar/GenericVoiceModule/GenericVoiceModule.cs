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
            IConfig voiceconfig = config.Configs["Voice"];
            if (voiceconfig == null)
                return;
            m_enabled = false;
            string voiceModule = "GenericVoice";
            if (voiceconfig.GetString ("Module", voiceModule) != voiceModule)
                return;
            m_enabled = true;
            IConfig m_config = config.Configs["GenericVoice"];

            if (null == m_config)
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
