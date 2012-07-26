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
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Xml;
using Aurora.Framework;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Services.Interfaces;
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
    public class AA_Api : MarshalByRefObject, IAA_Api, IScriptApi
    {
        internal IAssetConnector AssetConnector;
        internal ScriptProtectionModule ScriptProtection;
        internal IScriptModulePlugin m_ScriptEngine;
        internal ISceneChildEntity m_host;
        internal UUID m_itemID;

        public IScene World
        {
            get { return m_host.ParentEntity.Scene; }
        }

        #region IAA_Api Members

        public void aaSetCloudDensity(LSL_Float density)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "aaSetCloudDensity", m_host, "AA", m_itemID))
                return;
            if (!World.Permissions.CanIssueEstateCommand(m_host.OwnerID, false))
            {
                ShoutError("You do not have estate permissions here.");
                return;
            }
            ICloudModule CloudModule = World.RequestModuleInterface<ICloudModule>();
            if (CloudModule == null)
                return;
            CloudModule.SetCloudDensity((float)density);
        }

        public void aaUpdateDatabase(LSL_String key, LSL_String value, LSL_String token)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "aaUpdateDatabase", m_host, "AA", m_itemID))
                return;
            AssetConnector.UpdateLSLData(token.m_string, key.m_string, value.m_string);
        }

        public LSL_List aaQueryDatabase(LSL_String key, LSL_String token)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "aaQueryDatabase", m_host, "AA", m_itemID))
                return new LSL_List();

            List<string> query = AssetConnector.FindLSLData(token.m_string, key.m_string);
            LSL_List list = new LSL_List(query.ToArray());
            return list;
        }

        public LSL_String aaSerializeXML(LSL_List keys, LSL_List values)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "aaSerializeXML", m_host, "AA", m_itemID))
                return new LSL_String();
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "aaDeserializeXMLKeys", m_host, "AA", m_itemID))
                return new LSL_List();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlFile.m_string);
            XmlNodeList children = doc.ChildNodes;
            LSL_List keys = new LSL_List();
            foreach (XmlNode node in children)
            {
                keys.Add(node.Name);
            }
            return keys;
        }

        public LSL_List aaDeserializeXMLValues(LSL_String xmlFile)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "aaDeserializeXMLValues", m_host, "AA",
                                                   m_itemID)) return new LSL_List();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlFile.m_string);
            XmlNodeList children = doc.ChildNodes;
            LSL_List values = new LSL_List();
            foreach (XmlNode node in children)
            {
                values.Add(node.InnerText);
            }
            return values;
        }

        public void aaSetConeOfSilence(LSL_Float radius)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "aaSetConeOfSilence", m_host, "AA", m_itemID))
                return;
            if (World.Permissions.IsGod(m_host.OwnerID))
                m_host.SetConeOfSilence(radius.value);
            else
                ShoutError("You do not have god permissions here.");
        }

        public void aaJoinCombatTeam(LSL_Key uuid, LSL_String team)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "aaJoinCombatTeam", m_host, "AA", m_itemID)) return;
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "aaLeaveCombat", m_host, "AA", m_itemID)) return;
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "aaJoinCombat", m_host, "AA", m_itemID)) return;
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "aaGetHealth", m_host, "AA", m_itemID))
                return new LSL_Float();
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
                if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "aaGetTeam", m_host, "AA", m_itemID))
                    return new LSL_String();
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
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "aaGetTeamMembers", m_host, "AA", m_itemID))
                return new LSL_List();
            List<UUID> Members = new List<UUID>();
            ICombatModule module = World.RequestModuleInterface<ICombatModule>();
            if (module != null)
            {
                Members = module.GetTeammates(team);
            }
            LSL_List members = new LSL_List();
            foreach (UUID member in Members)
                members.Add(new LSL_Key(member.ToString()));
            return members;
        }

        public LSL_String aaGetLastOwner()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "aaGetLastOwner", m_host, "AA", m_itemID))
                return new LSL_String();
            return new LSL_String(m_host.LastOwnerID.ToString());
        }

        public LSL_String aaGetLastOwner(LSL_String PrimID)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "aaGetLastOwner", m_host, "AA", m_itemID))
                return new LSL_String();
            ISceneChildEntity part = m_host.ParentEntity.Scene.GetSceneObjectPart(UUID.Parse(PrimID.m_string));
            if (part != null)
                return new LSL_String(part.LastOwnerID.ToString());
            else
                return ScriptBaseClass.NULL_KEY;
        }

        public void aaSayDistance(int channelID, LSL_Float Distance, string text)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.VeryLow, "aaSayDistance", m_host, "AA", m_itemID))
                return;

            if (text.Length > 1023)
                text = text.Substring(0, 1023);

            IChatModule chatModule = World.RequestModuleInterface<IChatModule>();
            if (chatModule != null)
                chatModule.SimChat(text, ChatTypeEnum.Custom, channelID,
                                   m_host.ParentEntity.RootChild.AbsolutePosition, m_host.Name,
                                   m_host.UUID, false, false, (float)Distance.value, UUID.Zero, World);

            IWorldComm wComm = World.RequestModuleInterface<IWorldComm>();
            if (wComm != null)
                wComm.DeliverMessage(ChatTypeEnum.Custom, channelID, m_host.Name, m_host.UUID, text,
                                     (float)Distance.value);
        }

        public void aaSayTo(string userID, string text)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "aaSayTo", m_host, "AA", m_itemID)) return;


            UUID AgentID;
            if (UUID.TryParse(userID, out AgentID))
            {
                IChatModule chatModule = World.RequestModuleInterface<IChatModule>();
                if (chatModule != null)
                    chatModule.SimChatBroadcast(text, ChatTypeEnum.SayTo, 0,
                                                m_host.AbsolutePosition, m_host.Name, m_host.UUID, false, AgentID, World);
            }
            else
                LSLError("Incorrect UUID format.");
        }

        public LSL_String aaGetText()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "aaGetText", m_host, "AA", m_itemID))
                return new LSL_String();
            return m_host.Text;
        }

        public LSL_Rotation aaGetTextColor()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "aaGetTextColor", m_host, "AA", m_itemID))
                return new LSL_Rotation();
            LSL_Rotation v = new LSL_Rotation(m_host.Color.R, m_host.Color.G, m_host.Color.B, m_host.Color.A);
            return v;
        }

        public void aaRaiseError(string message)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "aaRaiseError", m_host, "AA", m_itemID)) return;
            m_ScriptEngine.PostScriptEvent(m_itemID, m_host.UUID, "on_error", new object[] { message });
            throw new EventAbortException();
        }

        public void aaFreezeAvatar(string ID)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "aaFreezeAvatar", m_host, "AA", m_itemID))
                return;
            UUID AgentID = UUID.Zero;
            if (UUID.TryParse(ID, out AgentID))
            {
                IScenePresence SP;
                if (World.TryGetScenePresence(AgentID, out SP))
                {
                    ICombatModule module = World.RequestModuleInterface<ICombatModule>();
                    if (module.CheckCombatPermission(AgentID) || World.Permissions.IsGod(AgentID))
                    {
                        //If they have combat permission on, do it whether the threat level is enabled or not
                        SP.AllowMovement = false;
                        return;
                    }

                    if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "aaFreezeAvatar", m_host, "AA", m_itemID))
                        return;
                    SP.AllowMovement = false;
                }
            }
        }

        public void aaThawAvatar(string ID)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "aaThawAvatar", m_host, "AA", m_itemID))
                return;
            UUID AgentID = UUID.Zero;
            if (UUID.TryParse(ID, out AgentID))
            {
                IScenePresence SP;
                if (World.TryGetScenePresence(AgentID, out SP))
                {
                    ICombatModule module = World.RequestModuleInterface<ICombatModule>();
                    if (module.CheckCombatPermission(AgentID) || World.Permissions.IsGod(AgentID))
                    {
                        //If they have combat permission on, do it whether the threat level is enabled or not
                        SP.AllowMovement = true;
                        return;
                    }

                    if (!ScriptProtection.CheckThreatLevel(ThreatLevel.High, "aaThawAvatar", m_host, "AA", m_itemID))
                        return;
                    SP.AllowMovement = true;
                }
                else
                    LSLError("Agent does not exist in the sim.");
            }
            else
                LSLError("Incorrect UUID format.");
        }

        //This asks the agent whether they would like to participate in the combat
        public void aaRequestCombatPermission(string ID)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "aaRequestCombatPermission", m_host, "AA", m_itemID))
                return;
            IScenePresence SP;
            UUID AgentID = UUID.Zero;
            if (UUID.TryParse(ID, out AgentID))
            {
                if (World.TryGetScenePresence(AgentID, out SP))
                {
                    ICombatModule module = World.RequestModuleInterface<ICombatModule>();
                    if (!module.CheckCombatPermission(AgentID)) //Don't ask multiple times
                        RequestPermissions(SP, ScriptBaseClass.PERMISSION_COMBAT);
                }
                else
                    LSLError("Agent does not exist in the sim.");
            }
            else
                LSLError("Incorrect UUID format.");
        }

        public LSL_Integer aaGetWalkDisabled(string vPresenceId)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "aaGetWalkDisabled", m_host, "AA", m_itemID))
                return new LSL_Integer();
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
                    else
                        LSLError("You do not have permission to access this information.");
                }
                else
                    LSLError("Agent does not exist in the sim.");
            }
            else
                LSLError("You do not have permission to access this information.");
            return false;
        }

        public void aaSetWalkDisabled(string vPresenceId, bool vbValue)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "aaSetWalkDisabled", m_host, "AA", m_itemID))
                return;
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
                    else
                        LSLError("You do not have permission to access this information.");
                }
                else
                    LSLError("Agent does not exist in the sim.");
            }
            else
                LSLError("You do not have permission to access this information.");
        }

        public LSL_Integer aaGetFlyDisabled(string vPresenceId)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "aaGetFlyDisabled", m_host, "AA", m_itemID))
                return new LSL_Integer();
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
                    else
                        LSLError("You do not have permission to access this information.");
                }
                else
                    LSLError("Agent does not exist in the sim.");
            }
            else
                LSLError("You do not have permission to access this information.");
            return false;
        }

        public void aaSetFlyDisabled(string vPresenceId, bool vbValue)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "aaSetFlyDisabled", m_host, "AA", m_itemID))
                return;
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
                    else
                        LSLError("You do not have permission to access this information.");
                }
                else
                    LSLError("Agent does not exist in the sim.");
            }
            else
                LSLError("You do not have permission to access this information.");
        }

        public LSL_Key aaAvatarFullName2Key(string fullname)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "aaAvatarFullName2Key", m_host, "AA", m_itemID))
                return new LSL_String();
            UserAccount account = World.UserAccountService.GetUserAccount(World.RegionInfo.AllScopeIDs, fullname);

            if (null == account)
                return UUID.Zero.ToString();

            return account.PrincipalID.ToString();
        }

        public void aaSetEnv(LSL_String name, LSL_List value)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.VeryHigh, "aaSetEnv", m_host, "AA", m_itemID))
                return;
            if (!World.Permissions.IsGod(m_host.OwnerID))
            {
                LSLError("You do not have god permissions.");
                return;
            }
            if (name == ScriptBaseClass.ENABLE_GRAVITY)
            {
                LSL_Integer enabled = value.GetLSLIntegerItem(0);
                float[] grav = m_host.ParentEntity.Scene.PhysicsScene.GetGravityForce();
                m_host.ParentEntity.Scene.PhysicsScene.SetGravityForce(enabled == 1, grav[0], grav[1], grav[2]);
            }
            else if (name == ScriptBaseClass.GRAVITY_FORCE_X)
            {
                LSL_Float f = value.GetLSLFloatItem(0);
                float[] grav = m_host.ParentEntity.Scene.PhysicsScene.GetGravityForce();
                m_host.ParentEntity.Scene.PhysicsScene.SetGravityForce(true, (float)f.value, grav[1], grav[2]);
            }
            else if (name == ScriptBaseClass.GRAVITY_FORCE_Y)
            {
                LSL_Float f = value.GetLSLFloatItem(0);
                float[] grav = m_host.ParentEntity.Scene.PhysicsScene.GetGravityForce();
                m_host.ParentEntity.Scene.PhysicsScene.SetGravityForce(true, grav[0], (float)f.value, grav[2]);
            }
            else if (name == ScriptBaseClass.GRAVITY_FORCE_Z)
            {
                LSL_Float f = value.GetLSLFloatItem(0);
                float[] grav = m_host.ParentEntity.Scene.PhysicsScene.GetGravityForce();
                m_host.ParentEntity.Scene.PhysicsScene.SetGravityForce(true, grav[0], grav[1], (float)f.value);
            }
            else if (name == ScriptBaseClass.ADD_GRAVITY_POINT)
            {
                LSL_Vector pos = value.GetVector3Item(0);
                LSL_Float gravForce = value.GetLSLFloatItem(1);
                LSL_Float radius = value.GetLSLFloatItem(2);
                LSL_Integer ident = value.GetLSLIntegerItem(3);
                m_host.ParentEntity.Scene.PhysicsScene.AddGravityPoint(false,
                                                                       new Vector3((float)pos.x, (float)pos.y,
                                                                                   (float)pos.z),
                                                                       0, 0, 0, (float)gravForce.value,
                                                                       (float)radius.value, ident.value);
            }
            else if (name == ScriptBaseClass.ADD_GRAVITY_FORCE)
            {
                LSL_Vector pos = value.GetVector3Item(0);
                LSL_Float xForce = value.GetLSLFloatItem(1);
                LSL_Float yForce = value.GetLSLFloatItem(2);
                LSL_Float zForce = value.GetLSLFloatItem(3);
                LSL_Float radius = value.GetLSLFloatItem(4);
                LSL_Integer ident = value.GetLSLIntegerItem(5);
                m_host.ParentEntity.Scene.PhysicsScene.AddGravityPoint(true,
                                                                       new Vector3((float)pos.x, (float)pos.y,
                                                                                   (float)pos.z),
                                                                       (float)xForce, (float)yForce, (float)zForce, 0,
                                                                       (float)radius.value, ident.value);
            }
            else if (name == ScriptBaseClass.START_TIME_REVERSAL_SAVING)
            {
                IPhysicsStateModule physicsState = World.RequestModuleInterface<IPhysicsStateModule>();
                if (physicsState != null)
                    physicsState.StartSavingPhysicsTimeReversalStates();
            }
            else if (name == ScriptBaseClass.STOP_TIME_REVERSAL_SAVING)
            {
                IPhysicsStateModule physicsState = World.RequestModuleInterface<IPhysicsStateModule>();
                if (physicsState != null)
                    physicsState.StopSavingPhysicsTimeReversalStates();
            }
            else if (name == ScriptBaseClass.START_TIME_REVERSAL)
            {
                IPhysicsStateModule physicsState = World.RequestModuleInterface<IPhysicsStateModule>();
                if (physicsState != null)
                    physicsState.StartPhysicsTimeReversal();
            }
            else if (name == ScriptBaseClass.STOP_TIME_REVERSAL)
            {
                IPhysicsStateModule physicsState = World.RequestModuleInterface<IPhysicsStateModule>();
                if (physicsState != null)
                    physicsState.StopPhysicsTimeReversal();
            }
        }

        #endregion

        #region Helpers

        protected UUID InventorySelf()
        {
            UUID invItemID = new UUID();

            lock (m_host.TaskInventory)
            {
#if (!ISWIN)
                foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory)
                {
                    if (inv.Value.Type == 10 && inv.Value.ItemID == m_itemID)
                    {
                        invItemID = inv.Key;
                        break;
                    }
                }
#else
                foreach (KeyValuePair<UUID, TaskInventoryItem> inv in m_host.TaskInventory.Where(inv => inv.Value.Type == 10 && inv.Value.ItemID == m_itemID))
                {
                    invItemID = inv.Key;
                    break;
                }
#endif
            }

            return invItemID;
        }

        public string resolveName(UUID objecUUID)
        {
            // try avatar username surname
            UserAccount account = World.UserAccountService.GetUserAccount(World.RegionInfo.AllScopeIDs, objecUUID);
            if (account != null)
                return account.Name;

            // try an scene object
            ISceneChildEntity SOP = World.GetSceneObjectPart(objecUUID);
            if (SOP != null)
                return SOP.Name;

            ISceneChildEntity SensedObject;
            if (!World.TryGetPart(objecUUID, out SensedObject))
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

        #region IScriptApi Members

        public void Initialize(IScriptModulePlugin ScriptEngine, ISceneChildEntity host, uint localID, UUID itemID,
                               ScriptProtectionModule module)
        {
            m_itemID = itemID;
            m_ScriptEngine = ScriptEngine;
            m_host = host;
            ScriptProtection = module;
            AssetConnector = DataManager.DataManager.RequestPlugin<IAssetConnector>();
        }

        public string Name
        {
            get { return "AA"; }
        }

        public string InterfaceName
        {
            get { return "IAA_Api"; }
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

        public IScriptApi Copy()
        {
            return new AA_Api();
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

        private void RequestPermissions(IScenePresence presence, int perm)
        {
            UUID invItemID = InventorySelf();

            if (invItemID == UUID.Zero)
                return; // Not in a prim? How??

            string ownerName = "";
            IScenePresence ownerPresence = World.GetScenePresence(m_host.ParentEntity.OwnerID);
            ownerName = ownerPresence == null ? resolveName(m_host.OwnerID) : ownerPresence.Name;

            if (ownerName == String.Empty)
                ownerName = "(hippos)";

            presence.ControllingClient.OnScriptAnswer += handleScriptAnswer;

            presence.ControllingClient.SendScriptQuestion(
                m_host.UUID, m_host.ParentEntity.RootChild.Name, ownerName, invItemID, perm);
        }

        private void handleScriptAnswer(IClientAPI client, UUID taskID, UUID itemID, int answer)
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
                                                                      "run_time_permissions", new Object[]
                                                                                                  {
                                                                                                      new LSL_Integer(
                                                                                                          answer)
                                                                                                  },
                                                                      new DetectParams[0]), EventPriority.FirstStart);
        }

        internal void ShoutError(string msg)
        {
            ILSL_Api api = (ILSL_Api)m_ScriptEngine.GetApi(m_itemID, "ll");
            api.llShout(ScriptBaseClass.DEBUG_CHANNEL, msg);
        }

        internal void LSLError(string msg)
        {
            throw new Exception("LSL Runtime Error: " + msg);
        }

        public void aaSetCharacterStat(string UUIDofAv, string StatName, float statValue)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "AASetCharacterStat", m_host, "AA", m_itemID))
                return;
            UUID avatarId = new UUID(UUIDofAv);
            IScenePresence presence = World.GetScenePresence(avatarId);
            if (presence != null)
            {
                ICombatPresence cp = presence.RequestModuleInterface<ICombatPresence>();
                cp.SetStat(StatName, statValue);
            }
            else
                LSLError("Agent does not exist in the sim.");
        }

        public LSL_Integer aaGetIsInfiniteRegion()
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "AAGetIsInfiniteRegion", m_host, "AA", m_itemID))
                return 0;
            return new LSL_Integer(World.RegionInfo.InfiniteRegion ? 1 : 0);
        }

        public void aaAllRegionInstanceSay(LSL_Integer channelID, string text)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "AAAllRegionInstanceSay", m_host, "AA", m_itemID))
                return;

            foreach (IScene scene in m_host.ParentEntity.Scene.RequestModuleInterface<ISceneManager>().GetAllScenes())
            {
                if (text.Length > 1023)
                    text = text.Substring(0, 1023);

                IChatModule chatModule = World.RequestModuleInterface<IChatModule>();
                if (chatModule != null)
                    chatModule.SimChat(text, ChatTypeEnum.Region, channelID,
                        m_host.ParentEntity.RootChild.AbsolutePosition, m_host.Name, m_host.UUID, false, World);

                var comms = scene.RequestModuleInterface<IWorldComm>();
                comms.DeliverMessage(ChatTypeEnum.Say, channelID, m_host.Name, m_host.UUID, text);
            }
        }

        #region Get Windlight

        public LSL_List aaWindlightGetScene(LSL_List rules)
        {
            IGenericsConnector gc = DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            OSDWrapper d = gc.GetGeneric<OSDWrapper>(World.RegionInfo.RegionID, "EnvironmentSettings", "");
            if (d != null)
            {
                WindlightDayCycle cycle = new WindlightDayCycle();
                cycle.FromOSD(d.Info);

                if (!cycle.Cycle.IsStaticDayCycle)
                    return new LSL_List(new object[2] { ScriptBaseClass.WL_ERROR, ScriptBaseClass.WL_ERROR_SCENE_MUST_BE_STATIC });

                LSL_List list = new LSL_List();
                for (int i = 0; i < rules.Data.Length; i++)
                {
                    int rule = rules.GetLSLIntegerItem(i);

                    ConvertWindlightDayCycle(cycle, 0, rule, ref list);
                }
                return list;
            }

            return new LSL_List(new object[2] { ScriptBaseClass.WL_ERROR, ScriptBaseClass.WL_ERROR_NO_SCENE_SET });
        }

        public LSL_List aaWindlightGetScene(int dayCycleIndex, LSL_List rules)
        {
            IGenericsConnector gc = DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            OSDWrapper d = gc.GetGeneric<OSDWrapper>(World.RegionInfo.RegionID, "EnvironmentSettings", "");
            if (d != null)
            {
                WindlightDayCycle cycle = new WindlightDayCycle();
                cycle.FromOSD(d.Info);

                if (cycle.Cycle.IsStaticDayCycle)
                    return new LSL_List(new object[2] { ScriptBaseClass.WL_ERROR, ScriptBaseClass.WL_ERROR_SCENE_MUST_NOT_BE_STATIC });

                if (dayCycleIndex >= cycle.Cycle.DataSettings.Count)
                    return new LSL_List(new object[2] { ScriptBaseClass.WL_ERROR, ScriptBaseClass.WL_ERROR_NO_PRESET_FOUND });

                LSL_List list = new LSL_List();
                for (int i = 0; i < rules.Data.Length; i++)
                {
                    int rule = rules.GetLSLIntegerItem(i);

                    ConvertWindlightDayCycle(cycle, dayCycleIndex, rule, ref list);
                }
                return list;
            }

            return new LSL_List(new object[2] { ScriptBaseClass.WL_ERROR, ScriptBaseClass.WL_ERROR_NO_SCENE_SET });
        }

        public LSL_Integer aaWindlightGetSceneIsStatic()
        {
            IGenericsConnector gc = DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            OSDWrapper d = gc.GetGeneric<OSDWrapper>(World.RegionInfo.RegionID, "EnvironmentSettings", "");
            if (d != null)
            {
                WindlightDayCycle cycle = new WindlightDayCycle();
                cycle.FromOSD(d.Info);
                return new LSL_Integer(cycle.Cycle.IsStaticDayCycle ? 1 : 0);
            }
            return new LSL_Integer(-1);
        }

        #endregion

        #region Day Cycle Changes

        public LSL_Integer aaWindlightGetSceneDayCycleKeyFrameCount()
        {
            IGenericsConnector gc = DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            OSDWrapper d = gc.GetGeneric<OSDWrapper>(World.RegionInfo.RegionID, "EnvironmentSettings", "");
            if (d != null)
            {
                WindlightDayCycle cycle = new WindlightDayCycle();
                cycle.FromOSD(d.Info);
                return new LSL_Integer(cycle.Cycle.DataSettings.Count);
            }
            return new LSL_Integer(-1);
        }

        public LSL_List aaWindlightGetDayCycle()
        {
            IGenericsConnector gc = DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            OSDWrapper d = gc.GetGeneric<OSDWrapper>(World.RegionInfo.RegionID, "EnvironmentSettings", "");
            if (d != null)
            {
                WindlightDayCycle cycle = new WindlightDayCycle();
                cycle.FromOSD(d.Info);
                if (cycle.Cycle.IsStaticDayCycle)
                    return new LSL_List(new object[3] { 0, -1, cycle.Cycle.DataSettings["-1"].preset_name });

                LSL_List list = new LSL_List();

                int i = 0;
                foreach (var key in cycle.Cycle.DataSettings)
                {
                    list.Add(i++);
                    list.Add(key.Key);
                    list.Add(key.Value.preset_name);
                }
                return list;
            }

            return new LSL_List(new object[2] { ScriptBaseClass.WL_ERROR, ScriptBaseClass.WL_ERROR_NO_SCENE_SET });
        }

        public LSL_Integer aaWindlightRemoveDayCycleFrame(int dayCycleFrame)
        {
            IGenericsConnector gc = DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            OSDWrapper d = gc.GetGeneric<OSDWrapper>(World.RegionInfo.RegionID, "EnvironmentSettings", "");
            if (d != null)
            {
                WindlightDayCycle cycle = new WindlightDayCycle();
                cycle.FromOSD(d.Info);

                if (cycle.Cycle.IsStaticDayCycle || dayCycleFrame >= cycle.Cycle.DataSettings.Count)
                    return LSL_Integer.FALSE;

                var data = cycle.Cycle.DataSettings.Keys.ToList();
                string keyToRemove = data[dayCycleFrame];
                cycle.Cycle.DataSettings.Remove(keyToRemove);
                gc.AddGeneric(World.RegionInfo.RegionID, "EnvironmentSettings", "", new OSDWrapper { Info = cycle.ToOSD() }.ToOSD());
                return LSL_Integer.TRUE;
            }
            return LSL_Integer.FALSE;
        }

        public LSL_Integer aaWindlightAddDayCycleFrame(LSL_Float dayCyclePosition, int dayCycleFrameToCopy)
        {
            IGenericsConnector gc = DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            OSDWrapper d = gc.GetGeneric<OSDWrapper>(World.RegionInfo.RegionID, "EnvironmentSettings", "");
            if (d != null)
            {
                WindlightDayCycle cycle = new WindlightDayCycle();
                cycle.FromOSD(d.Info);

                if (cycle.Cycle.IsStaticDayCycle || dayCycleFrameToCopy >= cycle.Cycle.DataSettings.Count)
                    return LSL_Integer.FALSE;

                var data = cycle.Cycle.DataSettings.Keys.ToList();
                cycle.Cycle.DataSettings.Add(dayCyclePosition.ToString(), cycle.Cycle.DataSettings[data[dayCycleFrameToCopy]]);
                gc.AddGeneric(World.RegionInfo.RegionID, "EnvironmentSettings", "", new OSDWrapper { Info = cycle.ToOSD() }.ToOSD());
                return LSL_Integer.TRUE;
            }
            return LSL_Integer.FALSE;
        }

        #endregion

        public LSL_Integer aaWindlightSetScene(LSL_List list)
        {
            IGenericsConnector gc = DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            OSDWrapper d = gc.GetGeneric<OSDWrapper>(World.RegionInfo.RegionID, "EnvironmentSettings", "");
            WindlightDayCycle cycle = new WindlightDayCycle();
            if (d != null)
            {
                cycle.FromOSD(d.Info);
                if (!cycle.Cycle.IsStaticDayCycle)
                    return ScriptBaseClass.WL_ERROR_SCENE_MUST_BE_STATIC;
            }
            else
                return ScriptBaseClass.WL_ERROR_NO_SCENE_SET;

            ConvertLSLToWindlight(ref cycle, 0, list);
            gc.AddGeneric(World.RegionInfo.RegionID, "EnvironmentSettings", "", new OSDWrapper { Info = cycle.ToOSD() }.ToOSD());

            IEnvironmentSettingsModule environmentSettings = World.RequestModuleInterface<IEnvironmentSettingsModule>();
            if (environmentSettings != null)
                environmentSettings.TriggerWindlightUpdate(1);
            
            return ScriptBaseClass.WL_OK;
        }

        public LSL_Integer aaWindlightSetScene(int dayCycleIndex, LSL_List list)
        {
            IGenericsConnector gc = DataManager.DataManager.RequestPlugin<IGenericsConnector>();
            OSDWrapper d = gc.GetGeneric<OSDWrapper>(World.RegionInfo.RegionID, "EnvironmentSettings", "");
            WindlightDayCycle cycle = new WindlightDayCycle();
            if (d != null)
            {
                cycle.FromOSD(d.Info);
                if (cycle.Cycle.IsStaticDayCycle)
                    return ScriptBaseClass.WL_ERROR_SCENE_MUST_BE_STATIC;
                if (dayCycleIndex >= cycle.Cycle.DataSettings.Count)
                    return ScriptBaseClass.WL_ERROR_BAD_SETTING;
            }
            else
                return ScriptBaseClass.WL_ERROR_NO_SCENE_SET;

            ConvertLSLToWindlight(ref cycle, dayCycleIndex, list);
            gc.AddGeneric(World.RegionInfo.RegionID, "EnvironmentSettings", "", new OSDWrapper { Info = cycle.ToOSD() }.ToOSD());

            IEnvironmentSettingsModule environmentSettings = World.RequestModuleInterface<IEnvironmentSettingsModule>();
            if (environmentSettings != null)
                environmentSettings.TriggerWindlightUpdate(1);
            
            return ScriptBaseClass.WL_OK;
        }

        #region Helpers

        private void ConvertWindlightDayCycle(WindlightDayCycle cycle, int preset, int rule, ref LSL_List list)
        {
            var skyDatas = cycle.Cycle.DataSettings.Values.ToList();
            var skyData = skyDatas[preset];

            switch (rule)
            {
                case (int)ScriptBaseClass.WL_AMBIENT:
                    list.Add(new LSL_Rotation(skyData.ambient.X, skyData.ambient.Y,
                        skyData.ambient.Z, skyData.ambient.W));
                    break;
                case (int)ScriptBaseClass.WL_SKY_BLUE_DENSITY:
                    list.Add(new LSL_Rotation(skyData.blue_density.X, skyData.blue_density.Y,
                        skyData.blue_density.Z, skyData.blue_density.W));
                    break;
                case (int)ScriptBaseClass.WL_SKY_BLUR_HORIZON:
                    list.Add(new LSL_Rotation(skyData.blue_horizon.X, skyData.blue_horizon.Y,
                        skyData.blue_horizon.Z, skyData.blue_horizon.W));
                    break;
                case (int)ScriptBaseClass.WL_CLOUD_COLOR:
                    list.Add(new LSL_Rotation(skyData.cloud_color.X, skyData.cloud_color.Y,
                        skyData.cloud_color.Z, skyData.cloud_color.W));
                    break;
                case (int)ScriptBaseClass.WL_CLOUD_POS_DENSITY1:
                    list.Add(new LSL_Rotation(skyData.cloud_pos_density1.X, skyData.cloud_pos_density1.Y,
                        skyData.cloud_pos_density1.Z, skyData.cloud_pos_density1.W));
                    break;
                case (int)ScriptBaseClass.WL_CLOUD_POS_DENSITY2:
                    list.Add(new LSL_Rotation(skyData.cloud_pos_density2.X, skyData.cloud_pos_density2.Y,
                        skyData.cloud_pos_density2.Z, skyData.cloud_pos_density2.W));
                    break;
                case (int)ScriptBaseClass.WL_CLOUD_SCALE:
                    list.Add(new LSL_Rotation(skyData.cloud_scale.X, skyData.cloud_scale.Y,
                        skyData.cloud_scale.Z, skyData.cloud_scale.W));
                    break;
                case (int)ScriptBaseClass.WL_CLOUD_SCROLL_X:
                    list.Add(new LSL_Float(skyData.cloud_scroll_rate.X));
                    break;
                case (int)ScriptBaseClass.WL_CLOUD_SCROLL_X_LOCK:
                    list.Add(new LSL_Integer(skyData.enable_cloud_scroll.X));
                    break;
                case (int)ScriptBaseClass.WL_CLOUD_SCROLL_Y:
                    list.Add(new LSL_Float(skyData.cloud_scroll_rate.Y));
                    break;
                case (int)ScriptBaseClass.WL_CLOUD_SCROLL_Y_LOCK:
                    list.Add(new LSL_Integer(skyData.enable_cloud_scroll.Y));
                    break;
                case (int)ScriptBaseClass.WL_CLOUD_SHADOW:
                    list.Add(new LSL_Rotation(skyData.cloud_shadow.X, skyData.cloud_shadow.Y, skyData.cloud_shadow.Z, skyData.cloud_shadow.W));
                    break;
                case (int)ScriptBaseClass.WL_SKY_DENSITY_MULTIPLIER:
                    list.Add(new LSL_Rotation(skyData.density_multiplier.X, skyData.density_multiplier.Y, skyData.density_multiplier.Z, skyData.density_multiplier.W));
                    break;
                case (int)ScriptBaseClass.WL_SKY_DISTANCE_MULTIPLIER:
                    list.Add(new LSL_Rotation(skyData.distance_multiplier.X, skyData.distance_multiplier.Y, skyData.distance_multiplier.Z, skyData.distance_multiplier.W));
                    break;
                case (int)ScriptBaseClass.WL_SKY_GAMMA:
                    list.Add(new LSL_Rotation(skyData.gamma.X, skyData.gamma.Y, skyData.gamma.Z, skyData.gamma.W));
                    break;
                case (int)ScriptBaseClass.WL_SKY_GLOW:
                    list.Add(new LSL_Rotation(skyData.glow.X, skyData.glow.Y, skyData.glow.Z, skyData.glow.W));
                    break;
                case (int)ScriptBaseClass.WL_SKY_HAZE_DENSITY:
                    list.Add(new LSL_Rotation(skyData.haze_density.X, skyData.haze_density.Y, skyData.haze_density.Z, skyData.haze_density.W));
                    break;
                case (int)ScriptBaseClass.WL_SKY_HAZE_HORIZON:
                    list.Add(new LSL_Rotation(skyData.haze_horizon.X, skyData.haze_horizon.Y, skyData.haze_horizon.Z, skyData.haze_horizon.W));
                    break;
                case (int)ScriptBaseClass.WL_SKY_LIGHT_NORMALS:
                    list.Add(new LSL_Rotation(skyData.lightnorm.X, skyData.lightnorm.Y, skyData.lightnorm.Z, skyData.lightnorm.W));
                    break;
                case (int)ScriptBaseClass.WL_SKY_MAX_ALTITUDE:
                    list.Add(new LSL_Rotation(skyData.max_y.X, skyData.max_y.Y, skyData.max_y.Z, skyData.max_y.W));
                    break;
                case (int)ScriptBaseClass.WL_SKY_STAR_BRIGHTNESS:
                    list.Add(new LSL_Float(skyData.star_brightness));
                    break;
                case (int)ScriptBaseClass.WL_SKY_SUNLIGHT_COLOR:
                    list.Add(new LSL_Rotation(skyData.sunlight_color.X, skyData.sunlight_color.Y, skyData.sunlight_color.Z, skyData.sunlight_color.W));
                    break;


                case (int)ScriptBaseClass.WL_WATER_BLUR_MULTIPLIER:
                    list.Add(new LSL_Float(cycle.Water.blurMultiplier));
                    break;
                case (int)ScriptBaseClass.WL_WATER_FRESNEL_OFFSET:
                    list.Add(new LSL_Float(cycle.Water.fresnelOffset));
                    break;
                case (int)ScriptBaseClass.WL_WATER_FRESNEL_SCALE:
                    list.Add(new LSL_Float(cycle.Water.fresnelScale));
                    break;
                case (int)ScriptBaseClass.WL_WATER_NORMAL_MAP:
                    list.Add(new LSL_String(cycle.Water.normalMap));
                    break;
                case (int)ScriptBaseClass.WL_WATER_NORMAL_SCALE:
                    list.Add(new LSL_Vector(cycle.Water.normScale.X, cycle.Water.normScale.Y,
                        cycle.Water.normScale.Z));
                    break;
                case (int)ScriptBaseClass.WL_WATER_SCALE_ABOVE:
                    list.Add(new LSL_Float(cycle.Water.scaleAbove));
                    break;
                case (int)ScriptBaseClass.WL_WATER_SCALE_BELOW:
                    list.Add(new LSL_Float(cycle.Water.scaleBelow));
                    break;
                case (int)ScriptBaseClass.WL_WATER_UNDERWATER_FOG_MODIFIER:
                    list.Add(new LSL_Float(cycle.Water.underWaterFogMod));
                    break;
                case (int)ScriptBaseClass.WL_WATER_FOG_COLOR:
                    list.Add(new LSL_Rotation(cycle.Water.waterFogColor.X, cycle.Water.waterFogColor.Y, cycle.Water.waterFogColor.Z,
                        cycle.Water.waterFogColor.W));
                    break;
                case (int)ScriptBaseClass.WL_WATER_FOG_DENSITY:
                    list.Add(new LSL_Float(cycle.Water.waterFogDensity));
                    break;
                case (int)ScriptBaseClass.WL_WATER_BIG_WAVE_DIRECTION:
                    list.Add(new LSL_Vector(cycle.Water.wave1Dir.X,
                        cycle.Water.wave1Dir.Y, 0.0f));
                    break;
                case (int)ScriptBaseClass.WL_WATER_LITTLE_WAVE_DIRECTION:
                    list.Add(new LSL_Vector(cycle.Water.wave2Dir.X,
                        cycle.Water.wave2Dir.Y, 0.0f));
                    break;
            }
        }

        private void ConvertLSLToWindlight(ref WindlightDayCycle cycle, int preset, LSL_List list)
        {
            var skyDatas = cycle.Cycle.DataSettings.Values.ToList();
            var skyData = skyDatas[preset];

            for (int i = 0; i < list.Data.Length; i += 2)
            {
                int key = list.GetLSLIntegerItem(i);
                switch (key)
                {
                    case ScriptBaseClass.WL_AMBIENT:
                        {
                            LSL_Rotation rot = list.GetQuaternionItem(i + 1);
                            skyData.ambient = rot.ToVector4();
                            break;
                        }
                    case ScriptBaseClass.WL_CLOUD_COLOR:
                        {
                            LSL_Rotation rot = list.GetQuaternionItem(i + 1);
                            skyData.cloud_color = rot.ToVector4();
                            break;
                        }
                    case ScriptBaseClass.WL_CLOUD_POS_DENSITY1:
                        {
                            LSL_Rotation rot = list.GetQuaternionItem(i + 1);
                            skyData.cloud_pos_density1 = rot.ToVector4();
                            break;
                        }
                    case ScriptBaseClass.WL_CLOUD_POS_DENSITY2:
                        {
                            LSL_Rotation rot = list.GetQuaternionItem(i + 1);
                            skyData.cloud_pos_density2 = rot.ToVector4();
                            break;
                        }
                    case ScriptBaseClass.WL_CLOUD_SCALE:
                        {
                            LSL_Rotation rot = list.GetQuaternionItem(i + 1);
                            skyData.cloud_scale = rot.ToVector4();
                            break;
                        }
                    case ScriptBaseClass.WL_CLOUD_SCROLL_X:
                        {
                            LSL_Integer integer = list.GetLSLIntegerItem(i + 1);
                            skyData.cloud_scroll_rate.X = integer;
                            break;
                        }
                    case ScriptBaseClass.WL_CLOUD_SCROLL_Y:
                        {
                            LSL_Integer integer = list.GetLSLIntegerItem(i + 1);
                            skyData.cloud_scroll_rate.Y = integer;
                            break;
                        }
                    case ScriptBaseClass.WL_CLOUD_SCROLL_X_LOCK:
                        {
                            LSL_Integer integer = list.GetLSLIntegerItem(i + 1);
                            skyData.enable_cloud_scroll.X = integer;
                            break;
                        }
                    case ScriptBaseClass.WL_CLOUD_SCROLL_Y_LOCK:
                        {
                            LSL_Integer integer = list.GetLSLIntegerItem(i + 1);
                            skyData.enable_cloud_scroll.Y = integer;
                            break;
                        }
                    case ScriptBaseClass.WL_CLOUD_SHADOW:
                        {
                            LSL_Rotation rot = list.GetQuaternionItem(i + 1);
                            skyData.cloud_shadow = rot.ToVector4();
                            break;
                        }
                    case ScriptBaseClass.WL_SKY_BLUE_DENSITY:
                        {
                            LSL_Rotation rot = list.GetQuaternionItem(i + 1);
                            skyData.blue_density = rot.ToVector4();
                            break;
                        }
                    case ScriptBaseClass.WL_SKY_BLUR_HORIZON:
                        {
                            LSL_Rotation rot = list.GetQuaternionItem(i + 1);
                            skyData.blue_horizon = rot.ToVector4();
                            break;
                        }
                    case ScriptBaseClass.WL_SKY_DENSITY_MULTIPLIER:
                        {
                            LSL_Rotation rot = list.GetQuaternionItem(i + 1);
                            skyData.density_multiplier = rot.ToVector4();
                            break;
                        }
                    case ScriptBaseClass.WL_SKY_DISTANCE_MULTIPLIER:
                        {
                            LSL_Rotation rot = list.GetQuaternionItem(i + 1);
                            skyData.distance_multiplier = rot.ToVector4();
                            break;
                        }
                    case ScriptBaseClass.WL_SKY_GAMMA:
                        {
                            LSL_Rotation rot = list.GetQuaternionItem(i + 1);
                            skyData.gamma = rot.ToVector4();
                            break;
                        }
                    case ScriptBaseClass.WL_SKY_GLOW:
                        {
                            LSL_Rotation rot = list.GetQuaternionItem(i + 1);
                            skyData.glow = rot.ToVector4();
                            break;
                        }
                    case ScriptBaseClass.WL_SKY_HAZE_DENSITY:
                        {
                            LSL_Rotation rot = list.GetQuaternionItem(i + 1);
                            skyData.haze_density = rot.ToVector4();
                            break;
                        }
                    case ScriptBaseClass.WL_SKY_HAZE_HORIZON:
                        {
                            LSL_Rotation rot = list.GetQuaternionItem(i + 1);
                            skyData.haze_horizon = rot.ToVector4();
                            break;
                        }
                    case ScriptBaseClass.WL_SKY_LIGHT_NORMALS:
                        {
                            LSL_Rotation rot = list.GetQuaternionItem(i + 1);
                            skyData.lightnorm = rot.ToVector4();
                            break;
                        }
                    case ScriptBaseClass.WL_SKY_MAX_ALTITUDE:
                        {
                            LSL_Rotation rot = list.GetQuaternionItem(i + 1);
                            skyData.max_y = rot.ToVector4();
                            break;
                        }
                    case ScriptBaseClass.WL_SKY_STAR_BRIGHTNESS:
                        {
                            LSL_Float f = list.GetLSLFloatItem(i + 1);
                            skyData.star_brightness = (float)f.value;
                            break;
                        }
                    case ScriptBaseClass.WL_SKY_SUNLIGHT_COLOR:
                        {
                            LSL_Rotation rot = list.GetQuaternionItem(i + 1);
                            skyData.sunlight_color = rot.ToVector4();
                            break;
                        }
                    case ScriptBaseClass.WL_WATER_BIG_WAVE_DIRECTION:
                        {
                            var rot = list.GetVector3Item(i + 1);
                            cycle.Water.wave1Dir = new Vector2((float)rot.x.value, (float)rot.y.value);
                            break;
                        }
                    case ScriptBaseClass.WL_WATER_BLUR_MULTIPLIER:
                        {
                            var f = list.GetLSLFloatItem(i + 1);
                            cycle.Water.blurMultiplier = (float)f.value;
                            break;
                        }
                    case ScriptBaseClass.WL_WATER_FOG_COLOR:
                        {
                            LSL_Rotation rot = list.GetQuaternionItem(i + 1);
                            cycle.Water.waterFogColor = rot.ToVector4();
                            break;
                        }
                    case ScriptBaseClass.WL_WATER_FOG_DENSITY:
                        {
                            var f = list.GetLSLFloatItem(i + 1);
                            cycle.Water.waterFogDensity = (float)f.value;
                            break;
                        }
                    case ScriptBaseClass.WL_WATER_FRESNEL_OFFSET:
                        {
                            var f = list.GetLSLFloatItem(i + 1);
                            cycle.Water.fresnelOffset = (float)f.value;
                            break;
                        }
                    case ScriptBaseClass.WL_WATER_FRESNEL_SCALE:
                        {
                            var f = list.GetLSLFloatItem(i + 1);
                            cycle.Water.fresnelScale = (float)f.value;
                            break;
                        }
                    case ScriptBaseClass.WL_WATER_LITTLE_WAVE_DIRECTION:
                        {
                            var rot = list.GetVector3Item(i + 1);
                            cycle.Water.wave2Dir = new Vector2((float)rot.x.value, (float)rot.y.value);
                            break;
                        }
                    case ScriptBaseClass.WL_WATER_NORMAL_MAP:
                        {
                            var f = list.GetLSLStringItem(i + 1);
                            cycle.Water.normalMap = UUID.Parse(f.m_string);
                            break;
                        }
                    case ScriptBaseClass.WL_WATER_NORMAL_SCALE:
                        {
                            LSL_Vector rot = list.GetVector3Item(i + 1);
                            cycle.Water.normScale = rot.ToVector3();
                            break;
                        }
                    case ScriptBaseClass.WL_WATER_SCALE_ABOVE:
                        {
                            var f = list.GetLSLFloatItem(i + 1);
                            cycle.Water.scaleAbove = (float)f.value;
                            break;
                        }
                    case ScriptBaseClass.WL_WATER_SCALE_BELOW:
                        {
                            var f = list.GetLSLFloatItem(i + 1);
                            cycle.Water.scaleBelow = (float)f.value;
                            break;
                        }
                    case ScriptBaseClass.WL_WATER_UNDERWATER_FOG_MODIFIER:
                        {
                            var f = list.GetLSLFloatItem(i + 1);
                            cycle.Water.underWaterFogMod = (float)f.value;
                            break;
                        }
                }
            }
        }

        #endregion
    }
}