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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Text;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Rendering;
using OpenMetaverse.StructuredData;
using Aurora.Modules.WorldMap.Warp3DMap;
using Rednettle.Warp3D;
using RegionSettings = Aurora.Framework.RegionSettings;
using WarpRenderer = Warp3D.Warp3D;

namespace Aurora.Modules.WorldMap
{
    public class WarpTileRenderer : IMapTileTerrainRenderer
    {
        private static readonly UUID TEXTURE_METADATA_MAGIC = new UUID("802dc0e0-f080-4931-8b57-d1be8611c4f3");
        private static readonly Color4 WATER_COLOR = new Color4(29, 72, 96, 216);
        private readonly Dictionary<UUID, Color4> m_colors = new Dictionary<UUID, Color4>();
        private IConfigSource m_config;
        private IRendering m_primMesher;
        private IScene m_scene;
        private bool m_texturePrims;
        private bool m_useAntiAliasing = false; // TODO: Make this a config option

        #region IMapTileTerrainRenderer Members

        public void Initialise(IScene scene, IConfigSource config)
        {
            m_scene = scene;
            m_config = config;
        }

        public Bitmap TerrainToBitmap(Bitmap mapbmp)
        {
            List<string> renderers = RenderingLoader.ListRenderers(Util.ExecutingDirectory());
            if (renderers.Count > 0)
            {
                m_primMesher = RenderingLoader.LoadRenderer(renderers[0]);
                MainConsole.Instance.Debug("[MAPTILE]: Loaded prim mesher " + m_primMesher);
            }
            else
            {
                MainConsole.Instance.Info("[MAPTILE]: No prim mesher loaded, prim rendering will be disabled");
            }

            bool drawPrimVolume = true;
            bool textureTerrain = true;

            try
            {
                IConfig startupConfig = m_config.Configs["Startup"];
                drawPrimVolume = startupConfig.GetBoolean("DrawPrimOnMapTile", drawPrimVolume);
                textureTerrain = startupConfig.GetBoolean("TextureOnMapTile", textureTerrain);
            }
            catch
            {
                MainConsole.Instance.Warn("[MAPTILE]: Failed to load StartupConfig");
            }

            m_texturePrims = m_config.Configs["MapModule"].GetBoolean("WarpTexturePrims", false);
            m_colors.Clear();

            int scaledRemovalFactor = m_scene.RegionInfo.RegionSizeX/(Constants.RegionSize/2);
            Vector3 camPos = new Vector3(m_scene.RegionInfo.RegionSizeX/2 - 0.5f,
                                         m_scene.RegionInfo.RegionSizeY/2 - 0.5f, 221.7025033688163f);
            Viewport viewport = new Viewport(camPos, -Vector3.UnitZ, 1024f, 0.1f,
                                             m_scene.RegionInfo.RegionSizeX - scaledRemovalFactor,
                                             m_scene.RegionInfo.RegionSizeY - scaledRemovalFactor,
                                             m_scene.RegionInfo.RegionSizeX - scaledRemovalFactor,
                                             m_scene.RegionInfo.RegionSizeY - scaledRemovalFactor);

            int width = viewport.Width;
            int height = viewport.Height;

            if (m_useAntiAliasing)
            {
                width *= 2;
                height *= 2;
            }

            WarpRenderer renderer = new WarpRenderer();
            warp_Object terrainObj = null;
            renderer.CreateScene(width, height);
            renderer.Scene.autoCalcNormals = false;

            #region Camera

            warp_Vector pos = ConvertVector(viewport.Position);
            pos.z -= 0.001f; // Works around an issue with the Warp3D camera
            warp_Vector lookat = warp_Vector.add(ConvertVector(viewport.Position), ConvertVector(viewport.LookDirection));

            renderer.Scene.defaultCamera.setPos(pos);
            renderer.Scene.defaultCamera.lookAt(lookat);

            if (viewport.Orthographic)
            {
                renderer.Scene.defaultCamera.isOrthographic = true;
                renderer.Scene.defaultCamera.orthoViewWidth = viewport.OrthoWindowWidth;
                renderer.Scene.defaultCamera.orthoViewHeight = viewport.OrthoWindowHeight;
            }
            else
            {
                viewport.Orthographic = false;
                float fov = 256;
                //fov *= 1.75f; // FIXME: ???
                renderer.Scene.defaultCamera.setFov(fov);
            }

            #endregion Camera

            renderer.Scene.addLight("Light1", new warp_Light(new warp_Vector(1.0f, 0.5f, 1f), 0xffffff, 0, 320, 40));
            renderer.Scene.addLight("Light2", new warp_Light(new warp_Vector(-1f, -1f, 1f), 0xffffff, 0, 100, 40));

            try
            {
                CreateWater(renderer);
                terrainObj = CreateTerrain(renderer, textureTerrain);
                if (drawPrimVolume && m_primMesher != null)
                {
#if (!ISWIN)
                    foreach (ISceneEntity ent in m_scene.Entities.GetEntities())
                        foreach (ISceneChildEntity part in ent.ChildrenEntities())
                            CreatePrim(renderer, part);
#else
                    foreach (ISceneChildEntity part in m_scene.Entities.GetEntities().SelectMany(ent => ent.ChildrenEntities()))
                        CreatePrim(renderer, part);
#endif
                }
                    
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Warn("[Warp3D]: Exception in the map generation, " + ex);
            }

            renderer.Render();
            Bitmap bitmap = renderer.Scene.getImage();
            bitmap = ImageUtils.ResizeImage(bitmap, Constants.RegionSize, Constants.RegionSize);
            foreach (var o in renderer.Scene.objectData.Values)
            {
                warp_Object obj = (warp_Object)o;
                obj.vertexData = null;
                obj.triangleData = null;
            }
            renderer.Scene.removeAllObjects();
            renderer = null;
            viewport = null;
            m_primMesher = null;
            terrainObj.fastvertex = null;
            terrainObj.fasttriangle = null;
            terrainObj = null;
            m_colors.Clear();

            //Force GC to try to clean this mess up
            GC.GetTotalMemory(true);

            return bitmap;
        }

