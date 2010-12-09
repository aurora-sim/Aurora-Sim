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
            if (PhysConfig != null)
            {
                engine = PhysConfig.GetString("DefaultPhysicsEngine", "OpenDynamicsEngine");
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
                engine = "OpenDynamicsEngine";
                meshEngine = "Meshmerizer";
            }
            PhysicsPluginManager physicsPluginManager = new PhysicsPluginManager();
            physicsPluginManager.LoadPluginsFromAssemblies("Physics");

            PhysicsScene pScene = physicsPluginManager.GetPhysicsScene(engine, meshEngine, source, scene.RegionInfo.RegionName);
            scene.PhysicsScene = pScene;
        }

        public void PostInitialise(Scene scene, IConfigSource source, ISimulationBase openSimBase)
        {
        }
    }
}
