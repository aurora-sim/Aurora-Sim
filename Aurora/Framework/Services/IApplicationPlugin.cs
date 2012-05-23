/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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

using System.Linq;
using System.Collections.Generic;
using System.Data;
using Nini.Config;
using Aurora.Framework;

namespace Aurora.Framework
{
    /// <summary>
    ///   Aurora-Sim Application Plugin framework interface
    /// </summary>
    public interface IApplicationPlugin
    {
        /// <summary>
        ///   Returns the plugin name
        /// </summary>
        /// <returns></returns>
        string Name { get; }

        /// <summary>
        /// Called before any other calls are made, before the console is setup, and before the HTTP server is ready
        /// </summary>
        /// <param name="simBase"></param>
        void PreStartup(ISimulationBase simBase);

        /// <summary>
        ///   Initialize the Plugin
        /// </summary>
        /// <param name = "openSim">The Application instance</param>
        void Initialize(ISimulationBase simBase);

        /// <summary>
        ///   Called when the application initialization is completed
        /// </summary>
        void PostInitialise();

        /// <summary>
        ///   Called when the application loading is completed
        /// </summary>
        void Start();

        /// <summary>
        ///   Called when the application loading is completed
        /// </summary>
        void PostStart();

        /// <summary>
        ///   Close out the module
        /// </summary>
        void Close();

        /// <summary>
        ///   The configuration has changed, make sure that everything is updated with the new info
        /// </summary>
        /// <param name = "m_config"></param>
        void ReloadConfiguration(IConfigSource m_config);
    }
}

namespace Aurora.Framework
{
    public interface IGenericData
    {
        #region UPDATE

        /// <summary>
        /// UPDATE table SET values[i].key = values[i].value {magic happens with queryFilter here} [LIMIT start[, count]]
        /// </summary>
        /// <param name="table">table to update</param>
        /// <param name="values">dictionary of table fields and new values</param>
        /// <param name="incrementValues">dictionary of table fields and integer to increment by (use negative ints to decrement)</param>
        /// <param name="queryFilter">filter to control which rows get updated</param>
        /// <param name="start">LIMIT start or LIMIT start, count</param>
        /// <param name="count">LIMIT start, count</param>
        /// <returns></returns>
        bool Update(string table, Dictionary<string, object> values, Dictionary<string, int> incrementValues, QueryFilter queryFilter, uint? start, uint? count);

        #endregion

        #region SELECT

        /// <summary>
        /// SELECT string.join(", ", wantedValue) FROM table {magic happens with queryFilter here} {magic happens with sort here} [LIMIT start[, count]]
        /// </summary>
        /// <param name="wantedValue"></param>
        /// <param name="table"></param>
        /// <param name="queryFilter"></param>
        /// <param name="sort"></param>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        List<string> Query(string[] wantedValue, string table, QueryFilter queryFilter, Dictionary<string, bool> sort, uint? start, uint? count);

        /// <summary>
        ///   select 'wantedValue' from 'table' 'whereClause'
        /// </summary>
        List<string> QueryFullData(string whereClause, string table, string wantedValue);

        /// <summary>
        ///   select 'wantedValue' from 'table' 'whereClause'
        /// </summary>
        IDataReader QueryData(string whereClause, string table, string wantedValue);

        /// <summary>
        ///   select 'wantedValue' from 'table' where 'keyRow' = 'keyValue'
        ///   This gives the row names as well as the values
        /// </summary>
        Dictionary<string, List<string>> QueryNames(string[] keyRow, object[] keyValue, string table, string wantedValue);

        #region JOIN

        List<string> Query(string[] wantedValue, QueryTables tables, QueryFilter queryFilter, Dictionary<string, bool> sort, uint? start, uint? count);
        Dictionary<string, List<string>> QueryNames(string[] keyRow, object[] keyValue, QueryTables tables, string wantedValue);
        IDataReader QueryData(string whereClause, QueryTables tables, string wantedValue);
        List<string> QueryFullData(string whereClause, QueryTables tables, string wantedValue);

        #endregion

        #endregion

        #region INSERT

        /// <summary>
        ///   insert into 'table' values ('values')
        /// </summary>
        bool Insert(string table, object[] values);

        /// <summary>
        /// INSERT INTO table (row.Keys) VALUES(row.Values)
        /// </summary>
        /// <param name="table"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        bool Insert(string table, Dictionary<string, object> row);

        /// <summary>
        /// Runs multiple Insert(table, value) calls in one run
        /// </summary>
        /// <param name="table"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        bool InsertMultiple(string table, List<object[]> values);

