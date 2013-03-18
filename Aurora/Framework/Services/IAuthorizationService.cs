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

namespace Aurora.Framework
{
    // Generic Authorization service used for authorizing principals in a particular region

    public interface IAuthorizationService
    {
        /// <summary>
        ///   Gets whether the given agent is able to enter the given region as a root or child agent
        /// </summary>
        /// <param name = "region">The region the agent is attempting to enter</param>
        /// <param name = "agent">The CircuitData of the agent that is attempting to enter</param>
        /// <param name = "isRootAgent">Whether the agent is a root agent or not</param>
        /// <param name = "reason">If it fails, the reason they cannot enter</param>
        /// <returns></returns>
        bool IsAuthorizedForRegion(GridRegion region, AgentCircuitData agent, bool isRootAgent,
                                   out string reason);
    }

    public class AuthorizationRequest
    {
        public AuthorizationRequest()
        {
        }

        public AuthorizationRequest(string ID, string RegionID)
        {
            this.ID = ID;
            this.RegionID = RegionID;
        }

        public AuthorizationRequest(string ID, string FirstName, string SurName, string Email, string RegionID)
        {
            this.ID = ID;
            this.FirstName = FirstName;
            this.SurName = SurName;
            this.Email = Email;
            this.RegionID = RegionID;
        }

        public string ID { get; set; }

        public string FirstName { get; set; }

        public string SurName { get; set; }

        public string Email { get; set; }

        public string RegionID { get; set; }
    }

    public class AuthorizationResponse
    {
        public AuthorizationResponse()
        {
        }

        public AuthorizationResponse(bool isAuthorized, string message)
        {
            IsAuthorized = isAuthorized;
            Message = message;
        }

        public bool IsAuthorized { get; set; }

        public string Message { get; set; }
    }
}