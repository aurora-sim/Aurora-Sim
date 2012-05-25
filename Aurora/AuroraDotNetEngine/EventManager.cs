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
using System.Reflection;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Region.Framework.Scenes;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    /// <summary>
    ///   Prepares events so they can be directly executed upon a script by EventQueueManager, then queues it.
    /// </summary>
    public class EventManager
    {
        //
        // This class it the link between an event inside OpenSim and
        // the corresponding event in a user script being executed.
        //
        // For example when an user touches an object then the
        // "scene.EventManager.OnObjectGrab" event is fired
        // inside OpenSim.
        // We hook up to this event and queue a touch_start in
        // the event queue with the proper LSL parameters.
        //
        // You can check debug C# dump of an LSL script if you need to
        // verify what exact parameters are needed.
        //

        private readonly Dictionary<uint, Dictionary<UUID, DetectParams>> CoalescedTouchEvents =
            new Dictionary<uint, Dictionary<UUID, DetectParams>>();

        private readonly ScriptEngine m_scriptEngine;

        public EventManager(ScriptEngine _ScriptEngine)
        {
            m_scriptEngine = _ScriptEngine;
        }

        public void HookUpRegionEvents(IScene Scene)
        {
            //MainConsole.Instance.Info("[" + myScriptEngine.ScriptEngineName +
            //           "]: Hooking up to server events");

            Scene.EventManager.OnObjectGrab +=
                touch_start;
            Scene.EventManager.OnObjectGrabbing +=
                touch;
            Scene.EventManager.OnObjectDeGrab +=
                touch_end;
            Scene.EventManager.OnScriptChangedEvent +=
                changed;
            Scene.EventManager.OnScriptAtTargetEvent +=
                at_target;
            Scene.EventManager.OnScriptNotAtTargetEvent +=
                not_at_target;
            Scene.EventManager.OnScriptAtRotTargetEvent +=
                at_rot_target;
            Scene.EventManager.OnScriptNotAtRotTargetEvent +=
                not_at_rot_target;
            Scene.EventManager.OnScriptControlEvent +=
                control;
            Scene.EventManager.OnScriptColliderStart +=
                collision_start;
            Scene.EventManager.OnScriptColliding +=
                collision;
            Scene.EventManager.OnScriptCollidingEnd +=
                collision_end;
            Scene.EventManager.OnScriptLandColliderStart +=
                land_collision_start;
            Scene.EventManager.OnScriptLandColliding +=
                land_collision;
            Scene.EventManager.OnScriptLandColliderEnd +=
                land_collision_end;
            Scene.EventManager.OnAttach += attach;
            Scene.EventManager.OnScriptMovingStartEvent += moving_start;
            Scene.EventManager.OnScriptMovingEndEvent += moving_end;

            Scene.EventManager.OnRezScripts += rez_scripts;


            IMoneyModule money =
                Scene.RequestModuleInterface<IMoneyModule>();
            if (money != null)
            {
                money.OnObjectPaid += HandleObjectPaid;
                money.OnPostObjectPaid += HandlePostObjectPaid;
            }
        }

        //private void HandleObjectPaid(UUID objectID, UUID agentID, int amount)
        private bool HandleObjectPaid(UUID objectID, UUID agentID, int amount)
        {
            //bool ret = false;
            //ISceneChildEntity part = m_scriptEngine.findPrim(objectID);

            //if (part == null)
            //    return;

            //MainConsole.Instance.Debug("Paid: " + objectID + " from " + agentID + ", amount " + amount);
            //if (part.ParentGroup != null)
            //    part = part.ParentGroup.RootPart;

            //if (part != null)
            //{
            //    money(part.LocalId, agentID, amount);
            //}
            //if (part != null)
            //{
            //    MainConsole.Instance.Debug("Paid: " + objectID + " from " + agentID + ", amount " + amount);
            //    if (part.ParentEntity != null) part = part.ParentEntity.RootChild;
            //    if (part != null)
            //    {
            //        ret = money(part.LocalId, agentID, amount);
            //    }
            //}
            //return ret;
            return true;
        }

        private bool HandlePostObjectPaid(uint localID, ulong regionHandle, UUID agentID, int amount)
        {
            return money(localID, agentID, amount);
        }

        public void changed(ISceneChildEntity part, uint change)
        {
            ScriptData[] datas = ScriptEngine.ScriptProtection.GetScripts(part.UUID);

            if (datas == null || datas.Length == 0)
            {
                datas = ScriptEngine.ScriptProtection.GetScripts(part.ParentEntity.RootChild.UUID);
                if (datas == null || datas.Length == 0)
                    return;
            }
            string functionName = "changed";
            object[] param = new Object[] { new LSL_Types.LSLInteger(change) };

#if (!ISWIN)
            foreach (ScriptData ID in datas)
            {
                if (CheckIfEventShouldFire(ID, functionName, param))
                {
                    m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart, param);
                }
            }
#else
            foreach (ScriptData ID in datas.Where(ID => CheckIfEventShouldFire(ID, functionName, param)))
            {
                m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart,
                                                param);
            }