        #endregion

        #region Rendering Methods

        private void CreateWater(WarpRenderer renderer)
        {
            float waterHeight = (float) m_scene.RegionInfo.RegionSettings.WaterHeight;

            renderer.AddPlane("Water", m_scene.RegionInfo.RegionSizeX*0.5f);
            renderer.Scene.sceneobject("Water").setPos((m_scene.RegionInfo.RegionSizeX/2) - 0.5f, waterHeight,
                                                       (m_scene.RegionInfo.RegionSizeY/2) - 0.5f);

            RegionLightShareData rls = m_scene.RequestModuleInterface<IWindLightSettingsModule>().FindRegionWindLight();

            warp_Material waterColormaterial;
            if (rls != null)
                waterColormaterial =
                    new warp_Material(
                        ConvertColor(new Color4(rls.waterColor.X/256, rls.waterColor.Y/256, rls.waterColor.Z/256,
                                                WATER_COLOR.A)));
            else
                waterColormaterial = new warp_Material(ConvertColor(WATER_COLOR));

            waterColormaterial.setTransparency((byte) ((1f - WATER_COLOR.A)*255f)*2);
            waterColormaterial.setReflectivity(50);
            renderer.Scene.addMaterial("WaterColor", waterColormaterial);
            renderer.SetObjectMaterial("Water", "WaterColor");

            /*
            AssetBase textureAsset = m_scene.AssetService.Get(rls.normalMapTexture.ToString());
            if (textureAsset != null)
            {
                IJ2KDecoder decoder = m_scene.RequestModuleInterface<IJ2KDecoder> ();
                Bitmap bitmap = (Bitmap)decoder.DecodeToImage (textureAsset.Data);
                if (bitmap != null)
                {
                    textureAsset = null;
                    warp_Texture texture = new warp_Texture (bitmap);
                    warp_Material waterTextmaterial = new warp_Material (texture);
                    waterTextmaterial.setTransparency ((byte)((1f - WATER_COLOR.A) * 255f) * 4);
                    waterTextmaterial.setReflectivity (0);
                    renderer.AddPlane ("Water2", m_scene.RegionInfo.RegionSizeX * 0.5f);
                    renderer.Scene.sceneobject ("Water2").setPos ((m_scene.RegionInfo.RegionSizeX / 2) - 0.5f, waterHeight, (m_scene.RegionInfo.RegionSizeY / 2) - 0.5f);
                    renderer.Scene.addMaterial ("WaterColor2", waterTextmaterial);
                    renderer.SetObjectMaterial ("Water2", "WaterColor2");
                }
            }*/
        }

