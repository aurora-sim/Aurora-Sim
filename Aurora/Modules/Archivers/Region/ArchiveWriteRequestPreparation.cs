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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using OpenMetaverse;
using Aurora.Framework;
using Aurora.Framework.Serialization;

namespace Aurora.Modules.Archivers
{
    /// <summary>
    ///   Prepare to write out an archive.
    /// </summary>
    public class ArchiveWriteRequestPreparation
    {
        protected string m_checkPermissions;
        protected Guid m_requestId;
        protected Stream m_saveStream;
        protected IScene m_scene;

        /// <summary>
        ///   Constructor
        /// </summary>
        /// <param name = "scene"></param>
        /// <param name = "savePath">The path to which to save data.</param>
        /// <param name = "requestId">The id associated with this request</param>
        /// <exception cref = "System.IO.IOException">
        ///   If there was a problem opening a stream for the file specified by the savePath
        /// </exception>
        public ArchiveWriteRequestPreparation(IScene scene, string savePath, Guid requestId, string checkPermissions)
        {
            m_scene = scene;

            try
            {
                m_saveStream = new GZipStream(new FileStream(savePath, FileMode.Create), CompressionMode.Compress);
            }
            catch (EntryPointNotFoundException e)
            {
                MainConsole.Instance.ErrorFormat(
                    "[ARCHIVER]: Mismatch between Mono and zlib1g library version when trying to create compression stream."
                    + "If you've manually installed Mono, have you appropriately updated zlib1g as well?");
                MainConsole.Instance.Error(e);
            }

            m_requestId = requestId;
            m_checkPermissions = checkPermissions;
        }

        /// <summary>
        ///   Constructor.
        /// </summary>
        /// <param name = "scene"></param>
        /// <param name = "saveStream">The stream to which to save data.</param>
        /// <param name = "requestId">The id associated with this request</param>
        public ArchiveWriteRequestPreparation(IScene scene, Stream saveStream, Guid requestId)
        {
            m_scene = scene;
            m_saveStream = saveStream;
            m_requestId = requestId;
        }

        /// <summary>
        ///   Archive the region requested.
        /// </summary>
        /// <exception cref = "System.IO.IOException">if there was an io problem with creating the file</exception>
        public void ArchiveRegion()
        {
            Dictionary<UUID, AssetType> assetUuids = new Dictionary<UUID, AssetType>();

            ISceneEntity[] entities = m_scene.Entities.GetEntities();
            List<ISceneEntity> sceneObjects = new List<ISceneEntity>();
            int numObjectsSkippedPermissions = 0;

            // Filter entities so that we only have scene objects.
            // FIXME: Would be nicer to have this as a proper list in SceneGraph, since lots of methods
            // end up having to do this
            foreach (ISceneEntity entity in entities.Where(entity => !entity.IsDeleted && !entity.IsAttachment))
            {
                if (!CanUserArchiveObject(m_scene.RegionInfo.EstateSettings.EstateOwner, entity, m_checkPermissions))
                    // The user isn't allowed to copy/transfer this object, so it will not be included in the OAR.
                    ++numObjectsSkippedPermissions;
                else
                    sceneObjects.Add(entity);
            }

            UuidGatherer assetGatherer = new UuidGatherer(m_scene.AssetService);

            foreach (ISceneEntity sceneObject in sceneObjects)
            {
                assetGatherer.GatherAssetUuids(sceneObject, assetUuids, m_scene);
            }

            MainConsole.Instance.InfoFormat(
                "[ARCHIVER]: {0} scene objects to serialize requiring save of {1} assets",
                sceneObjects.Count, assetUuids.Count);

            if (numObjectsSkippedPermissions > 0)
            {
                MainConsole.Instance.DebugFormat(
                    "[ARCHIVER]: {0} scene objects skipped due to lack of permissions",
                    numObjectsSkippedPermissions);
            }

            // Make sure that we also request terrain texture assets
            RegionSettings regionSettings = m_scene.RegionInfo.RegionSettings;

            if (regionSettings.TerrainTexture1 != RegionSettings.DEFAULT_TERRAIN_TEXTURE_1)
                assetUuids[regionSettings.TerrainTexture1] = AssetType.Texture;

            if (regionSettings.TerrainTexture2 != RegionSettings.DEFAULT_TERRAIN_TEXTURE_2)
                assetUuids[regionSettings.TerrainTexture2] = AssetType.Texture;

            if (regionSettings.TerrainTexture3 != RegionSettings.DEFAULT_TERRAIN_TEXTURE_3)
                assetUuids[regionSettings.TerrainTexture3] = AssetType.Texture;

            if (regionSettings.TerrainTexture4 != RegionSettings.DEFAULT_TERRAIN_TEXTURE_4)
                assetUuids[regionSettings.TerrainTexture4] = AssetType.Texture;

            TarArchiveWriter archiveWriter = new TarArchiveWriter(m_saveStream);

            // Asynchronously request all the assets required to perform this archive operation
            ArchiveWriteRequestExecution awre
                = new ArchiveWriteRequestExecution(
                    sceneObjects,
                    m_scene.RequestModuleInterface<ITerrainModule>(),
                    m_scene.RequestModuleInterface<IRegionSerialiserModule>(),
                    m_scene,
                    archiveWriter,
                    m_requestId);

            new AssetsRequest(
                new AssetsArchiver(archiveWriter), assetUuids,
                m_scene.AssetService, awre.ReceivedAllAssets).Execute();
        }

