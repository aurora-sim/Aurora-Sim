using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class GroupsMigrator_0 : Migrator
    {
        public GroupsMigrator_0()
        {
            Version = new Version(0, 0, 0);
            MigrationName = "Groups";

            schema = new List<Rec<string, ColumnDefinition[]>>();

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
                ColDef("Message", ColumnTypes.String1024),
                ColDef("HasAttachment", ColumnTypes.String50),
                ColDef("ItemID", ColumnTypes.String50),
                ColDef("AssetType", ColumnTypes.String50),
                ColDef("ItemName", ColumnTypes.String50)));

            AddSchema("osgrouprolemembership", ColDefs(ColDef("GroupID", ColumnTypes.String50, true),
                ColDef("RoleID", ColumnTypes.String50, true),
                ColDef("AgentID", ColumnTypes.String50, true)));

            AddSchema("osrole", ColDefs(ColDef("GroupID", ColumnTypes.String50, true),
                ColDef("RoleID", ColumnTypes.String50, true),
                ColDef("Name", ColumnTypes.String512),
                ColDef("Description", ColumnTypes.String512),
                ColDef("Title", ColumnTypes.String512),
                ColDef("Powers", ColumnTypes.String50)));
        }

        protected override void DoCreateDefaults(IDataConnector genericData)
        {
            EnsureAllTablesInSchemaExist(genericData);
        }

        protected override bool DoValidate(IDataConnector genericData)
        {
            return TestThatAllTablesValidate(genericData);
        }

        protected override void DoMigrate(IDataConnector genericData)
        {
            DoCreateDefaults(genericData);
        }

        protected override void DoPrepareRestorePoint(IDataConnector genericData)
        {
            CopyAllTablesToTempVersions(genericData);
        }
    }
}