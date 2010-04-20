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
        private List<Scene> Scenes = new List<Scene>();
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
            Scenes.Add(scene);
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
                    foreach (Scene scene in Scenes)
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
            if (CurrentWeather == WeatherType.AlwaysRandom)
            {
                
            }
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

        public void MakeRegionWindLightData(Scene scene, RegionLightShareData RLS)
        {
            Random random = new Random();
            if (NextSunny)
            {
                RLS.cloudCoverage -= .05f;
                if (random.Next(0, 1) == 1)
                    RLS.cloudScale += .025f;
                else
                    RLS.cloudScale -= .025f;
            }
            if (CurrentlySunny && !NextSunny)
            {
                RLS.cloudCoverage -= .05f;
            }

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
