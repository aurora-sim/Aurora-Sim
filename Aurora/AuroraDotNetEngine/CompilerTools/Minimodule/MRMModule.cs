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
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using log4net;
using Microsoft.CSharp;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Aurora.ScriptEngine.AuroraDotNetEngine;

namespace Aurora.ScriptEngine.AuroraDotNetEngine.MiniModule
{
    public class MRMModule : INonSharedRegionModule, IMRMModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Scene m_scene;
        
        private readonly Dictionary<UUID,MRMBase> m_scripts = new Dictionary<UUID, MRMBase>();

        private readonly Dictionary<Type,object> m_extensions = new Dictionary<Type, object>();

        private static readonly CSharpCodeProvider CScodeProvider = new CSharpCodeProvider();


        private bool m_enabled = false;

        private IConfig m_config;

        public void RegisterExtension<T>(T instance)
        {
            m_extensions[typeof (T)] = instance;
        }

        public void RemoveRegion(Scene scene)
        {
            foreach (KeyValuePair<UUID, MRMBase> pair in m_scripts)
            {
                pair.Value.Stop();
            }
        }

        void EventManager_OnStopScript(uint localID, UUID itemID)
        {
            if (m_scripts.ContainsKey(itemID))
            {
                m_scripts[itemID].Stop();
            }
        }

        void EventManager_OnRezScript (ISceneChildEntity part, UUID itemID, string script, int startParam, bool postOnRez, int stateSource)
        {
                    m_log.Info("[MRM] Unwrapping into target AppDomain");
                    MRMBase mmb = (MRMBase) target.CreateInstanceFromAndUnwrap(
                                                CompileFromDotNetText(script, itemID.ToString()),
                                                "OpenSim.MiniModule");

                    m_log.Info("[MRM] Initialising MRM Globals");
                    InitializeMRM(mmb, part.LocalId, itemID);

                    m_scripts[itemID] = mmb;

                    m_log.Info("[MRM] Starting MRM");
                    mmb.Start();
        }

        public void GetGlobalEnvironment(uint localID, out IWorld world, out IHost host)
        {
            // UUID should be changed to object owner.
            UUID owner = m_scene.RegionInfo.EstateSettings.EstateOwner;
            SEUser securityUser = new SEUser(owner, "Name Unassigned");
            SecurityCredential creds = new SecurityCredential(securityUser, m_scene);

            world = new World(m_scene, creds);
            host = new Host(new SOPObject(m_scene, localID, creds), new ExtensionHandler(m_extensions));
        }

        public void InitializeMRM(MRMBase mmb, uint localID, UUID itemID)
        {

            m_log.Info("[MRM] Created MRM Instance");

            IWorld world;
            IHost host;

            GetGlobalEnvironment(localID, out world, out host);

            mmb.InitMiniModule(world, host, itemID);
        }
    }
}
