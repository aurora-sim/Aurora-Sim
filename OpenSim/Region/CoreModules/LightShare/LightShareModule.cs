/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using System.IO;
using System.Reflection;
using OpenMetaverse;
using log4net;
using Nini.Config;
using OpenSim.Data;
using OpenSim.Framework;
using OpenSim.Region.CoreModules.Framework.InterfaceCommander;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Mono.Addins;


namespace OpenSim.Region.CoreModules.World.LightShare
{
    public class RegionLightShareData : ICloneable
    {
        public UUID regionID = UUID.Zero;
        public Vector3 waterColor = new Vector3(4.0f, 38.0f, 64.0f);
        public float waterFogDensityExponent = 4.0f;
        public float underwaterFogModifier = 0.25f;
        public Vector3 reflectionWaveletScale = new Vector3(2.0f, 2.0f, 2.0f);
        public float fresnelScale = 0.40f;
        public float fresnelOffset = 0.50f;
        public float refractScaleAbove = 0.03f;
        public float refractScaleBelow = 0.20f;
        public float blurMultiplier = 0.040f;
        public Vector2 bigWaveDirection = new Vector2(1.05f, -0.42f);
        public Vector2 littleWaveDirection = new Vector2(1.11f, -1.16f);
        public UUID normalMapTexture = new UUID("822ded49-9a6c-f61c-cb89-6df54f42cdf4");
        public Vector4 horizon = new Vector4(0.25f, 0.25f, 0.32f, 0.32f);
        public float hazeHorizon = 0.19f;
        public Vector4 blueDensity = new Vector4(0.12f, 0.22f, 0.38f, 0.38f);
        public float hazeDensity = 0.70f;
        public float densityMultiplier = 0.18f;
        public float distanceMultiplier = 0.8f;
        public UInt16 maxAltitude = 1605;
        public Vector4 sunMoonColor = new Vector4(0.24f, 0.26f, 0.30f, 0.30f);
        public float sunMoonPosition = 0.317f;
        public Vector4 ambient = new Vector4(0.35f, 0.35f, 0.35f, 0.35f);
        public float eastAngle = 0.0f;
        public float sunGlowFocus = 0.10f;
        public float sunGlowSize = 1.75f;
        public float sceneGamma = 1.0f;
        public float starBrightness = 0.0f;
        public Vector4 cloudColor = new Vector4(0.41f, 0.41f, 0.41f, 0.41f);
        public Vector3 cloudXYDensity = new Vector3(1.00f, 0.53f, 1.00f);
        public float cloudCoverage = 0.27f;
        public float cloudScale = 0.42f;
        public Vector3 cloudDetailXYDensity = new Vector3(1.00f, 0.53f, 0.12f);
        public float cloudScrollX = 0.20f;
        public bool cloudScrollXLock = false;
        public float cloudScrollY = 0.01f;
        public bool cloudScrollYLock = false;
        public bool drawClassicClouds = true;

