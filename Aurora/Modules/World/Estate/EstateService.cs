using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.Framework.Interfaces;
using Aurora.Framework;

namespace Aurora.Modules
{
    public class EstateService : ISharedRegionModule, IEstateService
    {
        IEstateData m_DataStore = null;
        public void PostInitialise()
        {
        }

        public string Name
        {
            get { return "EstateService"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(Nini.Config.IConfigSource source)
        {
        }

        public void Close()
        {
        }

        public void AddRegion(OpenSim.Region.Framework.Scenes.Scene scene)
        {
            scene.RegisterModuleInterface<IEstateService>(this);
        }

        public void RemoveRegion(OpenSim.Region.Framework.Scenes.Scene scene)
        {
        }

        public void RegionLoaded(OpenSim.Region.Framework.Scenes.Scene scene)
        {
        }

        public void Initialise(IEstateDataStore dataStore)
        {
            m_DataStore = Aurora.DataManager.DataManager.GetEstatePlugin();// dataStore;
        }

        public OpenSim.Framework.EstateSettings LoadEstateSettings(OpenMetaverse.UUID regionID, bool create)
        {
            return m_DataStore.LoadEstateSettings(regionID, create);
        }

        public OpenSim.Framework.EstateSettings LoadEstateSettings(int estateID)
        {
            return m_DataStore.LoadEstateSettings(estateID);
        }

        public void StoreEstateSettings(OpenSim.Framework.EstateSettings es)
        {
            m_DataStore.StoreEstateSettings(es);
        }

        public List<int> GetEstates(string search)
        {
            return m_DataStore.GetEstates(search);
        }

        public bool LinkRegion(OpenMetaverse.UUID regionID, int estateID)
        {
            return m_DataStore.LinkRegion(regionID, estateID);
        }

        public List<OpenMetaverse.UUID> GetRegions(int estateID)
        {
            return m_DataStore.GetRegions(estateID);
        }

        public bool DeleteEstate(int estateID)
        {
            return m_DataStore.DeleteEstate(estateID);
        }
    }
}
