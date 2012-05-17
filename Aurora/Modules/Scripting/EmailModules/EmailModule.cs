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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Aurora.Framework;
using DotNetOpenMail;
using DotNetOpenMail.SmtpAuth;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;

namespace Aurora.Modules.Scripting
{
    public class EmailModule : IEmailModule
    {
        //
        // Module vars
        //
        private readonly Dictionary<UUID, DateTime> m_LastGetEmailCall = new Dictionary<UUID, DateTime>();
        private readonly Dictionary<UUID, List<Email>> m_MailQueues = new Dictionary<UUID, List<Email>>();

        private readonly TimeSpan m_QueueTimeout = new TimeSpan(2, 0, 0);
                                  // 2 hours without llGetNextEmail drops the queue

        // Scenes by Region Handle


        private string SMTP_SERVER_HOSTNAME = string.Empty;
        private string SMTP_SERVER_LOGIN = string.Empty;
        private string SMTP_SERVER_PASSWORD = string.Empty;
        private int SMTP_SERVER_PORT = 25;
        private IConfigSource m_Config;

        private bool m_Enabled;
        private string m_HostName = string.Empty;
        private string m_InterObjectHostname = "lsl.opensim.local";
        private const int m_MaxQueueSize = 50; // maximum size of an object mail queue
        private bool m_localOnly = true;
        private int m_MaxEmailSize = 4096;  // largest email allowed by default, as per lsl docs.

        public bool IsSharedModule
        {
            get { return true; }
        }

        #region IEmailModule Members

