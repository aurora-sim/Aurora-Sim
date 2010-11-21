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
    public delegate void OnGenericEventHandler(string FunctionName, object parameters);
    /// <summary>
    /// A generic event manager that fires one event for many generic events
    /// </summary>
    public class AuroraEventManager
    {
        public event OnGenericEventHandler OnGenericEvent;
        /// <summary>
        /// Fire a generic event for all modules hooking onto it
        /// </summary>
        /// <param name="FunctionName">Name of event to trigger</param>
        /// <param name="Param">Any parameters to pass along with the event</param>
        public void FireGenericEventHandler(string FunctionName, object Param)
        {
            //If not null, fire for all
            if (OnGenericEvent != null)
            {
                OnGenericEvent(FunctionName, Param);
            }
        }
    }
}
