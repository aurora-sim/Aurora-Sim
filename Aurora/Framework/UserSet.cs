using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nini.Config;

namespace Aurora.Framework
{
    /// <value>
    /// Different user set names that come in from the configuration file.
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
        /// Parse a user set configuration setting
        /// </summary>
        /// <param name="config"></param>
        /// <param name="settingName"></param>
        /// <param name="defaultValue">The default value for this attribute</param>
        /// <returns>The parsed value</returns>
        public static UserSet ParseUserSetConfigSetting (IConfig config, string settingName, UserSet defaultValue)
        {
            UserSet userSet = defaultValue;

            string rawSetting = config.GetString (settingName, defaultValue.ToString ());

            // Temporary measure to allow 'gods' to be specified in config for consistency's sake.  In the long term
            // this should disappear.
            if ("gods" == rawSetting.ToLower ())
                rawSetting = UserSet.Administrators.ToString ();

            // Doing it this was so that we can do a case insensitive conversion
            try
            {
                userSet = (UserSet)Enum.Parse (typeof (UserSet), rawSetting, true);
            }
            catch
            {
            }

            //m_log.DebugFormat("[PERMISSIONS]: {0} {1}", settingName, userSet);

            return userSet;
        }
    }
}
