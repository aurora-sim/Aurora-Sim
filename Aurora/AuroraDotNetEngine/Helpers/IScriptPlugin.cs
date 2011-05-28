using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Framework;
using Aurora.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    /// <summary>
    /// These plugins provide for the ability to hook up onto scripts for events and are called each iteration by the script engine event thread
    /// </summary>
    public interface IScriptPlugin
    {
        /// <summary>
        /// Returns the plugin name
        /// </summary>
        /// <returns></returns>
        string Name { get; }

        /// <summary>
        /// Start the plugin
        /// </summary>
        /// <param name="engine"></param>
        void Initialize (ScriptEngine engine);

        /// <summary>
        /// Add the given region to the plugin that it will be serving
        /// </summary>
        /// <param name="scene"></param>
        void AddRegion (Scene scene);

        /// <summary>
        /// Check the module and all scripts that it may be being used by
        /// This is called every iteration by the event thread
        /// </summary>
        /// <returns></returns>
        bool Check();

        /// <summary>
        /// Serialize any data that we may have for the given script
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="primID"></param>
        /// <returns></returns>
        OSD GetSerializationData (UUID itemID, UUID primID);

        /// <summary>
        /// Create from the information that we serialized earlier the info that we have for the given script and add it back to the script
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="objectID"></param>
        /// <param name="data"></param>
        void CreateFromData (UUID itemID, UUID objectID, OSD data);

        /// <summary>
        /// Remove the given script from the plugin
        /// </summary>
        /// <param name="primID"></param>
        /// <param name="itemID"></param>
        void RemoveScript(UUID primID, UUID itemID);
    }
}
