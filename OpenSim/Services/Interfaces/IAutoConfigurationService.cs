using System;
using System.Collections.Generic;
using System.Text;

namespace OpenSim.Services.Interfaces
{
    /// <summary>
    /// This service helps to automate the remote grid mode for users so that they do not have to input so many URLs
    /// </summary>
    public interface IAutoConfigurationService
    {
        /// <summary>
        /// Find the 'key' from the auto config service.
        /// First it will check for the key in the response it got from the AutoConfigurationInHandler, 
        ///   then check from the IConfig found by configurationSource.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="configurationSource"></param>
        /// <returns></returns>
        string FindValueOf(string key, string configurationSource);
    }
}
