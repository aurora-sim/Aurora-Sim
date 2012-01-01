/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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

using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;

namespace OpenSim.Region.Framework.Scenes.Components
{
    /// <summary>
    ///   This interface deals with setting up Components and hooking up to the serialization process
    /// </summary>
    public interface IComponentManager
    {
        /// <summary>
        ///   Register a new Component base with the manager.
        ///   This hooks the Component up to serialization and deserialization and also allows it to be pulled from IComponents[] in the SceneObjectPart.
        /// </summary>
        /// <param name = "component"></param>
        void RegisterComponent(IComponent component);

        /// <summary>
        ///   Remove a known Component from the manager.
        /// </summary>
        /// <param name = "component"></param>
        void DeregisterComponent(IComponent component);

        /// <summary>
        ///   Get all known registered Components
        /// </summary>
        /// <returns></returns>
        IComponent[] GetComponents();

        /// <summary>
        ///   Get the State of a Component with the given name
        /// </summary>
        /// <param name = "obj">The object being checked</param>
        /// <param name = "Name">Name of the Component</param>
        /// <returns>The State of the Component</returns>
        OSD GetComponentState(ISceneChildEntity obj, string Name);

        /// <summary>
        ///   Set the State of the Component with the given name
        /// </summary>
        /// <param name = "obj">The object to update</param>
        /// <param name = "Name">Name of the Component</param>
        /// <param name = "State">State to set the Component to</param>
        void SetComponentState(ISceneChildEntity obj, string Name, OSD State);

        /// <summary>
        ///   Take the serialized string and set up the Components for this object
        /// </summary>
        /// <param name = "obj"></param>
        /// <param name = "serialized"></param>
        void DeserializeComponents(ISceneChildEntity obj, string serialized);

        /// <summary>
        ///   Serialize all the registered Components into a string to be saved later
        /// </summary>
        /// <param name = "obj">The object to serialize</param>
        /// <returns>The serialized string</returns>
        string SerializeComponents(ISceneChildEntity obj);

        /// <summary>
        ///   Changes the UUIDs of one object to another
        /// </summary>
        /// <param name = "oldID"></param>
        /// <param name = "part"></param>
        void ResetComponentIDsToNewObject(UUID oldID, ISceneChildEntity part);

        /// <summary>
        ///   Remove the component for the given object with the given name, resets it to null
        /// </summary>
        /// <param name = "UUID"></param>
        /// <param name = "name"></param>
        void RemoveComponentState(UUID UUID, string name);

        /// <summary>
        ///   Remove all components for the given object, resets it to null
        /// </summary>
        /// <param name = "UUID"></param>
        /// <param name = "name"></param>
        void RemoveComponents(UUID obj);
    }
}