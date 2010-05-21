using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Timers;
using log4net;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.Framework.Scenes
{
    public class IThread
    {
        public delegate void ThreadClosing(Scene scene, IThread heartbeat);
        public Scene m_scene;
        public bool ShouldExit = false;
        public DateTime LastUpdate;
        public virtual void Start() { }
        public string type;
        public event ThreadClosing ThreadIsClosing;
        public void FireThreadClosing(Scene scene, IThread sh)
        {
            if(ThreadIsClosing != null)
                ThreadIsClosing(scene, sh);
        }
    }

    public class AuroraThreadTracker
    {
        private static readonly ILog m_log
            = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        Timer checkTimer = null;
        Dictionary<Scene, List<IThread>> AllHeartbeats = new Dictionary<Scene, List<IThread>>();

        public void Init(Scene scene)
        {
            if (AllHeartbeats.ContainsKey(scene))
            {
                List<IThread> threads = null;
                AllHeartbeats.TryGetValue(scene, out threads);
                foreach (IThread thread in threads)
                {
                    thread.ShouldExit = true;
                }
                AllHeartbeats.Remove(scene);
            }
            AllHeartbeats.Add(scene, new List<IThread>());
            if (checkTimer != null)
            {
                checkTimer.Stop();
                checkTimer.Close();
                checkTimer.Dispose();
            }
            checkTimer = new Timer();
            checkTimer.Enabled = true;
            checkTimer.Interval = 1000;
            checkTimer.Elapsed += Check;
        }

        private void Check(object sender, ElapsedEventArgs e)
        {
            FixThreadCount();
            foreach (KeyValuePair<Scene, List<IThread>> kvp in AllHeartbeats)
            {
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    try
                    {
                        CheckThread(kvp.Key, kvp.Value[i]);
                    }
                    catch { }
                }
            }
        }

        private void FixThreadCount()
        {
            foreach (KeyValuePair<Scene,List<IThread>> kvp in AllHeartbeats)
            {

                if (AllHeartbeats.Count < 3)
                {
                    //Make sure to kill off the right ones...
                    m_log.Warn("[SceneHeartbeatTracker]: Fixing thread count... " + AllHeartbeats.Count + " found. ");
                    bool foundPhysics = false;
                    bool foundBackup = false;
                    bool foundUpdate = false;
                    for (int i = 0; i < AllHeartbeats.Count; i++)
                    {
                        IThread hb = kvp.Value[i];
                        if (hb == null)
                            continue;
                        if (hb.type == "SceneBackupHeartbeat" && !foundBackup && !hb.ShouldExit)
                            foundBackup = true;
                        if (hb.type == "ScenePhysicsHeartbeat" && !foundPhysics && !hb.ShouldExit)
                            foundPhysics = true;
                        if (hb.type == "SceneUpdateHeartbeat" && !foundUpdate && !hb.ShouldExit)
                            foundUpdate = true;
                    }
                    m_log.Warn("[SceneHeartbeatTracker]: " + kvp.Key.RegionInfo.RegionName + " " + foundBackup + " " + foundPhysics + " " + foundUpdate + " ");
                    System.Threading.Thread thread;
                    if (!foundBackup)
                        AddSceneHeartbeat(kvp.Key, new Scene.SceneBackupHeartbeat(kvp.Key), out thread);
                    if (!foundPhysics)
                        AddSceneHeartbeat(kvp.Key, new Scene.ScenePhysicsHeartbeat(kvp.Key), out thread);
                    if (!foundUpdate)
                        AddSceneHeartbeat(kvp.Key, new Scene.SceneUpdateHeartbeat(kvp.Key), out thread);
                }
                if (AllHeartbeats.Count > 3)
                {
                    //Make sure to kill off the right ones...
                    //m_log.Warn("[SceneHeartbeatTracker]: Fixing thread count... " + AllHeartbeats.Count + " found. ");
                    bool foundPhysics = false;
                    bool foundBackup = false;
                    bool foundUpdate = false;
                    for (int i = 0; i < AllHeartbeats.Count; i++)
                    {
                        IThread hb = kvp.Value[i];
                        if (hb == null)
                            continue;
                        if (hb.type == "SceneUpdateHeartbeat" && foundUpdate)
                        {
                            m_log.Warn("[SceneHeartbeatTracker]: Killing " + hb.type);
                            hb.ShouldExit = true;
                        }
                        if (hb.type == "SceneBackupHeartbeat" && foundBackup)
                        {
                            m_log.Warn("[SceneHeartbeatTracker]: Killing " + hb.type);
                            hb.ShouldExit = true;
                        }
                        if (hb.type == "ScenePhysicsHeartbeat" && foundPhysics)
                        {
                            m_log.Warn("[SceneHeartbeatTracker]: Killing " + hb.type);
                            hb.ShouldExit = true;
                        }
                        if (hb.type == "SceneBackupHeartbeat" && !foundBackup && !hb.ShouldExit)
                            foundBackup = true;
                        if (hb.type == "ScenePhysicsHeartbeat" && !foundPhysics && !hb.ShouldExit)
                            foundPhysics = true;
                        if (hb.type == "SceneUpdateHeartbeat" && !foundUpdate && !hb.ShouldExit)
                            foundUpdate = true;
                    }
                }
            }
        }

        private void CheckThread(Scene scene, IThread hb)
        {
            if (hb.LastUpdate == new DateTime())
                return;
            TimeSpan ts = DateTime.UtcNow - hb.LastUpdate;
            if (ts.Seconds > 10)
            {
                List<IThread> threads = null;
                AllHeartbeats.TryGetValue(scene, out threads);
                threads.Remove(hb);
                System.Threading.Thread thread;
                hb.ShouldExit = true;
                m_log.Warn("[SceneHeartbeatTracker]: " + hb.type + " has been found dead, attempting to revive...");
                //Time to start a new one
                if (hb.type == "SceneBackupHeartbeat")
                {
                    Scene.SceneBackupHeartbeat sbhb = new Scene.SceneBackupHeartbeat(hb.m_scene);
                    AddSceneHeartbeat(scene, sbhb, out thread);
                }
                if (hb.type == "ScenePhysicsHeartbeat")
                {
                    Scene.ScenePhysicsHeartbeat shb = new Scene.ScenePhysicsHeartbeat(hb.m_scene);
                    AddSceneHeartbeat(scene, shb, out thread);
                }
                if (hb.type == "SceneUpdateHeartbeat")
                {
                    Scene.SceneUpdateHeartbeat suhb = new Scene.SceneUpdateHeartbeat(hb.m_scene);
                    AddSceneHeartbeat(scene, suhb, out thread);
                }
            }
        }

        public void AddSceneHeartbeat(Scene scene, IThread heartbeat, out System.Threading.Thread thread)
        {
            //m_log.Warn("[SceneHeartbeatTracker]: " + heartbeat.type + " has been started");
            thread = new System.Threading.Thread(heartbeat.Start);
            thread.Name = "SceneHeartbeat";
            thread.Start();
            heartbeat.ThreadIsClosing += ThreadDieing;
            List<IThread> threads = null;
            if (AllHeartbeats.ContainsKey(scene))
            {
                AllHeartbeats.TryGetValue(scene, out threads);
                AllHeartbeats.Remove(scene);
            }
            if (!threads.Contains(heartbeat))
                threads.Add(heartbeat);
            AllHeartbeats.Add(scene, threads);
        }

        public void ThreadDieing(Scene scene, IThread heartbeat)
        {
            m_log.Warn("[SceneHeartbeatTracker]: " + heartbeat.type + " has been found dead.");
            List<IThread> threads = null;
            if (AllHeartbeats.ContainsKey(scene))
            {
                AllHeartbeats.TryGetValue(scene, out threads);
                AllHeartbeats.Remove(scene);
            }
            if(threads.Contains(heartbeat))
                threads.Remove(heartbeat);
            AllHeartbeats.Add(scene, threads);
        }
    }
}
