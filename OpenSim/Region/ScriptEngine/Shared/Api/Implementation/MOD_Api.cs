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
    public class MOD_Api : MarshalByRefObject, IMOD_Api, IScriptApi
    {
        internal IScriptEngine m_ScriptEngine;
        internal SceneObjectPart m_host;
        internal uint m_localID;
        internal UUID m_itemID;
        internal bool m_MODFunctionsEnabled = false;
        internal IScriptModuleComms m_comms = null;
        internal IGenericData GenericData;
        internal IScriptProtectionModule ScriptProtection;
        
        public void Initialize(IScriptEngine ScriptEngine, SceneObjectPart host, uint localID, UUID itemID, IScriptProtectionModule module)
        {
            m_ScriptEngine = ScriptEngine;
            m_host = host;
            m_localID = localID;
            m_itemID = itemID;
            ScriptProtection = module;

            m_comms = m_ScriptEngine.World.RequestModuleInterface<IScriptModuleComms>();
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
            get { return m_ScriptEngine.World; }
        }

        internal void MODError(string msg)
        {
            throw new Exception("MOD Runtime Error: " + msg);
        }

        //
        //Dumps an error message on the debug console.
        //

        internal void MODShoutError(string message) 
        {
            if (message.Length > 1023)
                message = message.Substring(0, 1023);

            World.SimChat(OpenMetaverse.Utils.StringToBytes(message),
                          ChatTypeEnum.Shout, ScriptBaseClass.DEBUG_CHANNEL, m_host.ParentGroup.RootPart.AbsolutePosition, m_host.Name, m_host.UUID, true);

            IWorldComm wComm = m_ScriptEngine.World.RequestModuleInterface<IWorldComm>();
            wComm.DeliverMessage(ChatTypeEnum.Shout, ScriptBaseClass.DEBUG_CHANNEL, m_host.Name, m_host.UUID, message);
        }

        public string modSendCommand(string module, string command, string k)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "modSendCommand", m_host, "AA");
            
            UUID req = UUID.Random();

            m_comms.RaiseEvent(m_itemID, req.ToString(), module, command, k);

            return req.ToString();
        }

        public void AAUpdatePrimProperties(LSL_String type, LSL_String Keys, LSL_String Values)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "AAUpdatePrimProperties", m_host, "AA");
            GenericData = Aurora.DataManager.DataManager.GetGenericPlugin();
            List<string> SetValues = new List<string>();
            List<string> SetKeys = new List<string>();
            List<string> KeyRows = new List<string>();
            List<string> KeyValues = new List<string>();
            SetKeys.Add("primType");
            SetValues.Add(type);
            SetKeys.Add("primKeys");
            SetValues.Add(Keys);
            SetKeys.Add("primValues");
            SetValues.Add(Values);
            KeyRows.Add("primUUID");
            KeyValues.Add(m_host.UUID.ToString());
            GenericData.Update("auroraprims", SetValues.ToArray(), SetKeys.ToArray(), KeyRows.ToArray(), KeyValues.ToArray());
        }

        public void AASetCloudDensity(LSL_Float density)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "AASetCloudDensity", m_host, "AA");
            if (!World.Permissions.CanIssueEstateCommand(m_host.OwnerID, false))
                return;
            ICloudModule CloudModule = m_ScriptEngine.World.RequestModuleInterface<ICloudModule>();
            if (CloudModule == null)
                return;
            CloudModule.SetCloudDensity((float)density);
        }

        public void AAUpdateDatabase(LSL_String key, LSL_String value, LSL_String token)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "AAUpdateDatabase", m_host, "AA");
            List<string> Test = GenericData.Query(new string[] { "Token", "Key" }, new string[] { token.m_string, key.m_string }, "LSLGenericData", "*");
            if (Test.Count == 0)
            {
                GenericData.Insert("LSLGenericData", new string[] { token.m_string, key.m_string, value.m_string });
            }
            else
            {
                GenericData.Update("LSLGenericData", new string[] { "Value" }, new string[] { value.m_string }, new string[] { "key" }, new string[] { key.m_string });
            }
        }

        public LSL_List AAQueryDatabase(LSL_String key, LSL_String token)
        {
            ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "AAQueryDatabase", m_host, "AA");
            List<string> query = GenericData.Query(new string[] { "Token", "Key" }, new string[] { token.m_string, key.m_string }, "LSLGenericData", "*");
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
    }
}
