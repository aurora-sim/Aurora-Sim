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
using System.Reflection;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using GridRegion = Aurora.Framework.GridRegion;

/*
 * Steps to add a new prioritization policy:
 * 
 *  - Add a new value to the UpdatePrioritizationSchemes enum.
 *  - Specify this new value in the [InterestManagement] section of your
 *    Aurora.ini. The name in the config file must match the enum value name
 *    (although it is not case sensitive).
 *  - Write a new GetPriorityBy*() method in this class.
 *  - Add a new entry to the switch statement in GetUpdatePriority() that calls
 *    your method.
 */

namespace Aurora.Region
{
    public class Culler : ICuller
    {
        private readonly Dictionary<uint, bool> m_previousCulled = new Dictionary<uint, bool>();
        private readonly bool m_useDistanceCulling = true;
        private int m_lastCached;
        private float m_sizeToForceDualCulling = 10f;
        private bool m_useCulling = true;

        public Culler(IScene scene)
        {
            IConfig interestConfig = scene.Config.Configs["InterestManagement"];
            if (interestConfig != null)
            {
                m_useCulling = interestConfig.GetBoolean("UseCulling", m_useCulling);
                m_useDistanceCulling = interestConfig.GetBoolean("UseDistanceBasedCulling", m_useDistanceCulling);
            }
        }

        #region ICuller Members

        public bool UseCulling
        {
            get { return m_useCulling; }
            set { m_useCulling = value; }
        }

        public bool ShowEntityToClient(IScenePresence client, IEntity entity, IScene scene)
        {
            return ShowEntityToClient(client, entity, scene, Util.EnvironmentTickCount());
        }

        #endregion

        public bool ShowEntityToClient(IScenePresence client, IEntity entity, IScene scene, int currentTickCount)
        {
            if (!m_useCulling)
                return true; //If we arn't using culling, return true by default to show all prims
            if (entity == null || client == null || scene == null)
                return false;

            bool cull = false;
            lock (m_previousCulled)
            {
                if (m_previousCulled.TryGetValue(entity.LocalId, out cull))
                {
                    Int32 diff = currentTickCount - m_lastCached;
                    Int32 timingDiff = (diff >= 0) ? diff : (diff + Util.EnvironmentTickCountMask + 1);
                    if (timingDiff > 5*1000) //Only recheck every 5 seconds
                    {
                        m_lastCached = Util.EnvironmentTickCount();
                        m_previousCulled.Clear();
                    }
                    else
                        return cull;
                }
            }

            if (m_useDistanceCulling && !DistanceCulling(client, entity, scene))
            {
                lock (m_previousCulled)
                    m_previousCulled[entity.LocalId] = false;
                return false;
            }

            if (!ParcelPrivateCulling(client, entity))
            {
                lock (m_previousCulled)
                    m_previousCulled[entity.LocalId] = false;
                return false;
            }

            //No more, guess its fine
            lock (m_previousCulled)
                m_previousCulled[entity.LocalId] = true;
            return true;
        }

        private bool ParcelPrivateCulling(IScenePresence client, IEntity entity)
        {
            if (entity is IScenePresence)
            {
                IScenePresence pEntity = (IScenePresence) entity;
                if ((client.CurrentParcel != null &&
                     client.CurrentParcel.LandData.Private) ||
                    (pEntity.CurrentParcel != null &&
                     pEntity.CurrentParcel.LandData.Private))
                {
                    //We need to check whether this presence is sitting on anything, so that we can check from the object's
                    // position, rather than the offset position of the object that the avatar is sitting on
                    if (pEntity.CurrentParcelUUID != client.CurrentParcelUUID)
                        return false; //Can't see avatar's outside the parcel
                }
            }
            return true;
        }

