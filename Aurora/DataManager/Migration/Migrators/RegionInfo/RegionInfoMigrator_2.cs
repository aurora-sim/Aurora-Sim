using System;
using System.Collections.Generic;
using C5;
using Aurora.Framework;

namespace Aurora.DataManager.Migration.Migrators
{
    public class RegionInfoMigrator_2 : Migrator
    {
        public RegionInfoMigrator_2()
        {
            Version = new Version(0, 0, 2);
            MigrationName = "RegionInfo";

            schema = new List<Rec<string, ColumnDefinition[]>>();
            
            AddSchema("simulator", ColDefs(
                ColDef("RegionID", ColumnTypes.String50, true),
                ColDef("RegionName", ColumnTypes.String50),
                ColDef("RegionInfo", ColumnTypes.Text),
                ColDef("Disabled", ColumnTypes.String45)));

            AddSchema("regionsettings", ColDefs(
                ColDef ("regionUUID", ColumnTypes.Char36, true),
                ColDef ("block_terraform", ColumnTypes.Integer11),
                ColDef ("block_fly", ColumnTypes.Integer11),
                ColDef ("allow_damage", ColumnTypes.Integer11),
                ColDef ("restrict_pushing", ColumnTypes.Integer11),
                ColDef ("allow_land_resell", ColumnTypes.Integer11),
                ColDef ("allow_land_join_divide", ColumnTypes.Integer11),
                ColDef ("block_show_in_search", ColumnTypes.Integer11),
                ColDef ("agent_limit", ColumnTypes.Integer11),
                ColDef ("object_bonus", ColumnTypes.Double),
                ColDef ("maturity", ColumnTypes.Integer11),
                ColDef ("disable_scripts", ColumnTypes.Integer11),
                ColDef ("disable_collisions", ColumnTypes.Integer11),
                ColDef ("disable_physics", ColumnTypes.Integer11),
                ColDef ("terrain_texture_1", ColumnTypes.Char36),
                ColDef ("terrain_texture_2", ColumnTypes.Char36),
                ColDef ("terrain_texture_3", ColumnTypes.Char36),
                ColDef ("terrain_texture_4", ColumnTypes.Char36),
                ColDef ("elevation_1_nw", ColumnTypes.Double),
                ColDef ("elevation_2_nw", ColumnTypes.Double),
                ColDef ("elevation_1_ne", ColumnTypes.Double),
                ColDef ("elevation_2_ne", ColumnTypes.Double),
                ColDef ("elevation_1_se", ColumnTypes.Double),
                ColDef ("elevation_2_se", ColumnTypes.Double),
                ColDef ("elevation_1_sw", ColumnTypes.Double),
                ColDef ("elevation_2_sw", ColumnTypes.Double),
                ColDef ("water_height", ColumnTypes.Double),
                ColDef ("terrain_raise_limit", ColumnTypes.Double),
                ColDef ("terrain_lower_limit", ColumnTypes.Double),
                ColDef ("use_estate_sun", ColumnTypes.Integer11),
                ColDef ("fixed_sun", ColumnTypes.Integer11),
                ColDef ("sun_position", ColumnTypes.Double),
                ColDef ("covenant", ColumnTypes.Char36),
                ColDef ("Sandbox", ColumnTypes.Integer11),
                ColDef ("sunvectorx", ColumnTypes.Double),
                ColDef ("sunvectory", ColumnTypes.Double),
                ColDef ("sunvectorz", ColumnTypes.Double),
                ColDef ("loaded_creation_id", ColumnTypes.String64),
                ColDef ("loaded_creation_datetime", ColumnTypes.Integer11),
                ColDef ("map_tile_ID", ColumnTypes.Char36),
                ColDef ("terrain_tile_ID", ColumnTypes.Char36),
                ColDef ("minimum_age", ColumnTypes.Integer11),
                ColDef ("covenantlastupdated", ColumnTypes.String36),
                ColDef ("generic", ColumnTypes.LongText)));
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