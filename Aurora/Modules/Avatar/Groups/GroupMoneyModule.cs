using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Aurora.Modules.Avatar.Groups
{
    public class GroupMoneyModule : INonSharedRegionModule
    {
        private bool m_enabled = false;

        public string Name
        {
            get { return "GroupMoneyModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(IConfigSource source)
        {
            IConfig config = source.Configs["GroupMoney"];
            if (config != null)
                m_enabled = config.GetBoolean("Enabled", m_enabled);
        }

        public void AddRegion(IScene scene)
        {
        }

        public void RegionLoaded(IScene scene)
        {
            scene.EventManager.OnNewClient += new EventManager.OnNewClientDelegate(EventManager_OnNewClient);
            scene.EventManager.OnClosingClient += new EventManager.OnNewClientDelegate(EventManager_OnClosingClient);
        }

        public void RemoveRegion(IScene scene)
        {
        }

        public void Close()
        {
        }

        void EventManager_OnClosingClient(IClientAPI client)
        {
            client.OnGroupAccountSummaryRequest -= new GroupAccountSummaryRequest(client_OnGroupAccountSummaryRequest);
            client.OnGroupAccountTransactionsRequest -= new GroupAccountTransactionsRequest(client_OnGroupAccountTransactionsRequest);
            client.OnGroupAccountDetailsRequest -= new GroupAccountDetailsRequest(client_OnGroupAccountDetailsRequest);
        }

        void EventManager_OnNewClient(IClientAPI client)
        {
            client.OnGroupAccountSummaryRequest += new GroupAccountSummaryRequest(client_OnGroupAccountSummaryRequest);
            client.OnGroupAccountTransactionsRequest += new GroupAccountTransactionsRequest(client_OnGroupAccountTransactionsRequest);
            client.OnGroupAccountDetailsRequest += new GroupAccountDetailsRequest(client_OnGroupAccountDetailsRequest);
        }

        /// <summary>
        /// Sends the details about what
        /// </summary>
        /// <param name="client"></param>
        /// <param name="agentID"></param>
        /// <param name="groupID"></param>
        /// <param name="transactionID"></param>
        /// <param name="sessionID"></param>
        /// <param name="currentInterval"></param>
        /// <param name="intervalDays"></param>
        void client_OnGroupAccountDetailsRequest(IClientAPI client, UUID agentID, UUID groupID, UUID transactionID, UUID sessionID, int currentInterval, int intervalDays)
        {
            IGroupsModule groupsModule = client.Scene.RequestModuleInterface<IGroupsModule>();
            if (groupsModule != null && groupsModule.GroupPermissionCheck(agentID, groupID, GroupPowers.Accountable))
            {
                IMoneyModule moneyModule = client.Scene.RequestModuleInterface<IMoneyModule>();
                if (moneyModule != null)
                {
                    int amt = moneyModule.Balance(groupID);
                    List<GroupAccountHistory> history = moneyModule.GetTransactions(groupID, agentID, currentInterval, intervalDays);
                    history = (from h in history where h.Stipend select h).ToList();//We don't want payments, we only want stipends which we sent to users
                    GroupBalance balance = moneyModule.GetGroupBalance(groupID);
                    client.SendGroupAccountingDetails(client, groupID, transactionID, sessionID, amt, currentInterval, intervalDays,
                        Util.BuildYMDDateString(balance.StartingDate.AddDays(-currentInterval * intervalDays)), history.ToArray());
                }
                else
                    client.SendGroupAccountingDetails(client, groupID, transactionID, sessionID, 0, currentInterval, intervalDays,
                        "Never", new GroupAccountHistory[0]);
            }
        }

        /// <summary>
        /// Sends the transactions that the group has done over the given time period
        /// </summary>
        /// <param name="client"></param>
        /// <param name="agentID"></param>
        /// <param name="groupID"></param>
        /// <param name="transactionID"></param>
        /// <param name="sessionID"></param>
        /// <param name="currentInterval"></param>
        /// <param name="intervalDays"></param>
        void client_OnGroupAccountTransactionsRequest(IClientAPI client, UUID agentID, UUID groupID, UUID transactionID, UUID sessionID, int currentInterval, int intervalDays)
        {
            IGroupsModule groupsModule = client.Scene.RequestModuleInterface<IGroupsModule>();
            if (groupsModule != null && groupsModule.GroupPermissionCheck(agentID, groupID, GroupPowers.Accountable))
            {
                IMoneyModule moneyModule = client.Scene.RequestModuleInterface<IMoneyModule>();
                if (moneyModule != null)
                {
                    List<GroupAccountHistory> history = moneyModule.GetTransactions(groupID, agentID, currentInterval, intervalDays);
                    history = (from h in history where h.Payment select h).ToList();//We want payments for things only, not stipends
                    GroupBalance balance = moneyModule.GetGroupBalance(groupID);
                    client.SendGroupTransactionsSummaryDetails(client, groupID, transactionID, sessionID, currentInterval, intervalDays,
                        Util.BuildYMDDateString(balance.StartingDate.AddDays(-currentInterval * intervalDays)), history.ToArray());
                }
                else
                    client.SendGroupTransactionsSummaryDetails(client, groupID, transactionID, sessionID, currentInterval, intervalDays,
                        "Never", new GroupAccountHistory[0]);
            }
        }

        void client_OnGroupAccountSummaryRequest(IClientAPI client, UUID agentID, UUID groupID, UUID requestID, int currentInterval, int intervalDays)
        {
            IGroupsModule groupsModule = client.Scene.RequestModuleInterface<IGroupsModule>();
            if (groupsModule != null && groupsModule.GroupPermissionCheck(agentID, groupID, GroupPowers.Accountable))
            {
                IMoneyModule moneyModule = client.Scene.RequestModuleInterface<IMoneyModule>();
                if (moneyModule != null)
                {
                    int amt = moneyModule.Balance(groupID);
                    GroupBalance balance = moneyModule.GetGroupBalance(groupID);
                    client.SendGroupAccountingSummary(client, groupID, requestID, amt, balance.TotalTierDebit, balance.TotalTierCredits, Util.BuildYMDDateString(balance.StartingDate.AddDays(-currentInterval * intervalDays)),
                        currentInterval, intervalDays, Util.BuildYMDDateString(balance.StartingDate.AddDays(intervalDays)),
                        Util.BuildYMDDateString(balance.StartingDate.AddDays(-(currentInterval + 1) * intervalDays)), balance.ParcelDirectoryFee, balance.LandFee, balance.GroupFee, balance.ObjectFee);
                }
                else
                    client.SendGroupAccountingSummary(client, groupID, requestID, 0, 0, 0, "Never",
                        currentInterval, intervalDays, "Never",
                        "Never", 0, 0, 0, 0);
            }
        }
    }
}
