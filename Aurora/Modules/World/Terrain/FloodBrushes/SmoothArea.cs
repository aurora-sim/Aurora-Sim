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
using Aurora.Framework.Modules;
using Aurora.Framework.SceneInfo;
using OpenMetaverse;
using Aurora.Framework;

namespace Aurora.Modules.Terrain.FloodBrushes
{
    public class SmoothArea : ITerrainFloodEffect
    {
        #region ITerrainFloodEffect Members

        public void FloodEffect(ITerrainChannel map, UUID userID, float north,
                                float west, float south, float east, float strength)
        {
            float area = strength;
            float step = strength/4;

            for (int x = (int) west; x < (int) east; x++)
            {
                for (int y = (int) south; y < (int) north; y++)
                {
                    if (!map.Scene.Permissions.CanTerraformLand(userID, new Vector3(x, y, 0)))
                        continue;

                    float average = 0;
                    int avgsteps = 0;

                    float n;
                    for (n = 0 - area; n < area; n += step)
                    {
                        float l;
                        for (l = 0 - area; l < area; l += step)
                        {
                            avgsteps++;
                            average += TerrainUtil.GetBilinearInterpolate(x + n, y + l, map, new List<IScene>());
                        }
                    }

                    map[x, y] = average/avgsteps;
                }
            }
        }

        #endregion
    }
}