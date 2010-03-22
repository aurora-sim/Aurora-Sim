using System;
using System.Collections.Generic;
using C5;

namespace Aurora.DataManager.Migration
{
    public class Migrator : IRestorePoint
    {
        protected List<Rec<string, ColumnDefinition[]>> schema;

        public Version Version { get; protected set; }

        public bool CanProvideDefaults { get; protected set; }

        #region IRestorePoint Members

        public virtual void DoRestore(DataSessionProvider sessionProvider, IGenericData genericData)
        {
        }

        #endregion

        public bool Validate(DataSessionProvider sessionProvider, IGenericData genericData)
        {
            if (genericData.GetAuroraVersion() != Version)
            {
                return false;
            }
            return DoValidate(sessionProvider, genericData);
        }

        protected virtual bool DoValidate(DataSessionProvider sessionProvider, IGenericData genericData)
        {
            return false;
        }

        public IRestorePoint PrepareRestorePoint(DataSessionProvider sessionProvider, IGenericData genericData)
        {
            DoPrepareRestorePoint(sessionProvider, genericData);
            return this;
        }

        protected virtual void DoPrepareRestorePoint(DataSessionProvider sessionProvider, IGenericData genericData)
        {

        }

        public void Migrate(DataSessionProvider sessionProvider, IGenericData genericData)
        {
            DoMigrate(sessionProvider, genericData);
            genericData.WriteAuroraVersion(Version);
        }

        protected virtual void DoMigrate(DataSessionProvider sessionProvider, IGenericData genericData)
        {
        }

        public void CreateDefaults(DataSessionProvider sessionProvider, IGenericData genericData)
        {
            DoCreateDefaults(sessionProvider, genericData);
            genericData.WriteAuroraVersion(Version);
        }

        protected virtual void DoCreateDefaults(DataSessionProvider sessionProvider, IGenericData genericData)
        {
        }

        protected ColumnDefinition[] ColDefs(params ColumnDefinition[] defs)
        {
            return defs;
        }

        protected ColumnDefinition ColDef(string name, ColumnTypes columnType)
        {
            return new ColumnDefinition() { Name = name, Type = columnType, IsPrimary = false };
        }

        protected ColumnDefinition ColDef(string name, ColumnTypes columnType, bool isPrimary)
        {
            return new ColumnDefinition() { Name = name, Type = columnType, IsPrimary = isPrimary };
        }

        protected void AddSchema(string table, ColumnDefinition[] definitions)
        {
            schema.Add(new Rec<string, ColumnDefinition[]>(table, definitions));
        }

        protected void EnsureAllTablesInSchemaExist(IGenericData genericData)
        {
            foreach (var s in schema)
            {
                genericData.EnsureTableExists(s.X1, s.X2);
            }
        }

        protected bool TestThatAllTablesValidate(IGenericData genericData)
        {
            foreach (var s in schema)
            {
                if (!genericData.VerifyTableExists(s.X1, s.X2))
                {
                    return false;
                }
            }
            return true;
        }

        protected void CopyAllTablesToTempVersions(IGenericData genericData)
        {
            foreach (var s in schema)
            {
                CopyTableToTempVersion(genericData, s.X1, s.X2);
            }
        }

        protected void RestoreTempTablesToReal(IGenericData genericData)
        {
            foreach (var s in schema)
            {
                RestoreTempTableToReal(genericData, s.X1, s.X2);
            }
        }

        private void CopyTableToTempVersion(IGenericData genericData, string tablename, ColumnDefinition[] columnDefinitions)
        {
            genericData.CopyTableToTable(tablename, GetTempTableNameFromTableName(tablename), columnDefinitions);
        }

        private string GetTempTableNameFromTableName(string tablename)
        {
            return tablename + "_TEMP";
        }

        private void RestoreTempTableToReal(IGenericData genericData, string tablename, ColumnDefinition[] columnDefinitions)
        {
            genericData.CopyTableToTable(GetTempTableNameFromTableName(GetTempTableNameFromTableName(tablename)), tablename, columnDefinitions);
        }

        public void ClearRestorePoint(IGenericData genericData)
        {
            foreach (var s in schema)
            {
                DeleteTempVersion(genericData, s.X1);
            }
        }

        private void DeleteTempVersion(IGenericData genericData, string tableName)
        {
            string tempTableName = GetTempTableNameFromTableName(tableName);
            if (genericData.TableExists(tempTableName))
            {
                genericData.DropTable(tempTableName);   
            }
        }
    }
}