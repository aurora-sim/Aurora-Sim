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
using OpenMetaverse;

namespace Aurora.Framework.Serialization
{
    /// <summary>
    ///     Constants for the archiving module
    /// </summary>
    public class ArchiveConstants
    {
        /// <value>
        ///     The location of the archive control file
        /// </value>
        public const string CONTROL_FILE_PATH = "archive.xml";

        /// <value>
        ///     Path for the assets held in an archive
        /// </value>
        public const string ASSETS_PATH = "assets/";

        /// <value>
        ///     Path for the inventory data
        /// </value>
        public const string INVENTORY_PATH = "inventory/";

        /// <value>
        ///     Path for the prims file
        /// </value>
        public const string OBJECTS_PATH = "objects/";

        /// <value>
        ///     Path for terrains.  Technically these may be assets, but I think it's quite nice to split them out.
        /// </value>
        public const string TERRAINS_PATH = "terrains/";

        /// <value>
        ///     Path for region settings.
        /// </value>
        public const string SETTINGS_PATH = "settings/";

        /// <value>
        ///     Path for region settings.
        /// </value>
        public const string LANDDATA_PATH = "landdata/";

        /// <value>
        ///     Path for user profiles
        /// </value>
        public const string USERS_PATH = "userprofiles/";

        /// <value>
        ///     The character the separates the uuid from extension information in an archived asset filename
        /// </value>
        public const string ASSET_EXTENSION_SEPARATOR = "_";

        /// <value>
        ///     Used to separate components in an inventory node name
        /// </value>
        public const string INVENTORY_NODE_NAME_COMPONENT_SEPARATOR = "__";

        /// <summary>
        ///     Template used for creating filenames in OpenSim Archives.
        /// </summary>
        public const string OAR_OBJECT_FILENAME_TEMPLATE = "{0}_{1:000}-{2:000}-{3:000}__{4}.xml";

        /// <value>
        ///     Extensions used for asset types in the archive
        /// </value>
        public static readonly IDictionary<int, string> ASSET_TYPE_TO_EXTENSION = new Dictionary<int, string>();

        public static readonly IDictionary<string, AssetType> EXTENSION_TO_ASSET_TYPE =
            new Dictionary<string, AssetType>();

