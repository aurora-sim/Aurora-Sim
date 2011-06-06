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
using System.IO;
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
using Aurora.ScriptEngine.AuroraDotNetEngine;

namespace Aurora.BotManager
{
    [Serializable]
    public class Bot_Api : MarshalByRefObject, IBot_Api, IScriptApi
    {
        internal IScriptModulePlugin m_ScriptEngine;
        internal ISceneChildEntity m_host;
        internal ScriptProtectionModule ScriptProtection;
        internal UUID m_itemID;

        public void Initialize (IScriptModulePlugin ScriptEngine, ISceneChildEntity host, uint localID, UUID itemID, ScriptProtectionModule module)
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

        /// <summary>
        /// We have to add a ref here, as this API is NOT inside of the script engine
        /// So we add the referenced assembly to ourselves
        /// </summary>
        public string[] ReferencedAssemblies
        {
            get { return new string[1] {
                AssemblyFileName
            }; }
        }

        /// <summary>
        /// We use "Aurora.BotManager", and that isn't a default namespace, so we need to add it
        /// </summary>
        public string[] NamespaceAdditions
        {
            get { return new string[1] { "Aurora.BotManager" }; }
        }

        /// <summary>
        /// Created by John Sibly @ http://stackoverflow.com/questions/52797/c-how-do-i-get-the-path-of-the-assembly-the-code-is-in
        /// </summary>
        static public string AssemblyFileName
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetFileName(path);
            }
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

        public IScene World
        {
            get { return m_host.ParentEntity.Scene; }
        }

        public string botCreateBot(string FirstName, string LastName, string appearanceToClone, LSL_Vector startPos)
        {
            ScriptProtection.CheckThreatLevel (ThreatLevel.Moderate, "botCreateBot", m_host, "bot");
            IBotManager manager = World.RequestModuleInterface<IBotManager>();
            if (manager != null)
                return manager.CreateAvatar (FirstName, LastName, m_host.ParentEntity.Scene, UUID.Parse (appearanceToClone), m_host.OwnerID, new Vector3 ((float)startPos.x, (float)startPos.y, (float)startPos.z)).ToString ();
            return "";
        }

        public LSL_Vector botGetWaitingTime (LSL_Integer waitTime)
        {
            return new LSL_Vector (waitTime, 0, 0);
        }

        public void botPauseMovement (string bot)
        {
            ScriptProtection.CheckThreatLevel (ThreatLevel.Moderate, "botPauseMovement", m_host, "bot");
            IBotManager manager = World.RequestModuleInterface<IBotManager> ();
            if (manager != null)
                manager.PauseMovement (UUID.Parse (bot));
        }

        public void botResumeMovement (string bot)
        {
            ScriptProtection.CheckThreatLevel (ThreatLevel.Moderate, "botResumeMovement", m_host, "bot");
            IBotManager manager = World.RequestModuleInterface<IBotManager> ();
            if (manager != null)
                manager.ResumeMovement (UUID.Parse (bot));
        }

        public void botSetShouldFly (string keyOfBot, int ShouldFly)
        {
            ScriptProtection.CheckThreatLevel (ThreatLevel.Moderate, "botSetShouldFly", m_host, "bot");
            IBotManager manager = World.RequestModuleInterface<IBotManager> ();
            if (manager != null)
               manager.SetBotShouldFly (UUID.Parse(keyOfBot), ShouldFly == 1);
        }

        public void botSetMap(string keyOfBot, LSL_List positions, LSL_List movementType, LSL_Integer flags)
        {
            ScriptProtection.CheckThreatLevel (ThreatLevel.Moderate, "botSetMap", m_host, "bot");
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
                manager.SetBotMap(UUID.Parse(keyOfBot), PositionsMap, TravelMap, flags.value);
        }

        public void botRemoveBot (string bot)
        {
            ScriptProtection.CheckThreatLevel (ThreatLevel.Moderate, "botRemoveBot", m_host, "bot");
            IBotManager manager = World.RequestModuleInterface<IBotManager> ();
            if (manager != null)
                manager.RemoveAvatar (UUID.Parse (bot), m_host.ParentEntity.Scene);
        }

        public void botFollowAvatar (string bot, string avatarName, LSL_Float startFollowDistance, LSL_Float endFollowDistance)
        {
            ScriptProtection.CheckThreatLevel (ThreatLevel.Moderate, "botFollowAvatar", m_host, "bot");
            IBotManager manager = World.RequestModuleInterface<IBotManager> ();
            if (manager != null)
                manager.FollowAvatar (UUID.Parse (bot), avatarName, (float)startFollowDistance, (float)endFollowDistance);
        }

        public void botStopFollowAvatar (string bot)
        {
            ScriptProtection.CheckThreatLevel (ThreatLevel.Moderate, "botStopFollowAvatar", m_host, "bot");
            IBotManager manager = World.RequestModuleInterface<IBotManager> ();
            if (manager != null)
                manager.StopFollowAvatar (UUID.Parse (bot));
        }

        public void botSetPathMap (string bot, string pathMap, int x, int y, int cornerstoneX, int cornerstoneY)
        {
            ScriptProtection.CheckThreatLevel (ThreatLevel.Moderate, "botSetPathMap", m_host, "bot");
            IBotManager manager = World.RequestModuleInterface<IBotManager> ();
            if (manager != null)
                manager.ReadMap (UUID.Parse (bot), pathMap, x, y, cornerstoneX, cornerstoneY);
        }

        public void botFindPath (string bot, LSL_Vector startPos, LSL_Vector endPos)
        {
            ScriptProtection.CheckThreatLevel (ThreatLevel.Moderate, "botFindPath", m_host, "bot");
            IBotManager manager = World.RequestModuleInterface<IBotManager> ();
            if (manager != null)
                manager.FindPath (UUID.Parse (bot), new Vector3 ((float)startPos.x, (float)startPos.y, (float)startPos.z),
                    new Vector3 ((float)endPos.x, (float)endPos.y, (float)endPos.z));
        }

        public void botSendChatMessage (string bot, string message, int channel, int sayType)
        {
            ScriptProtection.CheckThreatLevel (ThreatLevel.Moderate, "botSendChatMessage", m_host, "bot");
            IBotManager manager = World.RequestModuleInterface<IBotManager> ();
            if (manager != null)
                manager.SendChatMessage (UUID.Parse (bot), message, sayType, channel);
        }
    }
}
