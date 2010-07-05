using System;
using System.Collections.Generic;
using System.Reflection;
using System.Net.Mail;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Client;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.ScriptEngine.Interfaces;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public class ScriptProtectionModule: IScriptProtectionModule
	{
		#region Declares
		
		private enum Trust : int
    	{
        	Full = 5,
        	Medium = 3,
        	Low = 1
    	}

		IConfigSource m_source;
        ScriptEngine m_engine;
        bool allowHTMLLinking = true;

        Dictionary<UUID, List<string>> WantedClassesByItemID = new Dictionary<UUID, List<string>>();
        //First String: ClassName, Second String: Class Source
        Dictionary<string, string> ClassScripts = new Dictionary<string, string>();

        //String: ClassName, InstanceData: data of the script.
        Dictionary<string, ScriptData> ClassInstances = new Dictionary<string, ScriptData>();
        
        //Threat Level for scripts.
        ThreatLevel m_MaxThreatLevel = 0;
        List<string> EnabledAPIs = new List<string>();
        internal Dictionary<string, List<UUID> > m_FunctionPerms = new Dictionary<string, List<UUID> >();
		public Dictionary<string, ScriptData> PreviouslyCompiled = new Dictionary<string, ScriptData>();
        
        #endregion
        
        #region Constructor
        
        public ScriptProtectionModule(IConfigSource source, ScriptEngine engine)
		{
			m_source = source;
            m_engine = engine;
            EnabledAPIs = new List<string>(m_engine.Config.GetString("AllowedAPIs", "LSL").Split(','));
            
            allowHTMLLinking = m_engine.Config.GetBoolean("AllowHTMLLinking", true);
		}
        
		#endregion
		
		#region ThreatLevels
		
		public ThreatLevel GetThreatLevel()
		{
			if(m_MaxThreatLevel != 0)
				return m_MaxThreatLevel;
			string risk = m_engine.Config.GetString("FunctionThreatLevel", "VeryLow");
			switch (risk)
			{
				case "None":
					m_MaxThreatLevel = ThreatLevel.None;
					break;
				case "VeryLow":
					m_MaxThreatLevel = ThreatLevel.VeryLow;
					break;
				case "Low":
					m_MaxThreatLevel = ThreatLevel.Low;
					break;
				case "Moderate":
					m_MaxThreatLevel = ThreatLevel.Moderate;
					break;
				case "High":
					m_MaxThreatLevel = ThreatLevel.High;
					break;
				case "VeryHigh":
					m_MaxThreatLevel = ThreatLevel.VeryHigh;
					break;
				case "Severe":
					m_MaxThreatLevel = ThreatLevel.Severe;
					break;
				default:
					break;
			}
            return m_MaxThreatLevel;
		}
		
		public void CheckThreatLevel(ThreatLevel level, string function, SceneObjectPart m_host, string API)
        {
            if (!EnabledAPIs.Contains(API))
                Error("", String.Format("{0} permission denied.  All "+API+" functions are disabled.", function)); // throws

            if (!m_FunctionPerms.ContainsKey(function))
            {
                string perm = m_engine.Config.GetString("Allow_" + function, "");
                if (perm == "")
                {
                    m_FunctionPerms[function] = null; // a null value is default
                }
                else
                {
                    bool allowed;

                    if (bool.TryParse(perm, out allowed))
                    {
                        // Boolean given
                        if (allowed)
                        {
                            m_FunctionPerms[function] = new List<UUID>();
                            m_FunctionPerms[function].Add(UUID.Zero);
                        }
                        else
                            m_FunctionPerms[function] = new List<UUID>(); // Empty list = none
                    }
                    else
                    {
                        m_FunctionPerms[function] = new List<UUID>();

                        string[] ids = perm.Split(new char[] {','});
                        foreach (string id in ids)
                        {
                            string current = id.Trim();
                            UUID uuid;

                            if (UUID.TryParse(current, out uuid))
                            {
                                if (uuid != UUID.Zero)
                                    m_FunctionPerms[function].Add(uuid);
                            }
                        }
                    }
                }
            }

            // If the list is null, then the value was true / undefined
            // Threat level governs permissions in this case
            //
            // If the list is non-null, then it is a list of UUIDs allowed
            // to use that particular function. False causes an empty
            // list and therefore means "no one"
            //
            // To allow use by anyone, the list contains UUID.Zero
            //
            if (m_FunctionPerms[function] == null) // No list = true
            {
                if (level > m_MaxThreatLevel)
                    Error("Runtime Error: ",
                        String.Format(
                            "{0} permission denied.  Allowed threat level is {1} but function threat level is {2}.",
                            function, m_MaxThreatLevel, level));
            }
            else
            {
                if (!m_FunctionPerms[function].Contains(UUID.Zero))
                {
                	if (!m_FunctionPerms[function].Contains(m_host.OwnerID))
                        Error("Runtime Error: ",
                            String.Format("{0} permission denied.  Prim owner is not in the list of users allowed to execute this function.",
                            function));
                }
            }
        }

		internal void Error(string surMessage, string msg)
        {
            throw new Exception(surMessage + msg);
        }

		#endregion
        
        public bool AllowHTMLLinking
        {
            get
            {
                return allowHTMLLinking;
            }
        }
        
        #region Previously Compiled Scripts
        
        public void AddPreviouslyCompiled(string source, ScriptData ID)
        {
            lock (PreviouslyCompiled)
            {
                if (!PreviouslyCompiled.ContainsKey(source))
                {
                    PreviouslyCompiled.Add(source, ID);
                }
            }
        }

        public void RemovePreviouslyCompiled(string source)
        {
            lock (PreviouslyCompiled)
            {
                if (PreviouslyCompiled.ContainsKey(source))
                {
                    PreviouslyCompiled.Remove(source);
                }
            }
        }
        
        public ScriptData TryGetPreviouslyCompiledScript(string source)
        {
            lock (PreviouslyCompiled)
            {
                ScriptData ID = null;
                PreviouslyCompiled.TryGetValue(source, out ID);
                //Just as a check...
                if (ID == null)
                    return null;
                return ID;
            }
        }
        
        public Dictionary<UUID, UUID> ScriptsItems = new Dictionary<UUID, UUID>();
        public Dictionary<UUID, Dictionary<UUID, ScriptData>> Scripts = new Dictionary<UUID, Dictionary<UUID, ScriptData>>();
        public ScriptData GetScript(UUID primID, UUID itemID)
        {
            if (!Scripts.ContainsKey(primID))
        		return null;
            Dictionary<UUID, ScriptData> Instances = Scripts[primID]; 
        	if(!Instances.ContainsKey(itemID))
        		return null;
        	return Instances[itemID];
        }
        
        public ScriptData GetScript(UUID itemID)
        {
        	if(!ScriptsItems.ContainsKey(itemID))
        		return null;
        	UUID primID = ScriptsItems[itemID];
            return GetScript(primID, itemID);
        }

        public ScriptData[] GetScripts(UUID primID)
        {
            if (!Scripts.ContainsKey(primID))
        		return null;
            Dictionary<UUID, ScriptData> Instances = Scripts[primID];
        	List<ScriptData> RetVal = new List<ScriptData>();
        	foreach(ScriptData ID in Instances.Values)
        	{
        		RetVal.Add(ID);
        	}
        	return RetVal.ToArray();
        }
        
        public void AddNewScript(ScriptData Data)
        {
            lock (Scripts)
            {
                ScriptData ID = Data;
                if (ScriptsItems.ContainsKey(ID.ItemID))
                    ScriptsItems.Remove(ID.ItemID);
                ScriptsItems.Add(ID.ItemID, ID.part.UUID);
                Dictionary<UUID, ScriptData> Instances = new Dictionary<UUID, ScriptData>();
                if (Scripts.ContainsKey(ID.part.UUID))
                {
                    Scripts.TryGetValue(ID.part.UUID, out Instances);
                    Scripts.Remove(ID.part.UUID);
                }
                if (Instances.ContainsKey(ID.ItemID))
                    Instances.Remove(ID.ItemID);
                Instances.Add(ID.ItemID, ID);
                Scripts.Add(ID.part.UUID, Instances);
            }
        }
        
        public ScriptData[] GetAllScripts()
        {
        	List<ScriptData> Ids = new List<ScriptData>();
        	foreach(Dictionary<UUID, ScriptData> Instances in Scripts.Values)
        	{
        		foreach(ScriptData ID in Instances.Values)
        		{
        			Ids.Add(ID);
        		}
        	}
        	return Ids.ToArray();
        }
        
        public void RemoveScript(ScriptData Data)
        {
        	ScriptData ID = Data;
        	ScriptsItems.Remove(ID.ItemID);
        	Dictionary<UUID, ScriptData> Instances = new Dictionary<UUID, ScriptData>();
            if (Scripts.ContainsKey(ID.part.UUID))
        	{
                Instances = Scripts[ID.part.UUID];
        		Instances.Remove(ID.ItemID);
        	}
            Scripts[ID.part.UUID] = Instances;
        }
        
        #endregion
    }
}