        public delegate void SaveDelegate(RegionLightShareData wl);
        public event SaveDelegate OnSave;
        public void Save()
        {
            if (OnSave != null)
                OnSave(this);
        }
        public object Clone()
        {
            return this.MemberwiseClone();      // call clone method
        }

    }

    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]
    public class LightShareModule : INonSharedRegionModule, ICommandableModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Commander m_commander = new Commander("windlight");
        private Scene m_scene;
        private static bool m_enableWindlight;

        #region ICommandableModule Members

        public ICommander CommandInterface
        {
            get { return m_commander; }
        }

        #endregion

        #region IRegionModule Members

        public static bool EnableWindlight
        {
            get
            {
                return m_enableWindlight;
            }
            set
            {
            }
        }

        public void Initialise(IConfigSource config)
        {
            try
            {
                m_enableWindlight = config.Configs["LightShare"].GetBoolean("enable_windlight", false);
            }
            catch (Exception)
            {
                m_log.Debug("[WINDLIGHT]: ini failure for enable_windlight - using default");
            }
        }

        public void AddRegion(Scene scene)
        {
            if (!m_enableWindlight)
                return;

            m_scene = scene;
            scene.LoadWindlightProfile();
            scene.EventManager.OnPluginConsole += EventManager_OnPluginConsole;

            scene.EventManager.OnMakeRootAgent += EventManager_OnMakeRootAgent;
            scene.EventManager.OnSaveNewWindlightProfile += EventManager_OnSaveNewWindlightProfile;
            scene.EventManager.OnSendNewWindlightProfileTargeted += EventManager_OnSendNewWindlightProfileTargeted;

            InstallCommands();

            //m_log.Debug("[WINDLIGHT]: Initialised windlight module");
        }

        public void RemoveRegion(Scene scene)
        {

        }

        public void RegionLoaded(Scene scene)
        {

        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        private List<byte[]> compileWindlightSettings(RegionLightShareData wl)
        {
            byte[] mBlock = new Byte[249];
            int pos = 0;

            wl.waterColor.ToBytes(mBlock, 0); pos += 12;
            Utils.FloatToBytes(wl.waterFogDensityExponent).CopyTo(mBlock, pos); pos += 4;
            Utils.FloatToBytes(wl.underwaterFogModifier).CopyTo(mBlock, pos); pos += 4;
            wl.reflectionWaveletScale.ToBytes(mBlock, pos); pos += 12;
            Utils.FloatToBytes(wl.fresnelScale).CopyTo(mBlock, pos); pos += 4;
            Utils.FloatToBytes(wl.fresnelOffset).CopyTo(mBlock, pos); pos += 4;
            Utils.FloatToBytes(wl.refractScaleAbove).CopyTo(mBlock, pos); pos += 4;
            Utils.FloatToBytes(wl.refractScaleBelow).CopyTo(mBlock, pos); pos += 4;
            Utils.FloatToBytes(wl.blurMultiplier).CopyTo(mBlock, pos); pos += 4;
            wl.bigWaveDirection.ToBytes(mBlock, pos); pos += 8;
            wl.littleWaveDirection.ToBytes(mBlock, pos); pos += 8;
            wl.normalMapTexture.ToBytes(mBlock, pos); pos += 16;
            wl.horizon.ToBytes(mBlock, pos); pos += 16;
            Utils.FloatToBytes(wl.hazeHorizon).CopyTo(mBlock, pos); pos += 4;
            wl.blueDensity.ToBytes(mBlock, pos); pos += 16;
            Utils.FloatToBytes(wl.hazeDensity).CopyTo(mBlock, pos); pos += 4;
            Utils.FloatToBytes(wl.densityMultiplier).CopyTo(mBlock, pos); pos += 4;
            Utils.FloatToBytes(wl.distanceMultiplier).CopyTo(mBlock, pos); pos += 4;
            wl.sunMoonColor.ToBytes(mBlock, pos); pos += 16;
            Utils.FloatToBytes(wl.sunMoonPosition).CopyTo(mBlock, pos); pos += 4;
            wl.ambient.ToBytes(mBlock, pos); pos += 16;
            Utils.FloatToBytes(wl.eastAngle).CopyTo(mBlock, pos); pos += 4;
            Utils.FloatToBytes(wl.sunGlowFocus).CopyTo(mBlock, pos); pos += 4;
            Utils.FloatToBytes(wl.sunGlowSize).CopyTo(mBlock, pos); pos += 4;
            Utils.FloatToBytes(wl.sceneGamma).CopyTo(mBlock, pos); pos += 4;
            Utils.FloatToBytes(wl.starBrightness).CopyTo(mBlock, pos); pos += 4;
            wl.cloudColor.ToBytes(mBlock, pos); pos += 16;
            wl.cloudXYDensity.ToBytes(mBlock, pos); pos += 12;
            Utils.FloatToBytes(wl.cloudCoverage).CopyTo(mBlock, pos); pos += 4;
            Utils.FloatToBytes(wl.cloudScale).CopyTo(mBlock, pos); pos += 4;
            wl.cloudDetailXYDensity.ToBytes(mBlock, pos); pos += 12;
            Utils.FloatToBytes(wl.cloudScrollX).CopyTo(mBlock, pos); pos += 4;
            Utils.FloatToBytes(wl.cloudScrollY).CopyTo(mBlock, pos); pos += 4;
            Utils.UInt16ToBytes(wl.maxAltitude).CopyTo(mBlock, pos); pos += 2;
            mBlock[pos] = Convert.ToByte(wl.cloudScrollXLock); pos++;
            mBlock[pos] = Convert.ToByte(wl.cloudScrollYLock); pos++;
            mBlock[pos] = Convert.ToByte(wl.drawClassicClouds); pos++;
            List<byte[]> param = new List<byte[]>();
            param.Add(mBlock);
            return param;
        }
        public void SendProfileToClient(ScenePresence presence)
        {
            IClientAPI client = presence.ControllingClient;
            if (m_enableWindlight)
            {
                if (presence.IsChildAgent == false)
                {
                    List<byte[]> param = compileWindlightSettings(m_scene.RegionInfo.WindlightSettings);
                    client.SendGenericMessage("Windlight", param);
                }
            }
            else
            {
                //We probably don't want to spam chat with this.. probably
                m_log.Debug("[WINDLIGHT]: Module disabled");
            }
        }
        public void SendProfileToClient(ScenePresence presence, RegionLightShareData wl)
        {
            IClientAPI client = presence.ControllingClient;
            if (m_enableWindlight)
            {
                if (presence.IsChildAgent == false)
                {
                    List<byte[]> param = compileWindlightSettings(wl);
                    client.SendGenericMessage("Windlight", param);
                }
            }
            else
            {
                //We probably don't want to spam chat with this.. probably
                m_log.Debug("[WINDLIGHT]: Module disabled");
            }
        }
        private void EventManager_OnMakeRootAgent(ScenePresence presence)
        {
            //m_log.Debug("[WINDLIGHT]: Sending windlight scene to new client");
            SendProfileToClient(presence);
        }
        private void EventManager_OnSendNewWindlightProfileTargeted(RegionLightShareData wl, UUID pUUID)
        {
            ScenePresence Sc;
            if (m_scene.TryGetScenePresence(pUUID,out Sc))
            {
                SendProfileToClient(Sc,wl);
            }
        }
        private void EventManager_OnSaveNewWindlightProfile()
        {
            m_scene.ForEachScenePresence(SendProfileToClient);
        }

        public void PostInitialise()
        {

        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "LightShareModule"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        #endregion

        #region ICommandableModule Members

        private void InstallCommands()
        {
            Command wlload = new Command("load", CommandIntentions.COMMAND_NON_HAZARDOUS, HandleLoad, "Load windlight profile from the database and broadcast");
            Command wlenable = new Command("enable", CommandIntentions.COMMAND_NON_HAZARDOUS, HandleEnable, "Enable the windlight plugin");
            Command wldisable = new Command("disable", CommandIntentions.COMMAND_NON_HAZARDOUS, HandleDisable, "Enable the windlight plugin");

            m_commander.RegisterCommand("load", wlload);
            m_commander.RegisterCommand("enable", wlenable);
            m_commander.RegisterCommand("disable", wldisable);

            m_scene.RegisterModuleCommander(m_commander);
        }

        private void HandleLoad(Object[] args)
        {
            if (!m_enableWindlight)
            {
                m_log.InfoFormat("[WINDLIGHT]: Cannot load windlight profile, module disabled. Use 'windlight enable' first.");
            }
            else
            {
                m_log.InfoFormat("[WINDLIGHT]: Loading Windlight profile from database");
                m_scene.LoadWindlightProfile();
                m_log.InfoFormat("[WINDLIGHT]: Load complete");
            }
        }

        private void HandleDisable(Object[] args)
        {
            m_log.InfoFormat("[WINDLIGHT]: Plugin now disabled");
            m_enableWindlight=false;
        }

        private void HandleEnable(Object[] args)
        {
            m_log.InfoFormat("[WINDLIGHT]: Plugin now enabled");
            m_enableWindlight = true;
        }

        /// <summary>
        /// Processes commandline input. Do not call directly.
        /// </summary>
        /// <param name="args">Commandline arguments</param>
        private void EventManager_OnPluginConsole(string[] args)
        {
            if (args[0] == "windlight")
            {
                if (args.Length == 1)
                {
                    m_commander.ProcessConsoleCommand("add", new string[0]);
                    return;
                }

                string[] tmpArgs = new string[args.Length - 2];
                int i;
                for (i = 2; i < args.Length; i++)
                {
                    tmpArgs[i - 2] = args[i];
                }

                m_commander.ProcessConsoleCommand(args[1], tmpArgs);
            }
        }
        #endregion

    }
}

