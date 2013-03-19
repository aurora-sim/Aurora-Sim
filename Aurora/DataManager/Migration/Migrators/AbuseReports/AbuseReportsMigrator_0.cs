/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using Aurora.Framework;
using Aurora.Framework.Utilities;

namespace Aurora.DataManager.Migration.Migrators
{
    public class AbuseReportsMigrator_0 : Migrator
    {
        public AbuseReportsMigrator_0()
        {
            Version = new Version(0, 0, 0);
            MigrationName = "AbuseReports";

            schema = new List<SchemaDefinition>();

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
                ColDef("Number", ColumnTypes.String100),
                ColDef("AssignedTo", ColumnTypes.String100),
                ColDef("Active", ColumnTypes.String100),
                ColDef("Checked", ColumnTypes.String100),
                ColDef("Notes", ColumnTypes.String1024)
                                          ),
                      IndexDefs(
                          IndexDef(new string[1] {"Number"}, IndexType.Primary)
                          )
                );
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