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

using System;
using System.Collections.Generic;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;

namespace OpenSim.Services.Interfaces
{
    public class FriendInfo : IDataTransferable
    {
        /// <summary>
        ///   The friend of PrincipalID
        /// </summary>
        public string Friend;

        /// <summary>
        ///   The flags that PrincipalID has given to Friend
        /// </summary>
        public int MyFlags;

        /// <summary>
        ///   The user who is a friend of "Friend"
        /// </summary>
        public UUID PrincipalID;

        /// <summary>
        ///   The flags Friend has given to PrincipalID
        /// </summary>
        public int TheirFlags;

        public FriendInfo()
        {
        }

        public FriendInfo(Dictionary<string, object> kvp)
        {
            FromKVP(kvp);
        }

        public override void FromKVP(Dictionary<string, object> kvp)
        {
            PrincipalID = UUID.Zero;
            if (kvp.ContainsKey("PrincipalID") && kvp["PrincipalID"] != null)
                UUID.TryParse(kvp["PrincipalID"].ToString(), out PrincipalID);
            Friend = string.Empty;
            if (kvp.ContainsKey("Friend") && kvp["Friend"] != null)
                Friend = kvp["Friend"].ToString();
            MyFlags = 0;
            if (kvp.ContainsKey("MyFlags") && kvp["MyFlags"] != null)
                Int32.TryParse(kvp["MyFlags"].ToString(), out MyFlags);
            TheirFlags = 0;
            if (kvp.ContainsKey("TheirFlags") && kvp["TheirFlags"] != null)
                Int32.TryParse(kvp["TheirFlags"].ToString(), out TheirFlags);
        }

        public override void FromOSD(OpenMetaverse.StructuredData.OSDMap map)
        {
            PrincipalID = map["PrincipalID"];
            Friend = map["Friend"];
            MyFlags = map["MyFlags"];
            TheirFlags = map["TheirFlags"];
        }

        public override Dictionary<string, object> ToKVP()
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result["PrincipalID"] = PrincipalID.ToString();
            result["Friend"] = Friend;
            result["MyFlags"] = MyFlags.ToString();
            result["TheirFlags"] = TheirFlags.ToString();

            return result;
        }

        public override OpenMetaverse.StructuredData.OSDMap ToOSD()
        {
            OpenMetaverse.StructuredData.OSDMap result = new OpenMetaverse.StructuredData.OSDMap();
            result["PrincipalID"] = PrincipalID;
            result["Friend"] = Friend;
            result["MyFlags"] = MyFlags;
            result["TheirFlags"] = TheirFlags;

            return result;
        }
    }

    public interface IFriendsService
    {
        /// <summary>
        ///   The local service (if possible)
        /// </summary>
        IFriendsService InnerService { get; }

        /// <summary>
        ///   Get all friends of the given user
        /// </summary>
        /// <param name = "PrincipalID"></param>
        /// <returns></returns>
        List<FriendInfo> GetFriends(UUID PrincipalID);

        /// <summary>
        ///   Store the changes of the friend of PrincipalID
        /// </summary>
        /// <param name = "PrincipalID"></param>
        /// <param name = "Friend"></param>
        /// <param name = "flags"></param>
        /// <returns></returns>
        bool StoreFriend(UUID PrincipalID, string Friend, int flags);

        /// <summary>
        ///   Delete the friendship between the two users
        /// </summary>
        /// <param name = "PrincipalID"></param>
        /// <param name = "Friend"></param>
        /// <returns></returns>
        bool Delete(UUID PrincipalID, string Friend);

        void Initialize(IConfigSource config, IRegistryCore registry);

        void FinishedStartup();

        void Start(IConfigSource config, IRegistryCore registry);
    }
}