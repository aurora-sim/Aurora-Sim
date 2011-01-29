using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.Interfaces
{
    /// <summary>
    /// This service helps to automate the remote grid mode for users so that they do not have to input so many URLs
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Add a user with a list of Urls for them
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="urls"></param>
        void AddNewUser(UUID userID, OSDMap urls);

        /// <summary>
        /// Get the default url's for this service
        /// </summary>
        /// <returns></returns>
        OSDMap GetDefaultValues();

        /// <summary>
        /// Find the 'key' from the auto config service.
        /// First it will check for the key in the response it got from the ConfigurationInHandler, 
        ///   then check from the IConfig found in 'Configuration'.
        /// </summary>
        /// <param name="key">A generic key to check for URLs</param>
        /// <returns>A list of URLs that are registered for this key</returns>
        List<string> FindValueOf(string key);

        /// <summary>
        /// Find the 'key' from the auto config service.
        /// First it will check for the key in the response it got from the ConfigurationInHandler, 
        ///   then check from the IConfig found in 'Configuration'.
        /// </summary>
        /// <param name="userID">The user who is requesting this info</param>
        /// <param name="key">A generic key to check for URLs</param>
        /// <returns>A list of URLs that are registered for this key</returns>
        List<string> FindValueOf(UUID userID, string key);
    }
}
