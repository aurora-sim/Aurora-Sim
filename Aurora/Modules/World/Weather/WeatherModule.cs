using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Timers;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Nini.Config;
using log4net;
using Aurora.DataManager;
using Aurora.Framework;
using Mono.Addins;
using OpenMetaverse;

namespace Aurora.Modules
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]
    public class WeatherModule : ISharedRegionModule
    {
        #region ISharedRegionModule Members
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Timer timer = new Timer();
        private Dictionary<Scene, WeatherInRegion> Scenes = new Dictionary<Scene, WeatherInRegion>();
        private WeatherType CurrentWeather = WeatherType.Realistic;
        private bool Clouds = true;

        public void Initialise(IConfigSource source)
        {
            timer.Interval = 10000;
            timer.Enabled = true;
            timer.Elapsed += new ElapsedEventHandler(GenerateNewWindlightProfiles);
            timer.Start();
        }

        public void Close()
        {

        }

        public void AddRegion(Scene scene)
        {
            WeatherInRegion WIR = new WeatherInRegion();
            WIR.Randomize();
            Scenes.Add(scene, WIR);
            scene.AddCommand(this, "change weather", "Changes weather", "Changes the weather in the current region", ConsoleChangeWeather);
        }

        public void RemoveRegion(Scene scene)
        {

        }

        public void RegionLoaded(Scene scene)
        {

        }

        public void ConsoleChangeWeather(string module, string[] cmdparams)
        {
            string WeatherType = "";
            string Region = "";
            while (true)
            {
                Region = MainConsole.Instance.CmdPrompt("Region", "None");
                if (Region == "None")
                    m_log.Info("No region selected. Please try again");
                else
                {
                    bool found = false;
                    foreach (Scene scene in Scenes.Keys)
                    {
                        if (scene.RegionInfo.RegionName == Region)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found) break;
                    m_log.Info("No region found. Please try again");
                }
            }
            WeatherType = MainConsole.Instance.CmdPrompt("Weather Type", "0");

            CurrentWeather = (WeatherType)int.Parse(WeatherType);
            GenerateNewWindlightProfiles();
        }



        private void GenerateNewWindlightProfiles()
        {
            SendCurrentProfilesToClients();
            List<Scene> isRaining = new List<Scene>();
            Random random = new Random();
            Dictionary<Scene, WeatherInRegion> unfinishedScenes = Scenes;
            foreach (KeyValuePair<Scene, WeatherInRegion> scene in Scenes)
            {
                if (scene.Value.CurrentRainy || scene.Value.NextRainy)
                    isRaining.Add(scene.Key);
                scene.Key.RegionInfo.WindlightSettings = scene.Value.MakeRegionWindLightData(scene.Key.RegionInfo.WindlightSettings);
            }

            #region Move rain
            if (isRaining.Count > 0)
            {
                
                //unfinishedScenes.Remove();
            }
            #endregion
            #region Make new rain
            else
            {
                if (random.Next(1) == 1)
                {
                    //Make new rain
                }
            }
            #endregion
            //Make the others sunny
            foreach (KeyValuePair<Scene, WeatherInRegion> kvp in unfinishedScenes)
            {
                kvp.Value.NextRainy = false;
                kvp.Value.NextSunny = true;
            }
        }

        private void SendCurrentProfilesToClients()
        {
            foreach (Scene scene in Scenes.Keys)
            {
                scene.ForEachScenePresence(SendProfileToClient);
            }
        }
        public void SendProfileToClient(ScenePresence presence)
        {
            IClientAPI client = presence.ControllingClient;
            if (presence.IsChildAgent == false)
            {
                List<byte[]> param = compileWindlightSettings(presence.Scene.RegionInfo.WindlightSettings);
                client.SendGenericMessage("Windlight", param);
            }
        }

        private List<byte[]> compileWindlightSettings(RegionLightShareData wl)
        {
            byte[] mBlock = new Byte[249];
            int pos = 0;

            wl.waterColor.ToBytes(mBlock, 0); pos += 12;
            OpenMetaverse.Utils.FloatToBytes(wl.waterFogDensityExponent).CopyTo(mBlock, pos); pos += 4;
            OpenMetaverse.Utils.FloatToBytes(wl.underwaterFogModifier).CopyTo(mBlock, pos); pos += 4;
            wl.reflectionWaveletScale.ToBytes(mBlock, pos); pos += 12;
            OpenMetaverse.Utils.FloatToBytes(wl.fresnelScale).CopyTo(mBlock, pos); pos += 4;
            OpenMetaverse.Utils.FloatToBytes(wl.fresnelOffset).CopyTo(mBlock, pos); pos += 4;
            OpenMetaverse.Utils.FloatToBytes(wl.refractScaleAbove).CopyTo(mBlock, pos); pos += 4;
            OpenMetaverse.Utils.FloatToBytes(wl.refractScaleBelow).CopyTo(mBlock, pos); pos += 4;
            OpenMetaverse.Utils.FloatToBytes(wl.blurMultiplier).CopyTo(mBlock, pos); pos += 4;
            wl.bigWaveDirection.ToBytes(mBlock, pos); pos += 8;
            wl.littleWaveDirection.ToBytes(mBlock, pos); pos += 8;
            wl.normalMapTexture.ToBytes(mBlock, pos); pos += 16;
            wl.horizon.ToBytes(mBlock, pos); pos += 16;
            OpenMetaverse.Utils.FloatToBytes(wl.hazeHorizon).CopyTo(mBlock, pos); pos += 4;
            wl.blueDensity.ToBytes(mBlock, pos); pos += 16;
            OpenMetaverse.Utils.FloatToBytes(wl.hazeDensity).CopyTo(mBlock, pos); pos += 4;
            OpenMetaverse.Utils.FloatToBytes(wl.densityMultiplier).CopyTo(mBlock, pos); pos += 4;
            OpenMetaverse.Utils.FloatToBytes(wl.distanceMultiplier).CopyTo(mBlock, pos); pos += 4;
            wl.sunMoonColor.ToBytes(mBlock, pos); pos += 16;
            OpenMetaverse.Utils.FloatToBytes(wl.sunMoonPosition).CopyTo(mBlock, pos); pos += 4;
            wl.ambient.ToBytes(mBlock, pos); pos += 16;
            OpenMetaverse.Utils.FloatToBytes(wl.eastAngle).CopyTo(mBlock, pos); pos += 4;
            OpenMetaverse.Utils.FloatToBytes(wl.sunGlowFocus).CopyTo(mBlock, pos); pos += 4;
            OpenMetaverse.Utils.FloatToBytes(wl.sunGlowSize).CopyTo(mBlock, pos); pos += 4;
            OpenMetaverse.Utils.FloatToBytes(wl.sceneGamma).CopyTo(mBlock, pos); pos += 4;
            OpenMetaverse.Utils.FloatToBytes(wl.starBrightness).CopyTo(mBlock, pos); pos += 4;
            wl.cloudColor.ToBytes(mBlock, pos); pos += 16;
            wl.cloudXYDensity.ToBytes(mBlock, pos); pos += 12;
            OpenMetaverse.Utils.FloatToBytes(wl.cloudCoverage).CopyTo(mBlock, pos); pos += 4;
            OpenMetaverse.Utils.FloatToBytes(wl.cloudScale).CopyTo(mBlock, pos); pos += 4;
            wl.cloudDetailXYDensity.ToBytes(mBlock, pos); pos += 12;
            OpenMetaverse.Utils.FloatToBytes(wl.cloudScrollX).CopyTo(mBlock, pos); pos += 4;
            OpenMetaverse.Utils.FloatToBytes(wl.cloudScrollY).CopyTo(mBlock, pos); pos += 4;
            OpenMetaverse.Utils.UInt16ToBytes(wl.maxAltitude).CopyTo(mBlock, pos); pos += 2;
            mBlock[pos] = Convert.ToByte(wl.cloudScrollXLock); pos++;
            mBlock[pos] = Convert.ToByte(wl.cloudScrollYLock); pos++;
            mBlock[pos] = Convert.ToByte(wl.drawClassicClouds); pos++;
            List<byte[]> param = new List<byte[]>();
            param.Add(mBlock);
            return param;
        }

        void GenerateNewWindlightProfiles(object sender, ElapsedEventArgs e)
        {
            GenerateNewWindlightProfiles();
        }

        public void PostInitialise() { }

        public string Name
        {
            get { return "WeatherModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion
    }

    private class WeatherInRegion
    {
        public bool CurrentlyCloudy = false;
        public bool CurrentlySunny = false;
        public bool NextCloudy = false;
        public bool NextSunny = false;
        public bool CurrentlyFoggy = false;
        public bool NextFoggy = false;
        public bool CurrentRainy = false;
        public bool NextRainy = false;
        //Temperary until I can get the wind from the wind module.
        public Vector2 CurrentWindDirection = Vector2.Zero;
        public Vector2 NextWindDirection = Vector2.Zero;

        public void Randomize()
        {
            Random random = new Random();

            int randomnum = random.Next(1);
            CurrentlyCloudy = randomnum == 1;

            randomnum = random.Next(1);
            CurrentlySunny = randomnum == 1;

            randomnum = random.Next(1);
            NextCloudy = randomnum == 1;

            randomnum = random.Next(1);
            NextSunny = randomnum == 1;

            randomnum = random.Next(1);
            CurrentlyFoggy = randomnum == 1;

            randomnum = random.Next(1);
            NextFoggy = randomnum == 1;

            randomnum = random.Next(1);
            CurrentRainy = randomnum == 1;

            randomnum = random.Next(1);
            NextRainy = randomnum == 1;
        }

        public RegionLightShareData MakeRegionWindLightData(RegionLightShareData RLS)
        {
            #region Sun position and star brightness
            RLS.sunMoonPosition += .1f;
            if (RLS.sunMoonPosition > 1)
                RLS.sunMoonPosition = 0;
            if (RLS.sunMoonPosition > .5f)
                RLS.starBrightness += .5f;
            else
                RLS.starBrightness -= .5f;
            if (RLS.starBrightness > 2)
                RLS.starBrightness = 2;
            if (RLS.starBrightness < 0)
                RLS.starBrightness = 0;

            #endregion

            #region Sunny and clouds

            Random random = new Random();
            if (NextSunny)
            {
                RLS.cloudCoverage -= .05f;
                if (random.Next(0, 1) == 1)
                {
                    RLS.cloudScale += .025f;
                }
                else
                {
                    RLS.cloudScale -= .025f;
                }
            }
            if (CurrentlySunny && !NextSunny)
            {
                RLS.cloudCoverage += .05f;
            }
            if (CurrentlySunny && NextSunny)
            {
                //Clear ambient light changes.
                RLS.ambient.W = .4f;
            }
            
            #endregion

            #region Rain

            if (CurrentRainy)
            {
                RLS.ambient.W -= .1f;
                RLS.sceneGamma -= .05f;
            }
            if (NextSunny && CurrentRainy)
            {
                RLS.ambient.W += .1f;
                RLS.sceneGamma += .05f;
            }
            if (CurrentRainy && NextRainy)
            {
                RLS.ambient.W -= .05f;
            }

            #endregion

            #region Fog

            if (CurrentlyFoggy)
            {
                RLS.densityMultiplier += .1f;
                RLS.distanceMultiplier += 10;
            }
            if (CurrentlyFoggy && !NextFoggy)
            {
                RLS.distanceMultiplier -= 5;
            }
            if (CurrentlyFoggy && NextFoggy)
            {
                RLS.distanceMultiplier += 5;
            }
            if (NextSunny)
            {
                RLS.densityMultiplier -= .1f;
                RLS.distanceMultiplier -= 5;
            }
            if (!CurrentlyFoggy && !NextFoggy && (RLS.densityMultiplier > 0.75 || RLS.distanceMultiplier > 10))
            {
                RLS.densityMultiplier -= .1f;
                RLS.distanceMultiplier -= 5;
            }
            if (RLS.distanceMultiplier > 100)
                RLS.distanceMultiplier = 100;
            if (RLS.distanceMultiplier < 0)
                RLS.distanceMultiplier = 1;
            if (RLS.densityMultiplier > 1)
                RLS.densityMultiplier = 1;
            if (RLS.densityMultiplier < 0.05f)
                RLS.densityMultiplier = 0.05f;

            #endregion

            #region Wind updating
            Vector2 NewWind = CurrentWindDirection - NextWindDirection;
            //Getting less windy in the X direction.
            if (CurrentWindDirection.X > NextWindDirection.X)
            {
                //Little decrease
                if (NewWind.X < 0.1)
                {
                    RLS.cloudScrollX -= 2;
                }
                //Significant decrease
                else if (NewWind.X < 0.25)
                {
                    RLS.cloudScrollX -= 3.5f;
                }
                //Large decrease
                else if (NewWind.X < .5)
                {
                    RLS.cloudScrollX -= 5;
                }
                //Check to make sure values dont get too small.
                if (RLS.cloudScrollX < -10)
                    RLS.cloudScrollX = -10;
            }
            //Its getting windier in the X direction.
            else
            {
                NewWind.X = NewWind.X * -1;
                //Little increase
                if (NewWind.X < 0.1)
                {
                    RLS.cloudScrollX += 2;
                }
                //Significant increase
                else if (NewWind.X < 0.25)
                {
                    RLS.cloudScrollX += 3.5f;
                }
                //Large increase
                else if (NewWind.X < .5)
                {
                    RLS.cloudScrollX += 5;
                }
                //Check to make sure values dont get too big.
                if (RLS.cloudScrollX > 10)
                    RLS.cloudScrollX = 10;
            }

            if (CurrentWindDirection.Y > NextWindDirection.Y)
            {
                //Little decrease
                if (NewWind.Y < 0.1)
                {
                    RLS.cloudScrollY -= 2;
                }
                //Significant decrease
                else if (NewWind.Y < 0.25)
                {
                    RLS.cloudScrollY -= 3.5f;
                }
                //Large decrease
                else if (NewWind.Y < .5)
                {
                    RLS.cloudScrollY -= 5;
                }
                //Check to make sure values dont get too small.
                if (RLS.cloudScrollY < -10)
                    RLS.cloudScrollY = -10;
            }
            //Its getting windier in the Y direction.
            else
            {
                NewWind.Y = NewWind.Y * -1;
                //Little increase
                if (NewWind.Y < 0.1)
                {
                    RLS.cloudScrollY += 2;
                }
                //Significant increase
                else if (NewWind.Y < 0.25)
                {
                    RLS.cloudScrollY += 3.5f;
                }
                //Large increase
                else if (NewWind.Y < .5)
                {
                    RLS.cloudScrollY += 5;
                }
                //Check to make sure values dont get too big.
                if (RLS.cloudScrollY > 10)
                    RLS.cloudScrollY = 10;
            }
            #endregion

            RLS.Save();
            return RLS;
        }
    }
    public enum WeatherType
    {
        Sunny = 0,
        Cloudy = 1,
        Blizzard = 2,
        Dark = 3,
        Realistic = 4,
        AlwaysRandom = 5,
        Sync = 6
    }
    
}
