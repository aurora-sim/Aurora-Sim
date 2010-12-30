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
using Nini.Config;
using log4net;
using Aurora.Framework;

namespace OpenSim.Region.Physics.Manager
{
    /// <summary>
    /// Description of MyClass.
    /// </summary>
    public class PhysicsPluginManager
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Dictionary<string, IPhysicsPlugin> _PhysPlugins = new Dictionary<string, IPhysicsPlugin>();
        private Dictionary<string, IMeshingPlugin> _MeshPlugins = new Dictionary<string, IMeshingPlugin>();

        /// <summary>
        /// Constructor.
        /// </summary>
        public PhysicsPluginManager()
        {
            // Load "plugins", that are hard coded and not existing in form of an external lib, and hence always 
            // available
            IMeshingPlugin plugHard;
            plugHard = new ZeroMesherPlugin();
            _MeshPlugins.Add(plugHard.GetName(), plugHard);
            
           // m_log.Info("[PHYSICS]: Added meshing engine: " + plugHard.GetName());
        }

        /// <summary>
        /// Get a physics scene for the given physics engine and mesher.
        /// </summary>
        /// <param name="physEngineName"></param>
        /// <param name="meshEngineName"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public PhysicsScene GetPhysicsScene(string physEngineName, string meshEngineName, IConfigSource config, string regionName)
        {
            if (String.IsNullOrEmpty(physEngineName))
            {
                return PhysicsScene.Null;
            }

            if (String.IsNullOrEmpty(meshEngineName))
            {
                return PhysicsScene.Null;
            }

            IMesher meshEngine = null;
            if (_MeshPlugins.ContainsKey(meshEngineName))
            {
                m_log.Info("[PHYSICS]: Creating meshing engine " + meshEngineName);
                meshEngine = _MeshPlugins[meshEngineName].GetMesher(config);
            }
            else
            {
                m_log.WarnFormat("[PHYSICS]: Couldn't find meshing engine: {0}", meshEngineName);
                throw new ArgumentException(String.Format("couldn't find meshing engine: {0}", meshEngineName));
            }

            if (_PhysPlugins.ContainsKey(physEngineName))
            {
                m_log.Info("[PHYSICS]: Creating physics engine " + physEngineName);
                PhysicsScene result = _PhysPlugins[physEngineName].GetScene(regionName);
                result.Initialise(meshEngine);
                result.PostInitialise(config);
                return result;
            }
            else
            {
                m_log.WarnFormat("[PHYSICS]: couldn't find physics engine: {0}", physEngineName);
                throw new ArgumentException(String.Format("couldn't find physics engine: {0}", physEngineName));
            }
        }

        /// <summary>
        /// Load all plugins in assemblies at the given path
        /// </summary>
        /// <param name="pluginsPath"></param>
        public void LoadPluginsFromAssemblies(string assembliesPath)
        {
            List<IPhysicsPlugin> physicsPlugins = AuroraModuleLoader.LoadModules<IPhysicsPlugin>(assembliesPath);
            List<IMeshingPlugin> meshingPlugins = AuroraModuleLoader.LoadModules<IMeshingPlugin>(assembliesPath);

            foreach (IPhysicsPlugin plug in physicsPlugins)
            {
                _PhysPlugins.Add(plug.GetName(), plug);
            }
            foreach (IMeshingPlugin plug in meshingPlugins)
            {
                _MeshPlugins.Add(plug.GetName(), plug);
            }
            
            /*// Walk all assemblies (DLLs effectively) and see if they are home
            // of a plugin that is of interest for us
            string[] pluginFiles = Directory.GetFiles(assembliesPath, "*.dll");

            for (int i = 0; i < pluginFiles.Length; i++)
            {
                LoadPluginsFromAssembly(pluginFiles[i]);
            }*/
        }

        //---
        public static void PhysicsPluginMessage(string message, bool isWarning)
        {
            if (isWarning)
            {
                m_log.Warn("[PHYSICS]: " + message);
            }
            else
            {
                m_log.Info("[PHYSICS]: " + message);
            }
        }

        //---
    }

    public interface IPhysicsPlugin
    {
        bool Init();
        PhysicsScene GetScene(String sceneIdentifier);
        string GetName();
        void Dispose();
    }

    public interface IMeshingPlugin
    {
        string GetName();
        IMesher GetMesher(IConfigSource config);
    }
}