#endif
        }

        /// <summary>
        ///   Handles piping the proper stuff to The script engine for touching
        ///   Including DetectedParams
        /// </summary>
        /// <param name = "part"></param>
        /// <param name = "child"></param>
        /// <param name = "offsetPos"></param>
        /// <param name = "remoteClient"></param>
        /// <param name = "surfaceArgs"></param>
        public void touch_start(ISceneChildEntity part, ISceneChildEntity child, Vector3 offsetPos,
                                IClientAPI remoteClient, SurfaceTouchEventArgs surfaceArgs)
        {
            // Add to queue for all scripts in ObjectID object
            Dictionary<UUID, DetectParams> det = new Dictionary<UUID, DetectParams>();
            if (!CoalescedTouchEvents.TryGetValue(part.LocalId, out det))
                det = new Dictionary<UUID, DetectParams>();

            DetectParams detparam = new DetectParams { Key = remoteClient.AgentId };

            detparam.Populate(part.ParentEntity.Scene);
            detparam.LinkNum = child.LinkNum;

            if (surfaceArgs != null)
            {
                detparam.SurfaceTouchArgs = surfaceArgs;
            }

            det[remoteClient.AgentId] = detparam;
            CoalescedTouchEvents[part.LocalId] = det;

            ScriptData[] datas = ScriptEngine.ScriptProtection.GetScripts(part.UUID);

            if (datas == null || datas.Length == 0)
                return;

            string functionName = "touch_start";
            object[] param = new Object[] { new LSL_Types.LSLInteger(det.Count) };

#if (!ISWIN)
            foreach (ScriptData ID in datas)
            {
                if (CheckIfEventShouldFire(ID, functionName, param))
                {
                    m_scriptEngine.AddToScriptQueue(ID, functionName, new List<DetectParams>(det.Values).ToArray(), EventPriority.FirstStart, param);
                }
            }
#else
            foreach (ScriptData ID in datas.Where(ID => CheckIfEventShouldFire(ID, functionName, param)))
            {
                m_scriptEngine.AddToScriptQueue(ID, functionName, new List<DetectParams>(det.Values).ToArray(),
                                                EventPriority.FirstStart, param);
            }
#endif
        }

        public void touch(ISceneChildEntity part, ISceneChildEntity child, Vector3 offsetPos,
                          IClientAPI remoteClient, SurfaceTouchEventArgs surfaceArgs)
        {
            Dictionary<UUID, DetectParams> det = new Dictionary<UUID, DetectParams>();
            if (!CoalescedTouchEvents.TryGetValue(part.LocalId, out det))
                det = new Dictionary<UUID, DetectParams>();

            // Add to queue for all scripts in ObjectID object
            DetectParams detparam = new DetectParams();
            detparam = new DetectParams
                           {
                               Key = remoteClient.AgentId,
                               OffsetPos = new LSL_Types.Vector3(offsetPos.X,
                                                                 offsetPos.Y,
                                                                 offsetPos.Z)
                           };

            detparam.Populate(part.ParentEntity.Scene);
            detparam.LinkNum = child.LinkNum;

            if (surfaceArgs != null)
                detparam.SurfaceTouchArgs = surfaceArgs;

            det[remoteClient.AgentId] = detparam;
            CoalescedTouchEvents[part.LocalId] = det;

            ScriptData[] datas = ScriptEngine.ScriptProtection.GetScripts(part.UUID);

            if (datas == null || datas.Length == 0)
                return;

            string functionName = "touch";
            object[] param = new Object[] { new LSL_Types.LSLInteger(det.Count) };

#if (!ISWIN)
            foreach (ScriptData ID in datas)
            {
                if (CheckIfEventShouldFire(ID, functionName, param))
                {
                    m_scriptEngine.AddToScriptQueue(ID, functionName, new List<DetectParams>(det.Values).ToArray(), EventPriority.FirstStart, param);
                }
            }
#else
            foreach (ScriptData ID in datas.Where(ID => CheckIfEventShouldFire(ID, functionName, param)))
            {
                m_scriptEngine.AddToScriptQueue(ID, functionName, new List<DetectParams>(det.Values).ToArray(),
                                                EventPriority.FirstStart, param);
            }
#endif
        }

        public void touch_end(ISceneChildEntity part, ISceneChildEntity child, IClientAPI remoteClient,
                              SurfaceTouchEventArgs surfaceArgs)
        {
            Dictionary<UUID, DetectParams> det = new Dictionary<UUID, DetectParams>();
            if (!CoalescedTouchEvents.TryGetValue(part.LocalId, out det))
                det = new Dictionary<UUID, DetectParams>();

            // Add to queue for all scripts in ObjectID object
            DetectParams detparam = new DetectParams();
            detparam = new DetectParams { Key = remoteClient.AgentId };

            detparam.Populate(m_scriptEngine.findPrimsScene(part.LocalId));
            detparam.LinkNum = child.LinkNum;

            if (surfaceArgs != null)
                detparam.SurfaceTouchArgs = surfaceArgs;

            det[remoteClient.AgentId] = detparam;
            CoalescedTouchEvents[part.LocalId] = det;

            ScriptData[] datas = ScriptEngine.ScriptProtection.GetScripts(part.UUID);

            if (datas == null || datas.Length == 0)
                return;

            string functionName = "touch_end";
            object[] param = new Object[] { new LSL_Types.LSLInteger(det.Count) };

#if (!ISWIN)
            foreach (ScriptData ID in datas)
            {
                if (CheckIfEventShouldFire(ID, functionName, param))
                {
                    m_scriptEngine.AddToScriptQueue(ID, functionName, new List<DetectParams>(det.Values).ToArray(), EventPriority.FirstStart, param);
                }
            }
#else
            foreach (ScriptData ID in datas.Where(ID => CheckIfEventShouldFire(ID, functionName, param)))
            {
                m_scriptEngine.AddToScriptQueue(ID, functionName, new List<DetectParams>(det.Values).ToArray(),
                                                EventPriority.FirstStart, param);
            }
#endif
            //Remove us from the det param list
            det.Remove(remoteClient.AgentId);
            CoalescedTouchEvents[part.LocalId] = det;
        }

        //public void money(uint localID, UUID agentID, int amount)
        public bool money(uint localID, UUID agentID, int amount)
        {
            bool ret = false;
            ISceneChildEntity part = m_scriptEngine.findPrim(localID);
            if (part == null) return ret;

            ScriptData[] datas = ScriptEngine.ScriptProtection.GetScripts(part.UUID);

            if (datas == null || datas.Length == 0)
            {
                datas = ScriptEngine.ScriptProtection.GetScripts(part.ParentEntity.UUID);
                if (datas == null || datas.Length == 0) return ret;
            }
            string functionName = "money";
            object[] param = new object[]
                                 {
                                     new LSL_Types.LSLString(agentID.ToString()),
                                     new LSL_Types.LSLInteger(amount)
                                 };

#if (!ISWIN)
            foreach (ScriptData ID in datas)
            {
                if (CheckIfEventShouldFire(ID, functionName, param))
                {
                    m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart, param);
                    ret = true;
                }
            }
