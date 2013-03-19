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

using Aurora.Framework;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Services;
using OpenMetaverse.StructuredData;
using System;
using System.IO;
using System.Text;

namespace Aurora.Services
{
    public class InstantMessageCAPS : ICapsServiceConnector
    {
        protected IInstantMessagingService m_imService;
        protected IRegionClientCapsService m_service;

        public void RegisterCaps(IRegionClientCapsService service)
        {
            m_service = service;
            m_imService = service.Registry.RequestModuleInterface<IInstantMessagingService>();
            if (m_imService != null)
            {
                service.AddStreamHandler("ChatSessionRequest",
                                         new GenericStreamHandler("POST", service.CreateCAPS("ChatSessionRequest", ""),
                                                                  ChatSessionRequest));
            }
        }

        public void EnteringRegion()
        {
        }

        public void DeregisterCaps()
        {
            m_service.RemoveStreamHandler("ChatSessionRequest", "POST");
        }

        #region Baked Textures

        public byte[] ChatSessionRequest(string path, Stream request, OSHttpRequest httpRequest,
                                         OSHttpResponse httpResponse)
        {
            try
            {
                OSDMap rm = (OSDMap) OSDParser.DeserializeLLSDXml(request);

                return Encoding.UTF8.GetBytes(m_imService.ChatSessionRequest(m_service, rm));
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[IMCAPS]: " + e.ToString());
            }

            return null;
        }

        #endregion
    }
}