using System;
using System.Collections;
using System.Net;
using System.Reflection;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using Nini.Config;
using Nwc.XmlRpc;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Services.Interfaces;
using System.Threading;

namespace Simple.Currency
{
    public class RPCHandler : IService
    {
        #region Declares

        private ISimpleCurrencyConnector m_connector;
        private ISyncMessagePosterService m_syncMessagePoster;
        private IAgentInfoService m_agentInfoService;

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
            if (config.Configs["Currency"] != null && config.Configs["Currency"].GetString("Module", "") != "SimpleCurrency")
                return;
            if (!config.Configs["Currency"].GetBoolean("RunServer", false))
                return;

            m_connector = Aurora.DataManager.DataManager.RequestPlugin<ISimpleCurrencyConnector>();
            IHttpServer server = registry.RequestModuleInterface<ISimulationBase>().GetHttpServer((uint)m_connector.GetConfig().ClientPort);
            server.AddXmlRPCHandler("getCurrencyQuote", QuoteFunc);
            server.AddXmlRPCHandler("buyCurrency", BuyFunc);
            server.AddXmlRPCHandler("preflightBuyLandPrep", PreflightBuyLandPrepFunc);
            server.AddXmlRPCHandler("buyLandPrep", LandBuyFunc);
            server.AddXmlRPCHandler("getBalance", GetbalanceFunc);
            server.AddXmlRPCHandler("/currency.php", GetbalanceFunc);
            server.AddXmlRPCHandler("/landtool.php", GetbalanceFunc);

            m_syncMessagePoster = registry.RequestModuleInterface<ISyncMessagePosterService>();
            m_agentInfoService = registry.RequestModuleInterface<IAgentInfoService>();
        }

        public void FinishedStartup()
        {
        }

        #endregion

        #region RPC Calls

        public XmlRpcResponse GetbalanceFunc(XmlRpcRequest request, IPEndPoint ep)
        {
            MainConsole.Instance.Error("Remote procdure calls GetbalanceFunc was called.");
            throw new NotImplementedException();
        }
        public XmlRpcResponse LandBuyFunc(XmlRpcRequest request, IPEndPoint ep)
        {
            Hashtable requestData = (Hashtable)request.Params[0];

            bool success = false;
            if (requestData.ContainsKey("agentId") && requestData.ContainsKey("currencyBuy") && m_connector.GetConfig().CanBuyCurrencyInworld)
            {
                UUID agentId;
                if (UUID.TryParse((string)requestData["agentId"], out agentId))
                {
                    uint amountBuying = uint.Parse(requestData["currencyBuy"].ToString());
                    m_connector.UserCurrencyTransfer(agentId, UUID.Zero, UUID.Zero, UUID.Zero, amountBuying, "Inworld purchase", TransactionType.SystemGenerated, UUID.Zero);
                    success = true;
                }
            }
            XmlRpcResponse returnval = new XmlRpcResponse();
            Hashtable returnresp = new Hashtable { { "success", success } };
            returnval.Value = returnresp;
            return returnval;
        }

        public XmlRpcResponse QuoteFunc(XmlRpcRequest request, IPEndPoint ep)
        {
            Hashtable requestData = (Hashtable)request.Params[0];

            XmlRpcResponse returnval = new XmlRpcResponse();

            if (requestData.ContainsKey("agentId") && requestData.ContainsKey("currencyBuy"))
            {
                if (m_connector.GetConfig().CanBuyCurrencyInworld)
                {
                    uint amount = uint.Parse(requestData["currencyBuy"].ToString());
                    returnval.Value = new Hashtable
                                          {
                                              {"success", true},
                                              {
                                                  "currency",
                                                  new Hashtable
                                                      {
                                                          { "estimatedCost",  0 },
                                                          {"currencyBuy", (int) amount}
                                                      }
                                                  },
                                              {"confirm", "asdfad9fj39ma9fj"}
                                          };
                }
                else
                {
                    returnval.Value = new Hashtable
                                          {
                                              {"success", false},
                                              {
                                                  "currency",
                                                  new Hashtable
                                                      {
                                                          { "estimatedCost",  0 },
                                                          {"currencyBuy", 0 }
                                                      }
                                                  },
                                              {"confirm", "asdfad9fj39ma9fj"}
                                          };
                }

                return returnval;

            }
            returnval.Value = new Hashtable
            {
                {"success", false},
                {"errorMessage", "Invalid parameters passed to the quote box"},
                {"errorURI", m_connector.GetConfig().ErrorURI}
            };
            return returnval;
        }

