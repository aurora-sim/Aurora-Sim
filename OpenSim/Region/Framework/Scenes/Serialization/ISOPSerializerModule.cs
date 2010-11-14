using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace OpenSim.Region.Framework.Scenes.Serialization
{
    /// <summary>
    /// This allows additional modules to link directly into the serialization process
    /// </summary>
    public interface ISOPSerializerModule
    {
        /// <summary>
        /// This is called to set up values in the module
        /// </summary>
        /// <param name="sop">The object being changed</param>
        /// <param name="reader">Contains the values that the .xml file has found</param>
        void Deserialization(SceneObjectPart sop, XmlTextReader reader);

        /// <summary>
        /// This is called when the object is being changed into .xml
        /// </summary>
        /// <param name="part">Object being worked on</param>
        /// <returns>The serialized part that will be added to the .xml file</returns>
        string Serialization(SceneObjectPart part);
    }
}
