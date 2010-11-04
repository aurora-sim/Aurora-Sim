using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class AuroraMigrator_2010_03_13 : Migrator
    {
        public AuroraMigrator_2010_03_13()
        {
            Version = new Version(2010, 3, 13);
            CanProvideDefaults = true;

            schema = new List<Rec<string, ColumnDefinition[]>>();

            AddSchema("userdata", ColDefs(
                ColDef("ID", ColumnTypes.String45, true),
                ColDef("Key", ColumnTypes.String50, true),
                ColDef("Value", ColumnTypes.Text)
                ));

            AddSchema("estates", ColDefs(
                ColDef("ID", ColumnTypes.String45, true),
                ColDef("Key", ColumnTypes.String50, true),
                ColDef("Value", ColumnTypes.Text)
                ));

            AddSchema("generics", ColDefs(
                ColDef("OwnerID", ColumnTypes.String45, true),
                ColDef("Type", ColumnTypes.String45, true),
                ColDef("Key", ColumnTypes.String50, true),
                ColDef("Value", ColumnTypes.Text)
                ));

            AddSchema("simulator", ColDefs(
                ColDef("RegionID", ColumnTypes.String50, true),
                ColDef("RegionName", ColumnTypes.String50),
                ColDef("RegionLocX", ColumnTypes.String50),
                ColDef("RegionLocY", ColumnTypes.String50),
                ColDef("InternalIP", ColumnTypes.String50),
                ColDef("Port", ColumnTypes.String50),
                ColDef("ExternalIP", ColumnTypes.String50),
                ColDef("RegionType", ColumnTypes.String50),
                ColDef("NonphysicalPrimMax", ColumnTypes.String50),
                ColDef("PhysicalPrimMax", ColumnTypes.String50),
                ColDef("ClampPrimSize", ColumnTypes.String50),
                ColDef("MaxPrims", ColumnTypes.String50),
                ColDef("LastUpdated", ColumnTypes.String50),
                ColDef("Online", ColumnTypes.String50),
                ColDef("AcceptingAgents", ColumnTypes.String50),
                ColDef("AccessLevel", ColumnTypes.String50),
                ColDef("Disabled", ColumnTypes.String50)
                ));

            AddSchema("telehubs", ColDefs(
                ColDef("RegionID", ColumnTypes.String50, true),
                ColDef("RegionLocX", ColumnTypes.String50),
                ColDef("RegionLocY", ColumnTypes.String50),
                ColDef("TelehubLocX", ColumnTypes.String50),
                ColDef("TelehubLocY", ColumnTypes.String50),
                ColDef("TelehubLocZ", ColumnTypes.String50),
                ColDef("TelehubRotX", ColumnTypes.String50),
                ColDef("TelehubRotY", ColumnTypes.String50),
                ColDef("TelehubRotZ", ColumnTypes.String50),
                ColDef("Spawns", ColumnTypes.String1024),
                ColDef("ObjectUUID", ColumnTypes.String50),
                ColDef("Name", ColumnTypes.String50)
                ));

            AddSchema("passwords", ColDefs(ColDef("Method", ColumnTypes.String50, true),
                ColDef("Password", ColumnTypes.String50)));

            AddSchema("avatararchives", ColDefs(ColDef("Name", ColumnTypes.String50, true),
                ColDef("Archive", ColumnTypes.String50)));

            AddSchema("macban", ColDefs(ColDef("macAddress", ColumnTypes.String50, true)));

            AddSchema("lslgenericdata", ColDefs(ColDef("Token", ColumnTypes.String50, true),
                ColDef("KeySetting", ColumnTypes.String50, true),
                ColDef("ValueSetting", ColumnTypes.String50)));
            
            AddSchema("bannedviewers", ColDefs(ColDef("Client", ColumnTypes.String50, true)));

            AddSchema("abusereports", ColDefs(
                ColDef("Category", ColumnTypes.String100),
                ColDef("ReporterName", ColumnTypes.String100),
                ColDef("ObjectName", ColumnTypes.String100),
                ColDef("ObjectUUID", ColumnTypes.String100),
                ColDef("AbuserName", ColumnTypes.String100),
                ColDef("AbuseLocation", ColumnTypes.String100),
                ColDef("AbuseDetails", ColumnTypes.String512),
                ColDef("ObjectPosition", ColumnTypes.String100),
                ColDef("RegionName", ColumnTypes.String100),
                ColDef("ScreenshotID", ColumnTypes.String100),
                ColDef("AbuseSummary", ColumnTypes.String100),
                ColDef("Number", ColumnTypes.String100, true),
                ColDef("AssignedTo", ColumnTypes.String100),
                ColDef("Active", ColumnTypes.String100),
                ColDef("Checked", ColumnTypes.String100),
                ColDef("Notes", ColumnTypes.String1024)
                ));

            AddSchema("landinfo", ColDefs(
                ColDef("ParcelID", ColumnTypes.String50, true),
                ColDef("LocalID", ColumnTypes.String50),
                ColDef("LandingX", ColumnTypes.String50),
                ColDef("LandingY", ColumnTypes.String50),
                ColDef("LandingZ", ColumnTypes.String50),
                ColDef("Name", ColumnTypes.String50),
                ColDef("Description", ColumnTypes.String50),
                ColDef("Flags", ColumnTypes.String50),
                ColDef("Dwell", ColumnTypes.String50),
                ColDef("InfoUUID", ColumnTypes.String50),
                ColDef("SalePrice", ColumnTypes.String50),
                ColDef("Auction", ColumnTypes.String50),
                ColDef("Area", ColumnTypes.String50),
                ColDef("Maturity", ColumnTypes.String50),
                ColDef("OwnerID", ColumnTypes.String50),
                ColDef("GroupID", ColumnTypes.String50),
                ColDef("MediaDescription", ColumnTypes.String50),
                ColDef("MediaHeight", ColumnTypes.String50),
                ColDef("MediaWidth", ColumnTypes.String50),
                ColDef("MediaLoop", ColumnTypes.String50),
                ColDef("MediaType", ColumnTypes.String50),
                ColDef("ObscureMedia", ColumnTypes.String50),
                ColDef("ObscureMusic", ColumnTypes.String50),
                ColDef("SnapshotID", ColumnTypes.String50),
                ColDef("MediaAutoScale", ColumnTypes.String50),
                ColDef("MediaURL", ColumnTypes.String50),
                ColDef("MusicURL", ColumnTypes.String50),
                ColDef("Bitmap", ColumnTypes.Blob),
                ColDef("Category", ColumnTypes.String50),
                ColDef("ClaimDate", ColumnTypes.String50),
                ColDef("ClaimPrice", ColumnTypes.String50),
                ColDef("Status", ColumnTypes.String50),
                ColDef("LandingType", ColumnTypes.String50),
                ColDef("PassHours", ColumnTypes.String50),
                ColDef("PassPrice", ColumnTypes.String50),
                ColDef("UserLookAtX", ColumnTypes.String50),
                ColDef("UserLookAtY", ColumnTypes.String50),
                ColDef("UserLookAtZ", ColumnTypes.String50),
                ColDef("AuthBuyerID", ColumnTypes.String50),
                ColDef("OtherCleanTime", ColumnTypes.String50),
                ColDef("RegionID", ColumnTypes.String50),
                ColDef("RegionHandle", ColumnTypes.String50),
                ColDef("GenericData", ColumnTypes.Text)));

            AddSchema("parcelaccess", ColDefs(
                ColDef("ParcelID", ColumnTypes.String50, true),
                ColDef("AccessID", ColumnTypes.String50),
                ColDef("Flags", ColumnTypes.String50),
                ColDef("Time", ColumnTypes.String50)));

            AddSchema("osagent", ColDefs(ColDef("AgentID", ColumnTypes.String50, true),
                ColDef("ActiveGroupID", ColumnTypes.String50)));

            AddSchema("osgroup", ColDefs(ColDef("GroupID", ColumnTypes.String50, true),
                ColDef("Name", ColumnTypes.String50),
                ColDef("Charter", ColumnTypes.String50),
                ColDef("InsigniaID", ColumnTypes.String50),
                ColDef("FounderID", ColumnTypes.String50),
                ColDef("MembershipFee", ColumnTypes.String50),
                ColDef("OpenEnrollment", ColumnTypes.String50),
                ColDef("ShowInList", ColumnTypes.String50),
                ColDef("AllowPublish", ColumnTypes.String50),
                ColDef("MaturePublish", ColumnTypes.String50),
                ColDef("OwnerRoleID", ColumnTypes.String50)));

            AddSchema("osgroupinvite", ColDefs(ColDef("InviteID", ColumnTypes.String50, true),
                ColDef("GroupID", ColumnTypes.String50, true),
                ColDef("RoleID", ColumnTypes.String50, true),
                ColDef("AgentID", ColumnTypes.String50, true),
                ColDef("TMStamp", ColumnTypes.String50),
                ColDef("FromAgentName", ColumnTypes.String50)));

            AddSchema("osgroupmembership", ColDefs(
                ColDef("GroupID", ColumnTypes.String50, true),
                ColDef("AgentID", ColumnTypes.String50, true),
                ColDef("SelectedRoleID", ColumnTypes.String50),
                ColDef("Contribution", ColumnTypes.String45),
                ColDef("ListInProfile", ColumnTypes.String45),
                ColDef("AcceptNotices", ColumnTypes.String45)));

            AddSchema("osgroupnotice", ColDefs(ColDef("GroupID", ColumnTypes.String50, true),
                ColDef("NoticeID", ColumnTypes.String50, true),
                ColDef("Timestamp", ColumnTypes.String50, true),
                ColDef("FromName", ColumnTypes.String50),
                ColDef("Subject", ColumnTypes.String50),
                ColDef("Message", ColumnTypes.String50),
                ColDef("HasAttachment", ColumnTypes.String50),
                ColDef("ItemID", ColumnTypes.String50),
                ColDef("AssetType", ColumnTypes.String50),
                ColDef("ItemName", ColumnTypes.String50)));

            AddSchema("osgrouprolemembership", ColDefs(ColDef("GroupID", ColumnTypes.String50, true),
                ColDef("RoleID", ColumnTypes.String50, true),
                ColDef("AgentID", ColumnTypes.String50, true)));

            AddSchema("osrole", ColDefs(ColDef("GroupID", ColumnTypes.String50, true),
                ColDef("RoleID", ColumnTypes.String50, true),
                ColDef("Name", ColumnTypes.String50),
                ColDef("Description", ColumnTypes.String50),
                ColDef("Title", ColumnTypes.String50),
                ColDef("Powers", ColumnTypes.String50)));

            AddSchema("auroradotnetstatesaves", ColDefs(
                ColDef("State", ColumnTypes.String50),
                ColDef("ItemID", ColumnTypes.String50, true),
                ColDef("Source", ColumnTypes.Text),
                ColDef("Running", ColumnTypes.String50),
                ColDef("Variables", ColumnTypes.Text),
                ColDef("Plugins", ColumnTypes.Text),
                ColDef("Permissions", ColumnTypes.String50),
                ColDef("MinEventDelay", ColumnTypes.String50),
                ColDef("AssemblyName", ColumnTypes.Text),
                ColDef("Disabled", ColumnTypes.String45),
                ColDef("UserInventoryItemID", ColumnTypes.String50)
                ));

            AddSchema("presenceinfo", ColDefs(
                ColDef("AgentID", ColumnTypes.String50, true),
                ColDef("CurrentRegion", ColumnTypes.String50),
                ColDef("Flags", ColumnTypes.String50),
                ColDef("KnownAlts", ColumnTypes.String50),
                ColDef("KnownID0s", ColumnTypes.String50),
                ColDef("KnownIPs", ColumnTypes.String50),
                ColDef("KnownMacs", ColumnTypes.String50),
                ColDef("KnownViewers", ColumnTypes.String50),
                ColDef("LastKnownID0", ColumnTypes.String50),
                ColDef("LastKnownIP", ColumnTypes.String50),
                ColDef("LastKnownMac", ColumnTypes.String50),
                ColDef("LastKnownViewer", ColumnTypes.String50),
                ColDef("Platform", ColumnTypes.String50),
                ColDef("Position", ColumnTypes.String50),
                ColDef("UserName", ColumnTypes.String50))); 

            #region Search Tables

            AddSchema("searchparcel", ColDefs(ColDef("RegionID", ColumnTypes.String50),
                ColDef("ParcelID", ColumnTypes.String50, true),
                ColDef("LocalID", ColumnTypes.String50),
                ColDef("LandingX", ColumnTypes.String50),
                ColDef("LandingY", ColumnTypes.String50),
                ColDef("LandingZ", ColumnTypes.String50),
                ColDef("Name", ColumnTypes.String50),
                ColDef("Description", ColumnTypes.String50),
                ColDef("Flags", ColumnTypes.String50),
                ColDef("Dwell", ColumnTypes.String50),
                ColDef("InfoUUID", ColumnTypes.String50),
                ColDef("ForSale", ColumnTypes.String50),
                ColDef("SalePrice", ColumnTypes.String50),
                ColDef("Auction", ColumnTypes.String50),
                ColDef("Area", ColumnTypes.String50),
                ColDef("EstateID", ColumnTypes.String50),
                ColDef("Maturity", ColumnTypes.String50),
                ColDef("OwnerID", ColumnTypes.String50),
                ColDef("GroupID", ColumnTypes.String50),
                ColDef("ShowInSearch", ColumnTypes.String50),
                ColDef("SnapshotID", ColumnTypes.String50)));
            
            AddSchema("events", ColDefs(
                ColDef("EOwnerID", ColumnTypes.String50),
                ColDef("EName", ColumnTypes.String50),
                ColDef("EID", ColumnTypes.String50, true),
                ColDef("ECreatorID", ColumnTypes.String50),
                ColDef("ECategory", ColumnTypes.String50),
                ColDef("EDesc", ColumnTypes.String50),
                ColDef("EDate", ColumnTypes.String50),
                ColDef("ECoverCharge", ColumnTypes.String50),
                ColDef("ECoverAmount", ColumnTypes.String50),
                ColDef("ESimName", ColumnTypes.String50),
                ColDef("EGlobalPos", ColumnTypes.String50),
                ColDef("EFlags", ColumnTypes.String50),
                ColDef("EMature", ColumnTypes.String50),
                ColDef("EDuration", ColumnTypes.String50)
                ));

            #endregion
        }

        protected override void DoCreateDefaults(DataSessionProvider sessionProvider, IDataConnector genericData)
        {
            EnsureAllTablesInSchemaExist(genericData);
        }

        protected override bool DoValidate(DataSessionProvider sessionProvider, IDataConnector genericData)
        {
            return TestThatAllTablesValidate(genericData);
        }

        protected override void DoMigrate(DataSessionProvider sessionProvider, IDataConnector genericData)
        {
            DoCreateDefaults(sessionProvider, genericData);
        }

        protected override void DoPrepareRestorePoint(DataSessionProvider sessionProvider, IDataConnector genericData)
        {
            CopyAllTablesToTempVersions(genericData);
        }

        public override void DoRestore(DataSessionProvider sessionProvider, IDataConnector genericData)
        {
            RestoreTempTablesToReal(genericData);
        }
    }
}