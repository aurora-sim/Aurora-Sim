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
using OpenSim.Region.ScriptEngine.Shared;
using OpenSim.Region.ScriptEngine.Shared.Api.Plugins;
using OpenSim.Region.ScriptEngine.Shared.ScriptBase;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.Api.Interfaces;

using LSL_Float = OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLFloat;
using LSL_Integer = OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLInteger;
using LSL_Key = OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLString;
using LSL_List = OpenSim.Region.ScriptEngine.Shared.LSL_Types.list;
using LSL_Rotation = OpenSim.Region.ScriptEngine.Shared.LSL_Types.Quaternion;
using LSL_String = OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLString;
using LSL_Vector = OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3;

namespace OpenSim.Region.ScriptEngine.Shared.Api
{
    [Serializable]
    public class AA_Api : MarshalByRefObject, IAA_Api, IScriptApi
    {
        internal IScriptEngine m_ScriptEngine;
        internal SceneObjectPart m_host;
        internal uint m_localID;
        internal UUID m_itemID;
        internal IAssetConnector AssetConnector;
        internal IScriptProtectionModule ScriptProtection;

        public void Initialize(IScriptEngine ScriptEngine, SceneObjectPart host, uint localID, UUID itemID, IScriptProtectionModule module)
        {
            m_ScriptEngine = ScriptEngine;
            m_host = host;
            m_localID = localID;
            m_itemID = itemID;
            ScriptProtection = module;
            AssetConnector = Aurora.DataManager.DataManager.RequestPlugin<IAssetConnector>("IAssetConnector");
        }

        public string Name
        {
            get { return "AA"; }
        }

        public void Dispose()
        {
        }

        public override Object InitializeLifetimeService()
        {
            ILease lease = (ILease)base.InitializeLifetimeService();

            if (lease.CurrentState == LeaseState.Initial)
            {
                lease.InitialLeaseTime = TimeSpan.FromMinutes(1);
                lease.RenewOnCallTime = TimeSpan.FromSeconds(10.0);
                lease.SponsorshipTimeout = TimeSpan.FromMinutes(1.0);
            }
            return lease;
        }

        public Scene World
        {
            get { return m_host.ParentGroup.Scene; }
        }

        public void AASetCloudDensity(LSL_Float density)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "AASetCloudDensity", m_host, "AA");
            if (!World.Permissions.CanIssueEstateCommand(m_host.OwnerID, false))
                return;
            ICloudModule CloudModule = World.RequestModuleInterface<ICloudModule>();
            if (CloudModule == null)
                return;
            CloudModule.SetCloudDensity((float)density);
        }

        public void AAUpdateDatabase(LSL_String key, LSL_String value, LSL_String token)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "AAUpdateDatabase", m_host, "AA");
            AssetConnector.UpdateLSLData(token.m_string, key.m_string, value.m_string);
        }

        public LSL_List AAQueryDatabase(LSL_String key, LSL_String token)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "AAQueryDatabase", m_host, "AA");

            List<string> query = AssetConnector.FindLSLData(token.m_string, key.m_string);
            LSL_List list = new LSL_Types.list(query.ToArray());
            return list;
        }

        public LSL_String AASerializeXML(LSL_List keys, LSL_List values)
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

        public LSL_List AADeserializeXMLKeys(LSL_String xmlFile)
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

        public LSL_List AADeserializeXMLValues(LSL_String xmlFile)
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

        public void AASetConeOfSilence(LSL_Float radius)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "AASetConeOfSilence", m_host, "AA");
            m_host.SetConeOfSilence(radius.value);
        }

        public void AAJoinCombatTeam(LSL_String team)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "AAJoinCombatTeam", m_host, "AA");
            ScenePresence SP = World.GetScenePresence(m_host.OwnerID);
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

        public void AALeaveCombat()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "AALeaveCombat", m_host, "AA");
            ScenePresence SP = World.GetScenePresence(m_host.OwnerID);
            if (SP != null)
            {
                ICombatPresence CP = SP.RequestModuleInterface<ICombatPresence>();
                if (CP != null)
                {
                    CP.LeaveCombat();
                }
            }
        }

        public void AAJoinCombat()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "AAJoinCombat", m_host, "AA");
            ScenePresence SP = World.GetScenePresence(m_host.OwnerID);
            if (SP != null)
            {
                ICombatPresence CP = SP.RequestModuleInterface<ICombatPresence>();
                if (CP != null)
                {
                    CP.JoinCombat();
                }
            }
        }

        public LSL_Float AAGetHealth()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.None, "AAGetHealth", m_host, "AA");
            ScenePresence SP = World.GetScenePresence(m_host.OwnerID);
            if (SP != null)
            {
                return new LSL_Float(SP.Health);
            }
            return new LSL_Float(-1);
        }

        public LSL_String AAGetTeam()
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Low, "AAGetTeam", m_host, "AA");
            ScenePresence SP = World.GetScenePresence(m_host.OwnerID);
            if (SP != null)
            {
                ICombatPresence CP = SP.RequestModuleInterface<ICombatPresence>();
                if (CP != null)
                {
                    return CP.Team;
                }
            }
            return "No Team";
        }
    }
}
