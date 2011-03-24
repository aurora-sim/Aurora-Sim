using System;
using System.Collections.Generic;
using System.Text;

namespace Aurora.Framework
{
    /// <summary>
    /// Delegate to fire when a generic event comes in
    /// </summary>
    /// <param name="FunctionName">Name of the event being fired</param>
    /// <param name="parameters">Parameters that the event has, can be null</param>
    public delegate object OnGenericEventHandler(string FunctionName, object parameters);
    /// <summary>
    /// A generic event manager that fires one event for many generic events
    /// </summary>
    public class AuroraEventManager
    {
        /// <summary>
        /// Events so far:
        /// 
        /// DrawDistanceChanged - Changed Draw Distance
        ///      param is a ScenePresence
        /// BanUser - Added a new banned user to the estate bans
        ///      param is a UUID of an agent
        /// UnBanUser - Removed a banned user from the estate bans
        ///      param is a UUID of an agent
        /// SignficantCameraMovement - The Camera has moved a distance that has triggered this update
        ///      param is a ScenePresence
        /// ObjectSelected - An object has been selected
        ///      param is a SceneObjectPart
        /// ObjectDeselected - An object has been selected
        ///      param is a SceneObjectPart
        /// ObjectChangedOwner - An object's owner was changed
        ///      param is a SceneObjectGroup
        /// ObjectChangedPhysicalStatus - An object's physical status has changed
        ///      param is a SceneObjectGroup
        /// ObjectEnteringNewParcel - An object has entered a new parcel
        ///      param is an object[], with o[0] a SceneObjectGroup, o[1] the new parcel UUID, and o[2] the old parcel UUID
        /// RegionRegistered - New Region has been registered
        ///      param is a GridRegion
        /// UserStatusChange - User's status (logged in/out) has changed
        ///      param is a UserInfo
        /// PreRegisterRegion - A region is about to be registered
        ///      param is a GridRegion
        /// NewUserConnection - A new user has been added to the scene (child or root)
        ///      param is the OSDMap that will be returned to the server
        /// EstateUpdated - An estate has been updated
        ///      param is the EstateSettings of the changed estate
        /// GridRegionRegistered - New Region has been registered with remote grid service
        ///      param is an object[], with o[0] a GridRegion, o[1] the OSDMap response from the server
        /// ObjectAddedFlag - An object in the Scene has added a prim flag
        ///      param is an object[], with o[0] ISceneChildEntity and o[1] the flag that was changed
        /// ObjectRemovedFlag - An object in the Scene has removed a prim flag
        ///      param is an object[], with o[0] ISceneChildEntity and o[1] the flag that was changed
        /// 
        /// </summary>
        public event OnGenericEventHandler OnGenericEvent;
        /// <summary>
        /// Fire a generic event for all modules hooking onto it
        /// </summary>
        /// <param name="FunctionName">Name of event to trigger</param>
        /// <param name="Param">Any parameters to pass along with the event</param>
        public List<object> FireGenericEventHandler(string FunctionName, object Param)
        {
            List<object> retVal = new List<object>();
            //If not null, fire for all
            if (OnGenericEvent != null)
            {
                foreach(OnGenericEventHandler handler in OnGenericEvent.GetInvocationList())
                {
                    object param = handler(FunctionName, Param);
                    if (param != null)
                        retVal.Add(param);
                }
            }
            return retVal;
        }
    }
}
