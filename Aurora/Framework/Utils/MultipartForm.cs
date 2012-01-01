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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace Aurora.Framework
{
    public static class MultipartForm
    {
        #region Helper Classes

        #region Nested type: Element

        public abstract class Element
        {
            public string Name;
        }

        #endregion

        #region Nested type: File

        public class File : Element
        {
            public string ContentType;
            public byte[] Data;
            public string Filename;

            public File(string name, string filename, string contentType, byte[] data)
            {
                Name = name;
                Filename = filename;
                ContentType = contentType;
                Data = data;
            }
        }

        #endregion

        #region Nested type: Parameter

        public class Parameter : Element
        {
            public string Value;

            public Parameter(string name, string value)
            {
                Name = name;
                Value = value;
            }
        }

        #endregion

        #endregion Helper Classes

        public static HttpWebResponse Post(HttpWebRequest request, List<Element> postParameters)
        {
            string boundary = Boundary();

            // Set up the request properties
            request.Method = "POST";
            request.ContentType = "multipart/form-data; boundary=" + boundary;

            #region Stream Writing

            using (MemoryStream formDataStream = new MemoryStream())
            {
                foreach (var param in postParameters)
                {
                    if (param is File)
                    {
                        File file = (File) param;

                        // Add just the first part of this param, since we will write the file data directly to the Stream
                        string header =
                            string.Format(
                                "--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\";\r\nContent-Type: {3}\r\n\r\n",
                                boundary,
                                file.Name,
                                !String.IsNullOrEmpty(file.Filename) ? file.Filename : "tempfile",
                                file.ContentType);

                        formDataStream.Write(Encoding.UTF8.GetBytes(header), 0, header.Length);
                        formDataStream.Write(file.Data, 0, file.Data.Length);
                    }
                    else
                    {
                        Parameter parameter = (Parameter) param;

                        string postData =
                            string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}\r\n",
                                          boundary,
                                          parameter.Name,
                                          parameter.Value);
                        formDataStream.Write(Encoding.UTF8.GetBytes(postData), 0, postData.Length);
                    }
                }

                // Add the end of the request
                byte[] footer = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
                formDataStream.Write(footer, 0, footer.Length);

                request.ContentLength = formDataStream.Length;

                // Copy the temporary stream to the network stream
                formDataStream.Seek(0, SeekOrigin.Begin);
                using (Stream requestStream = request.GetRequestStream())
                    formDataStream.CopyTo(requestStream, (int) formDataStream.Length);
            }

            #endregion Stream Writing

            return request.GetResponse() as HttpWebResponse;
        }

        private static string Boundary()
        {
            Random rnd = new Random();
            string formDataBoundary = String.Empty;

            while (formDataBoundary.Length < 15)
                formDataBoundary = formDataBoundary + rnd.Next();

            formDataBoundary = formDataBoundary.Substring(0, 15);
            formDataBoundary = "-----------------------------" + formDataBoundary;

            return formDataBoundary;
        }
    }

    public static class Extentions
    {
        #region Stream

        /// <summary>
        ///   Copies the contents of one stream to another, starting at the 
        ///   current position of each stream
        /// </summary>
        /// <param name = "copyFrom">The stream to copy from, at the position 
        ///   where copying should begin</param>
        /// <param name = "copyTo">The stream to copy to, at the position where 
        ///   bytes should be written</param>
        /// <param name = "maximumBytesToCopy">The maximum bytes to copy</param>
        /// <returns>The total number of bytes copied</returns>
        /// <remarks>
        ///   Copying begins at the streams' current positions. The positions are
        ///   NOT reset after copying is complete.
        /// </remarks>
        public static int CopyTo(this Stream copyFrom, Stream copyTo, int maximumBytesToCopy)
        {
            byte[] buffer = new byte[4096];
            int readBytes;
            int totalCopiedBytes = 0;

            while ((readBytes = copyFrom.Read(buffer, 0, Math.Min(4096, maximumBytesToCopy))) > 0)
            {
                int writeBytes = Math.Min(maximumBytesToCopy, readBytes);
                copyTo.Write(buffer, 0, writeBytes);
                totalCopiedBytes += writeBytes;
                maximumBytesToCopy -= writeBytes;
            }

            return totalCopiedBytes;
        }

        /// <summary>
        ///   Converts an entire stream to a string, regardless of current stream
        ///   position
        /// </summary>
        /// <param name = "stream">The stream to convert to a string</param>
        /// <returns></returns>
        /// <remarks>
        ///   When this method is done, the stream position will be 
        ///   reset to its previous position before this method was called
        /// </remarks>
        public static string GetStreamString(this Stream stream)
        {
            string value = null;

            if (stream != null && stream.CanRead)
            {
                long rewindPos = -1;

                if (stream.CanSeek)
                {
                    rewindPos = stream.Position;
                    stream.Seek(0, SeekOrigin.Begin);
                }

                StreamReader reader = new StreamReader(stream);
                value = reader.ReadToEnd();

                if (rewindPos >= 0)
                    stream.Seek(rewindPos, SeekOrigin.Begin);
            }

            return value;
        }

        #endregion

        #region Uri

        /// <summary>
        ///   Combines a Uri that can contain both a base Uri and relative path
        ///   with a second relative path fragment
        /// </summary>
        /// <param name = "uri">Starting (base) Uri</param>
        /// <param name = "fragment">Relative path fragment to append to the end
        ///   of the Uri</param>
        /// <returns>The combined Uri</returns>
        /// <remarks>
        ///   This is similar to the Uri constructor that takes a base
        ///   Uri and the relative path, except this method can append a relative
        ///   path fragment on to an existing relative path
        /// </remarks>
        public static Uri Combine(this Uri uri, string fragment)
        {
            string fragment1 = uri.Fragment;
            string fragment2 = fragment;

            if (!fragment1.EndsWith("/"))
                fragment1 = fragment1 + '/';
            if (fragment2.StartsWith("/"))
                fragment2 = fragment2.Substring(1);

            return new Uri(uri, fragment1 + fragment2);
        }

        /// <summary>
        ///   Combines a Uri that can contain both a base Uri and relative path
        ///   with a second relative path fragment. If the fragment is absolute,
        ///   it will be returned without modification
        /// </summary>
        /// <param name = "uri">Starting (base) Uri</param>
        /// <param name = "fragment">Relative path fragment to append to the end
        ///   of the Uri, or an absolute Uri to return unmodified</param>
        /// <returns>The combined Uri</returns>
        public static Uri Combine(this Uri uri, Uri fragment)
        {
            if (fragment.IsAbsoluteUri)
                return fragment;

            string fragment1 = uri.Fragment;
            string fragment2 = fragment.ToString();

            if (!fragment1.EndsWith("/"))
                fragment1 = fragment1 + '/';
            if (fragment2.StartsWith("/"))
                fragment2 = fragment2.Substring(1);

            return new Uri(uri, fragment1 + fragment2);
        }

        /// <summary>
        ///   Appends a query string to a Uri that may or may not have existing 
        ///   query parameters
        /// </summary>
        /// <param name = "uri">Uri to append the query to</param>
        /// <param name = "query">Query string to append. Can either start with ?
        ///   or just containg key/value pairs</param>
        /// <returns>String representation of the Uri with the query string
        ///   appended</returns>
        public static string AppendQuery(this Uri uri, string query)
        {
            if (String.IsNullOrEmpty(query))
                return uri.ToString();

            if (query[0] == '?' || query[0] == '&')
                query = query.Substring(1);

            string uriStr = uri.ToString();

            if (uriStr.Contains("?"))
                return uriStr + '&' + query;
            else
                return uriStr + '?' + query;
        }

        #endregion Uri

        ///<summary>
        ///</summary>
        ///<param name = "collection"></param>
        ///<param name = "key"></param>
        ///<returns></returns>
        public static string GetOne(this NameValueCollection collection, string key)
        {
            string[] values = collection.GetValues(key);
            if (values != null && values.Length > 0)
                return values[0];

            return null;
        }
    }
}