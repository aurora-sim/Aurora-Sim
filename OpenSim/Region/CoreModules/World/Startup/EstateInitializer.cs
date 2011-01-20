using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using Nini.Config;
using log4net;
using Aurora.DataManager;
using Aurora.Framework;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Region.CoreModules
{
    public class EstateInitializer : ISharedRegionStartupModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private string LastEstateName = "";
        private string LastEstateChoise = "no";

        public void Initialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            IEstateConnector EstateConnector = DataManager.RequestPlugin<IEstateConnector>();
            if (EstateConnector != null)
            {
                EstateSettings ES = EstateConnector.LoadEstateSettings(scene.RegionInfo.RegionID);
                if (ES != null && ES.EstateID == 0) // No record at all, new estate required
                {
                    m_log.Warn("Your region " + scene.RegionInfo.RegionName + " is not part of an estate.");
                    ES = CreateEstateInfo(scene);
                }
                else if (ES == null) //Cannot connect to the estate service
                {
                    m_log.Warn("The connection to the estate service was broken, please try again soon.");
                    while (true)
                    {
                        MainConsole.Instance.CmdPrompt("Press enter to try again.");
                        ES = EstateConnector.LoadEstateSettings(scene.RegionInfo.RegionID);
                        if (ES != null && ES.EstateID == 0)
                            ES = CreateEstateInfo(scene);
                        else if (ES == null)
                            continue;
                        break;
                    }
                }
                //Get the password from the database now that we have either created a new estate and saved it, joined a new estate, or just reloaded
                IGenericsConnector g = DataManager.RequestPlugin<IGenericsConnector>();
                EstatePassword s = null;
                if (g != null)
                    s = g.GetGeneric<EstatePassword>(scene.RegionInfo.RegionID, "EstatePassword", ES.EstateID.ToString(), new EstatePassword());
                if (s != null)
                    ES.EstatePass = s.Password;

                scene.RegionInfo.EstateSettings = ES;
            }
        }

        private EstateSettings CreateEstateInfo(Scene scene)
        {
            EstateSettings ES = null;
            while (true)
            {
                IEstateConnector EstateConnector = DataManager.RequestPlugin<IEstateConnector>();
                string response = MainConsole.Instance.CmdPrompt("Do you wish to join an existing estate for " + scene.RegionInfo.RegionName + "? (Options are {yes, no, find})", LastEstateChoise, new List<string>() { "yes", "no", "find" });
                LastEstateChoise = response;
                if (response == "no")
                {
                    // Create a new estate
                    ES = new EstateSettings();
                    ES.EstateName = MainConsole.Instance.CmdPrompt("New estate name", scene.RegionInfo.EstateSettings.EstateName);

                    //Set to auto connect to this region next
                    LastEstateName = ES.EstateName;
                    LastEstateChoise = "yes";

                    string Password = Util.Md5Hash(Util.Md5Hash(MainConsole.Instance.CmdPrompt("New estate password (to keep others from joining your estate, blank to have no pass)", ES.EstatePass)));
                    ES.EstatePass = Password;

                    ES = EstateConnector.CreateEstate(ES, scene.RegionInfo.RegionID);
                    if (ES == null)
                    {
                        m_log.Warn("The connection to the server was broken, please try again soon.");
                        continue;
                    }
                    else if (ES.EstateID == 0)
                    {
                        m_log.Warn("There was an error in creating this estate: " + ES.EstateName); //EstateName holds the error. See LocalEstateConnector for more info.
                        continue;
                    }
                    //We set this back if there wasn't an error because the EstateService will NOT send it back
                    IGenericsConnector g = DataManager.RequestPlugin<IGenericsConnector>();
                    EstatePassword s = new EstatePassword() { Password = Password };
                    if (g != null) //Save the pass to the database
                    {
                        g.AddGeneric(scene.RegionInfo.RegionID, "EstatePassword", ES.EstateID.ToString(), s.ToOSD());
                    }
                    break;
                }
                else if (response == "yes")
                {
                    response = MainConsole.Instance.CmdPrompt("Estate name to join", LastEstateName);
                    if (response == "None")
                        continue;
                    LastEstateName = response;

                    List<int> estateIDs = EstateConnector.GetEstates(response);
                    if (estateIDs == null)
                    {
                        m_log.Warn("The connection to the server was broken, please try again soon.");
                        continue;
                    }
                    if (estateIDs.Count < 1)
                    {
                        m_log.Warn("The name you have entered matches no known estate. Please try again");
                        continue;
                    }

                    int estateID = estateIDs[0];

                    string Password = Util.Md5Hash(Util.Md5Hash(MainConsole.Instance.CmdPrompt("Password for the estate", "")));
                    //We save the Password because we have to reset it after we tell the EstateService about it, as it clears it for security reasons
                    if (EstateConnector.LinkRegion(scene.RegionInfo.RegionID, estateID, Password))
                    {
                        ES = EstateConnector.LoadEstateSettings(scene.RegionInfo.RegionID); //We could do by EstateID now, but we need to completely make sure that it fully is set up
                        if (ES == null)
                        {
                            m_log.Warn("The connection to the server was broken, please try again soon.");
                            continue;
                        }
                        //Reset the pass and save it to the database
                        IGenericsConnector g = DataManager.RequestPlugin<IGenericsConnector>();
                        EstatePassword s = new EstatePassword() { Password = Password };
                        if (g != null) //Save the pass to the database
                        {
                            g.AddGeneric(scene.RegionInfo.RegionID, "EstatePassword", ES.EstateID.ToString(), s.ToOSD());
                        }
                        break;
                    }

                    m_log.Warn("Joining the estate failed. Please try again.");
                    continue;
                }
                else if (response == "find")
                {
                    ES = EstateConnector.LoadEstateSettings(scene.RegionInfo.RegionID);
                    if (ES == null)
                    {
                        m_log.Warn("The connection to the estate service was broken, please try again soon.");
                        continue;
                    }
                    break;
                }
            }
            return ES;
        }

        public void FinishStartup(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            //Now make sure we have an owner and that the owner's account exists on the grid
            UserAccount account = scene.UserAccountService.GetUserAccount(scene.RegionInfo.ScopeID, scene.RegionInfo.EstateSettings.EstateOwner);
            while (scene.RegionInfo.EstateSettings.EstateOwner == UUID.Zero && MainConsole.Instance != null)
            {
                MainConsole.Instance.Output("The current estate " + scene.RegionInfo.EstateSettings.EstateName + " has no owner set.");
                List<char> excluded = new List<char>(new char[1] { ' ' });
                string first = MainConsole.Instance.CmdPrompt("Estate owner first name", "Test", excluded);
                string last = MainConsole.Instance.CmdPrompt("Estate owner last name", "User", excluded);

                account = scene.UserAccountService.GetUserAccount(scene.RegionInfo.ScopeID, first, last);

                if (account == null)
                {
                    string name = first + " " + last;
                    string createNewUser = MainConsole.Instance.CmdPrompt("Could not find user " + name + ". Would you like to create this user?", "yes");

                    if (createNewUser == "yes")
                    {
                        // Create a new account
                        account = new UserAccount(scene.RegionInfo.ScopeID, first, last, String.Empty);
                        if (account.ServiceURLs == null || (account.ServiceURLs != null && account.ServiceURLs.Count == 0))
                        {
                            account.ServiceURLs = new Dictionary<string, object>();
                            account.ServiceURLs["HomeURI"] = string.Empty;
                            account.ServiceURLs["GatekeeperURI"] = string.Empty;
                            account.ServiceURLs["InventoryServerURI"] = string.Empty;
                            account.ServiceURLs["AssetServerURI"] = string.Empty;
                        }
                        account.UserTitle = "";

                        if (scene.UserAccountService.StoreUserAccount(account))
                        {
                            string password = MainConsole.Instance.PasswdPrompt(name + "'s password ");
                            string email = MainConsole.Instance.CmdPrompt(name + "'s email", "");

                            account.Email = email;
                            scene.UserAccountService.StoreUserAccount(account);

                            bool success = false;
                            success = scene.AuthenticationService.SetPassword(account.PrincipalID, password);
                            if (!success)
                            {
                                m_log.WarnFormat("[USER ACCOUNT SERVICE]: Unable to set password for account {0} {1}.",
                                   first, last);
                            }

                            GridRegion home = null;
                            if (scene.GridService != null)
                            {
                                List<GridRegion> defaultRegions = scene.GridService.GetDefaultRegions(UUID.Zero);
                                if (defaultRegions != null && defaultRegions.Count >= 1)
                                    home = defaultRegions[0];

                                if (scene.GridUserService != null && home != null)
                                    scene.GridUserService.SetHome(account.PrincipalID.ToString(), home.RegionID, new Vector3(128, 128, 0), new Vector3(0, 1, 0));
                                else
                                    m_log.WarnFormat("[USER ACCOUNT SERVICE]: Unable to set home for account {0} {1}.",
                                       first, last);

                            }
                            else
                                m_log.WarnFormat("[USER ACCOUNT SERVICE]: Unable to retrieve home region for account {0} {1}.",
                                   first, last);

                            if (scene.InventoryService != null)
                                success = scene.InventoryService.CreateUserInventory(account.PrincipalID);
                            if (!success)
                                m_log.WarnFormat("[USER ACCOUNT SERVICE]: Unable to create inventory for account {0} {1}.",
                                   first, last);


                            m_log.InfoFormat("[USER ACCOUNT SERVICE]: Account {0} {1} created successfully", first, last);

                            scene.RegionInfo.EstateSettings.EstateOwner = account.PrincipalID;
                            scene.RegionInfo.EstateSettings.Save();
                        }
                        else
                            m_log.ErrorFormat("[SCENE]: Unable to store account. If this simulator is connected to a grid, you must create the estate owner account first.");
                    }
                }
                else
                {
                    scene.RegionInfo.EstateSettings.EstateOwner = account.PrincipalID;
                    scene.RegionInfo.EstateSettings.Save();
                }
            }
            while (account == null)
            {
                MainConsole.Instance.Output("The current estate " + scene.RegionInfo.EstateSettings.EstateName + " has no owner that exists in this grid set.");
                List<char> excluded = new List<char>(new char[1] { ' ' });
                string first = MainConsole.Instance.CmdPrompt("Estate owner first name", "Test", excluded);
                string last = MainConsole.Instance.CmdPrompt("Estate owner last name", "User", excluded);
                account = scene.UserAccountService.GetUserAccount(scene.RegionInfo.ScopeID, first, last);
                if (account == null)
                {
                    string name = first + " " + last;
                    string createNewUser = MainConsole.Instance.CmdPrompt("Could not find user " + name + ". Would you like to create this user?", "yes");

                    if (createNewUser == "yes")
                    {
                        // Create a new account
                        account = new UserAccount(scene.RegionInfo.ScopeID, first, last, String.Empty);
                        if (account.ServiceURLs == null || (account.ServiceURLs != null && account.ServiceURLs.Count == 0))
                        {
                            account.ServiceURLs = new Dictionary<string, object>();
                            account.ServiceURLs["HomeURI"] = string.Empty;
                            account.ServiceURLs["GatekeeperURI"] = string.Empty;
                            account.ServiceURLs["InventoryServerURI"] = string.Empty;
                            account.ServiceURLs["AssetServerURI"] = string.Empty;
                        }
                        account.UserTitle = "";

                        if (scene.UserAccountService.StoreUserAccount(account))
                        {
                            string password = MainConsole.Instance.PasswdPrompt(name + "'s password ");
                            string email = MainConsole.Instance.CmdPrompt(name + "'s email", "");

                            account.Email = email;
                            scene.UserAccountService.StoreUserAccount(account);

                            bool success = false;
                            success = scene.AuthenticationService.SetPassword(account.PrincipalID, password);
                            if (!success)
                            {
                                m_log.WarnFormat("[USER ACCOUNT SERVICE]: Unable to set password for account {0} {1}.",
                                   first, last);
                            }

                            GridRegion home = null;
                            if (scene.GridService != null)
                            {
                                List<GridRegion> defaultRegions = scene.GridService.GetDefaultRegions(UUID.Zero);
                                if (defaultRegions != null && defaultRegions.Count >= 1)
                                    home = defaultRegions[0];

                                if (scene.GridUserService != null && home != null)
                                    scene.GridUserService.SetHome(account.PrincipalID.ToString(), home.RegionID, new Vector3(128, 128, 0), new Vector3(0, 1, 0));
                                else
                                    m_log.WarnFormat("[USER ACCOUNT SERVICE]: Unable to set home for account {0} {1}.",
                                       first, last);

                            }
                            else
                                m_log.WarnFormat("[USER ACCOUNT SERVICE]: Unable to retrieve home region for account {0} {1}.",
                                   first, last);

                            if (scene.InventoryService != null)
                                success = scene.InventoryService.CreateUserInventory(account.PrincipalID);
                            if (!success)
                                m_log.WarnFormat("[USER ACCOUNT SERVICE]: Unable to create inventory for account {0} {1}.",
                                   first, last);


                            m_log.InfoFormat("[USER ACCOUNT SERVICE]: Account {0} {1} created successfully", first, last);

                            scene.RegionInfo.EstateSettings.EstateOwner = account.PrincipalID;
                            scene.RegionInfo.EstateSettings.Save();
                        }
                        else
                        {
                            account = null;
                            m_log.ErrorFormat("[SCENE]: Unable to store account. If this simulator is connected to a grid, you must create the estate owner account first.");
                        }
                    }
                }
                else
                {
                    //Set the new user
                    scene.RegionInfo.EstateSettings.EstateOwner = account.PrincipalID;
                    scene.RegionInfo.EstateSettings.Save();
                }
            }
        }

        public void PostInitialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void StartupComplete()
        {
        }

        public void Close(Scene scene)
        {
        }

        /// <summary>
        /// This class is used to save the EstatePassword for the given region/estate service
        /// </summary>
        public class EstatePassword : IDataTransferable
        {
            public string Password;
            public override void FromOSD(OSDMap map)
            {
                Password = map["Password"].AsString();
            }

            public override OSDMap ToOSD()
            {
                OSDMap map = new OSDMap();
                map.Add("Password", Password);
                return map;
            }

            public override Dictionary<string, object> ToKeyValuePairs()
            {
                return Util.OSDToDictionary(ToOSD());
            }

            public override void FromKVP(Dictionary<string, object> KVP)
            {
                FromOSD(Util.DictionaryToOSD(KVP));
            }

            public override IDataTransferable Duplicate()
            {
                EstatePassword m = new EstatePassword();
                m.FromOSD(ToOSD());
                return m;
            }
        }
    }
}
