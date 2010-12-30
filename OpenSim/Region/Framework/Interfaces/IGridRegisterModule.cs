using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface IGridRegisterModule
    {
        /// <summary>
        /// Update the grid server with new info about this region
        /// </summary>
        /// <param name="scene"></param>
        void UpdateGridRegion(Scene scene);

        /// <summary>
        /// Register this region with the grid service
        /// </summary>
        /// <param name="scene"></param>
        void RegisterRegionWithGrid(Scene scene);
    }
}
