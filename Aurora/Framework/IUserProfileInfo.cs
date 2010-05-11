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
            result["MaturePublish"] = MaturePublish;
            result["WantToMask"] = Interests.WantToMask;
            result["WantToText"] = Interests.WantToText;
            result["CanDoMask"] = Interests.CanDoMask;
            result["CanDoText"] = Interests.CanDoText;
            result["Languages"] = Interests.Languages;
            result["AboutText"] = AboutText.ToString();
            result["FirstLifeImage"] = FirstLifeImage;
            result["FirstLifeAboutText"] = FirstLifeAboutText;
            result["Image"] = Image;
            result["WebURL"] = WebURL;
            result["Created"] = Created;
            result["Partner"] = Partner;

            //Classifieds
            Dictionary<string, object> ClassifiedsKVP = new Dictionary<string, object>();
            foreach(Classified CFI in Classifieds)
            {
                Dictionary<string, object> Classified = new Dictionary<string, object>();
                Classified["Category"] = CFI.Category;
                Classified["ClassifiedFlags"] = CFI.ClassifiedFlags;
                Classified["ClassifiedUUID"] = CFI.ClassifiedUUID;
                Classified["CreationDate"] = CFI.CreationDate;
                Classified["CreatorUUID"] = CFI.CreatorUUID;
                Classified["Description"] = CFI.Description;
                Classified["ExpirationDate"] = CFI.ExpirationDate;
                Classified["Name"] = CFI.Name;
                Classified["ParcelName"] = CFI.ParcelName;
                Classified["ParcelUUID"] = CFI.ParcelUUID;
                Classified["ParentEstate"] = CFI.ParentEstate;
                Classified["PosGlobal"] = CFI.PosGlobal;
                Classified["PriceForListing"] = CFI.PriceForListing;
                Classified["SimName"] = CFI.SimName;
                Classified["SnapshotUUID"] = CFI.SnapshotUUID;
                ClassifiedsKVP[CFI.ClassifiedUUID.ToString()] = Classified;
            }
            result["Classifieds"] = ClassifiedsKVP;

            //Classifieds
            Dictionary<string, object> PicksKVP = new Dictionary<string, object>();
            foreach (ProfilePickInfo PPI in Picks)
            {
                Dictionary<string, object> Pick = new Dictionary<string, object>();
                Pick["Category"] = PPI.creatoruuid;
                Pick["ClassifiedFlags"] = PPI.description;
                Pick["ClassifiedUUID"] = PPI.name;
                Pick["CreationDate"] = PPI.originalname;
                Pick["CreatorUUID"] = PPI.parceluuid;
                Pick["Description"] = PPI.pickuuid;
                Pick["Name"] = PPI.posglobal;
                Pick["ParcelName"] = PPI.simname;
                Pick["ParcelUUID"] = PPI.snapshotuuid;
                Pick["ParentEstate"] = PPI.sortorder;
                Pick["PosGlobal"] = PPI.toppick;
                Pick["PriceForListing"] = PPI.user;
                PicksKVP[PPI.pickuuid.ToString()] = Pick;
            }
            result["Picks"] = PicksKVP;

            result["Notes"] = Notes;

            return result;
        }

        public IUserProfileInfo() { }

        public IUserProfileInfo(Dictionary<string, object> main)
        {
            AllowPublish = bool.Parse(main["AllowPublish"].ToString());
            MaturePublish = bool.Parse(main["MaturePublish"].ToString());

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
            foreach (Dictionary<string, object> PPI in AllPicksKVP.Values)
            {
                ProfilePickInfo Pick = new ProfilePickInfo();
                Pick.creatoruuid = PPI["creatoruuid"].ToString();
                Pick.description = PPI["description"].ToString();
                Pick.enabled = PPI["enabled"].ToString();
                Pick.name = PPI["name"].ToString();
                Pick.originalname = PPI["originalname"].ToString();
                Pick.parceluuid = PPI["parceluuid"].ToString();
                Pick.pickuuid = PPI["pickuuid"].ToString();
                Pick.posglobal = PPI["posglobal"].ToString();
                Pick.simname = PPI["simname"].ToString();
                Pick.snapshotuuid = PPI["snapshotuuid"].ToString();
                Pick.sortorder = PPI["sortorder"].ToString();
                Pick.toppick = PPI["toppick"].ToString();
                Pick.user = PPI["user"].ToString();
                AllPicks.Add(Pick);
            }
            Picks = AllPicks.ToArray();

            //Classifieds
            Dictionary<string, Dictionary<string, object>> AllClassifiedsKVP = main["Classifieds"] as Dictionary<string, Dictionary<string, object>>;
            List<Classified> AllClassifieds = new List<Classified>();
            foreach (Dictionary<string, object> PPI in AllPicksKVP.Values)
            {
                Classified Classified = new Classified();
                Classified.Category = PPI["Category"].ToString();
                Classified.ClassifiedFlags = PPI["ClassifiedFlags"].ToString();
                Classified.ClassifiedUUID = PPI["ClassifiedUUID"].ToString();
                Classified.CreationDate = PPI["CreationDate"].ToString();
                Classified.CreatorUUID = PPI["CreatorUUID"].ToString();
                Classified.Description = PPI["Description"].ToString();
                Classified.ExpirationDate = PPI["ExpirationDate"].ToString();
                Classified.Name = PPI["Name"].ToString();
                Classified.ParcelName = PPI["ParcelName"].ToString();
                Classified.ParcelUUID = PPI["ParcelUUID"].ToString();
                Classified.ParentEstate = PPI["ParentEstate"].ToString();
                Classified.PosGlobal = PPI["PosGlobal"].ToString();
                Classified.PriceForListing = PPI["PriceForListing"].ToString();
                Classified.SimName = PPI["SimName"].ToString();
                Classified.SnapshotUUID = PPI["SnapshotUUID"].ToString();
                AllClassifieds.Add(Classified);
            }
            Classifieds = AllClassifieds.ToArray();

            Notes = main["Notes"] as Dictionary<string, string>;

            AboutText = main["AboutText"].ToString();
            FirstLifeImage = new UUID(main["FirstImage"].ToString());
            FirstLifeAboutText = main["FirstText"].ToString();
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
    }

    public class ProfilePickInfo
    {
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
    }
}
