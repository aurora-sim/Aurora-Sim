using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using OpenMetaverse;

namespace Aurora.Framework
{
    public interface IAuthService
    {
    }
    public interface IIWCAuthenticationService
    {
        bool CheckAuthenticationServer(IPEndPoint serverIP);
        bool CheckUserAccount(string Identifier);
    }
}
