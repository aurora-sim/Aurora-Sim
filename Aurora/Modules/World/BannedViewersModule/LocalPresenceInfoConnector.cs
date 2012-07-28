/*
 * Copyright 2011 Matthew Beardmore
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
using System.Reflection;
using System.Text;
using Nini.Config;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;

namespace Aurora.Modules.Ban
{
    public class LocalPresenceInfoConnector : IPresenceInfo
	{
        private IGenericData GD = null;
        private string DatabaseToAuthTable = "auth";

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore registry, string DefaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("PresenceInfoConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                if (source.Configs[Name] != null)
                {
                    DefaultConnectionString = source.Configs[Name].GetString("ConnectionString", DefaultConnectionString);
                    DatabaseToAuthTable = source.Configs[Name].GetString("DatabasePathToAuthTable", DatabaseToAuthTable);
                }
                GD.ConnectToDatabase(DefaultConnectionString, "PresenceInfo", true);
                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IPresenceInfo"; }
        }

        public void Dispose()
        {
        }

        public PresenceInfo GetPresenceInfo(UUID agentID)
		{
            PresenceInfo agent = new PresenceInfo();
            Dictionary<string, object> where = new Dictionary<string, object>(1);
            where["AgentID"] = agentID;
            List<string> query = GD.Query(new[] { "*" }, "baninfo", new QueryFilter
            {
                andFilters = where
            }, null, null, null);

            if (query.Count == 0) //Couldn't find it, return null then.
            {
                return null;
            }

            agent.AgentID = agentID;
            if (query[1] != "")
            {
                agent.Flags = (PresenceInfo.PresenceInfoFlags)Enum.Parse(typeof(PresenceInfo.PresenceInfoFlags), query[1]);
            }
            agent.KnownAlts = Util.ConvertToList(query[2]);
            agent.KnownID0s = Util.ConvertToList(query[3]);
            agent.KnownIPs = Util.ConvertToList(query[4]);
            agent.KnownMacs = Util.ConvertToList(query[5]);
            agent.KnownViewers = Util.ConvertToList(query[6]);
            agent.LastKnownID0 = query[7];
            agent.LastKnownIP = query[8];
            agent.LastKnownMac = query[9];
            agent.LastKnownViewer = query[10];
            agent.Platform = query[11];
            
			return agent;
		}

        public void UpdatePresenceInfo(PresenceInfo agent)
		{
            Dictionary<string, object> row = new Dictionary<string, object>(12);
            row["AgentID"] = agent.AgentID;
            row["Flags"] = agent.Flags;
            row["KnownAlts"] = Util.ConvertToString(agent.KnownAlts);
            row["KnownID0s"] = Util.ConvertToString(agent.KnownID0s);
            row["KnownIPs"] = Util.ConvertToString(agent.KnownIPs);
            row["KnownMacs"] = Util.ConvertToString(agent.KnownMacs);
            row["KnownViewers"] = Util.ConvertToString(agent.KnownViewers);
            row["LastKnownID0"] = agent.LastKnownID0;
            row["LastKnownIP"] = agent.LastKnownIP;
            row["LastKnownMac"] = agent.LastKnownMac;
            row["LastKnownViewer"] = agent.LastKnownViewer;
            row["Platform"] = agent.Platform;
            GD.Replace("baninfo", row);
        }

        public void Check(List<string> viewers, bool includeList)
        {
            List<string> query = GD.Query(new[] { "AgentID" }, "baninfo", new QueryFilter(), null, null, null);
            foreach (string ID in query)
            {
                //Check all
                Check(GetPresenceInfo(UUID.Parse(ID)), viewers, includeList);
            }
        }

        public void Check (PresenceInfo info, List<string> viewers, bool includeList)
        {
            //
            //Check passwords
            //Check IPs, Mac's, etc
            //

            bool needsUpdated = false;

            #region Check Password

            QueryFilter filter = new QueryFilter();
            filter.andFilters["UUID"] = info.AgentID;

            List<string> query = GD.Query(new[] { "passwordHash" }, DatabaseToAuthTable, filter, null, null, null);

            if (query.Count != 0)
            {
                filter = new QueryFilter();
                filter.andFilters["passwordHash"] = query[0];
                query = GD.Query(new[] { "UUID" }, DatabaseToAuthTable, filter, null, null, null);

                foreach (string ID in query)
                {
                    PresenceInfo suspectedInfo = GetPresenceInfo(UUID.Parse(ID));
                    if (suspectedInfo.AgentID == info.AgentID)
                    {
                        continue;
                    }

                    CoralateLists (info, suspectedInfo);

                    needsUpdated = true;
                }
            }

            #endregion

            #region Check ID0, IP, Mac, etc

            //Only check suspected and known offenders in this scan
            // 2 == Flags

            filter = new QueryFilter();
            query = GD.Query(new[] { "AgentID" }, "baninfo", filter, null, null, null);

            foreach (string ID in query)
            {
                PresenceInfo suspectedInfo = GetPresenceInfo(UUID.Parse(ID));
                if (suspectedInfo == null || suspectedInfo.AgentID == info.AgentID)
                    continue;
                foreach (string ID0 in suspectedInfo.KnownID0s)
                {
                    if (info.KnownID0s.Contains(ID0))
                    {
                        CoralateLists (info, suspectedInfo);
                        needsUpdated = true;
                    }
                }
                foreach (string IP in suspectedInfo.KnownIPs)
                {
                    if (info.KnownIPs.Contains(IP.Split(':')[0]))
                    {
                        CoralateLists (info, suspectedInfo);
                        needsUpdated = true;
                    }
                }
                foreach (string Mac in suspectedInfo.KnownMacs)
                {
                    if (info.KnownMacs.Contains(Mac))
                    {
                        CoralateLists (info, suspectedInfo);
                        needsUpdated = true;
                    }
                }
            }

            foreach (string viewer in info.KnownViewers)
            {
                if (IsViewerBanned(viewer, includeList, viewers))
                {
                    if ((info.Flags & PresenceInfo.PresenceInfoFlags.Clean) == PresenceInfo.PresenceInfoFlags.Clean)
                    {
                        //Update them to suspected for their viewer
                        AddFlag (ref info, PresenceInfo.PresenceInfoFlags.Suspected);
                        //And update them later
                        needsUpdated = true;
                    }
                    else if ((info.Flags & PresenceInfo.PresenceInfoFlags.Suspected) == PresenceInfo.PresenceInfoFlags.Suspected)
                    {
                        //Suspected, we don't really want to move them higher than this...
                    }
                    else if ((info.Flags & PresenceInfo.PresenceInfoFlags.Known) == PresenceInfo.PresenceInfoFlags.Known)
                    {
                        //Known, can't update anymore
                    }
                }
            }
            if (DoGC(info) & !needsUpdated)//Clean up all info
                needsUpdated = true;

            #endregion

            //Now update ours
            if (needsUpdated)
                UpdatePresenceInfo(info);
        }

        public bool IsViewerBanned(string name, bool include, List<string> list)
        {
            if (include)
            {
                if (!list.Contains(name))
                    return true;
            }
            else
            {
                if (list.Contains(name))
                    return true;
            }
            return false;
        }

        private bool DoGC(PresenceInfo info)
        {
            bool update = false;
            List<string> newIPs = new List<string>();
            foreach (string ip in info.KnownIPs)
            {
                string[] split;
                string newIP = ip;
                if ((split = ip.Split(':')).Length > 1)
                {
                    //Remove the port if it exists and force an update
                    newIP = split[0];
                    update = true;
                }
                if (!newIPs.Contains(newIP))
                    newIPs.Add(newIP);
            }
            if (info.KnownIPs.Count != newIPs.Count)
                update = true;
            info.KnownIPs = newIPs;

            return update;
        }

        private void CoralateLists (PresenceInfo info, PresenceInfo suspectedInfo)
        {
            bool addedFlag = false;
            const PresenceInfo.PresenceInfoFlags Flag = 0;

            if ((suspectedInfo.Flags & PresenceInfo.PresenceInfoFlags.Clean) == PresenceInfo.PresenceInfoFlags.Clean &&
                    (info.Flags & PresenceInfo.PresenceInfoFlags.Clean) == PresenceInfo.PresenceInfoFlags.Clean)
            {
                //They are both clean, do nothing
            }
            else if ((suspectedInfo.Flags & PresenceInfo.PresenceInfoFlags.Suspected) == PresenceInfo.PresenceInfoFlags.Suspected ||
                (info.Flags & PresenceInfo.PresenceInfoFlags.Suspected) == PresenceInfo.PresenceInfoFlags.Suspected)
            {
                //Suspected, update them both
                addedFlag = true;
                AddFlag (ref info, PresenceInfo.PresenceInfoFlags.Suspected);
                AddFlag (ref suspectedInfo, PresenceInfo.PresenceInfoFlags.Suspected);
            }
            else if ((suspectedInfo.Flags & PresenceInfo.PresenceInfoFlags.Known) == PresenceInfo.PresenceInfoFlags.Known ||
                (info.Flags & PresenceInfo.PresenceInfoFlags.Known) == PresenceInfo.PresenceInfoFlags.Known)
            {
                //Known, update them both
                addedFlag = true;
                AddFlag (ref info, PresenceInfo.PresenceInfoFlags.Known);
                AddFlag (ref suspectedInfo, PresenceInfo.PresenceInfoFlags.Known);
            }

            //Add the alt account flag
            AddFlag (ref info, PresenceInfo.PresenceInfoFlags.SuspectedAltAccount);
            AddFlag (ref suspectedInfo, PresenceInfo.PresenceInfoFlags.SuspectedAltAccount);

            if (suspectedInfo.Flags == PresenceInfo.PresenceInfoFlags.Suspected ||
                suspectedInfo.Flags == PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfSuspected ||
                info.Flags == PresenceInfo.PresenceInfoFlags.Suspected ||
                info.Flags == PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfSuspected)
            {
                //They might be an alt, but the other is clean, so don't bother them too much
                AddFlag (ref info, PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfSuspected);
                AddFlag (ref suspectedInfo, PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfSuspected);
            }
            else if (suspectedInfo.Flags == PresenceInfo.PresenceInfoFlags.Known ||
                suspectedInfo.Flags == PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfKnown ||
                info.Flags == PresenceInfo.PresenceInfoFlags.Known ||
                info.Flags == PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfKnown)
            {
                //Flag 'em
                AddFlag (ref info, PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfKnown);
                AddFlag (ref suspectedInfo, PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfKnown);
            }

            //Add the lists together
            List<string> alts = new List<string> ();
            foreach (string alt in info.KnownAlts)
            {
                if (!alts.Contains (alt))
                    alts.Add (alt);
            }
            foreach (string alt in suspectedInfo.KnownAlts)
            {
                if (!alts.Contains (alt))
                    alts.Add (alt);
            }
            if(!alts.Contains(suspectedInfo.AgentID.ToString()))
                alts.Add(suspectedInfo.AgentID.ToString());
            if (!alts.Contains(info.AgentID.ToString()))
                alts.Add(info.AgentID.ToString());

            //If we have added a flag, we need to update ALL alts as well
            if (addedFlag || alts.Count != 0)
            {
                foreach (string alt in alts.Where(s => s != suspectedInfo.AgentID.ToString() && s != info.AgentID.ToString()))
                {
                    PresenceInfo altInfo = GetPresenceInfo (UUID.Parse (alt));
                    if (altInfo != null)
                    {
                        //Give them the flag as well
                        AddFlag (ref altInfo, Flag);

                        //Add the alt account flag
                        AddFlag (ref altInfo, PresenceInfo.PresenceInfoFlags.SuspectedAltAccount);

                        //Also give them the flags for alts
                        if (suspectedInfo.Flags == PresenceInfo.PresenceInfoFlags.Suspected ||
                            suspectedInfo.Flags == PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfSuspected ||
                            info.Flags == PresenceInfo.PresenceInfoFlags.Suspected ||
                            info.Flags == PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfSuspected)
                        {
                            //They might be an alt, but the other is clean, so don't bother them too much
                            AddFlag (ref altInfo, PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfSuspected);
                        }
                        else if (suspectedInfo.Flags == PresenceInfo.PresenceInfoFlags.Known ||
                            suspectedInfo.Flags == PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfKnown ||
                            info.Flags == PresenceInfo.PresenceInfoFlags.Known ||
                            info.Flags == PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfKnown)
                        {
                            //Flag 'em
                            AddFlag (ref altInfo, PresenceInfo.PresenceInfoFlags.SuspectedAltAccountOfKnown);
                        }
                        altInfo.KnownAlts = new List<string>(alts.Where(s => s != altInfo.AgentID.ToString()));

                        //And update them in the db
                        UpdatePresenceInfo(altInfo);
                    }
                }
            }

            //Replace both lists now that they are merged
            info.KnownAlts = new List<string>(alts.Where(s => s != info.AgentID.ToString()));
            suspectedInfo.KnownAlts = new List<string>(alts.Where(s => s != suspectedInfo.AgentID.ToString()));

            //Update them, as we changed their info, we get updated below
            UpdatePresenceInfo (suspectedInfo);
        }

        private void AddFlag (ref PresenceInfo info, PresenceInfo.PresenceInfoFlags presenceInfoFlags)
        {
            if (presenceInfoFlags == 0)
                return;
            info.Flags &= PresenceInfo.PresenceInfoFlags.Clean; //Remove clean
            if (presenceInfoFlags == PresenceInfo.PresenceInfoFlags.Known)
                info.Flags &= PresenceInfo.PresenceInfoFlags.Clean; //Remove suspected as well
            info.Flags |= presenceInfoFlags; //Add the flag
        }
    }
}
