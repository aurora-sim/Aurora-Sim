using System;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Region.Framework.Scenes.Components
{
    public interface IComponent
    {
        /// <summary>
        /// The type of the Component, only one of each 'type' can be loaded.
        /// </summary>
        Type BaseType { get; }

        /// <summary>
        /// Name of this Component
        /// </summary>
        string Name { get; }

        /// <summary>
        /// A representation of the current state of the Component
        /// </summary>
        /// <param name="obj">The object to get the value from</param>
        /// <returns></returns>
        OSD GetState(UUID obj);

        /// <summary>
        /// Update the state of the Component
        /// </summary>
        /// <param name="obj">The object being edited</param>
        /// <param name="osd">The value as an OSD</param>
        void SetState(UUID obj, OSD osd);
    }
}