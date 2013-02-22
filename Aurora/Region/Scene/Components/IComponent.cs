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
using OpenMetaverse.StructuredData;

namespace OpenSim.Region.Framework.Scenes.Components
{
    public interface IComponent
    {
        /// <summary>
        ///   The type of the Component, only one of each 'type' can be loaded.
        /// </summary>
        Type BaseType { get; }

        /// <summary>
        ///   Name of this Component
        /// </summary>
        string Name { get; }

        /// <summary>
        ///   A representation of the current state of the Component
        /// </summary>
        /// <param name = "obj">The object to get the value from</param>
        /// <returns></returns>
        OSD GetState(UUID obj, bool copyComponent);

        /// <summary>
        ///   A representation of the current state of the Component
        /// </summary>
        /// <param name = "obj">The object to get the value from</param>
        /// <returns></returns>
        OSD GetState(UUID obj);

        /// <summary>
        ///   Update the state of the Component
        /// </summary>
        /// <param name = "obj">The object being edited</param>
        /// <param name = "osd">The value as an OSD</param>
        void SetState(UUID obj, OSD osd);

        /// <summary>
        ///   Removes the state for the given object
        /// </summary>
        /// <param name = "obj"></param>
        void RemoveState(UUID obj);
    }
}