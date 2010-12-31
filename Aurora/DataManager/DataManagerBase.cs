using System;
using System.Data;
using System.Collections.Generic;
using Aurora.Framework;
using OpenSim.Framework;
using OpenMetaverse;

namespace Aurora.DataManager
{
    public abstract class DataManagerBase : IDataConnector
    {
        private const string VERSION_TABLE_NAME = "aurora_version";
        private const string COLUMN_VERSION = "version";

        #region IGenericData Members

        public abstract string Identifier { get; }
        public abstract void ConnectToDatabase(string connectionString);
        public abstract List<string> Query(string keyRow, object keyValue, string table, string wantedValue, string Order);
        public abstract List<string> Query(string whereClause, string table, string wantedValue);
        public abstract List<string> QueryFullData(string whereClause, string table, string wantedValue);
        public abstract IDataReader QueryDataFull(string whereClause, string table, string wantedValue);
        public abstract IDataReader QueryData(string whereClause, string table, string wantedValue);
        public abstract List<string> Query(string keyRow, object keyValue, string table, string wantedValue);
        public abstract List<string> Query(string[] keyRow, object[] keyValue, string table, string wantedValue);
        public abstract bool Insert(string table, object[] values);
        public abstract bool Insert(string table, string[] keys, object[] values);
        public abstract IDataReader QueryReader(string keyRow, object keyValue, string table, string wantedValue);
        public abstract bool Delete(string table, string[] keys, object[] values);
        public abstract bool Insert(string table, object[] values, string updateKey, object updateValue);
        public abstract bool Update(string table, object[] setValues, string[] setRows, string[] keyRows, object[] keyValues);
        public abstract void CloseDatabase();
        public abstract bool TableExists(string table);
        public abstract void CreateTable(string table, ColumnDefinition[] columns);
        public abstract void UpdateTable(string table, ColumnDefinition[] columns);
        public abstract bool Replace(string table, string[] keys, object[] values);
        public abstract IGenericData Copy();

        public Version GetAuroraVersion()
        {
            if (!TableExists(VERSION_TABLE_NAME))
            {
                CreateTable(VERSION_TABLE_NAME, new[] {new ColumnDefinition {Name = COLUMN_VERSION, Type = ColumnTypes.String}});
            }

            List<string> results = Query(string.Empty, string.Empty, VERSION_TABLE_NAME, COLUMN_VERSION);
            if (results.Count > 0)
            {
                Version highestVersion = null;
                foreach (string result in results)
                {
                    if (result.Trim() == string.Empty)
                    {
                        continue;
                    }
                    var version = new Version(result);
                    if (highestVersion == null || version > highestVersion)
                    {
                        highestVersion = version;
                    }
                }
                return highestVersion;
            }

            return null;
        }

        public void WriteAuroraVersion(Version version)
        {
            if (!TableExists(VERSION_TABLE_NAME))
            {
                CreateTable(VERSION_TABLE_NAME, new[] {new ColumnDefinition {Name = COLUMN_VERSION, Type = ColumnTypes.String100}});
            }
            Insert(VERSION_TABLE_NAME, new[] {version.ToString()});
        }

        public void CopyTableToTable(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions)
        {
            if (!TableExists(sourceTableName))
            {
                throw new MigrationOperationException("Cannot copy table to new name, source table does not exist: " + sourceTableName);
            }

            if (TableExists(destinationTableName))
            {
                throw new MigrationOperationException("Cannot copy table to new name, table with same name already exists: " + destinationTableName);
            }

            if (!VerifyTableExists(sourceTableName, columnDefinitions))
            {
                throw new MigrationOperationException("Cannot copy table to new name, source table does not match columnDefinitions: " + destinationTableName);
            }

            EnsureTableExists(destinationTableName, columnDefinitions);
            CopyAllDataBetweenMatchingTables(sourceTableName, destinationTableName, columnDefinitions);
        }

        public bool VerifyTableExists(string tableName, ColumnDefinition[] columnDefinitions)
        {
            if (!TableExists(tableName))
            {
                OpenSim.Framework.Console.MainConsole.Instance.Output("[DataMigrator]: Issue finding table " + tableName + " when verifing tables exist!", "Warn");
                return false;
            }

            List<ColumnDefinition> extractedColumns = ExtractColumnsFromTable(tableName);
            foreach (ColumnDefinition columnDefinition in columnDefinitions)
            {
                if (!extractedColumns.Contains(columnDefinition))
                {
                    OpenSim.Framework.Console.MainConsole.Instance.Output("[DataMigrator]: Issue verifing table " + tableName + " column " + columnDefinition + " when verifing tables exist!", "Warn");
                    return false;
                }
            }

            return true;
        }

        public void EnsureTableExists(string tableName, ColumnDefinition[] columnDefinitions)
        {
            if (TableExists(tableName))
            {
                if (!VerifyTableExists(tableName, columnDefinitions))
                {
                    //throw new MigrationOperationException("Cannot create, table with same name and different columns already exists. This should be fixed in a migration: " + tableName);
                    UpdateTable(tableName, columnDefinitions);
                }
                return;
            }

            CreateTable(tableName, columnDefinitions);
        }

        public abstract void DropTable(string tableName);

        #endregion

        protected abstract void CopyAllDataBetweenMatchingTables(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions);
        protected abstract List<ColumnDefinition> ExtractColumnsFromTable(string tableName);
    }
}