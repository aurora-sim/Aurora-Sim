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
using Aurora.Framework.Modules;
using Aurora.Framework.Services;
using Aurora.Framework.Utilities;
using Nini.Config;
using OpenMetaverse;

namespace Aurora.Services.DataService.Connectors.Database.Scheduler
{
    public class LocalSchedulerConnector : ISchedulerDataPlugin
    {
        private IGenericData m_Gd;

        private readonly string[] theFields = new[]
                                                  {
                                                      "id", "fire_function", "fire_params", "run_once", "run_every",
                                                      "runs_next", "keep_history", "require_reciept", "last_history_id",
                                                      "create_time", "start_time", "run_every_type", "enabled",
                                                      "schedule_for"
                                                  };

        #region Implementation of IAuroraDataPlugin

        /// <summary>
        ///     Returns the plugin name
        /// </summary>
        /// <returns></returns>
        public string Name
        {
            get { return "ISchedulerDataPlugin"; }
        }

        /// <summary>
        ///     Starts the database plugin, performs migrations if needed
        /// </summary>
        /// <param name="GenericData">The Database Plugin</param>
        /// <param name="source">Config if more parameters are needed</param>
        /// <param name="simBase"></param>
        /// <param name="DefaultConnectionString">The connection string to use</param>
        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase,
                               string DefaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("SchedulerConnector", "LocalConnector") != "LocalConnector")
                return;

            if (source.Configs[Name] != null)
                DefaultConnectionString = source.Configs[Name].GetString("ConnectionString", DefaultConnectionString);
            if (GenericData != null)
                GenericData.ConnectToDatabase(DefaultConnectionString, "Scheduler",
                                              source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

            m_Gd = GenericData;
            Framework.Utilities.DataManager.RegisterPlugin(this);
        }

        #endregion

        #region Implementation of ISchedulerDataPlugin

        public string SchedulerSave(SchedulerItem I)
        {
            object[] dbv = GetDBValues(I);
            Dictionary<string, object> values = new Dictionary<string, object>(dbv.Length);
            int i = 0;
            foreach (object value in dbv)
            {
                values[theFields[i++]] = value;
            }
            if (SchedulerExist(I.id))
            {
                QueryFilter filter = new QueryFilter();
                filter.andFilters["id"] = I.id;

                m_Gd.Update("scheduler", values, null, filter, null, null);
            }
            else
            {
                m_Gd.Insert("scheduler", values);
            }
            return I.id;
        }

