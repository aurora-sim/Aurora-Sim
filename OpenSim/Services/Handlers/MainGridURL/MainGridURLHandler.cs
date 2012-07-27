using Aurora.Framework;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using Nini.Config;

namespace Aurora.Addon.HyperGrid
{
    public class MainGridURLModule : IService, IGridRegistrationUrlModule
    {
        public void Initialize (IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start (IConfigSource config, IRegistryCore registry)
        {
            registry.RequestModuleInterface<IGridRegistrationService> ().RegisterModule (this);
        }

        public void FinishedStartup ()
        {
        }

        public string UrlName
        {
            get
            {
                return "MainGridURL";
            }
        }

        public bool DoMultiplePorts { get { return false; } }

        public string GetUrlForRegisteringClient (string SessionID, uint port)
        {
            //We only return the gatekeeper URL so that regions know what it is
            return MainServer.Instance.ServerURI + "/";
        }

        public void AddExistingUrlForClient (string SessionID, string url, uint port)
        {
        }

        public void RemoveUrlForClient (string sessionID, string url, uint port)
        {
        }
    }
}
