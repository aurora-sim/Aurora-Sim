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

        public Dictionary<string, string> GetRegionHidden()
        {
            if (Aurora.DataManager.DataManager.DefaultRegionPlugin != null)
                return Aurora.DataManager.DataManager.DefaultRegionPlugin.GetRegionHidden();
            else
            {
                foreach (IRegionData plugin in Aurora.DataManager.DataManager.AllRegionPlugins)
                {
                    Dictionary<string, string> success = plugin.GetRegionHidden();
                    if (success != null && success.Count != 0)
                        return success;
                }
            }
            return new Dictionary<string, string>();
        }

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

        public ObjectMediaURLInfo[] getObjectMediaInfo(string objectID)
        {
            if (Aurora.DataManager.DataManager.DefaultRegionPlugin != null)
                return Aurora.DataManager.DataManager.DefaultRegionPlugin.getObjectMediaInfo(objectID);
            else
            {
                foreach (IRegionData plugin in Aurora.DataManager.DataManager.AllRegionPlugins)
                {
                    ObjectMediaURLInfo[] info = plugin.getObjectMediaInfo(objectID);
                    if (info.Length != 0)
                        return info;
                }
            }
            return new List<ObjectMediaURLInfo>().ToArray();
        }

        public bool GetIsRegionMature(string region)
        {
            if (Aurora.DataManager.DataManager.DefaultRegionPlugin != null)
                return Aurora.DataManager.DataManager.DefaultRegionPlugin.GetIsRegionMature(region);
            else
            {
                foreach (IRegionData plugin in Aurora.DataManager.DataManager.AllRegionPlugins)
                {
                    return plugin.GetIsRegionMature(region);
                }
            }
            return false;
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
