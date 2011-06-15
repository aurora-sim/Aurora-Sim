/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using OpenSim.Framework;
using OpenMetaverse;

namespace Aurora.BotManager
{
    public interface IBotManager
    {
        #region Create/Remove bot

        UUID CreateAvatar (string FirstName, string LastName, IScene scene, UUID cloneAppearanceFrom, UUID creatorID, Vector3 startPos);
        void RemoveAvatar (UUID Bot, IScene iScene);

        #endregion

        #region Tag/Remove bots

        void AddTagToBot (UUID Bot, string tag);
        List<UUID> GetBotsWithTag (string tag);
        void RemoveBots (string tag);

        #endregion

        #region Basic Movement

        void SetBotMap(UUID Bot, List<Vector3> Positions, List<TravelMode> mode, int flags);
        void SetMovementSpeedMod (UUID Bot, float modifier);
        void SetBotShouldFly (UUID botID, bool shouldFly);
        void PauseMovement (UUID botID);
        void ResumeMovement (UUID botID);

        #endregion

        #region Path following

        void ReadMap (UUID botID, string map, int X, int Y, int CornerStoneX, int CornerStoneY);
        void FindPath (UUID botID, Vector3 currentPos, Vector3 finishVector);

        #endregion

        #region FollowAvatar

        void FollowAvatar (UUID botID, string avatarName, float startFollowDistance, float endFollowDistance);
        void StopFollowAvatar (UUID botID);

        #endregion

        #region Chat

        void SendChatMessage (UUID botID, string message, int sayType, int channel);

        #endregion


    }
}
