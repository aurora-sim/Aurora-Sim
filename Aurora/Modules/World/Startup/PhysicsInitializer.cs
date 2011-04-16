using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Physics.Manager;
using Nini.Config;

namespace OpenSim.Region.CoreModules
{
    public class PhysicsInitializer : ISharedRegionStartupModule
    {
        public void Initialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
            IConfig PhysConfig = source.Configs["Physics"];
            IConfig MeshingConfig = source.Configs["Meshing"];
            string engine = "";
            string meshEngine = "";
            string Path = "Physics";
            if (PhysConfig != null)
            {
                Path = PhysConfig.GetString("PathToPhysicsAssemblies", Path);
                engine = PhysConfig.GetString("DefaultPhysicsEngine", "AuroraOpenDynamicsEngine");
                meshEngine = MeshingConfig.GetString("DefaultMeshingEngine", "Meshmerizer");
                string regionName = scene.RegionInfo.RegionName.Trim().Replace(' ', '_');
                string RegionPhysicsEngine = PhysConfig.GetString("Region_" + regionName + "_PhysicsEngine", String.Empty);
                if (RegionPhysicsEngine != "")
                    engine = RegionPhysicsEngine;
                string RegionMeshingEngine = MeshingConfig.GetString("Region_" + regionName + "_MeshingEngine", String.Empty);
                if (RegionMeshingEngine != "")
                    meshEngine = RegionMeshingEngine;
            }
            else
            {
                //Load Sane defaults
                engine = "AuroraOpenDynamicsEngine";
                meshEngine = "Meshmerizer";
            }
            PhysicsPluginManager physicsPluginManager = new PhysicsPluginManager();
            physicsPluginManager.LoadPluginsFromAssemblies(Util.BasePathCombine(Path));

            PhysicsScene pScene = physicsPluginManager.GetPhysicsScene(engine, meshEngine, source, scene.RegionInfo);
            scene.PhysicsScene = pScene;
        }

        public void PostInitialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void FinishStartup(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void PostFinishStartup(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }

        public void StartupComplete()
        {
        }

        public void Close(Scene scene)
        {
        }
    }
}
