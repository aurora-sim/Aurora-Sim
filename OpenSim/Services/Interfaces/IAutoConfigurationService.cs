using System;
using System.Collections.Generic;
using System.Text;

namespace OpenSim.Services.Interfaces
{
    /// <summary>
    /// This service helps to automate the remote grid mode for users so that they do not have to input so many URLs
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Find the 'key' from the auto config service.
        /// First it will check for the key in the response it got from the ConfigurationInHandler, 
        ///   then check from the IConfig found in 'Configuration'.
        /// </summary>
        /// <param name="key">A generic key to check for URLs</param>
        /// <returns>A list of URLs that are registered for this key</returns>
        List<string> FindValueOf(string key);
    }
}
