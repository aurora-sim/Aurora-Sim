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

namespace Aurora.DataManager.Migration.Migrators
{
    public class RegionInfoMigrator_4 : Migrator
    {
        public RegionInfoMigrator_4()
        {
            Version = new Version(0, 0, 4);
            MigrationName = "RegionInfo";

            schema = new List<SchemaDefinition>();

            AddSchema("simulator", ColDefs(
                ColDef("RegionID", ColumnTypes.String50),
                ColDef("RegionName", ColumnTypes.String50),
                ColDef("RegionInfo", ColumnTypes.Text),
                ColDef("Disabled", ColumnTypes.String45)
            ), IndexDefs(
                IndexDef(new string[1]{ "RegionID" }, IndexType.Primary),
                IndexDef(new string[1]{ "RegionName" }, IndexType.Index),
                IndexDef(new string[1]{ "Disabled" }, IndexType.Index)
            ));

            AddSchema("regionsettings", ColDefs(
                ColDef("regionUUID", ColumnTypes.Char36),
                ColDef("block_terraform", ColumnTypes.Integer11),
                ColDef("block_fly", ColumnTypes.Integer11),
                ColDef("allow_damage", ColumnTypes.Integer11),
                ColDef("restrict_pushing", ColumnTypes.Integer11),
                ColDef("allow_land_resell", ColumnTypes.Integer11),
                ColDef("allow_land_join_divide", ColumnTypes.Integer11),
                ColDef("block_show_in_search", ColumnTypes.Integer11),
                ColDef("agent_limit", ColumnTypes.Integer11),
                ColDef("object_bonus", ColumnTypes.Double),
                ColDef("maturity", ColumnTypes.Integer11),
                ColDef("disable_scripts", ColumnTypes.Integer11),
                ColDef("disable_collisions", ColumnTypes.Integer11),
                ColDef("disable_physics", ColumnTypes.Integer11),
                ColDef("terrain_texture_1", ColumnTypes.Char36),
                ColDef("terrain_texture_2", ColumnTypes.Char36),
                ColDef("terrain_texture_3", ColumnTypes.Char36),
                ColDef("terrain_texture_4", ColumnTypes.Char36),
                ColDef("elevation_1_nw", ColumnTypes.Double),
                ColDef("elevation_2_nw", ColumnTypes.Double),
                ColDef("elevation_1_ne", ColumnTypes.Double),
                ColDef("elevation_2_ne", ColumnTypes.Double),
                ColDef("elevation_1_se", ColumnTypes.Double),
                ColDef("elevation_2_se", ColumnTypes.Double),
                ColDef("elevation_1_sw", ColumnTypes.Double),
                ColDef("elevation_2_sw", ColumnTypes.Double),
                ColDef("water_height", ColumnTypes.Double),
                ColDef("terrain_raise_limit", ColumnTypes.Double),
                ColDef("terrain_lower_limit", ColumnTypes.Double),
                ColDef("use_estate_sun", ColumnTypes.Integer11),
                ColDef("fixed_sun", ColumnTypes.Integer11),
                ColDef("sun_position", ColumnTypes.Double),
                ColDef("covenant", ColumnTypes.Char36),
                ColDef("Sandbox", ColumnTypes.Integer11),
                ColDef("sunvectorx", ColumnTypes.Double),
                ColDef("sunvectory", ColumnTypes.Double),
                ColDef("sunvectorz", ColumnTypes.Double),
                ColDef("loaded_creation_id", ColumnTypes.String64),
                ColDef("loaded_creation_datetime", ColumnTypes.Integer11),
                ColDef("map_tile_ID", ColumnTypes.Char36),
                ColDef("terrain_tile_ID", ColumnTypes.Char36),
                ColDef("minimum_age", ColumnTypes.Integer11),
                ColDef("covenantlastupdated", ColumnTypes.String36),
                ColDef("generic", ColumnTypes.LongText),
                ColDef("terrainmaplastregenerated", ColumnTypes.Integer30)
            ), IndexDefs(
                IndexDef(new string[1]{ "regionUUID" }, IndexType.Primary)
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
    }
}