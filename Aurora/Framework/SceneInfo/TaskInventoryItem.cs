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
using OpenMetaverse;
using ProtoBuf;

namespace Aurora.Framework.SceneInfo
{
    public class TaskInventoryItemHelpers
    {
        /// <summary>
        ///     Full permissions
        /// </summary>
        public const uint FULL_MASK_PERMISSIONS_GENERAL = 2147483647;

        /// <summary>
        ///     Inventory types
        /// </summary>
        public static string[] InvTypes = new[]
                                              {
                                                  "texture",
                                                  "sound",
                                                  "calling_card",
                                                  "landmark",
                                                  String.Empty,
                                                  String.Empty,
                                                  "object",
                                                  "notecard",
                                                  String.Empty,
                                                  String.Empty,
                                                  "lsl_text",
                                                  String.Empty,
                                                  String.Empty,
                                                  "bodypart",
                                                  String.Empty,
                                                  "snapshot",
                                                  String.Empty,
                                                  String.Empty,
                                                  "wearable",
                                                  "animation",
                                                  "gesture",
                                                  String.Empty,
                                                  String.Empty,
                                                  "link",
                                                  String.Empty,
                                                  String.Empty,
                                                  String.Empty,
                                                  String.Empty,
                                                  String.Empty,
                                                  String.Empty,
                                                  String.Empty,
                                              };

        /// <summary>
        ///     Asset types
        /// </summary>
        public static string[] Types = new[]
                                           {
                                               "texture",
                                               "sound",
                                               "callcard",
                                               "landmark",
                                               "clothing", // Deprecated
                                               "clothing",
                                               "object",
                                               "notecard",
                                               "category",
                                               "root",
                                               "lsltext",
                                               "lslbyte",
                                               "txtr_tga",
                                               "bodypart",
                                               "trash",
                                               "snapshot",
                                               "lstndfnd",
                                               "snd_wav",
                                               "img_tga",
                                               "jpeg",
                                               "animatn",
                                               "gesture",
                                               "simstate",
                                               "favoritefolder",
                                               "link",
                                               "linkfolder",
                                               "ensemblestart",
                                               "ensembleend",
                                               "currentoutfitfolder",
                                               "outfitfolder",
                                               "myoutfitsfolder",
                                               "inboxfolder"
                                           };

        /// <summary>
        ///     Asset types
        /// </summary>
        public static string[] SaleTypes = new[]
                                               {
                                                   "not",
                                                   "original",
                                                   "copy",
                                                   "contents"
                                               };
    }

    /// <summary>
    ///     Represents an item in a task inventory
    /// </summary>
    [Serializable, ProtoContract()]
    public class TaskInventoryItem : ICloneable
    {
        private UUID _assetID = UUID.Zero;

        private uint _baseMask = TaskInventoryItemHelpers.FULL_MASK_PERMISSIONS_GENERAL;
        private string _creatorData = String.Empty;
        private UUID _creatorID = UUID.Zero;
        private string _description = String.Empty;
        private uint _everyoneMask = TaskInventoryItemHelpers.FULL_MASK_PERMISSIONS_GENERAL;
        private UUID _groupID = UUID.Zero;
        private uint _groupMask = TaskInventoryItemHelpers.FULL_MASK_PERMISSIONS_GENERAL;

        private UUID _itemID = UUID.Zero;
        private UUID _lastOwnerID = UUID.Zero;
        private string _name = String.Empty;
        private uint _nextOwnerMask = TaskInventoryItemHelpers.FULL_MASK_PERMISSIONS_GENERAL;
        private UUID _oldID;
        private UUID _ownerID = UUID.Zero;
        private uint _ownerMask = TaskInventoryItemHelpers.FULL_MASK_PERMISSIONS_GENERAL;
        private UUID _parentID = UUID.Zero; //parent folder id
        private UUID _parentPartID = UUID.Zero; // SceneObjectPart this is inside

