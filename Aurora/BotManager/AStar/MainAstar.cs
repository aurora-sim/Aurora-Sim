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
using System.Collections;

namespace Games.Pathfinding.AStar2DTest
{
    /// <summary>
    ///     Test class for doing A* pathfinding on a 2D map.
    /// </summary>
    internal class MainClass
    {
        #region Test Maps

        private static readonly int[,] Map =
            {
                {1, -1, 1, 1, 1, -1, 1, 1, 1, 1},
                {1, -1, 1, -1, 1, -1, 1, 1, 1, 1},
                {1, -1, 1, -1, 1, -1, 1, 1, 1, 1},
                {1, -1, 1, -1, 1, -1, 1, 1, 1, 1},
                {1, -1, 1, -1, 1, -1, 1, 1, 1, 1},
                {1, -1, 1, -1, 1, -1, 1, 1, 1, 1},
                {1, -1, 1, -1, 1, -1, 1, 1, 1, 1},
                {1, -1, 1, -1, 1, -1, 1, 1, 1, 1},
                {1, -1, 1, -1, 1, -1, 1, 2, 1, 1},
                {1, 1, 1, -1, 1, 1, 2, 3, 2, 1}
            };

        //		static int[,] Map = {
        //			{ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        //			{ 1, 1, 1, 1, 1, 1, 1, 1,-1, 1 },
        //			{ 1, 1, 1, 1, 1, 1, 1, 1,-1, 1 },
        //			{ 1, 1, 1, 1, 1, 1, 1, 1,-1, 1 },
        //			{ 1, 1, 1, 1, 1, 1, 1, 1,-1, 1 },
        //			{ 1, 1, 1, 1, 1, 1, 1, 1,-1, 1 },
        //			{ 1, 1, 1, 1, 1, 1, 1, 1,-1, 1 },
        //			{ 1, 1, 1, 1, 1, 1, 1, 1,-1, 1 },
        //			{ 1,-1,-1,-1,-1,-1,-1,-1,-1, 1 },
        //			{ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
        //		};
        //		static int[,] Map = {
        //			{ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        //			{ 1, 1, 1, 1, 1, 2, 1, 1, 1, 1 },
        //			{ 1, 1, 1, 1, 2, 3, 2, 1, 1, 1 },
        //			{ 1, 1, 1, 2, 3, 4, 3, 2, 1, 1 },
        //			{ 1, 1, 2, 3, 4, 5, 4, 3, 2, 1 },
        //			{ 1, 1, 1, 2, 3, 4, 3, 2, 1, 1 },
        //			{ 1, 1, 1, 1, 2, 3, 2, 1, 1, 1 },
        //			{ 1, 1, 1, 1, 1, 2, 1, 1, 1, 1 },
        //			{ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        //			{ 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
        //		};

        #endregion

        #region Public Methods

        /// <summary>
        ///     Gets movement cost from the 2-dimensional map
        /// </summary>
        /// <param name="x">X-coordinate</param>
        /// <param name="y">Y-coordinate</param>
        /// <returns>Returns movement cost at the specified point in the map</returns>
        public static int GetMap(int x, int y)
        {
            if ((x < 0) || (x > 9))
                return (-1);
            if ((y < 0) || (y > 9))
                return (-1);
            return (Map[y, x]);
        }

        /// <summary>
        ///     Prints the solution
        /// </summary>
        /// <param name="ASolution">The list that holds the solution</param>
        public static void PrintSolution(ArrayList ASolution)
        {
            for (int j = 0; j < 10; j++)
            {
                for (int i = 0; i < 10; i++)
                {
                    bool solution = false;
                    foreach (AStarNode2D n in ASolution)
                    {
                        AStarNode2D tmp = new AStarNode2D(null, null, 0, i, j);
                        solution = n.IsSameState(tmp);
                        if (solution)
                            break;
                    }
                    if (solution)
                        Console.Write("S ");
                    else if (GetMap(i, j) == -1)
                        Console.Write("X ");
                    else
                        Console.Write(". ");
                }
                Console.WriteLine("");
            }
        }

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            Console.WriteLine("Starting...");

            AStar astar = new AStar();

            AStarNode2D GoalNode = new AStarNode2D(null, null, 0, 9, 9);
            AStarNode2D StartNode = new AStarNode2D(null, GoalNode, 0, 0, 0) {GoalNode = GoalNode};
            astar.FindPath(StartNode, GoalNode);

            PrintSolution(astar.Solution);
            Console.ReadLine();
        }

        #endregion
    }
}