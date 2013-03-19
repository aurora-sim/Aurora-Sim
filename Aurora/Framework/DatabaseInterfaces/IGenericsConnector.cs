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

using Aurora.Framework.Modules;
using Aurora.Framework.Services;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System.Collections.Generic;

namespace Aurora.Framework
{
    /// <summary>
    ///     Some background to this class
    ///     This class saves any class that implements the IDataTransferable interface.
    ///     When implementing the IDataTransferable interface, it is heavily recommending to implement ToOSD and FromOSD first, then use the Utility methods to convert OSDMaps into Dictionarys, as shown in the LandData class.
    ///     This method of saving uses 4 columns in the database, OwnerID, Type, Key, and Value
    ///     - OwnerID : This is a way to be able to save Agent or Region or anything with a UUID into the database and have it be set to that UUID only.
    ///     - Type : What made this data? This just tells what module created the given row in the database.
    ///     - Key : Another identifying setting so that you can store more than one row under an OwnerID and Type
    ///     - Value : The value of the row
    ///     This class deals with the Getting/Setting/Removing of these generic interfaces.
    /// </summary>
    public interface IGenericsConnector : IAuroraDataPlugin
    {
        /// <summary>
        ///     Gets a Generic type as set by the ownerID, Type, and Key
        /// </summary>
        /// <typeparam name="T">return value of type IDataTransferable</typeparam>
        /// <param name="OwnerID"></param>
        /// <param name="Type"></param>
        /// <param name="Key"></param>
        /// <returns></returns>
        T GetGeneric<T>(UUID OwnerID, string Type, string Key) where T : IDataTransferable;

        /// <summary>
        ///     Gets a list of generic T's from the database
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="OwnerID"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        List<T> GetGenerics<T>(UUID OwnerID, string Type) where T : IDataTransferable;

        /// <summary>
        ///     Adds a generic IDataTransferable into the database
        /// </summary>
        /// <param name="OwnerID"></param>
        /// <param name="Type"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        void AddGeneric(UUID OwnerID, string Type, string Key, OSDMap Value);

        /// <summary>
        ///     Removes a generic IDataTransferable from the database
        /// </summary>
        /// <param name="OwnerID"></param>
        /// <param name="Type"></param>
        /// <param name="Key"></param>
        void RemoveGeneric(UUID OwnerID, string Type, string Key);

        /// <summary>
        ///     Removes a generic IDataTransferable from the database
        /// </summary>
        /// <param name="OwnerID"></param>
        /// <param name="Type"></param>
        void RemoveGeneric(UUID OwnerID, string Type);

        /// <summary>
        ///     Returns a list of UUIDs of the specified type that have the specified key
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="Key"></param>
        /// <returns></returns>
        List<UUID> GetOwnersByGeneric(string Type, string Key);

        /// <summary>
        ///     Returns a list of UUIDs of the specified type that have the specified key and value
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        List<UUID> GetOwnersByGeneric(string Type, string Key, OSDMap Value);

        /// <summary>
        ///     Gets the number of list of generic T's from the database
        /// </summary>
        /// <param name="OwnerID"></param>
        /// <param name="Type"></param>
        /// <returns></returns>
        int GetGenericCount(UUID OwnerID, string Type);
    }

    public class OSDWrapper : IDataTransferable
    {
        public OSD Info;

        public override void FromOSD(OSDMap map)
        {
            if (map != null)
                Info = map["Info"];
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["Info"] = Info;
            return map;
        }
    }
}