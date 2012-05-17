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
using System.Runtime.Remoting.Lifetime;
using Aurora.Framework;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using LSL_Float = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLFloat;
using LSL_Integer = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLInteger;
using LSL_Key = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_List = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.list;
using LSL_Rotation = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Quaternion;
using LSL_String = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_Vector = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Vector3;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.APIs
{
    [Serializable]
    public class MOD_Api : MarshalByRefObject, IMOD_Api, IScriptApi
    {
        internal ScriptProtectionModule ScriptProtection;
        internal IScriptModulePlugin m_ScriptEngine;
        internal IScriptModuleComms m_comms;
        internal ISceneChildEntity m_host;
        internal UUID m_itemID;
        internal uint m_localID;

        public IScene World
        {
            get { return m_host.ParentEntity.Scene; }
        }

        #region IMOD_Api Members

        public string modSendCommand(string module, string command, string k)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "modSendCommand", m_host, "MOD", m_itemID))
                return "";

            UUID req = UUID.Random();

            m_comms.RaiseEvent(m_itemID, req.ToString(), module, command, k);

            return req.ToString();
        }

        #endregion

        #region IScriptApi Members

        public void Initialize(IScriptModulePlugin ScriptEngine, ISceneChildEntity host, uint localID, UUID itemID,
                               ScriptProtectionModule module)
        {
            m_ScriptEngine = ScriptEngine;
            m_host = host;
            m_localID = localID;
            m_itemID = itemID;
            ScriptProtection = module;

            m_comms = World.RequestModuleInterface<IScriptModuleComms>();
        }

        public IScriptApi Copy()
        {
            return new MOD_Api();
        }

        public string Name
        {
            get { return "mod"; }
        }

        public string InterfaceName
        {
            get { return "IMOD_Api"; }
        }

        /// <summary>
        ///   We don't have to add any assemblies here
        /// </summary>
        public string[] ReferencedAssemblies
        {
            get { return new string[0]; }
        }

        /// <summary>
        ///   We use the default namespace, so we don't have any to add
        /// </summary>
        public string[] NamespaceAdditions
        {
            get { return new string[0]; }
        }

        #endregion

        public void Dispose()
        {
        }

        public override Object InitializeLifetimeService()
        {
            ILease lease = (ILease)base.InitializeLifetimeService();

            if (lease.CurrentState == LeaseState.Initial)
            {
                lease.InitialLeaseTime = TimeSpan.FromMinutes(0);
                //                lease.RenewOnCallTime = TimeSpan.FromSeconds(10.0);
                //                lease.SponsorshipTimeout = TimeSpan.FromMinutes(1.0);
            }
            return lease;
        }

        internal void modError(string msg)
        {
            throw new Exception("MOD Runtime Error: " + msg);
        }

        //
        //Dumps an error message on the debug console.
        //

        internal void modShoutError(string message)
        {
            if (message.Length > 1023)
                message = message.Substring(0, 1023);

            IChatModule chatModule = World.RequestModuleInterface<IChatModule>();
            if (chatModule != null)
                chatModule.SimChat(message, ChatTypeEnum.Shout, ScriptBaseClass.DEBUG_CHANNEL,
                                   m_host.ParentEntity.RootChild.AbsolutePosition, m_host.Name, m_host.UUID, true, World);

            IWorldComm wComm = World.RequestModuleInterface<IWorldComm>();
            wComm.DeliverMessage(ChatTypeEnum.Shout, ScriptBaseClass.DEBUG_CHANNEL, m_host.Name, m_host.UUID, message);
        }
    }
}