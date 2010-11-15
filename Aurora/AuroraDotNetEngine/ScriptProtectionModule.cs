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

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public class ScriptProtectionModule
	{
		#region Declares

        private IConfig m_config;
        private bool allowHTMLLinking = true;

        //Threat Level for scripts.
        private ThreatLevel m_MaxThreatLevel = 0;
        //List of all enabled APIs for scripts
        private List<string> EnabledAPIs = new List<string>();
        //Which owners have access to which functions
        private Dictionary<string, List<UUID> > m_FunctionPerms = new Dictionary<string, List<UUID> >();
        //Keeps track of whether the source has been compiled before
        public Dictionary<string, string> PreviouslyCompiled = new Dictionary<string, string>();

        public bool AllowHTMLLinking
        {
            get
            {
                return allowHTMLLinking;
            }
        }
        
        #endregion
        
        #region Constructor
        
        public ScriptProtectionModule(IConfig config)
		{
            m_config = config;
            EnabledAPIs = new List<string>(config.GetString("AllowedAPIs", "LSL").Split(','));

            allowHTMLLinking = config.GetBoolean("AllowHTMLLinking", true);
            GetThreatLevel();
		}
        
		#endregion
		
		#region ThreatLevels
		
		public ThreatLevel GetThreatLevel()
		{
			if(m_MaxThreatLevel != 0)
				return m_MaxThreatLevel;
            string risk = m_config.GetString("FunctionThreatLevel", "VeryLow");
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

        public bool CheckAPI(string Name)
        {
            if (!EnabledAPIs.Contains(Name))
                return false;
            return true;
        }
		
		public void CheckThreatLevel(ThreatLevel level, string function, SceneObjectPart m_host, string API)
        {
            List<UUID> FunctionPerms = new List<UUID>();
            if (!m_FunctionPerms.TryGetValue(function, out FunctionPerms))
            {
                string perm = m_config.GetString("Allow_" + function, "");
                if (perm == "")
                {
                    m_FunctionPerms[function] = null; // a null value is default
                    FunctionPerms = null;
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
                            FunctionPerms = new List<UUID>();
                            FunctionPerms.Add(UUID.Zero);
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
            if (FunctionPerms == null) // No list = true
            {
                if (level > m_MaxThreatLevel)
                    Error("Runtime Error: ",
                        String.Format(
                            "{0} permission denied.  Allowed threat level is {1} but function threat level is {2}.",
                            function, m_MaxThreatLevel, level));
            }
            else
            {
                if (!FunctionPerms.Contains(UUID.Zero))
                {
                    if (!FunctionPerms.Contains(m_host.OwnerID))
                        Error("Runtime Error: ",
                            String.Format("{0} permission denied.  Prim owner is not in the list of users allowed to execute this function.",
                            function));
                }
            }
        }

		internal void Error(string surMessage, string msg)
        {
            throw new ScriptPermissionsException(surMessage + msg);
        }

		#endregion
        
        #region Previously Compiled Scripts
        
        public void AddPreviouslyCompiled(string source, ScriptData ID)
        {
            lock (PreviouslyCompiled)
            {
                if (!PreviouslyCompiled.ContainsKey(source))
                {
                    PreviouslyCompiled.Add(source, ID.AssemblyName);
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
        
        public string TryGetPreviouslyCompiledScript(string source)
        {
            string assemblyName = "";
            PreviouslyCompiled.TryGetValue(source, out assemblyName);
            return assemblyName;
        }
        
        public Dictionary<UUID, UUID> ScriptsItems = new Dictionary<UUID, UUID>();
        public Dictionary<UUID, Dictionary<UUID, ScriptData>> Scripts = new Dictionary<UUID, Dictionary<UUID, ScriptData>>();
        public ScriptData GetScript(UUID primID, UUID itemID)
        {
            Dictionary<UUID, ScriptData> Instances;
            if (Scripts.TryGetValue(primID, out Instances))
            {
                ScriptData ID = null;
                Instances.TryGetValue(itemID, out ID);
                return ID;
            }
            return null;
        }
        
        public ScriptData GetScript(UUID itemID)
        {
            lock (ScriptsItems)
            {
                UUID primID;
                if (ScriptsItems.TryGetValue(itemID, out primID))
                    return GetScript(primID, itemID);
                return null;
            }
        }

        public ScriptData[] GetScripts(UUID primID)
        {
            Dictionary<UUID, ScriptData> Instances;
            if (Scripts.TryGetValue(primID, out Instances))
                return new List<ScriptData>(Instances.Values).ToArray();
            return null;
        }
        
        public void AddNewScript(ScriptData ID)
        {
            lock (Scripts)
            {
                lock (ScriptsItems)
                {
                    ScriptsItems[ID.ItemID] = ID.part.UUID;
                }
                Dictionary<UUID, ScriptData> Instances = new Dictionary<UUID, ScriptData>();
                if (!Scripts.TryGetValue(ID.part.UUID, out Instances))
                {
                    Instances = new Dictionary<UUID, ScriptData>();
                }
                Instances[ID.ItemID] = ID;
                Scripts[ID.part.UUID] = Instances;
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
            lock (ScriptsItems)
            {
                ScriptsItems.Remove(Data.ItemID);
            }
        	Dictionary<UUID, ScriptData> Instances = new Dictionary<UUID, ScriptData>();
            if (Scripts.ContainsKey(Data.part.UUID))
        	{
                Instances = Scripts[Data.part.UUID];
                Instances.Remove(Data.ItemID);
        	}
            Scripts[Data.part.UUID] = Instances;
        }
        
        #endregion
    }
}
