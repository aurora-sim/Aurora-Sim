using System;
using OpenMetaverse.StructuredData;
using OpenMetaverse;
using OpenSim.Framework;

namespace OpenSim.Region.Framework.Scenes.Components
{
    /// <summary>
    /// This interface deals with setting up Components and hooking up to the serialization process
    /// </summary>
    public interface IComponentManager
    {
        /// <summary>
        /// Register a new Component base with the manager.
        /// This hooks the Component up to serialization and deserialization and also allows it to be pulled from IComponents[] in the SceneObjectPart.
        /// </summary>
        /// <param name="component"></param>
        void RegisterComponent(IComponent component);

        /// <summary>
        /// Remove a known Component from the manager.
        /// </summary>
        /// <param name="component"></param>
        void DeregisterComponent(IComponent component);

        /// <summary>
        /// Get all known registered Components
        /// </summary>
        /// <returns></returns>
        IComponent[] GetComponents();

        /// <summary>
        /// Get the State of a Component with the given name
        /// </summary>
        /// <param name="obj">The object being checked</param>
        /// <param name="Name">Name of the Component</param>
        /// <returns>The State of the Component</returns>
        OSD GetComponentState (ISceneChildEntity obj, string Name);

        /// <summary>
        /// Set the State of the Component with the given name
        /// </summary>
        /// <param name="obj">The object to update</param>
        /// <param name="Name">Name of the Component</param>
        /// <param name="State">State to set the Component to</param>
        void SetComponentState (ISceneChildEntity obj, string Name, OSD State);

        /// <summary>
        /// Take the serialized string and set up the Components for this object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="serialized"></param>
        void DeserializeComponents (ISceneChildEntity obj, string serialized);

        /// <summary>
        /// Serialize all the registered Components into a string to be saved later
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>The serialized string</returns>
        string SerializeComponents (ISceneChildEntity obj);

        /// <summary>
        /// Changes the UUIDs of one object to another
        /// </summary>
        /// <param name="oldID"></param>
        /// <param name="part"></param>
        void ResetComponentIDsToNewObject (UUID oldID, ISceneChildEntity part);

        /// <summary>
        /// Remove the component for the given object with the given name, resets it to null
        /// </summary>
        /// <param name="UUID"></param>
        /// <param name="name"></param>
        void RemoveComponentState (UUID UUID, string name);
    }
}