using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Aurora.Framework
{
    public interface IAuth
    {
        bool LoginAuthenticateUser(string userName, string password);
        void CreateUserAccount(string userName, string password);
        bool CheckAuthenticationServer(IPEndPoint serverIP);
        bool CheckUserAccount(string Identifier);
        void CreateUserAuth(string UUID, string firstName, string lastName);
    }
}
