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
using Aurora.Framework.Utilities;

namespace Aurora.Modules.Monitoring.Monitors
{
    internal class TimeDilationMonitor : ITimeDilationMonitor
    {
        private readonly AveragingClass m_average = new AveragingClass(5);
        private readonly IScene m_scene;

        public TimeDilationMonitor(IScene scene)
        {
            m_scene = scene;
        }

        #region Implementation of IMonitor

        public double GetValue()
        {
            return m_scene.TimeDilation;
        }

        public string GetName()
        {
            return "Time Dilation";
        }

        public string GetFriendlyValue()
        {
            return (100*GetValue()) + "%";
        }

        public void SetPhysicsFPS(float value)
        {
            m_average.Add(value);
            //Now fix time dilation
            m_scene.TimeDilation = m_average.GetAverage()/m_scene.BaseSimPhysFPS;
            if (m_scene.TimeDilation < 0.1) //Limit so that the client (and physics engine) don't go crazy
                m_scene.TimeDilation = 0.1f;
            else if (m_scene.TimeDilation > 1.0) //No going over!
                m_scene.TimeDilation = 1.0f;
        }

        public void ResetStats()
        {
        }

        #endregion
    }
}