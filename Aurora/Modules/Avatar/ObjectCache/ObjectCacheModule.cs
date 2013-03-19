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
using System.Collections.Generic;
using System.IO;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;

namespace Aurora.Modules.ObjectCache
{
    public class ObjectCacheModule : INonSharedRegionModule, IObjectCache
    {
        #region Declares

        //private static readonly ILog MainConsole.Instance = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Dictionary<UUID, Dictionary<uint, uint>> ObjectCacheAgents =
            new Dictionary<UUID, Dictionary<uint, uint>>();

        protected bool m_Enabled = true;

        private string m_filePath = "ObjectCache/";
        private IScene m_scene;

        #endregion

        #region INonSharedRegionModule

        public virtual void Initialise(IConfigSource source)
        {
            IConfig moduleConfig = source.Configs["ObjectCache"];
            if (moduleConfig != null)
            {
                m_Enabled = moduleConfig.GetString("Module", "") == Name;
                m_filePath = moduleConfig.GetString("PathToSaveFiles", m_filePath);
            }
            if (!Directory.Exists(m_filePath))
            {
                try
                {
                    Directory.CreateDirectory(m_filePath);
                }
                catch (Exception)
                {
                }
            }
            m_Enabled = false;
        }

        public virtual void AddRegion(IScene scene)
        {
            if (!m_Enabled)
                return;
            m_scene = scene;
            scene.RegisterModuleInterface<IObjectCache>(this);
            scene.EventManager.OnNewClient += OnNewClient;
            scene.EventManager.OnClosingClient += OnClosingClient;
        }

        public virtual void RemoveRegion(IScene scene)
        {
            if (!m_Enabled)
                return;

            scene.UnregisterModuleInterface<IObjectCache>(this);
            scene.EventManager.OnNewClient -= OnNewClient;
            scene.EventManager.OnClosingClient -= OnClosingClient;
        }

        public virtual void RegionLoaded(IScene scene)
        {
        }

        public virtual void Close()
        {
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public virtual string Name
        {
            get { return "ObjectCacheModule"; }
        }

        #region Events

        public void OnNewClient(IClientAPI client)
        {
            IScenePresence sp;
            client.Scene.TryGetScenePresence(client.AgentId, out sp);
            //Create the client's cache
            //This is shared, so all get saved into one file
            if (sp != null && !sp.IsChildAgent)
            {
                Util.FireAndForget(LoadFileOnNewClient, sp.UUID);
            }
        }

        /// <summary>
        ///     Load the file for the client async so that we don't lock up the system for too long
        /// </summary>
        /// <param name="o"></param>
        public void LoadFileOnNewClient(object o)
        {
            UUID agentID = (UUID) o;
            LoadFromFileForClient(agentID);
        }

        public void OnClosingClient(IClientAPI client)
        {
            //Save the cache to the file for the client
            IScenePresence sp;
            client.Scene.TryGetScenePresence(client.AgentId, out sp);
            //This is shared, so all get saved into one file
            if (sp != null && !sp.IsChildAgent)
                SaveToFileForClient(client.AgentId);
            //Remove the client's cache
            lock (ObjectCacheAgents)
            {
                ObjectCacheAgents.Remove(client.AgentId);
            }
        }

        #endregion

        #region Serialization

        public string SerializeAgentCache(Dictionary<uint, uint> cache)
        {
            OSDMap cachedMap = new OSDMap();
            foreach (KeyValuePair<uint, uint> kvp in cache)
            {
                cachedMap.Add(kvp.Key.ToString(), OSD.FromUInteger(kvp.Value));
            }
            return OSDParser.SerializeJsonString(cachedMap);
        }

        public Dictionary<uint, uint> DeserializeAgentCache(string osdMap)
        {
            Dictionary<uint, uint> cache = new Dictionary<uint, uint>();
            try
            {
                OSDMap cachedMap = (OSDMap) OSDParser.DeserializeJson(osdMap);
                foreach (KeyValuePair<string, OSD> kvp in cachedMap)
                {
                    cache[uint.Parse(kvp.Key)] = kvp.Value.AsUInteger();
                }
            }
            catch
            {
                //It has an error, destroy the cache
                //null will tell the caller that it errored out and needs to be removed
                cache = null;
            }
            return cache;
        }

        #endregion

        #region Load/Save from file

        public void SaveToFileForClient(UUID AgentID)
        {
            Dictionary<uint, uint> cache;
            lock (ObjectCacheAgents)
            {
                if (!ObjectCacheAgents.ContainsKey(AgentID))
                    return;
                cache = new Dictionary<uint, uint>(ObjectCacheAgents[AgentID]);
                ObjectCacheAgents[AgentID].Clear();
                ObjectCacheAgents.Remove(AgentID);
            }
            FileStream stream = new FileStream(m_filePath + AgentID + m_scene.RegionInfo.RegionName + ".oc",
                                               FileMode.Create);
            StreamWriter m_streamWriter = new StreamWriter(stream);
            m_streamWriter.WriteLine(SerializeAgentCache(cache));
            m_streamWriter.Close();
        }

        public void LoadFromFileForClient(UUID AgentID)
        {
            FileStream stream = new FileStream(m_filePath + AgentID + m_scene.RegionInfo.RegionName + ".oc",
                                               FileMode.OpenOrCreate);
            StreamReader m_streamReader = new StreamReader(stream);
            string file = m_streamReader.ReadToEnd();
            m_streamReader.Close();
            //Read file here
            if (file != "") //New file
            {
                Dictionary<uint, uint> cache = DeserializeAgentCache(file);
                if (cache == null)
                {
                    //Something went wrong, delete the file
                    try
                    {
                        File.Delete(m_filePath + AgentID + m_scene.RegionInfo.RegionName + ".oc");
                    }
                    catch
                    {
                    }
                    return;
                }
                lock (ObjectCacheAgents)
                {
                    ObjectCacheAgents[AgentID] = cache;
                }
            }
        }

        #endregion

        public virtual void PostInitialise()
        {
        }

        #endregion

        #region IObjectCache

        /// <summary>
        ///     Check whether we can send a CachedObjectUpdate to the client
        /// </summary>
        /// <param name="AgentID"></param>
        /// <param name="localID"></param>
        /// <param name="CurrentEntityCRC"></param>
        /// <returns></returns>
        public bool UseCachedObject(UUID AgentID, uint localID, uint CurrentEntityCRC)
        {
            lock (ObjectCacheAgents)
            {
                if (ObjectCacheAgents.ContainsKey(AgentID))
                {
                    uint CurrentCachedCRC = 0;
                    if (ObjectCacheAgents[AgentID].TryGetValue(localID, out CurrentCachedCRC))
                    {
                        if (CurrentEntityCRC == CurrentCachedCRC)
                        {
                            //The client knows of the newest version
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public void AddCachedObject(UUID AgentID, uint localID, uint CRC)
        {
            lock (ObjectCacheAgents)
            {
                if (!ObjectCacheAgents.ContainsKey(AgentID))
                    ObjectCacheAgents[AgentID] = new Dictionary<uint, uint>();
                ObjectCacheAgents[AgentID][localID] = CRC;
            }
        }

        public void RemoveObject(UUID AgentID, uint localID, byte cacheMissType)
        {
            lock (ObjectCacheAgents)
            {
                if (ObjectCacheAgents.ContainsKey(AgentID))
                    ObjectCacheAgents[AgentID].Remove(localID);
            }
        }

        #endregion
    }
}