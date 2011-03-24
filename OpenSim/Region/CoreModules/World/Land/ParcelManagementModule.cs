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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenMetaverse.Messages.Linden;
using OpenSim.Framework;
using OpenSim.Framework.Capabilities;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Physics.Manager;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using Aurora.Framework;

namespace OpenSim.Region.CoreModules.World.Land
{
    public class ParcelManagementModule : INonSharedRegionModule, IParcelManagementModule
    {
        #region Constants

        //Land types set with flags in ParcelOverlay.
        //Only one of these can be used.
        public const float BAN_LINE_SAFETY_HEIGHT = 100;
        public const byte LAND_FLAG_PROPERTY_BORDER_SOUTH = 128; //Equals 10000000
        public const byte LAND_FLAG_PROPERTY_BORDER_WEST = 64; //Equals 01000000

        public const int LAND_RESULT_NO_DATA = -1; // The request they made had no data
        public const int LAND_RESULT_SINGLE = 0; // The request they made contained only a single piece of land
        public const int LAND_RESULT_MULTIPLE = 1; // The request they made contained more than a single peice of land
        
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

        public const int LAND_OVERLAY_CHUNKS = 4; //The number of land chunks to send to the client

        public const int LAND_MAX_ENTRIES_PER_PACKET = 48; //Max number of access/ban entry updates

        //These are other constants. Yay!
        public const int START_LAND_LOCAL_ID = 0;

        #endregion

        #region Declares

        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string remoteParcelRequestPath = "0009/";

        private Scene m_scene;

        /// <value>
        /// Local land ids at specified region co-ordinates (region size / 4)
        /// </value>
        private int[,] m_landIDList;
        private bool UseDwell = true;

        /// <value>
        /// Land objects keyed by local id
        /// </value>
        private readonly Dictionary<int, ILandObject> m_landList = new Dictionary<int, ILandObject>();

        private int m_lastLandLocalID = ParcelManagementModule.START_LAND_LOCAL_ID;

        private bool m_UpdateDirectoryOnUpdate = false;
        private bool m_UpdateDirectoryOnTimer = true;
        private List<LandData> m_TaintedLandData = new List<LandData>();
        private int m_minutesBeforeTimer = 60;
        private System.Timers.Timer m_UpdateDirectoryTimer = new System.Timers.Timer();
        private int m_update_land = 10; //Check whether we need to rebuild the parcel prim count and other land related functions
        protected Dictionary<UUID, ReturnInfo> m_returns = new Dictionary<UUID, ReturnInfo>();

        #endregion

        #region ILandChannel Members

