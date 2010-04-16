using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework;
using OpenSim.Framework;

namespace Aurora.DataManager.SQLite
{
    public class SQLiteEstate : SQLiteLoader, IEstateData
    {
        #region IEstateData Members

        public OpenSim.Framework.EstateSettings LoadEstateSettings(OpenMetaverse.UUID regionID, bool create)
        {
            string sql = "select * from estate_map left join estate_settings on estate_map.EstateID = estate_settings.EstateID where estate_settings.EstateID is not null and RegionID = ?RegionID";
            List<string> results = Query(sql);
            EstateSettings settings = new EstateSettings();
            return settings;
        }

        public OpenSim.Framework.EstateSettings LoadEstateSettings(int estateID)
        {
            string sql = "select * from estate_settings where EstateID = " + estateID.ToString();
            List<string> results = Query(sql);
            EstateSettings settings = new EstateSettings();
            return settings;
        }

        public void StoreEstateSettings(OpenSim.Framework.EstateSettings es)
        {
            throw new NotImplementedException();
        }

        public List<int> GetEstates(string search)
        {
            throw new NotImplementedException();
        }

        public bool LinkRegion(OpenMetaverse.UUID regionID, int estateID)
        {
            throw new NotImplementedException();
        }

        public List<OpenMetaverse.UUID> GetRegions(int estateID)
        {
            throw new NotImplementedException();
        }

        public bool DeleteEstate(int estateID)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
