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
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;

using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using Aurora.ScriptEngine.AuroraDotNetEngine.Plugins;
using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.Plugins
{
    public class SensorRepeatPlugin : IScriptPlugin
    {
        public ScriptEngine m_ScriptEngine;

        public void Initialize(ScriptEngine engine)
        {
            m_ScriptEngine = engine;
            maximumRange = engine.Config.GetDouble("SensorMaxRange", 512.0d);
            usemaximumRange = engine.Config.GetBoolean("UseSensorMaxRange", true);
            maximumToReturn = engine.Config.GetInt("SensorMaxResults", 32);
            usemaximumToReturn = engine.Config.GetBoolean("UseSensorMaxResults", true);
        }

        public void AddRegion (Scene scene)
        {
        }

        private Object SenseLock = new Object();

        private const int AGENT = 1;
        private const int ACTIVE = 2;
        private const int PASSIVE = 4;
        private const int SCRIPTED = 8;

        private double maximumRange = 96.0;
        private bool usemaximumRange = true;
        private int maximumToReturn = 16;
        private bool usemaximumToReturn = true;

        //
        // SenseRepeater and Sensors
        //
        private class SenseRepeatClass
        {
            public UUID objectID;
            public UUID itemID;
            public double interval;
            public DateTime next;

            public string name;
            public UUID keyID;
            public int type;
            public double range;
            public double arc;
            public ISceneChildEntity host;
        }

        //
        // Sensed entity
        //
        private class SensedEntity : IComparable
        {
            public SensedEntity(double detectedDistance, UUID detectedID)
            {
                distance = detectedDistance;
                itemID = detectedID;
            }
            public int CompareTo(object obj)
            {
                if (!(obj is SensedEntity)) throw new InvalidOperationException();
                SensedEntity ent = (SensedEntity)obj;
                if (ent == null || ent.distance < distance) return 1;
                if (ent.distance > distance) return -1;
                return 0;
            }
            public UUID itemID;
            public double distance;
        }

        private List<SenseRepeatClass> SenseRepeaters = new List<SenseRepeatClass>();
        private object SenseRepeatListLock = new object();

        public void SetSenseRepeatEvent(UUID objectID, UUID m_itemID,
                                        string name, UUID keyID, int type, double range,
                                        double arc, double sec, ISceneChildEntity host)
        {
            // Always remove first, in case this is a re-set
            RemoveScript(objectID, m_itemID);
            if (sec == 0) // Disabling timer
                return;

            // Add to timer
            SenseRepeatClass ts = new SenseRepeatClass();
            ts.objectID = objectID;
            ts.itemID = m_itemID;
            ts.interval = sec;
            ts.name = name;
            ts.keyID = keyID;
            ts.type = type;
            if (range > maximumRange && usemaximumRange)
                ts.range = maximumRange;
            else
                ts.range = range;
            ts.arc = arc;
            ts.host = host;

            ts.next = DateTime.Now.ToUniversalTime().AddSeconds(ts.interval);
            lock (SenseRepeatListLock)
            {
                SenseRepeaters.Add(ts);
            }
        }

        public void RemoveScript(UUID objectID, UUID m_itemID)
        {
            // Remove from timer
            lock (SenseRepeatListLock)
            {
                List<SenseRepeatClass> NewSensors = new List<SenseRepeatClass>();
                foreach (SenseRepeatClass ts in SenseRepeaters)
                {
                    if (ts.objectID != objectID && ts.itemID != m_itemID)
                    {
                        NewSensors.Add(ts);
                    }
                }
                SenseRepeaters.Clear();
                SenseRepeaters = NewSensors;
            }
        }

        public void Check()
        {
            // Nothing to do here?
            if (SenseRepeaters.Count == 0)
                return;

            lock (SenseRepeatListLock)
            {
                // Go through all timers
                DateTime UniversalTime = DateTime.Now.ToUniversalTime();
                foreach (SenseRepeatClass ts in SenseRepeaters)
                {
                    // Time has passed?
                    if (ts.next.ToUniversalTime() < UniversalTime)
                    {
                        SensorSweep(ts);
                        // set next interval
                        ts.next = DateTime.Now.ToUniversalTime().AddSeconds(ts.interval);
                    }
                }
            } // lock
        }

        public void SenseOnce(UUID objectID, UUID m_itemID,
                              string name, UUID keyID, int type,
                              double range, double arc, ISceneChildEntity host)
        {
            // Add to timer
            SenseRepeatClass ts = new SenseRepeatClass();
            ts.objectID = objectID;
            ts.itemID = m_itemID;
            ts.interval = 0;
            ts.name = name;
            ts.keyID = keyID;
            ts.type = type;
            if (range > maximumRange && usemaximumRange)
                ts.range = maximumRange;
            else
                ts.range = range;
            ts.arc = arc;
            ts.host = host;
            SensorSweep(ts);
        }

        private void SensorSweep(SenseRepeatClass ts)
        {
            if (ts.host == null)
            {
                return;
            }

            List<SensedEntity> sensedEntities = new List<SensedEntity>();

            // Is the sensor type is AGENT and not SCRIPTED then include agents
            if ((ts.type & AGENT) != 0 && (ts.type & SCRIPTED) == 0)
            {
               sensedEntities.AddRange(doAgentSensor(ts));
            }

            // If SCRIPTED or PASSIVE or ACTIVE check objects
            if ((ts.type & SCRIPTED) != 0 || (ts.type & PASSIVE) != 0 || (ts.type & ACTIVE) != 0)
            {
                sensedEntities.AddRange(doObjectSensor(ts));
            }

            lock (SenseLock)
            {
                if (sensedEntities.Count == 0)
                {
                    // send a "no_sensor"
                    // Add it to queue
                    m_ScriptEngine.PostScriptEvent(ts.itemID, ts.objectID,
                            new EventParams("no_sensor", new Object[0],
                            new DetectParams[0]));
                }
                else
                {
                    // Sort the list to get everything ordered by distance
                    sensedEntities.Sort();
                    int count = sensedEntities.Count;
                    int idx;
                    List<DetectParams> detected = new List<DetectParams>();
                    for (idx = 0; idx < count; idx++)
                    {
                        if (ts.host != null && ts.host.ParentEntity != null && ts.host.ParentEntity.Scene != null)
                        {	
                        	DetectParams detect = new DetectParams();
                            detect.Key = sensedEntities[idx].itemID;
                            detect.Populate (ts.host.ParentEntity.Scene);
                            detected.Add(detect);
                        	if (detected.Count == maximumToReturn &&
                                usemaximumToReturn)
                            	break;
                        }
                    }

                    if (detected.Count == 0)
                    {
                        // To get here with zero in the list there must have been some sort of problem
                        // like the object being deleted or the avatar leaving to have caused some
                        // difficulty during the Populate above so fire a no_sensor event
                        m_ScriptEngine.PostScriptEvent(ts.itemID, ts.objectID,
                                new EventParams("no_sensor", new Object[0],
                                new DetectParams[0]));
                    }
                    else
                    {
                        m_ScriptEngine.PostScriptEvent(ts.itemID, ts.objectID,
                                new EventParams("sensor",
                                new Object[] {new LSL_Types.LSLInteger(detected.Count) },
                                detected.ToArray()));
                    }
                }
            }
        }

        private List<SensedEntity> doObjectSensor(SenseRepeatClass ts)
        {
            List<ISceneEntity> Entities;
            List<SensedEntity> sensedEntities = new List<SensedEntity>();

            ISceneChildEntity SensePoint = ts.host;

            Vector3 fromRegionPos = SensePoint.AbsolutePosition;

            // If this is an object sense by key try to get it directly
            // rather than getting a list to scan through
            if (ts.keyID != UUID.Zero)
            {
                IEntity e = null;
                ts.host.ParentEntity.Scene.Entities.TryGetValue (ts.keyID, out e);
                if (e == null || !(e is ISceneEntity))
                    return sensedEntities;
                Entities = new List<ISceneEntity> ();
                Entities.Add (e as ISceneEntity);
            }
            else
            {
                Entities = new List<ISceneEntity> (ts.host.ParentEntity.Scene.Entities.GetEntities (fromRegionPos, (float)ts.range));
            }

            // pre define some things to avoid repeated definitions in the loop body
            Vector3 toRegionPos;
            double dis;
            int objtype;
            SceneObjectPart part;
            float dx;
            float dy;
            float dz;

            Quaternion q = SensePoint.RotationOffset;
            if (SensePoint.ParentEntity.RootChild.IsAttachment)
            {
                // In attachments, the sensor cone always orients with the
                // avatar rotation. This may include a nonzero elevation if
                // in mouselook.

                IScenePresence avatar = ts.host.ParentEntity.Scene.GetScenePresence (SensePoint.ParentEntity.RootChild.AttachedAvatar);
                q = avatar.Rotation;
            }
            LSL_Types.Quaternion r = new LSL_Types.Quaternion(q.X, q.Y, q.Z, q.W);
            LSL_Types.Vector3 forward_dir = (new LSL_Types.Vector3(1, 0, 0) * r);
            double mag_fwd = LSL_Types.Vector3.Mag(forward_dir);

            Vector3 ZeroVector = new Vector3(0, 0, 0);

            bool nameSearch = (ts.name != null && ts.name != "");

            foreach (ISceneEntity ent in Entities)
            {
                bool keep = true;

                if (nameSearch && ent.Name != ts.name) // Wrong name and it is a named search
                    continue;

                if (ent.IsDeleted) // taken so long to do this it has gone from the scene
                    continue;

                if (!(ent is SceneObjectGroup)) // dont bother if it is a pesky avatar
                    continue;
                toRegionPos = ent.AbsolutePosition;

                // Calculation is in line for speed
                dx = toRegionPos.X - fromRegionPos.X;
                dy = toRegionPos.Y - fromRegionPos.Y;
                dz = toRegionPos.Z - fromRegionPos.Z;

                // Weed out those that will not fit in a cube the size of the range
                // no point calculating if they are within a sphere the size of the range
                // if they arent even in the cube
                if (Math.Abs(dx) > ts.range || Math.Abs(dy) > ts.range || Math.Abs(dz) > ts.range)
                    dis = ts.range + 1.0;
                else
                    dis = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                if (keep && dis <= ts.range && ts.host.UUID != ent.UUID)
                {
                    // In Range and not the object containing the script, is it the right Type ?
                    objtype = 0;

                    part = ((SceneObjectGroup)ent).RootPart;
                    if (part.AttachmentPoint != 0) // Attached so ignore
                        continue;

                    if (part.Inventory.ContainsScripts())
                    {
                        objtype |= ACTIVE | SCRIPTED; // Scripted and active. It COULD have one hidden ...
                    }
                    else
                    {
                        if (ent.Velocity.Equals(ZeroVector))
                        {
                            objtype |= PASSIVE; // Passive non-moving
                        }
                        else
                        {
                            objtype |= ACTIVE; // moving so active
                        }
                    }

                    // If any of the objects attributes match any in the requested scan type
                    if (((ts.type & objtype) != 0))
                    {
                        // Right type too, what about the other params , key and name ?
                        if (ts.arc < Math.PI)
                        {
                            // not omni-directional. Can you see it ?
                            // vec forward_dir = llRot2Fwd(llGetRot())
                            // vec obj_dir = toRegionPos-fromRegionPos
                            // dot=dot(forward_dir,obj_dir)
                            // mag_fwd = mag(forward_dir)
                            // mag_obj = mag(obj_dir)
                            // ang = acos(dot /(mag_fwd*mag_obj))
                            double ang_obj = 0;
                            try
                            {
                                Vector3 diff = toRegionPos - fromRegionPos;
                                LSL_Types.Vector3 obj_dir = new LSL_Types.Vector3(diff.X, diff.Y, diff.Z);
                                double dot = LSL_Types.Vector3.Dot(forward_dir, obj_dir);
                                double mag_obj = LSL_Types.Vector3.Mag(obj_dir);
                                ang_obj = Math.Acos(dot / (mag_fwd * mag_obj));
                            }
                            catch
                            {
                            }

                            if (ang_obj > ts.arc) keep = false;
                        }

                        if (keep == true)
                        {
                            // add distance for sorting purposes later
                            sensedEntities.Add(new SensedEntity(dis, ent.UUID));
                        }
                    }
                }
            }
            return sensedEntities;
        }

        private List<SensedEntity> doAgentSensor(SenseRepeatClass ts)
        {
            List<SensedEntity> sensedEntities = new List<SensedEntity>();

            // If nobody about quit fast
            IEntityCountModule entityCountModule = ts.host.ParentEntity.Scene.RequestModuleInterface<IEntityCountModule> ();
            if (entityCountModule != null && entityCountModule.RootAgents == 0)
                return sensedEntities;

            ISceneChildEntity SensePoint = ts.host;
            Vector3 fromRegionPos = SensePoint.AbsolutePosition;
            Quaternion q = SensePoint.RotationOffset;
            LSL_Types.Quaternion r = new LSL_Types.Quaternion(q.X, q.Y, q.Z, q.W);
            LSL_Types.Vector3 forward_dir = (new LSL_Types.Vector3(1, 0, 0) * r);
            double mag_fwd = LSL_Types.Vector3.Mag(forward_dir);
            bool attached = (SensePoint.AttachmentPoint != 0);
            Vector3 toRegionPos;
            double dis;

            Action<IScenePresence> senseEntity = new Action<IScenePresence>(delegate(IScenePresence presence)
            {
                if (presence.IsDeleted || presence.IsChildAgent || presence.GodLevel > 0.0)
                    return;
                
                // if the object the script is in is attached and the avatar is the owner
                // then this one is not wanted
                if (attached && presence.UUID == SensePoint.OwnerID)
                    return;

                toRegionPos = presence.AbsolutePosition;
                dis = Math.Abs(Util.GetDistanceTo(toRegionPos, fromRegionPos));

                // are they in range
                if (dis <= ts.range)
                {
                    // Are they in the required angle of view
                    if (ts.arc < Math.PI)
                    {
                        // not omni-directional. Can you see it ?
                        // vec forward_dir = llRot2Fwd(llGetRot())
                        // vec obj_dir = toRegionPos-fromRegionPos
                        // dot=dot(forward_dir,obj_dir)
                        // mag_fwd = mag(forward_dir)
                        // mag_obj = mag(obj_dir)
                        // ang = acos(dot /(mag_fwd*mag_obj))
                        double ang_obj = 0;
                        try
                        {
                            Vector3 diff = toRegionPos - fromRegionPos;
                            LSL_Types.Vector3 obj_dir = new LSL_Types.Vector3(diff.X, diff.Y, diff.Z);
                            double dot = LSL_Types.Vector3.Dot(forward_dir, obj_dir);
                            double mag_obj = LSL_Types.Vector3.Mag(obj_dir);
                            ang_obj = Math.Acos(dot / (mag_fwd * mag_obj));
                        }
                        catch
                        {
                        }
                        if (ang_obj <= ts.arc)
                        {
                            sensedEntities.Add(new SensedEntity(dis, presence.UUID));
                        }
                    }
                    else
                    {
                        sensedEntities.Add(new SensedEntity(dis, presence.UUID));
                    }
                }
            });

            // If this is an avatar sense by key try to get them directly
            // rather than getting a list to scan through
            if (ts.keyID != UUID.Zero)
            {
                IScenePresence sp;
                // Try direct lookup by UUID
                if (!ts.host.ParentEntity.Scene.TryGetScenePresence (ts.keyID, out sp))
                    return sensedEntities;
                senseEntity(sp);
            }
            else if (ts.name != null && ts.name != "")
            {
                IScenePresence sp;
                // Try lookup by name will return if/when found
                if (!ts.host.ParentEntity.Scene.TryGetAvatarByName (ts.name, out sp))
                    return sensedEntities;
                senseEntity(sp);
            }
            else
            {
                ts.host.ParentEntity.Scene.ForEachScenePresence (senseEntity);
            }
            return sensedEntities;
        }

        public OSD GetSerializationData (UUID itemID, UUID primID)
        {
            OSDMap data = new OSDMap();

            lock (SenseRepeatListLock)
            {
                foreach (SenseRepeatClass ts in SenseRepeaters)
                {
                    if (ts.itemID == itemID)
                    {
                        OSDMap map = new OSDMap();
                        map.Add ("Interval", ts.interval);
                        map.Add ("Name", ts.name);
                        map.Add ("ID", ts.keyID);
                        map.Add ("Type", ts.type);
                        map.Add ("Range", ts.range);
                        map.Add ("Arc", ts.arc);
                        data[itemID.ToString ()] = map;
                    }
                }
            }

            return data;
        }

        public void CreateFromData(UUID itemID, UUID objectID,
                                   OSD data)
        {
            ISceneChildEntity part =
                findPrimsScene(objectID).GetSceneObjectPart(
                    objectID);

            if (part == null)
                return;

            OSDMap save = (OSDMap)data;
            
            foreach(KeyValuePair<string, OSD> kvp in save)
            {
                OSDMap map = (OSDMap)kvp.Value;
                SenseRepeatClass ts = new SenseRepeatClass ();

                ts.objectID = objectID;
                ts.itemID = itemID;

                ts.interval = (long)map["Interval"].AsInteger();
                ts.name = map["Name"].AsString();
                ts.keyID = map["ID"].AsUUID();
                ts.type = map["Type"].AsInteger();
                ts.range = map["Range"].AsReal();
                ts.arc = map["Arc"].AsReal ();
                ts.host = part;

                ts.next = DateTime.Now.ToUniversalTime().AddSeconds(ts.interval);

                SenseRepeaters.Add(ts);
            }
        }

        public Scene findPrimsScene(UUID objectID)
        {
            foreach (Scene s in m_ScriptEngine.Worlds)
            {
                ISceneChildEntity part = s.GetSceneObjectPart (objectID);
                if (part != null)
                {
                    return s;
                }
            }
            return null;
        }

        public Scene findPrimsScene(uint localID)
        {
            foreach (Scene s in m_ScriptEngine.Worlds)
            {
                ISceneChildEntity part = s.GetSceneObjectPart (localID);
                if (part != null)
                {
                    return s;
                }
            }
            return null;
        }

        public string Name
        {
            get { return "SensorRepeat"; }
        }

        public void Dispose()
        {
        }
    }
}
