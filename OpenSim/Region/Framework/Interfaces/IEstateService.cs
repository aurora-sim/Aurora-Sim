using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface IEstateService
    {
        //Gets called before it is requested for the first time.
        void Initialise(IEstateDataStore dataStore);
        EstateSettings LoadEstateSettings(UUID regionID, bool create);
        EstateSettings LoadEstateSettings(int estateID);
        void StoreEstateSettings(EstateSettings es);
        List<int> GetEstates(string search);
        bool LinkRegion(UUID regionID, int estateID);
        List<UUID> GetRegions(int estateID);
        bool DeleteEstate(int estateID);
    }
}
