using System;
using System.Net;
using System.Reflection;
using Aurora.Simulation.Base;
using Nini.Config;
using Aurora.Framework;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services
{
    public class HeloServiceConnector : IHeloServiceConnector, IService
    {
        #region IHeloServiceConnector Members

        public virtual string Helo(string serverURI)
        {
            HttpWebRequest req = (HttpWebRequest) WebRequest.Create(serverURI + "/helo");

            try
            {
                WebResponse response = req.GetResponse();
                if (response.Headers.Get("X-Handlers-Provided") == null) // just in case this ever returns a null
                    return string.Empty;
                return response.Headers.Get("X-Handlers-Provided");
            }
            catch (Exception e)
            {
                MainConsole.Instance.DebugFormat("[HELO SERVICE]: Unable to perform HELO request to {0}: {1}", serverURI, e.Message);
            }

            // fail
            return string.Empty;
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            registry.RegisterModuleInterface<IHeloServiceConnector>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        #endregion
    }
}