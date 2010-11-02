using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;

namespace Aurora.Framework
{
    public enum IAgentFlags : uint
    {
        Foreign = 1,
        Temperary = 2,
        Minor = 3,
        Locked = 4,
        PermBan = 5,
        TempBan = 6,
        Blocked = 7,
        Local = 8,
        LocalOnly = 9,
        PastPrelude = 10
    }

    public class IAgentInfo
    {
        /// <summary>
        /// The ID value for this user
        /// </summary>
        public UUID PrincipalID = UUID.Zero;

        /// <summary>
        /// AgentFlags
        /// </summary>
        public IAgentFlags Flags = 0;

        /// <summary>
        /// Max maturity rating the user wishes to see
        /// </summary>
        public int MaturityRating = 2;

        /// <summary>
        /// Max maturity rating the user can ever to see
        /// </summary>
        public int MaxMaturity = 2;

        /// <summary>
        /// Did this user accept the TOS?
        /// </summary>
        public bool AcceptTOS = false;

        /// <summary>
        /// Current language
        /// </summary>
        public string Language = "en-us";

        /// <summary>
        /// Is the users language public
        /// </summary>
        public bool LanguageIsPublic = true;

        public IAgentInfo() { }

        public IAgentInfo(Dictionary<string, object> kvp)
        {
            PrincipalID = UUID.Zero;
            if (kvp.ContainsKey("PrincipalID") && kvp["PrincipalID"] != null)
                PrincipalID = new UUID(kvp["PrincipalID"].ToString());
            Flags = 0;
            if (kvp.ContainsKey("Flags") && kvp["Flags"] != null)
                Flags = (IAgentFlags)Convert.ToUInt32(kvp["Flags"].ToString());
            MaxMaturity = 0;
            if (kvp.ContainsKey("MaxMaturity") && kvp["MaxMaturity"] != null)
                MaxMaturity = Convert.ToInt32(kvp["MaxMaturity"].ToString());
            MaturityRating = 0;
            if (kvp.ContainsKey("MaturityRating") && kvp["MaturityRating"] != null)
                MaturityRating = Convert.ToInt32(kvp["MaturityRating"].ToString());
            Language = "";
            if (kvp.ContainsKey("Language") && kvp["Language"] != null)
                Language = kvp["Language"].ToString();
            AcceptTOS = true;
            if (kvp.ContainsKey("AcceptTOS") && kvp["AcceptTOS"] != null)
                AcceptTOS = Convert.ToBoolean(kvp["AcceptTOS"].ToString());
            LanguageIsPublic = true;
            if (kvp.ContainsKey("LanguageIsPublic") && kvp["LanguageIsPublic"] != null)
                LanguageIsPublic = Convert.ToBoolean(kvp["LanguageIsPublic"].ToString());
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result["PrincipalID"] = PrincipalID.ToString();
            result["Flags"] = (uint)Flags;
            result["MaxMaturity"] = MaxMaturity;
            result["MaturityRating"] = MaturityRating;
            result["Language"] = Language.ToString();
            result["AcceptTOS"] = AcceptTOS.ToString();
            result["LanguageIsPublic"] = LanguageIsPublic.ToString();

            return result;
        }
    }

    public class IUserProfileInfo
    {
        /// <summary>
        /// The ID value for this user
        /// </summary>
        public UUID PrincipalID = UUID.Zero;

        /// <summary>
        /// Show in search
        /// </summary>
        public bool AllowPublish = true;

        /// <summary>
        /// Allow for mature publishing
        /// </summary>
        public bool MaturePublish = false;

        /// <summary>
        /// The partner of this user
        /// </summary>
        public UUID Partner = UUID.Zero;

        /// <summary>
        /// the web address of the Profile URL
        /// </summary>
        public string WebURL = String.Empty;

        /// <summary>
        /// The about text listed in a users profile.
        /// </summary>
        public string AboutText = String.Empty;

        /// <summary>
        /// The first life about text listed in a users profile
        /// </summary>
        public string FirstLifeAboutText = String.Empty;

        /// <summary>
        /// The profile image for an avatar stored on the asset server
        /// </summary>
        public UUID Image = UUID.Zero;

        /// <summary>
        /// The profile image for the users first life tab
        /// </summary>
        public UUID FirstLifeImage = UUID.Zero;

        /// <summary>
        /// The type of the user
        /// </summary>
        public string CustomType = String.Empty;

        /// <summary>
        /// Is this user's online status visible to others?
        /// </summary>
        public bool Visible = true;

        /// <summary>
        /// Should IM's be sent to the user's email?
        /// </summary>
        public bool IMViaEmail = false;

        /// <summary>
        /// The appearance archive to load for this user
        /// </summary>
        public string AArchiveName = String.Empty;

        /// <summary>
        /// Is the user a new user?
        /// </summary>
        public bool IsNewUser = true;

        /// <summary>
        /// The group that the user is assigned to, ex: Premium
        /// </summary>
        public string MembershipGroup = String.Empty;

