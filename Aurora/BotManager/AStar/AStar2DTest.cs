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
using System.Collections.Generic;

namespace Games.Pathfinding.AStar2DTest
{
    /// <summary>
    ///     A node class for doing pathfinding on a 2-dimensional map
    ///     Christy Lock Note:
    ///     Astar.cs, Heap.cs and Main.cs were originally written by Sune Trundslev 4 Jan 2004
    ///     I has made small modifications to Astar. cs and Main.cs to handle the 3d Metaverse
    ///     Specifically to return waypoints in generic string Lists broken into slope changes. These are returned to BotMe.cs.
    ///     You can find the original code at http://www.codeproject.com/KB/recipes/csharppathfind.aspx
    ///     Note that there is no specific license in the code download and the author states " With this class, you should be able to implement your own
    ///     A* pathfinding to your own c# projects."
    /// </summary>
    public class AStarNode2D : AStarNode
    {
        #region Properties

        private readonly int FX;

        private readonly int FY;

        /// <summary>
        ///     The X-coordinate of the node
        /// </summary>
        public int X
        {
            get { return FX; }
        }

        /// <summary>
        ///     The Y-coordinate of the node
        /// </summary>
        public int Y
        {
            get { return FY; }
        }

        #endregion

        #region Constructors

        /// <summary>
        ///     Constructor for a node in a 2-dimensional map
        /// </summary>
        /// <param name="AParent">Parent of the node</param>
        /// <param name="AGoalNode">Goal node</param>
        /// <param name="ACost">Accumulative cost</param>
        /// <param name="AX">X-coordinate</param>
        /// <param name="AY">Y-coordinate</param>
        public AStarNode2D(AStarNode AParent, AStarNode AGoalNode, double ACost, int AX, int AY)
            : base(AParent, AGoalNode, ACost)
        {
            FX = AX;
            FY = AY;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Adds a successor to a list if it is not impassible or the parent node
        /// </summary>
        /// <param name="ASuccessors">List of successors</param>
        /// <param name="AX">X-coordinate</param>
        /// <param name="AY">Y-coordinate</param>
        private void AddSuccessor(ArrayList ASuccessors, int AX, int AY)
        {
            int CurrentCost = StartPath.GetMap(AX, AY);
            if (CurrentCost == -1)
            {
                return;
            }
            AStarNode2D NewNode = new AStarNode2D(this, GoalNode, Cost + CurrentCost, AX, AY);
            if (NewNode.IsSameState(Parent))
            {
                return;
            }
            ASuccessors.Add(NewNode);
        }

        #endregion

        #region Overidden Methods

        /// <summary>
        ///     Determines wheather the current node is the same state as the on passed.
        /// </summary>
        /// <param name="ANode">AStarNode to compare the current node to</param>
        /// <returns>Returns true if they are the same state</returns>
        public override bool IsSameState(AStarNode ANode)
        {
            if (ANode == null)
            {
                return false;
            }
            return ((((AStarNode2D) ANode).X == FX) &&
                    (((AStarNode2D) ANode).Y == FY));
        }

        /// <summary>
        ///     Calculates the estimated cost for the remaining trip to the goal.
        /// </summary>
        public override void Calculate()
        {
            if (GoalNode != null)
            {
                double xd = Math.Abs(FX - ((AStarNode2D) GoalNode).X);
                double yd = Math.Abs(FY - ((AStarNode2D) GoalNode).Y);

                // "Euclidean distance" - Used when search can move at any angle.
                //GoalEstimate = Math.Sqrt((xd * xd) + (yd * yd));//was using this one

                // "Manhattan Distance" - Used when search can only move vertically and 
                // horizontally.
                GoalEstimate = Math.Abs(xd) + Math.Abs(yd);

                // "Diagonal Distance" - Used when the search can move in 8 directions.
                //GoalEstimate = Math.Max(Math.Abs(xd), Math.Abs(yd));
            }
            else
            {
                GoalEstimate = 0;
            }
        }

        /// <summary>
        ///     Gets all successors nodes from the current node and adds them to the successor list
        /// </summary>
        /// <param name="ASuccessors">List in which the successors will be added</param>
        public override void GetSuccessors(ArrayList ASuccessors)
        {
            ASuccessors.Clear();
            AddSuccessor(ASuccessors, FX - 1, FY);
            AddSuccessor(ASuccessors, FX - 1, FY - 1);
            AddSuccessor(ASuccessors, FX, FY - 1);
            AddSuccessor(ASuccessors, FX + 1, FY - 1);
            AddSuccessor(ASuccessors, FX + 1, FY);
            AddSuccessor(ASuccessors, FX + 1, FY + 1);
            AddSuccessor(ASuccessors, FX, FY + 1);
            AddSuccessor(ASuccessors, FX - 1, FY + 1);
        }

        /// <summary>
        ///     Prints information about the current node
        /// </summary>
        public int[] PrintNodeInfo()
        {
            int[] returnWaypoint = new int[2];
            returnWaypoint[0] = X;
            returnWaypoint[1] = Y;
            return returnWaypoint;
        }

        #endregion
    }

    internal class StartPath
    {
        #region Maps

        public static int[,] currentMap;

        public static int xLimit;

        public static int yLimit;

        /// <summary>
        ///     Entry and Exit from BotMe is StartPath.Path
        ///     CurrenMap is the map read from the file in ReadMap
        /// </summary>
        public static int[,] Map
        {
            get { return currentMap; }
            set { currentMap = value; }
        }

        /// <summary>
        ///     XL/YL comes from the map maker description - BotMe /gm ---> ReadMap sets this
        /// </summary>
        public static int xL
        {
            get { return xLimit - 1; }
            set { xLimit = value; }
        }

