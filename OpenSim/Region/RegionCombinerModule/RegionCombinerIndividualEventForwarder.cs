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
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.CoreModules.Avatar.Attachments;
using OpenSim.Region.CoreModules.Avatar.Gods;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.RegionCombinerModule
{
    public class RegionCombinerIndividualEventForwarder
    {
        private Scene m_rootScene;
        private Scene m_virtScene;

        public RegionCombinerIndividualEventForwarder(Scene rootScene, Scene virtScene)
        {
            m_rootScene = rootScene;
            m_virtScene = virtScene;
        }

        public void ClientConnect(IClientAPI client)
        {
            m_virtScene.UnSubscribeToClientPrimEvents(client);
            m_virtScene.UnSubscribeToClientPrimRezEvents(client);
            
            m_virtScene.UnSubscribeToClientNetworkEvents(client);

            m_rootScene.SubscribeToClientPrimEvents(client);
            m_rootScene.UnSubscribeToClientPrimRezEvents(client);
            m_virtScene.UnSubscribeToClientPrimRezEvents(client);

            IInventoryAccessModule module = m_virtScene.RequestModuleInterface<IInventoryAccessModule>();
            if (module != null) //Remove OnRezObject
                module.OnClosingClient(client);

            client.OnAddPrim += LocalAddNewPrim;
            client.OnRezObject += LocalRezObject;
            
            client.OnObjectDuplicateOnRay += LocalObjectDuplicateOnRay;
            
            m_rootScene.SubscribeToClientNetworkEvents(client);
        }

        public void ClosingClient(IClientAPI client)
        {
            client.OnAddPrim -= LocalAddNewPrim;
            client.OnRezObject -= LocalRezObject;
            client.OnObjectDuplicateOnRay -= LocalObjectDuplicateOnRay;
        }

        public void ClientClosed(UUID clientid, Scene scene)
        {
        }

        /// <summary>
        /// Fixes position based on the region the Rez event came in on
        /// </summary>
        /// <param name="remoteclient"></param>
        /// <param name="itemid"></param>
        /// <param name="rayend"></param>
        /// <param name="raystart"></param>
        /// <param name="raytargetid"></param>
        /// <param name="bypassraycast"></param>
        /// <param name="rayendisintersection"></param>
        /// <param name="rezselected"></param>
        /// <param name="removeitem"></param>
        /// <param name="fromtaskid"></param>
        private void LocalRezObject(IClientAPI remoteclient, UUID itemid, Vector3 rayend, Vector3 raystart, 
            UUID raytargetid, byte bypassraycast, bool rayendisintersection, bool rezselected, bool removeitem, 
            UUID fromtaskid)
        {
            int differenceX = (int)m_virtScene.RegionInfo.RegionLocX - (int)m_rootScene.RegionInfo.RegionLocX;
            int differenceY = (int)m_virtScene.RegionInfo.RegionLocY - (int)m_rootScene.RegionInfo.RegionLocY;
            rayend.X += differenceX;
            rayend.Y += differenceY;
            raystart.X += differenceX;
            raystart.Y += differenceY;

            IInventoryAccessModule module = m_rootScene.RequestModuleInterface<IInventoryAccessModule>();
            if(module != null)
                module.RezObject(remoteclient, itemid, rayend, raystart, raytargetid, bypassraycast,
                                  rayendisintersection, rezselected, removeitem, fromtaskid, false);
        }
        /// <summary>
        /// Fixes position based on the region the AddPrimShape event came in on
        /// </summary>
        /// <param name="ownerid"></param>
        /// <param name="groupid"></param>
        /// <param name="rayend"></param>
        /// <param name="rot"></param>
        /// <param name="shape"></param>
        /// <param name="bypassraycast"></param>
        /// <param name="raystart"></param>
        /// <param name="raytargetid"></param>
        /// <param name="rayendisintersection"></param>
        private void LocalAddNewPrim(UUID ownerid, UUID groupid, Vector3 rayend, Quaternion rot, 
            PrimitiveBaseShape shape, byte bypassraycast, Vector3 raystart, UUID raytargetid, 
            byte rayendisintersection)
        {
            int differenceX = (int)m_virtScene.RegionInfo.RegionLocX - (int)m_rootScene.RegionInfo.RegionLocX;
            int differenceY = (int)m_virtScene.RegionInfo.RegionLocY - (int)m_rootScene.RegionInfo.RegionLocY;
            rayend.X += differenceX;
            rayend.Y += differenceY;
            raystart.X += differenceX;
            raystart.Y += differenceY;
            m_rootScene.SceneGraph.AddNewPrim(ownerid, groupid, rayend, rot, shape, bypassraycast, raystart, raytargetid,
                                   rayendisintersection);
        }

        /// <summary>
        /// Duplicates object specified by localID at position raycasted against RayTargetObject using 
        /// RayEnd and RayStart to determine what the angle of the ray is
        /// </summary>
        /// <param name="localID">ID of object to duplicate</param>
        /// <param name="dupeFlags"></param>
        /// <param name="AgentID">Agent doing the duplication</param>
        /// <param name="GroupID">Group of new object</param>
        /// <param name="RayTargetObj">The target of the Ray</param>
        /// <param name="RayEnd">The ending of the ray (farthest away point)</param>
        /// <param name="RayStart">The Beginning of the ray (closest point)</param>
        /// <param name="BypassRaycast">Bool to bypass raycasting</param>
        /// <param name="RayEndIsIntersection">The End specified is the place to add the object</param>
        /// <param name="CopyCenters">Position the object at the center of the face that it's colliding with</param>
        /// <param name="CopyRotates">Rotate the object the same as the localID object</param>
        public void LocalObjectDuplicateOnRay(uint localID, uint dupeFlags, UUID AgentID, UUID GroupID,
                                           UUID RayTargetObj, Vector3 RayEnd, Vector3 RayStart,
                                           bool BypassRaycast, bool RayEndIsIntersection, bool CopyCenters, bool CopyRotates)
        {
            int differenceX = (int)m_virtScene.RegionInfo.RegionLocX - (int)m_rootScene.RegionInfo.RegionLocX;
            int differenceY = (int)m_virtScene.RegionInfo.RegionLocY - (int)m_rootScene.RegionInfo.RegionLocY;
            RayEnd.X += differenceX;
            RayEnd.Y += differenceY;
            RayStart.X += differenceX;
            RayStart.Y += differenceY;

            m_rootScene.SceneGraph.doObjectDuplicateOnRay(localID, dupeFlags, AgentID, GroupID, RayTargetObj,
                RayEnd, RayStart, BypassRaycast, RayEndIsIntersection, CopyCenters, CopyRotates);
        }
    }
}