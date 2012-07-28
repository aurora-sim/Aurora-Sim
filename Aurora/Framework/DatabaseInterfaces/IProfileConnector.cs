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

using System.Collections.Generic;
using OpenMetaverse;

namespace Aurora.Framework
{
    public interface IRemoteProfileConnector : IProfileConnector
    {
        void Init(string remoteURL, IRegistryCore registry);
    }

    public interface IProfileConnector : IAuroraDataPlugin
    {
        /// <summary>
        ///   Gets the profile for an agent
        /// </summary>
        /// <param name = "agentID"></param>
        /// <returns></returns>
        IUserProfileInfo GetUserProfile(UUID agentID);

        /// <summary>
        ///   Updates the user's profile (Note: the user must already have a profile created)
        /// </summary>
        /// <param name = "Profile"></param>
        /// <returns></returns>
        bool UpdateUserProfile(IUserProfileInfo Profile);

        /// <summary>
        ///   Creates an new profile for the user
        /// </summary>
        /// <param name = "UUID"></param>
        void CreateNewProfile(UUID UUID);

        bool AddClassified(Classified classified);
        Classified GetClassified(UUID queryClassifiedID);
        List<Classified> GetClassifieds(UUID ownerID);
        void RemoveClassified(UUID queryClassifiedID);

        bool AddPick(ProfilePickInfo pick);
        ProfilePickInfo GetPick(UUID queryPickID);
        List<ProfilePickInfo> GetPicks(UUID ownerID);
        void RemovePick(UUID queryPickID);
    }
}