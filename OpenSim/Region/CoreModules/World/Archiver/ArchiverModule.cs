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
using NDesk.Options;
using Nini.Config;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.CoreModules.World.Archiver
{
    /// <summary>
    /// This module loads and saves OpenSimulator region archives
    /// </summary>
    public class ArchiverModule : INonSharedRegionModule, IRegionArchiverModule
    {
        private static readonly ILog m_log = 
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_scene;

        /// <value>
        /// The file used to load and save an opensimulator archive if no filename has been specified
        /// </value>
        protected const string DEFAULT_OAR_BACKUP_FILENAME = "region.oar";

        public string Name 
        { 
            get { return "RegionArchiverModule"; } 
        }

        public Type ReplaceableInterface 
        { 
            get { return null; }
        }


        public void Initialise(IConfigSource source)
        {
            //m_log.Debug("[ARCHIVER] Initialising");
        }

        public void AddRegion(Scene scene)
        {
            m_scene = scene;
            m_scene.RegisterModuleInterface<IRegionArchiverModule>(this);
            //m_log.DebugFormat("[ARCHIVER]: Enabled for region {0}", scene.RegionInfo.RegionName);
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void Close()
        {
        }

        /// <summary>
        /// Load a whole region from an opensimulator archive.
        /// </summary>
        /// <param name="cmdparams"></param>
        public void HandleLoadOarConsoleCommand(string module, string[] cmdparams)
        {
            bool mergeOar = false;
            bool skipAssets = false;
            
            OptionSet options = new OptionSet().Add("m|merge", delegate (string v) { mergeOar = v != null; });
            options.Add("s|skip-assets", delegate(string v) { skipAssets = v != null; });
            
            List<string> mainParams = options.Parse(cmdparams);

            int offsetX = 0;
            int offsetY = 0;
            int offsetZ = 0;
            bool flipX = false;
            bool flipY = false;

            int i = 0;
            foreach (string param in mainParams)
            {
                if (param.StartsWith("OffsetX", StringComparison.CurrentCultureIgnoreCase))
                {
                    string retVal = param.Remove(0, 8);
                    int.TryParse(retVal, out offsetX);
                }
                if (param.StartsWith("OffsetY", StringComparison.CurrentCultureIgnoreCase))
                {
                    string retVal = param.Remove(0, 8);
                    int.TryParse(retVal, out offsetY);
                }
                if (param.StartsWith("OffsetZ", StringComparison.CurrentCultureIgnoreCase))
                {
                    string retVal = param.Remove(0, 8);
                    int.TryParse(retVal, out offsetZ);
                }
                if (param.StartsWith("FlipX", StringComparison.CurrentCultureIgnoreCase))
                {
                    flipX = true;
                }
                if (param.StartsWith("FlipY", StringComparison.CurrentCultureIgnoreCase))
                {
                    flipY = true;
                }
                i++;
            }
          
//            m_log.DebugFormat("MERGE OAR IS [{0}]", mergeOar);
//
//            foreach (string param in mainParams)
//                m_log.DebugFormat("GOT PARAM [{0}]", param);
            
            if (mainParams.Count > 2)
            {
                DearchiveRegion(mainParams[2], mergeOar, skipAssets, offsetX, offsetY, offsetZ, flipX, flipY);
            }
            else
            {
                DearchiveRegion(DEFAULT_OAR_BACKUP_FILENAME, mergeOar, skipAssets, offsetX, offsetY, offsetZ, flipX, flipY);
            }
        }

        /// <summary>
        /// Save a region to a file, including all the assets needed to restore it.
        /// </summary>
        /// <param name="cmdparams"></param>
        public void HandleSaveOarConsoleCommand(string module, string[] cmdparams)
        {
            if (cmdparams.Length > 2)
            {
                ArchiveRegion(cmdparams[2]);
            }
            else
            {
                ArchiveRegion(DEFAULT_OAR_BACKUP_FILENAME);
            }
        }
        
        public void ArchiveRegion(string savePath)
        {
            ArchiveRegion(savePath, Guid.Empty);
        }
        
        public void ArchiveRegion(string savePath, Guid requestId)
        {
            m_log.InfoFormat(
                "[ARCHIVER]: Writing archive for region {0} to {1}", m_scene.RegionInfo.RegionName, savePath);
            
            new ArchiveWriteRequestPreparation(m_scene, savePath, requestId).ArchiveRegion();
        }
        
        public void ArchiveRegion(Stream saveStream)
        {
            ArchiveRegion(saveStream, Guid.Empty);
        }
        
        public void ArchiveRegion(Stream saveStream, Guid requestId)
        {
            new ArchiveWriteRequestPreparation(m_scene, saveStream, requestId).ArchiveRegion();
        }

        public void DearchiveRegion(string loadPath)
        {
            DearchiveRegion(loadPath, false, false, 0, 0, 0, false, false);
        }

        public void DearchiveRegion(string loadPath, bool merge, bool skipAssets, int offsetX, int offsetY, int offsetZ, bool flipX, bool flipY)
        {
            m_log.InfoFormat(
                "[ARCHIVER]: Loading archive to region {0} from {1}", m_scene.RegionInfo.RegionName, loadPath);

            new ArchiveReadRequest(m_scene, loadPath, merge, skipAssets, offsetX, offsetY, offsetZ, flipX, flipY).DearchiveRegion();
        }
        
        public void DearchiveRegion(Stream loadStream)
        {
            DearchiveRegion(loadStream, false, false, 0, 0, 0);
        }

        public void DearchiveRegion(Stream loadStream, bool merge, bool skipAssets, int offsetX, int offsetY, int offsetZ)
        {
            new ArchiveReadRequest(m_scene, loadStream, merge, skipAssets, offsetX, offsetY, offsetZ).DearchiveRegion();
        }
    }
}
