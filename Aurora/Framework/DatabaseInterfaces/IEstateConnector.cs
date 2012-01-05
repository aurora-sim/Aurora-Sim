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

using System.Collections.Generic;
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.Framework
{
    public interface IEstateConnector : IAuroraDataPlugin
    {
        /// <summary>
        ///   Loads the estate data for the given region
        /// </summary>
        /// <param name = "regionID"></param>
        /// <returns></returns>
        bool LoadEstateSettings(UUID regionID, out EstateSettings settings);

        /// <summary>
        ///   Updates the given Estate data in the database
        /// </summary>
        /// <param name = "es"></param>
        void SaveEstateSettings(EstateSettings es);

        /// <summary>
        ///   Gets the estates that have the given name
        /// </summary>
        /// <param name = "search"></param>
        /// <returns></returns>
        List<int> GetEstates(string name);

        /// <summary>
        ///   Get all regions in the current estate
        /// </summary>
        /// <param name = "estateID"></param>
        /// <returns></returns>
        List<UUID> GetRegions(uint estateID);

        /// <summary>
        ///   Gets the estates that have the given owner
        /// </summary>
        /// <param name = "search"></param>
        /// <returns></returns>
        List<EstateSettings> GetEstates(UUID OwnerID);

        /// <summary>
        /// Gets the estates that have the specified owner, with optional filters.
        /// </summary>
        /// <param name="OwnerID"></param>
        /// <param name="boolFields"></param>
        /// <returns></returns>
        List<EstateSettings> GetEstates(UUID OwnerID, Dictionary<string, bool> boolFields);

        /// <summary>
        ///   Add a new region to the estate, authenticates with the password
        /// </summary>
        /// <param name = "regionID"></param>
        /// <param name = "estateID"></param>
        /// <param name = "password"></param>
        /// <returns></returns>
        bool LinkRegion(UUID regionID, int estateID, string password);

        /// <summary>
        ///   Remove an existing region from the estate, authenticates with the password
        /// </summary>
        /// <param name = "regionID"></param>
        /// <param name = "estateID"></param>
        /// <param name = "password"></param>
        /// <returns></returns>
        bool DelinkRegion(UUID regionID, string password);

        /// <summary>
        ///   Deletes the given estate by its estate ID, must be authenticated with the password
        /// </summary>
        /// <param name = "estateID"></param>
        /// <param name = "password"></param>
        /// <returns></returns>
        bool DeleteEstate(int estateID, string password);

        /// <summary>
        ///   Creates a new estate from the given info, returns the updated info
        /// </summary>
        /// <param name = "ES"></param>
        /// <param name = "RegionID"></param>
        /// <returns></returns>
        EstateSettings CreateEstate(EstateSettings ES, UUID RegionID);
    }
}