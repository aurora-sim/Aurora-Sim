using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using OpenMetaverse;
using Aurora.Framework.Servers.HttpServer;

namespace Aurora.Modules.Web
{
    public class Authenticator
    {
        private static Dictionary<UUID, UUID> _authenticatedUsers = new Dictionary<UUID, UUID>();
        public static bool CheckAuthentication(OSHttpRequest request)
        {
            if (request.Cookies["SessionID"] != null)
            {
                if (_authenticatedUsers.ContainsKey(UUID.Parse(request.Cookies["SessionID"].Value)))
                    return true;
            }
            return false;
        }

        public static void AddAuthentication(UUID sessionID, UUID userID)
        {
            _authenticatedUsers.Add(sessionID, userID);
        }
    }
}
