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

using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework.Services.ClassHelpers.Inventory
{
    /// <summary>
    ///     User inventory folder
    /// </summary>
    public class InventoryFolderBase : InventoryNodeBase
    {
        public InventoryFolderBase()
        {
        }

        public InventoryFolderBase(UUID id)
        {
            ID = id;
        }

        public InventoryFolderBase(UUID id, UUID owner)
        {
            ID = id;
            Owner = owner;
        }

        public InventoryFolderBase(UUID id, string name, UUID owner, UUID parent)
        {
            ID = id;
            Name = name;
            Owner = owner;
            ParentID = parent;
        }

        public InventoryFolderBase(UUID id, string name, UUID owner, short type, UUID parent, ushort version)
        {
            ID = id;
            Name = name;
            Owner = owner;
            Type = type;
            ParentID = parent;
            Version = version;
        }

        public UUID ParentID { get; set; }

        public short Type { get; set; }

        public ushort Version { get; set; }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();

            map["ID"] = ID;
            map["Name"] = Name;
            map["Owner"] = Owner;
            map["Type"] = (int) Type;
            map["ParentID"] = ParentID;
            map["Version"] = (int) Version;

            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            ID = map["ID"];
            Name = map["Name"];
            Owner = map["Owner"];
            Type = (short) map["Type"];
            ParentID = map["ParentID"];
            Version = (ushort) (int) map["Version"];
        }
    }
}