using System;
using System.Collections.Generic;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using ProtoBuf;

namespace Aurora.Framework
{
    public interface IAuctionModule
    {
        void StartAuction(int LocalID, UUID SnapshotID);
        void SetAuctionInfo(int LocalID, AuctionInfo info);
        void AddAuctionBid(int LocalID, UUID userID, int bid);
        void AuctionEnd(int LocalID);
    }

    [Serializable, ProtoContract(UseProtoMembersOnly = false)]
    public class AuctionInfo
    {
        /// <summary>
        /// Auction length (in days)
        /// </summary>
        [ProtoMember(1)]
        public int AuctionLength = 7;

        /// <summary>
        /// Date the auction started
        /// </summary>
        [ProtoMember(2)]
        public DateTime AuctionStart = DateTime.Now;

        /// <summary>
        /// Description of the parcel
        /// </summary>
        [ProtoMember(3)]
        public string Description = "";

        /// <summary>
        /// List of bids on the auction so far
        /// </summary>
        [ProtoMember(4)]
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

    [Serializable, ProtoContract(UseProtoMembersOnly = false)]
    public class AuctionBid
    {
        /// <summary>
        /// The person who bid on the auction
        /// </summary>
        [ProtoMember(1)]
        public UUID AuctionBidder;

        /// <summary>
        /// The amount bid on the auction
        /// </summary>
        [ProtoMember(2)]
        public int Amount;

        /// <summary>
        /// The time the bid was added
        /// </summary>
        [ProtoMember(3)]
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
