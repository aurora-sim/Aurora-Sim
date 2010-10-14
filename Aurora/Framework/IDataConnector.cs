using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;
using Nini.Config;

namespace Aurora.Framework
{
    public interface IDataConnector : IGenericData
    {
        void CloseDatabase();
        bool TableExists(string table);
        void CreateTable(string table, ColumnDefinition[] columns);
        Version GetAuroraVersion();
        void WriteAuroraVersion(Version version);
        void CopyTableToTable(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions);
        bool VerifyTableExists(string tableName, ColumnDefinition[] columnDefinitions);
        void EnsureTableExists(string tableName, ColumnDefinition[] columnDefinitions);
        void DropTable(string tableName);
        string Identifier { get; }
    }

    public enum DataManagerTechnology
    {
        SQLite,
        MySql,
        MSSQL2008,
        MSSQL7
    }

    public enum ColumnTypes
    {
        Integer,
        String,
        Date,
        String50,
        String512,
        String45,
        String1024,
        String8196,
        String2,
        String1,
        String100,
        Blob,
        Text
    }
    public class ColumnDefinition
    {
        public string Name { get; set; }
        public ColumnTypes Type { get; set; }
        public bool IsPrimary { get; set; }

        public override bool Equals(object obj)
        {
            var cdef = obj as ColumnDefinition;
            if (cdef != null)
            {
                return cdef.Name == Name && cdef.Type == Type && cdef.IsPrimary == IsPrimary;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
