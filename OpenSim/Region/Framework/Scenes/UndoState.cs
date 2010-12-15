/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
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

using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Framework;

namespace OpenSim.Region.Framework.Scenes
{
    public class UndoState
    {
        public Vector3 Position = Vector3.Zero;
        public Vector3 Scale = Vector3.Zero;
        public Quaternion Rotation = Quaternion.Identity;

        public UndoState(SceneObjectPart part)
        {
            if (part != null)
            {
                if (part.UUID == part.ParentGroup.UUID)
                {
                    Position = part.ParentGroup.AbsolutePosition;
                    Rotation = part.RotationOffset;
                    Scale = part.Shape.Scale;
                }
                else
                {
                    Position = part.OffsetPosition;
                    Rotation = part.RotationOffset;
                    Scale = part.Shape.Scale;
                }
            }
        }

        public bool Compare(SceneObjectPart part)
        {
            if (part != null)
            {
                if (part.UUID == part.ParentGroup.UUID)
                {
                    if (Position == part.AbsolutePosition && Rotation == part.RotationOffset && Scale == part.Shape.Scale)
                        return true;
                    else
                        return false;
                }
                else
                {
                    if (Position == part.OffsetPosition && Rotation == part.RotationOffset && Scale == part.Shape.Scale)
                        return true;
                    else
                        return false;

                }
            }
            return false;
        }

        public void PlaybackState(SceneObjectPart part)
        {
            if (part != null)
            {
                part.Undoing = true;

                bool ChangedScale = false;
                bool ChangedRot = false;
                bool ChangedPos = false;

                if (part.UUID == part.ParentGroup.UUID)
                {
                    if (Position != Vector3.Zero)
                    {
                        ChangedPos = true;
                        part.ParentGroup.AbsolutePosition = Position;
                    }
                    ChangedRot = true;
                    part.RotationOffset = Rotation;
                    if (Scale != Vector3.Zero)
                    {
                        ChangedScale = true;
                        part.Scale = Scale;
                    }

                    foreach (SceneObjectPart child in part.ParentGroup.ChildrenList)
                    {
                        if(child.UUID != part.UUID)
                            child.Undo(); //No updates here, child undo will do it on their own
                    }
                }
                else
                {
                    if (Position != Vector3.Zero)
                    {
                        ChangedPos = true;
                        part.OffsetPosition = Position;
                    }
                    ChangedRot = true;
                    part.UpdateRotation(Rotation);
                    if (Scale != Vector3.Zero)
                    {
                        ChangedScale = true;
                        part.Resize(Scale);
                    }
                }
                part.Undoing = false;
                part.ScheduleUpdate((ChangedScale ? PrimUpdateFlags.Shape : PrimUpdateFlags.None) |
                    (ChangedPos ? PrimUpdateFlags.Position : PrimUpdateFlags.None) |
                    (ChangedRot ? PrimUpdateFlags.Rotation : PrimUpdateFlags.None));
            }
        }
        public void PlayfwdState(SceneObjectPart part)
        {
            if (part != null)
            {
                bool ChangedScale = false;
                bool ChangedRot = false;
                bool ChangedPos = false;
                part.Undoing = true;

                if (part.UUID == part.ParentGroup.UUID)
                {
                    if (Position != Vector3.Zero)
                    {
                        ChangedPos = true;
                        part.ParentGroup.AbsolutePosition = Position;
                    }
                    if (Rotation != Quaternion.Identity)
                    {
                        ChangedRot = true;
                        part.UpdateRotation(Rotation);
                    }
                    if (Scale != Vector3.Zero)
                    {
                        ChangedScale = true;
                        part.Resize(Scale);
                    }

                    foreach (SceneObjectPart child in part.ParentGroup.ChildrenList)
                    {
                        if (child.UUID != part.UUID)
                            child.Redo(); //No updates here, child redo will do it on their own
                    }
                }
                else
                {
                    if (Position != Vector3.Zero)
                    {
                        ChangedPos = true;
                        part.OffsetPosition = Position;
                    }
                    if (Rotation != Quaternion.Identity)
                    {
                        ChangedRot = true;
                        part.ParentGroup.Rotation = (Rotation);
                    }
                    if (Scale != Vector3.Zero)
                    {
                        ChangedScale = true;
                        part.Resize(Scale);
                    }
                }

                part.ScheduleUpdate((ChangedScale ? PrimUpdateFlags.Shape : PrimUpdateFlags.None) |
                    (ChangedPos ? PrimUpdateFlags.Position : PrimUpdateFlags.None) | 
                    (ChangedRot ? PrimUpdateFlags.Rotation : PrimUpdateFlags.None));
                part.Undoing = false;
            }
        }
    }
    public class LandUndoState
    {
        public ITerrainModule m_terrainModule;
        public ITerrainChannel m_terrainChannel;

        public LandUndoState(ITerrainModule terrainModule, ITerrainChannel terrainChannel)
        {
            m_terrainModule = terrainModule;
            m_terrainChannel = terrainChannel;
        }

        public bool Compare(ITerrainChannel terrainChannel)
        {
            if (m_terrainChannel != terrainChannel)
                return false;
            else
                return false;
        }

        public void PlaybackState()
        {
            m_terrainModule.UndoTerrain(m_terrainChannel);
        }
    }
}
