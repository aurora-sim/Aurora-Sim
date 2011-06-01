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
using Aurora.ScriptEngine.AuroraDotNetEngine.Plugins;

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

namespace Aurora.ScriptEngine.AuroraDotNetEngine.APIs
{
    [Serializable]
    public class AA_Api : MarshalByRefObject, IAA_Api, IScriptApi
    {
        internal IScriptModulePlugin m_ScriptEngine;
        internal ISceneChildEntity m_host;
        internal IAssetConnector AssetConnector;
        internal ScriptProtectionModule ScriptProtection;
        internal UUID m_itemID;

        public void Initialize (IScriptModulePlugin ScriptEngine, ISceneChildEntity host, uint localID, UUID itemID, ScriptProtectionModule module)
        {
            m_itemID = itemID;
            m_ScriptEngine = ScriptEngine;
            m_host = host;
            ScriptProtection = module;
            AssetConnector = Aurora.DataManager.DataManager.RequestPlugin<IAssetConnector>();
        }

        public string Name
        {
            get { return "aa"; }
        }

        public string InterfaceName
        {
            get { return "IAA_Api"; }
        }

        /// <summary>
        /// We don't have to add any assemblies here
        /// </summary>
        public string[] ReferencedAssemblies
        {
            get { return new string[0]; }
        }

        /// <summary>
        /// We use the default namespace, so we don't have any to add
        /// </summary>
        public string[] NamespaceAdditions
        {
            get { return new string[0]; }
        }

        public IScriptApi Copy()
        {
            return new AA_Api();
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

        public void aaSetCloudDensity(LSL_Float density)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "AASetCloudDensity", m_host, "AA");
            if (!World.Permissions.CanIssueEstateCommand(m_host.OwnerID, false))
                return;
            ICloudModule CloudModule = World.RequestModuleInterface<ICloudModule>();
            if (CloudModule == null)
                return;
            CloudModule.SetCloudDensity((float)density);
        }

