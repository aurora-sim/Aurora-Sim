using Aurora.Framework;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Simple.Currency
{
    public class SimpleCurrencyConfig : IDataTransferable
    {
        #region declarations

        private uint m_priceUpload = 0;
        private uint m_priceGroupCreate = 0;
        private int m_stipend = 0;
        private string m_upgradeMembershipUri = "";

        private string m_errorURI = "";

        private bool m_CanBuyCurrencyInworld = true;
        private bool m_GiveStipends = false;
        private string m_stipendsEveryType = "month";
        private bool m_stipendsPremiumOnly = false;
        private int m_StipendsEvery = 1;
        private uint m_clientPort = 8002;
        private bool m_StipendsLoadOldUsers = true;
        private bool m_GiveStipendsOnlyWhenLoggedIn = false;
        private bool m_saveTransactionLogs = false;

        #endregion

        #region functions

        public SimpleCurrencyConfig(IConfig economyConfig)
        {
            foreach (PropertyInfo propertyInfo in GetType().GetProperties())
            {
                try
                {
                    if (propertyInfo.PropertyType.IsAssignableFrom(typeof (float)))
                        propertyInfo.SetValue(this,
                                              economyConfig.GetFloat(propertyInfo.Name,
                                                                     float.Parse(
                                                                         propertyInfo.GetValue(this, new object[0])
                                                                                     .ToString())), new object[0]);
                    else if (propertyInfo.PropertyType.IsAssignableFrom(typeof (int)))
                        propertyInfo.SetValue(this,
                                              economyConfig.GetInt(propertyInfo.Name,
                                                                   int.Parse(
                                                                       propertyInfo.GetValue(this, new object[0])
                                                                                   .ToString())), new object[0]);
                    else if (propertyInfo.PropertyType.IsAssignableFrom(typeof (bool)))
                        propertyInfo.SetValue(this,
                                              economyConfig.GetBoolean(propertyInfo.Name,
                                                                       bool.Parse(
                                                                           propertyInfo.GetValue(this, new object[0])
                                                                                       .ToString())), new object[0]);
                    else if (propertyInfo.PropertyType.IsAssignableFrom(typeof (string)))
                        propertyInfo.SetValue(this,
                                              economyConfig.GetString(propertyInfo.Name,
                                                                      propertyInfo.GetValue(this, new object[0])
                                                                                  .ToString()), new object[0]);
                    else if (propertyInfo.PropertyType.IsAssignableFrom(typeof (UUID)))
                        propertyInfo.SetValue(this,
                                              new UUID(economyConfig.GetString(propertyInfo.Name,
                                                                               propertyInfo.GetValue(this, new object[0])
                                                                                           .ToString())), new object[0]);
                }
                catch (Exception)
                {
                    MainConsole.Instance.Warn("[SimpleCurrency]: Exception reading economy config: " + propertyInfo.Name);
                }
            }
        }

        public SimpleCurrencyConfig()
        {
        }

        public SimpleCurrencyConfig(OSDMap values)
        {
            FromOSD(values);
        }

        public override OSDMap ToOSD()
        {
            OSDMap returnvalue = new OSDMap();
            foreach (PropertyInfo propertyInfo in GetType().GetProperties())
            {
                try
                {
                    if (propertyInfo.PropertyType.IsAssignableFrom(typeof (float)))
                        returnvalue.Add(propertyInfo.Name, (float) propertyInfo.GetValue(this, new object[0]));
                    else if (propertyInfo.PropertyType.IsAssignableFrom(typeof (int)))
                        returnvalue.Add(propertyInfo.Name, (int) propertyInfo.GetValue(this, new object[0]));
                    else if (propertyInfo.PropertyType.IsAssignableFrom(typeof (bool)))
                        returnvalue.Add(propertyInfo.Name, (bool) propertyInfo.GetValue(this, new object[0]));
                    else if (propertyInfo.PropertyType.IsAssignableFrom(typeof (string)))
                        returnvalue.Add(propertyInfo.Name, (string) propertyInfo.GetValue(this, new object[0]));
                    else if (propertyInfo.PropertyType.IsAssignableFrom(typeof (UUID)))
                        returnvalue.Add(propertyInfo.Name, (UUID) propertyInfo.GetValue(this, new object[0]));
                }
                catch (Exception ex)
                {
                    MainConsole.Instance.Warn("[SimpleCurrency]: Exception toOSD() config: " + ex.ToString());
                }
            }
            return returnvalue;
        }

        public override sealed void FromOSD(OSDMap values)
        {
            foreach (PropertyInfo propertyInfo in GetType().GetProperties())
            {
                if (values.ContainsKey(propertyInfo.Name))
                {
                    try
                    {
                        if (propertyInfo.PropertyType.IsAssignableFrom(typeof (float)))
                            propertyInfo.SetValue(this, float.Parse(values[propertyInfo.Name].AsString()), new object[0]);
                        else if (propertyInfo.PropertyType.IsAssignableFrom(typeof (int)))
                            propertyInfo.SetValue(this, values[propertyInfo.Name].AsInteger(), new object[0]);
                        else if (propertyInfo.PropertyType.IsAssignableFrom(typeof (bool)))
                            propertyInfo.SetValue(this, values[propertyInfo.Name].AsBoolean(), new object[0]);
                        else if (propertyInfo.PropertyType.IsAssignableFrom(typeof (string)))
                            propertyInfo.SetValue(this, values[propertyInfo.Name].AsString(), new object[0]);
                        else if (propertyInfo.PropertyType.IsAssignableFrom(typeof (UUID)))
                            propertyInfo.SetValue(this, values[propertyInfo.Name].AsUUID(), new object[0]);
                    }
                    catch (Exception ex)
                    {
                        MainConsole.Instance.Warn("[SimpleCurrency]: Exception reading fromOSD() config: " +
                                                  ex.ToString());
                    }
                }
            }
        }

        #endregion

        #region properties

        public string ErrorURI
        {
            get { return m_errorURI; }
            set { m_errorURI = value; }
        }

        public string UpgradeMembershipUri
        {
            get { return m_upgradeMembershipUri; }
            set { m_upgradeMembershipUri = value; }
        }

        public int Stipend
        {
            get { return m_stipend; }
            set { m_stipend = value; }
        }

        public bool GiveStipends
        {
            get { return m_GiveStipends; }
            set { m_GiveStipends = value; }
        }

        public string StipendsEveryType
        {
            get { return m_stipendsEveryType; }
            set { m_stipendsEveryType = value; }
        }

        public int StipendsEvery
        {
            get { return m_StipendsEvery; }
            set { m_StipendsEvery = value; }
        }

        public int PriceGroupCreate
        {
            get { return (int) m_priceGroupCreate; }
            set { m_priceGroupCreate = (uint) value; }
        }

        public int PriceUpload
        {
            get { return (int) m_priceUpload; }
            set { m_priceUpload = (uint) value; }
        }

        public bool StipendsPremiumOnly
        {
            get { return m_stipendsPremiumOnly; }
            set { m_stipendsPremiumOnly = value; }
        }

        public int ClientPort
        {
            get { return (int) m_clientPort; }
            set { m_clientPort = (uint) value; }
        }

        public bool CanBuyCurrencyInworld
        {
            get { return m_CanBuyCurrencyInworld; }
            set { m_CanBuyCurrencyInworld = value; }
        }

        public bool StipendsLoadOldUsers
        {
            get { return m_StipendsLoadOldUsers; }
            set { m_StipendsLoadOldUsers = value; }
        }

        public bool GiveStipendsOnlyWhenLoggedIn
        {
            get { return m_GiveStipendsOnlyWhenLoggedIn; }
            set { m_GiveStipendsOnlyWhenLoggedIn = value; }
        }

        public bool SaveTransactionLogs
        {
            get { return m_saveTransactionLogs; }
            set { m_saveTransactionLogs = value; }
        }

        #endregion
    }

    public class UserCurrency : IDataTransferable
    {
        public UUID PrincipalID;
        public uint Amount;
        public uint LandInUse;
        public uint Tier;
        public bool IsGroup;
        public uint StipendsBalance;

        /// <summary>
        /// </summary>
        /// <param name="osdMap"></param>
        public UserCurrency(OSDMap osdMap)
        {
            if (osdMap != null)
                FromOSD(osdMap);
        }

        public UserCurrency(List<string> queryResults)
        {
           FromArray(queryResults);
        }

        public UserCurrency(UUID agentID, uint balance, uint landuse, uint tier_bal, bool group_toggle, uint stipend_bal)
        {
            PrincipalID = agentID;
            Amount = landuse;
            LandInUse = landuse;
            Tier = tier_bal;
            IsGroup = group_toggle;
            StipendsBalance = stipend_bal;
        }

        /// <summary></summary>
        /// <param name="osdMap"></param>
        public override sealed void FromOSD(OSDMap osdMap)
        {
            UUID.TryParse(osdMap["PrincipalID"].AsString(), out PrincipalID);
            uint.TryParse(osdMap["Amount"].AsString(), out Amount);
            uint.TryParse(osdMap["LandInUse"].AsString(), out LandInUse);
            uint.TryParse(osdMap["Tier"].AsString(), out Tier);
            bool.TryParse(osdMap["IsGroup"].AsString(), out IsGroup);
            uint.TryParse(osdMap["StipendsBalance"].AsString(), out StipendsBalance);
        }

        public bool FromArray(List<string> queryResults)
        {
            return UUID.TryParse(queryResults[0], out PrincipalID) &&
                   uint.TryParse(queryResults[1], out Amount) &&
                   uint.TryParse(queryResults[2], out LandInUse) &&
                   uint.TryParse(queryResults[3], out Tier) &&
                   bool.TryParse(queryResults[4], out IsGroup) &&
                   uint.TryParse(queryResults[5], out StipendsBalance);
        }

        /// <summary></summary>
        /// <returns></returns>
        public override OSDMap ToOSD()
        {
            return
                new OSDMap
                    {
                        {"PrincipalID", PrincipalID},
                        {"Amount", Amount},
                        {"LandInUse", LandInUse},
                        {"Tier", Tier},
                        {"IsGroup", IsGroup},
                        {"StipendsBalance", StipendsBalance}
                    };
        }
    }
}