        public static int yL
        {
            get { return yLimit - 1; }
            set { yLimit = value; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Gets movement cost from the 2-dimensional map
        ///     -1 blocks off all movement there
        /// </summary>
        /// <param name="x">X-coordinate</param>
        /// <param name="y">Y-coordinate</param>
        /// <returns>Returns movement cost at the specified point in the map</returns>
        public static int GetMap(int x, int y)
        {
            if ((x < 0) || (x > xL))
                return (-1);
            if ((y < 0) || (y > yL))
                return (-1);
            if (Map[x, y] > 5) //5 is a wall 6789 are needs but they need to be a 1 for him to path through them
            {
                return 1;
            }
            return (Map[x, y]);
        }

        #endregion

        #region Entry

        /// <summary>
        ///     The main entry point for the pathfinding routines.
        ///     AstarNode2D is derived from AStar then the StarPath class creates an instance of AStar and uses AstarNode2D
        ///     to override the methds in AStar.cs.
        ///     Using Path method as an entry and return point from/to BotMe. Also StartPath is used to make maps and check limits
        ///     as well as print the map out in a console if we use console apps.
        /// </summary>
        [STAThread]
        public static int[,] ReadMap(string map, int mapx, int mapy)
        {
            // This works because each character is internally represented by a number. 
            // The characters '0' to '9' are represented by consecutive numbers, so finding 
            // the difference between the characters '0' and '2' results in the number 2.if char = 2 or whatever.
            int[,] mapArray = new int[mapx,mapy];
            xLimit = mapx;
            yLimit = mapy;

            try
            {
                string[] lines = map.Split('\n');
                for (int lineNum = 0; lineNum < lines.Length; lineNum++)
                {
                    string line = lines[lineNum];
                    char[] each = line.ToCharArray();
                    for (int i = 0; i < each.Length; i++)
                    {
                        int fooBar = each[i] - '0';
                        if (fooBar == 5)
                        {
                            fooBar = -1;
                        }
                        mapArray[i, lineNum] = fooBar;
                    }
                }
                currentMap = mapArray;
                return mapArray;
            }
            catch
            {
                mapArray[0, 0] = -99;
                return mapArray;
            }
        }

        public static List<string> Path(int startx, int starty, int endx, int endy, int endz, int csx, int csy)
        {
            // Here is where we come in from BotMe with our start and end points from the world
            AStar astar = new AStar();

            AStarNode2D GoalNode = new AStarNode2D(null, null, 0, endx, endy);
            AStarNode2D StartNode = new AStarNode2D(null, GoalNode, 0, startx, starty) {GoalNode = GoalNode};

            // Prepare the final List that will become the waypoints for him to leaf through
            List<string> botPoint = new List<string>();


            // Go get the solution
            astar.FindPath(StartNode, GoalNode);

            // First check if the path was possible
            bool pathDone = astar.pathPossible;
            if (pathDone == false)
            {
                //Use botPoint List as a flag to break out of this. Return to Botme
                botPoint.Add("no_path");
                return botPoint;
            }

            // Slope calculation data
            int slope = 99; // Use 99 here to mean the slope has never been calculated yet
            int lastSlope = 99;
            int X1 = startx; //startx
            int Y1 = starty; //starty     
            int Z = endz;
            //startz - we need this to make a vector but will override with current z in Botme enabling him to walk up hills

            int xtemp = 0;
            int ytemp = 0;


            // This gets the solution from Astar.cs and runs it through PrintInfo which has the xyz of each path node - our Node solution
            ArrayList Nodes = new ArrayList(astar.Solution);
            foreach (AStarNode nn in Nodes)
            {
                AStarNode2D n = (AStarNode2D) nn;
                // Return x and y from printinfo
                int[] XYreturn = new int[2];
                XYreturn = n.PrintNodeInfo();
                int X2 = XYreturn[0];
                int Y2 = XYreturn[1];

                // Here I calculate point only where the line changes direction
                // In this way the bot doesn't start and stop each step 
                // Since it has been determined that the path is clear between these points this will work
                // You can see the trouble with moving objects here though - he will have to constantly check on the way to these points
                // To detect scene changes.
                slope = calcSlope(Y2, Y1, X2, X1);

                if (lastSlope != slope)
                {
                    // Build the list of waypoints only where changes of slope occur
                    xtemp = X1 + csx; //conerStone x and y from our map to get these into sim coordinates
                    ytemp = Y1 + csy;
                    string temp = xtemp.ToString() + "," + ytemp.ToString() + "," + Z.ToString();
                    botPoint.Add(temp);
                }
                X1 = X2;
                Y1 = Y2;
                lastSlope = slope;
            }
            // This adds the last point to the step
            xtemp = X1 + csx;
            ytemp = Y1 + csy;
            string temp2 = xtemp.ToString() + "," + ytemp.ToString() + "," + Z.ToString();
            botPoint.Add(temp2);
            // This removes the first point of the steps so they turn and go right to the first bend point(slope)
            botPoint.RemoveRange(0, 1);
            // Let em have it - return to Botme path with slopes only no start point but with end point always   
            return botPoint;
        }

        public static int calcSlope(int Y2, int Y1, int X2, int X1)
        {
            // The 88 and 99 numbers above are flags to keep from dividing by zero and to know if we are on the first step
            // I was trying to not set a point 1 step from the start if it was not a change in slope.
            int deltaX = X2 - X1;
            if (deltaX == 0)
            {
                return 88;
            }
            return (Y2 - Y1)/(X2 - X1);
        }

        #endregion
    }
}