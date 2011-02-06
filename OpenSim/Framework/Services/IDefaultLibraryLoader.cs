using System;
using System.Collections.Generic;
using System.Text;
using Nini.Config;
using OpenSim.Framework;

namespace OpenSim.Services.Interfaces
{
    public interface IDefaultLibraryLoader
    {
        void LoadLibrary(ILibraryService service, IConfigSource source, IRegistryCore registry);
    }
}
