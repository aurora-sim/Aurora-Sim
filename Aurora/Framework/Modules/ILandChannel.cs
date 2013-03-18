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

namespace Aurora.Framework
{
    public interface IParcelManagementModule
    {
        /// <summary>
        ///   Gets the list of what parcel's own each piece of land
        /// </summary>
        int[,] LandIDList { get; }

        /// <summary>
        /// Gets the UUID of the user who is the "default" parcel owner
        /// </summary>
        UUID GodParcelOwner { get; }

        /// <summary>
        ///   Get all parcels
        /// </summary>
        /// <returns></returns>
        List<ILandObject> AllParcels();

        /// <summary>
        ///   Get the parcel at the specified point
        /// </summary>
        /// <param name = "x">Value between 0 - 256 on the x axis of the point</param>
        /// <param name = "y">Value between 0 - 256 on the y axis of the point</param>
        /// <returns>Land object at the point supplied</returns>
        ILandObject GetLandObject(int x, int y);

        /// <summary>
        ///   Get the parcel at the specified point
        /// </summary>
        /// <param name = "x">Value between 0 - 256 on the x axis of the point</param>
        /// <param name = "y">Value between 0 - 256 on the y axis of the point</param>
        /// <returns>Land object at the point supplied</returns>
        ILandObject GetLandObject(float x, float y);

        /// <summary>
        ///   Get a parcel by GlobalID
        /// </summary>
        /// <param name = "GlobalID"></param>
        /// <returns></returns>
        ILandObject GetLandObject(UUID GlobalID);

        /// <summary>
        ///   Get the parcels near the specified point
        /// </summary>
        /// <param name = "position"></param>
        /// <returns></returns>
        List<ILandObject> ParcelsNearPoint(Vector3 position);

        /// <summary>
        ///   Get the parcel given the land's local id.
        /// </summary>
        /// <param name = "localID"></param>
        /// <returns></returns>
        ILandObject GetLandObject(int localID);

        /// <summary>
        ///   Update the given land object in the cache
        /// </summary>
        /// <param name = "parcel"></param>
        void UpdateLandObject(ILandObject parcel);

        /// <summary>
        ///   Delete all parcels and create one default parcel that spreads over the entire sim
        /// </summary>
        ILandObject ResetSimLandObjects();

        /// <summary>
        ///   Join all parcels within the given range into one large parcel
        /// </summary>
        /// <param name = "start_x"></param>
        /// <param name = "start_y"></param>
        /// <param name = "end_x"></param>
        /// <param name = "end_y"></param>
        /// <param name = "attempting_user_id"></param>
        void Join(int start_x, int start_y, int end_x, int end_y, UUID attempting_user_id);

        /// <summary>
        ///   Subdivide the land area given into a new parcel
        /// </summary>
        /// <param name = "start_x"></param>
        /// <param name = "start_y"></param>
        /// <param name = "end_x"></param>
        /// <param name = "end_y"></param>
        /// <param name = "attempting_user_id"></param>
        void Subdivide(int start_x, int start_y, int end_x, int end_y, UUID attempting_user_id);

        /// <summary>
        ///   Get the nearest region border to the given avatars position
        /// </summary>
        /// <param name = "avatar"></param>
        /// <returns></returns>
        Vector3 GetNearestRegionEdgePosition(IScenePresence avatar);

        /// <summary>
        ///   Get the nearest allowed parcel that the given avatarID is allowed in from point (x,y)
        /// </summary>
        /// <param name = "avatarId"></param>
        /// <param name = "x"></param>
        /// <param name = "y"></param>
        /// <returns></returns>
        ILandObject GetNearestAllowedParcel(UUID avatarId, float x, float y);

        /// <summary>
        ///   Get the nearest allowed position for the given avatar
        /// </summary>
        /// <param name = "avatar"></param>
        /// <returns></returns>
        Vector3 GetNearestAllowedPosition(IScenePresence avatar);

        /// <summary>
        ///   Get the center of a parcel at ground level
        /// </summary>
        /// <param name = "parcel"></param>
        /// <returns></returns>
        Vector3 GetParcelCenterAtGround(ILandObject parcel);

        /// <summary>
        ///   Add the given return to the list of returns to send to the client
        /// </summary>
        /// <param name = "agentID"></param>
        /// <param name = "objectName"></param>
        /// <param name = "position"></param>
        /// <param name = "reason"></param>
        /// <param name = "deleteGroups">Groups to delete</param>
        void AddReturns(UUID agentID, string objectName, Vector3 position, string reason,
                        List<ISceneEntity> deleteGroups);

        /// <summary>
        ///   Resets the sim to have no land objects
        /// </summary>
        void ClearAllParcels();
    }
}