        private warp_Object CreateTerrain(WarpRenderer renderer, bool textureTerrain)
        {
            ITerrainChannel terrain = m_scene.RequestModuleInterface<ITerrainChannel>();

            float diff = (float)m_scene.RegionInfo.RegionSizeY / (float)Constants.RegionSize;
            warp_Object obj =
                new warp_Object(Constants.RegionSize * Constants.RegionSize,
                                ((Constants.RegionSize - 1) * (Constants.RegionSize - 1) *2));

            for (float y = 0; y < m_scene.RegionInfo.RegionSizeY; y += diff)
            {
                for (float x = 0; x < m_scene.RegionInfo.RegionSizeX; x += diff)
                {
                    warp_Vector pos = ConvertVector(x, y, terrain[(int)x, (int)y]);
                    obj.addVertex(new warp_Vertex(pos, x / (float)(m_scene.RegionInfo.RegionSizeX),
                                                  (((float)m_scene.RegionInfo.RegionSizeX) - y) /
                                                  (m_scene.RegionInfo.RegionSizeX)));
                }
            }

            for (float y = 0; y < m_scene.RegionInfo.RegionSizeY; y += diff)
            {
                for (float x = 0; x < m_scene.RegionInfo.RegionSizeX; x += diff)
                {
                    float newX = x / diff;
                    float newY = y / diff;
                    if (newX < Constants.RegionSize - 1 && newY < Constants.RegionSize - 1)
                    {
                        int v = (int)(newY*Constants.RegionSize + newX);

                        // Normal
                        Vector3 v1 = new Vector3(newX, newY, (terrain[(int)x, (int)y]) / Constants.TerrainCompression);
                        Vector3 v2 = new Vector3(newX + 1, newY, (terrain[(int)x + 1, (int)y]) / Constants.TerrainCompression);
                        Vector3 v3 = new Vector3(newX, newY + 1, (terrain[(int)x, (int)(y + 1)]) / Constants.TerrainCompression);
                        warp_Vector norm = ConvertVector(SurfaceNormal(v1, v2, v3));
                        norm = norm.reverse();
                        obj.vertex(v).n = norm;

                        // Triangle 1
                        obj.addTriangle(
                            v,
                            v + 1,
                            v + Constants.RegionSize);

                        // Triangle 2
                        obj.addTriangle(
                            v + Constants.RegionSize + 1,
                            v + Constants.RegionSize,
                            v + 1);
                    }
                }
            }

            renderer.Scene.addObject("Terrain", obj);

            UUID[] textureIDs = new UUID[4];
            float[] startHeights = new float[4];
            float[] heightRanges = new float[4];

            RegionSettings regionInfo = m_scene.RegionInfo.RegionSettings;

            textureIDs[0] = regionInfo.TerrainTexture1;
            textureIDs[1] = regionInfo.TerrainTexture2;
            textureIDs[2] = regionInfo.TerrainTexture3;
            textureIDs[3] = regionInfo.TerrainTexture4;

            startHeights[0] = (float) regionInfo.Elevation1SW;
            startHeights[1] = (float) regionInfo.Elevation1NW;
            startHeights[2] = (float) regionInfo.Elevation1SE;
            startHeights[3] = (float) regionInfo.Elevation1NE;

            heightRanges[0] = (float) regionInfo.Elevation2SW;
            heightRanges[1] = (float) regionInfo.Elevation2NW;
            heightRanges[2] = (float) regionInfo.Elevation2SE;
            heightRanges[3] = (float) regionInfo.Elevation2NE;

            uint globalX, globalY;
            Utils.LongToUInts(m_scene.RegionInfo.RegionHandle, out globalX, out globalY);

            Bitmap image = TerrainSplat.Splat(terrain, textureIDs, startHeights, heightRanges,
                                              new Vector3d(globalX, globalY, 0.0), m_scene.AssetService, textureTerrain);
            warp_Texture texture = new warp_Texture(image);
            warp_Material material = new warp_Material(texture);
            material.setReflectivity(0); // reduces tile seams a bit thanks lkalif
            renderer.Scene.addMaterial("TerrainColor", material);
            renderer.SetObjectMaterial("Terrain", "TerrainColor");
            return obj;
        }

        private static Vector3 SurfaceNormal(Vector3 c1, Vector3 c2, Vector3 c3)
        {
            Vector3 edge1 = new Vector3(c2.X - c1.X, c2.Y - c1.Y, c2.Z - c1.Z);
            Vector3 edge2 = new Vector3(c3.X - c1.X, c3.Y - c1.Y, c3.Z - c1.Z);

            Vector3 normal = Vector3.Cross(edge1, edge2);
            normal.Normalize();

            return normal;
        }

