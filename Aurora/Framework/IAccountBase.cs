using System;
using System.Collections.Generic;
using System.Text;

namespace Aurora.Framework
{
    public interface IAccountBase
    {
        void CreateUserAccount(string userName, string password);
        AuroraProfileData FindUserAccountByName(string userName);
        AuroraProfileData FindUserAccountByIdentifier(string Identifier);
        string UserNameToIdentifier(string userName);
        string IdentifierToUserName(string Identifier);
        bool TryGetClient(object Identifier, out AuroraProfileData client);
        void OnNewClient(AuroraProfileData client);
        void OnClosingClient(AuroraProfileData client);
        void ForEachClient(Action<IClient> action);
        int EntitiesCount();
    }
}
