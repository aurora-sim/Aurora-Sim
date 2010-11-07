using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse.StructuredData;
using OpenMetaverse;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface IOpenRegionSettingsModule
    {
        float MaxDragDistance { get; set; }
        float DefaultDrawDistance { get; set; }
        float MaximumPrimScale { get; set; }
        float MinimumPrimScale { get; set; }
        float MaximumPhysPrimScale { get; set; }
        float MaximumHollowSize { get; set; }
        float MinimumHoleSize { get; set; }
        int MaximumLinkCount { get; set; }
        int MaximumLinkCountPhys { get; set; }
        OSDArray LSLCommands { get; set; }
        float WhisperDistance { get; set; }
        float SayDistance { get; set; }
        float ShoutDistance { get; set; }
        bool RenderWater { get; set; }
        int MaximumInventoryItemsTransfer { get; set; }
        bool DisplayMinimap { get; set; }
        bool AllowPhysicalPrims { get; set; }
        string OffsetOfUTC { get; set; }
        bool EnableTeenMode { get; set; }
        UUID DefaultUnderpants { get; set; }
        UUID DefaultUndershirt { get; set; }
        bool ClampPrimSizes { get; set; }
        bool ForceDrawDistance { get; set; }
        int ShowTags { get; set; }
        int MaxGroups { get; set; }
        bool AllowParcelWindLight { get; set; }
        void RegisterGenericValue(string key, string value);
    }
}
