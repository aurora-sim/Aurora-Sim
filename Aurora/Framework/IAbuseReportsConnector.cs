using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework;
using OpenMetaverse;

namespace Aurora.Framework
{
	public interface IAbuseReportsConnector
	{
		AbuseReport GetAbuseReport(int Number, string Password);
        void AddAbuseReport(AbuseReport report, string Password);
        void UpdateAbuseReport(AbuseReport report, string Password);
	}
}
