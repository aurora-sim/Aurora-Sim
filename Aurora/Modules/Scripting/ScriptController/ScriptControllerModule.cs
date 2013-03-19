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
using System.Linq;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using GridRegion = Aurora.Framework.Services.GridRegion;

namespace Aurora.Modules.Scripting
{
    public class ScriptControllerModule : INonSharedRegionModule
    {
        #region INonSharedRegionModule Members

        public void Initialise(IConfigSource source)
        {
        }

        public void AddRegion(IScene scene)
        {
            scene.EventManager.OnNewPresence += EventManager_OnNewPresence;
            scene.EventManager.OnRemovePresence += EventManager_OnRemovePresence;
        }

        public void RegionLoaded(IScene scene)
        {
        }

        public void RemoveRegion(IScene scene)
        {
            scene.EventManager.OnNewPresence -= EventManager_OnNewPresence;
            scene.EventManager.OnRemovePresence -= EventManager_OnRemovePresence;
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "ScriptControllerModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        private void EventManager_OnNewPresence(IScenePresence presence)
        {
            ScriptControllerPresenceModule m = new ScriptControllerPresenceModule(presence);
            presence.RegisterModuleInterface<IScriptControllerModule>(m);
        }

        private void EventManager_OnRemovePresence(IScenePresence presence)
        {
            ScriptControllerPresenceModule m =
                (ScriptControllerPresenceModule) presence.RequestModuleInterface<IScriptControllerModule>();
            if (m != null)
            {
                m.Close();
                presence.UnregisterModuleInterface<IScriptControllerModule>(m);
            }
        }

        #region Nested type: ScriptControllerPresenceModule

        public class ScriptControllerPresenceModule : IScriptControllerModule
        {
            private readonly Dictionary<UUID, ScriptControllers> scriptedcontrols =
                new Dictionary<UUID, ScriptControllers>();

            private ScriptControlled IgnoredControls = ScriptControlled.CONTROL_ZERO;
            private ScriptControlled LastCommands = ScriptControlled.CONTROL_ZERO;
            private bool MouseDown;
            public IScenePresence m_sp;

            public ScriptControllerPresenceModule(IScenePresence sp)
            {
                m_sp = sp;
                m_sp.ControllingClient.OnForceReleaseControls += HandleForceReleaseControls;
                m_sp.Scene.EventManager.OnMakeChildAgent += EventManager_OnMakeChildAgent;
            }

            #region IScriptControllerModule Members

            public ScriptControllers GetScriptControler(UUID itemID)
            {
                ScriptControllers takecontrols;

                lock (scriptedcontrols)
                {
                    scriptedcontrols.TryGetValue(itemID, out takecontrols);
                }
                return takecontrols;
            }

            public void RegisterControlEventsToScript(int controls, int accept, int pass_on, ISceneChildEntity part,
                                                      UUID Script_item_UUID)
            {
                ScriptControllers obj = new ScriptControllers
                                            {
                                                ignoreControls = ScriptControlled.CONTROL_ZERO,
                                                eventControls = ScriptControlled.CONTROL_ZERO,
                                                itemID = Script_item_UUID,
                                                part = part
                                            };

                if (pass_on == 0 && accept == 0)
                {
                    IgnoredControls |= (ScriptControlled) controls;
                    obj.ignoreControls = (ScriptControlled) controls;
                }

                if (pass_on == 0 && accept == 1)
                {
                    IgnoredControls |= (ScriptControlled) controls;
                    obj.ignoreControls = (ScriptControlled) controls;
                    obj.eventControls = (ScriptControlled) controls;
                }
                if (pass_on == 1 && accept == 1)
                {
                    IgnoredControls = ScriptControlled.CONTROL_ZERO;
                    obj.eventControls = (ScriptControlled) controls;
                    obj.ignoreControls = ScriptControlled.CONTROL_ZERO;
                }

                lock (scriptedcontrols)
                {
                    if (pass_on == 1 && accept == 0)
                    {
                        IgnoredControls &= ~(ScriptControlled) controls;
                        if (scriptedcontrols.ContainsKey(Script_item_UUID))
                            scriptedcontrols.Remove(Script_item_UUID);
                    }
                    else
                    {
                        scriptedcontrols[Script_item_UUID] = obj;
                    }
                }
                m_sp.ControllingClient.SendTakeControls(controls, pass_on == 1 ? true : false, true);
            }

            public void RegisterScriptController(ScriptControllers SC)
            {
                lock (scriptedcontrols)
                {
                    scriptedcontrols[SC.itemID] = SC;
                }
                m_sp.ControllingClient.SendTakeControls((int) SC.eventControls, true, true);
            }

            public void UnRegisterControlEventsToScript(uint Obj_localID, UUID Script_item_UUID)
            {
                ScriptControllers takecontrols;

                lock (scriptedcontrols)
                {
                    if (scriptedcontrols.TryGetValue(Script_item_UUID, out takecontrols))
                    {
                        ScriptControlled sctc = takecontrols.eventControls;

                        m_sp.ControllingClient.SendTakeControls((int) sctc, false, false);
                        m_sp.ControllingClient.SendTakeControls((int) sctc, true, false);

                        scriptedcontrols.Remove(Script_item_UUID);
                        IgnoredControls = ScriptControlled.CONTROL_ZERO;
                        foreach (ScriptControllers scData in scriptedcontrols.Values)
                        {
                            IgnoredControls |= scData.ignoreControls;
                        }
                    }
                }
            }

            public void OnNewMovement(ref AgentManager.ControlFlags flags)
            {
                lock (scriptedcontrols)
                {
                    if (scriptedcontrols.Count > 0)
                    {
                        SendControlToScripts((uint) flags);
                        flags = RemoveIgnoredControls(flags, IgnoredControls);
                    }
                }
            }

            public void RemoveAllScriptControllers(ISceneChildEntity part)
            {
                TaskInventoryDictionary taskIDict = part.TaskInventory;
                if (taskIDict != null)
                {
                    lock (taskIDict)
                    {
                        foreach (UUID taskID in taskIDict.Keys)
                        {
                            UnRegisterControlEventsToScript(m_sp.LocalId, taskID);
                            taskIDict[taskID].PermsMask &= ~(
                                                                (int) ScriptPermission.ControlCamera |
                                                                (int) ScriptPermission.TakeControls);
                        }
                    }
                }
            }

            public ControllerData[] Serialize()
            {
                lock (scriptedcontrols)
                {
                    ControllerData[] controls = new ControllerData[scriptedcontrols.Count];
                    int i = 0;

                    foreach (ScriptControllers c in scriptedcontrols.Values)
                    {
                        controls[i++] = new ControllerData(c.itemID, c.part.UUID, (uint) c.ignoreControls,
                                                           (uint) c.eventControls);
                    }
                    return controls;
                }
            }

            public void Deserialize(ControllerData[] controllerData)
            {
                lock (scriptedcontrols)
                {
                    scriptedcontrols.Clear();

#if (!ISWIN)
                    foreach (ControllerData c in controllerData)
                    {
                        ScriptControllers sc = new ScriptControllers
                                                   {
                                                       itemID = c.ItemID,
                                                       part = m_sp.Scene.GetSceneObjectPart(c.ObjectID),
                                                       ignoreControls = (ScriptControlled) c.IgnoreControls,
                                                       eventControls = (ScriptControlled) c.EventControls
                                                   };
                        if (sc.part != null)
                        {
                            scriptedcontrols[sc.itemID] = sc;
                        }
                    }
#else
                    foreach (ScriptControllers sc in controllerData.Select(c => new ScriptControllers
                                                                                    {
                                                                                        itemID = c.ItemID,
                                                                                        part =
                                                                                            m_sp.Scene
                                                                                                .GetSceneObjectPart(
                                                                                                    c.ObjectID),
                                                                                        ignoreControls =
                                                                                            (ScriptControlled)
                                                                                            c.IgnoreControls,
                                                                                        eventControls =
                                                                                            (ScriptControlled)
                                                                                            c.EventControls
                                                                                    }).Where(sc => sc.part != null))
                    {
                        scriptedcontrols[sc.itemID] = sc;
                    }
#endif
                }
            }

            #endregion

            public void Close()
            {
                m_sp.Scene.EventManager.OnMakeChildAgent -= EventManager_OnMakeChildAgent;
                m_sp.ControllingClient.OnForceReleaseControls -= HandleForceReleaseControls;
                m_sp = null;
            }

            private void EventManager_OnMakeChildAgent(IScenePresence presence, GridRegion destination)
            {
                lock (scriptedcontrols)
                {
                    scriptedcontrols.Clear(); //Remove all controls when we leave the region
                }
            }

            public void HandleForceReleaseControls(IClientAPI remoteClient, UUID agentID)
            {
                IgnoredControls = ScriptControlled.CONTROL_ZERO;
                lock (scriptedcontrols)
                {
                    scriptedcontrols.Clear();
                }
                m_sp.ControllingClient.SendTakeControls(int.MaxValue, false, false);
            }

            protected internal void SendControlToScripts(uint flags)
            {
                ScriptControlled allflags = ScriptControlled.CONTROL_ZERO;

                if (MouseDown)
                {
                    allflags = LastCommands & (ScriptControlled.CONTROL_ML_LBUTTON | ScriptControlled.CONTROL_LBUTTON);
                    if ((flags & (uint) AgentManager.ControlFlags.AGENT_CONTROL_LBUTTON_UP) != 0 ||
                        (flags & unchecked ((uint) AgentManager.ControlFlags.AGENT_CONTROL_ML_LBUTTON_UP)) != 0)
                    {
                        allflags = ScriptControlled.CONTROL_ZERO;
                        MouseDown = true;
                    }
                }

                if ((flags & (uint) AgentManager.ControlFlags.AGENT_CONTROL_ML_LBUTTON_DOWN) != 0)
                {
                    allflags |= ScriptControlled.CONTROL_ML_LBUTTON;
                    MouseDown = true;
                }
                if ((flags & (uint) AgentManager.ControlFlags.AGENT_CONTROL_LBUTTON_DOWN) != 0)
                {
                    allflags |= ScriptControlled.CONTROL_LBUTTON;
                    MouseDown = true;
                }

                // find all activated controls, whether the scripts are interested in them or not
                if ((flags & (uint) AgentManager.ControlFlags.AGENT_CONTROL_AT_POS) != 0 ||
                    (flags & (uint) AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_AT_POS) != 0)
                {
                    allflags |= ScriptControlled.CONTROL_FWD;
                }
                if ((flags & (uint) AgentManager.ControlFlags.AGENT_CONTROL_AT_NEG) != 0 ||
                    (flags & (uint) AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_AT_NEG) != 0)
                {
                    allflags |= ScriptControlled.CONTROL_BACK;
                }
                if ((flags & (uint) AgentManager.ControlFlags.AGENT_CONTROL_UP_POS) != 0 ||
                    (flags & (uint) AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_UP_POS) != 0)
                {
                    allflags |= ScriptControlled.CONTROL_UP;
                }
                if ((flags & (uint) AgentManager.ControlFlags.AGENT_CONTROL_UP_NEG) != 0 ||
                    (flags & (uint) AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_UP_NEG) != 0)
                {
                    allflags |= ScriptControlled.CONTROL_DOWN;
                }
                if ((flags & (uint) AgentManager.ControlFlags.AGENT_CONTROL_LEFT_POS) != 0 ||
                    (flags & (uint) AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_LEFT_POS) != 0)
                {
                    allflags |= ScriptControlled.CONTROL_LEFT;
                }
                if ((flags & (uint) AgentManager.ControlFlags.AGENT_CONTROL_LEFT_NEG) != 0 ||
                    (flags & (uint) AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_LEFT_NEG) != 0)
                {
                    allflags |= ScriptControlled.CONTROL_RIGHT;
                }
                if ((flags & (uint) AgentManager.ControlFlags.AGENT_CONTROL_YAW_NEG) != 0)
                {
                    allflags |= ScriptControlled.CONTROL_ROT_RIGHT;
                }
                if ((flags & (uint) AgentManager.ControlFlags.AGENT_CONTROL_YAW_POS) != 0)
                {
                    allflags |= ScriptControlled.CONTROL_ROT_LEFT;
                }
                // optimization; we have to check per script, but if nothing is pressed and nothing changed, we can skip that
                if (allflags != ScriptControlled.CONTROL_ZERO || allflags != LastCommands)
                {
                    lock (scriptedcontrols)
                    {
                        foreach (KeyValuePair<UUID, ScriptControllers> kvp in scriptedcontrols)
                        {
                            UUID scriptUUID = kvp.Key;
                            ScriptControllers scriptControlData = kvp.Value;

                            ScriptControlled localHeld = allflags & scriptControlData.eventControls;
                            // the flags interesting for us
                            ScriptControlled localLast = LastCommands & scriptControlData.eventControls;
                            // the activated controls in the last cycle
                            ScriptControlled localChange = localHeld ^ localLast; // the changed bits
                            if (localHeld != ScriptControlled.CONTROL_ZERO ||
                                localChange != ScriptControlled.CONTROL_ZERO)
                            {
                                // only send if still pressed or just changed
                                m_sp.Scene.EventManager.TriggerControlEvent(scriptControlData.part, scriptUUID,
                                                                            m_sp.UUID, (uint) localHeld,
                                                                            (uint) localChange);
                            }
                        }
                    }
                }

                LastCommands = allflags;
            }

            protected internal static AgentManager.ControlFlags RemoveIgnoredControls(AgentManager.ControlFlags flags,
                                                                                      ScriptControlled ignored)
            {
                if (ignored == ScriptControlled.CONTROL_ZERO)
                    return flags;

                if ((ignored & ScriptControlled.CONTROL_BACK) != 0)
                    flags &=
                        ~(AgentManager.ControlFlags.AGENT_CONTROL_AT_NEG |
                          AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_AT_NEG);
                if ((ignored & ScriptControlled.CONTROL_FWD) != 0)
                    flags &=
                        ~(AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_AT_POS |
                          AgentManager.ControlFlags.AGENT_CONTROL_AT_POS);
                if ((ignored & ScriptControlled.CONTROL_DOWN) != 0)
                    flags &=
                        ~(AgentManager.ControlFlags.AGENT_CONTROL_UP_NEG |
                          AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_UP_NEG);
                if ((ignored & ScriptControlled.CONTROL_UP) != 0)
                    flags &=
                        ~(AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_UP_POS |
                          AgentManager.ControlFlags.AGENT_CONTROL_UP_POS);
                if ((ignored & ScriptControlled.CONTROL_LEFT) != 0)
                    flags &=
                        ~(AgentManager.ControlFlags.AGENT_CONTROL_LEFT_POS |
                          AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_LEFT_POS);
                if ((ignored & ScriptControlled.CONTROL_RIGHT) != 0)
                    flags &=
                        ~(AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_LEFT_NEG |
                          AgentManager.ControlFlags.AGENT_CONTROL_LEFT_NEG);
                if ((ignored & ScriptControlled.CONTROL_ROT_LEFT) != 0)
                    flags &= ~(AgentManager.ControlFlags.AGENT_CONTROL_YAW_NEG);
                if ((ignored & ScriptControlled.CONTROL_ROT_RIGHT) != 0)
                    flags &= ~(AgentManager.ControlFlags.AGENT_CONTROL_YAW_POS);
                if ((ignored & ScriptControlled.CONTROL_ML_LBUTTON) != 0)
                    flags &= ~(AgentManager.ControlFlags.AGENT_CONTROL_ML_LBUTTON_DOWN);
                if ((ignored & ScriptControlled.CONTROL_LBUTTON) != 0)
                    flags &=
                        ~(AgentManager.ControlFlags.AGENT_CONTROL_LBUTTON_UP |
                          AgentManager.ControlFlags.AGENT_CONTROL_LBUTTON_DOWN);

                //DIR_CONTROL_FLAG_FORWARD = AgentManager.ControlFlags.AGENT_CONTROL_AT_POS,
                //DIR_CONTROL_FLAG_BACK = AgentManager.ControlFlags.AGENT_CONTROL_AT_NEG,
                //DIR_CONTROL_FLAG_LEFT = AgentManager.ControlFlags.AGENT_CONTROL_LEFT_POS,
                //DIR_CONTROL_FLAG_RIGHT = AgentManager.ControlFlags.AGENT_CONTROL_LEFT_NEG,
                //DIR_CONTROL_FLAG_UP = AgentManager.ControlFlags.AGENT_CONTROL_UP_POS,
                //DIR_CONTROL_FLAG_DOWN = AgentManager.ControlFlags.AGENT_CONTROL_UP_NEG,
                //DIR_CONTROL_FLAG_DOWN_NUDGE = AgentManager.ControlFlags.AGENT_CONTROL_NUDGE_UP_NEG

                return flags;
            }
        }

        #endregion
    }
}