using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using Nini.Config;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using RegionFlags = Aurora.Framework.RegionFlags;

namespace Aurora.Services.DataService
{
    public class LocalAvatarConnector : IAvatarData
    {
        private IGenericData GD = null;
        private string m_realm = "avatars";
        //private string m_cacherealm = "avatarscache";

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase, string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("AvatarConnector", "LocalConnector") == "LocalConnector")
            {
                GD = GenericData;

                string connectionString = defaultConnectionString;
                if (source.Configs[Name] != null)
                    connectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(connectionString, "Avatars", source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

                DataManager.DataManager.RegisterPlugin(Name, this);
            }
        }

        public string Name
        {
            get { return "IAvatarData"; }
        }

        public void Dispose()
        {
        }

        #region IAvatarData Members

        public AvatarData Get (string field, string val)
        {
            return InternalGet (m_realm, field, val);
        }

        private AvatarData InternalGet (string realm, string field, string val)
        {
            List<string> data = GD.Query (field, val, realm, "Name, Value");
            AvatarData retVal = new AvatarData ();
            retVal.AvatarType = 1;
            retVal.Data = new Dictionary<string, string> ();
            for (int i = 0; i < data.Count; i += 2)
            {
                retVal.Data[data[i]] = data[i + 1];
            }
            return retVal;
        }

        public bool Store (UUID PrincipalID, AvatarData data)
        {
            GD.Delete (m_realm, new string[1] { "PrincipalID" }, new object[1] { PrincipalID });
            for (int i = 0; i < data.Data.Count; i++)
            {
                GD.Insert (m_realm, new object[3] { PrincipalID, data.Data.ElementAt (i).Key, data.Data.ElementAt (i).Value });
            }
            return true;
        }

        public bool SetItems (UUID principalID, string[] names, string[] values)
        {
            return GD.Update (m_realm, names, values, new string[1] { "PrincipalID" }, new object[1] { principalID });
        }

        public bool Delete (UUID principalID, string name)
        {
            return GD.Delete (m_realm, new string[2] { "PrincipalID", "Name" }, new object[2] { principalID, name });
        }

        public bool Delete (string field, string val)
        {
            return GD.Delete (m_realm, new string[1] { field }, new object[1] { val });
        }

        #endregion
    }
}
