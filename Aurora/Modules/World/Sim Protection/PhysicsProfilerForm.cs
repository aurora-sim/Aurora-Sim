using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using Timer = System.Timers.Timer;
using System.Text;
using System.Windows.Forms;
using Aurora.Framework;
using OpenMetaverse;
using OpenSim.Region.Framework.Scenes;

namespace Aurora.Modules
{
    public partial class PhysicsProfilerForm : Form
    {
        private Timer m_updateStats = new Timer();
        private List<Scene> m_scenes = new List<Scene>();
        private UUID SceneSelected = UUID.Zero;
        public PhysicsProfilerForm(List<Scene> scenes)
        {
            m_scenes = scenes;
            SceneSelected = scenes[0].RegionInfo.RegionID;
            InitializeComponent();
        }

        private void PhysicsProfilerForm_Load(object sender, EventArgs e)
        {
            foreach (Scene scene in m_scenes)
            {
                RegionNameSelector.Items.Add(scene.RegionInfo.RegionName);
            }
            RegionNameSelector.SelectedIndex = 0;
            m_updateStats = new Timer();
            m_updateStats.Interval = 10000;
            m_updateStats.Enabled = true;
            m_updateStats.Elapsed += new System.Timers.ElapsedEventHandler(m_updateStats_Elapsed);
            m_updateStats.Start();
            UpdateStatsBars();
        }

        void m_updateStats_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            UpdateStatsBars();
        }

        private void RegionNameSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (Scene scene in m_scenes)
            {
                if (scene.RegionInfo.RegionName == RegionNameSelector.SelectedItem.ToString())
                {
                    SceneSelected = scene.RegionInfo.RegionID;
                    break;
                }
            }
            UpdateStatsBars();
        }

        private void UpdateStatsBars()
        {
            Profiler p = ProfilerManager.GetProfiler();
            PhysicsTaintBox.Image = p.DrawGraph("StatPhysicsTaintTime " + SceneSelected, 200).Bitmap();
            PhysicsMoveTimeBox.Image = p.DrawGraph("StatPhysicsMoveTime " + SceneSelected, 200).Bitmap();
            CollisionOptimizedTimeBox.Image = p.DrawGraph("StatCollisionOptimizedTime " + SceneSelected, 200).Bitmap();
            SendCollisionsTimeBox.Image = p.DrawGraph("StatSendCollisionsTime " + SceneSelected, 200).Bitmap();
            AvatarUpdatePosAndVelocityBox.Image = p.DrawGraph("StatAvatarUpdatePosAndVelocity " + SceneSelected, 200).Bitmap();
            PrimUpdatePosAndVelocityBox.Image = p.DrawGraph("StatPrimUpdatePosAndVelocity " + SceneSelected, 200).Bitmap();
            UnlockedTimeBox.Image = p.DrawGraph("StatUnlockedArea " + SceneSelected, 200).Bitmap();
        }
    }
}
