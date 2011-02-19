using System;
using System.Collections.Generic;
using System.Text;
using Nini.Config;
using OpenSim.Framework;

namespace OpenSim.Services.Interfaces
{
    public interface IDefaultLibraryLoader
    {
        /// <summary>
        /// Load any default inventory folders and items from this module into the main ILibraryService
        /// </summary>
        /// <param name="service"></param>
        /// <param name="source"></param>
        /// <param name="registry"></param>
        void LoadLibrary(ILibraryService service, IConfigSource source, IRegistryCore registry);
    }
}
