using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;

namespace Aurora.Modules.Assets
{
    public class BlackholeRegionCommands : ISharedRegionModule
    {
        private bool initialized;

        #region ISharedRegionModule Members

        public void Initialise(IConfigSource source)
        {
        }

        public void PostInitialise()
        {
        }

        public void AddRegion(IScene scene)
        {
        }

        public void RegionLoaded(IScene scene)
        {
            if (MainConsole.Instance != null && !initialized)
            {
                MainConsole.Instance.Commands.AddCommand("assets optimise", "assets optimise",
                                                         "Reduces the number of assets a region displays", OptimiseWorld);
                MainConsole.Instance.Commands.AddCommand("assets details", "assets details",
                                                         "Learn about the details of the assets used on this region",
                                                         AssetDetails);
            }
            initialized = true;
        }

        public void RemoveRegion(IScene scene)
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "BlackholeRegionCommands"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        public void AssetDetails(string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene == null)
            {
                MainConsole.Instance.Output("Select a scene first");
                return;
            }

            int totalBytes = 0;
            int totalTextures = 0;
            int couldSave = 0;
            int totalChagnesTextures = 0;
            int totalInventory = 0;
            int totalChangesInventory = 0;
            int scriptCount = 0;
            int objectCount = 0;

            List<UUID> converted = new List<UUID>();

            NameValueCollection allAssetCount = new NameValueCollection();
            List<UUID> allAssetIDLookup = new List<UUID>();
            List<AssetBase> allAssets = new List<AssetBase>();

            int counter = 0;

            ISceneEntity[] entities = MainConsole.Instance.ConsoleScene.Entities.GetEntities();
            foreach (ISceneEntity entity in entities)
                foreach (ISceneChildEntity child in entity.ChildrenEntities())
                {
                    objectCount++;
                    IEnumerable<UUID> textures = GetTextures(child.Shape.Textures);
                    foreach (UUID t in textures)
                    {
                        AssetBase ass = MainConsole.Instance.ConsoleScene.AssetService.Get(t.ToString());
                        if (ass != null)
                        {
                            if (!allAssetIDLookup.Contains(ass.ID))
                            {
                                totalTextures++;
                                totalBytes += ass.Data.Length;
                                allAssetIDLookup.Add(ass.ID);
                                ass.Description = ass.Data.Length.ToString();
                                ass.Data = new byte[] {};
                                allAssets.Add(ass);
                                allAssetCount.Add(ass.ID.ToString(), "1");
                                counter++;
                            }
                            else
                            {
                                allAssetCount[ass.ID.ToString()] = (int.Parse(allAssetCount[ass.ID.ToString()]) + 1).ToString();
                            }
                            if ((ass.ParentID != UUID.Zero) && (ass.ParentID != ass.ID))
                            {
                                if (converted.Contains(ass.ParentID))
                                    couldSave += ass.Data.Length;
                                else
                                    converted.Add(ass.ParentID);
                                totalChagnesTextures++;
                            }
                        }
                    }
                    foreach (TaskInventoryItem inventoryItem in child.Inventory.GetInventoryItems())
                    {
                        totalInventory++;
                        AssetBase ass2 = MainConsole.Instance.ConsoleScene.AssetService.Get(inventoryItem.AssetID.ToString());
                        if (ass2 != null)
                        {
                            if ((ass2 != null) && (ass2.ParentID != UUID.Zero) && (ass2.ParentID != ass2.ID))
                                totalChangesInventory++;
                            if ((ass2.TypeAsset == AssetType.LSLText) || (ass2.TypeAsset == AssetType.LSLBytecode))
                                scriptCount++;
                        }
                    }
                }

            AssetBase[] SortResults = new AssetBase[] {};
            SortResults = SortAssetArray(allAssets.ToArray(), SortResults, 0);

