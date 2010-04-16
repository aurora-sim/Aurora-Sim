using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.Framework.Interfaces;
using Aurora.Framework;
using OpenSim.Framework;
using OpenSim.Region.Framework;
using OpenMetaverse;

namespace Aurora.Modules
{
    public class EstateSettingsModule : IRegionModule, IEstateSettingsModule
    {
        Scene m_scene;
        IProfileData PD;

        public void Initialise(Scene scene, IConfigSource source)
        {
            scene.RegisterModuleInterface<IEstateSettingsModule>(this);
            m_scene = scene;
        }

        public void PostInitialise()
        {
            PD = Aurora.DataManager.DataManager.GetProfilePlugin();
        }

        public void Close() { }

        public string Name { get { return "EstateSettingsModule"; } }

        public bool IsSharedModule { get { return true; } }

        public bool AllowTeleport(IScene scene, UUID userID, Vector3 Position, out Vector3 newPosition)
        {
            newPosition = Position;
            EstateSettings ES = m_scene.EstateService.LoadEstateSettings(scene.RegionInfo.RegionID, false);
            AuroraProfileData Profile = PD.GetProfileInfo(userID);

            if (scene.RegionInfo.RegionSettings.Maturity > Profile.Mature)
                return false;

            if (ES.DenyMinors && Profile.Minor)
                return false;

            if (!ES.PublicAccess)
            {
                if (!new List<UUID>(ES.EstateManagers).Contains(userID) || ES.EstateOwner != userID)
                    return false;
            }
            if (!ES.AllowDirectTeleport)
            {
                IGenericData GenericData = Aurora.DataManager.DataManager.GetGenericPlugin();
                List<string> Telehubs = GenericData.Query("regionUUID", scene.RegionInfo.RegionID.ToString(), "auroraregions", "telehubX,telehubY");
                newPosition = new Vector3(Convert.ToInt32(Telehubs[0]), Convert.ToInt32(Telehubs[1]), Position.Z);
            }

            return true;
        }
    }
}
