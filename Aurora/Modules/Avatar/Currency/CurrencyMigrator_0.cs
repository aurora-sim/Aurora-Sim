using System;
using System.Collections.Generic;
using Aurora.DataManager.Migration;
using Aurora.Framework;
using Aurora.Framework.Utilities;

namespace Simple.Currency
{
    public class CurrencyMigrator_0 : Migrator
    {
        private static readonly List<SchemaDefinition> _schema = new List<SchemaDefinition>()
        {
            new SchemaDefinition("simple_currency",  
                new ColumnDefinition[]
                {
                    new ColumnDefinition {Name = "PrincipalID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "Amount", Type = ColumnTypeDef.Integer30},
                    new ColumnDefinition {Name = "LandInUse", Type = ColumnTypeDef.Integer30},
                    new ColumnDefinition {Name = "Tier", Type = ColumnTypeDef.Integer30},
                    new ColumnDefinition {Name = "IsGroup", Type = ColumnTypeDef.TinyInt1},
                    new ColumnDefinition {Name = "StipendsBalance", Type = ColumnTypeDef.Integer11},
                },
                new IndexDefinition[] 
                {
                    new IndexDefinition() { Fields = new string[] {"PrincipalID"}, Type = IndexType.Primary }
                }),
            new SchemaDefinition("simple_currency_history",  
                new ColumnDefinition[]
                {
                    new ColumnDefinition {Name = "TransactionID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "Description", Type = ColumnTypeDef.String128},
                    new ColumnDefinition {Name = "FromPrincipalID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "FromName", Type = ColumnTypeDef.String128},
                    new ColumnDefinition {Name = "ToPrincipalID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "ToName", Type = ColumnTypeDef.String128},
                    new ColumnDefinition {Name = "Amount", Type = ColumnTypeDef.Integer30},
                    new ColumnDefinition {Name = "TransType", Type = ColumnTypeDef.Integer11},
                    new ColumnDefinition {Name = "Created", Type = ColumnTypeDef.Integer30},
                    new ColumnDefinition {Name = "ToBalance", Type = ColumnTypeDef.Integer30},
                    new ColumnDefinition {Name = "FromBalance", Type = ColumnTypeDef.Integer30},
                    new ColumnDefinition {Name = "FromObjectName", Type = ColumnTypeDef.String50},
                    new ColumnDefinition {Name = "ToObjectName", Type = ColumnTypeDef.String50},
                    new ColumnDefinition {Name = "RegionID", Type = ColumnTypeDef.Char36},
                },
                new IndexDefinition[] 
                {
                    new IndexDefinition() { Fields = new string[] {"TransactionID"}, Type = IndexType.Primary }
                }),
            new SchemaDefinition("simple_purchased",  
                new ColumnDefinition[]
                {
                    new ColumnDefinition {Name = "PurchaseID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "PrincipalID", Type = ColumnTypeDef.Char36},
                    new ColumnDefinition {Name = "IP", Type = ColumnTypeDef.String64},
                    new ColumnDefinition {Name = "Amount", Type = ColumnTypeDef.Integer30},
                    new ColumnDefinition {Name = "RealAmount", Type = ColumnTypeDef.Integer30},
                    new ColumnDefinition {Name = "Created", Type = ColumnTypeDef.Integer30},
                    new ColumnDefinition {Name = "Updated", Type = ColumnTypeDef.Integer30},
                },
                new IndexDefinition[] 
                {
                    new IndexDefinition() { Fields = new string[] {"PurchaseID"}, Type = IndexType.Primary }
                }),
        };

        public CurrencyMigrator_0()
        {
            Version = new Version(0, 1, 0);
            MigrationName = "SimpleCurrency";
            base.schema = _schema;
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