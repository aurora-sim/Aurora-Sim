using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;

namespace Aurora.BotManager
{
    public class NodeGraph
    {
        private List<Vector3> m_listOfPositions = new List<Vector3> ();
        private List<TravelMode> m_listOfStates = new List<TravelMode> ();
        private object m_lock = new object ();
        private DateTime m_lastChangedPosition = DateTime.MinValue;

        public NodeGraph ()
        {
        }

        #region Add

        public void Add (Vector3 position, TravelMode state)
        {
            lock (m_lock)
            {
                m_listOfPositions.Add (position);
                m_listOfStates.Add (state);
            }
        }

        public void AddRange (IEnumerable<Vector3> positions, IEnumerable<TravelMode> states)
        {
            lock (m_lock)
            {
                m_listOfPositions.AddRange (positions);
                m_listOfStates.AddRange (states);
            }
        }

        #endregion

        #region Clear

        public void Clear ()
        {
            lock (m_lock)
            {
                m_listOfPositions.Clear ();
                m_listOfStates.Clear ();
            }
        }

        #endregion

        public bool GetNextPosition (Vector3 currentPos, float closeToRange, int secondsBeforeForcedTeleport, out Vector3 position, out TravelMode state, out bool needsToTeleportToPosition)
        {
            bool found = false;
            lock (m_lock)
            {
            findNewTarget:
                position = Vector3.Zero;
                state = TravelMode.None;
                needsToTeleportToPosition = false;
                if (m_listOfPositions.Count > 0)
                {
                    position = m_listOfPositions[0];
                    state = m_listOfStates[0];
                    if (m_lastChangedPosition == DateTime.MinValue)
                        m_lastChangedPosition = DateTime.Now;
                    if (position.ApproxEquals (currentPos, closeToRange))
                    {
                        //Its close to a position, go look for the next pos
                        m_listOfPositions.RemoveAt (0);
                        m_listOfStates.RemoveAt (0);
                        m_lastChangedPosition = DateTime.MinValue;
                        goto findNewTarget;
                    }
                    else
                    {
                        if ((DateTime.Now - m_lastChangedPosition).Seconds > secondsBeforeForcedTeleport)
                            needsToTeleportToPosition = true;
                    }
                }
            }
            return found;
        }

        public void CopyFrom (NodeGraph graph)
        {
            m_listOfPositions = graph.m_listOfPositions;
            m_listOfStates = graph.m_listOfStates;
        }
    }
}
