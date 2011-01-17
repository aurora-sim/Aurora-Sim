using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using Nini.Config;
using OpenSim.Framework;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService
{
    public class LocalMuteListConnector : IMuteListConnector
    {
        IGenericData GD;

        public void Initialize(IGenericData GenericData, ISimulationBase simBase, string defaultConnectionString)
        {
            IConfigSource source = simBase.ConfigSource;
            if (source.Configs["AuroraConnectors"].GetString("MuteListConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                if (source.Configs[Name] != null)
                    defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(defaultConnectionString);

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
            else
            {
                //Check to make sure that something else exists
                string m_ServerURI = simBase.ApplicationRegistry.RequestModuleInterface<IAutoConfigurationService>().FindValueOf("RemoteServerURI", "AuroraData");
                if (m_ServerURI == "") //Blank, not set up
                {
                    OpenSim.Framework.Console.MainConsole.Instance.Output("[AuroraDataService]: Falling back on local connector for " + "MuteListConnector", "None");
                    GD = GenericData;

                    if (source.Configs[Name] != null)
                        defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                    GD.ConnectToDatabase(defaultConnectionString);

                    DataManager.DataManager.RegisterPlugin(Name, this);
                }
            }
        }

        public string Name
        {
            get { return "IMuteListConnector"; }
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Gets the full mute list for the given agent.
        /// </summary>
        /// <param name="AgentID"></param>
        /// <returns></returns>
        public MuteList[] GetMuteList(UUID AgentID)
        {
            return GenericUtils.GetGenerics<MuteList>(AgentID, "MuteList", GD, new MuteList()).ToArray();
        }

        /// <summary>
        /// Updates or adds a mute for the given agent
        /// </summary>
        /// <param name="mute"></param>
        /// <param name="AgentID"></param>
        public void UpdateMute(MuteList mute, UUID AgentID)
        {
            GenericUtils.AddGeneric(AgentID, "MuteList", mute.MuteID.ToString(), mute.ToOSD(), GD);
        }

        /// <summary>
        /// Deletes a mute for the given agent
        /// </summary>
        /// <param name="muteID"></param>
        /// <param name="AgentID"></param>
        public void DeleteMute(UUID muteID, UUID AgentID)
        {
            GenericUtils.RemoveGeneric(AgentID, "MuteList", muteID.ToString(), GD);
        }

        /// <summary>
        /// Checks to see if PossibleMuteID is muted by AgentID
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="PossibleMuteID"></param>
        /// <returns></returns>
        public bool IsMuted(UUID AgentID, UUID PossibleMuteID)
        {
            return GenericUtils.GetGeneric<MuteList>(AgentID, "MuteList", PossibleMuteID.ToString(), GD, new MuteList()) != null;
        }
    }
}