#else
            foreach (ScriptData ID in datas.Where(ID => CheckIfEventShouldFire(ID, functionName, param)))
            {
                m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart,
                                                param);
                ret = true;
            }
#endif
            return ret;
        }

        public void collision_start(ISceneChildEntity part, ColliderArgs col)
        {
            // Add to queue for all scripts in ObjectID object
            List<DetectParams> det = new List<DetectParams>();

#if (!ISWIN)
            foreach (DetectedObject detobj in col.Colliders)
            {
                DetectParams d = new DetectParams { Key = detobj.keyUUID };
                d.Populate(part.ParentEntity.Scene);
                d.LinkNum = part.LinkNum;
                det.Add(d);
            }
#else
            foreach (DetectParams d in col.Colliders.Select(detobj => new DetectParams {Key = detobj.keyUUID}))
            {
                d.Populate(part.ParentEntity.Scene);
                d.LinkNum = part.LinkNum;
                det.Add(d);
            }
#endif

            if (det.Count > 0)
            {
                ScriptData[] datas = ScriptEngine.ScriptProtection.GetScripts(part.UUID);

                if (datas == null || datas.Length == 0)
                {
                    //datas = ScriptEngine.ScriptProtection.GetScripts(part.ParentGroup.RootPart.UUID);
                    //if (datas == null || datas.Length == 0)
                    return;
                }
                string functionName = "collision_start";
                object[] param = new Object[] { new LSL_Types.LSLInteger(det.Count) };

#if (!ISWIN)
                foreach (ScriptData ID in datas)
                {
                    if (CheckIfEventShouldFire(ID, functionName, param))
                    {
                        m_scriptEngine.AddToScriptQueue(ID, functionName, det.ToArray(), EventPriority.FirstStart, param);
                    }
                }
#else
                foreach (ScriptData ID in datas.Where(ID => CheckIfEventShouldFire(ID, functionName, param)))
                {
                    m_scriptEngine.AddToScriptQueue(ID, functionName, det.ToArray(), EventPriority.FirstStart, param);
                }
#endif
            }
        }

        public void collision(ISceneChildEntity part, ColliderArgs col)
        {
            // Add to queue for all scripts in ObjectID object
            List<DetectParams> det = new List<DetectParams>();

#if (!ISWIN)
            foreach (DetectedObject detobj in col.Colliders)
            {
                DetectParams d = new DetectParams { Key = detobj.keyUUID };
                d.Populate(part.ParentEntity.Scene);
                d.LinkNum = part.LinkNum;
                det.Add(d);
            }
#else
            foreach (DetectParams d in col.Colliders.Select(detobj => new DetectParams {Key = detobj.keyUUID}))
            {
                d.Populate(part.ParentEntity.Scene);
                d.LinkNum = part.LinkNum;
                det.Add(d);
            }
#endif

            if (det.Count > 0)
            {
                ScriptData[] datas = ScriptEngine.ScriptProtection.GetScripts(part.UUID);

                if (datas == null || datas.Length == 0)
                {
                    //datas = ScriptEngine.ScriptProtection.GetScripts(part.ParentGroup.RootPart.UUID);
                    //if (datas == null || datas.Length == 0)
                    return;
                }
                string functionName = "collision";
                object[] param = new Object[] { new LSL_Types.LSLInteger(det.Count) };

#if (!ISWIN)
                foreach (ScriptData ID in datas)
                {
                    if (CheckIfEventShouldFire(ID, functionName, param))
                    {
                        m_scriptEngine.AddToScriptQueue(ID, functionName, det.ToArray(), EventPriority.FirstStart, param);
                    }
                }
#else
                foreach (ScriptData ID in datas.Where(ID => CheckIfEventShouldFire(ID, functionName, param)))
                {
                    m_scriptEngine.AddToScriptQueue(ID, functionName, det.ToArray(), EventPriority.FirstStart, param);
                }
#endif
            }
        }

        public void collision_end(ISceneChildEntity part, ColliderArgs col)
        {
            // Add to queue for all scripts in ObjectID object
            List<DetectParams> det = new List<DetectParams>();

#if (!ISWIN)
            foreach (DetectedObject detobj in col.Colliders)
            {
                DetectParams d = new DetectParams { Key = detobj.keyUUID };
                d.Populate(part.ParentEntity.Scene);
                d.LinkNum = part.LinkNum;
                det.Add(d);
            }
#else
            foreach (DetectParams d in col.Colliders.Select(detobj => new DetectParams {Key = detobj.keyUUID}))
            {
                d.Populate(part.ParentEntity.Scene);
                d.LinkNum = part.LinkNum;
                det.Add(d);
            }
#endif

            if (det.Count > 0)
            {
                ScriptData[] datas = ScriptEngine.ScriptProtection.GetScripts(part.UUID);

                if (datas == null || datas.Length == 0)
                {
                    //datas = ScriptEngine.ScriptProtection.GetScripts(part.ParentGroup.RootPart.UUID);
                    //if (datas == null || datas.Length == 0)
                    return;
                }
                string functionName = "collision_end";
                object[] param = new Object[] { new LSL_Types.LSLInteger(det.Count) };

#if (!ISWIN)
                foreach (ScriptData ID in datas)
                {
                    if (CheckIfEventShouldFire(ID, functionName, param))
                    {
                        m_scriptEngine.AddToScriptQueue(ID, functionName, det.ToArray(), EventPriority.FirstStart, param);
                    }
                }
#else
                foreach (ScriptData ID in datas.Where(ID => CheckIfEventShouldFire(ID, functionName, param)))
                {
                    m_scriptEngine.AddToScriptQueue(ID, functionName, det.ToArray(), EventPriority.FirstStart, param);
                }
#endif
            }
        }

        public void land_collision_start(ISceneChildEntity part, ColliderArgs col)
        {
            List<DetectParams> det = new List<DetectParams>();

#if (!ISWIN)
            foreach (DetectedObject detobj in col.Colliders)
            {
                DetectParams d = new DetectParams
                                     {
                                         Position =
                                             new LSL_Types.Vector3(detobj.posVector.X, detobj.posVector.Y,
                                                                   detobj.posVector.Z),
                                         Key = detobj.keyUUID
                                     };
                d.Populate(part.ParentEntity.Scene);
                d.LinkNum = part.LinkNum;
                det.Add(d);
            }
#else
            foreach (DetectParams d in col.Colliders.Select(detobj => new DetectParams
                                                                          {
                                                                              Position = new LSL_Types.Vector3(detobj.posVector.X,
                                                                                                               detobj.posVector.Y,
                                                                                                               detobj.posVector.Z),
                                                                              Key = detobj.keyUUID
                                                                          }))
            {
                d.Populate(part.ParentEntity.Scene);
                d.LinkNum = part.LinkNum;
                det.Add(d);
            }
#endif
            if (det.Count != 0)
            {
                ScriptData[] datas = ScriptEngine.ScriptProtection.GetScripts(part.UUID);

                if (datas == null || datas.Length == 0)
                {
                    //datas = ScriptEngine.ScriptProtection.GetScripts(part.ParentGroup.RootPart.UUID);
                    //if (datas == null || datas.Length == 0)
                    return;
                }
                string functionName = "land_collision_start";
                object[] param = new Object[] { new LSL_Types.Vector3(det[0].Position) };

#if (!ISWIN)
                foreach (ScriptData ID in datas)
                {
                    if (CheckIfEventShouldFire(ID, functionName, param))
                    {
                        m_scriptEngine.AddToScriptQueue(ID, functionName, det.ToArray(), EventPriority.FirstStart, param);
                    }
                }
#else
                foreach (ScriptData ID in datas.Where(ID => CheckIfEventShouldFire(ID, functionName, param)))
                {
                    m_scriptEngine.AddToScriptQueue(ID, functionName, det.ToArray(), EventPriority.FirstStart, param);
                }
#endif
            }
        }

        public void land_collision(ISceneChildEntity part, ColliderArgs col)
        {
            List<DetectParams> det = new List<DetectParams>();

#if (!ISWIN)
            foreach (DetectedObject detobj in col.Colliders)
            {
                DetectParams d = new DetectParams
                                     {
                                         Position = new LSL_Types.Vector3(detobj.posVector.X, detobj.posVector.Y, detobj.posVector.Z),
                                         Key = detobj.keyUUID
                                     };
                d.Populate(part.ParentEntity.Scene);
                d.LinkNum = part.LinkNum;
                det.Add(d);
            }
#else
            foreach (DetectParams d in col.Colliders.Select(detobj => new DetectParams
                                                                          {
                                                                              Position = new LSL_Types.Vector3(detobj.posVector.X,
                                                                                                               detobj.posVector.Y,
                                                                                                               detobj.posVector.Z),
                                                                              Key = detobj.keyUUID
                                                                          }))
            {
                d.Populate(part.ParentEntity.Scene);
                d.LinkNum = part.LinkNum;
                det.Add(d);
            }
#endif
            if (det.Count != 0)
            {
                ScriptData[] datas = ScriptEngine.ScriptProtection.GetScripts(part.UUID);

                if (datas == null || datas.Length == 0)
                {
                    //datas = ScriptEngine.ScriptProtection.GetScripts(part.ParentGroup.RootPart.UUID);
                    //if (datas == null || datas.Length == 0)
                    return;
                }
                string functionName = "land_collision";
                object[] param = new Object[] { new LSL_Types.Vector3(det[0].Position) };

#if (!ISWIN)
                foreach (ScriptData ID in datas)
                {
                    if (CheckIfEventShouldFire(ID, functionName, param))
                    {
                        m_scriptEngine.AddToScriptQueue(ID, functionName, det.ToArray(), EventPriority.FirstStart, param);
                    }
                }
#else
                foreach (ScriptData ID in datas.Where(ID => CheckIfEventShouldFire(ID, functionName, param)))
                {
                    m_scriptEngine.AddToScriptQueue(ID, functionName, det.ToArray(), EventPriority.FirstStart, param);
                }
#endif
            }
        }

        public void land_collision_end(ISceneChildEntity part, ColliderArgs col)
        {
            List<DetectParams> det = new List<DetectParams>();

#if (!ISWIN)
            foreach (DetectedObject detobj in col.Colliders)
            {
                DetectParams d = new DetectParams
                                     {
                                         Position = new LSL_Types.Vector3(detobj.posVector.X, detobj.posVector.Y, detobj.posVector.Z),
                                         Key = detobj.keyUUID
                                     };
                d.Populate(part.ParentEntity.Scene);
                d.LinkNum = part.LinkNum;
                det.Add(d);
            }
#else
            foreach (DetectParams d in col.Colliders.Select(detobj => new DetectParams
                                                                          {
                                                                              Position = new LSL_Types.Vector3(detobj.posVector.X,
                                                                                                               detobj.posVector.Y,
                                                                                                               detobj.posVector.Z),
                                                                              Key = detobj.keyUUID
                                                                          }))
            {
                d.Populate(part.ParentEntity.Scene);
                d.LinkNum = part.LinkNum;
                det.Add(d);
            }
#endif
            if (det.Count != 0)
            {
                ScriptData[] datas = ScriptEngine.ScriptProtection.GetScripts(part.UUID);

                if (datas == null || datas.Length == 0)
                {
                    //datas = ScriptEngine.ScriptProtection.GetScripts(part.ParentGroup.RootPart.UUID);
                    //if (datas == null || datas.Length == 0)
                    return;
                }
                string functionName = "land_collision_end";
                object[] param = new Object[] { new LSL_Types.Vector3(det[0].Position) };

#if (!ISWIN)
                foreach (ScriptData ID in datas)
                {
                    if (CheckIfEventShouldFire(ID, functionName, param))
                    {
                        m_scriptEngine.AddToScriptQueue(ID, functionName, det.ToArray(), EventPriority.FirstStart, param);
                    }
                }
#else
                foreach (ScriptData ID in datas.Where(ID => CheckIfEventShouldFire(ID, functionName, param)))
                {
                    m_scriptEngine.AddToScriptQueue(ID, functionName, det.ToArray(), EventPriority.FirstStart, param);
                }
#endif
            }
        }

        public void control(ISceneChildEntity part, UUID itemID, UUID agentID, uint held, uint change)
        {
            if (part == null)
                return;
            ScriptData ID = ScriptEngine.ScriptProtection.GetScript(part.UUID, itemID);

            if (ID == null)
                return;

            string functionName = "control";
            object[] param = new object[]
                                 {
                                     new LSL_Types.LSLString(agentID.ToString()),
                                     new LSL_Types.LSLInteger(held),
                                     new LSL_Types.LSLInteger(change)
                                 };

            if (CheckIfEventShouldFire(ID, functionName, param))
                m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart, param);
        }

        public void email(uint localID, UUID itemID, string timeSent,
                          string address, string subject, string message, int numLeft)
        {
            ISceneChildEntity part = m_scriptEngine.findPrim(localID);
            if (part == null)
                return;
            ScriptData[] datas = ScriptEngine.ScriptProtection.GetScripts(part.UUID);

            if (datas == null || datas.Length == 0)
            {
                datas = ScriptEngine.ScriptProtection.GetScripts(part.ParentEntity.UUID);
                if (datas == null || datas.Length == 0)
                    return;
            }
            string functionName = "email";
            object[] param = new object[]
                                 {
                                     new LSL_Types.LSLString(timeSent),
                                     new LSL_Types.LSLString(address),
                                     new LSL_Types.LSLString(subject),
                                     new LSL_Types.LSLString(message),
                                     new LSL_Types.LSLInteger(numLeft)
                                 };

#if (!ISWIN)
            foreach (ScriptData ID in datas)
            {
                if (CheckIfEventShouldFire(ID, functionName, param))
                {
                    m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart, param);
                }
            }
