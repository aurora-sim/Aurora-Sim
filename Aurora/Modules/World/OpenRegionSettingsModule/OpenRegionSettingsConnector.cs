using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.DataManager;
using Aurora.Framework;
using OpenSim.Framework;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Region.Framework.Interfaces;

namespace Aurora.Modules
{
    public class OpenRegionSettingsConnector : IOpenRegionSettingsConnector
    {
        #region IAuroraDataPlugin Members

        public string Name
        {
            get { return "IOpenRegionSettingsConnector"; }
        }

        public void Initialize(IGenericData GenericData, Nini.Config.IConfigSource source, IRegistryCore simBase, string DefaultConnectionString)
        {
            Aurora.DataManager.DataManager.RegisterPlugin(Name, this);
        }

        #endregion

        #region IOpenRegionSettingsConnector Members

        public OpenRegionSettings GetSettings(UUID regionID)
        {
            OpenRegionSettings settings = new OpenRegionSettings();
            IGenericsConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();

            if (connector != null)
            {
                settings = connector.GetGeneric<OpenRegionSettings>(regionID, "OpenRegionSettings", "OpenRegionSettings", new OpenRegionSettings());
                if (settings == null)
                    settings = new OpenRegionSettings();
            }
            return settings;
        }

        public void SetSettings(UUID regionID, OpenRegionSettings settings)
        {
            IGenericsConnector connector = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();

            //Update the database
            if (connector != null)
                connector.AddGeneric(regionID, "OpenRegionSettings", "OpenRegionSettings", settings.ToOSD());
        }

        #endregion
    }
}
