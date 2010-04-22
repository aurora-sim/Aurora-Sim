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
using System.Collections.Generic;
using System.Collections;
using OpenMetaverse;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.ScriptEngine.Shared;

namespace OpenSim.Region.ScriptEngine.Interfaces
{
    public interface ICompiler
    {
        void PerformScriptCompile(string Script, UUID assetID, UUID ownerUUID, UUID itemID, string InheritedClases, string ClassName, IScriptProtectionModule ScriptProtection, uint localID, object InstanceData,
            out string assembly, out Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> linemap, out string Identifier);
        string[] GetWarnings();
    }
    
    public interface IScriptProtectionModule
    {
        void AddWantedSRC(UUID itemID, string ClassName);
        string GetSRC(UUID itemID, uint localID, UUID OwnerID);
        void AddNewClassSource(string ClassName, string SRC, object ID);
        bool AllowMacroScripting { get; }
        ThreatLevel GetThreatLevel();
        void CheckThreatLevel(ThreatLevel level, string function, SceneObjectPart m_host, string API);
        IInstanceData TryGetPreviouslyCompiledScript(string source);
        void AddPreviouslyCompiled(string source, IInstanceData ID);
        IInstanceData GetScript(uint localID, UUID itemID);
        IInstanceData GetScript(UUID itemID);
        IInstanceData[] GetScript(uint localID);
        void AddNewScript(IInstanceData Data);
        IInstanceData[] GetAllScripts();
        void RemoveScript(IInstanceData Data);
    }
	
	public enum ThreatLevel
    {
        None = 0,
        Nuisance = 1,
        VeryLow = 2,
        Low = 3,
        Moderate = 4,
        High = 5,
        VeryHigh = 6,
        Severe = 7
    };
	
	public interface IScript
    {
        string[] GetApis();
        void InitApi(string name, IScriptApi data);

        int GetStateEventFlags(string state);
        int ExecuteEvent(string state, string FunctionName, object[] args, int startingposition);
        Dictionary<string,Object> GetVars();
        void SetVars(Dictionary<string,Object> vars);
        void ResetVars();

        void Close();
        string Name { get;}
    }
	
	public class IInstanceData
	{
		IScript Script;
		string State;
		bool Running;
		bool Disabled;
		string Source;
		string ClassSource;
		int StartParam;
		StateSource stateSource;
		AppDomain AppDomain;
		Dictionary<string, IScriptApi> Apis;
		Dictionary<KeyValuePair<int, int>, KeyValuePair<int, int>> LineMap;
		
		SceneObjectPart part;

		long EventDelayTicks = 0;
		long NextEventTimeTicks = 0;
		UUID AssetID;
		string AssemblyName;
		//This is the UUID of the actual script.
		UUID ItemID;
		//This is the localUUID of the object the script is in.
		uint localID;
		string ClassID;
		bool PostOnRez;
		TaskInventoryItem InventoryItem;
		ScenePresence presence;
		DetectParams[] LastDetectParams;
		bool IsCompiling;
		bool ErrorsWaiting;
	}
}
