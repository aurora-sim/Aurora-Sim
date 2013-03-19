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

using System;

namespace Aurora.BotManager
{
    /// <summary>
    ///     Created by Christy Lock
    /// </summary>
    internal class SimBots
    {
        public static double target;

        /// <summary>
        ///     This is used to seacrh the current map for
        ///     Items that the bot needs. Like when he is hungry he will search for 9 on the map
        ///     Fun is 7 and Comfort is 8 etc.
        ///     This returns the x and y to BotMe and then he passes them on to Astar and builds a list of waypoints to
        ///     Reach the goal.
        /// </summary>
        public static double distTarget
        {
            get { return target; }
            set { target = value; }
        }

        public static int[] CheckMap(int[,] currentMap, int xsize, int ysize, int botx, int boty, int type)
        {
            // This searches the current map for the number of his needs
            // It grabs the closest one and then returns it to Botme so he can path to it
            // Hunger = 6   Comfort = 7   Fun = 8  Personal = 9     5 is reserved for walls
            target = 500;
            int i = 0;
            int j = 0;
            int[] itemLoc = new int[2];

            for (j = 0; j < ysize; j++)
            {
                for (i = 0; i < xsize; i++)
                {
                    int fooBar = currentMap[i, j];
                    if (fooBar == type)
                    {
                        double distx = botx - i;
                        double disty = boty - j;
                        double goalDist = Math.Sqrt((distx*distx) + (disty*disty));
                        if (goalDist < distTarget)
                        {
                            target = goalDist;
                            itemLoc[0] = i;
                            itemLoc[1] = j;
                        }
                    }
                }
            }
            return itemLoc;
        }
    }
}