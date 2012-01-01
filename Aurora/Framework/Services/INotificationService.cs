using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nini.Config;

namespace Aurora.Framework
{
    public enum AlertLevel
    {
        Low,
        Normal,
        Priority,
        Immediate
    }

    public delegate string NotificationRequest(List<string> notificationsToCombine);

    public interface INotificationService
    {
        #region Init

        void Init(IConfigSource config, IRegistryCore registry);

        #endregion

        #region Notifications

        void AddNotification(AlertLevel level, string message);
        void AddNotification(AlertLevel level, string message, string identifer);
        void AddNotification(AlertLevel level, string message, string identifer, NotificationRequest notificationCombiner);

        #endregion
    }
}
