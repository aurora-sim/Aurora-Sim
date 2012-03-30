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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using Aurora.Simulation.Base;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using Aurora.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes.Serialization;

namespace OpenSim.Region.Framework.Scenes.Components
{
    public class ComponentManager : IService, IComponentManager, ISOPSerializerModule
    {
        #region Declares

        /// <summary>
        ///   Dictionary of all the components that we have by name, component
        /// </summary>
        private readonly Dictionary<string, IComponent> m_components = new Dictionary<string, IComponent>();

        private readonly Dictionary<Type, string> m_componentsBaseType = new Dictionary<Type, string>();
        private bool m_hasStarted;

        #endregion

        #region IComponentManager Members

        /// <summary>
        ///   Register a new Component base with the manager.
        ///   This hooks the Component up to serialization and deserialization and also allows it to be pulled from IComponents[] in the SceneObjectPart.
        /// </summary>
        /// <param name = "component"></param>
        public void RegisterComponent(IComponent component)
        {
            //Check for the base type first if it isn't null
            if (component.BaseType != null)
            {
                if (m_componentsBaseType.ContainsKey(component.BaseType))
                {
                    //We only register one base type per session
                    MainConsole.Instance.Warn(
                        "[COMPONENTMANAGER]: Tried registering a component while another base type was already registed by the same base type! The previously registered module was " +
                        m_componentsBaseType[component.BaseType]);
                    return;
                }
            }
            //Now check for name duplication
            if (m_components.ContainsKey(component.Name))
            {
                MainConsole.Instance.Warn(
                    "[COMPONENTMANAGER]: Tried registering a component while another module already has used this name '" +
                    component.Name + "'!");
                return;
            }
            //Only add if it is not null
            if (component.BaseType != null)
                m_componentsBaseType.Add(component.BaseType, component.Name);
            //Add to the list
            m_components.Add(component.Name, component);
        }

        /// <summary>
        ///   Remove a known Component from the manager.
        /// </summary>
        /// <param name = "component"></param>
        public void DeregisterComponent(IComponent component)
        {
            //Remove the base type if it exists
            if (component.BaseType != null)
            {
                if (m_componentsBaseType.ContainsKey(component.BaseType))
                {
                    m_componentsBaseType.Remove(component.BaseType);
                }
            }
            //Now clear the name 
            if (m_components.ContainsKey(component.Name))
                m_components.Remove(component.Name);
        }

        /// <summary>
        ///   Get a list of all the Components that we have registered
        /// </summary>
        /// <returns></returns>
        public IComponent[] GetComponents()
        {
            return new List<IComponent>(m_components.Values).ToArray();
        }

        /// <summary>
        ///   Get the State of a Component with the given name
        /// </summary>
        /// <param name = "obj">The object being checked</param>
        /// <param name = "Name">Name of the Component</param>
        /// <returns>The State of the Component</returns>
        public OSD GetComponentState(ISceneChildEntity obj, string Name)
        {
            //Check whether a Component exists for this name
            if (m_components.ContainsKey(Name))
            {
                //Return the State of the object
                return m_components[Name].GetState(obj.UUID);
            }
            else
            {
                MainConsole.Instance.Warn("PUT THIS IN THE AURORA-SIM IRC CHANNEL IF POSSIBLE: " + Name);
                DefaultComponents com = new DefaultComponents(Name, 0);
                RegisterComponent(com);
                return m_components[Name].GetState(obj.UUID);
            }
        }

        /// <summary>
        ///   Set the State of the Component with the given name
        /// </summary>
        /// <param name = "obj">The object to update</param>
        /// <param name = "Name">Name of the Component</param>
        /// <param name = "State">State to set the Component to</param>
        public void SetComponentState(ISceneChildEntity obj, string Name, OSD State)
        {
            if (obj.UUID == UUID.Zero)
                return;
            //Check whether a Component exists for this name
            if (m_components.ContainsKey(Name))
            {
                //Set the State
                m_components[Name].SetState(obj.UUID, State);
            }
            else
            {
                DefaultComponents com = new DefaultComponents(Name, 0);
                RegisterComponent(com);
                m_components[Name].SetState(obj.UUID, State);
            }
        }

        public void RemoveComponentState(UUID obj, string name)
        {
            if (obj == UUID.Zero)
                return;
            //Check whether a Component exists for this name
            if (m_components.ContainsKey(name))
            {
                //Set the State
                m_components[name].RemoveState(obj);
            }
            else
            {
                DefaultComponents com = new DefaultComponents(name, 0);
                RegisterComponent(com);
                m_components[name].RemoveState(obj);
            }
        }

        public void RemoveComponents(UUID obj)
        {
            if (obj == UUID.Zero)
                return;
            //Check whether a Component exists for this name
            foreach (IComponent comp in m_components.Values)
            {
                //Set the State
                comp.RemoveState(obj);
            }
        }

