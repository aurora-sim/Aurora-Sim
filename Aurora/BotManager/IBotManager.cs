using System;
using System.Collections.Generic;
using OpenSim.Framework;
using OpenMetaverse;

namespace Aurora.BotManager
{
    public interface IBotManager
    {
        UUID CreateAvatar (string FirstName, string LastName, IScene scene, UUID cloneAppearanceFrom, UUID creatorID);
        void SetBotMap(UUID Bot, List<Vector3> Positions, List<TravelMode> mode);
        void SetMovementSpeedMod(UUID Bot, float modifier);
        void RemoveAvatar (UUID Bot, IScene iScene);

        #region Path following

        void ReadMap (UUID botID, string map, int X, int Y, int CornerStoneX, int CornerStoneY);
        void FindPath (UUID botID, Vector3 currentPos, Vector3 finishVector);

        #endregion

        #region FollowAvatar

        void FollowAvatar (UUID botID, string avatarName, float followDistance);
        void StopFollowAvatar (UUID botID);

        #endregion

        #region Chat

        void SendChatMessage (UUID botID, string message, int sayType, int channel);

        #endregion

        void SetBotShouldFly (UUID Bot, bool shouldFly);
    }

    /// <summary>
    /// Created by RealXtend
    /// </summary>
    public interface IRexBot
    {
        void SetPath (List<Vector3> Positions, List<TravelMode> mode);
        void SetMovementSpeedMod (float speed);
        void DisableWalk ();
        void EnableWalk ();
        void ReadMap (string filename, int X, int Y, int CornerStoneX, int CornerStoneY);
        void FindPath (Vector3 currentPos, Vector3 finishVector);
        void FollowAvatar (string avatarName, float followDistance);
        void StopFollowAvatar ();
        void SendChatMessage (int sayType, string message, int channel);
    }
}
