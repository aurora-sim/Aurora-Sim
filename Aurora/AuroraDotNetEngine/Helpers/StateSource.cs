using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public enum StateSource
    {
        NewRez = 0,
        PrimCrossing = 1,
        ScriptedRez = 2,
        AttachedRez = 3
    }

    public enum ThreatLevel
    {
        None = 0,
        Nuisance = 1,
        VeryLow = 2,
        Low = 3,
        Moderate = 4,
        High = 5,
        VeryHigh = 6,
        Severe = 7
    };
}
