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

using Aurora.Framework.SceneInfo;
using OpenMetaverse;
using System.Collections.Generic;

namespace Aurora.Framework.Modules
{
    public enum TravelMode
    {
        Walk,
        Run,
        Fly,
        Teleport,
        Wait,
        TriggerHereEvent,
        None
    };

    public interface IBotManager
    {
        #region Create/Remove bot

        UUID CreateAvatar(string firstName, string LastName, IScene scene, UUID cloneAppearanceFrom, UUID creatorID,
                          Vector3 startPos);

        void RemoveAvatar(UUID Bot, IScene iScene, UUID userAttempting);

        #endregion

        #region Tag/Remove bots

        void AddTagToBot(UUID Bot, string tag, UUID userAttempting);
        List<UUID> GetBotsWithTag(string tag);
        void RemoveBots(string tag, UUID userAttempting);

        #endregion

        #region Basic Movement

        void SetBotMap(UUID Bot, List<Vector3> positions, List<TravelMode> mode, int flags, UUID userAttempting);
        void SetMovementSpeedMod(UUID Bot, float modifier, UUID userAttempting);
        void SetBotShouldFly(UUID botID, bool shouldFly, UUID userAttempting);
        void PauseMovement(UUID botID, UUID userAttempting);
        void ResumeMovement(UUID botID, UUID userAttempting);
        void SetSpeed(UUID botID, UUID userAttempting, float speedModifier);

        #endregion

        #region FollowAvatar

        void FollowAvatar(UUID botID, string avatarName, float startFollowDistance, float endFollowDistance,
                          bool requireLOS, Vector3 offsetFromAvatar, UUID userAttempting);

        void StopFollowAvatar(UUID botID, UUID userAttempting);

        #endregion

        #region Chat

        void SendChatMessage(UUID botID, string message, int sayType, int channel, UUID userAttempting);

        void SendIM(UUID botID, UUID toUser, string message, UUID userAttempting);

        #endregion

        #region Characters

        void CreateCharacter(UUID primID, IScene scene);
        void RemoveCharacter(UUID primID);
        IBotController GetCharacterManager(UUID primID);

        #endregion
    }
}