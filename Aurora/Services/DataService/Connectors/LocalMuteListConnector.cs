using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;

namespace Aurora.Services.DataService
{
    public class LocalMuteListConnector : IMuteListConnector
    {
        IGenericData GD;
        public LocalMuteListConnector()
        {
            GD = Aurora.DataManager.DataManager.DefaultGenericPlugin;
        }

        public MuteList[] GetMuteList(UUID AgentID)
        {
            List<MuteList> Mutes = new List<MuteList>();
            List<string> muteListName = GD.Query("PrincipalID", AgentID, "mutelists", "MutedName");
            List<string> muteListType = GD.Query("PrincipalID", AgentID, "mutelists", "MuteType");
            List<string> muteListID = GD.Query("PrincipalID", AgentID, "mutelists", "MutedID");
            int i = 0;
            foreach (string A in muteListID)
            {
                MuteList mute = new MuteList();
                mute.MuteName = muteListName[i];
                mute.MuteType = muteListType[i];
                mute.MuteID = UUID.Parse(muteListID[i]);
                Mutes.Add(mute);
            }
            return Mutes.ToArray();
        }

        public void UpdateMute(MuteList mute, UUID AgentID)
        {
            List<object> values = new List<object>();
            values.Add(AgentID);
            values.Add(mute.MuteID);
            values.Add(mute.MuteName);
            values.Add(mute.MuteType);
            GD.Insert("mutelists", values.ToArray(), "MuteType", mute.MuteType);
        }

        public void DeleteMute(UUID muteID, UUID AgentID)
        {
            List<string> keys = new List<string>();
            List<object> values = new List<object>();
            keys.Add("PrincipalID");
            keys.Add("MutedID");
            values.Add(AgentID);
            values.Add(muteID);
            GD.Delete("mutelists", keys.ToArray(), values.ToArray());
        }

        public bool IsMuted(UUID AgentID, UUID PossibleMuteID)
        {
            List<string> check = GD.Query("PrincipalID", AgentID, "mutelists", "MutedID");
            if (check.Count == 0)
                return false;
            else
                return true;
        }
    }
}
