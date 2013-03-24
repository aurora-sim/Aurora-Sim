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
using System.Linq;
using System.Net;
using Aurora.Framework.PresenceInfo;
using OpenMetaverse;

namespace Aurora.Framework.SceneInfo
{
    /// <summary>
    ///     Manage client circuits
    /// </summary>
    public class AgentCircuitManager
    {
        public Dictionary<uint, AgentCircuitData> AgentCircuits = new Dictionary<uint, AgentCircuitData>();

        public virtual AgentCircuitData AuthenticateSession(UUID sessionID, UUID agentID, uint circuitcode,
                                                            IPEndPoint IP)
        {
            AgentCircuitData validcircuit = null;
            if (AgentCircuits.ContainsKey(circuitcode))
            {
                validcircuit = AgentCircuits[circuitcode];
            }
            //User never logged in... they shouldn't be attempting to connect
            if (validcircuit == null)
            {
                //don't have this circuit code in our list
                return null;
            }

            //There is a session found... just is the sessionID right
            if ((sessionID == validcircuit.SessionID) && (agentID == validcircuit.AgentID))
            {
                return validcircuit;
            }

            return null;
        }

        /// <summary>
        ///     Add information about a new circuit so that later on we can authenticate a new client session.
        /// </summary>
        /// <param name="circuitCode"></param>
        /// <param name="agentData"></param>
        public virtual void AddNewCircuit(uint circuitCode, AgentCircuitData agentData)
        {
            lock (AgentCircuits)
            {
                AgentCircuits[circuitCode] = agentData;
            }
        }

        public virtual void RemoveCircuit(UUID agentID)
        {
            lock (AgentCircuits)
            {
                foreach (
                    AgentCircuitData circuitData in
                        new List<AgentCircuitData>(AgentCircuits.Values).Where(
                            circuitData => circuitData.AgentID == agentID))
                {
                    AgentCircuits.Remove(circuitData.CircuitCode);
                }
            }
        }

        public AgentCircuitData GetAgentCircuitData(UUID agentID)
        {
            return
                new List<AgentCircuitData>(AgentCircuits.Values).FirstOrDefault(
                    circuitData => circuitData.AgentID == agentID);
        }
    }
}