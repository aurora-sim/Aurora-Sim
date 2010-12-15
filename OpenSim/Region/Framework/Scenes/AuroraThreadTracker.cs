using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Timers;
using System.Threading;
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
        public virtual void Restart()
        {
        }
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

        //Timer checkTimer = null;
        List<IThread> AllHeartbeats = new List<IThread>();
        Scene m_scene = null;
        public delegate void NeedToAddThread(string type);
        public event NeedToAddThread OnNeedToAddThread;
        private bool Alive = true;

        public void Init(Scene scene)
        {
            m_scene = scene;
            //checkTimer = new Timer();
            //checkTimer.Enabled = true;
            //checkTimer.Interval = 1000;
            //checkTimer.Elapsed += Check;
        }

        public void Close()
        {
            Alive = false;
            //checkTimer.Stop();
            //checkTimer.Dispose();
            //checkTimer = null;
            foreach (IThread thread in AllHeartbeats)
            {
                thread.ThreadIsClosing -= ThreadDieing;
                thread.ShouldExit = true;
            }
            AllHeartbeats.Clear();
        }

        private void Check(object sender, ElapsedEventArgs e)
        {
            if (!Alive)
                return;
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
            if (AllHeartbeats.Count < 5)
            {
                //Make sure to kill off the right ones...
                m_log.Warn("[SceneHeartbeatTracker]: Fixing thread count... " + AllHeartbeats.Count + " found. ");
                bool foundPhysics = false;
                bool foundBackup = false;
                bool foundUpdate = false;
                bool foundOutgoing = false;
                bool foundIncoming = false;
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
                    if (hb.type == "OutgoingPacketHandler" && !foundOutgoing && !hb.ShouldExit)
                        foundOutgoing = true;
                    if (hb.type == "IncomingPacketHandler" && !foundIncoming && !hb.ShouldExit)
                        foundIncoming = true;
                    //if (hb.type == "SceneHeartbeat" && !foundMain && !hb.ShouldExit)
                    //    foundMain = true;
                }
                m_log.Warn("[SceneHeartbeatTracker]: " + m_scene.RegionInfo.RegionName + " " + foundBackup + " " + foundPhysics + " " + foundUpdate + " ");
                if (!foundBackup)
                {
                    if (OnNeedToAddThread != null)
                    {
                        foreach (NeedToAddThread dg in OnNeedToAddThread.GetInvocationList())
                        {
                            dg("SceneBackupHeartbeat");
                        }
                    }
                }
                if (!foundPhysics)
                {
                    if (OnNeedToAddThread != null)
                    {
                        foreach (NeedToAddThread dg in OnNeedToAddThread.GetInvocationList())
                        {
                            dg("ScenePhysicsHeartbeat");
                        }
                    }
                }
                if (!foundUpdate)
                {
                    if (OnNeedToAddThread != null)
                    {
                        foreach (NeedToAddThread dg in OnNeedToAddThread.GetInvocationList())
                        {
                            dg("SceneUpdateHeartbeat");
                        }
                    }
                }
                if (!foundIncoming)
                {
                    if (OnNeedToAddThread != null)
                    {
                        foreach (NeedToAddThread dg in OnNeedToAddThread.GetInvocationList())
                        {
                            dg("IncomingPacketHandler");
                        }
                    }
                }
                if (!foundOutgoing)
                {
                    if (OnNeedToAddThread != null)
                    {
                        foreach (NeedToAddThread dg in OnNeedToAddThread.GetInvocationList())
                        {
                            dg("OutgoingPacketHandler");
                        }
                    }
                }
                /*if (!foundMain)
                {
                    if (OnNeedToAddThread != null)
                    {
                        foreach (NeedToAddThread dg in OnNeedToAddThread.GetInvocationList())
                        {
                            dg("SceneHeartbeat");
                        }
                    }
                }*/
            }
            if (AllHeartbeats.Count > 5)
            {
                //Make sure to kill off the right ones...
                m_log.Warn("[SceneHeartbeatTracker]: Fixing thread count... " + AllHeartbeats.Count + " found. ");
                bool foundPhysics = false;
                bool foundBackup = false;
                bool foundUpdate = false;
                bool foundMain = false;
                bool foundOutgoing = false;
                bool foundIncoming = false;
                for (int i = 0; i < AllHeartbeats.Count; i++)
                {
                    IThread hb = AllHeartbeats[i];
                    if (hb == null)
                        continue;
                    if (hb.type == "SceneUpdateHeartbeat" && foundUpdate)
                    {
                        m_log.Warn("[SceneHeartbeatTracker]: Killing " + hb.type);
                        hb.ShouldExit = true;
                        AllHeartbeats.Remove(hb);
                    }
                    if (hb.type == "SceneHeartbeat" && foundMain)
                    {
                        m_log.Warn("[SceneHeartbeatTracker]: Killing " + hb.type);
                        hb.ShouldExit = true;
                        AllHeartbeats.Remove(hb);
                    }
                    if (hb.type == "SceneBackupHeartbeat" && foundBackup)
                    {
                        m_log.Warn("[SceneHeartbeatTracker]: Killing " + hb.type);
                        hb.ShouldExit = true;
                        AllHeartbeats.Remove(hb);
                    }
                    if (hb.type == "ScenePhysicsHeartbeat" && foundPhysics)
                    {
                        m_log.Warn("[SceneHeartbeatTracker]: Killing " + hb.type);
                        hb.ShouldExit = true;
                        AllHeartbeats.Remove(hb);
                    }

                    if (hb.type == "OutgoingPacketHandler" && foundOutgoing)
                    {
                        m_log.Warn("[SceneHeartbeatTracker]: Killing " + hb.type);
                        hb.ShouldExit = true;
                        AllHeartbeats.Remove(hb);
                    }
                    if (hb.type == "IncomingPacketHandler" && foundIncoming)
                    {
                        m_log.Warn("[SceneHeartbeatTracker]: Killing " + hb.type);
                        hb.ShouldExit = true;
                        AllHeartbeats.Remove(hb);
                    }
                    if (hb.type == "SceneBackupHeartbeat" && !foundBackup && !hb.ShouldExit)
                        foundBackup = true;
                    if (hb.type == "SceneHeartbeat" && !foundMain && !hb.ShouldExit)
                        foundMain = true;
                    if (hb.type == "ScenePhysicsHeartbeat" && !foundPhysics && !hb.ShouldExit)
                        foundPhysics = true;
                    if (hb.type == "SceneUpdateHeartbeat" && !foundUpdate && !hb.ShouldExit)
                        foundUpdate = true;
                    if (hb.type == "OutgoingPacketHandler" && !foundOutgoing && !hb.ShouldExit)
                        foundOutgoing = true;
                    if (hb.type == "IncomingPacketHandler" && !foundIncoming && !hb.ShouldExit)
                        foundIncoming = true;
                }
            }
        }

        private void CheckThread(IThread hb)
        {
            if (hb.LastUpdate == new DateTime())
                return;
            TimeSpan ts = DateTime.UtcNow - hb.LastUpdate;
            if (ts.Seconds > 5)
            {
                m_log.Warn("[SceneHeartbeatTracker]: " + hb.type + " has been found dead, attempting to revive...");
                //Time to start a new one
                AllHeartbeats.Remove(hb);
                hb.Restart();
                
            }
        }

        public void AddSceneHeartbeat(IThread heartbeat)
        {
            //m_log.Warn("[SceneHeartbeatTracker]: " + heartbeat.type + " has been started");
            Thread thread = new System.Threading.Thread(heartbeat.Start);
            thread.Name = "SceneHeartbeat";
            thread.Priority = System.Threading.ThreadPriority.Normal;
            thread.IsBackground = true;
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
