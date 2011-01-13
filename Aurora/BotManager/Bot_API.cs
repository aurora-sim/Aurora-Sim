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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Lifetime;
using System.Xml;
using Aurora.Framework;
using OpenMetaverse;
using Nini.Config;
using OpenSim;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

using LSL_Float = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLFloat;
using LSL_Integer = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLInteger;
using LSL_Key = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_List = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.list;
using LSL_Rotation = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Quaternion;
using LSL_String = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_Vector = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Vector3;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;
using OpenSim.Services.Interfaces;
using Aurora.ScriptEngine.AuroraDotNetEngine;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.APIs
{
    [Serializable]
    public class Bot_Api : MarshalByRefObject, IBot_Api, IScriptApi
    {
        internal IScriptModulePlugin m_ScriptEngine;
        internal SceneObjectPart m_host;
        internal ScriptProtectionModule ScriptProtection;
        internal UUID m_itemID;

        public void Initialize(IScriptModulePlugin ScriptEngine, SceneObjectPart host, uint localID, UUID itemID, ScriptProtectionModule module)
        {
            m_itemID = itemID;
            m_ScriptEngine = ScriptEngine;
            m_host = host;
            ScriptProtection = module;
        }

        public IScriptApi Copy()
        {
            return new Bot_Api();
        }

        public string Name
        {
            get { return "bot"; }
        }

        public string InterfaceName
        {
            get { return "IBot_Api"; }
        }

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

        public Scene World
        {
            get { return m_host.ParentGroup.Scene; }
        }

        public string botCreateBot(string FirstName, string LastName, string appearanceToClone)
        {
            IBotManager manager = World.RequestModuleInterface<IBotManager>();
            //if (manager != null)
            //    return manager.CreateAvatar(FirstName, LastName, UUID.Parse(appearanceToClone)).ToString();
            return "";
        }

        public void botSetMap(string keyOfBot, LSL_List positions, LSL_List movementType)
        {
            List<Vector3> PositionsMap = new List<Vector3>();
            for(int i = 0; i < positions.Length; i++)
            {
                LSL_Vector pos = positions.GetVector3Item(i);
                PositionsMap.Add(new Vector3((float)pos.x, (float)pos.y, (float)pos.z));
            }
            List<TravelMode> TravelMap = new List<TravelMode>();
            for(int i = 0; i < movementType.Length; i++)
            {
                LSL_Integer travel = movementType.GetLSLIntegerItem(i);
                TravelMap.Add((TravelMode)travel.value);
            }

            IBotManager manager = World.RequestModuleInterface<IBotManager>();
            if (manager != null)
                manager.SetBotMap(UUID.Parse(keyOfBot), PositionsMap, TravelMap);
        }

        public void botPause(string bot)
        {
            IBotManager manager = World.RequestModuleInterface<IBotManager>();
            if (manager != null)
                manager.PauseAutoMove(UUID.Parse(bot));
        }

        public void botUnPause(string bot)
        {
            IBotManager manager = World.RequestModuleInterface<IBotManager>();
            if (manager != null)
                manager.UnpauseAutoMove(UUID.Parse(bot));
        }

        public void botStop(string bot)
        {
            IBotManager manager = World.RequestModuleInterface<IBotManager>();
            if (manager != null)
                manager.StopAutoMove(UUID.Parse(bot));
        }

        public void botStart(string bot)
        {
            IBotManager manager = World.RequestModuleInterface<IBotManager>();
            if (manager != null)
                manager.EnableAutoMove(UUID.Parse(bot));
        }
    }
}
