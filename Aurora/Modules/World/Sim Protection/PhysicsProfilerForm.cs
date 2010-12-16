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
        private int MaxVal = 200;
        private bool m_useInstantUpdating = false;
        private int TimeToUpdate = 500;

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

            InstantUpdatesSet.Hide();
            TimeBetweenUpdates.Hide();
            IULabel.Hide();

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
            if (m_useInstantUpdating)
            {
                PhysicsTaintBox.Image = p.DrawGraph("CurrentStatPhysicsTaintTime " + SceneSelected, MaxVal).Bitmap();
                PhysicsMoveTimeBox.Image = p.DrawGraph("CurrentStatPhysicsMoveTime " + SceneSelected, MaxVal).Bitmap();
                CollisionOptimizedTimeBox.Image = p.DrawGraph("CurrentStatCollisionOptimizedTime " + SceneSelected, MaxVal).Bitmap();
                SendCollisionsTimeBox.Image = p.DrawGraph("CurrentStatSendCollisionsTime " + SceneSelected, MaxVal).Bitmap();
                AvatarUpdatePosAndVelocityBox.Image = p.DrawGraph("CurrentStatAvatarUpdatePosAndVelocity " + SceneSelected, MaxVal).Bitmap();
                PrimUpdatePosAndVelocityBox.Image = p.DrawGraph("CurrentStatPrimUpdatePosAndVelocity " + SceneSelected, MaxVal).Bitmap();
                UnlockedTimeBox.Image = p.DrawGraph("CurrentStatUnlockedArea " + SceneSelected, MaxVal).Bitmap();
                FindContactsTimeBox.Image = p.DrawGraph("CurrentStatFindContactsTime " + SceneSelected, MaxVal).Bitmap();
                ContactLoopTimeBox.Image = p.DrawGraph("CurrentStatContactLoopTime " + SceneSelected, MaxVal).Bitmap();
                CollisionAccountingTimeBox.Image = p.DrawGraph("CurrentStatCollisionAccountingTime " + SceneSelected, MaxVal).Bitmap();
            }
            else
            {
                PhysicsTaintBox.Image = p.DrawGraph("StatPhysicsTaintTime " + SceneSelected, MaxVal).Bitmap();
                PhysicsMoveTimeBox.Image = p.DrawGraph("StatPhysicsMoveTime " + SceneSelected, MaxVal).Bitmap();
                CollisionOptimizedTimeBox.Image = p.DrawGraph("StatCollisionOptimizedTime " + SceneSelected, MaxVal).Bitmap();
                SendCollisionsTimeBox.Image = p.DrawGraph("StatSendCollisionsTime " + SceneSelected, MaxVal).Bitmap();
                AvatarUpdatePosAndVelocityBox.Image = p.DrawGraph("StatAvatarUpdatePosAndVelocity " + SceneSelected, MaxVal).Bitmap();
                PrimUpdatePosAndVelocityBox.Image = p.DrawGraph("StatPrimUpdatePosAndVelocity " + SceneSelected, MaxVal).Bitmap();
                UnlockedTimeBox.Image = p.DrawGraph("StatUnlockedArea " + SceneSelected, MaxVal).Bitmap();
                FindContactsTimeBox.Image = p.DrawGraph("StatFindContactsTime " + SceneSelected, MaxVal).Bitmap();
                ContactLoopTimeBox.Image = p.DrawGraph("StatContactLoopTime " + SceneSelected, MaxVal).Bitmap();
                CollisionAccountingTimeBox.Image = p.DrawGraph("StatCollisionAccountingTime " + SceneSelected, MaxVal).Bitmap();
            }
        }

        private void Change_Click(object sender, EventArgs e)
        {
            if(int.TryParse(MaxValBox.Text, out MaxVal))
            {
                Max1.Text = MaxVal.ToString();
                Max2.Text = MaxVal.ToString();
                Max3.Text = MaxVal.ToString();
                Max4.Text = MaxVal.ToString();
                Max5.Text = MaxVal.ToString();
                Max6.Text = MaxVal.ToString();
                Max7.Text = MaxVal.ToString();

                HMax1.Text = (MaxVal / 2).ToString();
                HMax2.Text = (MaxVal / 2).ToString();
                HMax3.Text = (MaxVal / 2).ToString();
                HMax4.Text = (MaxVal / 2).ToString();
                HMax5.Text = (MaxVal / 2).ToString();
                HMax6.Text = (MaxVal / 2).ToString();
                HMax7.Text = (MaxVal / 2).ToString();

                UpdateStatsBars();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            m_useInstantUpdating = !m_useInstantUpdating;
            if (m_useInstantUpdating)
            {
                InstantUpdatesSet.Show();
                TimeBetweenUpdates.Show();
                IULabel.Show();

                button1.Text = "Switch to Average Updating";
                m_updateStats.Interval = TimeToUpdate;
            }
            else
            {
                InstantUpdatesSet.Hide();
                TimeBetweenUpdates.Hide();
                IULabel.Hide();

                m_updateStats.Interval = 10000;
                button1.Text = "Switch to Instant Updating";
            }

            UpdateStatsBars();
        }

        private void InstantUpdatesSet_Click(object sender, EventArgs e)
        {
            if (int.TryParse(TimeBetweenUpdates.Text, out TimeToUpdate))
            {
                m_updateStats.Interval = TimeToUpdate;
            }
        }

        private void PhysicsProfilerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_updateStats.Stop();
        }
    }
}
