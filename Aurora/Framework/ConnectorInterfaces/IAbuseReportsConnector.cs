using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework;
using OpenMetaverse;

namespace Aurora.Framework
{
    public interface IAbuseReportsConnector : IAuroraDataPlugin
	{
        /// <summary>
        /// Gets the abuse report associated with the number and uses the pass to authenticate.
        /// </summary>
        /// <param name="Number"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
		AbuseReport GetAbuseReport(int Number, string Password);

        /// <summary>
        /// Adds a new abuse report to the database
        /// </summary>
        /// <param name="report"></param>
        /// <param name="Password"></param>
        void AddAbuseReport(AbuseReport report);

        /// <summary>
        /// Updates an abuse report and authenticates with the password.
        /// </summary>
        /// <param name="report"></param>
        /// <param name="Password"></param>
        void UpdateAbuseReport(AbuseReport report, string Password);
	}
}
