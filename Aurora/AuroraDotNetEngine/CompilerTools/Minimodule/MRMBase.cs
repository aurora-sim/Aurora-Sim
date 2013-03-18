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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Lifetime;
using Aurora.Framework;
using OpenMetaverse;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.MiniModule
{
    public abstract class MRMBase : IScript
    {
        private IHost m_host;
        private UUID m_id;
        private IWorld m_world;

        protected IWorld World
        {
            get { return m_world; }
        }

        protected IHost Host
        {
            get { return m_host; }
        }

        public UUID ID
        {
            get { return m_id; }
        }

        #region IScript Members

        public void InitApi(IScriptApi data)
        {
        }

        public ISponsor Sponsor
        {
            get { return null; }
        }

        public long GetStateEventFlags(string state)
        {
            return 0;
        }

        public EnumeratorInfo ExecuteEvent(string state, string FunctionName, object[] args, EnumeratorInfo Start,
                                           out Exception ex)
        {
            ex = null;
            return null;
        }

        public Dictionary<string, object> GetVars()
        {
            return new Dictionary<string, object>();
        }

        public void SetVars(Dictionary<string, object> vars)
        {
        }

        public Dictionary<string, object> GetStoreVars()
        {
            return new Dictionary<string, object>();
        }

        public void SetStoreVars(Dictionary<string, object> vars)
        {
        }

        public void ResetVars()
        {
        }

        public void UpdateInitialValues()
        {
        }

        public void Close()
        {
            Stop();
        }

        public string Name
        {
            get { return "MRMBase"; }
        }

        public void Dispose()
        {
        }

        public void SetSceneRefs(IScene iScene, ISceneChildEntity iSceneChildEntity, bool useStateSaves)
        {
        }

        public bool NeedsStateSaved
        {
            get { return false; }
            set { }
        }

        public IEnumerator FireEvent(string evName, object[] parameters)
        {
            yield break;
        }

        #endregion

        public void InitMiniModule(IWorld world, IHost host, UUID uniqueID)
        {
            m_world = world;
            m_host = host;
            m_id = uniqueID;
        }

        public abstract void Start();
        public abstract void Stop();
    }
}