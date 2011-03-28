using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using OpenSim.Framework;
using OpenSim.Framework.Serialization;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using Nini.Config;
using log4net;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Simulation.Base;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Region.CoreModules
{
    public class EstateInitializer : ISharedRegionStartupModule, IAuroraBackupModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private string LastEstateName = "";
        private string LastEstateChoise = "no";
        private string LastEstateOwner = "Test User";

        public void Initialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            scene.StackModuleInterface<IAuroraBackupModule>(this);
        }

        private EstateSettings CreateEstateInfo(Scene scene)
        {
            EstateSettings ES = new EstateSettings();
            while (true)
            {
                IEstateConnector EstateConnector = DataManager.RequestPlugin<IEstateConnector>();

                string name = MainConsole.Instance.CmdPrompt("Estate owner name", LastEstateOwner);
                UserAccount account = scene.UserAccountService.GetUserAccount(scene.RegionInfo.ScopeID, name);

                if (account == null)
                {
                    string createNewUser = MainConsole.Instance.CmdPrompt("Could not find user " + name + ". Would you like to create this user?", "yes");

                    if (createNewUser == "yes")
                    {
                        // Create a new account
                        string password = MainConsole.Instance.PasswdPrompt(name + "'s password");
                        string email = MainConsole.Instance.CmdPrompt(name + "'s email", "");

                        scene.UserAccountService.CreateUser(name, Util.Md5Hash(password), email);
                        account = scene.UserAccountService.GetUserAccount(scene.RegionInfo.ScopeID, name);

                        if (account == null)
                        {
                            m_log.ErrorFormat("[EstateService]: Unable to store account. If this simulator is connected to a grid, you must create the estate owner account first.");
                            continue;
                        }
                    }
                    else
                        continue;
                }

                LastEstateOwner = account.Name;

                string response = "no";
                List<EstateSettings> ownerEstates = EstateConnector.GetEstates(account.PrincipalID);
                if (ownerEstates != null)
                {
                    m_log.WarnFormat("Found user. {0} has {1} estates currently. {2}", account.Name, ownerEstates.Count,
                        "These estates are the following:");
                    for (int i = 0; i < ownerEstates.Count; i++)
                    {
                        m_log.Warn(ownerEstates[i].EstateName);
                    }
                    response = MainConsole.Instance.CmdPrompt("Do you wish to join one of these existing estates? (Options are {yes, no})", LastEstateChoise, new List<string>() { "yes", "no" });
                }
                else
                {
                    m_log.WarnFormat("Found user. {0} has no estates currently. Creating a new estate.", account.Name);
                }
                LastEstateChoise = response;
                if (response == "no")
                {
                    // Create a new estate
                    ES.EstateName = MainConsole.Instance.CmdPrompt("New estate name", scene.RegionInfo.EstateSettings.EstateName);

                    //Set to auto connect to this region next
                    LastEstateName = ES.EstateName;
                    LastEstateChoise = "yes";

                    string Password = Util.Md5Hash(Util.Md5Hash(MainConsole.Instance.CmdPrompt("New estate password (to keep others from joining your estate, blank to have no pass)", ES.EstatePass)));
                    ES.EstatePass = Password;
                    ES.EstateOwner = account.PrincipalID;

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
                        if (EstateConnector.LoadEstateSettings(scene.RegionInfo.RegionID, out ES)) //We could do by EstateID now, but we need to completely make sure that it fully is set up
                        {
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
                        }
                        else
                        {
                            m_log.Warn("The connection to the server was broken, please try again soon.");
                            continue;
                        }
                        m_log.Warn("Successfully joined the estate!");
                        break;
                    }

                    m_log.Warn("Joining the estate failed. Please try again.");
                    continue;
                }
            }
            return ES;
        }

        public void PostInitialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void FinishStartup(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            IEstateConnector EstateConnector = DataManager.RequestPlugin<IEstateConnector>();
            if (EstateConnector != null)
            {
                EstateSettings ES;
                if (EstateConnector.LoadEstateSettings(scene.RegionInfo.RegionID, out ES) && ES == null)
                {
                    //It found the estate service, but found no estates for this region, make a new one
                    m_log.Warn("Your region " + scene.RegionInfo.RegionName + " is not part of an estate.");
                    ES = CreateEstateInfo(scene);
                }
                else if (ES != null)
                {
                    //It found the estate service and it found an estate for this region
                }
                else
                {
                    //It could not find the estate service, wait until it can find it
                    m_log.Warn("We could not find the estate service for this sim. Please make sure that your URLs are correct in grid mode.");
                    while (true)
                    {
                        MainConsole.Instance.CmdPrompt("Press enter to try again.");
                        if (EstateConnector.LoadEstateSettings(scene.RegionInfo.RegionID, out ES) && ES == null)
                        {
                            ES = CreateEstateInfo(scene);
                            break;
                        }
                        else if (ES != null)
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

        public void PostFinishStartup(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void StartupComplete()
        {
            MainConsole.Instance.Commands.AddCommand("Estates", true, "change estate", "change estate",
                "change info about the estate for the given region", ChangeEstate); 
        }

        public void Close(Scene scene)
        {
        }

        protected void ChangeEstate(string module, string[] cmd)
        {
            IEstateConnector EstateConnector = DataManager.RequestPlugin<IEstateConnector>();
            if (EstateConnector != null)
            {
                if (MainConsole.Instance.ConsoleScene == null)
                {
                    m_log.Warn("Select a region before using this command.");
                    return;
                }
                Scene scene = (Scene)MainConsole.Instance.ConsoleScene;
                string removeFromEstate = MainConsole.Instance.CmdPrompt("Are you sure you want to leave the estate for region " + scene.RegionInfo.RegionName + "?", "yes");
                if (removeFromEstate == "yes")
                {
                    if (!EstateConnector.DelinkRegion(scene.RegionInfo.RegionID, scene.RegionInfo.EstateSettings.EstatePass))
                    {
                        m_log.Warn("Unable to remove this region from the estate.");
                        return;
                    }
                    scene.RegionInfo.EstateSettings = CreateEstateInfo(scene);
                    IGenericsConnector g = DataManager.RequestPlugin<IGenericsConnector>();
                    EstatePassword s = null;
                    if (g != null)
                        s = g.GetGeneric<EstatePassword>(scene.RegionInfo.RegionID, "EstatePassword", scene.RegionInfo.EstateSettings.EstateID.ToString(), new EstatePassword());
                    if (s != null)
                        scene.RegionInfo.EstateSettings.EstatePass = s.Password;
                }
                else
                    m_log.Warn("No action has been taken.");
            }
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

        public bool IsArchiving
        {
            get { return false; }
        }

        public void SaveModuleToArchive(TarArchiveWriter writer, IScene scene)
        {
            m_log.Info("[Archive]: Writing estates to archive");

            writer.WriteDir("estate");
            EstateSettings settings = scene.RegionInfo.EstateSettings;
            string xmlData = WebUtils.BuildXmlResponse(settings.ToKeyValuePairs(true));
            writer.WriteFile("estate/" + scene.RegionInfo.RegionName, xmlData);

            m_log.Info("[Archive]: Finished writing estates to archive");
            m_log.Info("[Archive]: Writing region info to archive");

            writer.WriteDir("regioninfo");
            RegionInfo regionInfo = scene.RegionInfo;

            writer.WriteFile("regioninfo/" + scene.RegionInfo.RegionName, OSDParser.SerializeLLSDBinary(regionInfo.PackRegionInfoData(true)));

            m_log.Info("[Archive]: Finished writing region info to archive");
        }
    }
}
