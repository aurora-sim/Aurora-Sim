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

using System;
using OpenMetaverse;

namespace Aurora.Framework.Modules
{
    public interface IRestartModule
    {
        /// <summary>
        ///     The time until the next restart will occur
        /// </summary>
        TimeSpan TimeUntilRestart { get; }

        /// <summary>
        ///     Schedule a restart for the scene this module runs in
        /// </summary>
        /// <param name="initiator">The user (or other ID) that caused this restart</param>
        /// <param name="message">The message to send to the clients in the sim</param>
        /// <param name="alerts">The times to send alert messages to the clients in the sim</param>
        /// <param name="notice">Send the alert messages as notices instead of blue box popups</param>
        void ScheduleRestart(UUID initiator, string message, int[] alerts, bool notice);

        /// <summary>
        ///     Stop the restart and send the given message to the clients
        /// </summary>
        /// <param name="message"></param>
        void AbortRestart(string message);

        /// <summary>
        ///     Restart the scene that this module is running in
        /// </summary>
        void RestartScene();
    }
}