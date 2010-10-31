using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml;
using log4net;
using Nini.Config;
using OpenSim;
using OpenSim.Framework;

namespace Aurora.Modules
{
    public class UpdaterPlugin : IApplicationPlugin
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void Initialize(OpenSim.Framework.IOpenSimBase openSim)
        {
            try
            {
                m_log.Info("[AURORAUPDATOR]: Checking for updates...");
                IConfig updateConfig = openSim.ConfigSource.Configs["Update"];
                if (updateConfig == null)
                    return;

                if (!updateConfig.GetBoolean("Enabled", false))
                    return;
                float CurrentVersion = float.Parse(openSim.Version.Split(' ')[1]);
                float LastestVersionToBlock = updateConfig.GetFloat("LatestRelease", 0);

                string WebSite = updateConfig.GetString("URLToCheckForUpdates", "http://auroraserver.ath.cx:8080/updater.xml");

                string XmlData = ReadExternalWebsite(WebSite);

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(XmlData);
                XmlNodeList parts = doc.GetElementsByTagName("Updater");
                XmlNode UpdaterNode = parts[0];
                if (float.Parse(UpdaterNode.ChildNodes[2].InnerText) > CurrentVersion && LastestVersionToBlock < float.Parse(UpdaterNode.ChildNodes[2].InnerText))
                {
                    //This version is not supported anymore
                    System.Windows.Forms.MessageBox.Show("A new version of Aurora has been released, version " + UpdaterNode.ChildNodes[2].InnerText + " released " + UpdaterNode.ChildNodes[3].InnerText + ". Release notes: " + UpdaterNode.ChildNodes[4].InnerText, "Aurora Update");
                    updateConfig.Set("LatestRelease", UpdaterNode.ChildNodes[2].InnerText);
                    updateConfig.ConfigSource.Save();
                }
                else if (float.Parse(UpdaterNode.ChildNodes[0].InnerText) > CurrentVersion && LastestVersionToBlock < float.Parse(UpdaterNode.ChildNodes[2].InnerText))
                {
                    //This version is not supported anymore
                    System.Windows.Forms.MessageBox.Show("Your version of Aurora (" + CurrentVersion + ", Released " + UpdaterNode.ChildNodes[1].InnerText + ") is not supported anymore.", "Aurora Update");
                }
            }
            catch
            {
            }
        }

        public void PostInitialise()
        {
        }

        public string Name
        {
            get { return "AuroraDataStartupPlugin"; }
        }

        public void Dispose()
        {
        }

        public string ReadExternalWebsite(string URL)
        {
            String website = "";
            UTF8Encoding utf8 = new UTF8Encoding();

            WebClient webClient = new WebClient();
            try
            {
                website = utf8.GetString(webClient.DownloadData(URL));
            }
            catch (Exception)
            {
            }
            return website;
        }
    }
}
