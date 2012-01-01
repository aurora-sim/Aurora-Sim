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

using System;
using System.IO;
using System.Net;
using System.Reflection;

namespace Aurora.Framework.Servers.HttpServer
{
    public class SynchronousRestFormsRequester
    {
        ///<summary>
        ///  Perform a synchronous REST request.
        ///</summary>
        ///<param name = "verb"></param>
        ///<param name = "requestUrl"></param>
        ///<param name = "obj"> </param>
        ///<returns></returns>
        ///<exception cref = "System.Net.WebException">Thrown if we encounter a network issue while posting
        ///  the request.  You'll want to make sure you deal with this as they're not uncommon</exception>
        public static string MakeRequest(string verb, string requestUrl, string obj)
        {
            WebRequest request;
            try
            {
                request = WebRequest.Create(requestUrl);
            }
            catch
            {
                return "";
            }
            request.Method = verb;
            request.Timeout = 10000;
            string respstring = String.Empty;

            using (MemoryStream buffer = new MemoryStream())
            {
                if ((verb == "POST") || (verb == "PUT"))
                {
                    request.ContentType = "text/www-form-urlencoded";

                    int length = 0;
                    using (StreamWriter writer = new StreamWriter(buffer))
                    {
                        writer.Write(obj);
                        writer.Flush();
                    }

                    length = obj.Length;
                    request.ContentLength = length;

                    Stream requestStream = null;
                    try
                    {
                        requestStream = request.GetRequestStream();
                        requestStream.Write(buffer.ToArray(), 0, length);
                    }
                    catch (Exception e)
                    {
                        MainConsole.Instance.DebugFormat("[FORMS]: exception occured on sending request to {0}: " + e, requestUrl);
                        return (respstring);
                    }
                    finally
                    {
                        if (requestStream != null)
                            requestStream.Close();
                    }
                }

                try
                {
                    using (WebResponse resp = request.GetResponse())
                    {
                        if (resp.ContentLength != 0)
                        {
                            Stream respStream = null;
                            try
                            {
                                respStream = resp.GetResponseStream();
                                if (respStream != null)
                                    using (StreamReader reader = new StreamReader(respStream))
                                    {
                                        respstring = reader.ReadToEnd();
                                    }
                            }
                            catch (Exception e)
                            {
                                MainConsole.Instance.DebugFormat("[FORMS]: exception occured on receiving reply " + e);
                            }
                            finally
                            {
                                if (respStream != null)
                                    respStream.Close();
                            }
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    // This is what happens when there is invalid XML
                    MainConsole.Instance.DebugFormat("[FORMS]: InvalidOperationException on receiving request");
                }
            }
            return respstring;
        }
    }
}