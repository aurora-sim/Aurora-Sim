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
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Aurora.DataManager;
using System.Data.SQLite;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Data.SQLite
{
    /// <summary>
    ///   A Region Data Interface to the SQLite database
    /// </summary>
    public class SQLiteSimulationData : ILegacySimulationDataStore
    {
        private const string terrainSelect = "select * from terrain limit 1";
        private const string landSelect = "select * from land";
        private const string landAccessListSelect = "select distinct * from landaccesslist";
        private const string regionbanListSelect = "select * from regionban";
        private const string regionSettingsSelect = "select * from regionsettings";

        private DataSet ds;
        private SQLiteDataAdapter landAccessListDa;
        private SQLiteDataAdapter landDa;

        private SQLiteConnection m_conn;
        private SQLiteDataAdapter regionSettingsDa;
        private SQLiteDataAdapter terrainDa;

        /***********************************************************************
         *
         *  Public Interface Functions
         *
         **********************************************************************/

        #region ILegacySimulationDataStore Members

        public string Name
        {
            get { return "SQLite"; }
        }

        /// <summary>
        ///   See IRegionDataStore
        ///   <list type = "bullet">
        ///     <item>Initialises RegionData Interface</item>
        ///     <item>Loads and initialises a new SQLite connection and maintains it.</item>
        ///   </list>
        /// </summary>
        /// <param name = "connectionString">the connection string</param>
        public void Initialise(string connectionString)
        {
            try
            {
                ds = new DataSet("Region");

                connectionString = connectionString.Replace("URI=file:", "URI=file:" + Util.BasePathCombine("") + "/");
                m_conn = new SQLiteConnection(connectionString);
                m_conn.Open();

                // SQLiteCommandBuilder shapeCb = new SQLiteCommandBuilder(shapeDa);

                SQLiteCommand terrainSelectCmd = new SQLiteCommand(terrainSelect, m_conn);
                terrainDa = new SQLiteDataAdapter(terrainSelectCmd);

                SQLiteCommand landSelectCmd = new SQLiteCommand(landSelect, m_conn);
                landDa = new SQLiteDataAdapter(landSelectCmd);

                SQLiteCommand landAccessListSelectCmd = new SQLiteCommand(landAccessListSelect, m_conn);
                landAccessListDa = new SQLiteDataAdapter(landAccessListSelectCmd);

                SQLiteCommand regionSettingsSelectCmd = new SQLiteCommand(regionSettingsSelect, m_conn);
                regionSettingsDa = new SQLiteDataAdapter(regionSettingsSelectCmd);

                // This actually does the roll forward assembly stuff
                LegacyMigration m = new LegacyMigration(m_conn, GetType().Assembly, "RegionStore");
                m.Update();

                lock (ds)
                {
                    ds.Tables.Add(createTerrainTable());
                    setupTerrainCommands(terrainDa, m_conn);

                    ds.Tables.Add(createLandTable());
                    setupLandCommands(landDa, m_conn);

                    ds.Tables.Add(createLandAccessListTable());
                    setupLandAccessCommands(landAccessListDa, m_conn);

                    ds.Tables.Add(createRegionSettingsTable());
                    setupRegionSettingsCommands(regionSettingsDa, m_conn);

                    try
                    {
                        terrainDa.Fill(ds.Tables["terrain"]);
                    }
                    catch (Exception)
                    {
                        MainConsole.Instance.Info("[SQLite REGION DB]: Caught fill error on terrain table");
                    }

                    try
                    {
                        landDa.Fill(ds.Tables["land"]);
                    }
                    catch (Exception)
                    {
                        MainConsole.Instance.Info("[SQLite REGION DB]: Caught fill error on land table");
                    }

                    try
                    {
                        landAccessListDa.Fill(ds.Tables["landaccesslist"]);
                    }
                    catch (Exception)
                    {
                        MainConsole.Instance.Info("[SQLite REGION DB]: Caught fill error on landaccesslist table");
                    }

                    try
                    {
                        regionSettingsDa.Fill(ds.Tables["regionsettings"]);
                    }
                    catch (Exception)
                    {
                        MainConsole.Instance.Info("[SQLite REGION DB]: Caught fill error on regionsettings table");
                    }

                    // We have to create a data set mapping for every table, otherwise the IDataAdaptor.Update() will not populate rows with values!
                    // Not sure exactly why this is - this kind of thing was not necessary before - justincc 20100409
                    // Possibly because we manually set up our own DataTables before connecting to the database
                    CreateDataSetMapping(terrainDa, "terrain");
                    CreateDataSetMapping(landDa, "land");
                    CreateDataSetMapping(landAccessListDa, "landaccesslist");
                    CreateDataSetMapping(regionSettingsDa, "regionsettings");
                }
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error(e);
                //TODO: better error for users!
                Thread.Sleep(10000); //Sleep so the user can see the error
                Environment.Exit(23);
            }

            return;
        }

        public void Dispose()
        {
            if (m_conn != null)
            {
                m_conn.Close();
                m_conn = null;
            }
            if (ds != null)
            {
                ds = null;
            }
            if (terrainDa != null)
            {
                terrainDa = null;
            }
            if (landDa != null)
            {
                landDa = null;
            }
            if (landAccessListDa != null)
            {
                landAccessListDa = null;
            }
            if (regionSettingsDa != null)
            {
                regionSettingsDa = null;
            }
        }

        /// <summary>
        ///   Load persisted objects from region storage.
        /// </summary>
        /// <param name = "regionUUID">The region UUID</param>
        /// <returns>List of loaded groups</returns>
        public List<ISceneEntity> LoadObjects(UUID regionUUID, IScene scene)
        {
            Dictionary<UUID, ISceneEntity> createdObjects = new Dictionary<UUID, ISceneEntity>();

            List<ISceneEntity> retvals = new List<ISceneEntity>();
            try
            {
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    string selectExp = "select * from prims where RegionUUID = '" + regionUUID + "'";

                    cmd.CommandText = selectExp;
                    cmd.Connection = m_conn;

                    List<uint> foundLocalIDs = new List<uint>();
                    // Fill root parts
                    using (IDataReader primRow = cmd.ExecuteReader())
                    {
                        while (primRow.Read())
                        {
                            try
                            {
                                SceneObjectPart prim = null;
                                string uuid = primRow["UUID"].ToString();
                                string objID = primRow["SceneGroupID"].ToString();

                                if (uuid == objID) //is new SceneObjectGroup ?
                                {
                                    prim = buildPrim(primRow, scene);
                                    prim.Shape = findPrimShape(uuid);
                                    if (prim.Shape == null)
                                        continue;

                                    if (!foundLocalIDs.Contains(prim.LocalId))
                                        foundLocalIDs.Add(prim.LocalId);
                                    else
                                        prim.LocalId = 0; //Reset it! Only use it once!

                                    SceneObjectGroup group = new SceneObjectGroup(prim, scene);
                                    createdObjects[group.UUID] = group;
                                    retvals.Add(group);
                                    LoadItems(prim);
                                }
                            }
                            catch (Exception e)
                            {
                                MainConsole.Instance.Error(
                                    "[SQLite REGION DB]: Failed create prim object in new group, exception and data follows");
                                MainConsole.Instance.Error("[SQLite REGION DB]: ", e);
                            }
                        }
                    }

                    // Now fill the groups with part data
                    using (IDataReader primRow = cmd.ExecuteReader())
                    {
                        while (primRow.Read())
                        {
                            try
                            {
                                SceneObjectPart prim = null;

                                string uuid = (string)primRow["UUID"];
                                string objID = (string)primRow["SceneGroupID"];

                                if (uuid != objID) //is not new SceneObjectGroup ?
                                {
                                    prim = buildPrim(primRow, scene);
                                    prim.Shape = findPrimShape(uuid);

                                    if (!createdObjects.ContainsKey(new UUID(objID)))
                                    {
                                        Console.WriteLine(
                                            "Found an SceneObjectPart without a SceneObjectGroup! ObjectID: " + objID);
                                        continue;
                                    }
                                    if (!foundLocalIDs.Contains(prim.LocalId))
                                        foundLocalIDs.Add(prim.LocalId);
                                    else
                                        prim.LocalId = 0; //Reset it! Only use it once!

                                    createdObjects[new UUID(objID)].AddChild(prim, prim.LinkNum);
                                    LoadItems(prim);
                                }
                            }
                            catch (Exception e)
                            {
                                MainConsole.Instance.Error(
                                    "[SQLite REGION DB]: Failed create prim object in new group, exception and data follows");
                                MainConsole.Instance.Error("[SQLite REGION DB]: ", e);
                            }
                        }
                    }
                }
            }
            catch { }
            return retvals;
        }

        /// <summary>
        ///   Load the latest terrain revision from region storage
        /// </summary>
        /// <param name = "regionID">the region UUID</param>
        /// <returns>Heightfield data</returns>
        public short[] LoadTerrain(IScene scene, bool revert, int RegionSizeX, int RegionSizeY)
        {
            try
            {
                lock (ds)
                {
                    String sql = "";
                    if (revert)
                    {
                        sql = "select Heightfield,X,Y from terrain" +
                              " where RegionUUID=:RegionUUID and Revert = 'True' order by Revision desc";
                    }
                    else
                    {
                        sql = "select Heightfield,X,Y from terrain" +
                              " where RegionUUID=:RegionUUID and Revert = 'False' order by Revision desc";
                    }

                    using (SQLiteCommand cmd = new SQLiteCommand(sql, m_conn))
                    {
                        cmd.Parameters.Add(new SQLiteParameter(":RegionUUID", scene.RegionInfo.RegionID.ToString()));

                        using (IDataReader row = cmd.ExecuteReader())
                        {
                            if (row.Read())
                            {
                                if (row["X"].ToString() == "-1")
                                {
                                    byte[] heightmap = (byte[])row["Heightfield"];
                                    short[] map = new short[RegionSizeX * RegionSizeX];
                                    double[,] terrain = null;
                                    terrain = new double[RegionSizeX, RegionSizeY];
                                    terrain.Initialize();

                                    using (MemoryStream mstr = new MemoryStream(heightmap))
                                    {
                                        using (BinaryReader br = new BinaryReader(mstr))
                                        {
                                            for (int x = 0; x < RegionSizeX; x++)
                                            {
                                                for (int y = 0; y < RegionSizeY; y++)
                                                {
                                                    terrain[x, y] = br.ReadDouble();
                                                }
                                            }
                                        }
                                    }
                                    for (int x = 0; x < RegionSizeX; x++)
                                    {
                                        for (int y = 0; y < RegionSizeY; y++)
                                        {
                                            map[y * RegionSizeX + x] = (short)(terrain[x, y] * Constants.TerrainCompression);
                                        }
                                    }

                                    this.StoreTerrain(map, scene.RegionInfo.RegionID, revert);
                                    return map;
                                }
                                else
                                {
                                    byte[] heightmap = (byte[])row["Heightfield"];
                                    short[] map = new short[RegionSizeX * RegionSizeX];
                                    int ii = 0;
                                    for (int i = 0; i < heightmap.Length; i += sizeof(short))
                                    {
                                        map[ii] = Utils.BytesToInt16(heightmap, i);
                                        ii++;
                                    }
                                    heightmap = null;
                                    return map;
                                }
                            }
                            else
                            {
                                MainConsole.Instance.Warn("[SQLite REGION DB]: No terrain found for region");
                                return null;
                            }

                            //MainConsole.Instance.Debug("[SQLite REGION DB]: Loaded terrain revision r" + rev.ToString());
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        public void RemoveAllLandObjects(UUID regionUUID)
        {
            lock (ds)
            {
                DataTable land = ds.Tables["land"];
                DataTable landaccesslist = ds.Tables["landaccesslist"];
                DataRow landRow = land.Rows.Find(regionUUID.ToString());
                if (landRow != null)
                {
                    landRow.Delete();
                    land.Rows.Remove(landRow);
                }
#if (!ISWIN)
                List<DataRow> rowsToDelete = new List<DataRow>();
                foreach (DataRow rowToCheck in landaccesslist.Rows)
                {
                    if (rowToCheck["RegionUUID"].ToString() == regionUUID.ToString()) rowsToDelete.Add(rowToCheck);
                }
#else
                List<DataRow> rowsToDelete = landaccesslist.Rows.Cast<DataRow>().Where(rowToCheck => rowToCheck["RegionUUID"].ToString() == regionUUID.ToString()).ToList();
#endif
                foreach (DataRow t in rowsToDelete)
                {
                    t.Delete();
                    landaccesslist.Rows.Remove(t);
                }
            }
            Commit();
        }

        ///<summary>
        ///</summary>
        ///<param name = "regionUUID"></param>
        ///<returns></returns>
        public List<LandData> LoadLandObjects(UUID regionUUID)
        {
            try
            {
                List<LandData> landDataForRegion = new List<LandData>();
                lock (ds)
                {
                    DataTable land = ds.Tables["land"];
                    DataTable landaccesslist = ds.Tables["landaccesslist"];
                    string searchExp = "RegionUUID = '" + regionUUID + "'";
                    DataRow[] rawDataForRegion = land.Select(searchExp);
                    foreach (DataRow rawDataLand in rawDataForRegion)
                    {
                        LandData newLand = buildLandData(rawDataLand);
                        string accessListSearchExp = "LandUUID = '" + newLand.GlobalID + "'";
                        DataRow[] rawDataForLandAccessList = landaccesslist.Select(accessListSearchExp);
                        foreach (DataRow rawDataLandAccess in rawDataForLandAccessList)
                        {
                            newLand.ParcelAccessList.Add(buildLandAccessData(rawDataLandAccess));
                        }

                        landDataForRegion.Add(newLand);
                    }
                }
                return landDataForRegion;
            }
            catch { return null; }
        }

        #endregion

        private PrimitiveBaseShape findPrimShape(string uuid)
        {
            PrimitiveBaseShape shape = null;
            string selectExp = "select * from primshapes where UUID = '" + uuid + "'";
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandText = selectExp;
                cmd.Connection = m_conn;

                using (IDataReader primRow = cmd.ExecuteReader())
                {
                    while (primRow.Read())
                    {
                        try
                        {
                            shape = buildShape(primRow);
                        }
                        catch
                        {
                            shape = PrimitiveBaseShape.Default;
                        }
                    }
                }
            }
            return shape;
        }

        /// <summary>
        ///   Load in a prim's persisted inventory.
        /// </summary>
        /// <param name = "prim">the prim</param>
        private void LoadItems(SceneObjectPart prim)
        {
            // MainConsole.Instance.DebugFormat(":[SQLite REGION DB]: Loading inventory for {0} {1}", prim.Name, prim.UUID);
            IList<TaskInventoryItem> inventory = new List<TaskInventoryItem>();
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                string selectExp = String.Format("select * from primitems where primID = '{0}'", prim.UUID.ToString());

                cmd.CommandText = selectExp;
                cmd.Connection = m_conn;

                // Fill root parts
                using (IDataReader primRow = cmd.ExecuteReader())
                {
                    while (primRow.Read())
                    {
                        TaskInventoryItem item = buildItem(primRow);
                        inventory.Add(item);
                    }
                }
            }

//			MainConsole.Instance.DebugFormat(
//			    "[SQLite REGION DB]: Found {0} items for {1} {2}", dbItemRows.Length, prim.Name, prim.UUID);

            prim.Inventory.RestoreInventoryItems(inventory);
        }

        public void Tainted()
        {
        }

        /// <summary>
        ///   Store a terrain revision in region storage
        /// </summary>
        /// <param name = "ter">terrain heightfield</param>
        /// <param name = "regionID">region UUID</param>
        public void StoreTerrain(short[] ter, UUID regionID, bool Revert)
        {
            lock (ds)
            {
                int revision = Util.UnixTimeSinceEpoch();

                // This is added to get rid of the infinitely growing
                // terrain databases which negatively impact on SQLite
                // over time.  Before reenabling this feature there
                // needs to be a limitter put on the number of
                // revisions in the database, as this old
                // implementation is a DOS attack waiting to happen.

                using (
                    SQLiteCommand cmd =
                        new SQLiteCommand(
                            "delete from terrain where RegionUUID=:RegionUUID and Revision <= :Revision and Revert = :Revert",
                            m_conn))
                {
                    cmd.Parameters.Add(new SQLiteParameter(":Revert", Revert.ToString()));
                    cmd.Parameters.Add(new SQLiteParameter(":RegionUUID", regionID.ToString()));
                    cmd.Parameters.Add(new SQLiteParameter(":Revision", revision));
                    cmd.ExecuteNonQuery();
                }

                // the following is an work around for .NET.  The perf
                // issues associated with it aren't as bad as you think.
                MainConsole.Instance.Debug("[SQLite REGION DB]: Storing terrain revision r" + revision.ToString());
                const string sql = "insert into terrain(RegionUUID, Revision, Heightfield, Revert, X, Y)" +
                                   " values(:RegionUUID, :Revision, :Heightfield, :Revert, :X, :Y)";

                using (SQLiteCommand cmd = new SQLiteCommand(sql, m_conn))
                {
                    cmd.Parameters.Add(new SQLiteParameter(":RegionUUID", regionID.ToString()));
                    cmd.Parameters.Add(new SQLiteParameter(":Revision", revision));
                    byte[] heightmap = new byte[ter.Length*sizeof (short)];
                    int ii = 0;
                    foreach (short t in ter)
                    {
                        Utils.Int16ToBytes(t, heightmap, ii);
                        ii += 2;
                    }
                    cmd.Parameters.Add(new SQLiteParameter(":Heightfield", heightmap));
                    cmd.Parameters.Add(new SQLiteParameter(":Revert", Revert.ToString()));
                    cmd.Parameters.Add(new SQLiteParameter(":X", "0"));
                    cmd.Parameters.Add(new SQLiteParameter(":Y", "0"));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        ///   Store a terrain revision in region storage
        /// </summary>
        /// <param name = "ter">terrain heightfield</param>
        /// <param name = "regionID">region UUID</param>
        public void StoreWater(short[] water, UUID regionID, bool Revert)
        {
            int r = Revert ? 3 : 2; //Use numbers so that we can coexist with terrain
            lock (ds)
            {
                int revision = Util.UnixTimeSinceEpoch();

                // This is added to get rid of the infinitely growing
                // terrain databases which negatively impact on SQLite
                // over time.  Before reenabling this feature there
                // needs to be a limitter put on the number of
                // revisions in the database, as this old
                // implementation is a DOS attack waiting to happen.

                using (
                    SQLiteCommand cmd =
                        new SQLiteCommand(
                            "delete from terrain where RegionUUID=:RegionUUID and Revision <= :Revision and Revert = :Revert",
                            m_conn))
                {
                    cmd.Parameters.Add(new SQLiteParameter(":Revert", r));
                    cmd.Parameters.Add(new SQLiteParameter(":RegionUUID", regionID.ToString()));
                    cmd.Parameters.Add(new SQLiteParameter(":Revision", revision));
                    cmd.ExecuteNonQuery();
                }

                // the following is an work around for .NET.  The perf
                // issues associated with it aren't as bad as you think.
                MainConsole.Instance.Debug("[SQLite REGION DB]: Storing terrain revision r" + revision.ToString());
                const string sql = "insert into terrain(RegionUUID, Revision, Heightfield, Revert, X, Y)" +
                                   " values(:RegionUUID, :Revision, :Heightfield, :Revert, :X, :Y)";

                using (SQLiteCommand cmd = new SQLiteCommand(sql, m_conn))
                {
                    cmd.Parameters.Add(new SQLiteParameter(":RegionUUID", regionID.ToString()));
                    cmd.Parameters.Add(new SQLiteParameter(":Revision", revision));
                    byte[] waterheightmap = new byte[water.Length*sizeof (short)];
                    int ii = 0;
                    foreach (short t in water)
                    {
                        Utils.Int16ToBytes(t, waterheightmap, ii);
                        ii += 2;
                    }
                    cmd.Parameters.Add(new SQLiteParameter(":Heightfield", waterheightmap));
                    cmd.Parameters.Add(new SQLiteParameter(":Revert", r));
                    cmd.Parameters.Add(new SQLiteParameter(":X", 0));
                    cmd.Parameters.Add(new SQLiteParameter(":Y", 0));
                    cmd.ExecuteNonQuery();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        ///   Load the latest terrain revision from region storage
        /// </summary>
        /// <param name = "regionID">the region UUID</param>
        /// <returns>Heightfield data</returns>
        public short[] LoadWater(IScene scene, bool revert, int RegionSizeX, int RegionSizeY)
        {
            try
            {
                lock (ds)
                {
                    double[,] terret = new double[RegionSizeX, RegionSizeY];
                    terret.Initialize();

                    String sql = "";
                    if (revert)
                    {
                        sql = "select Heightfield,X,Y from terrain" +
                              " where RegionUUID=:RegionUUID and Revert = '3' order by Revision desc";
                    }
                    else
                    {
                        sql = "select Heightfield,X,Y from terrain" +
                              " where RegionUUID=:RegionUUID and Revert = '2' order by Revision desc";
                    }

                    using (SQLiteCommand cmd = new SQLiteCommand(sql, m_conn))
                    {
                        cmd.Parameters.Add(new SQLiteParameter(":RegionUUID", scene.RegionInfo.RegionID.ToString()));

                        using (IDataReader row = cmd.ExecuteReader())
                        {
                            if (row.Read())
                            {
                                if (row["X"].ToString() == "-1")
                                {
                                    byte[] heightmap = (byte[])row["Heightfield"];
                                    short[] map = new short[RegionSizeX * RegionSizeX];
                                    int ii = 0;
                                    for (int i = 0; i < heightmap.Length; i += sizeof(double))
                                    {
                                        map[ii] = (short)(Utils.BytesToDouble(heightmap, i) * Constants.TerrainCompression);
                                        ii++;
                                    }
                                    this.StoreWater(map, scene.RegionInfo.RegionID, revert);
                                    return map;
                                }
                                else
                                {
                                    byte[] heightmap = (byte[])row["Heightfield"];
                                    short[] map = new short[RegionSizeX * RegionSizeX];
                                    int ii = 0;
                                    for (int i = 0; i < heightmap.Length; i += sizeof(short))
                                    {
                                        map[ii] = Utils.BytesToInt16(heightmap, i);
                                        ii++;
                                    }
                                    return map;
                                }
                            }
                            else
                            {
                                MainConsole.Instance.Warn("[SQLite REGION DB]: No terrain found for region");
                                return null;
                            }
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        ///<summary>
        ///</summary>
        ///<param name = "globalID"></param>
        public void RemoveLandObject(UUID RegionID, UUID globalID)
        {
            lock (ds)
            {
                // Can't use blanket SQL statements when using SqlAdapters unless you re-read the data into the adapter
                // after you're done.
                // replaced below code with the SQLiteAdapter version.
                //using (SQLiteCommand cmd = new SQLiteCommand(":delete from land where UUID=:UUID", m_conn))
                //{
                //    cmd.Parameters.Add(new SQLiteParameter("::UUID", globalID.ToString()));
                //    cmd.ExecuteNonQuery();
                //}

                //using (SQLiteCommand cmd = new SQLiteCommand(":delete from landaccesslist where LandUUID=:UUID", m_conn))
                //{
                //   cmd.Parameters.Add(new SQLiteParameter("::UUID", globalID.ToString()));
                //    cmd.ExecuteNonQuery();
                //}

                DataTable land = ds.Tables["land"];
                DataTable landaccesslist = ds.Tables["landaccesslist"];
                DataRow landRow = land.Rows.Find(globalID.ToString());
                if (landRow != null)
                {
                    landRow.Delete();
                    land.Rows.Remove(landRow);
                }
#if (!ISWIN)
                List<DataRow> rowsToDelete = new List<DataRow>();
                foreach (DataRow rowToCheck in landaccesslist.Rows)
                {
                    if (rowToCheck["LandUUID"].ToString() == globalID.ToString()) rowsToDelete.Add(rowToCheck);
                }
#else
                List<DataRow> rowsToDelete = landaccesslist.Rows.Cast<DataRow>().Where(rowToCheck => rowToCheck["LandUUID"].ToString() == globalID.ToString()).ToList();
#endif
                foreach (DataRow t in rowsToDelete)
                {
                    t.Delete();
                    landaccesslist.Rows.Remove(t);
                }
            }
            Commit();
        }

        ///<summary>
        ///</summary>
        public void Commit()
        {
            //MainConsole.Instance.Debug(":[SQLite]: Starting commit");
            lock (ds)
            {
                terrainDa.Update(ds, "terrain");
                landDa.Update(ds, "land");
                landAccessListDa.Update(ds, "landaccesslist");
                try
                {
                    regionSettingsDa.Update(ds, "regionsettings");
                }
                catch (SQLiteException SqlEx)
                {
                    throw new Exception(
                        "There was a SQL error or connection string configuration error when saving the region settings.  This could be a bug, it could also happen if ConnectionString is defined in the [DatabaseService] section of StandaloneCommon.ini in the config_include folder.  This could also happen if the config_include folder doesn't exist or if the Aurora.ini [Architecture] section isn't set.  If this is your first time running OpenSimulator, please restart the simulator and bug a developer to fix this!",
                        SqlEx);
                }
                ds.AcceptChanges();
            }
        }

        /// <summary>
        ///   See <see cref = "Commit" />
        /// </summary>
        public void Shutdown()
        {
            Commit();
        }

        /***********************************************************************
         *
         *  Database Definition Functions
         *
         *  This should be db agnostic as we define them in ADO.NET terms
         *
         **********************************************************************/

        protected void CreateDataSetMapping(IDataAdapter da, string tableName)
        {
            ITableMapping dbMapping = da.TableMappings.Add(tableName, tableName);
            foreach (DataColumn col in ds.Tables[tableName].Columns)
            {
                dbMapping.ColumnMappings.Add(col.ColumnName, col.ColumnName);
            }
        }

        ///<summary>
        ///</summary>
        ///<param name = "dt"></param>
        ///<param name = "name"></param>
        ///<param name = "type"></param>
        private void createCol(DataTable dt, string name, Type type)
        {
            DataColumn col = new DataColumn(name, type);
            dt.Columns.Add(col);
        }

        /// <summary>
        ///   Creates the "terrain" table
        /// </summary>
        /// <returns>terrain table DataTable</returns>
        private DataTable createTerrainTable()
        {
            DataTable terrain = new DataTable("terrain");

            createCol(terrain, "RegionUUID", typeof (String));
            createCol(terrain, "Revision", typeof (Int32));
            createCol(terrain, "Heightfield", typeof (Byte[]));

            return terrain;
        }

        /// <summary>
        ///   Creates "land" table
        /// </summary>
        /// <returns>land table DataTable</returns>
        private DataTable createLandTable()
        {
            DataTable land = new DataTable("land");
            createCol(land, "UUID", typeof (String));
            createCol(land, "RegionUUID", typeof (String));
            createCol(land, "LocalLandID", typeof (UInt32));

            // Bitmap is a byte[512]
            createCol(land, "Bitmap", typeof (Byte[]));

            createCol(land, "Name", typeof (String));
            createCol(land, "Desc", typeof (String));
            createCol(land, "OwnerUUID", typeof (String));
            createCol(land, "IsGroupOwned", typeof (Boolean));
            createCol(land, "Area", typeof (Int32));
            createCol(land, "AuctionID", typeof (Int32)); //Unemplemented
            createCol(land, "Category", typeof (Int32)); //Enum OpenMetaverse.Parcel.ParcelCategory
            createCol(land, "ClaimDate", typeof (Int32));
            createCol(land, "ClaimPrice", typeof (Int32));
            createCol(land, "GroupUUID", typeof (string));
            createCol(land, "SalePrice", typeof (Int32));
            createCol(land, "LandStatus", typeof (Int32)); //Enum. OpenMetaverse.Parcel.ParcelStatus
            createCol(land, "LandFlags", typeof (UInt32));
            createCol(land, "LandingType", typeof (Byte));
            createCol(land, "MediaAutoScale", typeof (Byte));
            createCol(land, "MediaTextureUUID", typeof (String));
            createCol(land, "MediaURL", typeof (String));
            createCol(land, "MusicURL", typeof (String));
            createCol(land, "PassHours", typeof (Double));
            createCol(land, "PassPrice", typeof (UInt32));
            createCol(land, "SnapshotUUID", typeof (String));
            createCol(land, "UserLocationX", typeof (Double));
            createCol(land, "UserLocationY", typeof (Double));
            createCol(land, "UserLocationZ", typeof (Double));
            createCol(land, "UserLookAtX", typeof (Double));
            createCol(land, "UserLookAtY", typeof (Double));
            createCol(land, "UserLookAtZ", typeof (Double));
            createCol(land, "AuthbuyerID", typeof (String));
            createCol(land, "OtherCleanTime", typeof (Int32));
            createCol(land, "Dwell", typeof (Int32));

            land.PrimaryKey = new[] {land.Columns["UUID"]};

            return land;
        }

        /// <summary>
        ///   create "landaccesslist" table
        /// </summary>
        /// <returns>Landacceslist DataTable</returns>
        private DataTable createLandAccessListTable()
        {
            DataTable landaccess = new DataTable("landaccesslist");
            createCol(landaccess, "LandUUID", typeof (String));
            createCol(landaccess, "AccessUUID", typeof (String));
            createCol(landaccess, "Flags", typeof (UInt32));

            return landaccess;
        }

        private DataTable createRegionSettingsTable()
        {
            DataTable regionsettings = new DataTable("regionsettings");
            createCol(regionsettings, "regionUUID", typeof (String));
            createCol(regionsettings, "block_terraform", typeof (Int32));
            createCol(regionsettings, "block_fly", typeof (Int32));
            createCol(regionsettings, "allow_damage", typeof (Int32));
            createCol(regionsettings, "restrict_pushing", typeof (Int32));
            createCol(regionsettings, "allow_land_resell", typeof (Int32));
            createCol(regionsettings, "allow_land_join_divide", typeof (Int32));
            createCol(regionsettings, "block_show_in_search", typeof (Int32));
            createCol(regionsettings, "agent_limit", typeof (Int32));
            createCol(regionsettings, "object_bonus", typeof (Double));
            createCol(regionsettings, "maturity", typeof (Int32));
            createCol(regionsettings, "disable_scripts", typeof (Int32));
            createCol(regionsettings, "disable_collisions", typeof (Int32));
            createCol(regionsettings, "disable_physics", typeof (Int32));
            createCol(regionsettings, "terrain_texture_1", typeof (String));
            createCol(regionsettings, "terrain_texture_2", typeof (String));
            createCol(regionsettings, "terrain_texture_3", typeof (String));
            createCol(regionsettings, "terrain_texture_4", typeof (String));
            createCol(regionsettings, "elevation_1_nw", typeof (Double));
            createCol(regionsettings, "elevation_2_nw", typeof (Double));
            createCol(regionsettings, "elevation_1_ne", typeof (Double));
            createCol(regionsettings, "elevation_2_ne", typeof (Double));
            createCol(regionsettings, "elevation_1_se", typeof (Double));
            createCol(regionsettings, "elevation_2_se", typeof (Double));
            createCol(regionsettings, "elevation_1_sw", typeof (Double));
            createCol(regionsettings, "elevation_2_sw", typeof (Double));
            createCol(regionsettings, "water_height", typeof (Double));
            createCol(regionsettings, "terrain_raise_limit", typeof (Double));
            createCol(regionsettings, "terrain_lower_limit", typeof (Double));
            createCol(regionsettings, "use_estate_sun", typeof (Int32));
            createCol(regionsettings, "sandbox", typeof (Int32));
            createCol(regionsettings, "sunvectorx", typeof (Double));
            createCol(regionsettings, "sunvectory", typeof (Double));
            createCol(regionsettings, "sunvectorz", typeof (Double));
            createCol(regionsettings, "fixed_sun", typeof (Int32));
            createCol(regionsettings, "sun_position", typeof (Double));
            createCol(regionsettings, "covenant", typeof (String));
            createCol(regionsettings, "map_tile_ID", typeof (String));
            regionsettings.PrimaryKey = new[] {regionsettings.Columns["regionUUID"]};
            return regionsettings;
        }

        /***********************************************************************
         *
         *  Convert between ADO.NET <=> OpenSim Objects
         *
         *  These should be database independant
         *
         **********************************************************************/

        ///<summary>
        ///</summary>
        ///<param name = "row"></param>
        ///<returns></returns>
        private SceneObjectPart buildPrim(IDataReader row, IScene scene)
        {
            // Code commented.  Uncomment to test the unit test inline.

            // The unit test mentions this commented code for the purposes 
            // of debugging a unit test failure

            // SceneObjectGroup sog = new SceneObjectGroup();
            // SceneObjectPart sop = new SceneObjectPart();
            // sop.LocalId = 1;
            // sop.Name = "object1";
            // sop.Description = "object1";
            // sop.Text = "";
            // sop.SitName = "";
            // sop.TouchName = "";
            // sop.UUID = UUID.Random();
            // sop.Shape = PrimitiveBaseShape.Default;
            // sog.SetRootPart(sop);
            // Add breakpoint in above line.  Check sop fields.

            // TODO: this doesn't work yet because something more
            // interesting has to be done to actually get these values
            // back out.  Not enough time to figure it out yet.

            object[] o = new object[row.FieldCount];
            row.GetValues(o);
            SceneObjectPart prim = new SceneObjectPart(scene);
            int ColorA = 0;
            int ColorR = 0;
            int ColorG = 0;
            int ColorB = 0;

            float PositionX = 0;
            float PositionY = 0;
            float PositionZ = 0;

            float GroupPositionX = 0;
            float GroupPositionY = 0;
            float GroupPositionZ = 0;

            float VelocityX = 0;
            float VelocityY = 0;
            float VelocityZ = 0;

            float AngularVelocityX = 0;
            float AngularVelocityY = 0;
            float AngularVelocityZ = 0;

            float AccelerationX = 0;
            float AccelerationY = 0;
            float AccelerationZ = 0;

            float RotationX = 0;
            float RotationY = 0;
            float RotationZ = 0;
            float RotationW = 0;

            float SitTargetOffsetX = 0;
            float SitTargetOffsetY = 0;
            float SitTargetOffsetZ = 0;

            float SitTargetOrientX = 0;
            float SitTargetOrientY = 0;
            float SitTargetOrientZ = 0;
            float SitTargetOrientW = 0;

            float OmegaX = 0;
            float OmegaY = 0;
            float OmegaZ = 0;

            float CameraEyeOffsetX = 0;
            float CameraEyeOffsetY = 0;
            float CameraEyeOffsetZ = 0;

            float CameraAtOffsetX = 0;
            float CameraAtOffsetY = 0;
            float CameraAtOffsetZ = 0;

            for (int i = 0; i < o.Length; i++)
            {
                string name = row.GetName(i);

                #region Switch

                switch (name)
                {
                    case "UUID":
                        prim.UUID = DBGuid.FromDB(o[i]);
                        break;
                    case "CreatorID":
                        prim.CreatorID = DBGuid.FromDB(o[i].ToString());
                        break;
                    case "OwnerID":
                        prim.OwnerID = DBGuid.FromDB(o[i].ToString());
                        break;
                    case "GroupID":
                        prim.GroupID = DBGuid.FromDB(o[i].ToString());
                        break;
                    case "LastOwnerID":
                        prim.LastOwnerID = DBGuid.FromDB(o[i].ToString());
                        break;
                    case "CreationDate":
                        prim.CreationDate = Convert.ToInt32(o[i].ToString());
                        break;
                    case "Name":
                        if (!(o[i] is DBNull))
                            prim.Name = o[i].ToString();
                        break;
                    case "Text":
                        prim.Text = o[i].ToString();
                        break;
                    case "ColorA":
                        ColorA = Convert.ToInt32(o[i].ToString());
                        break;
                    case "ColorR":
                        ColorR = Convert.ToInt32(o[i].ToString());
                        break;
                    case "ColorG":
                        ColorG = Convert.ToInt32(o[i].ToString());
                        break;
                    case "ColorB":
                        ColorB = Convert.ToInt32(o[i].ToString());
                        break;
                    case "Description":
                        prim.Description = o[i].ToString();
                        break;
                    case "SitName":
                        prim.SitName = o[i].ToString();
                        break;
                    case "TouchName":
                        prim.TouchName = o[i].ToString();
                        break;
                    case "ObjectFlags":
                        prim.Flags = (PrimFlags) Convert.ToUInt32(o[i].ToString());
                        break;
                    case "OwnerMask":
                        prim.OwnerMask = Convert.ToUInt32(o[i].ToString());
                        break;
                    case "NextOwnerMask":
                        prim.NextOwnerMask = Convert.ToUInt32(o[i].ToString());
                        break;
                    case "GroupMask":
                        prim.GroupMask = Convert.ToUInt32(o[i].ToString());
                        break;
                    case "EveryoneMask":
                        prim.EveryoneMask = Convert.ToUInt32(o[i].ToString());
                        break;
                    case "BaseMask":
                        prim.BaseMask = Convert.ToUInt32(o[i].ToString());
                        break;
                    case "PositionX":
                        PositionX = Convert.ToSingle(o[i].ToString());
                        break;
                    case "PositionY":
                        PositionY = Convert.ToSingle(o[i].ToString());
                        break;
                    case "PositionZ":
                        PositionZ = Convert.ToSingle(o[i].ToString());
                        break;
                    case "GroupPositionX":
                        GroupPositionX = Convert.ToSingle(o[i].ToString());
                        break;
                    case "GroupPositionY":
                        GroupPositionY = Convert.ToSingle(o[i].ToString());
                        break;
                    case "GroupPositionZ":
                        GroupPositionZ = Convert.ToSingle(o[i].ToString());
                        break;
                    case "VelocityX":
                        VelocityX = Convert.ToSingle(o[i].ToString());
                        break;
                    case "VelocityY":
                        VelocityY = Convert.ToSingle(o[i].ToString());
                        break;
                    case "VelocityZ":
                        VelocityZ = Convert.ToSingle(o[i].ToString());
                        break;
                    case "AngularVelocityX":
                        AngularVelocityX = Convert.ToSingle(o[i].ToString());
                        break;
                    case "AngularVelocityY":
                        AngularVelocityY = Convert.ToSingle(o[i].ToString());
                        break;
                    case "AngularVelocityZ":
                        AngularVelocityZ = Convert.ToSingle(o[i].ToString());
                        break;
                    case "AccelerationX":
                        AccelerationX = Convert.ToSingle(o[i].ToString());
                        break;
                    case "AccelerationY":
                        AccelerationY = Convert.ToSingle(o[i].ToString());
                        break;
                    case "AccelerationZ":
                        AccelerationZ = Convert.ToSingle(o[i].ToString());
                        break;
                    case "RotationX":
                        RotationX = Convert.ToSingle(o[i].ToString());
                        break;
                    case "RotationY":
                        RotationY = Convert.ToSingle(o[i].ToString());
                        break;
                    case "RotationZ":
                        RotationZ = Convert.ToSingle(o[i].ToString());
                        break;
                    case "RotationW":
                        RotationW = Convert.ToSingle(o[i].ToString());
                        break;
                    case "SitTargetOffsetX":
                        SitTargetOffsetX = Convert.ToSingle(o[i].ToString());
                        break;
                    case "SitTargetOffsetY":
                        SitTargetOffsetY = Convert.ToSingle(o[i].ToString());
                        break;
                    case "SitTargetOffsetZ":
                        SitTargetOffsetZ = Convert.ToSingle(o[i].ToString());
                        break;
                    case "SitTargetOrientX":
                        SitTargetOrientX = Convert.ToSingle(o[i].ToString());
                        break;
                    case "SitTargetOrientY":
                        SitTargetOrientY = Convert.ToSingle(o[i].ToString());
                        break;
                    case "SitTargetOrientZ":
                        SitTargetOrientZ = Convert.ToSingle(o[i].ToString());
                        break;
                    case "SitTargetOrientW":
                        SitTargetOrientW = Convert.ToSingle(o[i].ToString());
                        break;
                    case "PayPrice":
                        prim.PayPrice[0] = Convert.ToInt32(o[i].ToString());
                        break;
                    case "PayButton1":
                        prim.PayPrice[1] = Convert.ToInt32(o[i].ToString());
                        break;
                    case "PayButton2":
                        prim.PayPrice[2] = Convert.ToInt32(o[i].ToString());
                        break;
                    case "PayButton3":
                        prim.PayPrice[3] = Convert.ToInt32(o[i].ToString());
                        break;
                    case "PayButton4":
                        prim.PayPrice[4] = Convert.ToInt32(o[i].ToString());
                        break;
                    case "LoopedSound":
                        prim.Sound = new UUID(o[i].ToString());
                        break;
                    case "LoopedSoundGain":
                        prim.SoundGain = Convert.ToSingle(o[i].ToString());
                        break;
                    case "TextureAnimation":
                        if (!(row[i] is DBNull))
                            prim.TextureAnimation = Convert.FromBase64String(o[i].ToString());
                        break;
                    case "ParticleSystem":
                        if (!(row[i] is DBNull))
                            prim.ParticleSystem = Convert.FromBase64String(o[i].ToString());
                        break;
                    case "OmegaX":
                        OmegaX = Convert.ToSingle(o[i].ToString());
                        break;
                    case "OmegaY":
                        OmegaY = Convert.ToSingle(o[i].ToString());
                        break;
                    case "OmegaZ":
                        OmegaZ = Convert.ToSingle(o[i].ToString());
                        break;
                    case "CameraEyeOffsetX":
                        CameraEyeOffsetX = Convert.ToSingle(o[i].ToString());
                        break;
                    case "CameraEyeOffsetY":
                        CameraEyeOffsetY = Convert.ToSingle(o[i].ToString());
                        break;
                    case "CameraEyeOffsetZ":
                        CameraEyeOffsetZ = Convert.ToSingle(o[i].ToString());
                        break;
                    case "CameraAtOffsetX":
                        CameraAtOffsetX = Convert.ToSingle(o[i].ToString());
                        break;
                    case "CameraAtOffsetY":
                        CameraAtOffsetY = Convert.ToSingle(o[i].ToString());
                        break;
                    case "CameraAtOffsetZ":
                        CameraAtOffsetZ = Convert.ToSingle(o[i].ToString());
                        break;
                    case "ForceMouselook":
                        prim.ForceMouselook = Convert.ToInt32(o[i].ToString()) != 0;
                        break;
                    case "ScriptAccessPin":
                        prim.ScriptAccessPin = Convert.ToInt32(o[i].ToString());
                        break;
                    case "AllowedDrop":
                        prim.AllowedDrop = Convert.ToInt32(o[i].ToString()) != 0;
                        break;
                    case "DieAtEdge":
                        prim.DIE_AT_EDGE = Convert.ToInt32(o[i].ToString()) != 0;
                        break;
                    case "SalePrice":
                        prim.SalePrice = Convert.ToInt32(o[i].ToString());
                        break;
                    case "SaleType":
                        prim.ObjectSaleType = Convert.ToByte(o[i].ToString());
                        break;
                    case "Material":
                        prim.Material = Convert.ToByte(o[i].ToString());
                        break;
                    case "ClickAction":
                        if (!(row[i] is DBNull))
                            prim.ClickAction = Convert.ToByte(o[i].ToString());
                        break;
                    case "CollisionSound":
                        prim.CollisionSound = new UUID(o[i].ToString());
                        break;
                    case "CollisionSoundVolume":
                        prim.CollisionSoundVolume = Convert.ToSingle(o[i].ToString());
                        break;
                    case "PassTouches":
                        prim.PassTouch = (int) Convert.ToSingle(o[i].ToString());
                        break;
                    case "VolumeDetect":
                        if (!(row[i] is DBNull))
                            prim.VolumeDetectActive = Convert.ToInt32(o[i].ToString()) == 1;
                        break;
                    case "LinkNumber":
                        prim.LinkNum = int.Parse(o[i].ToString());
                        break;
                    case "Generic":
                        prim.GenericData = o[i].ToString();
                        break;
                    case "SceneGroupID":
                        break;
                    case "RegionUUID":
                        break;
                    default:
                        MainConsole.Instance.Warn("[NXGSQLite]: Unknown database row: " + name);
                        break;
                }

                #endregion
            }
            prim.SoundFlags = 1; // If it's persisted at all, it's looped

            prim.Color = Color.FromArgb(ColorA, ColorR, ColorG, ColorB);
            prim.FixOffsetPosition(new Vector3(PositionX, PositionY, PositionZ), true);
            prim.FixGroupPosition(new Vector3(GroupPositionX, GroupPositionY, GroupPositionZ), true);
            prim.Velocity = new Vector3(VelocityX, VelocityY, VelocityZ);
            prim.AngularVelocity = new Vector3(AngularVelocityX, AngularVelocityY, AngularVelocityZ);
            prim.RotationOffset = new Quaternion(RotationX, RotationY, RotationZ, RotationW);
            prim.SitTargetPositionLL = new Vector3(SitTargetOffsetX, SitTargetOffsetY, SitTargetOffsetZ);
            prim.SitTargetOrientationLL = new Quaternion(SitTargetOrientX, SitTargetOrientY, SitTargetOrientZ,
                                                         SitTargetOrientW);
            prim.AngularVelocity = new Vector3(OmegaX, OmegaY, OmegaZ);
            prim.CameraEyeOffset = new Vector3(CameraEyeOffsetX, CameraEyeOffsetY, CameraEyeOffsetZ);
            prim.CameraAtOffset = new Vector3(CameraAtOffsetX, CameraAtOffsetY, CameraAtOffsetZ);
            prim.Acceleration = new Vector3(AccelerationX, AccelerationY, AccelerationZ);

            return prim;
        }

        /// <summary>
        ///   Build a prim inventory item from the persisted data.
        /// </summary>
        /// <param name = "row"></param>
        /// <returns></returns>
        private static TaskInventoryItem buildItem(IDataReader row)
        {
            TaskInventoryItem taskItem = new TaskInventoryItem
                                             {
                                                 ItemID = new UUID(row["itemID"].ToString()),
                                                 ParentPartID = new UUID(row["primID"].ToString()),
                                                 AssetID = new UUID(row["assetID"].ToString()),
                                                 ParentID = new UUID(row["parentFolderID"].ToString()),
                                                 InvType = Convert.ToInt32(row["invType"].ToString()),
                                                 Type = Convert.ToInt32(row["assetType"].ToString()),
                                                 Name = row["name"].ToString(),
                                                 Description = row["description"].ToString(),
                                                 CreationDate = Convert.ToUInt32(row["creationDate"].ToString()),
                                                 CreatorIdentification = row["creatorID"].ToString(),
                                                 OwnerID = new UUID(row["ownerID"].ToString()),
                                                 LastOwnerID = new UUID(row["lastOwnerID"].ToString()),
                                                 GroupID = new UUID(row["groupID"].ToString()),
                                                 NextPermissions = Convert.ToUInt32(row["nextPermissions"].ToString()),
                                                 CurrentPermissions =
                                                     Convert.ToUInt32(row["currentPermissions"].ToString()),
                                                 BasePermissions = Convert.ToUInt32(row["basePermissions"].ToString()),
                                                 EveryonePermissions =
                                                     Convert.ToUInt32(row["everyonePermissions"].ToString()),
                                                 GroupPermissions = Convert.ToUInt32(row["groupPermissions"].ToString()),
                                                 Flags = Convert.ToUInt32(row["flags"].ToString()),
                                                 SalePrice = Convert.ToInt32(row["salePrice"]),
                                                 SaleType = Convert.ToByte(row["saleType"])
                                             };





            return taskItem;
        }

        /// <summary>
        ///   Build a Land Data from the persisted data.
        /// </summary>
        /// <param name = "row"></param>
        /// <returns></returns>
        private LandData buildLandData(DataRow row)
        {
            LandData newData = new LandData
                                   {
                                       GlobalID = new UUID((String) row["UUID"]),
                                       LocalID = Convert.ToInt32(row["LocalLandID"]),
                                       Bitmap = (Byte[]) row["Bitmap"],
                                       Name = (String) row["Name"],
                                       Description = (String) row["Desc"],
                                       OwnerID = (UUID) (String) row["OwnerUUID"],
                                       IsGroupOwned = (Boolean) row["IsGroupOwned"],
                                       Area = Convert.ToInt32(row["Area"]),
                                       AuctionID = Convert.ToUInt32(row["AuctionID"]),
                                       Category = (ParcelCategory) Convert.ToInt32(row["Category"]),
                                       ClaimDate = Convert.ToInt32(row["ClaimDate"]),
                                       ClaimPrice = Convert.ToInt32(row["ClaimPrice"]),
                                       GroupID = new UUID((String) row["GroupUUID"]),
                                       SalePrice = Convert.ToInt32(row["SalePrice"]),
                                       Status = (ParcelStatus) Convert.ToInt32(row["LandStatus"]),
                                       Flags = Convert.ToUInt32(row["LandFlags"]),
                                       LandingType = (Byte) row["LandingType"],
                                       MediaAutoScale = (Byte) row["MediaAutoScale"],
                                       MediaID = new UUID((String) row["MediaTextureUUID"]),
                                       MediaURL = (String) row["MediaURL"],
                                       MusicURL = (String) row["MusicURL"],
                                       PassHours = Convert.ToSingle(row["PassHours"]),
                                       PassPrice = Convert.ToInt32(row["PassPrice"]),
                                       SnapshotID = (UUID) (String) row["SnapshotUUID"]
                                   };


            // Bitmap is a byte[512]

            //Unemplemented
            //Enum OpenMetaverse.Parcel.ParcelCategory
            //Enum. OpenMetaverse.Parcel.ParcelStatus
            try
            {
                newData.UserLocation =
                    new Vector3(Convert.ToSingle(row["UserLocationX"]), Convert.ToSingle(row["UserLocationY"]),
                                Convert.ToSingle(row["UserLocationZ"]));
                newData.UserLookAt =
                    new Vector3(Convert.ToSingle(row["UserLookAtX"]), Convert.ToSingle(row["UserLookAtY"]),
                                Convert.ToSingle(row["UserLookAtZ"]));
            }
            catch (InvalidCastException)
            {
                MainConsole.Instance.ErrorFormat(":[SQLite REGION DB]: unable to get parcel telehub settings for {1}", newData.Name);
                newData.UserLocation = Vector3.Zero;
                newData.UserLookAt = Vector3.Zero;
            }
            newData.ParcelAccessList = new List<ParcelManager.ParcelAccessEntry>();
            UUID authBuyerID = UUID.Zero;

            UUID.TryParse((string) row["AuthbuyerID"], out authBuyerID);

            newData.OtherCleanTime = Convert.ToInt32(row["OtherCleanTime"]);
            newData.Dwell = Convert.ToInt32(row["Dwell"]);

            return newData;
        }

        private RegionSettings buildRegionSettings(DataRow row)
        {
            RegionSettings newSettings = new RegionSettings
                                             {
                                                 RegionUUID = new UUID((string) row["regionUUID"]),
                                                 BlockTerraform = Convert.ToBoolean(row["block_terraform"]),
                                                 AllowDamage = Convert.ToBoolean(row["allow_damage"]),
                                                 BlockFly = Convert.ToBoolean(row["block_fly"]),
                                                 RestrictPushing = Convert.ToBoolean(row["restrict_pushing"]),
                                                 AllowLandResell = Convert.ToBoolean(row["allow_land_resell"]),
                                                 AllowLandJoinDivide = Convert.ToBoolean(row["allow_land_join_divide"]),
                                                 BlockShowInSearch = Convert.ToBoolean(row["block_show_in_search"]),
                                                 AgentLimit = Convert.ToInt32(row["agent_limit"]),
                                                 ObjectBonus = Convert.ToDouble(row["object_bonus"]),
                                                 Maturity = Convert.ToInt32(row["maturity"]),
                                                 DisableScripts = Convert.ToBoolean(row["disable_scripts"]),
                                                 DisableCollisions = Convert.ToBoolean(row["disable_collisions"]),
                                                 DisablePhysics = Convert.ToBoolean(row["disable_physics"]),
                                                 TerrainTexture1 = new UUID((String) row["terrain_texture_1"]),
                                                 TerrainTexture2 = new UUID((String) row["terrain_texture_2"]),
                                                 TerrainTexture3 = new UUID((String) row["terrain_texture_3"]),
                                                 TerrainTexture4 = new UUID((String) row["terrain_texture_4"]),
                                                 Elevation1NW = Convert.ToDouble(row["elevation_1_nw"]),
                                                 Elevation2NW = Convert.ToDouble(row["elevation_2_nw"]),
                                                 Elevation1NE = Convert.ToDouble(row["elevation_1_ne"]),
                                                 Elevation2NE = Convert.ToDouble(row["elevation_2_ne"]),
                                                 Elevation1SE = Convert.ToDouble(row["elevation_1_se"]),
                                                 Elevation2SE = Convert.ToDouble(row["elevation_2_se"]),
                                                 Elevation1SW = Convert.ToDouble(row["elevation_1_sw"]),
                                                 Elevation2SW = Convert.ToDouble(row["elevation_2_sw"]),
                                                 WaterHeight = Convert.ToDouble(row["water_height"]),
                                                 TerrainRaiseLimit = Convert.ToDouble(row["terrain_raise_limit"]),
                                                 TerrainLowerLimit = Convert.ToDouble(row["terrain_lower_limit"]),
                                                 UseEstateSun = Convert.ToBoolean(row["use_estate_sun"]),
                                                 Sandbox = Convert.ToBoolean(row["sandbox"]),
                                                 SunVector = new Vector3(
                                                     Convert.ToSingle(row["sunvectorx"]),
                                                     Convert.ToSingle(row["sunvectory"]),
                                                     Convert.ToSingle(row["sunvectorz"])
                                                     ),
                                                 FixedSun = Convert.ToBoolean(row["fixed_sun"]),
                                                 SunPosition = Convert.ToDouble(row["sun_position"]),
                                                 Covenant = new UUID(row["covenant"].ToString()),
                                                 CovenantLastUpdated =
                                                     Convert.ToInt32(row["covenantlastupdated"].ToString()),
                                                 TerrainImageID = new UUID(row["map_tile_ID"].ToString()),
                                                 TerrainMapImageID = new UUID(row["terrain_tile_ID"].ToString()),
                                                 MinimumAge = Convert.ToInt32(row["minimum_age"]),
                                                 LoadedCreationDateTime =
                                                     int.Parse(row["loaded_creation_datetime"].ToString())
                                             };

            if (row["loaded_creation_id"] is DBNull)
                newSettings.LoadedCreationID = "";
            else
                newSettings.LoadedCreationID = row["loaded_creation_id"].ToString();

            OSD o = OSDParser.DeserializeJson(row["generic"].ToString());
            if (o.Type == OSDType.Map)
                newSettings.Generic = (OSDMap) o;

            return newSettings;
        }

        /// <summary>
        ///   Build a land access entry from the persisted data.
        /// </summary>
        /// <param name = "row"></param>
        /// <returns></returns>
        private ParcelManager.ParcelAccessEntry buildLandAccessData(DataRow row)
        {
            ParcelManager.ParcelAccessEntry entry = new ParcelManager.ParcelAccessEntry
                                                        {
                                                            AgentID = new UUID((string) row["AccessUUID"]),
                                                            Flags = (AccessList) row["Flags"],
                                                            Time = new DateTime()
                                                        };
            return entry;
        }

        ///<summary>
        ///</summary>
        ///<param name = "row"></param>
        ///<returns></returns>
        private PrimitiveBaseShape buildShape(IDataReader row)
        {
            PrimitiveBaseShape s = new PrimitiveBaseShape();

            float ScaleX = 0;
            float ScaleY = 0;
            float ScaleZ = 0;

            object[] o = new object[row.FieldCount];
            row.GetValues(o);
            for (int i = 0; i < o.Length; i++)
            {
                string name = row.GetName(i);

                #region Switch

                switch (name)
                {
                    case "ScaleX":
                        ScaleX = Convert.ToSingle(o[i]);
                        break;
                    case "ScaleY":
                        ScaleY = Convert.ToSingle(o[i]);
                        break;
                    case "ScaleZ":
                        ScaleZ = Convert.ToSingle(o[i]);
                        break;
                    case "PCode":
                        s.PCode = Convert.ToByte(o[i]);
                        break;
                    case "PathBegin":
                        s.PathBegin = Convert.ToUInt16(o[i]);
                        break;
                    case "PathEnd":
                        s.PathEnd = Convert.ToUInt16(o[i]);
                        break;
                    case "PathScaleX":
                        s.PathScaleX = Convert.ToByte(o[i]);
                        break;
                    case "PathScaleY":
                        s.PathScaleY = Convert.ToByte(o[i]);
                        break;
                    case "PathShearX":
                        s.PathShearX = Convert.ToByte(o[i]);
                        break;
                    case "PathShearY":
                        s.PathShearY = Convert.ToByte(o[i]);
                        break;
                    case "PathSkew":
                        s.PathSkew = Convert.ToSByte(o[i]);
                        break;
                    case "PathCurve":
                        s.PathCurve = Convert.ToByte(o[i]);
                        break;
                    case "PathRadiusOffset":
                        s.PathRadiusOffset = Convert.ToSByte(o[i]);
                        break;
                    case "PathRevolutions":
                        s.PathRevolutions = Convert.ToByte(o[i]);
                        break;
                    case "PathTaperX":
                        s.PathTaperX = Convert.ToSByte(o[i]);
                        break;
                    case "PathTaperY":
                        s.PathTaperY = Convert.ToSByte(o[i]);
                        break;
                    case "PathTwist":
                        s.PathTwist = Convert.ToSByte(o[i]);
                        break;
                    case "PathTwistBegin":
                        s.PathTwistBegin = Convert.ToSByte(o[i]);
                        break;
                    case "ProfileBegin":
                        s.ProfileBegin = Convert.ToUInt16(o[i]);
                        break;
                    case "ProfileEnd":
                        s.ProfileEnd = Convert.ToUInt16(o[i]);
                        break;
                    case "ProfileCurve":
                        s.ProfileCurve = Convert.ToByte(o[i]);
                        break;
                    case "ProfileHollow":
                        s.ProfileHollow = Convert.ToUInt16(o[i]);
                        break;
                    case "State":
                        s.State = Convert.ToByte(o[i]);
                        break;
                    case "Texture":
                        byte[] textureEntry = (byte[]) o[i];
                        s.TextureEntry = textureEntry;
                        break;
                    case "ExtraParams":
                        s.ExtraParams = (byte[]) o[i];
                        break;
                    case "UUID":
                        break;
                    case "Shape":
                        break;
                    default:
                        MainConsole.Instance.Warn("[NXGSQLite]: Found a row in BuildShape that was not implemented " + name);
                        break;
                }

                #endregion
            }
            s.Scale = new Vector3(ScaleX, ScaleY, ScaleZ);
            return s;
        }

        /***********************************************************************
         *
         *  SQL Statement Creation Functions
         *
         *  These functions create SQL statements for update, insert, and create.
         *  They can probably be factored later to have a db independant
         *  portion and a db specific portion
         *
         **********************************************************************/

        /// <summary>
        ///   Create an insert command
        /// </summary>
        /// <param name = "table">table name</param>
        /// <param name = "dt">data table</param>
        /// <returns>the created command</returns>
        /// <remarks>
        ///   This is subtle enough to deserve some commentary.
        ///   Instead of doing *lots* and *lots of hardcoded strings
        ///   for database definitions we'll use the fact that
        ///   realistically all insert statements look like "insert
        ///   into A(b, c) values(:b, :c) on the parameterized query
        ///   front.  If we just have a list of b, c, etc... we can
        ///   generate these strings instead of typing them out.
        /// </remarks>
        private SQLiteCommand createInsertCommand(string table, DataTable dt)
        {
            string[] cols = new string[dt.Columns.Count];
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                DataColumn col = dt.Columns[i];
                cols[i] = col.ColumnName;
            }

            string sql = "insert into " + table + "(";
            sql += String.Join(", ", cols);
            // important, the first ':' needs to be here, the rest get added in the join
            sql += ") values (:";
            sql += String.Join(", :", cols);
            sql += ")";
            //MainConsole.Instance.DebugFormat(":[SQLite]: Created insert command {0}", sql);
            SQLiteCommand cmd = new SQLiteCommand(sql);

            // this provides the binding for all our parameters, so
            // much less code than it used to be
            foreach (DataColumn col in dt.Columns)
            {
                cmd.Parameters.Add(createSQLiteParameter(col.ColumnName, col.DataType));
            }
            return cmd;
        }


        /// <summary>
        ///   create an update command
        /// </summary>
        /// <param name = "table">table name</param>
        /// <param name = "pk"></param>
        /// <param name = "dt"></param>
        /// <returns>the created command</returns>
        private SQLiteCommand createUpdateCommand(string table, string pk, DataTable dt)
        {
            string sql = "update " + table + " set ";
            string subsql = String.Empty;
            foreach (DataColumn col in dt.Columns)
            {
                if (subsql.Length > 0)
                {
                    // a map function would rock so much here
                    subsql += ", ";
                }
                subsql += col.ColumnName + "= :" + col.ColumnName;
            }
            sql += subsql;
            sql += " where " + pk;
            SQLiteCommand cmd = new SQLiteCommand(sql);

            // this provides the binding for all our parameters, so
            // much less code than it used to be

            foreach (DataColumn col in dt.Columns)
            {
                cmd.Parameters.Add(createSQLiteParameter(col.ColumnName, col.DataType));
            }
            return cmd;
        }

        /// <summary>
        ///   create an update command
        /// </summary>
        /// <param name = "table">table name</param>
        /// <param name = "pk"></param>
        /// <param name = "dt"></param>
        /// <returns>the created command</returns>
        private SQLiteCommand createUpdateCommand(string table, string pk1, string pk2, DataTable dt)
        {
            string sql = "update " + table + " set ";
            string subsql = String.Empty;
            foreach (DataColumn col in dt.Columns)
            {
                if (subsql.Length > 0)
                {
                    // a map function would rock so much here
                    subsql += ", ";
                }
                subsql += col.ColumnName + "= :" + col.ColumnName;
            }
            sql += subsql;
            sql += " where " + pk1 + " and " + pk2;
            SQLiteCommand cmd = new SQLiteCommand(sql);

            // this provides the binding for all our parameters, so
            // much less code than it used to be

            foreach (DataColumn col in dt.Columns)
            {
                cmd.Parameters.Add(createSQLiteParameter(col.ColumnName, col.DataType));
            }
            return cmd;
        }

        /***********************************************************************
         *
         *  Database Binding functions
         *
         *  These will be db specific due to typing, and minor differences
         *  in databases.
         *
         **********************************************************************/

        ///<summary>
        ///  This is a convenience function that collapses 5 repetitive
        ///  lines for defining SQLiteParameters to 2 parameters:
        ///  column name and database type.
        ///
        ///  It assumes certain conventions like :param as the param
        ///  name to replace in parametrized queries, and that source
        ///  version is always current version, both of which are fine
        ///  for us.
        ///</summary>
        ///<returns>a built SQLite parameter</returns>
        private SQLiteParameter createSQLiteParameter(string name, Type type)
        {
            SQLiteParameter param = new SQLiteParameter
                                        {
                                            ParameterName = ":" + name,
                                            DbType = dbtypeFromType(type),
                                            SourceColumn = name,
                                            SourceVersion = DataRowVersion.Current
                                        };
            return param;
        }

        private void setupTerrainCommands(SQLiteDataAdapter da, SQLiteConnection conn)
        {
            da.InsertCommand = createInsertCommand("terrain", ds.Tables["terrain"]);
            da.InsertCommand.Connection = conn;
        }

        private void setupLandCommands(SQLiteDataAdapter da, SQLiteConnection conn)
        {
            da.InsertCommand = createInsertCommand("land", ds.Tables["land"]);
            da.InsertCommand.Connection = conn;

            da.UpdateCommand = createUpdateCommand("land", "UUID=:UUID", ds.Tables["land"]);
            da.UpdateCommand.Connection = conn;

            SQLiteCommand delete = new SQLiteCommand("delete from land where UUID=:UUID");
            delete.Parameters.Add(createSQLiteParameter("UUID", typeof (String)));
            da.DeleteCommand = delete;
            da.DeleteCommand.Connection = conn;
        }

        private void setupLandAccessCommands(SQLiteDataAdapter da, SQLiteConnection conn)
        {
            da.InsertCommand = createInsertCommand("landaccesslist", ds.Tables["landaccesslist"]);
            da.InsertCommand.Connection = conn;

            da.UpdateCommand = createUpdateCommand("landaccesslist", "LandUUID=:landUUID", "AccessUUID=:AccessUUID",
                                                   ds.Tables["landaccesslist"]);
            da.UpdateCommand.Connection = conn;

            SQLiteCommand delete =
                new SQLiteCommand("delete from landaccesslist where LandUUID= :LandUUID and AccessUUID= :AccessUUID");
            delete.Parameters.Add(createSQLiteParameter("LandUUID", typeof (String)));
            delete.Parameters.Add(createSQLiteParameter("AccessUUID", typeof (String)));
            da.DeleteCommand = delete;
            da.DeleteCommand.Connection = conn;
        }

        private void setupRegionSettingsCommands(SQLiteDataAdapter da, SQLiteConnection conn)
        {
            da.InsertCommand = createInsertCommand("regionsettings", ds.Tables["regionsettings"]);
            da.InsertCommand.Connection = conn;
            da.UpdateCommand = createUpdateCommand("regionsettings", "regionUUID=:regionUUID",
                                                   ds.Tables["regionsettings"]);
            da.UpdateCommand.Connection = conn;
        }

        /***********************************************************************
         *
         *  Type conversion functions
         *
         **********************************************************************/

        /// <summary>
        ///   Type conversion function
        /// </summary>
        /// <param name = "type"></param>
        /// <returns></returns>
        private DbType dbtypeFromType(Type type)
        {
            if (type == typeof (String))
            {
                return DbType.String;
            }
            else if (type == typeof (Int32))
            {
                return DbType.Int32;
            }
            else if (type == typeof (Double))
            {
                return DbType.Double;
            }
            else if (type == typeof (Byte))
            {
                return DbType.Byte;
            }
            else if (type == typeof (Double))
            {
                return DbType.Double;
            }
            else if (type == typeof (Byte[]))
            {
                return DbType.Binary;
            }
            else
            {
                return DbType.String;
            }
        }
    }
}