#else
            foreach (ScriptData ID in datas.Where(ID => CheckIfEventShouldFire(ID, functionName, param)))
            {
                m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart,
                                                param);
            }
#endif
        }

        public void at_target(uint localID, uint handle, Vector3 targetpos,
                              Vector3 atpos)
        {
            ISceneChildEntity part = m_scriptEngine.findPrim(localID);
            if (part == null)
                return;
            ScriptData[] datas = ScriptEngine.ScriptProtection.GetScripts(part.UUID);

            if (datas == null || datas.Length == 0)
            {
                datas = ScriptEngine.ScriptProtection.GetScripts(part.ParentEntity.UUID);
                if (datas == null || datas.Length == 0)
                    return;
            }
            string functionName = "at_target";
            object[] param = new object[]
                                 {
                                     new LSL_Types.LSLInteger(handle),
                                     new LSL_Types.Vector3(targetpos.X, targetpos.Y, targetpos.Z),
                                     new LSL_Types.Vector3(atpos.X, atpos.Y, atpos.Z)
                                 };

#if (!ISWIN)
            foreach (ScriptData ID in datas)
            {
                if (CheckIfEventShouldFire(ID, functionName, param))
                {
                    m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart, param);
                }
            }
#else
            foreach (ScriptData ID in datas.Where(ID => CheckIfEventShouldFire(ID, functionName, param)))
            {
                m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart,
                                                param);
            }
