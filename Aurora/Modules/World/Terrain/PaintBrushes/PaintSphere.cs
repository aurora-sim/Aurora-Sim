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

using System.Collections.Generic;
using System.Drawing;
using BitmapProcessing;
using OpenMetaverse;
using OpenMetaverse.Imaging;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace Aurora.Modules.Terrain.PaintBrushes
{
    public class PaintSphere : ITerrainPaintableEffect
    {
        private volatile bool locked;
        private UUID m_textureToPaint = RegionSettings.DEFAULT_TERRAIN_TEXTURE_1;

        #region ITerrainPaintableEffect Members

        public void PaintEffect(ITerrainChannel map, UUID userID, float rx, float ry, float rz, float strength,
                                float duration, float BrushSize, List<IScene> scene)
        {
            if (locked)
                return;
            locked = true;
            strength = TerrainUtil.MetersToSphericalStrength(BrushSize);

            int x, y;

            int xFrom = (int) (rx - BrushSize + 0.5);
            int xTo = (int) (rx + BrushSize + 0.5) + 1;
            int yFrom = (int) (ry - BrushSize + 0.5);
            int yTo = (int) (ry + BrushSize + 0.5) + 1;

            if (xFrom < 0)
                xFrom = 0;

            if (yFrom < 0)
                yFrom = 0;

            if (xTo > map.Width)
                xTo = map.Width;

            if (yTo > map.Height)
                yTo = map.Height;

            //ONLY get cached assets, since this is a local asset ONLY
            AssetBase paintAsset =
                map.Scene.AssetService.Get(map.Scene.RegionInfo.RegionSettings.PaintableTerrainTexture.ToString());
            if (paintAsset == null)
            {
                paintAsset = new AssetBase(map.Scene.RegionInfo.RegionSettings.PaintableTerrainTexture,
                                           "PaintableTerrainTexture-" + map.Scene.RegionInfo.RegionID, AssetType.Texture,
                                           UUID.Zero) {Flags = AssetFlags.Deletable};
                AssetBase defaultTexture =
                    map.Scene.AssetService.Get(RegionSettings.DEFAULT_TERRAIN_TEXTURE_2.ToString()); //Nice grass
                if (defaultTexture == null)
                    //Erm... what to do!
                    return;

                paintAsset.Data = defaultTexture.Data;
                    //Eventually we need to replace this with an interpolation of the existing textures!
            }

            AssetBase textureToApply = map.Scene.AssetService.Get(m_textureToPaint.ToString());
                //The texture the client wants to paint
            if (textureToApply == null)
                return;

            Image paintiTexture = map.Scene.RequestModuleInterface<IJ2KDecoder>().DecodeToImage(paintAsset.Data);
            if (paintiTexture == null)
                return;

            Image textureToAddiTexture =
                map.Scene.RequestModuleInterface<IJ2KDecoder>().DecodeToImage(textureToApply.Data);
            if (textureToAddiTexture == null)
            {
                paintiTexture.Dispose();
                return;
            }

            FastBitmap paintTexture = new FastBitmap((Bitmap) paintiTexture);
            FastBitmap textureToAddTexture = new FastBitmap((Bitmap) textureToAddiTexture);

            paintTexture.LockBitmap();
            textureToAddTexture.LockBitmap();

            // blend in map
            for (x = xFrom; x < xTo; x++)
            {
                for (y = yFrom; y < yTo; y++)
                {
                    if (!map.Scene.Permissions.CanTerraformLand(userID, new Vector3(x, y, 0)))
                        continue;

                    Color c =
                        textureToAddTexture.GetPixel(
                            (int) ((x/(float) map.Scene.RegionInfo.RegionSizeX*textureToAddiTexture.Width)),
                            (int) ((y/(float) map.Scene.RegionInfo.RegionSizeX)*textureToAddiTexture.Height));
                    paintTexture.SetPixel((int) ((x/(float) map.Scene.RegionInfo.RegionSizeX)*paintiTexture.Width),
                                          (int) ((y/(float) map.Scene.RegionInfo.RegionSizeX)*paintiTexture.Height), c);
                }
            }
            map.Scene.AssetService.Delete(paintAsset.ID);
            paintTexture.UnlockBitmap();
            textureToAddTexture.UnlockBitmap();
            paintAsset.Data = OpenJPEG.EncodeFromImage(paintTexture.Bitmap(), false);
            paintAsset.Flags = AssetFlags.Deletable;
            map.Scene.RegionInfo.RegionSettings.PaintableTerrainTexture = UUID.Random();
            paintAsset.ID = map.Scene.RegionInfo.RegionSettings.PaintableTerrainTexture;
            paintAsset.ID = map.Scene.AssetService.Store(paintAsset);
            map.Scene.RequestModuleInterface<IEstateModule>().sendRegionHandshakeToAll();
            locked = false;
        }

        #endregion
    }
}