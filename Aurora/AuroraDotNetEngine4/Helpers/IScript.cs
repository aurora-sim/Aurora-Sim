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
using System.Text;
using OpenSim.Framework;
using System.Runtime.Remoting.Lifetime;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    [Serializable]
    public class EnumeratorInfo
    {
        public Guid Key = Guid.Empty;
        public DateTime SleepTo = DateTime.MinValue;
    }

    public interface IScript : IDisposable
    {
        void InitApi(IScriptApi data);

        ISponsor Sponsor { get; }
        void UpdateLease(TimeSpan time);
        long GetStateEventFlags(string state);
        void ExecuteEvent(string state, string FunctionName, object[] args);
        Dictionary<string, Object> GetVars();
        void SetVars(Dictionary<string, Object> vars);
        Dictionary<string, Object> GetStoreVars();
        void SetStoreVars(Dictionary<string, Object> vars);
        void ResetVars();
        /// <summary>
        /// Find the initial variables so that we can reset the state later if needed
        /// </summary>
        void UpdateInitialValues();
        
        void Close();
        string Name { get; }
        void SetSceneRefs(IScene scene, ISceneChildEntity child);
    }
}
