/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyrightD
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
using System.Text;

using Nini.Config;

using OpenMetaverse;
using Aurora.Framework.Utilities;
using Aurora.Framework.Physics;
using Aurora.Framework.SceneInfo;

namespace OpenSim.Region.Physics.BulletSPlugin
{

// The physical implementation of the terrain is wrapped in this class.
public abstract class BSTerrainPhys : IDisposable
{
    public enum TerrainImplementation
    {
        Heightmap   = 0,
        Mesh        = 1
    }

    public BSScene PhysicsScene { get; private set; }
    // Base of the region in world coordinates. Coordinates inside the region are relative to this.
    public Vector3 TerrainBase { get; private set; }
    public uint ID { get; private set; }

    public BSTerrainPhys(BSScene physicsScene, Vector3 regionBase, uint id)
    {
        PhysicsScene = physicsScene;
        TerrainBase = regionBase;
        ID = id;
    }
    public abstract void Dispose();
    public abstract float GetTerrainHeightAtXYZ(Vector3 pos);
    public abstract float GetWaterLevelAtXYZ(Vector3 pos);
}

// ==========================================================================================
public sealed class BSTerrainManager : IDisposable
{
    static string LogHeader = "[BULLETSIM TERRAIN MANAGER]";

    // These height values are fractional so the odd values will be
    //     noticable when debugging.
    public const float HEIGHT_INITIALIZATION = 24.987f;
    public const float HEIGHT_INITIAL_LASTHEIGHT = 24.876f;
    public const float HEIGHT_GETHEIGHT_RET = 24.765f;
    public const float WATER_HEIGHT_GETHEIGHT_RET = 19.998f;

    // If the min and max height are equal, we reduce the min by this
    //    amount to make sure that a bounding box is built for the terrain.
    public const float HEIGHT_EQUAL_FUDGE = 0.2f;

    // The scene that I am part of
    private BSScene PhysicsScene { get; set; }

    // The ground plane created to keep thing from falling to infinity.
    private BulletBody m_groundPlane;

    // If doing mega-regions, if we're region zero we will be managing multiple
    //    region terrains since region zero does the physics for the whole mega-region.
    private BSTerrainPhys m_terrain;

    // Flags used to know when to recalculate the height.
    private bool m_terrainModified = false;

    // If we are doing mega-regions, terrains are added from TERRAIN_ID to m_terrainCount.
    // This is incremented before assigning to new region so it is the last ID allocated.
    private uint m_terrainCount = BSScene.CHILDTERRAIN_ID - 1;
    public uint HighestTerrainID { get {return m_terrainCount; } }

    // If the parent region (region 0), this is the extent of the combined regions
    //     relative to the origin of region zero
    private Vector3 m_worldMax;
    public Vector3 WorldMax { get { return m_worldMax; } }

    public BSTerrainManager(BSScene physicsScene)
    {
        PhysicsScene = physicsScene;

        // Assume one region of default size
        m_worldMax = new Vector3(physicsScene.Scene.RegionInfo.RegionSizeX, 
            physicsScene.Scene.RegionInfo.RegionSizeY, physicsScene.Scene.RegionInfo.RegionSizeZ);
    }

    public void Dispose()
    {
        ReleaseGroundPlaneAndTerrain();
    }

    // Create the initial instance of terrain and the underlying ground plane.
    // This is called from the initialization routine so we presume it is
    //    safe to call Bullet in real time. We hope no one is moving prims around yet.
    public void CreateInitialGroundPlaneAndTerrain()
    {
        DetailLog("{0},BSTerrainManager.CreateInitialGroundPlaneAndTerrain,region={1}", BSScene.DetailLogZero, PhysicsScene.RegionName);
        // The ground plane is here to catch things that are trying to drop to negative infinity
        BulletShape groundPlaneShape = PhysicsScene.PE.CreateGroundPlaneShape(BSScene.GROUNDPLANE_ID, 1f, BSParam.TerrainCollisionMargin);
        m_groundPlane = PhysicsScene.PE.CreateBodyWithDefaultMotionState(groundPlaneShape, 
                                        BSScene.GROUNDPLANE_ID, Vector3.Zero, Quaternion.Identity);

        PhysicsScene.PE.AddObjectToWorld(PhysicsScene.World, m_groundPlane);
        PhysicsScene.PE.UpdateSingleAabb(PhysicsScene.World, m_groundPlane);
        // Ground plane does not move
        PhysicsScene.PE.ForceActivationState(m_groundPlane, ActivationState.DISABLE_SIMULATION);
        // Everything collides with the ground plane.
        m_groundPlane.collisionType = CollisionType.Groundplane;
        m_groundPlane.ApplyCollisionMask(PhysicsScene);

        m_terrain = new BSTerrainHeightmap(PhysicsScene, Vector3.Zero, BSScene.TERRAIN_ID, m_worldMax);
    }

