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

using System.Collections;

using key = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using rotation = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Quaternion;
using vector = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Vector3;
using LSL_List = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.list;
using LSL_Key = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_String = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_Integer = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLInteger;
using LSL_Float = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLFloat;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces
{
    public interface IAA_Api
    {
        void aaSetCloudDensity(LSL_Float density);

        void aaUpdateDatabase(LSL_String key, LSL_String value, LSL_String token);

        LSL_List aaQueryDatabase(LSL_String key, LSL_String token);

        LSL_Types.list aaDeserializeXMLValues(LSL_Types.LSLString xmlFile);

        LSL_Types.list aaDeserializeXMLKeys(LSL_Types.LSLString xmlFile);

        void aaSetConeOfSilence(LSL_Float radius);

        LSL_Types.LSLString aaSerializeXML(LSL_Types.list keys, LSL_Types.list values);

        LSL_String aaGetTeam(LSL_Key id);

        LSL_Float aaGetHealth(LSL_Key id);

        void aaJoinCombat(LSL_Key id);

        void aaLeaveCombat(LSL_Key id);

        void aaJoinCombatTeam(LSL_Key id, LSL_String team);

        LSL_List aaGetTeamMembers(LSL_String team);

        LSL_String aaGetLastOwner();

        LSL_String aaGetLastOwner(LSL_String PrimID);

        void aaSayDistance(int channelID, LSL_Float Distance, string text);

        void aaSayTo(string userID, string text);

        bool aaGetWalkDisabled(string userID);

        void aaSetWalkDisabled(string userID, bool Value);

        bool aaGetFlyDisabled(string userID);

        void aaSetFlyDisabled(string userID, bool Value);

        string aaAvatarFullName2Key(string username);

        void aaSetCenterOfGravity(LSL_Types.Vector3 position);

        void aaRaiseError(string message);

        LSL_Types.LSLString aaGetText();
    }
}
