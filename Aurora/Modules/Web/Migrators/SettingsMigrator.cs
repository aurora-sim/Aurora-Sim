using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.Modules.Web
{
    internal class SettingsMigrator
    {
        public const string Schema = "WebSettings";
        public const uint CurrentVersion = 1;
        public readonly GridSettings _settings;

        public SettingsMigrator()
        {
            _settings = new GridSettings { MapCenter = new Vector2(1000, 1000) };
        }

        public bool RequiresUpdate()
        {
            IGenericsConnector generics = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();

            OSDWrapper version = generics.GetGeneric<OSDWrapper>(UUID.Zero, Schema + "Version", "");
            return version == null || version.Info.AsInteger() < CurrentVersion;
        }

        public void ResetToDefaults()
        {
            IGenericsConnector generics = Aurora.DataManager.DataManager.RequestPlugin<IGenericsConnector>();

            //Remove all pages
            generics.RemoveGeneric(UUID.Zero, Schema);

            generics.AddGeneric(UUID.Zero, Schema, "Settings", _settings.ToOSD());
            generics.AddGeneric(UUID.Zero, Schema + "Version", "", new OSDWrapper { Info = CurrentVersion }.ToOSD());
        }
    }
}
