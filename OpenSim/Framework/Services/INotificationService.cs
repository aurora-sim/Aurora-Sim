using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nini.Config;

namespace OpenSim.Framework
{
    public enum AlertLevel
    {
        Low,
        Normal,
        Priority,
        Immediate
    }

    public interface INotificationService
    {
        #region Init

        void Init(IConfigSource config, IRegistryCore registry);

        #endregion

        #region Notifications

        void AddNotification(AlertLevel level, string message);
        void AddNotification(AlertLevel level, string message, string identifer);

        #endregion
    }
}
