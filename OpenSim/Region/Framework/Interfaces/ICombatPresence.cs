using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface ICombatPresence
    {
        string Team { get; set; }
        void LeaveCombat();
        void JoinCombat();
        List<UUID> GetTeammates();

        void IncurDamage(uint localID, double damage, UUID OwnerID);
        void IncurDamage(uint localID, double damage, string RegionName, Vector3 pos, Vector3 lookat, UUID OwnerID);
        void IncurHealing(double healing, UUID OwnerID);

        float Health { get; set; }
        bool HasLeftCombat { get; set; }

        void SetStat(string StatName, float statValue);
    }

    public interface ICombatModule
    {
        void AddCombatPermission(UUID AgentID);
        bool CheckCombatPermission(UUID AgentID);
        void RegisterToAvatarDeathEvents(UUID primID);

        void DeregisterFromAvatarDeathEvents(UUID primID);
        List<UUID> GetTeammates(string Team);
    }
}
