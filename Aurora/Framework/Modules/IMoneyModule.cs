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
using Aurora.Framework.PresenceInfo;
using OpenMetaverse;
using Aurora.Framework.Services;

namespace Aurora.Framework.Modules
{
    public delegate bool ObjectPaid(UUID objectID, UUID agentID, int amount);

    public enum TransactionType
    {
        SystemGenerated = 0,
        // One-Time Charges
        GroupCreate    	= 1002,
        GroupJoin      	= 1004,
        UploadCharge   	= 1101,
        LandAuction    	= 1102,
        ClassifiedCharge= 1103,
        // Recurrent Charges
        ParcelDirFee  	= 2003,
        ClassifiedRenew = 2005,
        ScheduledFee    = 2900,
        // Inventory Transactions
        GiveInventory   = 3000,
        // Transfers Between Users
        ObjectSale     	= 5000,
        Gift           	= 5001,
        LandSale       	= 5002,
        ReferBonus     	= 5003,
        InvntorySale   	= 5004,
        RefundPurchase 	= 5005,
        LandPassSale   	= 5006,
        DwellBonus     	= 5007,
        PayObject      	= 5008,
        ObjectPays     	= 5009,
        BuyMoney       	= 5010,
        MoveMoney      	= 5011,
        // Group Transactions
        GroupLiability 	= 6003,
        GroupDividend  	= 6004,
        // Stipend Credits
        StipendPayment 	= 10000
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

        bool Transfer(UUID toID, UUID fromID, int amount, string description);
        bool Transfer(UUID toID, UUID fromID, int amount, string description, TransactionType type);

        bool Transfer(UUID toID, UUID fromID, UUID toObjectID, UUID fromObjectID, int amount, string description,
                      TransactionType type);

        /// <summary>
        ///     Get a list of transactions that have occured over the given interval (0 is this period of interval days, positive #s go back previous sets)
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

    public interface ISimpleCurrencyConnector : IAuroraDataPlugin
    {
        /*SimpleCurrencyConfig GetConfig();
        UserCurrency GetUserCurrency(UUID agentId);
        bool UserCurrencyUpdate(UserCurrency agent);
        GroupBalance GetGroupBalance(UUID groupID);

        bool UserCurrencyTransfer(UUID toID, UUID fromID, UUID toObjectID, UUID fromObjectID, uint amount,
                                  string description, TransactionType type, UUID transactionID);*/
    }
}