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

using System;
using System.Collections.Generic;
using System.Timers;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Interfaces;

namespace OpenSim.Services
{
    public class Scheduler : IScheduleService, IService
    {
        public AuroraEventManager EventManager = new AuroraEventManager();
        private ISchedulerDataPlugin m_database;
        private bool m_enabled = false;
        
        private readonly Timer scheduleTimer = new Timer();

        #region Implementation of IService

        /// <summary>
        ///   Set up and register the module
        /// </summary>
        /// <param name = "config">Config file</param>
        /// <param name = "registry">Place to register the modules into</param>
        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
        }

        /// <summary>
        ///   Load other IService modules now that this is set up
        /// </summary>
        /// <param name = "config">Config file</param>
        /// <param name = "registry">Place to register and retrieve module interfaces</param>
        public void Start(IConfigSource config, IRegistryCore registry)
        {
            
        }

        /// <summary>
        ///   All modules have started up and it is ready to run
        /// </summary>
        public void FinishedStartup()
        {
            m_database = DataManager.RequestPlugin<ISchedulerDataPlugin>();
            if (m_database != null) 
                m_enabled = true;

            if (m_enabled)
            {
                // don't want to start to soon
                scheduleTimer.Interval = 60000;
                scheduleTimer.Elapsed += t_Elapsed;
                scheduleTimer.Start();
            }
        }

        #endregion

        #region Implementation of IScheduleService

        public bool Register(SchedulerItem I, OnGenericEventHandler handler)
        {
            EventManager.RegisterEventHandler(I.FireFunction, handler);
            return true;
        }

        public bool Register(string fName, OnGenericEventHandler handler)
        {
            EventManager.RegisterEventHandler(fName, handler);
            return true;
        }

        public string Save(SchedulerItem I)
        {
            return m_database.SchedulerSave(I);
        }

        public void Remove(string id)
        {
            m_database.SchedulerRemove(id);
        }

        #endregion

        #region Timer
        private void t_Elapsed(object sender, ElapsedEventArgs e)
        {
            scheduleTimer.Enabled = false;
            try
            {
                List<SchedulerItem> CurrentSchedule = m_database.ToRun();
                foreach (SchedulerItem I in CurrentSchedule)
                {
                    FireEvent(I);
                }
            }
            catch (Exception ee)
            {
                MainConsole.Instance.Error("[Scheduler] t_Elapsed Error ", ee);
            }
            finally
            {
                scheduleTimer.Enabled = true;
            }
        }

        private void FireEvent(SchedulerItem I)
        {
            try
            {
                I = m_database.SaveHistory(I);
                List<Object> reciept = EventManager.FireGenericEventHandler(I.FireFunction, I.FireParams);
                if (!I.HistoryReciept)
                    I = m_database.SaveHistoryComplete(I);
                else
                {
                    foreach (Object o in reciept)
                    {
                        string results = (string)o;
                        if (results != "")
                        {
                            m_database.SaveHistoryCompleteReciept(I.HistoryLastID, results);
                        }
                    }
                }
            
                if (I.RunOnce) I.Enabled = false;
                if (I.Enabled) I.TimeToRun = I.TimeToRun.AddSeconds(I.RunEvery);

                if (!I.HisotryKeep)
                    m_database.HistoryDeleteOld(I);

                m_database.SchedulerSave(I);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[Scheduler] FireEvent Error " + I.id, e);
            }
        }

        public void MarkComplete(string history_id, string reciept)
        {
            m_database.SaveHistoryCompleteReciept(history_id, reciept);
        }

        #endregion
    }

    
}
