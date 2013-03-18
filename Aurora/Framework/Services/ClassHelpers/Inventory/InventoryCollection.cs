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

using System.Collections.Generic;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
    /// <summary>
    ///   Used to serialize a whole inventory for transfer over the network.
    /// </summary>
    public class InventoryCollection : IDataTransferable
    {
        public List<InventoryFolderBase> Folders;
        public List<InventoryItemBase> Items;
        public UUID UserID;
        public UUID FolderID;

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();

            map["Items"] = new OSDArray(Items.ConvertAll<OSD>((item) => item.ToOSD()));
            map["Folders"] = new OSDArray(Folders.ConvertAll<OSD>((folder) => folder.ToOSD()));
            map["UserID"] = UserID;
            map["FolderID"] = FolderID;

            return map;
        }

        public override void FromOSD(OSDMap map)
        {
            OSDArray items = (OSDArray)map["Items"];
            Items = items.ConvertAll<InventoryItemBase>((osd) =>
            {
                InventoryItemBase item = new InventoryItemBase();
                item.FromOSD((OSDMap)osd);
                return item;
            }
            );
            OSDArray folders = (OSDArray)map["Folders"];
            Folders = folders.ConvertAll<InventoryFolderBase>((osd) =>
            {
                InventoryFolderBase folder = new InventoryFolderBase();
                folder.FromOSD((OSDMap)osd);
                return folder;
            }
            );
            UserID = map["UserID"];
            FolderID = map["FolderID"];
        }
    }
}