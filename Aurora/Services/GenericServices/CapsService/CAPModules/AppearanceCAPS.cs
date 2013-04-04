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

using Aurora.Framework;
using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.Servers;
using Aurora.Framework.Servers.HttpServer;
using Aurora.Framework.Servers.HttpServer.Implementation;
using Aurora.Framework.Services;
using Aurora.Framework.Services.ClassHelpers.Assets;
using Aurora.Framework.Services.ClassHelpers.Inventory;
using Aurora.Framework.Utilities;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Imaging;
using OpenMetaverse.StructuredData;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Encoder = System.Drawing.Imaging.Encoder;
using GridRegion = Aurora.Framework.Services.GridRegion;

namespace Aurora.Services
{
    public class AppearanceCAPS : IExternalCapsRequestHandler
    {
        protected IAssetService m_assetService;
        protected IAvatarService m_avatarService;
        protected IInventoryService m_inventoryService;
        protected ISyncMessagePosterService m_syncMessage;
        protected UUID m_agentID;
        protected GridRegion m_region;
        protected string m_uri;

        public string Name { get { return GetType().Name; } }

        public void IncomingCapsRequest(UUID agentID, GridRegion region, ISimulationBase simbase, ref OSDMap capURLs)
        {
            m_syncMessage = simbase.ApplicationRegistry.RequestModuleInterface<ISyncMessagePosterService>();
            m_inventoryService = simbase.ApplicationRegistry.RequestModuleInterface<IInventoryService>();
            m_avatarService = simbase.ApplicationRegistry.RequestModuleInterface<IAvatarService>();
            m_assetService = simbase.ApplicationRegistry.RequestModuleInterface<IAssetService>();
            m_region = region;
            m_agentID = agentID;

            m_uri = "/CAPS/UpdateAvatarAppearance/" + UUID.Random() + "/";
            MainServer.Instance.AddStreamHandler(new GenericStreamHandler("POST",
                                                    m_uri,
                                                    UpdateAvatarAppearance));
            capURLs["UpdateAvatarAppearance"] = MainServer.Instance.ServerURI + m_uri;
        }

        public void IncomingCapsDestruction()
        {
            MainServer.Instance.RemoveStreamHandler("POST", m_uri);
        }

        #region Server Side Baked Textures

