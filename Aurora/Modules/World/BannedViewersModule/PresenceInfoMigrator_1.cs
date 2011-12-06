/*
 *  Copyright 2011 Matthew Beardmore
 *
 *  This file is part of Aurora.Addon.Protection.
 *  Aurora.Addon.Protection is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 *  Aurora.Addon.Protection is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 *  You should have received a copy of the GNU General Public License along with Aurora.Addon.Protection. If not, see http://www.gnu.org/licenses/.
 */

using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class PresenceInfoMigrator_1 : Migrator
    {
        public PresenceInfoMigrator_1()
        {
            Version = new Version(0, 0, 1);
            MigrationName = "PresenceInfo";

            schema = new List<Rec<string, ColumnDefinition[]>>();

            AddSchema("baninfo", ColDefs(
                ColDef("AgentID", /*"AgentID"*/ ColumnTypes.String50, true),
                ColDef("Flags", /*"Flags"*/ ColumnTypes.String50),
                ColDef("KnownAlts", /*"KnownAlts"*/ ColumnTypes.Text),
                ColDef("KnownID0s", /*"KnownID0s"*/ ColumnTypes.Text),
                ColDef("KnownIPs", /*"KnownIPs"*/ ColumnTypes.Text),
                ColDef("KnownMacs", /*"KnownMacs"*/ ColumnTypes.Text),
                ColDef("KnownViewers", /*"KnownViewers"*/ ColumnTypes.Text),
                ColDef("LastKnownID0", /*"LastKnownID0"*/ ColumnTypes.String50),
                ColDef("LastKnownIP", /*"LastKnownIP"*/ ColumnTypes.String50),
                ColDef("LastKnownMac", /*"LastKnownMac"*/ ColumnTypes.String50),
                ColDef("LastKnownViewer", /*"LastKnownViewer"*/ ColumnTypes.String255),
                ColDef("Platform", /*"Platform"*/ ColumnTypes.String50)));
            RemoveSchema("presenceinfo");
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
