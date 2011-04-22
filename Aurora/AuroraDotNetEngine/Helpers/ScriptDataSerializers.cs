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

        public void AddScene (Scene scene)
        {
            scene.AuroraEventManager.OnGenericEvent += AuroraEventManager_OnGenericEvent;
        }

        public void Close ()
        {
        }

        object AuroraEventManager_OnGenericEvent (string FunctionName, object parameters)
        {
            if (FunctionName == "DeleteToInventory")
            {
                //Resave all the state saves for this object
                ISceneEntity entity = (ISceneEntity)parameters;
                foreach(ISceneChildEntity child in entity.ChildrenEntities())
                {
                    m_module.SaveStateSaves (child.UUID);
                }
            }
            return null;
        }

        public void SaveStateTo (ScriptData script)
        {
            if (script.Script == null)
                return; //If it didn't compile correctly, this happens
            if (!script.Script.NeedsStateSaved)
                return; //If it doesn't need a state save, don't save one
            script.Script.NeedsStateSaved = false;
            StateSave stateSave = new StateSave ();
            stateSave.State = script.State;
            stateSave.ItemID = script.ItemID;
            stateSave.Running = script.Running;
            stateSave.MinEventDelay = script.EventDelayTicks;
            stateSave.Disabled = script.Disabled;
            stateSave.UserInventoryID = script.UserInventoryItemID;
            //Allow for the full path to be put down, not just the assembly name itself
            stateSave.AssemblyName = script.AssemblyName;
            stateSave.Source = script.Source;
            if (script.InventoryItem != null)
            {
                stateSave.PermsGranter = script.InventoryItem.PermsGranter;
                stateSave.PermsMask = script.InventoryItem.PermsMask;
            }
            else
            {
                stateSave.PermsGranter = UUID.Zero;
                stateSave.PermsMask = 0;
            }
            stateSave.TargetOmegaWasSet = script.TargetOmegaWasSet;

            //Vars
            Dictionary<string, Object> vars = new Dictionary<string, object> ();
            if (script.Script != null)
                vars = script.Script.GetStoreVars ();
            stateSave.Variables = WebUtils.BuildXmlResponse (vars);

            //Plugins
            stateSave.Plugins = m_module.GetSerializationData (script.ItemID, script.Part.UUID);

            CreateOSDMapForState (script, stateSave);
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
            m_module.CreateFromData(instance.Part.UUID, instance.ItemID, instance.Part.UUID,
 	              instance.PluginData);
            instance.Source = save.Source;
            instance.InventoryItem.PermsGranter = save.PermsGranter;
            instance.InventoryItem.PermsMask = save.PermsMask;
            instance.TargetOmegaWasSet = save.TargetOmegaWasSet;

            Dictionary<string, object> vars = WebUtils.ParseXmlResponse (save.Variables);
            if (vars != null && vars.Count != 0 || instance.Script != null)
                instance.Script.SetStoreVars (vars);
        }

        public StateSave FindScriptStateSave (ScriptData script)
        {
            OSDMap component = m_manager.GetComponentState (script.Part, m_componentName) as OSDMap;
            //Attempt to find the state saves we have
            if (component != null)
            {
                OSD o;
                //If we have one for this item, deserialize it
                if (!component.TryGetValue (script.ItemID.ToString (), out o))
                {
                    if (!component.TryGetValue (script.InventoryItem.OldItemID.ToString (), out o))
                    {
                        return null;
                    }
                }
                StateSave save = new StateSave ();
                save.FromOSD ((OSDMap)o);
                return save;
            }
            return null;
        }

        public void DeleteFrom (ScriptData script)
        {
            OSDMap component = (OSDMap)m_manager.GetComponentState (script.Part, m_componentName);
            //Attempt to find the state saves we have
            if (component != null)
            {
                //if we did remove something, resave it
                if(component.Remove(script.ItemID.ToString()))
                {
                    m_manager.SetComponentState (script.Part, m_componentName, component);
                }
            }
        }

        private void CreateOSDMapForState (ScriptData script, StateSave save)
        {
            //Get any previous state saves from the component manager
            OSDMap component = m_manager.GetComponentState (script.Part, m_componentName) as OSDMap;
            if (component == null)
                component = new OSDMap ();

            //Add our state to the list of all scripts in this object
            component[script.ItemID.ToString ()] = save.ToOSD ();

            script.Part.ParentEntity.HasGroupChanged = true;

            //Now resave it
            m_manager.SetComponentState (script.Part, m_componentName, component);
        }
    }
}
