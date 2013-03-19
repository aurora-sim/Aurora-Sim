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

using System.Collections.Generic;

namespace Aurora.Framework
{
    public interface IGridRegisterModule
    {
        /// <summary>
        ///     Update the grid server with new info about this region
        /// </summary>
        /// <param name="scene"></param>
        void UpdateGridRegion(IScene scene);

        /// <summary>
        ///     Register this region with the grid service
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="returnResultImmediately">If false, it will walk the user through how to fix potential issues</param>
        /// <param name="continueTrying">Continue trying to register until it succeeds</param>
        /// <param name="password"> </param>
        /// <returns>Success</returns>
        bool RegisterRegionWithGrid(IScene scene, bool returnResultImmediately, bool continueTrying, string password);

        /// <summary>
        ///     Add this generic info to all registering regions
        /// </summary>
        /// <param name="key"> </param>
        /// <param name="value"> </param>
        void AddGenericInfo(string key, string value);

        /// <summary>
        ///     Get the neighbors of the given region
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        List<GridRegion> GetNeighbors(IScene scene);
    }
}