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
using System.Collections.Generic;
using Aurora.Framework;
using Aurora.Simulation.Base;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Region.Framework.Scenes.Components;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public class ScriptStateSave
    {
        private string m_componentName = "ScriptState";
        private IComponentManager m_manager;
        private ScriptEngine m_module;

        public void Initialize(ScriptEngine module)
        {
            m_module = module;

            m_manager = module.Worlds[0].RequestModuleInterface<IComponentManager>();
        }

        public void AddScene(IScene scene)
        {
            scene.AuroraEventManager.RegisterEventHandler("DeleteToInventory", AuroraEventManager_OnGenericEvent);
        }

        public void Close()
        {
        }

        private object AuroraEventManager_OnGenericEvent(string FunctionName, object parameters)
        {
            if (FunctionName == "DeleteToInventory")
            {
                //Resave all the state saves for this object
                ISceneEntity entity = (ISceneEntity)parameters;
                foreach (ISceneChildEntity child in entity.ChildrenEntities())
                {
                    m_module.SaveStateSaves(child.UUID);
                }
            }
            return null;
        }

        public void SaveStateTo(ScriptData script)
        {
            SaveStateTo(script, false);
        }

        public void SaveStateTo(ScriptData script, bool forced)
        {
            SaveStateTo(script, forced, true);
        }

        public void SaveStateTo(ScriptData script, bool forced, bool saveBackup)
        {
            if (!forced)
            {
                if (script.Script == null)
                    return; //If it didn't compile correctly, this happens
                if (!script.Script.NeedsStateSaved)
                    return; //If it doesn't need a state save, don't save one
            }
            if (script.Script != null)
                script.Script.NeedsStateSaved = false;
            if (saveBackup)
                script.Part.ParentEntity.HasGroupChanged = true;
            StateSave stateSave = new StateSave
                                      {
                                          State = script.State,
                                          ItemID = script.ItemID,
                                          Running = script.Running,
                                          MinEventDelay = script.EventDelayTicks,
                                          Disabled = script.Disabled,
                                          UserInventoryID = script.UserInventoryItemID,
                                          AssemblyName = script.AssemblyName,
                                          Compiled = script.Compiled,
                                          Source = script.Source
                                      };
            //Allow for the full path to be put down, not just the assembly name itself
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
            Dictionary<string, Object> vars = new Dictionary<string, object>();
            if (script.Script != null)
                vars = script.Script.GetStoreVars();
            try
            {
                stateSave.Variables = WebUtils.BuildXmlResponse(vars);
            }
            catch
            {
            }

            //Plugins
            stateSave.Plugins = m_module.GetSerializationData(script.ItemID, script.Part.UUID);

            CreateOSDMapForState(script, stateSave);
        }

        public void Deserialize(ScriptData instance, StateSave save)
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

            try
            {
                Dictionary<string, object> vars = WebUtils.ParseXmlResponse(save.Variables);
                if (vars != null && vars.Count != 0 || instance.Script != null)
                    instance.Script.SetStoreVars(vars);
            }
            catch
            {
            }
        }

        public StateSave FindScriptStateSave(ScriptData script)
        {
            OSDMap component = m_manager.GetComponentState(script.Part, m_componentName) as OSDMap;
            //Attempt to find the state saves we have
            if (component != null)
            {
                OSD o;
                //If we have one for this item, deserialize it
                if (!component.TryGetValue(script.ItemID.ToString(), out o))
                {
                    if (!component.TryGetValue(script.InventoryItem.OldItemID.ToString(), out o))
                    {
                        if (!component.TryGetValue(script.InventoryItem.ItemID.ToString(), out o))
                        {
                            return null;
                        }
                    }
                }
                StateSave save = new StateSave();
                save.FromOSD((OSDMap)o);
                return save;
            }
            return null;
        }

        public void DeleteFrom(ScriptData script)
        {
            OSDMap component = m_manager.GetComponentState(script.Part, m_componentName) as OSDMap;
            //Attempt to find the state saves we have
            if (component != null)
            {
                //if we did remove something, resave it
                if (component.Remove(script.ItemID.ToString()))
                {
                    if (component.Count == 0)
                        m_manager.RemoveComponentState(script.Part.UUID, m_componentName);
                    else
                        m_manager.SetComponentState(script.Part, m_componentName, component);
                    script.Part.ParentEntity.HasGroupChanged = true;
                }
            }
        }

        public void DeleteFrom(ISceneChildEntity Part, UUID ItemID)
        {
            OSDMap component = m_manager.GetComponentState(Part, m_componentName) as OSDMap;
            //Attempt to find the state saves we have
            if (component != null)
            {
                //if we did remove something, resave it
                if (component.Remove(ItemID.ToString()))
                {
                    if (component.Count == 0)
                        m_manager.RemoveComponentState(Part.UUID, m_componentName);
                    else
                        m_manager.SetComponentState(Part, m_componentName, component);
                    Part.ParentEntity.HasGroupChanged = true;
                }
            }
        }

        private void CreateOSDMapForState(ScriptData script, StateSave save)
        {
            //Get any previous state saves from the component manager
            OSDMap component = m_manager.GetComponentState(script.Part, m_componentName) as OSDMap;
            if (component == null)
                component = new OSDMap();

            //Add our state to the list of all scripts in this object
            component[script.ItemID.ToString()] = save.ToOSD();

            //Now resave it
            m_manager.SetComponentState(script.Part, m_componentName, component);
        }
    }
}