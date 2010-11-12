/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using System.Reflection;
using log4net;
using Mono.Data.Sqlite;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Data.SQLite
{
    /// <summary>
    /// A Region Data Interface to the SQLite database
    /// </summary>
    public class SQLiteSimulationData : ISimulationDataStore
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const string terrainSelect = "select * from terrain limit 1";
        private const string landSelect = "select * from land";
        private const string landAccessListSelect = "select distinct * from landaccesslist";
        private const string regionbanListSelect = "select * from regionban";
        private const string regionSettingsSelect = "select * from regionsettings";

        private DataSet ds;
        private SqliteDataAdapter terrainDa;
        private SqliteDataAdapter landDa;
        private SqliteDataAdapter landAccessListDa;
        private SqliteDataAdapter regionSettingsDa;

        private SqliteConnection m_conn;

        /***********************************************************************
         *
         *  Public Interface Functions
         *
         **********************************************************************/

        public SQLiteSimulationData()
        {
        }

        public SQLiteSimulationData(string connectionString)
        {
            Initialise(connectionString);
        }

        /// <summary>
        /// See IRegionDataStore
        /// <list type="bullet">
        /// <item>Initialises RegionData Interface</item>
        /// <item>Loads and initialises a new SQLite connection and maintains it.</item>
        /// </list>
        /// </summary>
        /// <param name="connectionString">the connection string</param>
        public void Initialise(string connectionString)
        {
            try
            {
                ds = new DataSet("Region");

                m_log.Info("[SQLITE REGION DB]: Sqlite - connecting: " + connectionString);
                m_conn = new SqliteConnection(connectionString);
                m_conn.Open();

                // SqliteCommandBuilder shapeCb = new SqliteCommandBuilder(shapeDa);

                SqliteCommand terrainSelectCmd = new SqliteCommand(terrainSelect, m_conn);
                terrainDa = new SqliteDataAdapter(terrainSelectCmd);

                SqliteCommand landSelectCmd = new SqliteCommand(landSelect, m_conn);
                landDa = new SqliteDataAdapter(landSelectCmd);

                SqliteCommand landAccessListSelectCmd = new SqliteCommand(landAccessListSelect, m_conn);
                landAccessListDa = new SqliteDataAdapter(landAccessListSelectCmd);

                SqliteCommand regionSettingsSelectCmd = new SqliteCommand(regionSettingsSelect, m_conn);
                regionSettingsDa = new SqliteDataAdapter(regionSettingsSelectCmd);

                // This actually does the roll forward assembly stuff
                Migration m = new Migration(m_conn, GetType().Assembly, "RegionStore");
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
                        m_log.Info("[SQLITE REGION DB]: Caught fill error on terrain table");
                    }

                    try
                    {
                        landDa.Fill(ds.Tables["land"]);
                    }
                    catch (Exception)
                    {
                        m_log.Info("[SQLITE REGION DB]: Caught fill error on land table");
                    }

                    try
                    {
                        landAccessListDa.Fill(ds.Tables["landaccesslist"]);
                    }
                    catch (Exception)
                    {
                        m_log.Info("[SQLITE REGION DB]: Caught fill error on landaccesslist table");
                    }

                    try
                    {
                        regionSettingsDa.Fill(ds.Tables["regionsettings"]);
                    }
                    catch (Exception)
                    {
                        m_log.Info("[SQLITE REGION DB]: Caught fill error on regionsettings table");
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
                m_log.Error(e);
                //TODO: better error for users!
                System.Threading.Thread.Sleep(10000); //Sleep so the user can see the error
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

        public void StoreRegionSettings(RegionSettings rs)
        {
            lock (ds)
            {
                DataTable regionsettings = ds.Tables["regionsettings"];

                DataRow settingsRow = regionsettings.Rows.Find(rs.RegionUUID.ToString());
                if (settingsRow == null)
                {
                    settingsRow = regionsettings.NewRow();
                    fillRegionSettingsRow(settingsRow, rs);
                    regionsettings.Rows.Add(settingsRow);
                }
                else
                {
                    fillRegionSettingsRow(settingsRow, rs);
                }

                Commit();
            }
        }

        public RegionSettings LoadRegionSettings(UUID regionUUID)
        {
            lock (ds)
            {
                DataTable regionsettings = ds.Tables["regionsettings"];

                string searchExp = "regionUUID = '" + regionUUID.ToString() + "'";
                DataRow[] rawsettings = regionsettings.Select(searchExp);
                if (rawsettings.Length == 0)
                {
                    RegionSettings rs = new RegionSettings();
                    rs.RegionUUID = regionUUID;
                    rs.OnSave += StoreRegionSettings;

                    StoreRegionSettings(rs);

                    return rs;
                }
                DataRow row = rawsettings[0];

                RegionSettings newSettings = buildRegionSettings(row);
                newSettings.OnSave += StoreRegionSettings;

                return newSettings;
            }
        }

        /// <summary>
        /// Adds an object into region storage
        /// </summary>
        /// <param name="obj">the object</param>
        /// <param name="regionUUID">the region UUID</param>
        public void StoreObject(SceneObjectGroup obj, UUID regionUUID)
        {
            uint flags = obj.RootPart.GetEffectiveObjectFlags();

            // Eligibility check
            //
            if ((flags & (uint)PrimFlags.Temporary) != 0)
                return;
            if ((flags & (uint)PrimFlags.TemporaryOnRez) != 0)
                return;

            using (SqliteCommand cmd = new SqliteCommand())
            {
                cmd.Connection = m_conn;
                foreach (SceneObjectPart prim in obj.ChildrenList)
                {
                    try
                    {
                        cmd.Parameters.Clear();

                        cmd.CommandText = "replace into prims (" +
                                "UUID, CreationDate, " +
                                "Name, Text, Description, " +
                                "SitName, TouchName, ObjectFlags, " +
                                "OwnerMask, NextOwnerMask, GroupMask, " +
                                "EveryoneMask, BaseMask, PositionX, " +
                                "PositionY, PositionZ, GroupPositionX, " +
                                "GroupPositionY, GroupPositionZ, VelocityX, " +
                                "VelocityY, VelocityZ, AngularVelocityX, " +
                                "AngularVelocityY, AngularVelocityZ, " +
                                "AccelerationX, AccelerationY, " +
                                "AccelerationZ, RotationX, " +
                                "RotationY, RotationZ, " +
                                "RotationW, SitTargetOffsetX, " +
                                "SitTargetOffsetY, SitTargetOffsetZ, " +
                                "SitTargetOrientW, SitTargetOrientX, " +
                                "SitTargetOrientY, SitTargetOrientZ, " +
                                "RegionUUID, CreatorID, " +
                                "OwnerID, GroupID, " +
                                "LastOwnerID, SceneGroupID, " +
                                "PayPrice, PayButton1, " +
                                "PayButton2, PayButton3, " +
                                "PayButton4, LoopedSound, " +
                                "LoopedSoundGain, TextureAnimation, " +
                                "OmegaX, OmegaY, OmegaZ, " +
                                "CameraEyeOffsetX, CameraEyeOffsetY, " +
                                "CameraEyeOffsetZ, CameraAtOffsetX, " +
                                "CameraAtOffsetY, CameraAtOffsetZ, " +
                                "ForceMouselook, ScriptAccessPin, " +
                                "AllowedDrop, DieAtEdge, " +
                                "SalePrice, SaleType, " +
                                "ColorR, ColorG, ColorB, ColorA, " +
                                "ParticleSystem, ClickAction, Material, " +
                                "CollisionSound, CollisionSoundVolume, " +
                                "PassTouches, " +
                                "LinkNumber, Generic) values (" + ":UUID, " +
                                ":CreationDate, :Name, :Text, " +
                                ":Description, :SitName, :TouchName, " +
                                ":ObjectFlags, :OwnerMask, :NextOwnerMask, " +
                                ":GroupMask, :EveryoneMask, :BaseMask, " +
                                ":PositionX, :PositionY, :PositionZ, " +
                                ":GroupPositionX, :GroupPositionY, " +
                                ":GroupPositionZ, :VelocityX, " +
                                ":VelocityY, :VelocityZ, :AngularVelocityX, " +
                                ":AngularVelocityY, :AngularVelocityZ, " +
                                ":AccelerationX, :AccelerationY, " +
                                ":AccelerationZ, :RotationX, " +
                                ":RotationY, :RotationZ, " +
                                ":RotationW, :SitTargetOffsetX, " +
                                ":SitTargetOffsetY, :SitTargetOffsetZ, " +
                                ":SitTargetOrientW, :SitTargetOrientX, " +
                                ":SitTargetOrientY, :SitTargetOrientZ, " +
                                ":RegionUUID, :CreatorID, :OwnerID, " +
                                ":GroupID, :LastOwnerID, :SceneGroupID, " +
                                ":PayPrice, :PayButton1, :PayButton2, " +
                                ":PayButton3, :PayButton4, :LoopedSound, " +
                                ":LoopedSoundGain, :TextureAnimation, " +
                                ":OmegaX, :OmegaY, :OmegaZ, " +
                                ":CameraEyeOffsetX, :CameraEyeOffsetY, " +
                                ":CameraEyeOffsetZ, :CameraAtOffsetX, " +
                                ":CameraAtOffsetY, :CameraAtOffsetZ, " +
                                ":ForceMouselook, :ScriptAccessPin, " +
                                ":AllowedDrop, :DieAtEdge, :SalePrice, " +
                                ":SaleType, :ColorR, :ColorG, " +
                                ":ColorB, :ColorA, :ParticleSystem, " +
                                ":ClickAction, :Material, :CollisionSound, " +
                                ":CollisionSoundVolume, :PassTouches, :LinkNumber, :Generic)";

                        FillPrimCommand(cmd, prim, obj.UUID, regionUUID);

                        cmd.ExecuteNonQuery();

                        cmd.Parameters.Clear();

                        cmd.CommandText = "replace into primshapes (" +
                                "UUID, Shape, ScaleX, ScaleY, " +
                                "ScaleZ, PCode, PathBegin, PathEnd, " +
                                "PathScaleX, PathScaleY, PathShearX, " +
                                "PathShearY, PathSkew, PathCurve, " +
                                "PathRadiusOffset, PathRevolutions, " +
                                "PathTaperX, PathTaperY, PathTwist, " +
                                "PathTwistBegin, ProfileBegin, ProfileEnd, " +
                                "ProfileCurve, ProfileHollow, Texture, " +
                                "ExtraParams, State) values (:UUID, " +
                                ":Shape, :ScaleX, :ScaleY, :ScaleZ, " +
                                ":PCode, :PathBegin, :PathEnd, " +
                                ":PathScaleX, :PathScaleY, " +
                                ":PathShearX, :PathShearY, " +
                                ":PathSkew, :PathCurve, :PathRadiusOffset, " +
                                ":PathRevolutions, :PathTaperX, " +
                                ":PathTaperY, :PathTwist, " +
                                ":PathTwistBegin, :ProfileBegin, " +
                                ":ProfileEnd, :ProfileCurve, " +
                                ":ProfileHollow, :Texture, :ExtraParams, " +
                                ":State)";

                        FillShapeCommand(cmd, prim);

                        cmd.ExecuteNonQuery();
                    }
                    catch( Exception ex)
                    {
                        string mes = ex.Message.Replace("\n", "");
                        mes = mes.Replace("\r", "");
                        m_log.Warn("[NxGSQLite]: Error saving prim " + mes);
                    }
                }
                cmd.Dispose();
            }
        }

        public void RemoveObjects(List<UUID> objGroups)
        {
            // m_log.InfoFormat(":[REGION DB]: Removing obj: {0} from region: {1}", obj.Guid, regionUUID);

            List<UUID> uuids = new List<UUID>();

            using (SqliteCommand cmd = new SqliteCommand())
            {
                string selectExp = "select UUID from prims where ";
                for (int i = 0; i < objGroups.Count; i++)
                {
                    cmd.Parameters.Add(new SqliteParameter(":SceneGroupID" + i, objGroups[i]));
                    selectExp += "SceneGroupID=:SceneGroupID" + i + " or ";
                }

                selectExp = selectExp.Remove(selectExp.Length - 4, 4);

                cmd.CommandText = selectExp;
                cmd.Connection = m_conn;

                using (IDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        uuids.Add(DBGuid.FromDB(reader["UUID"].ToString()));
                }

                // delete the main prims
                selectExp = "delete from prims where ";
                for (int i = 0; i < objGroups.Count; i++)
                    selectExp += "SceneGroupID=:SceneGroupID" + i + " or ";

                selectExp = selectExp.Remove(selectExp.Length - 4, 4);
                cmd.CommandText = selectExp;

                cmd.ExecuteNonQuery();
            }

            // there is no way this should be < 1 unless there is
            // a very corrupt database, but in that case be extra
            // safe anyway.
            if (uuids.Count > 0)
            {
                RemoveShapes(uuids);
                RemoveItems(uuids);
            }
        }

        /// <summary>
        /// Removes an object from region storage
        /// </summary>
        /// <param name="obj">the object</param>
        /// <param name="regionUUID">the region UUID</param>
        public void RemoveObject(UUID obj, UUID regionUUID)
        {
            List<UUID> uuids = new List<UUID>();

            using (SqliteCommand cmd = new SqliteCommand())
            {
                cmd.CommandText = "select UUID from prims where SceneGroupID=:SceneGroupID";
                cmd.Parameters.Add(new SqliteParameter(":SceneGroupID", obj));
                cmd.Connection = m_conn;

                using (IDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        uuids.Add(DBGuid.FromDB(reader["UUID"].ToString()));
                }

                // delete the main prims
                cmd.CommandText = "delete from prims where SceneGroupID=:SceneGroupID";
                cmd.ExecuteNonQuery();
            }

            // there is no way this should be < 1 unless there is
            // a very corrupt database, but in that case be extra
            // safe anyway.
            if (uuids.Count > 0)
            {
                RemoveShapes(uuids);
                RemoveItems(uuids);
            }
        }

        /// <summary>
        /// Removes an object from region storage
        /// </summary>
        /// <param name="obj">the object</param>
        /// <param name="regionUUID">the region UUID</param>
        public void RemoveRegion(UUID regionUUID)
        {
            List<UUID> uuids = new List<UUID>();

            using (SqliteCommand cmd = new SqliteCommand())
            {
                string selectExp = "select UUID from prims where RegionUUID = '" + regionUUID + "'";

                cmd.CommandText = selectExp;
                cmd.Connection = m_conn;

                using (IDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        uuids.Add(DBGuid.FromDB(reader["UUID"].ToString()));
                }

                // delete the main prims
                cmd.CommandText = "delete from prims where RegionUUID = '" + regionUUID + "'";
                cmd.ExecuteNonQuery();
            }

            // there is no way this should be < 1 unless there is
            // a very corrupt database, but in that case be extra
            // safe anyway.
            if (uuids.Count > 0)
            {
                RemoveShapes(uuids);
                RemoveItems(uuids);
            }
        }

        /// <summary>
        /// Remove all persisted shapes for a list of prims
        /// The caller must acquire the necessrary synchronization locks
        /// </summary>
        /// <param name="uuids">the list of UUIDs</param>
        private void RemoveShapes(List<UUID> uuids)
        {
            string sql = "delete from primshapes where ";
            using (SqliteCommand cmd = new SqliteCommand())
            {
                for (int i = 0; i < uuids.Count; i++)
                {
                    if ((i + 1) == uuids.Count)
                    {// end of the list
                        sql += "(UUID = :UUID" + i + ")";
                    }
                    else
                    {
                        sql += "(UUID = :UUID" + i + ") or ";
                    }
                    cmd.Parameters.AddWithValue(":UUID" + i, uuids[i].ToString());
                }
                cmd.CommandText = sql;
                cmd.Connection = m_conn;

                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Remove all persisted items for a list of prims
        /// The caller must acquire the necessrary synchronization locks
        /// </summary>
        /// <param name="uuids">the list of UUIDs</param>
        private void RemoveItems(List<UUID> uuids)
        {
            string sql = "delete from primitems where ";
            using (SqliteCommand cmd = new SqliteCommand())
            {
                for (int i = 0; i < uuids.Count; i++)
                {
                    if ((i + 1) == uuids.Count)
                    {
                        // end of the list
                        sql += "(PrimID = :PrimID" + i + ")";
                    }
                    else
                    {
                        sql += "(PrimID = :PrimID" + i + ") or ";
                    }
                    cmd.Parameters.AddWithValue(":PrimID" + i, uuids[i].ToString());
                }
                cmd.CommandText = sql;
                cmd.Connection = m_conn;

                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Remove all persisted items of the given prim.
        /// The caller must acquire the necessrary synchronization locks and commit or rollback changes.
        /// </summary>
        /// <param name="uuid">The item UUID</param>
        private void RemoveItems(UUID uuid)
        {
            using (SqliteCommand cmd = new SqliteCommand())
            {
                cmd.CommandText = String.Format("delete from primitems where primID = '{0}'", uuid);
                cmd.Connection = m_conn;

                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Load persisted objects from region storage.
        /// </summary>
        /// <param name="regionUUID">The region UUID</param>
        /// <returns>List of loaded groups</returns>
        public List<SceneObjectGroup> LoadObjects(UUID regionUUID, Scene scene)
        {
            Dictionary<UUID, SceneObjectGroup> createdObjects = new Dictionary<UUID, SceneObjectGroup>();

            List<SceneObjectGroup> retvals = new List<SceneObjectGroup>();

            using (SqliteCommand cmd = new SqliteCommand())
            {
                string selectExp = "select * from prims where RegionUUID = '" + regionUUID + "'";

                cmd.CommandText = selectExp;
                cmd.Connection = m_conn;

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

                                SceneObjectGroup group = new SceneObjectGroup(prim, scene);
                                createdObjects[group.UUID] = group;
                                retvals.Add(group);
                                LoadItems(prim);
                            }
                        }
                        catch (Exception e)
                        {
                            m_log.Error("[SQLITE REGION DB]: Failed create prim object in new group, exception and data follows");
                            m_log.Error("[SQLITE REGION DB]: ", e);
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
                                    Console.WriteLine("Found an SceneObjectPart without a SceneObjectGroup! ObjectID: " + objID);
                                    continue;
                                }

                                createdObjects[new UUID(objID)].AddPart(prim, true);
                                LoadItems(prim);
                            }
                        }
                        catch (Exception e)
                        {
                            m_log.Error("[SQLITE REGION DB]: Failed create prim object in new group, exception and data follows");
                            m_log.Error("[SQLITE REGION DB]: ", e);
                        }
                    }
                }
            }
            return retvals;
        }

        private PrimitiveBaseShape findPrimShape(string uuid)
        {
            PrimitiveBaseShape shape = null;
            string selectExp = "select * from primshapes where UUID = '" + uuid + "'";
            using (SqliteCommand cmd = new SqliteCommand())
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
        /// Load in a prim's persisted inventory.
        /// </summary>
        /// <param name="prim">the prim</param>
        private void LoadItems(SceneObjectPart prim)
        {
            // m_log.DebugFormat(":[SQLITE REGION DB]: Loading inventory for {0} {1}", prim.Name, prim.UUID);
            IList<TaskInventoryItem> inventory = new List<TaskInventoryItem>();
            using (SqliteCommand cmd = new SqliteCommand())
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

//			m_log.DebugFormat(
//			    "[SQLITE REGION DB]: Found {0} items for {1} {2}", dbItemRows.Length, prim.Name, prim.UUID);

            prim.Inventory.RestoreInventoryItems(inventory);
        }

        /// <summary>
        /// Store a terrain revision in region storage
        /// </summary>
        /// <param name="ter">terrain heightfield</param>
        /// <param name="regionID">region UUID</param>
        public void StoreTerrain(double[,] ter, UUID regionID, bool Revert)
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
                    SqliteCommand cmd =
                        new SqliteCommand("delete from terrain where RegionUUID=:RegionUUID and Revision <= :Revision and Revert = :Revert",
                                          m_conn))
                {
                    cmd.Parameters.Add(new SqliteParameter(":Revert", Revert.ToString()));
                    cmd.Parameters.Add(new SqliteParameter(":RegionUUID", regionID.ToString()));
                    cmd.Parameters.Add(new SqliteParameter(":Revision", revision));
                    cmd.ExecuteNonQuery();
                }

                // the following is an work around for .NET.  The perf
                // issues associated with it aren't as bad as you think.
                m_log.Debug("[SQLITE REGION DB]: Storing terrain revision r" + revision.ToString());
                String sql = "insert into terrain(RegionUUID, Revision, Heightfield, Revert)" +
                             " values(:RegionUUID, :Revision, :Heightfield, :Revert)";

                using (SqliteCommand cmd = new SqliteCommand(sql, m_conn))
                {
                    cmd.Parameters.Add(new SqliteParameter(":RegionUUID", regionID.ToString()));
                    cmd.Parameters.Add(new SqliteParameter(":Revision", revision));
                    cmd.Parameters.Add(new SqliteParameter(":Heightfield", serializeTerrain(ter)));
                    cmd.Parameters.Add(new SqliteParameter(":Revert", Revert.ToString()));
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Load the latest terrain revision from region storage
        /// </summary>
        /// <param name="regionID">the region UUID</param>
        /// <returns>Heightfield data</returns>
        public double[,] LoadTerrain(UUID regionID, bool revert)
        {
            lock (ds)
            {
                double[,] terret = new double[(int)Constants.RegionSize, (int)Constants.RegionSize];
                terret.Initialize();

                String sql = "";
                if (revert)
                {
                    sql = "select RegionUUID, Revision, Heightfield from terrain" +
                              " where RegionUUID=:RegionUUID and Revert = 'True' order by Revision desc";
                }
                else
                {
                    sql = "select RegionUUID, Revision, Heightfield from terrain" +
                              " where RegionUUID=:RegionUUID and Revert = 'False' order by Revision desc";
                }

                using (SqliteCommand cmd = new SqliteCommand(sql, m_conn))
                {
                    cmd.Parameters.Add(new SqliteParameter(":RegionUUID", regionID.ToString()));

                    using (IDataReader row = cmd.ExecuteReader())
                    {
                        int rev = 0;
                        if (row.Read())
                        {
                            // TODO: put this into a function
                            using (MemoryStream str = new MemoryStream((byte[])row["Heightfield"]))
                            {
                                using (BinaryReader br = new BinaryReader(str))
                                {
                                    for (int x = 0; x < (int)Constants.RegionSize; x++)
                                    {
                                        for (int y = 0; y < (int)Constants.RegionSize; y++)
                                        {
                                            terret[x, y] = br.ReadDouble();
                                        }
                                    }
                                }
                            }
                            rev = Convert.ToInt32(row["Revision"]);
                        }
                        else
                        {
                            m_log.Warn("[SQLITE REGION DB]: No terrain found for region");
                            return null;
                        }

                        m_log.Debug("[SQLITE REGION DB]: Loaded terrain revision r" + rev.ToString());
                    }
                }
                return terret;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="globalID"></param>
        public void RemoveLandObject(UUID RegionID, UUID globalID)
        {
            lock (ds)
            {
                // Can't use blanket SQL statements when using SqlAdapters unless you re-read the data into the adapter
                // after you're done.
                // replaced below code with the SqliteAdapter version.
                //using (SqliteCommand cmd = new SqliteCommand(":delete from land where UUID=:UUID", m_conn))
                //{
                //    cmd.Parameters.Add(new SqliteParameter("::UUID", globalID.ToString()));
                //    cmd.ExecuteNonQuery();
                //}

                //using (SqliteCommand cmd = new SqliteCommand(":delete from landaccesslist where LandUUID=:UUID", m_conn))
                //{
                //   cmd.Parameters.Add(new SqliteParameter("::UUID", globalID.ToString()));
                //    cmd.ExecuteNonQuery();
                //}

                DataTable land = ds.Tables["land"];
                DataTable landaccesslist = ds.Tables["landaccesslist"];
                DataRow landRow = land.Rows.Find(globalID.ToString());
                if (landRow != null)
                {
                    land.Rows.Remove(landRow);
                }
                List<DataRow> rowsToDelete = new List<DataRow>();
                foreach (DataRow rowToCheck in landaccesslist.Rows)
                {
                    if (rowToCheck["LandUUID"].ToString() == globalID.ToString())
                        rowsToDelete.Add(rowToCheck);
                }
                for (int iter = 0; iter < rowsToDelete.Count; iter++)
                {
                    landaccesslist.Rows.Remove(rowsToDelete[iter]);
                }

               
            }
            Commit();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="parcel"></param>
        public void StoreLandObject(LandData parcel)
        {
            lock (ds)
            {
                DataTable land = ds.Tables["land"];
                DataTable landaccesslist = ds.Tables["landaccesslist"];

                DataRow landRow = land.Rows.Find(parcel.GlobalID.ToString());
                if (landRow == null)
                {
                    landRow = land.NewRow();
                    fillLandRow(landRow, parcel, parcel.RegionID);
                    land.Rows.Add(landRow);
                }
                else
                {
                    fillLandRow(landRow, parcel, parcel.RegionID);
                }

                // I know this caused someone issues before, but OpenSim is unusable if we leave this stuff around
                //using (SqliteCommand cmd = new SqliteCommand(":delete from landaccesslist where LandUUID=:LandUUID", m_conn))
                //{
                //    cmd.Parameters.Add(new SqliteParameter("::LandUUID", parcel.LandData.GlobalID.ToString()));
                //    cmd.ExecuteNonQuery();

//                }

                // This is the slower..  but more appropriate thing to do

                // We can't modify the table with direct queries before calling Commit() and re-filling them.
                List<DataRow> rowsToDelete = new List<DataRow>();
                foreach (DataRow rowToCheck in landaccesslist.Rows)
                {
                    if (rowToCheck["LandUUID"].ToString() == parcel.GlobalID.ToString())
                        rowsToDelete.Add(rowToCheck);
                }
                for (int iter = 0; iter < rowsToDelete.Count; iter++)
                {
                    landaccesslist.Rows.Remove(rowsToDelete[iter]);
                }
                rowsToDelete.Clear();
                foreach (ParcelManager.ParcelAccessEntry entry in parcel.ParcelAccessList)
                {
                    DataRow newAccessRow = landaccesslist.NewRow();
                    fillLandAccessRow(newAccessRow, entry, parcel.GlobalID);
                    landaccesslist.Rows.Add(newAccessRow);
                }
            }

            Commit();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="regionUUID"></param>
        /// <returns></returns>
        public List<LandData> LoadLandObjects(UUID regionUUID)
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

        /// <summary>
        ///
        /// </summary>
        public void Commit()
        {
            //m_log.Debug(":[SQLITE]: Starting commit");
            lock (ds)
            {
                terrainDa.Update(ds, "terrain");
                landDa.Update(ds, "land");
                landAccessListDa.Update(ds, "landaccesslist");
                try
                {
                    regionSettingsDa.Update(ds, "regionsettings");
                } 
                catch (SqliteException SqlEx)
                {
                    throw new Exception(
                        "There was a SQL error or connection string configuration error when saving the region settings.  This could be a bug, it could also happen if ConnectionString is defined in the [DatabaseService] section of StandaloneCommon.ini in the config_include folder.  This could also happen if the config_include folder doesn't exist or if the OpenSim.ini [Architecture] section isn't set.  If this is your first time running OpenSimulator, please restart the simulator and bug a developer to fix this!",
                        SqlEx);
                }
                ds.AcceptChanges();
            }
        }

        /// <summary>
        /// See <see cref="Commit"/>
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        private void createCol(DataTable dt, string name, Type type)
        {
            DataColumn col = new DataColumn(name, type);
            dt.Columns.Add(col);
        }

        /// <summary>
        /// Creates the "terrain" table
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
        /// Creates "land" table
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
            createCol(land, "AuthbuyerID", typeof(String));
            createCol(land, "OtherCleanTime", typeof(Int32));
            createCol(land, "Dwell", typeof(Int32));

            land.PrimaryKey = new DataColumn[] {land.Columns["UUID"]};

            return land;
        }

        /// <summary>
        /// create "landaccesslist" table
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
            createCol(regionsettings, "regionUUID", typeof(String));
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
            createCol(regionsettings, "terrain_texture_1", typeof(String));
            createCol(regionsettings, "terrain_texture_2", typeof(String));
            createCol(regionsettings, "terrain_texture_3", typeof(String));
            createCol(regionsettings, "terrain_texture_4", typeof(String));
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
            createCol(regionsettings, "sunvectorx",typeof (Double));
            createCol(regionsettings, "sunvectory",typeof (Double));
            createCol(regionsettings, "sunvectorz",typeof (Double));
            createCol(regionsettings, "fixed_sun", typeof (Int32));
            createCol(regionsettings, "sun_position", typeof (Double));
            createCol(regionsettings, "covenant", typeof(String));
            createCol(regionsettings, "map_tile_ID", typeof(String));
            regionsettings.PrimaryKey = new DataColumn[] { regionsettings.Columns["regionUUID"] };
            return regionsettings;
        }

        /***********************************************************************
         *
         *  Convert between ADO.NET <=> OpenSim Objects
         *
         *  These should be database independant
         *
         **********************************************************************/

        /// <summary>
        ///
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private SceneObjectPart buildPrim(IDataReader row, Scene scene)
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
            
            SceneObjectPart prim = new SceneObjectPart(scene);
            prim.UUID = new UUID(row["UUID"].ToString());
            prim.CreatorID = new UUID(row["CreatorID"].ToString());
            prim.OwnerID = new UUID(row["OwnerID"].ToString());
            prim.GroupID = new UUID(row["GroupID"].ToString());
            prim.LastOwnerID = new UUID(row["LastOwnerID"].ToString());
            // explicit conversion of integers is required, which sort
            // of sucks.  No idea if there is a shortcut here or not.
            prim.CreationDate = Convert.ToInt32(row["CreationDate"].ToString());
            prim.Name = row["Name"] == DBNull.Value ? string.Empty : row["Name"].ToString();
            // various text fields
            prim.Text = row["Text"].ToString();
            prim.Color = Color.FromArgb(Convert.ToInt32(row["ColorA"].ToString()),
                                        Convert.ToInt32(row["ColorR"].ToString()),
                                        Convert.ToInt32(row["ColorG"].ToString()),
                                        Convert.ToInt32(row["ColorB"].ToString()));
            prim.Description = row["Description"].ToString();
            prim.SitName = row["SitName"].ToString();
            prim.TouchName = row["TouchName"].ToString();
            // permissions
            prim.Flags = (PrimFlags)Convert.ToUInt32(row["ObjectFlags"].ToString());
            prim.OwnerMask = Convert.ToUInt32(row["OwnerMask"].ToString());
            prim.NextOwnerMask = Convert.ToUInt32(row["NextOwnerMask"].ToString());
            prim.GroupMask = Convert.ToUInt32(row["GroupMask"].ToString());
            prim.EveryoneMask = Convert.ToUInt32(row["EveryoneMask"].ToString());
            prim.BaseMask = Convert.ToUInt32(row["BaseMask"].ToString());
            // vectors
            prim.OffsetPosition = new Vector3(
                Convert.ToSingle(row["PositionX"].ToString()),
                Convert.ToSingle(row["PositionY"].ToString()),
                Convert.ToSingle(row["PositionZ"].ToString())
                );
            prim.GroupPosition = new Vector3(
                Convert.ToSingle(row["GroupPositionX"].ToString()),
                Convert.ToSingle(row["GroupPositionY"].ToString()),
                Convert.ToSingle(row["GroupPositionZ"].ToString())
                );
            prim.Velocity = new Vector3(
                Convert.ToSingle(row["VelocityX"].ToString()),
                Convert.ToSingle(row["VelocityY"].ToString()),
                Convert.ToSingle(row["VelocityZ"].ToString())
                );
            prim.AngularVelocity = new Vector3(
                Convert.ToSingle(row["AngularVelocityX"].ToString()),
                Convert.ToSingle(row["AngularVelocityY"].ToString()),
                Convert.ToSingle(row["AngularVelocityZ"].ToString())
                );
            prim.Acceleration = new Vector3(
                Convert.ToSingle(row["AccelerationX"].ToString()),
                Convert.ToSingle(row["AccelerationY"].ToString()),
                Convert.ToSingle(row["AccelerationZ"].ToString())
                );
            // quaternions
            prim.RotationOffset = new Quaternion(
                Convert.ToSingle(row["RotationX"].ToString()),
                Convert.ToSingle(row["RotationY"].ToString()),
                Convert.ToSingle(row["RotationZ"].ToString()),
                Convert.ToSingle(row["RotationW"].ToString())
                );

            prim.SitTargetPositionLL = new Vector3(
                                                   Convert.ToSingle(row["SitTargetOffsetX"].ToString()),
                                                   Convert.ToSingle(row["SitTargetOffsetY"].ToString()),
                                                   Convert.ToSingle(row["SitTargetOffsetZ"].ToString()));
            prim.SitTargetOrientationLL = new Quaternion(
                                                         Convert.ToSingle(
                                                                          row["SitTargetOrientX"].ToString()),
                                                         Convert.ToSingle(
                                                                          row["SitTargetOrientY"].ToString()),
                                                         Convert.ToSingle(
                                                                          row["SitTargetOrientZ"].ToString()),
                                                         Convert.ToSingle(
                                                                          row["SitTargetOrientW"].ToString()));

            prim.PayPrice[0] = Convert.ToInt32(row["PayPrice"].ToString());
            prim.PayPrice[1] = Convert.ToInt32(row["PayButton1"].ToString());
            prim.PayPrice[2] = Convert.ToInt32(row["PayButton2"].ToString());
            prim.PayPrice[3] = Convert.ToInt32(row["PayButton3"].ToString());
            prim.PayPrice[4] = Convert.ToInt32(row["PayButton4"].ToString());

            prim.Sound = new UUID(row["LoopedSound"].ToString());
            prim.SoundGain = Convert.ToSingle(row["LoopedSoundGain"].ToString());
            prim.SoundFlags = 1; // If it's persisted at all, it's looped

            if (!(row["TextureAnimation"] is DBNull))
                prim.TextureAnimation = Convert.FromBase64String(row["TextureAnimation"].ToString());
            if (!(row["ParticleSystem"] is DBNull))
                prim.ParticleSystem = Convert.FromBase64String(row["ParticleSystem"].ToString());

            prim.AngularVelocity = new Vector3(
                Convert.ToSingle(row["OmegaX"].ToString()),
                Convert.ToSingle(row["OmegaY"].ToString()),
                Convert.ToSingle(row["OmegaZ"].ToString())
                );

            prim.CameraEyeOffset = new Vector3(
                Convert.ToSingle(row["CameraEyeOffsetX"].ToString()),
                Convert.ToSingle(row["CameraEyeOffsetY"].ToString()),
                Convert.ToSingle(row["CameraEyeOffsetZ"].ToString())
                );

            prim.CameraAtOffset = new Vector3(
                Convert.ToSingle(row["CameraAtOffsetX"].ToString()),
                Convert.ToSingle(row["CameraAtOffsetY"].ToString()),
                Convert.ToSingle(row["CameraAtOffsetZ"].ToString())
                );

            if (Convert.ToInt16(row["ForceMouselook"].ToString()) != 0)
                prim.ForceMouselook = true;

            prim.ScriptAccessPin = Convert.ToInt32(row["ScriptAccessPin"].ToString());

            if (Convert.ToInt16(row["AllowedDrop"].ToString()) != 0)
                prim.AllowedDrop = true;

            if (Convert.ToInt16(row["DieAtEdge"].ToString()) != 0)
                prim.DIE_AT_EDGE = true;

            prim.SalePrice = Convert.ToInt32(row["SalePrice"].ToString());
            prim.ObjectSaleType = Convert.ToByte(row["SaleType"].ToString());

            prim.Material = Convert.ToByte(row["Material"].ToString());

            if (!(row["ClickAction"] is DBNull))
                prim.ClickAction = Convert.ToByte(row["ClickAction"].ToString());
            
            prim.CollisionSound = new UUID(row["CollisionSound"].ToString().ToString());
            prim.CollisionSoundVolume = Convert.ToSingle(row["CollisionSoundVolume"].ToString());

            prim.PassTouch = (int)(double)row["CollisionSoundVolume"];
            prim.LinkNum = int.Parse(row["LinkNumber"].ToString());

            prim.GenericData = row["Generic"].ToString();

            return prim;
        }

        /// <summary>
        /// Build a prim inventory item from the persisted data.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private static TaskInventoryItem buildItem(IDataReader row)
        {
            TaskInventoryItem taskItem = new TaskInventoryItem();

            taskItem.ItemID = new UUID(row["itemID"].ToString());
            taskItem.ParentPartID = new UUID(row["primID"].ToString());
            taskItem.AssetID = new UUID(row["assetID"].ToString());
            taskItem.ParentID = new UUID(row["parentFolderID"].ToString());

            taskItem.InvType = Convert.ToInt32(row["invType"].ToString());
            taskItem.Type = Convert.ToInt32(row["assetType"].ToString());

            taskItem.Name = row["name"].ToString();
            taskItem.Description = row["description"].ToString();
            taskItem.CreationDate = Convert.ToUInt32(row["creationDate"].ToString());
            taskItem.CreatorID = new UUID(row["creatorID"].ToString());
            taskItem.OwnerID = new UUID(row["ownerID"].ToString());
            taskItem.LastOwnerID = new UUID(row["lastOwnerID"].ToString());
            taskItem.GroupID = new UUID(row["groupID"].ToString());

            taskItem.NextPermissions = Convert.ToUInt32(row["nextPermissions"].ToString());
            taskItem.CurrentPermissions = Convert.ToUInt32(row["currentPermissions"].ToString());
            taskItem.BasePermissions = Convert.ToUInt32(row["basePermissions"].ToString());
            taskItem.EveryonePermissions = Convert.ToUInt32(row["everyonePermissions"].ToString());
            taskItem.GroupPermissions = Convert.ToUInt32(row["groupPermissions"].ToString());
            taskItem.Flags = Convert.ToUInt32(row["flags"].ToString());
            taskItem.SalePrice = Convert.ToInt32(row["salePrice"]);
            taskItem.SaleType = Convert.ToByte(row["saleType"]);

            return taskItem;
        }

        /// <summary>
        /// Build a Land Data from the persisted data.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private LandData buildLandData(DataRow row)
        {
            LandData newData = new LandData();

            newData.GlobalID = new UUID((String) row["UUID"]);
            newData.LocalID = Convert.ToInt32(row["LocalLandID"]);

            // Bitmap is a byte[512]
            newData.Bitmap = (Byte[]) row["Bitmap"];

            newData.Name = (String) row["Name"];
            newData.Description = (String) row["Desc"];
            newData.OwnerID = (UUID)(String) row["OwnerUUID"];
            newData.IsGroupOwned = (Boolean) row["IsGroupOwned"];
            newData.Area = Convert.ToInt32(row["Area"]);
            newData.AuctionID = Convert.ToUInt32(row["AuctionID"]); //Unemplemented
            newData.Category = (ParcelCategory) Convert.ToInt32(row["Category"]);
                //Enum OpenMetaverse.Parcel.ParcelCategory
            newData.ClaimDate = Convert.ToInt32(row["ClaimDate"]);
            newData.ClaimPrice = Convert.ToInt32(row["ClaimPrice"]);
            newData.GroupID = new UUID((String) row["GroupUUID"]);
            newData.SalePrice = Convert.ToInt32(row["SalePrice"]);
            newData.Status = (ParcelStatus) Convert.ToInt32(row["LandStatus"]);
                //Enum. OpenMetaverse.Parcel.ParcelStatus
            newData.Flags = Convert.ToUInt32(row["LandFlags"]);
            newData.LandingType = (Byte) row["LandingType"];
            newData.MediaAutoScale = (Byte) row["MediaAutoScale"];
            newData.MediaID = new UUID((String) row["MediaTextureUUID"]);
            newData.MediaURL = (String) row["MediaURL"];
            newData.MusicURL = (String) row["MusicURL"];
            newData.PassHours = Convert.ToSingle(row["PassHours"]);
            newData.PassPrice = Convert.ToInt32(row["PassPrice"]);
            newData.SnapshotID = (UUID)(String) row["SnapshotUUID"];
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
                m_log.ErrorFormat(":[SQLITE REGION DB]: unable to get parcel telehub settings for {1}", newData.Name);
                newData.UserLocation = Vector3.Zero;
                newData.UserLookAt = Vector3.Zero;
            }
            newData.ParcelAccessList = new List<ParcelManager.ParcelAccessEntry>();
            UUID authBuyerID = UUID.Zero;

            UUID.TryParse((string)row["AuthbuyerID"], out authBuyerID);

            newData.OtherCleanTime = Convert.ToInt32(row["OtherCleanTime"]);
            newData.Dwell = Convert.ToInt32(row["Dwell"]);

            return newData;
        }

        private RegionSettings buildRegionSettings(DataRow row)
        {
            RegionSettings newSettings = new RegionSettings();

            newSettings.RegionUUID = new UUID((string) row["regionUUID"]);
            newSettings.BlockTerraform = Convert.ToBoolean(row["block_terraform"]);
            newSettings.AllowDamage = Convert.ToBoolean(row["allow_damage"]);
            newSettings.BlockFly = Convert.ToBoolean(row["block_fly"]);
            newSettings.RestrictPushing = Convert.ToBoolean(row["restrict_pushing"]);
            newSettings.AllowLandResell = Convert.ToBoolean(row["allow_land_resell"]);
            newSettings.AllowLandJoinDivide = Convert.ToBoolean(row["allow_land_join_divide"]);
            newSettings.BlockShowInSearch = Convert.ToBoolean(row["block_show_in_search"]);
            newSettings.AgentLimit = Convert.ToInt32(row["agent_limit"]);
            newSettings.ObjectBonus = Convert.ToDouble(row["object_bonus"]);
            newSettings.Maturity = Convert.ToInt32(row["maturity"]);
            newSettings.DisableScripts = Convert.ToBoolean(row["disable_scripts"]);
            newSettings.DisableCollisions = Convert.ToBoolean(row["disable_collisions"]);
            newSettings.DisablePhysics = Convert.ToBoolean(row["disable_physics"]);
            newSettings.TerrainTexture1 = new UUID((String) row["terrain_texture_1"]);
            newSettings.TerrainTexture2 = new UUID((String) row["terrain_texture_2"]);
            newSettings.TerrainTexture3 = new UUID((String) row["terrain_texture_3"]);
            newSettings.TerrainTexture4 = new UUID((String) row["terrain_texture_4"]);
            newSettings.Elevation1NW = Convert.ToDouble(row["elevation_1_nw"]);
            newSettings.Elevation2NW = Convert.ToDouble(row["elevation_2_nw"]);
            newSettings.Elevation1NE = Convert.ToDouble(row["elevation_1_ne"]);
            newSettings.Elevation2NE = Convert.ToDouble(row["elevation_2_ne"]);
            newSettings.Elevation1SE = Convert.ToDouble(row["elevation_1_se"]);
            newSettings.Elevation2SE = Convert.ToDouble(row["elevation_2_se"]);
            newSettings.Elevation1SW = Convert.ToDouble(row["elevation_1_sw"]);
            newSettings.Elevation2SW = Convert.ToDouble(row["elevation_2_sw"]);
            newSettings.WaterHeight = Convert.ToDouble(row["water_height"]);
            newSettings.TerrainRaiseLimit = Convert.ToDouble(row["terrain_raise_limit"]);
            newSettings.TerrainLowerLimit = Convert.ToDouble(row["terrain_lower_limit"]);
            newSettings.UseEstateSun = Convert.ToBoolean(row["use_estate_sun"]);
            newSettings.Sandbox = Convert.ToBoolean(row["sandbox"]);
            newSettings.SunVector = new Vector3 (
                                     Convert.ToSingle(row["sunvectorx"]),
                                     Convert.ToSingle(row["sunvectory"]),
                                     Convert.ToSingle(row["sunvectorz"])
                                     );
            newSettings.FixedSun = Convert.ToBoolean(row["fixed_sun"]);
            newSettings.SunPosition = Convert.ToDouble(row["sun_position"]);
            newSettings.Covenant = new UUID(row["covenant"].ToString());
            newSettings.TerrainImageID = new UUID(row["map_tile_ID"].ToString());
            newSettings.TerrainMapImageID = new UUID(row["terrain_tile_ID"].ToString());
            newSettings.MinimumAge = Convert.ToInt32(row["minimum_age"]);
            newSettings.LoadedCreationDateTime = int.Parse(row["loaded_creation_datetime"].ToString());
            if (row["loaded_creation_id"] is DBNull)
                newSettings.LoadedCreationID = "";
            else
                newSettings.LoadedCreationID = row["loaded_creation_id"].ToString();

            return newSettings;
        }

        /// <summary>
        /// Build a land access entry from the persisted data.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private ParcelManager.ParcelAccessEntry buildLandAccessData(DataRow row)
        {
            ParcelManager.ParcelAccessEntry entry = new ParcelManager.ParcelAccessEntry();
            entry.AgentID = new UUID((string) row["AccessUUID"]);
            entry.Flags = (AccessList) row["Flags"];
            entry.Time = new DateTime();
            return entry;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private Array serializeTerrain(double[,] val)
        {
            MemoryStream str = new MemoryStream(((int)Constants.RegionSize * (int)Constants.RegionSize) *sizeof (double));
            BinaryWriter bw = new BinaryWriter(str);

            // TODO: COMPATIBILITY - Add byte-order conversions
            for (int x = 0; x < (int)Constants.RegionSize; x++)
                for (int y = 0; y < (int)Constants.RegionSize; y++)
                    bw.Write(val[x, y]);

            return str.ToArray();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="row"></param>
        /// <param name="prim"></param>
        /// <param name="sceneGroupID"></param>
        /// <param name="regionUUID"></param>

        private void FillPrimCommand(SqliteCommand row, SceneObjectPart prim, UUID sceneGroupID, UUID regionUUID)
        {
            row.Parameters.AddWithValue(":UUID", prim.UUID.ToString());
            row.Parameters.AddWithValue(":RegionUUID", regionUUID.ToString());
            row.Parameters.AddWithValue(":CreationDate", prim.CreationDate);
            row.Parameters.AddWithValue(":Name", prim.Name);
            row.Parameters.AddWithValue(":SceneGroupID", sceneGroupID.ToString());
            // the UUID of the root part for this SceneObjectGroup
            // various text fields
            row.Parameters.AddWithValue(":Text", prim.Text);
            row.Parameters.AddWithValue(":Description", prim.Description);
            row.Parameters.AddWithValue(":SitName", prim.SitName);
            row.Parameters.AddWithValue(":TouchName", prim.TouchName);
            // permissions
            row.Parameters.AddWithValue(":ObjectFlags", (uint)prim.Flags);
            row.Parameters.AddWithValue(":CreatorID", prim.CreatorID.ToString());
            row.Parameters.AddWithValue(":OwnerID", prim.OwnerID.ToString());
            row.Parameters.AddWithValue(":GroupID", prim.GroupID.ToString());
            row.Parameters.AddWithValue(":LastOwnerID", prim.LastOwnerID.ToString());
            row.Parameters.AddWithValue(":OwnerMask", prim.OwnerMask);
            row.Parameters.AddWithValue(":NextOwnerMask", prim.NextOwnerMask);
            row.Parameters.AddWithValue(":GroupMask", prim.GroupMask);
            row.Parameters.AddWithValue(":EveryoneMask", prim.EveryoneMask);
            row.Parameters.AddWithValue(":BaseMask", prim.BaseMask);
            // vectors
            row.Parameters.AddWithValue(":PositionX", prim.OffsetPosition.X);
            row.Parameters.AddWithValue(":PositionY", prim.OffsetPosition.Y);
            row.Parameters.AddWithValue(":PositionZ", prim.OffsetPosition.Z);
            row.Parameters.AddWithValue(":GroupPositionX", prim.GroupPosition.X);
            row.Parameters.AddWithValue(":GroupPositionY", prim.GroupPosition.Y);
            row.Parameters.AddWithValue(":GroupPositionZ", prim.GroupPosition.Z);
            row.Parameters.AddWithValue(":VelocityX", prim.Velocity.X);
            row.Parameters.AddWithValue(":VelocityY", prim.Velocity.Y);
            row.Parameters.AddWithValue(":VelocityZ", prim.Velocity.Z);
            row.Parameters.AddWithValue(":AngularVelocityX", prim.AngularVelocity.X);
            row.Parameters.AddWithValue(":AngularVelocityY", prim.AngularVelocity.Y);
            row.Parameters.AddWithValue(":AngularVelocityZ", prim.AngularVelocity.Z);
            row.Parameters.AddWithValue(":AccelerationX", prim.Acceleration.X);
            row.Parameters.AddWithValue(":AccelerationY", prim.Acceleration.Y);
            row.Parameters.AddWithValue(":AccelerationZ", prim.Acceleration.Z);
            // quaternions
            row.Parameters.AddWithValue(":RotationX", prim.RotationOffset.X);
            row.Parameters.AddWithValue(":RotationY", prim.RotationOffset.Y);
            row.Parameters.AddWithValue(":RotationZ", prim.RotationOffset.Z);
            row.Parameters.AddWithValue(":RotationW", prim.RotationOffset.W);

            // Sit target
            Vector3 sitTargetPos = prim.SitTargetPositionLL;
            row.Parameters.AddWithValue(":SitTargetOffsetX", sitTargetPos.X);
            row.Parameters.AddWithValue(":SitTargetOffsetY", sitTargetPos.Y);
            row.Parameters.AddWithValue(":SitTargetOffsetZ", sitTargetPos.Z);

            Quaternion sitTargetOrient = prim.SitTargetOrientationLL;
            row.Parameters.AddWithValue(":SitTargetOrientW", sitTargetOrient.W);
            row.Parameters.AddWithValue(":SitTargetOrientX", sitTargetOrient.X);
            row.Parameters.AddWithValue(":SitTargetOrientY", sitTargetOrient.Y);
            row.Parameters.AddWithValue(":SitTargetOrientZ", sitTargetOrient.Z);
            row.Parameters.AddWithValue(":ColorR", Convert.ToInt32(prim.Color.R));
            row.Parameters.AddWithValue(":ColorG", Convert.ToInt32(prim.Color.G));
            row.Parameters.AddWithValue(":ColorB", Convert.ToInt32(prim.Color.B));
            row.Parameters.AddWithValue(":ColorA", Convert.ToInt32(prim.Color.A));
            row.Parameters.AddWithValue(":PayPrice", prim.PayPrice[0]);
            row.Parameters.AddWithValue(":PayButton1", prim.PayPrice[1]);
            row.Parameters.AddWithValue(":PayButton2", prim.PayPrice[2]);
            row.Parameters.AddWithValue(":PayButton3", prim.PayPrice[3]);
            row.Parameters.AddWithValue(":PayButton4", prim.PayPrice[4]);


            row.Parameters.AddWithValue(":TextureAnimation", Convert.ToBase64String(prim.TextureAnimation));
            row.Parameters.AddWithValue(":ParticleSystem", Convert.ToBase64String(prim.ParticleSystem));

            row.Parameters.AddWithValue(":OmegaX", prim.AngularVelocity.X);
            row.Parameters.AddWithValue(":OmegaY", prim.AngularVelocity.Y);
            row.Parameters.AddWithValue(":OmegaZ", prim.AngularVelocity.Z);

            row.Parameters.AddWithValue(":CameraEyeOffsetX", prim.CameraEyeOffset.X);
            row.Parameters.AddWithValue(":CameraEyeOffsetY", prim.CameraEyeOffset.Y);
            row.Parameters.AddWithValue(":CameraEyeOffsetZ", prim.CameraEyeOffset.Z);

            row.Parameters.AddWithValue(":CameraAtOffsetX", prim.CameraAtOffset.X);
            row.Parameters.AddWithValue(":CameraAtOffsetY", prim.CameraAtOffset.Y);
            row.Parameters.AddWithValue(":CameraAtOffsetZ", prim.CameraAtOffset.Z);


            if ((prim.SoundFlags & 1) != 0) // Looped
            {
                row.Parameters.AddWithValue(":LoopedSound", prim.Sound.ToString());
                row.Parameters.AddWithValue(":LoopedSoundGain", prim.SoundGain);
            }
            else
            {
                row.Parameters.AddWithValue(":LoopedSound", UUID.Zero.ToString());
                row.Parameters.AddWithValue(":LoopedSoundGain", 0.0f);
            }

            row.Parameters.AddWithValue(":ForceMouselook", prim.ForceMouselook ? 1 : 0);

            row.Parameters.AddWithValue(":ScriptAccessPin", prim.ScriptAccessPin);

            row.Parameters.AddWithValue(":AllowedDrop", prim.AllowedDrop ? 1 : 0);

            row.Parameters.AddWithValue(":DieAtEdge", prim.DIE_AT_EDGE ? 1 : 0);

            row.Parameters.AddWithValue(":SalePrice", prim.SalePrice);
            row.Parameters.AddWithValue(":SaleType", Convert.ToInt16(prim.ObjectSaleType));

            // click action
            row.Parameters.AddWithValue(":ClickAction", prim.ClickAction);

            row.Parameters.AddWithValue(":SalePrice", prim.SalePrice);
            row.Parameters.AddWithValue(":Material", prim.Material);

            row.Parameters.AddWithValue(":CollisionSound", prim.CollisionSound.ToString());
            row.Parameters.AddWithValue(":CollisionSoundVolume", prim.CollisionSoundVolume);

            row.Parameters.AddWithValue(":LinkNumber", prim.LinkNum);
            row.Parameters.AddWithValue(":PassTouches", prim.PassTouch);
            row.Parameters.AddWithValue(":Generic", prim.GenericData);

        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="row"></param>
        /// <param name="prim"></param>
        private void FillShapeCommand(SqliteCommand row, SceneObjectPart prim)
        {
            PrimitiveBaseShape s = prim.Shape;
            row.Parameters.AddWithValue(":UUID", prim.UUID.ToString());
            // shape is an enum
            row.Parameters.AddWithValue(":Shape", 0);
            // vectors
            row.Parameters.AddWithValue(":ScaleX", s.Scale.X);
            row.Parameters.AddWithValue(":ScaleY", s.Scale.Y);
            row.Parameters.AddWithValue(":ScaleZ", s.Scale.Z);
            // paths
            row.Parameters.AddWithValue(":PCode", s.PCode);
            row.Parameters.AddWithValue(":PathBegin", s.PathBegin);
            row.Parameters.AddWithValue(":PathEnd", s.PathEnd);
            row.Parameters.AddWithValue(":PathScaleX", s.PathScaleX);
            row.Parameters.AddWithValue(":PathScaleY", s.PathScaleY);
            row.Parameters.AddWithValue(":PathShearX", s.PathShearX);
            row.Parameters.AddWithValue(":PathShearY", s.PathShearY);
            row.Parameters.AddWithValue(":PathSkew", s.PathSkew);
            row.Parameters.AddWithValue(":PathCurve", s.PathCurve);
            row.Parameters.AddWithValue(":PathRadiusOffset", s.PathRadiusOffset);
            row.Parameters.AddWithValue(":PathRevolutions", s.PathRevolutions);
            row.Parameters.AddWithValue(":PathTaperX", s.PathTaperX);
            row.Parameters.AddWithValue(":PathTaperY", s.PathTaperY);
            row.Parameters.AddWithValue(":PathTwist", s.PathTwist);
            row.Parameters.AddWithValue(":PathTwistBegin", s.PathTwistBegin);
            // profile
            row.Parameters.AddWithValue(":ProfileBegin", s.ProfileBegin);
            row.Parameters.AddWithValue(":ProfileEnd", s.ProfileEnd);
            row.Parameters.AddWithValue(":ProfileCurve", s.ProfileCurve);
            row.Parameters.AddWithValue(":ProfileHollow", s.ProfileHollow);
            row.Parameters.AddWithValue(":State", s.State);

            row.Parameters.AddWithValue(":Texture", s.TextureEntry);
            row.Parameters.AddWithValue(":ExtraParams", s.ExtraParams);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="row"></param>
        /// <param name="taskItem"></param>
        private void FillItemCommand(SqliteCommand cmd, TaskInventoryItem item)
        {
            cmd.Parameters.AddWithValue(":itemID", item.ItemID.ToString());
            cmd.Parameters.AddWithValue(":primID", item.ParentPartID.ToString());
            cmd.Parameters.AddWithValue(":assetID", item.AssetID.ToString());
            cmd.Parameters.AddWithValue(":parentFolderID", item.ParentID.ToString());

            cmd.Parameters.AddWithValue(":invType", item.InvType);
            cmd.Parameters.AddWithValue(":assetType", item.Type);

            cmd.Parameters.AddWithValue(":name", item.Name);
            cmd.Parameters.AddWithValue(":description", item.Description);
            cmd.Parameters.AddWithValue(":creationDate", item.CreationDate);
            cmd.Parameters.AddWithValue(":creatorID", item.CreatorID.ToString());
            cmd.Parameters.AddWithValue(":ownerID", item.OwnerID.ToString());
            cmd.Parameters.AddWithValue(":lastOwnerID", item.LastOwnerID.ToString());
            cmd.Parameters.AddWithValue(":groupID", item.GroupID.ToString());
            cmd.Parameters.AddWithValue(":nextPermissions", item.NextPermissions);
            cmd.Parameters.AddWithValue(":currentPermissions", item.CurrentPermissions);
            cmd.Parameters.AddWithValue(":basePermissions", item.BasePermissions);
            cmd.Parameters.AddWithValue(":everyonePermissions", item.EveryonePermissions);
            cmd.Parameters.AddWithValue(":groupPermissions", item.GroupPermissions);
            cmd.Parameters.AddWithValue(":flags", item.Flags);
            cmd.Parameters.AddWithValue(":salePrice", item.SalePrice);
            cmd.Parameters.AddWithValue(":saleType", item.SaleType);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="row"></param>
        /// <param name="land"></param>
        /// <param name="regionUUID"></param>
        private void fillLandRow(DataRow row, LandData land, UUID regionUUID)
        {
            row["UUID"] = land.GlobalID.ToString();
            row["RegionUUID"] = regionUUID.ToString();
            row["LocalLandID"] = land.LocalID;

            // Bitmap is a byte[512]
            row["Bitmap"] = land.Bitmap;

            row["Name"] = land.Name;
            row["Desc"] = land.Description;
            row["OwnerUUID"] = land.OwnerID.ToString();
            row["IsGroupOwned"] = land.IsGroupOwned;
            row["Area"] = land.Area;
            row["AuctionID"] = land.AuctionID; //Unemplemented
            row["Category"] = land.Category; //Enum OpenMetaverse.Parcel.ParcelCategory
            row["ClaimDate"] = land.ClaimDate;
            row["ClaimPrice"] = land.ClaimPrice;
            row["GroupUUID"] = land.GroupID.ToString();
            row["SalePrice"] = land.SalePrice;
            row["LandStatus"] = land.Status; //Enum. OpenMetaverse.Parcel.ParcelStatus
            row["LandFlags"] = land.Flags;
            row["LandingType"] = land.LandingType;
            row["MediaAutoScale"] = land.MediaAutoScale;
            row["MediaTextureUUID"] = land.MediaID.ToString();
            row["MediaURL"] = land.MediaURL;
            row["MusicURL"] = land.MusicURL;
            row["PassHours"] = land.PassHours;
            row["PassPrice"] = land.PassPrice;
            row["SnapshotUUID"] = land.SnapshotID.ToString();
            row["UserLocationX"] = land.UserLocation.X;
            row["UserLocationY"] = land.UserLocation.Y;
            row["UserLocationZ"] = land.UserLocation.Z;
            row["UserLookAtX"] = land.UserLookAt.X;
            row["UserLookAtY"] = land.UserLookAt.Y;
            row["UserLookAtZ"] = land.UserLookAt.Z;
            row["AuthbuyerID"] = land.AuthBuyerID.ToString();
            row["OtherCleanTime"] = land.OtherCleanTime;
            row["Dwell"] = land.Dwell;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="row"></param>
        /// <param name="entry"></param>
        /// <param name="parcelID"></param>
        private void fillLandAccessRow(DataRow row, ParcelManager.ParcelAccessEntry entry, UUID parcelID)
        {
            row["LandUUID"] = parcelID.ToString();
            row["AccessUUID"] = entry.AgentID.ToString();
            row["Flags"] = entry.Flags;
        }

        private void fillRegionSettingsRow(DataRow row, RegionSettings settings)
        {
            row["regionUUID"] = settings.RegionUUID.ToString();
            row["block_terraform"] = settings.BlockTerraform;
            row["block_fly"] = settings.BlockFly;
            row["allow_damage"] = settings.AllowDamage;
            row["restrict_pushing"] = settings.RestrictPushing;
            row["allow_land_resell"] = settings.AllowLandResell;
            row["allow_land_join_divide"] = settings.AllowLandJoinDivide;
            row["block_show_in_search"] = settings.BlockShowInSearch;
            row["agent_limit"] = settings.AgentLimit;
            row["object_bonus"] = settings.ObjectBonus;
            row["maturity"] = settings.Maturity;
            row["disable_scripts"] = settings.DisableScripts;
            row["disable_collisions"] = settings.DisableCollisions;
            row["disable_physics"] = settings.DisablePhysics;
            row["terrain_texture_1"] = settings.TerrainTexture1.ToString();
            row["terrain_texture_2"] = settings.TerrainTexture2.ToString();
            row["terrain_texture_3"] = settings.TerrainTexture3.ToString();
            row["terrain_texture_4"] = settings.TerrainTexture4.ToString();
            row["elevation_1_nw"] = settings.Elevation1NW;
            row["elevation_2_nw"] = settings.Elevation2NW;
            row["elevation_1_ne"] = settings.Elevation1NE;
            row["elevation_2_ne"] = settings.Elevation2NE;
            row["elevation_1_se"] = settings.Elevation1SE;
            row["elevation_2_se"] = settings.Elevation2SE;
            row["elevation_1_sw"] = settings.Elevation1SW;
            row["elevation_2_sw"] = settings.Elevation2SW;
            row["water_height"] = settings.WaterHeight;
            row["terrain_raise_limit"] = settings.TerrainRaiseLimit;
            row["terrain_lower_limit"] = settings.TerrainLowerLimit;
            row["use_estate_sun"] = settings.UseEstateSun;
            row["Sandbox"] = settings.Sandbox; // database uses upper case S for sandbox
            row["sunvectorx"] = settings.SunVector.X;
            row["sunvectory"] = settings.SunVector.Y;
            row["sunvectorz"] = settings.SunVector.Z;
            row["fixed_sun"] = settings.FixedSun;
            row["sun_position"] = settings.SunPosition;
            row["covenant"] = settings.Covenant.ToString();
            row["map_tile_ID"] = settings.TerrainImageID.ToString();
            row["terrain_tile_ID"] = settings.TerrainMapImageID.ToString();
            row["loaded_creation_datetime"] = settings.LoadedCreationDateTime.ToString();
            row["loaded_creation_id"] = settings.LoadedCreationID.ToString();
            row["minimum_age"] = settings.MinimumAge;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private PrimitiveBaseShape buildShape(IDataReader row)
        {
            PrimitiveBaseShape s = new PrimitiveBaseShape();
            s.Scale = new Vector3(
                Convert.ToSingle(row["ScaleX"]),
                Convert.ToSingle(row["ScaleY"]),
                Convert.ToSingle(row["ScaleZ"])
                );
            // paths
            s.PCode = Convert.ToByte(row["PCode"]);
            s.PathBegin = Convert.ToUInt16(row["PathBegin"]);
            s.PathEnd = Convert.ToUInt16(row["PathEnd"]);
            s.PathScaleX = Convert.ToByte(row["PathScaleX"]);
            s.PathScaleY = Convert.ToByte(row["PathScaleY"]);
            s.PathShearX = Convert.ToByte(row["PathShearX"]);
            s.PathShearY = Convert.ToByte(row["PathShearY"]);
            s.PathSkew = Convert.ToSByte(row["PathSkew"]);
            s.PathCurve = Convert.ToByte(row["PathCurve"]);
            s.PathRadiusOffset = Convert.ToSByte(row["PathRadiusOffset"]);
            s.PathRevolutions = Convert.ToByte(row["PathRevolutions"]);
            s.PathTaperX = Convert.ToSByte(row["PathTaperX"]);
            s.PathTaperY = Convert.ToSByte(row["PathTaperY"]);
            s.PathTwist = Convert.ToSByte(row["PathTwist"]);
            s.PathTwistBegin = Convert.ToSByte(row["PathTwistBegin"]);
            // profile
            s.ProfileBegin = Convert.ToUInt16(row["ProfileBegin"]);
            s.ProfileEnd = Convert.ToUInt16(row["ProfileEnd"]);
            s.ProfileCurve = Convert.ToByte(row["ProfileCurve"]);
            s.ProfileHollow = Convert.ToUInt16(row["ProfileHollow"]);
            s.State = Convert.ToByte(row["State"]);

            byte[] textureEntry = (byte[])row["Texture"];
            s.TextureEntry = textureEntry;

            s.ExtraParams = (byte[]) row["ExtraParams"];
            return s;
        }

        /// <summary>
        /// see IRegionDatastore
        /// </summary>
        /// <param name="primID"></param>
        /// <param name="items"></param>
        public void StorePrimInventory(UUID primID, ICollection<TaskInventoryItem> items)
        {
//            m_log.DebugFormat(":[SQLITE REGION DB]: Entered StorePrimInventory with prim ID {0}", primID);

            RemoveItems(primID);

            using (SqliteCommand cmd = new SqliteCommand())
            {
                if (items.Count == 0)
                    return;

                cmd.CommandText = "insert into primitems (" +
                        "invType, assetType, name, " +
                        "description, creationDate, nextPermissions, " +
                        "currentPermissions, basePermissions, " +
                        "everyonePermissions, groupPermissions, " +
                        "flags, itemID, primID, assetID, " +
                        "parentFolderID, creatorID, ownerID, " +
                        "groupID, lastOwnerID) values (:invType, " +
                        ":assetType, :name, :description, " +
                        ":creationDate, :nextPermissions, " +
                        ":currentPermissions, :basePermissions, " +
                        ":everyonePermissions, :groupPermissions, " +
                        ":flags, :itemID, :primID, :assetID, " +
                        ":parentFolderID, :creatorID, :ownerID, " +
                        ":groupID, :lastOwnerID)";
                cmd.Connection = m_conn;

                foreach (TaskInventoryItem item in items)
                {
                    cmd.Parameters.Clear();

                    FillItemCommand(cmd, item);

                    cmd.ExecuteNonQuery();
                }

                cmd.Dispose();
            }
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
        /// Create an insert command
        /// </summary>
        /// <param name="table">table name</param>
        /// <param name="dt">data table</param>
        /// <returns>the created command</returns>
        /// <remarks>
        /// This is subtle enough to deserve some commentary.
        /// Instead of doing *lots* and *lots of hardcoded strings
        /// for database definitions we'll use the fact that
        /// realistically all insert statements look like "insert
        /// into A(b, c) values(:b, :c) on the parameterized query
        /// front.  If we just have a list of b, c, etc... we can
        /// generate these strings instead of typing them out.
        /// </remarks>
        private SqliteCommand createInsertCommand(string table, DataTable dt)
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
            //m_log.DebugFormat(":[SQLITE]: Created insert command {0}", sql);
            SqliteCommand cmd = new SqliteCommand(sql);

            // this provides the binding for all our parameters, so
            // much less code than it used to be
            foreach (DataColumn col in dt.Columns)
            {
                cmd.Parameters.Add(createSqliteParameter(col.ColumnName, col.DataType));
            }
            return cmd;
        }


        /// <summary>
        /// create an update command
        /// </summary>
        /// <param name="table">table name</param>
        /// <param name="pk"></param>
        /// <param name="dt"></param>
        /// <returns>the created command</returns>
        private SqliteCommand createUpdateCommand(string table, string pk, DataTable dt)
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
            SqliteCommand cmd = new SqliteCommand(sql);

            // this provides the binding for all our parameters, so
            // much less code than it used to be

            foreach (DataColumn col in dt.Columns)
            {
                cmd.Parameters.Add(createSqliteParameter(col.ColumnName, col.DataType));
            }
            return cmd;
        }

        /// <summary>
        /// create an update command
        /// </summary>
        /// <param name="table">table name</param>
        /// <param name="pk"></param>
        /// <param name="dt"></param>
        /// <returns>the created command</returns>
        private SqliteCommand createUpdateCommand(string table, string pk1, string pk2, DataTable dt)
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
            SqliteCommand cmd = new SqliteCommand(sql);

            // this provides the binding for all our parameters, so
            // much less code than it used to be

            foreach (DataColumn col in dt.Columns)
            {
                cmd.Parameters.Add(createSqliteParameter(col.ColumnName, col.DataType));
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
        /// This is a convenience function that collapses 5 repetitive
        /// lines for defining SqliteParameters to 2 parameters:
        /// column name and database type.
        ///
        /// It assumes certain conventions like :param as the param
        /// name to replace in parametrized queries, and that source
        /// version is always current version, both of which are fine
        /// for us.
        ///</summary>
        ///<returns>a built sqlite parameter</returns>
        private SqliteParameter createSqliteParameter(string name, Type type)
        {
            SqliteParameter param = new SqliteParameter();
            param.ParameterName = ":" + name;
            param.DbType = dbtypeFromType(type);
            param.SourceColumn = name;
            param.SourceVersion = DataRowVersion.Current;
            return param;
        }

        private void setupTerrainCommands(SqliteDataAdapter da, SqliteConnection conn)
        {
            da.InsertCommand = createInsertCommand("terrain", ds.Tables["terrain"]);
            da.InsertCommand.Connection = conn;
        }

        private void setupLandCommands(SqliteDataAdapter da, SqliteConnection conn)
        {
            da.InsertCommand = createInsertCommand("land", ds.Tables["land"]);
            da.InsertCommand.Connection = conn;

            da.UpdateCommand = createUpdateCommand("land", "UUID=:UUID", ds.Tables["land"]);
            da.UpdateCommand.Connection = conn;

            SqliteCommand delete = new SqliteCommand("delete from land where UUID=:UUID");
            delete.Parameters.Add(createSqliteParameter("UUID", typeof(String)));
            da.DeleteCommand = delete;
            da.DeleteCommand.Connection = conn;
        }

        private void setupLandAccessCommands(SqliteDataAdapter da, SqliteConnection conn)
        {
            da.InsertCommand = createInsertCommand("landaccesslist", ds.Tables["landaccesslist"]);
            da.InsertCommand.Connection = conn;

            da.UpdateCommand = createUpdateCommand("landaccesslist", "LandUUID=:landUUID", "AccessUUID=:AccessUUID", ds.Tables["landaccesslist"]);
            da.UpdateCommand.Connection = conn;

            SqliteCommand delete = new SqliteCommand("delete from landaccesslist where LandUUID= :LandUUID and AccessUUID= :AccessUUID");
            delete.Parameters.Add(createSqliteParameter("LandUUID", typeof(String)));
            delete.Parameters.Add(createSqliteParameter("AccessUUID", typeof(String)));
            da.DeleteCommand = delete;
            da.DeleteCommand.Connection = conn;
            
        }

        private void setupRegionSettingsCommands(SqliteDataAdapter da, SqliteConnection conn)
        {
            da.InsertCommand = createInsertCommand("regionsettings", ds.Tables["regionsettings"]);
            da.InsertCommand.Connection = conn;
            da.UpdateCommand = createUpdateCommand("regionsettings", "regionUUID=:regionUUID", ds.Tables["regionsettings"]);
            da.UpdateCommand.Connection = conn;
        }

        /***********************************************************************
         *
         *  Type conversion functions
         *
         **********************************************************************/

        /// <summary>
        /// Type conversion function
        /// </summary>
        /// <param name="type"></param>
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
