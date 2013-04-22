using Aurora.Framework;
using Aurora.Framework.DatabaseInterfaces;
using Aurora.Framework.Modules;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using OpenMetaverse;
using System.Collections.Generic;
using GridRegion = Aurora.Framework.Services.GridRegion;
using RegionFlags = Aurora.Framework.Services.RegionFlags;

namespace Aurora.Modules.Web
{
    public class RegionInfoPage : IWebInterfacePage
    {
        public string[] FilePath
        {
            get
            {
                return new[]
                           {
                               "html/region_info.html"
                           };
            }
        }

        public bool RequiresAuthentication
        {
            get { return false; }
        }

        public bool RequiresAdminAuthentication
        {
            get { return false; }
        }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, OSHttpRequest httpRequest,
                                               OSHttpResponse httpResponse, Dictionary<string, object> requestParameters,
                                               ITranslator translator, out string response)
        {
            response = null;
            var vars = new Dictionary<string, object>();
            if (httpRequest.Query.ContainsKey("regionid"))
            {
                GridRegion region = webInterface.Registry.RequestModuleInterface<IGridService>().GetRegionByUUID(null,
                                                                                                                 UUID
                                                                                                                     .Parse
                                                                                                                     (httpRequest
                                                                                                                          .Query
                                                                                                                          [
                                                                                                                              "regionid"
                                                                                                                          ]
                                                                                                                          .ToString
                                                                                                                          ()));

                IEstateConnector estateConnector = Framework.Utilities.DataManager.RequestPlugin<IEstateConnector>();
                EstateSettings estate = estateConnector.GetEstateSettings(region.RegionID);

                vars.Add("RegionName", region.RegionName);
                vars.Add("OwnerUUID", estate.EstateOwner);
                var estateOwnerAccount = webInterface.Registry.RequestModuleInterface<IUserAccountService>().
                                                      GetUserAccount(null, estate.EstateOwner);
                vars.Add("OwnerName", estateOwnerAccount == null ? "No account found" : estateOwnerAccount.Name);
                vars.Add("RegionLocX", region.RegionLocX/Constants.RegionSize);
                vars.Add("RegionLocY", region.RegionLocY/Constants.RegionSize);
                vars.Add("RegionSizeX", region.RegionSizeX);
                vars.Add("RegionSizeY", region.RegionSizeY);
                vars.Add("RegionType", region.RegionType);
                vars.Add("RegionOnline",
                         (region.Flags & (int) RegionFlags.RegionOnline) ==
                         (int) RegionFlags.RegionOnline
                             ? translator.GetTranslatedString("Online")
                             : translator.GetTranslatedString("Offline"));

                IAgentInfoService agentInfoService = webInterface.Registry.RequestModuleInterface<IAgentInfoService>();
                IUserAccountService userService = webInterface.Registry.RequestModuleInterface<IUserAccountService>();
                if (agentInfoService != null)
                {
                    List<UserInfo> usersInRegion = agentInfoService.GetUserInfos(region.RegionID);
                    vars.Add("NumberOfUsersInRegion", usersInRegion.Count);
                    List<Dictionary<string, object>> users = new List<Dictionary<string, object>>();
                    foreach (var client in usersInRegion)
                    {
                        UserAccount account = userService.GetUserAccount(null, client.UserID);
                        if (account == null)
                            continue;
                        Dictionary<string, object> user = new Dictionary<string, object>();
                        user.Add("UserNameText", translator.GetTranslatedString("UserNameText"));
                        user.Add("UserUUID", client.UserID);
                        user.Add("UserName", account.Name);
                        users.Add(user);
                    }
                    vars.Add("UsersInRegion", users);
                }
                else
                {
                    vars.Add("NumberOfUsersInRegion", 0);
                    vars.Add("UsersInRegion", new List<Dictionary<string, object>>());
                }
                IDirectoryServiceConnector directoryConnector =
                    Framework.Utilities.DataManager.RequestPlugin<IDirectoryServiceConnector>();
                if (directoryConnector != null)
                {
                    List<LandData> data = directoryConnector.GetParcelsByRegion(0, 10, region.RegionID, UUID.Zero,
                                                                                ParcelFlags.None, ParcelCategory.Any);
                    List<Dictionary<string, object>> parcels = new List<Dictionary<string, object>>();
                    foreach (var p in data)
                    {
                        Dictionary<string, object> parcel = new Dictionary<string, object>();
                        parcel.Add("ParcelNameText", translator.GetTranslatedString("ParcelNameText"));
                        parcel.Add("ParcelOwnerText", translator.GetTranslatedString("ParcelOwnerText"));
                        parcel.Add("ParcelUUID", p.InfoUUID);
                        parcel.Add("ParcelName", p.Name);
                        parcel.Add("ParcelOwnerUUID", p.OwnerID);
                        IUserAccountService accountService =
                            webInterface.Registry.RequestModuleInterface<IUserAccountService>();
                        if (accountService != null)
                        {
                            var account = accountService.GetUserAccount(null, p.OwnerID);
                            if (account == null)
                                parcel.Add("ParcelOwnerName", translator.GetTranslatedString("NoAccountFound"));
                            else
                                parcel.Add("ParcelOwnerName", account.Name);
                        }
                        parcels.Add(parcel);
                    }
                    vars.Add("ParcelInRegion", parcels);
                    vars.Add("NumberOfParcelsInRegion", parcels.Count);
                }
                IWebHttpTextureService webTextureService = webInterface.Registry.
                                                                        RequestModuleInterface<IWebHttpTextureService>();
                if (webTextureService != null && region.TerrainMapImage != UUID.Zero)
                    vars.Add("RegionImageURL", webTextureService.GetTextureURL(region.TerrainMapImage));
                else
                    vars.Add("RegionImageURL", "images/icons/no_picture.jpg");

                // Menu Region
                vars.Add("MenuRegionTitle", translator.GetTranslatedString("MenuRegionTitle"));
                vars.Add("MenuParcelTitle", translator.GetTranslatedString("MenuParcelTitle"));
                vars.Add("MenuOwnerTitle", translator.GetTranslatedString("MenuOwnerTitle"));

                vars.Add("RegionInformationText", translator.GetTranslatedString("RegionInformationText"));
                vars.Add("OwnerNameText", translator.GetTranslatedString("OwnerNameText"));
                vars.Add("RegionLocationText", translator.GetTranslatedString("RegionLocationText"));
                vars.Add("RegionSizeText", translator.GetTranslatedString("RegionSizeText"));
                vars.Add("RegionNameText", translator.GetTranslatedString("RegionNameText"));
                vars.Add("RegionTypeText", translator.GetTranslatedString("RegionTypeText"));
                vars.Add("RegionInfoText", translator.GetTranslatedString("RegionInfoText"));
                vars.Add("RegionOnlineText", translator.GetTranslatedString("RegionOnlineText"));
                vars.Add("NumberOfUsersInRegionText", translator.GetTranslatedString("NumberOfUsersInRegionText"));
                vars.Add("ParcelsInRegionText", translator.GetTranslatedString("ParcelsInRegionText"));

                // Style Switcher
                vars.Add("styles1", translator.GetTranslatedString("styles1"));
                vars.Add("styles2", translator.GetTranslatedString("styles2"));
                vars.Add("styles3", translator.GetTranslatedString("styles3"));
                vars.Add("styles4", translator.GetTranslatedString("styles4"));
                vars.Add("styles5", translator.GetTranslatedString("styles5"));

                vars.Add("StyleSwitcherStylesText", translator.GetTranslatedString("StyleSwitcherStylesText"));
                vars.Add("StyleSwitcherLanguagesText", translator.GetTranslatedString("StyleSwitcherLanguagesText"));
                vars.Add("StyleSwitcherChoiceText", translator.GetTranslatedString("StyleSwitcherChoiceText"));

                // Language Switcher
                vars.Add("en", translator.GetTranslatedString("en"));
                vars.Add("fr", translator.GetTranslatedString("fr"));
                vars.Add("de", translator.GetTranslatedString("de"));
                vars.Add("it", translator.GetTranslatedString("it"));
                vars.Add("es", translator.GetTranslatedString("es"));
            }

            return vars;
        }

        public bool AttemptFindPage(string filename, ref OSHttpResponse httpResponse, out string text)
        {
            text = "";
            return false;
        }
    }
}