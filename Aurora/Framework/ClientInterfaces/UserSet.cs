/*
 * Copyright (c) Contributors, http://aurora-sim.org/
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

using System;
using Nini.Config;

namespace Aurora.Framework
{
    /// <value>
    ///     Different user set names that come in from the configuration file.
    /// </value>
    public enum UserSet
    {
        All,
        Administrators,
        ParcelOwners,
        None
    };

    public class UserSetHelpers
    {
        /// <summary>
        ///     Parse a user set configuration setting
        /// </summary>
        /// <param name="config"></param>
        /// <param name="settingName"></param>
        /// <param name="defaultValue">The default value for this attribute</param>
        /// <returns>The parsed value</returns>
        public static UserSet ParseUserSetConfigSetting(IConfig config, string settingName, UserSet defaultValue)
        {
            UserSet userSet = defaultValue;

            string rawSetting = config.GetString(settingName, defaultValue.ToString());

            // Temporary measure to allow 'gods' to be specified in config for consistency's sake.  In the long term
            // this should disappear.
            if ("gods" == rawSetting.ToLower())
                rawSetting = UserSet.Administrators.ToString();

            // Doing it this was so that we can do a case insensitive conversion
            try
            {
                userSet = (UserSet) Enum.Parse(typeof (UserSet), rawSetting, true);
            }
            catch
            {
            }

            //MainConsole.Instance.DebugFormat("[PERMISSIONS]: {0} {1}", settingName, userSet);

            return userSet;
        }
    }
}