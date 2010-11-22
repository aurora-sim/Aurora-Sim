using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;
using log4net;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;
using OpenSim.Region.Framework.Interfaces;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Region.Framework.Scenes.Components
{
    public class ComponentManager : ISharedRegionModule, IComponentManager, ISOPSerializerModule
    {
        #region Declares

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Dictionary of all the components that we have by name, component
        /// </summary>
        private Dictionary<string, IComponent> m_components = new Dictionary<string, IComponent>();
        private Dictionary<Type, string> m_componentsBaseType = new Dictionary<Type, string>();
        private bool m_hasStarted = false;

        #endregion

        #region ISharedRegionModule Members

        public void Initialise(IConfigSource source)
        {
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            if (!m_hasStarted)
            {
                RegisterDefaultComponents();
                SceneObjectSerializer.AddSerializer("Components", this);
                m_hasStarted = true;
            }
            scene.RegisterModuleInterface<IComponentManager>(this);
        }

        /// <summary>
        /// Register a few default Components that are in the SOP
        /// </summary>
        private void RegisterDefaultComponents()
        {
            DefaultComponents com = new DefaultComponents("APIDTarget");
            RegisterComponent(com);
            com = new DefaultComponents("APIDDamp");
            RegisterComponent(com);
            com = new DefaultComponents("APIDStrength");
            RegisterComponent(com);
            com = new DefaultComponents("ParticleSystem");
            RegisterComponent(com);
            com = new DefaultComponents("Expires");
            RegisterComponent(com);
            com = new DefaultComponents("Rezzed");
            RegisterComponent(com);
            com = new DefaultComponents("Damage");
            RegisterComponent(com);
            com = new DefaultComponents("DIE_AT_EDGE");
            RegisterComponent(com);
            com = new DefaultComponents("SitTargetOrientation");
            RegisterComponent(com);
            com = new DefaultComponents("SitTargetPosition");
            RegisterComponent(com);
            com = new DefaultComponents("SitTargetOrientationLL");
            RegisterComponent(com);
            com = new DefaultComponents("RETURN_AT_EDGE");
            RegisterComponent(com);
            com = new DefaultComponents("BlockGrab");
            RegisterComponent(com);
            com = new DefaultComponents("StatusSandbox");
            RegisterComponent(com);
            com = new DefaultComponents("StatusSandboxPos");
            RegisterComponent(com);
            com = new DefaultComponents("UseSoundQueue");
            RegisterComponent(com);
            com = new DefaultComponents("Sound");
            RegisterComponent(com);
            com = new DefaultComponents("SoundFlags");
            RegisterComponent(com);
            com = new DefaultComponents("SoundGain");
            RegisterComponent(com);
            com = new DefaultComponents("SoundRadius");
            RegisterComponent(com);
            com = new DefaultComponents("STATUS_ROTATE_X");
            RegisterComponent(com);
            com = new DefaultComponents("STATUS_ROTATE_Y");
            RegisterComponent(com);
            com = new DefaultComponents("STATUS_ROTATE_Z");
            RegisterComponent(com);
            com = new DefaultComponents("PIDTarget");
            RegisterComponent(com);
            com = new DefaultComponents("PIDActive");
            RegisterComponent(com);
            com = new DefaultComponents("PIDTau");
            RegisterComponent(com);
            com = new DefaultComponents("VolumeDetectActive");
            RegisterComponent(com);
            com = new DefaultComponents("CameraEyeOffset");
            RegisterComponent(com);
            com = new DefaultComponents("CameraAtOffset");
            RegisterComponent(com);
            com = new DefaultComponents("ForceMouselook");
            RegisterComponent(com);
            com = new DefaultComponents("CRC");
            RegisterComponent(com);
            com = new DefaultComponents("LocalId");
            RegisterComponent(com);
        }

        public void RemoveRegion(Scene scene)
        {
            scene.UnregisterModuleInterface<IComponentManager>(this);
            if (m_hasStarted) //This only needs removed once
            {
                SceneObjectSerializer.RemoveSerializer("Components");
                m_hasStarted = false;
            }
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public string Name
        {
            get { return "ComponentManager"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion

        #region IComponentManager Members

        /// <summary>
        /// Register a new Component base with the manager.
        /// This hooks the Component up to serialization and deserialization and also allows it to be pulled from IComponents[] in the SceneObjectPart.
        /// </summary>
        /// <param name="component"></param>
        public void RegisterComponent(IComponent component)
        {
            //Check for the base type first if it isn't null
            if (component.BaseType != null)
            {
                if (m_componentsBaseType.ContainsKey(component.BaseType))
                {
                    //We only register one base type per session
                    m_log.Warn("[COMPONENTMANAGER]: Tried registering a component while another base type was already registed by the same base type! The previously registered module was " + m_componentsBaseType[component.BaseType]);
                    return;
                }
            }
            //Now check for name duplication
            if (m_components.ContainsKey(component.Name))
            {
                m_log.Warn("[COMPONENTMANAGER]: Tried registering a component while another module already has used this name '" + component.Name + "'!");
                return;
            }
            //Only add if it is not null
            if(component.BaseType != null) 
                m_componentsBaseType.Add(component.BaseType, component.Name);
            //Add to the list
            m_components.Add(component.Name, component);
        }

        /// <summary>
        /// Remove a known Component from the manager.
        /// </summary>
        /// <param name="component"></param>
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
        /// Get a list of all the Components that we have registered
        /// </summary>
        /// <returns></returns>
        public IComponent[] GetComponents()
        {
            return new List<IComponent>(m_components.Values).ToArray();
        }

        /// <summary>
        /// Get the State of a Component with the given name
        /// </summary>
        /// <param name="obj">The object being checked</param>
        /// <param name="Name">Name of the Component</param>
        /// <returns>The State of the Component</returns>
        public OSD GetComponentState(SceneObjectPart obj, string Name)
        {
            //Check whether a Component exists for this name
            if (m_components.ContainsKey(Name))
            {
                //Return the State of the object
                return m_components[Name].GetState(obj.UUID);
            }
            return null;
        }

        /// <summary>
        /// Set the State of the Component with the given name
        /// </summary>
        /// <param name="obj">The object to update</param>
        /// <param name="Name">Name of the Component</param>
        /// <param name="State">State to set the Component to</param>
        public void SetComponentState(SceneObjectPart obj, string Name, OSD State)
        {
            if (obj.UUID == UUID.Zero)
                return;
            //Check whether a Component exists for this name
            if (m_components.ContainsKey(Name))
            {
                //Set the State
                m_components[Name].SetState(obj.UUID, State);
            }
        }

        /// <summary>
        /// Take the serialized string and set up the Components for this object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="serialized"></param>
        public void DeserializeComponents(SceneObjectPart obj, string serialized)
        {
            //Pull the OSDMap out for components
            OSDMap map;
            try
            {
                map = (OSDMap)OSDParser.DeserializeJson(serialized);
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
        }

        /// <summary>
        /// Serialize all the registered Components into a string to be saved later
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>The serialized string</returns>
        public string SerializeComponents(SceneObjectPart obj)
        {
            OSDMap ComponentsBody = new OSDMap();
            //Run through the list of components and serialize them
            foreach (IComponent component in m_components.Values)
            {
                //Add the componet to the map by its name
                OSD o = component.GetState(obj.UUID);
                if(o != null)
                    ComponentsBody.Add(component.Name, o);
            }
            return OSDParser.SerializeJsonString(ComponentsBody);
        }

        #endregion

        #region ISOPSerializerModule Members

        public void Deserialization(SceneObjectPart obj, XmlTextReader reader)
        {
            string components = reader.ReadElementContentAsString("Components", String.Empty);
            if (components != "")
            {
                //m_log.Info("[COMPONENTMANAGER]: Found components for SOP " + obj.Name + " > " + components);

                try
                {
                    DeserializeComponents(obj, components);
                }
                catch (Exception ex)
                {
                    m_log.Warn("[COMPONENTMANAGER]: Error on deserializing Components! " + ex.ToString());
                }
            }
        }

        public string Serialization(SceneObjectPart part)
        {
            return SerializeComponents(part);
        }

        #endregion
    }

    /// <summary>
    /// This sets up components for a few internal pieces in the 
    /// </summary>
    public class DefaultComponents : IComponent
    {
        Dictionary<UUID, OSD> m_states = new Dictionary<UUID, OSD>();
        public string m_name;

        public DefaultComponents(string name)
        {
            m_name = name;
        }

        public Type BaseType
        {
            get { return null; }
        }

        public string Name
        {
            get { return m_name; }
        }

        public OSD GetState(UUID obj)
        {
            if(m_states.ContainsKey(obj))
                return m_states[obj];

            return new OSD();
        }

        public void SetState(UUID obj, OSD osd)
        {
            m_states[obj] = osd;
        }
    }
}
