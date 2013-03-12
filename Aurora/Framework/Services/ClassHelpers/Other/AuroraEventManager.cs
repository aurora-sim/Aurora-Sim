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
using System.Linq;

namespace Aurora.Framework
{
    /// <summary>
    ///   Delegate to fire when a generic event comes in
    /// </summary>
    /// <param name = "FunctionName">Name of the event being fired</param>
    /// <param name = "parameters">Parameters that the event has, can be null</param>
    public delegate object OnGenericEventHandler(string FunctionName, object parameters);

    /// <summary>
    ///   A generic event manager that fires one event for many generic events
    /// </summary>
    public class AuroraEventManager
    {
        private readonly Dictionary<string, List<OnGenericEventHandler>> m_events =
            new Dictionary<string, List<OnGenericEventHandler>>();

        /// <summary>
        ///   Events so far:
        /// 
        ///   DrawDistanceChanged - Changed Draw Distance
        ///     param is a ScenePresence
        ///   BanUser - Added a new banned user to the estate bans
        ///     param is a UUID of an agent
        ///   UnBanUser - Removed a banned user from the estate bans
        ///     param is a UUID of an agent
        ///   SignficantCameraMovement - The Camera has moved a distance that has triggered this update
        ///     param is a ScenePresence
        ///   ObjectSelected - An object has been selected
        ///     param is a SceneObjectPart
        ///   ObjectDeselected - An object has been selected
        ///     param is a SceneObjectPart
        ///   ObjectChangedOwner - An object's owner was changed
        ///     param is a SceneObjectGroup
        ///   ObjectChangedPhysicalStatus - An object's physical status has changed
        ///     param is a SceneObjectGroup
        ///   ObjectEnteringNewParcel - An object has entered a new parcel
        ///     param is an object[], with o[0] a SceneObjectGroup, o[1] the new parcel UUID, and o[2] the old parcel UUID
        ///   UserStatusChange - User's status (logged in/out) has changed
        ///     param is a object[], with o[0] the UUID of the user (as a string), o[1] whether they are logging in, o[2] the region they are entering (if logging in)
        ///   PreRegisterRegion - A region is about to be registered
        ///     param is a GridRegion
        ///   NewUserConnection - A new user has been added to the scene (child or root)
        ///     param is an object[], with o[0] OSDMap that will be returned to the server, and o[1] the AgentCircuitData that will be added to the region
        ///   EstateUpdated - An estate has been updated
        ///     param is the EstateSettings of the changed estate
        ///   ObjectAddedFlag - An object in the Scene has added a prim flag
        ///     param is an object[], with o[0] a ISceneChildEntity and o[1] the flag that was changed
        ///   ObjectRemovedFlag - An object in the Scene has removed a prim flag
        ///     param is an object[], with o[0] a ISceneChildEntity and o[1] the flag that was changed
        ///   SetAppearance - An avatar has updated their appearance
        ///     param is an object[], with o[0] the UUID of the avatar and o[1] the AvatarData that is to be updated
        ///   GridRegionSuccessfullyRegistered - Aurora.Server, A region has registered with the grid service
        ///     param is an object[], with o[0] the OSDMap which will be sent to the new region, o[1] the SessionID, o[2] the GridRegion that registered
        ///   Backup - The 'backup' console command was triggered, everything should backup
        ///     no params
        ///   AssetRequested - This is fired when the asset is requested by the client
        ///     params are IClientAPI and TransferRequestPacket
        ///   DeleteUserInformation - The user is being deleted, remove all of their information from all databases
        ///     params are the user's UUID
        ///   CreateUserInformation - The user account is being created
        ///     params are the user's UUID
        ///   UpdateUserInformation - The user account is being updated
        ///     params are the user's UUID
        /// </summary>
        public void RegisterEventHandler(string functionName, OnGenericEventHandler handler)
        {
            lock (m_events)
            {
                if (!m_events.ContainsKey(functionName))
                    m_events.Add(functionName, new List<OnGenericEventHandler>());
                m_events[functionName].Add(handler);
            }
        }

        public void UnregisterEventHandler(string functionName, OnGenericEventHandler handler)
        {
            lock (m_events)
            {
                if (!m_events.ContainsKey(functionName))
                    return;
                m_events[functionName].Remove(handler);
            }
        }

        /// <summary>
        ///   Fire a generic event for all modules hooking onto it
        /// </summary>
        /// <param name = "FunctionName">Name of event to trigger</param>
        /// <param name = "Param">Any parameters to pass along with the event</param>
        public List<object> FireGenericEventHandler(string FunctionName, object Param)
        {
            List<object> retVal = new List<object>();
            lock (m_events)
            {
                //If not null, fire for all
                List<OnGenericEventHandler> events;
                if (m_events.TryGetValue(FunctionName, out events))
                {
#if (!ISWIN)
                    foreach (OnGenericEventHandler handler in new List<OnGenericEventHandler>(events))
                    {
                        object param = handler(FunctionName, Param);
                        if (param != null)
                            retVal.Add(param);
                    }
#else
                    retVal.AddRange(new List<OnGenericEventHandler>(events).Select(handler => handler(FunctionName, Param)).Where(param => param != null));
#endif
                }
            }
            return retVal;
        }
    }
}