using OpenMetaverse;

namespace OpenSim.Services.Interfaces
{
    public interface IUserFinder
    {
        string GetUserName(UUID uuid);
        string GetUserHomeURL(UUID uuid);
        string GetUserUUI(UUID uuid);
        string GetUserServerURL(UUID uuid, string serverType);
        void AddUser(UUID uuid, string userData);
        void AddUser(UUID uuid, string firstName, string lastName, string profileURL);
    }

    public interface IUserManagement
    {
        bool GetUserExists(UUID userID);
        string GetUserName(UUID uuid);
        string GetUserHomeURL(UUID uuid);
        string GetUserUUI(UUID uuid);
        string GetUserServerURL(UUID uuid, string serverType);
        void AddUser(UUID uuid, string userData);
        void AddUser(UUID uuid, string firstName, string lastName, string profileURL);
    }
}