#endif
        }

        public void not_at_target(uint localID)
        {
            ISceneChildEntity part = m_scriptEngine.findPrim(localID);
            if (part == null)
                return;
            ScriptData[] datas = ScriptEngine.ScriptProtection.GetScripts(part.UUID);

            if (datas == null || datas.Length == 0)
            {
                datas = ScriptEngine.ScriptProtection.GetScripts(part.ParentEntity.UUID);
                if (datas == null || datas.Length == 0)
                    return;
            }
            string functionName = "not_at_target";
            object[] param = new object[0];

#if (!ISWIN)
            foreach (ScriptData ID in datas)
            {
                if (CheckIfEventShouldFire(ID, functionName, param))
                {
                    m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart, param);
                }
            }
#else
            foreach (ScriptData ID in datas.Where(ID => CheckIfEventShouldFire(ID, functionName, param)))
            {
                m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart,
                                                param);
            }
#endif
        }

        public void at_rot_target(uint localID, uint handle, Quaternion targetrot,
                                  Quaternion atrot)
        {
            ISceneChildEntity part = m_scriptEngine.findPrim(localID);
            if (part == null)
                return;
            ScriptData[] datas = ScriptEngine.ScriptProtection.GetScripts(part.UUID);

            if (datas == null || datas.Length == 0)
            {
                datas = ScriptEngine.ScriptProtection.GetScripts(part.ParentEntity.UUID);
                if (datas == null || datas.Length == 0)
                    return;
            }
            string functionName = "at_rot_target";
            object[] param = new object[]
                                 {
                                     new LSL_Types.LSLInteger(handle),
                                     new LSL_Types.Quaternion(targetrot.X, targetrot.Y, targetrot.Z, targetrot.W),
                                     new LSL_Types.Quaternion(atrot.X, atrot.Y, atrot.Z, atrot.W)
                                 };

#if (!ISWIN)
            foreach (ScriptData ID in datas)
            {
                if (CheckIfEventShouldFire(ID, functionName, param))
                {
                    m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart, param);
                }
            }
