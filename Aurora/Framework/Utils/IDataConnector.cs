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
using System.Linq;

namespace Aurora.Framework
{
    /// <summary>
    ///   Connector that links Aurora IDataPlugins to a database backend
    /// </summary>
    public interface IDataConnector : IGenericData
    {
        /// <summary>
        ///   Name of the module
        /// </summary>
        string Identifier { get; }

        /// <summary>
        ///   Checks to see if table 'table' exists
        /// </summary>
        /// <param name = "table"></param>
        /// <returns></returns>
        bool TableExists(string table);

        /// <summary>
        /// Creates a table with indices
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columns"></param>
        /// <param name="indexDefinitions"></param>
        void CreateTable(string table, ColumnDefinition[] columns, IndexDefinition[] indexDefinitions);

        /// <summary>
        ///   Get the latest version of the database
        /// </summary>
        /// <returns></returns>
        Version GetAuroraVersion(string migratorName);

        /// <summary>
        ///   Set the version of the database
        /// </summary>
        /// <param name = "version"></param>
        /// <param name = "MigrationName"></param>
        void WriteAuroraVersion(Version version, string MigrationName);

        /// <summary>
        /// copy tables
        /// </summary>
        /// <param name="sourceTableName"></param>
        /// <param name="destinationTableName"></param>
        /// <param name="columnDefinitions"></param>
        /// <param name="indexDefinitions"></param>
        void CopyTableToTable(string sourceTableName, string destinationTableName, ColumnDefinition[] columnDefinitions, IndexDefinition[] indexDefinitions);

        /// <summary>
        ///   Check whether the data table exists and that the columns are correct
        /// </summary>
        /// <param name = "tableName"></param>
        /// <param name = "columnDefinitions"></param>
        /// <returns></returns>
        bool VerifyTableExists(string tableName, ColumnDefinition[] columnDefinitions, IndexDefinition[] indexDefinitions);

        /// <summary>
        ///   Check whether the data table exists and that the columns are correct
        ///   Then create the table if it is not created
        /// </summary>
        /// <param name = "tableName"></param>
        /// <param name = "columnDefinitions"></param>
        void EnsureTableExists(string tableName, ColumnDefinition[] columnDefinitions, IndexDefinition[] indexDefinitions, Dictionary<string, string> renameColumns);

        /// <summary>
        ///   Rename the table from oldTableName to newTableName
        /// </summary>
        /// <param name = "oldTableName"></param>
        /// <param name = "newTableName"></param>
        void RenameTable(string oldTableName, string newTableName);

        /// <summary>
        ///   Drop a table
        /// </summary>
        /// <param name = "tableName"></param>
        void DropTable(string tableName);
    }

    public enum DataManagerTechnology
    {
        SQLite,
        MySql,
        MSSQL2008,
        MSSQL7
    }

    public class SchemaDefinition
    {
        private string m_name;
        /// <summary>
        /// Name of schema
        /// </summary>
        public string Name {
            get{ return m_name; }
        }

        private ColumnDefinition[] m_columns;
        /// <summary>
        /// Columns in schema
        /// </summary>
        public ColumnDefinition[] Columns
        {
            get { return m_columns; }
        }

        private IndexDefinition[] m_indices;
        /// <summary>
        /// Indices in schema
        /// </summary>
        public IndexDefinition[] Indices
        {
            get { return m_indices; }
        }

        /// <summary>
        /// Defines a schema with no indices.
        /// </summary>
        /// <param name="schemaName">Name of schema</param>
        /// <param name="columns">Columns in schema</param>
        public SchemaDefinition(string schemaName, ColumnDefinition[] columns)
        {
            m_name = schemaName;
            m_columns = columns;
            m_indices = new IndexDefinition[0];
        }

        /// <summary>
        /// Defines a schema with indices
        /// </summary>
        /// <param name="schemaName">Name of schema</param>
        /// <param name="columns">Columns in schema</param>
        /// <param name="indices">Indices in schema</param>
        public SchemaDefinition(string schemaName, ColumnDefinition[] columns, IndexDefinition[] indices)
        {
            m_name = schemaName;
            m_columns = columns;
            m_indices = indices;
        }
    }

    public enum ColumnTypes
    {
        Blob,
        LongBlob,
        Char36,
        Char32,
        Char5,
        Date,
        DateTime,
        Double,
        Integer11,
        Integer30,
        UInteger11,
        UInteger30,
        String,
        String1,
        String2,
        String10,
        String16,
        String30,
        String32,
        String36,
        String45,
        String50,
        String64,
        String128,
        String100,
        String255,
        String512,
        String1024,
        String8196,
        Text,
        MediumText,
        LongText,
        TinyInt1,
        TinyInt4,
        Float,
        Unknown
    }

    public enum ColumnType
    {
        Blob,
        LongBlob,
        Char,
        Date,
        DateTime,
        Double,
        Integer,
        String,
        Text,
        MediumText,
        LongText,
        TinyInt,
        Float,
        Boolean,
        UUID,
        Unknown
    }

    public class ColumnTypeDef
    {
        public ColumnType Type { get; set; }
        public uint Size { get; set; }
        public string defaultValue { get; set; }
        public bool isNull { get; set; }
        public bool unsigned { get; set; }
        public bool auto_increment { get; set; }

        public override bool Equals(object obj)
        {
            ColumnTypeDef foo = obj as ColumnTypeDef;
            return (foo != null && foo.Type.ToString() == Type.ToString() && foo.Size == Size && foo.defaultValue == defaultValue && foo.isNull == isNull && foo.unsigned == unsigned && foo.auto_increment == auto_increment);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public class ColumnDefinition
    {
        public string Name { get; set; }
        public ColumnTypeDef Type { get; set; }

        public override bool Equals(object obj)
        {
            var cdef = obj as ColumnDefinition;
            if (cdef != null)
            {
                return cdef.Name == Name && cdef.Type == Type;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public enum IndexType
    {
        Primary,
        Index,
        Unique
    }

    public class IndexDefinition
    {
        public string[] Fields { get; set; }
        public IndexType Type { get; set; }

        public override bool Equals(object obj)
        {
            var idef = obj as IndexDefinition;
            if (idef != null && idef.Type == Type && idef.Fields.Length == Fields.Length)
            {
                uint i = 0;
                return idef.Fields.All(field => field == Fields[i++]);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
