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
        public void ConvertFrom(AuroraLandData landData)
        {
            Area = landData.Area;
            AuctionID = landData.AuctionID;
            Description = landData.Description;
            Dwell = landData.Dwell;
            EstateID = landData.EstateID;
            Flags = landData.Flags;
            ForSale = landData.ForSale;
            GroupID = landData.GroupID;
            InfoUUID = landData.InfoUUID;
            LandingX = landData.LandingX;
            LandingY = landData.LandingY;
            LandingZ = landData.LandingZ;
            LocalID = landData.LocalID;
            Maturity = landData.Maturity;
            MediaDescription = landData.MediaDescription;
            MediaLoop = landData.MediaLoop;
            MediaSize = landData.MediaSize;
            MediaType = landData.MediaType;
            Name = landData.Name;
            ObscureMedia = landData.ObscureMedia;
            ObscureMusic = landData.ObscureMusic;
            OwnerID = landData.OwnerID;
            ParcelID = landData.ParcelID;
            RegionID = landData.RegionID;
            SalePrice = landData.SalePrice;
            ShowInSearch = landData.ShowInSearch;
            SnapshotID = landData.SnapshotID;
        }
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
        public UUID MediaTextureID;
        public byte MediaAutoScale;
        public string MediaURL;
        public string MusicURL;
        public byte[] Bitmap;
        public ParcelCategory Category;
        public int ClaimDate;
        public int ClaimPrice;
        public ParcelStatus Status;
        public byte LandingType;
        public Single PassHours;
        public int PassPrice;
        public float LookAtX;
        public float LookAtY;
        public float LookAtZ;
        public int OtherCleanTime;
        public UUID AuthBuyerID;
        public List<ParcelManager.ParcelAccessEntry> AccessEntry;

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> KVP = new Dictionary<string, object>();
            KVP["SnapshotID"] = SnapshotID;
            KVP["ShowInSearch"] = ShowInSearch;
            KVP["GroupID"] = GroupID;
            KVP["OwnerID"] = OwnerID;
            KVP["Maturity"] = Maturity;
            KVP["EstateID"] = EstateID;
            KVP["Area"] = Area;
            KVP["AuctionID"] = AuctionID;
            KVP["SalePrice"] = SalePrice;
            KVP["ForSale"] = ForSale;
            KVP["InfoUUID"] = InfoUUID;
            KVP["Dwell"] = Dwell;
            KVP["Flags"] = Flags;
            KVP["Name"] = Name;
            KVP["Description"] = Description;
            KVP["LandingZ"] = LandingZ;
            KVP["LandingY"] = LandingY;
            KVP["LandingX"] = LandingX;
            KVP["LocalID"] = LocalID;
            KVP["ParcelID"] = ParcelID;
            KVP["RegionID"] = RegionID;
            return KVP;
        }

        public AuroraLandData()
        {
        }

        public AuroraLandData(Dictionary<string, object> KVP)
        {
            RegionID = UUID.Parse(KVP["RegionID"].ToString());
            ParcelID = UUID.Parse(KVP["ParcelID"].ToString());
            LocalID = int.Parse(KVP["LocalID"].ToString());
            LandingX = float.Parse(KVP["LandingX"].ToString());
            LandingY = float.Parse(KVP["LandingY"].ToString());
            LandingZ = float.Parse(KVP["LandingZ"].ToString());
            Name = KVP["Name"].ToString();
            Description = KVP["Description"].ToString();
            Flags = uint.Parse(KVP["Flags"].ToString());
            Dwell = int.Parse(KVP["Dwell"].ToString());
            InfoUUID = UUID.Parse(KVP["InfoUUID"].ToString());
            ForSale = bool.Parse(KVP["ForSale"].ToString());
            SalePrice = float.Parse(KVP["SalePrice"].ToString());
            AuctionID = uint.Parse(KVP["AuctionID"].ToString());
            Area = int.Parse(KVP["Area"].ToString());
            EstateID = uint.Parse(KVP["EstateID"].ToString());
            Maturity = int.Parse(KVP["Maturity"].ToString());
            OwnerID = UUID.Parse(KVP["OwnerID"].ToString());
            GroupID = UUID.Parse(KVP["GroupID"].ToString());
            ShowInSearch = bool.Parse(KVP["ShowInSearch"].ToString());
            SnapshotID = UUID.Parse(KVP["SnapshotID"].ToString());
        }
    }
}