#else
            foreach (ScriptData ID in datas.Where(ID => CheckIfEventShouldFire(ID, functionName, param)))
            {
                m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart,
                                                param);
            }
#endif
        }

        public void not_at_rot_target(uint localID)
        {
            ISceneChildEntity part = m_scriptEngine.findPrim(localID);
            if (part == null)
                return;
            ScriptData[] datas = ScriptEngine.ScriptProtection.GetScripts(part.UUID);

            if (datas == null || datas.Length == 0)
            {
                datas = ScriptEngine.ScriptProtection.GetScripts(part.ParentEntity.UUID);
                if (datas == null || datas.Length == 0)
                    return;
            }
            string functionName = "not_at_rot_target";
            object[] param = new object[0];

#if (!ISWIN)
            foreach (ScriptData ID in datas)
            {
                if (CheckIfEventShouldFire(ID, functionName, param))
                {
                    m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart, param);
                }
            }
#else
            foreach (ScriptData ID in datas.Where(ID => CheckIfEventShouldFire(ID, functionName, param)))
            {
                m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart,
                                                param);
            }
#endif
        }

        public void attach(uint localID, UUID itemID, UUID avatar)
        {
            ISceneChildEntity part = m_scriptEngine.findPrim(localID);
            if (part == null)
                return;
            ScriptData[] datas = ScriptEngine.ScriptProtection.GetScripts(part.UUID);

            if (datas == null || datas.Length == 0)
            {
                datas = ScriptEngine.ScriptProtection.GetScripts(part.ParentEntity.UUID);
                if (datas == null || datas.Length == 0)
                    return;
            }
            string functionName = "attach";
            object[] param = new object[]
                                 {
                                     new LSL_Types.LSLString(avatar.ToString())
                                 };

#if (!ISWIN)
            foreach (ScriptData ID in datas)
            {
                if (CheckIfEventShouldFire(ID, functionName, param))
                {
                    m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart, param);
                }
            }
#else
            foreach (ScriptData ID in datas.Where(ID => CheckIfEventShouldFire(ID, functionName, param)))
            {
                m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart,
                                                param);
            }
