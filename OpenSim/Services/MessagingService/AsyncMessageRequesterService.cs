using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using Aurora.Framework;
using Aurora.Simulation.Base;
using OpenSim.Services.Interfaces;
using Nini.Config;
using OpenMetaverse.StructuredData;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Services.MessagingService
{
    /// <summary>
    /// This module is run on Aurora.exe when it is being run in grid mode as it requests the
    /// AsyncMessagePostService for any async messages that might have been queued to be sent to us
    /// </summary>
    public class AsyncMessageRequesterService : ISharedRegionModule
    {
        #region Declares

        protected List<IScene> m_scenes = new List<IScene>();
        protected volatile bool m_locked = false;
        protected Timer m_timer = null;

        #endregion

        #region IRegionModuleBase Members

        public void Initialise(IConfigSource source)
        {
        }

        public void PostInitialise()
        {
        }

        public void AddRegion(Scene scene)
        {
        }

        public void RegionLoaded(Scene scene)
        {
            IConfig handlerConfig = scene.Config.Configs["Handlers"];
            if (handlerConfig.GetString("AsyncMessageRequesterServiceHandler", "") != Name)
                return;

            m_scenes.Add(scene);

            if (m_timer == null)
            {
                m_timer = new Timer();
                //Start the request timer
                m_timer.Elapsed += requestAsyncMessages;
                m_timer.Interval = 60 * 1000; //60 secs
                m_timer.Start();
            }
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void Close()
        {
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public string Name
        {
            get { return GetType().Name; }
        }

        #endregion

        #region Async Requester

        void requestAsyncMessages(object sender, ElapsedEventArgs e)
        {
            if (m_locked)
                return;
            m_locked = true;
            OSDMap message = CreateWebRequest();
            List<string> serverURIs = m_scenes[0].RequestModuleInterface<IConfigurationService>().FindValueOf(m_scenes[0].RegionInfo.RegionHandle.ToString(), "MessagingServerURI");
            foreach (string host in serverURIs)
            {
                OSDMap retval = WebUtils.PostToService (host, message, true, false);
                //Clean it up
                retval = CreateWebResponse(retval);
                OSD response = retval["Response"];
                if (response is OSDMap)
                {
                    retval = (OSDMap)response;
                    if (retval["Messages"].Type == OSDType.Map)
                    {
                        OSDMap messages = (OSDMap)retval["Messages"];
                        foreach (KeyValuePair<string, OSD> kvp in messages)
                        {
                            OSDArray array = (OSDArray)kvp.Value;
                            IAsyncMessageRecievedService service = GetScene(ulong.Parse(kvp.Key)).RequestModuleInterface<IAsyncMessageRecievedService>();
                            foreach (OSD asyncMessage in array)
                            {
                                service.FireMessageReceived((OSDMap)asyncMessage);
                            }
                        }
                    }
                }
            }
            m_locked = false;
        }

        private IScene GetScene(ulong regionHandle)
        {
            foreach (IScene scene in m_scenes)
            {
                if (scene.RegionInfo.RegionHandle == regionHandle)
                    return scene;
            }
            return null;
        }

        #region Helpers

        private OSDMap CreateWebRequest()
        {
            OSDMap message = new OSDMap();
            message["Method"] = "AsyncMessageRequest";
            OSDMap request = new OSDMap();
            request["Method"] = "AsyncMessageRequest";
            OSDArray array = new OSDArray();
            foreach (IScene scene in m_scenes)
            {
                array.Add(scene.RegionInfo.RegionHandle);
                array.Add(scene.RegionInfo.GridSecureSessionID);
            }
            request["RegionHandles"] = array;
            message["Message"] = request;
            return message;
        }

        private OSDMap CreateWebResponse(OSDMap request)
        {
            OSDMap message = new OSDMap();
            message["Response"] = OSDParser.DeserializeJson(request["_RawResult"]);
            return message;
        }

        #endregion

        #endregion
    }
}
