using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;

namespace Aurora.Services.DataService
{
	public class LocalCurrencyConnector : ICurrencyConnector
	{
		private IGenericData GD = null;
        public LocalCurrencyConnector()
		{
			GD = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
		}

		public IUserCurrency GetUserCurrency(UUID agentID)
		{
            IUserCurrency user = new IUserCurrency();
            List<string> query = GD.Query("PrincipalID", agentID, "usercurrency", "*");

			if (query.Count == 0)
				//Couldn't find it, return null then.
				return null;

            user.PrincipalID = new UUID(query[0]);
            user.Amount = uint.Parse(query[1]);
            user.LandInUse = uint.Parse(query[2]);
            user.Tier = uint.Parse(query[3]);

            return user;
		}

        public void UpdateUserCurrency(IUserCurrency agent)
		{
			List<object> SetValues = new List<object>();
			List<string> SetRows = new List<string>();
            SetRows.Add("Amount");
            SetRows.Add("LandInUse");
			SetRows.Add("Tier");
            SetValues.Add(agent.Amount);
			SetValues.Add(agent.LandInUse);
			SetValues.Add(agent.Tier);
			List<object> KeyValue = new List<object>();
			List<string> KeyRow = new List<string>();
			KeyRow.Add("PrincipalID");
			KeyValue.Add(agent.PrincipalID);
            GD.Update("usercurrency", SetValues.ToArray(), SetRows.ToArray(), KeyRow.ToArray(), KeyValue.ToArray());
		}

        public void CreateUserCurrency(UUID agentID)
		{
			List<object> values = new List<object>();
			values.Add(agentID.ToString());
			values.Add(0);
			values.Add(0);
			values.Add(0);
			GD.Insert("usercurrency", values.ToArray());
		}
	}
}
