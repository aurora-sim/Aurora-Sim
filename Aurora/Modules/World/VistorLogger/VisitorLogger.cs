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

namespace Aurora.Modules
{
    public class VisitorLoggerModule : INonSharedRegionModule
    {
        #region Declares

        public bool m_enabled = false;
        public string m_fileName = "Vistors.log";

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

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            if(m_enabled)
                scene.EventManager.OnMakeRootAgent += new EventManager.OnMakeRootAgentDelegate(EventManager_OnMakeRootAgent);
        }

        void EventManager_OnMakeRootAgent(ScenePresence presence)
        {
            try
            {
                FileStream stream = new FileStream(m_fileName, FileMode.OpenOrCreate);
                StreamWriter m_streamWriter = new StreamWriter(stream);
                m_streamWriter.BaseStream.Position += m_streamWriter.BaseStream.Length;
                m_streamWriter.WriteLine(presence.Name);
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
