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
using System.Collections.Generic;
using Aurora.Framework;
using OpenMetaverse;

namespace Aurora.BotManager
{
    public class NodeGraph
    {
        private readonly object m_lock = new object();
        public int CurrentPos;

        /// <summary>
        ///     Loop through the current positions over and over
        /// </summary>
        public bool FollowIndefinitely;

        private DateTime m_lastChangedPosition = DateTime.MinValue;
        private List<Vector3> m_listOfPositions = new List<Vector3>();
        private List<TravelMode> m_listOfStates = new List<TravelMode>();
        private DateTime m_waitingSince = DateTime.MinValue;

        #region Add

        public void Add(Vector3 position, TravelMode state)
        {
            lock (m_lock)
            {
                m_listOfPositions.Add(position);
                m_listOfStates.Add(state);
            }
        }

        public void AddRange(IEnumerable<Vector3> positions, IEnumerable<TravelMode> states)
        {
            lock (m_lock)
            {
                m_listOfPositions.AddRange(positions);
                m_listOfStates.AddRange(states);
            }
        }

        #endregion

        #region Clear

        public void Clear()
        {
            lock (m_lock)
            {
                CurrentPos = 0;
                m_listOfPositions.Clear();
                m_listOfStates.Clear();
            }
        }

        #endregion

        public bool GetNextPosition(Vector3 currentPos, float closeToRange, int secondsBeforeForcedTeleport,
                                    out Vector3 position, out TravelMode state, out bool needsToTeleportToPosition)
        {
            const bool found = false;
            lock (m_lock)
            {
                findNewTarget:
                position = Vector3.Zero;
                state = TravelMode.None;
                needsToTeleportToPosition = false;
                if ((m_listOfPositions.Count - CurrentPos) > 0)
                {
                    position = m_listOfPositions[CurrentPos];
                    state = m_listOfStates[CurrentPos];
                    if (state != TravelMode.Wait && state != TravelMode.TriggerHereEvent &&
                        position.ApproxEquals(currentPos, closeToRange))
                    {
                        //Its close to a position, go look for the next pos
                        //m_listOfPositions.RemoveAt (0);
                        //m_listOfStates.RemoveAt (0);
                        CurrentPos++;
                        m_lastChangedPosition = DateTime.MinValue;
                        goto findNewTarget;
                    }
                    if (state == TravelMode.TriggerHereEvent)
                    {
                        CurrentPos++; //Clear for next time, as we only fire this one time
                        m_lastChangedPosition = DateTime.MinValue;
                    }
                    else if (state == TravelMode.Wait)
                    {
                        if (m_waitingSince == DateTime.MinValue)
                            m_waitingSince = DateTime.Now;
                        else
                        {
                            if ((DateTime.Now - m_waitingSince).Seconds > position.X)
                            {
                                m_waitingSince = DateTime.MinValue;
                                CurrentPos++;
                                m_lastChangedPosition = DateTime.MinValue;
                                goto findNewTarget;
                            }
                        }
                    }
                    else
                    {
                        m_lastChangedPosition = DateTime.Now;
                        if ((DateTime.Now - m_lastChangedPosition).Seconds > secondsBeforeForcedTeleport)
                            needsToTeleportToPosition = true;
                    }
                    return true;
                }
                if (m_listOfPositions.Count == 0)
                    return false;
                if (FollowIndefinitely)
                {
                    CurrentPos = 0; //Reset the position to the beginning if we have run out of positions
                    goto findNewTarget;
                }
            }
            return found;
        }

        public void CopyFrom(NodeGraph graph)
        {
            m_listOfPositions = graph.m_listOfPositions;
            m_listOfStates = graph.m_listOfStates;
        }
    }
}