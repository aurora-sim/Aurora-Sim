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
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using Aurora.Simulation.Base;
using Aurora.ScriptEngine.AuroraDotNetEngine.Plugins;
using Aurora.ScriptEngine.AuroraDotNetEngine.CompilerTools;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs;
using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;

using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Components;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public class ScriptStateSave
    {
        private ScriptEngine m_module;
        private IComponentManager m_manager;
        private string m_componentName = "ScriptState";

        public void Initialize (ScriptEngine module)
        {
            m_module = module;

            m_manager = module.Worlds[0].RequestModuleInterface<IComponentManager> ();
            DefaultComponents com = new DefaultComponents (m_componentName);
            m_manager.RegisterComponent (com);
        }

        public void Close ()
        {
        }

        public StateSave FindScriptStateSave (ScriptData scriptData)
        {
            throw new NotImplementedException ();
        }

        public void SaveStateTo (ScriptData script)
        {
            StateSave stateSave = new StateSave ();
            stateSave.State = script.State;
            stateSave.ItemID = script.ItemID;
            stateSave.Running = script.Running;
            stateSave.MinEventDelay = script.EventDelayTicks;
            stateSave.Disabled = script.Disabled;
            stateSave.UserInventoryID = script.UserInventoryItemID;
            //Allow for the full path to be put down, not just the assembly name itself
            stateSave.AssemblyName = script.AssemblyName;

            //Vars
            Dictionary<string, Object> vars = new Dictionary<string, object> ();
            if (script.Script != null)
                vars = script.Script.GetStoreVars ();
            stateSave.Variables = WebUtils.BuildXmlResponse (vars);

            //Plugins
            stateSave.Plugins = m_module.GetSerializationData (script.ItemID, script.part.UUID);



            //
            //TODO: FIX THIS
            //




            //perms
            string perms = "";
            if (script.InventoryItem != null)
            {
                if (script.InventoryItem.PermsMask != 0 && script.InventoryItem.PermsGranter != UUID.Zero)
                {
                    perms += script.InventoryItem.PermsGranter.ToString () + "," + script.InventoryItem.PermsMask.ToString ();
                }
            }
            stateSave.Permissions = perms;

            CreateOSDMapForState (script, stateSave);
        }

        public void DeleteFrom (ScriptData script)
        {
        }

        public void Deserialize (ScriptData instance, StateSave save)
        {
            instance.State = save.State;
            instance.Running = save.Running;
            instance.EventDelayTicks = (long)save.MinEventDelay;
            instance.AssemblyName = save.AssemblyName;
            instance.Disabled = save.Disabled;
            instance.UserInventoryItemID = save.UserInventoryID;
            instance.PluginData = save.Plugins;

            Dictionary<string, object> vars = WebUtils.ParseXmlResponse (save.Variables);
            if (vars != null && vars.Count != 0)
                instance.Script.SetStoreVars (vars);


            //
            //TODO: FIX ME
            //




            if (save.Permissions != " " && save.Permissions != "")
            {
                instance.InventoryItem.PermsGranter = new UUID (save.Permissions.Split (',')[0]);
                instance.InventoryItem.PermsMask = int.Parse (save.Permissions.Split (',')[1], NumberStyles.Integer, Culture.NumberFormatInfo);
            }
        }

        private void CreateOSDMapForState (ScriptData script, StateSave save)
        {
            m_manager.SetComponentState (script.part, m_componentName, Insert.ToOSD ());
        }
    }
}
