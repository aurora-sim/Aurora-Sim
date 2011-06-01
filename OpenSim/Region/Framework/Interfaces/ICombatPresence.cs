using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface ICombatPresence
    {
        string Team { get; set; }
        void LeaveCombat();
        void JoinCombat();
        List<UUID> GetTeammates();

        void IncurDamage (IScenePresence killingAvatar, double damage);
        void IncurDamage (IScenePresence killingAvatar, double damage, string RegionName, Vector3 pos, Vector3 lookat);
        void IncurHealing(double healing);

        float Health { get; set; }
        bool HasLeftCombat { get; set; }

        void SetStat(string StatName, float statValue);
    }

    public interface ICombatModule
    {
        void AddCombatPermission(UUID AgentID);
        bool CheckCombatPermission(UUID AgentID);
        List<UUID> GetTeammates(string Team);
    }
}
