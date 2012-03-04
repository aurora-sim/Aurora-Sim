using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Services.Interfaces;

namespace Aurora.Framework
{
    public class CanBeReflected : Attribute
    {
        public ThreatLevel ThreatLevel;
        public string RenamedMethod = "";
    }
}
