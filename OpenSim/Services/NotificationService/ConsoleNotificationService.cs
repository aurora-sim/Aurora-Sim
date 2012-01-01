using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;

namespace OpenSim.Services.NotificationService
{
    public class Notification
    {
        public AlertLevel AlertLevel;
        public List<string> Message;
        public string Identifer;
        public NotificationRequest DuplicateNotificationRequest;

        public static Notification Combine(Notification a, Notification b)
        {
            if (a.Identifer != b.Identifer)
                throw new NotSupportedException("Identifers are not the same");
            a.Message.AddRange(b.Message);
            return a;
        }
    }
    public class ConsoleNotificationService : INotificationService
    {
        #region INotificationService Members

        private Dictionary<string, Notification> m_dicNotifications = new Dictionary<string, Notification>();

        public void Init(IConfigSource config, IRegistryCore registry)
        {
            registry.RegisterModuleInterface<INotificationService>(this);
            MainConsole.NotificationService = this;

            MainConsole.Instance.Commands.AddCommand("show notifications", "show notifications", "Shows all new notifications", DisplayNotifications);
        }

        public void AddNotification(AlertLevel level, string message)
        {
            AddNotification(level, message, "");
        }

        public void AddNotification(AlertLevel level, string message, string identifer)
        {
            AddNotification(level, message, identifer, null);
        }

        public void AddNotification(AlertLevel level, string message, string identifer, NotificationRequest notificationCombiner)
        {
            Notification not = new Notification()
            {
                AlertLevel = level,
                Message = new List<string>(new []{message}),
                Identifer = identifer,
                DuplicateNotificationRequest = notificationCombiner
            };
            if (m_dicNotifications.ContainsKey(identifer))
                m_dicNotifications[identifer] = Notification.Combine(m_dicNotifications[identifer], not);
            else
                m_dicNotifications[identifer] = not;
        }

        public void DisplayNotifications(string[] cmd)
        {
            List<string> notificationsToRemove = new List<string>();
            foreach (Notification not in m_dicNotifications.Values)
            {
                if (not.Message.Count > 1)
                {
                    if (not.DuplicateNotificationRequest != null)
                        MainConsole.Instance.Info(not.DuplicateNotificationRequest(not.Message));
                    else
                        foreach (string message in not.Message)
                            MainConsole.Instance.Info(message);
                }
                else
                    MainConsole.Instance.Info(not.Message[0]);
                if (MainConsole.Instance.Prompt("Do you want to remove this notification?", "yes", new List<string>(new[] { "yes", "no" })) == "yes")
                    notificationsToRemove.Add(not.Identifer);
            }
            foreach(string ident in notificationsToRemove)
                m_dicNotifications.Remove(ident);
        }

        #endregion
    }
}
