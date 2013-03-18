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
using Aurora.Modules.SimProtection;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace Aurora.Modules
{
    public partial class PhysicsProfilerForm : Form
    {
        private readonly PhysicsMonitor m_monitor;
        private readonly List<IScene> m_scenes = new List<IScene>();
        private readonly object m_statsLock = new object();
        private int MaxVal = 200;
        private UUID SceneSelected = UUID.Zero;
        private int TimeToUpdate = 500;
        private Timer m_updateStats = new Timer();
        private bool m_useInstantUpdating;

        public PhysicsProfilerForm(PhysicsMonitor monitor, List<IScene> scenes)
        {
            m_monitor = monitor;
            m_scenes = scenes;
            SceneSelected = scenes[0].RegionInfo.RegionID;
            InitializeComponent();
        }

        private void PhysicsProfilerForm_Load(object sender, EventArgs e)
        {
            foreach (IScene scene in m_scenes)
            {
                RegionNameSelector.Items.Add(scene.RegionInfo.RegionName);
            }
            RegionNameSelector.SelectedIndex = 0;
            m_updateStats = new Timer {Interval = 10000, Enabled = true};
            m_updateStats.Elapsed += m_updateStats_Elapsed;
            m_updateStats.Start();

            InstantUpdatesSet.Hide();
            TimeBetweenUpdates.Hide();
            IULabel.Hide();

            UpdateStatsBars();
        }

        private void m_updateStats_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (m_statsLock)
                m_updateStats.Stop();
            UpdateStatsBars();
            lock (m_statsLock)
                m_updateStats.Start();
        }

        private void RegionNameSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
#if (!ISWIN)
            foreach (IScene scene in m_scenes)
            {
                if (scene.RegionInfo.RegionName == RegionNameSelector.SelectedItem.ToString())
                {
                    SceneSelected = scene.RegionInfo.RegionID;
                    break;
                }
            }
#else
            foreach (IScene scene in m_scenes.Where(scene => scene.RegionInfo.RegionName == RegionNameSelector.SelectedItem.ToString()))
            {
                SceneSelected = scene.RegionInfo.RegionID;
                break;
            }
#endif
            UpdateStatsBars();
        }

        private void UpdateStatsBars()
        {
            Profiler p = ProfilerManager.GetProfiler();
            if (m_useInstantUpdating)
            {
                PhysicsTaintBox.Image = p.DrawGraph("CurrentStatPhysicsTaintTime " + SceneSelected, MaxVal).Bitmap();
                PhysicsMoveTimeBox.Image = p.DrawGraph("CurrentStatPhysicsMoveTime " + SceneSelected, MaxVal).Bitmap();
                CollisionOptimizedTimeBox.Image =
                    p.DrawGraph("CurrentStatCollisionOptimizedTime " + SceneSelected, MaxVal).Bitmap();
                SendCollisionsTimeBox.Image =
                    p.DrawGraph("CurrentStatSendCollisionsTime " + SceneSelected, MaxVal).Bitmap();
                AvatarUpdatePosAndVelocityBox.Image =
                    p.DrawGraph("CurrentStatAvatarUpdatePosAndVelocity " + SceneSelected, MaxVal).Bitmap();
                PrimUpdatePosAndVelocityBox.Image =
                    p.DrawGraph("CurrentStatPrimUpdatePosAndVelocity " + SceneSelected, MaxVal).Bitmap();
                UnlockedTimeBox.Image = p.DrawGraph("CurrentStatUnlockedArea " + SceneSelected, MaxVal).Bitmap();
                FindContactsTimeBox.Image = p.DrawGraph("CurrentStatFindContactsTime " + SceneSelected, MaxVal).Bitmap();
                ContactLoopTimeBox.Image = p.DrawGraph("CurrentStatContactLoopTime " + SceneSelected, MaxVal).Bitmap();
                CollisionAccountingTimeBox.Image =
                    p.DrawGraph("CurrentStatCollisionAccountingTime " + SceneSelected, MaxVal).Bitmap();
            }
            else
            {
                PhysicsTaintBox.Image = p.DrawGraph("StatPhysicsTaintTime " + SceneSelected, MaxVal).Bitmap();
                PhysicsMoveTimeBox.Image = p.DrawGraph("StatPhysicsMoveTime " + SceneSelected, MaxVal).Bitmap();
                CollisionOptimizedTimeBox.Image =
                    p.DrawGraph("StatCollisionOptimizedTime " + SceneSelected, MaxVal).Bitmap();
                SendCollisionsTimeBox.Image = p.DrawGraph("StatSendCollisionsTime " + SceneSelected, MaxVal).Bitmap();
                AvatarUpdatePosAndVelocityBox.Image =
                    p.DrawGraph("StatAvatarUpdatePosAndVelocity " + SceneSelected, MaxVal).Bitmap();
                PrimUpdatePosAndVelocityBox.Image =
                    p.DrawGraph("StatPrimUpdatePosAndVelocity " + SceneSelected, MaxVal).Bitmap();
                UnlockedTimeBox.Image = p.DrawGraph("StatUnlockedArea " + SceneSelected, MaxVal).Bitmap();
                FindContactsTimeBox.Image = p.DrawGraph("StatFindContactsTime " + SceneSelected, MaxVal).Bitmap();
                ContactLoopTimeBox.Image = p.DrawGraph("StatContactLoopTime " + SceneSelected, MaxVal).Bitmap();
                CollisionAccountingTimeBox.Image =
                    p.DrawGraph("StatCollisionAccountingTime " + SceneSelected, MaxVal).Bitmap();
            }
        }

        private void Change_Click(object sender, EventArgs e)
        {
            if (int.TryParse(MaxValBox.Text, out MaxVal))
            {
                Max1.Text = MaxVal.ToString();
                Max2.Text = MaxVal.ToString();
                Max3.Text = MaxVal.ToString();
                Max4.Text = MaxVal.ToString();
                Max5.Text = MaxVal.ToString();
                Max6.Text = MaxVal.ToString();
                Max7.Text = MaxVal.ToString();

                HMax1.Text = (MaxVal/2).ToString();
                HMax2.Text = (MaxVal/2).ToString();
                HMax3.Text = (MaxVal/2).ToString();
                HMax4.Text = (MaxVal/2).ToString();
                HMax5.Text = (MaxVal/2).ToString();
                HMax6.Text = (MaxVal/2).ToString();
                HMax7.Text = (MaxVal/2).ToString();

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
            m_monitor.m_collectingStats = false; //Turn it off!
        }
    }
}