        private OpenMetaverse.AppearanceManager.TextureData[] Textures = new OpenMetaverse.AppearanceManager.TextureData[(int)AvatarTextureIndex.NumberOfEntries];
        public byte[] UpdateAvatarAppearance(string path, Stream request, OSHttpRequest httpRequest,
                                             OSHttpResponse httpResponse)
        {
            try
            {
                OSDMap rm = (OSDMap) OSDParser.DeserializeLLSDXml(request);
                int cof_version = rm["cof_version"].AsInteger();

                bool success = false;
                string error = "";
                AvatarAppearance appearance = m_avatarService.GetAppearance(m_agentID);
                List<BakeType> pendingBakes = new List<BakeType>();
                List<InventoryItemBase> items = m_inventoryService.GetFolderItems(m_agentID, m_inventoryService.GetFolderForType(m_agentID, InventoryType.Unknown, AssetType.CurrentOutfitFolder).ID);
                foreach (InventoryItemBase itm in items)
                    MainConsole.Instance.Warn("[SSB]: Baking " + itm.Name);

                for (int i = 0; i < Textures.Length; i++)
                    Textures[i] = new AppearanceManager.TextureData();

                foreach (InventoryItemBase itm in items)
                {
                    if (itm.AssetType == (int)AssetType.Link)
                    {
                        UUID assetID = m_inventoryService.GetItemAssetID(m_agentID, itm.AssetID);
                        OpenMetaverse.AppearanceManager.WearableData wearable = new OpenMetaverse.AppearanceManager.WearableData();
                        AssetBase asset = m_assetService.Get(assetID.ToString());
                        if (asset != null && asset.TypeAsset != AssetType.Object)
                        {
                            wearable.Asset = new AssetClothing(assetID, asset.Data);
                            if (wearable.Asset.Decode())
                            {
                                wearable.AssetID = assetID;
                                wearable.AssetType = wearable.Asset.AssetType;
                                wearable.WearableType = wearable.Asset.WearableType;
                                wearable.ItemID = itm.AssetID;
                                DecodeWearableParams(wearable);
                            }
                        }
                    }
                }
                for (int i = 0; i < Textures.Length; i++)
                {
                    if (Textures[i].TextureID == UUID.Zero)
                        continue;
                    AssetBase asset = m_assetService.Get(Textures[i].TextureID.ToString());
                    if (asset != null)
                    {
                        Textures[i].Texture = new AssetTexture(Textures[i].TextureID, asset.Data);
                        Textures[i].Texture.Decode();
                    }
                }

                for (int bakedIndex = 0; bakedIndex < AppearanceManager.BAKED_TEXTURE_COUNT; bakedIndex++)
                {
                    AvatarTextureIndex textureIndex = AppearanceManager.BakeTypeToAgentTextureIndex((BakeType)bakedIndex);

                    if (Textures[(int)textureIndex].TextureID == UUID.Zero)
                    {
                        // If this is the skirt layer and we're not wearing a skirt then skip it
                        if (bakedIndex == (int)BakeType.Skirt && appearance.Wearables[(int)WearableType.Skirt].Count == 0)
                            continue;

                        pendingBakes.Add((BakeType)bakedIndex);
                    }
                }

                int start = Environment.TickCount;
                List<UUID> newBakeIDs = new List<UUID>();
                foreach (BakeType bakeType in pendingBakes)
                {
                    List<AvatarTextureIndex> textureIndices = OpenMetaverse.AppearanceManager.BakeTypeToTextures(bakeType);
                    Baker oven = new Baker(bakeType);

                    for (int i = 0; i < textureIndices.Count; i++)
                    {
                        int textureIndex = (int)textureIndices[i];
                        OpenMetaverse.AppearanceManager.TextureData texture = Textures[(int)textureIndex];
                        texture.TextureIndex = (AvatarTextureIndex)textureIndex;

                        oven.AddTexture(texture);
                    }

                    oven.Bake();
                    byte[] assetData = oven.BakedTexture.AssetData;
                    AssetBase newBakedAsset = new AssetBase(UUID.Random());
                    newBakedAsset.Data = assetData;
                    newBakedAsset.TypeAsset = AssetType.Texture;
                    newBakedAsset.Name = "SSB Texture";
                    newBakedAsset.Flags = AssetFlags.Deletable | AssetFlags.Collectable | AssetFlags.Rewritable | AssetFlags.Temporary;
                    if (appearance.Texture.FaceTextures[(int)AppearanceManager.BakeTypeToAgentTextureIndex(bakeType)].TextureID != UUID.Zero)
                        m_assetService.Delete(appearance.Texture.FaceTextures[(int)AppearanceManager.BakeTypeToAgentTextureIndex(bakeType)].TextureID);
                    UUID assetID = m_assetService.Store(newBakedAsset);
                    newBakeIDs.Add(assetID);
                    MainConsole.Instance.WarnFormat("[SSB]: Baked {0}", assetID);
                    int place = (int)AppearanceManager.BakeTypeToAgentTextureIndex(bakeType);
                    appearance.Texture.FaceTextures[place].TextureID = assetID;
                }

                MainConsole.Instance.ErrorFormat("[SSB]: Baking took {0} ms", (Environment.TickCount - start));

                appearance.Serial = cof_version+1;
                m_avatarService.SetAppearance(m_agentID, appearance);
                OSDMap uaamap = new OSDMap();
                uaamap["Method"] = "UpdateAvatarAppearance";
                uaamap["AgentID"] = m_agentID;
                uaamap["Appearance"] = appearance.ToOSD();
                m_syncMessage.Post(m_region.ServerURI, uaamap);
                success = true;

                OSDMap map = new OSDMap();
                map["success"] = success;
                map["error"] = error;
                map["agent_id"] = m_agentID;
                /*map["avatar_scale"] = appearance.AvatarHeight;
                map["textures"] = newBakeIDs.ToOSDArray();
                OSDArray visualParams = new OSDArray();
                foreach(byte b in appearance.VisualParams)
                    visualParams.Add((int)b);
                map["visual_params"] = visualParams;*/
                return OSDParser.SerializeLLSDXmlBytes(map);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[CAPS]: " + e);
            }

            return null;
        }

