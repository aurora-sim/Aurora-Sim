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
        public bool ShouldExit;
        public DateTime LastUpdate;
        public virtual void Start() { }
        public string type;
        public event ThreadClosing ThreadIsClosing;
        public void FireThreadClosing(IThread sh)
        {
            ThreadIsClosing(sh);
        }
    }

    public class AuroraThreadTracker
    {
        private static readonly ILog m_log
            = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        Timer checkTimer = new Timer();
        List<IThread> AllHeartbeats = new List<IThread>();
        public void Init()
        {
            checkTimer.Enabled = true;
            checkTimer.Interval = 1000;
            checkTimer.Elapsed += Check;
        }

        private void Check(object sender, ElapsedEventArgs e)
        {
            for(int i = 0; i < AllHeartbeats.Count; i++)
            {
                try
                {
                    CheckThread(AllHeartbeats[i]);
                }
                catch { }
            }
        }

        private void CheckThread(IThread hb)
        {
            if (hb.LastUpdate == new DateTime())
                return;
            TimeSpan ts = DateTime.UtcNow - hb.LastUpdate;
            if (ts.Seconds > 5)
            {
                AllHeartbeats.Remove(hb);
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
            }
        }

        public void AddSceneHeartbeat(IThread heartbeat, out System.Threading.Thread thread)
        {
            m_log.Warn("[SceneHeartbeatTracker]: " + heartbeat.type + " has been started");
            thread = new System.Threading.Thread(heartbeat.Start);
            thread.Name = "SceneHeartbeat";
            thread.Start();
            heartbeat.ThreadIsClosing += ThreadDieing;
            AllHeartbeats.Add(heartbeat);
        }

        public void ThreadDieing(IThread heartbeat)
        {
            m_log.Warn("[SceneHeartbeatTracker]: " + heartbeat.type + " has been found dead, attempting to revive...");
        }
    }
}
