/*
 * Copyright (c) Contributors, http://aurora-sim.org/, http://opensimulator.org/
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

namespace Aurora.Framework
{
    public enum ThrottleOutPacketType
    {
        //        Unknown = -1,  hack forcing unknown to State
        /// <summary>
        ///     Unthrottled packets
        /// </summary>
        /// <summary>
        ///     Packets that are being resent
        /// </summary>
        Resend = 0,

        /// <summary>
        ///     Terrain data
        /// </summary>
        Land = 1,

        /// <summary>
        ///     Wind data
        /// </summary>
        Wind = 2,

        /// <summary>
        ///     Cloud data
        /// </summary>
        Cloud = 3,

        /// <summary>
        ///     This deals almost exclusively with the object updates and properties.
        ///     It also does GenericMessages and KillObject packets as well.
        /// </summary>
        /// <remarks>
        ///     This category may become saturated with packets if there are many objects in the sim,
        ///     or if it is a very highly active sim (many moving objects).
        /// </remarks>
        Task = 4,

        /// <summary>
        ///     Texture assets
        /// </summary>
        Texture = 5,

        /// <summary>
        ///     Non-texture assets
        /// </summary>
        Asset = 6,

        /// <summary>
        ///     Avatar and primitive data
        /// </summary>
        /// <remarks>
        ///     This category WILL be saturated with packets after a link or selecting a large object.
        ///     So when assigning a packet to this category, be aware that after a link, packets will not be sent for some time
        ///     in this category.
        ///     This is a sub-category of Task
        /// </remarks>
        State = 7,

        /// <summary>
        ///     This handles info that the client uses to be able to function in the world.
        ///     This includes packets like Chat and directory, group, and profile packets
        /// </summary>
        /// <remarks>
        ///     This category shouldn't ever be extremely saturated with packets.
        ///     This is a sub-category of Task
        /// </remarks>
        Transfer = 8, //This is for when the client asks for a transfer, such as an asset or a inventory list
        AvatarInfo = 9,
        OutBand = 10,

        /// <summary>
        ///     The number of packet categories to throttle on.
        ///     If a throttle category is added or removed, this number must also change
        /// </summary>
        Count = 11, // this must be the LAST one 
        Immediate = 12,
        // This one is outside of Count and does NOT have a queue, and all packets will be sent immediately
    }
}