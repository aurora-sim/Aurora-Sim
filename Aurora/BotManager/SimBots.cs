using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Aurora.BotManager
{
    /// <summary>
    /// Created by Christy Lock
    /// </summary>
    class SimBots
    {
        /// <summary>
        /// This is used to seacrh the current map for
        /// Items that the bot needs. Like when he is hungry he will search for 9 on the map
        /// Fun is 7 and Comfort is 8 etc.
        /// This returns the x and y to BotMe and then he passes them on to Astar and builds a list of waypoints to
        /// Reach the goal.
        /// </summary>
        public static double distTarget
        {
            get
            {
                return target;
            }
            set
            {
                target = value;
            }
        }
        public static double target;

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
                        double goalDist = Math.Sqrt((distx * distx) + (disty * disty));
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
