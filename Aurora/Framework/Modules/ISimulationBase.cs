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

using Aurora.Framework.Servers.HttpServer.Interfaces;
using Aurora.Framework.Services.ClassHelpers.Other;
using Nini.Config;
using System;

namespace Aurora.Framework.Modules
{
    public interface ISimulationBase
    {
        /// <summary>
        ///     Get the configuration settings
        /// </summary>
        IConfigSource ConfigSource { get; set; }

        /// <summary>
        ///     Get the base instance of the Application (Module) Registry
        /// </summary>
        IRegistryCore ApplicationRegistry { get; }

        /// <summary>
        ///     The time this instance was started
        /// </summary>
        DateTime StartupTime { get; }

        /// <summary>
        ///     The event manager for the simulation base
        /// </summary>
        AuroraEventManager EventManager { get; }

        /// <summary>
        ///     The version string of Aurora
        /// </summary>
        string Version { get; }

        /// <summary>
        ///     All parameters that were passed by the command line when Aurora started
        /// </summary>
        string[] CommandLineParameters { get; }

        /// <summary>
        ///     Get an instance of the HTTP server on the given port
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        IHttpServer GetHttpServer(uint port);

        /// <summary>
        ///     Run any startup commands that may need to be run
        /// </summary>
        void RunStartupCommands();

        /// <summary>
        ///     Run the commands in the given file
        /// </summary>
        /// <param name="p"></param>
        void RunCommandScript(string p);

        /// <summary>
        ///     Shut down the simulation and close
        /// </summary>
        /// <param name="shouldForceExit">Runs Environment.Exit(0) if true</param>
        void Shutdown(bool shouldForceExit);

        /// <summary>
        ///     Make a copy of the simulation base
        /// </summary>
        /// <returns></returns>
        ISimulationBase Copy();

        /// <summary>
        ///     Start the base with the given parametsr
        /// </summary>
        /// <param name="originalConfigSource">The settings parsed from the command line</param>
        /// <param name="configSource">The .ini config</param>
        /// <param name="cmdParameters"></param>
        /// <param name="configLoader"></param>
        void Initialize(IConfigSource originalConfigSource, IConfigSource configSource, string[] cmdParameters,
                        ConfigurationLoader configLoader);

        /// <summary>
        ///     Start up any modules and run the HTTP server
        /// </summary>
        void Startup();

        /// <summary>
        ///     Start console processing
        /// </summary>
        void Run();
    }
}