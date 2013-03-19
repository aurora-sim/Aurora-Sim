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

using Aurora.Framework;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Aurora.Modules.WorldMap
{
    // Hue, Saturation, Value; used for color-interpolation
    public struct HSV
    {
        public float h;
        public float s;
        public float v;

        public HSV(float h, float s, float v)
        {
            this.h = h;
            this.s = s;
            this.v = v;
        }

        // (for info about algorithm, see http://en.wikipedia.org/wiki/HSL_and_HSV)
        public HSV(Color c)
        {
            float r = c.R/255f;
            float g = c.G/255f;
            float b = c.B/255f;
            float max = Math.Max(Math.Max(r, g), b);
            float min = Math.Min(Math.Min(r, g), b);
            float diff = max - min;

            if (max == min) h = 0f;
            else if (max == r) h = (g - b)/diff*60f;
            else if (max == g) h = (b - r)/diff*60f + 120f;
            else h = (r - g)/diff*60f + 240f;
            if (h < 0f) h += 360f;

            if (max == 0f) s = 0f;
            else s = diff/max;

            v = max;
        }

        // (for info about algorithm, see http://en.wikipedia.org/wiki/HSL_and_HSV)
        public Color toColor()
        {
            float f = h/60f;
            int sector = (int) f%6;
            f = f - (int) f;
            int pi = (int) (v*(1f - s)*255f);
            int qi = (int) (v*(1f - s*f)*255f);
            int ti = (int) (v*(1f - (1f - f)*s)*255f);
            int vi = (int) (v*255f);

            if (pi < 0) pi = 0;
            if (pi > 255) pi = 255;
            if (qi < 0) qi = 0;
            if (qi > 255) qi = 255;
            if (ti < 0) ti = 0;
            if (ti > 255) ti = 255;
            if (vi < 0) vi = 0;
            if (vi > 255) vi = 255;

            switch (sector)
            {
                case 0:
                    return Color.FromArgb(vi, ti, pi);
                case 1:
                    return Color.FromArgb(qi, vi, pi);
                case 2:
                    return Color.FromArgb(pi, vi, ti);
                case 3:
                    return Color.FromArgb(pi, qi, vi);
                case 4:
                    return Color.FromArgb(ti, pi, vi);
                default:
                    return Color.FromArgb(vi, pi, qi);
            }
        }
    }

    public class TexturedMapTileRenderer : IMapTileTerrainRenderer
    {
        #region Constants

        // some hardcoded terrain UUIDs that work with SL 1.20 (the four default textures and "Blank").
        // The color-values were choosen because they "look right" (at least to me) ;-)
        private static readonly UUID defaultTerrainTexture1 = new UUID("0bc58228-74a0-7e83-89bc-5c23464bcec5");
        private static readonly Color defaultColor1 = Color.FromArgb(165, 137, 118);
        private static readonly UUID defaultTerrainTexture2 = new UUID("63338ede-0037-c4fd-855b-015d77112fc8");
        private static readonly Color defaultColor2 = Color.FromArgb(69, 89, 49);
        private static readonly UUID defaultTerrainTexture3 = new UUID("303cd381-8560-7579-23f1-f0a880799740");
        private static readonly Color defaultColor3 = Color.FromArgb(162, 154, 141);
        private static readonly UUID defaultTerrainTexture4 = new UUID("53a2f406-4895-1d13-d541-d2e3b86bc19c");
        private static readonly Color defaultColor4 = Color.FromArgb(200, 200, 200);

        private static readonly Color WATER_COLOR = Color.FromArgb(29, 71, 95);

        #endregion

        // private IConfigSource m_config; // not used currently

        // mapping from texture UUIDs to averaged color. This will contain all the textures in the sim.
        //   This could be considered a memory-leak, but it's *hopefully* taken care of after the terrain is generated
        private Dictionary<UUID, Color> m_mapping;
        private IScene m_scene;

        #region IMapTileTerrainRenderer Members

        public void Initialise(IScene scene, IConfigSource source)
        {
            m_scene = scene;
            // m_config = source; // not used currently
            m_mapping = new Dictionary<UUID, Color>
                            {
                                {defaultTerrainTexture1, defaultColor1},
                                {defaultTerrainTexture2, defaultColor2},
                                {defaultTerrainTexture3, defaultColor3},
                                {defaultTerrainTexture4, defaultColor4},
                                {Util.BLANK_TEXTURE_UUID, Color.White}
                            };

            ReadCacheMap();
        }

        public Bitmap TerrainToBitmap(Bitmap mapbmp)
        {
            FastBitmap unsafeBMP = new FastBitmap(mapbmp);
            unsafeBMP.LockBitmap();
            //DateTime start = DateTime.Now;
            //MainConsole.Instance.Info("[MAPTILE]: Generating Maptile Step 1: Terrain");

            // These textures should be in the AssetCache anyway, as every client conneting to this
            // region needs them. Except on start, when the map is recreated (before anyone connected),
            // and on change of the estate settings (textures and terrain values), when the map should
            // be recreated.
            RegionSettings settings = m_scene.RegionInfo.RegionSettings;

            // the four terrain colors as HSVs for interpolation
            HSV hsv1 = new HSV(computeAverageColor(settings.TerrainTexture1, defaultColor1));
            HSV hsv2 = new HSV(computeAverageColor(settings.TerrainTexture2, defaultColor2));
            HSV hsv3 = new HSV(computeAverageColor(settings.TerrainTexture3, defaultColor3));
            HSV hsv4 = new HSV(computeAverageColor(settings.TerrainTexture4, defaultColor4));

            float levelNElow = (float) settings.Elevation1NE;
            float levelNEhigh = (float) settings.Elevation2NE;

            float levelNWlow = (float) settings.Elevation1NW;
            float levelNWhigh = (float) settings.Elevation2NW;

            float levelSElow = (float) settings.Elevation1SE;
            float levelSEhigh = (float) settings.Elevation2SE;

            float levelSWlow = (float) settings.Elevation1SW;
            float levelSWhigh = (float) settings.Elevation2SW;

            float waterHeight = (float) settings.WaterHeight;

            ITerrainChannel heightmap = m_scene.RequestModuleInterface<ITerrainChannel>();
            float sizeRatio = m_scene.RegionInfo.RegionSizeX/(float) Constants.RegionSize;
            for (float y = 0; y < m_scene.RegionInfo.RegionSizeY; y += sizeRatio)
            {
                float rowRatio = y/(m_scene.RegionInfo.RegionSizeY - 1); // 0 - 1, for interpolation
                for (float x = 0; x < m_scene.RegionInfo.RegionSizeX; x += sizeRatio)
                {
                    float columnRatio = x/(m_scene.RegionInfo.RegionSizeX - 1); // 0 - 1, for interpolation

                    float heightvalue = getHeight(heightmap, (int) x, (int) y);

                    if (heightvalue > waterHeight)
                    {
                        // add a bit noise for breaking up those flat colors:
                        // - a large-scale noise, for the "patches" (using an doubled s-curve for sharper contrast)
                        // - a small-scale noise, for bringing in some small scale variation
                        //float bigNoise = (float)TerrainUtil.InterpolatedNoise(x / 8.0, y / 8.0) * .5f + .5f; // map to 0.0 - 1.0
                        //float smallNoise = (float)TerrainUtil.InterpolatedNoise(x + 33, y + 43) * .5f + .5f;
                        //float hmod = heightvalue + smallNoise * 3f + S(S(bigNoise)) * 10f;
                        float hmod =
                            heightvalue; // 0 - 10

                        // find the low/high values for this point (interpolated bilinearily)
                        // (and remember, x=0,y=0 is SW)
                        float low = levelSWlow*(1f - rowRatio)*(1f - columnRatio) +
                                    levelSElow*(1f - rowRatio)*columnRatio +
                                    levelNWlow*rowRatio*(1f - columnRatio) +
                                    levelNElow*rowRatio*columnRatio;
                        float high = levelSWhigh*(1f - rowRatio)*(1f - columnRatio) +
                                     levelSEhigh*(1f - rowRatio)*columnRatio +
                                     levelNWhigh*rowRatio*(1f - columnRatio) +
                                     levelNEhigh*rowRatio*columnRatio;
                        if (high < low)
                        {
                            // someone tried to fool us. High value should be higher than low every time
                            float tmp = high;
                            high = low;
                            low = tmp;
                        }

                        HSV hsv;
                        if (hmod <= low) hsv = hsv1; // too low
                        else if (hmod >= high) hsv = hsv4; // too high
                        else
                        {
                            // HSV-interpolate along the colors
                            // first, rescale h to 0.0 - 1.0
                            hmod = (hmod - low)/(high - low);
                            // now we have to split: 0.00 => color1, 0.33 => color2, 0.67 => color3, 1.00 => color4
                            if (hmod < 1f/3f) hsv = interpolateHSV(ref hsv1, ref hsv2, hmod*3f);
                            else if (hmod < 2f/3f) hsv = interpolateHSV(ref hsv2, ref hsv3, (hmod*3f) - 1f);
                            else hsv = interpolateHSV(ref hsv3, ref hsv4, (hmod*3f) - 2f);
                        }
                        //get the data from the original image
                        Color hsvColor = hsv.toColor();
                        unsafeBMP.SetPixel((int) (x/sizeRatio),
                                           (int) (((m_scene.RegionInfo.RegionSizeY - 1) - y)/sizeRatio), hsvColor);
                    }
                    else
                    {
                        // We're under the water level with the terrain, so paint water instead of land
                        unsafeBMP.SetPixel((int) (x/sizeRatio),
                                           (int) (((m_scene.RegionInfo.RegionSizeY - 1) - y)/sizeRatio), WATER_COLOR);
                    }
                }
            }
            if (m_mapping != null)
            {
                SaveCache();
                m_mapping.Clear();
            }
            unsafeBMP.UnlockBitmap();
            //MainConsole.Instance.Info("[MAPTILE]: Generating Maptile Step 1: Done in " + (DateTime.Now - start).TotalSeconds + " ms");
            return unsafeBMP.Bitmap();
        }

        #endregion

        private void ReadCacheMap()
        {
            if (!Directory.Exists("assetcache"))
                Directory.CreateDirectory("assetcache");
            if (!Directory.Exists(Path.Combine("assetcache", "mapTileTextureCache")))
                Directory.CreateDirectory(Path.Combine("assetcache", "mapTileTextureCache"));

            FileStream stream =
                new FileStream(
                    Path.Combine(Path.Combine("assetcache", "mapTileTextureCache"),
                                 m_scene.RegionInfo.RegionName + ".tc"), FileMode.OpenOrCreate);
            StreamReader m_streamReader = new StreamReader(stream);
            string file = m_streamReader.ReadToEnd();
            m_streamReader.Close();
            //Read file here
            if (file != "") //New file
            {
                bool loaded = DeserializeCache(file);
                if (!loaded)
                {
                    //Something went wrong, delete the file
                    try
                    {
                        File.Delete(Path.Combine(Path.Combine("assetcache", "mapTileTextureCache"),
                                                 m_scene.RegionInfo.RegionName + ".tc"));
                    }
                    catch
                    {
                    }
                }
            }
        }

        private bool DeserializeCache(string file)
        {
            OSDMap map = OSDParser.DeserializeJson(file) as OSDMap;
            if (map == null)
                return false;

            foreach (KeyValuePair<string, OSD> kvp in map)
            {
                Color4 c = kvp.Value.AsColor4();
                UUID key = UUID.Parse(kvp.Key);
                if (!m_mapping.ContainsKey(key))
                    m_mapping.Add(key,
                                  Color.FromArgb((int) (c.A*255), (int) (c.R*255), (int) (c.G*255), (int) (c.B*255)));
            }

            return true;
        }

        private void SaveCache()
        {
            OSDMap map = SerializeCache();
            FileStream stream =
                new FileStream(
                    Path.Combine(Path.Combine("assetcache", "mapTileTextureCache"),
                                 m_scene.RegionInfo.RegionName + ".tc"), FileMode.Create);
            StreamWriter writer = new StreamWriter(stream);
            writer.WriteLine(OSDParser.SerializeJsonString(map));
            writer.Close();
        }

        private OSDMap SerializeCache()
        {
            OSDMap map = new OSDMap();
            foreach (KeyValuePair<UUID, Color> kvp in m_mapping)
            {
                map.Add(kvp.Key.ToString(), new Color4(kvp.Value.R, kvp.Value.G, kvp.Value.B, kvp.Value.A));
            }
            return map;
        }

        #region Helpers

        // This fetches the texture from the asset server synchroneously. That should be ok, as we
        // call map-creation either async or sync, depending on what the user specified and it shouldn't
        // take too long, as most assets should be cached
        private Bitmap fetchTexture(UUID id)
        {
            byte[] asset = m_scene.AssetService.GetData(id.ToString());
            if (asset != null)
            {
                try
                {
                    if (asset != null)
                    {
                        Image image = m_scene.RequestModuleInterface<IJ2KDecoder>().DecodeToImage(asset);
                        if (image != null)
                            return new Bitmap(image);
                    }
                }
                catch (DllNotFoundException)
                {
                    MainConsole.Instance.ErrorFormat(
                        "[TexturedMapTileRenderer]: OpenJpeg is not installed correctly on this system.   Asset Data is emtpy for {0}",
                        id);
                }
                catch (IndexOutOfRangeException)
                {
                    MainConsole.Instance.ErrorFormat(
                        "[TexturedMapTileRenderer]: OpenJpeg was unable to encode this.   Asset Data is emtpy for {0}",
                        id);
                }
                catch (Exception)
                {
                    MainConsole.Instance.ErrorFormat(
                        "[TexturedMapTileRenderer]: OpenJpeg was unable to encode this.   Asset Data is emtpy for {0}",
                        id);
                }
            }
            return null;
        }

        // Compute the average color of a texture.
        private Color computeAverageColor(Bitmap bmp)
        {
            FastBitmap unsafeBMP = new FastBitmap(bmp);
            // we have 256 x 256 pixel, each with 256 possible color-values per
            // color-channel, so 2^24 is the maximum value we can get, adding everything.
            unsafeBMP.LockBitmap();
            int r = 0;
            int g = 0;
            int b = 0;
            int y = 0;
            int x = 0;
            for (y = 0; y < bmp.Height; y += 10)
            {
                for (x = 0; x < bmp.Width; x += 10)
                {
                    Color pixel = unsafeBMP.GetPixel(x, y);
                    r += pixel.R;
                    g += pixel.G;
                    b += pixel.B;
                }
            }

            unsafeBMP.UnlockBitmap();

            int pixels = ((x/10)*(y/10));
            return Color.FromArgb(r/pixels, g/pixels, b/pixels);
        }

        // return either the average color of the texture, or the defaultColor if the texturID is invalid
        // or the texture couldn't be found
        private Color computeAverageColor(UUID textureID, Color defaultColor)
        {
            if (textureID == UUID.Zero)
                return defaultColor; // not set

            if (m_mapping.ContainsKey(textureID))
                return m_mapping[textureID]; // one of the predefined textures

            Bitmap bmp = fetchTexture(textureID);
            Color color = bmp == null ? defaultColor : computeAverageColor(bmp);
            if (bmp != null)
                bmp.Dispose(); //Destroy the image that we don't need
            // store it for future reference
            m_mapping[textureID] = color;

            return color;
        }

        // S-curve: f(x) = 3x² - 2x³:
        // f(0) = 0, f(0.5) = 0.5, f(1) = 1,
        // f'(x) = 0 at x = 0 and x = 1; f'(0.5) = 1.5,
        // f''(0.5) = 0, f''(x) != 0 for x != 0.5
        private float S(float v)
        {
            return (v*v*(3f - 2f*v));
        }

        // interpolate two colors in HSV space and return the resulting color
        private HSV interpolateHSV(ref HSV c1, ref HSV c2, float ratio)
        {
            if (ratio <= 0f) return c1;
            if (ratio >= 1f) return c2;

            // make sure we are on the same side on the hue-circle for interpolation
            // We change the hue of the parameters here, but we don't change the color
            // represented by that value
            if (c1.h - c2.h > 180f) c1.h -= 360f;
            else if (c2.h - c1.h > 180f) c1.h += 360f;

            return new HSV(c1.h*(1f - ratio) + c2.h*ratio,
                           c1.s*(1f - ratio) + c2.s*ratio,
                           c1.v*(1f - ratio) + c2.v*ratio);
        }

        // the heigthfield might have some jumps in values. Rendered land is smooth, though,
        // as a slope is rendered at that place. So average 4 neighbour values to emulate that.
        private float getHeight(ITerrainChannel hm, int x, int y)
        {
            if (x < (m_scene.RegionInfo.RegionSizeX - 1) && y < (m_scene.RegionInfo.RegionSizeY - 1))
                return (hm[x, y]*.444f + (hm[x + 1, y] + hm[x, y + 1])*.222f + hm[x + 1, y + 1]*.112f);
            else
                return 0;
        }

        #endregion
    }
}