            MainConsole.Instance.Info("[BlkHolAssets] Total Texture Bytes " + totalBytes);
            MainConsole.Instance.Info("[BlkHolAssets] Total Texture Kilobyte " + (totalBytes/1024.0));
            MainConsole.Instance.Info("[BlkHolAssets] Total Texture Megabyte " + (totalBytes/1048576.0));
            MainConsole.Instance.Info("[BlkHolAssets] " + totalTextures + " textures on region");
            MainConsole.Instance.Info("[BlkHolAssets] " + totalChagnesTextures + " textures could be optimised");
            MainConsole.Instance.Info("[BlkHolAssets] " + totalInventory + " item in object inventory");
            MainConsole.Instance.Info("[BlkHolAssets] " + scriptCount + " scripts in object inventory");
            MainConsole.Instance.Info("Largest Textures Top 10");
            MainConsole.Instance.Info("UUID                                 Size");
            MainConsole.Instance.Info("--------------------------------------------------------------");

            int loopTo = 9;
            if (SortResults.Count() <= 9)
                loopTo = SortResults.Count();
            for (int looper = 0; looper <= loopTo; looper++)
            {
                double mbsize = Math.Round((int.Parse(SortResults[looper].Description)/1048576.0)*100.0)/100.0;

                MainConsole.Instance.Info(SortResults[looper].ID + " " + mbsize + "MB");
            }

            MainConsole.Instance.Info("[BlkHolAssets] Nothing escapes the BlackHole!");
        }

        private string SetUpName(string name)
        {
            if (name.Length >= 36) return name.Substring(0, 36);
            for (int looper = name.Length; looper <= 36; looper++)
            {
                name += " ";
            }
            return name;
        }

        private AssetBase[] SortAssetArray(AssetBase[] toArray, AssetBase[] results, int onNow)
        {
            if (onNow == 0)
            {
                results = new AssetBase[toArray.Length];
            }

            int currentKing = 0;
            int count = 0;
            foreach (AssetBase ab in toArray)
            {
                if (int.Parse(ab.Description) >= int.Parse(toArray[currentKing].Description))
                    currentKing = count;
                count++;
            }
            if (results != null)
                results[onNow] = toArray[currentKing];
            List<AssetBase> toArrayTemp = toArray.ToList();
            toArrayTemp.RemoveAt(currentKing);
            toArray = toArrayTemp.ToArray();
            if (toArray.Length >= 1)
                return SortAssetArray(toArray, results, ++onNow);
            return results;
        }

        public void OptimiseWorld(string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene == null)
            {
                MainConsole.Instance.Output("Select a scene first");
                return;
            }

            int savedBytes = 0;
            int totalTextures = 0;
            int totalChagnesTextures = 0;
            int totalInventory = 0;
            int totalChangesInventory = 0;
            List<UUID> converted = new List<UUID>();

