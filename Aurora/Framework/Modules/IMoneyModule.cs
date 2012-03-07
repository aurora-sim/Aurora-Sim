/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using OpenMetaverse;

namespace Aurora.Framework
{
    //public delegate void ObjectPaid(UUID objectID, UUID agentID, int amount);
    // For legacy money module. Fumi.Iseki
    public delegate bool ObjectPaid(UUID objectID, UUID agentID, int amount);

    // For DTL money module.
    public delegate bool PostObjectPaid(uint localID, ulong regionHandle, UUID agentID, int amount);

    public enum TransactionType
    {
        SystemGenerated = 0,
        GroupCreate = 1002,
        UploadFee = 1101,
        AuctionFee = 1102,
        ClassifiedFee = 1103,
        DirectoryFee = 2003,
        ClassifiedRenewFee = 2005,
        Inventory = 3000,
        ObjectBuy = 5000,
        Gift = 5001,
        LandSale = 5002,
        LandPassFee = 5006,
        PayIntoObject = 5008,
        ObjectPaysAvatar = 5009,
        GroupLiability = 6003,
        GroupDividend = 6004,
        StipendPayment = 10000
    }

    public class GroupBalance : IDataTransferable
    {
        public int TotalTierDebit = 0;
        public int TotalTierCredits = 0;
        public int ParcelDirectoryFee = 0;
        public int LandFee = 0;
        public int ObjectFee = 0;
        public int GroupFee = 0;
        public DateTime StartingDate;

        public override void FromOSD(OpenMetaverse.StructuredData.OSDMap map)
        {
            TotalTierDebit = map["TotalTierDebit"];
            TotalTierCredits = map["TotalTierCredits"];
            ParcelDirectoryFee = map["ParcelDirectoryFee"];
            LandFee = map["LandFee"];
            ObjectFee = map["ObjectFee"];
            GroupFee = map["GroupFee"];
            StartingDate = map["StartingDate"];
        }

        public override OpenMetaverse.StructuredData.OSDMap ToOSD()
        {
            OpenMetaverse.StructuredData.OSDMap map = new OpenMetaverse.StructuredData.OSDMap();

            map["TotalTierDebit"] = TotalTierDebit;
            map["TotalTierCredits"] = TotalTierCredits;
            map["ParcelDirectoryFee"] = ParcelDirectoryFee;
            map["LandFee"] = LandFee;
            map["ObjectFee"] = ObjectFee;
            map["GroupFee"] = GroupFee;
            map["StartingDate"] = StartingDate;

            return map;
        }
    }

    public interface IMoneyModule
    {
        int UploadCharge { get; }
        int GroupCreationCharge { get; }
        int ClientPort { get; }

        bool ObjectGiveMoney(UUID objectID, UUID fromID, UUID toID,
                             int amount);

        int Balance(UUID agentID);
        bool Charge(IClientAPI client, int amount);
        bool Charge(UUID agentID, int amount, string text);

        event ObjectPaid OnObjectPaid;
        event PostObjectPaid OnPostObjectPaid;

        bool Transfer(UUID toID, UUID fromID, int amount, string description);
        bool Transfer(UUID toID, UUID fromID, int amount, string description, TransactionType type);

        bool Transfer(UUID toID, UUID fromID, UUID toObjectID, UUID fromObjectID, int amount, string description,
                      TransactionType type);

        /// <summary>
        /// Get a list of transactions that have occured over the given interval (0 is this period of interval days, positive #s go back previous sets)
        /// </summary>
        /// <param name="groupID"></param>
        /// <param name="agentID">Requesting agentID (must be checked whether they can call this)</param>
        /// <param name="currentInterval"></param>
        /// <param name="intervalDays"></param>
        List<GroupAccountHistory> GetTransactions(UUID groupID, UUID agentID, int currentInterval, int intervalDays);

        GroupBalance GetGroupBalance(UUID groupID);
    }

    public delegate void UserDidNotPay(UUID agentID, string paymentTextThatFailed);
    public delegate bool CheckWhetherUserShouldPay(UUID agentID, string paymentTextThatFailed);

    public interface IScheduledMoneyModule
    {
        event UserDidNotPay OnUserDidNotPay;
        event CheckWhetherUserShouldPay OnCheckWhetherUserShouldPay;
        bool Charge(UUID agentID, int amount, string text, int daysUntilNextCharge);
    }
}