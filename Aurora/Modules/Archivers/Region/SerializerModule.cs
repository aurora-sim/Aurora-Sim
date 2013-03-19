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

using Aurora.Framework;
using Aurora.Framework.Serialization;
using Nini.Config;
using System;
using System.IO;
using System.Xml;

namespace Aurora.Modules.Archivers
{
    public class SerializerModule : INonSharedRegionModule, IRegionSerialiserModule
    {
        #region IRegionSerialiserModule Members

        public ISceneEntity DeserializeGroupFromXml2(string xmlString, IScene scene)
        {
            XmlDocument doc = new XmlDocument();

            XmlTextReader reader = new XmlTextReader(new StringReader(xmlString))
                                       {
                                           WhitespaceHandling =
                                               WhitespaceHandling.None
                                       };
            doc.Load(reader);
            reader.Close();
            XmlNode rootNode = doc.FirstChild;

            return SceneEntitySerializer.SceneObjectSerializer.FromXml2Format(rootNode.OuterXml, scene);
        }

        public ISceneEntity DeserializeGroupFromXml2(byte[] xml, IScene scene)
        {
            XmlDocument doc = new XmlDocument();

            MemoryStream stream = new MemoryStream(xml);
            XmlTextReader reader = new XmlTextReader(stream) {WhitespaceHandling = WhitespaceHandling.None};
            doc.Load(reader);
            reader.Close();
            stream.Close();
            XmlNode rootNode = doc.FirstChild;

            return SceneEntitySerializer.SceneObjectSerializer.FromXml2Format(rootNode.OuterXml, scene);
        }

        public string SerializeGroupToXml2(ISceneEntity grp)
        {
            return SceneEntitySerializer.SceneObjectSerializer.ToXml2Format(grp);
        }

        #endregion

        #region INonSharedRegionModule Members

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion(IScene scene)
        {
            scene.RegisterModuleInterface<IRegionSerialiserModule>(this);
        }

        public void RegionLoaded(IScene scene)
        {
        }

        public void RemoveRegion(IScene scene)
        {
            scene.UnregisterModuleInterface<IRegionSerialiserModule>(this);
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "ExportSerialisationModule"; }
        }

        #endregion
    }
}