        public Vector3 GetNearestAllowedPosition (IScenePresence avatar)
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
            int size = m_scene.RegionInfo.RegionSizeX > m_scene.RegionInfo.RegionSizeY ? m_scene.RegionInfo.RegionSizeX : m_scene.RegionInfo.RegionSizeY;
            for (float distance = 0; distance < size * 2; distance += .5f)
            {
                Vector3 testPos = Vector3.Add(pos, Vector3.Multiply(unitDirection, distance));
                if (parcel.ContainsPoint((int)testPos.X, (int)testPos.Y))
                {
                    return GetPositionAtGround(testPos.X, testPos.Y);
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
            float avgx = 0;
            float avgy = 0;
            for (int x = 0; x < m_scene.RegionInfo.RegionSizeX; x++)
            {
                for (int y = 0; y < m_scene.RegionInfo.RegionSizeY; y++)
                {
                    //Just keep a running average as we check if all the points are inside or not
                    if (parcel.ContainsPoint(x, y))
                    {
                        if (count == 0)
                        {
                            //Set this to 1 so that when we multiply down below, it doesn't lock to 0
                            if (x == 0)
                                x = 1;
                            if (y == 0)
                                y = 1;
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

        public Vector3 GetNearestRegionEdgePosition (IScenePresence avatar)
        {
            float xdistance = avatar.AbsolutePosition.X < m_scene.RegionInfo.RegionSizeX / 2 ? avatar.AbsolutePosition.X : m_scene.RegionInfo.RegionSizeX - avatar.AbsolutePosition.X;
            float ydistance = avatar.AbsolutePosition.Y < m_scene.RegionInfo.RegionSizeY / 2 ? avatar.AbsolutePosition.Y : m_scene.RegionInfo.RegionSizeY - avatar.AbsolutePosition.Y;

            //find out what vertical edge to go to
            if (xdistance < ydistance)
            {
                if (avatar.AbsolutePosition.X < m_scene.RegionInfo.RegionSizeX / 2)
                {
                    return GetPositionAtAvatarHeightOrGroundHeight(avatar, 0.0f, avatar.AbsolutePosition.Y);
                }
                else
                {
                    return GetPositionAtAvatarHeightOrGroundHeight(avatar, m_scene.RegionInfo.RegionSizeX, avatar.AbsolutePosition.Y);
                }
            }
            //find out what horizontal edge to go to
            else
            {
                if (avatar.AbsolutePosition.Y < m_scene.RegionInfo.RegionSizeY / 2)
                {
                    return GetPositionAtAvatarHeightOrGroundHeight(avatar, avatar.AbsolutePosition.X, 0.0f);
                }
                else
                {
                    return GetPositionAtAvatarHeightOrGroundHeight(avatar, avatar.AbsolutePosition.X, m_scene.RegionInfo.RegionSizeY);
                }
            }
        }

        private Vector3 GetPositionAtAvatarHeightOrGroundHeight (IScenePresence avatar, float x, float y)
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
            ITerrainChannel heightmap = m_scene.RequestModuleInterface<ITerrainChannel>();
            return new Vector3(x, y, heightmap.GetNormalizedGroundHeight((int)x, (int)y));
        }

        #endregion

        #region INonSharedRegionModule Members

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(IConfigSource source)
        {
            IConfig config = source.Configs["LandManagement"];
            if (config != null)
            {
                m_UpdateDirectoryOnTimer = config.GetBoolean("UpdateOnTimer", m_UpdateDirectoryOnTimer);
                m_UpdateDirectoryOnUpdate = config.GetBoolean("UpdateOnUpdate", m_UpdateDirectoryOnUpdate);
                m_minutesBeforeTimer = config.GetInt("MinutesBeforeTimerUpdate", m_minutesBeforeTimer);
                UseDwell = config.GetBoolean("AllowDwell", true);
            }
        }

        public void AddRegion(Scene scene)
        {
            m_scene = scene;

            m_landIDList = new int[m_scene.RegionInfo.RegionSizeX / 4, m_scene.RegionInfo.RegionSizeY / 4];

            m_landIDList.Initialize();

            if (m_UpdateDirectoryOnTimer)
            {
                m_UpdateDirectoryTimer.Interval = 1000 * 60 * m_minutesBeforeTimer;
                m_UpdateDirectoryTimer.Elapsed += UpdateDirectoryTimerElapsed;
                m_UpdateDirectoryTimer.Start();
            }

            m_scene.EventManager.OnAvatarEnteringNewParcel += EventManagerOnAvatarEnteringNewParcel;
            m_scene.EventManager.OnValidateBuyLand += EventManagerOnValidateLandBuy;
            m_scene.EventManager.OnNewClient += EventManagerOnNewClient;
            m_scene.EventManager.OnMakeRootAgent += CheckEnteringNewParcel;
            m_scene.EventManager.OnSignificantClientMovement += EventManagerOnSignificantClientMovement;
            m_scene.EventManager.OnSignificantObjectMovement += EventManagerOnSignificantObjectMovement;
            m_scene.EventManager.OnIncomingLandDataFromStorage += EventManagerOnIncomingLandDataFromStorage;
            m_scene.EventManager.OnRegisterCaps += EventManagerOnRegisterCaps;
            m_scene.EventManager.OnClosingClient += OnClosingClient;
            m_scene.EventManager.OnFrame += EventManager_OnFrame;
            m_scene.AuroraEventManager.OnGenericEvent += AuroraEventManager_OnGenericEvent;
            if(m_UpdateDirectoryOnTimer)
                m_scene.EventManager.OnStartupComplete += EventManager_OnStartupComplete;

            m_scene.RegisterModuleInterface<IParcelManagementModule>(this);
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
            foreach (ILandObject land in m_landList.Values)
            {
                UpdateLandObject(land.LandData.LocalID, land.LandData);
            }

            if (m_UpdateDirectoryOnTimer)
            {
                m_UpdateDirectoryTimer.Stop();
                m_UpdateDirectoryTimer = null;
            }

            m_scene.EventManager.OnAvatarEnteringNewParcel -= EventManagerOnAvatarEnteringNewParcel;
            m_scene.EventManager.OnValidateBuyLand -= EventManagerOnValidateLandBuy;
            m_scene.EventManager.OnNewClient -= EventManagerOnNewClient;
            m_scene.EventManager.OnMakeRootAgent -= CheckEnteringNewParcel;
            m_scene.EventManager.OnSignificantClientMovement -= EventManagerOnSignificantClientMovement;
            m_scene.EventManager.OnSignificantObjectMovement -= EventManagerOnSignificantObjectMovement;
            m_scene.EventManager.OnIncomingLandDataFromStorage -= EventManagerOnIncomingLandDataFromStorage;
            m_scene.EventManager.OnRegisterCaps -= EventManagerOnRegisterCaps;
            m_scene.EventManager.OnClosingClient -= OnClosingClient;
            m_scene.UnregisterModuleInterface<IParcelManagementModule>(this);
        }

        private void EventManager_OnStartupComplete(IScene scene, List<string> data)
        {
            UpdateDirectoryTimerElapsed(null, null);
        }

        private void UpdateDirectoryTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            IDirectoryServiceConnector DSC = Aurora.DataManager.DataManager.RequestPlugin<IDirectoryServiceConnector>();
            if (DSC != null)
            {
                //lock (m_TaintedLandData)
                //{
                foreach (LandData parcel in m_TaintedLandData)
                {
                    LandData p = parcel.Copy();
                    Vector3 OldUserLocation = p.UserLocation;
                    if (p.UserLocation == Vector3.Zero)
                    {
                        //Set it to a position inside the parcel at the ground if it doesn't have one
                        p.UserLocation = GetParcelCenterAtGround(GetLandObject(p.LocalID));
                    }
                    DSC.AddLandObject(p);
                }
                //}
            }
        }

        // this is needed for non-convex parcels (example: rectangular parcel, and in the exact center
        // another, smaller rectangular parcel). Both will have the same initial coordinates.
        private void findPointInParcel(ILandObject land, ref uint refX, ref uint refY)
        {
            // the point we started with already is in the parcel
            if (land.ContainsPoint((int)refX, (int)refY)) return;

            // ... otherwise, we have to search for a point within the parcel
            uint startX = (uint)land.LandData.AABBMin.X;
            uint startY = (uint)land.LandData.AABBMin.Y;
            uint endX = (uint)land.LandData.AABBMax.X;
            uint endY = (uint)land.LandData.AABBMax.Y;

            // default: center of the parcel
            refX = (startX + endX) / 2;
            refY = (startY + endY) / 2;
            // If the center point is within the parcel, take that one
            if (land.ContainsPoint((int)refX, (int)refY)) return;

            // otherwise, go the long way.
            for (uint y = startY; y <= endY; ++y)
            {
                for (uint x = startX; x <= endX; ++x)
                {
                    if (land.ContainsPoint((int)x, (int)y))
                    {
                        // found a point
                        refX = x;
                        refY = y;
                        return;
                    }
                }
            }
        }

        void EventManagerOnNewClient(IClientAPI client)
        {
            //Register some client events
            client.OnParcelPropertiesRequest += ClientOnParcelPropertiesRequest;
            client.OnParcelDivideRequest += ClientOnParcelDivideRequest;
            client.OnParcelJoinRequest += ClientOnParcelJoinRequest;
            client.OnParcelPropertiesUpdateRequest += ClientOnParcelPropertiesUpdateRequest;
            client.OnParcelSelectObjects += ClientOnParcelSelectObjects;
            client.OnParcelObjectOwnerRequest += ClientOnParcelObjectOwnerRequest;
            client.OnParcelAccessListRequest += ClientOnParcelAccessListRequest;
            client.OnParcelAccessListUpdateRequest += ClientOnParcelAccessUpdateListRequest;
            client.OnParcelAbandonRequest += ClientOnParcelAbandonRequest;
            client.OnParcelGodForceOwner += ClientOnParcelGodForceOwner;
            client.OnParcelReclaim += ClientOnParcelReclaim;
            client.OnParcelInfoRequest += ClientOnParcelInfoRequest;
            client.OnParcelDwellRequest += ClientOnParcelDwellRequest;
            client.OnParcelDeedToGroup += ClientOnParcelDeedToGroup;
            client.OnParcelBuyPass += ParcelBuyPass;
            client.OnParcelFreezeUser += ClientOnParcelFreezeUser;
            client.OnParcelEjectUser += ClientOnParcelEjectUser;
            client.OnParcelReturnObjectsRequest += ReturnObjectsInParcel;
            client.OnParcelDisableObjectsRequest += DisableObjectsInParcel;
            client.OnParcelSetOtherCleanTime += SetParcelOtherCleanTime;
            client.OnParcelBuy += ProcessParcelBuy;

            EstateSettings ES = m_scene.RegionInfo.EstateSettings;
            if (UseDwell)
                UseDwell = !ES.BlockDwell;

            IEntity presenceEntity;
            if (m_scene.Entities.TryGetValue (client.AgentId, out presenceEntity) && presenceEntity is IScenePresence)
            {
                Util.FireAndForget(delegate(object o)
                {
                    SendParcelOverlay(client);
                });
            }
        }

        private void OnClosingClient(IClientAPI client)
        {
            client.OnParcelPropertiesRequest -= ClientOnParcelPropertiesRequest;
            client.OnParcelDivideRequest -= ClientOnParcelDivideRequest;
            client.OnParcelJoinRequest -= ClientOnParcelJoinRequest;
            client.OnParcelPropertiesUpdateRequest -= ClientOnParcelPropertiesUpdateRequest;
            client.OnParcelSelectObjects -= ClientOnParcelSelectObjects;
            client.OnParcelObjectOwnerRequest -= ClientOnParcelObjectOwnerRequest;
            client.OnParcelAccessListRequest -= ClientOnParcelAccessListRequest;
            client.OnParcelAccessListUpdateRequest -= ClientOnParcelAccessUpdateListRequest;
            client.OnParcelAbandonRequest -= ClientOnParcelAbandonRequest;
            client.OnParcelGodForceOwner -= ClientOnParcelGodForceOwner;
            client.OnParcelReclaim -= ClientOnParcelReclaim;
            client.OnParcelInfoRequest -= ClientOnParcelInfoRequest;
            client.OnParcelDwellRequest -= ClientOnParcelDwellRequest;
            client.OnParcelDeedToGroup -= ClientOnParcelDeedToGroup;
            client.OnParcelBuyPass -= ParcelBuyPass;
            client.OnParcelFreezeUser -= ClientOnParcelFreezeUser;
            client.OnParcelEjectUser -= ClientOnParcelEjectUser;
            client.OnParcelReturnObjectsRequest -= ReturnObjectsInParcel;
            client.OnParcelDisableObjectsRequest -= DisableObjectsInParcel;
            client.OnParcelSetOtherCleanTime -= SetParcelOtherCleanTime;
            client.OnParcelBuy -= ProcessParcelBuy;
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "LandManagementModule"; }
        }

        #endregion

        #region Heartbeat Tick, Parcel Returns, Clean temp objects

        void EventManager_OnFrame()
        {
            if (m_scene.Frame % m_update_land == 0)
            {
                //It's time, check the parts we have
                CheckFrameEvents();
            }
        }

        object AuroraEventManager_OnGenericEvent (string FunctionName, object parameters)
        {
            if (FunctionName == "ObjectAddedFlag")
            {
                object[] param = (object[])parameters;
                ISceneChildEntity child = (ISceneChildEntity)param[0];
                PrimFlags flag = (PrimFlags)param[1];
                if (flag == PrimFlags.TemporaryOnRez)
                    m_entitiesInAutoReturnQueue.Add (child.ParentEntity);
            }
            else if (FunctionName == "ObjectRemovedFlag")
            {
                object[] param = (object[])parameters;
                ISceneChildEntity child = (ISceneChildEntity)param[0];
                PrimFlags flag = (PrimFlags)param[1];
                if (flag == PrimFlags.TemporaryOnRez)
                    m_entitiesInAutoReturnQueue.Remove (child.ParentEntity);
            }
            return null;
        }

        HashSet<ISceneEntity> m_entitiesInAutoReturnQueue = new HashSet<ISceneEntity> ();

        /// <summary>
        /// Return object to avatar Message
        /// </summary>
        /// <param name="agentID">Avatar Unique Id</param>
        /// <param name="objectName">Name of object returned</param>
        /// <param name="location">Location of object returned</param>
        /// <param name="reason">Reasion for object return</param>
        /// <param name="groups">The objects to return</param>
        public void AddReturns(UUID agentID, string objectName, Vector3 location, string reason, List<ISceneEntity> Groups)
        {
            lock (m_returns)
            {
                if (m_returns.ContainsKey(agentID))
                {
                    ReturnInfo info = m_returns[agentID];
                    info.count += Groups.Count;
                    info.Groups.AddRange(Groups);
                    m_returns[agentID] = info;
                }
                else
                {
                    ReturnInfo info = new ReturnInfo();
                    info.count = Groups.Count;
                    info.objectName = objectName;
                    info.location = location;
                    info.reason = reason;
                    info.Groups = Groups;
                    m_returns[agentID] = info;
                }
            }
        }

        /// <summary>
        /// This deals with sending the return IMs as well as actually returning the objects
        /// </summary>
        protected internal void CheckFrameEvents()
        {
            // Go through all updates and check for temp and auto return
            CheckPrimForAutoReturn();
            CheckPrimForTemperary ();
            lock (m_returns)
            {
                foreach (KeyValuePair<UUID, ReturnInfo> ret in m_returns)
                {
                    if (ret.Value.reason != "")
                    {
                        UUID transaction = UUID.Random ();

                        GridInstantMessage msg = new GridInstantMessage ();
                        msg.fromAgentID = new Guid (UUID.Zero.ToString ()); // From server
                        msg.toAgentID = new Guid (ret.Key.ToString ());
                        msg.imSessionID = new Guid (transaction.ToString ());
                        msg.timestamp = (uint)Util.UnixTimeSinceEpoch ();
                        msg.fromAgentName = "Server";
                        msg.dialog = (byte)19; // Object msg
                        msg.fromGroup = false;
                        msg.offline = (byte)1;
                        msg.ParentEstateID = m_scene.RegionInfo.EstateSettings.ParentEstateID;
                        msg.Position = Vector3.Zero;
                        msg.RegionID = m_scene.RegionInfo.RegionID.Guid;
                        msg.binaryBucket = new byte[0];

                        if (ret.Value.count > 1)
                            msg.message = string.Format ("Your {0} objects were returned from {1} in region {2} due to {3}", ret.Value.count, ret.Value.location.ToString (), m_scene.RegionInfo.RegionName, ret.Value.reason);
                        else
                            msg.message = string.Format ("Your object {0} was returned from {1} in region {2} due to {3}", ret.Value.objectName, ret.Value.location.ToString (), m_scene.RegionInfo.RegionName, ret.Value.reason);

                        IMessageTransferModule tr = m_scene.RequestModuleInterface<IMessageTransferModule> ();
                        if (tr != null)
                            tr.SendInstantMessage (msg);

                        if (ret.Value.Groups.Count > 1)
                            m_log.InfoFormat ("[LandManagement]: Returning {0} objects due to parcel auto return.", ret.Value.Groups.Count);
                        else
                            m_log.Info ("[LandManagement]: Returning 1 object due to parcel auto return.");

                    }
                    IAsyncSceneObjectGroupDeleter async = m_scene.RequestModuleInterface<IAsyncSceneObjectGroupDeleter> ();
                    if (async != null)
                    {
                        async.DeleteToInventory (
                                DeRezAction.Return, ret.Value.Groups[0].RootChild.OwnerID, ret.Value.Groups, ret.Value.Groups[0].RootChild.OwnerID,
                                true, true);
                    }
                }
                m_returns.Clear();
            }
        }

        protected void CheckPrimForTemperary ()
        {
            HashSet<ISceneEntity> entitiesToRemove = new HashSet<ISceneEntity>();
            foreach (ISceneEntity entity in m_entitiesInAutoReturnQueue)
            {
                if (entity.RootChild.Expires <= DateTime.Now)
                {
                    entitiesToRemove.Add (entity);
                    //Temporary objects don't get a reason, they return quietly
                    AddReturns (entity.OwnerID, entity.Name, entity.AbsolutePosition, "", new List<ISceneEntity> () { entity });
                }
            }
            foreach (ISceneEntity entity in entitiesToRemove)
            {
                m_entitiesInAutoReturnQueue.Remove (entity);
            }
        }

        protected void CheckPrimForAutoReturn()
        {
            // Don't abort the whole thing if one entity happens to give us an exception.
            try
            {
                IPrimCountModule primCount = m_scene.RequestModuleInterface<IPrimCountModule> ();
                if(primCount == null)
                    return;
                foreach (ILandObject parcel in AllParcels ())
                {
                    if (parcel != null && parcel.LandData != null &&
                            parcel.LandData.OtherCleanTime != 0)
                    {
                        //This parcel needs its prims iterated
                        foreach (ISceneEntity sog in primCount.GetPrimCounts (parcel.LandData.GlobalID).Objects)
                        {
                            if (parcel.LandData.OwnerID != sog.OwnerID &&
                                    ((parcel.LandData.GroupID == UUID.Zero) || //If there is no group, don't check the groups part
                                    ((parcel.LandData.GroupID != UUID.Zero) && //If there is a group, check for group rezzed prims and group owned prims
                                    (parcel.LandData.GroupID != sog.GroupID && ///Allow prims set to the group
                                    parcel.LandData.GroupID != sog.OwnerID && //Allow group deeded prims!
                                    parcel.LandData.OwnerID != sog.GroupID) //Allow group deeded prims!
                                    )) &&
                                !m_scene.Permissions.IsAdministrator (sog.OwnerID) //Also check for admin/estate status
                                )
                            {
                                //The prim needs to be checked for auto return
                                if ((DateTime.UtcNow - sog.RootChild.Rezzed).TotalSeconds >
                                        parcel.LandData.OtherCleanTime * 60)
                                {
                                    AddReturns (sog.OwnerID, sog.Name, sog.AbsolutePosition, "Auto Parcel Return", new List<ISceneEntity> () { sog });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                m_log.ErrorFormat (
                    "[LandManagement]: Failed to check for parcel returns: {0}", e);
            }
        }

        #endregion

        #region Parcel Add/Remove/Get/Create

        protected void AddLandObjectToSearch(LandData parcel)
        {
            IDirectoryServiceConnector DSC = Aurora.DataManager.DataManager.RequestPlugin<IDirectoryServiceConnector>();
            if (DSC != null)
            {
                Vector3 OldUserLocation = parcel.UserLocation;
                if (parcel.UserLocation == Vector3.Zero)
                {
                    //Set it to a position inside the parcel at the ground if it doesn't have one
                    parcel.UserLocation = GetParcelCenterAtGround(GetLandObject(parcel.LocalID));
                }
                if (m_UpdateDirectoryOnUpdate)
                    //Update search database
                    DSC.AddLandObject(parcel);
                else if (m_UpdateDirectoryOnTimer)
                {
                    //lock (m_TaintedLandData)
                    //{
                    //Tell the timer about it
                    if (!m_TaintedLandData.Contains(parcel))
                        m_TaintedLandData.Add(parcel);
                    //}
                }
                //Reset the position 
                parcel.UserLocation = OldUserLocation;
            }
        }

        public void UpdateLandObject(int local_id, LandData data)
        {
            LandData newData = data.Copy();
            newData.LocalID = local_id;

            lock (m_landList)
            {
                if (m_landList.ContainsKey(local_id))
                {
                    m_landList[local_id].LandData = newData;
                    AddLandObjectToSearch(data);
                    IParcelServiceConnector conn = Aurora.DataManager.DataManager.RequestPlugin<IParcelServiceConnector>();
                    if (conn != null)
                        conn.StoreLandObject(data);
                    m_scene.EventManager.TriggerLandObjectAdded(m_landList[local_id].LandData);
                }
            }
        }

        /// <summary>
        /// Adds a land object to the stored list and adds them to the landIDList to what they own
        /// </summary>
        /// <param name="new_land">The land object being added</param>
        public ILandObject AddLandObject(ILandObject land)
        {
            return AddLandObject(land, false);
        }

        protected ILandObject AddLandObject(ILandObject land, bool incomingFromDatabase)
        {
            //Don't make a copy unless necessary
            ILandObject new_land = incomingFromDatabase ? land : land.Copy();

            lock (m_landList)
            {
                //Update the localID
                int newLandLocalID = ++m_lastLandLocalID;
                new_land.LandData.LocalID = newLandLocalID;

                //Add this parcels area to the region wide area tracker
                bool[,] landBitmap = new_land.GetLandBitmap();
                for (int x = 0; x < m_scene.RegionInfo.RegionSizeX / 4; x++)
                {
                    for (int y = 0; y < m_scene.RegionInfo.RegionSizeY / 4; y++)
                    {
                        if (landBitmap[x, y])
                        {
                            m_landIDList[x, y] = newLandLocalID;
                        }
                    }
                }
                //Add it to the list of land in this region
                m_landList.Add(newLandLocalID, new_land);
            }
            new_land.ForceUpdateLandInfo();
            //If it isn't coming in from the database, make sure to save the new parcel and add it to search
            if (!incomingFromDatabase)
            {
                AddLandObjectToSearch(new_land.LandData);
                IParcelServiceConnector conn = Aurora.DataManager.DataManager.RequestPlugin<IParcelServiceConnector>();
                if (conn != null)
                    conn.StoreLandObject(new_land.LandData);
            }
            //Trigger the event for any interested listeners
            m_scene.EventManager.TriggerLandObjectAdded(new_land.LandData);
            return new_land;
        }

        /// <summary>
        /// Resets the sim to the default land object (full sim piece of land owned by the default user)
        /// </summary>
        public void ResetSimLandObjects()
        {
            //Remove all the land objects in the sim and add a blank, full sim land object set to public
            lock (m_landList)
            {
                m_landList.Clear();
                m_lastLandLocalID = ParcelManagementModule.START_LAND_LOCAL_ID;
                m_landIDList.Initialize();
            }

            ILandObject fullSimParcel = new LandObject(UUID.Zero, false, m_scene);

            fullSimParcel.SetLandBitmap(fullSimParcel.GetSquareLandBitmap(0, 0, m_scene.RegionInfo.RegionSizeX, m_scene.RegionInfo.RegionSizeY));
            
            if (fullSimParcel.LandData.OwnerID == UUID.Zero)
                fullSimParcel.LandData.OwnerID = m_scene.RegionInfo.EstateSettings.EstateOwner;

            UserAccount account = m_scene.UserAccountService.GetUserAccount(m_scene.RegionInfo.ScopeID, fullSimParcel.LandData.OwnerID);

            while (fullSimParcel.LandData.OwnerID == UUID.Zero || account == null)
            {
                m_log.Warn("[ParcelManagement]: Could not find user for parcel, please give a valid user to make the owner");
                string userName = MainConsole.Instance.CmdPrompt("User Name:", "");
                if (userName == "")
                {
                    m_log.Warn("Put in a valid username.");
                    continue;
                }
                account = m_scene.UserAccountService.GetUserAccount(m_scene.RegionInfo.ScopeID, userName);
                if (account != null)
                    fullSimParcel.LandData.OwnerID = account.PrincipalID;
                else
                    m_log.Warn("Could not find the user.");
            }
            m_log.Info("[ParcelManagement]: No land found for region " + m_scene.RegionInfo.RegionName +
                ", setting owner to " + fullSimParcel.LandData.OwnerID);
            fullSimParcel.LandData.ClaimDate = Util.UnixTimeSinceEpoch();
            fullSimParcel.SetInfoID();
            AddLandObject(fullSimParcel);
        }

        public List<ILandObject> AllParcels()
        {
            lock (m_landList)
            {
                return new List<ILandObject>(m_landList.Values);
            }
        }

        public List<ILandObject> ParcelsNearPoint(Vector3 position)
        {
            List<ILandObject> parcelsNear = new List<ILandObject>();
            for (int x = -4; x <= 4; x += 4)
            {
                for (int y = -4; y <= 4; y += 4)
                {
                    ILandObject check = GetLandObject((int)(position.X + x), (int)(position.Y + y));
                    if (check != null)
                    {
                        if (!parcelsNear.Contains(check))
                        {
                            parcelsNear.Add(check);
                        }
                    }
                }
            }

            return parcelsNear;
        }

        public void SendYouAreBannedNotice (IScenePresence avatar)
        {
            avatar.ControllingClient.SendAlertMessage(
                    "You are not allowed on this parcel because you are banned.");
        }

        public void SendYouAreRestrictedNotice (IScenePresence avatar)
        {
            avatar.ControllingClient.SendAlertMessage(
                    "You are not allowed on this parcel because the land owner has restricted access.");
        }

        public void EventManagerOnAvatarEnteringNewParcel (IScenePresence avatar, int localLandID, UUID regionID)
        {
            if (m_scene.RegionInfo.RegionID == regionID)
            {
                ILandObject parcelAvatarIsEntering = GetLandObject(localLandID);

                if (parcelAvatarIsEntering != null)
                {
                    //Tell the clint about it
                    parcelAvatarIsEntering.SendLandUpdateToClient(avatar.ControllingClient);
                    
                    if (UseDwell)
                        parcelAvatarIsEntering.LandData.Dwell += 1;
                    if (avatar.AbsolutePosition.Z < ParcelManagementModule.BAN_LINE_SAFETY_HEIGHT)
                    {
                        if (parcelAvatarIsEntering.IsBannedFromLand(avatar.UUID))
                        {
                            SendYouAreBannedNotice(avatar);
                            Vector3 pos = GetNearestAllowedPosition(avatar);
                            avatar.Teleport(pos);
                        }
                        else if (parcelAvatarIsEntering.IsRestrictedFromLand(avatar.UUID))
                        {
                            SendYouAreRestrictedNotice(avatar);
                            Vector3 pos = GetNearestAllowedPosition(avatar);
                            avatar.Teleport(pos);
                        }
                    }
                }
            }
        }

        public void SendOutNearestBanLine(IClientAPI client)
        {
            IScenePresence sp = m_scene.GetScenePresence (client.AgentId);
            if (sp == null || sp.IsChildAgent)
                return;

            List<ILandObject> checkLandParcels = ParcelsNearPoint(sp.AbsolutePosition);
            int multiple = 0;
            int result = 0;
            foreach (ILandObject checkBan in checkLandParcels)
            {
                if (checkBan.IsBannedFromLand(client.AgentId))
                {
                    multiple++;
                    result |= (int)ParcelPropertiesStatus.CollisionBanned;
                    break; //Only send one
                }
                if (checkBan.IsRestrictedFromLand(client.AgentId))
                {
                    multiple++;
                    result |= (int)ParcelPropertiesStatus.CollisionNotOnAccessList;
                    break; //Only send one
                }
            }
            ParcelResult dataResult = ParcelResult.NoData;
            if (multiple > 1)
                dataResult = ParcelResult.Multiple;
            else if (multiple == 1)
                dataResult = ParcelResult.Single;
            else //If there is no result, don't send anything
                return;

            ILandObject ourLandObject = GetLandObject((int)sp.AbsolutePosition.X, (int)sp.AbsolutePosition.Y);
            if(ourLandObject != null)
                ourLandObject.SendLandProperties(result, false, (int)dataResult, client);
        }

        public void CheckEnteringNewParcel (IScenePresence avatar, bool force)
        {
            ILandObject over = GetLandObject((int)avatar.AbsolutePosition.X,
                                             (int)avatar.AbsolutePosition.Y);

            if (over != null)
            {
                if (force || avatar.CurrentParcelUUID != over.LandData.GlobalID)
                {
                    if (!avatar.IsChildAgent)
                    {
                        avatar.CurrentParcelUUID = over.LandData.GlobalID;
                        m_scene.EventManager.TriggerAvatarEnteringNewParcel(avatar, over.LandData.LocalID,
                                                                            m_scene.RegionInfo.RegionID);
                    }
                }
            }
        }

        private void CheckEnteringNewParcel (IScenePresence avatar)
        {
            CheckEnteringNewParcel(avatar, false);
        }

        public void EventManagerOnSignificantClientMovement(IClientAPI remote_client)
        {
            Util.FireAndForget (delegate (object o)
            {
                IScenePresence clientAvatar = m_scene.GetScenePresence (remote_client.AgentId);
                if (clientAvatar != null)
                {
                    ILandObject over = GetLandObject ((int)clientAvatar.AbsolutePosition.X, (int)clientAvatar.AbsolutePosition.Y);
                    if (over != null)
                    {
                        if (!over.IsRestrictedFromLand (clientAvatar.UUID) && (!over.IsBannedFromLand (clientAvatar.UUID) || clientAvatar.AbsolutePosition.Z >= ParcelManagementModule.BAN_LINE_SAFETY_HEIGHT))
                        {
                            clientAvatar.LastKnownAllowedPosition =
                                new Vector3 (clientAvatar.AbsolutePosition.X, clientAvatar.AbsolutePosition.Y, clientAvatar.AbsolutePosition.Z);
                        }
                        else
                        {
                            //Kick them out
                            Vector3 pos = GetNearestAllowedPosition (clientAvatar);
                            clientAvatar.Teleport (pos);
                        }
                        CheckEnteringNewParcel (clientAvatar, false);
                        SendOutNearestBanLine (remote_client);
                    }
                }
            });
        }

        //Like handleEventManagerOnSignificantClientMovement, but for objects for parcel incoming object permissions
        public void EventManagerOnSignificantObjectMovement (ISceneEntity group)
        {
            ILandObject over = GetLandObject((int)group.AbsolutePosition.X, (int)group.AbsolutePosition.Y);
            if (over != null)
            {
                //Entered this new parcel
                if (over.LandData.GlobalID != group.LastParcelUUID)
                {
                    if (!m_scene.Permissions.CanObjectEntry(group.UUID,
                        false, group.AbsolutePosition, group.OwnerID))
                    {
                        //Revert the position and do not update the parcel ID
                        group.AbsolutePosition = group.LastSignificantPosition;

                        //If the object has physics, stop it from moving
                        if ((group.RootChild.Flags & PrimFlags.Physics) == PrimFlags.Physics)
                        {
                            bool wasTemporary = ((group.RootChild.Flags & PrimFlags.TemporaryOnRez) != 0);
                            bool wasPhantom = ((group.RootChild.Flags & PrimFlags.Phantom) != 0);
                            bool wasVD = group.RootChild.VolumeDetectActive;
                            group.RootChild.UpdatePrimFlags (false, wasTemporary, wasPhantom, wasVD);
                        }
                        //Send an update so that all clients see it
                        group.ScheduleGroupTerseUpdate();
                    }
                    else
                    {
                        UUID oldParcelUUID = group.LastParcelUUID;
                        //Update the UUID then
                        group.LastParcelUUID = over.LandData.GlobalID;
                        //Trigger the event
                        object[] param = new object[3];
                        param[0] = group;
                        param[1] = over.LandData.GlobalID;
                        param[2] = oldParcelUUID;
                        m_scene.AuroraEventManager.FireGenericEventHandler("ObjectEnteringNewParcel", param);
                    }
                }
            }
        }

        public void ClientOnParcelAccessListRequest(UUID agentID, UUID sessionID, uint flags, int sequenceID,
                                                    int landLocalID, IClientAPI remote_client)
        {
            ILandObject land = GetLandObject(landLocalID);

            if (land != null)
            {
                land.SendAccessList(agentID, sessionID, flags, sequenceID, remote_client);
            }
        }

        public void ClientOnParcelAccessUpdateListRequest(UUID agentID, UUID sessionID, uint flags, int landLocalID,
                                                          List<ParcelManager.ParcelAccessEntry> entries,
                                                          IClientAPI remote_client)
        {
            ILandObject land = GetLandObject(landLocalID);

            if (land != null)
            {
                if (m_scene.Permissions.CanEditParcelAccessList(remote_client.AgentId, land, flags))
                {
                    land.UpdateAccessList(flags, entries, remote_client);
                }
            }
            else
            {
                m_log.WarnFormat("[LAND]: Invalid local land ID {0}", landLocalID);
            }
        }

        /// <summary>
        /// Removes a land object from the list. Will not remove if local_id is still owning an area in landIDList
        /// </summary>
        /// <param name="local_id">Land.localID of the peice of land to remove.</param>
        public void removeLandObject(int local_id)
        {
            lock (m_landList)
            {
                for (int x = 0; x < m_scene.RegionInfo.RegionSizeX / 4; x++)
                {
                    for (int y = 0; y < m_scene.RegionInfo.RegionSizeY / 4; y++)
                    {
                        if (m_landIDList[x, y] == local_id)
                        {
                            m_log.WarnFormat("[LAND]: Not removing land object {0}; still being used at {1}, {2}",
                                             local_id, x, y);
                            return;
                            //throw new Exception("Could not remove land object. Still being used at " + x + ", " + y);
                        }
                    }
                }
                IParcelServiceConnector conn = Aurora.DataManager.DataManager.RequestPlugin<IParcelServiceConnector>();
                if (conn != null)
                    conn.RemoveLandObject(m_landList[local_id].LandData.RegionID, m_landList[local_id].LandData.GlobalID);

                m_scene.EventManager.TriggerLandObjectRemoved(m_landList[local_id].LandData.RegionID, m_landList[local_id].LandData.GlobalID);
                m_landList.Remove(local_id);
            }
        }

        public void ParcelBuyPass(IClientAPI client, UUID agentID, int ParcelLocalID)
        {
            ILandObject landObject = GetLandObject(ParcelLocalID);
            if (landObject == null)
            {
                client.SendAlertMessage("Could not find the parcel you are currently on.");
                return;
            }

            if (landObject.IsBannedFromLand(agentID))
            {
                client.SendAlertMessage("You cannot buy a pass as you are banned from this parcel.");
                return;
            }

            IMoneyModule module = m_scene.RequestModuleInterface<IMoneyModule>();
            if (module != null)
                if (!module.ApplyCharge(client.AgentId, landObject.LandData.PassPrice, "Parcel Pass"))
                {
                    client.SendAlertMessage("You do not have enough funds to complete this transaction.");
                    return;
                }

            ParcelManager.ParcelAccessEntry entry = new ParcelManager.ParcelAccessEntry();
            entry.AgentID = agentID;
            entry.Flags = AccessList.Access;
            entry.Time = DateTime.Now.AddHours(landObject.LandData.PassHours);
            landObject.LandData.ParcelAccessList.Add(entry);
            client.SendAgentAlertMessage("You have been added to the parcel access list.", false);
        }

        private void performFinalLandJoin(ILandObject master, ILandObject slave)
        {
            bool[,] landBitmapSlave = slave.GetLandBitmap();
            lock (m_landList)
            {
                for (int x = 0; x < m_scene.RegionInfo.RegionSizeX / 4; x++)
                {
                    for (int y = 0; y < m_scene.RegionInfo.RegionSizeY / 4; y++)
                    {
                        if (landBitmapSlave[x, y])
                        {
                            m_landIDList[x, y] = master.LandData.LocalID;
                        }
                    }
                }
            }

            removeLandObject(slave.LandData.LocalID);
            UpdateLandObject(master.LandData.LocalID, master.LandData);
        }

        public ILandObject GetLandObject(int parcelLocalID)
        {
            lock (m_landList)
            {
                if (m_landList.ContainsKey(parcelLocalID))
                {
                    return m_landList[parcelLocalID];
                }
            }
            return null;
        }

        public ILandObject GetLandObject(UUID GlobalID)
        {
            foreach (ILandObject land in AllParcels())
            {
                if (land.LandData.GlobalID == GlobalID)
                    return land;
            }
            return null;
        }

        public ILandObject GetLandObject(float x, float y)
        {
            return GetLandObject((int)x, (int)y);
        }

        public ILandObject GetLandObject(int x, int y)
        {
            if (x >= m_scene.RegionInfo.RegionSizeX || y >= m_scene.RegionInfo.RegionSizeY || x < 0 || y < 0)
            {
                if (x >= m_scene.RegionInfo.RegionSizeX)
                    x = m_scene.RegionInfo.RegionSizeX - 1;
                if (x < 0)
                    x = 1;
                if (y >= m_scene.RegionInfo.RegionSizeY)
                    y = m_scene.RegionInfo.RegionSizeY - 1;
                if (y < 0)
                    y = 1;
            }

            lock (m_landIDList)
            {
                try
                {
                    int localID = m_landIDList[x / 4, y / 4];
                    return m_landList[m_landIDList[x / 4, y / 4]];
                }
                catch (IndexOutOfRangeException)
                {
                    return null;
                }
                catch (KeyNotFoundException)
                {
                    return null;
                }
            }
        }

        #endregion

        #region Parcel Modification

        /// <summary>
        /// Subdivides a piece of land
        /// </summary>
        /// <param name="start_x">West Point</param>
        /// <param name="start_y">South Point</param>
        /// <param name="end_x">East Point</param>
        /// <param name="end_y">North Point</param>
        /// <param name="attempting_user_id">UUID of user who is trying to subdivide</param>
        /// <returns>Returns true if successful</returns>
        private void subdivide(int start_x, int start_y, int end_x, int end_y, UUID attempting_user_id)
        {
            //First, lets loop through the points and make sure they are all in the same peice of land
            //Get the land object at start

            ILandObject startLandObject = GetLandObject(start_x, start_y);

            if (startLandObject == null) return;

            //Loop through the points
            try
            {
                int totalX = end_x - start_x;
                int totalY = end_y - start_y;
                for (int y = 0; y < totalY; y += 2)
                {
                    for (int x = 0; x < totalX; x += 2)
                    {
                        ILandObject tempLandObject = GetLandObject(start_x + x, start_y + y);
                        if (tempLandObject == null) return;
                        if (tempLandObject != startLandObject) return;
                    }
                }
            }
            catch (Exception)
            {
                return;
            }

            //If we are still here, then they are subdividing within one piece of land
            //Check owner
            IClientAPI client;
            m_scene.TryGetClient(attempting_user_id, out client);

            if (!m_scene.Permissions.CanSubdivideParcel(attempting_user_id, startLandObject) ||
                (!m_scene.RegionInfo.RegionSettings.AllowLandJoinDivide &&
                !m_scene.Permissions.IsGod(attempting_user_id)))
            {
                client.SendAlertMessage("Permissions: you cannot split this parcel.");
                return;
            }

            //Lets create a new land object with bitmap activated at that point (keeping the old land objects info)
            ILandObject newLand = startLandObject.Copy();
            newLand.LandData.Name = newLand.LandData.Name;
            newLand.LandData.GlobalID = UUID.Random();

            newLand.SetLandBitmap(newLand.GetSquareLandBitmap(start_x, start_y, end_x, end_y));
            newLand.SetInfoID();

            //Now, lets set the subdivision area of the original to false
            int startLandObjectIndex = startLandObject.LandData.LocalID;
            lock (m_landList)
            {
                m_landList[startLandObjectIndex].SetLandBitmap(
                    newLand.ModifyLandBitmapSquare(startLandObject.GetLandBitmap(), start_x, start_y, end_x, end_y, false));
                m_landList[startLandObjectIndex].ForceUpdateLandInfo();
            }

            IPrimCountModule primCountsModule = m_scene.RequestModuleInterface<IPrimCountModule>();
            //Taint both land objects
            if (primCountsModule != null)
            {
                primCountsModule.TaintPrimCount(newLand);
                primCountsModule.TaintPrimCount(startLandObject);
            }

            //Now add the new land object
            ILandObject result = AddLandObject(newLand);
            //Fix the old land object as well
            UpdateLandObject(startLandObject.LandData.LocalID, startLandObject.LandData);
            result.SendLandUpdateToAvatarsOverMe();
            //Update the parcel overlay for ALL clients
            m_scene.ForEachClient(SendParcelOverlay);
        }

        /// <summary>
        /// Join 2 land objects together
        /// </summary>
        /// <param name="start_x">x value in first piece of land</param>
        /// <param name="start_y">y value in first piece of land</param>
        /// <param name="end_x">x value in second peice of land</param>
        /// <param name="end_y">y value in second peice of land</param>
        /// <param name="attempting_user_id">UUID of the avatar trying to join the land objects</param>
        /// <returns>Returns true if successful</returns>
        private void join(int start_x, int start_y, int end_x, int end_y, UUID attempting_user_id)
        {
            IClientAPI client;
            m_scene.TryGetClient(attempting_user_id, out client);
            if (client == null)
                return;
            end_x -= 4;
            end_y -= 4;

            List<ILandObject> selectedLandObjects = new List<ILandObject>();
            int stepYSelected;
            for (stepYSelected = start_y; stepYSelected <= end_y; stepYSelected += 4)
            {
                int stepXSelected;
                for (stepXSelected = start_x; stepXSelected <= end_x; stepXSelected += 4)
                {
                    ILandObject p = GetLandObject(stepXSelected, stepYSelected);

                    if (p != null)
                    {
                        if (!selectedLandObjects.Contains(p))
                        {
                            selectedLandObjects.Add(p);
                        }
                    }
                }
            }
            ILandObject masterLandObject = selectedLandObjects[0];

            foreach (ILandObject p in selectedLandObjects)
            {
                if (!m_scene.Permissions.CanSubdivideParcel(attempting_user_id, p) ||
                   (!m_scene.RegionInfo.RegionSettings.AllowLandJoinDivide &&
                   !m_scene.Permissions.CanIssueEstateCommand(attempting_user_id, false)))
                {
                    client.SendAlertMessage("Permissions: you cannot join these parcels");
                    return;
                }
            }

            selectedLandObjects.RemoveAt(0);
            if (selectedLandObjects.Count < 1)
            {
                client.SendAlertMessage("Permissions: select more than one parcel before joining");
                return;
            }

            lock (m_landList)
            {
                foreach (ILandObject slaveLandObject in selectedLandObjects)
                {
                    m_landList[masterLandObject.LandData.LocalID].SetLandBitmap(
                        slaveLandObject.MergeLandBitmaps(masterLandObject.GetLandBitmap(), slaveLandObject.GetLandBitmap()));
                    performFinalLandJoin(masterLandObject, slaveLandObject);
                }
            }
            masterLandObject.LandData.OwnerID = attempting_user_id;
            IPrimCountModule primCountsModule = m_scene.RequestModuleInterface<IPrimCountModule>();
            //Taint both land objects
            if (primCountsModule != null)
            {
                foreach (ILandObject slaveLandObject in selectedLandObjects)
                {
                    primCountsModule.TaintPrimCount(slaveLandObject);
                }
                primCountsModule.TaintPrimCount(masterLandObject);
            }

            masterLandObject.SendLandUpdateToAvatarsOverMe();
        }

        public void Join(int start_x, int start_y, int end_x, int end_y, UUID attempting_user_id)
        {
            join(start_x, start_y, end_x, end_y, attempting_user_id);
        }

        public void Subdivide(int start_x, int start_y, int end_x, int end_y, UUID attempting_user_id)
        {
            subdivide(start_x, start_y, end_x, end_y, attempting_user_id);
        }

        #endregion

        #region Parcel Updating

        /// <summary>
        /// Where we send the ParcelOverlay packet to the client
        /// </summary>
        /// <param name="remote_client">The object representing the client</param>
        public void SendParcelOverlay(IClientAPI remote_client)
        {
            const int LAND_BLOCKS_PER_PACKET = 1024;

            byte[] byteArray = new byte[LAND_BLOCKS_PER_PACKET];
            int byteArrayCount = 0;
            int sequenceID = 0;
            for (int y = 0; y < (ParcelManagementModule.LAND_OVERLAY_CHUNKS * m_scene.RegionInfo.RegionSizeY / Constants.TerrainPatchSize); y++)
            {
                for (int x = 0; x < (ParcelManagementModule.LAND_OVERLAY_CHUNKS * m_scene.RegionInfo.RegionSizeX / Constants.TerrainPatchSize); x++)
                {
                    byte tempByte = 0; //This represents the byte for the current 4x4

                    ILandObject currentParcelBlock = GetLandObject(x * 4, y * 4);

                    if (currentParcelBlock != null)
                    {
                        if (currentParcelBlock.LandData.OwnerID == remote_client.AgentId)
                        {
                            //Owner Flag
                            tempByte = Convert.ToByte(tempByte | ParcelManagementModule.LAND_TYPE_OWNED_BY_REQUESTER);
                        }
                        else if (currentParcelBlock.LandData.SalePrice > 0 &&
                                 (currentParcelBlock.LandData.AuthBuyerID == UUID.Zero ||
                                  currentParcelBlock.LandData.AuthBuyerID == remote_client.AgentId))
                        {
                            //Sale Flag
                            tempByte = Convert.ToByte(tempByte | ParcelManagementModule.LAND_TYPE_IS_FOR_SALE);
                        }
                        else if (currentParcelBlock.LandData.OwnerID == UUID.Zero)
                        {
                            //Public Flag
                            tempByte = Convert.ToByte(tempByte | ParcelManagementModule.LAND_TYPE_PUBLIC);
                        }
                        else if (currentParcelBlock.LandData.GroupID != UUID.Zero)
                        {
                            tempByte = Convert.ToByte(tempByte | ParcelManagementModule.LAND_TYPE_OWNED_BY_GROUP);
                        }
                        else
                        {
                            //Other Flag
                            tempByte = Convert.ToByte(tempByte | ParcelManagementModule.LAND_TYPE_OWNED_BY_OTHER);
                        }

                        //Now for border control

                        ILandObject westParcel = null;
                        ILandObject southParcel = null;
                        if (x > 0)
                        {
                            westParcel = GetLandObject((x - 1) * 4, y * 4);
                        }
                        if (y > 0)
                        {
                            southParcel = GetLandObject(x * 4, (y - 1) * 4);
                        }

                        if (x == 0)
                        {
                            tempByte = Convert.ToByte(tempByte | ParcelManagementModule.LAND_FLAG_PROPERTY_BORDER_WEST);
                        }
                        else if (westParcel != null && westParcel != currentParcelBlock)
                        {
                            tempByte = Convert.ToByte(tempByte | ParcelManagementModule.LAND_FLAG_PROPERTY_BORDER_WEST);
                        }

                        if (y == 0)
                        {
                            tempByte = Convert.ToByte(tempByte | ParcelManagementModule.LAND_FLAG_PROPERTY_BORDER_SOUTH);
                        }
                        else if (southParcel != null && southParcel != currentParcelBlock)
                        {
                            tempByte = Convert.ToByte(tempByte | ParcelManagementModule.LAND_FLAG_PROPERTY_BORDER_SOUTH);
                        }

                        byteArray[byteArrayCount] = tempByte;
                        byteArrayCount++;
                        if (byteArrayCount >= LAND_BLOCKS_PER_PACKET)
                        {
                            remote_client.SendLandParcelOverlay(byteArray, sequenceID);
                            byteArrayCount = 0;
                            sequenceID++;
                            byteArray = new byte[LAND_BLOCKS_PER_PACKET];
                        }
                    }
                }
            }
        }

        public void ClientOnParcelPropertiesRequest(int start_x, int start_y, int end_x, int end_y, int sequence_id,
                                                    bool snap_selection, IClientAPI remote_client)
        {
            //Get the land objects within the bounds
            List<ILandObject> temp = new List<ILandObject>();
            int inc_x = end_x - start_x;
            int inc_y = end_y - start_y;
            for (int x = 0; x < inc_x; x++)
            {
                for (int y = 0; y < inc_y; y++)
                {
                    ILandObject currentParcel = GetLandObject(start_x + x, start_y + y);

                    if (currentParcel != null)
                    {
                        if (!temp.Contains(currentParcel))
                        {
                            currentParcel.ForceUpdateLandInfo();
                            temp.Add(currentParcel);
                        }
                    }
                }
            }

            int requestResult = ParcelManagementModule.LAND_RESULT_SINGLE;
            if (temp.Count > 1)
            {
                requestResult = ParcelManagementModule.LAND_RESULT_MULTIPLE;
            }

            for (int i = 0; i < temp.Count; i++)
            {
                temp[i].SendLandProperties(sequence_id, snap_selection, requestResult, remote_client);
            }

            SendParcelOverlay(remote_client);
        }

        public void ClientOnParcelPropertiesUpdateRequest(LandUpdateArgs args, int localID, IClientAPI remote_client)
        {
            ILandObject land = GetLandObject(localID);

            if (m_scene.Permissions.CanEditParcel(remote_client.AgentId, land))
                if (land != null)
                    land.UpdateLandProperties(args, remote_client);
        }

        public void ClientOnParcelDivideRequest(int west, int south, int east, int north, IClientAPI remote_client)
        {
            subdivide(west, south, east, north, remote_client.AgentId);
        }

        public void ClientOnParcelJoinRequest(int west, int south, int east, int north, IClientAPI remote_client)
        {
            join(west, south, east, north, remote_client.AgentId);
        }

        public void ClientOnParcelSelectObjects(int local_id, int request_type,
                                                List<UUID> returnIDs, IClientAPI remote_client)
        {
            m_landList[local_id].SendForceObjectSelect(local_id, request_type, returnIDs, remote_client);
        }

        public void ClientOnParcelObjectOwnerRequest(int local_id, IClientAPI remote_client)
        {
            ILandObject land = GetLandObject(local_id);

            if (land != null)
            {
                m_landList[local_id].SendLandObjectOwners(remote_client);
            }
            else
            {
                m_log.WarnFormat("[PARCEL]: Invalid land object {0} passed for parcel object owner request", local_id);
            }
        }

        public void ClientOnParcelGodForceOwner(int local_id, UUID ownerID, IClientAPI remote_client)
        {
            ILandObject land = GetLandObject(local_id);

            if (land != null)
            {
                if (m_scene.Permissions.IsGod(remote_client.AgentId))
                {
                    land.LandData.OwnerID = ownerID;
                    land.LandData.AuctionID = 0; //This must be reset!
                    land.LandData.GroupID = UUID.Zero;
                    land.LandData.IsGroupOwned = false;
                    land.LandData.SalePrice = 0;
                    land.LandData.AuthBuyerID = UUID.Zero;
                    land.LandData.Flags &= ~(uint)(ParcelFlags.ForSale | ParcelFlags.ForSaleObjects | ParcelFlags.SellParcelObjects | ParcelFlags.ShowDirectory | ParcelFlags.AllowDeedToGroup | ParcelFlags.ContributeWithDeed);

                    m_scene.ForEachClient(SendParcelOverlay);
                    land.SendLandUpdateToClient(true, remote_client);
                }
            }
        }

        public void ClientOnParcelAbandonRequest(int local_id, IClientAPI remote_client)
        {
            ILandObject land = GetLandObject(local_id);

            if (land != null)
            {
                if (m_scene.Permissions.CanAbandonParcel(remote_client.AgentId, land))
                {
                    land.LandData.OwnerID = m_scene.RegionInfo.EstateSettings.EstateOwner;
                    land.LandData.AuctionID = 0; //This must be reset!
                    land.LandData.GroupID = UUID.Zero;
                    land.LandData.IsGroupOwned = false;
                    land.LandData.SalePrice = 0;
                    land.LandData.AuthBuyerID = UUID.Zero;
                    land.LandData.Flags &= ~(uint)(ParcelFlags.ForSale | ParcelFlags.ForSaleObjects | ParcelFlags.SellParcelObjects | ParcelFlags.ShowDirectory | ParcelFlags.AllowDeedToGroup | ParcelFlags.ContributeWithDeed);
                    m_scene.ForEachClient(SendParcelOverlay);
                    land.SendLandUpdateToClient(true, remote_client);
                }
            }
        }

        public void ClientOnParcelReclaim(int local_id, IClientAPI remote_client)
        {
            ILandObject land = GetLandObject(local_id);

            if (land != null)
            {
                if (m_scene.Permissions.CanReclaimParcel(remote_client.AgentId, land))
                {
                    land.LandData.AuctionID = 0; //This must be reset!
                    land.LandData.OwnerID = m_scene.RegionInfo.EstateSettings.EstateOwner;
                    land.LandData.ClaimDate = Util.UnixTimeSinceEpoch();
                    land.LandData.GroupID = UUID.Zero;
                    land.LandData.IsGroupOwned = false;
                    land.LandData.SalePrice = 0;
                    land.LandData.AuthBuyerID = UUID.Zero;
                    land.LandData.Flags &= ~(uint)(ParcelFlags.ForSale | ParcelFlags.ForSaleObjects | ParcelFlags.SellParcelObjects | ParcelFlags.ShowDirectory | ParcelFlags.AllowDeedToGroup | ParcelFlags.ContributeWithDeed);

                    land.SendLandUpdateToClient(true, remote_client);
                    m_scene.ForEachClient(SendParcelOverlay);
                }
            }
        }

        #endregion

        public void ProcessParcelBuy(UUID agentId, UUID groupId, bool final, bool groupOwned,
                bool removeContribution, int parcelLocalID, int parcelArea, int parcelPrice, bool authenticated)
        {
            EventManager.LandBuyArgs args = new EventManager.LandBuyArgs(agentId, groupId, final, groupOwned,
                                                                         removeContribution, parcelLocalID, parcelArea,
                                                                         parcelPrice, authenticated);

            ILandObject land = GetLandObject(args.parcelLocalID);
            if (land != null)
            {
                // Make sure that we do all checking that we can sell this land
                if (m_scene.EventManager.TriggerValidateBuyLand(args))
                {
                    land.UpdateLandSold(args.agentId, args.groupId, args.groupOwned, (uint)args.transactionID, args.parcelPrice, args.parcelArea);
                }
            }
        }

        // After receiving a land buy packet, first the data needs to
        // be validated. This method validates the right to buy the
        // parcel

        public bool EventManagerOnValidateLandBuy(EventManager.LandBuyArgs e)
        {
            if (e.landValidated == false)
            {
                ILandObject lob = GetLandObject(e.parcelLocalID);

                if (lob != null)
                {
                    UUID AuthorizedID = lob.LandData.AuthBuyerID;
                    int saleprice = lob.LandData.SalePrice;
                    UUID pOwnerID = lob.LandData.OwnerID;

                    bool landforsale = ((lob.LandData.Flags &
                                         (uint)(ParcelFlags.ForSale | ParcelFlags.ForSaleObjects | ParcelFlags.SellParcelObjects)) != 0);
                    if ((AuthorizedID == UUID.Zero || AuthorizedID == e.agentId) && e.parcelPrice >= saleprice && landforsale)
                    {
                        e.parcelOwnerID = pOwnerID;
                        e.landValidated = true;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            return true;
        }

        public void ClientOnParcelDeedToGroup(int parcelLocalID, UUID groupID, IClientAPI remote_client)
        {
            ILandObject land = GetLandObject(parcelLocalID);

            if (!m_scene.Permissions.CanDeedParcel(remote_client.AgentId, land))
                return;

            if (land != null)
            {
                land.DeedToGroup(groupID);

                land.SendLandUpdateToClient(true, remote_client);
                m_scene.ForEachClient(SendParcelOverlay);
            }
        }

        #region Land Object From Storage Functions

        public void EventManagerOnIncomingLandDataFromStorage(List<LandData> data)
        {
            bool result = false;
            for (int i = 0; i < data.Count; i++)
            {
                if (PreprocessIncomingLandObjectFromStorage(data[i]))
                    result = true;
            }
            if (!result)
                ResetSimLandObjects();
        }

        public bool PreprocessIncomingLandObjectFromStorage(LandData data)
        {
            ILandObject new_land = new LandObject(data.OwnerID, data.IsGroupOwned, m_scene);
            new_land.LandData = data;
            new_land.SetInfoID();
            if (!new_land.SetLandBitmapFromByteArray())
                return false;
            AddLandObject(new_land, true);
            return true;
        }

        public void ReturnObjectsInParcel(int localID, uint returnType, UUID[] agentIDs, UUID[] taskIDs, IClientAPI remoteClient)
        {
            if (localID != -1)
            {
                ILandObject selectedParcel = GetLandObject(localID);

                if (selectedParcel == null) return;

                selectedParcel.ReturnLandObjects(returnType, agentIDs, taskIDs, remoteClient);
            }
            else
            {
                foreach (ILandObject selectedParcel in AllParcels())
                {
                    selectedParcel.ReturnLandObjects(returnType, agentIDs, taskIDs, remoteClient);
                }
            }
        }

        public void DisableObjectsInParcel(int localID, uint returnType, UUID[] agentIDs, UUID[] taskIDs, IClientAPI remoteClient)
        {
            if (localID != -1)
            {
                ILandObject selectedParcel = GetLandObject(localID);

                if (selectedParcel == null) return;

                selectedParcel.DisableLandObjects(returnType, agentIDs, taskIDs, remoteClient);
            }
            else
            {
                foreach (ILandObject selectedParcel in AllParcels())
                {
                    selectedParcel.DisableLandObjects(returnType, agentIDs, taskIDs, remoteClient);
                }
            }
        }

        #endregion

        #region CAPS handler

        private OSDMap EventManagerOnRegisterCaps(UUID agentID, IHttpServer server)
        {
            OSDMap retVal = new OSDMap();
            retVal["RemoteParcelRequest"] = CapsUtil.CreateCAPS("RemoteParcelRequest", remoteParcelRequestPath);

            server.AddStreamHandler(new RestStreamHandler("POST", retVal["RemoteParcelRequest"],
                                                       delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                       {
                                                           return RemoteParcelRequest(request, path, param, agentID);
                                                       }));
            retVal["ParcelPropertiesUpdate"] = CapsUtil.CreateCAPS("ParcelPropertiesUpdate", "");
            server.AddStreamHandler(new RestStreamHandler("POST", retVal["ParcelPropertiesUpdate"],
                                                       delegate(string request, string path, string param,
                                                                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
                                                       {
                                                           return ProcessPropertiesUpdate(request, path, param, agentID);
                                                       }));
            return retVal;
        }

        private string ProcessPropertiesUpdate(string request, string path, string param, UUID agentID)
        {
            IClientAPI client;
            if (!m_scene.TryGetClient(agentID, out client))
            {
                m_log.WarnFormat("[LAND] unable to retrieve IClientAPI for {0}", agentID.ToString());
                return OSDParser.SerializeLLSDXmlString(new OSDMap());
            }

            ParcelPropertiesUpdateMessage properties = new ParcelPropertiesUpdateMessage();
            OSDMap args = (OSDMap)OSDParser.DeserializeLLSDXml(request);

            properties.Deserialize(args);

            LandUpdateArgs land_update = new LandUpdateArgs();
            int parcelID = properties.LocalID;
            land_update.AuthBuyerID = properties.AuthBuyerID;
            land_update.Category = properties.Category;
            land_update.Desc = properties.Desc;
            land_update.GroupID = properties.GroupID;
            land_update.LandingType = (byte)properties.Landing;
            land_update.MediaAutoScale = (byte)Convert.ToInt32(properties.MediaAutoScale);
            land_update.MediaID = properties.MediaID;
            land_update.MediaURL = properties.MediaURL;
            land_update.MusicURL = properties.MusicURL;
            land_update.Name = properties.Name;
            land_update.ParcelFlags = (uint)properties.ParcelFlags;
            land_update.PassHours = (int)properties.PassHours;
            land_update.PassPrice = (int)properties.PassPrice;
            land_update.SalePrice = (int)properties.SalePrice;
            land_update.SnapshotID = properties.SnapshotID;
            land_update.UserLocation = properties.UserLocation;
            land_update.UserLookAt = properties.UserLookAt;
            land_update.MediaDescription = properties.MediaDesc;
            land_update.MediaType = properties.MediaType;
            land_update.MediaWidth = properties.MediaWidth;
            land_update.MediaHeight = properties.MediaHeight;
            land_update.MediaLoop = properties.MediaLoop;
            land_update.ObscureMusic = properties.ObscureMusic;
            land_update.ObscureMedia = properties.ObscureMedia;
            ILandObject land = GetLandObject(parcelID);

            if (land != null)
                land.UpdateLandProperties(land_update, client);
            else
            {
                m_log.WarnFormat("[LAND] unable to find parcelID {0}", parcelID);
            }
            return OSDParser.SerializeLLSDXmlString(new OSDMap());
        }

        // We create the InfoUUID by using the regionHandle (64 bit), and the local (integer) x
        // and y coordinate (each 8 bit), encoded in a UUID (128 bit).
        //
        // Request format:
        // <llsd>
        //   <map>
        //     <key>location</key>
        //     <array>
        //       <real>1.23</real>
        //       <real>45..6</real>
        //       <real>78.9</real>
        //     </array>
        //     <key>region_id</key>
        //     <uuid>xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx</uuid>
        //   </map>
        // </llsd>
        private string RemoteParcelRequest(string request, string path, string param, UUID agentID)
        {
            UUID parcelID = UUID.Zero;
            try
            {
                OSDMap map = (OSDMap)OSDParser.DeserializeLLSDXml(request);
                if ((map.ContainsKey("region_id") || map.ContainsKey("region_handle")) && map.ContainsKey("location"))
                {
                    UUID regionID = map["region_id"].AsUUID();
                    OSDArray list = (OSDArray)map["location"];
                    uint x = list[0].AsUInteger();
                    uint y = list[1].AsUInteger();
                    if (map.ContainsKey("region_handle"))
                    {
                        // if you do a "About Landmark" on a landmark a second time, the viewer sends the
                        // region_handle it got earlier via RegionHandleRequest
                        ulong regionHandle = map["region_handle"].AsULong();
                        parcelID = Util.BuildFakeParcelID(regionHandle, x, y);
                    }
                    else if (regionID == m_scene.RegionInfo.RegionID)
                    {
                        // a parcel request for a local parcel => no need to query the grid
                        parcelID = Util.BuildFakeParcelID(m_scene.RegionInfo.RegionHandle, x, y);
                    }
                    else
                    {
                        // a parcel request for a parcel in another region. Ask the grid about the region
                        GridRegion info = m_scene.GridService.GetRegionByUUID(UUID.Zero, regionID);
                        if (info != null)
                            parcelID = Util.BuildFakeParcelID(info.RegionHandle, x, y);
                    }
                }
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[LAND] Fetch error: {0}", e.Message);
                m_log.ErrorFormat("[LAND] ... in request {0}", request);
            }

            if (parcelID == UUID.Zero)
            {
                m_log.Warn ("[Land]: Failed to find parcel, " + request);
            }

            OSDMap res = new OSDMap();
            res["parcel_id"] = parcelID;
            m_log.DebugFormat("[LAND] got parcelID {0}", parcelID);

            return OSDParser.SerializeLLSDXmlString(res);
        }

        #endregion

        private void ClientOnParcelDwellRequest(int localID, IClientAPI remoteClient)
        {
            ILandObject selectedParcel = GetLandObject(localID);
            if (selectedParcel == null)
                return;

            remoteClient.SendParcelDwellReply(localID, selectedParcel.LandData.GlobalID, selectedParcel.LandData.Dwell);
        }

        private void ClientOnParcelInfoRequest(IClientAPI remoteClient, UUID parcelID)
        {
            if (parcelID == UUID.Zero)
                return;
            ulong RegionHandle = 0;
            uint X, Y;
            Util.ParseFakeParcelID(parcelID, out RegionHandle,
                out X, out Y);
            m_log.DebugFormat("[LAND] got parcelinfo request for regionHandle {0}, x/y {1}/{2}",
                                                                            RegionHandle, X, Y);
            IDirectoryServiceConnector DSC = Aurora.DataManager.DataManager.RequestPlugin<IDirectoryServiceConnector>();
            if (DSC != null)
            {
                LandData data = DSC.GetParcelInfo(parcelID);

                if (data != null)  // if we found some data, send it
                {
                    GridRegion info;
                    if (RegionHandle == m_scene.RegionInfo.RegionHandle)
                    {
                        info = new GridRegion(m_scene.RegionInfo);
                    }
                    else
                    {
                        int RegionX, RegionY;
                        Util.UlongToInts(RegionHandle, out RegionX, out RegionY);
                        // most likely still cached from building the extLandData entry
                        info = m_scene.GridService.GetRegionByPosition(UUID.Zero, RegionX, RegionY);
                    }
                    if (info == null)
                    {
                        m_log.WarnFormat("[LAND]: Failed to find region having parcel {0} @ {1) {2}", parcelID, X, Y);
                        return;
                    }
                    // we need to transfer the fake parcelID, not the one in landData, so the viewer can match it to the landmark.
                    m_log.DebugFormat("[LAND] got parcelinfo for parcel {0} in region {1}; sending...",
                                      data.Name, RegionHandle);
                    remoteClient.SendParcelInfo(data, parcelID, (uint)(info.RegionLocX + data.UserLocation.X), (uint)(info.RegionLocY + data.UserLocation.Y), info.RegionName);
                }
                else
                    m_log.WarnFormat("[LAND]: Failed to find parcel {0}", parcelID);
            }
            else
                m_log.Debug("[LAND] got no directory service; not sending");
        }

        public void SetParcelOtherCleanTime(IClientAPI remoteClient, int localID, int otherCleanTime)
        {
            ILandObject land = GetLandObject(localID);

            if (land == null) return;

            if (!m_scene.Permissions.CanEditParcel(remoteClient.AgentId, land))
                return;

            land.LandData.OtherCleanTime = otherCleanTime;

            UpdateLandObject(localID, land.LandData);
        }

        public void ClientOnParcelFreezeUser(IClientAPI client, UUID parcelowner, uint flags, UUID target)
        {
            IScenePresence targetAvatar = m_scene.GetScenePresence (target);
            IScenePresence parcelOwner = m_scene.GetScenePresence (parcelowner);

            ILandObject land = GetLandObject(targetAvatar.AbsolutePosition.X, targetAvatar.AbsolutePosition.Y);
            if (!m_scene.Permissions.GenericParcelPermission(client.AgentId, land, (ulong)GroupPowers.LandEjectAndFreeze))
                return;

            if (flags == 0)
            {
                targetAvatar.Frozen = true;
                targetAvatar.ControllingClient.SendAlertMessage(parcelOwner.Name + " has frozen you for 30 seconds.  You cannot move or interact with the world.");
                parcelOwner.ControllingClient.SendAlertMessage("Avatar Frozen.");
            }
            else
            {
                targetAvatar.Frozen = false;
                targetAvatar.ControllingClient.SendAlertMessage(parcelOwner.Name + " has unfrozen you.");
                parcelOwner.ControllingClient.SendAlertMessage("Avatar Unfrozen.");
            }
        }

        public void ClientOnParcelEjectUser(IClientAPI client, UUID parcelowner, uint flags, UUID target)
        {
            IScenePresence targetAvatar = m_scene.GetScenePresence (target);
            IScenePresence parcelOwner = m_scene.GetScenePresence (parcelowner);

            ILandObject land = GetLandObject(targetAvatar.AbsolutePosition.X, targetAvatar.AbsolutePosition.Y);
            if (!m_scene.Permissions.GenericParcelPermission(client.AgentId, land, (ulong)GroupPowers.LandEjectAndFreeze))
                return;

            land.LandData.ParcelAccessList.Add(new ParcelManager.ParcelAccessEntry()
            {
                AgentID = targetAvatar.UUID,
                Flags = AccessList.Ban,
                Time = DateTime.MaxValue
            });

            land.LandData.Flags |= (uint)ParcelFlags.UseBanList;

            Vector3 Pos = GetNearestAllowedPosition(targetAvatar);

            if (flags == 0)
            {
                //Remove if ban wasn't selected
                land.LandData.ParcelAccessList.Remove(new ParcelManager.ParcelAccessEntry()
                {
                    AgentID = targetAvatar.UUID,
                    Flags = AccessList.Ban,
                    Time = DateTime.MaxValue
                });
            }

            targetAvatar.Teleport(Pos);

            targetAvatar.ControllingClient.SendAlertMessage("You have been ejected by " + parcelOwner.Name);
            parcelOwner.ControllingClient.SendAlertMessage("Avatar Ejected.");
        }
    }
}
