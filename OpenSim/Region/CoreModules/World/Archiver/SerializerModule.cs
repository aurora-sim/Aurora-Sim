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

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using log4net;
using Nini.Config;

using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;

namespace OpenSim.Region.CoreModules.World.Serialiser
{
    public class SerialiserModule : ISharedRegionModule, IRegionSerialiserModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region ISharedRegionModule Members

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(IConfigSource source)
        {
        }

        public void PostInitialise()
        {
        }


        public void AddRegion(Scene scene)
        {
            scene.RegisterModuleInterface<IRegionSerialiserModule>(this);
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
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

        #region IRegionSerialiser Members

        public void LoadPrimsFromXml(Scene scene, string fileName, bool newIDS, Vector3 loadOffset)
        {
            SceneXmlLoader.LoadPrimsFromXml(scene, fileName, newIDS, loadOffset);
        }

        public void SavePrimsToXml(Scene scene, string fileName)
        {
            SceneXmlLoader.SavePrimsToXml(scene, fileName);
        }

        public void LoadPrimsFromXml2(Scene scene, string fileName)
        {
            SceneXmlLoader.LoadPrimsFromXml2(scene, fileName);
        }

        public void LoadPrimsFromXml2(Scene scene, TextReader reader, bool startScripts)
        {
            SceneXmlLoader.LoadPrimsFromXml2(scene, reader, startScripts);
        }

        public void SavePrimsToXml2(Scene scene, string fileName)
        {
            SceneXmlLoader.SavePrimsToXml2(scene, fileName);
        }

        public void SavePrimsToXml2(Scene scene, TextWriter stream, Vector3 min, Vector3 max)
        {
            SceneXmlLoader.SavePrimsToXml2(scene, stream, min, max);
        }

        public void SaveNamedPrimsToXml2(Scene scene, string primName, string fileName)
        {
            SceneXmlLoader.SaveNamedPrimsToXml2(scene, primName, fileName);
        }

        public SceneObjectGroup DeserializeGroupFromXml2(string xmlString, Scene scene)
        {
            return SceneXmlLoader.DeserializeGroupFromXml2(xmlString, scene);
        }

        public string SerializeGroupToXml2(SceneObjectGroup grp)
        {
            return SceneXmlLoader.SaveGroupToXml2(grp);
        }

        public void SavePrimListToXml2(EntityBase[] entityList, string fileName)
        {
            SceneXmlLoader.SavePrimListToXml2(entityList, fileName);
        }

        public void SavePrimListToXml2(EntityBase[] entityList, TextWriter stream, Vector3 min, Vector3 max)
        {
            SceneXmlLoader.SavePrimListToXml2(entityList, stream, min, max);
        }

        #endregion
    }
}