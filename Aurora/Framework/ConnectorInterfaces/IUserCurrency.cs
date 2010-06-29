using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;

namespace Aurora.Framework
{
    public class IUserCurrency
    {
        public UUID PrincipalID;
        public uint Amount;
        public uint LandInUse;
        public uint Tier;
    }
}
