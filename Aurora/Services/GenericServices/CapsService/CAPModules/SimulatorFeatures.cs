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
using System.IO;
using OpenMetaverse.StructuredData;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework;

namespace Aurora.Services
{
    public class SimulatorFeatures : ICapsServiceConnector
    {
        private IRegionClientCapsService m_service;

        #region ICapsServiceConnector Members

        public void RegisterCaps(IRegionClientCapsService service)
        {
            m_service = service;

            m_service.AddStreamHandler("SimulatorFeatures",
                                       new GenericStreamHandler("GET", m_service.CreateCAPS("SimulatorFeatures", ""),
                                                           SimulatorFeaturesCAP));
        }

        public void DeregisterCaps()
        {
            m_service.RemoveStreamHandler("SimulatorFeatures", "GET");
        }

        public void EnteringRegion()
        {
        }

        #endregion

        private byte[] SimulatorFeaturesCAP(string path, Stream request,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            OSDMap data = new OSDMap();
            data["MeshRezEnabled"] = true;
            data["MeshUploadEnabled"] = true;
            data["MeshXferEnabled"] = true;
            data["PhysicsMaterialsEnabled"] = true;


            OSDMap typesMap = new OSDMap();

            typesMap["convex"] = true;
            typesMap["none"] = true;
            typesMap["prim"] = true;

            data["PhysicsShapeTypes"] = typesMap;


            //Data URLS need sent as well
            //Not yet...
            //data["DataUrls"] = m_service.Registry.RequestModuleInterface<IGridRegistrationService> ().GetUrlForRegisteringClient (m_service.AgentID + "|" + m_service.RegionHandle);

            //Send back data
            return OSDParser.SerializeLLSDXmlBytes(data);
        }
    }
}