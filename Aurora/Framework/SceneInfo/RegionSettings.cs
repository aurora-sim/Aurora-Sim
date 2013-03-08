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

using System;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
    public class RegionSettings
    {
        #region Delegates

        public delegate void SaveDelegate(RegionSettings rs);

        #endregion

        /// <value>
        ///   These appear to be terrain textures that are shipped with the client.
        /// </value>
        public static readonly UUID DEFAULT_TERRAIN_TEXTURE_1 = new UUID("b8d3965a-ad78-bf43-699b-bff8eca6c975");

        public static readonly UUID DEFAULT_TERRAIN_TEXTURE_2 = new UUID("abb783e6-3e93-26c0-248a-247666855da3");
        public static readonly UUID DEFAULT_TERRAIN_TEXTURE_3 = new UUID("179cdabd-398a-9b6b-1391-4dc333ba321f");
        public static readonly UUID DEFAULT_TERRAIN_TEXTURE_4 = new UUID("beb169c7-11ea-fff2-efe5-0f24dc881df2");
        private int m_AgentLimit = 40;
        private bool m_AllowLandJoinDivide = true;
        private bool m_AllowLandResell = true;
        private UUID m_Covenant = UUID.Zero;
        private double m_Elevation1NE = 10;
        private double m_Elevation1NW = 10;
        private double m_Elevation1SE = 10;
        private double m_Elevation1SW = 10;
        private double m_Elevation2NE = 60;
        private double m_Elevation2NW = 60;
        private double m_Elevation2SE = 60;
        private double m_Elevation2SW = 60;
        private OSDMap m_Generic = new OSDMap();
        private String m_LoadedCreationID = String.Empty;
        private double m_ObjectBonus = 1.0;
        private UUID m_PaintTerrainTexture = UUID.Zero;

        private UUID m_RegionUUID = UUID.Zero;
        private double m_TerrainLowerLimit = -100;
        private double m_TerrainRaiseLimit = 100;
        private UUID m_TerrainTexture1 = UUID.Zero;
        private UUID m_TerrainTexture2 = UUID.Zero;
        private UUID m_TerrainTexture3 = UUID.Zero;
        private UUID m_TerrainTexture4 = UUID.Zero;
        private bool m_UseEstateSun = true;
        private double m_WaterHeight = 20;

        public bool UsePaintableTerrain
        {
            get { return false; }
        }

        public UUID RegionUUID
        {
            get { return m_RegionUUID; }
            set { m_RegionUUID = value; }
        }

        public bool BlockTerraform { get; set; }

        public bool BlockFly { get; set; }

        public bool AllowDamage { get; set; }

        public bool RestrictPushing { get; set; }

        public bool AllowLandResell
        {
            get { return m_AllowLandResell; }
            set { m_AllowLandResell = value; }
        }

        public bool AllowLandJoinDivide
        {
            get { return m_AllowLandJoinDivide; }
            set { m_AllowLandJoinDivide = value; }
        }

        public bool BlockShowInSearch { get; set; }

        public int AgentLimit
        {
            get { return m_AgentLimit; }
            set { m_AgentLimit = value; }
        }

        public double ObjectBonus
        {
            get { return m_ObjectBonus; }
            set { m_ObjectBonus = value; }
        }

        public int Maturity { get; set; }

        public bool DisableScripts { get; set; }

        public bool DisableCollisions { get; set; }

        public bool DisablePhysics { get; set; }

        public int MinimumAge { get; set; }

        public UUID PaintableTerrainTexture
        {
            get
            {
                if (m_PaintTerrainTexture == UUID.Zero)
                    m_PaintTerrainTexture = UUID.Random();
                return m_PaintTerrainTexture;
            }
            set {
                m_PaintTerrainTexture = value == UUID.Zero ? UUID.Random() : value;
            }
        }

        public UUID TerrainTexture1
        {
            get { return m_TerrainTexture1; }
            set {
                m_TerrainTexture1 = value == UUID.Zero ? DEFAULT_TERRAIN_TEXTURE_1 : value;
            }
        }

        public UUID TerrainTexture2
        {
            get { return m_TerrainTexture2; }
            set {
                m_TerrainTexture2 = value == UUID.Zero ? DEFAULT_TERRAIN_TEXTURE_2 : value;
            }
        }

        public UUID TerrainTexture3
        {
            get { return m_TerrainTexture3; }
            set {
                m_TerrainTexture3 = value == UUID.Zero ? DEFAULT_TERRAIN_TEXTURE_3 : value;
            }
        }

        public UUID TerrainTexture4
        {
            get { return m_TerrainTexture4; }
            set {
                m_TerrainTexture4 = value == UUID.Zero ? DEFAULT_TERRAIN_TEXTURE_4 : value;
            }
        }

        public double Elevation1NW
        {
            get { return m_Elevation1NW; }
            set { m_Elevation1NW = value; }
        }

        public double Elevation2NW
        {
            get { return m_Elevation2NW; }
            set { m_Elevation2NW = value; }
        }

        public double Elevation1NE
        {
            get { return m_Elevation1NE; }
            set { m_Elevation1NE = value; }
        }

        public double Elevation2NE
        {
            get { return m_Elevation2NE; }
            set { m_Elevation2NE = value; }
        }

        public double Elevation1SE
        {
            get { return m_Elevation1SE; }
            set { m_Elevation1SE = value; }
        }

        public double Elevation2SE
        {
            get { return m_Elevation2SE; }
            set { m_Elevation2SE = value; }
        }

        public double Elevation1SW
        {
            get { return m_Elevation1SW; }
            set { m_Elevation1SW = value; }
        }

        public double Elevation2SW
        {
            get { return m_Elevation2SW; }
            set { m_Elevation2SW = value; }
        }

        public double WaterHeight
        {
            get { return m_WaterHeight; }
            set { m_WaterHeight = value; }
        }

        public double TerrainRaiseLimit
        {
            get { return m_TerrainRaiseLimit; }
            set { m_TerrainRaiseLimit = value; }
        }

        public double TerrainLowerLimit
        {
            get { return m_TerrainLowerLimit; }
            set { m_TerrainLowerLimit = value; }
        }

        public bool UseEstateSun
        {
            get { return m_UseEstateSun; }
            set { m_UseEstateSun = value; }
        }

        public bool Sandbox { get; set; }

        public Vector3 SunVector { get; set; }

        /// <summary>
        ///   Terrain (and probably) prims asset ID for the map
        /// </summary>
        public UUID TerrainImageID { get; set; }

        /// <summary>
        /// Displays which lands are for sale (and for auction)
        /// </summary>
        public UUID ParcelMapImageID { get; set; }

        /// <summary>
        ///   Terrain only asset ID for the map
        /// </summary>
        public UUID TerrainMapImageID { get; set; }

        /// <summary>
        ///   Time that the map tile was last created
        /// </summary>
        public DateTime TerrainMapLastRegenerated { get; set; }

        public bool FixedSun { get; set; }

        public double SunPosition { get; set; }

        public UUID Covenant
        {
            get { return m_Covenant; }
            set { m_Covenant = value; }
        }

        public int CovenantLastUpdated { get; set; }

        public OSDMap Generic
        {
            get { return m_Generic; }
            set { m_Generic = value; }
        }

        public int LoadedCreationDateTime { get; set; }

        public String LoadedCreationDate
        {
            get
            {
                TimeSpan ts = new TimeSpan(0, 0, LoadedCreationDateTime);
                DateTime stamp = new DateTime(1970, 1, 1) + ts;
                return stamp.ToLongDateString();
            }
        }

        public String LoadedCreationTime
        {
            get
            {
                TimeSpan ts = new TimeSpan(0, 0, LoadedCreationDateTime);
                DateTime stamp = new DateTime(1970, 1, 1) + ts;
                return stamp.ToLongTimeString();
            }
        }

        public String LoadedCreationID
        {
            get { return m_LoadedCreationID; }
            set { m_LoadedCreationID = value; }
        }

        public void AddGeneric(string key, OSD value)
        {
            m_Generic[key] = value;
        }

        public void RemoveGeneric(string key)
        {
            if (m_Generic.ContainsKey(key))
                m_Generic.Remove(key);
        }

        public OSD GetGeneric(string key)
        {
            OSD value;
            m_Generic.TryGetValue(key, out value);
            return value;
        }

        public OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();

            map["AgentLimit"] = this.AgentLimit;
            map["AllowDamage"] = this.AllowDamage;
            map["AllowLandJoinDivide"] = this.AllowLandJoinDivide;
            map["AllowLandResell"] = this.AllowLandResell;
            map["BlockFly"] = this.BlockFly;
            map["BlockShowInSearch"] = this.BlockShowInSearch;
            map["BlockTerraform"] = this.BlockTerraform;
            map["Covenant"] = this.Covenant;
            map["CovenantLastUpdated"] = this.CovenantLastUpdated;
            map["DisableCollisions"] = this.DisableCollisions;
            map["DisablePhysics"] = this.DisablePhysics;
            map["DisableScripts"] = this.DisableScripts;
            map["Elevation1NE"] = this.Elevation1NE;
            map["Elevation1NW"] = this.Elevation1NW;
            map["Elevation1SE"] = this.Elevation1SE;
            map["Elevation1SW"] = this.Elevation1SW;
            map["Elevation2NE"] = this.Elevation2NE;
            map["Elevation2NW"] = this.Elevation2NW;
            map["Elevation2SE"] = this.Elevation2SE;
            map["Elevation2SW"] = this.Elevation2SW;
            map["FixedSun"] = this.FixedSun;
            map["Generic"] = this.Generic;
            map["LoadedCreationDateTime"] = this.LoadedCreationDateTime;
            map["LoadedCreationID"] = this.LoadedCreationID;
            map["Maturity"] = this.Maturity;
            map["MinimumAge"] = this.MinimumAge;
            map["ObjectBonus"] = this.ObjectBonus;
            map["RegionUUID"] = this.RegionUUID;
            map["RestrictPushing"] = this.RestrictPushing;
            map["Sandbox"] = this.Sandbox;
            map["SunPosition"] = this.SunPosition;
            map["SunVector"] = this.SunVector;
            map["TerrainImageID"] = this.TerrainImageID;
            map["ParcelMapImageID"] = this.ParcelMapImageID;
            map["TerrainLowerLimit"] = this.TerrainLowerLimit;
            map["TerrainMapImageID"] = this.TerrainMapImageID;
            map["TerrainMapLastRegenerated"] = this.TerrainMapLastRegenerated;
            map["TerrainRaiseLimit"] = this.TerrainRaiseLimit;
            map["TerrainTexture1"] = this.TerrainTexture1;
            map["TerrainTexture2"] = this.TerrainTexture2;
            map["TerrainTexture3"] = this.TerrainTexture3;
            map["TerrainTexture4"] = this.TerrainTexture4;
            map["PaintableTerrainTexture"] = this.PaintableTerrainTexture;
            map["UseEstateSun"] = this.UseEstateSun;
            map["WaterHeight"] = this.WaterHeight;

            return map;
        }

        public void FromOSD(OSDMap map)
        {
            this.AgentLimit = map["AgentLimit"];
            this.AllowLandJoinDivide = map["AllowLandJoinDivide"];
            this.AllowLandResell = map["AllowLandResell"];
            this.BlockFly = map["BlockFly"];
            this.BlockShowInSearch = map["BlockShowInSearch"];
            this.BlockTerraform = map["BlockTerraform"];
            this.Covenant = map["Covenant"];
            this.CovenantLastUpdated = map["CovenantLastUpdated"];
            this.DisableCollisions = map["DisableCollisions"];
            this.DisablePhysics = map["DisablePhysics"];
            this.DisableScripts = map["DisableScripts"];
            this.Elevation1NE = map["Elevation1NE"];
            this.Elevation1NW = map["Elevation1NW"];
            this.Elevation1SE = map["Elevation1SE"];
            this.Elevation1SW = map["Elevation1SW"];
            this.Elevation2NE = map["Elevation2NE"];
            this.Elevation2NW = map["Elevation2NW"];
            this.Elevation2SE = map["Elevation2SE"];
            this.Elevation2SW = map["Elevation2SW"];
            this.FixedSun = map["FixedSun"];
            this.Generic = (OSDMap) map["Generic"];
            this.LoadedCreationDateTime = map["LoadedCreationDateTime"];
            this.LoadedCreationID = map["LoadedCreationID"];
            this.Maturity = map["Maturity"];
            this.MinimumAge = map["MinimumAge"];
            this.ObjectBonus = map["ObjectBonus"];
            this.RegionUUID = map["RegionUUID"];
            this.RestrictPushing = map["RestrictPushing"];
            this.Sandbox = map["Sandbox"];
            this.SunPosition = map["SunPosition"];
            this.SunVector = map["SunVector"];
            this.TerrainImageID = map["TerrainImageID"];
            this.TerrainMapImageID = map["TerrainMapImageID"];
            this.TerrainMapLastRegenerated = map["TerrainMapLastRegenerated"];
            this.ParcelMapImageID = map["ParcelMapImageID"];
            this.TerrainLowerLimit = map["TerrainLowerLimit"];
            this.TerrainRaiseLimit = map["TerrainRaiseLimit"];
            this.TerrainTexture1 = map["TerrainTexture1"];
            this.TerrainTexture2 = map["TerrainTexture2"];
            this.TerrainTexture3 = map["TerrainTexture3"];
            this.TerrainTexture4 = map["TerrainTexture4"];
            this.PaintableTerrainTexture = map["PaintableTerrainTexture"];
            this.UseEstateSun = map["UseEstateSun"];
            this.WaterHeight = map["WaterHeight"];
        }
    }
}