        private void CreatePrim(WarpRenderer renderer, ISceneChildEntity prim)
        {
            try
            {
                const float MIN_SIZE = 2f;

                if ((PCode) prim.Shape.PCode != PCode.Prim)
                    return;
                if (prim.Scale.LengthSquared() < MIN_SIZE*MIN_SIZE)
                    return;

                Primitive omvPrim = prim.Shape.ToOmvPrimitive(prim.OffsetPosition, prim.RotationOffset);
                FacetedMesh renderMesh = null;

                // Are we dealing with a sculptie or mesh?
                if (omvPrim.Sculpt != null && omvPrim.Sculpt.SculptTexture != UUID.Zero)
                {
                    // Try fetchinng the asset
                    AssetBase sculptAsset = m_scene.AssetService.Get(omvPrim.Sculpt.SculptTexture.ToString());
                    if (sculptAsset != null)
                    {
                        // Is it a mesh?
                        if (omvPrim.Sculpt.Type == SculptType.Mesh)
                        {
                            AssetMesh meshAsset = new AssetMesh(omvPrim.Sculpt.SculptTexture, sculptAsset.Data);
                            FacetedMesh.TryDecodeFromAsset(omvPrim, meshAsset, DetailLevel.Highest, out renderMesh);
                        }
                        else // It's sculptie
                        {
                            IJ2KDecoder imgDecoder = m_scene.RequestModuleInterface<IJ2KDecoder>();
                            Image sculpt = imgDecoder.DecodeToImage(sculptAsset.Data);
                            if (sculpt != null)
                            {
                                renderMesh = m_primMesher.GenerateFacetedSculptMesh(omvPrim, (Bitmap) sculpt,
                                                                                    DetailLevel.Medium);
                                sculpt.Dispose();
                            }
                        }
                    }
                }
                else // Prim
                {
                    renderMesh = m_primMesher.GenerateFacetedMesh(omvPrim, DetailLevel.Medium);
                }

                if (renderMesh == null)
                    return;

                warp_Vector primPos = ConvertVector(prim.GetWorldPosition());
                warp_Quaternion primRot = ConvertQuaternion(prim.RotationOffset);

                warp_Matrix m = warp_Matrix.quaternionMatrix(primRot);

                if (prim.ParentID != 0)
                {
                    ISceneEntity group = m_scene.GetGroupByPrim(prim.LocalId);
                    if (group != null)
                        m.transform(warp_Matrix.quaternionMatrix(ConvertQuaternion(group.RootChild.RotationOffset)));
                }

                warp_Vector primScale = ConvertVector(prim.Scale);

                string primID = prim.UUID.ToString();

                // Create the prim faces
                for (int i = 0; i < renderMesh.Faces.Count; i++)
                {
                    Face face = renderMesh.Faces[i];
                    string meshName = primID + "-Face-" + i.ToString();

                    warp_Object faceObj = new warp_Object(face.Vertices.Count, face.Indices.Count/3);

                    foreach (Vertex v in face.Vertices)
                    {
                        warp_Vector pos = ConvertVector(v.Position);
                        warp_Vector norm = ConvertVector(v.Normal);

                        if (prim.Shape.SculptTexture == UUID.Zero)
                            norm = norm.reverse();
                        warp_Vertex vert = new warp_Vertex(pos, norm, v.TexCoord.X, v.TexCoord.Y);

                        faceObj.addVertex(vert);
                    }

                    for (int j = 0; j < face.Indices.Count;)
                    {
                        faceObj.addTriangle(
                            face.Indices[j++],
                            face.Indices[j++],
                            face.Indices[j++]);
                    }

                    Primitive.TextureEntryFace teFace = prim.Shape.Textures.GetFace((uint) i);
                    string materialName;
                    Color4 faceColor = GetFaceColor(teFace);

                    if (m_texturePrims && prim.Scale.LengthSquared() > 48*48)
                    {
                        materialName = GetOrCreateMaterial(renderer, faceColor, teFace.TextureID);
                    }
                    else
                    {
                        materialName = GetOrCreateMaterial(renderer, faceColor);
                    }

                    faceObj.transform(m);
                    faceObj.setPos(primPos);
                    faceObj.scaleSelf(primScale.x, primScale.y, primScale.z);

                    renderer.Scene.addObject(meshName, faceObj);

                    renderer.SetObjectMaterial(meshName, materialName);
                }
                renderMesh.Faces.Clear();
                renderMesh = null;
            }
            catch (Exception ex)
            {
                MainConsole.Instance.Warn("[Warp3D]: Exception creating prim, " + ex);
            }
        }

