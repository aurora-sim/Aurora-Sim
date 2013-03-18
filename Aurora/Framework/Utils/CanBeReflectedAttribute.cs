using System;

namespace Aurora.Framework
{
    public class CanBeReflected : Attribute
    {
        public ThreatLevel ThreatLevel;
        public string RenamedMethod = "";
        public bool UsePassword = false;
        /// <summary>
        /// Used for helper methods, in which the method to call is not this method, but the next up the stack
        /// </summary>
        public bool NotReflectableLookUpAnotherTrace = false;
    }
}