        /// <summary>
        ///   Change/add all references from the oldID to the new UUID
        /// </summary>
        /// <param name = "oldID"></param>
        /// <param name = "newID"></param>
        public void ResetComponentIDsToNewObject(UUID oldID, ISceneChildEntity part)
        {
            //Run through the list of components and serialize them
            foreach (IComponent component in m_components.Values)
            {
                //Add the componet to the map by its name
                OSD o = component.GetState(oldID, true);
                if (o != null && o.Type != OSDType.Unknown)
                    SetComponentState(part, component.Name, o);
            }
        }

        /// <summary>
        ///   Take the serialized string and set up the Components for this object
        /// </summary>
        /// <param name = "obj"></param>
        /// <param name = "serialized"></param>
        public void DeserializeComponents(ISceneChildEntity obj, string serialized)
        {
            //Pull the OSDMap out for components
            OSDMap map;
            try
            {
                if (serialized == "")
                    map = new OSDMap();
                else
                    map = (OSDMap) OSDParser.DeserializeJson(serialized);
            }
            catch
            {
                //Bad JSON? Just return
                return;
            }

            //Now check against the list of components we have loaded
            foreach (KeyValuePair<string, OSD> kvp in map)
            {
                //Find the component if it exists
                IComponent component;
                if (m_components.TryGetValue(kvp.Key, out component))
                {
                    //Update the components value
                    component.SetState(obj.UUID, kvp.Value);
                }
            }
            map.Clear();
            map = null;
        }

        /// <summary>
        ///   Serialize all the registered Components into a string to be saved later
        /// </summary>
        /// <param name = "obj">The object to serialize</param>
        /// <returns>The serialized string</returns>
        public string SerializeComponents(ISceneChildEntity obj)
        {
            OSDMap ComponentsBody = new OSDMap();
            //Run through the list of components and serialize them
            foreach (IComponent component in m_components.Values)
            {
                //Add the componet to the map by its name
                OSD o = component.GetState(obj.UUID, true);
                if (o != null && o.Type != OSDType.Unknown)
                    ComponentsBody.Add(component.Name, o);
            }
            string result = OSDParser.SerializeJsonString(ComponentsBody,  true);
            ComponentsBody.Clear();

            return result;
        }

        #endregion

        #region ISOPSerializerModule Members

        public void Deserialization(SceneObjectPart obj, XmlTextReader reader)
        {
            string components = reader.ReadElementContentAsString("Components", String.Empty);
            if (components != "")
            {
                try
                {
                    DeserializeComponents(obj, components);
                    obj.FinishedSerializingGenericProperties();
                }
                catch (Exception ex)
                {
                    MainConsole.Instance.Warn("[COMPONENTMANAGER]: Error on deserializing Components! " + ex);
                }
            }
        }

        public string Serialization(SceneObjectPart part)
        {
            return SerializeComponents(part);
        }

        #endregion

        #region Register Default Components

