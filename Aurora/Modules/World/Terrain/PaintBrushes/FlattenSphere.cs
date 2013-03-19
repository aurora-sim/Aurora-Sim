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
using Aurora.Framework.Modules;
using Aurora.Framework.SceneInfo;
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.Modules.Terrain.PaintBrushes
{
    public class FlattenSphere : ITerrainPaintableEffect
    {
        #region ITerrainPaintableEffect Members

        public void PaintEffect(ITerrainChannel map, UUID userID, float rx, float ry, float rz, float strength,
                                float duration, float BrushSize, List<IScene> scene)
        {
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

            // blend in map
            for (x = xFrom; x < xTo; x++)
            {
                for (y = yFrom; y < yTo; y++)
                {
                    if (!map.Scene.Permissions.CanTerraformLand(userID, new Vector3(x, y, 0)))
                        continue;

                    float z;
                    if (duration < 4.0)
                    {
                        z = TerrainUtil.SphericalFactor(x, y, rx, ry, strength)*duration*0.25f;
                    }
                    else
                    {
                        z = 1;
                    }

                    float delta = rz - map[x, y];
                    if (Math.Abs(delta) > 0.1)
                    {
                        if (z > 1)
                        {
                            z = 1;
                        }
                        else if (z < 0)
                        {
                            z = 0;
                        }
                        delta *= z;
                    }

                    if (delta != 0) // add in non-zero amount
                    {
                        map[x, y] += delta;
                    }
                }
            }
        }

        #endregion
    }
}