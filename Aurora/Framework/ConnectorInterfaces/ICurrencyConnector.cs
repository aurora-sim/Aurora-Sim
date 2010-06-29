using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.Framework
{
    public interface ICurrencyConnector
	{
        IUserCurrency GetUserCurrency(UUID agentID);
        void UpdateUserCurrency(IUserCurrency agent);
        void CreateUserCurrency(UUID agentID);
	}
}
