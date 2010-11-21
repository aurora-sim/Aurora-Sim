using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using log4net;
using Nini.Config;
using OpenSim;
using OpenSim.Framework;
using Aurora.Framework;

namespace Aurora.Modules
{
    public class UpdaterPlugin : IApplicationPlugin
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void Initialize(OpenSim.Framework.IOpenSimBase openSim)
        {
            try
            {
                //Check whether this is enabled
                IConfig updateConfig = openSim.ConfigSource.Configs["Update"];
                if (updateConfig == null)
                    return;

                if (!updateConfig.GetBoolean("Enabled", false))
                    return;
                
                m_log.Info("[AURORAUPDATOR]: Checking for updates...");

                float CurrentVersion = float.Parse(openSim.Version.Split(' ')[1]);
                float LastestVersionToBlock = updateConfig.GetFloat("LatestRelease", 0);

                string WebSite = updateConfig.GetString("URLToCheckForUpdates", "http://auroraserver.ath.cx:8080/updater.xml");
                //Pull the xml from the website
                string XmlData = Utilities.ReadExternalWebsite(WebSite);

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(XmlData);

                XmlNodeList parts = doc.GetElementsByTagName("Updater");
                XmlNode UpdaterNode = parts[0];

                //[0] - Minimum supported release #
                //[1] - Minimum supported release date
                //[2] - Newest version #
                //[3] - Date released
                //[4] - Release notes
                //[5] - Download link

                //Read the newest version [2] and see if it is higher than the current version and less than the version the user last told us to block
                if (float.Parse(UpdaterNode.ChildNodes[2].InnerText) > CurrentVersion && LastestVersionToBlock < float.Parse(UpdaterNode.ChildNodes[2].InnerText))
                {
                    //Ask if they would like to update
                    DialogResult result = MessageBox.Show("Aurora Update",
                        "A new version of Aurora has been released, version " +
                        UpdaterNode.ChildNodes[2].InnerText +
                        " released " + UpdaterNode.ChildNodes[3].InnerText +
                        ". Release notes: " + UpdaterNode.ChildNodes[4].InnerText +
                        ", do you want to download the update?",
                        System.Windows.Forms.MessageBoxButtons.YesNo);

                    //If so, download the new version
                    if (result == DialogResult.Yes)
                    {
                        Utilities.DownloadFile(UpdaterNode.ChildNodes[5].InnerText,
                            "AuroraVersion" + UpdaterNode.ChildNodes[2].InnerText + ".zip");
                    }
                    //Update the config so that we do not ask again
                    updateConfig.Set("LatestRelease", UpdaterNode.ChildNodes[2].InnerText);
                    updateConfig.ConfigSource.Save();
                }
                else if (float.Parse(UpdaterNode.ChildNodes[0].InnerText) > CurrentVersion && LastestVersionToBlock < float.Parse(UpdaterNode.ChildNodes[2].InnerText))
                {
                    //This version is not supported anymore
                    MessageBox.Show("Your version of Aurora (" + CurrentVersion + ", Released " + UpdaterNode.ChildNodes[1].InnerText + ") is not supported anymore.", "Aurora Update");
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
    }
}
