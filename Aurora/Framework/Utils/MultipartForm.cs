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

    /// <summary>
    /// Stream-based multipart handling.
    ///
    /// In this incarnation deals with an HttpInputStream as we are now using
    /// IntPtr-based streams instead of byte [].   In the future, we will also
    /// send uploads above a certain threshold into the disk (to implement
    /// limit-less HttpInputFiles). 
    /// </summary>
    /// <remarks>
    /// Taken from HttpRequest in mono (http://www.mono-project.com)
    /// </remarks>
    internal class HttpMultipart
    {
        private const byte CR = (byte)'\r';
        private const byte LF = (byte)'\n';
        private readonly string boundary;
        private readonly byte[] boundary_bytes;
        private readonly byte[] buffer;
        private readonly Stream data;
        private readonly Encoding encoding;
        private readonly StringBuilder sb;
        private bool _atEof;

        // See RFC 2046 
        // In the case of multipart entities, in which one or more different
        // sets of data are combined in a single body, a "multipart" media type
        // field must appear in the entity's header.  The body must then contain
        // one or more body parts, each preceded by a boundary delimiter line,
        // and the last one followed by a closing boundary delimiter line.
        // After its boundary delimiter line, each body part then consists of a
        // header area, a blank line, and a body area.  Thus a body part is
        // similar to an RFC 822 message in syntax, but different in meaning.

        public HttpMultipart(Stream data, string b, Encoding encoding)
        {
            this.data = data;
            boundary = b;
            boundary_bytes = encoding.GetBytes(b);
            buffer = new byte[boundary_bytes.Length + 2]; // CRLF or '--'
            this.encoding = encoding;
            sb = new StringBuilder();
        }

        private bool CompareBytes(byte[] orig, byte[] other)
        {
            for (var i = orig.Length - 1; i >= 0; i--)
                if (orig[i] != other[i])
                    return false;

            return true;
        }

        private static string GetContentDispositionAttribute(string l, string name)
        {
            var idx = l.IndexOf(name + "=\"");
            if (idx < 0)
                return null;
            var begin = idx + name.Length + "=\"".Length;
            var end = l.IndexOf('"', begin);
            if (end < 0)
                return null;
            if (begin == end)
                return "";
            return l.Substring(begin, end - begin);
        }

        private string GetContentDispositionAttributeWithEncoding(string l, string name)
        {
            var idx = l.IndexOf(name + "=\"");
            if (idx < 0)
                return null;
            var begin = idx + name.Length + "=\"".Length;
            var end = l.IndexOf('"', begin);
            if (end < 0)
                return null;
            if (begin == end)
                return "";

            var temp = l.Substring(begin, end - begin);
            var source = new byte[temp.Length];
            for (var i = temp.Length - 1; i >= 0; i--)
                source[i] = (byte)temp[i];

            return encoding.GetString(source);
        }

        private long MoveToNextBoundary()
        {
            long retval = 0;
            var got_cr = false;

            var state = 0;
            var c = data.ReadByte();
            while (true)
            {
                if (c == -1)
                    return -1;

                if (state == 0 && c == LF)
                {
                    retval = data.Position - 1;
                    if (got_cr)
                        retval--;
                    state = 1;
                    c = data.ReadByte();
                }
                else if (state == 0)
                {
                    got_cr = (c == CR);
                    c = data.ReadByte();
                }
                else if (state == 1 && c == '-')
                {
                    c = data.ReadByte();
                    if (c == -1)
                        return -1;

                    if (c != '-')
                    {
                        state = 0;
                        got_cr = false;
                        continue; // no ReadByte() here
                    }

                    var nread = data.Read(buffer, 0, buffer.Length);
                    var bl = buffer.Length;
                    if (nread != bl)
                        return -1;

                    if (!CompareBytes(boundary_bytes, buffer))
                    {
                        state = 0;
                        data.Position = retval + 2;
                        if (got_cr)
                        {
                            data.Position++;
                            got_cr = false;
                        }
                        c = data.ReadByte();
                        continue;
                    }

                    if (buffer[bl - 2] == '-' && buffer[bl - 1] == '-')
                    {
                        _atEof = true;
                    }
                    else if (buffer[bl - 2] != CR || buffer[bl - 1] != LF)
                    {
                        state = 0;
                        data.Position = retval + 2;
                        if (got_cr)
                        {
                            data.Position++;
                            got_cr = false;
                        }
                        c = data.ReadByte();
                        continue;
                    }
                    data.Position = retval + 2;
                    if (got_cr)
                        data.Position++;
                    break;
                }
                else
                {
                    // state == 1
                    state = 0; // no ReadByte() here
                }
            }

            return retval;
        }

        private bool ReadBoundary()
        {
            try
            {
                var line = ReadLine();
                while (line == "")
                    line = ReadLine();
                if (line[0] != '-' || line[1] != '-')
                    return false;

                if (!line.EndsWith(boundary, false, System.Globalization.CultureInfo.CurrentCulture))
                    return true;
            }
            catch
            {
            }

            return false;
        }

        private string ReadHeaders()
        {
            var s = ReadLine();
            if (s == "")
                return null;

            return s;
        }

        private string ReadLine()
        {
            // CRLF or LF are ok as line endings.
            var got_cr = false;
            var b = 0;
            sb.Length = 0;
            while (true)
            {
                b = data.ReadByte();
                if (b == -1)
                {
                    return null;
                }

                if (b == LF)
                {
                    break;
                }
                got_cr = (b == CR);
                sb.Append((char)b);
            }

            if (got_cr)
                sb.Length--;

            return sb.ToString();
        }

        public Element ReadNextElement()
        {
            if (_atEof || ReadBoundary())
                return null;

            var elem = new Element();
            string header;
            while ((header = ReadHeaders()) != null)
            {
                if (header.StartsWith("Content-Disposition:", true, System.Globalization.CultureInfo.CurrentCulture))
                {
                    elem.Name = GetContentDispositionAttribute(header, "name");
                    elem.Filename = StripPath(GetContentDispositionAttributeWithEncoding(header, "filename"));
                }
                else if (header.StartsWith("Content-Type:", true, System.Globalization.CultureInfo.CurrentCulture))
                {
                    elem.ContentType = header.Substring("Content-Type:".Length).Trim();
                }
            }

            var start = data.Position;
            elem.Start = start;
            var pos = MoveToNextBoundary();
            if (pos == -1)
                return null;

            elem.Length = pos - start;
            return elem;
        }

        private static string StripPath(string path)
        {
            if (path == null || path.Length == 0)
                return path;

            if (path.IndexOf(":\\") != 1 && !path.StartsWith("\\\\"))
                return path;
            return path.Substring(path.LastIndexOf('\\') + 1);
        }

        #region Nested type: Element

        public class Element
        {
            public string ContentType;
            public string Filename;
            public long Length;
            public string Name;
            public long Start;

            public override string ToString()
            {
                return "ContentType " + ContentType + ", Name " + Name + ", Filename " + Filename + ", Start " +
                       Start.ToString() + ", Length " + Length.ToString();
            }
        }

        #endregion
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