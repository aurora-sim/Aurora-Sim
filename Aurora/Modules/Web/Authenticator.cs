using Aurora.Framework;
using Aurora.Framework.Servers.HttpServer;
using OpenMetaverse;
using System.Collections.Generic;
using System.Linq;

namespace Aurora.Modules.Web
{
    public class Authenticator
    {
        private static Dictionary<UUID, UserAccount> _authenticatedUsers = new Dictionary<UUID, UserAccount>();
        private static Dictionary<UUID, UserAccount> _authenticatedAdminUsers = new Dictionary<UUID, UserAccount>();

        public static bool CheckAuthentication(OSHttpRequest request)
        {
            if (request.Cookies["SessionID"] != null)
            {
                if (_authenticatedUsers.ContainsKey(UUID.Parse(request.Cookies["SessionID"].Value)))
                    return true;
            }
            return false;
        }

        public static bool CheckAdminAuthentication(OSHttpRequest request)
        {
            if (request.Cookies["SessionID"] != null)
            {
                if (_authenticatedAdminUsers.ContainsKey(UUID.Parse(request.Cookies["SessionID"].Value)))
                    return true;
            }
            return false;
        }

        public static bool CheckAdminAuthentication(OSHttpRequest request, int adminLevelRequired)
        {
            if (request.Cookies["SessionID"] != null)
            {
                var session = _authenticatedAdminUsers.FirstOrDefault((acc) => acc.Key == UUID.Parse(request.Cookies["SessionID"].Value));
                if (session.Value != null && session.Value.UserLevel >= adminLevelRequired)
                    return true;
            }
            return false;
        }

        public static void AddAuthentication(UUID sessionID, UserAccount account)
        {
            _authenticatedUsers.Add(sessionID, account);
        }

        public static void AddAdminAuthentication(UUID sessionID, UserAccount account)
        {
            _authenticatedAdminUsers.Add(sessionID, account);
        }

        public static void RemoveAuthentication(OSHttpRequest request)
        {
            UUID sessionID = GetAuthenticationSession(request);
            _authenticatedUsers.Remove(sessionID);
            _authenticatedAdminUsers.Remove(sessionID);
        }

        public static UserAccount GetAuthentication(OSHttpRequest request)
        {
            if (request.Cookies["SessionID"] != null)
            {
                UUID sessionID = UUID.Parse(request.Cookies["SessionID"].Value);
                if (_authenticatedUsers.ContainsKey(sessionID))
                    return _authenticatedUsers[sessionID];
            }
            return null;
        }

        public static UUID GetAuthenticationSession(OSHttpRequest request)
        {
            if (request.Cookies["SessionID"] != null)
                return UUID.Parse(request.Cookies["SessionID"].Value);
            return UUID.Zero;
        }

        public static void ChangeAuthentication(OSHttpRequest request, UserAccount account)
        {
            if (request.Cookies["SessionID"] != null)
            {
                UUID sessionID = UUID.Parse(request.Cookies["SessionID"].Value);
                if (_authenticatedUsers.ContainsKey(sessionID))
                    _authenticatedUsers[sessionID] = account;
                if (_authenticatedAdminUsers.ContainsKey(sessionID))
                    _authenticatedAdminUsers[sessionID] = account;
            }
        }
    }
}
