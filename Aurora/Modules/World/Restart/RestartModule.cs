/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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
using Nini.Config;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Timers;

namespace Aurora.Modules.Restart
{
    public class RestartModule : INonSharedRegionModule, IRestartModule
    {
        protected List<int> m_Alerts;
        protected Timer m_CountdownTimer;
        protected IDialogModule m_DialogModule;
        protected UUID m_Initiator;
        protected string m_Message;
        protected bool m_Notice;
        protected DateTime m_RestartBegin;
        protected IScene m_scene;

        #region INonSharedRegionModule Members

        public void Initialise(IConfigSource config)
        {
        }

        public void AddRegion(IScene scene)
        {
            m_scene = scene;
            scene.RegisterModuleInterface<IRestartModule>(this);
            if (MainConsole.Instance != null)
            {
                MainConsole.Instance.Commands.AddCommand(
                    "region restart",
                    "region restart <message> <time (in seconds)>",
                    "Restart the region", HandleRegionRestart);
                MainConsole.Instance.Commands.AddCommand(
                    "region restart abort",
                    "region restart abort [<message>]",
                    "Restart the region", HandleRegionRestart);
            }
        }

        public void RegionLoaded(IScene scene)
        {
            m_DialogModule = m_scene.RequestModuleInterface<IDialogModule>();
        }

        public void RemoveRegion(IScene scene)
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "RestartModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return typeof (IRestartModule); }
        }

        #endregion

        #region IRestartModule Members

        public TimeSpan TimeUntilRestart
        {
            get { return DateTime.Now - m_RestartBegin; }
        }

        public void ScheduleRestart(UUID initiator, string message, int[] alerts, bool notice)
        {
            if (alerts.Length == 0)
            {
                AbortRestart("Restart aborted");
                return;
            }

            if (m_CountdownTimer != null)
            {
                MainConsole.Instance.Warn("[Region]: Reseting the restart timer for new settings.");
                m_CountdownTimer.Stop();
                m_CountdownTimer = null;
            }

            if (alerts == null)
            {
                RestartScene();
                return;
            }

            m_Message = message;
            m_Initiator = initiator;
            m_Notice = notice;
            m_Alerts = new List<int>(alerts);
            m_Alerts.Sort();
            m_Alerts.Reverse();

            if (m_Alerts[0] == 0)
            {
                RestartScene();
                return;
            }

            int nextInterval = DoOneNotice();

            SetTimer(nextInterval);
        }

        public void AbortRestart(string message)
        {
            if (m_CountdownTimer != null)
            {
                m_CountdownTimer.Stop();
                m_CountdownTimer = null;
                if (m_DialogModule != null && message != String.Empty)
                    m_DialogModule.SendGeneralAlert(message);

                MainConsole.Instance.Warn("[Region]: Region restart aborted");
            }
        }

        /// <summary>
        ///     This causes the region to restart immediatley.
        /// </summary>
        public void RestartScene()
        {
            MainConsole.Instance.Error("[Scene]: Restaring Now");
            m_scene.RequestModuleInterface<ISceneManager>().RestartRegion();
        }

        #endregion

        public int DoOneNotice()
        {
            if (m_Alerts.Count == 0 || m_Alerts[0] == 0)
            {
                RestartScene();
                return 0;
            }

            int nextAlert = 0;
            while (m_Alerts.Count > 1)
            {
                if (m_Alerts[1] == m_Alerts[0])
                {
                    m_Alerts.RemoveAt(0);
                    continue;
                }
                nextAlert = m_Alerts[1];
                break;
            }

            int currentAlert = m_Alerts[0];

            m_Alerts.RemoveAt(0);

            int minutes = currentAlert/60;
            string currentAlertString = String.Empty;
            if (minutes > 0)
            {
                if (minutes == 1)
                    currentAlertString += "1 minute";
                else
                    currentAlertString += String.Format("{0} minutes", minutes);
                if ((currentAlert%60) != 0)
                    currentAlertString += " and ";
            }
            if ((currentAlert%60) != 0)
            {
                int seconds = currentAlert%60;
                if (seconds == 1)
                    currentAlertString += "1 second";
                else
                    currentAlertString += String.Format("{0} seconds", seconds);
            }

            string msg = String.Format(m_Message, currentAlertString);

            if (m_DialogModule != null && msg != String.Empty)
            {
                if (m_Notice)
                    m_DialogModule.SendGeneralAlert(msg);
                else
                    m_DialogModule.SendNotificationToUsersInRegion(m_Initiator, "System", msg);
                MainConsole.Instance.Warn("[Region]: Region will restart in " + currentAlertString);
            }

            return currentAlert - nextAlert;
        }

        public void SetTimer(int intervalSeconds)
        {
            if (intervalSeconds == 0)
                return;
            m_CountdownTimer = new Timer {AutoReset = false, Interval = intervalSeconds*1000};
            m_CountdownTimer.Elapsed += OnTimer;
            m_CountdownTimer.Start();
        }

        private void OnTimer(object source, ElapsedEventArgs e)
        {
            int nextInterval = DoOneNotice();

            SetTimer(nextInterval);
        }

        private void HandleRegionRestart(string[] args)
        {
            if (MainConsole.Instance.ConsoleScene != m_scene)
                return;

            if (args.Length < 4)
            {
                if (args.Length >= 3)
                {
                    if (args[2] == "abort")
                    {
                        string msg = "Restart aborted";
                        if (args.Length > 3)
                            msg = args[3];

                        if (m_Alerts != null)
                        {
                            AbortRestart(msg);
                        }
                        return;
                    }
                    int seconds = 0;
                    if (int.TryParse(args[2], out seconds))
                    {
                        List<int> times = new List<int>();
                        while (seconds > 0)
                        {
                            times.Add(seconds);
                            if (seconds > 300)
                                seconds -= 120;
                            else if (seconds > 30)
                                seconds -= 30;
                            else
                                seconds -= 15;
                        }
                        string msg = "Region will restart in {0}";
                        if (args.Length > 3)
                            msg = args[3];

                        ScheduleRestart(UUID.Zero, msg, times.ToArray(), true);
                        return;
                    }
                }
                MainConsole.Instance.Info("Error: restart region <mode> <name> <time> ...");
                return;
            }
        }
    }
}