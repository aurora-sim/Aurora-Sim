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

using System.Collections.Generic;
using System.Reflection;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using log4net;

namespace OpenSim.Region.CoreModules.World.Land
{
    public class LandChannel : ILandChannel
    {
        #region Constants

        //Land types set with flags in ParcelOverlay.
        //Only one of these can be used.
        public const float BAN_LINE_SAFETY_HEIGHT = 100;
        public const byte LAND_FLAG_PROPERTY_BORDER_SOUTH = 128; //Equals 10000000
        public const byte LAND_FLAG_PROPERTY_BORDER_WEST = 64; //Equals 01000000

        //RequestResults (I think these are right, they seem to work):
        public const int LAND_RESULT_MULTIPLE = 1; // The request they made contained more than a single peice of land
        public const int LAND_RESULT_SINGLE = 0; // The request they made contained only a single piece of land

        //ParcelSelectObjects
        public const int LAND_SELECT_OBJECTS_GROUP = 4;
        public const int LAND_SELECT_OBJECTS_OTHER = 8;
        public const int LAND_SELECT_OBJECTS_OWNER = 2;
        public const byte LAND_TYPE_IS_BEING_AUCTIONED = 5; //Equals 00000101
        public const byte LAND_TYPE_IS_FOR_SALE = 4; //Equals 00000100
        public const byte LAND_TYPE_OWNED_BY_GROUP = 2; //Equals 00000010
        public const byte LAND_TYPE_OWNED_BY_OTHER = 1; //Equals 00000001
        public const byte LAND_TYPE_OWNED_BY_REQUESTER = 3; //Equals 00000011
        public const byte LAND_TYPE_PUBLIC = 0; //Equals 00000000

        //These are other constants. Yay!
        public const int START_LAND_LOCAL_ID = 1;

        #endregion

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Scene m_scene;
        private readonly LandManagementModule m_landManagementModule;

        public LandChannel(Scene scene, LandManagementModule landManagementMod)
        {
            m_scene = scene;
            m_landManagementModule = landManagementMod;
        }

        #region ILandChannel Members

        public ILandObject GetLandObject(float x_float, float y_float)
        {
            if (m_landManagementModule != null)
            {
                return m_landManagementModule.GetLandObject((int)x_float,(int)y_float);
            }
            
            ILandObject obj = new LandObject(UUID.Zero, false, m_scene);
            obj.LandData.Name = "NO LAND";
            return obj;
        }

        public ILandObject GetLandObject(int localID)
        {
            if (m_landManagementModule != null)
            {
                return m_landManagementModule.GetLandObject(localID);
            }
            return null;
        }

        public ILandObject GetLandObject(int x, int y)
        {
            if (m_landManagementModule != null)
            {
                return m_landManagementModule.GetLandObject(x, y);
            }
            
            ILandObject obj = new LandObject(UUID.Zero, false, m_scene);
            obj.LandData.Name = "NO LAND";
            return obj;
        }

        public List<ILandObject> AllParcels()
        {
            if (m_landManagementModule != null)
            {
                return m_landManagementModule.AllParcels();
            }

            return new List<ILandObject>();
        }

        public List<ILandObject> ParcelsNearPoint(Vector3 position)
        {
            if (m_landManagementModule != null)
            {
                return m_landManagementModule.ParcelsNearPoint(position);
            }

            return new List<ILandObject>();
        }

        public bool IsLandPrimCountTainted()
        {
            if (m_landManagementModule != null)
            {
                return m_landManagementModule.IsLandPrimCountTainted();
            }

            return false;
        }

        public bool IsForcefulBansAllowed()
        {
            if (m_landManagementModule != null)
            {
                return m_landManagementModule.AllowedForcefulBans;
            }

            return false;
        }

        public void UpdateLandObject(int localID, LandData data)
        {
            if (m_landManagementModule != null)
            {
                m_landManagementModule.UpdateLandObject(localID, data);
            }
        }

        public void Join(int start_x, int start_y, int end_x, int end_y, UUID attempting_user_id)
        {
            if (m_landManagementModule != null)
            {
                m_landManagementModule.Join(start_x, start_y, end_x, end_y, attempting_user_id);
            }
        }

        public void Subdivide(int start_x, int start_y, int end_x, int end_y, UUID attempting_user_id)
        {
            if (m_landManagementModule != null)
            {
                m_landManagementModule.Subdivide(start_x, start_y, end_x, end_y, attempting_user_id);
            }
        }
        
        public void ReturnObjectsInParcel(int localID, uint returnType, UUID[] agentIDs, UUID[] taskIDs, IClientAPI remoteClient)
        {
            if (m_landManagementModule != null)
            {
                m_landManagementModule.ReturnObjectsInParcel(localID, returnType, agentIDs, taskIDs, remoteClient);
            }
        }

        public void SetParcelOtherCleanTime(IClientAPI remoteClient, int localID, int otherCleanTime)
        {
            if (m_landManagementModule != null)
            {
                m_landManagementModule.setParcelOtherCleanTime(remoteClient, localID, otherCleanTime);
            }
        }

