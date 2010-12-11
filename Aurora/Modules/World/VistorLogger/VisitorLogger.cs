using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Timers;
using OpenSim.Framework;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Nini.Config;
using OpenMetaverse;

namespace Aurora.Modules
{
    /// <summary>
    /// This module logs all visitors to the sim to a specified file
    /// </summary>
    public class VisitorLoggerModule : ISharedRegionModule
    {
        #region Declares

        protected bool m_enabled = false;
        protected string m_fileName = "Vistors.log";
        protected Dictionary<UUID, DateTime> m_timesOfUsers = new Dictionary<UUID, DateTime>();

        #endregion

        #region ISharedRegionModule

        public void Initialise(IConfigSource source)
        {
            IConfig config = source.Configs["VisitorLogModule"];
            if (config != null)
            {
                m_enabled = config.GetBoolean("Enabled", m_enabled);
                m_fileName = config.GetString("FileName", m_fileName);
            }
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            if (m_enabled)
            {
                scene.EventManager.OnMakeRootAgent += OnMakeRootAgent;
                scene.EventManager.OnClosingClient += EventManager_OnClosingClient;
            }
        }

        void EventManager_OnClosingClient(IClientAPI client)
        {
            IScenePresence presence;
            if (client.Scene.TryGetScenePresence(client.AgentId, out presence) && !presence.IsChildAgent)
            {
                try
                {
                    //Add the user
                    FileStream stream = new FileStream(m_fileName, FileMode.OpenOrCreate);
                    StreamWriter m_streamWriter = new StreamWriter(stream);
                    m_streamWriter.BaseStream.Position += m_streamWriter.BaseStream.Length;
                    
                    string LineToWrite = DateTime.Now.ToLongTimeString() + " - " + client.Name + " left " + client.Scene.RegionInfo.RegionName + " after " + (DateTime.Now - m_timesOfUsers[client.AgentId]).Minutes + " minutes.";
                    m_timesOfUsers.Remove(presence.UUID);

                    m_streamWriter.WriteLine(LineToWrite);
                    m_streamWriter.WriteLine();
                    m_streamWriter.Close();
                }
                catch { }
            }
        }

        void OnMakeRootAgent(ScenePresence presence)
        {
            try
            {
                //Add the user
                FileStream stream = new FileStream(m_fileName, FileMode.OpenOrCreate);
                StreamWriter m_streamWriter = new StreamWriter(stream);
                m_streamWriter.BaseStream.Position += m_streamWriter.BaseStream.Length;
                
                string LineToWrite = DateTime.Now.ToLongTimeString() + " - " + presence.Name + " entered " + presence.Scene.RegionInfo.RegionName + ".";
                m_timesOfUsers[presence.UUID] = DateTime.Now;

                m_streamWriter.WriteLine(LineToWrite);
                m_streamWriter.WriteLine();
                m_streamWriter.Close();
            }
            catch { }
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public string Name
        {
            get { return "VisitorLoggerModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion
    }
}
