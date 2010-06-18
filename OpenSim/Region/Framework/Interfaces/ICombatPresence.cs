using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface ICombatPresence
    {
        public string Team { get; set; }
        void KillAvatar(uint killerObjectLocalID);
    }
}
