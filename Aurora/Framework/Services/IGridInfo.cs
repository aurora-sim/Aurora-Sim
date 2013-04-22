using System.Collections.Generic;
namespace Aurora.Framework.Services
{
    public interface IGridInfo
    {
        string GridName { get; }
        string GridNick { get; }
        string GridLoginURI { get; }
        string GridWelcomeURI { get; }
        string GridEconomyURI { get; }
        string GridAboutURI { get; }
        string GridHelpURI { get; }
        string GridRegisterURI { get; }
        string GridForgotPasswordURI { get; }
        string GridMapTileURI { get; set; }
        string AgentAppearanceURI { get; set; }
        string GridWebProfileURI { get; }
        string GridSearchURI { get; }
        string GridDestinationURI { get; }
        string GridMarketplaceURI { get; }
        string GridTutorialURI { get; }
        string GridSnapshotConfigURI { get; }

        void UpdateGridInfo();
    }

    public interface IGridServerInfoService
    {
        List<string> GetGridURIs(string key);
        string GetGridURI(string key);
        Dictionary<string, List<string>> RetrieveAllGridURIs(bool secure);
        void AddURI(string key, string value);
    }
}