    // Release all the terrain structures we might have allocated
    public void ReleaseGroundPlaneAndTerrain()
    {
        DetailLog("{0},BSTerrainManager.ReleaseGroundPlaneAndTerrain,region={1}", BSScene.DetailLogZero, PhysicsScene.RegionName);
        if (m_groundPlane.HasPhysicalBody)
        {
            if (PhysicsScene.PE.RemoveObjectFromWorld(PhysicsScene.World, m_groundPlane))
            {
                PhysicsScene.PE.DestroyObject(PhysicsScene.World, m_groundPlane);
            }
            m_groundPlane.Clear();
        }

        ReleaseTerrain();
    }

    // Release all the terrain we have allocated
    public void ReleaseTerrain()
    {
        m_terrain.Dispose();
    }

    // The simulator wants to set a new heightmap for the terrain.
    public void SetTerrain(float[] heightMap) {
        float[] localHeightMap = heightMap;
        // If there are multiple requests for changes to the same terrain between ticks,
        //      only do that last one.
        PhysicsScene.PostTaintObject("TerrainManager.SetTerrain", 0, delegate()
        {
            UpdateTerrain(BSScene.TERRAIN_ID, localHeightMap);
        });
    }

    // If called for terrain has has not been previously allocated, a new terrain will be built
    //     based on the passed information. The 'id' should be either the terrain id or
    //     BSScene.CHILDTERRAIN_ID. If the latter, a new child terrain ID will be allocated and used.
    //     The latter feature is for creating child terrains for mega-regions.
    // If there is an existing terrain body, a new
    //     terrain shape is created and added to the body.
    //     This call is most often used to update the heightMap and parameters of the terrain.
    // (The above does suggest that some simplification/refactoring is in order.)
    // Called during taint-time.
    private void UpdateTerrain(uint id, float[] heightMap)
    {
        DetailLog("{0},BSTerrainManager.UpdateTerrain,call,id={1}",
                            BSScene.DetailLogZero, id);

        if (m_terrain != null)
        {
            // There is already a terrain in this spot. Free the old and build the new.
            DetailLog("{0},BSTErrainManager.UpdateTerrain:UpdateExisting,call,id={1}",
                            BSScene.DetailLogZero, id);

            // Release any physical memory it may be using.
            m_terrain.Dispose();

            m_terrain = BuildPhysicalTerrain(id, heightMap);

            m_terrainModified = true;
        }
        else
        {
            // We don't know about this terrain so either we are creating a new terrain or
            //    our mega-prim child is giving us a new terrain to add to the phys world

            // if this is a child terrain, calculate a unique terrain id
            uint newTerrainID = id;
            if (newTerrainID >= BSScene.CHILDTERRAIN_ID)
                newTerrainID = ++m_terrainCount;

            DetailLog("{0},BSTerrainManager.UpdateTerrain:NewTerrain,taint,newID={1}",
                                        BSScene.DetailLogZero, newTerrainID);
            m_terrain = BuildPhysicalTerrain(id, heightMap);
           
            m_terrainModified = true;
        }
    }

