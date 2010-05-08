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
    public class AbuseReports : IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private bool enabled = false;
        private List<Scene> m_SceneList = new List<Scene>();
        private static char[] charSeparators = new char[] {  };
        private IGenericData GenericData = null;
        private IProfileData ProfileData = null;
        private IRegionData RegionData = null;

        public void Initialise(Scene scene, IConfigSource config)
        {
            IConfig cnf = config.Configs["AbuseReports"];
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
            m_log.Info("[ABUSE REPORTS MODULE] Enabled");
            
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
            GenericData = Aurora.DataManager.DataManager.GetDefaultGenericPlugin();
            ProfileData = Aurora.DataManager.DataManager.GetDefaultProfilePlugin();
            RegionData = Aurora.DataManager.DataManager.GetDefaultRegionPlugin();
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
        	string abuserUUID = "";
        	string bytecatagory = "";
        	string checkflagsbyte = "";
        	string Position = "";
        	string reportTypebyte = "";
        	string screenshotUUID = "";
        	string reporterUUID = "";
        	string ObjectName;
        	try
        	{
        		abuserUUID= abuserID.ToString();
        		bytecatagory = catagory.ToString();
        		checkflagsbyte = checkflags.ToString();
        		Position = position.ToString();
        		reportTypebyte = reportType.ToString();
        		screenshotUUID = screenshotID.ToString();
        		reporterUUID = reporter.ToString();
        		SceneObjectPart Object = m_SceneList[0].GetSceneObjectPart(objectID);
        		ObjectName = Object.Name;
        	}
        	catch(Exception ex)
        	{
        		ex = new Exception();
        		ObjectName = "";
        	}
        	details =details.Replace("\n", "`");
        	string [] detailssplit = details.Split(new Char [] {'`'});
        	string Adetails;
        	if(ObjectName != "")
        	{
        		Adetails = detailssplit[6];
        	}
        	else
        	{
        		Adetails = detailssplit[4];
        	}

            Aurora.DataManager.Frontends.ProfileFrontend data = new Aurora.DataManager.Frontends.ProfileFrontend();
            IUserProfileInfo reporterProfile = data.GetUserProfile(reporter);
            string ReporterName = "";
            string AbuserName = "";
            if (reporterProfile != null)
                ReporterName = reporterProfile.FirstName + " " + reporterProfile.LastName;
            IUserProfileInfo AbuserProfile = data.GetUserProfile(abuserID);
            if (AbuserProfile != null)
                AbuserName = AbuserProfile.FirstName + " " + AbuserProfile.LastName;
            summery = summery.Replace("\"", "`");
        	summery =summery.Replace("|","");
        	summery =summery.Replace(")","");
        	summery =summery.Replace("(","");
        	summery =summery.Replace("{","`");
        	summery =summery.Replace("}","`");
        	summery =summery.Replace("[","`");
        	summery =summery.Replace("]","`");
        	string [] summerysplit = summery.Split(new Char [] {' '});
        	string Estate = summerysplit[1];
        	string Aloc = summerysplit[2];
        	
        	string [] summerysplit2 = summery.Split(new Char [] {'`'});
        	string categoryname = summerysplit2[1];
        	string Summery = summerysplit2[5];
        	int Number = 0;
        	try
        	{
                Number = Convert.ToInt32(RegionData.AbuseReports());
        	}
        	catch(Exception ex)
        	{
        		ex = new Exception();
        	}
        	Number += 1;
            EstateSettings ES = m_SceneList[0].EstateService.LoadEstateSettings(client.Scene.RegionInfo.RegionID, false);
            if(ES.AbuseEmailToEstateOwner)
            {
                IEmailModule Email = m_SceneList[0].RequestModuleInterface<IEmailModule>();
                if(Email != null)
                    Email.SendEmail(UUID.Zero, ES.AbuseEmail, "Abuse Report - Number " + Number.ToString(), "This abuse report was submitted by " +
                        ReporterName + " against " + AbuserName + " at " + Aloc + " in your estate " + Estate +
                        ". Summary: " + Summery + ". Details: " + Adetails + ".");
            }
        	List<string> values= new List<string>();
            values.Add(categoryname);
            values.Add(ReporterName);
            values.Add(ObjectName);
            values.Add(objectID.ToString());
            values.Add(AbuserName);
            values.Add(Aloc);
            values.Add(Adetails);
            values.Add(Position);
            values.Add(Estate);
            values.Add(Summery);
            values.Add(Number.ToString());
            values.Add("No One");
            values.Add("No");
            values.Add("");
            values.Add("");
        	GenericData.Insert("abusereports",values.ToArray());
        }
    }
}
    