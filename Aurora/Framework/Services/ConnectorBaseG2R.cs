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
using System.Diagnostics;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Interfaces;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
    public class ConnectorBaseG2R : ConnectorBase
    {
        private IGenericsConnector m_genericsConnector;

        public void Init(IRegistryCore registry, string name)
        {
            base.Init(registry, name);
            //flip it
            IConfigSource source = registry.RequestModuleInterface<ISimulationBase>().ConfigSource;
            IConfig config;
            m_doRemoteCalls = (config = source.Configs["AuroraConnectors"]) == null || config.GetBoolean("DoRemoteCalls", true);
            SetDoRemoteCalls(!m_doRemoteCalls);
            //this should be the grid server
            if (m_doRemoteCalls)
                m_genericsConnector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();
        }

        protected override List<string> GetURIs(bool urlOverrides, OSDMap map, string url, string userID)
        {
            List<string> returnValue = new List<string>();
            if (!map.Keys.Contains("regionID")) return returnValue;
            UUID regionid = (UUID)Util.OSDToObject(map["regionID"], typeof(UUID));

            List<RegionRegistrationURLs> urls = m_genericsConnector.GetGenerics<RegionRegistrationURLs>(regionid, "RegionRegistrationUrls");
            foreach (RegionRegistrationURLs regionRegistrationUrLse in urls)
            {
                returnValue.Add(regionRegistrationUrLse.URLS["regionURI"].AsString());
            }
            return returnValue;
        }
    }

    public class RegionRegistrationURLs : IDataTransferable
    {
        public OSDMap URLS;
        public string SessionID;
        public override OSDMap ToOSD()
        {
            OSDMap retVal = new OSDMap();
            retVal["URLS"] = URLS;
            retVal["SessionID"] = SessionID;
            return retVal;
        }

        public override void FromOSD(OSDMap retVal)
        {
            URLS = (OSDMap)retVal["URLS"];
            SessionID = retVal["SessionID"].AsString();
        }
    }

}