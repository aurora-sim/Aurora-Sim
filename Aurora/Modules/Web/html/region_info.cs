using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

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

        public bool RequiresAuthentication { get { return false; } }
        public bool RequiresAdminAuthentication { get { return false; } }

        public Dictionary<string, object> Fill(WebInterface webInterface, string filename, Hashtable query, OSHttpResponse httpResponse,
            Dictionary<string, object> requestParameters, ITranslator translator)
        {
            var vars = new Dictionary<string, object>();
            if (query.ContainsKey("regionid"))
            {
                GridRegion region = webInterface.Registry.RequestModuleInterface<IGridService>().GetRegionByUUID(UUID.Zero,
                    UUID.Parse(query["regionid"].ToString()));

                IEstateConnector estateConnector = Aurora.DataManager.DataManager.RequestPlugin<IEstateConnector>();
                EstateSettings estate = estateConnector.GetEstateSettings(region.RegionID);

                vars.Add("RegionName", region.RegionName);
                vars.Add("OwnerUUID", estate.EstateOwner);
                vars.Add("OwnerName", webInterface.Registry.RequestModuleInterface<IUserAccountService>().
                    GetUserAccount(UUID.Zero, estate.EstateOwner).Name);
                vars.Add("RegionLocX", region.RegionLocX / Constants.RegionSize);
                vars.Add("RegionLocY", region.RegionLocY / Constants.RegionSize);
                vars.Add("RegionSizeX", region.RegionSizeX);
                vars.Add("RegionSizeY", region.RegionSizeY);
                vars.Add("RegionType", region.RegionType);
                IWebHttpTextureService webTextureService = webInterface.Registry.
                    RequestModuleInterface<IWebHttpTextureService>();
                if (webTextureService != null && region.TerrainMapImage != UUID.Zero)
                    vars.Add("RegionImageURL", webTextureService.GetTextureURL(region.TerrainMapImage));
                else
                    vars.Add("RegionImageURL", "info.jpg");

                vars.Add("RegionInformationText", translator.GetTranslatedString("RegionInformationText"));
                vars.Add("OwnerNameText", translator.GetTranslatedString("OwnerNameText"));
                vars.Add("RegionLocationText", translator.GetTranslatedString("RegionLocationText"));
                vars.Add("RegionSizeText", translator.GetTranslatedString("RegionSizeText"));
                vars.Add("RegionNameText", translator.GetTranslatedString("RegionNameText"));
                vars.Add("RegionTypeText", translator.GetTranslatedString("RegionTypeText"));
            }


            return vars;
        }
    }
}
