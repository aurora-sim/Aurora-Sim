using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Framework
{
    public interface IGroupNotificationService
    {
        void InformAllUsersOfNewGroupNotice(GroupNoticeInfo groupNotice);
    }
}
