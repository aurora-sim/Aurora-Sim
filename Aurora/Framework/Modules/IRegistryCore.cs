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
using System.Collections.Generic;

namespace Aurora.Framework.Modules
{
    public interface IRegistryCore
    {
        /// <summary>
        ///     Register a module into the registry.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mod"></param>
        void RegisterModuleInterface<T>(T mod);

        /// <summary>
        ///     Add more than one module interface.
        ///     This is usually used to copy the contents of one registry core into another
        /// </summary>
        /// <param name="dictionary"></param>
        void AddModuleInterfaces(Dictionary<Type, List<object>> dictionary);

        /// <summary>
        ///     Remove a module from the registry.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mod"></param>
        void UnregisterModuleInterface<T>(T mod);

        /// <summary>
        ///     Add more than one module to the same interface in the registry.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mod"></param>
        void StackModuleInterface<T>(T mod);

        /// <summary>
        ///     Get a module from the interface by type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T RequestModuleInterface<T>();

        /// <summary>
        ///     Try to get a module from the interface by type.
        ///     Returns false if it could not find a module.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="iface"></param>
        /// <returns></returns>
        bool TryRequestModuleInterface<T>(out T iface);

        /// <summary>
        ///     Get all the modules for the given interface.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T[] RequestModuleInterfaces<T>();

        /// <summary>
        ///     Get all the modules in the registry.
        /// </summary>
        /// <returns></returns>
        Dictionary<Type, List<object>> GetInterfaces();

        /// <summary>
        ///     Removes all interfaces from the registry
        /// </summary>
        void RemoveAllInterfaces();
    }
}