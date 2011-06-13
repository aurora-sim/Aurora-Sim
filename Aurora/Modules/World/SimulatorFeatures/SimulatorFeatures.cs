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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework;
using OpenSim.Framework.Capabilities;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.World
{
    public class SimulatorFeatures : ISharedRegionModule
    {
        public void PostInitialise ()
        {
        }

        public void Initialise (IConfigSource source)
        {
        }

        public void AddRegion (Scene scene)
        {
            scene.EventManager.OnRegisterCaps += RegisterCaps;
            scene.EventManager.OnDeregisterCaps += new EventManager.DeregisterCapsEvent (EventManager_OnDeregisterCaps);
        }

        void EventManager_OnDeregisterCaps (UUID agentID, IRegionClientCapsService caps)
        {
        }

        public OSDMap RegisterCaps (UUID agentID, IHttpServer server)
        {
            OSDMap retVal = new OSDMap ();
            retVal["SimulatorFeatures"] = CapsUtil.CreateCAPS ("SimulatorFeatures", "");

            server.AddStreamHandler (new RestHTTPHandler ("GET", retVal["SimulatorFeatures"],
                                                      delegate (Hashtable m_dhttpMethod)
                                                      {
                                                          return SimulatorFeaturesCAP (m_dhttpMethod);
                                                      }));
            return retVal;
        }

        private Hashtable SimulatorFeaturesCAP (Hashtable mDhttpMethod)
        {
            //OSDMap rm = (OSDMap)OSDParser.DeserializeLLSDXml((string)mDhttpMethod["requestbody"]);


            OSDMap data = new OSDMap ();
            data["MeshRezEnabled"] = true;
            data["MeshUploadEnabled"] = true;
            data["MeshXferEnabled"] = true;
            data["PhysicsMaterialsEnabled"] = true;
            OSDMap typesMap = new OSDMap ();

            typesMap["convex"] = true;
            typesMap["none"] = true;
            typesMap["prim"] = true;

            data["PhysicsShapeTypes"] = typesMap;


            //Send back data
            Hashtable responsedata = new Hashtable ();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = OSDParser.SerializeLLSDXmlString(data);
            return responsedata;
        }

        public void RegionLoaded (Scene scene)
        {
        }

        public void RemoveRegion (Scene scene)
        {
        }

        public void Close ()
        {
        }

        public string Name
        {
            get
            {
                return "SimulatorFeatures";
            }
        }

        public Type ReplaceableInterface
        {
            get
            {
                return null;
            }
        }
    }
}
