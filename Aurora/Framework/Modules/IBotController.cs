using Aurora.Framework.SceneInfo;
using OpenMetaverse;

namespace Aurora.Framework
{
    public interface IBotController
    {
        string Name { get; }
        UUID UUID { get; }
        bool SetAlwaysRun { get; set; }
        bool ForceFly { get; set; }
        PhysicsActor PhysicsActor { get; }
        bool CanMove { get; }
        Vector3 AbsolutePosition { get; }

        void SendChatMessage(int sayType, string message, int channel);
        void SendInstantMessage(GridInstantMessage im);
        void Close();
        void OnBotAgentUpdate(Vector3 toward, uint controlFlag, Quaternion bodyRotation);
        void UpdateMovementAnimations(bool sendTerseUpdate);
        void StandUp();
        void Teleport(Vector3 pos);
        IScene GetScene();
        void StopMoving(bool fly, bool clearPath);
        void SetSpeedModifier(float speed);
        void SetDrawDistance(float draw);

        void Jump();
    }
}