        public TaskInventoryItem()
        {
            CreationDate = (uint) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        [ProtoMember(1)]
        public UUID AssetID
        {
            get { return _assetID; }
            set { _assetID = value; }
        }

        [ProtoMember(2)]
        public uint BasePermissions
        {
            get { return _baseMask; }
            set { _baseMask = value; }
        }

        [ProtoMember(3)]
        public uint CreationDate { get; set; }

        [ProtoMember(4)]
        public UUID CreatorID
        {
            get { return _creatorID; }
            set { _creatorID = value; }
        }

        [ProtoMember(5)]
        public string CreatorData // = <profile url>;<name>
        {
            get { return _creatorData; }
            set { _creatorData = value; }
        }

        /// <summary>
        ///     Used by the DB layer to retrieve / store the entire user identification.
        ///     The identification can either be a simple UUID or a string of the form
        ///     uuid[;profile_url[;name]]
        /// </summary>
        [ProtoMember(6)]
        public string CreatorIdentification
        {
            get
            {
                if (!string.IsNullOrEmpty(_creatorData))
                    return _creatorID.ToString() + ';' + _creatorData;
                else
                    return _creatorID.ToString();
            }
            set
            {
                if ((value == null) || (value != null && value == string.Empty))
                {
                    _creatorData = string.Empty;
                    return;
                }

                if (!value.Contains(";")) // plain UUID
                {
                    UUID uuid = UUID.Zero;
                    UUID.TryParse(value, out uuid);
                    _creatorID = uuid;
                }
                else // <uuid>[;<endpoint>[;name]]
                {
                    string name = "Unknown User";
                    string[] parts = value.Split(';');
                    if (parts.Length >= 1)
                    {
                        UUID uuid = UUID.Zero;
                        UUID.TryParse(parts[0], out uuid);
                        _creatorID = uuid;
                    }
                    if (parts.Length >= 2)
                        _creatorData = parts[1];
                    if (parts.Length >= 3)
                        name = parts[2];

                    _creatorData += ';' + name;
                }
            }
        }

        [ProtoMember(7)]
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        [ProtoMember(8)]
        public uint EveryonePermissions
        {
            get { return _everyoneMask; }
            set { _everyoneMask = value; }
        }

        [ProtoMember(9)]
        public uint Flags { get; set; }

        [ProtoMember(10)]
        public UUID GroupID
        {
            get { return _groupID; }
            set { _groupID = value; }
        }

        [ProtoMember(11)]
        public uint GroupPermissions
        {
            get { return _groupMask; }
            set { _groupMask = value; }
        }

        [ProtoMember(12)]
        public int InvType { get; set; }

        [ProtoMember(13)]
        public UUID ItemID
        {
            get { return _itemID; }
            set { _itemID = value; }
        }

        [ProtoMember(14)]
        public UUID OldItemID
        {
            get { return _oldID; }
            set
            {
                if (_oldID == UUID.Zero)
                    _oldID = value;
            }
        }

        [ProtoMember(15)]
        public UUID LastOwnerID
        {
            get { return _lastOwnerID; }
            set { _lastOwnerID = value; }
        }

        [ProtoMember(16)]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        [ProtoMember(17)]
        public uint NextPermissions
        {
            get { return _nextOwnerMask; }
            set { _nextOwnerMask = value; }
        }

        [ProtoMember(18)]
        public UUID OwnerID
        {
            get { return _ownerID; }
            set { _ownerID = value; }
        }

        [ProtoMember(19)]
        public uint CurrentPermissions
        {
            get { return _ownerMask; }
            set { _ownerMask = value; }
        }

        [ProtoMember(20)]
        public UUID ParentID
        {
            get { return _parentID; }
            set { _parentID = value; }
        }

        [ProtoMember(21)]
        public UUID ParentPartID
        {
            get { return _parentPartID; }
            set { _parentPartID = value; }
        }

        [ProtoMember(22)]
        public UUID PermsGranter { get; set; }

        [ProtoMember(23)]
        public int PermsMask { get; set; }

        [ProtoMember(24)]
        public int Type { get; set; }

        [ProtoMember(25)]
        public bool OwnerChanged { get; set; }

        [ProtoMember(26)]
        public int SalePrice { get; set; }

        [ProtoMember(27)]
        public byte SaleType { get; set; }

        // See ICloneable

        #region ICloneable Members

        public Object Clone()
        {
            return MemberwiseClone();
        }

        #endregion

        /// <summary>
        ///     Reset the UUIDs for this item.
        /// </summary>
        /// <param name="partID">The new part ID to which this item belongs</param>
        public void ResetIDs(UUID partID)
        {
            OldItemID = ItemID;
            ItemID = UUID.Random();
            ParentPartID = partID;
            ParentID = partID;
        }
    }
}