    // TODO: redo terrain implementation selection to allow other base types than heightMap.
    private BSTerrainPhys BuildPhysicalTerrain(uint id, float[] heightMap)
    {
        // Find high and low points of passed heightmap.
        // The min and max passed in is usually the area objects can be in (maximum
        //     object height, for instance). The terrain wants the bounding box for the
        //     terrain so replace passed min and max Z with the actual terrain min/max Z.
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;
        foreach (float height in heightMap)
        {
            if (height < minZ) minZ = height;
            if (height > maxZ) maxZ = height;
        }
        if (minZ == maxZ)
        {
            // If min and max are the same, reduce min a little bit so a good bounding box is created.
            minZ -= BSTerrainManager.HEIGHT_EQUAL_FUDGE;
        }
        Vector3 minCoords = new Vector3(0, 0, minZ);
        Vector3 maxCoords = new Vector3(PhysicsScene.Scene.RegionInfo.RegionSizeX, PhysicsScene.Scene.RegionInfo.RegionSizeY, maxZ);
        
        PhysicsScene.Logger.DebugFormat("{0} Terrain for {1}/ created with {2}", 
                                            LogHeader, PhysicsScene.RegionName, 
                                            (BSTerrainPhys.TerrainImplementation)BSParam.TerrainImplementation);
        BSTerrainPhys newTerrainPhys = null;
        switch ((int)BSParam.TerrainImplementation)
        {
            case (int)BSTerrainPhys.TerrainImplementation.Heightmap:
                newTerrainPhys = new BSTerrainHeightmap(PhysicsScene, Vector3.Zero, id,
                                            heightMap, minCoords, maxCoords);
                break;
            case (int)BSTerrainPhys.TerrainImplementation.Mesh:
                newTerrainPhys = new BSTerrainMesh(PhysicsScene, Vector3.Zero, id,
                                            heightMap, minCoords, maxCoords);
                break;
            default:
                PhysicsScene.Logger.ErrorFormat("{0} Bad terrain implementation specified. Type={1}/{2},Region={3}",
                                            LogHeader, 
                                            (int)BSParam.TerrainImplementation, 
                                            BSParam.TerrainImplementation,
                                            PhysicsScene.RegionName);
                break;
        }
        return newTerrainPhys;
    }

    // Return 'true' of this position is somewhere in known physical terrain space
    public bool IsWithinKnownTerrain(Vector3 pos)
    {
        return !(pos.X < 0f || pos.Y < 0f || pos.X >= m_worldMax.X || pos.Y >= m_worldMax.Y);
    }

    // Return a new position that is over known terrain if the position is outside our terrain.
    public Vector3 ClampPositionIntoKnownTerrain(Vector3 pPos)
    {
        Vector3 ret = pPos;

        // First, base addresses are never negative so correct for that possible problem.
        if (ret.X < 0f || ret.Y < 0f)
        {
            ret.X = Util.Clamp<float>(ret.X, 0f, 1000000f);
            ret.Y = Util.Clamp<float>(ret.Y, 0f, 1000000f);
            DetailLog("{0},BSTerrainManager.ClampPositionToKnownTerrain,zeroingNegXorY,oldPos={1},newPos={2}",
                                        BSScene.DetailLogZero, pPos, ret);
        }
        if (ret.X >= m_worldMax.X || ret.Y >= m_worldMax.Y)
        {
            ret.X = Util.Clamp<float>(ret.X, ret.X, m_worldMax.X);
            ret.Y = Util.Clamp<float>(ret.Y, ret.X, m_worldMax.Y);
            DetailLog("{0},BSTerrainManager.ClampPositionToKnownTerrain,maxingPosXorY,oldPos={1},newPos={2}",
                                        BSScene.DetailLogZero, pPos, ret);
        }

        return ret;
    }

    // Given an X and Y, find the height of the terrain.
    // Since we could be handling multiple terrains for a mega-region,
    //    the base of the region is calcuated assuming all regions are
    //    the same size and that is the default.
    // Once the heightMapInfo is found, we have all the information to
    //    compute the offset into the array.
    private float lastHeightTX = 999999f;
    private float lastHeightTY = 999999f;
    private float lastHeight = HEIGHT_INITIAL_LASTHEIGHT;
    public float GetTerrainHeightAtXYZ(Vector3 pos)
    {
        float tX = pos.X;
        float tY = pos.Y;
        // You'd be surprized at the number of times this routine is called
        //    with the same parameters as last time.
        if (!m_terrainModified && (lastHeightTX == tX) && (lastHeightTY == tY))
            return lastHeight;
        m_terrainModified = false;

        lastHeightTX = tX;
        lastHeightTY = tY;
        return (lastHeight = m_terrain.GetTerrainHeightAtXYZ(pos));
    }

    public float GetWaterLevelAtXYZ(Vector3 pos)
    {
        return m_terrain.GetWaterLevelAtXYZ(pos);
    }

    private void DetailLog(string msg, params Object[] args)
    {
        //PhysicsScene.PhysicsLogging.TraceFormat(msg, args);
    }
}
}
