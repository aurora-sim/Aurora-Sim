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
        public Dictionary<UUID, string> Notes;

        /// <summary>
        /// The picks of the user
        /// </summary>
        public ProfilePickInfo[] Picks;

        /// <summary>
        /// All the classifieds of the user
        /// </summary>
        public Classified[] Classifieds;

        public OSDMap Pack()
        {
            OSDMap main = new OSDMap();
            OSDString OSDS = new OSDString(AllowPublish.ToString());
            main["AllowPublish"] = OSDS;
            OSDS = new OSDString(MaturePublish.ToString());
            main["MaturePublish"] = OSDS;

            //Interests
            OSDS = new OSDString(Interests.WantToMask.ToString());
            main["WantToMask"] = OSDS;
            OSDS = new OSDString(Interests.WantToText.ToString());
            main["WantToText"] = OSDS;
            OSDS = new OSDString(Interests.CanDoMask.ToString());
            main["CanDoMask"] = OSDS;
            OSDS = new OSDString(Interests.CanDoText.ToString());
            main["CanDoText"] = OSDS;
            OSDS = new OSDString(Interests.Languages.ToString());
            main["Languages"] = OSDS;
            //End interests

            //Picks
            //End Picks

            //Classifieds
            //End Classifieds


            OSDS = new OSDString(AboutText.ToString());
            main["AboutText"] = OSDS;
            OSDS = new OSDString(FirstLifeImage.ToString());
            main["FirstImage"] = OSDS;
            OSDS = new OSDString(FirstLifeAboutText.ToString());
            main["FirstText"] = OSDS;
            OSDS = new OSDString(Image.ToString());
            main["Image"] = OSDS;
            OSDS = new OSDString(WebURL.ToString());
            main["WebURL"] = OSDS;
            OSDS = new OSDString(Created.ToString());
            main["Created"] = OSDS;
            OSDS = new OSDString(Partner.ToString());
            main["Partner"] = OSDS;
            return main;
        }

        public void Unpack(OSDMap main)
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
            //End Picks

            //Classifieds
            //End Classifieds


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
