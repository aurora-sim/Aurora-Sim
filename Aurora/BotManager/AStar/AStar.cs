using System;
using System.Collections;
using Tanis.Collections;

namespace Games.Pathfinding
{
    /// <summary>
    /// Base class for pathfinding nodes, it holds no actual information about the map. 
    /// An inherited class must be constructed from this class and all virtual methods must be 
    /// implemented. Note, that calling base() in the overridden methods is not needed.
    /// </summary>
    public class AStarNode : IComparable
    {
        #region Properties

        private AStarNode FParent;
        /// <summary>
        /// The parent of the node.
        /// </summary>
        public AStarNode Parent
        {
            get
            {
                return FParent;
            }
            set
            {
                FParent = value;
            }
        }

        /// <summary>
        /// The accumulative cost of the path until now.
        /// </summary>
        public double Cost
        {
            set
            {
                FCost = value;
            }
            get
            {
                return FCost;
            }
        }
        private double FCost;

        /// <summary>
        /// The estimated cost to the goal from here.
        /// </summary>
        public double GoalEstimate
        {
            set
            {
                FGoalEstimate = value;
            }
            get
            {
                Calculate();
                return (FGoalEstimate);
            }
        }
        private double FGoalEstimate;

        /// <summary>
        /// The cost plus the estimated cost to the goal from here.
        /// </summary>
        public double TotalCost
        {
            get
            {
                return (Cost + GoalEstimate);
            }
        }

        /// <summary>
        /// The goal node.
        /// </summary>
        public AStarNode GoalNode
        {
            set
            {
                FGoalNode = value;
                Calculate();
            }
            get
            {
                return FGoalNode;
            }
        }
        private AStarNode FGoalNode;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="AParent">The node's parent</param>
        /// <param name="AGoalNode">The goal node</param>
        /// <param name="ACost">The accumulative cost until now</param>
        public AStarNode(AStarNode AParent, AStarNode AGoalNode, double ACost)
        {
            FParent = AParent;
            FCost = ACost;
            GoalNode = AGoalNode;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Determines wheather the current node is the goal.
        /// </summary>
        /// <returns>Returns true if current node is the goal</returns>
        public bool IsGoal()
        {
            return IsSameState(FGoalNode);
        }

        #endregion

        #region Virtual Methods

        /// <summary>
        /// Determines wheather the current node is the same state as the on passed.
        /// </summary>
        /// <param name="ANode">AStarNode to compare the current node to</param>
        /// <returns>Returns true if they are the same state</returns>
        public virtual bool IsSameState(AStarNode ANode)
        {
            return false;
        }

        /// <summary>
        /// Calculates the estimated cost for the remaining trip to the goal.
        /// </summary>
        public virtual void Calculate()
        {
            FGoalEstimate = 0.0f;
        }

        /// <summary>
        /// Gets all successors nodes from the current node and adds them to the successor list
        /// </summary>
        /// <param name="ASuccessors">List in which the successors will be added</param>
        public virtual void GetSuccessors(ArrayList ASuccessors)
        {
        }

        #endregion

        #region Overridden Methods

        public override bool Equals(object obj)
        {
            return IsSameState((AStarNode)obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            return (-TotalCost.CompareTo(((AStarNode)obj).TotalCost));
        }

        #endregion
    }

    /// <summary>
    /// Class for performing A* pathfinding
    /// </summary>
    public sealed class AStar
    {
        #region Private Fields

        private AStarNode FStartNode;
        private AStarNode FGoalNode;
        private Heap FOpenList;
        private Heap FClosedList;
        private ArrayList FSuccessors;

        #endregion

        #region Properties

        /// <summary>
        /// Holds the solution after pathfinding is done. <see>FindPath()</see>
        /// </summary>
        public ArrayList Solution
        {
            get
            {
                return FSolution;
            }
        }
        private ArrayList FSolution;

        public bool pathPossible
        {
            get { return m_pathPossible; }
        }
        private bool m_pathPossible = true;

        #endregion

        #region Constructors

        public AStar()
        {
            FOpenList = new Heap();
            FClosedList = new Heap();
            FSuccessors = new ArrayList();
            FSolution = new ArrayList();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Prints all the nodes in a list
        /// </summary>
        /// <param name="ANodeList">List to print</param>
        private void PrintNodeList(object ANodeList)
        {
            Console.WriteLine("Node list:");
            foreach (AStarNode n in (ANodeList as IEnumerable))
            {
                //n.PrintNodeInfo();
            }
            Console.WriteLine("=====");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Finds the shortest path from the start node to the goal node
        /// </summary>
        /// <param name="AStartNode">Start node</param>
        /// <param name="AGoalNode">Goal node</param>
        public void FindPath(AStarNode AStartNode, AStarNode AGoalNode)
        {
            FStartNode = AStartNode;
            FGoalNode = AGoalNode;

            FOpenList.Add(FStartNode);
            int i = 0;
            while (FOpenList.Count > 0 && i < 2000)
            {
                // Get the node with the lowest TotalCost
                AStarNode NodeCurrent = (AStarNode)FOpenList.Pop();

                // If the node is the goal copy the path to the solution array
                if (NodeCurrent.IsGoal())
                {
                    while (NodeCurrent != null)
                    {
                        FSolution.Insert(0, NodeCurrent);
                        NodeCurrent = NodeCurrent.Parent;
                    }
                    break;
                }

                // Get successors to the current node
                NodeCurrent.GetSuccessors(FSuccessors);
                foreach (AStarNode NodeSuccessor in FSuccessors)
                {
                    // Test if the currect successor node is on the open list, if it is and
                    // the TotalCost is higher, we will throw away the current successor.
                    AStarNode NodeOpen = null;
                    if (FOpenList.Contains(NodeSuccessor))
                        NodeOpen = (AStarNode)FOpenList[FOpenList.IndexOf(NodeSuccessor)];
                    if ((NodeOpen != null) && (NodeSuccessor.TotalCost > NodeOpen.TotalCost))
                        continue;

                    // Test if the currect successor node is on the closed list, if it is and
                    // the TotalCost is higher, we will throw away the current successor.
                    AStarNode NodeClosed = null;
                    if (FClosedList.Contains(NodeSuccessor))
                        NodeClosed = (AStarNode)FClosedList[FClosedList.IndexOf(NodeSuccessor)];
                    if ((NodeClosed != null) && (NodeSuccessor.TotalCost > NodeClosed.TotalCost))
                        continue;

                    // Remove the old successor from the open list
                    FOpenList.Remove(NodeOpen);

                    // Remove the old successor from the closed list
                    FClosedList.Remove(NodeClosed);

                    // Add the current successor to the open list
                    FOpenList.Push(NodeSuccessor);
                }
                // Add the current node to the closed list
                FClosedList.Add(NodeCurrent);
                i++;
            }

            if (i == 2000)
            {
                m_pathPossible = false;
            }
        }

        #endregion
    }
}