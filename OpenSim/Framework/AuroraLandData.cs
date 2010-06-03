using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;

namespace Aurora.Framework
{
    public class ExtendedAuroraLandData : AuroraLandData
    {
        public float GlobalPosX;
        public float GlobalPosY;
        public string RegionType;
        public string RegionName;
    }

    public class AuroraLandData
    {
        public UUID RegionID;
        public UUID ParcelID;
        public int LocalID;
        public float LandingX;
        public float LandingY;
        public float LandingZ;
        public string Name;
        public string Description;
        public uint Flags;
        public int Dwell;
        public UUID InfoUUID;
        public bool ForSale;
        public float SalePrice;
        public uint AuctionID;
        public int Area;
        public uint EstateID;
        public int Maturity;
        public UUID OwnerID;
        public UUID GroupID;
        public string MediaDescription;
        public int[] MediaSize;
        public byte MediaLoop;
        public string MediaType;
        public bool ObscureMedia;
        public bool ObscureMusic;
        public bool ShowInSearch;
        public UUID SnapshotID;
    }
}
