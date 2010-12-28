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
using System.Runtime.Remoting.Lifetime;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using integer = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLInteger;
using vector = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Vector3;
using rotation = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Quaternion;
using key = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_List = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.list;
using LSL_String = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_Float = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLFloat;
using LSL_Integer = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLInteger;
using Aurora.ScriptEngine.AuroraDotNetEngine;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using Aurora.ScriptEngine.AuroraDotNetEngine.CompilerTools;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.Runtime
{
    public partial class ScriptBaseClass : MarshalByRefObject
    {
        public IAA_Api m_AA_Functions;

        public void ApiTypeAA(IScriptApi api)
        {
            if (!(api is IAA_Api))
                return;

            m_AA_Functions = (IAA_Api)api;
        }

        public void aaSetCloudDensity(LSL_Float density)
        {
            m_AA_Functions.aaSetCloudDensity(density);
        }

        public void aaUpdateDatabase(LSL_String key, LSL_String value, LSL_String token)
        {
            m_AA_Functions.aaUpdateDatabase(key, value, token);
        }

        public LSL_List aaQueryDatabase(LSL_String key, LSL_String token)
        {
            return m_AA_Functions.aaQueryDatabase(key, token);
        }

        public LSL_String aaSerializeXML(LSL_List keys, LSL_List values)
        {
            return m_AA_Functions.aaSerializeXML(keys, values);
        }

        public LSL_List aaDeserializeXMLKeys(LSL_String xmlFile)
        {
            return m_AA_Functions.aaDeserializeXMLKeys(xmlFile);
        }

        public LSL_List aaDeserializeXMLValues(LSL_String xmlFile)
        {
            return m_AA_Functions.aaDeserializeXMLValues(xmlFile);
        }

        public void aaSetConeOfSilence(LSL_Float radius)
        {
            m_AA_Functions.aaSetConeOfSilence(radius);
        }

        public void aaJoinCombatTeam(LSL_Types.key id, LSL_String team)
        {
            m_AA_Functions.aaJoinCombatTeam(id, team);
        }

        public LSL_String aaGetText()
        {
            return m_AA_Functions.aaGetText();
        }

        public void aaJoinCombat(LSL_Types.key id)
        {
            m_AA_Functions.aaJoinCombat(id);
        }

        public void aaLeaveCombat(LSL_Types.key id)
        {
            m_AA_Functions.aaLeaveCombat(id);
        }

        public LSL_Float aaGetHealth(LSL_Types.key id)
        {
            return m_AA_Functions.aaGetHealth(id);
        }

        public LSL_String aaGetTeam(LSL_Types.key id)
        {
            return m_AA_Functions.aaGetTeam(id);
        }

        public LSL_List aaGetTeamMembers(LSL_String team)
        {
            return m_AA_Functions.aaGetTeamMembers(team);
        }

        public LSL_String aaGetLastOwner()
        {
            return m_AA_Functions.aaGetLastOwner();
        }

        public LSL_String aaGetLastOwner(LSL_String PrimID)
        {
            return m_AA_Functions.aaGetLastOwner(PrimID);
        }

        public void aaSayDistance(int channelID, LSL_Float Distance, string text)
        {
            m_AA_Functions.aaSayDistance(channelID, Distance, text);
        }

        public void aaSayTo(string userID, string text)
        {
            m_AA_Functions.aaSayTo(userID, text);
        }

        public bool aaGetWalkDisabled(string userID)
        {
            return m_AA_Functions.aaGetWalkDisabled(userID);
        }

        public void aaSetWalkDisabled(string userID, bool Value)
        {
            m_AA_Functions.aaSetWalkDisabled(userID, Value);
        }

        public bool aaGetFlyDisabled(string userID)
        {
            return m_AA_Functions.aaGetFlyDisabled(userID);
        }

        public void aaRaiseError(string message)
        {
            m_AA_Functions.aaRaiseError(message);
        }

        public void aaSetFlyDisabled(string userID, bool Value)
        {
            m_AA_Functions.aaSetFlyDisabled(userID, Value);
        }

        public string aaAvatarFullName2Key(string username)
        {
            return m_AA_Functions.aaAvatarFullName2Key(username);
        }

        public void osCauseDamage(string avatar, double damage)
        {
            m_AA_Functions.osCauseDamage(avatar, damage);
        }

        public void osCauseDamage(string avatar, double damage, string regionName, LSL_Types.Vector3 position, LSL_Types.Vector3 lookat)
        {
            m_AA_Functions.osCauseDamage(avatar, damage, regionName, position, lookat);
        }

        public void osCauseHealing(string avatar, double healing)
        {
            m_AA_Functions.osCauseHealing(avatar, healing);
        }

        public void aaSetCenterOfGravity(LSL_Types.Vector3 position)
        {
            m_AA_Functions.aaSetCenterOfGravity(position);
        }
    }
}
