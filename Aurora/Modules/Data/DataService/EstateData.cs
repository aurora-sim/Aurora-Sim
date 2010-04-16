using System;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenSim.Framework;

namespace Aurora.Modules
{
    public class EstateData: IEstateData
    {
        #region IEstateData Members

        public EstateSettings LoadEstateSettings(OpenMetaverse.UUID regionID, bool create)
        {
            if (Aurora.DataManager.DataManager.DefaultEstatePlugin != null)
                return Aurora.DataManager.DataManager.DefaultEstatePlugin.LoadEstateSettings(regionID,create);
            else
            {
                foreach (IEstateData plugin in Aurora.DataManager.DataManager.AllEstatePlugins)
                {
                    EstateSettings ES = plugin.LoadEstateSettings(regionID,create);
                    if (ES != null)
                        return ES;
                }
            }
            return null;
        }

        public EstateSettings LoadEstateSettings(int estateID)
        {
            if (Aurora.DataManager.DataManager.DefaultEstatePlugin != null)
                return Aurora.DataManager.DataManager.DefaultEstatePlugin.LoadEstateSettings(estateID);
            else
            {
                foreach (IEstateData plugin in Aurora.DataManager.DataManager.AllEstatePlugins)
                {
                    EstateSettings ES = plugin.LoadEstateSettings(estateID);
                    if (ES != null)
                        return ES;
                }
            }
            return null;
        }

        public bool StoreEstateSettings(EstateSettings es)
        {
            if (Aurora.DataManager.DataManager.DefaultEstatePlugin != null)
                Aurora.DataManager.DataManager.DefaultEstatePlugin.StoreEstateSettings(es);
            else
            {
                foreach (IEstateData plugin in Aurora.DataManager.DataManager.AllEstatePlugins)
                {
                    bool success = plugin.StoreEstateSettings(es);
                    if (success)
                        return true;
                }
            }
            return false;
        }

        public List<int> GetEstates(string search)
        {
            if (Aurora.DataManager.DataManager.DefaultEstatePlugin != null)
                return Aurora.DataManager.DataManager.DefaultEstatePlugin.GetEstates(search);
            else
            {
                foreach (IEstateData plugin in Aurora.DataManager.DataManager.AllEstatePlugins)
                {
                    List<int> success = plugin.GetEstates(search);
                    if (success.Count != 0)
                        return success;
                }
            }
            return new List<int>();
        }

        public bool LinkRegion(OpenMetaverse.UUID regionID, int estateID)
        {
            throw new NotImplementedException();
        }

        public List<OpenMetaverse.UUID> GetRegions(int estateID)
        {
            if (Aurora.DataManager.DataManager.DefaultEstatePlugin != null)
                return Aurora.DataManager.DataManager.DefaultEstatePlugin.GetRegions(estateID);
            else
            {
                foreach (IEstateData plugin in Aurora.DataManager.DataManager.AllEstatePlugins)
                {
                    List<OpenMetaverse.UUID> success = plugin.GetRegions(estateID);
                    if (success.Count != 0)
                        return success;
                }
            }
            return new List<OpenMetaverse.UUID>();
        }

        public bool DeleteEstate(int estateID)
        {
            if (Aurora.DataManager.DataManager.DefaultEstatePlugin != null)
                return Aurora.DataManager.DataManager.DefaultEstatePlugin.DeleteEstate(estateID);
            else
            {
                foreach (IEstateData plugin in Aurora.DataManager.DataManager.AllEstatePlugins)
                {
                    return plugin.DeleteEstate(estateID);
                }
            }
            return false;
        }

        #endregion
    }
}
