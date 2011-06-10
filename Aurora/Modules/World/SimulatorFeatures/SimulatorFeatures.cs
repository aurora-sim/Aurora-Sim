using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework;
using OpenSim.Framework.Capabilities;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;

namespace Aurora.Modules.World
{
    public class SimulatorFeatures : ISharedRegionModule
    {
        public void PostInitialise ()
        {
        }

        public void Initialise (IConfigSource source)
        {
        }

        public void AddRegion (Scene scene)
        {
            scene.EventManager.OnRegisterCaps += RegisterCaps;
            scene.EventManager.OnDeregisterCaps += new EventManager.DeregisterCapsEvent (EventManager_OnDeregisterCaps);
        }

        void EventManager_OnDeregisterCaps (UUID agentID, IRegionClientCapsService caps)
        {
        }

        public OSDMap RegisterCaps (UUID agentID, IHttpServer server)
        {
            OSDMap retVal = new OSDMap ();
            retVal["SimulatorFeatures"] = CapsUtil.CreateCAPS ("SimulatorFeatures", "");

            server.AddStreamHandler (new RestHTTPHandler ("Get", retVal["SimulatorFeatures"],
                                                      delegate (Hashtable m_dhttpMethod)
                                                      {
                                                          return SimulatorFeatures (m_dhttpMethod);
                                                      }));
            return retVal;
        }

        private Hashtable SimulatorFeatures (Hashtable mDhttpMethod)
        {
            //OSDMap rm = (OSDMap)OSDParser.DeserializeLLSDXml((string)mDhttpMethod["requestbody"]);


            OSDMap data = new OSDMap ();
            data["MeshRezEnabled"] = true;
            data["MeshUploadEnabled"] = true;
            data["MeshXferEnabled"] = true;
            data["PhysicsMaterialsEnabled"] = true;
            OSDMap typesMap = new OSDMap ();

            typesMap["convex"] = true;
            typesMap["none"] = true;
            typesMap["prim"] = true;

            data["PhysicsShapeTypes"] = typesMap;


            //Send back data
            Hashtable responsedata = new Hashtable ();
            responsedata["int_response_code"] = 200; //501; //410; //404;
            responsedata["content_type"] = "text/plain";
            responsedata["keepalive"] = false;
            responsedata["str_response_string"] = OSDParser.SerializeLLSDXmlString(data);
            return responsedata;
        }

        public void RegionLoaded (Scene scene)
        {
        }

        public void RemoveRegion (Scene scene)
        {
        }

        public void Close ()
        {
        }

        public string Name
        {
            get
            {
                return "SimulatorFeatures";
            }
        }

        public Type ReplaceableInterface
        {
            get
            {
                return null;
            }
        }
    }
}
