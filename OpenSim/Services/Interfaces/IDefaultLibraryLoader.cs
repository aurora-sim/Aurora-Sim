using System;
using System.Collections.Generic;
using System.Text;
using Nini.Config;

namespace OpenSim.Services.Interfaces
{
    public interface IDefaultLibraryLoader
    {
        void LoadLibrary(ILibraryService service, IConfigSource source);
    }
}
