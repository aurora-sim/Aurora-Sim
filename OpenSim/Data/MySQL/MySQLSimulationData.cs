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
using System.Threading;
using log4net;
using MySql.Data.MySqlClient;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Data;

namespace OpenSim.Data.MySQL
{
    /// <summary>
    /// A MySQL Interface for the Region Server
    /// </summary>
    public class MySQLSimulationData : ISimulationDataStore
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private string m_connectionString;
        private object m_dbLock = new object();

        public MySQLSimulationData()
        {
        }

        public MySQLSimulationData(string connectionString)
        {
            Initialise(connectionString);
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
                Migration m = new Migration(dbcon, assem, "RegionStore");
                m.Update();
            }
        }

        private IDataReader ExecuteReader(MySqlCommand c)
        {
            IDataReader r = null;

            try
            {
                r = c.ExecuteReader();
            }
            catch (Exception e)
            {
                m_log.Error("[REGION DB]: MySQL error in ExecuteReader: " + e.Message);
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
            catch (Exception e)
            {
                m_log.Error("[REGION DB]: MySQL error in ExecuteNonQuery: " + e.Message);
                throw;
            }
        }

        public void Dispose() {}

        public void StoreObject(SceneObjectGroup obj, UUID regionUUID)
        {
            uint flags = obj.RootPart.GetEffectiveObjectFlags();

            // Eligibility check
            //
            if ((flags & (uint)PrimFlags.Temporary) != 0)
                return;
            if ((flags & (uint)PrimFlags.TemporaryOnRez) != 0)
                return;

            lock (m_dbLock)
            {
                using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                {
                    dbcon.Open();
                    MySqlCommand cmd = dbcon.CreateCommand();

                    foreach (SceneObjectPart prim in obj.ChildrenList)
                    {
                        cmd.Parameters.Clear();

                        //Remove the old prim
                        cmd.CommandText = "delete from prims where UUID = '" + prim.UUID + "' OR SceneGroupID = '" + prim.UUID + "'";
                        cmd.ExecuteNonQuery();

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
                                "LinkNumber, MediaURL, Generic) values (" + "?UUID, " +
                                "?CreationDate, ?Name, ?Text, " +
                                "?Description, ?SitName, ?TouchName, " +
                                "?ObjectFlags, ?OwnerMask, ?NextOwnerMask, " +
                                "?GroupMask, ?EveryoneMask, ?BaseMask, " +
                                "?PositionX, ?PositionY, ?PositionZ, " +
                                "?GroupPositionX, ?GroupPositionY, " +
                                "?GroupPositionZ, ?VelocityX, " +
                                "?VelocityY, ?VelocityZ, ?AngularVelocityX, " +
                                "?AngularVelocityY, ?AngularVelocityZ, " +
                                "?AccelerationX, ?AccelerationY, " +
                                "?AccelerationZ, ?RotationX, " +
                                "?RotationY, ?RotationZ, " +
                                "?RotationW, ?SitTargetOffsetX, " +
                                "?SitTargetOffsetY, ?SitTargetOffsetZ, " +
                                "?SitTargetOrientW, ?SitTargetOrientX, " +
                                "?SitTargetOrientY, ?SitTargetOrientZ, " +
                                "?RegionUUID, ?CreatorID, ?OwnerID, " +
                                "?GroupID, ?LastOwnerID, ?SceneGroupID, " +
                                "?PayPrice, ?PayButton1, ?PayButton2, " +
                                "?PayButton3, ?PayButton4, ?LoopedSound, " +
                                "?LoopedSoundGain, ?TextureAnimation, " +
                                "?OmegaX, ?OmegaY, ?OmegaZ, " +
                                "?CameraEyeOffsetX, ?CameraEyeOffsetY, " +
                                "?CameraEyeOffsetZ, ?CameraAtOffsetX, " +
                                "?CameraAtOffsetY, ?CameraAtOffsetZ, " +
                                "?ForceMouselook, ?ScriptAccessPin, " +
                                "?AllowedDrop, ?DieAtEdge, ?SalePrice, " +
                                "?SaleType, ?ColorR, ?ColorG, " +
                                "?ColorB, ?ColorA, ?ParticleSystem, " +
                                "?ClickAction, ?Material, ?CollisionSound, " +
                                "?CollisionSoundVolume, ?PassTouches, ?LinkNumber, ?MediaURL, ?Generic)";

                        FillPrimCommand(cmd, prim, obj.UUID, regionUUID);

                        ExecuteNonQuery(cmd);

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
                                "ExtraParams, State, Media) values (?UUID, " +
                                "?Shape, ?ScaleX, ?ScaleY, ?ScaleZ, " +
                                "?PCode, ?PathBegin, ?PathEnd, " +
                                "?PathScaleX, ?PathScaleY, " +
                                "?PathShearX, ?PathShearY, " +
                                "?PathSkew, ?PathCurve, ?PathRadiusOffset, " +
                                "?PathRevolutions, ?PathTaperX, " +
                                "?PathTaperY, ?PathTwist, " +
                                "?PathTwistBegin, ?ProfileBegin, " +
                                "?ProfileEnd, ?ProfileCurve, " +
                                "?ProfileHollow, ?Texture, ?ExtraParams, " +
                                "?State, ?Media)";

                        FillShapeCommand(cmd, prim);

                        ExecuteNonQuery(cmd);
                    }
                    
                    cmd.Dispose();
                }
            }
        }

        public void RemoveObject(UUID obj, UUID regionUUID)
        {
//            m_log.DebugFormat("[REGION DB]: Deleting scene object {0} from {1} in database", obj, regionUUID);
            
            List<UUID> uuids = new List<UUID>();

            // Formerly, this used to check the region UUID.
            // That makes no sense, as we remove the contents of a prim
            // unconditionally, but the prim dependent on the region ID.
            // So, we would destroy an object and cause hard to detect
            // issues if we delete the contents only. Deleting it all may
            // cause the loss of a prim, but is cleaner.
            // It's also faster because it uses the primary key.
            //
            lock (m_dbLock)
            {
                using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                {
                    dbcon.Open();

                    using (MySqlCommand cmd = dbcon.CreateCommand())
                    {
                        cmd.CommandText = "select UUID from prims where SceneGroupID= ?UUID";
                        cmd.Parameters.AddWithValue("UUID", obj.ToString());

                        using (IDataReader reader = ExecuteReader(cmd))
                        {
                            while (reader.Read())
                                uuids.Add(DBGuid.FromDB(reader["UUID"].ToString()));
                        }

                        // delete the main prims
                        cmd.CommandText = "delete from prims where SceneGroupID= ?UUID";
                        ExecuteNonQuery(cmd);
                    }
                }
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

        public void RemoveObjects(List<UUID> uuids)
        {
            // Formerly, this used to check the region UUID.
            // That makes no sense, as we remove the contents of a prim
            // unconditionally, but the prim dependent on the region ID.
            // So, we would destroy an object and cause hard to detect
            // issues if we delete the contents only. Deleting it all may
            // cause the loss of a prim, but is cleaner.
            // It's also faster because it uses the primary key.
            //
            lock (m_dbLock)
            {
                using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                {
                    dbcon.Open();

                    using (MySqlCommand cmd = dbcon.CreateCommand())
                    {
                        for (int cntr = 0; cntr < uuids.Count; cntr += 10)
                        {
                            string sql = "delete from prims where ";
                            int max = (uuids.Count - cntr) < 10 ? (uuids.Count - cntr) : 10;
                            for (int i = 0; i < max; i++)
                            {
                                if ((i + 1) == max)
                                {// end of the list
                                    sql += "(SceneGroupID = ?UUID" + i + ")";
                                }
                                else
                                {
                                    sql += "(SceneGroupID = ?UUID" + i + ") or ";
                                }
                                cmd.Parameters.AddWithValue("UUID" + i, uuids[cntr + i].ToString());
                            }
                            cmd.CommandText = sql;

                            ExecuteNonQuery(cmd);
                            cmd.Parameters.Clear();
                        }
                    }
                }
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

        public void RemoveRegion(UUID regionUUID)
        {
            List<UUID> uuids = new List<UUID>();
            lock (m_dbLock)
            {
                using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                {
                    dbcon.Open();

                    using (MySqlCommand cmd = dbcon.CreateCommand())
                    {
                        cmd.CommandText = "select UUID from prims where RegionUUID = ?UUID";
                        cmd.Parameters.AddWithValue("UUID", regionUUID.ToString());

                        using (IDataReader reader = ExecuteReader(cmd))
                        {
                            while (reader.Read())
                                uuids.Add(DBGuid.FromDB(reader["UUID"].ToString()));
                        }

                        cmd.CommandText = "delete from prims where RegionUUID = ?UUID";
                        ExecuteNonQuery(cmd);
                    }
                }
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
        /// Remove all persisted items of the given prim.
        /// The caller must acquire the necessrary synchronization locks
        /// </summary>
        /// <param name="uuid">the Item UUID</param>
        private void RemoveItems(UUID uuid)
        {
            lock (m_dbLock)
            {
                using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                {
                    dbcon.Open();

                    using (MySqlCommand cmd = dbcon.CreateCommand())
                    {
                        cmd.CommandText = "delete from primitems where PrimID = ?PrimID";
                        cmd.Parameters.AddWithValue("PrimID", uuid.ToString());

                        ExecuteNonQuery(cmd);
                    }
                }
            }
        }

        /// <summary>
        /// Remove all persisted shapes for a list of prims
        /// The caller must acquire the necessrary synchronization locks
        /// </summary>
        /// <param name="uuids">the list of UUIDs</param>
        private void RemoveShapes(List<UUID> uuids)
        {
            lock (m_dbLock)
            {
                string sql = "delete from primshapes where ";
                using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                {
                    dbcon.Open();

                    using (MySqlCommand cmd = dbcon.CreateCommand())
                    {
                        for (int cntr = 0; cntr < uuids.Count; cntr += 10)
                        {
                            int max = (uuids.Count - cntr) < 10 ? (uuids.Count - cntr) : 10;
                            for (int i = 0; i < max; i++)
                            {
                                if ((i + 1) == max)
                                {// end of the list
                                    sql += "(UUID = ?UUID" + i + ")";
                                }
                                else
                                {
                                    sql += "(UUID = ?UUID" + i + ") or ";
                                }
                                cmd.Parameters.AddWithValue("UUID" + i, uuids[cntr + i].ToString());
                            }
                            cmd.CommandText = sql;

                            ExecuteNonQuery(cmd);
                            sql = "delete from primshapes where ";
                            cmd.Parameters.Clear();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Remove all persisted items for a list of prims
        /// The caller must acquire the necessrary synchronization locks
        /// </summary>
        /// <param name="uuids">the list of UUIDs</param>
        private void RemoveItems(List<UUID> uuids)
        {
            lock (m_dbLock)
            {
                string sql = "delete from primitems where ";
                using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                {
                    dbcon.Open();

                    using (MySqlCommand cmd = dbcon.CreateCommand())
                    {
                        for (int cntr = 0; cntr < uuids.Count; cntr += 10)
                        {
                            int max = (uuids.Count - cntr) < 10 ? (uuids.Count - cntr) : 10;
                            for (int i = 0; i < max; i++)
                            {
                                if ((i + 1) == max)
                                {
                                    // end of the list
                                    sql += "(PrimID = ?PrimID" + i + ")";
                                }
                                else
                                {
                                    sql += "(PrimID = ?PrimID" + i + ") or ";
                                }
                                cmd.Parameters.AddWithValue("PrimID" + i, uuids[cntr + i].ToString());
                            }
                            cmd.CommandText = sql;

                            ExecuteNonQuery(cmd);
                            sql = "delete from primitems where ";
                            cmd.Parameters.Clear();
                        }
                    }
                }
            }
        }

        public List<SceneObjectGroup> LoadObjects(UUID regionID, Scene scene)
        {
            const int ROWS_PER_QUERY = 5000;

            Dictionary<UUID, SceneObjectPart> prims = new Dictionary<UUID, SceneObjectPart>(ROWS_PER_QUERY);
            Dictionary<UUID, SceneObjectGroup> objects = new Dictionary<UUID, SceneObjectGroup>();
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
                                    m_log.Debug("[REGION DB]: Loaded " + count + " prims...");
                            }
                        }
                    }
                }
            }

            #endregion Prim Loading

            #region SceneObjectGroup Creation

            // Create all of the SOGs from the root prims first
            foreach (SceneObjectPart prim in prims.Values)
            {
                if (prim.ParentUUID == UUID.Zero)
                    objects[prim.UUID] = new SceneObjectGroup(prim, scene);
            }

            // Add all of the children objects to the SOGs
            foreach (SceneObjectPart prim in prims.Values)
            {
                SceneObjectGroup sog;
                if (prim.UUID != prim.ParentUUID)
                {
                    if (objects.TryGetValue(prim.ParentUUID, out sog))
                    {
                        sog.AddChild(prim, prim.LinkNum);
                    }
                    else
                    {
                        m_log.WarnFormat(
                            "[REGION DB]: Database contains an orphan child prim {0} {1} in region {2} pointing to missing parent {3}.  This prim will not be loaded.",
                            prim.Name, prim.UUID, regionID, prim.ParentUUID);
                    }
                }
            }

            #endregion SceneObjectGroup Creation

            m_log.DebugFormat("[REGION DB]: Loaded {0} objects using {1} prims", objects.Count, prims.Count);

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

            m_log.DebugFormat("[REGION DB]: Loaded inventory from {0} objects", primsWithInventory.Count);

            return new List<SceneObjectGroup>(objects.Values);
        }

        /// <summary>
        /// Load in a prim's persisted inventory.
        /// </summary>
        /// <param name="prim">The prim</param>
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

        public void StoreTerrain(double[,] ter, UUID regionID, bool Revert)
        {
            m_log.Info("[REGION DB]: Storing terrain");

            lock (m_dbLock)
            {
                using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                {
                    dbcon.Open();

                    using (MySqlCommand cmd = dbcon.CreateCommand())
                    {
                        cmd.CommandText = "delete from terrain where RegionUUID = ?RegionUUID and Revert = ?Revert";
                        cmd.Parameters.AddWithValue("RegionUUID", regionID.ToString());
                        cmd.Parameters.AddWithValue("Revert", Revert.ToString());

                        ExecuteNonQuery(cmd);

                        cmd.CommandText = "insert into terrain (RegionUUID, " +
                            "Revision, Heightfield, Revert) values (?RegionUUID, " +
                            "1, ?Heightfield, ?Revert)";

                        cmd.Parameters.AddWithValue("Heightfield", SerializeTerrain(ter));

                        ExecuteNonQuery(cmd);
                    }
                }
            }
        }

        public double[,] LoadTerrain(UUID regionID, bool Revert)
        {
            double[,] terrain = null;

            lock (m_dbLock)
            {
                using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                {
                    dbcon.Open();

                    using (MySqlCommand cmd = dbcon.CreateCommand())
                    {
                        cmd.CommandText = "select RegionUUID, Revision, Heightfield " +
                            "from terrain where RegionUUID = ?RegionUUID and Revert = '" + Revert.ToString() + "'" +
                            "order by Revision desc limit 1";
                        
                        cmd.Parameters.AddWithValue("RegionUUID", regionID.ToString());

                        using (IDataReader reader = ExecuteReader(cmd))
                        {
                            while (reader.Read())
                            {
                                int rev = Convert.ToInt32(reader["Revision"]);

                                terrain = new double[(int)Constants.RegionSize, (int)Constants.RegionSize];
                                terrain.Initialize();

                                using (MemoryStream mstr = new MemoryStream((byte[])reader["Heightfield"]))
                                {
                                    using (BinaryReader br = new BinaryReader(mstr))
                                    {
                                        for (int x = 0; x < (int)Constants.RegionSize; x++)
                                        {
                                            for (int y = 0; y < (int)Constants.RegionSize; y++)
                                            {
                                                terrain[x, y] = br.ReadDouble();
                                            }
                                        }
                                    }

                                    //m_log.InfoFormat("[REGION DB]: Loaded terrain revision r{0}", rev);
                                }
                            }
                        }
                    }
                }
            }

            return terrain;
        }

        public void RemoveLandObject(UUID RegionID, UUID globalID)
        {
            lock (m_dbLock)
            {
                using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                {
                    dbcon.Open();

                    using (MySqlCommand cmd = dbcon.CreateCommand())
                    {
                        cmd.CommandText = "delete from land where UUID = ?UUID";
                        cmd.Parameters.AddWithValue("UUID", globalID.ToString());

                        ExecuteNonQuery(cmd);
                    }
                }
            }
        }

        public void StoreLandObject(LandData parcel)
        {
            lock (m_dbLock)
            {
                using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                {
                    dbcon.Open();

                    using (MySqlCommand cmd = dbcon.CreateCommand())
                    {
                        cmd.CommandText = "replace into land (UUID, RegionUUID, " +
                            "LocalLandID, Bitmap, Name, Description, " +
                            "OwnerUUID, IsGroupOwned, Area, AuctionID, " +
                            "Category, ClaimDate, ClaimPrice, GroupUUID, " +
                            "SalePrice, LandStatus, LandFlags, LandingType, " +
                            "MediaAutoScale, MediaTextureUUID, MediaURL, " +
                            "MusicURL, PassHours, PassPrice, SnapshotUUID, " +
                            "UserLocationX, UserLocationY, UserLocationZ, " +
                            "UserLookAtX, UserLookAtY, UserLookAtZ, " +
                            "AuthbuyerID, OtherCleanTime, MediaType, MediaDescription, " +
                            "MediaSize, MediaLoop, ObscureMusic, ObscureMedia) values (" +
                            "?UUID, ?RegionUUID, " +
                            "?LocalLandID, ?Bitmap, ?Name, ?Description, " +
                            "?OwnerUUID, ?IsGroupOwned, ?Area, ?AuctionID, " +
                            "?Category, ?ClaimDate, ?ClaimPrice, ?GroupUUID, " +
                            "?SalePrice, ?LandStatus, ?LandFlags, ?LandingType, " +
                            "?MediaAutoScale, ?MediaTextureUUID, ?MediaURL, " +
                            "?MusicURL, ?PassHours, ?PassPrice, ?SnapshotUUID, " +
                            "?UserLocationX, ?UserLocationY, ?UserLocationZ, " +
                            "?UserLookAtX, ?UserLookAtY, ?UserLookAtZ, " +
                            "?AuthbuyerID, ?OtherCleanTime, ?MediaType, ?MediaDescription, "+
                            "CONCAT(?MediaWidth, ',', ?MediaHeight), ?MediaLoop, ?ObscureMusic, ?ObscureMedia)";

                        FillLandCommand(cmd, parcel, parcel.RegionID);

                        ExecuteNonQuery(cmd);

                        cmd.CommandText = "delete from landaccesslist where LandUUID = ?UUID";

                        ExecuteNonQuery(cmd);

                        cmd.Parameters.Clear();
                        cmd.CommandText = "insert into landaccesslist (LandUUID, " +
                                "AccessUUID, Flags) values (?LandUUID, ?AccessUUID, " +
                                "?Flags)";

                        foreach (ParcelManager.ParcelAccessEntry entry in parcel.ParcelAccessList)
                        {
                            FillLandAccessCommand(cmd, entry, parcel.GlobalID);
                            ExecuteNonQuery(cmd);
                            cmd.Parameters.Clear();
                        }
                    }
                }
            }
        }

        public RegionSettings LoadRegionSettings(UUID regionUUID)
        {
            RegionSettings rs = null;

            lock (m_dbLock)
            {
                using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                {
                    dbcon.Open();

                    using (MySqlCommand cmd = dbcon.CreateCommand())
                    {
                        cmd.CommandText = "select * from regionsettings where regionUUID = ?RegionUUID";
                        cmd.Parameters.AddWithValue("regionUUID", regionUUID);

                        using (IDataReader reader = ExecuteReader(cmd))
                        {
                            if (reader.Read())
                            {
                                rs = BuildRegionSettings(reader);
                                rs.OnSave += StoreRegionSettings;
                            }
                            else
                            {
                                rs = new RegionSettings();
                                rs.RegionUUID = regionUUID;
                                rs.OnSave += StoreRegionSettings;

                                StoreRegionSettings(rs);
                            }
                        }
                    }
                }
            }

            return rs;
        }

        public void StoreRegionSettings(RegionSettings rs)
        {
            lock (m_dbLock)
            {
                using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                {
                    dbcon.Open();

                    using (MySqlCommand cmd = dbcon.CreateCommand())
                    {
                        cmd.CommandText = "replace into regionsettings (regionUUID, " +
                            "block_terraform, block_fly, allow_damage, " +
                            "restrict_pushing, allow_land_resell, " +
                            "allow_land_join_divide, block_show_in_search, " +
                            "agent_limit, object_bonus, maturity, " +
                            "disable_scripts, disable_collisions, " +
                            "disable_physics, terrain_texture_1, " +
                            "terrain_texture_2, terrain_texture_3, " +
                            "terrain_texture_4, elevation_1_nw, " +
                            "elevation_2_nw, elevation_1_ne, " +
                            "elevation_2_ne, elevation_1_se, " +
                            "elevation_2_se, elevation_1_sw, " +
                            "elevation_2_sw, water_height, " +
                            "terrain_raise_limit, terrain_lower_limit, " +
                            "use_estate_sun, fixed_sun, sun_position, " +
                            "covenant, Sandbox, sunvectorx, sunvectory, " +
                            "sunvectorz, loaded_creation_datetime, " +
                            "loaded_creation_id, map_tile_ID, terrain_tile_ID) values (?RegionUUID, ?BlockTerraform, " +
                            "?BlockFly, ?AllowDamage, ?RestrictPushing, " +
                            "?AllowLandResell, ?AllowLandJoinDivide, " +
                            "?BlockShowInSearch, ?AgentLimit, ?ObjectBonus, " +
                            "?Maturity, ?DisableScripts, ?DisableCollisions, " +
                            "?DisablePhysics, ?TerrainTexture1, " +
                            "?TerrainTexture2, ?TerrainTexture3, " +
                            "?TerrainTexture4, ?Elevation1NW, ?Elevation2NW, " +
                            "?Elevation1NE, ?Elevation2NE, ?Elevation1SE, " +
                            "?Elevation2SE, ?Elevation1SW, ?Elevation2SW, " +
                            "?WaterHeight, ?TerrainRaiseLimit, " +
                            "?TerrainLowerLimit, ?UseEstateSun, ?FixedSun, " +
                            "?SunPosition, ?Covenant, ?Sandbox, " +
                            "?SunVectorX, ?SunVectorY, ?SunVectorZ, " +
                            "?LoadedCreationDateTime, ?LoadedCreationID, " +
                            "?TerrainImageID, ?TerrainMapImageID)";

                        FillRegionSettingsCommand(cmd, rs);

                        ExecuteNonQuery(cmd);
                    }
                }
            }
        }

        public List<LandData> LoadLandObjects(UUID regionUUID)
        {
            List<LandData> landData = new List<LandData>();

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

            return landData;
        }

        public void Shutdown()
        {
        }

        private SceneObjectPart BuildPrim(IDataReader row, Scene scene)
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
                            prim.Flags = (PrimFlags)Convert.ToUInt32(o[i].ToString());
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
                                prim.TextureAnimation = (byte[])o[i];
                            break;
                        case "ParticleSystem":
                            if (!(row[i] is DBNull))
                                prim.ParticleSystem = (byte[])o[i];
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
                            prim.PassTouch = (int)Convert.ToSingle(o[i].ToString());
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
                            if (!(o[i] is System.DBNull))
                                prim.MediaUrl = (string)o[i];
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
                            byte[] textureEntry = (byte[])o[i];
                            s.TextureEntry = textureEntry;
                            break;
                        case "ExtraParams":
                            s.ExtraParams = (byte[])o[i];
                            break;
                        case "Media":
                            if (!(o[i] is System.DBNull))
                                s.Media = PrimitiveBaseShape.MediaList.FromXml((string)o[i]);
                            break;
                        default:
                            m_log.Warn("[MySQL]: Unknown database row: " + name);
                            break;
                    }

                    #endregion
                }
            }
            catch(Exception ex)
            {
                m_log.Warn("[MySQL]: Exception loading a SceneObject, " + ex.ToString() + ", deleting..");
                this.RemoveObject(prim.UUID, UUID.Zero);
            }
            s.Scale = new Vector3(ScaleX, ScaleY, ScaleZ);
            prim.Shape = s;
            prim.SoundFlags = 1; // If it's persisted at all, it's looped

            prim.Color = Color.FromArgb(ColorA, ColorR, ColorG, ColorB);
            prim.OffsetPosition = new Vector3(PositionX, PositionY, PositionZ);
            prim.GroupPosition = new Vector3(GroupPositionX, GroupPositionY, GroupPositionZ);
            prim.Velocity = new Vector3(VelocityX, VelocityY, VelocityZ);
            prim.AngularVelocity = new Vector3(AngularVelocityX, AngularVelocityY, AngularVelocityZ);
            prim.RotationOffset = new Quaternion(RotationX, RotationY, RotationZ, RotationW);
            prim.SitTargetPositionLL = new Vector3(SitTargetOffsetX, SitTargetOffsetY, SitTargetOffsetZ);
            prim.SitTargetOrientationLL = new Quaternion(SitTargetOrientX, SitTargetOrientY, SitTargetOrientZ, SitTargetOrientW);
            prim.AngularVelocity = new Vector3(OmegaX, OmegaY, OmegaZ);
            prim.CameraEyeOffset = new Vector3(CameraEyeOffsetX, CameraEyeOffsetY, CameraEyeOffsetZ);
            prim.CameraAtOffset = new Vector3(CameraAtOffsetX, CameraAtOffsetY, CameraAtOffsetZ);

            return prim;
        }


        /// <summary>
        /// Build a prim inventory item from the persisted data.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private static TaskInventoryItem BuildItem(IDataReader row)
        {
            TaskInventoryItem taskItem = new TaskInventoryItem();

            taskItem.ItemID        = DBGuid.FromDB(row["itemID"]);
            taskItem.ParentPartID  = DBGuid.FromDB(row["primID"]);
            taskItem.AssetID       = DBGuid.FromDB(row["assetID"]);
            taskItem.ParentID      = DBGuid.FromDB(row["parentFolderID"]);

            taskItem.InvType       = Convert.ToInt32(row["invType"]);
            taskItem.Type          = Convert.ToInt32(row["assetType"]);

            taskItem.Name          = (String)row["name"];
            taskItem.Description   = (String)row["description"];
            taskItem.CreationDate  = Convert.ToUInt32(row["creationDate"]);
            taskItem.CreatorID     = DBGuid.FromDB(row["creatorID"]);
            taskItem.OwnerID       = DBGuid.FromDB(row["ownerID"]);
            taskItem.LastOwnerID   = DBGuid.FromDB(row["lastOwnerID"]);
            taskItem.GroupID       = DBGuid.FromDB(row["groupID"]);

            taskItem.NextPermissions = Convert.ToUInt32(row["nextPermissions"]);
            taskItem.CurrentPermissions     = Convert.ToUInt32(row["currentPermissions"]);
            taskItem.BasePermissions      = Convert.ToUInt32(row["basePermissions"]);
            taskItem.EveryonePermissions  = Convert.ToUInt32(row["everyonePermissions"]);
            taskItem.GroupPermissions     = Convert.ToUInt32(row["groupPermissions"]);
            taskItem.Flags = Convert.ToUInt32(row["flags"]);
            taskItem.SalePrice = Convert.ToInt32(row["salePrice"]);
            taskItem.SaleType = Convert.ToByte(row["saleType"]);

            return taskItem;
        }

        private static RegionSettings BuildRegionSettings(IDataReader row)
        {
            RegionSettings newSettings = new RegionSettings();

            newSettings.RegionUUID = DBGuid.FromDB(row["regionUUID"]);
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
            newSettings.TerrainTexture1 = DBGuid.FromDB(row["terrain_texture_1"]);
            newSettings.TerrainTexture2 = DBGuid.FromDB(row["terrain_texture_2"]);
            newSettings.TerrainTexture3 = DBGuid.FromDB(row["terrain_texture_3"]);
            newSettings.TerrainTexture4 = DBGuid.FromDB(row["terrain_texture_4"]);
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
            newSettings.Covenant = DBGuid.FromDB(row["covenant"]);
            newSettings.CovenantLastUpdated = Convert.ToInt32(row["covenantlastupdated"]);
            
            newSettings.LoadedCreationDateTime = Convert.ToInt32(row["loaded_creation_datetime"]);
            
            if (row["loaded_creation_id"] is DBNull)
                newSettings.LoadedCreationID = "";
            else 
                newSettings.LoadedCreationID = (String) row["loaded_creation_id"];

            newSettings.TerrainImageID = DBGuid.FromDB(row["map_tile_ID"]);
            newSettings.TerrainMapImageID = DBGuid.FromDB(row["terrain_tile_ID"]);
            newSettings.MinimumAge = Convert.ToInt32(row["minimum_age"]);

            OSD o = OSDParser.DeserializeJson((String)row["generic"]);
            if (o.Type == OSDType.Map)
                newSettings.Generic = (OSDMap)o;
            
            return newSettings;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private static LandData BuildLandData(IDataReader row)
        {
            LandData newData = new LandData();

            newData.GlobalID = DBGuid.FromDB(row["UUID"]);
            newData.LocalID = Convert.ToInt32(row["LocalLandID"]);

            // Bitmap is a byte[512]
            newData.Bitmap = (Byte[]) row["Bitmap"];

            newData.Name = (String) row["Name"];
            newData.Description = (String) row["Description"];
            newData.OwnerID = DBGuid.FromDB(row["OwnerUUID"]);
            newData.IsGroupOwned = Convert.ToBoolean(row["IsGroupOwned"]);
            newData.Area = Convert.ToInt32(row["Area"]);
            newData.AuctionID = Convert.ToUInt32(row["AuctionID"]); //Unimplemented
            newData.Category = (ParcelCategory) Convert.ToInt32(row["Category"]);
                //Enum libsecondlife.Parcel.ParcelCategory
            newData.ClaimDate = Convert.ToInt32(row["ClaimDate"]);
            newData.ClaimPrice = Convert.ToInt32(row["ClaimPrice"]);
            newData.GroupID = DBGuid.FromDB(row["GroupUUID"]);
            newData.SalePrice = Convert.ToInt32(row["SalePrice"]);
            newData.Status = (ParcelStatus) Convert.ToInt32(row["LandStatus"]);
                //Enum. libsecondlife.Parcel.ParcelStatus
            newData.Flags = Convert.ToUInt32(row["LandFlags"]);
            newData.LandingType = Convert.ToByte(row["LandingType"]);
            newData.MediaAutoScale = Convert.ToByte(row["MediaAutoScale"]);
            newData.MediaID = DBGuid.FromDB(row["MediaTextureUUID"]);
            newData.MediaURL = (String) row["MediaURL"];
            newData.MusicURL = (String) row["MusicURL"];
            newData.PassHours = Convert.ToSingle(row["PassHours"]);
            newData.PassPrice = Convert.ToInt32(row["PassPrice"]);
            UUID authedbuyer = UUID.Zero;
            UUID snapshotID = UUID.Zero;

            UUID.TryParse((string)row["AuthBuyerID"], out authedbuyer);
            UUID.TryParse((string)row["SnapshotUUID"], out snapshotID);
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
                m_log.ErrorFormat("[PARCEL]: unable to get parcel telehub settings for {1}", newData.Name);
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private static ParcelManager.ParcelAccessEntry BuildLandAccessData(IDataReader row)
        {
            ParcelManager.ParcelAccessEntry entry = new ParcelManager.ParcelAccessEntry();
            entry.AgentID = DBGuid.FromDB(row["AccessUUID"]);
            entry.Flags = (AccessList) Convert.ToInt32(row["Flags"]);
            entry.Time = new DateTime();
            return entry;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private static Array SerializeTerrain(double[,] val)
        {
            MemoryStream str = new MemoryStream(((int)Constants.RegionSize * (int)Constants.RegionSize) *sizeof (double));
            BinaryWriter bw = new BinaryWriter(str);

            // TODO: COMPATIBILITY - Add byte-order conversions
            for (int x = 0; x < (int)Constants.RegionSize; x++)
                for (int y = 0; y < (int)Constants.RegionSize; y++)
                {
                    double height = val[x, y];
                    if (height == 0.0)
                        height = double.Epsilon;

                    bw.Write(height);
                }

            return str.ToArray();
        }

        /// <summary>
        /// Fill the prim command with prim values
        /// </summary>
        /// <param name="row"></param>
        /// <param name="prim"></param>
        /// <param name="sceneGroupID"></param>
        /// <param name="regionUUID"></param>
        private void FillPrimCommand(MySqlCommand cmd, SceneObjectPart prim, UUID sceneGroupID, UUID regionUUID)
        {
            cmd.Parameters.AddWithValue("UUID", prim.UUID.ToString());
            cmd.Parameters.AddWithValue("RegionUUID", regionUUID.ToString());
            cmd.Parameters.AddWithValue("CreationDate", prim.CreationDate);
            cmd.Parameters.AddWithValue("Name", prim.Name);
            cmd.Parameters.AddWithValue("SceneGroupID", sceneGroupID.ToString());
                // the UUID of the root part for this SceneObjectGroup
            // various text fields
            cmd.Parameters.AddWithValue("Text", prim.Text);
            cmd.Parameters.AddWithValue("ColorR", prim.Color.R);
            cmd.Parameters.AddWithValue("ColorG", prim.Color.G);
            cmd.Parameters.AddWithValue("ColorB", prim.Color.B);
            cmd.Parameters.AddWithValue("ColorA", prim.Color.A);
            cmd.Parameters.AddWithValue("Description", prim.Description);
            cmd.Parameters.AddWithValue("SitName", prim.SitName);
            cmd.Parameters.AddWithValue("TouchName", prim.TouchName);
            // permissions
            cmd.Parameters.AddWithValue("ObjectFlags", (uint)prim.Flags);
            cmd.Parameters.AddWithValue("CreatorID", prim.CreatorID.ToString());
            cmd.Parameters.AddWithValue("OwnerID", prim.OwnerID.ToString());
            cmd.Parameters.AddWithValue("GroupID", prim.GroupID.ToString());
            cmd.Parameters.AddWithValue("LastOwnerID", prim.LastOwnerID.ToString());
            cmd.Parameters.AddWithValue("OwnerMask", prim.OwnerMask);
            cmd.Parameters.AddWithValue("NextOwnerMask", prim.NextOwnerMask);
            cmd.Parameters.AddWithValue("GroupMask", prim.GroupMask);
            cmd.Parameters.AddWithValue("EveryoneMask", prim.EveryoneMask);
            cmd.Parameters.AddWithValue("BaseMask", prim.BaseMask);
            // vectors
            cmd.Parameters.AddWithValue("PositionX", (double)prim.OffsetPosition.X);
            cmd.Parameters.AddWithValue("PositionY", (double)prim.OffsetPosition.Y);
            cmd.Parameters.AddWithValue("PositionZ", (double)prim.OffsetPosition.Z);
            cmd.Parameters.AddWithValue("GroupPositionX", (double)prim.GroupPosition.X);
            cmd.Parameters.AddWithValue("GroupPositionY", (double)prim.GroupPosition.Y);
            cmd.Parameters.AddWithValue("GroupPositionZ", (double)prim.GroupPosition.Z);
            cmd.Parameters.AddWithValue("VelocityX", (double)prim.Velocity.X);
            cmd.Parameters.AddWithValue("VelocityY", (double)prim.Velocity.Y);
            cmd.Parameters.AddWithValue("VelocityZ", (double)prim.Velocity.Z);
            cmd.Parameters.AddWithValue("AngularVelocityX", (double)prim.AngularVelocity.X);
            cmd.Parameters.AddWithValue("AngularVelocityY", (double)prim.AngularVelocity.Y);
            cmd.Parameters.AddWithValue("AngularVelocityZ", (double)prim.AngularVelocity.Z);
            cmd.Parameters.AddWithValue("AccelerationX", (double)prim.Acceleration.X);
            cmd.Parameters.AddWithValue("AccelerationY", (double)prim.Acceleration.Y);
            cmd.Parameters.AddWithValue("AccelerationZ", (double)prim.Acceleration.Z);
            // quaternions
            cmd.Parameters.AddWithValue("RotationX", (double)prim.RotationOffset.X);
            cmd.Parameters.AddWithValue("RotationY", (double)prim.RotationOffset.Y);
            cmd.Parameters.AddWithValue("RotationZ", (double)prim.RotationOffset.Z);
            cmd.Parameters.AddWithValue("RotationW", (double)prim.RotationOffset.W);

            // Sit target
            Vector3 sitTargetPos = prim.SitTargetPositionLL;
            cmd.Parameters.AddWithValue("SitTargetOffsetX", (double)sitTargetPos.X);
            cmd.Parameters.AddWithValue("SitTargetOffsetY", (double)sitTargetPos.Y);
            cmd.Parameters.AddWithValue("SitTargetOffsetZ", (double)sitTargetPos.Z);

            Quaternion sitTargetOrient = prim.SitTargetOrientationLL;
            cmd.Parameters.AddWithValue("SitTargetOrientW", (double)sitTargetOrient.W);
            cmd.Parameters.AddWithValue("SitTargetOrientX", (double)sitTargetOrient.X);
            cmd.Parameters.AddWithValue("SitTargetOrientY", (double)sitTargetOrient.Y);
            cmd.Parameters.AddWithValue("SitTargetOrientZ", (double)sitTargetOrient.Z);

            cmd.Parameters.AddWithValue("PayPrice", prim.PayPrice[0]);
            cmd.Parameters.AddWithValue("PayButton1", prim.PayPrice[1]);
            cmd.Parameters.AddWithValue("PayButton2", prim.PayPrice[2]);
            cmd.Parameters.AddWithValue("PayButton3", prim.PayPrice[3]);
            cmd.Parameters.AddWithValue("PayButton4", prim.PayPrice[4]);

            if ((prim.SoundFlags & 1) != 0) // Looped
            {
                cmd.Parameters.AddWithValue("LoopedSound", prim.Sound.ToString());
                cmd.Parameters.AddWithValue("LoopedSoundGain", prim.SoundGain);
            }
            else
            {
                cmd.Parameters.AddWithValue("LoopedSound", UUID.Zero);
                cmd.Parameters.AddWithValue("LoopedSoundGain", 0.0f);
            }

            cmd.Parameters.AddWithValue("TextureAnimation", prim.TextureAnimation);
            cmd.Parameters.AddWithValue("ParticleSystem", prim.ParticleSystem);

            cmd.Parameters.AddWithValue("OmegaX", (double)prim.AngularVelocity.X);
            cmd.Parameters.AddWithValue("OmegaY", (double)prim.AngularVelocity.Y);
            cmd.Parameters.AddWithValue("OmegaZ", (double)prim.AngularVelocity.Z);

            cmd.Parameters.AddWithValue("CameraEyeOffsetX", (double)prim.CameraEyeOffset.X);
            cmd.Parameters.AddWithValue("CameraEyeOffsetY", (double)prim.CameraEyeOffset.Y);
            cmd.Parameters.AddWithValue("CameraEyeOffsetZ", (double)prim.CameraEyeOffset.Z);

            cmd.Parameters.AddWithValue("CameraAtOffsetX", (double)prim.CameraAtOffset.X);
            cmd.Parameters.AddWithValue("CameraAtOffsetY", (double)prim.CameraAtOffset.Y);
            cmd.Parameters.AddWithValue("CameraAtOffsetZ", (double)prim.CameraAtOffset.Z);

            if (prim.ForceMouselook)
                cmd.Parameters.AddWithValue("ForceMouselook", 1);
            else
                cmd.Parameters.AddWithValue("ForceMouselook", 0);

            cmd.Parameters.AddWithValue("ScriptAccessPin", prim.ScriptAccessPin);

            if (prim.AllowedDrop)
                cmd.Parameters.AddWithValue("AllowedDrop", 1);
            else
                cmd.Parameters.AddWithValue("AllowedDrop", 0);

            if (prim.DIE_AT_EDGE)
                cmd.Parameters.AddWithValue("DieAtEdge", 1);
            else
                cmd.Parameters.AddWithValue("DieAtEdge", 0);

            cmd.Parameters.AddWithValue("SalePrice", prim.SalePrice);
            cmd.Parameters.AddWithValue("SaleType", unchecked((sbyte)(prim.ObjectSaleType)));

            byte clickAction = prim.ClickAction;
            cmd.Parameters.AddWithValue("ClickAction", unchecked((sbyte)(clickAction)));

            cmd.Parameters.AddWithValue("Material", unchecked((sbyte)(prim.Material)));

            cmd.Parameters.AddWithValue("CollisionSound", prim.CollisionSound.ToString());
            cmd.Parameters.AddWithValue("CollisionSoundVolume", prim.CollisionSoundVolume);

            cmd.Parameters.AddWithValue("PassTouches", prim.PassTouch);

            cmd.Parameters.AddWithValue("LinkNumber", prim.LinkNum);
            cmd.Parameters.AddWithValue("Generic", prim.GenericData);
			cmd.Parameters.AddWithValue("MediaURL", prim.MediaUrl);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="row"></param>
        /// <param name="taskItem"></param>
        private static void FillItemCommand(MySqlCommand cmd, TaskInventoryItem taskItem)
        {
            cmd.Parameters.AddWithValue("itemID", taskItem.ItemID);
            cmd.Parameters.AddWithValue("primID", taskItem.ParentPartID);
            cmd.Parameters.AddWithValue("assetID", taskItem.AssetID);
            cmd.Parameters.AddWithValue("parentFolderID", taskItem.ParentID);

            cmd.Parameters.AddWithValue("invType", taskItem.InvType);
            cmd.Parameters.AddWithValue("assetType", taskItem.Type);

            cmd.Parameters.AddWithValue("name", taskItem.Name);
            cmd.Parameters.AddWithValue("description", taskItem.Description);
            cmd.Parameters.AddWithValue("creationDate", taskItem.CreationDate);
            cmd.Parameters.AddWithValue("creatorID", taskItem.CreatorID);
            cmd.Parameters.AddWithValue("ownerID", taskItem.OwnerID);
            cmd.Parameters.AddWithValue("lastOwnerID", taskItem.LastOwnerID);
            cmd.Parameters.AddWithValue("groupID", taskItem.GroupID);
            cmd.Parameters.AddWithValue("nextPermissions", taskItem.NextPermissions);
            cmd.Parameters.AddWithValue("currentPermissions", taskItem.CurrentPermissions);
            cmd.Parameters.AddWithValue("basePermissions", taskItem.BasePermissions);
            cmd.Parameters.AddWithValue("everyonePermissions", taskItem.EveryonePermissions);
            cmd.Parameters.AddWithValue("groupPermissions", taskItem.GroupPermissions);
            cmd.Parameters.AddWithValue("flags", taskItem.Flags);
            cmd.Parameters.AddWithValue("salePrice", taskItem.SalePrice);
            cmd.Parameters.AddWithValue("saleType", taskItem.SaleType);
        }

        /// <summary>
        ///
        /// </summary>
        private static void FillRegionSettingsCommand(MySqlCommand cmd, RegionSettings settings)
        {
            cmd.Parameters.AddWithValue("RegionUUID", settings.RegionUUID.ToString());
            cmd.Parameters.AddWithValue("BlockTerraform", settings.BlockTerraform);
            cmd.Parameters.AddWithValue("BlockFly", settings.BlockFly);
            cmd.Parameters.AddWithValue("AllowDamage", settings.AllowDamage);
            cmd.Parameters.AddWithValue("RestrictPushing", settings.RestrictPushing);
            cmd.Parameters.AddWithValue("AllowLandResell", settings.AllowLandResell);
            cmd.Parameters.AddWithValue("AllowLandJoinDivide", settings.AllowLandJoinDivide);
            cmd.Parameters.AddWithValue("BlockShowInSearch", settings.BlockShowInSearch);
            cmd.Parameters.AddWithValue("AgentLimit", settings.AgentLimit);
            cmd.Parameters.AddWithValue("ObjectBonus", settings.ObjectBonus);
            cmd.Parameters.AddWithValue("Maturity", settings.Maturity);
            cmd.Parameters.AddWithValue("DisableScripts", settings.DisableScripts);
            cmd.Parameters.AddWithValue("DisableCollisions", settings.DisableCollisions);
            cmd.Parameters.AddWithValue("DisablePhysics", settings.DisablePhysics);
            cmd.Parameters.AddWithValue("TerrainTexture1", settings.TerrainTexture1.ToString());
            cmd.Parameters.AddWithValue("TerrainTexture2", settings.TerrainTexture2.ToString());
            cmd.Parameters.AddWithValue("TerrainTexture3", settings.TerrainTexture3.ToString());
            cmd.Parameters.AddWithValue("TerrainTexture4", settings.TerrainTexture4.ToString());
            cmd.Parameters.AddWithValue("Elevation1NW", settings.Elevation1NW);
            cmd.Parameters.AddWithValue("Elevation2NW", settings.Elevation2NW);
            cmd.Parameters.AddWithValue("Elevation1NE", settings.Elevation1NE);
            cmd.Parameters.AddWithValue("Elevation2NE", settings.Elevation2NE);
            cmd.Parameters.AddWithValue("Elevation1SE", settings.Elevation1SE);
            cmd.Parameters.AddWithValue("Elevation2SE", settings.Elevation2SE);
            cmd.Parameters.AddWithValue("Elevation1SW", settings.Elevation1SW);
            cmd.Parameters.AddWithValue("Elevation2SW", settings.Elevation2SW);
            cmd.Parameters.AddWithValue("WaterHeight", settings.WaterHeight);
            cmd.Parameters.AddWithValue("TerrainRaiseLimit", settings.TerrainRaiseLimit);
            cmd.Parameters.AddWithValue("TerrainLowerLimit", settings.TerrainLowerLimit);
            cmd.Parameters.AddWithValue("UseEstateSun", settings.UseEstateSun);
            cmd.Parameters.AddWithValue("Sandbox", settings.Sandbox);
            cmd.Parameters.AddWithValue("SunVectorX", settings.SunVector.X);
            cmd.Parameters.AddWithValue("SunVectorY", settings.SunVector.Y);
            cmd.Parameters.AddWithValue("SunVectorZ", settings.SunVector.Z);
            cmd.Parameters.AddWithValue("FixedSun", settings.FixedSun);
            cmd.Parameters.AddWithValue("SunPosition", settings.SunPosition);
            cmd.Parameters.AddWithValue("Covenant", settings.Covenant.ToString());
            cmd.Parameters.AddWithValue("covenantlastupdated", settings.CovenantLastUpdated.ToString());
            cmd.Parameters.AddWithValue("LoadedCreationDateTime", settings.LoadedCreationDateTime);
            cmd.Parameters.AddWithValue("LoadedCreationID", settings.LoadedCreationID);
            cmd.Parameters.AddWithValue("TerrainImageID", settings.TerrainImageID);
            cmd.Parameters.AddWithValue("TerrainMapImageID", settings.TerrainMapImageID);
            cmd.Parameters.AddWithValue("minimum_age", settings.MinimumAge);
            cmd.Parameters.AddWithValue("generic", OSDParser.SerializeJsonString(settings.Generic));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="row"></param>
        /// <param name="land"></param>
        /// <param name="regionUUID"></param>
        private static void FillLandCommand(MySqlCommand cmd, LandData land, UUID regionUUID)
        {
            cmd.Parameters.AddWithValue("UUID", land.GlobalID.ToString());
            cmd.Parameters.AddWithValue("RegionUUID", regionUUID.ToString());
            cmd.Parameters.AddWithValue("LocalLandID", land.LocalID);

            // Bitmap is a byte[512]
            cmd.Parameters.AddWithValue("Bitmap", land.Bitmap);

            cmd.Parameters.AddWithValue("Name", land.Name);
            cmd.Parameters.AddWithValue("Description", land.Description);
            cmd.Parameters.AddWithValue("OwnerUUID", land.OwnerID.ToString());
            cmd.Parameters.AddWithValue("IsGroupOwned", land.IsGroupOwned);
            cmd.Parameters.AddWithValue("Area", land.Area);
            cmd.Parameters.AddWithValue("AuctionID", land.AuctionID); //Unemplemented
            cmd.Parameters.AddWithValue("Category", land.Category); //Enum libsecondlife.Parcel.ParcelCategory
            cmd.Parameters.AddWithValue("ClaimDate", land.ClaimDate);
            cmd.Parameters.AddWithValue("ClaimPrice", land.ClaimPrice);
            cmd.Parameters.AddWithValue("GroupUUID", land.GroupID.ToString());
            cmd.Parameters.AddWithValue("SalePrice", land.SalePrice);
            cmd.Parameters.AddWithValue("LandStatus", land.Status); //Enum. libsecondlife.Parcel.ParcelStatus
            cmd.Parameters.AddWithValue("LandFlags", land.Flags);
            cmd.Parameters.AddWithValue("LandingType", land.LandingType);
            cmd.Parameters.AddWithValue("MediaAutoScale", land.MediaAutoScale);
            cmd.Parameters.AddWithValue("MediaTextureUUID", land.MediaID.ToString());
            cmd.Parameters.AddWithValue("MediaURL", land.MediaURL);
            cmd.Parameters.AddWithValue("MusicURL", land.MusicURL);
            cmd.Parameters.AddWithValue("PassHours", land.PassHours);
            cmd.Parameters.AddWithValue("PassPrice", land.PassPrice);
            cmd.Parameters.AddWithValue("SnapshotUUID", land.SnapshotID.ToString());
            cmd.Parameters.AddWithValue("UserLocationX", land.UserLocation.X);
            cmd.Parameters.AddWithValue("UserLocationY", land.UserLocation.Y);
            cmd.Parameters.AddWithValue("UserLocationZ", land.UserLocation.Z);
            cmd.Parameters.AddWithValue("UserLookAtX", land.UserLookAt.X);
            cmd.Parameters.AddWithValue("UserLookAtY", land.UserLookAt.Y);
            cmd.Parameters.AddWithValue("UserLookAtZ", land.UserLookAt.Z);
            cmd.Parameters.AddWithValue("AuthBuyerID", land.AuthBuyerID);
            cmd.Parameters.AddWithValue("OtherCleanTime", land.OtherCleanTime);
            cmd.Parameters.AddWithValue("MediaDescription", land.MediaDescription);
            cmd.Parameters.AddWithValue("MediaType", land.MediaType);
            cmd.Parameters.AddWithValue("MediaWidth", land.MediaWidth);
            cmd.Parameters.AddWithValue("MediaHeight", land.MediaHeight);
            cmd.Parameters.AddWithValue("MediaLoop", land.MediaLoop);
            cmd.Parameters.AddWithValue("ObscureMusic", land.ObscureMusic);
            cmd.Parameters.AddWithValue("ObscureMedia", land.ObscureMedia);

        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="row"></param>
        /// <param name="entry"></param>
        /// <param name="parcelID"></param>
        private static void FillLandAccessCommand(MySqlCommand cmd, ParcelManager.ParcelAccessEntry entry, UUID parcelID)
        {
            cmd.Parameters.AddWithValue("LandUUID", parcelID.ToString());
            cmd.Parameters.AddWithValue("AccessUUID", entry.AgentID.ToString());
            cmd.Parameters.AddWithValue("Flags", entry.Flags);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="row"></param>
        /// <param name="prim"></param>
        private void FillShapeCommand(MySqlCommand cmd, SceneObjectPart prim)
        {
            PrimitiveBaseShape s = prim.Shape;
            cmd.Parameters.AddWithValue("UUID", prim.UUID.ToString());
            // shape is an enum
            cmd.Parameters.AddWithValue("Shape", 0);
            // vectors
            cmd.Parameters.AddWithValue("ScaleX", (double)s.Scale.X);
            cmd.Parameters.AddWithValue("ScaleY", (double)s.Scale.Y);
            cmd.Parameters.AddWithValue("ScaleZ", (double)s.Scale.Z);
            // paths
            cmd.Parameters.AddWithValue("PCode", s.PCode);
            cmd.Parameters.AddWithValue("PathBegin", s.PathBegin);
            cmd.Parameters.AddWithValue("PathEnd", s.PathEnd);
            cmd.Parameters.AddWithValue("PathScaleX", s.PathScaleX);
            cmd.Parameters.AddWithValue("PathScaleY", s.PathScaleY);
            cmd.Parameters.AddWithValue("PathShearX", s.PathShearX);
            cmd.Parameters.AddWithValue("PathShearY", s.PathShearY);
            cmd.Parameters.AddWithValue("PathSkew", s.PathSkew);
            cmd.Parameters.AddWithValue("PathCurve", s.PathCurve);
            cmd.Parameters.AddWithValue("PathRadiusOffset", s.PathRadiusOffset);
            cmd.Parameters.AddWithValue("PathRevolutions", s.PathRevolutions);
            cmd.Parameters.AddWithValue("PathTaperX", s.PathTaperX);
            cmd.Parameters.AddWithValue("PathTaperY", s.PathTaperY);
            cmd.Parameters.AddWithValue("PathTwist", s.PathTwist);
            cmd.Parameters.AddWithValue("PathTwistBegin", s.PathTwistBegin);
            // profile
            cmd.Parameters.AddWithValue("ProfileBegin", s.ProfileBegin);
            cmd.Parameters.AddWithValue("ProfileEnd", s.ProfileEnd);
            cmd.Parameters.AddWithValue("ProfileCurve", s.ProfileCurve);
            cmd.Parameters.AddWithValue("ProfileHollow", s.ProfileHollow);
            cmd.Parameters.AddWithValue("Texture", s.TextureEntry);
            cmd.Parameters.AddWithValue("ExtraParams", s.ExtraParams);
            cmd.Parameters.AddWithValue("State", s.State);
            cmd.Parameters.AddWithValue("Media", null == s.Media ? null : s.Media.ToXml());
        }

        public void StorePrimInventory(UUID primID, ICollection<TaskInventoryItem> items)
        {
            lock (m_dbLock)
            {
                RemoveItems(primID);

                using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
                {
                    dbcon.Open();

                    MySqlCommand cmd = dbcon.CreateCommand();

                    if (items.Count == 0)
                        return;

                    cmd.CommandText = "insert into primitems (" +
                            "invType, assetType, name, " +
                            "description, creationDate, nextPermissions, " +
                            "currentPermissions, basePermissions, " +
                            "everyonePermissions, groupPermissions, " +
                            "flags, itemID, primID, assetID, " +
                            "parentFolderID, creatorID, ownerID, " +
                            "groupID, lastOwnerID) values (?invType, " +
                            "?assetType, ?name, ?description, " +
                            "?creationDate, ?nextPermissions, " +
                            "?currentPermissions, ?basePermissions, " +
                            "?everyonePermissions, ?groupPermissions, " +
                            "?flags, ?itemID, ?primID, ?assetID, " +
                            "?parentFolderID, ?creatorID, ?ownerID, " +
                            "?groupID, ?lastOwnerID)";

                    foreach (TaskInventoryItem item in items)
                    {
                        cmd.Parameters.Clear();

                        FillItemCommand(cmd, item);

                        ExecuteNonQuery(cmd);
                    }

                    cmd.Dispose();
                }
            }
        }
    }
}
