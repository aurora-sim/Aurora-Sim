using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Framework;
using Aurora.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public interface IScriptPlugin : IPlugin
    {
        void Check();
        Object[] GetSerializationData(UUID itemID, UUID primID);
        void CreateFromData(UUID itemID, UUID objectID, Object[] data);
        void RemoveScript(UUID primID, UUID itemID);
    }

    public interface ISharedScriptPlugin : IScriptPlugin
    {
        void Initialize(ScriptEngine engine);
    }

    public interface INonSharedScriptPlugin : IScriptPlugin
    {
        void Initialize(ScriptEngine engine, Scene scene);
    }
}
