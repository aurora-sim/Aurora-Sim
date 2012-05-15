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

using System;
using System.Runtime.Remoting.Lifetime;
using Aurora.Framework;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using LSL_Float = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLFloat;
using LSL_Integer = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLInteger;
using LSL_Key = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_List = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.list;
using LSL_Rotation = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Quaternion;
using LSL_String = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.LSLString;
using LSL_Vector = Aurora.ScriptEngine.AuroraDotNetEngine.LSL_Types.Vector3;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.APIs
{
    [Serializable]
    public class LS_Api : MarshalByRefObject, ILS_Api, IScriptApi
    {
        internal ScriptProtectionModule ScriptProtection;
        internal bool m_LSFunctionsEnabled;
        internal IScriptModulePlugin m_ScriptEngine;
        internal IScriptModuleComms m_comms;
        internal ISceneChildEntity m_host;
        internal UUID m_itemID;
        internal uint m_localID;

        public IScene World
        {
            get { return m_host.ParentEntity.Scene; }
        }

        #region ILS_Api Members

        /// <summary>
        ///   Get the current Windlight scene
        /// </summary>
        /// <returns>List of windlight parameters</returns>
        public LSL_List lsGetWindlightScene(LSL_List rules)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.None, "lsGetWindlightScene", m_host, "LS", m_itemID))
                return new LSL_List();

            /*RegionLightShareData wl = m_lightShareModule.WindLightSettings;

            LSL_List values = new LSL_List();
            int idx = 0;
            while (idx < rules.Length)
            {
                uint rule = (uint)rules.GetLSLIntegerItem(idx);
                LSL_List toadd = new LSL_List();

                switch (rule)
                {
                    case (int)ScriptBaseClass.WL_AMBIENT:
                        toadd.Add(new LSL_Rotation(wl.ambient.X, wl.ambient.Y, wl.ambient.Z, wl.ambient.W));
                        break;
                    case (int)ScriptBaseClass.WL_BIG_WAVE_DIRECTION:
                        toadd.Add(new LSL_Vector(wl.bigWaveDirection.X, wl.bigWaveDirection.Y, 0.0f));
                        break;
                    case (int)ScriptBaseClass.WL_BLUE_DENSITY:
                        toadd.Add(new LSL_Rotation(wl.blueDensity.X, wl.blueDensity.Y, wl.blueDensity.Z, wl.blueDensity.W));
                        break;
                    case (int)ScriptBaseClass.WL_BLUR_MULTIPLIER:
                        toadd.Add(new LSL_Float(wl.blurMultiplier));
                        break;
                    case (int)ScriptBaseClass.WL_CLOUD_COLOR:
                        toadd.Add(new LSL_Rotation(wl.cloudColor.X, wl.cloudColor.Y, wl.cloudColor.Z, wl.cloudColor.W));
                        break;
                    case (int)ScriptBaseClass.WL_CLOUD_COVERAGE:
                        toadd.Add(new LSL_Float(wl.cloudCoverage));
                        break;
                    case (int)ScriptBaseClass.WL_CLOUD_DETAIL_XY_DENSITY:
                        toadd.Add(new LSL_Vector(wl.cloudDetailXYDensity.X, wl.cloudDetailXYDensity.Y, wl.cloudDetailXYDensity.Z));
                        break;
                    case (int)ScriptBaseClass.WL_CLOUD_SCALE:
                        toadd.Add(new LSL_Float(wl.cloudScale));
                        break;
                    case (int)ScriptBaseClass.WL_CLOUD_SCROLL_X:
                        toadd.Add(new LSL_Float(wl.cloudScrollX));
                        break;
                    case (int)ScriptBaseClass.WL_CLOUD_SCROLL_X_LOCK:
                        toadd.Add(new LSL_Integer(wl.cloudScrollXLock ? 1 : 0));
                        break;
                    case (int)ScriptBaseClass.WL_CLOUD_SCROLL_Y:
                        toadd.Add(new LSL_Float(wl.cloudScrollY));
                        break;
                    case (int)ScriptBaseClass.WL_CLOUD_SCROLL_Y_LOCK:
                        toadd.Add(new LSL_Integer(wl.cloudScrollYLock ? 1 : 0));
                        break;
                    case (int)ScriptBaseClass.WL_CLOUD_XY_DENSITY:
                        toadd.Add(new LSL_Vector(wl.cloudXYDensity.X, wl.cloudXYDensity.Y, wl.cloudXYDensity.Z));
                        break;
                    case (int)ScriptBaseClass.WL_DENSITY_MULTIPLIER:
                        toadd.Add(new LSL_Float(wl.densityMultiplier));
                        break;
                    case (int)ScriptBaseClass.WL_DISTANCE_MULTIPLIER:
                        toadd.Add(new LSL_Float(wl.distanceMultiplier));
                        break;
                    case (int)ScriptBaseClass.WL_DRAW_CLASSIC_CLOUDS:
                        toadd.Add(new LSL_Integer(wl.drawClassicClouds ? 1 : 0));
                        break;
                    case (int)ScriptBaseClass.WL_EAST_ANGLE:
                        toadd.Add(new LSL_Float(wl.eastAngle));
                        break;
                    case (int)ScriptBaseClass.WL_FRESNEL_OFFSET:
                        toadd.Add(new LSL_Float(wl.fresnelOffset));
                        break;
                    case (int)ScriptBaseClass.WL_FRESNEL_SCALE:
                        toadd.Add(new LSL_Float(wl.fresnelScale));
                        break;
                    case (int)ScriptBaseClass.WL_HAZE_DENSITY:
                        toadd.Add(new LSL_Float(wl.hazeDensity));
                        break;
                    case (int)ScriptBaseClass.WL_HAZE_HORIZON:
                        toadd.Add(new LSL_Float(wl.hazeHorizon));
                        break;
                    case (int)ScriptBaseClass.WL_HORIZON:
                        toadd.Add(new LSL_Rotation(wl.horizon.X, wl.horizon.Y, wl.horizon.Z, wl.horizon.W));
                        break;
                    case (int)ScriptBaseClass.WL_LITTLE_WAVE_DIRECTION:
                        toadd.Add(new LSL_Vector(wl.littleWaveDirection.X, wl.littleWaveDirection.Y, 0.0f));
                        break;
                    case (int)ScriptBaseClass.WL_MAX_ALTITUDE:
                        toadd.Add(new LSL_Integer(wl.maxAltitude));
                        break;
                    case (int)ScriptBaseClass.WL_NORMAL_MAP_TEXTURE:
                        toadd.Add(new LSL_Key(wl.normalMapTexture.ToString()));
                        break;
                    case (int)ScriptBaseClass.WL_REFLECTION_WAVELET_SCALE:
                        toadd.Add(new LSL_Vector(wl.reflectionWaveletScale.X, wl.reflectionWaveletScale.Y, wl.reflectionWaveletScale.Z));
                        break;
                    case (int)ScriptBaseClass.WL_REFRACT_SCALE_ABOVE:
                        toadd.Add(new LSL_Float(wl.refractScaleAbove));
                        break;
                    case (int)ScriptBaseClass.WL_REFRACT_SCALE_BELOW:
                        toadd.Add(new LSL_Float(wl.refractScaleBelow));
                        break;
                    case (int)ScriptBaseClass.WL_SCENE_GAMMA:
                        toadd.Add(new LSL_Float(wl.sceneGamma));
                        break;
                    case (int)ScriptBaseClass.WL_STAR_BRIGHTNESS:
                        toadd.Add(new LSL_Float(wl.starBrightness));
                        break;
                    case (int)ScriptBaseClass.WL_SUN_GLOW_FOCUS:
                        toadd.Add(new LSL_Float(wl.sunGlowFocus));
                        break;
                    case (int)ScriptBaseClass.WL_SUN_GLOW_SIZE:
                        toadd.Add(new LSL_Float(wl.sunGlowSize));
                        break;
                    case (int)ScriptBaseClass.WL_SUN_MOON_COLOR:
                        toadd.Add(new LSL_Rotation(wl.sunMoonColor.X, wl.sunMoonColor.Y, wl.sunMoonColor.Z, wl.sunMoonColor.W));
                        break;
                    case (int)ScriptBaseClass.WL_UNDERWATER_FOG_MODIFIER:
                        toadd.Add(new LSL_Float(wl.underwaterFogModifier));
                        break;
                    case (int)ScriptBaseClass.WL_WATER_COLOR:
                        toadd.Add(new LSL_Vector(wl.waterColor.X, wl.waterColor.Y, wl.waterColor.Z));
                        break;
                    case (int)ScriptBaseClass.WL_WATER_FOG_DENSITY_EXPONENT:
                        toadd.Add(new LSL_Float(wl.waterFogDensityExponent));
                        break;
                }

                if (toadd.Length > 0)
                {
                    values.Add(rule);
                    values.Add(toadd.Data[0]);
                }
                idx++;
            }
            

            return values;
            */
            return null;
        }

        /// <summary>
        ///   Set the current Windlight scene
        /// </summary>
        /// <param name = "rules"></param>
        /// <returns>success: true or false</returns>
        public int lsSetWindlightScene(LSL_List rules)
        {
            if (!ScriptProtection.CheckThreatLevel(ThreatLevel.Moderate, "lsSetWindlightScene", m_host, "LS", m_itemID))
                return 0;

            if (!World.RegionInfo.EstateSettings.IsEstateManager(m_host.OwnerID) &&
                World.GetScenePresence(m_host.OwnerID).GodLevel < 200)
            {
                LSShoutError("lsSetWindlightScene can only be used by estate managers or owners.");
                return 0;
            }
            int success = 0;

            /*if (m_lightShareModule.EnableWindLight)
            {
                RegionLightShareData wl = getWindlightProfileFromRules(rules);
                //m_lightShareModule.SaveWindLightSettings(0, wl);
                success = 1;
            }
            else
            {
                LSShoutError("Windlight module is disabled");
                return 0;
            }*/
            return success;
        }

        /// <summary>
        ///   Set the current Windlight scene to a target avatar
        /// </summary>
        /// <param name = "rules"></param>
        /// <returns>success: true or false</returns>
        public int lsSetWindlightSceneTargeted(LSL_List rules, LSL_Key target)
        {
            if (!m_LSFunctionsEnabled)
            {
                LSShoutError("LightShare functions are not enabled.");
                return 0;
            }
            if (!World.RegionInfo.EstateSettings.IsEstateManager(m_host.OwnerID) &&
                World.GetScenePresence(m_host.OwnerID).GodLevel < 200)
            {
                LSShoutError("lsSetWindlightSceneTargeted can only be used by estate managers or owners.");
                return 0;
            }
            int success = 0;

            /*if (m_lightShareModule.EnableWindLight)
            { 
                RegionLightShareData wl = getWindlightProfileFromRules(rules);
                m_lightShareModule.SendWindlightProfileTargeted(wl, new UUID(target.m_string));
                success = 1;
            }
            else
            {
                LSShoutError("Windlight module is disabled");
                return 0;
            }*/
            return success;
        }

        #endregion

        //internal IWindLightSettingsModule m_lightShareModule;

        #region IScriptApi Members

        public void Initialize(IScriptModulePlugin ScriptEngine, ISceneChildEntity host, uint localID, UUID itemID,
                               ScriptProtectionModule module)
        {
            m_ScriptEngine = ScriptEngine;
            m_host = host;
            m_localID = localID;
            m_itemID = itemID;
            ScriptProtection = module;

            if (m_ScriptEngine.Config.GetBoolean("AllowLightShareFunctions", false))
                m_LSFunctionsEnabled = true;

            m_comms = World.RequestModuleInterface<IScriptModuleComms>();
            if (m_comms == null)
                m_LSFunctionsEnabled = false;
        }

        public string Name
        {
            get { return "ls"; }
        }

        public string InterfaceName
        {
            get { return "ILS_Api"; }
        }

        /// <summary>
        ///   We don't have to add any assemblies here
        /// </summary>
        public string[] ReferencedAssemblies
        {
            get { return new string[0]; }
        }

        /// <summary>
        ///   We use the default namespace, so we don't have any to add
        /// </summary>
        public string[] NamespaceAdditions
        {
            get { return new string[0]; }
        }

        public IScriptApi Copy()
        {
            return new LS_Api();
        }

        #endregion

        public void Dispose()
        {
        }

        public override Object InitializeLifetimeService()
        {
            ILease lease = (ILease)base.InitializeLifetimeService();

            if (lease.CurrentState == LeaseState.Initial)
            {
                lease.InitialLeaseTime = TimeSpan.FromMinutes(0);
                //                lease.RenewOnCallTime = TimeSpan.FromSeconds(10.0);
                //                lease.SponsorshipTimeout = TimeSpan.FromMinutes(1.0);
            }
            return lease;
        }

        //
        //Dumps an error message on the debug console.
        //

        internal void LSShoutError(string message)
        {
            if (message.Length > 1023)
                message = message.Substring(0, 1023);

            IChatModule chatModule = World.RequestModuleInterface<IChatModule>();
            if (chatModule != null)
                chatModule.SimChat(message,
                                   ChatTypeEnum.Shout, ScriptBaseClass.DEBUG_CHANNEL,
                                   m_host.ParentEntity.RootChild.AbsolutePosition, m_host.Name, m_host.UUID, true, World);

            IWorldComm wComm = World.RequestModuleInterface<IWorldComm>();
            wComm.DeliverMessage(ChatTypeEnum.Shout, ScriptBaseClass.DEBUG_CHANNEL, m_host.Name, m_host.UUID, message);
        }

        private RegionLightShareData getWindlightProfileFromRules(LSL_List rules)
        {
            /*RegionLightShareData wl = m_lightShareModule.WindLightSettings;

            LSL_List values = new LSL_List();
            int idx = 0;
            while (idx < rules.Length)
            {
                uint rule = (uint)rules.GetLSLIntegerItem(idx);
                LSL_Types.Quaternion iQ;
                LSL_Types.Vector3 iV;
                switch (rule)
                {
                    case (int)ScriptBaseClass.WL_SUN_MOON_POSITION:
                        idx++;
                        wl.sunMoonPosition = (float)rules.GetLSLFloatItem(idx);
                        break;
                    case (int)ScriptBaseClass.WL_AMBIENT:
                        idx++;
                        iQ = rules.GetQuaternionItem(idx);
                        wl.ambient = new Vector4((float)iQ.x, (float)iQ.y, (float)iQ.z, (float)iQ.s);
                        break;
                    case (int)ScriptBaseClass.WL_BIG_WAVE_DIRECTION:
                        idx++;
                        iV = rules.GetVector3Item(idx);
                        wl.bigWaveDirection = new Vector2((float)iV.x, (float)iV.y);
                        break;
                    case (int)ScriptBaseClass.WL_BLUE_DENSITY:
                        idx++;
                        iQ = rules.GetQuaternionItem(idx);
                        wl.blueDensity = new Vector4((float)iQ.x, (float)iQ.y, (float)iQ.z, (float)iQ.s);
                        break;
                    case (int)ScriptBaseClass.WL_BLUR_MULTIPLIER:
                        idx++;
                        wl.blurMultiplier = (float)rules.GetLSLFloatItem(idx);
                        break;
                    case (int)ScriptBaseClass.WL_CLOUD_COLOR:
                        idx++;
                        iQ = rules.GetQuaternionItem(idx);
                        wl.cloudColor = new Vector4((float)iQ.x, (float)iQ.y, (float)iQ.z, (float)iQ.s);
                        break;
                    case (int)ScriptBaseClass.WL_CLOUD_COVERAGE:
                        idx++;
                        wl.cloudCoverage = (float)rules.GetLSLFloatItem(idx);
                        break;
                    case (int)ScriptBaseClass.WL_CLOUD_DETAIL_XY_DENSITY:
                        idx++;
                        iV = rules.GetVector3Item(idx);
                        wl.cloudDetailXYDensity = new Vector3((float)iV.x, (float)iV.y, (float)iV.z);
                        break;
                    case (int)ScriptBaseClass.WL_CLOUD_SCALE:
                        idx++;
                        wl.cloudScale = (float)rules.GetLSLFloatItem(idx);
                        break;
                    case (int)ScriptBaseClass.WL_CLOUD_SCROLL_X:
                        idx++;
                        wl.cloudScrollX = (float)rules.GetLSLFloatItem(idx);
                        break;
                    case (int)ScriptBaseClass.WL_CLOUD_SCROLL_X_LOCK:
                        idx++;
                        wl.cloudScrollXLock = rules.GetLSLIntegerItem(idx).value == 1 ? true : false;
                        break;
                    case (int)ScriptBaseClass.WL_CLOUD_SCROLL_Y:
                        idx++;
                        wl.cloudScrollY = (float)rules.GetLSLFloatItem(idx);
                        break;
                    case (int)ScriptBaseClass.WL_CLOUD_SCROLL_Y_LOCK:
                        idx++;
                        wl.cloudScrollYLock = rules.GetLSLIntegerItem(idx).value == 1 ? true : false;
                        break;
                    case (int)ScriptBaseClass.WL_CLOUD_XY_DENSITY:
                        idx++;
                        iV = rules.GetVector3Item(idx);
                        wl.cloudXYDensity = new Vector3((float)iV.x, (float)iV.y, (float)iV.z);
                        break;
                    case (int)ScriptBaseClass.WL_DENSITY_MULTIPLIER:
                        idx++;
                        wl.densityMultiplier = (float)rules.GetLSLFloatItem(idx);
                        break;
                    case (int)ScriptBaseClass.WL_DISTANCE_MULTIPLIER:
                        idx++;
                        wl.distanceMultiplier = (float)rules.GetLSLFloatItem(idx);
                        break;
                    case (int)ScriptBaseClass.WL_DRAW_CLASSIC_CLOUDS:
                        idx++;
                        wl.drawClassicClouds = rules.GetLSLIntegerItem(idx).value == 1 ? true : false;
                        break;
                    case (int)ScriptBaseClass.WL_EAST_ANGLE:
                        idx++;
                        wl.eastAngle = (float)rules.GetLSLFloatItem(idx);
                        break;
                    case (int)ScriptBaseClass.WL_FRESNEL_OFFSET:
                        idx++;
                        wl.fresnelOffset = (float)rules.GetLSLFloatItem(idx);
                        break;
                    case (int)ScriptBaseClass.WL_FRESNEL_SCALE:
                        idx++;
                        wl.fresnelScale = (float)rules.GetLSLFloatItem(idx);
                        break;
                    case (int)ScriptBaseClass.WL_HAZE_DENSITY:
                        idx++;
                        wl.hazeDensity = (float)rules.GetLSLFloatItem(idx);
                        break;
                    case (int)ScriptBaseClass.WL_HAZE_HORIZON:
                        idx++;
                        wl.hazeHorizon = (float)rules.GetLSLFloatItem(idx);
                        break;
                    case (int)ScriptBaseClass.WL_HORIZON:
                        idx++;
                        iQ = rules.GetQuaternionItem(idx);
                        wl.horizon = new Vector4((float)iQ.x, (float)iQ.y, (float)iQ.z, (float)iQ.s);
                        break;
                    case (int)ScriptBaseClass.WL_LITTLE_WAVE_DIRECTION:
                        idx++;
                        iV = rules.GetVector3Item(idx);
                        wl.littleWaveDirection = new Vector2((float)iV.x, (float)iV.y);
                        break;
                    case (int)ScriptBaseClass.WL_MAX_ALTITUDE:
                        idx++;
                        wl.maxAltitude = (ushort)rules.GetLSLIntegerItem(idx).value;
                        break;
                    case (int)ScriptBaseClass.WL_NORMAL_MAP_TEXTURE:
                        idx++;
                        wl.normalMapTexture = new UUID(rules.GetLSLStringItem(idx).m_string);
                        break;
                    case (int)ScriptBaseClass.WL_REFLECTION_WAVELET_SCALE:
                        idx++;
                        iV = rules.GetVector3Item(idx);
                        wl.reflectionWaveletScale = new Vector3((float)iV.x, (float)iV.y, (float)iV.z);
                        break;
                    case (int)ScriptBaseClass.WL_REFRACT_SCALE_ABOVE:
                        idx++;
                        wl.refractScaleAbove = (float)rules.GetLSLFloatItem(idx);
                        break;
                    case (int)ScriptBaseClass.WL_REFRACT_SCALE_BELOW:
                        idx++;
                        wl.refractScaleBelow = (float)rules.GetLSLFloatItem(idx);
                        break;
                    case (int)ScriptBaseClass.WL_SCENE_GAMMA:
                        idx++;
                        wl.sceneGamma = (float)rules.GetLSLFloatItem(idx);
                        break;
                    case (int)ScriptBaseClass.WL_STAR_BRIGHTNESS:
                        idx++;
                        wl.starBrightness = (float)rules.GetLSLFloatItem(idx);
                        break;
                    case (int)ScriptBaseClass.WL_SUN_GLOW_FOCUS:
                        idx++;
                        wl.sunGlowFocus = (float)rules.GetLSLFloatItem(idx);
                        break;
                    case (int)ScriptBaseClass.WL_SUN_GLOW_SIZE:
                        idx++;
                        wl.sunGlowSize = (float)rules.GetLSLFloatItem(idx);
                        break;
                    case (int)ScriptBaseClass.WL_SUN_MOON_COLOR:
                        idx++;
                        iQ = rules.GetQuaternionItem(idx);
                        wl.sunMoonColor = new Vector4((float)iQ.x, (float)iQ.y, (float)iQ.z, (float)iQ.s);
                        break;
                    case (int)ScriptBaseClass.WL_UNDERWATER_FOG_MODIFIER:
                        idx++;
                        wl.underwaterFogModifier = (float)rules.GetLSLFloatItem(idx);
                        break;
                    case (int)ScriptBaseClass.WL_WATER_COLOR:
                        idx++;
                        iV = rules.GetVector3Item(idx);
                        wl.waterColor = new Vector4((float)iV.x, (float)iV.y, (float)iV.z, 0);
                        break;
                    case (int)ScriptBaseClass.WL_WATER_FOG_DENSITY_EXPONENT:
                        idx++;
                        wl.waterFogDensityExponent = (float)rules.GetLSLFloatItem(idx);
                        break;
                }
                idx++;
            }
            wl.regionID = m_host.ParentGroup.Scene.RegionInfo.RegionID;
            return wl;*/
            return null;
        }
    }
}