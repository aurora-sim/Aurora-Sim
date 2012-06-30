/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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

using System.IO;

namespace Aurora.Framework.Servers.HttpServer
{
    public delegate byte[] HttpServerHandle(string path, Stream request,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse);
    public class GenericStreamHandler : BaseRequestHandler
    {
        private HttpServerHandle _method;
        public GenericStreamHandler(string httpMethod, string path, HttpServerHandle method) 
            : base(httpMethod, path)
        {
            _method = method;
        }

        public override byte[] Handle(string path, Stream request, OSHttpRequest httpRequest,
                                      OSHttpResponse httpResponse)
        {
            return _method(path, request, httpRequest, httpResponse);
        }
    }

    public class HttpServerHandlerHelpers
    {
        public static byte[] ReadFully(Stream stream)
        {
            byte[] buffer = new byte[1024];
            using (MemoryStream ms = new MemoryStream(1024 * 256))
            {
                while (true)
                {
                    int read = stream.Read(buffer, 0, buffer.Length);

                    if (read <= 0)
                    {
                        return ms.ToArray();
                    }

                    ms.Write(buffer, 0, read);
                }
            }
        }
    }

    public static class MyExtensions
    {
        public static string ReadUntilEnd(this System.IO.Stream stream)
        {
            StreamReader rdr = new StreamReader(stream);
            string str = rdr.ReadToEnd();
            rdr.Close();
            return str;
        }
    }
}