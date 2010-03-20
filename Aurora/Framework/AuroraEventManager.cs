using System;
using System.Collections.Generic;
using System.Text;

namespace Aurora.Framework
{
    public delegate void OnGenericEventHandler(string FunctionName, object parameters);
    public class AuroraEventManager
    {
        public event OnGenericEventHandler OnGenericEvent;
        public void FireGenericEventHandler(string FunctionName, object Param)
        {
            if (OnGenericEvent != null)
            {
                OnGenericEvent(FunctionName, Param);
            }
        }
    }
}