        public void aaUpdateDatabase(LSL_String key, LSL_String value, LSL_String token)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "AAUpdateDatabase", m_host, "AA");
            AssetConnector.UpdateLSLData(token.m_string, key.m_string, value.m_string);
        }

        public LSL_List aaQueryDatabase(LSL_String key, LSL_String token)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "AAQueryDatabase", m_host, "AA");

            List<string> query = AssetConnector.FindLSLData(token.m_string, key.m_string);
            LSL_List list = new LSL_Types.list(query.ToArray());
            return list;
        }

        public LSL_String aaSerializeXML(LSL_List keys, LSL_List values)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "AASerializeXML", m_host, "AA");
            XmlDocument doc = new XmlDocument();
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys.GetLSLStringItem(i);
                string value = values.GetLSLStringItem(i);
                XmlNode node = doc.CreateNode(XmlNodeType.Element, key, "");
                node.InnerText = value;
                doc.AppendChild(node);
            }
            return new LSL_String(doc.OuterXml);
        }

        public LSL_List aaDeserializeXMLKeys(LSL_String xmlFile)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "AADeserializeXMLKeys", m_host, "AA");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlFile.m_string);
            XmlNodeList children = doc.ChildNodes;
            LSL_List keys = new LSL_Types.list();
            foreach (XmlNode node in children)
            {
                keys.Add(node.Name);
            }
            return keys;
        }

        public LSL_List aaDeserializeXMLValues(LSL_String xmlFile)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "AADeserializeXMLValues", m_host, "AA");
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlFile.m_string);
            XmlNodeList children = doc.ChildNodes;
            LSL_List values = new LSL_Types.list();
            foreach (XmlNode node in children)
            {
                values.Add(node.InnerText);
            }
            return values;
        }

        public void aaSetConeOfSilence(LSL_Float radius)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "AASetConeOfSilence", m_host, "AA");
            if(World.Permissions.IsAdministrator(m_host.OwnerID))
                m_host.SetConeOfSilence(radius.value);
        }

        public void aaJoinCombatTeam(LSL_Key uuid, LSL_String team)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "AAJoinCombatTeam", m_host, "AA");
            UUID avID;
            if (UUID.TryParse(uuid, out avID))
            {
                IScenePresence SP = World.GetScenePresence(avID);
                if (SP != null)
                {
                    ICombatPresence CP = SP.RequestModuleInterface<ICombatPresence>();
                    if (CP != null)
                    {
                        if (team.m_string == "No Team")
                        {
                            SP.ControllingClient.SendAlertMessage("You cannot join this team.");
                            return;
                        }
                        CP.Team = team.m_string;
                    }
                }
            }
        }

        public void aaLeaveCombat(LSL_Key uuid)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "AALeaveCombat", m_host, "AA");
            UUID avID;
            if (UUID.TryParse(uuid, out avID))
            {
                IScenePresence SP = World.GetScenePresence(avID);
                if (SP != null)
                {
                    ICombatPresence CP = SP.RequestModuleInterface<ICombatPresence>();
                    if (CP != null)
                    {
                        CP.LeaveCombat();
                    }
                }
            }
        }

        public void aaJoinCombat(LSL_Key uuid)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "AAJoinCombat", m_host, "AA");
            UUID avID;
            if (UUID.TryParse(uuid, out avID))
            {
                IScenePresence SP = World.GetScenePresence(avID);
                if (SP != null)
                {
                    ICombatPresence CP = SP.RequestModuleInterface<ICombatPresence>();
                    if (CP != null)
                    {
                        CP.JoinCombat();
                    }
                }
            }
        }

        public LSL_Float aaGetHealth(LSL_Key uuid)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "AAGetHealth", m_host, "AA");
            UUID avID;
            if (UUID.TryParse(uuid, out avID))
            {
                IScenePresence SP = World.GetScenePresence(avID);
                if (SP != null)
                {
                    ICombatPresence cp = SP.RequestModuleInterface<ICombatPresence>();
                    return new LSL_Float(cp.Health);
                }
            }
            return new LSL_Float(-1);
        }

        public LSL_String aaGetTeam(LSL_Key uuid)
        {
            UUID avID;
            if (UUID.TryParse(uuid, out avID))
            {
                ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "AAGetTeam", m_host, "AA");
                IScenePresence SP = World.GetScenePresence(avID);
                if (SP != null)
                {
                    ICombatPresence CP = SP.RequestModuleInterface<ICombatPresence>();
                    if (CP != null)
                    {
                        return CP.Team;
                    }
                }
            }
            return "No Team";
        }

        public LSL_List aaGetTeamMembers(LSL_String team)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "AAGetTeamMembers", m_host, "AA");
            List<UUID> Members = new List<UUID>();
            ICombatModule module = World.RequestModuleInterface<ICombatModule>();
            if (module != null)
            {
                Members = module.GetTeammates(team);
            }
            LSL_List members = new LSL_Types.list(Members.ToArray());
            return members;
        }

        public LSL_String aaGetLastOwner()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "AAGetLastOwner", m_host, "AA");
            return new LSL_String(m_host.LastOwnerID.ToString());
        }

        public LSL_String aaGetLastOwner(LSL_String PrimID)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "AAGetLastOwner", m_host, "AA");
            ISceneChildEntity part = m_host.ParentEntity.Scene.GetSceneObjectPart(UUID.Parse(PrimID.m_string));
            if (part != null)
                return new LSL_String(part.LastOwnerID.ToString());
            else
                return ScriptBaseClass.NULL_KEY;
        }

        public void aaSayDistance(int channelID, LSL_Float Distance, string text)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.VeryLow, "AASayDistance", m_host, "AA");
            
            if (text.Length > 1023)
                text = text.Substring(0, 1023);

            IChatModule chatModule = World.RequestModuleInterface<IChatModule>();
            if(chatModule != null)
                chatModule.SimChat(text, ChatTypeEnum.Custom, channelID,
                    m_host.ParentEntity.RootChild.AbsolutePosition, m_host.Name,
                    m_host.UUID, false, false, (float)Distance.value, UUID.Zero, World);

            IWorldComm wComm = World.RequestModuleInterface<IWorldComm>();
            if (wComm != null)
                wComm.DeliverMessage(ChatTypeEnum.Custom, channelID, m_host.Name, m_host.UUID, text, (float)Distance.value);
        }

        public void aaSayTo(string userID, string text)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "AASayTo", m_host, "AA");
            

            UUID AgentID;
            if(UUID.TryParse(userID, out AgentID))
            {
                IChatModule chatModule = World.RequestModuleInterface<IChatModule>();
                if(chatModule != null)
                    chatModule.SimChatBroadcast(text, ChatTypeEnum.SayTo, 0,
                                       m_host.AbsolutePosition, m_host.Name, m_host.UUID, false, AgentID, World);
            }
        }

        public LSL_String aaGetText()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "AAGetText", m_host, "AA");
            return m_host.Text;
        }

        public void aaRaiseError(string message)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "AARaiseError", m_host, "AA");
            m_ScriptEngine.AddToObjectQueue(m_host.UUID, "on_error", new DetectParams[0], -1, new object[] { message });
            throw new EventAbortException();
        }

        public void aaFreezeAvatar(string ID)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "AAFreezeAvatar", m_host, "AA");
            UUID AgentID = UUID.Zero;
            if (UUID.TryParse(ID, out AgentID))
            {
                IScenePresence SP;
                if (World.TryGetScenePresence(AgentID, out SP))
                {
                    ICombatModule module = World.RequestModuleInterface<ICombatModule>();
                    if (module.CheckCombatPermission(AgentID) || World.Permissions.IsAdministrator(AgentID))
                    {
                        //If they have combat permission on, do it whether the threat level is enabled or not
                        SP.AllowMovement = false;
                        return;
                    }

                    ScriptProtection.CheckThreatLevel(ThreatLevel.High, "AAThawAvatar", m_host, "AA");
                    SP.AllowMovement = false;
                }
            }
        }

        public void aaThawAvatar(string ID)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "AAThawAvatar", m_host, "AA");
            UUID AgentID = UUID.Zero;
            if (UUID.TryParse(ID, out AgentID))
            {
                IScenePresence SP;
                if (World.TryGetScenePresence(AgentID, out SP))
                {
                    ICombatModule module = World.RequestModuleInterface<ICombatModule>();
                    if (module.CheckCombatPermission(AgentID) || World.Permissions.IsAdministrator(AgentID))
                    {
                        //If they have combat permission on, do it whether the threat level is enabled or not
                        SP.AllowMovement = true;
                        return;
                    }

                    ScriptProtection.CheckThreatLevel(ThreatLevel.High, "AAThawAvatar", m_host, "AA");
                    SP.AllowMovement = true;
                }
            }
        }

        public void aaRegisterToAvatarDeathEvents()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "AARegisterToAvatarDeathEvents", m_host, "AA");
            ICombatModule module = World.RequestModuleInterface<ICombatModule>();
            module.RegisterToAvatarDeathEvents(m_host.UUID);
        }

        public void aaDeregisterFromAvatarDeathEvents()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "AADeregisterFromAvatarDeathEvents", m_host, "AA");
            ICombatModule module = World.RequestModuleInterface<ICombatModule>();
            module.DeregisterFromAvatarDeathEvents(m_host.UUID);
        }

        //This asks the agent whether they would like to participate in the combat
        public void aaRequestCombatPermission(string ID)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "AARequestCombatPermission", m_host, "AA");
            IScenePresence SP;
            UUID AgentID = UUID.Zero;
            if (UUID.TryParse(ID, out AgentID))
            {
                if (World.TryGetScenePresence(AgentID, out SP))
                {
                    ICombatModule module = World.RequestModuleInterface<ICombatModule>();
                    if(!module.CheckCombatPermission(AgentID)) //Don't ask multiple times
                        RequestPermissions(SP, ScriptBaseClass.PERMISSION_COMBAT);
                }
            }
        }

        public bool aaGetWalkDisabled(string vPresenceId)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "AAGetWalkDisabled", m_host, "AA");
            TaskInventoryItem item;

            lock (m_host.TaskInventory)
            {
                if (!m_host.TaskInventory.ContainsKey(InventorySelf()))
                    return false;
                else
                    item = m_host.TaskInventory[InventorySelf()];
            }

            if (item.PermsGranter != UUID.Zero)
            {
                IScenePresence presence = World.GetScenePresence(item.PermsGranter);

                if (presence != null)
                {
                    if ((item.PermsMask & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) != 0)
                    {
                        UUID avatarId = new UUID(vPresenceId);
                        IScenePresence avatar = World.GetScenePresence(avatarId);
                        return avatar.ForceFly;
                    }
                }
            }
            return false;
        }

        public void aaSetWalkDisabled(string vPresenceId, bool vbValue)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "AASetWalkDisabled", m_host, "AA");
            TaskInventoryItem item;

            lock (m_host.TaskInventory)
            {
                if (!m_host.TaskInventory.ContainsKey(InventorySelf()))
                    return;
                else
                    item = m_host.TaskInventory[InventorySelf()];
            }

            if (item.PermsGranter != UUID.Zero)
            {
                IScenePresence presence = World.GetScenePresence(item.PermsGranter);

                if (presence != null)
                {
                    if ((item.PermsMask & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) != 0)
                    {
                        UUID avatarId = new UUID(vPresenceId);
                        IScenePresence avatar = World.GetScenePresence(avatarId);
                        avatar.ForceFly = vbValue;
                    }
                }
            }
        }

        public bool aaGetFlyDisabled(string vPresenceId)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "AAGetFlyDisabled", m_host, "AA");
            TaskInventoryItem item;

            lock (m_host.TaskInventory)
            {
                if (!m_host.TaskInventory.ContainsKey(InventorySelf()))
                    return false;
                else
                    item = m_host.TaskInventory[InventorySelf()];
            }

            if (item.PermsGranter != UUID.Zero)
            {
                IScenePresence presence = World.GetScenePresence(item.PermsGranter);

                if (presence != null)
                {
                    if ((item.PermsMask & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) != 0)
                    {
                        UUID avatarId = new UUID(vPresenceId);
                        IScenePresence avatar = World.GetScenePresence(avatarId);
                        return avatar.FlyDisabled;
                    }
                }
            }
            return false;
        }

        public void aaSetFlyDisabled(string vPresenceId, bool vbValue)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "AASetFlyDisabled", m_host, "AA");
            TaskInventoryItem item;

            lock (m_host.TaskInventory)
            {
                if (!m_host.TaskInventory.ContainsKey(InventorySelf()))
                    return;
                else
                    item = m_host.TaskInventory[InventorySelf()];
            }

            if (item.PermsGranter != UUID.Zero)
            {
                IScenePresence presence = World.GetScenePresence(item.PermsGranter);

                if (presence != null)
                {
                    if ((item.PermsMask & ScriptBaseClass.PERMISSION_TAKE_CONTROLS) != 0)
                    {
                        UUID avatarId = new UUID(vPresenceId);
                        IScenePresence avatar = World.GetScenePresence(avatarId);
                        avatar.FlyDisabled = vbValue;
                    }
                }
            }
        }

        public string aaAvatarFullName2Key(string fullname)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "AAAvatarFullName2Key", m_host, "AA");
            UserAccount account = World.UserAccountService.GetUserAccount(World.RegionInfo.ScopeID, fullname);

            if (null == account)
                return UUID.Zero.ToString();

            return account.PrincipalID.ToString();
        }

        private void RequestPermissions (IScenePresence presence, int perm)
        {
            UUID invItemID = InventorySelf();

            if (invItemID == UUID.Zero)
                return; // Not in a prim? How??

            string ownerName = "";
            IScenePresence ownerPresence = World.GetScenePresence (m_host.ParentEntity.OwnerID);
            if (ownerPresence == null)
                ownerName = resolveName(m_host.OwnerID);
            else
                ownerName = ownerPresence.Name;

            if (ownerName == String.Empty)
                ownerName = "(hippos)";

            presence.ControllingClient.OnScriptAnswer += handleScriptAnswer;

            presence.ControllingClient.SendScriptQuestion(
                m_host.UUID, m_host.ParentEntity.RootChild.Name, ownerName, invItemID, perm);
        }

        void handleScriptAnswer(IClientAPI client, UUID taskID, UUID itemID, int answer)
        {
            if (taskID != m_host.UUID)
                return;

            UUID invItemID = InventorySelf();

            if (invItemID == UUID.Zero)
                return;

            if (invItemID == itemID)
                return;

            client.OnScriptAnswer -= handleScriptAnswer;

            ICombatModule module = World.RequestModuleInterface<ICombatModule>();
            //Tell the combat module about this new permission
            if ((answer & ScriptBaseClass.PERMISSION_COMBAT) == ScriptBaseClass.PERMISSION_COMBAT)
                module.AddCombatPermission(client.AgentId);

            //Tell the prim about the new permissions
            m_ScriptEngine.PostScriptEvent(m_itemID, m_host.UUID, new EventParams(
                    "run_time_permissions", new Object[] {
                    new LSL_Integer(answer) },
                    new DetectParams[0]));
        }

        public void aaSetCharacterStat(string UUIDofAv, string StatName, float statValue)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "AASetCharacterStat", m_host, "AA");
            UUID avatarId = new UUID(UUIDofAv);
            IScenePresence presence = World.GetScenePresence (avatarId);
            if (presence != null)
            {
                ICombatPresence cp = presence.RequestModuleInterface<ICombatPresence>();
                cp.SetStat(StatName, statValue);
            }
        }

        public void aaSetCenterOfGravity(LSL_Types.Vector3 position)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.High, "AASetCenterOfGravity", m_host, "AA");
            if (m_host.ParentEntity.Scene.Permissions.CanIssueEstateCommand (m_host.OwnerID, true))
                m_host.ParentEntity.Scene.PhysicsScene.PointOfGravity = new Vector3 ((float)position.x, (float)position.y, (float)position.z);
        }

        #region Helpers

        protected UUID InventorySelf()
        {
            UUID invItemID = new UUID();

            lock (m_host.TaskInventory)
            {
                foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory)
                {
                    if (inv.Value.Type == 10 && inv.Value.ItemID == m_itemID)
                    {
                        invItemID = inv.Key;
                        break;
                    }
                }
            }

            return invItemID;
        }

        public string resolveName(UUID objecUUID)
        {
            // try avatar username surname
            UserAccount account = World.UserAccountService.GetUserAccount(World.RegionInfo.ScopeID, objecUUID);
            if (account != null)
                return account.Name;

            // try an scene object
            ISceneChildEntity SOP = World.GetSceneObjectPart (objecUUID);
            if (SOP != null)
                return SOP.Name;

            ISceneChildEntity SensedObject;
            if(!World.TryGetPart(objecUUID, out SensedObject))
            {
                IGroupsModule groups = World.RequestModuleInterface<IGroupsModule>();
                if (groups != null)
                {
                    GroupRecord gr = groups.GetGroupRecord(objecUUID);
                    if (gr != null)
                        return gr.GroupName;
                }
                return String.Empty;
            }

            return SensedObject.Name;
        }

        #endregion
    }
}
