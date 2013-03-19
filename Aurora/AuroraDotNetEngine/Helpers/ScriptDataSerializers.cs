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

using Aurora.Framework;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public class ScriptStateSave
    {
        private ScriptEngine m_module;
        private object StateSaveLock = new object();

        public void Initialize(ScriptEngine module)
        {
            m_module = module;
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
                ISceneEntity entity = (ISceneEntity) parameters;
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
                                          Source = script.Source == null ? "" : script.Source
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
            string vars = "";
            if (script.Script != null)
                vars = WebUtils.BuildXmlResponse(script.Script.GetStoreVars());
            stateSave.Variables = vars;

            //Plugins
            stateSave.Plugins =
                OSDParser.SerializeJsonString(m_module.GetSerializationData(script.ItemID, script.Part.UUID));

            lock (StateSaveLock)
                script.Part.StateSaves[script.ItemID] = stateSave;
            if (saveBackup)
                script.Part.ParentEntity.HasGroupChanged = true;
        }

        public void Deserialize(ScriptData instance, StateSave save)
        {
            instance.State = save.State;
            instance.Running = save.Running;
            instance.EventDelayTicks = (long) save.MinEventDelay;
            instance.AssemblyName = save.AssemblyName;
            instance.Disabled = save.Disabled;
            instance.UserInventoryItemID = save.UserInventoryID;
            if (save.Plugins != "")
                instance.PluginData = (OSDMap) OSDParser.DeserializeJson(save.Plugins);
            m_module.CreateFromData(instance.Part.UUID, instance.ItemID, instance.Part.UUID,
                                    instance.PluginData);
            instance.Source = save.Source;
            instance.InventoryItem.PermsGranter = save.PermsGranter;
            instance.InventoryItem.PermsMask = save.PermsMask;
            instance.TargetOmegaWasSet = save.TargetOmegaWasSet;

            if (!string.IsNullOrEmpty(save.Variables) && instance.Script != null)
                instance.Script.SetStoreVars(WebUtils.ParseXmlResponse(save.Variables));
        }

        public StateSave FindScriptStateSave(ScriptData script)
        {
            lock (StateSaveLock)
            {
                StateSave save;
                if (!script.Part.StateSaves.TryGetValue(script.ItemID, out save))
                {
                    if (!script.Part.StateSaves.TryGetValue(script.InventoryItem.OldItemID, out save))
                    {
                        if (!script.Part.StateSaves.TryGetValue(script.InventoryItem.ItemID, out save))
                        {
                            return null;
                        }
                    }
                }
                return save;
            }
        }

        public void DeleteFrom(ScriptData script)
        {
            bool changed = false;
            lock (StateSaveLock)
            {
                //if we did remove something, resave it
                if (script.Part.StateSaves.Remove(script.ItemID))
                    changed = true;
                if (script.Part.StateSaves.Remove(script.InventoryItem.OldItemID))
                    changed = true;
                if (script.Part.StateSaves.Remove(script.InventoryItem.ItemID))
                    changed = true;
            }
            if (changed)
                script.Part.ParentEntity.HasGroupChanged = true;
        }

        public void DeleteFrom(ISceneChildEntity Part, UUID ItemID)
        {
            lock (StateSaveLock)
            {
                //if we did remove something, resave it
                if (Part.StateSaves.Remove(ItemID))
                    Part.ParentEntity.HasGroupChanged = true;
            }
        }
    }
}