        /// <summary>
        /// A UNIX Timestamp (seconds since epoch) for the users creation
        /// </summary>
        public int Created = OpenSim.Framework.Util.UnixTimeSinceEpoch();

        /// <summary>
        /// The display name of the avatar
        /// </summary>
        public string DisplayName = String.Empty;

        /// <summary>
        /// The interests of the user
        /// </summary>
        public ProfileInterests Interests = new ProfileInterests();

        /// <summary>
        /// All of the notes of the user
        /// </summary>
        /// UUID - target agent
        /// string - notes
        public Dictionary<string, object> Notes = new Dictionary<string, object>();
        /// <summary>
        /// The picks of the user
        /// </summary>
        public Dictionary<string, object> Picks = new Dictionary<string,object>();

        /// <summary>
        /// All the classifieds of the user
        /// </summary>
        public Dictionary<string, object> Classifieds = new Dictionary<string,object>();

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result["PrincipalID"] = PrincipalID.ToString();
            result["AllowPublish"] = AllowPublish.ToString();
            result["MaturePublish"] = MaturePublish.ToString();
            result["WantToMask"] = Interests.WantToMask;
            result["WantToText"] = Interests.WantToText;
            result["CanDoMask"] = Interests.CanDoMask;
            result["CanDoText"] = Interests.CanDoText;
            result["Languages"] = Interests.Languages;
            result["AboutText"] = AboutText.ToString();
            result["FirstLifeImage"] = FirstLifeImage.ToString();
            result["FirstLifeAboutText"] = FirstLifeAboutText;
            result["Image"] = Image.ToString();
            result["WebURL"] = WebURL;
            result["Created"] = Created.ToString();
            result["DisplayName"] = DisplayName;
            result["Partner"] = Partner.ToString();
            result["Visible"] = Visible.ToString();
            result["AArchiveName"] = AArchiveName.ToString();
            result["CustomType"] = CustomType.ToString();
            result["AArchiveName"] = AArchiveName.ToString();
            result["IMViaEmail"] = IMViaEmail.ToString();
            result["IsNewUser"] = IsNewUser.ToString();
            result["MembershipGroup"] = MembershipGroup.ToString();

            result["Classifieds"] = OSDParser.SerializeJsonString(Util.DictionaryToOSD(Classifieds));
            result["Picks"] = OSDParser.SerializeJsonString(Util.DictionaryToOSD(Picks));
            result["Notes"] = OSDParser.SerializeJsonString(Util.DictionaryToOSD(Notes));

            return result;
        }

        // http://social.msdn.microsoft.com/forums/en-US/csharpgeneral/thread/68f7ca38-5cd1-411f-b8d4-e4f7a688bc03
        // By: A Million Lemmings
        public string ConvertDecString(int dvalue)
        {

            string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            string retVal = string.Empty;

            double value = Convert.ToDouble(dvalue);

            do
            {

                double remainder = value - (26 * Math.Truncate(value / 26));

                retVal = retVal + CHARS.Substring((int)remainder, 1);

                value = Math.Truncate(value / 26);

            }
            while (value > 0);



            return retVal;

        }

        public IUserProfileInfo() { }

