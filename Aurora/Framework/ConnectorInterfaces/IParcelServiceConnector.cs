using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;

namespace Aurora.Framework
{
    public interface IParcelServiceConnector
    {
        void StoreLandObject(LandData args);
        LandData GetLandData(UUID ParcelID);
        List<LandData> LoadLandObjects(UUID regionUUID);
        void RemoveLandObject(UUID ParcelID);
    }
}
