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
        void AddNewUser(string userID, OSDMap urls);

        /// <summary>
        /// Get all of the url's for this service
        /// </summary>
        /// <returns></returns>
        OSDMap GetValues();

        /// <summary>
        /// Get the default url's for this service
        /// </summary>
        /// <returns></returns>
        OSDMap GetValuesFor(string key);

        /// <summary>
        /// Add a list of new Urls to the default Urls
        /// </summary>
        /// <param name="key">Used to pull the Urls out later with GetValuesFor if needed</param>
        /// <param name="urls"></param>
        void AddNewUrls(string key, OSDMap urls);

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
        List<string> FindValueOf(string userID, string key);

        /// <summary>
        /// Find the 'key' from the auto config service.
        /// First it will check for the key in the response it got from the ConfigurationInHandler, 
        ///   then check from the IConfig found in 'Configuration'.
        /// </summary>
        /// <param name="userID">The user who is requesting this info</param>
        /// <param name="userID">The another way to check who is requesting this info</param>
        /// <param name="key">A generic key to check for URLs</param>
        /// <returns>A list of URLs that are registered for this key</returns>
        List<string> FindValueOf(string userID, string user2, string key);

        /// <summary>
        /// Remove the URLs that we have for this key
        /// </summary>
        /// <param name="key"></param>
        void RemoveUrls(string key);
    }
}