        public IUserProfileInfo(Dictionary<string, object> main)
        {
            PrincipalID = new UUID(main["PrincipalID"].ToString());
            AllowPublish = Convert.ToBoolean(main["AllowPublish"].ToString());
            MaturePublish = Convert.ToBoolean(main["MaturePublish"].ToString());

            //Interests
            Interests = new ProfileInterests();
            Interests.WantToMask = uint.Parse(main["WantToMask"].ToString());
            Interests.WantToText = main["WantToText"].ToString();
            Interests.CanDoMask = uint.Parse(main["CanDoMask"].ToString());
            Interests.CanDoText = main["CanDoText"].ToString();
            Interests.Languages = main["Languages"].ToString();
            //End interests

            if (main.ContainsKey("Classifieds"))
                Classifieds = Util.OSDToDictionary((OSDMap)OSDParser.DeserializeJson(main["Classifieds"].ToString()));

            if (main.ContainsKey("Picks"))
                Picks = Util.OSDToDictionary((OSDMap)OSDParser.DeserializeJson(main["Picks"].ToString()));

            if(main.ContainsKey("Notes"))
                Notes = Util.OSDToDictionary((OSDMap)OSDParser.DeserializeJson(main["Notes"].ToString()));
            
            AboutText = main["AboutText"].ToString();
            FirstLifeImage = new UUID(main["FirstLifeImage"].ToString());
            FirstLifeAboutText = main["FirstLifeAboutText"].ToString();
            Image = new UUID(main["Image"].ToString());
            WebURL = main["WebURL"].ToString();
            Created = Convert.ToInt32(main["Created"].ToString());
            DisplayName = main["DisplayName"].ToString();
            Partner = new UUID(main["Partner"].ToString());
            Visible = Convert.ToBoolean(main["Visible"].ToString());
            AArchiveName = main["AArchiveName"].ToString();
            CustomType = main["CustomType"].ToString();
            AArchiveName = main["AArchiveName"].ToString();
            IMViaEmail = Convert.ToBoolean(main["IMViaEmail"].ToString());
            IsNewUser = Convert.ToBoolean(main["IsNewUser"].ToString());
            MembershipGroup = main["MembershipGroup"].ToString();
        }
    }

    public class ProfileInterests
    {
        public uint WantToMask = 0;
        public string WantToText = "";
        public uint CanDoMask = 0;
        public string CanDoText = "";
        public string Languages = "";
    }

    public class Classified
    {
        public UUID ClassifiedUUID;
        public UUID CreatorUUID;
        public uint CreationDate;
        public uint ExpirationDate;
        public uint Category;
        public string Name;
        public string Description;
        public UUID ParcelUUID;
        public uint ParentEstate;
        public UUID SnapshotUUID;
        public string SimName;
        public Vector3 GlobalPos;
        public string ParcelName;
        public byte ClassifiedFlags;
        public int PriceForListing;

        public Classified() { }

        public Classified(Dictionary<string, object> KVP)
        {
            ClassifiedUUID = UUID.Parse(KVP["ClassifiedUUID"].ToString());
            CreatorUUID = UUID.Parse(KVP["CreatorUUID"].ToString());
            CreationDate = uint.Parse(KVP["CreationDate"].ToString());
            ExpirationDate = uint.Parse(KVP["ExpirationDate"].ToString());
            Category = uint.Parse(KVP["Category"].ToString());
            Name = KVP["Name"].ToString();
            Description = KVP["Description"].ToString();
            ParcelUUID = UUID.Parse(KVP["ParcelUUID"].ToString());
            ParentEstate = uint.Parse(KVP["ParentEstate"].ToString());
            SnapshotUUID = UUID.Parse(KVP["SnapshotUUID"].ToString());
            SimName = KVP["SimName"].ToString();
            GlobalPos = Vector3.Parse(KVP["GlobalPos"].ToString());
            ParcelName = KVP["ParcelName"].ToString();
            ClassifiedFlags = byte.Parse(KVP["ClassifiedFlags"].ToString());
            PriceForListing = int.Parse(KVP["PriceForListing"].ToString());
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> Classified = new Dictionary<string, object>();
            Classified["ClassifiedUUID"] = ClassifiedUUID;
            Classified["CreatorUUID"] = CreatorUUID;
            Classified["CreationDate"] = CreationDate;
            Classified["ExpirationDate"] = ExpirationDate;
            Classified["Category"] = Category;
            Classified["Name"] = Name;
            Classified["Description"] = Description;
            Classified["ParcelUUID"] = ParcelUUID;
            Classified["ParentEstate"] = ParentEstate;
            Classified["SnapshotUUID"] = SnapshotUUID;
            Classified["SimName"] = SimName;
            Classified["GlobalPos"] = GlobalPos;
            Classified["ParcelName"] = ParcelName;
            Classified["ClassifiedFlags"] = ClassifiedFlags;
            Classified["PriceForListing"] = PriceForListing;
            return Classified;
        }
    }

    public class ProfilePickInfo
    {
        public UUID PickUUID;
        public UUID CreatorUUID;
        public int TopPick;
        public UUID ParcelUUID;
        public string Name;
        public string Description;
        public UUID SnapshotUUID;
        public string User;
        public string OriginalName;
        public string SimName;
        public Vector3 GlobalPos;
        public int SortOrder;
        public int Enabled;

        public ProfilePickInfo() { }

        public ProfilePickInfo(Dictionary<string, object> KVP)
        {
            PickUUID = UUID.Parse(KVP["PickUUID"].ToString());
            CreatorUUID = UUID.Parse(KVP["CreatorUUID"].ToString());
            TopPick = int.Parse(KVP["TopPick"].ToString());
            ParcelUUID = UUID.Parse(KVP["ParcelUUID"].ToString());
            Name = KVP["Name"].ToString();
            Description = KVP["Description"].ToString();
            SnapshotUUID = UUID.Parse(KVP["SnapshotUUID"].ToString());
            User = KVP["User"].ToString();
            OriginalName = KVP["OriginalName"].ToString();
            SimName = KVP["SimName"].ToString();
            GlobalPos = Vector3.Parse(KVP["GlobalPos"].ToString());
            SortOrder = int.Parse(KVP["SortOrder"].ToString());
            Enabled = int.Parse(KVP["Enabled"].ToString());
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> Pick = new Dictionary<string, object>();
            Pick["PickUUID"] = PickUUID;
            Pick["CreatorUUID"] = CreatorUUID;
            Pick["TopPick"] = TopPick;
            Pick["ParcelUUID"] = ParcelUUID;
            Pick["Name"] = Name;
            Pick["Description"] = Description;
            Pick["SnapshotUUID"] = SnapshotUUID;
            Pick["User"] = User;
            Pick["OriginalName"] = OriginalName;
            Pick["SimName"] = SimName;
            Pick["GlobalPos"] = GlobalPos;
            Pick["SortOrder"] = SortOrder;
            Pick["Enabled"] = Enabled;
            return Pick;
        }
    }
}
