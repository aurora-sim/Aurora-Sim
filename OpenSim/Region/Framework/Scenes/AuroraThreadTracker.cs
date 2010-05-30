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
        public delegate void ThreadClosing(IThread heartbeat);
        public Scene m_scene;
        public bool ShouldExit = false;
        public DateTime LastUpdate;
        public virtual void Start() { }
        public string type;
        public event ThreadClosing ThreadIsClosing;
        public void FireThreadClosing(IThread sh)
        {
            if (ThreadIsClosing != null)
                ThreadIsClosing(sh);
        }
    }

    public class AuroraThreadTracker
    {
        private static readonly ILog m_log
            = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        Timer checkTimer = null;
        List<IThread> AllHeartbeats = new List<IThread>();
        Scene m_scene = null;

        public void Init(Scene scene)
        {
            m_scene = scene;
            checkTimer = new Timer();
            checkTimer.Enabled = true;
            checkTimer.Interval = 1000;
            checkTimer.Elapsed += Check;
        }

        private void Check(object sender, ElapsedEventArgs e)
        {
            FixThreadCount();
            for (int i = 0; i < AllHeartbeats.Count; i++)
            {
                try
                {
                    CheckThread(AllHeartbeats[i]);
                }
                catch { }
            }
        }

        private void FixThreadCount()
        {
            if (AllHeartbeats.Count < 3)
            {
                //Make sure to kill off the right ones...
                m_log.Warn("[SceneHeartbeatTracker]: Fixing thread count... " + AllHeartbeats.Count + " found. ");
                bool foundPhysics = false;
                bool foundBackup = false;
                bool foundUpdate = false;
                //bool foundMain = false;
                for (int i = 0; i < AllHeartbeats.Count; i++)
                {
                    IThread hb = AllHeartbeats[i];
                    if (hb == null)
                        continue;
                    if (hb.type == "SceneBackupHeartbeat" && !foundBackup && !hb.ShouldExit)
                        foundBackup = true;
                    if (hb.type == "ScenePhysicsHeartbeat" && !foundPhysics && !hb.ShouldExit)
                        foundPhysics = true;
                    if (hb.type == "SceneUpdateHeartbeat" && !foundUpdate && !hb.ShouldExit)
                        foundUpdate = true;
                    //if (hb.type == "SceneHeartbeat" && !foundMain && !hb.ShouldExit)
                    //    foundMain = true;
                }
                m_log.Warn("[SceneHeartbeatTracker]: " + m_scene.RegionInfo.RegionName + " " + foundBackup + " " + foundPhysics + " " + foundUpdate + " ");
                System.Threading.Thread thread;
                if (!foundBackup)
                    AddSceneHeartbeat(new Scene.SceneBackupHeartbeat(m_scene), out thread);
                if (!foundPhysics)
                    AddSceneHeartbeat(new Scene.ScenePhysicsHeartbeat(m_scene), out thread);
                if (!foundUpdate)
                    AddSceneHeartbeat(new Scene.SceneUpdateHeartbeat(m_scene), out thread);
                //if (!foundMain)
                //    AddSceneHeartbeat(new Scene.SceneHeartbeat(m_scene), out thread);
            }
            if (AllHeartbeats.Count > 3)
            {
                //Make sure to kill off the right ones...
                m_log.Warn("[SceneHeartbeatTracker]: Fixing thread count... " + AllHeartbeats.Count + " found. ");
                bool foundPhysics = false;
                bool foundBackup = false;
                bool foundUpdate = false;
                bool foundMain = false;
                for (int i = 0; i < AllHeartbeats.Count; i++)
                {
                    IThread hb = AllHeartbeats[i];
                    if (hb == null)
                        continue;
                    if (hb.type == "SceneUpdateHeartbeat" && foundUpdate)
                    {
                        m_log.Warn("[SceneHeartbeatTracker]: Killing " + hb.type);
                        hb.ShouldExit = true;
                    }
                    if (hb.type == "SceneHeartbeat" && foundMain)
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
                    if (hb.type == "SceneHeartbeat" && !foundMain && !hb.ShouldExit)
                        foundMain = true;
                    if (hb.type == "ScenePhysicsHeartbeat" && !foundPhysics && !hb.ShouldExit)
                        foundPhysics = true;
                    if (hb.type == "SceneUpdateHeartbeat" && !foundUpdate && !hb.ShouldExit)
                        foundUpdate = true;
                }
            }
        }

        private void CheckThread(IThread hb)
        {
            if (hb.LastUpdate == new DateTime())
                return;
            TimeSpan ts = DateTime.UtcNow - hb.LastUpdate;
            if (ts.Seconds > 10)
            {
                System.Threading.Thread thread;
                hb.ShouldExit = true;
                m_log.Warn("[SceneHeartbeatTracker]: " + hb.type + " has been found dead, attempting to revive...");
                //Time to start a new one
                if (hb.type == "SceneBackupHeartbeat")
                {
                    Scene.SceneBackupHeartbeat sbhb = new Scene.SceneBackupHeartbeat(hb.m_scene);
                    AddSceneHeartbeat(sbhb, out thread);
                }
                if (hb.type == "ScenePhysicsHeartbeat")
                {
                    Scene.ScenePhysicsHeartbeat shb = new Scene.ScenePhysicsHeartbeat(hb.m_scene);
                    AddSceneHeartbeat(shb, out thread);
                }
                if (hb.type == "SceneUpdateHeartbeat")
                {
                    Scene.SceneUpdateHeartbeat suhb = new Scene.SceneUpdateHeartbeat(hb.m_scene);
                    AddSceneHeartbeat(suhb, out thread);
                }
                if (hb.type == "SceneHeartbeat")
                {
                    AddSceneHeartbeat(new Scene.SceneHeartbeat(m_scene), out thread);
                }
            }
        }

        public void AddSceneHeartbeat(IThread heartbeat, out System.Threading.Thread thread)
        {
            //m_log.Warn("[SceneHeartbeatTracker]: " + heartbeat.type + " has been started");
            thread = new System.Threading.Thread(heartbeat.Start);
            thread.Name = "SceneHeartbeat";
            thread.Priority = System.Threading.ThreadPriority.Normal;
            thread.IsBackground = false;
            thread.Start();
            heartbeat.ThreadIsClosing += ThreadDieing;
            AllHeartbeats.Add(heartbeat);
        }

        public void ThreadDieing(IThread heartbeat)
        {
            m_log.Warn("[SceneHeartbeatTracker]: " + heartbeat.type + " has been found dead for " + m_scene.RegionInfo.RegionName + ".");
            if(AllHeartbeats.Contains(heartbeat))
                AllHeartbeats.Remove(heartbeat);
        }
    }
}
