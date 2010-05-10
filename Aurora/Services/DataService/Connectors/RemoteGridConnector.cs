using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using log4net;
using System.IO;
using System.Reflection;
using Nini.Config;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenSim.Server.Base;

namespace Aurora.Services.DataService
{
    public class RemoteGridConnector : IGridConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private string m_ServerURI = "";

        public RemoteGridConnector(string serverURI)
        {
            m_ServerURI = serverURI;
        }

        #region IGridConnector Members

        public GridRegionFlags GetRegionFlags(UUID regionID)
        {
            throw new NotImplementedException();
        }

        public void SetRegionFlags(UUID regionID, GridRegionFlags flags)
        {
            throw new NotImplementedException();
        }

        public void CreateRegion(UUID regionID)
        {
            throw new NotImplementedException();
        }

        public void AddTelehub(UUID regionID, Vector3 position, int regionPosX, int regionPosY)
        {
            throw new NotImplementedException();
        }

        public void RemoveTelehub(UUID regionID)
        {
            throw new NotImplementedException();
        }

        public bool FindTelehub(UUID regionID, out Vector3 position)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
