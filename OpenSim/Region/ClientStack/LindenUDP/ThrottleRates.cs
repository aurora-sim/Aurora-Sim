/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
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
using OpenSim.Framework;
using Nini.Config;

namespace OpenSim.Region.ClientStack.LindenUDP
{
    /// <summary>
    /// Holds drip rates and maximum burst rates for throttling with hierarchical
    /// token buckets. The maximum burst rates set here are hard limits and can
    /// not be overridden by client requests
    /// </summary>
    public sealed class ThrottleRates
    {
        /// <summary>Minimum bytes in the token bucket before it can be taken from for resent packets</summary>
        public int ResendMin;
        /// <summary>Minimum bytes in the token bucket before it can be taken from for terrain packets</summary>
        public int LandMin;
        /// <summary>Minimum bytes in the token bucket before it can be taken from for wind packets</summary>
        public int WindMin;
        /// <summary>Minimum bytes in the token bucket before it can be taken from for cloud packets</summary>
        public int CloudMin;
        /// <summary>Minimum bytes in the token bucket before it can be taken from for task packets</summary>
        public int TaskMin;
        /// <summary>Minimum bytes in the token bucket before it can be taken from for texture packets</summary>
        public int TextureMin;
        /// <summary>Minimum bytes in the token bucket before it can be taken from for asset packets</summary>
        public int AssetMin;
        /// <summary>Minimum bytes in the token bucket before it can be taken from for transfer packets</summary>
        public int TransferMin;
        /// <summary>Minimum bytes in the token bucket before it can be taken from for state packets</summary>
        public int StateMin;
        /// <summary>Minimum bytes in the token bucket before it can be taken from for AvatarInfo packets</summary>
        public int AvatarInfoMin;
        /// <summary>Drip rate for the parent token bucket</summary>
        public int Total;

        /// <summary>Maximum burst rate for resent packets</summary>
        public int ResendLimit;
        /// <summary>Maximum burst rate for land packets</summary>
        public int LandLimit;
        /// <summary>Maximum burst rate for wind packets</summary>
        public int WindLimit;
        /// <summary>Maximum burst rate for cloud packets</summary>
        public int CloudLimit;
        /// <summary>Maximum burst rate for task (state and transaction) packets</summary>
        public int TaskLimit;
        /// <summary>Maximum burst rate for texture packets</summary>
        public int TextureLimit;
        /// <summary>Maximum burst rate for asset packets</summary>
        public int AssetLimit;
        /// <summary>Maximum burst rate for asset packets</summary>
        public int TransferLimit;
        /// <summary>Maximum burst rate for state packets</summary>
        public int StateLimit;
        /// <summary>Burst rate for the parent token bucket</summary>
        public int TotalLimit;
        /// <summary>Burst rate for the parent token bucket</summary>
        public int AvatarInfoLimit;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="config">Config source to load defaults from</param>
        public ThrottleRates(IConfigSource config)
        {
            try
            {
                IConfig throttleConfig = config.Configs["ClientStack.LindenUDP"];

                ResendMin = throttleConfig.GetInt("resend_min", 1500);
                LandMin = throttleConfig.GetInt("land_min", 400);
                WindMin = throttleConfig.GetInt("wind_min", 500);
                CloudMin = throttleConfig.GetInt("cloud_min", 300);
                TaskMin = throttleConfig.GetInt("task_min", 1500);
                TextureMin = throttleConfig.GetInt("texture_min", 1000);
                AssetMin = throttleConfig.GetInt("asset_min", 250);
                TransferMin = throttleConfig.GetInt("transfer_min", 1000);
                StateMin = throttleConfig.GetInt("state_min", 500);
                AvatarInfoMin = throttleConfig.GetInt("avatar_info_min", 250);

                ResendLimit = throttleConfig.GetInt("resend_limit", 18750);
                LandLimit = throttleConfig.GetInt("land_limit", 29750);
                WindLimit = throttleConfig.GetInt("wind_limit", 18750);
                CloudLimit = throttleConfig.GetInt("cloud_limit", 18750);
                TaskLimit = throttleConfig.GetInt("task_limit", 18750);
                TextureLimit = throttleConfig.GetInt("texture_limit", 55750);
                AssetLimit = throttleConfig.GetInt("asset_limit", 27500);
                TransferLimit = throttleConfig.GetInt("transfer_limit", 27500);
                StateLimit = throttleConfig.GetInt("state_limit", 37000);
                AvatarInfoLimit = throttleConfig.GetInt("avatar_info_limit", 37000);

                Total = throttleConfig.GetInt("client_throttle_max_bps", 0);
                TotalLimit = Total;
            }
            catch (Exception) { }
        }

        public int GetMinimum(ThrottleOutPacketType type)
        {
            switch (type)
            {
                case ThrottleOutPacketType.Resend:
                    return ResendMin;
                case ThrottleOutPacketType.Land:
                    return LandMin;
                case ThrottleOutPacketType.Wind:
                    return WindMin;
                case ThrottleOutPacketType.Cloud:
                    return CloudMin;
                case ThrottleOutPacketType.Task:
                    return TaskMin;
                case ThrottleOutPacketType.Texture:
                    return TextureMin;
                case ThrottleOutPacketType.Asset:
                    return AssetMin;
                case ThrottleOutPacketType.Transfer:
                    return TransferMin;
                case ThrottleOutPacketType.State:
                    return StateMin;
                case ThrottleOutPacketType.AvatarInfo:
                    return AvatarInfoMin;
                default:
                    return 0;
            }
        }

        public int GetLimit(ThrottleOutPacketType type)
        {
            switch (type)
            {
                case ThrottleOutPacketType.Resend:
                    return ResendLimit;
                case ThrottleOutPacketType.Land:
                    return LandLimit;
                case ThrottleOutPacketType.Wind:
                    return WindLimit;
                case ThrottleOutPacketType.Cloud:
                    return CloudLimit;
                case ThrottleOutPacketType.Task:
                    return TaskLimit;
                case ThrottleOutPacketType.Texture:
                    return TextureLimit;
                case ThrottleOutPacketType.Asset:
                    return AssetLimit;
                case ThrottleOutPacketType.Transfer:
                    return TransferLimit;
                case ThrottleOutPacketType.State:
                    return StateLimit;
                case ThrottleOutPacketType.AvatarInfo:
                    return AvatarInfoLimit;
                default:
                    return 0;
            }
        }
    }
}
