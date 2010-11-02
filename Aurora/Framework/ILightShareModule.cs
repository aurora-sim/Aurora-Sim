using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;

namespace Aurora.Framework
{
    public interface IWindLightSettingsModule
    {
        void SendWindlightProfileTargeted(RegionLightShareData wl, UUID pUUID);
        void SaveWindLightSettings(float MinEffectiveAltitude, RegionLightShareData wl);
        //RegionLightShareData WindLightSettings { get; }
        bool EnableWindLight { get; }
    }
}
