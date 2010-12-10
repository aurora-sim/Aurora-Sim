using System;
using System.Collections.Generic;
using System.Text;

namespace OpenSim.Services.Interfaces
{
    public interface IAutoConfigurationService
    {
        string FindValueOf(string key, string configurationSource);
    }
}
