using System;
using System.Collections.Generic;
using System.Text;
using Aurora.DataManager;
using Aurora.Framework;

namespace Aurora.Services.DataService
{
    public class RegionData: IRegionData
    {
        #region IRegionData Members

        public string AbuseReports()
        {
            if (Aurora.DataManager.DataManager.DefaultRegionPlugin != null)
                return Aurora.DataManager.DataManager.DefaultRegionPlugin.AbuseReports();
            else
            {
                foreach (IRegionData plugin in Aurora.DataManager.DataManager.AllRegionPlugins)
                {
                    string success = plugin.AbuseReports();
                    if (success != null && success != "")
                        return success;
                }
            }
            return "";
        }

        public ObjectMediaURLInfo getObjectMediaInfo(string objectID, int side)
        {
            if (Aurora.DataManager.DataManager.DefaultRegionPlugin != null)
                return Aurora.DataManager.DataManager.DefaultRegionPlugin.getObjectMediaInfo(objectID, side);
            else
            {
                foreach (IRegionData plugin in Aurora.DataManager.DataManager.AllRegionPlugins)
                {
                    ObjectMediaURLInfo info = plugin.getObjectMediaInfo(objectID, side);
                    if (info != null)
                        return info;
                }
            }
            return null;
        }

        public bool StoreRegionWindlightSettings(OpenSim.Framework.RegionLightShareData wl)
        {
            if (Aurora.DataManager.DataManager.DefaultRegionPlugin != null)
                Aurora.DataManager.DataManager.DefaultRegionPlugin.StoreRegionWindlightSettings(wl);
            else
            {
                foreach (IRegionData plugin in Aurora.DataManager.DataManager.AllRegionPlugins)
                {
                    bool success = plugin.StoreRegionWindlightSettings(wl);
                    if (success)
                        return true;
                }
            }
            return false;
        }

        public OpenSim.Framework.RegionLightShareData LoadRegionWindlightSettings(OpenMetaverse.UUID regionUUID)
        {
            if (Aurora.DataManager.DataManager.DefaultRegionPlugin != null)
                return Aurora.DataManager.DataManager.DefaultRegionPlugin.LoadRegionWindlightSettings(regionUUID);
            else
            {
                foreach (IRegionData plugin in Aurora.DataManager.DataManager.AllRegionPlugins)
                {
                    OpenSim.Framework.RegionLightShareData LSD = plugin.LoadRegionWindlightSettings(regionUUID);
                    if (LSD != null)
                        return LSD;
                }
            }
            return null;
        }

        public AbuseReport GetAbuseReport(int formNumber)
        {
            if (Aurora.DataManager.DataManager.DefaultRegionPlugin != null)
                return Aurora.DataManager.DataManager.DefaultRegionPlugin.GetAbuseReport(formNumber);
            else
            {
                foreach (IRegionData plugin in Aurora.DataManager.DataManager.AllRegionPlugins)
                {
                    AbuseReport report = plugin.GetAbuseReport(formNumber);
                    if (report != null)
                        return report;
                }
            }
            return null;
        }

        public void AddLandObject(OpenSim.Framework.LandData ILandData)
        {
            if (Aurora.DataManager.DataManager.DefaultRegionPlugin != null)
                Aurora.DataManager.DataManager.DefaultRegionPlugin.AddLandObject(ILandData);
            else
            {
                foreach (IRegionData plugin in Aurora.DataManager.DataManager.AllRegionPlugins)
                {
                    plugin.AddLandObject(ILandData);
                }
            }
        }

        public OfflineMessage[] GetOfflineMessages(string agentID)
        {
            if (Aurora.DataManager.DataManager.DefaultRegionPlugin != null)
                return Aurora.DataManager.DataManager.DefaultRegionPlugin.GetOfflineMessages(agentID);
            else
            {
                foreach (IRegionData plugin in Aurora.DataManager.DataManager.AllRegionPlugins)
                {
                    OfflineMessage[] messages = plugin.GetOfflineMessages(agentID);
                    if (messages.Length != 0)
                        return messages;
                }
            }
            return new List<OfflineMessage>().ToArray();
        }

        public bool AddOfflineMessage(string fromUUID, string fromName, string toUUID, string message)
        {
            if (Aurora.DataManager.DataManager.DefaultRegionPlugin != null)
                Aurora.DataManager.DataManager.DefaultRegionPlugin.AddOfflineMessage(fromUUID, fromName, toUUID, message);
            else
            {
                foreach (IRegionData plugin in Aurora.DataManager.DataManager.AllRegionPlugins)
                {
                    bool success = plugin.AddOfflineMessage(fromUUID,fromName,toUUID,message);
                    if (success)
                        return true;
                }
            }
            return false;
        }

        #endregion
    }
}
