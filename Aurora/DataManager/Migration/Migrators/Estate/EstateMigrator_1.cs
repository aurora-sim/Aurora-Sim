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
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.DataManager.Migration.Migrators.Estate
{
    public class EstateMigrator_1 : Migrator
    {
        public EstateMigrator_1()
        {
            Version = new Version(0, 0, 1);
            MigrationName = "Estate";

            schema = new List<SchemaDefinition>();

            RemoveSchema("estates");
            AddSchema("estateregions", ColDefs(
                ColDef("RegionID", ColumnTypes.String36),
                ColDef("EstateID", ColumnTypes.Integer11)
                                           ), IndexDefs(
                                               IndexDef(new string[1] {"RegionID"}, IndexType.Primary)
                                                  ));

            AddSchema("estatesettings", ColDefs(
                ColDef("EstateID", ColumnTypes.Integer11),
                ColDef("EstateName", ColumnTypes.String100),
                ColDef("EstateOwner", ColumnTypes.String36),
                ColDef("ParentEstateID", ColumnTypes.Integer11),
                ColDef("Settings", ColumnTypes.Text)
                                            ), IndexDefs(
                                                IndexDef(new string[1] {"EstateID"}, IndexType.Primary)
                                                   ));
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

        public override void FinishedMigration(IDataConnector genericData)
        {
            if (!genericData.TableExists("estates")) return;
            DataReaderConnection dr = genericData.QueryData("WHERE `Key` = 'EstateID'", "estates",
                                                            "`ID`, `Key`, `Value`");

            if (dr != null)
            {
                try
                {
                    while (dr.DataReader.Read())
                    {
                        try
                        {
                            UUID ID = UUID.Parse(dr.DataReader["ID"].ToString());
                            string value = dr.DataReader["Value"].ToString();
                            QueryFilter filter = new QueryFilter();
                            filter.andFilters["`ID`"] = value;
                            filter.andFilters["`Key`"] = "EstateSettings";
                            List<string> results = genericData.Query(new string[1] {"`Value`"}, "estates", filter, null,
                                                                     null, null);
                            if ((results != null) && (results.Count >= 1))
                            {
                                EstateSettings es = new EstateSettings();
                                es.FromOSD((OSDMap) OSDParser.DeserializeLLSDXml(results[0]));
                                genericData.Insert("estateregions", new object[] {ID, value});

                                filter = new QueryFilter();
                                filter.andFilters["`EstateID`"] = value;

                                List<string> exist = genericData.Query(new string[1] {"`EstateID`"}, "estatesettings",
                                                                       filter, null, null, null);
                                if (exist == null || exist.Count == 0)
                                {
                                    genericData.Insert("estatesettings",
                                                       new object[]
                                                           {
                                                               value, es.EstateName, es.EstateOwner, es.ParentEstateID,
                                                               es.ToOSD()
                                                           });
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                }
                finally
                {
                    dr.DataReader.Close();
                    genericData.CloseDatabase(dr);
                }
            }
        }
    }
}