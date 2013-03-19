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
using System.Linq;

namespace Aurora.Framework.Utilities
{
    public class RegistryCore : IRegistryCore
    {
        /// <value>
        ///     The module interfaces available from this scene.
        /// </value>
        protected Dictionary<Type, List<object>> ModuleInterfaces = new Dictionary<Type, List<object>>();

        #region IRegistryCore Members

        /// <summary>
        ///     Register an interface to a region module.  This allows module methods to be called directly as
        ///     well as via events.  If there is already a module registered for this interface, it is not replaced
        ///     (is this the best behaviour?)
        /// </summary>
        /// <param name="mod"></param>
        public void RegisterModuleInterface<T>(T mod)
        {
            //            MainConsole.Instance.DebugFormat("[SCENE BASE]: Registering interface {0}", typeof(M));

            List<Object> l = null;
            if (!ModuleInterfaces.TryGetValue(typeof (T), out l))
            {
                l = new List<Object>();
                ModuleInterfaces.Add(typeof (T), l);
            }

            if (l.Count > 0)
                l.Clear();

            l.Add(mod);
        }

        public void AddModuleInterfaces(Dictionary<Type, List<object>> dictionary)
        {
            foreach (KeyValuePair<Type, List<object>> kvp in dictionary)
            {
                if (!ModuleInterfaces.ContainsKey(kvp.Key))
                    ModuleInterfaces.Add(kvp.Key, kvp.Value);
                else
                    ModuleInterfaces[kvp.Key].AddRange(kvp.Value);
            }
        }

        public void UnregisterModuleInterface<T>(T mod)
        {
            List<Object> l;
            if (ModuleInterfaces.TryGetValue(typeof (T), out l))
            {
                l.Remove(mod);
            }
        }

        public void StackModuleInterface<T>(T mod)
        {
            List<Object> l;
            l = ModuleInterfaces.ContainsKey(typeof (T)) ? ModuleInterfaces[typeof (T)] : new List<Object>();

            if (l.Contains(mod))
                return;

            l.Add(mod);

            ModuleInterfaces[typeof (T)] = l;
        }

        /// <summary>
        ///     For the given interface, retrieve the region module which implements it.
        /// </summary>
        /// <returns>null if there is no registered module implementing that interface</returns>
        public T RequestModuleInterface<T>()
        {
            if (ModuleInterfaces.ContainsKey(typeof (T)) &&
                (ModuleInterfaces[typeof (T)].Count > 0))
                return (T) ModuleInterfaces[typeof (T)][0];
            else
                return default(T);
        }

        public bool TryRequestModuleInterface<T>(out T iface)
        {
            iface = default(T);
            if (ModuleInterfaces.ContainsKey(typeof (T)) &&
                (ModuleInterfaces[typeof (T)].Count > 0))
            {
                iface = (T) ModuleInterfaces[typeof (T)][0];
                return true;
            }
            else
                return false;
        }

        /// <summary>
        ///     For the given interface, retrieve an array of region modules that implement it.
        /// </summary>
        /// <returns>an empty array if there are no registered modules implementing that interface</returns>
        public T[] RequestModuleInterfaces<T>()
        {
            if (ModuleInterfaces.ContainsKey(typeof (T)))
            {
#if (!ISWIN)
                List<T> ret = new List<T>();

                foreach (Object o in ModuleInterfaces[typeof(T)])
                    ret.Add((T)o);
                return ret.ToArray();
#else
                return ModuleInterfaces[typeof (T)].Select(o => (T) o).ToArray();
#endif
            }
            else
            {
                return new[] {default(T)};
            }
        }

        public Dictionary<Type, List<object>> GetInterfaces()
        {
            //Flatten the array
#if (!ISWIN)
            Dictionary<Type, List<object>> dictionary = new Dictionary<Type, List<object>>();
            foreach (KeyValuePair<Type, List<object>> @interface in ModuleInterfaces)
                dictionary.Add(@interface.Key, @interface.Value);
            return dictionary;
#else
            return ModuleInterfaces.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
#endif
        }

        public void RemoveAllInterfaces()
        {
            ModuleInterfaces.Clear();
        }

        #endregion
    }
}