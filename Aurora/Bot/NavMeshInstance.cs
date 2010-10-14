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

namespace OpenSim.Region.Examples.RexBot
{
    public class NavMeshInstance
    {
        public const TravelMode DEFAULT_TRAVELMODE = TravelMode.None;
        public const int DEFAULT_STARTNODE = 0;
        public const bool DEFAULT_RANDOM = false;
        public const bool DEFAULT_REVERSE = false;
        public const bool DEFAULT_ALLOWU = true;
        public const int DEFAULT_TIMEOUT = 30;

        private NavMesh m_navMesh;
        private Node m_currentNode;

        private readonly int m_startNode;
        private readonly bool m_reverse;

        private readonly int m_timeOut; // time out for path finding, if not in destination in this item, teleport
        public int TimeOut
        {
            get { return m_timeOut; }
        }

        public NavMesh NavMesh
        {
            get { return m_navMesh; }
        }

        public NavMeshInstance(NavMesh mesh, int startNode, bool reverse, int timeOut)
        {
            m_navMesh = mesh;
            m_currentNode = new Node(startNode);

            m_startNode = startNode;
            m_reverse = reverse;
            timeOut = Math.Max(0, timeOut);
            
            m_timeOut = timeOut;
        }

        public Node GetNextNode()
        {
            if (m_reverse == false)
                m_currentNode = m_navMesh.GetNextNode(m_currentNode);
            else
                m_currentNode = m_navMesh.GetPreviousNode(m_currentNode);

            return m_currentNode;
        }
    }
}
