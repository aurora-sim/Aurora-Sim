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
using System.Collections;
using key = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using rotation = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Quaternion;
using vector = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Vector3;
using LSL_List = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.list;
using LSL_String = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_Integer = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLInteger;
using LSL_Float = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLFloat;
using LSL_Key = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces
{
    public interface IOSSL_Api
    {
        //OpenSim functions
        string osSetDynamicTextureURL(string dynamicID, string contentType, string url, string extraParams, int timer);

        string osSetDynamicTextureURLBlend(string dynamicID, string contentType, string url, string extraParams,
                                           int timer, int alpha);

        string osSetDynamicTextureURLBlendFace(string dynamicID, string contentType, string url, string extraParams,
                                               bool blend, int disp, int timer, int alpha, int face);

        string osSetDynamicTextureData(string dynamicID, string contentType, string data, string extraParams, int timer);

        string osSetDynamicTextureDataBlend(string dynamicID, string contentType, string data, string extraParams,
                                            int timer, int alpha);

        string osSetDynamicTextureDataBlendFace(string dynamicID, string contentType, string data, string extraParams,
                                                bool blend, int disp, int timer, int alpha, int face);

        LSL_Float osGetTerrainHeight(int x, int y);
        LSL_Integer osSetTerrainHeight(int x, int y, double val);
        void osTerrainFlush();

        int osRegionRestart(double seconds);
        void osRegionNotice(string msg);
        bool osConsoleCommand(string Command);
        void osSetParcelMediaURL(string url);
        void osSetPrimFloatOnWater(int floatYN);
        void osSetParcelSIPAddress(string SIPAddress);

        // Avatar Info Commands
        LSL_String osGetAgentIP(string agent);
        LSL_List osGetAgents();

        // Teleport commands
        DateTime osTeleportAgent(string agent, string regionName, vector position, vector lookat);
        DateTime osTeleportAgent(string agent, int regionX, int regionY, vector position, vector lookat);
        DateTime osTeleportAgent(string agent, vector position, vector lookat);

        // Animation commands
        void osAvatarPlayAnimation(string avatar, string animation);
        void osAvatarStopAnimation(string avatar, string animation);
        
        void osSetTerrainTexture(int level, LSL_Key texture);
        void osSetTerrainTextureHeight(int corner, double low, double high);

        // Attachment commands
        /// <summary>
        /// Attach the object containing this script to the avatar that owns it without checking for PERMISSION_ATTACH
        /// </summary>
        /// <param name='attachment'>The attachment point.  For example, ATTACH_CHEST</param>
        void osForceAttachToAvatar(int attachment);
        /// <summary>
        /// Detach the object containing this script from the avatar it is attached to without checking for PERMISSION_ATTACH
        /// </summary>
        /// <remarks>Nothing happens if the object is not attached.</remarks>
        void osForceDetachFromAvatar();

        //texture draw functions
        string osMovePen(string drawList, int x, int y);
        string osDrawLine(string drawList, int startX, int startY, int endX, int endY);
        string osDrawLine(string drawList, int endX, int endY);
        string osDrawText(string drawList, string text);
        string osDrawEllipse(string drawList, int width, int height);
        string osDrawRectangle(string drawList, int width, int height);
        string osDrawFilledRectangle(string drawList, int width, int height);
        string osDrawPolygon(string drawList, LSL_List x, LSL_List y);
        string osDrawFilledPolygon(string drawList, LSL_List x, LSL_List y);
        string osSetFontName(string drawList, string fontName);
        string osSetFontSize(string drawList, int fontSize);
        string osSetPenSize(string drawList, int penSize);
        string osSetPenColor(string drawList, string colour);
        string osSetPenCap(string drawList, string direction, string type);
        string osDrawImage(string drawList, int width, int height, string imageUrl);
        vector osGetDrawStringSize(string contentType, string text, string fontName, int fontSize);

        double osList2Double(LSL_List src, int index);

        void osSetRegionWaterHeight(double height);
        void osSetRegionSunSettings(bool useEstateSun, bool sunFixed, double sunHour);
        void osSetEstateSunSettings(bool sunFixed, double sunHour);
        double osGetCurrentSunHour();
        double osSunGetParam(string param);
        void osSunSetParam(string param, double value);

        // Wind Module Functions
        string osWindActiveModelPluginName();
        void osSetWindParam(string plugin, string param, LSL_Float value);
        LSL_Float osGetWindParam(string plugin, string param);

        // Parcel commands
        void osParcelJoin(vector pos1, vector pos2);
        void osParcelSubdivide(vector pos1, vector pos2);
        void osSetParcelDetails(vector pos, LSL_List rules);

        string osGetScriptEngineName();
        string osGetSimulatorVersion();
        Hashtable osParseJSON(string JSON);

        void osMessageObject(key objectUUID, string message);

        void osMakeNotecard(string notecardName, LSL_List contents);

        string osGetNotecardLine(string name, int line);
        string osGetNotecard(string name);
        int osGetNumberOfNotecardLines(string name);

        string osAvatarName2Key(string firstname, string lastname);
        string osKey2Name(string id);

        // Grid Info Functions
        string osGetGridNick();
        string osGetGridName();
        string osGetGridLoginURI();

        LSL_String osFormatString(string str, LSL_List strings);
        LSL_List osMatchString(string src, string pattern, int start);

        // Information about data loaded into the region
        string osLoadedCreationDate();
        string osLoadedCreationTime();
        string osLoadedCreationID();

        LSL_List osGetLinkPrimitiveParams(int linknumber, LSL_List rules);

        key osGetMapTexture();
        key osGetRegionMapTexture(string regionName);
        LSL_List osGetRegionStats();

        int osGetSimulatorMemory();
        void osKickAvatar(LSL_String FirstName, LSL_String SurName, LSL_String alert);
        void osSetSpeed(LSL_Key UUID, LSL_Float SpeedModifier);
        LSL_List osGetPrimitiveParams(LSL_Key prim, LSL_List rules);
        void osSetPrimitiveParams(LSL_Key prim, LSL_List rules);
        void osSetProjectionParams(bool projection, LSL_Key texture, double fov, double focus, double amb);
        void osSetProjectionParams(LSL_Key prim, bool projection, LSL_Key texture, double fov, double focus, double amb);

        LSL_List osGetAvatarList();

        void osReturnObject(LSL_Key userID);
        void osReturnObjects(LSL_Float Parameter);
        void osShutDown();

        LSL_Integer osAddAgentToGroup(LSL_Key AgentID, LSL_String GroupName, LSL_String RequestedRole);

        DateTime osRezObject(string inventory, vector pos, vector vel, rotation rot, int param, LSL_Integer isRezAtRoot,
                             LSL_Integer doRecoil, LSL_Integer SetDieAtEdge, LSL_Integer CheckPos);

        LSL_String osUnixTimeToTimestamp(long time);
        DateTime osTeleportOwner(string regionName, vector position, vector lookat);
        DateTime osTeleportOwner(int regionX, int regionY, vector position, vector lookat);
        DateTime osTeleportOwner(vector position, vector lookat);

        void osCauseDamage(string avatar, double damage, string regionName, vector position, vector lookat);
        void osCauseHealing(string avatar, double healing);
        void osCauseDamage(string avatar, double damage);
        LSL_String osGetInventoryDesc(string item);
        LSL_Integer osInviteToGroup(LSL_Key agentId);
        LSL_Integer osEjectFromGroup(LSL_Key agentId);
    }
}