        private Color4 GetFaceColor(Primitive.TextureEntryFace face)
        {
            Color4 color;

            if (face.TextureID == UUID.Zero)
                return face.RGBA;

            if (!m_colors.TryGetValue(face.TextureID, out color))
            {
                bool fetched = false;

                // Attempt to fetch the texture metadata
                UUID metadataID = UUID.Combine(face.TextureID, TEXTURE_METADATA_MAGIC);
                AssetBase metadata = m_scene.AssetService.Get(metadataID.ToString());
                if (metadata != null)
                {
                    OSDMap map = null;
                    try
                    {
                        map = OSDParser.Deserialize(metadata.Data) as OSDMap;
                    }
                    catch
                    {
                    }

                    if (map != null)
                    {
                        color = map["X-JPEG2000-RGBA"].AsColor4();
                        if (!(color.R == 0.5f && color.G == 0.5f && color.B == 0.5f && color.A == 1.0f))
                            //If we failed, don't save it
                            fetched = true;
                    }
                    map = null;
                    metadata = null;
                }

                if (!fetched)
                {
                    // Fetch the texture, decode and get the average color,
                    // then save it to a temporary metadata asset
                    AssetBase textureAsset = m_scene.AssetService.Get(face.TextureID.ToString());
                    if (textureAsset != null)
                    {
                        int width, height;
                        color = GetAverageColor(textureAsset.ID, textureAsset.Data, m_scene, out width, out height);
                        if (!(color.R == 0.5f && color.G == 0.5f && color.B == 0.5f && color.A == 1.0f))
                            //If we failed, don't save it
                        {
                            OSDMap data = new OSDMap {{"X-JPEG2000-RGBA", OSD.FromColor4(color)}};
                            metadata = new AssetBase
                                           {
                                               Data = Encoding.UTF8.GetBytes(OSDParser.SerializeJsonString(data)),
                                               Description = "Avg Color-JPEG2000 texture " + face.TextureID.ToString(),
                                               Flags = AssetFlags.Collectable | AssetFlags.Temporary | AssetFlags.Local,
                                               ID = metadataID,
                                               Name = String.Empty,
                                               TypeAsset = AssetType.Simstate
                                               // Make something up to get around OpenSim's myopic treatment of assets
                                           };
                            metadata.ID = m_scene.AssetService.Store(metadata);
                        }
                        textureAsset = null;
                    }
                    else
                    {
                        color = new Color4(0.5f, 0.5f, 0.5f, 1.0f);
                    }
                }

                m_colors[face.TextureID] = color;
            }

            return color*face.RGBA;
        }

        private string GetOrCreateMaterial(WarpRenderer renderer, Color4 color)
        {
            string name = color.ToString();

            warp_Material material = renderer.Scene.material(name);
            if (material != null)
                return name;

            renderer.AddMaterial(name, ConvertColor(color));
            if (color.A < 1f)
                renderer.Scene.material(name).setTransparency((byte) ((1f - color.A)*255f));
            return name;
        }

        public string GetOrCreateMaterial(WarpRenderer renderer, Color4 faceColor, UUID textureID)
        {
            string materialName = "Color-" + faceColor.ToString() + "-Texture-" + textureID.ToString();

            if (renderer.Scene.material(materialName) == null)
            {
                MainConsole.Instance.DebugFormat("Creating material {0}", materialName);
                renderer.AddMaterial(materialName, ConvertColor(faceColor));
                if (faceColor.A < 1f)
                {
                    renderer.Scene.material(materialName).setTransparency((byte) ((1f - faceColor.A)*255f));
                }
                warp_Texture texture = GetTexture(textureID);
                if (texture != null)
                {
                    renderer.Scene.material(materialName).setTexture(texture);
                }
            }

            return materialName;
        }

        private warp_Texture GetTexture(UUID id)
        {
            warp_Texture ret = null;
            AssetBase asset = m_scene.AssetService.Get(id.ToString());
            if (asset != null)
            {
                IJ2KDecoder imgDecoder = m_scene.RequestModuleInterface<IJ2KDecoder>();
                Bitmap img = (Bitmap) imgDecoder.DecodeToImage(asset.Data);
                if (img != null)
                {
                    return new warp_Texture(img);
                }
            }
            return ret;
        }

        #endregion Rendering Methods

        #region Static Helpers

        private static warp_Vector ConvertVector(float x, float y, float z)
        {
            return new warp_Vector(x, z, y);
        }

        private static warp_Vector ConvertVector(Vector3 vector)
        {
            return new warp_Vector(vector.X, vector.Z, vector.Y);
        }

