using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using Nini.Config;

namespace Aurora.Services.DataService
{
    public class LocalMuteListConnector : IMuteListConnector, IAuroraDataPlugin
    {
        IGenericData GD;

        public void Initialize(IGenericData GenericData, IConfigSource source, string defaultConnectionString)
        {
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
                string m_ServerURI = source.Configs["AuroraData"].GetString("RemoteServerURI", "");
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

        public MuteList[] GetMuteList(UUID AgentID)
        {
            return GenericUtils.GetGenerics<MuteList>(AgentID, "MuteList", GD, new MuteList()).ToArray();
        }

        public void UpdateMute(MuteList mute, UUID AgentID)
        {
            GenericUtils.AddGeneric(AgentID, "MuteList", mute.MuteID.ToString(), mute.ToOSD(), GD);
        }

        public void DeleteMute(UUID muteID, UUID AgentID)
        {
            GenericUtils.RemoveGeneric(AgentID, "MuteList", muteID.ToString(), GD);
        }

        public bool IsMuted(UUID AgentID, UUID PossibleMuteID)
        {
            return GenericUtils.GetGeneric<MuteList>(AgentID, "MuteList", PossibleMuteID.ToString(), GD, new MuteList()) != null;
        }
    }
}
