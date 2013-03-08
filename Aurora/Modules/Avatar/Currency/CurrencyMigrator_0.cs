using System;
using System.Collections.Generic;
using Aurora.DataManager.Migration;

using Aurora.Framework;

namespace Simple.Currency
{
    public class CurrencyMigrator_0 : Migrator
    {
        public CurrencyMigrator_0()
        {
            Version = new Version(0, 0, 0);
            MigrationName = "SimpleCurrency";

            schema = new List<SchemaDefinition>();

            AddSchema("simple_currency", ColDefs(
                ColDef("PrincipalID", ColumnTypes.String50),
                ColDef("Amount", ColumnTypes.Integer30),
                ColDef("LandInUse", ColumnTypes.Integer30),
                ColDef("Tier", ColumnTypes.Integer30),
                ColDef("IsGroup", ColumnTypes.TinyInt1),
                new ColumnDefinition
                {
                    Name = "StipendsBalance",
                    Type = new ColumnTypeDef
                    {
                        Type = ColumnType.Integer,
                        Size = 11,
                        defaultValue = "0"
                    }
                }
                ),
                IndexDefs(
                    IndexDef(new string[1] { "PrincipalID" }, IndexType.Primary)
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