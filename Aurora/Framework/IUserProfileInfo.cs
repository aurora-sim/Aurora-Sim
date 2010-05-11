using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace Aurora.Framework
{
    public class IAgentInfo
    {
        /// <summary>
        /// The ID value for this user
        /// </summary>
        public UUID PrincipalID = UUID.Zero;

        /// <summary>
        /// Banned forever
        /// </summary>
        public int PermaBanned = 0;

        /// <summary>
        /// Temperarily banned
        /// </summary>
        public int TempBanned = 0;

        /// <summary>
        /// Max maturity rating the user wishes to see
        /// </summary>
        public int MaxMaturity = 2;

        /// <summary>
        /// Is this user a minor?
        /// </summary>
        public bool IsMinor = false;

        /// <summary>
        /// Current Mac Address
        /// </summary>
        public string Mac = "";

        /// <summary>
        /// Current IP Address
        /// </summary>
        public string IP = "";

        /// <summary>
        /// Did this user accept the TOS?
        /// </summary>
        public bool AcceptTOS = false;

        /// <summary>
        /// User's first name
        /// </summary>
        public string RealFirst = "";

        /// <summary>
        /// User's last name
        /// </summary>
        public string RealLast = "";

        /// <summary>
        /// User's address
        /// </summary>
        public string RealAddress = "";

        /// <summary>
        /// User's Zip code
        /// </summary>
        public string RealZip = "";

        /// <summary>
        /// Current IP Address
        /// </summary>
        public string RealCountry = "";

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
                UUID.TryParse(kvp["PrincipalID"].ToString(), out PrincipalID);
            PermaBanned = 0;
            if (kvp.ContainsKey("PermaBanned") && kvp["PermaBanned"] != null)
                Int32.TryParse(kvp["PermaBanned"].ToString(), out PermaBanned);
            TempBanned = 0;
            if (kvp.ContainsKey("TempBanned") && kvp["TempBanned"] != null)
                Int32.TryParse(kvp["TempBanned"].ToString(), out TempBanned);
            MaxMaturity = 0;
            if (kvp.ContainsKey("MaxMaturity") && kvp["MaxMaturity"] != null)
                Int32.TryParse(kvp["MaxMaturity"].ToString(), out MaxMaturity);
            IsMinor = false;
            if (kvp.ContainsKey("IsMinor") && kvp["IsMinor"] != null)
                bool.TryParse(kvp["TheirFlags"].ToString(), out IsMinor);
            Mac = "";
            if (kvp.ContainsKey("Mac") && kvp["Mac"] != null)
                Mac = kvp["Mac"].ToString();
            IP = "";
            if (kvp.ContainsKey("IP") && kvp["IP"] != null)
                IP = kvp["IP"].ToString();
            RealFirst = "";
            if (kvp.ContainsKey("RealFirst") && kvp["RealFirst"] != null)
                RealFirst = kvp["RealFirst"].ToString();
            RealLast = "";
            if (kvp.ContainsKey("RealLast") && kvp["RealLast"] != null)
                RealLast = kvp["RealLast"].ToString();
            RealAddress = "";
            if (kvp.ContainsKey("RealAddress") && kvp["RealAddress"] != null)
                RealAddress = kvp["RealAddress"].ToString();
            RealZip = "";
            if (kvp.ContainsKey("RealZip") && kvp["RealZip"] != null)
                RealZip = kvp["RealZip"].ToString();
            RealCountry = "";
            if (kvp.ContainsKey("RealCountry") && kvp["RealCountry"] != null)
                RealCountry = kvp["RealCountry"].ToString();
            Language = "";
            if (kvp.ContainsKey("Language") && kvp["Language"] != null)
                Language = kvp["Language"].ToString();
            AcceptTOS = true;
            if (kvp.ContainsKey("AcceptTOS") && kvp["AcceptTOS"] != null)
                bool.TryParse(kvp["AcceptTOS"].ToString(), out AcceptTOS);
            LanguageIsPublic = true;
            if (kvp.ContainsKey("LanguageIsPublic") && kvp["LanguageIsPublic"] != null)
                bool.TryParse(kvp["LanguageIsPublic"].ToString(), out LanguageIsPublic);
        }

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            result["PrincipalID"] = PrincipalID.ToString();
            result["PermaBanned"] = PermaBanned;
            result["TempBanned"] = TempBanned;
            result["MaxMaturity"] = MaxMaturity;
            result["IsMinor"] = IsMinor.ToString();
            result["Mac"] = Mac.ToString();
            result["IP"] = IP.ToString();
            result["RealFirst"] = RealFirst.ToString();
            result["RealLast"] = RealLast.ToString();
            result["RealAddress"] = RealAddress.ToString();
            result["RealZip"] = RealZip.ToString();
            result["RealCountry"] = RealCountry.ToString();
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
        public UUID PrincipalID;

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
        public UUID Partner;

        /// <summary>
        /// the web address of the Profile URL
        /// </summary>
        public string WebURL = "";

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
        public UUID Image;

        /// <summary>
        /// The profile image for the users first life tab
        /// </summary>
        public UUID FirstLifeImage;

        /// <summary>
        /// The type of the user
        /// </summary>
        public string CustomType;

        /// <summary>
        /// Is this user's online status visible to others?
        /// </summary>
        public bool Visible;

        /// <summary>
        /// Should IM's be sent to the user's email?
        /// </summary>
        public bool IMViaEmail;

        /// <summary>
        /// The appearance archive to load for this user
        /// </summary>
        public string AArchiveName;

        /// <summary>
        /// Is the user a new user?
        /// </summary>
        public bool IsNewUser;

        /// <summary>
        /// The group that the user is assigned to, ex: Premium
        /// </summary>
        public string MembershipGroup;

        /// <summary>
        /// A UNIX Timestamp (seconds since epoch) for the users creation
        /// </summary>
        public int Created;

        /// <summary>
        /// The interests of the user
        /// </summary>
        public ProfileInterests Interests;

        /// <summary>
        /// All of the notes of the user
        /// </summary>
        /// UUID - target agent
        /// string - notes
        public Dictionary<string, string> Notes;

        /// <summary>
        /// The picks of the user
        /// </summary>
        public ProfilePickInfo[] Picks;

        /// <summary>
        /// All the classifieds of the user
        /// </summary>
        public Classified[] Classifieds;

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
            result["Partner"] = Partner.ToString();

            //Classifieds
            Dictionary<string, object> ClassifiedsKVP = new Dictionary<string, object>();
            foreach(Classified CFI in Classifieds)
            {
                if (CFI.ClassifiedUUID == "")
                    continue;
                ClassifiedsKVP[CFI.ClassifiedUUID.ToString()] = CFI.ToKeyValuePairs();
            }
            result["Classifieds"] = ClassifiedsKVP;

            //Classifieds
            Dictionary<string, object> PicksKVP = new Dictionary<string, object>();
            foreach (ProfilePickInfo PPI in Picks)
            {
                if (PPI.pickuuid == "")
                    continue;
                PicksKVP[PPI.pickuuid.ToString()] = PPI.ToKeyValuePairs();
            }
            result["Picks"] = PicksKVP;

            result["Notes"] = Notes;

            return result;
        }

        public IUserProfileInfo() { }

        public IUserProfileInfo(Dictionary<string, object> main)
        {
            AllowPublish = Convert.ToBoolean(main["AllowPublish"].ToString());
            MaturePublish = Convert.ToBoolean(main["MaturePublish"].ToString());

            //Interests
            Interests = new ProfileInterests();
            Interests.WantToMask = main["WantToMask"].ToString();
            Interests.WantToText = main["WantToText"].ToString();
            Interests.CanDoMask = main["CanDoMask"].ToString();
            Interests.CanDoText = main["CanDoText"].ToString();
            Interests.Languages = main["Languages"].ToString();
            //End interests

            //Picks
            Dictionary<string, Dictionary<string, object>> AllPicksKVP = main["Picks"] as Dictionary<string, Dictionary<string, object>>;
            List<ProfilePickInfo> AllPicks = new List<ProfilePickInfo>();
            if (AllPicksKVP != null)
            {
                foreach (Dictionary<string, object> PPI in AllPicksKVP.Values)
                {
                    ProfilePickInfo Pick = new ProfilePickInfo(PPI);
                    AllPicks.Add(Pick);
                }
            }
            Picks = AllPicks.ToArray();

            //Classifieds
            Dictionary<string, Dictionary<string, object>> AllClassifiedsKVP = main["Classifieds"] as Dictionary<string, Dictionary<string, object>>;
            List<Classified> AllClassifieds = new List<Classified>();
            if (AllPicksKVP != null)
            {
                foreach (Dictionary<string, object> PPI in AllPicksKVP.Values)
                {
                    Classified Classified = new Classified(PPI);
                    AllClassifieds.Add(Classified);
                }
            }
            Classifieds = AllClassifieds.ToArray();

            Dictionary<string, string> notes = main["Notes"] as Dictionary<string, string>;
            if (notes != null)
            {
                Notes = notes;
            }
            AboutText = main["AboutText"].ToString();
            FirstLifeImage = new UUID(main["FirstLifeImage"].ToString());
            FirstLifeAboutText = main["FirstLifeAboutText"].ToString();
            Image = new UUID(main["Image"].ToString());
            WebURL = main["WebURL"].ToString();
            Created = Convert.ToInt32(main["Created"].ToString());
            Partner = new UUID(main["Partner"].ToString());
        }
    }
    public class ProfileInterests
    {
        public string WantToMask;
        public string WantToText;
        public string CanDoMask;
        public string CanDoText;
        public string Languages;
    }

    public class Classified
    {
        public Classified() { }
        public Classified(Dictionary<string, object> KVP)
        {
            Category = KVP["Category"].ToString();
            ClassifiedFlags = KVP["ClassifiedFlags"].ToString();
            ClassifiedUUID = KVP["ClassifiedUUID"].ToString();
            CreationDate = KVP["CreationDate"].ToString();
            CreatorUUID = KVP["CreatorUUID"].ToString();
            Description = KVP["Description"].ToString();
            ExpirationDate = KVP["ExpirationDate"].ToString();
            Name = KVP["Name"].ToString();
            ParcelName = KVP["ParcelName"].ToString();
            ParcelUUID = KVP["ParcelUUID"].ToString();
            ParentEstate = KVP["ParentEstate"].ToString();
            PosGlobal = KVP["PosGlobal"].ToString();
            PriceForListing = KVP["PriceForListing"].ToString();
            SimName = KVP["SimName"].ToString();
            SnapshotUUID = KVP["SnapshotUUID"].ToString();
        }
        public string ClassifiedUUID;
        public string CreatorUUID;
        public string CreationDate;
        public string ExpirationDate;
        public string Category;
        public string Name;
        public string Description;
        public string ParcelUUID;
        public string ParentEstate;
        public string SnapshotUUID;
        public string SimName;
        public string PosGlobal;
        public string ParcelName;
        public string ClassifiedFlags;
        public string PriceForListing;

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> Classified = new Dictionary<string, object>();
            Classified["Category"] = Category;
            Classified["ClassifiedFlags"] = ClassifiedFlags;
            Classified["ClassifiedUUID"] = ClassifiedUUID;
            Classified["CreationDate"] = CreationDate;
            Classified["CreatorUUID"] = CreatorUUID;
            Classified["Description"] = Description;
            Classified["ExpirationDate"] = ExpirationDate;
            Classified["Name"] = Name;
            Classified["ParcelName"] = ParcelName;
            Classified["ParcelUUID"] = ParcelUUID;
            Classified["ParentEstate"] = ParentEstate;
            Classified["PosGlobal"] = PosGlobal;
            Classified["PriceForListing"] = PriceForListing;
            Classified["SimName"] = SimName;
            Classified["SnapshotUUID"] = SnapshotUUID;
            return Classified;
        }
    }

    public class ProfilePickInfo
    {
        public ProfilePickInfo() { }
        public ProfilePickInfo(Dictionary<string, object> KVP)
        {
            creatoruuid = KVP["creatoruuid"].ToString();
            description = KVP["description"].ToString();
            enabled = KVP["enabled"].ToString();
            name = KVP["name"].ToString();
            originalname = KVP["originalname"].ToString();
            parceluuid = KVP["parceluuid"].ToString();
            pickuuid = KVP["pickuuid"].ToString();
            posglobal = KVP["posglobal"].ToString();
            simname = KVP["simname"].ToString();
            snapshotuuid = KVP["snapshotuuid"].ToString();
            sortorder = KVP["sortorder"].ToString();
            toppick = KVP["toppick"].ToString();
            user = KVP["user"].ToString();
        }
        public string pickuuid;
        public string creatoruuid;
        public string toppick;
        public string parceluuid;
        public string name;
        public string description;
        public string snapshotuuid;
        public string user;
        public string originalname;
        public string simname;
        public string posglobal;
        public string sortorder;
        public string enabled;

        public Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> Pick = new Dictionary<string, object>();
            Pick["Category"] = creatoruuid;
            Pick["ClassifiedFlags"] = description;
            Pick["ClassifiedUUID"] = name;
            Pick["CreationDate"] = originalname;
            Pick["CreatorUUID"] = parceluuid;
            Pick["Description"] = pickuuid;
            Pick["Name"] = posglobal;
            Pick["ParcelName"] = simname;
            Pick["ParcelUUID"] = snapshotuuid;
            Pick["ParentEstate"] = sortorder;
            Pick["PosGlobal"] = toppick;
            Pick["PriceForListing"] = user;
            return Pick;
        }
    }
}
