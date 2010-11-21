using System;
using System.Collections.Generic;
using OpenMetaverse;

namespace Aurora.Framework
{
    public enum TravelMode { Walk, Fly, None };

    public interface IBotManager
    {
        UUID CreateAvatar(string FirstName, string LastName, UUID cloneAppearanceFrom);
        void SetBotMap(UUID Bot, List<Vector3> Positions, List<TravelMode> mode);
        void UnpauseAutoMove(UUID Bot);
        void PauseAutoMove(UUID Bot);
        void StopAutoMove(UUID Bot);
        void EnableAutoMove(UUID Bot);
        void SetMovementSpeedMod(UUID Bot, float modifier);
    }

    public interface IAStarBotManager : IBotManager
    {
        UUID CreateAStarAvatar(string FirstName, string LastName, UUID cloneAppearanceFrom);
        int[,] ReadMap(UUID botID, string filename, int X, int Y, int CornerStoneX, int CornerStoneY);
        void FindPath(UUID botID, Vector3 currentPos, Vector3 finishVector);
        void FollowAvatar(UUID botID, string avatarName);
    }
}
