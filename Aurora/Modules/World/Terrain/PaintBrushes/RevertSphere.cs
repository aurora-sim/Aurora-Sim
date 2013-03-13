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
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.Modules.Terrain.PaintBrushes
{
    public class RevertSphere : ITerrainPaintableEffect
    {
        private readonly ITerrainModule m_module;

        public RevertSphere(ITerrainModule module)
        {
            m_module = module;
        }

        #region ITerrainPaintableEffect Members

        public void PaintEffect(ITerrainChannel map, UUID userID, float rx, float ry, float rz, float strength,
                                float duration, float BrushSize, List<IScene> scene)
        {
            if (m_module == null)
                return;
            strength = TerrainUtil.MetersToSphericalStrength(BrushSize);
            duration = 0.03f; //MCP Should be read from ini file

            if (duration > 1.0)
                duration = 1;
            if (duration < 0)
                return;

            int n = (int) (BrushSize + 0.5f);
            int zx = (int) (rx + 0.5);
            int zy = (int) (ry + 0.5);

            int dx;
            for (dx = -n; dx <= n; dx++)
            {
                int dy;
                for (dy = -n; dy <= n; dy++)
                {
                    int x = zx + dx;
                    int y = zy + dy;
                    if (x >= 0 && y >= 0 && x < map.Width && y < map.Height)
                    {
                        if (!map.Scene.Permissions.CanTerraformLand(userID, new Vector3(x, y, 0)))
                            continue;

                        // Calculate a sphere and add it to the heighmap
                        float z = 0;
                        if (duration < 4.0)
                            z = TerrainUtil.SphericalFactor(x, y, rx, ry, strength)*duration*0.25f;

                        if (z > 0.0)
                        {
                            float s = strength*0.025f;
                            map[x, y] = (map[x, y]*(1 - s)) + (m_module.TerrainRevertMap[x, y]*s);
                        }
                    }
                }
            }
        }

        #endregion
    }
}