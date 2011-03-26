/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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

using Nini.Config;
using log4net;
using System;
using System.Reflection;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.MessagingService
{
    public class MessagingServiceInPostHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private MessagingServiceInHandler m_handler;
        private ulong m_ourRegionHandle;

        public MessagingServiceInPostHandler(string url, IRegistryCore registry, MessagingServiceInHandler handler, ulong handle) :
                base("POST", url)
        {
            m_handler = handler;
            m_ourRegionHandle = handle;
        }

        public override byte[] Handle(string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            //m_log.DebugFormat("[XXX]: query String: {0}", body);

            try
            {
                OSDMap map = WebUtils.GetOSDMap(body);
                if (map != null)
                {
                    return NewMessage(map);
                }
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[GRID HANDLER]: Exception {0}", e);
            }

            return FailureResult();
        }

        private byte[] NewMessage(OSDMap map)
        {
            OSDMap message = (OSDMap)map["Message"];
            if(m_ourRegionHandle != 0)
                ((OSDMap)message)["RegionHandle"] = m_ourRegionHandle;
            OSDMap result = m_handler.FireMessageReceived(message);
            if (result != null)
                return ReturnResult(result);
            else
                return SuccessResult();
        }

        private byte[] FailureResult()
        {
            OSDMap map = new OSDMap();
            map["Success"] = false;
            return ReturnResult(map);
        }

        private byte[] SuccessResult()
        {
            OSDMap map = new OSDMap();
            map["Success"] = true;
            return ReturnResult(map);
        }

        private byte[] ReturnResult(OSDMap map)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            string m = OSDParser.SerializeJsonString(map);
            return encoding.GetBytes(m);
        }
    }
}
