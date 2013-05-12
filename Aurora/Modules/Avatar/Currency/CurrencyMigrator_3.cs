using System;
using System.Collections.Generic;
using Aurora.DataManager.Migration;
using Aurora.Framework;
using Aurora.Framework.Utilities;

namespace Simple.Currency
{
    public class CurrencyMigrator_3 : Migrator
    {
        public CurrencyMigrator_3()
        {
            Version = new Version(0, 0, 3);
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
                          IndexDef(new string[1] {"PrincipalID"}, IndexType.Primary)
                          ));

            // Currency Transaction Logs
            AddSchema("simple_currency_history", ColDefs(
                ColDef("TransactionID", ColumnTypes.String36),
                ColDef("Description", ColumnTypes.String128),
                ColDef("FromPrincipalID", ColumnTypes.String36),
                ColDef("FromName", ColumnTypes.String128),
                ColDef("ToPrincipalID", ColumnTypes.String36),
                ColDef("ToName", ColumnTypes.String128),
                ColDef("Amount", ColumnTypes.Integer30),
                ColDef("TransType", ColumnTypes.Integer11),
                ColDef("Created", ColumnTypes.Integer30),
                ColDef("ToBalance", ColumnTypes.Integer30),
                ColDef("FromBalance", ColumnTypes.Integer30),
                ColDef("FromObjectName", ColumnTypes.String50),
                ColDef("ToObjectName", ColumnTypes.String50),
                ColDef("RegionID", ColumnTypes.Char36)),
                IndexDefs(
                    IndexDef(new string[1] { "TransactionID" }, IndexType.Primary)
                    ));

            // this is actually used for all purchases now.. a better name would be _purchased
            AddSchema("simple_purchased", ColDefs(
                ColDef("PurchaseID", ColumnTypes.String36),
                ColDef("PrincipalID", ColumnTypes.String36),
                ColDef("IP", ColumnTypes.String64),
                ColDef("Amount", ColumnTypes.Integer30),
                ColDef("RealAmount", ColumnTypes.Integer30),
                ColDef("Created", ColumnTypes.Integer30),
                ColDef("Updated", ColumnTypes.Integer30)),
                IndexDefs(
                    IndexDef(new string[1] { "PurchaseID" }, IndexType.Primary)
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