using System.Collections.Generic;
using System.Linq;
using Aurora.DataManager;
using Aurora.Framework;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.Interfaces
{
    public interface IUserFinder
    {
        string GetUserName(UUID uuid);
        string GetUserHomeURL(UUID uuid);
        string GetUserUUI(UUID uuid);
        string GetUserServerURL(UUID uuid, string serverType);
        string[] GetUserNames(UUID uuid);

        void AddUser(UUID uuid, string userData);
        void AddUser(UUID uuid, string firstName, string lastName, Dictionary<string, object> serviceUrls);

        bool GetUserExists(UUID userID);
        bool IsLocalGridUser(UUID uuid);
    }

    public class UserData : IDataTransferable
    {
        public UUID Id;
        public string FirstName;
        public string LastName;
        public string HomeURL;
        public Dictionary<string, object> ServerURLs;

        public override void FromOSD(OSDMap map)
        {
            Id = map["Id"];
            FirstName = map["FirstName"];
            LastName = map["LastName"];
            HomeURL = map["HomeURL"];
            ServerURLs = ((OSDMap)map["ServerURLs"]).ConvertMap<object>(o => o.AsString());
        }

        public override OSDMap ToOSD()
        {
            OSDMap map = new OSDMap();

            map["Id"] = Id;
            map["FirstName"] = FirstName;
            map["LastName"] = LastName;
            map["HomeURL"] = HomeURL;
            map["ServerURLs"] = ServerURLs.ToOSDMap();

            return map;
        }
    }
}