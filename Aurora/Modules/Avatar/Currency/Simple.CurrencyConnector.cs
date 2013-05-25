using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Simple.Currency
{
    
    public class SimpleCurrencyConnector : ConnectorBase, ISimpleCurrencyConnector
    {
        #region Declares

        private IGenericData m_gd;
        private SimpleCurrencyConfig m_config;
        private ISyncMessagePosterService m_syncMessagePoster;
        private IAgentInfoService m_userInfoService;
        private const string _REALM = "simple_currency";

        #endregion

        #region IAuroraDataPlugin Members

        public string InterfaceName
        {
            get { return "ISimpleCurrencyConnector"; }
        }

        public void Initialize(IGenericData genericData, IConfigSource source, IRegistryCore simBase)
        {
            m_gd = genericData;

            m_config = new SimpleCurrencyConfig(source.Configs["Currency"]);

            Init(simBase, InterfaceName, "", "/currency/", "CurrencyServerURI");

            if (!m_doRemoteCalls)
            {
                MainConsole.Instance.Commands.AddCommand("money add", "money add", "Adds money to a user's account.",
                                                         AddMoney);
                MainConsole.Instance.Commands.AddCommand("money set", "money set",
                                                         "Sets the amount of money a user has.",
                                                         SetMoney);
                MainConsole.Instance.Commands.AddCommand("money get", "money get",
                                                         "Gets the amount of money a user has.",
                                                         GetMoney);
            }
        }

        #endregion

        #region Service Members

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public SimpleCurrencyConfig GetConfig()
        {
            object remoteValue = DoRemoteByURL("CurrencyServerURI");
            if (remoteValue != null || m_doRemoteOnly)
                return (SimpleCurrencyConfig) remoteValue;

            return m_config;
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public UserCurrency GetUserCurrency(UUID agentId)
        {
            object remoteValue = DoRemoteByURL("CurrencyServerURI", agentId);
            if (remoteValue != null || m_doRemoteOnly)
                return (UserCurrency) remoteValue;

            Dictionary<string, object> where = new Dictionary<string, object>(1);
            where["PrincipalID"] = agentId;
            List<string> query = m_gd.Query(new string[] {"*"}, _REALM, new QueryFilter()
                                                                            {
                                                                                andFilters = where
                                                                            }, null, null, null);

            UserCurrency currency;

            if (query.Count == 0)
            {
                currency = new UserCurrency(agentId, 0, 0, 0, false, 0);
                UserCurrencyCreate(agentId);
                return currency;
            }
            
            return new UserCurrency(query);
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public GroupBalance GetGroupBalance(UUID groupID)
        {
            object remoteValue = DoRemoteByURL("CurrencyServerURI", groupID);
            if (remoteValue != null || m_doRemoteOnly)
                return (GroupBalance) remoteValue;

            GroupBalance gb = new GroupBalance()
                                  {
                                      GroupFee = 0,
                                      LandFee = 0,
                                      ObjectFee = 0,
                                      ParcelDirectoryFee = 0,
                                      TotalTierCredits = 0,
                                      TotalTierDebit = 0,
                                      StartingDate = DateTime.UtcNow
                                  };
            Dictionary<string, object> where = new Dictionary<string, object>(1);
            where["PrincipalID"] = groupID;
            List<string> queryResults = m_gd.Query(new string[] {"*"}, _REALM, new QueryFilter()
                                                                                   {
                                                                                       andFilters = where
                                                                                   }, null, null, null);

            if (queryResults.Count == 0)
            {
                GroupCurrencyCreate(groupID);
                return gb;
            }

            int.TryParse(queryResults[1], out gb.TotalTierCredits);
            return gb;
        }

        public int CalculateEstimatedCost(uint amount)
        {
            return Convert.ToInt32(
                Math.Round(((float.Parse(amount.ToString()) /
                            m_config.RealCurrencyConversionFactor) +
                            ((float.Parse(amount.ToString()) /
                            m_config.RealCurrencyConversionFactor) *
                            (m_config.AdditionPercentage / 10000.0)) +
                            (m_config.AdditionAmount / 100.0)) * 100));
        }

        public int CheckMinMaxTransferSettings(UUID agentID, uint amount)
        {
            amount = Math.Max(amount, (uint)m_config.MinAmountPurchasable);
            amount = Math.Min(amount, (uint)m_config.MaxAmountPurchasable);
            List<uint> recentTransactions = GetAgentRecentTransactions(agentID);

            long currentlyBought = recentTransactions.Sum((u) => u);
            return (int)Math.Min(amount, m_config.MaxAmountPurchasableOverTime - currentlyBought);
        }

        public bool InworldCurrencyBuyTransaction(UUID agentID, uint amount, IPEndPoint ep)
        {
            amount = (uint)CheckMinMaxTransferSettings(agentID, amount);
            if (amount == 0)
                return false;
            UserCurrencyTransfer(agentID, UUID.Zero, amount,
                                             "Inworld purchase", TransactionType.SystemGenerated, UUID.Zero);

            //Log to the database
            List<object> values = new List<object>
            {
                UUID.Random(),                         // TransactionID
                agentID.ToString(),                    // PrincipalID
                ep.ToString(),                         // IP
                amount,                                // Amount
                CalculateEstimatedCost(amount),        // Actual cost
                Utils.GetUnixTime(),                   // Created
                Utils.GetUnixTime()                    // Updated
            };
            m_gd.Insert("simple_purchased", values.ToArray());
            return true;
        }

        private List<uint> GetAgentRecentTransactions(UUID agentID)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["PrincipalID"] = agentID;
            DateTime now = DateTime.Now;
            RepeatType runevertype = (RepeatType)Enum.Parse(typeof(RepeatType), m_config.MaxAmountPurchasableEveryType);
            switch (runevertype)
            {
                case RepeatType.second:
                    now = now.AddSeconds(-m_config.MaxAmountPurchasableEveryAmount);
                    break;
                case RepeatType.minute:
                    now = now.AddMinutes(-m_config.MaxAmountPurchasableEveryAmount);
                    break;
                case RepeatType.hours:
                    now = now.AddHours(-m_config.MaxAmountPurchasableEveryAmount);
                    break;
                case RepeatType.days:
                    now = now.AddDays(-m_config.MaxAmountPurchasableEveryAmount);
                    break;
                case RepeatType.weeks:
                    now = now.AddDays(-m_config.MaxAmountPurchasableEveryAmount * 7);
                    break;
                case RepeatType.months:
                    now = now.AddMonths(-m_config.MaxAmountPurchasableEveryAmount);
                    break;
                case RepeatType.years:
                    now = now.AddYears(-m_config.MaxAmountPurchasableEveryAmount);
                    break;
            }
            filter.andGreaterThanEqFilters["Created"] = Utils.DateTimeToUnixTime(now);//Greater than the time that we are checking against
            filter.andLessThanEqFilters["Created"] = Utils.GetUnixTime();//Less than now
            List<string> query = m_gd.Query(new string[1] { "Amount" }, "simple_purchased", filter, null, null, null);
            if (query == null)
                return new List<uint>();
            return query.ConvertAll<uint>(s=>uint.Parse(s));
        }

        public bool UserCurrencyTransfer(UUID toID, UUID fromID, uint amount,
                                         string description, TransactionType type, UUID transactionID)
        {
            return UserCurrencyTransfer(toID, fromID, UUID.Zero, "", UUID.Zero, "", amount, description, type, transactionID);
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public bool UserCurrencyTransfer(UUID toID, UUID fromID, UUID toObjectID, string toObjectName, UUID fromObjectID,
            string fromObjectName, uint amount, string description, TransactionType type, UUID transactionID)
        {
            object remoteValue = DoRemoteByURL("CurrencyServerURI", toID, fromID, toObjectID, toObjectName, fromObjectID,
                fromObjectName, amount, description, type, transactionID);
            if (remoteValue != null || m_doRemoteOnly)
                return (bool) remoteValue;

            UserCurrency toCurrency = GetUserCurrency(toID);
            UserCurrency fromCurrency = fromID == UUID.Zero ? null : GetUserCurrency(fromID);
            if (toCurrency == null)
                return false;
            if (fromCurrency != null)
            {
                //Check to see whether they have enough money
                if ((int) fromCurrency.Amount - (int) amount < 0)
                    return false; //Not enough money
                fromCurrency.Amount -= amount;

                UserCurrencyUpdate(fromCurrency, true);
            }
            if (fromID == toID) toCurrency = GetUserCurrency(toID);

            //Update the user whose getting paid
            toCurrency.Amount += amount;
            UserCurrencyUpdate(toCurrency, true);

            //Must send out noficiations to the users involved so that they get the updates
            if (m_syncMessagePoster == null)
            {
                m_syncMessagePoster = m_registry.RequestModuleInterface<ISyncMessagePosterService>();
                m_userInfoService = m_registry.RequestModuleInterface<IAgentInfoService>();
            }
            if (m_syncMessagePoster != null)
            {
                UserInfo toUserInfo = m_userInfoService.GetUserInfo(toID.ToString());
                UserInfo fromUserInfo = fromID == UUID.Zero ? null : m_userInfoService.GetUserInfo(fromID.ToString());
                UserAccount toAccount = m_registry.RequestModuleInterface<IUserAccountService>()
                                                  .GetUserAccount(null, toID);
                UserAccount fromAccount = m_registry.RequestModuleInterface<IUserAccountService>()
                                                    .GetUserAccount(null, fromID);
                if (m_config.SaveTransactionLogs)
                    AddTransactionRecord((transactionID == UUID.Zero ? UUID.Random() : transactionID), 
                        description, toID, fromID, amount, type, (toCurrency == null ? 0 : toCurrency.Amount), 
                        (fromCurrency == null ? 0 : fromCurrency.Amount), (toAccount == null ? "System" : toAccount.Name), 
                        (fromAccount == null ? "System" : fromAccount.Name), toObjectName, fromObjectName, (fromUserInfo == null ? 
                        UUID.Zero : fromUserInfo.CurrentRegionID));

                if (fromID == toID)
                {
                    if (toUserInfo != null && toUserInfo.IsOnline)
                        SendUpdateMoneyBalanceToClient(toID, transactionID, toUserInfo.CurrentRegionURI, toCurrency.Amount,
                            toAccount == null ? "" : (toAccount.Name + " paid you $" + amount + (description == "" ? "" : ": " + description)));
                }
                else
                {
                    if (toUserInfo != null && toUserInfo.IsOnline)
                    {
                        SendUpdateMoneyBalanceToClient(toID, transactionID, toUserInfo.CurrentRegionURI, toCurrency.Amount,
                            fromAccount == null ? "" : (fromAccount.Name + " paid you $" + amount + (description == "" ? "" : ": " + description)));
                    }
                    if (fromUserInfo != null && fromUserInfo.IsOnline)
                    {
                        SendUpdateMoneyBalanceToClient(fromID, transactionID, fromUserInfo.CurrentRegionURI, fromCurrency.Amount,
                            "You paid " + (toAccount == null ? "" : toAccount.Name) + " $" + amount);
                    }
                }
            }
            return true;
        }

        private void SendUpdateMoneyBalanceToClient(UUID toID, UUID transactionID, string serverURI, uint balance, string message)
        {
            OSDMap map = new OSDMap();
            map["Method"] = "UpdateMoneyBalance";
            map["AgentID"] = toID;
            map["Amount"] = balance;
            map["Message"] = message;
            map["TransactionID"] = transactionID;
            m_syncMessagePoster.Post(serverURI, map);
        }

        // Method Added By Alicia Raven
        private void AddTransactionRecord(UUID TransID, string Description, UUID ToID, UUID FromID, uint Amount,
            TransactionType TransType, uint ToBalance, uint FromBalance, string ToName, string FromName, string toObjectName, string fromObjectName, UUID regionID)
        {
            if(Amount > m_config.MaxAmountBeforeLogging)
                m_gd.Insert("simple_currency_history", new object[] { TransID, (Description == null ? "" : Description),
                    FromID.ToString(), FromName, ToID.ToString(), ToName, Amount, (int)TransType, Util.UnixTimeSinceEpoch(), ToBalance, FromBalance,
                    toObjectName == null ? "" : toObjectName, fromObjectName == null ? "" : fromObjectName, regionID });
        }

        #endregion

        #region Helper Methods

        private void UserCurrencyUpdate(UserCurrency agent, bool full)
        {
            if (full)
                m_gd.Update(_REALM,
                            new Dictionary<string, object>
                                {
                                    {"LandInUse", agent.LandInUse},
                                    {"Tier", agent.Tier},
                                    {"IsGroup", agent.IsGroup},
                                    {"Amount", agent.Amount},
                                    {"StipendsBalance", agent.StipendsBalance}
                                }, null,
                            new QueryFilter()
                                {
                                    andFilters =
                                        new Dictionary<string, object>
                                            {
                                                {"PrincipalID", agent.PrincipalID}
                                            }
                                }
                            , null, null);
            else
                m_gd.Update(_REALM,
                            new Dictionary<string, object>
                                {
                                    {"LandInUse", agent.LandInUse},
                                    {"Tier", agent.Tier},
                                    {"IsGroup", agent.IsGroup}
                                }, null,
                            new QueryFilter()
                                {
                                    andFilters =
                                        new Dictionary<string, object>
                                            {
                                                {"PrincipalID", agent.PrincipalID}
                                            }
                                }
                            , null, null);
        }

        private void UserCurrencyCreate(UUID agentId)
        {
            m_gd.Insert(_REALM, new object[] {agentId.ToString(), 0, 0, 0, 0, 0});
        }

        private void GroupCurrencyCreate(UUID groupID)
        {
            m_gd.Insert(_REALM, new object[] {groupID.ToString(), 0, 0, 0, 1, 0});
        }

        #endregion

        #region Console Methods

        public void AddMoney(string[] cmd)
        {
            string name = MainConsole.Instance.Prompt("User Name: ");
            uint amount = 0;
            while (!uint.TryParse(MainConsole.Instance.Prompt("Amount: ", "0"), out amount))
                MainConsole.Instance.Info("Bad input, must be a number > 0");

            UserAccount account =
                m_registry.RequestModuleInterface<IUserAccountService>()
                          .GetUserAccount(new List<UUID> {UUID.Zero}, name);
            if (account == null)
            {
                MainConsole.Instance.Info("No account found");
                return;
            }
            var currency = GetUserCurrency(account.PrincipalID);
            m_gd.Update(_REALM,
                        new Dictionary<string, object>
                            {
                                {
                                    "Amount", currency.Amount + amount
                                }
                            }, null, new QueryFilter()
                                         {
                                             andFilters =
                                                 new Dictionary<string, object> {{"PrincipalID", account.PrincipalID}}
                                         }, null, null);
            MainConsole.Instance.Info(account.Name + " now has $" + (currency.Amount + amount));

            if (m_syncMessagePoster == null)
            {
                m_syncMessagePoster = m_registry.RequestModuleInterface<ISyncMessagePosterService>();
                m_userInfoService = m_registry.RequestModuleInterface<IAgentInfoService>();
            }
            if (m_syncMessagePoster != null)
            {
                UserInfo toUserInfo = m_userInfoService.GetUserInfo(account.PrincipalID.ToString());
                if (toUserInfo != null && toUserInfo.IsOnline)
                    SendUpdateMoneyBalanceToClient(account.PrincipalID, UUID.Zero, toUserInfo.CurrentRegionURI, (currency.Amount + amount), "");
            }
        }

        public void SetMoney(string[] cmd)
        {
            string name = MainConsole.Instance.Prompt("User Name: ");
            uint amount = 0;
            while (!uint.TryParse(MainConsole.Instance.Prompt("Set User's Money Amount: ", "0"), out amount))
                MainConsole.Instance.Info("Bad input, must be a number > 0");

            UserAccount account =
                m_registry.RequestModuleInterface<IUserAccountService>()
                          .GetUserAccount(new List<UUID> {UUID.Zero}, name);
            if (account == null)
            {
                MainConsole.Instance.Info("No account found");
                return;
            }
            m_gd.Update(_REALM,
                        new Dictionary<string, object>
                            {
                                {
                                    "Amount", amount
                                }
                            }, null, new QueryFilter()
                                         {
                                             andFilters =
                                                 new Dictionary<string, object> {{"PrincipalID", account.PrincipalID}}
                                         }, null, null);
            MainConsole.Instance.Info(account.Name + " now has $" + amount);

            if (m_syncMessagePoster == null)
            {
                m_syncMessagePoster = m_registry.RequestModuleInterface<ISyncMessagePosterService>();
                m_userInfoService = m_registry.RequestModuleInterface<IAgentInfoService>();
            }
            if (m_syncMessagePoster != null)
            {
                UserInfo toUserInfo = m_userInfoService.GetUserInfo(account.PrincipalID.ToString());
                if (toUserInfo != null && toUserInfo.IsOnline)
                    SendUpdateMoneyBalanceToClient(account.PrincipalID, UUID.Zero, toUserInfo.CurrentRegionURI, amount, "");
            }
        }

        public void GetMoney(string[] cmd)
        {
            string name = MainConsole.Instance.Prompt("User Name: ");
            UserAccount account =
                m_registry.RequestModuleInterface<IUserAccountService>()
                          .GetUserAccount(new List<UUID> {UUID.Zero}, name);
            if (account == null)
            {
                MainConsole.Instance.Info("No account found");
                return;
            }
            var currency = GetUserCurrency(account.PrincipalID);
            if (currency == null)
            {
                MainConsole.Instance.Info("No currency account found");
                return;
            }
            MainConsole.Instance.Info(account.Name + " has $" + currency.Amount);
        }

        #endregion
    }
}