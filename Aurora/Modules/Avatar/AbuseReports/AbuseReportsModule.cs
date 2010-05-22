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
using System.Collections.Generic;
using System.Reflection;
using System.Net.Mail;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Framework.Client;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Modules.AbuseReportsGUI;
using OpenSim.Framework.Console;
using System.Windows.Forms;

namespace Aurora.Modules
{
    public class AbuseReports : ISharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private bool enabled = false;
        private List<Scene> m_SceneList = new List<Scene>();
        private static char[] charSeparators = new char[] {  };
        
        public void Initialise(IConfigSource source)
        {
            IConfig cnf = source.Configs["AbuseReports"];
            if (cnf == null)
            {
                enabled = false;
                return;
            }
            enabled = cnf.GetBoolean("Enabled",true);
            if (!enabled)
            {
                return;
            }
            //m_log.Info("[ABUSE REPORTS MODULE] Enabled");
        }

        public void AddRegion(Scene scene)
        {
            lock (m_SceneList)
            {
                if (!m_SceneList.Contains(scene))
                    m_SceneList.Add(scene);
            }

            MainConsole.Instance.Commands.AddCommand("region", false, "open abusereportsGUI",
                                          "open abusereportsGUI",
                                          "Opens the abuse reports GUI", OpenGUI);
            scene.EventManager.OnNewClient += new EventManager.OnNewClientDelegate(EventManager_OnNewClient);
        }

        public void RemoveRegion(Scene scene)
        {

        }

        public void RegionLoaded(Scene scene)
        {

        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        protected void OpenGUI(string module, string[] cmdparams)
        {
            System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadProcARGUI));
            t.Start();
        }

        public void ThreadProcARGUI()
        {
            Application.Run(new Abuse());
        }

        void EventManager_OnNewClient(IClientAPI client)
        {
            client.OnUserReport += UserReport;
        }

        public void PostInitialise()
        {
        }

        public string Name
        {
            get { return "AbuseReportsModule"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }
        
        public void Close()
        {
        }

        private void UserReport(IClientAPI client, string regionName,UUID abuserID, byte catagory, byte checkflags, string details, UUID objectID, Vector3 position, byte reportType ,UUID screenshotID, string summery, UUID reporter)
        {
            AbuseReport report = new AbuseReport();
            report.ObjectUUID = objectID;
            report.ObjectPosition = position.ToString();
            report.Active = true;
            report.Checked = false;
            report.Notes = "";
            report.AssignedTo = "No One";
            report.ScreenshotID = screenshotID;
            if (objectID != UUID.Zero)
            {
                SceneObjectPart Object = m_SceneList[0].GetSceneObjectPart(objectID);
                report.ObjectName = Object.Name;
            }
        	else
                report.ObjectName = "";

        	details =details.Replace("\n", "`");
        	string [] detailssplit = details.Split(new Char [] {'`'});
        	
            if (report.ObjectName != "")
        	{
        		report.AbuseDetails = detailssplit[6];
        	}
        	else
        	{
                report.AbuseDetails = detailssplit[4];
        	}

            OpenSim.Services.Interfaces.UserAccount reporterProfile = m_SceneList[0].UserAccountService.GetUserAccount(UUID.Zero, reporter);
            if (reporterProfile != null)
                report.ReporterName = reporterProfile.FirstName + " " + reporterProfile.LastName;
            
            OpenSim.Services.Interfaces.UserAccount AbuserProfile = m_SceneList[0].UserAccountService.GetUserAccount(UUID.Zero, abuserID);
            if (AbuserProfile != null)
                report.AbuserName = AbuserProfile.FirstName + " " + AbuserProfile.LastName;
            
            summery = summery.Replace("\"", "`");
        	summery =summery.Replace("|","");
        	summery =summery.Replace(")","");
        	summery =summery.Replace("(","");
        	summery =summery.Replace("{","`");
        	summery =summery.Replace("}","`");
        	summery =summery.Replace("[","`");
        	summery =summery.Replace("]","`");
        	string [] summerysplit = summery.Split(new Char [] {' '});

            report.EstateID = int.Parse(summerysplit[1]);
            report.AbuseLocation = summerysplit[2];
        	
        	string [] summerysplit2 = summery.Split(new Char [] {'`'});
        	report.Category = summerysplit2[1];
            report.AbuseSummary = summerysplit2[5];
        	//Since the server doesn't trust this anyway...
            report.Number = (-1);

            EstateSettings ES = m_SceneList[0].EstateService.LoadEstateSettings(client.Scene.RegionInfo.RegionID, false);
            if(ES.AbuseEmailToEstateOwner)
            {
                IEmailModule Email = m_SceneList[0].RequestModuleInterface<IEmailModule>();
                if(Email != null)
                    Email.SendEmail(UUID.Zero, ES.AbuseEmail, "Abuse Report", "This abuse report was submitted by " +
                        report.ReporterName + " against " + report.AbuserName + " at " + report.AbuseLocation + " in your estate " + report.EstateID.ToString() +
                        ". Summary: " + report.AbuseSummary + ". Details: " + report.AbuseDetails + ".");
            }

        }
    }
}
    