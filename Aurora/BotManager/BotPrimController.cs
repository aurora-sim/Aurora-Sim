using Aurora.Framework;
using Aurora.Framework.Modules;
using Aurora.Framework.Physics;
using Aurora.Framework.SceneInfo;
using Aurora.Framework.SceneInfo.Entities;
using OpenMetaverse;

namespace Aurora.BotManager
{
    public class BotPrimController : IBotController
    {
        private ISceneEntity m_object;
        private Bot m_bot;
        private bool m_run;
        private float m_speed = 1;
        private bool m_hasStoppedMoving = false;

        public BotPrimController(ISceneEntity obj, Bot bot)
        {
            m_object = obj;
            m_bot = bot;
        }

        public string Name
        {
            get { return m_object.Name; }
        }

        public UUID UUID
        {
            get { return m_object.UUID; }
        }

        public bool SetAlwaysRun
        {
            get { return m_run; }
            set { m_run = value; }
        }

        public bool ForceFly
        {
            get { return false; }
            set { }
        }

        public PhysicsActor PhysicsActor
        {
            get { return m_object.RootChild.PhysActor; }
        }

        public bool CanMove
        {
            get { return true; }
        }

        public Vector3 AbsolutePosition
        {
            get { return m_object.AbsolutePosition; }
        }

        public void SendChatMessage(int sayType, string message, int channel)
        {
            IChatModule chatModule = m_object.Scene.RequestModuleInterface<IChatModule>();
            if (chatModule != null)
                chatModule.SimChat(message, (ChatTypeEnum) sayType, channel,
                                   m_object.RootChild.AbsolutePosition, m_object.Name, m_object.UUID, false,
                                   m_object.Scene);
        }

        public void SendInstantMessage(GridInstantMessage im)
        {
            IMessageTransferModule m_TransferModule =
                m_object.Scene.RequestModuleInterface<IMessageTransferModule>();
            if (m_TransferModule != null)
                m_TransferModule.SendInstantMessage(im);
        }

        public void Close()
        {
        }

        public void OnBotAgentUpdate(Vector3 toward, uint controlFlag, Quaternion bodyRotation)
        {
            OnBotAgentUpdate(toward, controlFlag, bodyRotation, true);
        }

        public void OnBotAgentUpdate(Vector3 toward, uint controlFlag, Quaternion bodyRotation, bool isMoving)
        {
            if (isMoving)
                m_hasStoppedMoving = false;
            m_object.AbsolutePosition += toward*(m_speed*(1f/45f));
            m_object.ScheduleGroupTerseUpdate();
        }

        public void UpdateMovementAnimations(bool sendTerseUpdate)
        {
            if (sendTerseUpdate)
                m_object.ScheduleGroupTerseUpdate();
        }

        public void Teleport(OpenMetaverse.Vector3 pos)
        {
            m_object.AbsolutePosition = pos;
        }

        public IScene GetScene()
        {
            return m_object.Scene;
        }

        public void StopMoving(bool fly, bool clearPath)
        {
            if (m_hasStoppedMoving)
                return;
            m_hasStoppedMoving = true;
            m_bot.State = BotState.Idle;
            //Clear out any nodes
            if (clearPath)
                m_bot.m_nodeGraph.Clear();
            //Send the stop message
            m_bot.m_movementFlag = (uint) AgentManager.ControlFlags.NONE;
            if (fly)
                m_bot.m_movementFlag |= (uint) AgentManager.ControlFlags.AGENT_CONTROL_FLY;
            OnBotAgentUpdate(Vector3.Zero, m_bot.m_movementFlag, m_bot.m_bodyDirection, false);

            if (m_object.RootChild.PhysActor != null)
                m_object.RootChild.PhysActor.ForceSetVelocity(Vector3.Zero);
        }

        public void SetSpeedModifier(float speed)
        {
            if (speed > 4)
                speed = 4;
            m_speed = speed;
        }

        public void SetDrawDistance(float draw)
        {
        }

        public void StandUp()
        {
        }

        public void Jump()
        {
            m_bot.m_nodeGraph.Clear();
            m_bot.m_nodeGraph.FollowIndefinitely = false;
            m_bot.m_nodeGraph.Add(m_object.AbsolutePosition + new Vector3(0, 0, 1.5f), TravelMode.Walk);
            m_bot.m_nodeGraph.Add(m_object.AbsolutePosition, TravelMode.Walk);
            m_bot.ForceCloseToPoint = true;
            m_bot.m_closeToPoint = 0.1f;
            m_bot.GetNextDestination();
        }
    }
}