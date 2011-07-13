using log4net;
using System;
using System.Net;
using System.Reflection;
using Nini.Config;
using OpenSim.Services.Interfaces;
using Aurora.Simulation.Base;

namespace OpenSim.Services
{
    public class HeloServiceConnector : IHeloServiceConnector, IService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        public virtual string Helo(string serverURI)
        {
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create (serverURI + "/helo");

            try
            {
                WebResponse response = req.GetResponse();
                if (response.Headers.Get("X-Handlers-Provided") == null) // just in case this ever returns a null
                    return string.Empty;
                return response.Headers.Get("X-Handlers-Provided");
            }
            catch (Exception e)
            {
                m_log.DebugFormat ("[HELO SERVICE]: Unable to perform HELO request to {0}: {1}", serverURI, e.Message);
            }

            // fail
            return string.Empty;
        }

        public void Initialize (IConfigSource config, Framework.IRegistryCore registry)
        {
            registry.RegisterModuleInterface<IHeloServiceConnector> (this);
        }

        public void Start (IConfigSource config, Framework.IRegistryCore registry)
        {
        }

        public void FinishedStartup ()
        {
        }
    }
}
