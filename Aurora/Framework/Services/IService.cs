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

using Aurora.Framework.Modules;
using Nini.Config;

namespace Aurora.Framework.Services
{
    /// <summary>
    ///     IService is a module that loads up by default and is loaded on every startup by either OpenSim.exe or Aurora.Server.exe
    ///     It loads modules including IAssetService and others
    /// </summary>
    public interface IService
    {
        /// <summary>
        ///     Set up and register the module
        ///     NOTE: Do NOT load module interfaces from this method, wait until PostInit runs
        ///     NOTE: This is normally used to set up the 'base' services, ones that should be used in standalone or Aurora.Server
        /// </summary>
        /// <param name="config">Config file</param>
        /// <param name="registry">Place to register the modules into</param>
        void Initialize(IConfigSource config, IRegistryCore registry);

        /// <summary>
        ///     Load other IService modules now that this is set up
        ///     NOTE: This is normally used to load remote connectors for remote grid mode
        /// </summary>
        /// <param name="config">Config file</param>
        /// <param name="registry">Place to register and retrieve module interfaces</param>
        void Start(IConfigSource config, IRegistryCore registry);

        /// <summary>
        ///     All modules have started up and it is ready to run
        /// </summary>
        void FinishedStartup();
    }
}