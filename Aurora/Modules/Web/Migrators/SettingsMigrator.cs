using Aurora.Framework;
using OpenMetaverse;

namespace Aurora.Modules.Web
{
    internal class SettingsMigrator
    {
        public static readonly string Schema = "WebSettings";
        public static uint CurrentVersion = 1;
        public static GridSettings _settings;

        public static void InitializeDefaults()
        {
            _settings = new GridSettings { MapCenter = new Vector2(1000, 1000), LastSettingsVersionUpdateIgnored = CurrentVersion, LastPagesVersionUpdateIgnored = PagesMigrator.GetVersion() };
        }

        public static bool RequiresUpdate()
        {
            IGenericsConnector generics = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();

            OSDWrapper version = generics.GetGeneric<OSDWrapper>(UUID.Zero, Schema + "Version", "");
            return version == null || version.Info.AsInteger() < CurrentVersion;
        }

        public static uint GetVersion()
        {
            IGenericsConnector generics = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();

            OSDWrapper version = generics.GetGeneric<OSDWrapper>(UUID.Zero, Schema + "Version", "");
            return version == null ? 0 : (uint)version.Info.AsInteger();
        }

        public static bool RequiresInitialUpdate()
        {
            IGenericsConnector generics = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();

            OSDWrapper version = generics.GetGeneric<OSDWrapper>(UUID.Zero, Schema + "Version", "");
            return version == null || version.Info.AsInteger() < 1;
        }

        public static void ResetToDefaults()
        {
            InitializeDefaults();

            IGenericsConnector generics = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();

            //Remove all pages
            generics.RemoveGeneric(UUID.Zero, Schema);

            generics.AddGeneric(UUID.Zero, Schema, "Settings", _settings.ToOSD());
            generics.AddGeneric(UUID.Zero, Schema + "Version", "", new OSDWrapper { Info = CurrentVersion }.ToOSD());
        }

        public static bool CheckWhetherIgnoredVersionUpdate(uint version)
        {
            return version != SettingsMigrator.CurrentVersion;
        }
    }
}
