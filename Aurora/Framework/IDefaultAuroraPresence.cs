using System;
using System.Collections.Generic;
using System.Text;

namespace Aurora.Framework
{
    public interface IDefaultAuroraPresence
    {
        void OnProfileRequest(AuroraProfileData client);
    }
}
