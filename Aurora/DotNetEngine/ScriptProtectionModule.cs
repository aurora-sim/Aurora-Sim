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
using OpenSim.Region.ScriptEngine.Shared.CodeTools;

namespace OpenSim.Region.ScriptEngine.DotNetEngine
{
    public enum Trust : int
    {
        Full = 5,
        Medium = 3,
        Low = 1
    }
	public class ScriptProtectionModule: IScriptProtectionModule
	{
		IConfigSource m_source;
        ScriptEngine m_engine;
        Trust TrustLevel = Trust.Full;
        Dictionary<UUID, List<string>> WantedClassesByItemID = new Dictionary<UUID, List<string>>();
        //First String: ClassName, Second String: Class Source
        Dictionary<string, string> ClassScripts = new Dictionary<string, string>();

        //String: ClassName, InstanceData: data of the script.
        Dictionary<string, InstanceData> ClassInstances = new Dictionary<string, InstanceData>();
        
        public ScriptProtectionModule(IConfigSource source, ScriptEngine engine)
		{
			m_source = source;
            m_engine = engine;
		}

        public void AddNewClassSource(string ClassName, string SRC, object ID)
        {
            if (!ClassScripts.ContainsKey(ClassName))
            {
                ClassScripts.Add(ClassName, SRC);
                if(ID != null)
                    ClassInstances.Add(ClassName, (InstanceData)ID);
            }
        }

        public string GetSRC(OpenMetaverse.UUID itemID, uint localID, UUID OwnerID)
        {
            string ReturnValue = "";
            List<string> SRCWanted = new List<string>();
            if (WantedClassesByItemID.ContainsKey(itemID))
            {
                WantedClassesByItemID.TryGetValue(itemID, out SRCWanted);
                foreach (string ClassName in SRCWanted)
                {
                    InstanceData id = ClassInstances[ClassName];
                    if (id == null)
                        continue;

                    bool isInSameObject = (id.localID == localID);
                    bool isSameOwner = (id.InventoryItem.OwnerID == OwnerID);
                    if (isInSameObject)
                    {
                        //Only check for owner
                        if (isSameOwner)
                        {
                            //No checks required
                            ReturnValue += ClassScripts[ClassName];
                        }
                        else
                        {
                            if (TrustLevel == Trust.Low)
                                continue;
                            else
                                ReturnValue += ClassScripts[ClassName];
                        }
                    }
                    else
                    {
                        if (isSameOwner)
                        {
                            if (TrustLevel == Trust.Low)
                                continue;
                            else
                                ReturnValue += ClassScripts[ClassName];
                        }
                        else
                        {
                            if (TrustLevel < Trust.Full)
                                continue;
                            else
                                ReturnValue += ClassScripts[ClassName];
                        }
                    }
                }
            }
            return ReturnValue;
        }

        public void AddWantedSRC(UUID itemID, string ClassName)
        {
            List<string> SRCWanted = new List<string>();
            if(WantedClassesByItemID.ContainsKey(itemID))
            {
                WantedClassesByItemID.TryGetValue(itemID, out SRCWanted);
                WantedClassesByItemID.Remove(itemID);
            }
            SRCWanted.Add(ClassName);
            WantedClassesByItemID.Add(itemID, SRCWanted);
        }
    }
}
