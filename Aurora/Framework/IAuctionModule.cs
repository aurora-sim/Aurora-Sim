using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
    public interface IAuctionModule
    {
        void StartAuction(int LocalID, UUID SnapshotID);
        void SetAuctionInfo(int LocalID, AuctionInfo info);
        void AddAuctionBid(int LocalID, UUID userID, int bid);
        void AuctionEnd(int LocalID);
    }

    public class AuctionInfo
    {
        /// <summary>
        /// Auction length (in days)
        /// </summary>
        public int AuctionLength = 7;

        /// <summary>
        /// Date the auction started
        /// </summary>
        public DateTime AuctionStart = DateTime.Now;

        /// <summary>
        /// Description of the parcel
        /// </summary>
        public string Description = "";

        /// <summary>
        /// List of bids on the auction so far
        /// </summary>
        public List<AuctionBid> AuctionBids = new List<AuctionBid>();

        public void FromOSD(OSDMap map)
        {
            AuctionStart = map["AuctionStart"];
            Description = map["Description"];
            AuctionLength = map["AuctionLength"];
            foreach(OSD o in (OSDArray)map["AuctionBids"])
            {
                AuctionBid bid = new AuctionBid();
                bid.FromOSD((OSDMap)o);
                AuctionBids.Add(bid);
            }
        }

        public OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["AuctionStart"] = AuctionStart;
            map["AuctionLength"] = AuctionLength;
            map["Description"] = Description;
            OSDArray array = new OSDArray();
            foreach (AuctionBid bid in AuctionBids)
                array.Add(bid.ToOSD());
            map["AuctionBids"] = array;
            return map;
        }
    }

    public class AuctionBid
    {
        /// <summary>
        /// The person who bid on the auction
        /// </summary>
        public UUID AuctionBidder;

        /// <summary>
        /// The amount bid on the auction
        /// </summary>
        public int Amount;

        /// <summary>
        /// The time the bid was added
        /// </summary>
        public DateTime TimeBid;

        public OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();
            map["TimeBid"] = TimeBid;
            map["Amount"] = Amount;
            map["AuctionBidder"] = AuctionBidder;
            return map;
        }

        public void FromOSD(OSDMap map)
        {
            TimeBid = map["TimeBid"];
            Amount = map["Amount"];
            AuctionBidder = map["AuctionBidder"];
        }
    }
}