        /// <summary>
        ///   SendMail function utilized by llEMail
        /// </summary>
        /// <param name = "objectID"></param>
        /// <param name = "address"></param>
        /// <param name = "subject"></param>
        /// <param name = "body"></param>
        /// <param name="scene">Can be null</param>
        public void SendEmail(UUID objectID, string address, string subject, string body, IScene scene)
        {
            //Check if address is empty
            if (address == string.Empty)
                return;

            //FIXED:Check the email is correct form in REGEX
            const string EMailpatternStrict = @"^(([^<>()[\]\\.,;:\s@\""]+"
                                              + @"(\.[^<>()[\]\\.,;:\s@\""]+)*)|(\"".+\""))@"
                                              + @"((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}"
                                              + @"\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+"
                                              + @"[a-zA-Z]{2,}))$";
            Regex EMailreStrict = new Regex(EMailpatternStrict);
            bool isEMailStrictMatch = EMailreStrict.IsMatch(address);
            if (!isEMailStrictMatch)
            {
                MainConsole.Instance.Error("[EMAIL] REGEX Problem in EMail Address: " + address);
                return;
            }
            //FIXME:Check if subject + body = 4096 Byte
            if ((subject.Length + body.Length) > m_MaxEmailSize)
            {
                MainConsole.Instance.Error("[EMAIL] subject + body larger than limit of " + m_MaxEmailSize + " bytes");
                return;
            }

            string LastObjectName = string.Empty;
            string LastObjectPosition = string.Empty;
            string LastObjectRegionName = string.Empty;

            if (scene != null)
                resolveNamePositionRegionName(objectID, out LastObjectName, out LastObjectPosition, out LastObjectRegionName, scene);

            if (!address.EndsWith(m_InterObjectHostname))
            {
                bool didError = false;
                if (!m_localOnly)
                {
                    // regular email, send it out
                    try
                    {
                        //Creation EmailMessage

                        string fromEmailAddress = "@" + m_HostName;
                        if (scene != null)
                            fromEmailAddress = objectID.ToString() + "@" + m_HostName;
                        else
                            fromEmailAddress = "noreply@" + m_HostName;
                        EmailMessage emailMessage = new EmailMessage
                                               {
                                                   FromAddress =
                                                       new EmailAddress(fromEmailAddress), 
                                                       Subject = subject
                                               };

                        //To - Only One
                        emailMessage.AddToAddress(new EmailAddress(address));
                        //Text
                        emailMessage.BodyText = body;
                        if (scene != null)
                            emailMessage.BodyText = "Object-Name: " + LastObjectName +
                                                "\nRegion: " + LastObjectRegionName + "\nLocal-Position: " +
                                                LastObjectPosition + "\n\n" + emailMessage.BodyText;

                        //Config SMTP Server
                        //Set SMTP SERVER config
                        SmtpServer smtpServer = new SmtpServer(SMTP_SERVER_HOSTNAME, SMTP_SERVER_PORT);
                        // Add authentication only when requested
                        if (SMTP_SERVER_LOGIN != String.Empty && SMTP_SERVER_PASSWORD != String.Empty)
                            smtpServer.SmtpAuthToken = new SmtpAuthToken(SMTP_SERVER_LOGIN, SMTP_SERVER_PASSWORD);
                        //Add timeout of 15 seconds
                        smtpServer.ServerTimeout = 15000;
                        //Send Email Message
                        didError = !emailMessage.Send(smtpServer);

                        //Log
                        if (!didError)
                            MainConsole.Instance.Info("[EMAIL] EMail sent to: " + address + " from object: " + fromEmailAddress);
                    }
                    catch (Exception e)
                    {
                        MainConsole.Instance.Error("[EMAIL] DefaultEmailModule Exception: " + e.Message);
                        didError = true;
                    }
                }
                if (((didError) || (m_localOnly)) && (scene != null))
                {
                    // Notify Owner
                    ISceneChildEntity part = findPrim(objectID, out LastObjectRegionName, scene);
                    if (part != null)
                    {
                        IScenePresence sp = scene.GetScenePresence(part.OwnerID);
                        if ((sp != null) && (!sp.IsChildAgent))
                        {
                            sp.ControllingClient.SendAlertMessage("llEmail: email module not configured for outgoing emails");
                        }
                    }
                }
            }
            else
            {
                // inter object email, keep it in the family
                string guid = address.Substring(0, address.IndexOf("@", StringComparison.Ordinal));
                UUID toID = new UUID(guid);

                if (IsLocal(toID, scene))
                {
                    // object in this region
                    InsertEmail(toID, new Email
                                  {
                                      time =
                                          ((int) ((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds)).
                                          ToString(CultureInfo.InvariantCulture),
                                      subject = subject,
                                      sender = objectID.ToString() + "@" + m_InterObjectHostname,
                                      message = "Object-Name: " + LastObjectName +
                                                "\nRegion: " + LastObjectRegionName + "\nLocal-Position: " +
                                                LastObjectPosition + "\n\n" + body,
                                      toPrimID = toID
                                  });
                }
                else
                {
                    // object on another region

                    Email email = new Email
                    {
                        time =
                            ((int)((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds)).
                            ToString(CultureInfo.InvariantCulture),
                        subject = subject,
                        sender = objectID.ToString() + "@" + m_InterObjectHostname,
                        message = body,
                        toPrimID = toID
                    };
                    IEmailConnector conn = DataManager.DataManager.RequestPlugin<IEmailConnector>();
                    conn.InsertEmail(email);
                }
            }
        }

        ///<summary>
        ///   Gets any emails that a prim may have asyncronously
        ///</summary>
        ///<param name = "objectID"></param>
        ///<param name = "sender"></param>
        ///<param name = "subject"></param>
        ///<param name="handler"> </param>
        ///<param name="scene"> </param>
        ///<returns></returns>
        public void GetNextEmailAsync(UUID objectID, string sender, string subject, NextEmail handler, IScene scene)
        {
#if (!ISWIN)
            Util.FireAndForget(delegate { handler(GetNextEmail(objectID, sender, subject, scene)); });
#else
            Util.FireAndForget(state => handler(GetNextEmail(objectID, sender, subject, scene)));
#endif
        }

        ///<summary>
        ///   Gets any emails that a prim may have
        ///</summary>
        ///<param name = "objectID"></param>
        ///<param name = "sender"></param>
        ///<param name = "subject"></param>
        ///<param name="scene"> </param>
        ///<returns></returns>
        public Email GetNextEmail(UUID objectID, string sender, string subject, IScene scene)
        {
            List<Email> queue = null;

            lock (m_LastGetEmailCall)
            {
                if (m_LastGetEmailCall.ContainsKey(objectID))
                {
                    m_LastGetEmailCall.Remove(objectID);
                }

                m_LastGetEmailCall.Add(objectID, DateTime.Now);

                // Hopefully this isn't too time consuming.  If it is, we can always push it into a worker thread.
                DateTime now = DateTime.Now;
#if (!ISWIN)
                List<UUID> removal = new List<UUID>();
                foreach (UUID uuid in m_LastGetEmailCall.Keys)
                {
                    if ((now - m_LastGetEmailCall[uuid]) > m_QueueTimeout) removal.Add(uuid);
                }
#else
                List<UUID> removal = m_LastGetEmailCall.Keys.Where(uuid => (now - m_LastGetEmailCall[uuid]) > m_QueueTimeout).ToList();
#endif

                foreach (UUID remove in removal)
                {
                    m_LastGetEmailCall.Remove(remove);
                    lock (m_MailQueues)
                    {
                        m_MailQueues.Remove(remove);
                    }
                }
            }

            GetRemoteEmails(objectID, scene);
            lock (m_MailQueues)
            {
                if (m_MailQueues.ContainsKey(objectID))
                {
                    queue = m_MailQueues[objectID];
                }
            }

            if (queue != null)
            {
                lock (queue)
                {
                    if (queue.Count > 0)
                    {
                        int i;

                        for (i = 0; i < queue.Count; i++)
                        {
                            if ((sender == null || sender.Equals("") || sender.Equals(queue[i].sender)) &&
                                (subject == null || subject.Equals("") || subject.Equals(queue[i].subject)))
                            {
                                break;
                            }
                        }

                        if (i != queue.Count)
                        {
                            Email ret = queue[i];
                            queue.Remove(ret);
                            ret.numLeft = queue.Count;
                            return ret;
                        }
                    }
                }
            }
            else
            {
                lock (m_MailQueues)
                {
                    m_MailQueues.Add(objectID, new List<Email>());
                }
            }

            return null;
        }

        private void GetRemoteEmails(UUID objectID, IScene scene)
        {
            IEmailConnector conn = DataManager.DataManager.RequestPlugin<IEmailConnector>();
            List<Email> emails = conn.GetEmails(objectID);
            if (emails.Count > 0)
            {
                if (!m_MailQueues.ContainsKey(objectID))
                    m_MailQueues.Add(objectID, new List<Email>());
                foreach (Email email in emails)
                {
                    string LastObjectName = string.Empty;
                    string LastObjectPosition = string.Empty;
                    string LastObjectRegionName = string.Empty;

                    resolveNamePositionRegionName(objectID, out LastObjectName, out LastObjectPosition, out LastObjectRegionName, scene);

                    email.message = "Object-Name: " + LastObjectName +
                                  "\nRegion: " + LastObjectRegionName + "\nLocal-Position: " +
                                  LastObjectPosition + "\n\n" + email.message;
                    InsertEmail(objectID, email);
                }
            }
        }

        #endregion

        public void InsertEmail(UUID to, Email email)
        {
            // It's tempting to create the queue here.  Don't; objects which have
            // not yet called GetNextEmail should have no queue, and emails to them
            // should be silently dropped.

            lock (m_MailQueues)
            {
                if (m_MailQueues.ContainsKey(to))
                {
                    if (m_MailQueues[to].Count >= m_MaxQueueSize)
                    {
                        // fail silently
                        return;
                    }

                    lock (m_MailQueues[to])
                    {
                        m_MailQueues[to].Add(email);
                    }
                }
            }
        }

        private bool IsLocal(UUID objectID, IScene scene)
        {
            string unused;
            return (findPrim(objectID, out unused, scene) != null);
        }

        private ISceneChildEntity findPrim(UUID objectID, out string ObjectRegionName, IScene s)
        {
            ISceneChildEntity part = s.GetSceneObjectPart(objectID);
            if (part != null)
            {
                ObjectRegionName = s.RegionInfo.RegionName;
                int localX = s.RegionInfo.RegionLocX;
                int localY = s.RegionInfo.RegionLocY;
                ObjectRegionName = ObjectRegionName + " (" + localX + ", " + localY + ")";
                return part;
            }

            ObjectRegionName = string.Empty;
            return null;
        }

        private void resolveNamePositionRegionName(UUID objectID, out string ObjectName,
                                                   out string ObjectAbsolutePosition, out string ObjectRegionName, IScene scene)
        {
            string m_ObjectRegionName;
            ISceneChildEntity part = findPrim(objectID, out m_ObjectRegionName, scene);
            if (part != null)
            {
                int objectLocX = (int) part.AbsolutePosition.X;
                int objectLocY = (int) part.AbsolutePosition.Y;
                int objectLocZ = (int) part.AbsolutePosition.Z;
                ObjectAbsolutePosition = "(" + objectLocX + ", " + objectLocY + ", " + objectLocZ + ")";
                ObjectName = part.Name;
                ObjectRegionName = m_ObjectRegionName;
                return;
            }
            ObjectName = null;
            ObjectAbsolutePosition = null;
            ObjectRegionName = null;
        }

        #region Implementation of IService

        public string Name
        {
            get { return "DefaultEmailModule"; }
        }

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            m_Config = config;

            //Load SMTP SERVER config
            try
            {
                IConfig SMTPConfig;
                if ((SMTPConfig = m_Config.Configs["SMTP"]) == null)
                {
                    //MainConsole.Instance.InfoFormat("[SMTP] SMTP server not configured");
                    m_Enabled = false;
                    return;
                }
                m_Enabled = SMTPConfig.GetBoolean("enabled", true);
                if (!m_Enabled)
                {
                    //MainConsole.Instance.InfoFormat("[SMTP] module disabled in configuration");
                    m_Enabled = false;
                    return;
                }
                m_localOnly = SMTPConfig.GetBoolean("local_only", true);
                m_HostName = SMTPConfig.GetString("host_domain_header_from", m_HostName);
                m_InterObjectHostname = SMTPConfig.GetString("internal_object_host", m_InterObjectHostname);
                SMTP_SERVER_HOSTNAME = SMTPConfig.GetString("SMTP_SERVER_HOSTNAME", SMTP_SERVER_HOSTNAME);
                SMTP_SERVER_PORT = SMTPConfig.GetInt("SMTP_SERVER_PORT", SMTP_SERVER_PORT);
                SMTP_SERVER_LOGIN = SMTPConfig.GetString("SMTP_SERVER_LOGIN", SMTP_SERVER_LOGIN);
                SMTP_SERVER_PASSWORD = SMTPConfig.GetString("SMTP_SERVER_PASSWORD", SMTP_SERVER_PASSWORD);
                m_MaxEmailSize = SMTPConfig.GetInt("email_max_size", m_MaxEmailSize);

                registry.RegisterModuleInterface<IEmailModule>(this);
            }
            catch (Exception e)
            {
                MainConsole.Instance.Error("[EMAIL] DefaultEmailModule not configured: " + e.Message);
                m_Enabled = false;
            }
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void FinishedStartup()
        {
        }

        #endregion
    }
}