using Aurora.Framework.ClientInterfaces;
using Aurora.Framework.PresenceInfo;
using Aurora.Framework.SceneInfo;
using OpenMetaverse;

namespace Aurora.Framework.Modules
{
    public enum ScriptControlled : uint
    {
        CONTROL_ZERO = 0,
        CONTROL_FWD = 1,
        CONTROL_BACK = 2,
        CONTROL_LEFT = 4,
        CONTROL_RIGHT = 8,
        CONTROL_UP = 16,
        CONTROL_DOWN = 32,
        CONTROL_ROT_LEFT = 256,
        CONTROL_ROT_RIGHT = 512,
        CONTROL_LBUTTON = 268435456,
        CONTROL_ML_LBUTTON = 1073741824
    }

    public struct ScriptControllers
    {
        public UUID itemID;
        public ISceneChildEntity part;
        public ScriptControlled ignoreControls;
        public ScriptControlled eventControls;
    }

    public interface IScriptControllerModule
    {
        ScriptControllers GetScriptControler(UUID uUID);

        void RegisterScriptController(ScriptControllers SC);

        void UnRegisterControlEventsToScript(uint p, UUID uUID);

        void RegisterControlEventsToScript(int controls, int accept, int pass_on, ISceneChildEntity m_host,
                                           UUID m_itemID);

        void OnNewMovement(ref AgentManager.ControlFlags flags);

        void RemoveAllScriptControllers(ISceneChildEntity part);

        void HandleForceReleaseControls(IClientAPI remoteClient, UUID agentID);

        ControllerData[] Serialize();

        void Deserialize(ControllerData[] controllerData);
    }
}