        public XmlRpcResponse BuyFunc(XmlRpcRequest request, IPEndPoint ep)
        {
            Hashtable requestData = (Hashtable)request.Params[0];
            bool success = false;
            if (requestData.ContainsKey("agentId") && requestData.ContainsKey("currencyBuy") && m_connector.GetConfig().CanBuyCurrencyInworld)
            {
                UUID agentId;
                if (UUID.TryParse((string)requestData["agentId"], out agentId))
                {
                    uint amountBuying = uint.Parse(requestData["currencyBuy"].ToString());
                    m_connector.UserCurrencyTransfer(agentId, UUID.Zero, UUID.Zero, UUID.Zero, amountBuying, "Inworld purchase", TransactionType.SystemGenerated, UUID.Zero);
                    success = true;
                }
            }
            XmlRpcResponse returnval = new XmlRpcResponse();
            Hashtable returnresp = new Hashtable { { "success", success } };
            returnval.Value = returnresp;
            return returnval;
        }

        public XmlRpcResponse PreflightBuyLandPrepFunc(XmlRpcRequest request, IPEndPoint ep)
        {
            Hashtable requestData = (Hashtable)request.Params[0];
            XmlRpcResponse ret = new XmlRpcResponse();
            Hashtable retparam = new Hashtable();

            Hashtable membershiplevels = new Hashtable();
            membershiplevels.Add("levels", membershiplevels);

            Hashtable landuse = new Hashtable();

            Hashtable level = new Hashtable
                {
                    {"id", "00000000-0000-0000-0000-000000000000"},
                    {m_connector.GetConfig().UpgradeMembershipUri, "Premium Membership"}
                };

            if (requestData.ContainsKey("agentId") && requestData.ContainsKey("currencyBuy"))
            {
                UUID agentId;
                UUID.TryParse((string)requestData["agentId"], out agentId);
                UserCurrency currency = m_connector.GetUserCurrency(agentId);
                IUserProfileInfo profile = DataManager.RequestPlugin<IProfileConnector>("IProfileConnector").GetUserProfile(agentId);


                //IClientCapsService client = m_dustCurrencyService.Registry.RequestModuleInterface<ICapsService>().GetClientCapsService(agentId);
                OSDMap replyData = null;
                bool response = false;
                UserInfo user = m_agentInfoService.GetUserInfo(agentId.ToString());
                if(user == null)
                {
                    landuse.Add("action", false);

                    retparam.Add("success", false);
                    retparam.Add("currency", currency);
                    retparam.Add("membership", level);
                    retparam.Add("landuse", landuse);
                    retparam.Add("confirm", "asdfajsdkfjasdkfjalsdfjasdf");
                    ret.Value = retparam;
                }
                else
                {
                    OSDMap map = new OSDMap();
                    map["Method"] = "GetLandData";
                    map["AgentID"] = agentId;
                    m_syncMessagePoster.Get(user.CurrentRegionURI, map, (o)=> { replyData = o; response = true; });
                    while (!response)
                        Thread.Sleep(10);
                    if (replyData == null || replyData["Success"] == false)
                    {
                        landuse.Add("action", false);

                        retparam.Add("success", false);
                        retparam.Add("currency", currency);
                        retparam.Add("membership", level);
                        retparam.Add("landuse", landuse);
                        retparam.Add("confirm", "asdfajsdkfjasdkfjalsdfjasdf");
                        ret.Value = retparam;
                    }
                    else
                    {
                        //if (client != null)
                        //    m_dustCurrencyService.SendGridMessage(agentId, String.Format(m_dustCurrencyService.m_options.MessgeBeforeBuyLand, profile.DisplayName, replyData.ContainsKey("SalePrice")), false, UUID.Zero);
                        if (replyData.ContainsKey("SalePrice"))
                        {
                            // I think, this might be usable if they don't have the money
                            // Hashtable currencytable = new Hashtable { { "estimatedCost", replyData["SalePrice"].AsInteger() } };

                            int landTierNeeded = (int)(currency.LandInUse + replyData["Area"].AsInteger());
                            bool needsUpgrade = false;
                            switch (profile.MembershipGroup)
                            {
                                case "Premium":
                                case "":
                                    needsUpgrade = landTierNeeded >= currency.Tier;
                                    break;
                                case "Banned":
                                    needsUpgrade = true;
                                    break;
                            }
                            // landuse.Add("action", m_DustCurrencyService.m_options.upgradeMembershipUri);
                            landuse.Add("action", needsUpgrade);

                            retparam.Add("success", true);
                            retparam.Add("currency", currency);
                            retparam.Add("membership", level);
                            retparam.Add("landuse", landuse);
                            retparam.Add("confirm", "asdfajsdkfjasdkfjalsdfjasdf");
                            ret.Value = retparam;
                        }
                    }
                }
            }

            return ret;
        }
        #endregion
    }
}
