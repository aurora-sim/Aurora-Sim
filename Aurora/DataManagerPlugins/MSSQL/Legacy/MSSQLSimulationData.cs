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
using System.Data.SqlClient;
using System.Drawing;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Data.MSSQL
{
    /// <summary>
    ///   A MSSQL Interface for the Region Server.
    /// </summary>
    public class MSSQLSimulationData : ILegacySimulationDataStore
    {
        private const string _migrationStore = "RegionStore";

        // private static FileSystemDataStore Instance = new FileSystemDataStore();
        //private static readonly ILog _Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        ///   The database manager
        /// </summary>
        private MSSQLManager _Database;

        private string m_connectionString;

        #region ILegacySimulationDataStore Members

        public string Name
        {
            get { return "MSSQL"; }
        }

        /// <summary>
        ///   Initialises the region datastore
        /// </summary>
        /// <param name = "connectionString">The connection string.</param>
        public void Initialise(string connectionString)
        {
            m_connectionString = connectionString;
            _Database = new MSSQLManager(connectionString);


            //Migration settings
            _Database.CheckMigration(_migrationStore);
        }

        /// <summary>
        ///   Dispose the database
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        ///   Loads the terrain map.
        /// </summary>
        /// <param name = "regionID">regionID.</param>
        /// <returns></returns>
        public short[] LoadTerrain(IScene scene, bool Revert, int RegionSizeX, int RegionSizeY)
        {
            const string sql = "select top 1 RegionUUID, Revision, Heightfield from terrain where RegionUUID = @RegionUUID and Revert = @Revert order by Revision desc";

            using (SqlConnection conn = new SqlConnection(m_connectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                // MySqlParameter param = new MySqlParameter();
                cmd.Parameters.Add(_Database.CreateParameter("@RegionUUID", scene.RegionInfo.RegionID));
                cmd.Parameters.Add(_Database.CreateParameter("@Revert", Revert));
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return (short[]) reader["Heightfield"];
                    }
                    else
                    {
                        return null;
                    }
                    //_Log.Info("[REGION DB]: Loaded terrain revision r" + rev);
                }
            }
        }

        /// <summary>
        ///   Loads all the land objects of a region.
        /// </summary>
        /// <param name = "regionUUID">The region UUID.</param>
        /// <returns></returns>
        public List<LandData> LoadLandObjects(UUID regionUUID)
        {
            List<LandData> LandDataForRegion = new List<LandData>();

            string sql = "select * from land where RegionUUID = @RegionUUID";

            //Retrieve all land data from region
            using (SqlConnection conn = new SqlConnection(m_connectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add(_Database.CreateParameter("@RegionUUID", regionUUID));
                conn.Open();
                using (SqlDataReader readerLandData = cmd.ExecuteReader())
                {
                    while (readerLandData.Read())
                    {
                        LandDataForRegion.Add(BuildLandData(readerLandData));
                    }
                }
            }

            //Retrieve all accesslist data for all landdata
            foreach (LandData LandData in LandDataForRegion)
            {
                sql = "select * from landaccesslist where LandUUID = @LandUUID";
                using (SqlConnection conn = new SqlConnection(m_connectionString))
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add(_Database.CreateParameter("@LandUUID", LandData.GlobalID));
                    conn.Open();
                    using (SqlDataReader readerAccessList = cmd.ExecuteReader())
                    {
                        while (readerAccessList.Read())
                        {
                            LandData.ParcelAccessList.Add(BuildLandAccessData(readerAccessList));
                        }
                    }
                }
            }

            //Return data
            return LandDataForRegion;
        }

        public void RemoveAllLandObjects(UUID regionUUID)
        {
        }

        #endregion

        #region SceneObjectGroup region for loading and Store of the scene.

        /// <summary>
        ///   Loads the objects present in the region.
        /// </summary>
        /// <param name = "regionUUID">The region UUID.</param>
        /// <returns></returns>
        public List<ISceneEntity> LoadObjects(UUID regionUUID, IScene scene)
        {
            UUID lastGroupID = UUID.Zero;

            Dictionary<UUID, SceneObjectPart> prims = new Dictionary<UUID, SceneObjectPart>();
            Dictionary<UUID, ISceneEntity> objects = new Dictionary<UUID, ISceneEntity>();
            SceneObjectGroup grp = null;

            const string sql = "SELECT *, " +
                               "sort = CASE WHEN prims.UUID = prims.SceneGroupID THEN 0 ELSE 1 END " +
                               "FROM prims " +
                               "LEFT JOIN primshapes ON prims.UUID = primshapes.UUID " +
                               "WHERE RegionUUID = @RegionUUID " +
                               "ORDER BY SceneGroupID asc, sort asc, LinkNumber asc";

            using (SqlConnection conn = new SqlConnection(m_connectionString))
            using (SqlCommand command = new SqlCommand(sql, conn))
            {
                command.Parameters.Add(_Database.CreateParameter("@regionUUID", regionUUID));
                conn.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        SceneObjectPart sceneObjectPart = BuildPrim(reader, scene);
                        if (reader["Shape"] is DBNull)
                            sceneObjectPart.Shape = PrimitiveBaseShape.Default;
                        else
                            sceneObjectPart.Shape = BuildShape(reader);

                        prims[sceneObjectPart.UUID] = sceneObjectPart;

                        UUID groupID = new UUID((Guid) reader["SceneGroupID"]);

                        if (groupID != lastGroupID) // New SOG
                        {
                            if (grp != null)
                                objects[grp.UUID] = grp;

                            lastGroupID = groupID;

                            // There sometimes exist OpenSim bugs that 'orphan groups' so that none of the prims are
                            // recorded as the root prim (for which the UUID must equal the persisted group UUID).  In
                            // this case, force the UUID to be the same as the group UUID so that at least these can be
                            // deleted (we need to change the UUID so that any other prims in the linkset can also be 
                            // deleted).
                            if (sceneObjectPart.UUID != groupID && groupID != UUID.Zero)
                            {
                                sceneObjectPart.UUID = groupID;
                            }

                            grp = new SceneObjectGroup(sceneObjectPart, scene);
                        }
                        else
                        {
                            grp.AddChild(sceneObjectPart, sceneObjectPart.LinkNum);
                        }
                    }
                }
            }

            if (grp != null)
                objects[grp.UUID] = grp;

            // Instead of attempting to LoadItems on every prim,
            // most of which probably have no items... get a 
            // list from DB of all prims which have items and
            // LoadItems only on those
            List<SceneObjectPart> primsWithInventory = new List<SceneObjectPart>();
            const string qry = "select distinct primID from primitems";
            using (SqlConnection conn = new SqlConnection(m_connectionString))
            using (SqlCommand command = new SqlCommand(qry, conn))
            {
                conn.Open();
                using (SqlDataReader itemReader = command.ExecuteReader())
                {
                    while (itemReader.Read())
                    {
                        if (!(itemReader["primID"] is DBNull))
                        {
                            UUID primID = new UUID(itemReader["primID"].ToString());
                            if (prims.ContainsKey(primID))
                            {
                                primsWithInventory.Add(prims[primID]);
                            }
                        }
                    }
                }
            }

            LoadItems(primsWithInventory);

            return new List<ISceneEntity>(objects.Values);
        }

        /// <summary>
        ///   Load in the prim's persisted inventory.
        /// </summary>
        /// <param name = "allPrims">all prims with inventory on a region</param>
        private void LoadItems(List<SceneObjectPart> allPrimsWithInventory)
        {
            const string sql = "SELECT * FROM primitems WHERE PrimID = @PrimID";
            using (SqlConnection conn = new SqlConnection(m_connectionString))
            using (SqlCommand command = new SqlCommand(sql, conn))
            {
                conn.Open();
                foreach (SceneObjectPart objectPart in allPrimsWithInventory)
                {
                    command.Parameters.Clear();
                    command.Parameters.Add(_Database.CreateParameter("@PrimID", objectPart.UUID));

                    List<TaskInventoryItem> inventory = new List<TaskInventoryItem>();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            TaskInventoryItem item = BuildItem(reader);

                            item.ParentID = objectPart.UUID; // Values in database are
                            // often wrong
                            inventory.Add(item);
                        }
                    }

                    objectPart.Inventory.RestoreInventoryItems(inventory);
                }
            }
        }

        #endregion

        /// <summary>
        ///   Loads the terrain map.
        /// </summary>
        /// <param name = "regionID">regionID.</param>
        /// <returns></returns>
        public short[] LoadWater(IScene scene, bool Revert, int RegionSizeX, int RegionSizeY)
        {
            const string sql = "select top 1 RegionUUID, Revision, Heightfield from terrain where RegionUUID = @RegionUUID and Revert = @Revert order by Revision desc";

            int r = Revert ? 3 : 2; //Use numbers so that we can coexist with terrain

            using (SqlConnection conn = new SqlConnection(m_connectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                // MySqlParameter param = new MySqlParameter();
                cmd.Parameters.Add(_Database.CreateParameter("@RegionUUID", scene.RegionInfo.RegionID));
                cmd.Parameters.Add(_Database.CreateParameter("@Revert", r));
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return (short[]) reader["Heightfield"];
                    }
                    else
                    {
                        return null;
                    }
                    //_Log.Info("[REGION DB]: Loaded terrain revision r" + rev);
                }
            }
        }

        public void Shutdown()
        {
            //Not used??
        }

        #region Private DataRecord conversion methods

        /// <summary>
        ///   Builds the region settings from a datarecod.
        /// </summary>
        /// <param name = "row">datarecord with regionsettings.</param>
        /// <returns></returns>
        private static RegionSettings BuildRegionSettings(IDataRecord row)
        {
            //TODO change this is some more generic code so we doesnt have to change it every time a new field is added?
            RegionSettings newSettings = new RegionSettings
                                             {
                                                 RegionUUID = new UUID((Guid) row["regionUUID"]),
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
                                                 TerrainTexture1 = new UUID((Guid) row["terrain_texture_1"]),
                                                 TerrainTexture2 = new UUID((Guid) row["terrain_texture_2"]),
                                                 TerrainTexture3 = new UUID((Guid) row["terrain_texture_3"]),
                                                 TerrainTexture4 = new UUID((Guid) row["terrain_texture_4"]),
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
                                                 FixedSun = Convert.ToBoolean(row["fixed_sun"]),
                                                 SunPosition = Convert.ToDouble(row["sun_position"]),
                                                 SunVector = new Vector3(
                                                     Convert.ToSingle(row["sunvectorx"]),
                                                     Convert.ToSingle(row["sunvectory"]),
                                                     Convert.ToSingle(row["sunvectorz"])
                                                     ),
                                                 Covenant = new UUID((Guid) row["covenant"]),
                                                 CovenantLastUpdated = Convert.ToInt32(row["covenantlastupdated"]),
                                                 MinimumAge = Convert.ToInt32(row["minimum_age"]),
                                                 LoadedCreationDateTime =
                                                     Convert.ToInt32(row["loaded_creation_datetime"])
                                             };



            if (row["loaded_creation_id"] is DBNull)
                newSettings.LoadedCreationID = "";
            else
                newSettings.LoadedCreationID = (String) row["loaded_creation_id"];

            OSD o = OSDParser.DeserializeJson((String) row["generic"]);
            if (o.Type == OSDType.Map)
                newSettings.Generic = (OSDMap) o;
            return newSettings;
        }

        /// <summary>
        ///   Builds the land data from a datarecord.
        /// </summary>
        /// <param name = "row">datarecord with land data</param>
        /// <returns></returns>
        private static LandData BuildLandData(IDataRecord row)
        {
            LandData newData = new LandData
                                   {
                                       GlobalID = new UUID((Guid) row["UUID"]),
                                       LocalID = Convert.ToInt32(row["LocalLandID"]),
                                       Bitmap = (Byte[]) row["Bitmap"],
                                       Name = (string) row["Name"],
                                       Description = (string) row["Description"],
                                       OwnerID = new UUID((Guid) row["OwnerUUID"]),
                                       IsGroupOwned = Convert.ToBoolean(row["IsGroupOwned"]),
                                       Area = Convert.ToInt32(row["Area"]),
                                       AuctionID = Convert.ToUInt32(row["AuctionID"]),
                                       Category = (ParcelCategory) Convert.ToInt32(row["Category"]),
                                       ClaimDate = Convert.ToInt32(row["ClaimDate"]),
                                       ClaimPrice = Convert.ToInt32(row["ClaimPrice"]),
                                       GroupID = new UUID((Guid) row["GroupUUID"]),
                                       SalePrice = Convert.ToInt32(row["SalePrice"]),
                                       Status = (ParcelStatus) Convert.ToInt32(row["LandStatus"]),
                                       Flags = Convert.ToUInt32(row["LandFlags"]),
                                       LandingType = Convert.ToByte(row["LandingType"]),
                                       MediaAutoScale = Convert.ToByte(row["MediaAutoScale"]),
                                       MediaID = new UUID((Guid) row["MediaTextureUUID"]),
                                       MediaURL = (string) row["MediaURL"],
                                       MusicURL = (string) row["MusicURL"],
                                       PassHours = Convert.ToSingle(row["PassHours"]),
                                       PassPrice = Convert.ToInt32(row["PassPrice"]),
                                       AuthBuyerID = new UUID((Guid) row["AuthBuyerID"]),
                                       SnapshotID = new UUID((Guid) row["SnapshotUUID"]),
                                       OtherCleanTime = Convert.ToInt32(row["OtherCleanTime"])
                                   };


            // Bitmap is a byte[512]

            //Unemplemented
            //Enum libsecondlife.Parcel.ParcelCategory
            //Enum. libsecondlife.Parcel.ParcelStatus

            //            UUID authedbuyer;
            //            UUID snapshotID;
            //
            //            if (UUID.TryParse((string)row["AuthBuyerID"], out authedbuyer))
            //                newData.AuthBuyerID = authedbuyer;
            //
            //            if (UUID.TryParse((string)row["SnapshotUUID"], out snapshotID))
            //                newData.SnapshotID = snapshotID;


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
            }

            newData.ParcelAccessList = new List<ParcelManager.ParcelAccessEntry>();

            return newData;
        }

        /// <summary>
        ///   Builds the landaccess data from a data record.
        /// </summary>
        /// <param name = "row">datarecord with landaccess data</param>
        /// <returns></returns>
        private static ParcelManager.ParcelAccessEntry BuildLandAccessData(IDataRecord row)
        {
            ParcelManager.ParcelAccessEntry entry = new ParcelManager.ParcelAccessEntry
                                                        {
                                                            AgentID = new UUID((Guid) row["AccessUUID"]),
                                                            Flags = (AccessList) Convert.ToInt32(row["Flags"]),
                                                            Time = new DateTime()
                                                        };
            return entry;
        }

        /// <summary>
        ///   Builds the prim from a datarecord.
        /// </summary>
        /// <param name = "primRow">datarecord</param>
        /// <returns></returns>
        private static SceneObjectPart BuildPrim(IDataRecord primRow, IScene scene)
        {
            SceneObjectPart prim = new SceneObjectPart(scene)
                                       {
                                           UUID = new UUID((Guid) primRow["UUID"]),
                                           CreationDate = Convert.ToInt32(primRow["CreationDate"]),
                                           Name = (string) primRow["Name"],
                                           Text = (string) primRow["Text"],
                                           Color = Color.FromArgb(Convert.ToInt32(primRow["ColorA"]),
                                                                  Convert.ToInt32(primRow["ColorR"]),
                                                                  Convert.ToInt32(primRow["ColorG"]),
                                                                  Convert.ToInt32(primRow["ColorB"])),
                                           Description = (string) primRow["Description"],
                                           SitName = (string) primRow["SitName"],
                                           TouchName = (string) primRow["TouchName"],
                                           Flags = (PrimFlags) Convert.ToUInt32(primRow["ObjectFlags"]),
                                           CreatorID = new UUID((Guid) primRow["CreatorID"]),
                                           OwnerID = new UUID((Guid) primRow["OwnerID"]),
                                           GroupID = new UUID((Guid) primRow["GroupID"]),
                                           LastOwnerID = new UUID((Guid) primRow["LastOwnerID"]),
                                           OwnerMask = Convert.ToUInt32(primRow["OwnerMask"]),
                                           NextOwnerMask = Convert.ToUInt32(primRow["NextOwnerMask"]),
                                           GroupMask = Convert.ToUInt32(primRow["GroupMask"]),
                                           EveryoneMask = Convert.ToUInt32(primRow["EveryoneMask"]),
                                           BaseMask = Convert.ToUInt32(primRow["BaseMask"])
                                       };

            // explicit conversion of integers is required, which sort
            // of sucks.  No idea if there is a shortcut here or not.
            // various text fields
            // permissions
            // vectors
            prim.FixOffsetPosition(new Vector3(
                                       Convert.ToSingle(primRow["PositionX"]),
                                       Convert.ToSingle(primRow["PositionY"]),
                                       Convert.ToSingle(primRow["PositionZ"]))
                                   , true);

            prim.FixGroupPosition(new Vector3(
                                      Convert.ToSingle(primRow["GroupPositionX"]),
                                      Convert.ToSingle(primRow["GroupPositionY"]),
                                      Convert.ToSingle(primRow["GroupPositionZ"]))
                                  , true);

            prim.Velocity = new Vector3(
                Convert.ToSingle(primRow["VelocityX"]),
                Convert.ToSingle(primRow["VelocityY"]),
                Convert.ToSingle(primRow["VelocityZ"]));

            prim.AngularVelocity = new Vector3(
                Convert.ToSingle(primRow["AngularVelocityX"]),
                Convert.ToSingle(primRow["AngularVelocityY"]),
                Convert.ToSingle(primRow["AngularVelocityZ"]));

            prim.Acceleration = new Vector3(
                Convert.ToSingle(primRow["AccelerationX"]),
                Convert.ToSingle(primRow["AccelerationY"]),
                Convert.ToSingle(primRow["AccelerationZ"]));

            // quaternions
            prim.RotationOffset = new Quaternion(
                Convert.ToSingle(primRow["RotationX"]),
                Convert.ToSingle(primRow["RotationY"]),
                Convert.ToSingle(primRow["RotationZ"]),
                Convert.ToSingle(primRow["RotationW"]));

            prim.SitTargetPositionLL = new Vector3(
                Convert.ToSingle(primRow["SitTargetOffsetX"]),
                Convert.ToSingle(primRow["SitTargetOffsetY"]),
                Convert.ToSingle(primRow["SitTargetOffsetZ"]));

            prim.SitTargetOrientationLL = new Quaternion(
                Convert.ToSingle(primRow["SitTargetOrientX"]),
                Convert.ToSingle(primRow["SitTargetOrientY"]),
                Convert.ToSingle(primRow["SitTargetOrientZ"]),
                Convert.ToSingle(primRow["SitTargetOrientW"]));

            prim.PayPrice[0] = Convert.ToInt32(primRow["PayPrice"]);
            prim.PayPrice[1] = Convert.ToInt32(primRow["PayButton1"]);
            prim.PayPrice[2] = Convert.ToInt32(primRow["PayButton2"]);
            prim.PayPrice[3] = Convert.ToInt32(primRow["PayButton3"]);
            prim.PayPrice[4] = Convert.ToInt32(primRow["PayButton4"]);

            prim.Sound = new UUID((Guid) primRow["LoopedSound"]);
            prim.SoundGain = Convert.ToSingle(primRow["LoopedSoundGain"]);
            prim.SoundFlags = 1; // If it's persisted at all, it's looped

            if (!(primRow["TextureAnimation"] is DBNull))
                prim.TextureAnimation = (Byte[]) primRow["TextureAnimation"];
            if (!(primRow["ParticleSystem"] is DBNull))
                prim.ParticleSystem = (Byte[]) primRow["ParticleSystem"];

            prim.AngularVelocity = new Vector3(
                Convert.ToSingle(primRow["OmegaX"]),
                Convert.ToSingle(primRow["OmegaY"]),
                Convert.ToSingle(primRow["OmegaZ"]));

            prim.CameraEyeOffset = new Vector3(
                Convert.ToSingle(primRow["CameraEyeOffsetX"]),
                Convert.ToSingle(primRow["CameraEyeOffsetY"]),
                Convert.ToSingle(primRow["CameraEyeOffsetZ"])
                );

            prim.CameraAtOffset = new Vector3(
                Convert.ToSingle(primRow["CameraAtOffsetX"]),
                Convert.ToSingle(primRow["CameraAtOffsetY"]),
                Convert.ToSingle(primRow["CameraAtOffsetZ"])
                );

            if (Convert.ToInt16(primRow["ForceMouselook"]) != 0)
                prim.ForceMouselook = (true);

            prim.ScriptAccessPin = Convert.ToInt32(primRow["ScriptAccessPin"]);

            if (Convert.ToInt16(primRow["AllowedDrop"]) != 0)
                prim.AllowedDrop = true;

            if (Convert.ToInt16(primRow["DieAtEdge"]) != 0)
                prim.DIE_AT_EDGE = true;

            prim.SalePrice = Convert.ToInt32(primRow["SalePrice"]);
            prim.ObjectSaleType = Convert.ToByte(primRow["SaleType"]);

            prim.Material = Convert.ToByte(primRow["Material"]);

            if (!(primRow["ClickAction"] is DBNull))
                prim.ClickAction = Convert.ToByte(primRow["ClickAction"]);

            prim.CollisionSound = new UUID((Guid) primRow["CollisionSound"]);
            prim.CollisionSoundVolume = Convert.ToSingle(primRow["CollisionSoundVolume"]);
            prim.PassTouch = Convert.ToInt32(primRow["PassTouches"]);
            prim.LinkNum = Convert.ToInt32(primRow["LinkNumber"]);
            prim.GenericData = (string) primRow["Generic"];
            if (!(primRow["MediaURL"] is DBNull))
                prim.MediaUrl = (string) primRow["MediaURL"];

            return prim;
        }

        /// <summary>
        ///   Builds the prim shape from a datarecord.
        /// </summary>
        /// <param name = "shapeRow">The row.</param>
        /// <returns></returns>
        private static PrimitiveBaseShape BuildShape(IDataRecord shapeRow)
        {
            PrimitiveBaseShape baseShape = new PrimitiveBaseShape
                                               {
                                                   Scale = new Vector3(
                                                       Convert.ToSingle(shapeRow["ScaleX"]),
                                                       Convert.ToSingle(shapeRow["ScaleY"]),
                                                       Convert.ToSingle(shapeRow["ScaleZ"])),
                                                   PCode = Convert.ToByte(shapeRow["PCode"]),
                                                   PathBegin = Convert.ToUInt16(shapeRow["PathBegin"]),
                                                   PathEnd = Convert.ToUInt16(shapeRow["PathEnd"]),
                                                   PathScaleX = Convert.ToByte(shapeRow["PathScaleX"]),
                                                   PathScaleY = Convert.ToByte(shapeRow["PathScaleY"]),
                                                   PathShearX = Convert.ToByte(shapeRow["PathShearX"]),
                                                   PathShearY = Convert.ToByte(shapeRow["PathShearY"]),
                                                   PathSkew = Convert.ToSByte(shapeRow["PathSkew"]),
                                                   PathCurve = Convert.ToByte(shapeRow["PathCurve"]),
                                                   PathRadiusOffset = Convert.ToSByte(shapeRow["PathRadiusOffset"]),
                                                   PathRevolutions = Convert.ToByte(shapeRow["PathRevolutions"]),
                                                   PathTaperX = Convert.ToSByte(shapeRow["PathTaperX"]),
                                                   PathTaperY = Convert.ToSByte(shapeRow["PathTaperY"]),
                                                   PathTwist = Convert.ToSByte(shapeRow["PathTwist"]),
                                                   PathTwistBegin = Convert.ToSByte(shapeRow["PathTwistBegin"]),
                                                   ProfileBegin = Convert.ToUInt16(shapeRow["ProfileBegin"]),
                                                   ProfileEnd = Convert.ToUInt16(shapeRow["ProfileEnd"]),
                                                   ProfileCurve = Convert.ToByte(shapeRow["ProfileCurve"]),
                                                   ProfileHollow = Convert.ToUInt16(shapeRow["ProfileHollow"])
                                               };


            // paths
            // profile

            byte[] textureEntry = (byte[]) shapeRow["Texture"];
            baseShape.TextureEntry = textureEntry;

            baseShape.ExtraParams = (byte[]) shapeRow["ExtraParams"];

            try
            {
                baseShape.State = Convert.ToByte(shapeRow["State"]);
            }
            catch (InvalidCastException)
            {
            }

            if (!(shapeRow["Media"] is DBNull))
                baseShape.Media = PrimitiveBaseShape.MediaList.FromXml((string) shapeRow["Media"]);

            return baseShape;
        }

        /// <summary>
        ///   Build a prim inventory item from the persisted data.
        /// </summary>
        /// <param name = "inventoryRow"></param>
        /// <returns></returns>
        private static TaskInventoryItem BuildItem(IDataRecord inventoryRow)
        {
            TaskInventoryItem taskItem = new TaskInventoryItem
                                             {
                                                 ItemID = new UUID((Guid) inventoryRow["itemID"]),
                                                 ParentPartID = new UUID((Guid) inventoryRow["primID"]),
                                                 AssetID = new UUID((Guid) inventoryRow["assetID"]),
                                                 ParentID = new UUID((Guid) inventoryRow["parentFolderID"]),
                                                 InvType = Convert.ToInt32(inventoryRow["invType"]),
                                                 Type = Convert.ToInt32(inventoryRow["assetType"]),
                                                 Name = (string) inventoryRow["name"],
                                                 Description = (string) inventoryRow["description"],
                                                 CreationDate = Convert.ToUInt32(inventoryRow["creationDate"]),
                                                 CreatorIdentification = inventoryRow["creatorID"].ToString(),
                                                 OwnerID = new UUID((Guid) inventoryRow["ownerID"]),
                                                 LastOwnerID = new UUID((Guid) inventoryRow["lastOwnerID"]),
                                                 GroupID = new UUID((Guid) inventoryRow["groupID"]),
                                                 NextPermissions = Convert.ToUInt32(inventoryRow["nextPermissions"]),
                                                 CurrentPermissions =
                                                     Convert.ToUInt32(inventoryRow["currentPermissions"]),
                                                 BasePermissions = Convert.ToUInt32(inventoryRow["basePermissions"]),
                                                 EveryonePermissions =
                                                     Convert.ToUInt32(inventoryRow["everyonePermissions"]),
                                                 GroupPermissions = Convert.ToUInt32(inventoryRow["groupPermissions"]),
                                                 Flags = Convert.ToUInt32(inventoryRow["flags"]),
                                                 SalePrice = Convert.ToInt32(inventoryRow["salePrice"]),
                                                 SaleType = Convert.ToByte(inventoryRow["saleType"])
                                             };





            return taskItem;
        }

        #endregion
    }
}