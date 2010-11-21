using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Framework
{
    public interface IAssetConnector : IAuroraDataPlugin
	{
        /// <summary>
        /// Adds data from the AA commands to the database to be saved.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void UpdateLSLData(string token, string key, string value);

        /// <summary>
        /// Finds previously saved AA data. 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        List<string> FindLSLData(string token, string key);
    }
}