        /// <summary>
        /// Populates textures and visual params from a decoded asset
        /// </summary>
        /// <param name="wearable">Wearable to decode</param>
        private void DecodeWearableParams(OpenMetaverse.AppearanceManager.WearableData wearable)
        {
            Dictionary<VisualAlphaParam, float> alphaMasks = new Dictionary<VisualAlphaParam, float>();
            List<ColorParamInfo> colorParams = new List<ColorParamInfo>();

            // Populate collection of alpha masks from visual params
            // also add color tinting information
            foreach (KeyValuePair<int, float> kvp in wearable.Asset.Params)
            {
                if (!VisualParams.Params.ContainsKey(kvp.Key)) continue;

                VisualParam p = VisualParams.Params[kvp.Key];

                ColorParamInfo colorInfo = new ColorParamInfo();
                colorInfo.WearableType = wearable.WearableType;
                colorInfo.VisualParam = p;
                colorInfo.Value = kvp.Value;

                // Color params
                if (p.ColorParams.HasValue)
                {
                    colorInfo.VisualColorParam = p.ColorParams.Value;

                    // If this is not skin, just add params directly
                    if (wearable.WearableType != WearableType.Skin)
                    {
                        colorParams.Add(colorInfo);
                    }
                    else
                    {
                        // For skin we skip makeup params for now and use only the 3
                        // that are used to determine base skin tone
                        // Param 108 - Rainbow Color
                        // Param 110 - Red Skin (Ruddiness)
                        // Param 111 - Pigment
                        if (kvp.Key == 108 || kvp.Key == 110 || kvp.Key == 111)
                        {
                            colorParams.Add(colorInfo);
                        }
                    }
                }

                // Add alpha mask
                if (p.AlphaParams.HasValue && p.AlphaParams.Value.TGAFile != string.Empty && !p.IsBumpAttribute && !alphaMasks.ContainsKey(p.AlphaParams.Value))
                {
                    alphaMasks.Add(p.AlphaParams.Value, kvp.Value);
                }

                // Alhpa masks can also be specified in sub "driver" params
                if (p.Drivers != null)
                {
                    for (int i = 0; i < p.Drivers.Length; i++)
                    {
                        if (VisualParams.Params.ContainsKey(p.Drivers[i]))
                        {
                            VisualParam driver = VisualParams.Params[p.Drivers[i]];
                            if (driver.AlphaParams.HasValue && driver.AlphaParams.Value.TGAFile != string.Empty && !driver.IsBumpAttribute && !alphaMasks.ContainsKey(driver.AlphaParams.Value))
                            {
                                alphaMasks.Add(driver.AlphaParams.Value, kvp.Value);
                            }
                        }
                    }
                }
            }

            Color4 wearableColor = Color4.White; // Never actually used
            if (colorParams.Count > 0)
            {
                wearableColor = GetColorFromParams(colorParams);
                Logger.DebugLog("Setting tint " + wearableColor + " for " + wearable.WearableType);
            }

            // Loop through all of the texture IDs in this decoded asset and put them in our cache of worn textures
            foreach (KeyValuePair<AvatarTextureIndex, UUID> entry in wearable.Asset.Textures)
            {
                int i = (int)entry.Key;

                // Update information about color and alpha masks for this texture
                Textures[i].AlphaMasks = alphaMasks;
                Textures[i].Color = wearableColor;

                // If this texture changed, update the TextureID and clear out the old cached texture asset
                if (Textures[i].TextureID != entry.Value)
                {
                    // Treat DEFAULT_AVATAR_TEXTURE as null
                    if (entry.Value != AppearanceManager.DEFAULT_AVATAR_TEXTURE)
                        Textures[i].TextureID = entry.Value;
                    else
                        Textures[i].TextureID = UUID.Zero;

                    Textures[i].Texture = null;
                }
            }
        }

