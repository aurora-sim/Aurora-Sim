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

using Aurora.Framework;
using Aurora.ScriptEngine.AuroraDotNetEngine;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;
using LSL_Float = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLFloat;
using LSL_Integer = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLInteger;
using LSL_Key = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_List = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.list;
using LSL_Rotation = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Quaternion;
using LSL_String = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_Vector = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Vector3;
using ThreatLevel = Aurora.ScriptEngine.AuroraDotNetEngine.ThreatLevel;

namespace Aurora.BotManager
{
    [Serializable]
    public class Bot_Api : MarshalByRefObject, IBot_Api, IScriptApi
    {
        internal ScriptProtectionModule ScriptProtection;
        internal IScriptModulePlugin m_ScriptEngine;
        internal ISceneChildEntity m_host;
        internal UUID m_itemID;

        /// <summary>
        ///     Created by John Sibly @ http://stackoverflow.com/questions/52797/c-how-do-i-get-the-path-of-the-assembly-the-code-is-in
        /// </summary>
        public static string AssemblyFileName
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetFileName(path);
            }
        }

        public IScene World
        {
            get { return m_host.ParentEntity.Scene; }
        }

        #region IBot_Api Members

        public LSL_String botCreateBot(string FirstName, string LastName, string appearanceToClone, LSL_Vector startPos)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "botCreateBot", m_host, "bot", m_itemID))
                return "";
            IBotManager manager = World.RequestModuleInterface<IBotManager>();
            if (manager != null)
                return
                    new LSL_String(
                        manager.CreateAvatar(FirstName, LastName, m_host.ParentEntity.Scene,
                                             UUID.Parse(appearanceToClone), m_host.OwnerID,
                                             new Vector3((float) startPos.x, (float) startPos.y, (float) startPos.z)).
                                ToString());
            return new LSL_String("");
        }

        public LSL_Vector botGetWaitingTime(LSL_Integer waitTime)
        {
            return new LSL_Vector(waitTime, 0, 0);
        }

        public void botPauseMovement(string bot)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "botPauseMovement", m_host, "bot", m_itemID))
                return;
            IBotManager manager = World.RequestModuleInterface<IBotManager>();
            if (manager != null)
                manager.PauseMovement(UUID.Parse(bot), m_host.OwnerID);
        }

        public void botResumeMovement(string bot)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "botResumeMovement", m_host, "bot", m_itemID))
                return;
            IBotManager manager = World.RequestModuleInterface<IBotManager>();
            if (manager != null)
                manager.ResumeMovement(UUID.Parse(bot), m_host.OwnerID);
        }

        public void botSetShouldFly(string keyOfBot, int ShouldFly)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "botSetShouldFly", m_host, "bot", m_itemID))
                return;
            IBotManager manager = World.RequestModuleInterface<IBotManager>();
            if (manager != null)
                manager.SetBotShouldFly(UUID.Parse(keyOfBot), ShouldFly == 1, m_host.OwnerID);
        }

        public void botSetMap(string keyOfBot, LSL_List positions, LSL_List movementType, LSL_Integer flags)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "botSetMap", m_host, "bot", m_itemID)) return;
            List<Vector3> PositionsMap = new List<Vector3>();
            for (int i = 0; i < positions.Length; i++)
            {
                LSL_Vector pos = positions.GetVector3Item(i);
                PositionsMap.Add(new Vector3((float) pos.x, (float) pos.y, (float) pos.z));
            }
            List<TravelMode> TravelMap = new List<TravelMode>();
            for (int i = 0; i < movementType.Length; i++)
            {
                LSL_Integer travel = movementType.GetLSLIntegerItem(i);
                TravelMap.Add((TravelMode) travel.value);
            }

            IBotManager manager = World.RequestModuleInterface<IBotManager>();
            if (manager != null)
                manager.SetBotMap(UUID.Parse(keyOfBot), PositionsMap, TravelMap, flags.value, m_host.OwnerID);
        }

        public void botRemoveBot(string bot)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "botRemoveBot", m_host, "bot", m_itemID))
                return;
            IBotManager manager = World.RequestModuleInterface<IBotManager>();
            if (manager != null)
                manager.RemoveAvatar(UUID.Parse(bot), m_host.ParentEntity.Scene, m_host.OwnerID);
        }

        public void botFollowAvatar(string bot, string avatarName, LSL_Float startFollowDistance,
                                    LSL_Float endFollowDistance)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "botFollowAvatar", m_host, "bot", m_itemID))
                return;
            IBotManager manager = World.RequestModuleInterface<IBotManager>();
            if (manager != null)
                manager.FollowAvatar(UUID.Parse(bot), avatarName, (float) startFollowDistance, (float) endFollowDistance,
                                     false, Vector3.Zero,
                                     m_host.OwnerID);
        }

        public void botStopFollowAvatar(string bot)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "botStopFollowAvatar", m_host, "bot", m_itemID))
                return;
            IBotManager manager = World.RequestModuleInterface<IBotManager>();
            if (manager != null)
                manager.StopFollowAvatar(UUID.Parse(bot), m_host.OwnerID);
        }

        public void botSendChatMessage(string bot, string message, int channel, int sayType)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "botSendChatMessage", m_host, "bot", m_itemID))
                return;
            IBotManager manager = World.RequestModuleInterface<IBotManager>();
            if (manager != null)
                manager.SendChatMessage(UUID.Parse(bot), message, sayType, channel, m_host.OwnerID);
        }

        public void botSendIM(string bot, string user, string message)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "botSendIM", m_host, "bot", m_itemID))
                return;
            IBotManager manager = World.RequestModuleInterface<IBotManager>();
            if (manager != null)
                manager.SendIM(UUID.Parse(bot), UUID.Parse(user), message, m_host.OwnerID);
        }

        public void botTouchObject(string bot, string objectID)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "botTouchObject", m_host, "bot", m_itemID))
                return;
            SurfaceTouchEventArgs touchArgs = new SurfaceTouchEventArgs();

            IScenePresence sp = World.GetScenePresence(UUID.Parse(bot));
            if (sp == null)
                return;
            ISceneChildEntity child = World.GetSceneObjectPart(UUID.Parse(objectID));
            if (child == null)
                throw new Exception("Failed to find entity to touch");

            World.EventManager.TriggerObjectGrab(child.ParentEntity.RootChild, child, Vector3.Zero, sp.ControllingClient,
                                                 touchArgs);
            World.EventManager.TriggerObjectGrabbing(child.ParentEntity.RootChild, child, Vector3.Zero,
                                                     sp.ControllingClient, touchArgs);
            World.EventManager.TriggerObjectDeGrab(child.ParentEntity.RootChild, child, sp.ControllingClient, touchArgs);
        }

        public void botSitObject(string bot, string objectID, LSL_Vector offset)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "botTouchObject", m_host, "bot", m_itemID))
                return;
            IScenePresence sp = World.GetScenePresence(UUID.Parse(bot));
            if (sp == null)
                return;
            ISceneChildEntity child = World.GetSceneObjectPart(UUID.Parse(objectID));
            if (child == null)
                throw new Exception("Failed to find entity to sit on");

            sp.HandleAgentRequestSit(sp.ControllingClient, UUID.Parse(objectID),
                                     new Vector3((float) offset.x, (float) offset.y, (float) offset.z));
        }

        public void botStandUp(string bot)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "botStandUp", m_host, "bot", m_itemID)) return;
            IScenePresence sp = World.GetScenePresence(UUID.Parse(bot));
            if (sp == null)
                return;
            sp.StandUp();
        }

        public void botAddTag(string bot, string tag)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "botAddTag", m_host, "bot", m_itemID)) return;
            IBotManager manager = World.RequestModuleInterface<IBotManager>();
            if (manager != null)
                manager.AddTagToBot(UUID.Parse(bot), tag, m_host.OwnerID);
        }

        public LSL_List botGetBotsWithTag(string tag)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "botGetBotsWithTag", m_host, "bot", m_itemID))
                return new LSL_List();
            IBotManager manager = World.RequestModuleInterface<IBotManager>();
            List<UUID> bots = new List<UUID>();
            if (manager != null)
                bots = manager.GetBotsWithTag(tag);
            LSL_List b = new LSL_List();
            foreach (UUID bot in bots)
                b.Add(bot.ToString());

            return b;
        }

        public void botRemoveBotsWithTag(string tag)
        {
            if (
                !ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "botRemoveBotsWithTag", m_host, "bot", m_itemID))
                return;
            IBotManager manager = World.RequestModuleInterface<IBotManager>();
            if (manager != null)
                manager.RemoveBots(tag, m_host.OwnerID);
        }

        #endregion

        #region IScriptApi Members

        public void Initialize(IScriptModulePlugin ScriptEngine, ISceneChildEntity host, uint localID, UUID itemID,
                               ScriptProtectionModule module)
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
        ///     We have to add a ref here, as this API is NOT inside of the script engine
        ///     So we add the referenced assembly to ourselves
        /// </summary>
        public string[] ReferencedAssemblies
        {
            get
            {
                return new string[1]
                           {
                               AssemblyFileName
                           };
            }
        }

        /// <summary>
        ///     We use "Aurora.BotManager", and that isn't a default namespace, so we need to add it
        /// </summary>
        public string[] NamespaceAdditions
        {
            get { return new string[1] {"Aurora.BotManager"}; }
        }

        #endregion

        public void Dispose()
        {
        }

        public override Object InitializeLifetimeService()
        {
            ILease lease = (ILease) base.InitializeLifetimeService();

            if (lease.CurrentState == LeaseState.Initial)
            {
                lease.InitialLeaseTime = TimeSpan.FromMinutes(0);
                //                lease.RenewOnCallTime = TimeSpan.FromSeconds(10.0);
                //                lease.SponsorshipTimeout = TimeSpan.FromMinutes(1.0);
            }
            return lease;
        }
    }
}