        public Vector3? GetNearestAllowedPosition(ScenePresence avatar)
        {
            ILandObject nearestParcel = GetNearestAllowedParcel(avatar.UUID, avatar.AbsolutePosition.X, avatar.AbsolutePosition.Y);

            if (nearestParcel != null)
            {
                Vector3 dir = Vector3.Normalize(Vector3.Multiply(avatar.Velocity, -1));
                //Try to get a location that feels like where they came from
                Vector3? nearestPoint = GetNearestPointInParcelAlongDirectionFromPoint(avatar.AbsolutePosition, dir, nearestParcel);
                if (nearestPoint != null)
                {
                    //m_log.Info("Found a sane previous position based on velocity, sending them to: " + nearestPoint.ToString());
                    //Fix Z pos
                    nearestPoint = new Vector3(nearestPoint.Value.X, nearestPoint.Value.Y, avatar.AbsolutePosition.Z);
                    return nearestPoint.Value;
                }

                //Sometimes velocity might be zero (local teleport), so try finding point along path from avatar to center of nearest parcel
                Vector3 directionToParcelCenter = Vector3.Subtract(GetParcelCenterAtGround(nearestParcel), avatar.AbsolutePosition);
                dir = Vector3.Normalize(directionToParcelCenter);
                nearestPoint = GetNearestPointInParcelAlongDirectionFromPoint(avatar.AbsolutePosition, dir, nearestParcel);
                if (nearestPoint != null)
                {
                    //m_log.Info("They had a zero velocity, sending them to: " + nearestPoint.ToString());
                    return nearestPoint.Value;
                }

                //Ultimate backup if we have no idea where they are 
                //m_log.Info("Have no idea where they are, sending them to the center of the parcel");
                return GetParcelCenterAtGround(nearestParcel);

            }

            //Go to the edge, this happens in teleporting to a region with no available parcels
            Vector3 nearestRegionEdgePoint = GetNearestRegionEdgePosition(avatar);
            //Debug.WriteLine("They are really in a place they don't belong, sending them to: " + nearestRegionEdgePoint.ToString());
            return nearestRegionEdgePoint;
        }

        public Vector3 GetParcelCenterAtGround(ILandObject parcel)
        {
            Vector2 center = GetParcelCenter(parcel);
            return GetPositionAtGround(center.X, center.Y);
        }

        private Vector3? GetNearestPointInParcelAlongDirectionFromPoint(Vector3 pos, Vector3 direction, ILandObject parcel)
        {
            Vector3 unitDirection = Vector3.Normalize(direction);
            //Making distance to search go through some sane limit of distance
            for (float distance = 0; distance < Constants.RegionSize * 2; distance += .5f)
            {
                Vector3 testPos = Vector3.Add(pos, Vector3.Multiply(unitDirection, distance));
                if (parcel.ContainsPoint((int)testPos.X, (int)testPos.Y))
                {
                    return testPos;
                }
            }
            return null;
        }

        public ILandObject GetNearestAllowedParcel(UUID avatarId, float x, float y)
        {
            List<ILandObject> all = AllParcels();
            float minParcelDistance = float.MaxValue;
            ILandObject nearestParcel = null;

            foreach (var parcel in all)
            {
                if (!parcel.IsEitherBannedOrRestricted(avatarId))
                {
                    float parcelDistance = GetParcelDistancefromPoint(parcel, x, y);
                    if (parcelDistance < minParcelDistance)
                    {
                        minParcelDistance = parcelDistance;
                        nearestParcel = parcel;
                    }
                }
            }

            return nearestParcel;
        }

        private float GetParcelDistancefromPoint(ILandObject parcel, float x, float y)
        {
            return Vector2.Distance(new Vector2(x, y), GetParcelCenter(parcel));
        }

        //calculate the average center point of a parcel
        private Vector2 GetParcelCenter(ILandObject parcel)
        {
            int count = 0;
            int avgx = 0;
            int avgy = 0;
            for (int x = 0; x < Constants.RegionSize; x++)
            {
                for (int y = 0; y < Constants.RegionSize; y++)
                {
                    //Just keep a running average as we check if all the points are inside or not
                    if (parcel.ContainsPoint(x, y))
                    {
                        if (count == 0)
                        {
                            avgx = x;
                            avgy = y;
                        }
                        else
                        {
                            avgx = (avgx * count + x) / (count + 1);
                            avgy = (avgy * count + y) / (count + 1);
                        }
                        count += 1;
                    }
                }
            }
            return new Vector2(avgx, avgy);
        }

        public Vector3 GetNearestRegionEdgePosition(ScenePresence avatar)
        {
            float xdistance = avatar.AbsolutePosition.X < Constants.RegionSize / 2 ? avatar.AbsolutePosition.X : Constants.RegionSize - avatar.AbsolutePosition.X;
            float ydistance = avatar.AbsolutePosition.Y < Constants.RegionSize / 2 ? avatar.AbsolutePosition.Y : Constants.RegionSize - avatar.AbsolutePosition.Y;

            //find out what vertical edge to go to
            if (xdistance < ydistance)
            {
                if (avatar.AbsolutePosition.X < Constants.RegionSize / 2)
                {
                    return GetPositionAtAvatarHeightOrGroundHeight(avatar, 0.0f, avatar.AbsolutePosition.Y);
                }
                else
                {
                    return GetPositionAtAvatarHeightOrGroundHeight(avatar, Constants.RegionSize, avatar.AbsolutePosition.Y);
                }
            }
            //find out what horizontal edge to go to
            else
            {
                if (avatar.AbsolutePosition.Y < Constants.RegionSize / 2)
                {
                    return GetPositionAtAvatarHeightOrGroundHeight(avatar, avatar.AbsolutePosition.X, 0.0f);
                }
                else
                {
                    return GetPositionAtAvatarHeightOrGroundHeight(avatar, avatar.AbsolutePosition.X, Constants.RegionSize);
                }
            }
        }

        private Vector3 GetPositionAtAvatarHeightOrGroundHeight(ScenePresence avatar, float x, float y)
        {
            Vector3 ground = GetPositionAtGround(x, y);
            if (avatar.AbsolutePosition.Z > ground.Z)
            {
                ground.Z = avatar.AbsolutePosition.Z;
            }
            return ground;
        }

        private Vector3 GetPositionAtGround(float x, float y)
        {
            return new Vector3(x, y, m_scene.GetGroundHeight(x, y));
        }

        #endregion
    }
}
