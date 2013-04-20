using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.Modules;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Aurora.Modules
{
    public class InworldRestartSerializer : INonSharedRegionModule
    {
        private IScene m_scene;
        private string m_fileName = "sceneagents";
        private string m_storeDirectory = "";

        public string Name
        {
            get { return "InworldRestartSerializer"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(IConfigSource source)
        {
            IConfig config = source.Configs["FileBasedSimulationData"];
            if (config != null)
                m_storeDirectory = PathHelpers.ComputeFullPath(config.GetString("StoreBackupDirectory", m_storeDirectory));
            config = source.Configs["Startup"];
            if (config != null)
                m_fileName = config.GetString("RegionDataFileName", m_fileName);
            MainConsole.Instance.Commands.AddCommand("quit serialized", "quit serialized", "Closes the scene and saves all agents", quitSerialized);
        }

        public void AddRegion(IScene scene)
        {
        }

        public void RegionLoaded(IScene scene)
        {
            m_scene = scene;
            m_scene.EventManager.OnStartupFullyComplete += EventManager_OnStartupFullyComplete;
        }

        void EventManager_OnStartupFullyComplete(IScene scene, List<string> data)
        {
            DeserializeUsers();
        }

        public void RemoveRegion(IScene scene)
        {
        }

        public void Close()
        {
        }

        private void quitSerialized(string[] args)
        {
            SerializeUsers();
            m_scene.CloseQuietly = true;
            m_scene.Close(false);

            m_scene.RequestModuleInterface<ISimulationBase>().Shutdown(true);
        }

        private void SerializeUsers()
        {
            OSDMap userMap = new OSDMap();
            foreach (IScenePresence presence in m_scene.GetScenePresences())
            {
                OSDMap user = new OSDMap();
                OSDMap remoteIP = new OSDMap();
                remoteIP["Address"] = presence.ControllingClient.RemoteEndPoint.Address.ToString();
                remoteIP["Port"] = presence.ControllingClient.RemoteEndPoint.Port;
                user["RemoteEndPoint"] = remoteIP;
                user["ClientInfo"] = presence.ControllingClient.RequestClientInfo().ToOSD();
                user["Position"] = presence.AbsolutePosition;
                user["IsFlying"] = presence.PhysicsActor.Flying;

                userMap[presence.UUID.ToString()] = user;
            }


            File.WriteAllText(BuildSaveFileName(), OSDParser.SerializeJsonString(userMap));
        }

        private void DeserializeUsers()
        {
            if (!File.Exists(BuildSaveFileName()))
                return;
            foreach (OSD o in ((OSDMap)OSDParser.DeserializeJson(File.ReadAllText(BuildSaveFileName()))).Values)
            {
                AgentCircuitData data = new AgentCircuitData();
                OSDMap user = (OSDMap)o;
                data.FromOSD((OSDMap)user["ClientInfo"]);
                m_scene.AuthenticateHandler.AddNewCircuit(data.CircuitCode, data);
                OSDMap remoteIP = (OSDMap)user["RemoteEndPoint"];
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(remoteIP["Address"].AsString()), remoteIP["Port"].AsInteger());
                m_scene.ClientServers[0].AddClient(data.CircuitCode, data.AgentID, data.SessionID, ep, data);
                IScenePresence sp = m_scene.GetScenePresence(data.AgentID);
                sp.MakeRootAgent(user["Position"].AsVector3(), user["IsFlying"].AsBoolean(), true);
                sp.SceneViewer.SendPresenceFullUpdate(sp);
            }

            File.Delete(BuildSaveFileName());
        }

        private string BuildSaveFileName()
        {
            return (m_storeDirectory == "" || m_storeDirectory == "/")
                       ? m_fileName + ".siminfo"
                       : Path.Combine(m_storeDirectory, m_fileName + ".siminfo");
        }
    }
}
