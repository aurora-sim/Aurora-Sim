using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Framework;

namespace OpenSim.Framework
{
    public interface IGridRegisterModule
    {
        /// <summary>
        /// Update the grid server with new info about this region
        /// </summary>
        /// <param name="scene"></param>
        void UpdateGridRegion(IScene scene);

        /// <summary>
        /// Register this region with the grid service
        /// </summary>
        /// <param name="scene"></param>
        void RegisterRegionWithGrid(IScene scene);

        /// <summary>
        /// Add this generic info to all registering regions
        /// </summary>
        /// <param name="p"></param>
        /// <param name="path"></param>
        void AddGenericInfo(string key, string value);
    }
}
