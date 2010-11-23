using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nini.Config;
using OpenMetaverse;
using Aurora.ScriptEngine.AuroraDotNetEngine.Plugins;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    public interface IScriptModulePlugin : IScriptModule
    {
        IConfig Config { get; }

        IConfigSource ConfigSource { get; }

        IScriptModule ScriptModule { get; }

        bool PostScriptEvent(UUID m_itemID, UUID uUID, EventParams EventParams, EventPriority EventPriority);

        void SetState(UUID m_itemID, string newState);

        void SetScriptRunningState(UUID item, bool p);

        IScriptPlugin GetScriptPlugin(string p);

        DetectParams GetDetectParams(UUID uUID, UUID m_itemID, int number);

        bool AddToObjectQueue(UUID uUID, string p, DetectParams[] detectParams, int p_4, object[] p_5);

        void ResetScript(UUID uUID, UUID m_itemID, bool p);

        bool GetScriptRunningState(UUID item);

        int GetStartParameter(UUID m_itemID, UUID uUID);

        void SetMinEventDelay(UUID m_itemID, UUID uUID, double delay);

        IScriptApi GetApi(UUID m_itemID, string p);

        bool PipeEventsForScript(SceneObjectPart m_host, Vector3 vector3);
    }
}
