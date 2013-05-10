using Aurora.Framework;
using Aurora.Framework.Modules;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;

namespace Aurora.Modules.Avatar.Currency
{
    public class ScheduledCurrencyTransferModule : IService, IScheduledMoneyModule
    {
        #region Declares

        public IRegistryCore m_registry;

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
            IMoneyModule moneyModule = m_registry.RequestModuleInterface<IMoneyModule>();
            if (moneyModule != null) //Only register if money is enabled
            {
                m_registry.RegisterModuleInterface<IScheduledMoneyModule>(this);
                m_registry.RequestModuleInterface<ISimulationBase>()
                          .EventManager.RegisterEventHandler("ScheduledPayment", ChargeNext);
            }
        }

        #endregion

        #region IScheduledMoneyModule Members

        public event UserDidNotPay OnUserDidNotPay;
        public event CheckWhetherUserShouldPay OnCheckWhetherUserShouldPay;

        public bool Charge(UUID agentID, int amount, string text, int daysUntilNextCharge, TransactionType type, string identifer, bool chargeImmediately)
        {
            IMoneyModule moneyModule = m_registry.RequestModuleInterface<IMoneyModule>();
            if (moneyModule != null)
            {
                if (chargeImmediately)
                {
                    bool success = moneyModule.Charge(agentID, amount, text, type);
                    if (!success)
                        return false;
                }
                IScheduleService scheduler = m_registry.RequestModuleInterface<IScheduleService>();
                if (scheduler != null)
                {
                    OSDMap itemInfo = new OSDMap();
                    itemInfo.Add("AgentID", agentID);
                    itemInfo.Add("Amount", amount);
                    itemInfo.Add("Text", text);
                    itemInfo.Add("Type", (int)type);
                    SchedulerItem item = new SchedulerItem("ScheduledPayment" + identifer,
                                                           OSDParser.SerializeJsonString(itemInfo), false,
                                                           DateTime.UtcNow, daysUntilNextCharge, RepeatType.days, agentID);
                    itemInfo.Add("SchedulerID", item.id);
                    scheduler.Save(item);
                }
            }
            return true;
        }

        public void RemoveFromScheduledCharge(string identifer)
        {
            IScheduleService scheduler = m_registry.RequestModuleInterface<IScheduleService>();
            if (scheduler != null)
                scheduler.Remove("ScheduledPayment" + identifer);
        }

        private object ChargeNext(string functionName, object parameters)
        {
            if (functionName.StartsWith("ScheduledPayment"))
            {
                OSDMap itemInfo = (OSDMap) OSDParser.DeserializeJson(parameters.ToString());
                IMoneyModule moneyModule = m_registry.RequestModuleInterface<IMoneyModule>();
                UUID agentID = itemInfo["AgentID"];
                string scdID = itemInfo["SchedulerID"];
                string text = itemInfo["Text"];
                int amount = itemInfo["Amount"];
                TransactionType type = !itemInfo.ContainsKey("Type") ? TransactionType.SystemGenerated : (TransactionType)itemInfo["Type"].AsInteger();
                if (CheckWhetherUserShouldPay(agentID, text))
                {
                    bool success = moneyModule.Charge(agentID, amount, text, type);
                    if (!success)
                    {
                        if (OnUserDidNotPay != null)
                            OnUserDidNotPay(agentID, functionName.Replace("ScheduledPayment", ""), text);
                    }
                }
                else
                {
                    IScheduleService scheduler = m_registry.RequestModuleInterface<IScheduleService>();
                    if (scheduler != null)
                        scheduler.Remove(scdID);
                }
            }
            return null;
        }

        private bool CheckWhetherUserShouldPay(UUID agentID, string text)
        {
            if (OnCheckWhetherUserShouldPay == null)
                return true;
            foreach (CheckWhetherUserShouldPay d in OnCheckWhetherUserShouldPay.GetInvocationList())
            {
                if (!d(agentID, text))
                    return false;
            }
            return true;
        }

        #endregion
    }
}