        /// <summary>
        ///   Inserts a row into the database 
        ///   insert into 'table' values ('values') ON DUPLICATE KEY UPDATE 'updateKey' = 'updateValue'
        /// </summary>
        /// <param name = "table">table name</param>
        /// <param name = "values">All values to be inserted in the correct table order</param>
        /// <param name = "updateKey">If a row is already existing, update this key</param>
        /// <param name = "updateValue">If a row is already existing, update this value</param>
        /// <returns></returns>
        bool Insert(string table, object[] values, string updateKey, object updateValue);

        /// <summary>
        /// Inserts rows selected from another table.
        /// </summary>
        /// <param name="tableA"></param>
        /// <param name="fieldsA"></param>
        /// <param name="tableB"></param>
        /// <param name="valuesB"></param>
        /// <returns></returns>
        bool InsertSelect(string tableA, string[] fieldsA, string tableB, string[] valuesB);

        #endregion

        #region REPLACE INTO

        /// <summary>
        /// REPLACE INTO table (row.Keys) VALUES(row.Values)
        /// </summary>
        /// <param name="table">table name</param>
        /// <param name="row"></param>
        /// <returns></returns>
        bool Replace(string table, Dictionary<string, object> row);

        #endregion

        #region DELETE

        /// <summary>
        ///   delete from 'table' where 'key' < now()
        /// </summary>
        /// <param name = "table"></param>
        /// <param name = "keys"></param>
        /// <param name = "values"></param>
        /// <returns></returns>
        bool DeleteByTime(string table, string keys);

        /// <summary>
        /// DELETE FROM table WHERE {magic happens with queryFilter here}
        /// </summary>
        /// <param name="table">table name</param>
        /// <param name="queryFilter">filter for determining which rows to delete</param>
        /// <returns></returns>
        bool Delete(string table, QueryFilter queryFilter);

        #endregion

        /// <summary>
        ///   Connects to the database and then performs migrations
        /// </summary>
        /// <param name = "connectionString"></param>
        void ConnectToDatabase(string connectionString, string migrationName, bool validateTables);

        /// <summary>
        ///   Makes a copy of the IGenericData plugin
        /// </summary>
        /// <returns></returns>
        IGenericData Copy();

        /// <summary>
        ///   Close the given database connection
        /// </summary>
        void CloseDatabase();

        /// <summary>
        ///   in the sql the strings will return joined fields
        /// </summary>
        /// <param name = "toConCat"></param>
        /// <returns></returns>
        string ConCat(string[] toConCat);
    }

    public class QueryFilter
    {
        public Dictionary<string, object> andFilters = new Dictionary<string, object>();
        public Dictionary<string, object> orFilters = new Dictionary<string, object>();
        public Dictionary<string, List<object>> orMultiFilters = new Dictionary<string, List<object>>();

        public Dictionary<string, string> andLikeFilters = new Dictionary<string, string>();
        public Dictionary<string, string> orLikeFilters = new Dictionary<string, string>();
        public Dictionary<string, List<string>> orLikeMultiFilters = new Dictionary<string, List<string>>();

        public Dictionary<string, uint> andBitfieldAndFilters = new Dictionary<string, uint>();
        public Dictionary<string, uint> orBitfieldAndFilters = new Dictionary<string, uint>();

        public Dictionary<string, uint> andBitfieldNandFilters = new Dictionary<string, uint>();

        public Dictionary<string, object> andGreaterThanFilters = new Dictionary<string, object>();
        public Dictionary<string, object> orGreaterThanFilters = new Dictionary<string, object>();

        public Dictionary<string, object> andGreaterThanEqFilters = new Dictionary<string, object>();
        public Dictionary<string, object> orGreaterThanEqFilters = new Dictionary<string, object>();

        public Dictionary<string, object> andLessThanFilters = new Dictionary<string, object>();
        public Dictionary<string, object> orLessThanFilters = new Dictionary<string, object>();

        public Dictionary<string, object> andLessThanEqFilters = new Dictionary<string, object>();

        public Dictionary<string, object> andNotFilters = new Dictionary<string, object>();

        public List<string> andIsNullFilters = new List<string>();
        public List<string> andIsNotNullFilters = new List<string>();

//        public List<QueryFilter> subFilters = new List<QueryFilter>();

        public uint Count
        {
            get
            {
                uint total = (uint)(
                    andFilters.Count +
                    orFilters.Count +
                    orMultiFilters.Count +
                    andLikeFilters.Count +
                    orLikeFilters.Count +
                    orLikeMultiFilters.Count +
                    andBitfieldAndFilters.Count +
                    orBitfieldAndFilters.Count +
                    andBitfieldNandFilters.Count +
                    andGreaterThanFilters.Count +
                    orGreaterThanFilters.Count +
                    andGreaterThanEqFilters.Count +
                    orGreaterThanEqFilters.Count +
                    andLessThanFilters.Count +
                    orLessThanFilters.Count +
                    andLessThanEqFilters.Count +
                    andNotFilters.Count +
                    andIsNullFilters.Count +
                    andIsNotNullFilters.Count
                );

//                subFilters.ForEach(delegate(QueryFilter filter)
//                {
//                    total += filter.Count;
//                });

                return total;
            }
        }

