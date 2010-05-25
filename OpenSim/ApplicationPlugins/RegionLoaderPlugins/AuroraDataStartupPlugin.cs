using System;
using System.Collections.Generic;
using System.Text;

namespace OpenSim.ApplicationPlugins.RegionLoaderPlugin
{
    public class AuroraDataStartupPlugin : IApplicationPlugin
    {
        OpenSim.Framework.IOpenSimBase OpenSimBase;
        public void Initialise(OpenSim.Framework.IOpenSimBase openSim)
        {
            OpenSimBase = openSim;
            Aurora.Services.DataService.LocalDataService service = new Aurora.Services.DataService.LocalDataService();
            service.Initialise(openSim.ConfigSource);
        }

        public void PostInitialise()
        {
        }

        public string Name
        {
            get { return "AuroraDataStartupPlugin"; }
        }

        public void Dispose()
        {
        }
    }
}