        private static warp_Quaternion ConvertQuaternion(Quaternion quat)
        {
            return new warp_Quaternion(quat.X, quat.Z, quat.Y, -quat.W);
        }

        private static int ConvertColor(Color4 color)
        {
            int c = warp_Color.getColor((byte) (color.R*255f), (byte) (color.G*255f), (byte) (color.B*255f));
            if (color.A < 1f)
                c |= (byte) (color.A*255f) << 24;

            return c;
        }

        public static Color4 GetAverageColor(UUID textureID, byte[] j2kData, IScene scene, out int width, out int height)
        {
            ulong r = 0;
            ulong g = 0;
            ulong b = 0;
            ulong a = 0;
            Bitmap bitmap = null;
            try
            {
                IJ2KDecoder decoder = scene.RequestModuleInterface<IJ2KDecoder>();

                bitmap = (Bitmap) decoder.DecodeToImage(j2kData);
                width = 0;
                height = 0;
                if (bitmap == null)
                    return new Color4(0.5f, 0.5f, 0.5f, 1.0f);
                j2kData = null;
                width = bitmap.Width;
                height = bitmap.Height;

                BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly,
                                                        bitmap.PixelFormat);
                int pixelBytes = (bitmap.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;

                // Sum up the individual channels
                unsafe
                {
                    if (pixelBytes == 4)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            byte* row = (byte*) bitmapData.Scan0 + (y*bitmapData.Stride);

                            for (int x = 0; x < width; x++)
                            {
                                b += row[x*pixelBytes + 0];
                                g += row[x*pixelBytes + 1];
                                r += row[x*pixelBytes + 2];
                                a += row[x*pixelBytes + 3];
                            }
                        }
                    }
                    else
                    {
                        for (int y = 0; y < height; y++)
                        {
                            byte* row = (byte*) bitmapData.Scan0 + (y*bitmapData.Stride);

                            for (int x = 0; x < width; x++)
                            {
                                b += row[x*pixelBytes + 0];
                                g += row[x*pixelBytes + 1];
                                r += row[x*pixelBytes + 2];
                            }
                        }
                    }
                }

                // Get the averages for each channel
                const decimal OO_255 = 1m/255m;
                decimal totalPixels = (width*height);

                decimal rm = (r/totalPixels)*OO_255;
                decimal gm = (g/totalPixels)*OO_255;
                decimal bm = (b/totalPixels)*OO_255;
                decimal am = (a/totalPixels)*OO_255;

                if (pixelBytes == 3)
                    am = 1m;

                return new Color4((float) rm, (float) gm, (float) bm, (float) am);
            }
            catch (Exception ex)
            {
                MainConsole.Instance.WarnFormat("[MAPTILE]: Error decoding JPEG2000 texture {0} ({1} bytes): {2}", textureID,
                                 j2kData.Length, ex.Message);
                width = 0;
                height = 0;
                return new Color4(0.5f, 0.5f, 0.5f, 1.0f);
            }
            finally
            {
                if (bitmap != null)
                    bitmap.Dispose();
                bitmap = null;
            }
        }

        #endregion Static Helpers
    }

    public static class ImageUtils
    {
        /// <summary>
        ///   Performs bilinear interpolation between four values
        /// </summary>
        /// <param name = "v00">First, or top left value</param>
        /// <param name = "v01">Second, or top right value</param>
        /// <param name = "v10">Third, or bottom left value</param>
        /// <param name = "v11">Fourth, or bottom right value</param>
        /// <param name = "xPercent">Interpolation value on the X axis, between 0.0 and 1.0</param>
        /// <param name = "yPercent">Interpolation value on fht Y axis, between 0.0 and 1.0</param>
        /// <returns>The bilinearly interpolated result</returns>
        public static float Bilinear(float v00, float v01, float v10, float v11, float xPercent, float yPercent)
        {
            return Utils.Lerp(Utils.Lerp(v00, v01, xPercent), Utils.Lerp(v10, v11, xPercent), yPercent);
        }

        /// <summary>
        ///   Performs a high quality image resize
        /// </summary>
        /// <param name = "image">Image to resize</param>
        /// <param name = "width">New width</param>
        /// <param name = "height">New height</param>
        /// <returns>Resized image</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);

            using (Graphics graphics = Graphics.FromImage(result))
            {
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                graphics.DrawImage(image, 0, 0, result.Width, result.Height);
            }
            image.Dispose();

            return result;
        }
    }
}