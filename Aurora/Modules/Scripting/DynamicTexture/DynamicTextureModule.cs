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
using Aurora.Framework.Modules;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Services.ClassHelpers.Assets;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace Aurora.Modules.Scripting
{
    public class DynamicTextureModule : INonSharedRegionModule, IDynamicTextureManager
    {
        //private static readonly ILog MainConsole.Instance = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const int ALL_SIDES = -1;

        public const int DISP_EXPIRE = 1;
        public const int DISP_TEMP = 2;

        private readonly Dictionary<UUID, IScene> RegisteredScenes = new Dictionary<UUID, IScene>();

        private readonly Dictionary<string, IDynamicTextureRender> RenderPlugins =
            new Dictionary<string, IDynamicTextureRender>();

        private readonly Dictionary<UUID, DynamicTextureUpdater> Updaters =
            new Dictionary<UUID, DynamicTextureUpdater>();

        #region IDynamicTextureManager Members

        public void RegisterRender(string handleType, IDynamicTextureRender render)
        {
            if (!RenderPlugins.ContainsKey(handleType))
            {
                RenderPlugins.Add(handleType, render);
            }
        }

        /// <summary>
        ///     Called by code which actually renders the dynamic texture to supply texture data.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        public void ReturnData(UUID id, byte[] data)
        {
            DynamicTextureUpdater updater = null;

            lock (Updaters)
            {
                if (Updaters.ContainsKey(id))
                {
                    updater = Updaters[id];
                }
            }

            if (updater != null)
            {
                if (RegisteredScenes.ContainsKey(updater.SimUUID))
                {
                    IScene scene = RegisteredScenes[updater.SimUUID];
                    updater.DataReceived(data, scene);
                }
            }

            if (updater.UpdateTimer == 0)
            {
                lock (Updaters)
                {
                    if (!Updaters.ContainsKey(updater.UpdaterID))
                    {
                        Updaters.Remove(updater.UpdaterID);
                    }
                }
            }
        }

        public UUID AddDynamicTextureURL(UUID simID, UUID primID, UUID oldAssetID, string contentType, string url,
                                         string extraParams, int updateTimer)
        {
            return AddDynamicTextureURL(simID, primID, oldAssetID, contentType, url, extraParams, updateTimer, false,
                                        255);
        }

        public UUID AddDynamicTextureURL(UUID simID, UUID primID, UUID oldAssetID, string contentType, string url,
                                         string extraParams, int updateTimer, bool SetBlending, byte AlphaValue)
        {
            return AddDynamicTextureURL(simID, primID, oldAssetID, contentType, url,
                                        extraParams, updateTimer, SetBlending,
                                        (DISP_TEMP | DISP_EXPIRE), AlphaValue, ALL_SIDES);
        }

        public UUID AddDynamicTextureURL(UUID simID, UUID primID, UUID oldAssetID, string contentType, string url,
                                         string extraParams, int updateTimer, bool SetBlending,
                                         int disp, byte AlphaValue, int face)
        {
            if (RenderPlugins.ContainsKey(contentType))
            {
                DynamicTextureUpdater updater = new DynamicTextureUpdater
                                                    {
                                                        SimUUID = simID,
                                                        PrimID = primID,
                                                        ContentType = contentType,
                                                        Url = url,
                                                        UpdateTimer = updateTimer,
                                                        UpdaterID = UUID.Random(),
                                                        Params = extraParams,
                                                        BlendWithOldTexture = SetBlending,
                                                        FrontAlpha = AlphaValue,
                                                        Face = face,
                                                        Disp = disp,
                                                        LastAssetID = oldAssetID
                                                    };

                lock (Updaters)
                {
                    if (!Updaters.ContainsKey(updater.UpdaterID))
                    {
                        Updaters.Add(updater.UpdaterID, updater);
                    }
                }

                RenderPlugins[contentType].AsyncConvertUrl(updater.UpdaterID, url, extraParams);
                return updater.UpdaterID;
            }
            return UUID.Zero;
        }

        public UUID AddDynamicTextureData(UUID simID, UUID primID, UUID oldAssetID, string contentType, string data,
                                          string extraParams, int updateTimer)
        {
            return AddDynamicTextureData(simID, primID, oldAssetID, contentType, data, extraParams, updateTimer, false,
                                         255);
        }

        public UUID AddDynamicTextureData(UUID simID, UUID primID, UUID oldAssetID, string contentType, string data,
                                          string extraParams, int updateTimer, bool SetBlending, byte AlphaValue)
        {
            return AddDynamicTextureData(simID, primID, oldAssetID, contentType, data, extraParams, updateTimer,
                                         SetBlending,
                                         (DISP_TEMP | DISP_EXPIRE), AlphaValue, ALL_SIDES);
        }

        public UUID AddDynamicTextureData(UUID simID, UUID primID, UUID oldAssetID, string contentType, string data,
                                          string extraParams, int updateTimer, bool SetBlending, int disp,
                                          byte AlphaValue, int face)
        {
            if (RenderPlugins.ContainsKey(contentType))
            {
                DynamicTextureUpdater updater = new DynamicTextureUpdater
                                                    {
                                                        SimUUID = simID,
                                                        PrimID = primID,
                                                        ContentType = contentType,
                                                        BodyData = data,
                                                        UpdateTimer = updateTimer,
                                                        UpdaterID = UUID.Random(),
                                                        Params = extraParams,
                                                        BlendWithOldTexture = SetBlending,
                                                        FrontAlpha = AlphaValue,
                                                        Face = face,
                                                        Url = "Local image",
                                                        Disp = disp,
                                                        LastAssetID = oldAssetID
                                                    };

                lock (Updaters)
                {
                    if (!Updaters.ContainsKey(updater.UpdaterID))
                    {
                        Updaters.Add(updater.UpdaterID, updater);
                    }
                }

                RenderPlugins[contentType].AsyncConvertData(updater.UpdaterID, data, extraParams);
                return updater.UpdaterID;
            }
            return UUID.Zero;
        }

        public void GetDrawStringSize(string contentType, string text, string fontName, int fontSize,
                                      out double xSize, out double ySize)
        {
            xSize = 0;
            ySize = 0;
            if (RenderPlugins.ContainsKey(contentType))
            {
                RenderPlugins[contentType].GetDrawStringSize(text, fontName, fontSize, out xSize, out ySize);
            }
        }

        #endregion

        #region INonSharedRegionModule Members

        public void Initialise(IConfigSource config)
        {
        }

        public void AddRegion(IScene scene)
        {
            if (!RegisteredScenes.ContainsKey(scene.RegionInfo.RegionID))
            {
                RegisteredScenes.Add(scene.RegionInfo.RegionID, scene);
                scene.RegisterModuleInterface<IDynamicTextureManager>(this);
            }
        }

        public void RemoveRegion(IScene scene)
        {
        }

        public void RegionLoaded(IScene scene)
        {
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "DynamicTextureModule"; }
        }

        #endregion

        #region Nested type: DynamicTextureUpdater

        public class DynamicTextureUpdater
        {
            public bool BlendWithOldTexture;
            public string BodyData;
            public string ContentType;
            public int Disp;
            public int Face;
            public byte FrontAlpha = 255;
            public UUID LastAssetID = UUID.Zero;
            public string Params;
            public UUID PrimID;
            public bool SetNewFrontAlpha;
            public UUID SimUUID;
            public int UpdateTimer;
            public UUID UpdaterID;
            public string Url;

            public DynamicTextureUpdater()
            {
                UpdateTimer = 0;
                BodyData = null;
            }

            /// <summary>
            ///     Called once new texture data has been received for this updater.
            /// </summary>
            public void DataReceived(byte[] data, IScene scene)
            {
                ISceneChildEntity part = scene.GetSceneObjectPart(PrimID);

                if (part == null || data == null || data.Length <= 1)
                {
                    string msg =
                        String.Format("DynamicTextureModule: Error preparing image using URL {0}", Url);
                    IChatModule chatModule = scene.RequestModuleInterface<IChatModule>();
                    if (chatModule != null)
                        chatModule.SimChat(msg, ChatTypeEnum.Say, 0,
                                           part.ParentEntity.AbsolutePosition, part.Name, part.UUID, false, scene);
                    return;
                }

                byte[] assetData = null;
                AssetBase oldAsset = null;

                if (BlendWithOldTexture)
                {
                    Primitive.TextureEntryFace defaultFace = part.Shape.Textures.DefaultTexture;
                    if (defaultFace != null)
                    {
                        oldAsset = scene.AssetService.Get(defaultFace.TextureID.ToString());

                        if (oldAsset != null)
                            assetData = BlendTextures(data, oldAsset.Data, SetNewFrontAlpha, FrontAlpha, scene);
                    }
                }

                if (assetData == null)
                {
                    assetData = new byte[data.Length];
                    Array.Copy(data, assetData, data.Length);
                }

                AssetBase asset = null;

                if (LastAssetID != UUID.Zero)
                {
                    asset = scene.AssetService.Get(LastAssetID.ToString());
                    asset.Description = String.Format("URL image : {0}", Url);
                    asset.Data = assetData;
                    if ((asset.Flags & AssetFlags.Local) == AssetFlags.Local)
                    {
                        asset.Flags = asset.Flags & ~AssetFlags.Local;
                    }
                    if (((asset.Flags & AssetFlags.Temporary) == AssetFlags.Temporary) != ((Disp & DISP_TEMP) != 0))
                    {
                        if ((Disp & DISP_TEMP) != 0) asset.Flags |= AssetFlags.Temporary;
                        else asset.Flags = asset.Flags & ~AssetFlags.Temporary;
                    }
                    asset.ID = scene.AssetService.Store(asset);
                }
                else
                {
                    // Create a new asset for user
                    asset = new AssetBase(UUID.Random(), "DynamicImage" + Util.RandomClass.Next(1, 10000),
                                          AssetType.Texture,
                                          scene.RegionInfo.RegionID)
                                {Data = assetData, Description = String.Format("URL image : {0}", Url)};
                    if ((Disp & DISP_TEMP) != 0) asset.Flags = AssetFlags.Temporary;
                    asset.ID = scene.AssetService.Store(asset);
                }

                IJ2KDecoder cacheLayerDecode = scene.RequestModuleInterface<IJ2KDecoder>();
                if (cacheLayerDecode != null)
                {
                    cacheLayerDecode.Decode(asset.ID, asset.Data);
                    cacheLayerDecode = null;
                    LastAssetID = asset.ID;
                }

                UUID oldID = UUID.Zero;

                lock (part)
                {
                    // mostly keep the values from before
                    Primitive.TextureEntry tmptex = part.Shape.Textures;

                    // remove the old asset from the cache
                    oldID = tmptex.DefaultTexture.TextureID;

                    if (Face == ALL_SIDES)
                    {
                        tmptex.DefaultTexture.TextureID = asset.ID;
                    }
                    else
                    {
                        try
                        {
                            Primitive.TextureEntryFace texface = tmptex.CreateFace((uint) Face);
                            texface.TextureID = asset.ID;
                            tmptex.FaceTextures[Face] = texface;
                        }
                        catch (Exception)
                        {
                            tmptex.DefaultTexture.TextureID = asset.ID;
                        }
                    }

                    // I'm pretty sure we always want to force this to true
                    // I'm pretty sure noone whats to set fullbright true if it wasn't true before.
                    // tmptex.DefaultTexture.Fullbright = true;

                    part.UpdateTexture(tmptex, true);
                }

                if (oldID != UUID.Zero && ((Disp & DISP_EXPIRE) != 0))
                {
                    if (oldAsset == null) oldAsset = scene.AssetService.Get(oldID.ToString());
                    if (oldAsset != null)
                    {
                        if ((oldAsset.Flags & AssetFlags.Temporary) == AssetFlags.Temporary)
                        {
                            scene.AssetService.Delete(oldID);
                        }
                    }
                }
            }

            private byte[] BlendTextures(byte[] frontImage, byte[] backImage, bool setNewAlpha, byte newAlpha,
                                         IScene scene)
            {
                Image image = scene.RequestModuleInterface<IJ2KDecoder>().DecodeToImage(frontImage);

                if (image != null)
                {
                    Bitmap image1 = new Bitmap(image);

                    image = scene.RequestModuleInterface<IJ2KDecoder>().DecodeToImage(backImage);
                    if (image != null)
                    {
                        Bitmap image2 = new Bitmap(image);

                        if (setNewAlpha)
                            SetAlpha(ref image1, newAlpha);

                        Bitmap joint = MergeBitMaps(image1, image2);

                        byte[] result = new byte[0];

                        try
                        {
                            result = OpenJPEG.EncodeFromImage(joint, true);
                        }
                        catch (Exception)
                        {
                            MainConsole.Instance.Error(
                                "[DYNAMICTEXTUREMODULE]: OpenJpeg Encode Failed.  Empty byte data returned!");
                        }

                        return result;
                    }
                }

                return null;
            }

            public Bitmap MergeBitMaps(Bitmap front, Bitmap back)
            {
                Bitmap joint;
                Graphics jG;

                joint = new Bitmap(back.Width, back.Height, PixelFormat.Format32bppArgb);
                jG = Graphics.FromImage(joint);

                jG.DrawImage(back, 0, 0, back.Width, back.Height);
                jG.DrawImage(front, 0, 0, back.Width, back.Height);

                return joint;
            }

            private void SetAlpha(ref Bitmap b, byte alpha)
            {
                for (int w = 0; w < b.Width; w++)
                {
                    for (int h = 0; h < b.Height; h++)
                    {
                        b.SetPixel(w, h, Color.FromArgb(alpha, b.GetPixel(w, h)));
                    }
                }
            }
        }

        #endregion
    }
}