#endif
        }

        public void moving_start(ISceneChildEntity part)
        {
            ScriptData[] datas = ScriptEngine.ScriptProtection.GetScripts(part.UUID);

            if (datas == null || datas.Length == 0)
            {
                datas = ScriptEngine.ScriptProtection.GetScripts(part.ParentEntity.RootChild.UUID);
                if (datas == null || datas.Length == 0)
                    return;
            }
            string functionName = "moving_start";
            object[] param = new object[0];

#if (!ISWIN)
            foreach (ScriptData ID in datas)
            {
                if (CheckIfEventShouldFire(ID, functionName, param))
                {
                    m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart, param);
                }
            }
#else
            foreach (ScriptData ID in datas.Where(ID => CheckIfEventShouldFire(ID, functionName, param)))
            {
                m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart,
                                                param);
            }
#endif
        }

        public void moving_end(ISceneChildEntity part)
        {
            ScriptData[] datas = ScriptEngine.ScriptProtection.GetScripts(part.UUID);

            if (datas == null || datas.Length == 0)
            {
                datas = ScriptEngine.ScriptProtection.GetScripts(part.ParentEntity.RootChild.UUID);
                if (datas == null || datas.Length == 0)
                    return;
            }
            string functionName = "moving_end";
            object[] param = new object[0];

#if (!ISWIN)
            foreach (ScriptData ID in datas)
            {
                if (CheckIfEventShouldFire(ID, functionName, param))
                {
                    m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart, param);
                }
            }
#else
            foreach (ScriptData ID in datas.Where(ID => CheckIfEventShouldFire(ID, functionName, param)))
            {
                m_scriptEngine.AddToScriptQueue(ID, functionName, new DetectParams[0], EventPriority.FirstStart,
                                                param);
            }
