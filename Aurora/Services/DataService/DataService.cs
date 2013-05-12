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

//using Aurora.DataManager.MSSQL;
using Aurora.DataManager.MySQL;
using Aurora.DataManager.SQLite;
using Aurora.Framework;
using Aurora.Framework.ConsoleFramework;
using Aurora.Framework.ModuleLoader;
using Aurora.Framework.Modules;
using Aurora.Framework.Services;
using Nini.Config;
using System;
using System.Collections.Generic;

namespace Aurora.Services.DataService
{
    public class DataService
    {
        public void Initialise(IConfigSource source, IRegistryCore simBase)
        {
            List<IAuroraDataPlugin> Plugins = AuroraModuleLoader.PickupModules<IAuroraDataPlugin>();
            foreach (IAuroraDataPlugin plugin in Plugins)
            {
                InitializeDataPlugin(source, simBase, plugin);
            }
        }

        public void Initialise(IConfigSource source, IRegistryCore simBase, List<Type> types)
        {
            foreach (Type t in types)
            {
                List<dynamic> Plugins = AuroraModuleLoader.PickupModules(t);
                foreach (dynamic plugin in Plugins)
                {
                    InitializeDataPlugin(source, simBase, plugin);
                }
            }
        }

        private void InitializeDataPlugin(IConfigSource source, IRegistryCore simBase, IAuroraDataPlugin plugin)
        {
            try
            {
                IConfig config = source.Configs["AuroraConnectors"];
                if (config.GetString(plugin.InterfaceName, "LocalConnector") != "LocalConnector")
                    return;
                plugin.Initialize(CreateDataService(plugin.InterfaceName, source), source, simBase);
                Framework.Utilities.DataManager.RegisterPlugin(plugin);
            }
            catch (Exception ex)
            {
                if (MainConsole.Instance != null)
                    MainConsole.Instance.Warn("[DataService]: Exeception occured starting data plugin " +
                                              plugin.InterfaceName + ", " + ex.ToString());
            }
        }

        private IGenericData CreateDataService(string pluginName, IConfigSource source)
        {

            IConfig config = source.Configs["AuroraData"];
            string storageProvider, connectionString;
            if (config != null)
            {
                storageProvider = config.GetString("StorageProvider");
                connectionString = config.GetString("ConnectionString");
            }
            else
                return null;
            if (source.Configs[pluginName] != null)
                connectionString = source.Configs[pluginName].GetString("ConnectionString", connectionString);

            IGenericData DataConnector = null;
            if (storageProvider == "MySQL")
                DataConnector = new MySQLDataLoader();
            else if (storageProvider == "SQLite")
                DataConnector = new SQLiteLoader();
            else
                return null;

            DataConnector.ConnectToDatabase(connectionString);

            return DataConnector;
        }
    }
}