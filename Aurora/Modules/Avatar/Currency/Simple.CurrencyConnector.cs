using Aurora.DataManager;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Simple.Currency
{
    public interface ISimpleCurrencyConnector : IAuroraDataPlugin
    {
        SimpleCurrencyConfig GetConfig();
        UserCurrency GetUserCurrency(UUID agentId);
        bool UserCurrencyUpdate(UserCurrency agent);
        GroupBalance GetGroupBalance(UUID groupID);

        bool UserCurrencyTransfer(UUID toID, UUID fromID, UUID toObjectID, UUID fromObjectID, uint amount, string description, TransactionType type, UUID transactionID);
    }

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

        public string Name
        {
            get { return "ISimpleCurrencyConnector"; }
        }

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore registry, string defaultConnectionString)
        {
            m_gd = GenericData;
            m_registry = registry;

            IConfig config = source.Configs["Currency"];
            if (source.Configs["Currency"] != null && source.Configs["Currency"].GetString("Module", "") != "SimpleCurrency")
                return;

            if (source.Configs[Name] != null)
                defaultConnectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

            if(GenericData != null)
                GenericData.ConnectToDatabase(defaultConnectionString, "SimpleCurrency", true);
            DataManager.RegisterPlugin(Name, this);

            m_config = new SimpleCurrencyConfig(source.Configs["Currency"]);

            MainConsole.Instance.Commands.AddCommand("money add", "money add", "Adds money to a user's account.",
                AddMoney);
            MainConsole.Instance.Commands.AddCommand("money set", "money set", "Sets the amount of money a user has.",
                SetMoney);
            MainConsole.Instance.Commands.AddCommand("money get", "money get", "Gets the amount of money a user has.",
                GetMoney);

            Init(m_registry, Name, "", "/currency/");
        }

        #endregion

        #region Service Members

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public SimpleCurrencyConfig GetConfig()
        {
            object remoteValue = DoRemoteByURL("CurrencyServerURI");
            if (remoteValue != null || m_doRemoteOnly)
                return (SimpleCurrencyConfig)remoteValue;

            return m_config;
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public UserCurrency GetUserCurrency(UUID agentId)
        {
            object remoteValue = DoRemoteByURL("CurrencyServerURI", agentId);
            if (remoteValue != null || m_doRemoteOnly)
                return (UserCurrency)remoteValue;

            Dictionary<string, object> where = new Dictionary<string, object>(1);
            where["PrincipalID"] = agentId;
            List<string> query = m_gd.Query(new string[] { "*" }, _REALM, new QueryFilter()
            {
                andFilters = where
            }, null, null, null);

            UserCurrency currency = new UserCurrency();
            if (query.Count == 0)
            {
                UserCurrencyCreate(agentId);
                return currency;
            }
            currency.FromArray(query);
            return currency;
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public bool UserCurrencyUpdate(UserCurrency agent)
        {
            object remoteValue = DoRemoteByURL("CurrencyServerURI", agent);
            if (remoteValue != null || m_doRemoteOnly)
                return (bool)remoteValue;

            UserCurrencyUpdate(agent, false);
            return true;
        }

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public GroupBalance GetGroupBalance(UUID groupID)
        {
            object remoteValue = DoRemoteByURL("CurrencyServerURI", groupID);
            if (remoteValue != null || m_doRemoteOnly)
                return (GroupBalance)remoteValue;

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
            List<string> queryResults = m_gd.Query(new string[] { "*" }, _REALM, new QueryFilter()
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

        [CanBeReflected(ThreatLevel = ThreatLevel.Low)]
        public bool UserCurrencyTransfer(UUID toID, UUID fromID, UUID toObjectID, UUID fromObjectID, uint amount, string description, TransactionType type, UUID transactionID)
        {
            object remoteValue = DoRemoteByURL("CurrencyServerURI", toID, fromID, toObjectID, fromObjectID, amount, description, type, transactionID);
            if (remoteValue != null || m_doRemoteOnly)
                return (bool)remoteValue;

            UserCurrency toCurrency = GetUserCurrency(toID);
            UserCurrency fromCurrency = fromID == UUID.Zero ? null : GetUserCurrency(fromID);
            if (toCurrency == null)
                return false;
            if (fromCurrency != null)
            {
                //Check to see whether they have enough money
                if ((int)fromCurrency.Amount - (int)amount < 0)
                    return false;//Not enough money
                fromCurrency.Amount -= amount;

                UserCurrencyUpdate(fromCurrency, true);
            }

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
                UserAccount toAccount = m_registry.RequestModuleInterface<IUserAccountService>().GetUserAccount(null, toID);
                UserAccount fromAccount = m_registry.RequestModuleInterface<IUserAccountService>().GetUserAccount(null, fromID);
                if (toUserInfo != null && toUserInfo.IsOnline)
                {
                    OSDMap map = new OSDMap();
                    map["Method"] = "UpdateMoneyBalance";
                    map["AgentID"] = toID;
                    map["Amount"] = toCurrency.Amount;
                    map["Message"] = fromAccount.Name + " paid you $" + amount + (description == "" ? "" : ": " + description);
                    map["TransactionID"] = transactionID;
                    m_syncMessagePoster.Post(toUserInfo.CurrentRegionURI, map);
                }
                if (fromUserInfo != null && fromUserInfo.IsOnline)
                {
                    OSDMap map = new OSDMap();
                    map["Method"] = "UpdateMoneyBalance";
                    map["AgentID"] = fromID;
                    map["Amount"] = fromCurrency.Amount;
                    map["Message"] = "You paid " + (toAccount == null ? "" : toAccount.Name) + " $" + amount;
                    map["TransactionID"] = transactionID;
                    m_syncMessagePoster.Post(fromUserInfo.CurrentRegionURI, map);
                }
            }
            return true;
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
                            new QueryFilter() { andFilters = new Dictionary<string, object> { { "PrincipalID", agent.PrincipalID } } }
                            , null, null);
            else
                m_gd.Update(_REALM,
                            new Dictionary<string, object>
                            {
                                {"LandInUse", agent.LandInUse}, 
                                {"Tier", agent.Tier},
                                {"IsGroup", agent.IsGroup}
                            }, null,
                            new QueryFilter() { andFilters = new Dictionary<string, object> { { "PrincipalID", agent.PrincipalID } } }
                            , null, null);
        }

        private void UserCurrencyCreate(UUID agentId)
        {
            m_gd.Insert(_REALM, new object[] { agentId.ToString(), 0, 0, 0, 0, 0 });
        }

        private void GroupCurrencyCreate(UUID groupID)
        {
            m_gd.Insert(_REALM, new object[] { groupID.ToString(), 0, 0, 0, 1, 0 });
        }

        #endregion

        #region Console Methods

        public void AddMoney(string[] cmd)
        {
            string name = MainConsole.Instance.Prompt("User Name: ");
            uint amount = 0;
            while (!uint.TryParse(MainConsole.Instance.Prompt("Amount: ", "0"), out amount))
                MainConsole.Instance.Info("Bad input, must be a number > 0");

            UserAccount account = m_registry.RequestModuleInterface<IUserAccountService>().GetUserAccount(new List<UUID> { UUID.Zero }, name);
            if (account == null)
            {
                MainConsole.Instance.Info("No account found");
                return;
            }
            var currency = GetUserCurrency(account.PrincipalID);
            m_gd.Update("stardust_currency",
                        new Dictionary<string, object> { 
                        {
                            "Amount", currency.Amount + amount }
                        }, null, new QueryFilter()
                        {
                            andFilters = new Dictionary<string, object> { { "PrincipalID", account.PrincipalID } }
                        }, null, null);
            MainConsole.Instance.Info(account.Name + " now has $" + (currency.Amount + amount));
        }

        public void SetMoney(string[] cmd)
        {
            string name = MainConsole.Instance.Prompt("User Name: ");
            uint amount = 0;
            while (!uint.TryParse(MainConsole.Instance.Prompt("Set User's Money Amount: ", "0"), out amount))
                MainConsole.Instance.Info("Bad input, must be a number > 0");

            UserAccount account = m_registry.RequestModuleInterface<IUserAccountService>().GetUserAccount(new List<UUID> { UUID.Zero }, name);
            if (account == null)
            {
                MainConsole.Instance.Info("No account found");
                return;
            }
            var currency = GetUserCurrency(account.PrincipalID);
            m_gd.Update(_REALM,
                        new Dictionary<string, object> { 
                        {
                            "Amount", amount }
                        }, null, new QueryFilter()
                        {
                            andFilters = new Dictionary<string, object> { { "PrincipalID", account.PrincipalID } }
                        }, null, null);
            MainConsole.Instance.Info(account.Name + " now has $" + amount);
        }

        public void GetMoney(string[] cmd)
        {
            string name = MainConsole.Instance.Prompt("User Name: ");
            uint amount = 0;
            while (!uint.TryParse(MainConsole.Instance.Prompt("Set User's Money Amount: ", "0"), out amount))
                MainConsole.Instance.Info("Bad input, must be a number > 0");

            UserAccount account = m_registry.RequestModuleInterface<IUserAccountService>().GetUserAccount(new List<UUID> { UUID.Zero }, name);
            if (account == null)
            {
                MainConsole.Instance.Info("No account found");
                return;
            }
            var currency = GetUserCurrency(account.PrincipalID);
            MainConsole.Instance.Info(account.Name + " has $" + currency.Amount);
        }

        #endregion
    }
}
