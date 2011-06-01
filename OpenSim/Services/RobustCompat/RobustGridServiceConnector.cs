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

using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using Aurora.Simulation.Base;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Services.Connectors
{
    public class RobustGridServicesConnector : GridServicesConnector
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        #region IGridService

        public override GridRegion GetRegionByUUID (UUID scopeID, UUID regionID)
        {
            return FixGridRegion (base.GetRegionByUUID (scopeID, regionID));
        }

        public override GridRegion GetRegionByPosition (UUID scopeID, int x, int y)
        {
            return FixGridRegion (base.GetRegionByPosition (scopeID, x, y));
        }

        public override GridRegion GetRegionByName (UUID scopeID, string regionName)
        {
            return FixGridRegion (base.GetRegionByName (scopeID, regionName));
        }

        public override List<GridRegion> GetRegionsByName (UUID scopeID, string name, int maxNumber)
        {
            return FixGridRegions (base.GetRegionsByName (scopeID, name, maxNumber));
        }

        public override List<GridRegion> GetRegionRange (UUID scopeID, int xmin, int xmax, int ymin, int ymax)
        {
            return FixGridRegions (base.GetRegionRange (scopeID, xmin, xmax, ymin, ymax));
        }

        #endregion

        private GridRegion FixGridRegion (GridRegion gridRegion)
        {
            SceneManager manager = m_registry.RequestModuleInterface<SceneManager> ();
            if (manager != null)
            {
                foreach (Scene scene in manager.Scenes)
                {
                    if (scene.RegionInfo.RegionID == gridRegion.RegionID)
                    {
                        gridRegion.RegionSizeX = scene.RegionInfo.RegionSizeX;
                        gridRegion.RegionSizeY = scene.RegionInfo.RegionSizeY;
                        return gridRegion;
                    }
                }
            }
            return gridRegion;
        }

        private List<GridRegion> FixGridRegions (List<GridRegion> list)
        {
            List<GridRegion> rs = new List<GridRegion> ();
            foreach (GridRegion r in list)
            {
                rs.Add (FixGridRegion(r));
            }
            return rs;
        }

        #region IService Members

        public override string Name
        {
            get { return GetType().Name; }
        }

        public override void Initialize (IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("GridHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IGridService>(this);
        }

        #endregion
    }
}