        public void SchedulerRemove(string id)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["id"] = id;
            m_Gd.Delete("scheduler", filter);
        }

        private object[] GetDBValues(SchedulerItem I)
        {
            return new object[]
                       {
                           I.id,
                           I.FireFunction,
                           I.FireParams,
                           I.RunOnce,
                           I.RunEvery,
                           I.TimeToRun,
                           I.HisotryKeep,
                           I.HistoryReciept,
                           I.HistoryLastID,
                           I.CreateTime,
                           I.StartTime,
                           (int) I.RunEveryType,
                           I.Enabled,
                           I.ScheduleFor
                       };
        }

        public bool SchedulerExist(string id)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["id"] = id;
            return m_Gd.Query(new string[] {"id"}, "scheduler", filter, null, null, null).Count >= 1;
        }


        public List<SchedulerItem> ToRun()
        {
            List<SchedulerItem> returnValue = new List<SchedulerItem>();
            DataReaderConnection dr = null;
            try
            {
                dr =
                    m_Gd.QueryData(
                        "WHERE enabled = 1 AND runs_next < '" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") +
                        "' ORDER BY runs_next desc", "scheduler", string.Join(", ", theFields));
                if (dr != null && dr.DataReader != null)
                {
                    while (dr.DataReader.Read())
                    {
                        returnValue.Add(LoadFromDataReader(dr.DataReader));
                    }
                }
            }
            catch
            {
            }
            finally
            {
                m_Gd.CloseDatabase(dr);
            }

            return returnValue;
        }

        public SchedulerItem SaveHistory(SchedulerItem I)
        {
            string his_id = UUID.Random().ToString();

            Dictionary<string, object> row = new Dictionary<string, object>(7);
            row["id"] = his_id;
            row["scheduler_id"] = I.id;
            row["ran_time"] = DateTime.UtcNow;
            row["run_time"] = I.TimeToRun;
            row["is_complete"] = 0;
            row["complete_time"] = DateTime.UtcNow;
            row["reciept"] = "";
            m_Gd.Insert("scheduler_history", row);

            I.HistoryLastID = his_id;
            return I;
        }

        public SchedulerItem SaveHistoryComplete(SchedulerItem I)
        {
            Dictionary<string, object> values = new Dictionary<string, object>(3);
            values["is_complete"] = true;
            values["complete_time"] = DateTime.UtcNow;
            values["reciept"] = "";

            QueryFilter filter = new QueryFilter();
            filter.andFilters["id"] = I.HistoryLastID;

            m_Gd.Update("scheduler_history", values, null, filter, null, null);

            return I;
        }

        public void SaveHistoryCompleteReciept(string historyID, string reciept)
        {
            Dictionary<string, object> values = new Dictionary<string, object>(3);
            values["is_complete"] = 1;
            values["complete_time"] = DateTime.UtcNow;
            values["reciept"] = reciept;

            QueryFilter filter = new QueryFilter();
            filter.andFilters["id"] = historyID;

            m_Gd.Update("scheduler_history", values, null, filter, null, null);
        }

        public void HistoryDeleteOld(SchedulerItem I)
        {
            if ((I.id != "") && (I.HistoryLastID != ""))
            {
                QueryFilter filter = new QueryFilter();
                filter.andNotFilters["id"] = I.HistoryLastID;
                filter.andFilters["scheduler_id"] = I.id;
                m_Gd.Delete("scheduler_history", filter);
            }
        }

        public SchedulerItem Get(string id)
        {
            if (id != "")
            {
                QueryFilter filter = new QueryFilter();
                filter.andFilters["id"] = id;
                List<string> results = m_Gd.Query(theFields, "scheduler", filter, null, null, null);
                return LoadFromList(results);
            }
            return null;
        }

        public SchedulerItem Get(string scheduleFor, string fireFunction)
        {
            if (scheduleFor != UUID.Zero.ToString())
            {
                QueryFilter filter = new QueryFilter();
                filter.andFilters["schedule_for"] = scheduleFor;
                filter.andFilters["fire_function"] = fireFunction;
                List<string> results = m_Gd.Query(theFields, "scheduler", filter, null, null, null);
                return LoadFromList(results);
            }
            return null;
        }

        private SchedulerItem LoadFromDataReader(IDataReader dr)
        {
            return new SchedulerItem
                       {
                           id = dr["id"].ToString(),
                           FireFunction = dr["fire_function"].ToString(),
                           FireParams = dr["fire_params"].ToString(),
                           HisotryKeep = bool.Parse(dr["keep_history"].ToString()),
                           Enabled = bool.Parse(dr["enabled"].ToString()),
                           CreateTime = DateTime.Parse(dr["create_time"].ToString()),
                           HistoryLastID = dr["last_history_id"].ToString(),
                           TimeToRun = DateTime.Parse(dr["runs_next"].ToString()),
                           HistoryReciept = bool.Parse(dr["require_reciept"].ToString()),
                           RunEvery = int.Parse(dr["run_every"].ToString()),
                           RunOnce = bool.Parse(dr["run_once"].ToString()),
                           RunEveryType = (RepeatType) int.Parse(dr["run_every_type"].ToString()),
                           StartTime = DateTime.Parse(dr["start_time"].ToString()),
                           ScheduleFor = UUID.Parse(dr["schedule_for"].ToString())
                       };
        }

        private SchedulerItem LoadFromList(List<string> values)
        {
            if (values == null) return null;
            if (values.Count == 0) return null;
            return new SchedulerItem
                       {
                           id = values[0],
                           FireFunction = values[1],
                           FireParams = values[2],
                           RunOnce = bool.Parse(values[3]),
                           RunEvery = int.Parse(values[4]),
                           TimeToRun = DateTime.Parse(values[5]),
                           HisotryKeep = bool.Parse(values[6]),
                           HistoryReciept = bool.Parse(values[7]),
                           HistoryLastID = values[8],
                           CreateTime = DateTime.Parse(values[9]),
                           StartTime = DateTime.Parse(values[10]),
                           RunEveryType = (RepeatType) int.Parse(values[11]),
                           Enabled = bool.Parse(values[12]),
                           ScheduleFor = UUID.Parse(values[13])
                       };
        }

        #endregion
    }
}