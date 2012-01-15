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
using OpenMetaverse;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services.Connectors
{
    public class UserAccountCache
    {
        private const double CACHE_EXPIRATION_SECONDS = 6*60*1000;
                             // 6 hour cache on useraccounts, since they should not change

        private const bool m_allowNullCaching = true;
        private readonly ExpiringCache<string, UUID> m_NameCache;
        private readonly ExpiringCache<UUID, UserAccount> m_UUIDCache;
        private readonly Dictionary<UUID, int> m_nullCacheTimes = new Dictionary<UUID, int>();

        public UserAccountCache()
        {
            m_UUIDCache = new ExpiringCache<UUID, UserAccount>();
            m_NameCache = new ExpiringCache<string, UUID>();
        }

        public void Cache(UUID userID, UserAccount account)
        {
            if (!m_allowNullCaching && account == null)
                return;
            if ((account != null) && (account.UserLevel <= -1))
                return;
            if (account == null)
            {
                if (!m_nullCacheTimes.ContainsKey(userID))
                    m_nullCacheTimes[userID] = 0;
                else
                    m_nullCacheTimes[userID]++;
                if (m_nullCacheTimes[userID] < 5)
                    return;
            }
            else if (m_nullCacheTimes.ContainsKey(userID))
                m_nullCacheTimes.Remove(userID);
            // Cache even null accounts
            m_UUIDCache.AddOrUpdate(userID, account, CACHE_EXPIRATION_SECONDS);
            if (account != null)
                m_NameCache.AddOrUpdate(account.Name, account.PrincipalID, CACHE_EXPIRATION_SECONDS);

            //MainConsole.Instance.DebugFormat("[USER CACHE]: cached user {0}", userID);
        }

        public bool Get(UUID userID, out UserAccount account)
        {
            if (m_UUIDCache.TryGetValue(userID, out account))
                return true;

            return false;
        }

        public bool Get(string name, out UserAccount account)
        {
            account = null;

            UUID uuid = UUID.Zero;
            if (m_NameCache.TryGetValue(name, out uuid))
                if (m_UUIDCache.TryGetValue(uuid, out account))
                    return true;

            return false;
        }
    }
}