        /// <summary>
        ///   Register a few default Components that are in the SOP
        /// </summary>
        private void RegisterDefaultComponents()
        {
            DefaultComponents com = new DefaultComponents("APIDTarget", Quaternion.Identity);
            RegisterComponent(com);
            com = new DefaultComponents("APIDDamp", 0);
            RegisterComponent(com);
            com = new DefaultComponents("APIDStrength", 0);
            RegisterComponent(com);
            com = new DefaultComponents("ParticleSystem", new byte[0]);
            RegisterComponent(com);
            com = new DefaultComponents("Expires", null);
            RegisterComponent(com);
            com = new DefaultComponents("Rezzed", null);
            RegisterComponent(com);
            com = new DefaultComponents("Damage", 0);
            RegisterComponent(com);
            com = new DefaultComponents("DIE_AT_EDGE", false);
            RegisterComponent(com);
            com = new DefaultComponents("SitTargetOrientation", Quaternion.Identity);
            RegisterComponent(com);
            com = new DefaultComponents("SitTargetPosition", Vector3.Zero);
            RegisterComponent(com);
            com = new DefaultComponents("SitTargetOrientationLL", Vector3.Zero);
            RegisterComponent(com);
            com = new DefaultComponents("RETURN_AT_EDGE", false);
            RegisterComponent(com);
            com = new DefaultComponents("BlockGrab", false);
            RegisterComponent(com);
            com = new DefaultComponents("BlockGrabObject", false);
            RegisterComponent(com);
            com = new DefaultComponents("StatusSandbox", false);
            RegisterComponent(com);
            com = new DefaultComponents("StatusSandboxPos", Vector3.Zero);
            RegisterComponent(com);
            com = new DefaultComponents("UseSoundQueue", 0);
            RegisterComponent(com);
            com = new DefaultComponents("Sound", UUID.Zero);
            RegisterComponent(com);
            com = new DefaultComponents("SoundFlags", 0);
            RegisterComponent(com);
            com = new DefaultComponents("SoundGain", 0);
            RegisterComponent(com);
            com = new DefaultComponents("SoundRadius", 0);
            RegisterComponent(com);
            com = new DefaultComponents("STATUS_ROTATE_X", 0);
            RegisterComponent(com);
            com = new DefaultComponents("STATUS_ROTATE_Y", 0);
            RegisterComponent(com);
            com = new DefaultComponents("STATUS_ROTATE_Z", 0);
            RegisterComponent(com);
            com = new DefaultComponents("PIDTarget", Vector3.Zero);
            RegisterComponent(com);
            com = new DefaultComponents("PIDActive", false);
            RegisterComponent(com);
            com = new DefaultComponents("PIDTau", 0);
            RegisterComponent(com);
            com = new DefaultComponents("VolumeDetectActive", false);
            RegisterComponent(com);
            com = new DefaultComponents("CameraEyeOffset", Vector3.Zero);
            RegisterComponent(com);
            com = new DefaultComponents("CameraAtOffset", Vector3.Zero);
            RegisterComponent(com);
            com = new DefaultComponents("ForceMouselook", false);
            RegisterComponent(com);
            com = new DefaultComponents("CRC", 0);
            RegisterComponent(com);
            com = new DefaultComponents("LocalId", 0);
            RegisterComponent(com);
            com = new DefaultComponents("TextureAnimation", new byte[0]);
            RegisterComponent(com);
            com = new DefaultComponents("SavedAttachedPos", Vector3.Zero);
            RegisterComponent(com);
            com = new DefaultComponents("SavedAttachmentPoint", 0);
            RegisterComponent(com);
            com = new DefaultComponents("PhysicsType", 0);
            RegisterComponent(com);
            com = new DefaultComponents("Density", 0);
            RegisterComponent(com);
            com = new DefaultComponents("GravityMultiplier", 0);
            RegisterComponent(com);
            com = new DefaultComponents("Friction", 0);
            RegisterComponent(com);
            com = new DefaultComponents("Restitution", 0);
            RegisterComponent(com);
            com = new DefaultComponents("ScriptState", "");
            RegisterComponent(com);
            com = new DefaultComponents("OmegaAxis", Vector3.Zero);
            RegisterComponent(com);
            com = new DefaultComponents("OmegaSpinRate", 0);
            RegisterComponent(com);
            com = new DefaultComponents("OmegaGain", 0);
            RegisterComponent(com);
            com = new DefaultComponents("VehicleType", 0);
            RegisterComponent(com);
            com = new DefaultComponents("VehicleParameters", 0);
            RegisterComponent(com);
            com = new DefaultComponents("VehicleFlags", 0);
            RegisterComponent(com);
            com = new DefaultComponents("PIDHoverActive", 0);
            RegisterComponent(com);
            com = new DefaultComponents("KeyframeAnimation", null);
            RegisterComponent(com);
            com = new DefaultComponents("APIDEnabled", null);
            RegisterComponent(com);
            com = new DefaultComponents("APIDIterations", 0);
            RegisterComponent(com);
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            if (!m_hasStarted)
            {
                RegisterDefaultComponents();
                SceneObjectSerializer.AddSerializer("Components", this);
                m_hasStarted = true;
            }
            registry.RegisterModuleInterface<IComponentManager>(this);
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        #endregion
    }

    /// <summary>
    ///   This sets up components for a few internal pieces in the
    /// </summary>
    public class DefaultComponents : IComponent
    {
        private readonly Dictionary<UUID, OSD> m_states = new Dictionary<UUID, OSD>();
        private readonly object m_statesLock = new object();
        public object m_defaultValue;
        public string m_name;

        public DefaultComponents(string name, object defaultValue)
        {
            m_name = name;
            m_defaultValue = defaultValue;
        }

        #region IComponent Members

        public Type BaseType
        {
            get { return null; }
        }

        public string Name
        {
            get { return m_name; }
        }

        public virtual OSD GetState(UUID obj)
        {
            return GetState(obj, false);
        }

        public virtual OSD GetState(UUID obj, bool copy)
        {
            OSD o = null;
            lock (m_statesLock)
            {
                if (m_states.TryGetValue(obj, out o))
                {
                    if (o == m_defaultValue)
                        return null;
                    if (copy)
                        return o.Copy();
                    return o;
                }
            }

            return new OSD();
        }

        public virtual void SetState(UUID obj, OSD osd)
        {
            lock (m_statesLock)
                m_states[obj] = osd;
        }

        public virtual void RemoveState(UUID obj)
        {
            lock (m_statesLock)
                m_states.Remove(obj);
        }

        #endregion
    }
}