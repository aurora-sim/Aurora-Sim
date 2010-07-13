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
 *     * Neither the name of the OpenSimulator Project nor the
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
using System.Collections;
using System.Collections.Generic;
using OpenMetaverse;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using Aurora.ScriptEngine.AuroraDotNetEngine.Plugins;
using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.Plugins
{
    public class Dataserver
    {
        public ScriptEngine m_ScriptEngine;

        private Dictionary<string, DataserverRequest> DataserverRequests =
                new Dictionary<string, DataserverRequest>();

        public Dataserver(ScriptEngine ScriptEngine)
        {
            m_ScriptEngine = ScriptEngine;
        }

        private class DataserverRequest
        {
            public UUID primID;
            public UUID itemID;

            public UUID ID;
            public string handle;

            public DateTime startTime;
            public DateTime IsCompleteAt;
            public string Reply;
        }

        public UUID RegisterRequest(UUID primID, UUID itemID,
                                      string identifier)
        {
            lock (DataserverRequests)
            {
                if (DataserverRequests.ContainsKey(identifier))
                    return UUID.Zero;

                DataserverRequest ds = new DataserverRequest();

                ds.primID = primID;
                ds.itemID = itemID;

                ds.ID = UUID.Random();
                ds.handle = identifier;

                ds.startTime = DateTime.Now;
                ds.IsCompleteAt = DateTime.Now.AddHours(1);
                ds.Reply = "";

                DataserverRequests[identifier] = ds;

                return ds.ID;
            }
        }

        private void DataserverReply(string identifier, string reply)
        {
            DataserverRequest ds;

            lock (DataserverRequests)
            {
                if (!DataserverRequests.ContainsKey(identifier))
                    return;

                ds = DataserverRequests[identifier];
                DataserverRequests.Remove(identifier);
            }

            m_ScriptEngine.PostObjectEvent(ds.primID,
                    "dataserver", new Object[]
                            { new LSL_Types.LSLString(ds.ID.ToString()),
                            new LSL_Types.LSLString(reply)});
        }

        public void RemoveEvents(UUID primID, UUID itemID)
        {
            lock (DataserverRequests)
            {
                foreach (DataserverRequest ds in DataserverRequests.Values)
                {
                    if (ds.itemID == itemID)
                        DataserverRequests.Remove(ds.handle);
                }
            }
        }

        public void CheckAndExpireRequests()
        {
            if (DataserverRequests.Count == 0)
                return;
            lock (DataserverRequests)
            {
                foreach (DataserverRequest ds in DataserverRequests.Values)
                {
                    if (ds.IsCompleteAt < DateTime.Now)
                        DataserverReply(ds.handle, ds.Reply);

                    if (ds.startTime > DateTime.Now.AddSeconds(30))
                        DataserverRequests.Remove(ds.handle);
                }
            }
        }

        internal void AddReply(string handle, string reply, int millisecondsToWait)
        {
            DataserverRequest request = null;
            DataserverRequests.TryGetValue(handle, out request);
            //Wait for the value to be returned in LSL_Api
            request.IsCompleteAt = DateTime.Now.AddSeconds(millisecondsToWait / 1000 + 0.1);
            request.Reply = reply;
        }
    }
}
