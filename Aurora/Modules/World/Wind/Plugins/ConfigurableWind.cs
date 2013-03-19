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
using Aurora.Framework.SceneInfo;
using Nini.Config;
using OpenMetaverse;
using System;
using System.Collections.Generic;

namespace Aurora.Modules.Wind.Plugins
{
    public class ConfigurableWind : IWindModelPlugin
    {
        private float m_avgDirection; // Average direction of the wind in degrees
        private float m_avgStrength = 5.0f; // Average magnitude of the wind vector

        private Vector2 m_curPredominateWind;
        private float m_rateChange = 1.0f; // 
        private float m_varDirection = 30.0f; // Max Direction Variance
        private float m_varStrength = 5.0f; // Max Strength  Variance
        private Vector2[] m_windSpeeds = new Vector2[16*16];

        public string Version
        {
            get { return "1.0.0.0"; }
        }

        #region IWindModelPlugin Members

        public string Name
        {
            get { return "ConfigurableWind"; }
        }

        public void Initialise()
        {
        }

        public void WindConfig(IScene scene, IConfig windConfig)
        {
            if (windConfig != null)
            {
                // Uses strength value if avg_strength not specified
                m_avgStrength = windConfig.GetFloat("strength", 5.0F);
                m_avgStrength = windConfig.GetFloat("avg_strength", 5.0F);

                m_avgDirection = windConfig.GetFloat("avg_direction", 0.0F);
                m_varStrength = windConfig.GetFloat("var_strength", 5.0F);
                m_varDirection = windConfig.GetFloat("var_direction", 30.0F);
                m_rateChange = windConfig.GetFloat("rate_change", 1.0F);
            }
        }

        public void WindUpdate(uint frame)
        {
            double avgAng = m_avgDirection*(Math.PI/180.0f);
            double varDir = m_varDirection*(Math.PI/180.0f);

            // Prevailing wind algorithm
            // Inspired by Kanker Greenacre

            // TODO: 
            // * This should probably be based on in-world time.
            // * should probably move all these local variables to class members and constants
            double time = DateTime.Now.TimeOfDay.TotalSeconds/86400.0f;

            double theta = time*(2*Math.PI)*m_rateChange;

            double offset = Math.Sin(theta)*Math.Sin(theta*2)*Math.Sin(theta*9)*Math.Cos(theta*4);

            double windDir = avgAng + (varDir*offset);

            offset = Math.Sin(theta)*Math.Sin(theta*4) + (Math.Sin(theta*13)/3);
            double windSpeed = m_avgStrength + (m_varStrength*offset);

            if (windSpeed < 0)
                windSpeed = 0;


            m_curPredominateWind.X = (float) Math.Cos(windDir);
            m_curPredominateWind.Y = (float) Math.Sin(windDir);

            m_curPredominateWind.Normalize();
            m_curPredominateWind.X *= (float) windSpeed;
            m_curPredominateWind.Y *= (float) windSpeed;

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    m_windSpeeds[y*16 + x] = m_curPredominateWind;
                }
            }
        }

        public Vector3 WindSpeed(float fX, float fY, float fZ)
        {
            return new Vector3(m_curPredominateWind, 0.0f);
        }

        public Vector2[] WindLLClientArray()
        {
            return m_windSpeeds;
        }

        public string Description
        {
            get
            {
                return
                    "Provides a predominate wind direction that can change within configured variances for direction and speed.";
            }
        }

        public Dictionary<string, string> WindParams()
        {
            Dictionary<string, string> Params = new Dictionary<string, string>
                                                    {
                                                        {"avgStrength", "average wind strength"},
                                                        {"avgDirection", "average wind direction in degrees"},
                                                        {"varStrength", "allowable variance in wind strength"},
                                                        {
                                                            "varDirection",
                                                            "allowable variance in wind direction in +/- degrees"
                                                        },
                                                        {"rateChange", "rate of change"}
                                                    };

            return Params;
        }

        public void WindParamSet(string param, float value)
        {
            switch (param)
            {
                case "avgStrength":
                    m_avgStrength = value;
                    break;
                case "avgDirection":
                    m_avgDirection = value;
                    break;
                case "varStrength":
                    m_varStrength = value;
                    break;
                case "varDirection":
                    m_varDirection = value;
                    break;
                case "rateChange":
                    m_rateChange = value;
                    break;
            }
        }

        public float WindParamGet(string param)
        {
            switch (param)
            {
                case "avgStrength":
                    return m_avgStrength;
                case "avgDirection":
                    return m_avgDirection;
                case "varStrength":
                    return m_varStrength;
                case "varDirection":
                    return m_varDirection;
                case "rateChange":
                    return m_rateChange;
                default:
                    throw new Exception(String.Format("Unknown {0} parameter {1}", this.Name, param));
            }
        }

        #endregion

        public void Dispose()
        {
            m_windSpeeds = null;
        }

        private void LogSettings()
        {
            MainConsole.Instance.InfoFormat("[ConfigurableWind] Average Strength   : {0}", m_avgStrength);
            MainConsole.Instance.InfoFormat("[ConfigurableWind] Average Direction  : {0}", m_avgDirection);
            MainConsole.Instance.InfoFormat("[ConfigurableWind] Varience Strength  : {0}", m_varStrength);
            MainConsole.Instance.InfoFormat("[ConfigurableWind] Varience Direction : {0}", m_varDirection);
            MainConsole.Instance.InfoFormat("[ConfigurableWind] Rate Change        : {0}", m_rateChange);
        }
    }
}