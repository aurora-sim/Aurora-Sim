using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenSim.Services.Interfaces
{
    /// <summary>
    /// This gets the HTTP based Map Service set up
    /// </summary>
    public interface IMapService
    {
        /// <summary>
        /// Get the URL to the HTTP based Map Service
        /// </summary>
        /// <returns></returns>
        string GetURLOfMap ();
    }
}
