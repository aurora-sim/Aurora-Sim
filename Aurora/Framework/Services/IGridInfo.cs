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
        string GridMapTileURI { get; }
        string AgentAppearanceURI { get; }
        string GridWebProfileURI { get; }
        string GridSearchURI { get; }
        string GridDestinationURI { get; }
        string GridMarketplaceURI { get; }
        string GridTutorialURI { get; }
        string GridSnapshotConfigURI { get; }
    }
}