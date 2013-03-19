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

namespace Aurora.Framework.Utilities
{
    public class FileSaving
    {
        private string fileName;

        public FileSaving()
        {
        }

        public FileSaving(string file)
        {
            fileName = file;
        }

        public static T LoadFromFile<T>(string file) where T : FileSaving
        {
            try
            {
                if (file == "" || !System.IO.File.Exists(file))
                    return null;
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(typeof (T));
                var stream = System.IO.File.OpenRead(file);
                System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                doc.Load(stream);
                FileSaving config;
                if (typeof (T).Name != "opencv_storage")
                {
                    System.IO.MemoryStream r =
                        new System.IO.MemoryStream(
                            System.Text.Encoding.UTF8.GetBytes(xmlHeader + doc.DocumentElement.InnerXml));
                    config = (FileSaving) x.Deserialize(r);
                }
                else
                {
                    stream.Close();
                    stream = System.IO.File.OpenRead(file);
                    config = (FileSaving) x.Deserialize(stream);
                }

                stream.Close();
                config.fileName = file;
                return (T) config;
            }
            catch
            {
            }
            return null;
        }

        private static object _lock = new object();
        private const string xmlHeader = "<?xml version=\"1.0\"?>";

        public static void SaveToFile(string file, FileSaving config)
        {
            lock (_lock)
            {
                var xns = new System.Xml.Serialization.XmlSerializerNamespaces();
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(config.GetType());
                System.IO.Stream stream;
                if (config.GetType().Name != "opencv_storage")
                    stream = new System.IO.MemoryStream();
                else
                {
                    System.IO.File.WriteAllText(file, "");
                    stream = System.IO.File.OpenWrite(file);
                }
                xns.Add(string.Empty, string.Empty);
                x.Serialize(stream, config, xns);
                if (config.GetType().Name != "opencv_storage")
                {
                    stream.Position = 0;
                    byte[] bs = new byte[stream.Length];
                    ((System.IO.MemoryStream) stream).Read(bs, 0, bs.Length);
                    string s = xmlHeader + "<opencv_storage>" +
                               System.Text.Encoding.UTF8.GetString(bs).Replace(xmlHeader, "") + "</opencv_storage>";
                    System.IO.File.WriteAllText(file, s);
                }
                stream.Close();
            }
        }

        public virtual void Save()
        {
            FileSaving.SaveToFile(fileName, this);
        }
    }
}