        /// <summary>
        /// Calculates base color/tint for a specific wearable
        /// based on its params
        /// </summary>
        /// <param name="param">All the color info gathered from wearable's VisualParams
        /// passed as list of ColorParamInfo tuples</param>
        /// <returns>Base color/tint for the wearable</returns>
        private Color4 GetColorFromParams(List<ColorParamInfo> param)
        {
            // Start off with a blank slate, black, fully transparent
            Color4 res = new Color4(0, 0, 0, 0);

            // Apply color modification from each color parameter
            foreach (ColorParamInfo p in param)
            {
                int n = p.VisualColorParam.Colors.Length;

                Color4 paramColor = new Color4(0, 0, 0, 0);

                if (n == 1)
                {
                    // We got only one color in this param, use it for application
                    // to the final color
                    paramColor = p.VisualColorParam.Colors[0];
                }
                else if (n > 1)
                {
                    // We have an array of colors in this parameter
                    // First, we need to find out, based on param value
                    // between which two elements of the array our value lands

                    // Size of the step using which we iterate from Min to Max
                    float step = (p.VisualParam.MaxValue - p.VisualParam.MinValue) / ((float)n - 1);

                    // Our color should land inbetween colors in the array with index a and b
                    int indexa = 0;
                    int indexb = 0;

                    int i = 0;

                    for (float a = p.VisualParam.MinValue; a <= p.VisualParam.MaxValue; a += step)
                    {
                        if (a <= p.Value)
                        {
                            indexa = i;
                        }
                        else
                        {
                            break;
                        }

                        i++;
                    }

                    // Sanity check that we don't go outside bounds of the array
                    if (indexa > n - 1)
                        indexa = n - 1;

                    indexb = (indexa == n - 1) ? indexa : indexa + 1;

                    // How far is our value from Index A on the 
                    // line from Index A to Index B
                    float distance = p.Value - (float)indexa * step;

                    // We are at Index A (allowing for some floating point math fuzz),
                    // use the color on that index
                    if (distance < 0.00001f || indexa == indexb)
                    {
                        paramColor = p.VisualColorParam.Colors[indexa];
                    }
                    else
                    {
                        // Not so simple as being precisely on the index eh? No problem.
                        // We take the two colors that our param value places us between
                        // and then find the value for each ARGB element that is
                        // somewhere on the line between color1 and color2 at some
                        // distance from the first color
                        Color4 c1 = paramColor = p.VisualColorParam.Colors[indexa];
                        Color4 c2 = paramColor = p.VisualColorParam.Colors[indexb];

                        // Distance is some fraction of the step, use that fraction
                        // to find the value in the range from color1 to color2
                        paramColor = Color4.Lerp(c1, c2, distance / step);
                    }

                    // Please leave this fragment even if its commented out
                    // might prove useful should ($deity forbid) there be bugs in this code
                    //string carray = "";
                    //foreach (Color c in p.VisualColorParam.Colors)
                    //{
                    //    carray += c.ToString() + " - ";
                    //}
                    //Logger.DebugLog("Calculating color for " + p.WearableType + " from " + p.VisualParam.Name + ", value is " + p.Value + " in range " + p.VisualParam.MinValue + " - " + p.VisualParam.MaxValue + " step " + step + " with " + n + " elements " + carray + " A: " + indexa + " B: " + indexb + " at distance " + distance);
                }

                // Now that we have calculated color from the scale of colors
                // that visual params provided, lets apply it to the result
                switch (p.VisualColorParam.Operation)
                {
                    case VisualColorOperation.Add:
                        res += paramColor;
                        break;
                    case VisualColorOperation.Multiply:
                        res *= paramColor;
                        break;
                    case VisualColorOperation.Blend:
                        res = Color4.Lerp(res, paramColor, p.Value);
                        break;
                }
            }

            return res;
        }

        /// <summary>
        /// Data collected from visual params for each wearable
        /// needed for the calculation of the color
        /// </summary>
        private struct ColorParamInfo
        {
            public VisualParam VisualParam;
            public VisualColorParam VisualColorParam;
            public float Value;
            public WearableType WearableType;
        }

        #endregion
    }
}