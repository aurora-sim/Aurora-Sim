using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Framework;
using OpenMetaverse;

namespace Aurora.Services.DataService
{
	public class LocalRegionInfoConnector : IRegionInfoConnector
	{
		private IGenericData GD = null;
        public LocalRegionInfoConnector()
		{
			GD = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
		}

		public RegionInfo[] GetRegionInfos()
		{
            List<RegionInfo> Infos = new List<RegionInfo>();
            List<string> RetVal = GD.Query("Disabled", false, "Simulator", "*");
            int DataCount = 0;
            RegionInfo replyData = new RegionInfo();
            for (int i = 0; i < RetVal.Count; i++)
            {
                if (DataCount == 0)
                    replyData.RegionID = new UUID(RetVal[i]);
                if (DataCount == 1)
                    replyData.RegionName =RetVal[i];
                if (DataCount == 2)
                    replyData.RegionLocX = uint.Parse(RetVal[i]);
                if (DataCount == 3)
                    replyData.RegionLocY = uint.Parse(RetVal[i]);
                if (DataCount == 6)
                    replyData.ExternalHostName = RetVal[i];
                if (DataCount == 7)
                    replyData.RegionType = RetVal[i];
                if (DataCount == 8)
                    replyData.NonphysPrimMax = Convert.ToInt32(RetVal[i]);
                if (DataCount == 9)
                    replyData.PhysPrimMax = Convert.ToInt32(RetVal[i]);
                if (DataCount == 10)
                    replyData.ClampPrimSize = Convert.ToBoolean(RetVal[i]);
                if (DataCount == 11)
                    replyData.ObjectCapacity = Convert.ToInt32(RetVal[i]);
                if (DataCount == 15)
                    replyData.AccessLevel = Convert.ToByte(RetVal[i]);
                DataCount++;
                if (DataCount == 16)
                {
                    replyData.SetEndPoint(RetVal[(i - DataCount) + 4], int.Parse(RetVal[(i - DataCount) + 5]));
                    DataCount = 0;
                    Infos.Add(replyData);
                    replyData = new RegionInfo();
                }
            }
            return Infos.ToArray();
		}
	}
}
