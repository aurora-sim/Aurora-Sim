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
using Aurora.Framework;

namespace Aurora.Modules.Monitoring.Monitors
{
    public class LoginMonitor : IMonitor, ILoginMonitor
    {
        #region Declares

        private DateTime StartTime = DateTime.Now;
        private long abnormalClientThreadTerminations;
        private int logoutsToday;
        private int logoutsTotal;
        private int logoutsYesterday;
        private int successfulLoginsToday;
        private int successfulLoginsTotal;
        private int successfulLoginsYesterday;

        /// <summary>
        ///     Number of times that a client thread terminated because of an exception
        /// </summary>
        public long AbnormalClientThreadTerminations
        {
            get { return abnormalClientThreadTerminations; }
        }

        public int SuccessfulLoginsTotal
        {
            get { return successfulLoginsTotal; }
        }

        public int SuccessfulLoginsToday
        {
            get
            {
                if (DateTime.Now.AddDays(1) < StartTime)
                {
                    StartTime = DateTime.Now;
                    successfulLoginsYesterday = successfulLoginsToday;
                    successfulLoginsToday = 0;
                }
                return successfulLoginsToday;
            }
        }

        public int SuccessfulLoginsYesterday
        {
            get { return successfulLoginsYesterday; }
        }

        public int LogoutsTotal
        {
            get { return logoutsTotal; }
        }

        public int LogoutsToday
        {
            get
            {
                if (DateTime.Now.AddDays(1) < StartTime)
                {
                    StartTime = DateTime.Now;
                    logoutsYesterday = logoutsToday;
                    logoutsToday = 0;
                }
                return logoutsToday;
            }
        }

        public int LogoutsYesterday
        {
            get { return logoutsYesterday; }
        }

        #endregion

        #region Implementation of IMonitor

        #region ILoginMonitor Members

        public void AddAbnormalClientThreadTermination()
        {
            abnormalClientThreadTerminations++;
        }

        public void AddSuccessfulLogin()
        {
            successfulLoginsTotal++;
            successfulLoginsToday++;
        }

        public void AddLogout()
        {
            logoutsTotal++;
            logoutsToday++;
        }

        #endregion

        #region IMonitor Members

        public double GetValue()
        {
            return 0;
        }

        public string GetName()
        {
            return "LoginMonitor";
        }

        public string GetFriendlyValue()
        {
            string Value = "";
            Value += "CONNECTION STATISTICS" + "\n";
            Value +=
                string.Format(
                    @"Successful logins Total: {0}
Successful logins Today: {1}
Successful logins Yesterday: {2}
Logouts Total: {3}
Logouts Today: {4}
Logouts Yesterday: {5} 
Abnormal client thread terminations: {6}",
                    SuccessfulLoginsTotal,
                    SuccessfulLoginsToday,
                    SuccessfulLoginsYesterday,
                    LogoutsTotal,
                    LogoutsToday,
                    LogoutsYesterday,
                    abnormalClientThreadTerminations);
            return Value;
        }

        public void ResetStats()
        {
        }

        #endregion

        #endregion
    }
}