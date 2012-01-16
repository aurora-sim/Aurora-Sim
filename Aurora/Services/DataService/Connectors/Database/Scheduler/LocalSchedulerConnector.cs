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
using System.Data;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Services.Interfaces;

namespace Aurora.Services.DataService.Connectors.Database.Scheduler
{
    public class LocalSchedulerConnector : ISchedulerDataPlugin
    {
        private IGenericData m_Gd;

        private readonly string[] theFields = new[]
                                         {
                                             "id", "fire_function", "fire_params", "run_once", "run_every",
                                             "runs_next", "keep_history", "require_reciept", "last_history_id", 
                                             "create_time", "enabled"
                                         };

        #region Implementation of IAuroraDataPlugin

        /// <summary>
        ///   Returns the plugin name
        /// </summary>
        /// <returns></returns>
        public string Name
        {
            get { return "ISchedulerDataPlugin"; }
        }

        /// <summary>
        ///   Starts the database plugin, performs migrations if needed
        /// </summary>
        /// <param name = "GenericData">The Database Plugin</param>
        /// <param name = "source">Config if more parameters are needed</param>
        /// <param name="simBase"></param>
        /// <param name = "DefaultConnectionString">The connection string to use</param>
        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string DefaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("SchedulerConnector", "LocalConnector") != "LocalConnector")
                return;

            if (source.Configs[Name] != null)
                DefaultConnectionString = source.Configs[Name].GetString("ConnectionString", DefaultConnectionString);
            GenericData.ConnectToDatabase(DefaultConnectionString, "Scheduler",
                                          source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

            m_Gd = GenericData;
            DataManager.DataManager.RegisterPlugin(this);
        }

        #endregion

        #region Implementation of ISchedulerDataPlugin

        public string SchedulerSave(SchedulerItem I)
        {
            if (SchedulerExist(I.id))
            {
                m_Gd.Update("scheduler", GetDBValues(I), theFields, new[] { "id" }, new object[] { I.id });
            }
            else
            {
                m_Gd.Insert("scheduler", theFields, GetDBValues(I));
            }
            return I.id;
        }

        public void SchedulerRemove(string id)
        {
            m_Gd.Delete("scheduler", new[] { "id" }, new object[] { id });
        }

        private object[] GetDBValues(SchedulerItem I)
        {
            return new object[]
                       {
                           I.id, I.FireFunction, I.FireParams, (I.RunOnce)?1:0, I.RunEvery, Util.ToUnixTime(I.TimeToRun), 
                           (I.HisotryKeep)?1:0,(I.HistoryReciept)?1:0, I.HistoryLastID, Util.ToUnixTime(I.CreateTime), 
                           (I.Enabled)?1:0
                       };
        }

        public bool SchedulerExist(string id)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["id"] = id;
            return m_Gd.Query(new string[] { "id" }, "scheduler", filter, null, null, null).Count >= 1;
        }


        public List<SchedulerItem> ToRun()
        {
            List<SchedulerItem> returnValue = new List<SchedulerItem>();
            IDataReader dr = null;
            try
            {
                dr =  m_Gd.QueryData("WHERE enabled = 1 AND runs_next < " + Util.ToUnixTime(DateTime.UtcNow) + " ORDER BY runs_next desc", "scheduler", string.Join(", ", theFields));
                if (dr != null)
                {
                    while (dr.Read())
                    {
                        returnValue.Add(LoadFromDataReader(dr));
                    }
                }
            }
            catch{}
            finally
            {
                if (dr != null) dr.Close();
            }
            
            return returnValue;
        }

        public SchedulerItem SaveHistory(SchedulerItem I)
        {
            string his_id = UUID.Random().ToString();
            m_Gd.Insert("scheduler_history",
                        new[]
                            {"id", "scheduler_id", "ran_time", "run_time", "is_complete", "complete_time", "reciept"},
                        new object[]
                            {his_id, I.id, Util.ToUnixTime(DateTime.UtcNow), Util.ToUnixTime(I.TimeToRun), 0, 0, ""}
                );
            I.HistoryLastID = his_id;
            return I;
        }

        public SchedulerItem SaveHistoryComplete(SchedulerItem I)
        {
            m_Gd.Update("scheduler_history", new object[] { 1, Util.ToUnixTime(I.TimeToRun), "" },
                        new[] { "is_complete", "complete_time", "reciept" }, new[] { "id" },
                        new object[] { I.HistoryLastID });
            return I;
        }

        public void SaveHistoryCompleteReciept(string historyID, string reciept)
        {
            m_Gd.Update("Scheduler_history", new object[] { 1, Util.ToUnixTime(DateTime.UtcNow), reciept },
                        new[] { "is_complete", "complete_time", "reciept" }, new[] { "id" },
                        new object[] { historyID });
        }

        public void HistoryDeleteOld(SchedulerItem I)
        {
            if ((I.id != "") && (I.HistoryLastID != ""))
                m_Gd.Delete("scheduler_history", "WHERE id != '" + I.HistoryLastID + "' AND scheduler_id = '" + I.id + "'");
        }

        private SchedulerItem LoadFromDataReader(IDataReader dr)
        {
            return new SchedulerItem
                       {
                           id = dr["id"].ToString(),
                           FireFunction = dr["fire_function"].ToString(),
                           FireParams = dr["fire_params"].ToString(),
                           HisotryKeep = (dr["keep_history"].ToString() == "1"),
                           Enabled = (dr["enabled"].ToString() == "1"),
                           CreateTime = UnixTimeStampToDateTime(int.Parse(dr["create_time"].ToString())),
                           HistoryLastID = dr["last_history_id"].ToString(),
                           TimeToRun = UnixTimeStampToDateTime(int.Parse(dr["runs_next"].ToString())),
                           HistoryReciept = (dr["require_reciept"].ToString() == "1"),
                           RunEvery = int.Parse(dr["run_every"].ToString()),
                           RunOnce = (dr["run_once"].ToString() == "1")
                       };
        }

        #endregion

        #region util functions

        private static DateTime UnixTimeStampToDateTime(int unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        #endregion
    }
}
