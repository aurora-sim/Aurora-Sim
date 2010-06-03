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
    }
}