        static ArchiveConstants()
        {
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.Animation] = ASSET_EXTENSION_SEPARATOR + "animation.bvh";
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.Bodypart] = ASSET_EXTENSION_SEPARATOR + "bodypart.txt";
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.CallingCard] = ASSET_EXTENSION_SEPARATOR + "callingcard.txt";
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.Clothing] = ASSET_EXTENSION_SEPARATOR + "clothing.txt";
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.Folder] = ASSET_EXTENSION_SEPARATOR + "folder.txt";
            // Not sure if we'll ever see this
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.Gesture] = ASSET_EXTENSION_SEPARATOR + "gesture.txt";
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.ImageJPEG] = ASSET_EXTENSION_SEPARATOR + "image.jpg";
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.ImageTGA] = ASSET_EXTENSION_SEPARATOR + "image.tga";
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.Landmark] = ASSET_EXTENSION_SEPARATOR + "landmark.txt";
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.LostAndFoundFolder] = ASSET_EXTENSION_SEPARATOR +
                                                                          "lostandfoundfolder.txt";
            // Not sure if we'll ever see this
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.LSLBytecode] = ASSET_EXTENSION_SEPARATOR + "bytecode.lso";
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.LSLText] = ASSET_EXTENSION_SEPARATOR + "script.lsl";
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.Mesh] = ASSET_EXTENSION_SEPARATOR + "mesh.llmesh";
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.Notecard] = ASSET_EXTENSION_SEPARATOR + "notecard.txt";
            ASSET_TYPE_TO_EXTENSION[(sbyte) AssetType.Object] = ASSET_EXTENSION_SEPARATOR + "object.xml";
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.RootFolder] = ASSET_EXTENSION_SEPARATOR + "rootfolder.txt";
            // Not sure if we'll ever see this
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.Simstate] = ASSET_EXTENSION_SEPARATOR + "simstate.bin";
            // Not sure if we'll ever see this
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.SnapshotFolder] = ASSET_EXTENSION_SEPARATOR + "snapshotfolder.txt";
            // Not sure if we'll ever see this
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.Sound] = ASSET_EXTENSION_SEPARATOR + "sound.ogg";
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.SoundWAV] = ASSET_EXTENSION_SEPARATOR + "sound.wav";
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.Texture] = ASSET_EXTENSION_SEPARATOR + "texture.jp2";
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.TextureTGA] = ASSET_EXTENSION_SEPARATOR + "texture.tga";
            ASSET_TYPE_TO_EXTENSION[(int) AssetType.TrashFolder] = ASSET_EXTENSION_SEPARATOR + "trashfolder.txt";
            // Not sure if we'll ever see this

            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "animation.bvh"] = AssetType.Animation;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "bodypart.txt"] = AssetType.Bodypart;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "callingcard.txt"] = AssetType.CallingCard;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "clothing.txt"] = AssetType.Clothing;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "folder.txt"] = AssetType.Folder;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "gesture.txt"] = AssetType.Gesture;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "image.jpg"] = AssetType.ImageJPEG;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "image.tga"] = AssetType.ImageTGA;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "landmark.txt"] = AssetType.Landmark;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "lostandfoundfolder.txt"] = AssetType.LostAndFoundFolder;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "bytecode.lso"] = AssetType.LSLBytecode;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "script.lsl"] = AssetType.LSLText;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "mesh.llmesh"] = AssetType.Mesh;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "notecard.txt"] = AssetType.Notecard;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "object.xml"] = AssetType.Object;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "rootfolder.txt"] = AssetType.RootFolder;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "simstate.bin"] = AssetType.Simstate;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "snapshotfolder.txt"] = AssetType.SnapshotFolder;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "sound.ogg"] = AssetType.Sound;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "sound.wav"] = AssetType.SoundWAV;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "texture.jp2"] = AssetType.Texture;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "texture.tga"] = AssetType.TextureTGA;
            EXTENSION_TO_ASSET_TYPE[ASSET_EXTENSION_SEPARATOR + "trashfolder.txt"] = AssetType.TrashFolder;
        }

        /// <summary>
        ///     Create the filename used to store an object in an OpenSim Archive.
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="uuid"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static string CreateOarObjectFilename(string objectName, UUID uuid, Vector3 pos)
        {
            return string.Format(
                OAR_OBJECT_FILENAME_TEMPLATE, objectName,
                Math.Round(pos.X), Math.Round(pos.Y), Math.Round(pos.Z),
                uuid);
        }

        /// <summary>
        ///     Create the path used to store an object in an OpenSim Archives.
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="uuid"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static string CreateOarObjectPath(string objectName, UUID uuid, Vector3 pos)
        {
            return OBJECTS_PATH + CreateOarObjectFilename(objectName, uuid, pos);
        }

        /// <summary>
        ///     Extract a plain path from an IAR path
        /// </summary>
        /// <param name="iarPath"></param>
        /// <returns></returns>
        public static string ExtractPlainPathFromIarPath(string iarPath)
        {
            List<string> plainDirs = new List<string>();

            string[] iarDirs = iarPath.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);

            foreach (string iarDir in iarDirs)
            {
                if (!iarDir.Contains(INVENTORY_NODE_NAME_COMPONENT_SEPARATOR))
                    plainDirs.Add(iarDir);

                int i = iarDir.LastIndexOf(INVENTORY_NODE_NAME_COMPONENT_SEPARATOR);

                plainDirs.Add(iarDir.Remove(i));
            }

            return string.Join("/", plainDirs.ToArray());
        }
    }
}