        public bool DistanceCulling(IScenePresence client, IEntity entity, IScene scene)
        {
            float DD = client.DrawDistance;
            if (DD < 32) //Limit to a small distance
                DD = 32;
            if (DD > scene.RegionInfo.RegionSizeX &&
                DD > scene.RegionInfo.RegionSizeY)
                return true; //Its larger than the region, no culling check even necessary
            Vector3 posToCheckFrom = client.GetAbsolutePosition();
            if (client.IsChildAgent)
            {
                /*if (m_cachedXOffset == 0 && m_cachedYOffset == 0) //Not found yet
                {
                    int RegionLocX, RegionLocY;
                    Util.UlongToInts(client.RootAgentHandle, out RegionLocX, out RegionLocY);
                    m_cachedXOffset = scene.RegionInfo.RegionLocX - RegionLocX;
                    m_cachedYOffset = scene.RegionInfo.RegionLocY - RegionLocY;
                }
                //We need to add the offset so that we can check from the right place in child regions
                if (m_cachedXOffset < 0)
                    posToCheckFrom.X = scene.RegionInfo.RegionSizeX -
                                       (scene.RegionInfo.RegionSizeX + client.AbsolutePosition.X + m_cachedXOffset);
                if (m_cachedYOffset < 0)
                    posToCheckFrom.Y = scene.RegionInfo.RegionSizeY -
                                       (scene.RegionInfo.RegionSizeY + client.AbsolutePosition.Y + m_cachedYOffset);
                if (m_cachedXOffset > scene.RegionInfo.RegionSizeX)
                    posToCheckFrom.X = scene.RegionInfo.RegionSizeX -
                                       (scene.RegionInfo.RegionSizeX - (client.AbsolutePosition.X + m_cachedXOffset));
                if (m_cachedYOffset > scene.RegionInfo.RegionSizeY)
                    posToCheckFrom.Y = scene.RegionInfo.RegionSizeY -
                                       (scene.RegionInfo.RegionSizeY - (client.AbsolutePosition.Y + m_cachedYOffset));*/
            }
            Vector3 entityPosToCheckFrom = Vector3.Zero;
            bool doHeavyCulling = false;
            if (entity is ISceneEntity)
            {
                doHeavyCulling = true;
                //We need to check whether this object is an attachment, and if so, set it so that we check from the avatar's
                // position, rather than from the offset of the attachment
                ISceneEntity sEntity = (ISceneEntity) entity;
                if (sEntity.RootChild.IsAttachment)
                {
                    IScenePresence attachedAvatar = scene.GetScenePresence(sEntity.RootChild.AttachedAvatar);
                    if (attachedAvatar != null)
                        entityPosToCheckFrom = attachedAvatar.AbsolutePosition;
                }
                else
                    entityPosToCheckFrom = sEntity.RootChild.GetGroupPosition();
            }
            else if (entity is IScenePresence)
            {
                //We need to check whether this presence is sitting on anything, so that we can check from the object's
                // position, rather than the offset position of the object that the avatar is sitting on
                IScenePresence pEntity = (IScenePresence) entity;
                if (pEntity.Sitting)
                {
                    ISceneChildEntity sittingEntity = scene.GetSceneObjectPart(pEntity.SittingOnUUID);
                    if (sittingEntity != null)
                        entityPosToCheckFrom = sittingEntity.GetGroupPosition();
                }
                else
                    entityPosToCheckFrom = pEntity.GetAbsolutePosition();
            }
            //If the distance is greater than the clients draw distance, its out of range
            if (Vector3.DistanceSquared(posToCheckFrom, entityPosToCheckFrom) >
                DD*DD) //Use squares to make it faster than having to do the sqrt
            {
                if (!doHeavyCulling)
                    return false; //Don't do the hardcore checks
                ISceneEntity childEntity = (entity as ISceneEntity);
                if (childEntity != null && HardCullingCheck(childEntity))
                {
                    #region Side culling check (X, Y, Z) plane checks

                    if (
                        Vector3.DistanceSquared(posToCheckFrom,
                                                entityPosToCheckFrom + new Vector3(childEntity.OOBsize.X, 0, 0)) <
                        DD*DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (
                        Vector3.DistanceSquared(posToCheckFrom,
                                                entityPosToCheckFrom - new Vector3(childEntity.OOBsize.X, 0, 0)) <
                        DD*DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (
                        Vector3.DistanceSquared(posToCheckFrom,
                                                entityPosToCheckFrom + new Vector3(0, childEntity.OOBsize.Y, 0)) <
                        DD*DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (
                        Vector3.DistanceSquared(posToCheckFrom,
                                                entityPosToCheckFrom - new Vector3(0, childEntity.OOBsize.Y, 0)) <
                        DD*DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (
                        Vector3.DistanceSquared(posToCheckFrom,
                                                entityPosToCheckFrom + new Vector3(0, 0, childEntity.OOBsize.Z)) <
                        DD*DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (
                        Vector3.DistanceSquared(posToCheckFrom,
                                                entityPosToCheckFrom - new Vector3(0, 0, childEntity.OOBsize.Z)) <
                        DD*DD) //Use squares to make it faster than having to do the sqrt
                        return true;

                    #endregion

                    #region Corner checks ((x,y),(-x,-y),(x,-y),(-x,y), (y,z),(-y,-z),(y,-z),(-y,z), (x,z),(-x,-z),(x,-z),(-x,z))

                    if (
                        Vector3.DistanceSquared(posToCheckFrom,
                                                entityPosToCheckFrom +
                                                new Vector3(childEntity.OOBsize.X, childEntity.OOBsize.Y, 0)) <
                        DD*DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (
                        Vector3.DistanceSquared(posToCheckFrom,
                                                entityPosToCheckFrom -
                                                new Vector3(childEntity.OOBsize.X, childEntity.OOBsize.Y, 0)) <
                        DD*DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (
                        Vector3.DistanceSquared(posToCheckFrom,
                                                entityPosToCheckFrom +
                                                new Vector3(childEntity.OOBsize.X, -childEntity.OOBsize.Y, 0)) <
                        DD*DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (
                        Vector3.DistanceSquared(posToCheckFrom,
                                                entityPosToCheckFrom -
                                                new Vector3(childEntity.OOBsize.X, -childEntity.OOBsize.Y, 0)) <
                        DD*DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (
                        Vector3.DistanceSquared(posToCheckFrom,
                                                entityPosToCheckFrom +
                                                new Vector3(0, childEntity.OOBsize.Y, childEntity.OOBsize.Z)) <
                        DD*DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (
                        Vector3.DistanceSquared(posToCheckFrom,
                                                entityPosToCheckFrom -
                                                new Vector3(0, childEntity.OOBsize.Y, childEntity.OOBsize.Z)) <
                        DD*DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (
                        Vector3.DistanceSquared(posToCheckFrom,
                                                entityPosToCheckFrom +
                                                new Vector3(0, childEntity.OOBsize.Y, -childEntity.OOBsize.Z)) <
                        DD*DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (
                        Vector3.DistanceSquared(posToCheckFrom,
                                                entityPosToCheckFrom -
                                                new Vector3(0, childEntity.OOBsize.Y, -childEntity.OOBsize.Z)) <
                        DD*DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (
                        Vector3.DistanceSquared(posToCheckFrom,
                                                entityPosToCheckFrom +
                                                new Vector3(childEntity.OOBsize.X, 0, childEntity.OOBsize.Z)) <
                        DD*DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (
                        Vector3.DistanceSquared(posToCheckFrom,
                                                entityPosToCheckFrom -
                                                new Vector3(childEntity.OOBsize.X, 0, childEntity.OOBsize.Z)) <
                        DD*DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (
                        Vector3.DistanceSquared(posToCheckFrom,
                                                entityPosToCheckFrom +
                                                new Vector3(-childEntity.OOBsize.X, 0, childEntity.OOBsize.Z)) <
                        DD*DD) //Use squares to make it faster than having to do the sqrt
                        return true;
                    if (
                        Vector3.DistanceSquared(posToCheckFrom,
                                                entityPosToCheckFrom -
                                                new Vector3(-childEntity.OOBsize.X, 0, childEntity.OOBsize.Z)) <
                        DD*DD) //Use squares to make it faster than having to do the sqrt
                        return true;

                    #endregion
                }
                return false;
            }

            return true;
        }

        private bool HardCullingCheck(ISceneEntity childEntity)
        {
            Vector3 OOBsize = childEntity.OOBsize;
            if (LengthSquared(OOBsize.X, OOBsize.Y) > m_sizeToForceDualCulling*m_sizeToForceDualCulling ||
                LengthSquared(OOBsize.Y, OOBsize.Z) > m_sizeToForceDualCulling*m_sizeToForceDualCulling ||
                LengthSquared(OOBsize.Z, OOBsize.X) > m_sizeToForceDualCulling*m_sizeToForceDualCulling)
                return true;
            return false;
        }

        private float LengthSquared(float a, float b)
        {
            return (a*a) + (b*b);
        }
    }

    public class Prioritizer : IPrioritizer
    {
        private readonly double m_childReprioritizationDistance = 20.0;

        public Prioritizer(IScene scene)
        {
            IConfig interestConfig = scene.Config.Configs["InterestManagement"];
            if (interestConfig != null)
                m_childReprioritizationDistance = interestConfig.GetDouble("ChildReprioritizationDistance", 20.0);

        }

        #region IPrioritizer Members

        public double ChildReprioritizationDistance
        {
            get { return m_childReprioritizationDistance; }
        }

        public double GetUpdatePriority(IScenePresence client, IEntity entity)
        {
            double priority = 0;

            if (entity == null)
                return double.PositiveInfinity;

            try
            {
                priority = GetPriorityByOOBDistance(client, entity);
            }
            catch (Exception ex)
            {
                if (!(ex is InvalidOperationException))
                {
                    MainConsole.Instance.Warn("[PRIORITY]: Error in finding priority of a prim/user:" + ex);
                }
                //Set it to max if it errors
                priority = double.PositiveInfinity;
            }

            return priority;
        }

        #endregion

        private double GetPriorityByOOBDistance(IScenePresence presence, IEntity entity)
        {
            // If this is an update for our own avatar give it the highest priority
            if (presence == entity)
                return 0.0;

            // Use the camera position for local agents and avatar position for remote agents
            Vector3 presencePos = (presence.IsChildAgent)
                                      ? presence.AbsolutePosition
                                      : presence.CameraPosition;


            // Use group position for child prims
            Vector3 entityPos;
            float distsq;
            if (entity is SceneObjectGroup)
            {
                SceneObjectGroup g = (SceneObjectGroup) entity;
                entityPos = g.AbsolutePosition;
                distsq = Vector3.DistanceSquared(presencePos, entityPos);
                if (g.IsAttachment)
                    return 0.0;
            }
            else if (entity is SceneObjectPart)
            {
                SceneObjectPart p = (SceneObjectPart) entity;
                if (p.IsRoot)
                {
                    SceneObjectGroup g = p.ParentGroup;
                    entityPos = g.AbsolutePosition;
                    distsq = Vector3.DistanceSquared(presencePos, entityPos);
                    if (g.IsAttachment)
                        return 0.0;
                }
                else
                {
                    distsq = p.clampedAABdistanceToSQ(presencePos) + 1.0f;
                    if (p.ParentGroup.RootChild.IsAttachment)
                        return 1.0;
                }
            }
            else
            {
                entityPos = entity.AbsolutePosition;
                distsq = Vector3.DistanceSquared(presencePos, entityPos);
            }

            return distsq;
        }
    }
}