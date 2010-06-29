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

using System.Collections.Generic;
using System.Data;
using Nini.Config;
using OpenSim.Framework;

namespace OpenSim
{
    /// <summary>
    /// OpenSimulator Application Plugin framework interface
    /// </summary>
    public interface IApplicationPlugin: IPlugin
    {
        /// <summary>
        /// Initialize the Plugin
        /// </summary>
        /// <param name="openSim">The Application instance</param>
        void Initialise(IOpenSimBase openSim);

        /// <summary>
        /// Called when the application loading is completed 
        /// </summary>
        void PostInitialise();
    }

    public class ApplicationPluginInitialiser : PluginInitialiserBase
    {
        private IOpenSimBase m_server;
        protected List<IApplicationPlugin> m_plugins = new List<IApplicationPlugin>();

        public ApplicationPluginInitialiser(IOpenSimBase s)
        {
            m_server = s;
        }

        public override void Initialise(IPlugin plugin)
        {
            IApplicationPlugin p = plugin as IApplicationPlugin;
            p.Initialise(m_server);
            m_plugins.Add(p);
        }

        public void PostInitialise()
        {
            foreach (IApplicationPlugin plugin in m_plugins)
            {
                plugin.PostInitialise();
            }
        }
    }
}
namespace Aurora.Framework
{
    public interface IGenericData
    {
        /// <summary>
        /// update table set setRow = setValue WHERE keyRow = keyValue
        /// </summary>
        bool Update(string table, object[] setValues, string[] setRows, string[] keyRows, object[] keyValues);
        /// <summary>
        /// select wantedValue from table where keyRow = keyValue
        /// </summary>
        List<string> Query(string keyRow, object keyValue, string table, string wantedValue);
        List<string> Query(string whereClause, string table, string wantedValue);
        List<string> Query(string keyRow, object keyValue, string table, string wantedValue, string Order);
        List<string> Query(string[] keyRow, object[] keyValue, string table, string wantedValue);
        IDataReader QueryReader(string keyRow, object keyValue, string table, string wantedValue);
        bool Insert(string table, object[] values);
        bool Insert(string table, string[] keys, object[] values);
        bool Delete(string table, string[] keys, object[] values);
        bool Insert(string table, object[] values, string updateKey, object updateValue);
    }

    public interface IAuroraDataPlugin : IPlugin
    {
        void Initialise(IGenericData GenericData, IConfigSource source);
    }
}