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
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.SceneInfo.Entities;
using Aurora.Framework.Serialization;
using Aurora.Framework.Serialization.External;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Aurora.Modules.Archivers
{
    /// <summary>
    ///     Method called when all the necessary assets for an archive request have been received.
    /// </summary>
    public delegate void AssetsRequestCallback(
        ICollection<UUID> assetsFoundUuids, ICollection<UUID> assetsNotFoundUuids);

    /// <summary>
    ///     Execute the write of an archive once we have received all the necessary data
    /// </summary>
    public class ArchiveWriteRequestExecution
    {
        protected TarArchiveWriter m_archiveWriter;
        protected Guid m_requestId;
        protected IScene m_scene;
        protected List<ISceneEntity> m_sceneObjects;
        protected IRegionSerialiserModule m_serialiser;
        protected ITerrainModule m_terrainModule;

        public ArchiveWriteRequestExecution(
            List<ISceneEntity> sceneObjects,
            ITerrainModule terrainModule,
            IRegionSerialiserModule serialiser,
            IScene scene,
            TarArchiveWriter archiveWriter,
            Guid requestId)
        {
            m_sceneObjects = sceneObjects;
            m_terrainModule = terrainModule;
            m_serialiser = serialiser;
            m_scene = scene;
            m_archiveWriter = archiveWriter;
            m_requestId = requestId;
        }

        protected internal void ReceivedAllAssets(
            ICollection<UUID> assetsFoundUuids, ICollection<UUID> assetsNotFoundUuids)
        {
            try
            {
                Save(assetsFoundUuids, assetsNotFoundUuids);
            }
            finally
            {
                m_archiveWriter.Close();
            }

            MainConsole.Instance.InfoFormat("[ARCHIVER]: Finished writing out OAR for {0}",
                                            m_scene.RegionInfo.RegionName);

            m_scene.EventManager.TriggerOarFileSaved(m_requestId, String.Empty);
        }

        protected internal void Save(ICollection<UUID> assetsFoundUuids, ICollection<UUID> assetsNotFoundUuids)
        {
            foreach (UUID uuid in assetsNotFoundUuids)
            {
                MainConsole.Instance.DebugFormat("[ARCHIVER]: Could not find asset {0}", uuid);
            }

//            MainConsole.Instance.InfoFormat(
//                "[ARCHIVER]: Received {0} of {1} assets requested",
//                assetsFoundUuids.Count, assetsFoundUuids.Count + assetsNotFoundUuids.Count);

            MainConsole.Instance.InfoFormat("[ARCHIVER]: Creating archive file.  This may take some time.");

            // Write out control file
            m_archiveWriter.WriteFile(ArchiveConstants.CONTROL_FILE_PATH, Create0p2ControlFile());
            MainConsole.Instance.InfoFormat("[ARCHIVER]: Added control file to archive.");

            // Write out region settings
            string settingsPath
                = String.Format("{0}{1}.xml", ArchiveConstants.SETTINGS_PATH, m_scene.RegionInfo.RegionName);
            m_archiveWriter.WriteFile(settingsPath,
                                      RegionSettingsSerializer.Serialize(m_scene.RegionInfo.RegionSettings));

            MainConsole.Instance.InfoFormat("[ARCHIVER]: Added region settings to archive.");

            // Write out land data (aka parcel) settings
            IParcelManagementModule parcelManagement = m_scene.RequestModuleInterface<IParcelManagementModule>();
            if (parcelManagement != null)
            {
                List<ILandObject> landObjects = parcelManagement.AllParcels();
                foreach (ILandObject lo in landObjects)
                {
                    LandData landData = lo.LandData;
                    string landDataPath = String.Format("{0}{1}.xml", ArchiveConstants.LANDDATA_PATH,
                                                        landData.GlobalID.ToString());
                    m_archiveWriter.WriteFile(landDataPath, LandDataSerializer.Serialize(landData));
                }
            }
            MainConsole.Instance.InfoFormat("[ARCHIVER]: Added parcel settings to archive.");

            // Write out terrain
            string terrainPath
                = String.Format("{0}{1}.r32", ArchiveConstants.TERRAINS_PATH, m_scene.RegionInfo.RegionName);

            MemoryStream ms = new MemoryStream();
            m_terrainModule.SaveToStream(m_terrainModule.TerrainMap, terrainPath, ms);
            m_archiveWriter.WriteFile(terrainPath, ms.ToArray());
            ms.Close();

            MainConsole.Instance.InfoFormat("[ARCHIVER]: Added terrain information to archive.");

            // Write out scene object metadata
            foreach (ISceneEntity sceneObject in m_sceneObjects)
            {
                //MainConsole.Instance.DebugFormat("[ARCHIVER]: Saving {0} {1}, {2}", entity.Name, entity.UUID, entity.GetType());

                string serializedObject = m_serialiser.SerializeGroupToXml2(sceneObject);
                m_archiveWriter.WriteFile(ArchiveHelpers.CreateObjectPath(sceneObject), serializedObject);
            }

            MainConsole.Instance.InfoFormat("[ARCHIVER]: Added scene objects to archive.");
        }

        /// <summary>
        ///     Create the control file for a 0.2 version archive
        /// </summary>
        /// <returns></returns>
        public static string Create0p2ControlFile()
        {
            StringWriter sw = new StringWriter();
            XmlTextWriter xtw = new XmlTextWriter(sw) {Formatting = Formatting.Indented};
            xtw.WriteStartDocument();
            xtw.WriteStartElement("archive");
            xtw.WriteAttributeString("major_version", "0");
            xtw.WriteAttributeString("minor_version", "3");

            xtw.WriteStartElement("creation_info");
            DateTime now = DateTime.UtcNow;
            TimeSpan t = now - new DateTime(1970, 1, 1);
            xtw.WriteElementString("datetime", ((int) t.TotalSeconds).ToString());
            xtw.WriteElementString("id", UUID.Random().ToString());
            xtw.WriteEndElement();
            xtw.WriteEndElement();

            xtw.Flush();
            xtw.Close();

            String s = sw.ToString();
            sw.Close();

            return s;
        }
    }
}