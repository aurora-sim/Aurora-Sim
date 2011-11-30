/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System.Collections.Generic;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.Interfaces
{
    /// <summary>
    ///   This service helps to automate the remote grid mode for users so that they do not have to input so many URLs
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        ///   Add a user with a list of Urls for them
        /// </summary>
        /// <param name = "userID"></param>
        /// <param name = "urls"></param>
        void AddNewUser(string userID, OSDMap urls);

        /// <summary>
        ///   Get all of the url's for this service
        /// </summary>
        /// <returns></returns>
        OSDMap GetValues();

        /// <summary>
        ///   Get the default url's for this service
        /// </summary>
        /// <returns></returns>
        OSDMap GetValuesFor(string key);

        /// <summary>
        ///   Add a list of new Urls to the default Urls
        /// </summary>
        /// <param name = "key">Used to pull the Urls out later with GetValuesFor if needed</param>
        /// <param name = "urls"></param>
        void AddNewUrls(string key, OSDMap urls);

        /// <summary>
        ///   Find the 'key' from the auto config service.
        ///   First it will check for the key in the response it got from the ConfigurationInHandler, 
        ///   then check from the IConfig found in 'Configuration'.
        /// </summary>
        /// <param name = "key">A generic key to check for URLs</param>
        /// <returns>A list of URLs that are registered for this key</returns>
        List<string> FindValueOf(string key);

        /// <summary>
        ///   Find the 'key' from the auto config service.
        ///   First it will check for the key in the response it got from the ConfigurationInHandler, 
        ///   then check from the IConfig found in 'Configuration'.
        /// </summary>
        /// <param name = "userID">The user who is requesting this info</param>
        /// <param name = "key">A generic key to check for URLs</param>
        /// <returns>A list of URLs that are registered for this key</returns>
        List<string> FindValueOf(string userID, string key);

        /// <summary>
        ///   Find the 'key' from the auto config service.
        ///   First it will check for the key in the response it got from the ConfigurationInHandler, 
        ///   then check from the IConfig found in 'Configuration'.
        /// </summary>
        /// <param name = "userID">The user who is requesting this info</param>
        /// <param name = "key">A generic key to check for URLs</param>
        /// <param name = "returnAllPossible">Return all possible URLs that could be used for the user, not only the user's home (if it exists)</param>
        /// <returns>A list of URLs that are registered for this key</returns>
        List<string> FindValueOf(string userID, string key, bool returnAllPossible);

        /// <summary>
        ///   Find the 'key' from the auto config service.
        ///   First it will check for the key in the response it got from the ConfigurationInHandler, 
        ///   then check from the IConfig found in 'Configuration'.
        /// </summary>
        /// <param name = "userID">The user who is requesting this info</param>
        /// <param name = "userID">The another way to check who is requesting this info</param>
        /// <param name = "key">A generic key to check for URLs</param>
        /// <returns>A list of URLs that are registered for this key</returns>
        List<string> FindValueOf(string userID, string user2, string key);

        /// <summary>
        ///   Remove the URLs that we have for this key
        /// </summary>
        /// <param name = "key"></param>
        void RemoveUrls(string key);
    }
}