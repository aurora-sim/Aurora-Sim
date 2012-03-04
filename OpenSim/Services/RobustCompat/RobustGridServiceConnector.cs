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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nini.Config;
using OpenMetaverse;
using Aurora.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Connectors;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Services.RobustCompat
{
    /*public class RobustGridServicesConnector : GridServicesConnector
    {
        #region IGridService

        public override GridRegion GetRegionByUUID(UUID scopeID, UUID regionID)
        {
            return FixGridRegion(base.GetRegionByUUID(scopeID, regionID));
        }

        public override GridRegion GetRegionByPosition(UUID scopeID, int x, int y)
        {
            return FixGridRegion(base.GetRegionByPosition(scopeID, x, y));
        }

        public override GridRegion GetRegionByName(UUID scopeID, string regionName)
        {
            return FixGridRegion(base.GetRegionByName(scopeID, regionName));
        }

        public override List<GridRegion> GetRegionsByName(UUID scopeID, string name, int maxNumber)
        {
            return FixGridRegions(base.GetRegionsByName(scopeID, name, maxNumber));
        }

        public override List<GridRegion> GetRegionRange(UUID scopeID, int xmin, int xmax, int ymin, int ymax)
        {
            return FixGridRegions(base.GetRegionRange(scopeID, xmin, xmax, ymin, ymax));
        }

        #endregion

        public override string Name
        {
            get { return GetType().Name; }
        }

        private GridRegion FixGridRegion(GridRegion gridRegion)
        {
            if (gridRegion == null)
                return null;
            SceneManager manager = m_registry.RequestModuleInterface<SceneManager>();
            if (manager != null)
            {
#if (!ISWIN)
                foreach (IScene scene in manager.Scenes)
                {
                    if (scene.RegionInfo.RegionID == gridRegion.RegionID)
                    {
                        gridRegion.RegionSizeX = scene.RegionInfo.RegionSizeX;
                        gridRegion.RegionSizeY = scene.RegionInfo.RegionSizeY;
                        return gridRegion;
                    }
                }
#else
                foreach (IScene scene in manager.Scenes.Where(scene => scene.RegionInfo.RegionID == gridRegion.RegionID))
                {
                    gridRegion.RegionSizeX = scene.RegionInfo.RegionSizeX;
                    gridRegion.RegionSizeY = scene.RegionInfo.RegionSizeY;
                    return gridRegion;
                }
#endif
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

        public override void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_registry = registry;
            IConfig handlerConfig = config.Configs["Handlers"];
            if (handlerConfig.GetString("GridHandler", "") != Name)
                return;

            registry.RegisterModuleInterface<IGridService>(this);
        }
    }*/
}