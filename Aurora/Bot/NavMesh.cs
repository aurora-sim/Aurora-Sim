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
 *     * Neither the name of the OpenSim Project nor the
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
using OpenMetaverse;
using OpenSim.Framework;

namespace OpenSim.Region.Examples.RexBot
{
    public enum TravelMode { Walk, Fly, None };

    public struct Edge
    {
        public int Start;
        public int End;
        public TravelMode Mode;
    };

    public struct Node
    {
        public Vector3 Position;
        public TravelMode Mode;
        public int Index;

        public Node(int index)
        { 
            Index = index;
            Mode = TravelMode.None;
            Position = Vector3.Zero;
        }
    };

    public class NavMesh
    {
        private List<Vector3> m_nodes;
        private List<Edge> m_edges;

        public NavMesh()
        {
            m_nodes = new List<Vector3>();
            m_edges = new List<Edge>();
        }

        public void AddNode(Vector3 node)
        {
            m_nodes.Add(node);
        }

        public Vector3 GetNode(int index)
        {
            return m_nodes[index];
        }

        // Returns next node
        public Node GetNextNode(Node previous)
        {
            foreach (Edge edge in m_edges)
            {
                if (edge.Start == previous.Index)
                {
                    Node node = new Node(edge.End);
                    node.Mode = edge.Mode;
                    try
                    {
                        node.Position = m_nodes[edge.End];
                    }
                    catch (System.Exception)
                    {
                        node.Position = previous.Position;
                    }

                    return node;
                }
            }
            throw new Exception("Path node number " + previous.Index.ToString() + " does not exist for path " + Name + ".");
        }

        // returns previous node
        public Node GetPreviousNode(Node previous)
        {
            foreach (Edge edge in m_edges)
            {
                if (edge.End == previous.Index)
                {
                    Node node = new Node(edge.Start);
                    node.Mode = edge.Mode;
                    try
                    {
                        node.Position = m_nodes[edge.Start];
                    }
                    catch (System.Exception)
                    {
                        node.Position = previous.Position;
                    }

                    return node;
                }
            }
            throw new Exception("Path node number " + previous.Index.ToString() + " does not exist for path " + Name + ".");
        }

        public void AddEdge(int e1, int e2, TravelMode mode)
        {
            Edge edge = new Edge();
            edge.Start = e1;
            edge.End = e2;
            edge.Mode = mode;
            m_edges.Add(edge);
        }

        private string m_name;
        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        private TravelMode m_defaultMode;
        public TravelMode DefaultMode
        {
            get { return m_defaultMode; }
            set { m_defaultMode = value; }
        }

        static public TravelMode ParseTravelMode(string mode)
        {
            if (mode == "fly")
                return TravelMode.Fly;
            else if (mode == "walk")
                return TravelMode.Walk;

            return TravelMode.None;
        }
    }
}
