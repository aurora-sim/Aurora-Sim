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
    /// <summary>
    ///     Speed-Optimised Hybrid Erosion Brush
    ///     As per Jacob Olsen's Paper
    ///     http://www.oddlabs.com/download/terrain_generation.pdf
    /// </summary>
    public class OlsenSphere : ITerrainPaintableEffect
    {
        private const float nConst = 1024;
        private const NeighbourSystem type = NeighbourSystem.Moore;

        #region Supporting Functions

        private static int[] Neighbours(NeighbourSystem neighbourType, int index)
        {
            int[] coord = new int[2];

            index++;

            switch (neighbourType)
            {
                case NeighbourSystem.Moore:
                    switch (index)
                    {
                        case 1:
                            coord[0] = -1;
                            coord[1] = -1;
                            break;

                        case 2:
                            coord[0] = -0;
                            coord[1] = -1;
                            break;

                        case 3:
                            coord[0] = +1;
                            coord[1] = -1;
                            break;

                        case 4:
                            coord[0] = -1;
                            coord[1] = -0;
                            break;

                        case 5:
                            coord[0] = -0;
                            coord[1] = -0;
                            break;

                        case 6:
                            coord[0] = +1;
                            coord[1] = -0;
                            break;

                        case 7:
                            coord[0] = -1;
                            coord[1] = +1;
                            break;

                        case 8:
                            coord[0] = -0;
                            coord[1] = +1;
                            break;

                        case 9:
                            coord[0] = +1;
                            coord[1] = +1;
                            break;

                        default:
                            break;
                    }
                    break;

                case NeighbourSystem.VonNeumann:
                    switch (index)
                    {
                        case 1:
                            coord[0] = 0;
                            coord[1] = -1;
                            break;

                        case 2:
                            coord[0] = -1;
                            coord[1] = 0;
                            break;

                        case 3:
                            coord[0] = +1;
                            coord[1] = 0;
                            break;

                        case 4:
                            coord[0] = 0;
                            coord[1] = +1;
                            break;

                        case 5:
                            coord[0] = -0;
                            coord[1] = -0;
                            break;

                        default:
                            break;
                    }
                    break;
            }

            return coord;
        }

        private enum NeighbourSystem
        {
            Moore,
            VonNeumann
        };

        #endregion

        #region ITerrainPaintableEffect Members

        public void PaintEffect(ITerrainChannel map, UUID userID, float rx, float ry, float rz, float strength,
                                float duration, float BrushSize, List<IScene> scene)
        {
            strength = TerrainUtil.MetersToSphericalStrength(strength);

            int x;

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

            for (x = xFrom; x < xTo; x++)
            {
                int y;
                for (y = yFrom; y < yTo; y++)
                {
                    if (!map.Scene.Permissions.CanTerraformLand(userID, new Vector3(x, y, 0)))
                        continue;

                    float z = TerrainUtil.SphericalFactor(x, y, rx, ry, strength);

                    if (z > 0) // add in non-zero amount
                    {
                        const int NEIGHBOUR_ME = 4;
                        const int NEIGHBOUR_MAX = 9;

                        float max = float.MinValue;
                        int loc = 0;


                        for (int j = 0; j < NEIGHBOUR_MAX; j++)
                        {
                            if (j != NEIGHBOUR_ME)
                            {
                                int[] coords = Neighbours(type, j);

                                coords[0] += x;
                                coords[1] += y;

                                if (coords[0] > map.Width - 1)
                                    continue;
                                if (coords[1] > map.Height - 1)
                                    continue;
                                if (coords[0] < 0)
                                    continue;
                                if (coords[1] < 0)
                                    continue;

                                float cellmax = map[x, y] - map[coords[0], coords[1]];
                                if (cellmax > max)
                                {
                                    max = cellmax;
                                    loc = j;
                                }
                            }
                        }

                        float T = nConst/((map.Width + map.Height)/2);
                        // Apply results
                        if (0 < max && max <= T)
                        {
                            int[] maxCoords = Neighbours(type, loc);
                            float heightDelta = 0.5f*max*z*duration;
                            map[x, y] -= heightDelta;
                            map[x + maxCoords[0], y + maxCoords[1]] += heightDelta;
                        }
                    }
                }
            }
        }

        #endregion
    }
}