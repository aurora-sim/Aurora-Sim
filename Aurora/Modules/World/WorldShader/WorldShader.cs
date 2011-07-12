using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenSim.Framework;
using Aurora.Framework;
using Nini.Config;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;

namespace Aurora.Modules.World.WorldShader
{
    public class WorldShader : ISharedRegionModule
    {
        private bool initialized = false;
        public void Initialise (IConfigSource source)
        {
        }

        public void PostInitialise ()
        {
        }

        public void AddRegion (Scene scene)
        {
        }

        public void RegionLoaded (Scene scene)
        {
            if (MainConsole.Instance != null && !initialized)
                MainConsole.Instance.Commands.AddCommand ("shade world", "shade world", "Shades the world with a color", ShadeWorld);
            initialized = true;
        }

        public void RemoveRegion (Scene scene)
        {
        }

        public void Close ()
        {
        }

        public string Name
        {
            get
            {
                return "WorldShader";
            }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        private Dictionary<UUID, UUID> m_previouslyConverted = new Dictionary<UUID, UUID> ();
        public void ShadeWorld (string[] cmd)
        {
            if (MainConsole.Instance.ConsoleScene == null)
            {
                MainConsole.Instance.Output ("Select a scene first");
                return;
            }
            int R = int.Parse (MainConsole.Instance.CmdPrompt ("R color (0 - 255)"));
            int G = int.Parse (MainConsole.Instance.CmdPrompt ("G color (0 - 255)"));
            int B = int.Parse (MainConsole.Instance.CmdPrompt ("B color (0 - 255)"));
            float percent = float.Parse (MainConsole.Instance.CmdPrompt ("Percent to merge in the shade (0 - 100)"));
            Color shader = Color.FromArgb (R, G, B);

            IJ2KDecoder j2kDecoder = MainConsole.Instance.ConsoleScene.RequestModuleInterface<IJ2KDecoder>();
            ISceneEntity[] entities = MainConsole.Instance.ConsoleScene.Entities.GetEntities ();
            m_previouslyConverted.Clear ();
            foreach (ISceneEntity entity in entities)
            {
                foreach (ISceneChildEntity child in entity.ChildrenEntities ())
                {
                    UUID[] textures = GetTextures (child.Shape.Textures);
                    foreach (UUID t in textures)
                    {
                        if (m_previouslyConverted.ContainsKey (t))
                        {
                            child.Shape.Textures = SetTexture (child.Shape, m_previouslyConverted[t], t);
                        }
                        else
                        {
                            AssetBase a = MainConsole.Instance.ConsoleScene.AssetService.Get (t.ToString ());
                            if (a != null)
                            {
                                Bitmap texture = (Bitmap)j2kDecoder.DecodeToImage (a.Data);
                                if (texture == null)
                                    continue;
                                a.FullID = UUID.Random ();
                                texture = Shade (texture, shader, percent);
                                a.Data = OpenMetaverse.Imaging.OpenJPEG.EncodeFromImage (texture, false);
                                texture.Dispose ();
                                MainConsole.Instance.ConsoleScene.AssetService.Store (a);
                                child.Shape.Textures = SetTexture (child.Shape, a.FullID, t);
                                m_previouslyConverted.Add (t, a.FullID);
                                m_previouslyConverted.Add (a.FullID, a.FullID);
                            }
                        }
                    }
                }
            }
        }

        private Primitive.TextureEntry SetTexture (PrimitiveBaseShape shape, UUID newID, UUID oldID)
        {
            Primitive.TextureEntry oldShape = shape.Textures;
            Primitive.TextureEntry newShape;
            if (shape.Textures.DefaultTexture.TextureID == oldID)
                newShape = Copy (shape.Textures, newID);
            else
                newShape = Copy (shape.Textures, shape.Textures.DefaultTexture.TextureID);
                

            int i = 0;
            foreach (Primitive.TextureEntryFace face in shape.Textures.FaceTextures)
            {
                if (face != null)
                    if (face.TextureID == oldID)
                    {
                        Primitive.TextureEntryFace f = newShape.CreateFace ((uint)i);
                        CopyFace (oldShape.FaceTextures[i], f);
                        f.TextureID = newID;
                        newShape.FaceTextures[i] = f;
                    }
                i++;
            }
            return newShape;
        }

        private Primitive.TextureEntry Copy (Primitive.TextureEntry c, UUID id)
        {
            Primitive.TextureEntry Textures = new Primitive.TextureEntry (id);
            Textures.DefaultTexture = CopyFace (c.DefaultTexture, Textures.DefaultTexture);
            //for(int i = 0; i < c.FaceTextures.Length; i++)
            //{
            //    Textures.FaceTextures[i] = c.FaceTextures[i];
            //}
            return Textures;
        }

        private Primitive.TextureEntryFace CopyFace (Primitive.TextureEntryFace old, Primitive.TextureEntryFace face)
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

        private UUID[] GetTextures (Primitive.TextureEntry textureEntry)
        {
            List<UUID> textures = new List<UUID> ();
            foreach (Primitive.TextureEntryFace face in textureEntry.FaceTextures)
            {
                if (face != null)
                    textures.Add (face.TextureID);
            }
            textures.Add(textureEntry.DefaultTexture.TextureID);
            return textures.ToArray ();
        }

        public Bitmap Shade (Bitmap source, Color shade, float percent)
        {
            BitmapProcessing.FastBitmap b = new BitmapProcessing.FastBitmap (source);
            b.LockBitmap ();
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    Color c = b.GetPixel (x, y);
                    int luma = (int)(c.R * 0.3 + c.G * 0.59 + c.B * 0.11);
                    float amtFrom = 1 - percent;
                    int lumaR = (int)(c.R * amtFrom + 190 * percent);
                    int lumaG = (int)(c.G * amtFrom + 200 * percent);
                    int lumaB = (int)(c.B * amtFrom + 175 * percent);
                    b.SetPixel (x, y, Color.FromArgb (c.A, lumaR, lumaG, lumaB));
                }
            }
            b.UnlockBitmap ();
            return b.Bitmap();
        }
    }
}
