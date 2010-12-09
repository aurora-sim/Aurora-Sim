using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using Nini.Config;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface ISharedRegionStartupModule
    {
        void Initialise(Scene scene, IConfigSource source, IOpenSimBase openSimBase);
        void PostInitialise(Scene scene, IConfigSource source, IOpenSimBase openSimBase);
    }
}
