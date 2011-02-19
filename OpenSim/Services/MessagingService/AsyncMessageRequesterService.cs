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
    public class AsyncMessageRequesterService : INonSharedRegionModule
    {
        #region Declares

        protected List<string> m_hosts = new List<string>();
        protected IScene m_scene;

        #endregion

        #region IRegionModuleBase Members

        public void Initialise(IConfigSource source)
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

            m_hosts = scene.RequestModuleInterface<IConfigurationService>().FindValueOf("MessagingServerURI");
            m_scene = scene;

            //Start the request timer
            Timer timer = new Timer();
            timer.Elapsed += requestAsyncMessages;
            timer.Interval = 30 * 1000; //30 secs
            timer.Start();
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
            OSDMap message = CreateWebRequest();
            IAsyncMessageRecievedService service = m_scene.RequestModuleInterface<IAsyncMessageRecievedService>();
            foreach (string host in m_hosts)
            {
                OSDMap retval = WebUtils.PostToService(host, message);
                //Clean it up
                retval = CreateWebResponse(retval);

                OSDArray messages = (OSDArray)retval["Messages"];
                foreach (OSD asyncMessage in messages)
                {
                    service.FireMessageReceived((OSDMap)asyncMessage);
                }
            }
        }

        #region Helpers

        private OSDMap CreateWebRequest()
        {
            OSDMap message = new OSDMap();
            message["Method"] = "AsyncMessageRequest";
            OSDMap request = new OSDMap();
            request["Method"] = "AsyncMessageRequest";
            request["RegionHandle"] = m_scene.RegionInfo.RegionHandle;
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