#endif
        }

        /// <summary>
        ///   Start multiple scripts in the object
        /// </summary>
        /// <param name = "part"></param>
        /// <param name = "items"></param>
        /// <param name = "startParam"></param>
        /// <param name = "postOnRez"></param>
        /// <param name = "engine"></param>
        /// <param name = "stateSource"></param>
        /// <param name = "RezzedFrom"></param>
        public void rez_scripts(ISceneChildEntity part, TaskInventoryItem[] items,
                                int startParam, bool postOnRez, StateSource stateSource, UUID RezzedFrom)
        {
#if (!ISWIN)
            List<LUStruct> ItemsToStart = new List<LUStruct>();
            foreach (TaskInventoryItem item in items)
            {
                LUStruct itemToQueue = m_scriptEngine.StartScript(part, item.ItemID, startParam, postOnRez, stateSource, RezzedFrom);
                if (itemToQueue.Action != LUType.Unknown) ItemsToStart.Add(itemToQueue);
            }
#else
            List<LUStruct> ItemsToStart = items.Select(item => m_scriptEngine.StartScript(part, item.ItemID, startParam, postOnRez, stateSource, RezzedFrom)).Where(itemToQueue => itemToQueue.Action != LUType.Unknown).ToList();
#endif
            if (ItemsToStart.Count != 0)
                m_scriptEngine.MaintenanceThread.AddScriptChange(ItemsToStart.ToArray(), LoadPriority.FirstStart);
        }

        /// <summary>
        ///   This checks the minimum amount of time between script firings as well as control events, making sure that events do NOT fire after scripts reset, close or restart, etc
        /// </summary>
        /// <param name = "ID"></param>
        /// <param name = "FunctionName"></param>
        /// <param name = "param"></param>
        /// <returns></returns>
        private bool CheckIfEventShouldFire(ScriptData ID, string FunctionName, object[] param)
        {
            lock (ID.ScriptEventLock)
            {
                if (ID.Loading)
                {
                    //If the script is loading, enqueue all events
                    return true;
                }
                //This will happen if the script doesn't compile correctly
                if (ID.Script == null)
                {
                    MainConsole.Instance.Info("[AuroraDotNetEngine]: Could not load script from item '" + ID.InventoryItem.Name +
                               "' to fire event " + FunctionName);
                    return false;
                }
                scriptEvents eventType = (scriptEvents)Enum.Parse(typeof(scriptEvents), FunctionName);

                // this must be done even if there is no event method

                if (eventType == scriptEvents.touch_start)
                    ID.RemoveTouchEvents = false;
                else if (eventType == scriptEvents.collision_start)
                    ID.RemoveCollisionEvents = false;
                else if (eventType == scriptEvents.land_collision_start)
                    ID.RemoveLandCollisionEvents = false;

                if (eventType == scriptEvents.state_entry)
                    ID.ResetEvents();

                if ((ID.Script.GetStateEventFlags(ID.State) & (long)eventType) == 0)
                    return false; //If the script doesn't contain the state, don't even bother queueing it

                //Make sure we can execute events at position
                if (!m_scriptEngine.PipeEventsForScript(ID.Part))
                    return false;

                switch (eventType)
                {
                    case scriptEvents.timer:
                        if (ID.TimerInQueue)
                            return false;
                        ID.TimerInQueue = true;
                        break;
                    case scriptEvents.sensor:
                        if (ID.SensorInQueue)
                            return false;
                        ID.SensorInQueue = true;
                        break;
                    case scriptEvents.no_sensor:
                        if (ID.NoSensorInQueue)
                            return false;
                        ID.NoSensorInQueue = true;
                        break;
                    case scriptEvents.at_target:
                        if (ID.AtTargetInQueue)
                            return false;
                        ID.AtTargetInQueue = true;
                        break;
                    case scriptEvents.not_at_target:
                        if (ID.NotAtTargetInQueue)
                            return false;
                        ID.NotAtTargetInQueue = true;
                        break;
                    case scriptEvents.at_rot_target:
                        if (ID.AtRotTargetInQueue)
                            return false;
                        ID.AtRotTargetInQueue = true;
                        break;
                    case scriptEvents.not_at_rot_target:
                        if (ID.NotAtRotTargetInQueue)
                            return false;
                        ID.NotAtRotTargetInQueue = true;
                        break;
                    case scriptEvents.control:
                        int held = ((LSL_Types.LSLInteger)param[1]).value;
                        // int changed = ((LSL_Types.LSLInteger)data.Params[2]).value;

                        // If the last message was a 0 (nothing held)
                        // and this one is also nothing held, drop it
                        //
                        if (ID.LastControlLevel == held && held == 0)
                            return true;

                        // If there is one or more queued, then queue
                        // only changed ones, else queue unconditionally
                        //
                        if (ID.ControlEventsInQueue > 0)
                        {
                            if (ID.LastControlLevel == held)
                                return false;
                        }
                        break;
                    case scriptEvents.collision:
                        if (ID.CollisionInQueue || ID.RemoveCollisionEvents)
                            return false;
                        ID.CollisionInQueue = true;
                        break;
                    case scriptEvents.moving_start:
                        if (ID.MovingInQueue) //Block all other moving_starts until moving_end is called
                            return false;
                        ID.MovingInQueue = true;
                        break;
                    case scriptEvents.moving_end:
                        if (!ID.MovingInQueue) //If we get a moving_end after we have sent one event, don't fire another
                            return false;
                        break;
                    case scriptEvents.collision_end:
                        if (ID.RemoveCollisionEvents)
                            return false;
                        break;
                    case scriptEvents.touch:
                        if (ID.TouchInQueue || ID.RemoveTouchEvents)
                            return false;
                        ID.TouchInQueue = true;
                        break;
                    case scriptEvents.touch_end:
                        if (ID.RemoveTouchEvents)
                            return false;
                        break;
                    case scriptEvents.land_collision:
                        if (ID.LandCollisionInQueue || ID.RemoveLandCollisionEvents)
                            return false;
                        ID.LandCollisionInQueue = true;
                        break;
                    case scriptEvents.land_collision_end:
                        if (ID.RemoveLandCollisionEvents)
                            return false;
                        break;
                    case scriptEvents.changed:
                        Changed changed;
                        if (param[0] is Changed)
                            changed = (Changed)param[0];
                        else
                            changed = (Changed)(((LSL_Types.LSLInteger)param[0]).value);
                        if (ID.ChangedInQueue.Contains(changed))
                            return false;
                        ID.ChangedInQueue.Add(changed);
                        break;
                }
            }
            return true;
        }

        /// <summary>
        ///   This removes the event from the queue and allows it to be fired again
        /// </summary>
        /// <param name = "QIS"></param>
        public void EventComplete(QueueItemStruct QIS)
        {
            lock (QIS.ID.ScriptEventLock)
            {
                scriptEvents eventType = (scriptEvents)Enum.Parse(typeof(scriptEvents), QIS.functionName);
                switch (eventType)
                {
                    case scriptEvents.timer:
                        QIS.ID.TimerInQueue = false;
                        break;
                    case scriptEvents.control:
                        if (QIS.ID.ControlEventsInQueue > 0)
                            QIS.ID.ControlEventsInQueue--;
                        break;
                    case scriptEvents.collision:
                        QIS.ID.CollisionInQueue = false;
                        break;
                    case scriptEvents.collision_end:
                        QIS.ID.CollisionInQueue = false;
                        break;
                    case scriptEvents.moving_end:
                        QIS.ID.MovingInQueue = false;
                        break;
                    case scriptEvents.touch:
                        QIS.ID.TouchInQueue = false;
                        break;
                    case scriptEvents.touch_end:
                        QIS.ID.TouchInQueue = false;
                        break;
                    case scriptEvents.land_collision:
                        QIS.ID.LandCollisionInQueue = false;
                        break;
                    case scriptEvents.land_collision_end:
                        QIS.ID.LandCollisionInQueue = false;
                        break;
                    case scriptEvents.sensor:
                        QIS.ID.SensorInQueue = false;
                        break;
                    case scriptEvents.no_sensor:
                        QIS.ID.NoSensorInQueue = false;
                        break;
                    case scriptEvents.at_target:
                        QIS.ID.AtTargetInQueue = false;
                        break;
                    case scriptEvents.not_at_target:
                        QIS.ID.NotAtTargetInQueue = false;
                        break;
                    case scriptEvents.at_rot_target:
                        QIS.ID.AtRotTargetInQueue = false;
                        break;
                    case scriptEvents.not_at_rot_target:
                        QIS.ID.NotAtRotTargetInQueue = false;
                        break;
                    case scriptEvents.changed:
                        Changed changed;
                        if (QIS.param[0] is Changed)
                        {
                            changed = (Changed)QIS.param[0];
                        }
                        else
                        {
                            changed = (Changed)(((LSL_Types.LSLInteger)QIS.param[0]).value);
                        }
                        QIS.ID.ChangedInQueue.Remove(changed);
                        break;
                }
            }
        }
    }
}