using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BitmapProcessing;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Imaging;
using Aurora.Framework;

namespace Aurora.Modules.WorldShader
{
    public class WorldShader : INonSharedRegionModule
    {
        private readonly Dictionary<UUID, UUID> m_previouslyConverted = new Dictionary<UUID, UUID>();
        private readonly Dictionary<UUID, UUID> m_revertConverted = new Dictionary<UUID, UUID>();
        private bool initialized;

        #region ISharedRegionModule Members

        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion(IScene scene)
        {
        }

        public void RegionLoaded(IScene scene)
        {
            if (MainConsole.Instance != null && !initialized)
            {
                MainConsole.Instance.Commands.AddCommand("revert shade world", "revert shade world",
                                                         "Reverts the shading of the world", RevertShadeWorld);
                MainConsole.Instance.Commands.AddCommand("shade world", "shade world", "Shades the world with a color",
                                                         ShadeWorld);
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
            get { return "WorldShader"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        public void RevertShadeWorld(string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene == null)
            {
                MainConsole.Instance.Output("Select a scene first");
                return;
            }
            ISceneEntity[] entities = MainConsole.Instance.ConsoleScene.Entities.GetEntities();
            foreach (ISceneEntity entity in entities)
            {
                foreach (ISceneChildEntity child in entity.ChildrenEntities())
                {
                    UUID[] textures = GetTextures(child.Shape.Textures);
                    foreach (UUID t in textures)
                    {
                        UUID oldID = t;
                        while (m_revertConverted.ContainsKey(oldID))
                        {
                            child.Shape.Textures = SetTexture(child.Shape, m_revertConverted[oldID], oldID);
                            oldID = m_revertConverted[oldID];
                        }
                    }
                }
            }
            m_revertConverted.Clear();
            m_previouslyConverted.Clear();
        }

        public void ShadeWorld(string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene == null)
            {
                MainConsole.Instance.Output("Select a scene first");
                return;
            }
            bool greyScale = MainConsole.Instance.Prompt("Greyscale (yes or no)?").ToLower() == "yes";
            int R = 0;
            int G = 0;
            int B = 0;
            float percent = 0;
            if (!greyScale)
            {
                R = int.Parse(MainConsole.Instance.Prompt("R color (0 - 255)"));
                G = int.Parse(MainConsole.Instance.Prompt("G color (0 - 255)"));
                B = int.Parse(MainConsole.Instance.Prompt("B color (0 - 255)"));
                percent = float.Parse(MainConsole.Instance.Prompt("Percent to merge in the shade (0 - 100)"));
            }
            if (percent > 1)
                percent /= 100;
            Color shader = Color.FromArgb(R, G, B);

            IJ2KDecoder j2kDecoder = MainConsole.Instance.ConsoleScene.RequestModuleInterface<IJ2KDecoder>();
            ISceneEntity[] entities = MainConsole.Instance.ConsoleScene.Entities.GetEntities();
            foreach (ISceneEntity entity in entities)
            {
                foreach (ISceneChildEntity child in entity.ChildrenEntities())
                {
                    UUID[] textures = GetTextures(child.Shape.Textures);
                    foreach (UUID t in textures)
                    {
                        if (m_previouslyConverted.ContainsKey(t))
                        {
                            child.Shape.Textures = SetTexture(child.Shape, m_previouslyConverted[t], t);
                        }
                        else
                        {
                            AssetBase a = MainConsole.Instance.ConsoleScene.AssetService.Get(t.ToString());
                            if (a != null)
                            {
                                Bitmap texture = (Bitmap) j2kDecoder.DecodeToImage(a.Data);
                                if (texture == null)
                                    continue;
                                a.ID = UUID.Random();
                                texture = Shade(texture, shader, percent, greyScale);
                                a.Data = OpenJPEG.EncodeFromImage(texture, false);
                                texture.Dispose();
                                a.ID = MainConsole.Instance.ConsoleScene.AssetService.Store(a);
                                child.Shape.Textures = SetTexture(child.Shape, a.ID, t);
                                m_previouslyConverted.Add(t, a.ID);
                                m_revertConverted.Add(a.ID, t);
                            }
                        }
                    }
                }
            }
        }

        private Primitive.TextureEntry SetTexture(PrimitiveBaseShape shape, UUID newID, UUID oldID)
        {
            Primitive.TextureEntry oldShape = shape.Textures;
            Primitive.TextureEntry newShape;
            newShape = shape.Textures.DefaultTexture.TextureID == oldID
                           ? Copy(shape.Textures, newID)
                           : Copy(shape.Textures, shape.Textures.DefaultTexture.TextureID);

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

        private UUID[] GetTextures(Primitive.TextureEntry textureEntry)
        {
            List<UUID> textures = (from face in textureEntry.FaceTextures where face != null select face.TextureID).ToList();
            textures.Add(textureEntry.DefaultTexture.TextureID);
            return textures.ToArray();
        }

        public Bitmap Shade(Bitmap source, Color shade, float percent, bool greyScale)
        {
            FastBitmap b = new FastBitmap(source);
            b.LockBitmap();
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    Color c = b.GetPixel(x, y);
                    if (greyScale)
                    {
                        int luma = (int) (c.R*0.3 + c.G*0.59 + c.B*0.11);
                        b.SetPixel(x, y, Color.FromArgb(c.A, luma, luma, luma));
                    }
                    else
                    {
                        float amtFrom = 1 - percent;
                        int lumaR = (int) (c.R*amtFrom + shade.R*percent);
                        int lumaG = (int) (c.G*amtFrom + shade.G*percent);
                        int lumaB = (int) (c.B*amtFrom + shade.B*percent);
                        b.SetPixel(x, y, Color.FromArgb(c.A, lumaR, lumaG, lumaB));
                    }
                }
            }
            b.UnlockBitmap();
            return b.Bitmap();
        }
    }
}