        /// <summary>
        ///   Checks whether the user has permission to export an object group to an OAR.
        /// </summary>
        /// <param name = "user">The user</param>
        /// <param name = "objGroup">The object group</param>
        /// <param name = "checkPermissions">Which permissions to check: "C" = Copy, "T" = Transfer</param>
        /// <returns>Whether the user is allowed to export the object to an OAR</returns>
        private bool CanUserArchiveObject(UUID user, ISceneEntity objGroup, string checkPermissions)
        {
            if (checkPermissions == null)
                return true;

            IPermissionsModule module = m_scene.RequestModuleInterface<IPermissionsModule>();
            if (module == null)
                return true; // this shouldn't happen

            // Check whether the user is permitted to export all of the parts in the SOG. If any
            // part can't be exported then the entire SOG can't be exported.

            bool permitted = true;
            //int primNumber = 1;

            foreach (ISceneChildEntity obj in objGroup.ChildrenEntities())
            {
                uint perm;
                PermissionClass permissionClass = module.GetPermissionClass(user, obj);
                switch (permissionClass)
                {
                    case PermissionClass.Owner:
                        perm = obj.BaseMask;
                        break;
                    case PermissionClass.Group:
                        perm = obj.GroupMask | obj.EveryoneMask;
                        break;
                    case PermissionClass.Everyone:
                    default:
                        perm = obj.EveryoneMask;
                        break;
                }

                bool canCopy = (perm & (uint) PermissionMask.Copy) != 0;
                bool canTransfer = (perm & (uint) PermissionMask.Transfer) != 0;

                // Special case: if Everyone can copy the object then this implies it can also be
                // Transferred.
                // However, if the user is the Owner then we don't check EveryoneMask, because it seems that the mask
                // always (incorrectly) includes the Copy bit set in this case. But that's a mistake: the viewer
                // does NOT show that the object has Everyone-Copy permissions, and doesn't allow it to be copied.
                if (permissionClass != PermissionClass.Owner)
                {
                    canTransfer |= (obj.EveryoneMask & (uint) PermissionMask.Copy) != 0;
                }


                bool partPermitted = true;
                if (checkPermissions.Contains("C") && !canCopy)
                    partPermitted = false;
                if (checkPermissions.Contains("T") && !canTransfer)
                    partPermitted = false;

                //string name = (objGroup.PrimCount == 1) ? objGroup.Name : string.Format("{0} ({1}/{2})", obj.Name, primNumber, objGroup.PrimCount);
                //MainConsole.Instance.DebugFormat("[ARCHIVER]: Object permissions: {0}: Base={1:X4}, Owner={2:X4}, Everyone={3:X4}, permissionClass={4}, checkPermissions={5}, canCopy={6}, canTransfer={7}, permitted={8}",
                //    name, obj.BaseMask, obj.OwnerMask, obj.EveryoneMask,
                //    permissionClass, checkPermissions, canCopy, canTransfer, permitted);

                if (!partPermitted)
                {
                    permitted = false;
                    break;
                }

                //++primNumber;
            }

            return permitted;
        }
    }
}