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
using Nini.Config;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Framework;

namespace Aurora.Simulation.Base
{
    /// <summary>
    /// IService is a module that loads up by default and is loaded on every startup by either OpenSim.exe or Aurora.Server.exe
    /// It loads modules including IAssetService and others
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// Set up and register the module
        /// NOTE: Do NOT load module interfaces from this method, wait until PostInit runs
        /// </summary>
        /// <param name="config">Config file</param>
        /// <param name="registry">Place to register the modules into</param>
        void Initialize(IConfigSource config, IRegistryCore registry);

        /// <summary>
        /// Finish setting up any module pieces
        /// NOTE: Do NOT load module interfaces from this method, wait until PostInit runs
        /// </summary>
        /// <param name="config">Config file</param>
        /// <param name="registry">Place to register the modules into</param>
        void PostInitialize(IConfigSource config, IRegistryCore registry);

        /// <summary>
        /// Load other IService modules now that this is set up
        /// </summary>
        /// <param name="config">Config file</param>
        /// <param name="registry">Place to register and retrieve module interfaces</param>
        void Start(IConfigSource config, IRegistryCore registry);

        /// <summary>
        /// If there is a reason that another registry needs added, we call this to register the relevent interfaces
        /// </summary>
        /// <param name="config">Config file</param>
        /// <param name="registry">Place to register and retrieve module interfaces</param>
        void AddNewRegistry(IConfigSource config, IRegistryCore registry);
    }
}