            ISceneEntity[] entities = MainConsole.Instance.ConsoleScene.Entities.GetEntities();
            foreach (ISceneEntity entity in entities)
            {
                foreach (ISceneChildEntity child in entity.ChildrenEntities())
                {
                    IEnumerable<UUID> textures = GetTextures(child.Shape.Textures);
                    foreach (UUID t in textures)
                    {
                        totalTextures++;
                        AssetBase ass = MainConsole.Instance.ConsoleScene.AssetService.Get(t.ToString());
                        if ((ass != null) && (ass.ParentID != UUID.Zero) && (ass.ParentID != ass.ID))
                        {
                            if (converted.Contains(ass.ParentID))
                                savedBytes += ass.Data.Length;
                            else
                                converted.Add(ass.ParentID);
                            totalChagnesTextures++;
                            child.Shape.Textures = SetTexture(child.Shape, ass.ParentID, t);
                            entity.HasGroupChanged = true;
                        }
                    }
                    foreach (TaskInventoryItem inventoryItem in child.Inventory.GetInventoryItems())
                    {
                        totalInventory++;
                        AssetBase ass2 =
                            MainConsole.Instance.ConsoleScene.AssetService.Get(inventoryItem.AssetID.ToString());
                        if ((ass2 != null) && (ass2.ParentID != UUID.Zero) && (ass2.ParentID != ass2.ID))
                        {
                            totalChangesInventory++;
                            inventoryItem.AssetID = ass2.ParentID;
                            child.Inventory.UpdateInventoryItem(inventoryItem);
                            entity.HasGroupChanged = true;
                        }
                        if (inventoryItem.InvType == (int) InventoryType.LSL)
                        {
                            // search for assets keys and replace if needed ??
                        }
                    }
                }
            }
            MainConsole.Instance.Info("[BlkHolAssets] Bytes Saved " + savedBytes);
            MainConsole.Instance.Info("[BlkHolAssets] Kilobyte Saved " + (savedBytes/1024.0f));
            MainConsole.Instance.Info("[BlkHolAssets] Megabyte Saved " + (savedBytes/1048576.0f));
            MainConsole.Instance.Info("[BlkHolAssets] " + totalChagnesTextures + " out of " + totalTextures + " textures changed");
            MainConsole.Instance.Info("[BlkHolAssets] " + totalChangesInventory + " out of " + totalInventory +
                       " inventory items changed");
            MainConsole.Instance.Info("[BlkHolAssets] Your are optimised.. Nothing escapes the BlackHole!");
        }

        private Primitive.TextureEntry SetTexture(PrimitiveBaseShape shape, UUID newID, UUID oldID)
        {
            Primitive.TextureEntry oldShape = shape.Textures;
            Primitive.TextureEntry newShape;
            newShape = Copy(shape.Textures,
                            shape.Textures.DefaultTexture.TextureID == oldID
                                ? newID
                                : shape.Textures.DefaultTexture.TextureID);

            int i = 0;
            foreach (Primitive.TextureEntryFace face in shape.Textures.FaceTextures)
            {
                if (face != null)
                    if (face.TextureID == oldID)
                    {
                        Primitive.TextureEntryFace f = newShape.CreateFace((uint) i);
                        CopyFace(oldShape.FaceTextures[i], f);
                        f.TextureID = newID;
                        newShape.FaceTextures[i] = f;
                    }
                    else
                    {
                        Primitive.TextureEntryFace f = newShape.CreateFace((uint) i);
                        CopyFace(oldShape.FaceTextures[i], f);
                        f.TextureID = oldShape.FaceTextures[i].TextureID;
                        newShape.FaceTextures[i] = f;
                    }
                i++;
            }
            return newShape;
        }

        private Primitive.TextureEntry Copy(Primitive.TextureEntry c, UUID id)
        {
            Primitive.TextureEntry Textures = new Primitive.TextureEntry(id);
            Textures.DefaultTexture = CopyFace(c.DefaultTexture, Textures.DefaultTexture);
            //for(int i = 0; i < c.FaceTextures.Length; i++)
            //{
            //    Textures.FaceTextures[i] = c.FaceTextures[i];
            //}
            return Textures;
        }

        private Primitive.TextureEntryFace CopyFace(Primitive.TextureEntryFace old, Primitive.TextureEntryFace face)
        {
            face.Bump = old.Bump;
            face.Fullbright = old.Fullbright;
            face.Glow = old.Glow;
            face.MediaFlags = old.MediaFlags;
            face.OffsetU = old.OffsetU;
            face.OffsetV = old.OffsetV;
            face.RepeatU = old.RepeatU;
            face.RepeatV = old.RepeatV;
            face.RGBA = old.RGBA;
            face.Rotation = old.Rotation;
            face.Shiny = old.Shiny;
            face.TexMapType = old.TexMapType;
            return face;
        }

        private IEnumerable<UUID> GetTextures(Primitive.TextureEntry textureEntry)
        {
            List<UUID> textures =
                (from face in textureEntry.FaceTextures where face != null select face.TextureID).ToList();
            textures.Add(textureEntry.DefaultTexture.TextureID);
            return textures.ToArray();
        }
    }
}