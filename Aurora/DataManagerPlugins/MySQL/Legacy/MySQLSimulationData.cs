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
using Aurora.DataManager;
using MySql.Data.MySqlClient;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Data.MySQL
{
    /// <summary>
    ///   A MySQL Interface for the Region Server
    /// </summary>
    public class MySQLSimulationData : ILegacySimulationDataStore
    {
        private readonly object m_dbLock = new object();
        private string m_connectionString;

        #region ILegacySimulationDataStore Members

        public string Name
        {
            get { return "MySQL"; }
        }

        public void Initialise(string connectionString)
        {
            m_connectionString = connectionString;

            using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
            {
                dbcon.Open();

                // Apply new Migrations
                //
                Assembly assem = GetType().Assembly;
                LegacyMigration m = new LegacyMigration(dbcon, assem, "RegionStore");
                m.Update();
            }
        }

        public void Dispose()
        {
        }

        #endregion

        #region Objects

        public List<ISceneEntity> LoadObjects(UUID regionID, IScene scene)
        {
            try
            {
                const int ROWS_PER_QUERY = 5000;

                Dictionary<UUID, SceneObjectPart> prims = new Dictionary<UUID, SceneObjectPart>(ROWS_PER_QUERY);
                Dictionary<UUID, ISceneEntity> objects = new Dictionary<UUID, ISceneEntity>();
                int count = 0;

                #region Prim Loading

                lock (m_dbLock)
                {
                    using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                    {
                        dbcon.Open();

                        using (MySqlCommand cmd = dbcon.CreateCommand())
                        {
                            cmd.CommandText =
                                "SELECT * FROM prims LEFT JOIN primshapes ON prims.UUID = primshapes.UUID WHERE RegionUUID = ?RegionUUID";
                            cmd.Parameters.AddWithValue("RegionUUID", regionID.ToString());

                            using (IDataReader reader = ExecuteReader(cmd))
                            {
                                while (reader.Read())
                                {
                                    SceneObjectPart prim = BuildPrim(reader, scene);

                                    UUID parentID = DBGuid.FromDB(reader["SceneGroupID"].ToString());
                                    if (parentID != prim.UUID)
                                        prim.ParentUUID = parentID;

                                    prims[prim.UUID] = prim;

                                    ++count;
                                    if (count % ROWS_PER_QUERY == 0)
                                        MainConsole.Instance.Info("[REGION DB]: Loaded " + count + " prims...");
                                }
                            }
                        }
                    }
                }

                #endregion Prim Loading

                #region SceneObjectGroup Creation

                // Create all of the SOGs from the root prims first
#if (!ISWIN)
                foreach (SceneObjectPart prim in prims.Values)
                {
                    if (prim.ParentUUID == UUID.Zero)
                    {
                        objects[prim.UUID] = new SceneObjectGroup(prim, scene);
                    }
                }
#else
            foreach (SceneObjectPart prim in prims.Values.Where(prim => prim.ParentUUID == UUID.Zero))
            {
                objects[prim.UUID] = new SceneObjectGroup(prim, scene);
            }
#endif


                List<uint> foundLocalIDs = new List<uint>();
                // Add all of the children objects to the SOGs
                foreach (SceneObjectPart prim in prims.Values)
                {
                    if (!foundLocalIDs.Contains(prim.LocalId))
                        foundLocalIDs.Add(prim.LocalId);
                    else
                        prim.LocalId = 0; //Reset it! Only use it once!
                    ISceneEntity sog;
                    if (prim.UUID != prim.ParentUUID)
                    {
                        if (objects.TryGetValue(prim.ParentUUID, out sog))
                        {
                            sog.AddChild(prim, prim.LinkNum);
                        }
                        else
                        {
                            MainConsole.Instance.WarnFormat(
                                "[REGION DB]: Database contains an orphan child prim {0} {1} in region {2} pointing to missing parent {3}.  This prim will not be loaded.",
                                prim.Name, prim.UUID, regionID, prim.ParentUUID);
                        }
                    }
                }

                #endregion SceneObjectGroup Creation

                MainConsole.Instance.DebugFormat("[REGION DB]: Loaded {0} objects using {1} prims", objects.Count, prims.Count);

                #region Prim Inventory Loading

                // Instead of attempting to LoadItems on every prim,
                // most of which probably have no items... get a 
                // list from DB of all prims which have items and
                // LoadItems only on those
                List<SceneObjectPart> primsWithInventory = new List<SceneObjectPart>();
                lock (m_dbLock)
                {
                    using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                    {
                        dbcon.Open();

                        using (MySqlCommand itemCmd = dbcon.CreateCommand())
                        {
                            itemCmd.CommandText = "SELECT DISTINCT primID FROM primitems";
                            using (IDataReader itemReader = ExecuteReader(itemCmd))
                            {
                                while (itemReader.Read())
                                {
                                    if (!(itemReader["primID"] is DBNull))
                                    {
                                        UUID primID = DBGuid.FromDB(itemReader["primID"].ToString());
                                        if (prims.ContainsKey(primID))
                                            primsWithInventory.Add(prims[primID]);
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (SceneObjectPart prim in primsWithInventory)
                {
                    LoadItems(prim);
                }

                #endregion Prim Inventory Loading

                MainConsole.Instance.DebugFormat("[REGION DB]: Loaded inventory from {0} objects", primsWithInventory.Count);

                return new List<ISceneEntity>(objects.Values);
            }
            catch { return new List<ISceneEntity>(); }
        }

        /// <summary>
        ///   Load in a prim's persisted inventory.
        /// </summary>
        /// <param name = "prim">The prim</param>
        private void LoadItems(SceneObjectPart prim)
        {
            lock (m_dbLock)
            {
                List<TaskInventoryItem> inventory = new List<TaskInventoryItem>();

                using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                {
                    dbcon.Open();

                    using (MySqlCommand cmd = dbcon.CreateCommand())
                    {
                        cmd.CommandText = "select * from primitems where PrimID = ?PrimID";
                        cmd.Parameters.AddWithValue("PrimID", prim.UUID.ToString());

                        using (IDataReader reader = ExecuteReader(cmd))
                        {
                            while (reader.Read())
                            {
                                TaskInventoryItem item = BuildItem(reader);

                                item.ParentID = prim.UUID; // Values in database are often wrong
                                inventory.Add(item);
                            }
                        }
                    }
                }

                prim.Inventory.RestoreInventoryItems(inventory);
            }
        }

        #endregion

        #region Terrain (legacy)

        public short[] LoadTerrain(IScene scene, bool Revert, int RegionSizeX, int RegionSizeY)
        {
            lock (m_dbLock)
            {
                bool hasX = false;
                bool hasRevert = false;
                using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                {
                    dbcon.Open();

                    using (MySqlCommand cmd = dbcon.CreateCommand())
                    {
                        cmd.CommandText = "SHOW COLUMNS from terrain";
                        using (IDataReader reader = ExecuteReader(cmd))
                        {
                            while (reader.Read())
                            {
                                if (reader["Field"].ToString() == "X")
                                    hasX = true;
                                if (reader["Field"].ToString() == "Revert")
                                    hasRevert = true;
                            }
                        }
                    }
                }
                using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                {
                    dbcon.Open();

                    using (MySqlCommand cmd = dbcon.CreateCommand())
                    {
                        cmd.CommandText = "select Heightfield" + (hasX ? ",X,Y " : "") +
                                          "from terrain where RegionUUID = ?RegionUUID " +
                                          (hasRevert ? ("and Revert = '" + Revert.ToString() + "'") : "") +
                                          " order by Revision desc limit 1";

                        cmd.Parameters.AddWithValue("RegionUUID", scene.RegionInfo.RegionID.ToString());

                        using (IDataReader reader = ExecuteReader(cmd))
                        {
                            while (reader.Read())
                            {
                                if (!hasX || !hasRevert || reader["X"].ToString() == "-1")
                                {
                                    MainConsole.Instance.Warn("Found double terrain");
                                    byte[] heightmap = (byte[]) reader["Heightfield"];
                                    short[] map = new short[RegionSizeX*RegionSizeX];
                                    double[,] terrain = null;
                                    terrain = new double[RegionSizeX,RegionSizeY];
                                    terrain.Initialize();

                                    using (MemoryStream str = new MemoryStream(heightmap))
                                    {
                                        using (BinaryReader br = new BinaryReader(str))
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
                                            map[y*RegionSizeX + x] =
                                                (short) (terrain[x, y]*Constants.TerrainCompression);
                                        }
                                    }
                                    return map;
                                }
                                else
                                {
                                    MainConsole.Instance.Warn("Found single terrain");
                                    byte[] heightmap = (byte[]) reader["Heightfield"];
                                    short[] map = new short[RegionSizeX*RegionSizeX];
                                    int ii = 0;
                                    for (int i = 0; i < heightmap.Length; i += sizeof (short))
                                    {
                                        map[ii] = Utils.BytesToInt16(heightmap, i);
                                        ii++;
                                    }
                                    heightmap = null;
                                    return map;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        public short[] LoadWater(IScene scene, bool Revert, int RegionSizeX, int RegionSizeY)
        {
            lock (m_dbLock)
            {
                using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                {
                    dbcon.Open();

                    int r = Revert ? 3 : 2; //Use numbers so that we can coexist with terrain

                    using (MySqlCommand cmd = dbcon.CreateCommand())
                    {
                        cmd.CommandText = "select Heightfield,X,Y " +
                                          "from terrain where RegionUUID = ?RegionUUID and Revert = '" + r + "'" +
                                          "order by Revision desc limit 1";

                        cmd.Parameters.AddWithValue("RegionUUID", scene.RegionInfo.RegionID.ToString());

                        using (IDataReader reader = ExecuteReader(cmd))
                        {
                            while (reader.Read())
                            {
                                if (reader["X"].ToString() == "-1")
                                {
                                    byte[] heightmap = (byte[]) reader["Heightfield"];
                                    short[] map = new short[RegionSizeX*RegionSizeX];
                                    int ii = 0;
                                    for (int i = 0; i < heightmap.Length; i += sizeof (double))
                                    {
                                        map[ii] =
                                            (short) (Utils.BytesToDouble(heightmap, i)*Constants.TerrainCompression);
                                        ii++;
                                    }
                                    return map;
                                }
                                else
                                {
                                    byte[] heightmap = (byte[]) reader["Heightfield"];
                                    short[] map = new short[RegionSizeX*RegionSizeX];
                                    int ii = 0;
                                    for (int i = 0; i < heightmap.Length; i += sizeof (short))
                                    {
                                        map[ii] = Utils.BytesToInt16(heightmap, i);
                                        ii++;
                                    }
                                    return map;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        #endregion

        #region Land (legacy)

        public void RemoveAllLandObjects(UUID regionUUID)
        {
            lock (m_dbLock)
            {
                using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                {
                    dbcon.Open();

                    using (MySqlCommand cmd = dbcon.CreateCommand())
                    {
                        cmd.CommandText = "delete from land where RegionUUID = ?RegionUUID";
                        cmd.Parameters.AddWithValue("RegionUUID", regionUUID.ToString());
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public List<LandData> LoadLandObjects(UUID regionUUID)
        {
            List<LandData> landData = new List<LandData>();

            try
            {
                lock (m_dbLock)
                {
                    using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                    {
                        dbcon.Open();

                        using (MySqlCommand cmd = dbcon.CreateCommand())
                        {
                            cmd.CommandText = "select * from land where RegionUUID = ?RegionUUID";
                            cmd.Parameters.AddWithValue("RegionUUID", regionUUID.ToString());

                            using (IDataReader reader = ExecuteReader(cmd))
                            {
                                while (reader.Read())
                                {
                                    LandData newLand = BuildLandData(reader);
                                    landData.Add(newLand);
                                }
                            }
                        }

                        using (MySqlCommand cmd = dbcon.CreateCommand())
                        {
                            foreach (LandData land in landData)
                            {
                                cmd.Parameters.Clear();
                                cmd.CommandText = "select * from landaccesslist where LandUUID = ?LandUUID";
                                cmd.Parameters.AddWithValue("LandUUID", land.GlobalID.ToString());

                                using (IDataReader reader = ExecuteReader(cmd))
                                {
                                    while (reader.Read())
                                    {
                                        land.ParcelAccessList.Add(BuildLandAccessData(reader));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return landData;
        }

        #endregion

        private IDataReader ExecuteReader(MySqlCommand c)
        {
            IDataReader r = null;

            try
            {
                r = c.ExecuteReader();
            }
            catch (Exception)
            {
                //MainConsole.Instance.Error("[REGION DB]: MySQL error in ExecuteReader: " + e.Message);
                throw;
            }

            return r;
        }

        private void ExecuteNonQuery(MySqlCommand c)
        {
            try
            {
                c.ExecuteNonQuery();
            }
            catch (Exception)
            {
                //MainConsole.Instance.Error("[REGION DB]: MySQL error in ExecuteNonQuery: " + e.Message);
                throw;
            }
        }

        public void Shutdown()
        {
        }

        private SceneObjectPart BuildPrim(IDataReader row, IScene scene)
        {
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

            PrimitiveBaseShape s = new PrimitiveBaseShape();

            float ScaleX = 0;
            float ScaleY = 0;
            float ScaleZ = 0;

            try
            {
                for (int i = 0; i < o.Length; i++)
                {
                    string name = row.GetName(i);

                    #region Switch

                    switch (name)
                    {
                        case "UUID":
                            prim.UUID = DBGuid.FromDB(o[i].ToString());
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
                            prim.Sound = DBGuid.FromDB(o[i].ToString());
                            break;
                        case "LoopedSoundGain":
                            prim.SoundGain = Convert.ToSingle(o[i].ToString());
                            break;
                        case "TextureAnimation":
                            if (!(row[i] is DBNull))
                                prim.TextureAnimation = (byte[]) o[i];
                            break;
                        case "ParticleSystem":
                            if (!(row[i] is DBNull))
                                prim.ParticleSystem = (byte[]) o[i];
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
                            prim.CollisionSound = DBGuid.FromDB(o[i].ToString());
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
                        case "MediaURL":
                            if (!(o[i] is DBNull))
                                prim.MediaUrl = (string) o[i];
                            break;
                        case "SceneGroupID":
                            break;
                        case "RegionUUID":
                            break;
                        case "Shape":
                            break;
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
                        case "Media":
                            if (!(o[i] is DBNull))
                                s.Media = PrimitiveBaseShape.MediaList.FromXml((string) o[i]);
                            break;
                        default:
                            MainConsole.Instance.Warn("[MySQL]: Unknown database row: " + name);
                            break;
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Warn("[MySQL]: Exception loading a SceneObject, " + ex + ", not loading..");
                return null;
            }
            s.Scale = new Vector3(ScaleX, ScaleY, ScaleZ);
            prim.Shape = s;
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
        private static TaskInventoryItem BuildItem(IDataReader row)
        {
            TaskInventoryItem taskItem = new TaskInventoryItem
                                             {
                                                 ItemID = DBGuid.FromDB(row["itemID"]),
                                                 ParentPartID = DBGuid.FromDB(row["primID"]),
                                                 AssetID = DBGuid.FromDB(row["assetID"]),
                                                 ParentID = DBGuid.FromDB(row["parentFolderID"]),
                                                 InvType = Convert.ToInt32(row["invType"]),
                                                 Type = Convert.ToInt32(row["assetType"]),
                                                 Name = (String) row["name"],
                                                 Description = (String) row["description"],
                                                 CreationDate = Convert.ToUInt32(row["creationDate"]),
                                                 CreatorIdentification = row["creatorID"].ToString(),
                                                 OwnerID = DBGuid.FromDB(row["ownerID"]),
                                                 LastOwnerID = DBGuid.FromDB(row["lastOwnerID"]),
                                                 GroupID = DBGuid.FromDB(row["groupID"]),
                                                 NextPermissions = Convert.ToUInt32(row["nextPermissions"]),
                                                 CurrentPermissions = Convert.ToUInt32(row["currentPermissions"]),
                                                 BasePermissions = Convert.ToUInt32(row["basePermissions"]),
                                                 EveryonePermissions = Convert.ToUInt32(row["everyonePermissions"]),
                                                 GroupPermissions = Convert.ToUInt32(row["groupPermissions"]),
                                                 Flags = Convert.ToUInt32(row["flags"]),
                                                 SalePrice = Convert.ToInt32(row["salePrice"]),
                                                 SaleType = Convert.ToByte(row["saleType"])
                                             };





            return taskItem;
        }

        private static RegionSettings BuildRegionSettings(IDataReader row)
        {
            RegionSettings newSettings = new RegionSettings
                                             {
                                                 RegionUUID = DBGuid.FromDB(row["regionUUID"]),
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
                                                 TerrainTexture1 = DBGuid.FromDB(row["terrain_texture_1"]),
                                                 TerrainTexture2 = DBGuid.FromDB(row["terrain_texture_2"]),
                                                 TerrainTexture3 = DBGuid.FromDB(row["terrain_texture_3"]),
                                                 TerrainTexture4 = DBGuid.FromDB(row["terrain_texture_4"]),
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
                                                 Covenant = DBGuid.FromDB(row["covenant"]),
                                                 CovenantLastUpdated = Convert.ToInt32(row["covenantlastupdated"]),
                                                 LoadedCreationDateTime =
                                                     Convert.ToInt32(row["loaded_creation_datetime"])
                                             };



            if (row["loaded_creation_id"] is DBNull)
                newSettings.LoadedCreationID = "";
            else
                newSettings.LoadedCreationID = (String) row["loaded_creation_id"];

            newSettings.TerrainImageID = DBGuid.FromDB(row["map_tile_ID"]);
            newSettings.TerrainMapImageID = DBGuid.FromDB(row["terrain_tile_ID"]);
            newSettings.MinimumAge = Convert.ToInt32(row["minimum_age"]);

            OSD o = OSDParser.DeserializeJson((String) row["generic"]);
            if (o.Type == OSDType.Map)
                newSettings.Generic = (OSDMap) o;

            return newSettings;
        }

        ///<summary>
        ///</summary>
        ///<param name = "row"></param>
        ///<returns></returns>
        private static LandData BuildLandData(IDataReader row)
        {
            LandData newData = new LandData
                                   {
                                       GlobalID = DBGuid.FromDB(row["UUID"]),
                                       LocalID = Convert.ToInt32(row["LocalLandID"]),
                                       Bitmap = (Byte[]) row["Bitmap"],
                                       Name = (String) row["Name"],
                                       Description = (String) row["Description"],
                                       OwnerID = DBGuid.FromDB(row["OwnerUUID"]),
                                       IsGroupOwned = Convert.ToBoolean(row["IsGroupOwned"]),
                                       Area = Convert.ToInt32(row["Area"]),
                                       AuctionID = Convert.ToUInt32(row["AuctionID"]),
                                       Category = (ParcelCategory) Convert.ToInt32(row["Category"]),
                                       ClaimDate = Convert.ToInt32(row["ClaimDate"]),
                                       ClaimPrice = Convert.ToInt32(row["ClaimPrice"]),
                                       GroupID = DBGuid.FromDB(row["GroupUUID"]),
                                       SalePrice = Convert.ToInt32(row["SalePrice"]),
                                       Status = (ParcelStatus) Convert.ToInt32(row["LandStatus"]),
                                       Flags = Convert.ToUInt32(row["LandFlags"]),
                                       LandingType = Convert.ToByte(row["LandingType"]),
                                       MediaAutoScale = Convert.ToByte(row["MediaAutoScale"]),
                                       MediaID = DBGuid.FromDB(row["MediaTextureUUID"]),
                                       MediaURL = (String) row["MediaURL"],
                                       MusicURL = (String) row["MusicURL"],
                                       PassHours = Convert.ToSingle(row["PassHours"]),
                                       PassPrice = Convert.ToInt32(row["PassPrice"])
                                   };


            // Bitmap is a byte[512]

            //Unimplemented
            //Enum libsecondlife.Parcel.ParcelCategory
            //Enum. libsecondlife.Parcel.ParcelStatus
            UUID authedbuyer = UUID.Zero;
            UUID snapshotID = UUID.Zero;

            UUID.TryParse((string) row["AuthBuyerID"], out authedbuyer);
            UUID.TryParse((string) row["SnapshotUUID"], out snapshotID);
            newData.OtherCleanTime = Convert.ToInt32(row["OtherCleanTime"]);

            newData.AuthBuyerID = authedbuyer;
            newData.SnapshotID = snapshotID;
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
                newData.UserLocation = Vector3.Zero;
                newData.UserLookAt = Vector3.Zero;
                MainConsole.Instance.ErrorFormat("[PARCEL]: unable to get parcel telehub settings for {1}", newData.Name);
            }

            newData.MediaDescription = (string) row["MediaDescription"];
            newData.MediaType = (string) row["MediaType"];
            newData.MediaWidth = Convert.ToInt32((((string) row["MediaSize"]).Split(','))[0]);
            newData.MediaHeight = Convert.ToInt32((((string) row["MediaSize"]).Split(','))[1]);
            newData.MediaLoop = Convert.ToBoolean(row["MediaLoop"]);
            newData.ObscureMusic = Convert.ToBoolean(row["ObscureMusic"]);
            newData.ObscureMedia = Convert.ToBoolean(row["ObscureMedia"]);

            newData.ParcelAccessList = new List<ParcelManager.ParcelAccessEntry>();

            return newData;
        }

        ///<summary>
        ///</summary>
        ///<param name = "row"></param>
        ///<returns></returns>
        private static ParcelManager.ParcelAccessEntry BuildLandAccessData(IDataReader row)
        {
            ParcelManager.ParcelAccessEntry entry = new ParcelManager.ParcelAccessEntry
                                                        {
                                                            AgentID = DBGuid.FromDB(row["AccessUUID"]),
                                                            Flags = (AccessList) Convert.ToInt32(row["Flags"]),
                                                            Time = new DateTime()
                                                        };
            return entry;
        }
    }
}