        private static string preparedKey(string key)
        {
            return key.Replace("`", "").Replace("(", "_").Replace(")", "").Replace(" ", "_").Replace("-", "minus").Replace("+", "add").Replace("/", "divide").Replace("*", "multiply").Replace("'", "").Replace(",", "").Replace(".","alias");
        }

        public string ToSQL(char prepared, out Dictionary<string, object> ps)
        {
            ps = new Dictionary<string, object>();
            Dictionary<string, object>[] pss = { ps };
            string query = "";
            List<string> parts;
            uint i = 0;
            bool had = false;
            if (Count > 0)
            {
                query += "(";

                #region equality

                parts = new List<string>();
                foreach (KeyValuePair<string, object> where in andFilters)
                {
                    string key = prepared.ToString() + "where_AND_" + (++i) + preparedKey(where.Key);
                    ps[key] = where.Value;
                    parts.Add(string.Format("{0} = {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, object> where in orFilters)
                {
                    string key = prepared.ToString() + "where_OR_" + (++i) + preparedKey(where.Key);
                    ps[key] = where.Value;
                    parts.Add(string.Format("{0} = {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, List<object>> where in orMultiFilters)
                {
                    foreach (object value in where.Value)
                    {
                        string key = prepared.ToString() + "where_OR_" + (++i) + preparedKey(where.Key);
                        ps[key] = value;
                        parts.Add(string.Format("{0} = {1}", where.Key, key));
                    }
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, object> where in andNotFilters)
                {
                    string key = prepared.ToString() + "where_AND_NOT_" + (++i) + preparedKey(where.Key);
                    ps[key] = where.Value;
                    parts.Add(string.Format("{0} != {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                #endregion

                #region LIKE

                parts = new List<string>();
                foreach (KeyValuePair<string, string> where in andLikeFilters)
                {
                    string key = prepared.ToString() + "where_ANDLIKE_" + (++i) + preparedKey(where.Key);
                    ps[key] = where.Value;
                    parts.Add(string.Format("{0} LIKE {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, string> where in orLikeFilters)
                {
                    string key = prepared.ToString() + "where_ORLIKE_" + (++i) + preparedKey(where.Key);
                    ps[key] = where.Value;
                    parts.Add(string.Format("{0} LIKE {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, List<string>> where in orLikeMultiFilters)
                {
                    foreach (string value in where.Value)
                    {
                        string key = prepared.ToString() + "where_ORLIKE_" + (++i) + preparedKey(where.Key);
                        ps[key] = value;
                        parts.Add(string.Format("{0} LIKE {1}", where.Key, key));
                    }
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                #endregion

                #region bitfield &

                parts = new List<string>();
                foreach (KeyValuePair<string, uint> where in andBitfieldAndFilters)
                {
                    string key = prepared.ToString() + "where_bAND_" + (++i) + preparedKey(where.Key);
                    ps[key] = where.Value;
                    parts.Add(string.Format("{0} & {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, uint> where in orBitfieldAndFilters)
                {
                    string key = prepared.ToString() + "where_bOR_" + (++i) + preparedKey(where.Key);
                    ps[key] = where.Value;
                    parts.Add(string.Format("{0} & {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, uint> where in andBitfieldNandFilters)
                {
                    string key = prepared.ToString() + "where_bNAND_" + (++i) + preparedKey(where.Key);
                    ps[key] = where.Value;
                    parts.Add(string.Format("({0} & {1}) = 0", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                #endregion

                #region greater than

                parts = new List<string>();
                foreach (KeyValuePair<string, object> where in andGreaterThanFilters)
                {
                    string key = prepared.ToString() + "where_gtAND_" + (++i) + preparedKey(where.Key);
                    ps[key] = float.Parse((where.Value).ToString());
                    parts.Add(string.Format("{0} > {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, object> where in orGreaterThanFilters)
                {
                    string key = prepared.ToString() + "where_gtOR_" + (++i) + preparedKey(where.Key);
                    ps[key] = float.Parse((where.Value).ToString());
                    parts.Add(string.Format("{0} > {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, object> where in andGreaterThanEqFilters)
                {
                    string key = prepared.ToString() + "where_gteqAND_" + (++i) + preparedKey(where.Key);
                    ps[key] = float.Parse((where.Value).ToString());
                    parts.Add(string.Format("{0} >= {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, object> where in orGreaterThanEqFilters)
                {
                    string key = prepared.ToString() + "where_gteqOR_" + (++i) + preparedKey(where.Key);
                    ps[key] = float.Parse((where.Value).ToString());
                    parts.Add(string.Format("{0} >= {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                #endregion

                #region less than

                parts = new List<string>();
                foreach (KeyValuePair<string, object> where in andLessThanFilters)
                {
                    string key = prepared.ToString() + "where_ltAND_" + (++i) + preparedKey(where.Key);
                    ps[key] = float.Parse((where.Value).ToString());
                    parts.Add(string.Format("{0} < {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, object> where in orLessThanFilters)
                {
                    string key = prepared.ToString() + "where_ltOR_" + (++i) + preparedKey(where.Key);
                    ps[key] = float.Parse((where.Value).ToString());
                    parts.Add(string.Format("{0} < {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" OR ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (KeyValuePair<string, object> where in andLessThanEqFilters)
                {
                    string key = prepared.ToString() + "where_lteqAND_" + (++i) + preparedKey(where.Key);
                    ps[key] = float.Parse((where.Value).ToString());
                    parts.Add(string.Format("{0} <= {1}", where.Key, key));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                #endregion

                #region NULL

                parts = new List<string>();
                foreach (string field in andIsNotNullFilters)
                {
                    parts.Add(string.Format("{0} IS NOT NULL", field));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                parts = new List<string>();
                foreach (string field in andIsNullFilters)
                {
                    parts.Add(string.Format("{0} IS NULL", field));
                }
                if (parts.Count > 0)
                {
                    query += (had ? " AND" : string.Empty) + " (" + string.Join(" AND ", parts.ToArray()) + ")";
                    had = true;
                }

                #endregion

//                foreach (QueryFilter subFilter in subFilters)
//                {
//                    Dictionary<string, object> sps;
//                    query += (had ? " AND" : string.Empty) + subFilter.ToSQL(prepared, out sps, ref i);
//                    pss[pss.Length] = sps;
//                    if (subFilter.Count > 0)
//                    {
//                        had = true;
//                    }
//                }

                query += ")";
            }
            pss.SelectMany(x => x).ToLookup(x => x.Key, x => x.Value).ToDictionary(x => x.Key, x => x.First());
            return query;
        }
    }

    public class Query2Type<T> : IDataTransferable
    {
        public static List<T> doQuery2Type(List<string> query)
        {
            return new List<T>(0);
        }

        public override void FromKVP(Dictionary<string, object> KVP)
        {
            FromOSD(Util.DictionaryToOSD(KVP));
        }

        public override Dictionary<string, object> ToKVP()
        {
            return Util.OSDToDictionary(ToOSD());
        }
    }

    public interface IAuroraDataPlugin
    {
        /// <summary>
        ///   Returns the plugin name
        /// </summary>
        /// <returns></returns>
        string Name { get; }

        /// <summary>
        ///   Starts the database plugin, performs migrations if needed
        /// </summary>
        /// <param name = "GenericData">The Database Plugin</param>
        /// <param name = "source">Config if more parameters are needed</param>
        /// <param name = "DefaultConnectionString">The connection string to use</param>
        void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string DefaultConnectionString);
    }

    public class QueryTables
    {
        readonly List<QueryTable> tables = new List<QueryTable>();

        public void AddTable(QueryTable newTable)
        {
            tables.Add(newTable);
        }

        /// <summary>
        /// Add main table to select from
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Alias"></param>
        public void AddTable(string Name, string Alias)
        {
            AddTable(new QueryTable(){TableName = Name, TableAlias = Alias});
        }

        /// <summary>
        /// Add secondary table that joins to another table
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Alias"></param>
        /// <param name="jType"></param>
        /// <param name="toJoinOn"></param>
        public void AddTable(string Name, string Alias, JoinType jType, string[,] toJoinOn)
        {
            AddTable(new QueryTable() { JoinOn = toJoinOn, TableAlias = Alias, TableName = Name, TypeJoin = jType });
        }

        public string ToSQL()
        {
            string returnValue = "";
            foreach (QueryTable t in tables)
            {
                if (returnValue == "")
                    returnValue = t.TableName + " AS " + t.TableAlias + " ";
                else
                {
                    if (t.TypeJoin == JoinType.Inner)
                        returnValue += " INNER JOIN " + t.TableName + " AS " + t.TableAlias + " ";
                    else
                        returnValue += " OUTER JOIN " + t.TableName + " AS " + t.TableAlias + " ";
                    for (int loop = 0; loop < t.JoinOn.Length / 2; loop++)
                    {
                        if (loop != 0)
                            returnValue += " AND ";
                        else
                            returnValue += " ON ";
                        returnValue += t.JoinOn[loop, 0] + " = " + t.JoinOn[loop, 1];
                    }
                }
            }
            return returnValue;
        }
    }

    public enum JoinType
    {
        Inner = 1,
        Left = 2
    }

    public class QueryTable
    {
        public string TableName { get; set; }
        public string TableAlias { get; set; }
        public JoinType TypeJoin { get; set; }
        public string[,] JoinOn { get; set; }
    }
}