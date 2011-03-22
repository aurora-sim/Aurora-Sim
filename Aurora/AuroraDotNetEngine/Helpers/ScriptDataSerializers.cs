using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Threading;
using System.Xml;
using log4net;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Simulation.Base;
using Aurora.ScriptEngine.AuroraDotNetEngine.Plugins;
using Aurora.ScriptEngine.AuroraDotNetEngine.CompilerTools;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs;
using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public class ScriptStateSave
    {
        private ScriptEngine m_module;

        public void Initialize (ScriptEngine module)
        {
            m_module = module;
        }

        public void Close ()
        {
        }

        public void SaveStateTo (ScriptData script)
        {
            StateSave Insert = new StateSave ();
            Insert.State = script.State;
            Insert.ItemID = script.ItemID;
            Insert.Running = script.Running;
            Insert.MinEventDelay = script.EventDelayTicks;
            Insert.Disabled = script.Disabled;
            Insert.UserInventoryID = script.UserInventoryItemID;
            //Allow for the full path to be put down, not just the assembly name itself
            Insert.AssemblyName = script.AssemblyName;

            //Vars
            Dictionary<string, Object> vars = new Dictionary<string, object> ();
            if (script.Script != null)
                vars = script.Script.GetStoreVars ();
            Insert.Variables = WebUtils.BuildXmlResponse(vars);



            //
            //TODO: FIX THIS
            //
            //Plugins
            object[] Plugins = m_module.GetSerializationData (script.ItemID, script.part.UUID);
            string plugins = "";
            foreach (object plugin in Plugins)
                plugins += plugin + ",";
            Insert.Plugins = plugins;

            //perms
            string perms = "";
            if (script.InventoryItem != null)
            {
                if (script.InventoryItem.PermsMask != 0 && script.InventoryItem.PermsGranter != UUID.Zero)
                {
                    perms += script.InventoryItem.PermsGranter.ToString () + "," + script.InventoryItem.PermsMask.ToString ();
                }
            }
            Insert.Permissions = perms;

        }

        public void DeleteFrom (ScriptData script)
        {
        }

        public StateSave FindScriptStateSave (ScriptData scriptData)
        {
            throw new NotImplementedException ();
        }

        public void Deserialize (ScriptData instance, StateSave save)
        {
            Dictionary<string, object> vars = WebUtils.ParseXmlResponse (save.Variables); ;
            instance.State = save.State;
            instance.Running = save.Running;
            instance.EventDelayTicks = (long)save.MinEventDelay;
            instance.AssemblyName = save.AssemblyName;
            instance.Disabled = save.Disabled;
            instance.UserInventoryItemID = save.UserInventoryID;

            if (vars != null && vars.Count != 0)
                instance.Script.SetStoreVars (vars);

            if (save.Plugins is object[])
                instance.PluginData = (object[])save.Plugins;
            else
                instance.PluginData = new object[1] { (object)save.Plugins };

            if (save.Permissions != " " && save.Permissions != "")
            {
                instance.InventoryItem.PermsGranter = new UUID (save.Permissions.Split (',')[0]);
                instance.InventoryItem.PermsMask = int.Parse (save.Permissions.Split (',')[1], NumberStyles.Integer, Culture.NumberFormatInfo);
            }
        }
    }
}
